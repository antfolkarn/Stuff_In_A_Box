using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace StuffInABox.Web.Auth;

/// <summary>
/// Authorization-Code + PKCE OAuth for Google and Apple. We only ever read the
/// <c>sub</c> (stable provider user id) from the returned id_token — no name or
/// email is requested or stored, matching the privacy model of the app.
/// </summary>
public sealed class OAuthService(HttpClient http, IConfiguration config)
{
    public bool IsConfigured(string provider) =>
        !string.IsNullOrWhiteSpace(config[$"OAuth:{Section(provider)}:ClientId"]);

    public string PostLoginRedirect => config["OAuth:PostLoginRedirect"] ?? "/";

    public string BuildAuthorizationUrl(string provider, string state, string codeChallenge)
    {
        var p = provider.ToLowerInvariant();
        var clientId = Require(p, "ClientId");
        var redirectUri = Require(p, "RedirectUri");

        var (authEndpoint, scope, extra) = p switch
        {
            "google" => ("https://accounts.google.com/o/oauth2/v2/auth", "openid", ""),
            // Apple requires response_mode=form_post when any scope is requested,
            // so we request none and still receive `sub` in the id_token.
            "apple" => ("https://appleid.apple.com/auth/authorize", "", "&response_mode=query"),
            // "common" = both personal Microsoft accounts and work/school (Entra) accounts.
            "microsoft" => ("https://login.microsoftonline.com/common/oauth2/v2.0/authorize", "openid", ""),
            _ => throw new InvalidOperationException($"Okänd OAuth-leverantör: {provider}"),
        };

        var query =
            $"?response_type=code" +
            $"&client_id={Uri.EscapeDataString(clientId)}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            $"&state={Uri.EscapeDataString(state)}" +
            $"&code_challenge={Uri.EscapeDataString(codeChallenge)}" +
            $"&code_challenge_method=S256" +
            extra;

        if (!string.IsNullOrEmpty(scope))
            query += $"&scope={Uri.EscapeDataString(scope)}";

        return authEndpoint + query;
    }

    /// <summary>Exchanges the authorization code for tokens and returns the provider subject id.</summary>
    public async Task<string?> ExchangeCodeForSubjectAsync(
        string provider, string code, string codeVerifier, CancellationToken ct)
    {
        var p = provider.ToLowerInvariant();
        var tokenEndpoint = p switch
        {
            "google" => "https://oauth2.googleapis.com/token",
            "apple" => "https://appleid.apple.com/auth/token",
            "microsoft" => "https://login.microsoftonline.com/common/oauth2/v2.0/token",
            _ => throw new InvalidOperationException($"Okänd OAuth-leverantör: {provider}"),
        };

        var clientSecret = p == "apple" ? GenerateAppleClientSecret() : Require(p, "ClientSecret");

        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = Require(p, "RedirectUri"),
            ["client_id"] = Require(p, "ClientId"),
            ["client_secret"] = clientSecret,
            ["code_verifier"] = codeVerifier,
        };

        using var resp = await http.PostAsync(tokenEndpoint, new FormUrlEncodedContent(form), ct);
        if (!resp.IsSuccessStatusCode) return null;

        using var doc = System.Text.Json.JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
        if (!doc.RootElement.TryGetProperty("id_token", out var idTokenEl)) return null;

        // The id_token came directly from the provider's token endpoint over TLS,
        // so per OIDC it can be consumed without re-verifying the signature.
        var idToken = idTokenEl.GetString();
        if (string.IsNullOrEmpty(idToken)) return null;

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(idToken);
        return jwt.Subject; // the `sub` claim
    }

    // PKCE helpers
    public static string GenerateCodeVerifier() =>
        Base64Url(RandomNumberGenerator.GetBytes(64));

    public static string ComputeCodeChallenge(string verifier) =>
        Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));

    public static string GenerateState() =>
        Base64Url(RandomNumberGenerator.GetBytes(16));

    private string GenerateAppleClientSecret()
    {
        var teamId = Require("apple", "TeamId");
        var keyId = Require("apple", "KeyId");
        var clientId = Require("apple", "ClientId");
        var privateKeyPem = Require("apple", "PrivateKey");

        using var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(privateKeyPem);

        var key = new ECDsaSecurityKey(ecdsa) { KeyId = keyId };
        var creds = new SigningCredentials(key, SecurityAlgorithms.EcdsaSha256);
        var now = DateTime.UtcNow;

        var token = new JwtSecurityToken(
            issuer: teamId,
            audience: "https://appleid.apple.com",
            claims: [new Claim("sub", clientId)],
            notBefore: now,
            expires: now.AddMinutes(5),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string Section(string provider) =>
        char.ToUpperInvariant(provider[0]) + provider[1..].ToLowerInvariant();

    private string Require(string provider, string key) =>
        config[$"OAuth:{Section(provider)}:{key}"]
        ?? throw new InvalidOperationException($"OAuth:{Section(provider)}:{key} saknas i konfigurationen.");

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
}
