using MediatR;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Repositories;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Application.Items.Commands.AddItem;

public sealed class GetItemsByBoxQueryHandler(
    IItemRepository repo,
    ICurrentUserService currentUser,
    IStorageService storage)
    : IRequestHandler<GetItemsByBoxQuery, IReadOnlyList<ItemDto>>
{
    public async Task<IReadOnlyList<ItemDto>> Handle(GetItemsByBoxQuery request, CancellationToken ct)
    {
        var items = await repo.GetByBoxAsync(new BoxNumber(request.BoxNumber), currentUser.UserId, ct);
        return items.Select(i => new ItemDto(
            i.Id,
            i.Name,
            i.Tags,
            i.PhotoStorageKey is not null ? storage.GetUrl(i.PhotoStorageKey) : null))
            .ToList();
    }
}
