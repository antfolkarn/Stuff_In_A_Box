using Microsoft.Extensions.Configuration;
using StuffInABox.Application.Admin;

namespace StuffInABox.Infrastructure.Admin;

/// <summary>Reads the tiers and their limits from the "Plans" configuration section (each
/// child key is a tier). Falls back to a built-in free/medium/large catalog when nothing is
/// configured. This is the seam where a future DB-backed, admin-editable catalog would slot
/// in — callers use <see cref="IPlanCatalog"/>.</summary>
public sealed class PlanCatalog : IPlanCatalog
{
    private readonly IReadOnlyList<PlanInfo> _plans;
    private readonly Dictionary<string, PlanInfo> _byTier;

    public PlanCatalog(IConfiguration config)
    {
        var configured = config.GetSection("Plans").GetChildren()
            .Select(BindPlan)
            .Where(p => p is not null)
            .Select(p => p!)
            .ToList();

        // Config children come back sorted by key, not declaration order, so impose a
        // stable display order ourselves: cheapest first.
        _plans = (configured.Count > 0 ? configured : DefaultPlans())
            .OrderBy(p => p.PriceSek)
            .ToList();
        _byTier = _plans.ToDictionary(p => p.Tier, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<string> Tiers => _plans.Select(p => p.Tier).ToList();

    public IReadOnlyList<PlanInfo> Plans => _plans;

    public bool IsValidTier(string tier) =>
        !string.IsNullOrWhiteSpace(tier) && _byTier.ContainsKey(tier.Trim());

    public PlanInfo? GetPlan(string tier) =>
        string.IsNullOrWhiteSpace(tier) ? null : _byTier.GetValueOrDefault(tier.Trim());

    private static PlanInfo? BindPlan(IConfigurationSection s)
    {
        var tier = s.Key.Trim().ToLowerInvariant();
        if (tier.Length == 0) return null;
        return new PlanInfo(
            Tier: tier,
            PriceSek: s.GetValue("priceSek", 0),
            MaxSpaces: s.GetValue("maxSpaces", -1),
            MaxItems: s.GetValue("maxItems", -1),
            MaxMembers: s.GetValue("maxMembers", -1),
            AiPhotosPerMonth: s.GetValue("aiPhotosPerMonth", 0),
            StorageMb: s.GetValue<long>("storageMb", 0),
            ClaudeEnrichment: s.GetValue("claudeEnrichment", false),
            PriorityQueue: s.GetValue("priorityQueue", false),
            AllThemes: s.GetValue("allThemes", false));
    }

    // Built-in fallback (the tier sketch) so the catalog works out of the box; appsettings
    // "Plans" overrides it wholesale when present.
    private static List<PlanInfo> DefaultPlans() =>
    [
        new("free",   0,  MaxSpaces: 1,  MaxItems: 100,  MaxMembers: 1,  AiPhotosPerMonth: 20,   StorageMb: 250,   ClaudeEnrichment: false, PriorityQueue: false, AllThemes: false),
        new("medium", 49, MaxSpaces: 5,  MaxItems: 5000, MaxMembers: 4,  AiPhotosPerMonth: 500,  StorageMb: 5000,  ClaudeEnrichment: true,  PriorityQueue: false, AllThemes: true),
        new("large",  99, MaxSpaces: -1, MaxItems: -1,   MaxMembers: -1, AiPhotosPerMonth: 5000, StorageMb: 50000, ClaudeEnrichment: true,  PriorityQueue: true,  AllThemes: true),
    ];
}
