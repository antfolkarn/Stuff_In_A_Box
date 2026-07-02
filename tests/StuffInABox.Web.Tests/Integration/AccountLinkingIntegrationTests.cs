using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StuffInABox.Domain.Entities;
using StuffInABox.Infrastructure.Persistence;

namespace StuffInABox.Web.Tests.Integration;

/// <summary>Account linking: the same email should mean the same person. A fresh factory + DB
/// per test (not a class fixture) so the shared identity table doesn't leak between cases.</summary>
public class AccountLinkingIntegrationTests : IDisposable
{
    private const string Sub = "google-link-sub";
    private const string Email = "linkme@example.com";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly SqliteConnection _keepAlive;

    public AccountLinkingIntegrationTests()
    {
        var cs = $"Data Source=sib_link_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        _keepAlive = new SqliteConnection(cs);
        _keepAlive.Open();

        var idToken = new JwtSecurityTokenHandler().WriteToken(
            new JwtSecurityToken(claims: [new Claim("sub", Sub), new Claim("email", Email)]));

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var d = services.SingleOrDefault(x => x.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (d is not null) services.Remove(d);
                services.AddDbContext<AppDbContext>(o => o.UseSqlite(cs));
                services.AddHttpClient<StuffInABox.Web.Auth.OAuthService>()
                    .ConfigurePrimaryHttpMessageHandler(() => new StubTokenHandler(idToken));
            });
            builder.UseSetting("Jwt:Secret", "test-secret-key-min-32-chars-long!!");
            builder.UseSetting("RateLimiting:AuthPermitLimit", "1000");
            builder.UseSetting("OAuth:Google:ClientId", "cid");
            builder.UseSetting("OAuth:Google:ClientSecret", "secret");
            builder.UseSetting("OAuth:Google:RedirectUri", "https://app/api/v1/auth/google/callback");
        });
    }

    public void Dispose()
    {
        _factory.Dispose();
        _keepAlive.Dispose();
    }

    private sealed class StubTokenHandler(string idToken) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($$"""{ "id_token": "{{idToken}}" }""", Encoding.UTF8, "application/json"),
            });
    }

    private void SeedEmailIdentity(bool verified)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var id = UserIdentity.CreateEmail("hashed-linkme", "pwhash", Email);
        if (verified) id.MarkEmailVerified();
        db.UserIdentities.Add(id);
        db.SaveChanges();
    }

    private async Task GoogleSignInAsync()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var start = await client.GetAsync("/api/v1/auth/google/start");
        var state = new Uri(start.Headers.Location!.ToString()).Query
            .TrimStart('?').Split('&').Select(p => p.Split('=', 2)).First(p => p[0] == "state")[1];
        await client.GetAsync($"/api/v1/auth/google/callback?code=any&state={state}");
    }

    private Guid PersonOf(string provider)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return db.UserIdentities.First(u => u.Provider == provider).UserId;
    }

    [Fact]
    public async Task GoogleSignIn_LinksToVerifiedEmailAccount()
    {
        SeedEmailIdentity(verified: true);
        var emailPerson = PersonOf("email");

        await GoogleSignInAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var google = db.UserIdentities.First(u => u.Provider == "google" && u.ExternalId == Sub);
        Assert.Equal(emailPerson, google.UserId);         // linked → same person and data
        Assert.NotEqual(emailPerson, google.InternalUserId); // but its own login row
    }

    [Fact]
    public async Task GoogleSignIn_DoesNotLinkToUnverifiedEmailAccount()
    {
        SeedEmailIdentity(verified: false);
        var emailPerson = PersonOf("email");

        await GoogleSignInAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var google = db.UserIdentities.First(u => u.Provider == "google" && u.ExternalId == Sub);
        Assert.NotEqual(emailPerson, google.UserId); // unverified stub must not capture the login
    }

    [Fact]
    public async Task Register_WhenVerifiedGoogleAccountExists_IsBlocked()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.UserIdentities.Add(UserIdentity.CreateOAuth("google", "g-sub", Email)); // OAuth = verified
            db.SaveChanges();
        }

        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/v1/auth/register", new { email = Email, password = "password123" });

        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("account_exists", body.GetProperty("code").GetString());
    }
}
