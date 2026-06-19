using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Domain.Entities;

public class Item
{
    public Guid Id { get; private set; }
    public BoxNumber BoxNumber { get; private set; }
    public UserId OwnerId { get; private set; }
    public string Name { get; private set; }
    public string? PhotoStorageKey { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private readonly List<string> _tags = [];
    public IReadOnlyList<string> Tags => _tags.AsReadOnly();

    private Item()
    {
        BoxNumber = null!;
        OwnerId = null!;
        Name = null!;
    }

    public static Item Create(BoxNumber boxNumber, UserId ownerId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Item name cannot be empty.", nameof(name));

        return new Item
        {
            Id = Guid.NewGuid(),
            BoxNumber = boxNumber,
            OwnerId = ownerId,
            Name = name.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Item name cannot be empty.", nameof(name));
        Name = name.Trim();
    }

    public void SetPhoto(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            throw new ArgumentException("Storage key cannot be empty.", nameof(storageKey));
        PhotoStorageKey = storageKey;
    }

    public void ReplaceTags(IEnumerable<string> tags)
    {
        _tags.Clear();
        _tags.AddRange(tags.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim().ToLowerInvariant()));
    }

    public void MergeTags(IEnumerable<string> additional)
    {
        var newTags = additional
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim().ToLowerInvariant())
            .Where(t => !_tags.Contains(t));
        _tags.AddRange(newTags);
    }
}
