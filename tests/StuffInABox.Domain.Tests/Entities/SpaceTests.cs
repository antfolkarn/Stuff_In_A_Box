using StuffInABox.Domain.Entities;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Domain.Tests.Entities;

public class SpaceTests
{
    private static readonly UserId OwnerId = new(Guid.NewGuid());

    [Fact]
    public void Create_WithValidArgs_SetsProperties()
    {
        var space = Space.Create(OwnerId, "Garage", "ti-car");

        Assert.NotEqual(Guid.Empty, space.Id);
        Assert.Equal(OwnerId, space.OwnerId);
        Assert.Equal("Garage", space.Name);
        Assert.Equal("GAR", space.Code.Value);
        Assert.Equal("ti-car", space.Icon);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyName_Throws(string name)
    {
        Assert.Throws<ArgumentException>(() => Space.Create(OwnerId, name, "ti-car"));
    }

    [Fact]
    public void ChangeIcon_UpdatesIcon()
    {
        var space = Space.Create(OwnerId, "Vinden", "ti-home");
        space.ChangeIcon("ti-stairs");
        Assert.Equal("ti-stairs", space.Icon);
    }
}
