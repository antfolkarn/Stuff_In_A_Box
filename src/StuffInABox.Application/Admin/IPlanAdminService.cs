namespace StuffInABox.Application.Admin;

/// <summary>Values for creating or updating a plan. <c>-1</c> numeric limits = unlimited.</summary>
public sealed record PlanInput(
    string Tier,
    int PriceSek,
    int MaxSpaces,
    int MaxItems,
    int MaxMembers,
    int AiPhotosPerMonth,
    long StorageMb,
    bool ClaudeEnrichment,
    bool PriorityQueue,
    bool AllThemes,
    int SortOrder);

/// <summary>Admin CRUD over the plan catalog (the DB-backed tiers). Reloads the shared
/// <see cref="IPlanCatalog"/> cache after every write. Implemented in Infrastructure.</summary>
public interface IPlanAdminService
{
    Task<IReadOnlyList<PlanInfo>> ListAsync(CancellationToken ct = default);

    /// <summary>Creates the tier if new, otherwise updates it in place.</summary>
    Task UpsertAsync(PlanInput input, CancellationToken ct = default);

    /// <summary>Removes a tier. Returns false if it doesn't exist; throws
    /// <see cref="InvalidOperationException"/> if any account is currently on it.</summary>
    Task<bool> DeleteAsync(string tier, CancellationToken ct = default);
}
