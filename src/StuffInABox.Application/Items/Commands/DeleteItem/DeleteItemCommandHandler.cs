using MediatR;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Exceptions;
using StuffInABox.Domain.Repositories;

namespace StuffInABox.Application.Items.Commands.DeleteItem;

public sealed class DeleteItemCommandHandler(
    IItemRepository itemRepo,
    IStorageService storage,
    ICurrentUserService currentUser)
    : IRequestHandler<DeleteItemCommand>
{
    public async Task Handle(DeleteItemCommand request, CancellationToken ct)
    {
        var item = await itemRepo.GetByIdAsync(request.ItemId, ct);
        if (item is null || item.OwnerId != currentUser.UserId)
            throw new NotFoundException(nameof(Item), request.ItemId);

        if (item.PhotoStorageKey is not null)
            await storage.DeleteAsync(item.PhotoStorageKey, ct);

        await itemRepo.DeleteAsync(item.Id, ct);
    }
}
