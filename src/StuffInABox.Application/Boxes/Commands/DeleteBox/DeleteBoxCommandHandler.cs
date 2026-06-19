using MediatR;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Exceptions;
using StuffInABox.Domain.Repositories;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Application.Boxes.Commands.DeleteBox;

public sealed class DeleteBoxCommandHandler(
    IBoxRepository boxRepo,
    IItemRepository itemRepo,
    IStorageService storage,
    ICurrentUserService currentUser)
    : IRequestHandler<DeleteBoxCommand>
{
    public async Task Handle(DeleteBoxCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var boxNumber = new BoxNumber(request.BoxNumber);

        var box = await boxRepo.GetByNumberAsync(boxNumber, userId, ct)
            ?? throw new NotFoundException(nameof(Box), request.BoxNumber);

        // Cascade: remove all items in the box (and their photos) first
        var items = await itemRepo.GetByBoxAsync(boxNumber, userId, ct);
        foreach (var item in items)
        {
            if (item.PhotoStorageKey is not null)
                await storage.DeleteAsync(item.PhotoStorageKey, ct);
            await itemRepo.DeleteAsync(item.Id, ct);
        }

        await boxRepo.DeleteAsync(box.Number, userId, ct);
    }
}
