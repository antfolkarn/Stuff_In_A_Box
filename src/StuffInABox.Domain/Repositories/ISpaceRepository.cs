using StuffInABox.Domain.Entities;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Domain.Repositories;

public interface ISpaceRepository
{
    Task<Space?> GetByIdAsync(Guid id, UserId ownerId, CancellationToken ct = default);
    Task<IReadOnlyList<Space>> GetAllAsync(UserId ownerId, CancellationToken ct = default);
    Task AddAsync(Space space, CancellationToken ct = default);
    Task UpdateAsync(Space space, CancellationToken ct = default);
    Task DeleteAsync(Guid id, UserId ownerId, CancellationToken ct = default);
}
