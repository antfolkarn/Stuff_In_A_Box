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
    ICurrentUserService currentUser,
    IEnrichmentQueue enrichmentQueue)
    : IRequestHandler<AddItemCommand, AddItemResult>
{
    public async Task<AddItemResult> Handle(AddItemCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var boxNumber = new BoxNumber(request.BoxNumber);

        _ = await boxRepo.GetByNumberAsync(boxNumber, userId, ct)
            ?? throw new NotFoundException(nameof(Box), request.BoxNumber);

        var item = Item.Create(boxNumber, userId, request.Name);

        // Synchronous tokenizer tags from the item name
        var quickTags = Tokenize(request.Name);
        item.ReplaceTags(quickTags);

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
