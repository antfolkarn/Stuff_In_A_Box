using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using StuffInABox.Application.Settings.Commands;
using StuffInABox.Application.Settings.Queries;

namespace StuffInABox.Web.Endpoints;

public static class SettingsEndpoints
{
    public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.V1 + "/settings").WithTags("Settings").RequireAuthorization();

        group.MapGet("/", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetSettingsQuery(), ct);
            return Results.Ok(result);
        }).WithSummary("Hämta användarens inställningar (tema, design)");

        group.MapPut("/", async (UpdateSettingsCommand cmd, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(cmd, ct);
            return Results.Ok(result);
        }).WithSummary("Spara användarens inställningar");

        return app;
    }
}
