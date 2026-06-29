using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StuffInABox.Application.Common.Interfaces;

namespace StuffInABox.Infrastructure.Recognition;

/// <summary>
/// Analyzes a photo with the Staik vision API (<c>https://api.staik.se</c>) and
/// returns a Swedish name + searchable tags. Staik exposes an OpenAI-compatible
/// Chat Completions endpoint, so the image is sent as a base64 data URI in an
/// <c>image_url</c> message part and the model's reply comes back in
/// <c>choices[0].message.content</c>.
///
/// The prompt and the defensive JSON-to-tags parsing are shared with the other
/// vision providers — see <see cref="VisionRecognition"/>. This service only
/// handles the Staik transport. Honors the never-throws contract — returns null
/// on any failure, so the add-item flow is never blocked.
/// </summary>
public sealed class StaikImageRecognitionService(
    HttpClient http,
    IConfiguration config,
    ILogger<StaikImageRecognitionService> logger) : IImageRecognitionService
{
    private string BaseUrl => (config["ImageRecognition:Staik:BaseUrl"] ?? "https://api.staik.se").TrimEnd('/');

    // Only gemma4:31b currently has vision on Staik; configurable so a stronger
    // vision model can be swapped in without a code change.
    private string Model => config["ImageRecognition:Staik:Model"] ?? "gemma4:31b";

    private string? ApiKey => config["ImageRecognition:Staik:ApiKey"];

    public async Task<RecognitionResult?> RecognizeAsync(byte[] imageBytes, CancellationToken ct = default)
    {
        if (imageBytes.Length == 0) return null;

        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            logger.LogWarning("Staik recognition skipped: no API key configured.");
            return null;
        }

        try
        {
            var dataUri = $"data:image/jpeg;base64,{Convert.ToBase64String(imageBytes)}";
            var requestBody = new
            {
                model = Model,
                temperature = 0,
                response_format = new { type = "json_object" },
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "text", text = VisionRecognition.Prompt },
                            new { type = "image_url", image_url = new { url = dataUri } },
                        },
                    },
                },
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/v1/chat/completions")
            {
                Content = JsonContent.Create(requestBody),
            };
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);

            using var resp = await http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
            {
                logger.LogWarning("Staik recognition returned {Status}.", resp.StatusCode);
                return null;
            }

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
            var text = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
            return VisionRecognition.Parse(text);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Staik recognition failed.");
            return null;
        }
    }
}
