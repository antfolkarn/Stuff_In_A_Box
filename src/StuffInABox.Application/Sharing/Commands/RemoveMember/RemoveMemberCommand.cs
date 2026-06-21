using MediatR;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Repositories;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Application.Sharing.Commands.RemoveMember;

/// <summary>Owner removes a member's access to the space.</summary>
public sealed record RemoveMemberCommand(Guid SpaceId, Guid UserId) : IRequest;

public sealed class RemoveMemberCommandHandler(
    ISpaceMembershipRepository memberships,
    ISpaceAccessService access)
    : IRequestHandler<RemoveMemberCommand>
{
    public async Task Handle(RemoveMemberCommand request, CancellationToken ct)
    {
        await access.RequireSpaceAsync(request.SpaceId, ownerOnly: true, ct);
        await memberships.RemoveAsync(request.SpaceId, new UserId(request.UserId), ct);
    }
}
