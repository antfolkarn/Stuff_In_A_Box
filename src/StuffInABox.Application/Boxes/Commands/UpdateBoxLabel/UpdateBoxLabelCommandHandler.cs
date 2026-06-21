using MediatR;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Exceptions;
using StuffInABox.Domain.Repositories;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Application.Boxes.Commands.UpdateBoxLabel;

public sealed class UpdateBoxLabelCommandHandler(
    IBoxRepository boxRepo,
    ISpaceAccessService access)
    : IRequestHandler<UpdateBoxLabelCommand>
{
    public async Task Handle(UpdateBoxLabelCommand request, CancellationToken ct)
    {
        var ownerId = await access.RequireSpaceAsync(request.SpaceId, ct: ct);
        var box = await boxRepo.GetByNumberAsync(new BoxNumber(request.BoxNumber), ownerId, ct);
        if (box is null || box.SpaceId != request.SpaceId)
            throw new NotFoundException(nameof(Box), request.BoxNumber);

        box.UpdateLabel(request.Label);
        await boxRepo.UpdateAsync(box, ct);
    }
}
