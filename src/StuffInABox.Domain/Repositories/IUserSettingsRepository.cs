using StuffInABox.Domain.Entities;

namespace StuffInABox.Domain.Repositories;

public interface IUserSettingsRepository
{
    Task<UserSettings?> GetAsync(Guid userId, CancellationToken ct = default);
    Task UpsertAsync(UserSettings settings, CancellationToken ct = default);
    Task DeleteAsync(Guid userId, CancellationToken ct = default);
}
