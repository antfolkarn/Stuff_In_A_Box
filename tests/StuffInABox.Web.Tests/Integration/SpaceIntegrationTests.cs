using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StuffInABox.Infrastructure.Persistence;

namespace StuffInABox.Web.Tests.Integration;

public class SpaceIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    // Keep connection open so named in-memory SQLite database persists for the test session
    private readonly SqliteConnection _keepAlive;

    public SpaceIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _keepAlive = new SqliteConnection("Data Source=sib_test;Mode=Memory;Cache=Shared");
        _keepAlive.Open();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor is not null) services.Remove(descriptor);

                services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlite("Data Source=sib_test;Mode=Memory;Cache=Shared"));
            });
            builder.UseSetting("Jwt:Secret", "test-secret-key-min-32-chars-long!!");
            builder.UseSetting("RateLimiting:AuthPermitLimit", "1000");
        });
    }

    public void Dispose() => _keepAlive.Dispose();

    private async Task<string> GetTokenAsync(HttpClient client)
    {
        const string email = "test@example.com";
        var registerResp = await client.PostAsJsonAsync("/api/v1/auth/register",
            new { email, password = "password123" });
        registerResp.EnsureSuccessStatusCode();
        TestVerify.MarkVerified(_factory, email);
        var body = await registerResp.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("token").GetString()!;
    }

    [Fact]
    public async Task CreateSpace_WithValidToken_Returns201()
    {
        var client = _factory.CreateClient();
        var token = await GetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/v1/spaces",
            new { name = "Garage", icon = "ti-car" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task GetSpaces_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/spaces");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
