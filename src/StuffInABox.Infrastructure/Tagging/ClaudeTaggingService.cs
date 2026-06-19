using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StuffInABox.Application.Common.Interfaces;

namespace StuffInABox.Infrastructure.Tagging;

/// <summary>
/// Generates 4–7 broad Swedish tags (synonyms, category, material, use) via the
/// Claude Messages API. Honors the ITaggingService contract: never throws —
/// returns an empty list on any failure so enrichment never blocks a save.
/// </summary>
public sealed class ClaudeTaggingService(
    HttpClient http,
    IConfiguration config,
    ILogger<ClaudeTaggingService> logger) : ITaggingService
{
    private const string Endpoint = "https://api.anthropic.com/v1/messages";
    private const string AnthropicVersion = "2023-06-01";

    private string? ApiKey => config["Tagging:Claude:ApiKey"];
    private string Model => config["Tagging:Claude:Model"] ?? "claude-haiku-4-5-20251001";

    public async Task<IReadOnlyList<string>> GenerateTagsAsync(string itemName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            logger.LogWarning("Claude tagging selected but Tagging:Claude:ApiKey is not configured.");
            return [];
        }

        try
        {
            var prompt =
                $"Föremål: \"{itemName}\".\n" +
                "Ge 4–7 svenska sökord som hjälper någon hitta detta föremål: synonymer, " +
                "överordnad kategori, material och användning. Svara ENDAST med en JSON-array " +
                "av gemena strängar, inget annat. Exempel: [\"jacka\",\"ytterkläder\",\"vinter\"].";

            var requestBody = new
            {
                model = Model,
                max_tokens = 200,
                messages = new[] { new { role = "user", content = prompt } },
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, Endpoint)
            {
                Content = JsonContent.Create(requestBody),
            };
            req.Headers.Add("x-api-key", ApiKey);
            req.Headers.Add("anthropic-version", AnthropicVersion);

            using var resp = await http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
            {
                logger.LogWarning("Claude tagging returned {Status} for '{Item}'.", resp.StatusCode, itemName);
                return [];
            }

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
            var text = doc.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString();

            return ParseTags(text);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Claude tagging failed for '{Item}'.", itemName);
            return [];
        }
    }

    private static IReadOnlyList<string> ParseTags(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return [];

        // Be tolerant: extract the first JSON array from the response.
        var start = text.IndexOf('[');
        var end = text.LastIndexOf(']');
        if (start < 0 || end <= start) return [];

        try
        {
            var arr = JsonSerializer.Deserialize<List<string>>(text[start..(end + 1)]);
            return arr is null
                ? []
                : arr.Where(t => !string.IsNullOrWhiteSpace(t))
                     .Select(t => t.Trim().ToLowerInvariant())
                     .Distinct()
                     .Take(7)
                     .ToList();
        }
        catch
        {
            return [];
        }
    }
}
