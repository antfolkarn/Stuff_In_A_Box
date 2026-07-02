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
    public string Theme { get; private set; }   // "light" | "dark"
    public string Design { get; private set; }  // "standard" | "atelier" | "pop"
    /// <summary>Optional nickname the user picks. When set it's shown to other members
    /// instead of their email; null means "no nickname" (callers fall back to email).</summary>
    public string? DisplayName { get; private set; }
    /// <summary>Subscription tier the account is on (e.g. "free" | "medium" | "large").
    /// Validated against the plan catalog by the admin application before it's set.</summary>
    public string PlanTier { get; private set; } = DefaultPlanTier;

    /// <summary>AI recognition runs consumed in <see cref="AiUsageYearMonth"/> (the monthly
    /// quota resets when the month rolls over — see <see cref="RecordAiUsage"/>).</summary>
    public int AiUsedThisMonth { get; private set; }
    /// <summary>The month <see cref="AiUsedThisMonth"/> counts, as year*100 + month (e.g. 202607).</summary>
    public int AiUsageYearMonth { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    private UserSettings()
    {
        Theme = null!;
        Design = null!;
    }

    public static UserSettings CreateDefault(Guid userId) => new()
    {
        UserId = userId,
        Theme = "light",
        Design = "standard",
        PlanTier = DefaultPlanTier,
        UpdatedAt = DateTimeOffset.UtcNow,
    };

    /// <summary>AI runs used in the given month (year*100+month); 0 once the month has rolled over.</summary>
    public int AiUsedIn(int yearMonth) => AiUsageYearMonth == yearMonth ? AiUsedThisMonth : 0;

    /// <summary>Records one AI recognition run in the given month, resetting the counter first
    /// when the month has changed since the last run.</summary>
    public void RecordAiUsage(int yearMonth)
    {
        if (AiUsageYearMonth != yearMonth)
        {
            AiUsageYearMonth = yearMonth;
            AiUsedThisMonth = 0;
        }
        AiUsedThisMonth++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

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
