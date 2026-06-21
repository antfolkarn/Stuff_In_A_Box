using MediatR;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Exceptions;
using StuffInABox.Domain.Repositories;

namespace StuffInABox.Application.Sharing.Commands.AcceptInvite;

/// <summary>Any signed-in user joins a space by redeeming a valid share token.</summary>
public sealed record AcceptInviteCommand(string Token) : IRequest<AcceptInviteResult>;

public sealed class AcceptInviteCommandHandler(
    ISpaceInviteRepository invites,
    ISpaceRepository spaces,
    ISpaceMembershipRepository memberships,
    ICurrentUserService currentUser)
    : IRequestHandler<AcceptInviteCommand, AcceptInviteResult>
{
    public async Task<AcceptInviteResult> Handle(AcceptInviteCommand request, CancellationToken ct)
    {
        var invite = await invites.GetActiveByTokenAsync(request.Token, ct)
            ?? throw new NotFoundException("Invite", request.Token);

        var space = await spaces.GetByIdAsync(invite.SpaceId, ct)
            ?? throw new NotFoundException(nameof(Space), invite.SpaceId);

        var me = currentUser.UserId;
        // The owner is already in; only add a membership for genuinely new members.
        if (space.OwnerId != me && !await memberships.ExistsAsync(space.Id, me, ct))
            await memberships.AddAsync(SpaceMembership.Create(space.Id, me), ct);

        return new AcceptInviteResult(space.Id, space.Name);
    }
}
