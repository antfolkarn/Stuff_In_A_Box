using StuffInABox.Domain.Entities;

namespace StuffInABox.Domain.Repositories;

public interface IUserIdentityRepository
{
    Task<UserIdentity?> FindAsync(string provider, string externalId, CancellationToken ct = default);
    Task<UserIdentity?> FindByIdAsync(Guid internalUserId, CancellationToken ct = default);
    /// <summary>Finds an identity by stored email (case-insensitive), preferring a verified one.
    /// Used to block duplicate registrations and to link an OAuth login to an existing account.</summary>
    Task<UserIdentity?> FindByEmailAsync(string email, CancellationToken ct = default);
    /// <summary>All login methods belonging to one person (share the same <c>UserId</c>).</summary>
    Task<IReadOnlyList<UserIdentity>> FindAllByUserIdAsync(Guid userId, CancellationToken ct = default);
    /// <summary>Emails for the given users that have one stored, in a single query.</summary>
    Task<IReadOnlyDictionary<Guid, string>> GetEmailsAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct = default);
    Task AddAsync(UserIdentity identity, CancellationToken ct = default);
    Task UpdateAsync(UserIdentity identity, CancellationToken ct = default);
    /// <summary>Deletes every login method belonging to the person (by <c>UserId</c>).</summary>
    Task DeleteAsync(Guid userId, CancellationToken ct = default);
}
