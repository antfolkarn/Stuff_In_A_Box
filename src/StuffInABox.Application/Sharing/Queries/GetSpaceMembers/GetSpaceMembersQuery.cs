using MediatR;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Repositories;

namespace StuffInABox.Application.Sharing.Queries.GetSpaceMembers;

/// <summary>Owner lists who has joined the space.</summary>
public sealed record GetSpaceMembersQuery(Guid SpaceId) : IRequest<IReadOnlyList<MemberDto>>;

public sealed class GetSpaceMembersQueryHandler(
    ISpaceMembershipRepository memberships,
    IUserSettingsRepository userSettings,
    IUserIdentityRepository identities,
    ISpaceAccessService access)
    : IRequestHandler<GetSpaceMembersQuery, IReadOnlyList<MemberDto>>
{
    public async Task<IReadOnlyList<MemberDto>> Handle(GetSpaceMembersQuery request, CancellationToken ct)
    {
        await access.RequireSpaceAsync(request.SpaceId, ownerOnly: true, ct);
        var members = await memberships.GetBySpaceAsync(request.SpaceId, ct);

        // Resolve a display label per member: their nickname, else their email, else null.
        var ids = members.Select(m => m.UserId.Value).ToList();
        var nicknames = await userSettings.GetDisplayNamesAsync(ids, ct);
        var emails = await identities.GetEmailsAsync(ids, ct);

        return members
            .OrderBy(m => m.CreatedAt)
            .Select(m => new MemberDto(
                m.UserId.Value,
                m.CreatedAt,
                nicknames.GetValueOrDefault(m.UserId.Value) ?? emails.GetValueOrDefault(m.UserId.Value)))
            .ToList();
    }
}
