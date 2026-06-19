using MediatR;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Exceptions;
using StuffInABox.Domain.Repositories;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Application.Boxes.Commands.UpdateBoxLabel;

public sealed class UpdateBoxLabelCommandHandler(
    IBoxRepository boxRepo,
    ICurrentUserService currentUser)
    : IRequestHandler<UpdateBoxLabelCommand>
{
    public async Task Handle(UpdateBoxLabelCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var box = await boxRepo.GetByNumberAsync(new BoxNumber(request.BoxNumber), userId, ct)
            ?? throw new NotFoundException(nameof(Box), request.BoxNumber);

        box.UpdateLabel(request.Label);
        await boxRepo.UpdateAsync(box, ct);
    }
}
