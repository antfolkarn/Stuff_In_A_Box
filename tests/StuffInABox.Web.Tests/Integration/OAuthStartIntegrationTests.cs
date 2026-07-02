using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

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
            // Force Google "unconfigured" so the test doesn't depend on ambient machine
            // config. Must be appended via ConfigureAppConfiguration (runs AFTER the app's
            // own sources) — a plain UseSetting is loaded first and would be overridden by
            // real user-secrets on a developer machine running in Development.
            b.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(
                new Dictionary<string, string?> { ["OAuth:Google:ClientId"] = "" }));
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

    [Fact]
    public async Task GoogleStart_WhenConfigured_RedirectsToGoogleWithEmailScope()
    {
        // With a client id present, /start must send the browser to Google's authorize
        // endpoint (never back to #error=oauth_not_configured) and request the email scope.
        var factory = _factory.WithWebHostBuilder(b =>
            b.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["OAuth:Google:ClientId"] = "test-client-id",
                    ["OAuth:Google:RedirectUri"] = "https://localhost/api/v1/auth/google/callback",
                })));
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var resp = await client.GetAsync("/api/v1/auth/google/start");

        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        // OriginalString keeps the raw percent-encoding (Uri.ToString() would decode %20).
        var location = resp.Headers.Location!.OriginalString;
        Assert.StartsWith("https://accounts.google.com/o/oauth2/v2/auth?", location);
        Assert.Contains("client_id=test-client-id", location);
        Assert.Contains("scope=openid%20email", location);
        Assert.DoesNotContain("oauth_not_configured", location);
    }
}
