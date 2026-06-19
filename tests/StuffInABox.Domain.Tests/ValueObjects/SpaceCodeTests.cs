using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Domain.Tests.ValueObjects;

public class SpaceCodeTests
{
    [Fact]
    public void Constructor_StoresUppercase()
    {
        var code = new SpaceCode("gar");
        Assert.Equal("GAR", code.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("AB")]
    [InlineData("ABCD")]
    public void Constructor_WithInvalidLength_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => new SpaceCode(value));
    }

    [Theory]
    [InlineData("Garage", "GAR")]
    [InlineData("Vinden", "VIN")]
    [InlineData("Förråd", "FOR")]
    [InlineData("AB", "ABX")]
    public void FromName_DerivesThreeLetterCode(string name, string expected)
    {
        var code = SpaceCode.FromName(name);
        Assert.Equal(expected, code.Value);
    }
}
