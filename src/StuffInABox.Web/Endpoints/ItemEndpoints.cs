using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using StuffInABox.Application.Items.Commands.AddItem;
using StuffInABox.Application.Items.Commands.CreateItemFromPhoto;
using StuffInABox.Application.Items.Commands.DeleteItem;
using StuffInABox.Application.Items.Commands.UpdateItem;
using StuffInABox.Application.Items.Commands.UploadItemPhoto;

namespace StuffInABox.Web.Endpoints;

public static class ItemEndpoints
{
    public static IEndpointRouteBuilder MapItemEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.V1 + "/boxes/{boxNumber:int}/items").WithTags("Items").RequireAuthorization();

        group.MapGet("/", async (int boxNumber, Guid spaceId, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetItemsByBoxQuery(boxNumber, spaceId), ct);
            return Results.Ok(result);
        }).WithSummary("Hämta föremål i en låda");

        group.MapPost("/", async (int boxNumber, AddItemRequest req, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new AddItemCommand(boxNumber, req.SpaceId, req.Name, req.Tags), ct);
            return Results.Created($"{ApiRoutes.V1}/boxes/{boxNumber}/items/{result.ItemId}", result);
        }).WithSummary("Lägg till föremål i låda");

        group.MapPatch("/{itemId:guid}", async (Guid itemId, UpdateItemRequest req, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new UpdateItemCommand(itemId, req.Name, req.Tags), ct);
            return Results.NoContent();
        }).WithSummary("Uppdatera föremål (namn/taggar)");

        group.MapDelete("/{itemId:guid}", async (Guid itemId, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new DeleteItemCommand(itemId), ct);
            return Results.NoContent();
        }).WithSummary("Ta bort föremål");

        group.MapPost("/{itemId:guid}/photo", async (Guid itemId, IFormFile file, IMediator mediator, CancellationToken ct) =>
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms, ct);
            var result = await mediator.Send(
                new UploadItemPhotoCommand(itemId, ms.ToArray(), file.FileName), ct);
            return Results.Ok(result);
        })
        .DisableAntiforgery()
        .WithSummary("Ladda upp foto för föremål");

        // Fast bulk path: upload a photo and create the item immediately with a placeholder;
        // name + tags are filled in by background recognition. The client uploads several of
        // these in parallel (capped) so a batch of photos goes fast.
        group.MapPost("/photo", async (int boxNumber, IFormFile file, [FromForm] Guid spaceId, IMediator mediator, CancellationToken ct) =>
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms, ct);
            var result = await mediator.Send(
                new CreateItemFromPhotoCommand(boxNumber, spaceId, ms.ToArray(), file.FileName), ct);
            return Results.Created($"{ApiRoutes.V1}/boxes/{boxNumber}/items/{result.ItemId}", result);
        })
        .DisableAntiforgery()
        .WithSummary("Skapa föremål från foto (igenkänning i bakgrunden)");

        return app;
    }

    private record AddItemRequest(Guid SpaceId, string Name, IReadOnlyList<string>? Tags);
    private record UpdateItemRequest(string? Name, IReadOnlyList<string>? Tags);
}
