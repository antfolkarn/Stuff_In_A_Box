using StuffInABox.Domain.Entities;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Domain.Repositories;

public interface ISpaceMembershipRepository
{
    Task<bool> ExistsAsync(Guid spaceId, UserId userId, CancellationToken ct = default);
    Task<IReadOnlyList<SpaceMembership>> GetByUserAsync(UserId userId, CancellationToken ct = default);
    Task<IReadOnlyList<SpaceMembership>> GetBySpaceAsync(Guid spaceId, CancellationToken ct = default);
    /// <summary>Number of members in a space, excluding the owner (for quota checks).</summary>
    Task<int> CountBySpaceAsync(Guid spaceId, CancellationToken ct = default);
    Task AddAsync(SpaceMembership membership, CancellationToken ct = default);
    Task RemoveAsync(Guid spaceId, UserId userId, CancellationToken ct = default);
    Task RemoveAllForSpaceAsync(Guid spaceId, CancellationToken ct = default);
    Task RemoveAllForUserAsync(UserId userId, CancellationToken ct = default);
}
