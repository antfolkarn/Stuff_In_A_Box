namespace StuffInABox.Domain.Entities;

/// <summary>
/// Per-user preferences that follow the account across devices (stored in the DB,
/// not just localStorage). Keyed by the internal UserId.
/// </summary>
public class UserSettings
{
    public const int MaxDisplayNameLength = 40;
    public const int MaxPlanTierLength = 40;
    public const string DefaultPlanTier = "free";

    public Guid UserId { get; private set; }
    public string Theme { get; private set; }   // "light" | "dark" | "system"
    public string Design { get; private set; }  // "standard" | "atelier" | "pop"
    /// <summary>Optional nickname the user picks. When set it's shown to other members
    /// instead of their email; null means "no nickname" (callers fall back to email).</summary>
    public string? DisplayName { get; private set; }
    /// <summary>Subscription tier the account is on (e.g. "free" | "medium" | "large").
    /// Validated against the plan catalog by the admin application before it's set.</summary>
    public string PlanTier { get; private set; } = DefaultPlanTier;
    public DateTimeOffset UpdatedAt { get; private set; }

    private UserSettings()
    {
        Theme = null!;
        Design = null!;
    }

    public static UserSettings CreateDefault(Guid userId) => new()
    {
        UserId = userId,
        Theme = "system",
        Design = "standard",
        PlanTier = DefaultPlanTier,
        UpdatedAt = DateTimeOffset.UtcNow,
    };

    /// <summary>Sets the subscription tier. The caller (admin app) is responsible for
    /// validating the value against the plan catalog first.</summary>
    public void SetPlanTier(string tier)
    {
        if (string.IsNullOrWhiteSpace(tier)) throw new ArgumentException("Plan krävs.", nameof(tier));
        PlanTier = tier.Trim().ToLowerInvariant();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Update(string theme, string design, string? displayName)
    {
        if (string.IsNullOrWhiteSpace(theme)) throw new ArgumentException("Theme krävs.", nameof(theme));
        if (string.IsNullOrWhiteSpace(design)) throw new ArgumentException("Design krävs.", nameof(design));
        Theme = theme;
        Design = design;
        // Blank means "clear it"; trim so an all-spaces nickname doesn't masquerade as set.
        var trimmed = displayName?.Trim();
        DisplayName = string.IsNullOrEmpty(trimmed) ? null : trimmed;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
