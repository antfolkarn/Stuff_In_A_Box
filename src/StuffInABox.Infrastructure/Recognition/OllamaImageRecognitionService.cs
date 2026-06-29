using System.Net.Http.Headers;
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
/// The prompt and the defensive JSON-to-tags parsing are shared across vision
/// providers — see <see cref="VisionRecognition"/> for the guardrails and the
/// "structured fields flattened into tags" design. This service only handles the
/// Ollama transport. Honors the never-throws contract — returns null on any failure.
/// </summary>
public sealed class OllamaImageRecognitionService(
    HttpClient http,
    IConfiguration config,
    ILogger<OllamaImageRecognitionService> logger) : IImageRecognitionService
{
    private string BaseUrl => (config["ImageRecognition:Ollama:BaseUrl"] ?? "http://localhost:11434").TrimEnd('/');
    private string Model => config["ImageRecognition:Ollama:Model"] ?? "llava";

    // Optional bearer token — set when the Ollama endpoint is behind an auth proxy
    // (e.g. a self-hosted model exposed to the cloud via a tunnel). Empty = no header.
    private string? ApiKey => config["ImageRecognition:Ollama:ApiKey"];

    public async Task<RecognitionResult?> RecognizeAsync(byte[] imageBytes, CancellationToken ct = default)
    {
        if (imageBytes.Length == 0) return null;

        try
        {
            var requestBody = new
            {
                model = Model,
                prompt = VisionRecognition.Prompt,
                images = new[] { Convert.ToBase64String(imageBytes) },
                stream = false,
                format = "json",
                options = new { temperature = 0 },
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/api/generate")
            {
                Content = JsonContent.Create(requestBody),
            };
            if (!string.IsNullOrWhiteSpace(ApiKey))
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);

            using var resp = await http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
            {
                logger.LogWarning("Ollama recognition returned {Status}.", resp.StatusCode);
                return null;
            }

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
            var text = doc.RootElement.TryGetProperty("response", out var r) ? r.GetString() : null;
            return VisionRecognition.Parse(text);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Ollama recognition failed.");
            return null;
        }
    }
}
