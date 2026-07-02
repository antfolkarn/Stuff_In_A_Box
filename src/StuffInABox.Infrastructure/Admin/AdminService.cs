using Microsoft.EntityFrameworkCore;
using StuffInABox.Application.Admin;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.ValueObjects;
using StuffInABox.Infrastructure.Persistence;

namespace StuffInABox.Infrastructure.Admin;

/// <summary>Admin operations against accounts and subscriptions, backed directly by the
/// shared <see cref="AppDbContext"/>. Consumed only by the separate admin host.</summary>
public sealed class AdminService(AppDbContext db, IPlanCatalog catalog, IAccountDeletionService deletion) : IAdminService
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

        // Group login methods into persons (identities that share a UserId), so a linked
        // email + Google account shows as one row. Ordering + IsDisabled/IsEmailVerified are
        // computed, so materialize first (SQLite also can't ORDER BY a DateTimeOffset in SQL).
        var persons = (await q.ToListAsync(ct))
            .GroupBy(u => u.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                Email = g.Select(u => u.Email).FirstOrDefault(e => !string.IsNullOrWhiteSpace(e)),
                Providers = g.Select(u => u.Provider).Distinct().OrderBy(p => p).ToList(),
                EmailVerified = g.Any(u => u.IsEmailVerified),
                IsDisabled = g.Any(u => u.IsDisabled), // disabled if any login method is disabled
                CreatedAt = g.Min(u => u.CreatedAt),
            })
            .OrderByDescending(p => p.CreatedAt)
            .Take(ListCap)
            .ToList();

        var ids = persons.Select(p => p.UserId).ToList();

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

        return persons.Select(p => new AdminUserRow(
            UserId: p.UserId,
            Email: p.Email,
            Providers: p.Providers,
            EmailVerified: p.EmailVerified,
            PlanTier: planByUser.GetValueOrDefault(p.UserId, UserSettings.DefaultPlanTier),
            IsDisabled: p.IsDisabled,
            CreatedAt: p.CreatedAt,
            SpaceCount: spaceCounts.GetValueOrDefault(p.UserId),
            ItemCount: itemCounts.GetValueOrDefault(p.UserId)))
            .ToList();
    }

    public async Task<bool> SetPlanTierAsync(Guid userId, string tier, CancellationToken ct = default)
    {
        if (!catalog.IsValidTier(tier))
            throw new ArgumentException($"Okänd plan '{tier}'.", nameof(tier));

        var exists = await db.UserIdentities.AnyAsync(u => u.UserId == userId, ct);
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
        // Disable/enable applies to the whole person — every linked login method.
        var identities = await db.UserIdentities.Where(u => u.UserId == userId).ToListAsync(ct);
        if (identities.Count == 0) return false;

        foreach (var identity in identities)
        {
            if (disabled) identity.Disable();
            else identity.Enable();
        }

        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteUserAsync(Guid userId, CancellationToken ct = default)
    {
        var exists = await db.UserIdentities.AnyAsync(u => u.UserId == userId, ct);
        if (!exists) return false;

        await deletion.DeleteAsync(new UserId(userId), ct);
        return true;
    }
}
