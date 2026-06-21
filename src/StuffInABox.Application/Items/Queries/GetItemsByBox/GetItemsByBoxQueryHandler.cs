using MediatR;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Repositories;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Application.Items.Commands.AddItem;

public sealed class GetItemsByBoxQueryHandler(
    IItemRepository repo,
    IBoxRepository boxRepo,
    ISpaceAccessService access,
    IStorageService storage)
    : IRequestHandler<GetItemsByBoxQuery, IReadOnlyList<ItemDto>>
{
    public async Task<IReadOnlyList<ItemDto>> Handle(GetItemsByBoxQuery request, CancellationToken ct)
    {
        var ownerId = await access.RequireSpaceAsync(request.SpaceId, ct: ct);
        var boxNumber = new BoxNumber(request.BoxNumber);

        // Confirm the box is in the authorized space before listing its items.
        var box = await boxRepo.GetByNumberAsync(boxNumber, ownerId, ct);
        if (box is null || box.SpaceId != request.SpaceId) return [];

        var items = await repo.GetByBoxAsync(boxNumber, ownerId, ct);
        return items.Select(i => new ItemDto(
            i.Id,
            i.Name,
            i.Tags,
            i.PhotoStorageKey is not null ? storage.GetUrl(i.PhotoStorageKey) : null))
            .ToList();
    }
}
