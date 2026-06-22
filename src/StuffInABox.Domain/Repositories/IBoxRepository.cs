using StuffInABox.Domain.Entities;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Domain.Repositories;

public interface IBoxRepository
{
    Task<Box?> GetByNumberAsync(BoxNumber number, UserId ownerId, CancellationToken ct = default);
    Task<IReadOnlyList<Box>> GetBySpaceAsync(Guid spaceId, UserId ownerId, CancellationToken ct = default);
    Task<BoxNumber> GetNextBoxNumberAsync(UserId ownerId, CancellationToken ct = default);
    Task AddAsync(Box box, CancellationToken ct = default);
    Task UpdateAsync(Box box, CancellationToken ct = default);
    Task DeleteAsync(BoxNumber number, UserId ownerId, CancellationToken ct = default);
    Task DeleteAllForOwnerAsync(UserId ownerId, CancellationToken ct = default);
}
