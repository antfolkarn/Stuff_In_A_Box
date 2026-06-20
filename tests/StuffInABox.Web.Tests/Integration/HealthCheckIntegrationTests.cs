using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StuffInABox.Infrastructure.Persistence;

namespace StuffInABox.Web.Tests.Integration;

public class HealthCheckIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly SqliteConnection _keepAlive;

    public HealthCheckIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _keepAlive = new SqliteConnection("Data Source=sib_health_test;Mode=Memory;Cache=Shared");
        _keepAlive.Open();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor is not null) services.Remove(descriptor);
                services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlite("Data Source=sib_health_test;Mode=Memory;Cache=Shared"));
            });
            builder.UseSetting("Jwt:Secret", "test-secret-key-min-32-chars-long!!");
        });
    }

    public void Dispose() => _keepAlive.Dispose();

    [Fact]
    public async Task Liveness_ReturnsHealthy()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task Readiness_WithReachableDb_ReturnsHealthy()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal("Healthy", await resp.Content.ReadAsStringAsync());
    }
}
