using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using StuffInABox.Infrastructure.Recognition;

namespace StuffInABox.Infrastructure.Tests.Recognition;

public class OllamaImageRecognitionServiceTests
{
    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
    }

    // Wraps an inner model reply as Ollama's /api/generate envelope: { "response": "<text>" }
    private static string Envelope(string modelReply) =>
        JsonSerializer.Serialize(new { response = modelReply });

    private static OllamaImageRecognitionService Create(HttpStatusCode status, string body)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        return new OllamaImageRecognitionService(
            new HttpClient(new StubHandler(status, body)), config, NullLogger<OllamaImageRecognitionService>.Instance);
    }

    private static readonly byte[] FakeImage = [1, 2, 3, 4];

    [Fact]
    public async Task Recognize_ParsesNameAndTags()
    {
        var reply = """{"namn":"Röd jacka","taggar":["jacka","röd","ytterkläder"]}""";
        var svc = Create(HttpStatusCode.OK, Envelope(reply));

        var result = await svc.RecognizeAsync(FakeImage);

        Assert.NotNull(result);
        Assert.Equal("Röd jacka", result!.Name);
        Assert.Equal(new[] { "jacka", "röd", "ytterkläder" }, result.Tags);
    }

    [Fact]
    public async Task Recognize_NormalizesTags_LowercaseDedupeCapitalizeName()
    {
        var reply = """{"namn":"böcker","taggar":["Bok","BOK","  Deckare  ",""]}""";
        var svc = Create(HttpStatusCode.OK, Envelope(reply));

        var result = await svc.RecognizeAsync(FakeImage);

        Assert.Equal("Böcker", result!.Name);               // name capitalized
        Assert.Equal(new[] { "bok", "deckare" }, result.Tags); // lowercased, de-duped, blanks dropped
    }

    [Fact]
    public async Task Recognize_ToleratesProseAroundJson()
    {
        var reply = "Här är resultatet:\n{\"namn\":\"Ekskiva\",\"taggar\":[\"ek\",\"trä\"]}\nTack!";
        var svc = Create(HttpStatusCode.OK, Envelope(reply));

        var result = await svc.RecognizeAsync(FakeImage);

        Assert.Equal("Ekskiva", result!.Name);
        Assert.Equal(new[] { "ek", "trä" }, result.Tags);
    }

    [Fact]
    public async Task Recognize_MissingName_FallsBackToFirstTag()
    {
        var reply = """{"taggar":["hammare","verktyg"]}""";
        var svc = Create(HttpStatusCode.OK, Envelope(reply));

        var result = await svc.RecognizeAsync(FakeImage);

        Assert.Equal("Hammare", result!.Name);
        Assert.Equal(new[] { "hammare", "verktyg" }, result.Tags);
    }

    [Fact]
    public async Task Recognize_EmptyImage_ReturnsNull() =>
        Assert.Null(await Create(HttpStatusCode.OK, Envelope("{}")).RecognizeAsync([]));

    [Fact]
    public async Task Recognize_HttpError_ReturnsNull() =>
        Assert.Null(await Create(HttpStatusCode.InternalServerError, "boom").RecognizeAsync(FakeImage));

    [Fact]
    public async Task Recognize_NoJson_ReturnsNull() =>
        Assert.Null(await Create(HttpStatusCode.OK, Envelope("inget vettigt")).RecognizeAsync(FakeImage));
}
