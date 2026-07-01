using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StuffInABox.Domain.Entities;
using StuffInABox.Infrastructure.Persistence;

namespace StuffInABox.Web.Tests.Integration;

/// <summary>Exercises the full OAuth callback: /start seeds the state+PKCE cookie, then
/// /callback exchanges the code (against a stubbed token endpoint) and persists the identity.
/// Verifies the email captured from the id_token is stored on new accounts and backfilled
/// onto pre-existing ones.</summary>
public class OAuthCallbackIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private const string Sub = "google-sub-42";
    private const string Email = "oauth.user@example.com";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly SqliteConnection _keepAlive;

    public OAuthCallbackIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _keepAlive = new SqliteConnection("Data Source=sib_oauth_cb_test;Mode=Memory;Cache=Shared");
        _keepAlive.Open();

        var idToken = new JwtSecurityTokenHandler().WriteToken(
            new JwtSecurityToken(claims: [new Claim("sub", Sub), new Claim("email", Email)]));

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor is not null) services.Remove(descriptor);
                services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlite("Data Source=sib_oauth_cb_test;Mode=Memory;Cache=Shared"));

                // Stub the provider token endpoint so the code exchange returns our id_token.
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

    public void Dispose() => _keepAlive.Dispose();

    private sealed class StubTokenHandler(string idToken) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($$"""{ "id_token": "{{idToken}}" }""", Encoding.UTF8, "application/json"),
            });
    }

    // Runs /start then /callback on a cookie-aware client, returning the callback response.
    private static async Task<HttpResponseMessage> RunFlowAsync(HttpClient client)
    {
        var start = await client.GetAsync("/api/v1/auth/google/start");
        Assert.Equal(HttpStatusCode.Redirect, start.StatusCode);

        var state = new Uri(start.Headers.Location!.ToString()).Query
            .TrimStart('?').Split('&')
            .Select(p => p.Split('=', 2))
            .First(p => p[0] == "state")[1];

        return await client.GetAsync($"/api/v1/auth/google/callback?code=any-code&state={state}");
    }

    private async Task<string?> EmailForSubAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var identity = await db.UserIdentities.FirstOrDefaultAsync(u => u.Provider == "google" && u.ExternalId == Sub);
        return identity?.Email;
    }

    [Fact]
    public async Task Callback_NewAccount_StoresEmailFromIdToken()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var callback = await RunFlowAsync(client);

        Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode);
        Assert.Contains("#token=", callback.Headers.Location!.ToString());
        Assert.Equal(Email, await EmailForSubAsync());
    }

    [Fact]
    public async Task Callback_ExistingAccountWithoutEmail_BackfillsIt()
    {
        // Seed an OAuth identity created before we captured email (Email = null).
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.UserIdentities.Add(UserIdentity.CreateOAuth("google", Sub));
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var callback = await RunFlowAsync(client);

        Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode);
        Assert.Equal(Email, await EmailForSubAsync());
    }
}
