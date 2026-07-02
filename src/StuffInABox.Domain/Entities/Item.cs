using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Domain.Entities;

/// <summary>
/// Whether background photo recognition (name + tags) is still pending for an item.
/// Manually created items are <see cref="Completed"/> from the start; items created
/// from a photo start <see cref="Pending"/> until the recognition worker has run.
/// </summary>
public enum ItemEnrichmentStatus
{
    /// <summary>Photo recognition is queued/running.</summary>
    Pending = 0,
    /// <summary>Recognition ran and produced a result (or the item was created manually).</summary>
    Completed = 1,
    /// <summary>A photo item that hasn't been AI-analyzed — the monthly quota was spent, or
    /// recognition produced nothing. Eligible for the on-demand "run AI" action.</summary>
    Skipped = 2,
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
    /// <summary>Stored bytes of the current photo (0 when there's none). Summed per owner for
    /// the storage quota.</summary>
    public long PhotoSizeBytes { get; private set; }
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

    /// <summary>Marks a photo item as not AI-analyzed (quota spent, or recognition produced
    /// nothing). The UI stops the spinner and offers the on-demand "run AI" action.</summary>
    public void MarkAiSkipped() => EnrichmentStatus = ItemEnrichmentStatus.Skipped;

    /// <summary>Re-queues recognition for a photo item (e.g. one created without AI when the
    /// monthly quota was spent). The UI shows it as processing again until the worker runs.</summary>
    public void MarkPendingRecognition() => EnrichmentStatus = ItemEnrichmentStatus.Pending;

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Item name cannot be empty.", nameof(name));
        Name = name.Trim();
    }

    public void SetPhoto(string storageKey, long sizeBytes)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            throw new ArgumentException("Storage key cannot be empty.", nameof(storageKey));
        PhotoStorageKey = storageKey;
        PhotoSizeBytes = sizeBytes < 0 ? 0 : sizeBytes;
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
