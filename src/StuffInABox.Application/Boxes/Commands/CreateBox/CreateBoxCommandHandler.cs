using MediatR;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Exceptions;
using StuffInABox.Domain.Repositories;

namespace StuffInABox.Application.Boxes.Commands.CreateBox;

public sealed class CreateBoxCommandHandler(
    IBoxRepository boxRepo,
    ISpaceRepository spaceRepo,
    ICurrentUserService currentUser)
    : IRequestHandler<CreateBoxCommand, CreateBoxResult>
{
    public async Task<CreateBoxResult> Handle(CreateBoxCommand request, CancellationToken ct)
    {
        var space = await spaceRepo.GetByIdAsync(request.SpaceId, currentUser.UserId, ct)
            ?? throw new NotFoundException(nameof(Space), request.SpaceId);

        var nextNumber = await boxRepo.GetNextBoxNumberAsync(currentUser.UserId, ct);
        var box = Box.Create(nextNumber, space.Id, currentUser.UserId, request.Label);
        await boxRepo.AddAsync(box, ct);

        return new CreateBoxResult(box.Number.Value, box.SpaceId, box.Label);
    }
}
