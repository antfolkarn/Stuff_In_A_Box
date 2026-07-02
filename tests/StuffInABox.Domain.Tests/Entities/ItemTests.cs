using StuffInABox.Domain.Entities;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Domain.Tests.Entities;

public class ItemTests
{
    private static readonly UserId OwnerId = new(Guid.NewGuid());
    private static readonly BoxNumber BoxNum = new(1);

    [Fact]
    public void Create_WithValidArgs_HasEmptyTags()
    {
        var item = Item.Create(BoxNum, OwnerId, "Hammare");

        Assert.Equal("Hammare", item.Name);
        Assert.Equal(BoxNum, item.BoxNumber);
        Assert.Empty(item.Tags);
        Assert.Null(item.PhotoStorageKey);
    }

    [Fact]
    public void ReplaceTags_SetsTags_Lowercased()
    {
        var item = Item.Create(BoxNum, OwnerId, "Hammare");
        item.ReplaceTags(["Verktyg", "HAMMARE", "metall"]);

        Assert.Equal(["verktyg", "hammare", "metall"], item.Tags);
    }

    [Fact]
    public void MergeTags_AddsNewTagsOnly()
    {
        var item = Item.Create(BoxNum, OwnerId, "Hammare");
        item.ReplaceTags(["verktyg"]);
        item.MergeTags(["verktyg", "hantverk", "metall"]);

        Assert.Equal(3, item.Tags.Count);
        Assert.Contains("verktyg", item.Tags);
        Assert.Contains("hantverk", item.Tags);
        Assert.Contains("metall", item.Tags);
    }

    [Fact]
    public void SetPhoto_SetsStorageKey()
    {
        var item = Item.Create(BoxNum, OwnerId, "Hammare");
        item.SetPhoto("items/user1/abc123.jpg", 4096);

        Assert.Equal("items/user1/abc123.jpg", item.PhotoStorageKey);
        Assert.Equal(4096, item.PhotoSizeBytes);
    }

    [Fact]
    public void Create_IsCompleted_NotPending()
    {
        var item = Item.Create(BoxNum, OwnerId, "Hammare");
        Assert.Equal(ItemEnrichmentStatus.Completed, item.EnrichmentStatus);
    }

    [Fact]
    public void CreateFromPhoto_StartsPending_WithPlaceholderName()
    {
        var item = Item.CreateFromPhoto(BoxNum, OwnerId);

        Assert.Equal(Item.PhotoPlaceholderName, item.Name);
        Assert.Equal(ItemEnrichmentStatus.Pending, item.EnrichmentStatus);
        Assert.Empty(item.Tags);
    }

    [Fact]
    public void ApplyRecognition_SetsNameAndTags_AndCompletes()
    {
        var item = Item.CreateFromPhoto(BoxNum, OwnerId);

        item.ApplyRecognition("Röd jacka", ["jacka", "röd"]);

        Assert.Equal("Röd jacka", item.Name);
        Assert.Equal(["jacka", "röd"], item.Tags);
        Assert.Equal(ItemEnrichmentStatus.Completed, item.EnrichmentStatus);
    }

    [Fact]
    public void ApplyRecognition_KeepsUserRename_WhenNoLongerPlaceholder()
    {
        var item = Item.CreateFromPhoto(BoxNum, OwnerId);
        item.Rename("Min egen titel");

        item.ApplyRecognition("Igenkänt namn", ["tagg"]);

        // The user's name wins; tags are still merged and the item completes.
        Assert.Equal("Min egen titel", item.Name);
        Assert.Contains("tagg", item.Tags);
        Assert.Equal(ItemEnrichmentStatus.Completed, item.EnrichmentStatus);
    }

    [Fact]
    public void MarkEnriched_CompletesWithoutChangingName()
    {
        var item = Item.CreateFromPhoto(BoxNum, OwnerId);

        item.MarkEnriched();

        Assert.Equal(Item.PhotoPlaceholderName, item.Name);
        Assert.Equal(ItemEnrichmentStatus.Completed, item.EnrichmentStatus);
    }
}
