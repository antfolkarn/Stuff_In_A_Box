using StuffInABox.Domain.Entities;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Domain.Tests.Entities;

public class BoxTests
{
    private static readonly UserId OwnerId = new(Guid.NewGuid());
    private static readonly BoxNumber Num = new(3);
    private static readonly Guid SpaceId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidArgs_SetsProperties()
    {
        var box = Box.Create(Num, SpaceId, OwnerId, "Verktyg");

        Assert.Equal(Num, box.Number);
        Assert.Equal(SpaceId, box.SpaceId);
        Assert.Equal(OwnerId, box.OwnerId);
        Assert.Equal("Verktyg", box.Label);
    }

    [Fact]
    public void MoveTo_UpdatesSpaceId_NumberUnchanged()
    {
        var box = Box.Create(Num, SpaceId, OwnerId, "Verktyg");
        var newSpace = Guid.NewGuid();
        box.MoveTo(newSpace);

        Assert.Equal(newSpace, box.SpaceId);
        Assert.Equal(Num, box.Number);
    }

    [Fact]
    public void MoveTo_WithEmptyGuid_Throws()
    {
        var box = Box.Create(Num, SpaceId, OwnerId, "Verktyg");
        Assert.Throws<ArgumentException>(() => box.MoveTo(Guid.Empty));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyLabel_Throws(string label)
    {
        Assert.Throws<ArgumentException>(() => Box.Create(Num, SpaceId, OwnerId, label));
    }
}
