using MediatR;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Exceptions;
using StuffInABox.Domain.Repositories;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Application.Boxes.Commands.MoveBox;

public sealed class MoveBoxCommandHandler(
    IBoxRepository boxRepo,
    ISpaceRepository spaceRepo,
    ICurrentUserService currentUser)
    : IRequestHandler<MoveBoxCommand>
{
    public async Task Handle(MoveBoxCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var boxNumber = new BoxNumber(request.BoxNumber);

        var box = await boxRepo.GetByNumberAsync(boxNumber, userId, ct)
            ?? throw new NotFoundException(nameof(Box), request.BoxNumber);

        _ = await spaceRepo.GetByIdAsync(request.TargetSpaceId, userId, ct)
            ?? throw new NotFoundException(nameof(Space), request.TargetSpaceId);

        box.MoveTo(request.TargetSpaceId);
        await boxRepo.UpdateAsync(box, ct);
    }
}
