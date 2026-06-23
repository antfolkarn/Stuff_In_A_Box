using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StuffInABox.Infrastructure.Persistence;

namespace StuffInABox.Web.Tests.Integration;

/// <summary>
/// Guards that every client-facing endpoint lives under the versioned /api/v1 prefix.
/// Regression test: labels/search/recognize were once mapped at /api/* and silently
/// fell through to the SPA fallback (200 text/html), surfacing as "unexpected error".
/// </summary>
public class ApiVersionRouteIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly SqliteConnection _keepAlive;

    public ApiVersionRouteIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _keepAlive = new SqliteConnection("Data Source=sib_apiversion_test;Mode=Memory;Cache=Shared");
        _keepAlive.Open();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor is not null) services.Remove(descriptor);
                services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlite("Data Source=sib_apiversion_test;Mode=Memory;Cache=Shared"));
            });
            builder.UseSetting("Jwt:Secret", "test-secret-key-min-32-chars-long!!");
            builder.UseSetting("RateLimiting:AuthPermitLimit", "1000");
        });
    }

    public void Dispose() => _keepAlive.Dispose();

    private async Task<HttpClient> SignInAsync()
    {
        var client = _factory.CreateClient();
        var reg = await client.PostAsJsonAsync("/api/v1/auth/register",
            new { email = "apiversion@test.se", password = "password123" });
        var token = (await reg.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Theory]
    [InlineData("/api/v1/labels")]
    [InlineData("/api/v1/search?q=test")]
    public async Task VersionedEndpoint_ReturnsJson_NotSpaFallback(string url)
    {
        var client = await SignInAsync();

        var resp = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        // The SPA fallback would return text/html; a real endpoint returns JSON.
        Assert.Equal("application/json", resp.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task UnversionedLabels_DoesNotExist()
    {
        var client = await SignInAsync();

        // The old, buggy path must no longer be a live API endpoint. It either 404s or
        // falls through to the SPA — either way it must NOT return label JSON.
        var resp = await client.GetAsync("/api/labels");

        Assert.NotEqual("application/json", resp.Content.Headers.ContentType?.MediaType);
    }
}
