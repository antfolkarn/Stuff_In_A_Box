using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Infrastructure.Persistence;

namespace StuffInABox.Web.Tests.Integration;

/// <summary>
/// End-to-end password-reset flow. A capturing email service records the reset link
/// so the test can extract the token (which only ever travels by email).
/// </summary>
public class PasswordResetIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private sealed class CapturingEmailService : IEmailService
    {
        public string? LastLink;
        public Task SendPasswordResetAsync(string toEmail, string resetLink, CancellationToken ct = default)
        {
            LastLink = resetLink;
            return Task.CompletedTask;
        }

        public Task SendEmailVerificationAsync(string toEmail, string verifyLink, CancellationToken ct = default)
        {
            LastLink = verifyLink;
            return Task.CompletedTask;
        }
    }

    private readonly WebApplicationFactory<Program> _factory;
    private readonly SqliteConnection _keepAlive;
    private readonly CapturingEmailService _email = new();

    public PasswordResetIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _keepAlive = new SqliteConnection("Data Source=sib_reset_test;Mode=Memory;Cache=Shared");
        _keepAlive.Open();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor is not null) services.Remove(descriptor);
                services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlite("Data Source=sib_reset_test;Mode=Memory;Cache=Shared"));

                // Capture reset links instead of "sending" them.
                services.RemoveAll<IEmailService>();
                services.AddSingleton<IEmailService>(_email);
            });
            builder.UseSetting("Jwt:Secret", "test-secret-key-min-32-chars-long!!");
            builder.UseSetting("RateLimiting:AuthPermitLimit", "1000");
        });
    }

    public void Dispose() => _keepAlive.Dispose();

    [Fact]
    public async Task ForgotThenReset_ChangesPassword_AndInvalidatesOldOne()
    {
        var client = _factory.CreateClient();
        const string emailAddr = "reset-int@test.se";

        var reg = await client.PostAsJsonAsync("/api/v1/auth/register",
            new { email = emailAddr, password = "password123" });
        reg.EnsureSuccessStatusCode();

        // Request a reset — always 200, link captured.
        var forgot = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email = emailAddr });
        Assert.Equal(HttpStatusCode.OK, forgot.StatusCode);
        Assert.NotNull(_email.LastLink);
        var token = _email.LastLink!.Split("#reset=")[1];

        // Reset to a new password.
        var reset = await client.PostAsJsonAsync("/api/v1/auth/reset-password",
            new { token, password = "newpassword456" });
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);

        // Old password no longer works; new one does.
        var oldLogin = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = emailAddr, password = "password123" });
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);

        var newLogin = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = emailAddr, password = "newpassword456" });
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);

        // The reset token is single-use.
        var reuse = await client.PostAsJsonAsync("/api/v1/auth/reset-password",
            new { token, password = "anotherpass789" });
        Assert.Equal(HttpStatusCode.BadRequest, reuse.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_UnknownEmail_Returns200_NoLeak()
    {
        var client = _factory.CreateClient();
        _email.LastLink = null;

        var forgot = await client.PostAsJsonAsync("/api/v1/auth/forgot-password",
            new { email = "nobody@nowhere.test" });

        Assert.Equal(HttpStatusCode.OK, forgot.StatusCode); // same response as a known address
        Assert.Null(_email.LastLink); // but no email was sent
    }

    [Fact]
    public async Task ResetPassword_InvalidToken_Returns400()
    {
        var client = _factory.CreateClient();
        var reset = await client.PostAsJsonAsync("/api/v1/auth/reset-password",
            new { token = "not-a-real-token", password = "whatever123" });
        Assert.Equal(HttpStatusCode.BadRequest, reset.StatusCode);
    }
}
