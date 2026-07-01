using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using StuffInABox.Web.Auth;

namespace StuffInABox.Web.Tests.Auth;

public class OAuthServiceTests
{
    private static OAuthService Create(Dictionary<string, string?> settings)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return new OAuthService(new HttpClient(), config);
    }

    // Stubs the provider token endpoint with a canned response.
    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
    }

    private static OAuthService CreateWithHandler(Dictionary<string, string?> settings, HttpMessageHandler handler)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return new OAuthService(new HttpClient(handler), config);
    }

    // An id_token the way a provider returns it: JWT with the given claims (unsigned is
    // fine — the service reads it without verifying, per OIDC over the TLS token endpoint).
    private static string MakeIdToken(params Claim[] claims) =>
        new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(claims: claims));

    private static Dictionary<string, string?> GoogleCreds() => new()
    {
        ["OAuth:Google:ClientId"] = "cid",
        ["OAuth:Google:ClientSecret"] = "secret",
        ["OAuth:Google:RedirectUri"] = "https://app/api/v1/auth/google/callback",
    };

    [Fact]
    public void IsConfigured_FalseWhenNoClientId()
    {
        var svc = Create(new());
        Assert.False(svc.IsConfigured("google"));
        Assert.False(svc.IsConfigured("apple"));
    }

    [Fact]
    public void IsConfigured_TrueWhenClientIdPresent()
    {
        var svc = Create(new() { ["OAuth:Google:ClientId"] = "abc.apps.googleusercontent.com" });
        Assert.True(svc.IsConfigured("google"));
    }

    [Fact]
    public void ComputeCodeChallenge_IsDeterministicBase64Url()
    {
        var verifier = "the-verifier-value";
        var a = OAuthService.ComputeCodeChallenge(verifier);
        var b = OAuthService.ComputeCodeChallenge(verifier);

        Assert.Equal(a, b);
        Assert.DoesNotContain('+', a);
        Assert.DoesNotContain('/', a);
        Assert.DoesNotContain('=', a);
    }

    [Fact]
    public void BuildAuthorizationUrl_Google_IncludesPkceAndClientId()
    {
        var svc = Create(new()
        {
            ["OAuth:Google:ClientId"] = "cid",
            ["OAuth:Google:RedirectUri"] = "https://app/api/v1/auth/google/callback",
        });

        var url = svc.BuildAuthorizationUrl("google", "state123", "challenge123");

        Assert.StartsWith("https://accounts.google.com/o/oauth2/v2/auth?", url);
        Assert.Contains("response_type=code", url);
        Assert.Contains("client_id=cid", url);
        Assert.Contains("code_challenge=challenge123", url);
        Assert.Contains("code_challenge_method=S256", url);
        Assert.Contains("state=state123", url);
    }

    [Fact]
    public void BuildAuthorizationUrl_Apple_UsesResponseModeQuery()
    {
        var svc = Create(new()
        {
            ["OAuth:Apple:ClientId"] = "com.example.app",
            ["OAuth:Apple:RedirectUri"] = "https://app/api/v1/auth/apple/callback",
        });

        var url = svc.BuildAuthorizationUrl("apple", "s", "c");

        Assert.StartsWith("https://appleid.apple.com/auth/authorize?", url);
        Assert.Contains("response_mode=query", url);
    }

    [Fact]
    public void BuildAuthorizationUrl_Google_RequestsEmailScope()
    {
        var svc = Create(GoogleCreds());

        var url = svc.BuildAuthorizationUrl("google", "s", "c");

        // "openid email" is URL-encoded (space → %20).
        Assert.Contains("scope=openid%20email", url);
    }

    [Fact]
    public void BuildAuthorizationUrl_Microsoft_RequestsEmailScope()
    {
        var svc = Create(new()
        {
            ["OAuth:Microsoft:ClientId"] = "cid",
            ["OAuth:Microsoft:RedirectUri"] = "https://app/api/v1/auth/microsoft/callback",
        });

        var url = svc.BuildAuthorizationUrl("microsoft", "s", "c");

        Assert.StartsWith("https://login.microsoftonline.com/common/oauth2/v2.0/authorize?", url);
        Assert.Contains("scope=openid%20email", url);
    }

    [Fact]
    public void BuildAuthorizationUrl_Apple_RequestsNoScope()
    {
        var svc = Create(new()
        {
            ["OAuth:Apple:ClientId"] = "com.example.app",
            ["OAuth:Apple:RedirectUri"] = "https://app/api/v1/auth/apple/callback",
        });

        var url = svc.BuildAuthorizationUrl("apple", "s", "c");

        // Apple would need response_mode=form_post to return a scope, so we request none.
        Assert.DoesNotContain("scope=", url);
    }

    [Fact]
    public async Task ExchangeCodeForPrincipalAsync_ReadsSubAndEmail()
    {
        var idToken = MakeIdToken(new Claim("sub", "google-sub-1"), new Claim("email", "user@example.com"));
        var svc = CreateWithHandler(GoogleCreds(),
            new StubHandler(HttpStatusCode.OK, $$"""{ "id_token": "{{idToken}}" }"""));

        var principal = await svc.ExchangeCodeForPrincipalAsync("google", "code", "verifier", default);

        Assert.NotNull(principal);
        Assert.Equal("google-sub-1", principal!.Subject);
        Assert.Equal("user@example.com", principal.Email);
    }

    [Fact]
    public async Task ExchangeCodeForPrincipalAsync_NoEmailClaim_ReturnsNullEmail()
    {
        var idToken = MakeIdToken(new Claim("sub", "google-sub-1"));
        var svc = CreateWithHandler(GoogleCreds(),
            new StubHandler(HttpStatusCode.OK, $$"""{ "id_token": "{{idToken}}" }"""));

        var principal = await svc.ExchangeCodeForPrincipalAsync("google", "code", "verifier", default);

        Assert.NotNull(principal);
        Assert.Equal("google-sub-1", principal!.Subject);
        Assert.Null(principal.Email);
    }

    [Fact]
    public async Task ExchangeCodeForPrincipalAsync_TokenEndpointError_ReturnsNull()
    {
        var svc = CreateWithHandler(GoogleCreds(),
            new StubHandler(HttpStatusCode.BadRequest, "{ \"error\": \"invalid_grant\" }"));

        var principal = await svc.ExchangeCodeForPrincipalAsync("google", "code", "verifier", default);

        Assert.Null(principal);
    }
}
