using System.Net;
using System.Text;
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

    private static OllamaImageRecognitionService Create(HttpStatusCode status, string body)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        return new OllamaImageRecognitionService(
            new HttpClient(new StubHandler(status, body)), config, NullLogger<OllamaImageRecognitionService>.Instance);
    }

    private static readonly byte[] FakeImage = [1, 2, 3, 4];

    [Fact]
    public async Task Recognize_ParsesResponse_AndCapitalizes()
    {
        var svc = Create(HttpStatusCode.OK, """{ "response": "vinterjacka" }""");
        Assert.Equal("Vinterjacka", await svc.RecognizeAsync(FakeImage));
    }

    [Fact]
    public async Task Recognize_TrimsPunctuationAndExtraLines()
    {
        var svc = Create(HttpStatusCode.OK, "{ \"response\": \"En skruvdragare.\\nDet är ett verktyg.\" }");
        Assert.Equal("En skruvdragare", await svc.RecognizeAsync(FakeImage));
    }

    [Fact]
    public async Task Recognize_EmptyImage_ReturnsNull()
    {
        var svc = Create(HttpStatusCode.OK, """{ "response": "x" }""");
        Assert.Null(await svc.RecognizeAsync([]));
    }

    [Fact]
    public async Task Recognize_HttpError_ReturnsNull()
    {
        var svc = Create(HttpStatusCode.InternalServerError, "boom");
        Assert.Null(await svc.RecognizeAsync(FakeImage));
    }

    [Fact]
    public async Task Recognize_BlankResponse_ReturnsNull()
    {
        var svc = Create(HttpStatusCode.OK, """{ "response": "   " }""");
        Assert.Null(await svc.RecognizeAsync(FakeImage));
    }
}
