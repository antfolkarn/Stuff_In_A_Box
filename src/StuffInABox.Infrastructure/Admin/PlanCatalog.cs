using Microsoft.Extensions.Configuration;
using StuffInABox.Application.Admin;

namespace StuffInABox.Infrastructure.Admin;

/// <summary>Reads the tier list from the "Plans" configuration section (each child key is a
/// tier). Falls back to free/medium/large when nothing is configured. This is the seam where
/// a future DB-backed, admin-editable catalog would slot in — callers use <see cref="IPlanCatalog"/>.</summary>
public sealed class PlanCatalog : IPlanCatalog
{
    private static readonly string[] Fallback = ["free", "medium", "large"];
    private readonly string[] _tiers;

    public PlanCatalog(IConfiguration config)
    {
        var configured = config.GetSection("Plans").GetChildren()
            .Select(c => c.Key.Trim().ToLowerInvariant())
            .Where(k => k.Length > 0)
            .ToArray();
        _tiers = configured.Length > 0 ? configured : Fallback;
    }

    public IReadOnlyList<string> Tiers => _tiers;

    public bool IsValidTier(string tier) =>
        !string.IsNullOrWhiteSpace(tier)
        && _tiers.Contains(tier.Trim().ToLowerInvariant());
}
