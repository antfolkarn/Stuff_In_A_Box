using Microsoft.EntityFrameworkCore;
using StuffInABox.Application.Admin;
using StuffInABox.Domain.Entities;
using StuffInABox.Infrastructure.Persistence;

namespace StuffInABox.Infrastructure.Admin;

/// <summary>Admin operations against accounts and subscriptions, backed directly by the
/// shared <see cref="AppDbContext"/>. Consumed only by the separate admin host.</summary>
public sealed class AdminService(AppDbContext db, IPlanCatalog catalog) : IAdminService
{
    private const int ListCap = 500;

    public async Task<IReadOnlyList<AdminUserRow>> ListUsersAsync(string? query, CancellationToken ct = default)
    {
        var q = db.UserIdentities.AsQueryable();
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            q = q.Where(u => u.Email != null && EF.Functions.Like(u.Email, $"%{term}%"));
        }

        // Order on the client: SQLite can't ORDER BY a DateTimeOffset in SQL.
        var users = (await q.ToListAsync(ct))
            .OrderByDescending(u => u.CreatedAt)
            .Take(ListCap)
            .ToList();

        var ids = users.Select(u => u.InternalUserId).ToList();

        var plans = await db.UserSettings
            .Where(s => ids.Contains(s.UserId))
            .Select(s => new { s.UserId, s.PlanTier })
            .ToListAsync(ct);
        var planByUser = plans.ToDictionary(s => s.UserId, s => s.PlanTier);

        // Owner ids are value objects; pull them and aggregate in memory to avoid
        // value-object translation pitfalls. Fine at this scale; revisit if it grows.
        var spaceCounts = (await db.Spaces.Select(s => s.OwnerId).ToListAsync(ct))
            .GroupBy(o => o.Value).ToDictionary(g => g.Key, g => g.Count());
        var itemCounts = (await db.Items.Select(i => i.OwnerId).ToListAsync(ct))
            .GroupBy(o => o.Value).ToDictionary(g => g.Key, g => g.Count());

        return users.Select(u => new AdminUserRow(
            UserId: u.InternalUserId,
            Email: u.Email,
            Provider: u.Provider,
            EmailVerified: u.IsEmailVerified,
            PlanTier: planByUser.GetValueOrDefault(u.InternalUserId, UserSettings.DefaultPlanTier),
            IsDisabled: u.IsDisabled,
            CreatedAt: u.CreatedAt,
            SpaceCount: spaceCounts.GetValueOrDefault(u.InternalUserId),
            ItemCount: itemCounts.GetValueOrDefault(u.InternalUserId)))
            .ToList();
    }

    public async Task<bool> SetPlanTierAsync(Guid userId, string tier, CancellationToken ct = default)
    {
        if (!catalog.IsValidTier(tier))
            throw new ArgumentException($"Okänd plan '{tier}'.", nameof(tier));

        var exists = await db.UserIdentities.AnyAsync(u => u.InternalUserId == userId, ct);
        if (!exists) return false;

        var settings = await db.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId, ct);
        if (settings is null)
        {
            settings = UserSettings.CreateDefault(userId);
            settings.SetPlanTier(tier);
            await db.UserSettings.AddAsync(settings, ct);
        }
        else
        {
            settings.SetPlanTier(tier);
        }

        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> SetDisabledAsync(Guid userId, bool disabled, CancellationToken ct = default)
    {
        var identity = await db.UserIdentities.FirstOrDefaultAsync(u => u.InternalUserId == userId, ct);
        if (identity is null) return false;

        if (disabled) identity.Disable();
        else identity.Enable();

        await db.SaveChangesAsync(ct);
        return true;
    }
}
