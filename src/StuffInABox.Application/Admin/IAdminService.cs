namespace StuffInABox.Application.Admin;

/// <summary>Operations the admin application performs against accounts and subscriptions.
/// Implemented in Infrastructure; consumed only by the separate admin host.</summary>
public interface IAdminService
{
    /// <summary>Lists users (most recent first), optionally filtered by an email substring.</summary>
    Task<IReadOnlyList<AdminUserRow>> ListUsersAsync(string? query, CancellationToken ct = default);

    /// <summary>Sets a user's subscription tier. Returns false if the user doesn't exist;
    /// throws if the tier isn't in the plan catalog.</summary>
    Task<bool> SetPlanTierAsync(Guid userId, string tier, CancellationToken ct = default);

    /// <summary>Enables or disables a user account. Returns false if the user doesn't exist.</summary>
    Task<bool> SetDisabledAsync(Guid userId, bool disabled, CancellationToken ct = default);

    /// <summary>Permanently deletes a user and all their data (not reversible, unlike disable).
    /// Returns false if the user doesn't exist.</summary>
    Task<bool> DeleteUserAsync(Guid userId, CancellationToken ct = default);
}
