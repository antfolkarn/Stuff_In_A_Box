using System.Net.Http;
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
            ["OAuth:Google:RedirectUri"] = "https://app/api/auth/google/callback",
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
            ["OAuth:Apple:RedirectUri"] = "https://app/api/auth/apple/callback",
        });

        var url = svc.BuildAuthorizationUrl("apple", "s", "c");

        Assert.StartsWith("https://appleid.apple.com/auth/authorize?", url);
        Assert.Contains("response_mode=query", url);
    }
}
