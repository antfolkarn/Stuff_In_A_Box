using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StuffInABox.Application.Common.Interfaces;

namespace StuffInABox.Infrastructure.Recognition;

/// <summary>
/// Recognizes a photo's main object with a local vision model served by Ollama
/// (e.g. llava, qwen2.5vl, llama3.2-vision). Everything stays on the user's
/// machine; no per-call cost. Honors the never-throws contract: returns null on
/// any failure so the add-item flow keeps working even if Ollama is down.
/// </summary>
public sealed class OllamaImageRecognitionService(
    HttpClient http,
    IConfiguration config,
    ILogger<OllamaImageRecognitionService> logger) : IImageRecognitionService
{
    private const string Prompt =
        "Vad föreställer bilden? Svara med ENDAST ett kort svenskt substantiv på 1–3 ord " +
        "som namnger föremålet. Ingen mening, ingen förklaring, ingen punkt.";

    private string BaseUrl => (config["ImageRecognition:Ollama:BaseUrl"] ?? "http://localhost:11434").TrimEnd('/');
    private string Model => config["ImageRecognition:Ollama:Model"] ?? "llava";

    public async Task<string?> RecognizeAsync(byte[] imageBytes, CancellationToken ct = default)
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
            return Clean(text);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Ollama recognition failed.");
            return null;
        }
    }

    /// <summary>Reduces a model reply to a single short noun phrase.</summary>
    private static string? Clean(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var first = text.Trim().Split('\n')[0].Trim().Trim('"', '\'', '.', ' ');
        if (first.Length == 0) return null;
        if (first.Length > 50) first = first[..50].Trim();

        return char.ToUpperInvariant(first[0]) + first[1..];
    }
}
