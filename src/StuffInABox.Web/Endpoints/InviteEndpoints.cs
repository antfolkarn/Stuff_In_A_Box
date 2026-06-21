using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using StuffInABox.Application.Sharing.Commands.AcceptInvite;
using StuffInABox.Application.Sharing.Queries.GetInvitePreview;

namespace StuffInABox.Web.Endpoints;

public static class InviteEndpoints
{
    public static IEndpointRouteBuilder MapInviteEndpoints(this IEndpointRouteBuilder app)
    {
        // Any signed-in user can preview/accept a share link they were given.
        var group = app.MapGroup("/api/invites").WithTags("Invites").RequireAuthorization();

        group.MapGet("/{token}", async (string token, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetInvitePreviewQuery(token), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).WithSummary("Förhandsgranska en delningslänk");

        group.MapPost("/{token}/accept", async (string token, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new AcceptInviteCommand(token), ct);
            return Results.Ok(result);
        }).WithSummary("Gå med i ett utrymme via delningslänk");

        return app;
    }
}
