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

    public static async Task<string> IssueAsync(
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
        return accessToken;
    }

    public static void SetRefreshCookie(HttpContext ctx, string value, DateTimeOffset expires) =>
        ctx.Response.Cookies.Append(RefreshCookie, value, new CookieOptions
        {
            HttpOnly = true,
            Secure = ctx.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth",
            Expires = expires,
        });

    public static void ClearRefreshCookie(HttpContext ctx) =>
        ctx.Response.Cookies.Delete(RefreshCookie, new CookieOptions
        {
            HttpOnly = true,
            Secure = ctx.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth",
        });
}
