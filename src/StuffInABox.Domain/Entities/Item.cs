using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Domain.Entities;

/// <summary>
/// Whether background photo recognition (name + tags) is still pending for an item.
/// Manually created items are <see cref="Completed"/> from the start; items created
/// from a photo start <see cref="Pending"/> until the recognition worker has run.
/// </summary>
public enum ItemEnrichmentStatus
{
    Pending = 0,
    Completed = 1,
}

public class Item
{
    /// <summary>Placeholder name shown while a photo-created item awaits recognition.</summary>
    public const string PhotoPlaceholderName = "Nytt föremål";

    public Guid Id { get; private set; }
    public BoxNumber BoxNumber { get; private set; }
    public UserId OwnerId { get; private set; }
    public string Name { get; private set; }
    public string? PhotoStorageKey { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public ItemEnrichmentStatus EnrichmentStatus { get; private set; } = ItemEnrichmentStatus.Completed;

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

    /// <summary>
    /// Creates an item from an uploaded photo before recognition has run: it gets the
    /// placeholder name and <see cref="ItemEnrichmentStatus.Pending"/>, and the background
    /// recognition worker later fills in the real name and tags via <see cref="ApplyRecognition"/>.
    /// </summary>
    public static Item CreateFromPhoto(BoxNumber boxNumber, UserId ownerId) =>
        new()
        {
            Id = Guid.NewGuid(),
            BoxNumber = boxNumber,
            OwnerId = ownerId,
            Name = PhotoPlaceholderName,
            CreatedAt = DateTimeOffset.UtcNow,
            EnrichmentStatus = ItemEnrichmentStatus.Pending,
        };

    /// <summary>
    /// Applies the result of background photo recognition: sets the name (only if the user
    /// hasn't already renamed away from the placeholder), merges the detected tags, and marks
    /// enrichment complete.
    /// </summary>
    public void ApplyRecognition(string? name, IEnumerable<string> tags)
    {
        if (!string.IsNullOrWhiteSpace(name) && Name == PhotoPlaceholderName)
            Name = name.Trim();
        MergeTags(tags);
        EnrichmentStatus = ItemEnrichmentStatus.Completed;
    }

    /// <summary>Marks enrichment complete even when recognition produced nothing, so the UI stops waiting.</summary>
    public void MarkEnriched() => EnrichmentStatus = ItemEnrichmentStatus.Completed;

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
