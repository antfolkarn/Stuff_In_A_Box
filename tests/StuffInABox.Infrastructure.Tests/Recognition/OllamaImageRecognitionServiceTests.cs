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
    public async Task Recognize_FlattensStructuredFieldsIntoTags()
    {
        // Structured reply: fields are flattened into the tag list in priority order
        // (föremål, märke, färger, material, kategori, text, detaljer).
        var reply = """{"namn":"Röd jacka","kategori":"kläder","föremål":["jacka"],"färger":["röd"]}""";
        var svc = Create(HttpStatusCode.OK, Envelope(reply));

        var result = await svc.RecognizeAsync(FakeImage);

        Assert.NotNull(result);
        Assert.Equal("Röd jacka", result!.Name);
        Assert.Equal(new[] { "jacka", "röd", "kläder" }, result.Tags);
    }

    [Fact]
    public async Task Recognize_NormalizesTags_LowercaseDedupeCapitalizeName()
    {
        var reply = """{"namn":"böcker","föremål":["Bok","BOK","  Deckare  ",""]}""";
        var svc = Create(HttpStatusCode.OK, Envelope(reply));

        var result = await svc.RecognizeAsync(FakeImage);

        Assert.Equal("Böcker", result!.Name);               // name capitalized
        Assert.Equal(new[] { "bok", "deckare" }, result.Tags); // lowercased, de-duped, blanks dropped
    }

    [Fact]
    public async Task Recognize_DedupesAcrossFields()
    {
        // The same word can appear in several fields; it should be added only once.
        var reply = """{"namn":"Ekskiva","föremål":["ekskiva"],"material":["ek","trä"],"detaljer":["ek"]}""";
        var svc = Create(HttpStatusCode.OK, Envelope(reply));

        var result = await svc.RecognizeAsync(FakeImage);

        Assert.Equal("Ekskiva", result!.Name);
        Assert.Equal(new[] { "ekskiva", "ek", "trä" }, result.Tags);
    }

    [Fact]
    public async Task Recognize_ToleratesProseAroundJson()
    {
        var reply = "Här är resultatet:\n{\"namn\":\"Ekskiva\",\"material\":[\"ek\",\"trä\"]}\nTack!";
        var svc = Create(HttpStatusCode.OK, Envelope(reply));

        var result = await svc.RecognizeAsync(FakeImage);

        Assert.Equal("Ekskiva", result!.Name);
        Assert.Equal(new[] { "ek", "trä" }, result.Tags);
    }

    [Fact]
    public async Task Recognize_MissingName_FallsBackToFirstTag()
    {
        var reply = """{"föremål":["hammare"],"kategori":"verktyg"}""";
        var svc = Create(HttpStatusCode.OK, Envelope(reply));

        var result = await svc.RecognizeAsync(FakeImage);

        Assert.Equal("Hammare", result!.Name);
        Assert.Equal(new[] { "hammare", "verktyg" }, result.Tags);
    }

    [Fact]
    public async Task Recognize_ReadsBrandAndVisibleText()
    {
        var reply = """{"namn":"Bosch borr","märke":"bosch","text":["gsr 18v-50"],"detaljer":["sladdlös"]}""";
        var svc = Create(HttpStatusCode.OK, Envelope(reply));

        var result = await svc.RecognizeAsync(FakeImage);

        Assert.Equal("Bosch borr", result!.Name);
        Assert.Contains("bosch", result.Tags);
        Assert.Contains("gsr 18v-50", result.Tags);
        Assert.Contains("sladdlös", result.Tags);
    }

    [Fact]
    public async Task Recognize_DropsNullishPlaceholders()
    {
        // Models sometimes emit "None"/"ingen" for an absent brand — must not become a tag.
        var reply = """{"namn":"Tröja","föremål":["tröja"],"märke":"None","text":["ingen"]}""";
        var svc = Create(HttpStatusCode.OK, Envelope(reply));

        var result = await svc.RecognizeAsync(FakeImage);

        Assert.Equal("Tröja", result!.Name);
        Assert.Equal(new[] { "tröja" }, result.Tags);
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
