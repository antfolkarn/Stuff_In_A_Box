using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using StuffInABox.Application.Sharing.Commands.CreateInvite;
using StuffInABox.Application.Sharing.Commands.LeaveSpace;
using StuffInABox.Application.Sharing.Commands.RemoveMember;
using StuffInABox.Application.Sharing.Commands.RevokeInvite;
using StuffInABox.Application.Sharing.Queries.GetActiveInvite;
using StuffInABox.Application.Sharing.Queries.GetSpaceMembers;
using StuffInABox.Application.Spaces.Commands.CreateSpace;
using StuffInABox.Application.Spaces.Commands.DeleteSpace;
using StuffInABox.Application.Spaces.Commands.UpdateSpaceIcon;
using StuffInABox.Application.Spaces.Queries.GetSpaces;

namespace StuffInABox.Web.Endpoints;

public static class SpaceEndpoints
{
    public static IEndpointRouteBuilder MapSpaceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.V1 + "/spaces").WithTags("Spaces").RequireAuthorization();

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

        // --- Sharing: invite link (owner) ---
        group.MapPost("/{id:guid}/invite", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new CreateInviteCommand(id), ct);
            return Results.Ok(result);
        }).WithSummary("Skapa delningslänk för utrymme");

        group.MapGet("/{id:guid}/invite", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetActiveInviteQuery(id), ct);
            return result is null ? Results.NoContent() : Results.Ok(result);
        }).WithSummary("Hämta aktiv delningslänk");

        group.MapDelete("/{id:guid}/invite", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new RevokeInviteCommand(id), ct);
            return Results.NoContent();
        }).WithSummary("Återkalla delningslänk");

        // --- Sharing: members (owner) ---
        group.MapGet("/{id:guid}/members", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetSpaceMembersQuery(id), ct);
            return Results.Ok(result);
        }).WithSummary("Hämta medlemmar i utrymme");

        group.MapDelete("/{id:guid}/members/{userId:guid}", async (Guid id, Guid userId, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new RemoveMemberCommand(id, userId), ct);
            return Results.NoContent();
        }).WithSummary("Ta bort medlem ur utrymme");

        // --- Sharing: leave (member) ---
        group.MapDelete("/{id:guid}/membership", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new LeaveSpaceCommand(id), ct);
            return Results.NoContent();
        }).WithSummary("Lämna ett delat utrymme");

        return app;
    }

    private record UpdateIconRequest(string Icon);
}
