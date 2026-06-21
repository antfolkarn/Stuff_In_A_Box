using StuffInABox.Domain.Entities;

namespace StuffInABox.Domain.Repositories;

public interface ISpaceInviteRepository
{
    Task<SpaceInvite?> GetActiveBySpaceAsync(Guid spaceId, CancellationToken ct = default);
    Task<SpaceInvite?> GetActiveByTokenAsync(string token, CancellationToken ct = default);
    Task AddAsync(SpaceInvite invite, CancellationToken ct = default);
    Task UpdateAsync(SpaceInvite invite, CancellationToken ct = default);
    Task RemoveAllForSpaceAsync(Guid spaceId, CancellationToken ct = default);
}
