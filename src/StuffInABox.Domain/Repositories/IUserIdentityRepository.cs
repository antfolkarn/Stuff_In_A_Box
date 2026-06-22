using StuffInABox.Domain.Entities;

namespace StuffInABox.Domain.Repositories;

public interface IUserIdentityRepository
{
    Task<UserIdentity?> FindAsync(string provider, string externalId, CancellationToken ct = default);
    Task<UserIdentity?> FindByIdAsync(Guid internalUserId, CancellationToken ct = default);
    Task AddAsync(UserIdentity identity, CancellationToken ct = default);
    Task UpdateAsync(UserIdentity identity, CancellationToken ct = default);
}
