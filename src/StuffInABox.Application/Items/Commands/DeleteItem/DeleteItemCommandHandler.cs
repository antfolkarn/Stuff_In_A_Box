using MediatR;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Exceptions;
using StuffInABox.Domain.Repositories;

namespace StuffInABox.Application.Items.Commands.DeleteItem;

public sealed class DeleteItemCommandHandler(
    IItemRepository itemRepo,
    IBoxRepository boxRepo,
    IStorageService storage,
    ISpaceAccessService access)
    : IRequestHandler<DeleteItemCommand>
{
    public async Task Handle(DeleteItemCommand request, CancellationToken ct)
    {
        var item = await itemRepo.GetByIdAsync(request.ItemId, ct)
            ?? throw new NotFoundException(nameof(Item), request.ItemId);

        var box = await boxRepo.GetByNumberAsync(item.BoxNumber, item.OwnerId, ct)
            ?? throw new NotFoundException(nameof(Item), request.ItemId);
        await access.RequireSpaceAsync(box.SpaceId, ct: ct);

        if (item.PhotoStorageKey is not null)
            await storage.DeleteAsync(item.PhotoStorageKey, ct);

        await itemRepo.DeleteAsync(item.Id, ct);
    }
}
