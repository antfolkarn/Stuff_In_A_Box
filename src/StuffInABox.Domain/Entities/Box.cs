using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Domain.Entities;

public class Box
{
    public BoxNumber Number { get; private set; }
    public Guid SpaceId { get; private set; }
    public UserId OwnerId { get; private set; }
    public string Label { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Box()
    {
        Number = null!;
        OwnerId = null!;
        Label = null!;
    }

    public static Box Create(BoxNumber number, Guid spaceId, UserId ownerId, string label)
    {
        if (string.IsNullOrWhiteSpace(label))
            throw new ArgumentException("Box label cannot be empty.", nameof(label));

        return new Box
        {
            Number = number,
            SpaceId = spaceId,
            OwnerId = ownerId,
            Label = label.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void MoveTo(Guid newSpaceId)
    {
        if (newSpaceId == Guid.Empty)
            throw new ArgumentException("SpaceId cannot be empty.", nameof(newSpaceId));
        SpaceId = newSpaceId;
    }

    public void UpdateLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
            throw new ArgumentException("Box label cannot be empty.", nameof(label));
        Label = label.Trim();
    }
}
