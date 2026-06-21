using MediatR;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Repositories;

namespace StuffInABox.Application.Sharing.Queries.GetSpaceMembers;

/// <summary>Owner lists who has joined the space.</summary>
public sealed record GetSpaceMembersQuery(Guid SpaceId) : IRequest<IReadOnlyList<MemberDto>>;

public sealed class GetSpaceMembersQueryHandler(
    ISpaceMembershipRepository memberships,
    ISpaceAccessService access)
    : IRequestHandler<GetSpaceMembersQuery, IReadOnlyList<MemberDto>>
{
    public async Task<IReadOnlyList<MemberDto>> Handle(GetSpaceMembersQuery request, CancellationToken ct)
    {
        await access.RequireSpaceAsync(request.SpaceId, ownerOnly: true, ct);
        var members = await memberships.GetBySpaceAsync(request.SpaceId, ct);
        return members
            .OrderBy(m => m.CreatedAt)
            .Select(m => new MemberDto(m.UserId.Value, m.CreatedAt))
            .ToList();
    }
}
