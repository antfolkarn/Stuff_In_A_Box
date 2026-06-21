namespace StuffInABox.Domain.Entities;

/// <summary>
/// Per-user preferences that follow the account across devices (stored in the DB,
/// not just localStorage). Keyed by the internal UserId.
/// </summary>
public class UserSettings
{
    public Guid UserId { get; private set; }
    public string Theme { get; private set; }   // "light" | "dark" | "system"
    public string Design { get; private set; }  // "standard" | "atelier" | "pop"
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
        UpdatedAt = DateTimeOffset.UtcNow,
    };

    public void Update(string theme, string design)
    {
        if (string.IsNullOrWhiteSpace(theme)) throw new ArgumentException("Theme krävs.", nameof(theme));
        if (string.IsNullOrWhiteSpace(design)) throw new ArgumentException("Design krävs.", nameof(design));
        Theme = theme;
        Design = design;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
