using MediatR;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Repositories;

namespace StuffInABox.Application.Spaces.Commands.DeleteSpace;

public sealed class DeleteSpaceCommandHandler(
    ISpaceRepository repo,
    ISpaceMembershipRepository membershipRepo,
    ISpaceInviteRepository inviteRepo,
    ISpaceAccessService access)
    : IRequestHandler<DeleteSpaceCommand>
{
    public async Task Handle(DeleteSpaceCommand request, CancellationToken ct)
    {
        // Only the owner may delete the space.
        var ownerId = await access.RequireSpaceAsync(request.SpaceId, ownerOnly: true, ct);

        // Clean up access grants so nothing dangles after the space is gone.
        await membershipRepo.RemoveAllForSpaceAsync(request.SpaceId, ct);
        await inviteRepo.RemoveAllForSpaceAsync(request.SpaceId, ct);
        await repo.DeleteAsync(request.SpaceId, ownerId, ct);
    }
}
