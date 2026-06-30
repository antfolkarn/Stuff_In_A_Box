namespace StuffInABox.Application.Admin;

/// <summary>A user as shown in the admin user list — identity joined with its settings
/// (plan) and a little usage context. Read model, no behaviour.</summary>
public sealed record AdminUserRow(
    Guid UserId,
    string? Email,
    string Provider,
    bool EmailVerified,
    string PlanTier,
    bool IsDisabled,
    DateTimeOffset CreatedAt,
    int SpaceCount,
    int ItemCount);
