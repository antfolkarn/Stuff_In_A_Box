using MediatR;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Repositories;

namespace StuffInABox.Application.Boxes.Commands.CreateBox;

public sealed class CreateBoxCommandHandler(
    IBoxRepository boxRepo,
    ISpaceAccessService access)
    : IRequestHandler<CreateBoxCommand, CreateBoxResult>
{
    public async Task<CreateBoxResult> Handle(CreateBoxCommand request, CancellationToken ct)
    {
        // Members may add content; the box is owned by the space owner and uses the
        // owner's box-number sequence.
        var ownerId = await access.RequireSpaceAsync(request.SpaceId, ct: ct);

        var nextNumber = await boxRepo.GetNextBoxNumberAsync(ownerId, ct);
        var box = Box.Create(nextNumber, request.SpaceId, ownerId, request.Label);
        await boxRepo.AddAsync(box, ct);

        return new CreateBoxResult(box.Number.Value, box.SpaceId, box.Label);
    }
}
