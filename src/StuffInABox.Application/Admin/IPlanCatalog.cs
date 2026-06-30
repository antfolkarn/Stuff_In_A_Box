namespace StuffInABox.Application.Admin;

/// <summary>The set of subscription tiers the system knows about, plus their limits.
/// Backed by configuration today (the "Plans" section); can move to the database later
/// so the admin UI edits it live — callers depend only on this interface.</summary>
public interface IPlanCatalog
{
    /// <summary>Tier keys in display order, e.g. ["free", "medium", "large"].</summary>
    IReadOnlyList<string> Tiers { get; }

    /// <summary>True if the given tier key exists in the catalog (case-insensitive).</summary>
    bool IsValidTier(string tier);
}
