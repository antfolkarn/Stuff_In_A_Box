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
/// What is extracted: the model fills a small set of *structured* property fields
/// (category, objects, colours, materials, brand, visible text, details) so the
/// answer captures how people actually search for their belongings. Those fields
/// are then flattened into the flat tag list the rest of the pipeline (storage,
/// <c>ItemRepository.SearchAsync</c>) already works against — so the richer prompt
/// needs no schema or search changes. <see cref="Prompt"/> defines the fields and
/// the model is set via config, so new categories or a stronger model can be added
/// without touching the pipeline.
/// </summary>
public sealed class OllamaImageRecognitionService(
    HttpClient http,
    IConfiguration config,
    ILogger<OllamaImageRecognitionService> logger) : IImageRecognitionService
{
    private const int MaxTags = 20;
    private const int MaxTagLength = 40;
    private const int MaxNameLength = 60;

    // Null-ish placeholders models sometimes emit (e.g. "märke":"None") — dropped so they
    // don't become junk tags or a junk name.
    private static readonly HashSet<string> JunkValues =
    [
        "none", "null", "n/a", "na", "ingen", "inga", "saknas", "okänd", "okänt",
        "unknown", "ej angivet", "ingen text", "ingen logga", "-",
    ];

    // The structured property fields we flatten into tags, in priority order
    // (most identifying first, so the cap keeps the best keywords).
    private static readonly string[] TagFields =
        ["föremål", "märke", "färger", "material", "kategori", "text", "detaljer"];

    private const string Prompt =
        "Analysera bilden och svara ENDAST med ett JSON-objekt, inget annat, ingen markdown.\n" +
        "Beskriv föremålet/föremålen så att man senare kan SÖKA på dem efter egenskaper.\n" +
        "Format (fyll bara i det du faktiskt ser, hoppa över det som inte syns):\n" +
        "{\n" +
        "  \"namn\": \"kort svensk rubrik, 1–4 ord, gärna med färg/märke\",\n" +
        "  \"kategori\": \"övergripande kategori, t.ex. verktyg, kläder, elektronik, kök, leksaker\",\n" +
        "  \"föremål\": [\"varje föremål för sig\"],\n" +
        "  \"färger\": [\"synliga färger\"],\n" +
        "  \"material\": [\"t.ex. trä, metall, plast, glas, tyg\"],\n" +
        "  \"märke\": \"varumärke/logga om det syns, annars utelämna\",\n" +
        "  \"text\": [\"text som syns: etiketter, boktitlar, modellnummer\"],\n" +
        "  \"detaljer\": [\"övriga egenskaper: storlek, skick, t.ex. sladdlös, begagnad, stl m\"]\n" +
        "}\n" +
        "Regler: endast svenska, gemener i listorna, inga meningar, inga förklaringar, inga dubbletter. " +
        "Gissa inte storlek eller mått om det inte tydligt syns. Ange \"märke\" bara om varumärkets namn " +
        "faktiskt går att läsa i bilden — gissa aldrig ett varumärke utifrån en symbol eller logga. " +
        "Utelämna \"märke\" annars, och skriv aldrig \"None\", \"ingen\" eller liknande platshållare. " +
        "Om bilden visar flera olika saker: lista alla under \"föremål\" och välj en övergripande \"namn\" " +
        "(t.ex. \"Blandade saker\").\n" +
        "Exempel: {\"namn\":\"Röd Bosch borrmaskin\",\"kategori\":\"verktyg\",\"föremål\":[\"borrmaskin\"," +
        "\"batteriladdare\"],\"färger\":[\"röd\",\"svart\"],\"material\":[\"plast\",\"metall\"]," +
        "\"märke\":\"bosch\",\"text\":[\"gsr 18v-50\"],\"detaljer\":[\"sladdlös\"]}";

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

    /// <summary>
    /// Parses the model's structured JSON reply into a normalized result, tolerant of
    /// surrounding prose. The structured property fields are flattened into the flat tag
    /// list (priority order, normalized, de-duplicated, count-capped).
    /// </summary>
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

            // Flatten every structured field into the tag list, best keywords first.
            var tags = new List<string>();
            foreach (var field in TagFields)
            {
                if (tags.Count >= MaxTags) break;
                if (root.TryGetProperty(field, out var el))
                    AddValues(el, tags);
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

    /// <summary>Adds a field's value(s) — a string or an array of strings — as normalized tags.</summary>
    private static void AddValues(JsonElement el, List<string> tags)
    {
        switch (el.ValueKind)
        {
            case JsonValueKind.String:
                AddTag(el.GetString(), tags);
                break;
            case JsonValueKind.Array:
                foreach (var item in el.EnumerateArray())
                {
                    if (tags.Count >= MaxTags) break;
                    if (item.ValueKind == JsonValueKind.String)
                        AddTag(item.GetString(), tags);
                }
                break;
        }
    }

    private static void AddTag(string? value, List<string> tags)
    {
        if (tags.Count >= MaxTags) return;
        var tag = CleanTag(value);
        if (tag is not null && !tags.Contains(tag))
            tags.Add(tag);
    }

    private static string? CleanName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var name = value.Trim().Split('\n')[0].Trim().Trim('"', '\'', '.', ' ');
        if (name.Length == 0 || JunkValues.Contains(name.ToLowerInvariant())) return null;
        if (name.Length > MaxNameLength) name = name[..MaxNameLength].Trim();
        return char.ToUpperInvariant(name[0]) + name[1..];
    }

    private static string? CleanTag(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var tag = value.Trim().Trim('"', '\'', '.', ',', ' ').ToLowerInvariant();
        if (tag.Length == 0 || JunkValues.Contains(tag)) return null;
        if (tag.Length > MaxTagLength) tag = tag[..MaxTagLength];
        return tag;
    }
}
