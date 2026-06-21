using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StuffInABox.Infrastructure.Persistence;

namespace StuffInABox.Web.Tests.Integration;

public class RefreshFlowIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly SqliteConnection _keepAlive;

    public RefreshFlowIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _keepAlive = new SqliteConnection("Data Source=sib_refresh_test;Mode=Memory;Cache=Shared");
        _keepAlive.Open();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor is not null) services.Remove(descriptor);
                services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlite("Data Source=sib_refresh_test;Mode=Memory;Cache=Shared"));
            });
            builder.UseSetting("Jwt:Secret", "test-secret-key-min-32-chars-long!!");
            builder.UseSetting("RateLimiting:AuthPermitLimit", "1000");
        });
    }

    public void Dispose() => _keepAlive.Dispose();

    [Fact]
    public async Task Register_ThenRefresh_IssuesNewToken_AndLogoutRevokes()
    {
        // Cookie-aware client (HandleCookies defaults to true)
        var client = _factory.CreateClient();

        var register = await client.PostAsJsonAsync("/api/v1/auth/register",
            new { email = "refresh@test.se", password = "password123" });
        register.EnsureSuccessStatusCode();
        Assert.Contains(register.Headers, h => h.Key == "Set-Cookie" && h.Value.Any(v => v.Contains("sib_refresh")));

        // Browser flow: the refresh token must NOT leak into the response body (it lives
        // only in the HttpOnly cookie, out of reach of JavaScript).
        var regBody = await register.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(regBody.TryGetProperty("refreshToken", out _));

        // Refresh using the cookie — should succeed with a fresh token
        var refresh = await client.PostAsync("/api/v1/auth/refresh", null);
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        var body = await refresh.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrEmpty(body.GetProperty("token").GetString()));

        // Logout revokes the (rotated) refresh token
        var logout = await client.PostAsync("/api/v1/auth/logout", null);
        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);

        // Subsequent refresh is rejected
        var afterLogout = await client.PostAsync("/api/v1/auth/refresh", null);
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithoutCookie_Returns401()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var response = await client.PostAsync("/api/v1/auth/refresh", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MobileClient_GetsRefreshTokenInBody_AndRefreshesViaHeader()
    {
        // Native clients have no cookie jar: opt in with X-Client: mobile and refresh
        // by presenting the token in the X-Refresh-Token header.
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        client.DefaultRequestHeaders.Add("X-Client", "mobile");

        var register = await client.PostAsJsonAsync("/api/v1/auth/register",
            new { email = "mobile@test.se", password = "password123" });
        register.EnsureSuccessStatusCode();
        var regBody = await register.Content.ReadFromJsonAsync<JsonElement>();
        var refreshToken = regBody.GetProperty("refreshToken").GetString();
        Assert.False(string.IsNullOrEmpty(refreshToken));

        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        req.Headers.Add("X-Refresh-Token", refreshToken);
        var refresh = await client.SendAsync(req);

        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        var body = await refresh.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrEmpty(body.GetProperty("token").GetString()));
        Assert.False(string.IsNullOrEmpty(body.GetProperty("refreshToken").GetString())); // rotated
    }
}
