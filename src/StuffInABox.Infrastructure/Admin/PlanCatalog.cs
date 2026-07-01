using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StuffInABox.Application.Admin;
using StuffInABox.Domain.Entities;
using StuffInABox.Infrastructure.Persistence;

namespace StuffInABox.Infrastructure.Admin;

/// <summary>Reads the tiers and their limits from the database (the <c>Plans</c> table, edited
/// live by the admin app), with an in-memory cache. Falls back to <see cref="PlanDefaults"/>
/// only when the table is empty. Callers depend on <see cref="IPlanCatalog"/>; this is a
/// singleton, so it reaches the scoped <see cref="AppDbContext"/> via a scope factory.</summary>
public sealed class PlanCatalog(IServiceScopeFactory scopeFactory) : IPlanCatalog
{
    private readonly object _lock = new();
    private IReadOnlyList<PlanInfo>? _cache;

    public IReadOnlyList<string> Tiers => Current().Select(p => p.Tier).ToList();

    public IReadOnlyList<PlanInfo> Plans => Current();

    public bool IsValidTier(string tier) =>
        !string.IsNullOrWhiteSpace(tier)
        && Current().Any(p => string.Equals(p.Tier, tier.Trim(), StringComparison.OrdinalIgnoreCase));

    public PlanInfo? GetPlan(string tier) =>
        string.IsNullOrWhiteSpace(tier)
            ? null
            : Current().FirstOrDefault(p => string.Equals(p.Tier, tier.Trim(), StringComparison.OrdinalIgnoreCase));

    public void Reload()
    {
        lock (_lock) { _cache = Load(); }
    }

    private IReadOnlyList<PlanInfo> Current()
    {
        if (_cache is not null) return _cache;
        lock (_lock) { _cache ??= Load(); }
        return _cache;
    }

    private IReadOnlyList<PlanInfo> Load()
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        List<PlanInfo> plans;
        try
        {
            plans = db.Plans.AsNoTracking().ToList()
                .OrderBy(p => p.SortOrder).ThenBy(p => p.PriceSek)
                .Select(ToInfo).ToList();
        }
        catch (DbException)
        {
            // Stale dev SQLite DB without the Plans table — fall back to the built-in defaults
            // until it's recreated. Postgres always has the table (migration), so this is dev-only.
            plans = [];
        }

        return plans.Count > 0 ? plans : PlanDefaults.All().Select(ToInfo).ToList();
    }

    private static PlanInfo ToInfo(Plan p) => new(
        p.Tier, p.PriceSek, p.MaxSpaces, p.MaxItems, p.MaxMembers,
        p.AiPhotosPerMonth, p.StorageMb, p.ClaudeEnrichment, p.PriorityQueue, p.AllThemes);
}
