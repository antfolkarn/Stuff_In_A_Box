using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Domain.Tests.ValueObjects;

public class BoxNumberTests
{
    [Fact]
    public void Constructor_WithPositiveValue_SetsValue()
    {
        var bn = new BoxNumber(5);
        Assert.Equal(5, bn.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_WithNonPositiveValue_Throws(int value)
    {
        Assert.Throws<ArgumentException>(() => new BoxNumber(value));
    }

    [Fact]
    public void ToString_ReturnsHashPrefixed()
    {
        var bn = new BoxNumber(42);
        Assert.Equal("#42", bn.ToString());
    }

    [Fact]
    public void EqualityByValue()
    {
        var a = new BoxNumber(7);
        var b = new BoxNumber(7);
        Assert.Equal(a, b);
        Assert.True(a == b);
    }
}
