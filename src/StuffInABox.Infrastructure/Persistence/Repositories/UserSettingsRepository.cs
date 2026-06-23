using Microsoft.EntityFrameworkCore;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Repositories;

namespace StuffInABox.Infrastructure.Persistence.Repositories;

public class UserSettingsRepository(AppDbContext db) : IUserSettingsRepository
{
    public async Task<UserSettings?> GetAsync(Guid userId, CancellationToken ct = default) =>
        await db.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId, ct);

    public async Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(
        IReadOnlyCollection<Guid> userIds, CancellationToken ct = default)
    {
        if (userIds.Count == 0) return new Dictionary<Guid, string>();
        var rows = await db.UserSettings
            .Where(s => userIds.Contains(s.UserId) && s.DisplayName != null)
            .Select(s => new { s.UserId, s.DisplayName })
            .ToListAsync(ct);
        return rows.ToDictionary(r => r.UserId, r => r.DisplayName!);
    }

    public async Task UpsertAsync(UserSettings settings, CancellationToken ct = default)
    {
        var exists = await db.UserSettings.AnyAsync(s => s.UserId == settings.UserId, ct);
        if (exists)
            db.UserSettings.Update(settings);
        else
            await db.UserSettings.AddAsync(settings, ct);
        await db.SaveChangesAsync(ct);
    }

    public Task DeleteAsync(Guid userId, CancellationToken ct = default) =>
        db.UserSettings.Where(s => s.UserId == userId).ExecuteDeleteAsync(ct);
}
