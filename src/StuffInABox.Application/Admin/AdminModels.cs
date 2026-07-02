namespace StuffInABox.Application.Admin;

/// <summary>A person as shown in the admin user list — one row per account (all linked login
/// methods grouped), joined with its settings (plan) and a little usage context. Read model.</summary>
public sealed record AdminUserRow(
    Guid UserId,
    string? Email,
    IReadOnlyList<string> Providers,
    bool EmailVerified,
    string PlanTier,
    bool IsDisabled,
    DateTimeOffset CreatedAt,
    int SpaceCount,
    int ItemCount);
