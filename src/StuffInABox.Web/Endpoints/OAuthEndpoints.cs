using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Repositories;
using StuffInABox.Web.Auth;

namespace StuffInABox.Web.Endpoints;

public static class OAuthEndpoints
{
    private const string OAuthCookie = "sib_oauth";
    private static readonly string[] Providers = ["google", "apple", "microsoft"];

    public static IEndpointRouteBuilder MapOAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.Auth).WithTags("Auth (OAuth)").RequireRateLimiting("auth");

        group.MapGet("/{provider}/start", StartAsync).AllowAnonymous()
            .WithSummary("Starta OAuth-inloggning (Google/Apple)");

        group.MapGet("/{provider}/callback", CallbackAsync).AllowAnonymous()
            .WithSummary("OAuth-callback");

        return app;
    }

    private static IResult StartAsync(string provider, OAuthService oauth, HttpContext ctx)
    {
        provider = provider.ToLowerInvariant();
        if (!Providers.Contains(provider)) return Results.NotFound();

        if (!oauth.IsConfigured(provider))
            return Results.Redirect($"{oauth.PostLoginRedirect}#error=oauth_not_configured");

        var verifier = OAuthService.GenerateCodeVerifier();
        var challenge = OAuthService.ComputeCodeChallenge(verifier);
        var state = OAuthService.GenerateState();

        // Short-lived cookie binding state + PKCE verifier to this browser.
        ctx.Response.Cookies.Append(OAuthCookie, $"{state}:{verifier}:{provider}", new CookieOptions
        {
            HttpOnly = true,
            Secure = ctx.Request.IsHttps,
            SameSite = SameSiteMode.Lax, // allows the cookie on the external top-level redirect back
            Path = ApiRoutes.Auth,
            Expires = DateTimeOffset.UtcNow.AddMinutes(10),
        });

        return Results.Redirect(oauth.BuildAuthorizationUrl(provider, state, challenge));
    }

    private static async Task<IResult> CallbackAsync(
        string provider,
        string? code,
        string? state,
        string? error,
        OAuthService oauth,
        IUserIdentityRepository userRepo,
        IRefreshTokenRepository refreshRepo,
        JwtTokenService jwt,
        HttpContext ctx,
        CancellationToken ct)
    {
        provider = provider.ToLowerInvariant();
        if (!Providers.Contains(provider)) return Results.NotFound();

        var redirect = oauth.PostLoginRedirect;

        // Always clear the one-time OAuth cookie
        var cookie = ctx.Request.Cookies[OAuthCookie];
        ctx.Response.Cookies.Delete(OAuthCookie, new CookieOptions { Path = ApiRoutes.Auth });

        if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            return Results.Redirect($"{redirect}#error=oauth_failed");

        // Validate state + recover PKCE verifier from the cookie
        if (cookie is null) return Results.Redirect($"{redirect}#error=oauth_state");
        var parts = cookie.Split(':');
        if (parts.Length != 3 || parts[0] != state || parts[2] != provider)
            return Results.Redirect($"{redirect}#error=oauth_state");
        var verifier = parts[1];

        var principal = await oauth.ExchangeCodeForPrincipalAsync(provider, code, verifier, ct);
        if (principal is null || string.IsNullOrEmpty(principal.Subject))
            return Results.Redirect($"{redirect}#error=oauth_exchange");

        // Look up or create the identity, keyed on (provider, sub). We also store the
        // email (when the provider returned one) so admins can see who the account is.
        var identity = await userRepo.FindAsync(provider, principal.Subject, ct);
        if (identity is null)
        {
            identity = UserIdentity.CreateOAuth(provider, principal.Subject, principal.Email);
            await userRepo.AddAsync(identity, ct);
        }
        else if (identity.IsDisabled)
        {
            return Results.Redirect($"{redirect}#error=account_disabled");
        }
        else if (string.IsNullOrWhiteSpace(identity.Email) && !string.IsNullOrWhiteSpace(principal.Email))
        {
            // Backfill accounts created before we captured the email.
            identity.SetEmailFromProvider(principal.Email);
            await userRepo.UpdateAsync(identity, ct);
        }

        // OAuth is a browser redirect flow → refresh stays in the cookie (raw ignored).
        var (accessToken, _) = await TokenIssuer.IssueAsync(identity.GetUserId(), jwt, refreshRepo, ctx, ct);

        // Hand the access token to the SPA via the URL fragment (never logged server-side).
        return Results.Redirect($"{redirect}#token={Uri.EscapeDataString(accessToken)}");
    }
}
