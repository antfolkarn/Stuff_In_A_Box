using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using StuffInABox.Application.Spaces.Commands.CreateSpace;
using StuffInABox.Application.Spaces.Commands.DeleteSpace;
using StuffInABox.Application.Spaces.Commands.UpdateSpaceIcon;
using StuffInABox.Application.Spaces.Queries.GetSpaces;

namespace StuffInABox.Web.Endpoints;

public static class SpaceEndpoints
{
    public static IEndpointRouteBuilder MapSpaceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/spaces").WithTags("Spaces").RequireAuthorization();

        group.MapGet("/", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetSpacesQuery(), ct);
            return Results.Ok(result);
        }).WithSummary("Hämta alla utrymmen");

        group.MapPost("/", async (CreateSpaceCommand cmd, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(cmd, ct);
            return Results.Created($"/api/spaces/{result.SpaceId}", result);
        }).WithSummary("Skapa nytt utrymme");

        group.MapPatch("/{id:guid}/icon", async (Guid id, UpdateIconRequest req, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new UpdateSpaceIconCommand(id, req.Icon), ct);
            return Results.NoContent();
        }).WithSummary("Byt ikon på utrymme");

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new DeleteSpaceCommand(id), ct);
            return Results.NoContent();
        }).WithSummary("Ta bort utrymme");

        return app;
    }

    private record UpdateIconRequest(string Icon);
}
