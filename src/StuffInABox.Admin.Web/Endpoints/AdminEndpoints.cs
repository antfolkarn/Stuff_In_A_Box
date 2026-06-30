using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using StuffInABox.Application.Admin;

namespace StuffInABox.Admin.Web.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        // Browser navigations: start the Entra sign-in, then sign out.
        app.MapGet("/login", (string? returnUrl) =>
            Results.Challenge(
                new AuthenticationProperties { RedirectUri = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl },
                [OpenIdConnectDefaults.AuthenticationScheme]));

        app.MapGet("/logout", () =>
            Results.SignOut(
                new AuthenticationProperties { RedirectUri = "/" },
                [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]));

        var g = app.MapGroup("/api/admin").RequireAuthorization("admin").WithTags("Admin");
        g.MapGet("/me", (ClaimsPrincipal user) => Results.Ok(new
        {
            name = user.Identity?.Name,
            email = user.FindFirstValue("preferred_username")
                    ?? user.FindFirstValue(ClaimTypes.Email)
                    ?? user.Identity?.Name,
        }));
        g.MapGet("/plans", (IPlanCatalog catalog) => Results.Ok(new { tiers = catalog.Tiers }));
        g.MapGet("/users", ListUsersAsync);
        g.MapPost("/users/{id:guid}/plan", SetPlanAsync);
        g.MapPost("/users/{id:guid}/disable", DisableAsync);
        g.MapPost("/users/{id:guid}/enable", EnableAsync);

        return app;
    }

    private static async Task<IResult> ListUsersAsync(string? query, IAdminService svc, CancellationToken ct) =>
        Results.Ok(await svc.ListUsersAsync(query, ct));

    private static async Task<IResult> SetPlanAsync(Guid id, SetPlanRequest req, IAdminService svc, CancellationToken ct)
    {
        try
        {
            return await svc.SetPlanTierAsync(id, req.Tier, ct)
                ? Results.Ok()
                : Results.NotFound();
        }
        catch (ArgumentException e)
        {
            return Results.BadRequest(new { error = e.Message });
        }
    }

    private static async Task<IResult> DisableAsync(Guid id, IAdminService svc, CancellationToken ct) =>
        await svc.SetDisabledAsync(id, true, ct) ? Results.Ok() : Results.NotFound();

    private static async Task<IResult> EnableAsync(Guid id, IAdminService svc, CancellationToken ct) =>
        await svc.SetDisabledAsync(id, false, ct) ? Results.Ok() : Results.NotFound();

    private record SetPlanRequest(string Tier);
}
