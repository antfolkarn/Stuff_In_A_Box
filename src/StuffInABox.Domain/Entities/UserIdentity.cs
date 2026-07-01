using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Domain.Entities;

public class UserIdentity
{
    public Guid InternalUserId { get; private set; }
    public string Provider { get; private set; }
    public string ExternalId { get; private set; }
    public string? PasswordHash { get; private set; }
    /// <summary>
    /// Plaintext email, stored so we can contact the user (e.g. password resets).
    /// Null for OAuth identities (the provider returns no email by default). Lookups
    /// still go via the hashed email in <see cref="ExternalId"/>; this is for sending.
    /// </summary>
    public string? Email { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// When the email address was verified (null = not yet). Only meaningful for the
    /// "email" provider — OAuth identities are always treated as verified since the
    /// provider has already verified the address. See <see cref="IsEmailVerified"/>.
    /// </summary>
    public DateTimeOffset? EmailVerifiedAt { get; private set; }

    /// <summary>True for OAuth identities, or for email identities that have verified.</summary>
    public bool IsEmailVerified => Provider != "email" || EmailVerifiedAt is not null;

    /// <summary>When the account was disabled (null = active). A disabled account cannot
    /// log in or refresh its session; its data is left untouched.</summary>
    public DateTimeOffset? DisabledAt { get; private set; }

    /// <summary>True while <see cref="DisabledAt"/> is set.</summary>
    public bool IsDisabled => DisabledAt is not null;

    private UserIdentity()
    {
        Provider = null!;
        ExternalId = null!;
    }

    public static UserIdentity CreateOAuth(string provider, string externalId, string? email = null)
    {
        ValidateProvider(provider);
        if (string.IsNullOrWhiteSpace(externalId))
            throw new ArgumentException("External ID cannot be empty.", nameof(externalId));

        var now = DateTimeOffset.UtcNow;
        return new UserIdentity
        {
            InternalUserId = Guid.NewGuid(),
            Provider = provider.ToLowerInvariant(),
            ExternalId = externalId,
            // The provider returns the email; store it so admins can see who this is.
            // Null when the provider didn't supply one (e.g. Apple).
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            CreatedAt = now,
            EmailVerifiedAt = now // the OAuth provider has already verified the address
        };
    }

    public static UserIdentity CreateEmail(string hashedEmail, string passwordHash, string email)
    {
        if (string.IsNullOrWhiteSpace(hashedEmail))
            throw new ArgumentException("Hashed email cannot be empty.", nameof(hashedEmail));
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash cannot be empty.", nameof(passwordHash));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty.", nameof(email));

        return new UserIdentity
        {
            InternalUserId = Guid.NewGuid(),
            Provider = "email",
            ExternalId = hashedEmail,
            PasswordHash = passwordHash,
            Email = email.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void UpdatePasswordHash(string newHash)
    {
        if (Provider != "email")
            throw new InvalidOperationException("Password can only be set for email provider.");
        PasswordHash = newHash;
    }

    /// <summary>Backfills the email from the OAuth provider for an identity that doesn't
    /// have one yet (older OAuth accounts created before we requested the email scope).
    /// Never overwrites an existing address, and ignores an empty incoming value.</summary>
    public void SetEmailFromProvider(string? email)
    {
        if (!string.IsNullOrWhiteSpace(Email)) return;
        if (string.IsNullOrWhiteSpace(email)) return;
        Email = email.Trim();
    }

    /// <summary>Marks the email address as verified (idempotent).</summary>
    public void MarkEmailVerified() => EmailVerifiedAt ??= DateTimeOffset.UtcNow;

    /// <summary>Disables the account (idempotent). Logging in and refreshing are blocked.</summary>
    public void Disable() => DisabledAt ??= DateTimeOffset.UtcNow;

    /// <summary>Re-enables a previously disabled account (idempotent).</summary>
    public void Enable() => DisabledAt = null;

    private static void ValidateProvider(string provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException("Provider cannot be empty.", nameof(provider));
    }

    public UserId GetUserId() => new(InternalUserId);
}
