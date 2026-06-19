using MediatR;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Repositories;

namespace StuffInABox.Application.Spaces.Commands.DeleteSpace;

public sealed class DeleteSpaceCommandHandler(ISpaceRepository repo, ICurrentUserService currentUser)
    : IRequestHandler<DeleteSpaceCommand>
{
    public async Task Handle(DeleteSpaceCommand request, CancellationToken ct) =>
        await repo.DeleteAsync(request.SpaceId, currentUser.UserId, ct);
}
