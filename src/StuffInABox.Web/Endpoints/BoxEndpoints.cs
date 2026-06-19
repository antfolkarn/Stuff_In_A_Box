using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using StuffInABox.Application.Boxes.Commands.CreateBox;
using StuffInABox.Application.Boxes.Commands.DeleteBox;
using StuffInABox.Application.Boxes.Commands.MoveBox;
using StuffInABox.Application.Boxes.Commands.UpdateBoxLabel;
using StuffInABox.Application.Boxes.Queries.GetBoxDetail;
using StuffInABox.Application.Boxes.Queries.GetBoxesBySpace;

namespace StuffInABox.Web.Endpoints;

public static class BoxEndpoints
{
    public static IEndpointRouteBuilder MapBoxEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/boxes").WithTags("Boxes").RequireAuthorization();

        group.MapGet("/space/{spaceId:guid}", async (Guid spaceId, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetBoxesBySpaceQuery(spaceId), ct);
            return Results.Ok(result);
        }).WithSummary("Hämta lådor för ett utrymme");

        group.MapGet("/{number:int}", async (int number, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetBoxDetailQuery(number), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).WithSummary("Hämta detaljer för en låda");

        group.MapPost("/", async (CreateBoxCommand cmd, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(cmd, ct);
            return Results.Created($"/api/boxes/{result.BoxNumber}", result);
        }).WithSummary("Skapa ny låda");

        group.MapPatch("/{number:int}/space", async (int number, MoveBoxRequest req, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new MoveBoxCommand(number, req.SpaceId), ct);
            return Results.NoContent();
        }).WithSummary("Flytta låda till annat utrymme");

        group.MapPatch("/{number:int}/label", async (int number, UpdateLabelRequest req, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new UpdateBoxLabelCommand(number, req.Label), ct);
            return Results.NoContent();
        }).WithSummary("Byt namn på låda");

        group.MapDelete("/{number:int}", async (int number, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new DeleteBoxCommand(number), ct);
            return Results.NoContent();
        }).WithSummary("Ta bort låda och dess föremål");

        return app;
    }

    private record MoveBoxRequest(Guid SpaceId);
    private record UpdateLabelRequest(string Label);
}
