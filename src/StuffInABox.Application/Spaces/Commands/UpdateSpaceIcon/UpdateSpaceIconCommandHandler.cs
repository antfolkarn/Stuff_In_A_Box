using MediatR;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Exceptions;
using StuffInABox.Domain.Repositories;

namespace StuffInABox.Application.Spaces.Commands.UpdateSpaceIcon;

public sealed class UpdateSpaceIconCommandHandler(ISpaceRepository repo, ICurrentUserService currentUser)
    : IRequestHandler<UpdateSpaceIconCommand>
{
    public async Task Handle(UpdateSpaceIconCommand request, CancellationToken ct)
    {
        var space = await repo.GetByIdAsync(request.SpaceId, currentUser.UserId, ct)
            ?? throw new NotFoundException(nameof(Space), request.SpaceId);
        space.ChangeIcon(request.Icon);
        await repo.UpdateAsync(space, ct);
    }
}
