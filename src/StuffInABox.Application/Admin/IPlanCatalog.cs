namespace StuffInABox.Application.Admin;

/// <summary>A single subscription tier and its limits/flags. <c>-1</c> means "unlimited".
/// Kept as explicit fields (matching the tier sketch) rather than a generic bag — simple
/// to consume in the UI today; can grow into a key→value catalog when the admin editor lands.</summary>
public sealed record PlanInfo(
    string Tier,
    int PriceSek,
    int MaxSpaces,
    int MaxItems,
    int MaxMembers,
    int AiPhotosPerMonth,
    long StorageMb,
    bool ClaudeEnrichment,
    bool PriorityQueue,
    bool AllThemes);

/// <summary>The set of subscription tiers the system knows about, plus their limits.
/// Backed by configuration today (the "Plans" section, falling back to built-in defaults);
/// can move to the database later so the admin UI edits it live — callers depend only on
/// this interface.</summary>
public interface IPlanCatalog
{
    /// <summary>Tier keys in display order, e.g. ["free", "medium", "large"].</summary>
    IReadOnlyList<string> Tiers { get; }

    /// <summary>Every plan in display order (cheapest first).</summary>
    IReadOnlyList<PlanInfo> Plans { get; }

    /// <summary>True if the given tier key exists in the catalog (case-insensitive).</summary>
    bool IsValidTier(string tier);

    /// <summary>The plan for a tier key, or null when it isn't in the catalog.</summary>
    PlanInfo? GetPlan(string tier);
}
