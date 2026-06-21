using MediatR;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Exceptions;
using StuffInABox.Domain.Repositories;

namespace StuffInABox.Application.Items.Commands.UpdateItem;

public sealed class UpdateItemCommandHandler(
    IItemRepository itemRepo,
    IBoxRepository boxRepo,
    ISpaceAccessService access)
    : IRequestHandler<UpdateItemCommand>
{
    public async Task Handle(UpdateItemCommand request, CancellationToken ct)
    {
        var item = await itemRepo.GetByIdAsync(request.ItemId, ct)
            ?? throw new NotFoundException(nameof(Item), request.ItemId);

        // Authorize via the item's box → space (owner or invited member).
        var box = await boxRepo.GetByNumberAsync(item.BoxNumber, item.OwnerId, ct)
            ?? throw new NotFoundException(nameof(Item), request.ItemId);
        await access.RequireSpaceAsync(box.SpaceId, ct: ct);

        if (request.Name is not null)
            item.Rename(request.Name);

        if (request.Tags is not null)
            item.ReplaceTags(request.Tags);

        await itemRepo.UpdateAsync(item, ct);
    }
}
