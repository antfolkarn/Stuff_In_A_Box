using MediatR;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Repositories;

namespace StuffInABox.Application.Sharing.Queries.GetInvitePreview;

/// <summary>Any signed-in user previews where a share link leads before joining.</summary>
public sealed record GetInvitePreviewQuery(string Token) : IRequest<InvitePreviewDto?>;

public sealed class GetInvitePreviewQueryHandler(
    ISpaceInviteRepository invites,
    ISpaceRepository spaces,
    ISpaceMembershipRepository memberships,
    ICurrentUserService currentUser)
    : IRequestHandler<GetInvitePreviewQuery, InvitePreviewDto?>
{
    public async Task<InvitePreviewDto?> Handle(GetInvitePreviewQuery request, CancellationToken ct)
    {
        var invite = await invites.GetActiveByTokenAsync(request.Token, ct);
        if (invite is null) return null;

        var space = await spaces.GetByIdAsync(invite.SpaceId, ct);
        if (space is null) return null;

        var me = currentUser.UserId;
        var isOwner = space.OwnerId == me;
        var alreadyMember = isOwner || await memberships.ExistsAsync(space.Id, me, ct);
        return new InvitePreviewDto(space.Id, space.Name, alreadyMember, isOwner);
    }
}
