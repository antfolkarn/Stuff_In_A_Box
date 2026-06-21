using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Exceptions;
using StuffInABox.Domain.Repositories;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Application.Common.Access;

public sealed class SpaceAccessService(
    ISpaceRepository spaces,
    ISpaceMembershipRepository memberships,
    ICurrentUserService currentUser) : ISpaceAccessService
{
    public async Task<UserId> RequireSpaceAsync(Guid spaceId, bool ownerOnly = false, CancellationToken ct = default)
    {
        var space = await spaces.GetByIdAsync(spaceId, ct)
            ?? throw new NotFoundException(nameof(Space), spaceId);

        var me = currentUser.UserId;
        if (space.OwnerId == me)
            return space.OwnerId;

        if (!ownerOnly && await memberships.ExistsAsync(spaceId, me, ct))
            return space.OwnerId;

        throw new ForbiddenException("You do not have access to this space.");
    }

    public async Task<IReadOnlyList<Space>> GetAccessibleSpacesAsync(CancellationToken ct = default)
    {
        var me = currentUser.UserId;
        var owned = await spaces.GetAllAsync(me, ct);

        var myMemberships = await memberships.GetByUserAsync(me, ct);
        var ownedIds = owned.Select(s => s.Id).ToHashSet();
        var memberSpaceIds = myMemberships
            .Select(m => m.SpaceId)
            .Where(id => !ownedIds.Contains(id)) // an owner is never also "a member"
            .ToList();

        var memberSpaces = await spaces.GetByIdsAsync(memberSpaceIds, ct);
        return [.. owned, .. memberSpaces];
    }
}
