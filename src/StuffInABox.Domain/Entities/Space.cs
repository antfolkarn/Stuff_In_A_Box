using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Domain.Entities;

public class Space
{
    public Guid Id { get; private set; }
    public UserId OwnerId { get; private set; }
    public string Name { get; private set; }
    public SpaceCode Code { get; private set; }
    public string Icon { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Space()
    {
        Name = null!;
        Code = null!;
        Icon = null!;
        OwnerId = null!;
    }

    public static Space Create(UserId ownerId, string name, string icon)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Space name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(icon))
            throw new ArgumentException("Icon cannot be empty.", nameof(icon));

        return new Space
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Name = name.Trim(),
            Code = SpaceCode.FromName(name),
            Icon = icon,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void ChangeIcon(string newIcon)
    {
        if (string.IsNullOrWhiteSpace(newIcon))
            throw new ArgumentException("Icon cannot be empty.", nameof(newIcon));
        Icon = newIcon;
    }
}
