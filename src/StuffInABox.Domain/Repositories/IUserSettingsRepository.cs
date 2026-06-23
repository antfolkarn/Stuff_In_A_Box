using StuffInABox.Domain.Entities;

namespace StuffInABox.Domain.Repositories;

public interface IUserSettingsRepository
{
    Task<UserSettings?> GetAsync(Guid userId, CancellationToken ct = default);
    /// <summary>Display names for the given users that have one set, in a single query.</summary>
    Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct = default);
    Task UpsertAsync(UserSettings settings, CancellationToken ct = default);
    Task DeleteAsync(Guid userId, CancellationToken ct = default);
}
