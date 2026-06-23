using StuffInABox.Domain.Entities;

namespace StuffInABox.Domain.Repositories;

public interface IUserIdentityRepository
{
    Task<UserIdentity?> FindAsync(string provider, string externalId, CancellationToken ct = default);
    Task<UserIdentity?> FindByIdAsync(Guid internalUserId, CancellationToken ct = default);
    /// <summary>Emails for the given users that have one stored, in a single query.</summary>
    Task<IReadOnlyDictionary<Guid, string>> GetEmailsAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct = default);
    Task AddAsync(UserIdentity identity, CancellationToken ct = default);
    Task UpdateAsync(UserIdentity identity, CancellationToken ct = default);
    Task DeleteAsync(Guid internalUserId, CancellationToken ct = default);
}
