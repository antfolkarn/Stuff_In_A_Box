using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace StuffInABox.Web.Tests.Integration;

public class OAuthStartIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public OAuthStartIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(b =>
        {
            b.UseSetting("Jwt:Secret", "test-secret-key-min-32-chars-long!!");
            b.UseSetting("RateLimiting:AuthPermitLimit", "1000");
            // Force Google "unconfigured" so the test doesn't depend on ambient
            // machine config (e.g. real OAuth client id in user-secrets).
            b.UseSetting("OAuth:Google:ClientId", "");
        });
    }

    [Fact]
    public async Task GoogleStart_WhenUnconfigured_RedirectsWithError()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var resp = await client.GetAsync("/api/v1/auth/google/start");

        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        Assert.Contains("error=oauth_not_configured", resp.Headers.Location!.ToString());
    }

    [Fact]
    public async Task UnknownProvider_Returns404()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var resp = await client.GetAsync("/api/v1/auth/facebook/start");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }
}
