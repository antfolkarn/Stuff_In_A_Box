namespace StuffInABox.Domain.Entities;

/// <summary>
/// A single-use email-verification token, stored only as a SHA-256 hash (the raw token
/// lives in the emailed link). Short-lived and consumed on use. Mirrors
/// <see cref="PasswordResetToken"/>.
/// </summary>
public class EmailVerificationToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }

    private EmailVerificationToken()
    {
        TokenHash = null!;
    }

    public static EmailVerificationToken Issue(Guid userId, string tokenHash, TimeSpan lifetime)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new ArgumentException("Token hash cannot be empty.", nameof(tokenHash));

        var now = DateTimeOffset.UtcNow;
        return new EmailVerificationToken
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
