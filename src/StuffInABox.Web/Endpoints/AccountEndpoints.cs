using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using StuffInABox.Application.Account.Commands.DeleteAccount;
using StuffInABox.Application.Account.Queries.ExportAccount;
using StuffInABox.Web.Auth;

namespace StuffInABox.Web.Endpoints;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.V1 + "/account").WithTags("Account").RequireAuthorization();

        // GDPR data portability — download everything we store about the user.
        group.MapGet("/export", async (IMediator mediator, CancellationToken ct) =>
        {
            var export = await mediator.Send(new ExportAccountQuery(), ct);
            var json = JsonSerializer.SerializeToUtf8Bytes(export, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // match the rest of the API
            });
            return Results.File(json, "application/json", $"stuffinabox-export-{DateTime.UtcNow:yyyy-MM-dd}.json");
        }).WithSummary("Exportera all min data (JSON)");

        // GDPR right to erasure — delete the account and all associated data.
        group.MapDelete("/", async (IMediator mediator, HttpContext ctx, CancellationToken ct) =>
        {
            await mediator.Send(new DeleteAccountCommand(), ct);
            TokenIssuer.ClearRefreshCookie(ctx);
            return Results.NoContent();
        }).WithSummary("Radera mitt konto och all data");

        return app;
    }
}
