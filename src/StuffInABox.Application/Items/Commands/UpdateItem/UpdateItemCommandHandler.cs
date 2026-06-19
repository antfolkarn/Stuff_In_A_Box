using MediatR;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Exceptions;
using StuffInABox.Domain.Repositories;

namespace StuffInABox.Application.Items.Commands.UpdateItem;

public sealed class UpdateItemCommandHandler(
    IItemRepository itemRepo,
    ICurrentUserService currentUser)
    : IRequestHandler<UpdateItemCommand>
{
    public async Task Handle(UpdateItemCommand request, CancellationToken ct)
    {
        var item = await itemRepo.GetByIdAsync(request.ItemId, ct);
        if (item is null || item.OwnerId != currentUser.UserId)
            throw new NotFoundException(nameof(Item), request.ItemId);

        if (request.Name is not null)
            item.Rename(request.Name);

        if (request.Tags is not null)
            item.ReplaceTags(request.Tags);

        await itemRepo.UpdateAsync(item, ct);
    }
}
