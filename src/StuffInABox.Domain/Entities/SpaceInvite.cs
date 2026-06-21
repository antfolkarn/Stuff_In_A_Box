using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Domain.Entities;

/// <summary>
/// A shareable invite link for a space. The owner generates a token, shares it
/// out-of-band (chat, etc.), and any signed-in user who opens it joins the space.
/// This matches the app's privacy model — no email handling required.
/// </summary>
public class SpaceInvite
{
    public Guid Id { get; private set; }
    public Guid SpaceId { get; private set; }
    public string Token { get; private set; }
    public UserId CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    public bool IsActive => RevokedAt is null;

    private SpaceInvite()
    {
        Token = null!;
        CreatedBy = null!;
    }

    public static SpaceInvite Create(Guid spaceId, string token, UserId createdBy)
    {
        if (spaceId == Guid.Empty)
            throw new ArgumentException("SpaceId cannot be empty.", nameof(spaceId));
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token cannot be empty.", nameof(token));

        return new SpaceInvite
        {
            Id = Guid.NewGuid(),
            SpaceId = spaceId,
            Token = token,
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Revoke()
    {
        RevokedAt ??= DateTimeOffset.UtcNow;
    }
}
