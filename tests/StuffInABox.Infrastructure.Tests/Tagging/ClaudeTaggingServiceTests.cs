using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using StuffInABox.Infrastructure.Tagging;

namespace StuffInABox.Infrastructure.Tests.Tagging;

public class ClaudeTaggingServiceTests
{
    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
    }

    private static IConfiguration Config(string? apiKey) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Tagging:Claude:ApiKey"] = apiKey,
            })
            .Build();

    private static ClaudeTaggingService Create(HttpStatusCode status, string body, string? apiKey = "sk-test") =>
        new(new HttpClient(new StubHandler(status, body)), Config(apiKey), NullLogger<ClaudeTaggingService>.Instance);

    [Fact]
    public async Task GenerateTags_ParsesJsonArrayFromResponse()
    {
        var body = """
        { "content": [ { "type": "text", "text": "[\"jacka\",\"ytterkläder\",\"vinter\",\"jacka\"]" } ] }
        """;
        var svc = Create(HttpStatusCode.OK, body);

        var tags = await svc.GenerateTagsAsync("Vinterjacka");

        Assert.Equal(new[] { "jacka", "ytterkläder", "vinter" }, tags); // deduped + lowercased
    }

    [Fact]
    public async Task GenerateTags_NoApiKey_ReturnsEmpty()
    {
        var svc = Create(HttpStatusCode.OK, "{}", apiKey: "");
        Assert.Empty(await svc.GenerateTagsAsync("Vinterjacka"));
    }

    [Fact]
    public async Task GenerateTags_HttpError_ReturnsEmpty()
    {
        var svc = Create(HttpStatusCode.InternalServerError, "boom");
        Assert.Empty(await svc.GenerateTagsAsync("Vinterjacka"));
    }

    [Fact]
    public async Task GenerateTags_MalformedBody_ReturnsEmpty()
    {
        var svc = Create(HttpStatusCode.OK, "{ \"content\": [] }");
        Assert.Empty(await svc.GenerateTagsAsync("Vinterjacka"));
    }
}
