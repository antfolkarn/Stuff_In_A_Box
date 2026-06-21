using Microsoft.AspNetCore.Http;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Repositories;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Web.Auth;

/// <summary>
/// Shared access/refresh token issuance used by both password and OAuth login flows.
/// Issues a short-lived access token (returned to the caller) and a rotating
/// refresh token stored as a hash and delivered in an HttpOnly cookie.
/// </summary>
internal static class TokenIssuer
{
    public const string RefreshCookie = "sib_refresh";

    /// <summary>
    /// Issues an access token and a rotating refresh token. The raw refresh token is
    /// always set in the HttpOnly cookie (for browsers) and also returned so callers
    /// can hand it to native clients in the response body (see <see cref="WantsTokenInBody"/>).
    /// </summary>
    public static async Task<(string AccessToken, string RawRefresh)> IssueAsync(
        UserId userId,
        JwtTokenService jwt,
        IRefreshTokenRepository refreshRepo,
        HttpContext ctx,
        CancellationToken ct)
    {
        var accessToken = jwt.GenerateAccessToken(userId);

        var rawRefresh = JwtTokenService.GenerateRefreshTokenRaw();
        var entity = RefreshToken.Issue(
            userId.Value, JwtTokenService.HashRefreshToken(rawRefresh), jwt.RefreshTokenLifetime);
        await refreshRepo.AddAsync(entity, ct);

        SetRefreshCookie(ctx, rawRefresh, entity.ExpiresAt);
        return (accessToken, rawRefresh);
    }

    /// <summary>
    /// Whether the caller is a native client that should receive the refresh token in
    /// the body (and store it securely). Browsers must NOT — the HttpOnly cookie keeps
    /// it out of JavaScript. Native clients opt in via <c>X-Client: mobile</c> or by
    /// presenting the refresh token in the <c>X-Refresh-Token</c> header.
    /// </summary>
    public static bool WantsTokenInBody(HttpContext ctx) =>
        string.Equals(ctx.Request.Headers["X-Client"], "mobile", StringComparison.OrdinalIgnoreCase)
        || !string.IsNullOrEmpty(ctx.Request.Headers["X-Refresh-Token"]);

    /// <summary>Reads the presented refresh token: native header first, then the cookie.</summary>
    public static string? ReadPresentedRefresh(HttpContext ctx)
    {
        var header = ctx.Request.Headers["X-Refresh-Token"].ToString();
        return !string.IsNullOrEmpty(header) ? header : ctx.Request.Cookies[RefreshCookie];
    }

    public static void SetRefreshCookie(HttpContext ctx, string value, DateTimeOffset expires) =>
        ctx.Response.Cookies.Append(RefreshCookie, value, new CookieOptions
        {
            HttpOnly = true,
            Secure = ctx.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = ApiRoutes.Auth,
            Expires = expires,
        });

    public static void ClearRefreshCookie(HttpContext ctx) =>
        ctx.Response.Cookies.Delete(RefreshCookie, new CookieOptions
        {
            HttpOnly = true,
            Secure = ctx.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = ApiRoutes.Auth,
        });
}
