using StuffInABox.Domain.Entities;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Domain.Repositories;

public interface ISpaceRepository
{
    Task<Space?> GetByIdAsync(Guid id, UserId ownerId, CancellationToken ct = default);
    /// <summary>Unscoped lookup — access control is enforced separately (owner-or-member).</summary>
    Task<Space?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Space>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
    Task<IReadOnlyList<Space>> GetAllAsync(UserId ownerId, CancellationToken ct = default);
    /// <summary>Number of spaces owned by the user (for quota checks).</summary>
    Task<int> CountByOwnerAsync(UserId ownerId, CancellationToken ct = default);
    Task AddAsync(Space space, CancellationToken ct = default);
    Task UpdateAsync(Space space, CancellationToken ct = default);
    Task DeleteAsync(Guid id, UserId ownerId, CancellationToken ct = default);
    Task DeleteAllForOwnerAsync(UserId ownerId, CancellationToken ct = default);
}
