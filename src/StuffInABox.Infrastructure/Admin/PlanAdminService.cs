using Microsoft.EntityFrameworkCore;
using StuffInABox.Application.Admin;
using StuffInABox.Domain.Entities;
using StuffInABox.Infrastructure.Persistence;

namespace StuffInABox.Infrastructure.Admin;

/// <summary>Admin CRUD over the <c>Plans</c> table. Every write reloads the shared
/// <see cref="IPlanCatalog"/> cache so changes take effect live.</summary>
public sealed class PlanAdminService(AppDbContext db, IPlanCatalog catalog) : IPlanAdminService
{
    public Task<IReadOnlyList<PlanInfo>> ListAsync(CancellationToken ct = default) =>
        Task.FromResult(catalog.Plans);

    public async Task UpsertAsync(PlanInput input, CancellationToken ct = default)
    {
        var tier = input.Tier.Trim().ToLowerInvariant();
        var existing = await db.Plans.FirstOrDefaultAsync(p => p.Tier == tier, ct);
        if (existing is null)
        {
            await db.Plans.AddAsync(Plan.Create(
                tier, input.PriceSek, input.MaxSpaces, input.MaxItems, input.MaxMembers,
                input.AiPhotosPerMonth, input.StorageMb, input.ClaudeEnrichment,
                input.PriorityQueue, input.AllThemes, input.SortOrder), ct);
        }
        else
        {
            existing.Update(
                input.PriceSek, input.MaxSpaces, input.MaxItems, input.MaxMembers,
                input.AiPhotosPerMonth, input.StorageMb, input.ClaudeEnrichment,
                input.PriorityQueue, input.AllThemes, input.SortOrder);
        }

        await db.SaveChangesAsync(ct);
        catalog.Reload();
    }

    public async Task<bool> DeleteAsync(string tier, CancellationToken ct = default)
    {
        tier = tier.Trim().ToLowerInvariant();
        var plan = await db.Plans.FirstOrDefaultAsync(p => p.Tier == tier, ct);
        if (plan is null) return false;

        // Don't orphan accounts onto a tier that no longer exists.
        if (await db.UserSettings.AnyAsync(s => s.PlanTier == tier, ct))
            throw new InvalidOperationException($"Nivån '{tier}' används av minst en användare och kan inte tas bort.");

        db.Plans.Remove(plan);
        await db.SaveChangesAsync(ct);
        catalog.Reload();
        return true;
    }
}
