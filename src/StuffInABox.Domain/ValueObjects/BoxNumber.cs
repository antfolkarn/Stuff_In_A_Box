namespace StuffInABox.Domain.ValueObjects;

public sealed record BoxNumber
{
    public int Value { get; }

    public BoxNumber(int value)
    {
        if (value < 1)
            throw new ArgumentException("Box number must be >= 1.", nameof(value));
        Value = value;
    }

    public override string ToString() => $"#{Value}";
}
