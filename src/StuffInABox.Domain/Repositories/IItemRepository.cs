using StuffInABox.Domain.Entities;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Domain.Repositories;

public interface IItemRepository
{
    Task<Item?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Item>> GetByBoxAsync(BoxNumber boxNumber, UserId ownerId, CancellationToken ct = default);
    /// <summary>Item count per box number for an owner, in a single query (avoids N+1 when listing).</summary>
    Task<IReadOnlyDictionary<int, int>> GetCountsByBoxAsync(UserId ownerId, CancellationToken ct = default);
    Task<IReadOnlyList<Item>> GetByOwnerAsync(UserId ownerId, CancellationToken ct = default);
    Task<IReadOnlyList<Item>> SearchAsync(UserId ownerId, string query, CancellationToken ct = default);
    Task AddAsync(Item item, CancellationToken ct = default);
    Task UpdateAsync(Item item, CancellationToken ct = default);
    Task DeleteAsync(Guid itemId, CancellationToken ct = default);
    Task DeleteAllForOwnerAsync(UserId ownerId, CancellationToken ct = default);
}
