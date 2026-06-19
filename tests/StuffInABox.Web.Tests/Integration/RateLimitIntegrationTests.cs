using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace StuffInABox.Web.Tests.Integration;

public class RateLimitIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RateLimitIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(b =>
        {
            b.UseSetting("Jwt:Secret", "test-secret-key-min-32-chars-long!!");
            b.UseSetting("RateLimiting:AuthPermitLimit", "3"); // tiny limit for the test
        });
    }

    [Fact]
    public async Task AuthEndpoints_ExceedingLimit_Returns429()
    {
        var client = _factory.CreateClient();

        // First 3 login attempts are allowed (they fail auth → 401, but pass the limiter)
        for (var i = 0; i < 3; i++)
        {
            var ok = await client.PostAsJsonAsync("/api/auth/login",
                new { email = $"nobody{i}@test.se", password = "wrongpass" });
            Assert.Equal(HttpStatusCode.Unauthorized, ok.StatusCode);
        }

        // The 4th within the window is rejected by the limiter
        var limited = await client.PostAsJsonAsync("/api/auth/login",
            new { email = "nobody@test.se", password = "wrongpass" });
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
    }
}
