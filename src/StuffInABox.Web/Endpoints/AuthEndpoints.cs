using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using StuffInABox.Application.Common.Interfaces;
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
            .RequireRateLimiting("auth-email") // stacks on "auth": caps verification-email sends per IP
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

        group.MapPost("/forgot-password", ForgotPasswordAsync)
            .AllowAnonymous()
            .RequireRateLimiting("auth-email") // stacks on "auth": caps reset-email sends per IP
            .WithSummary("Begär återställningslänk för lösenord");

        group.MapPost("/reset-password", ResetPasswordAsync)
            .AllowAnonymous()
            .WithSummary("Återställ lösenord med token");

        group.MapPost("/verify-email", VerifyEmailAsync)
            .AllowAnonymous()
            .WithSummary("Verifiera e-postadress med token");

        group.MapPost("/resend-verification", ResendVerificationAsync)
            .RequireAuthorization()
            .RequireRateLimiting("auth-email") // caps repeat verification-email sends per IP
            .WithSummary("Skicka nytt verifieringsmejl");

        group.MapGet("/me", MeAsync)
            .RequireAuthorization()
            .DisableRateLimiting() // called on load; must not consume the auth limiter
            .WithSummary("Aktuell användares konto- och verifieringsstatus");

        return app;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest req,
        IUserIdentityRepository repo,
        IRefreshTokenRepository refreshRepo,
        IEmailVerificationTokenRepository verifyRepo,
        IEmailService email,
        JwtTokenService jwt,
        IConfiguration config,
        IHostEnvironment env,
        HttpContext ctx,
        CancellationToken ct)
    {
        var externalId = HashEmail(req.Email);
        var existing = await repo.FindAsync("email", externalId, ct);
        if (existing is not null)
            return Results.Conflict(new { code = "email_taken", error = "E-postadress redan registrerad." });

        // Block registering an email that already belongs to a verified account (e.g. signed up
        // with Google) — one address = one person. They should sign in with their existing method.
        var linked = await repo.FindByEmailAsync(req.Email, ct);
        if (linked is not null && linked.IsEmailVerified)
            return Results.Conflict(new { code = "account_exists", error = "Du har redan ett konto med den här e-postadressen — logga in med din befintliga metod." });

        // Reject addresses whose domain doesn't exist — cuts hard bounces and stops bots burning
        // the email quota on junk domains. Production only: dev/tests use the log email provider
        // (no real sends) and shouldn't depend on live DNS.
        if (env.IsProduction() && !await DomainResolvesAsync(req.Email))
            return Results.UnprocessableEntity(new { code = "invalid_email_domain", error = "E-postdomänen kan inte nås." });

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(req.Password);
        var identity = UserIdentity.CreateEmail(externalId, passwordHash, req.Email.Trim());
        await repo.AddAsync(identity, ct);

        // Send the verification email (best-effort — registration still succeeds + logs in).
        await SendVerificationEmailAsync(identity, verifyRepo, email, config, ctx, ct);

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

        if (identity.IsDisabled)
            return Results.Json(
                new { code = "account_disabled", error = "Kontot är avstängt." },
                statusCode: StatusCodes.Status403Forbidden);

        var (token, rawRefresh) = await TokenIssuer.IssueAsync(identity.GetUserId(), jwt, refreshRepo, ctx, ct);
        return TokenResult(ctx, token, rawRefresh);
    }

    private static async Task<IResult> RefreshAsync(
        IRefreshTokenRepository refreshRepo,
        IUserIdentityRepository userRepo,
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

        // A disabled account loses its session on next refresh: revoke the token, don't reissue.
        var identity = await userRepo.FindByIdAsync(stored.UserId, ct);
        if (identity is null || identity.IsDisabled)
        {
            stored.Revoke(DateTimeOffset.UtcNow);
            await refreshRepo.UpdateAsync(stored, ct);
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

    private static async Task<IResult> ForgotPasswordAsync(
        ForgotPasswordRequest req,
        IUserIdentityRepository userRepo,
        IPasswordResetTokenRepository resetRepo,
        IEmailService email,
        IConfiguration config,
        HttpContext ctx,
        CancellationToken ct)
    {
        var identity = await userRepo.FindAsync("email", HashEmail(req.Email), ct);

        // Only send when the account exists and has a stored email — but ALWAYS return
        // 200 so the endpoint never reveals which addresses are registered.
        if (identity is not null && !string.IsNullOrEmpty(identity.Email))
        {
            await resetRepo.InvalidateAllForUserAsync(identity.InternalUserId, ct);

            var rawToken = JwtTokenService.GenerateRefreshTokenRaw();
            var entity = PasswordResetToken.Issue(
                identity.InternalUserId, JwtTokenService.HashRefreshToken(rawToken), TimeSpan.FromHours(1));
            await resetRepo.AddAsync(entity, ct);

            await email.SendPasswordResetAsync(identity.Email, BuildResetLink(config, ctx, rawToken), ct);
        }

        return Results.Ok();
    }

    private static async Task<IResult> ResetPasswordAsync(
        ResetPasswordRequest req,
        IUserIdentityRepository userRepo,
        IPasswordResetTokenRepository resetRepo,
        IRefreshTokenRepository refreshRepo,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 6)
            return Results.BadRequest(new { code = "weak_password", error = "Lösenordet måste vara minst 6 tecken." });

        var stored = await resetRepo.FindByHashAsync(JwtTokenService.HashRefreshToken(req.Token), ct);
        if (stored is null || !stored.IsActive(DateTimeOffset.UtcNow))
            return Results.BadRequest(new { code = "invalid_token", error = "Återställningslänken är ogiltig eller har gått ut." });

        var identity = await userRepo.FindByIdAsync(stored.UserId, ct);
        if (identity is null || identity.PasswordHash is null)
            return Results.BadRequest(new { code = "invalid_token", error = "Återställningslänken är ogiltig eller har gått ut." });

        identity.UpdatePasswordHash(BCrypt.Net.BCrypt.HashPassword(req.Password));
        await userRepo.UpdateAsync(identity, ct);

        stored.Use(DateTimeOffset.UtcNow);
        await resetRepo.UpdateAsync(stored, ct);

        // Security: drop any other reset tokens and revoke all existing sessions.
        await resetRepo.InvalidateAllForUserAsync(identity.InternalUserId, ct);
        await refreshRepo.RevokeAllForUserAsync(identity.InternalUserId, ct);

        return Results.Ok();
    }

    private static async Task<IResult> VerifyEmailAsync(
        VerifyEmailRequest req,
        IUserIdentityRepository userRepo,
        IEmailVerificationTokenRepository verifyRepo,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Token))
            return Results.BadRequest(new { code = "invalid_token", error = "Verifieringslänken är ogiltig eller har gått ut." });

        var stored = await verifyRepo.FindByHashAsync(JwtTokenService.HashRefreshToken(req.Token), ct);
        if (stored is null || !stored.IsActive(DateTimeOffset.UtcNow))
            return Results.BadRequest(new { code = "invalid_token", error = "Verifieringslänken är ogiltig eller har gått ut." });

        var identity = await userRepo.FindByIdAsync(stored.UserId, ct);
        if (identity is null)
            return Results.BadRequest(new { code = "invalid_token", error = "Verifieringslänken är ogiltig eller har gått ut." });

        identity.MarkEmailVerified();
        await userRepo.UpdateAsync(identity, ct);

        stored.Use(DateTimeOffset.UtcNow);
        await verifyRepo.UpdateAsync(stored, ct);
        await verifyRepo.InvalidateAllForUserAsync(identity.InternalUserId, ct);

        return Results.Ok();
    }

    private static async Task<IResult> ResendVerificationAsync(
        ICurrentUserService currentUser,
        IUserIdentityRepository userRepo,
        IEmailVerificationTokenRepository verifyRepo,
        IEmailService email,
        IConfiguration config,
        HttpContext ctx,
        CancellationToken ct)
    {
        var identity = await userRepo.FindByIdAsync(currentUser.UserId.Value, ct);
        // Only send when there's something to verify — but always 200 (no info leak).
        if (identity is not null && !identity.IsEmailVerified && !string.IsNullOrEmpty(identity.Email))
            await SendVerificationEmailAsync(identity, verifyRepo, email, config, ctx, ct);

        return Results.Ok();
    }

    private static async Task<IResult> MeAsync(
        ICurrentUserService currentUser,
        IUserIdentityRepository userRepo,
        CancellationToken ct)
    {
        var identity = await userRepo.FindByIdAsync(currentUser.UserId.Value, ct);
        if (identity is null) return Results.Unauthorized();

        return Results.Ok(new
        {
            provider = identity.Provider,
            email = identity.Email,
            emailVerified = identity.IsEmailVerified,
        });
    }

    // Issues a fresh single-use verification token and emails the #verify deep link.
    private static async Task SendVerificationEmailAsync(
        UserIdentity identity,
        IEmailVerificationTokenRepository verifyRepo,
        IEmailService email,
        IConfiguration config,
        HttpContext ctx,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(identity.Email)) return;

        await verifyRepo.InvalidateAllForUserAsync(identity.InternalUserId, ct);

        var rawToken = JwtTokenService.GenerateRefreshTokenRaw();
        var entity = EmailVerificationToken.Issue(
            identity.InternalUserId, JwtTokenService.HashRefreshToken(rawToken), TimeSpan.FromHours(24));
        await verifyRepo.AddAsync(entity, ct);

        await email.SendEmailVerificationAsync(identity.Email, BuildVerifyLink(config, ctx, rawToken), ct);
    }

    private static string BuildVerifyLink(IConfiguration config, HttpContext ctx, string token)
    {
        var baseUrl = config["App:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
            baseUrl = $"{ctx.Request.Scheme}://{ctx.Request.Host}";
        return $"{baseUrl.TrimEnd('/')}/#verify={token}";
    }

    // Reset links point at the SPA's #reset deep link. Prefer a configured public base
    // URL (correct behind a reverse proxy); fall back to the request's own origin.
    private static string BuildResetLink(IConfiguration config, HttpContext ctx, string token)
    {
        var baseUrl = config["App:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
            baseUrl = $"{ctx.Request.Scheme}://{ctx.Request.Host}";
        return $"{baseUrl.TrimEnd('/')}/#reset={token}";
    }

    // Browsers get the access token only (refresh stays in the HttpOnly cookie);
    // native clients additionally get the refresh token in the body to store securely.
    private static IResult TokenResult(HttpContext ctx, string accessToken, string rawRefresh) =>
        TokenIssuer.WantsTokenInBody(ctx)
            ? Results.Ok(new { token = accessToken, refreshToken = rawRefresh })
            : Results.Ok(new { token = accessToken });

    private static string HashEmail(string email) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(email.Trim().ToLowerInvariant())));

    // True unless the email's domain definitively doesn't exist (NXDOMAIN). Deliberately lenient:
    // a domain with only MX records (no A/AAAA) or a transient DNS error fails open so we never
    // block a real user — it only weeds out the obviously made-up domains bots sign up with.
    private static async Task<bool> DomainResolvesAsync(string email)
    {
        var at = email.LastIndexOf('@');
        if (at < 0 || at == email.Length - 1) return false;
        var domain = email[(at + 1)..].Trim();
        try
        {
            return (await Dns.GetHostAddressesAsync(domain)).Length > 0;
        }
        catch (System.Net.Sockets.SocketException ex)
            when (ex.SocketErrorCode == System.Net.Sockets.SocketError.HostNotFound)
        {
            return false; // NXDOMAIN — the domain doesn't exist
        }
        catch
        {
            return true; // MX-only domain (NoData), timeout, or other transient issue — allow
        }
    }

    private record RegisterRequest(string Email, string Password);
    private record LoginRequest(string Email, string Password);
    private record ForgotPasswordRequest(string Email);
    private record ResetPasswordRequest(string Token, string Password);
    private record VerifyEmailRequest(string Token);
}
