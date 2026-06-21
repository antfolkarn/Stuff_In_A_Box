using MediatR;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Repositories;

namespace StuffInABox.Application.Sharing.Commands.LeaveSpace;

/// <summary>A member leaves a space they were invited to (removes their own access).</summary>
public sealed record LeaveSpaceCommand(Guid SpaceId) : IRequest;

public sealed class LeaveSpaceCommandHandler(
    ISpaceMembershipRepository memberships,
    ICurrentUserService currentUser)
    : IRequestHandler<LeaveSpaceCommand>
{
    public async Task Handle(LeaveSpaceCommand request, CancellationToken ct)
    {
        // Always allowed: you can only remove your own membership. Owners have no
        // membership row, so this is a no-op for them.
        await memberships.RemoveAsync(request.SpaceId, currentUser.UserId, ct);
    }
}
