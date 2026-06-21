using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Domain.Entities;

/// <summary>
/// Grants a user access to a space they don't own. The space owner stays the
/// owner of all content; members are collaborators who can view/edit boxes and
/// items but cannot manage the space itself.
/// </summary>
public class SpaceMembership
{
    public Guid Id { get; private set; }
    public Guid SpaceId { get; private set; }
    public UserId UserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private SpaceMembership()
    {
        UserId = null!;
    }

    public static SpaceMembership Create(Guid spaceId, UserId userId)
    {
        if (spaceId == Guid.Empty)
            throw new ArgumentException("SpaceId cannot be empty.", nameof(spaceId));

        return new SpaceMembership
        {
            Id = Guid.NewGuid(),
            SpaceId = spaceId,
            UserId = userId,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
