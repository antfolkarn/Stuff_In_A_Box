namespace StuffInABox.Domain.Entities;

/// <summary>
/// A single-use password-reset token, stored only as a SHA-256 hash (the raw token
/// lives in the emailed link). Short-lived and consumed on use.
/// </summary>
public class PasswordResetToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }

    private PasswordResetToken()
    {
        TokenHash = null!;
    }

    public static PasswordResetToken Issue(Guid userId, string tokenHash, TimeSpan lifetime)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new ArgumentException("Token hash cannot be empty.", nameof(tokenHash));

        var now = DateTimeOffset.UtcNow;
        return new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            CreatedAt = now,
            ExpiresAt = now.Add(lifetime),
        };
    }

    public bool IsActive(DateTimeOffset now) => UsedAt is null && ExpiresAt > now;

    public void Use(DateTimeOffset now) => UsedAt ??= now;
}
