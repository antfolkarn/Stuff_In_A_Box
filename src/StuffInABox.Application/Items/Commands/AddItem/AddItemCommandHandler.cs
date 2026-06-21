using MediatR;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Exceptions;
using StuffInABox.Domain.Repositories;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Application.Items.Commands.AddItem;

public sealed class AddItemCommandHandler(
    IItemRepository itemRepo,
    IBoxRepository boxRepo,
    ISpaceAccessService access,
    IEnrichmentQueue enrichmentQueue)
    : IRequestHandler<AddItemCommand, AddItemResult>
{
    public async Task<AddItemResult> Handle(AddItemCommand request, CancellationToken ct)
    {
        var ownerId = await access.RequireSpaceAsync(request.SpaceId, ct: ct);
        var boxNumber = new BoxNumber(request.BoxNumber);

        var box = await boxRepo.GetByNumberAsync(boxNumber, ownerId, ct);
        if (box is null || box.SpaceId != request.SpaceId)
            throw new NotFoundException(nameof(Box), request.BoxNumber);

        // Content is owned by the space owner regardless of which member added it.
        var item = Item.Create(boxNumber, ownerId, request.Name);

        // Synchronous tokenizer tags from the item name
        var quickTags = Tokenize(request.Name);
        item.ReplaceTags(quickTags);

        // Merge in tags detected from the photo (objects, colours, material, …)
        if (request.Tags is { Count: > 0 })
            item.MergeTags(request.Tags);

        await itemRepo.AddAsync(item, ct);

        // Fire-and-forget async enrichment (LLM/tagging service)
        enrichmentQueue.EnqueueEnrichment(item.Id, item.Name);

        return new AddItemResult(item.Id, item.Name, item.Tags);
    }

    private static IEnumerable<string> Tokenize(string name) =>
        name.Split([' ', '-', '_', ',', '.'], StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim().ToLowerInvariant())
            .Where(t => t.Length > 1);
}
