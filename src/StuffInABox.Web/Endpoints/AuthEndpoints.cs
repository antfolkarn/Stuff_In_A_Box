using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Repositories;
using StuffInABox.Domain.ValueObjects;
using StuffInABox.Web.Auth;

namespace StuffInABox.Web.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.Auth).WithTags("Auth").RequireRateLimiting("auth");

        group.MapPost("/register", RegisterAsync)
            .AllowAnonymous()
            .WithSummary("Registrera med e-post och lösenord");

        group.MapPost("/login", LoginAsync)
            .AllowAnonymous()
            .WithSummary("Logga in med e-post och lösenord");

        group.MapPost("/refresh", RefreshAsync)
            .AllowAnonymous()
            .WithSummary("Förnya access-token med refresh-cookie");

        group.MapPost("/logout", LogoutAsync)
            .AllowAnonymous()
            .WithSummary("Logga ut och återkalla refresh-token");

        return app;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest req,
        IUserIdentityRepository repo,
        IRefreshTokenRepository refreshRepo,
        JwtTokenService jwt,
        HttpContext ctx,
        CancellationToken ct)
    {
        var externalId = HashEmail(req.Email);
        var existing = await repo.FindAsync("email", externalId, ct);
        if (existing is not null)
            return Results.Conflict(new { code = "email_taken", error = "E-postadress redan registrerad." });

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(req.Password);
        var identity = UserIdentity.CreateEmail(externalId, passwordHash);
        await repo.AddAsync(identity, ct);

        var (token, rawRefresh) = await TokenIssuer.IssueAsync(identity.GetUserId(), jwt, refreshRepo, ctx, ct);
        return TokenResult(ctx, token, rawRefresh);
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest req,
        IUserIdentityRepository repo,
        IRefreshTokenRepository refreshRepo,
        JwtTokenService jwt,
        HttpContext ctx,
        CancellationToken ct)
    {
        var externalId = HashEmail(req.Email);
        var identity = await repo.FindAsync("email", externalId, ct);

        if (identity is null || identity.PasswordHash is null
            || !BCrypt.Net.BCrypt.Verify(req.Password, identity.PasswordHash))
            return Results.Unauthorized();

        var (token, rawRefresh) = await TokenIssuer.IssueAsync(identity.GetUserId(), jwt, refreshRepo, ctx, ct);
        return TokenResult(ctx, token, rawRefresh);
    }

    private static async Task<IResult> RefreshAsync(
        IRefreshTokenRepository refreshRepo,
        JwtTokenService jwt,
        HttpContext ctx,
        CancellationToken ct)
    {
        // Browsers present the refresh token via the HttpOnly cookie; native clients
        // via the X-Refresh-Token header.
        var raw = TokenIssuer.ReadPresentedRefresh(ctx);
        if (string.IsNullOrEmpty(raw))
            return Results.Unauthorized();

        var hash = JwtTokenService.HashRefreshToken(raw);
        var stored = await refreshRepo.FindByHashAsync(hash, ct);

        if (stored is null || !stored.IsActive(DateTimeOffset.UtcNow))
        {
            TokenIssuer.ClearRefreshCookie(ctx);
            return Results.Unauthorized();
        }

        // Rotate: revoke the presented token and issue a fresh pair.
        stored.Revoke(DateTimeOffset.UtcNow);
        await refreshRepo.UpdateAsync(stored, ct);

        var userId = new UserId(stored.UserId);
        var (token, rawRefresh) = await TokenIssuer.IssueAsync(userId, jwt, refreshRepo, ctx, ct);
        return TokenResult(ctx, token, rawRefresh);
    }

    private static async Task<IResult> LogoutAsync(
        IRefreshTokenRepository refreshRepo,
        HttpContext ctx,
        CancellationToken ct)
    {
        var raw = TokenIssuer.ReadPresentedRefresh(ctx);
        if (!string.IsNullOrEmpty(raw))
        {
            var stored = await refreshRepo.FindByHashAsync(JwtTokenService.HashRefreshToken(raw), ct);
            if (stored is not null)
            {
                stored.Revoke(DateTimeOffset.UtcNow);
                await refreshRepo.UpdateAsync(stored, ct);
            }
        }
        TokenIssuer.ClearRefreshCookie(ctx);
        return Results.Ok();
    }

    // Browsers get the access token only (refresh stays in the HttpOnly cookie);
    // native clients additionally get the refresh token in the body to store securely.
    private static IResult TokenResult(HttpContext ctx, string accessToken, string rawRefresh) =>
        TokenIssuer.WantsTokenInBody(ctx)
            ? Results.Ok(new { token = accessToken, refreshToken = rawRefresh })
            : Results.Ok(new { token = accessToken });

    private static string HashEmail(string email) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(email.Trim().ToLowerInvariant())));

    private record RegisterRequest(string Email, string Password);
    private record LoginRequest(string Email, string Password);
}
