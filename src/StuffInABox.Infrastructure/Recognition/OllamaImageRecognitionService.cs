using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StuffInABox.Application.Common.Interfaces;

namespace StuffInABox.Infrastructure.Recognition;

/// <summary>
/// Analyzes a photo with a local Ollama vision model (e.g. llava) and returns a
/// Swedish name + searchable tags. Everything stays on the user's machine.
///
/// Guardrails: the model is told to answer ONLY with a strict JSON object, in
/// Swedish; the response is then parsed defensively and the tags are normalized
/// (lowercased, de-duplicated, length- and count-capped). Honors the never-throws
/// contract — returns null on any failure.
///
/// Extension points: <see cref="Prompt"/> defines what is extracted (objects,
/// colours, material, book titles, …) and the model is set via config, so new
/// categories or a stronger model can be added without touching the pipeline.
/// </summary>
public sealed class OllamaImageRecognitionService(
    HttpClient http,
    IConfiguration config,
    ILogger<OllamaImageRecognitionService> logger) : IImageRecognitionService
{
    private const int MaxTags = 15;
    private const int MaxTagLength = 40;
    private const int MaxNameLength = 60;

    private const string Prompt =
        "Analysera bilden och svara ENDAST med ett JSON-objekt, inget annat, ingen markdown.\n" +
        "Format: {\"namn\": \"...\", \"taggar\": [\"...\", \"...\"]}\n" +
        "- \"namn\": en kort svensk rubrik (1–4 ord) för det viktigaste föremålet, gärna med " +
        "färg eller material om det är tydligt (t.ex. \"Röd jacka\", \"Ekskiva\", \"Motivglas\"). " +
        "Om bilden visar flera olika föremål, använd en övergripande rubrik (t.ex. \"Blandade saker\").\n" +
        "- \"taggar\": 4–15 svenska sökord med gemener. Inkludera föremålet eller föremålen, " +
        "färger, material och kategori. Om det är böcker: ta med boktitlarna. Om det är flera " +
        "föremål: lista alla som egna taggar.\n" +
        "Endast svenska. Inga meningar, inga förklaringar, inga dubbletter. " +
        "Exempel: {\"namn\":\"Röd jacka\",\"taggar\":[\"jacka\",\"röd\",\"ytterkläder\",\"vinter\"]}";

    private string BaseUrl => (config["ImageRecognition:Ollama:BaseUrl"] ?? "http://localhost:11434").TrimEnd('/');
    private string Model => config["ImageRecognition:Ollama:Model"] ?? "llava";

    public async Task<RecognitionResult?> RecognizeAsync(byte[] imageBytes, CancellationToken ct = default)
    {
        if (imageBytes.Length == 0) return null;

        try
        {
            var requestBody = new
            {
                model = Model,
                prompt = Prompt,
                images = new[] { Convert.ToBase64String(imageBytes) },
                stream = false,
                format = "json",
                options = new { temperature = 0 },
            };

            using var resp = await http.PostAsJsonAsync($"{BaseUrl}/api/generate", requestBody, ct);
            if (!resp.IsSuccessStatusCode)
            {
                logger.LogWarning("Ollama recognition returned {Status}.", resp.StatusCode);
                return null;
            }

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
            var text = doc.RootElement.TryGetProperty("response", out var r) ? r.GetString() : null;
            return Parse(text);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Ollama recognition failed.");
            return null;
        }
    }

    /// <summary>Parses the model's JSON reply into a normalized result, tolerant of surrounding prose.</summary>
    private static RecognitionResult? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start) return null;

        try
        {
            using var doc = JsonDocument.Parse(text[start..(end + 1)]);
            var root = doc.RootElement;

            var name = root.TryGetProperty("namn", out var n) ? CleanName(n.GetString()) : null;

            var tags = new List<string>();
            if (root.TryGetProperty("taggar", out var t) && t.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in t.EnumerateArray())
                {
                    var tag = CleanTag(el.ValueKind == JsonValueKind.String ? el.GetString() : null);
                    if (tag is not null && !tags.Contains(tag))
                        tags.Add(tag);
                    if (tags.Count >= MaxTags) break;
                }
            }

            // Fall back to the first tag as a name if the model omitted one.
            if (name is null && tags.Count > 0)
                name = char.ToUpperInvariant(tags[0][0]) + tags[0][1..];

            if (name is null && tags.Count == 0) return null;
            return new RecognitionResult(name, tags);
        }
        catch
        {
            return null;
        }
    }

    private static string? CleanName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var name = value.Trim().Split('\n')[0].Trim().Trim('"', '\'', '.', ' ');
        if (name.Length == 0) return null;
        if (name.Length > MaxNameLength) name = name[..MaxNameLength].Trim();
        return char.ToUpperInvariant(name[0]) + name[1..];
    }

    private static string? CleanTag(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var tag = value.Trim().Trim('"', '\'', '.', ',', ' ').ToLowerInvariant();
        if (tag.Length is 0 or > MaxTagLength) return tag.Length == 0 ? null : tag[..MaxTagLength];
        return tag;
    }
}
