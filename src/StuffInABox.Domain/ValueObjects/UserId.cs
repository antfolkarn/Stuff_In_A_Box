namespace StuffInABox.Domain.ValueObjects;

public sealed record UserId
{
    public Guid Value { get; }

    public UserId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(value));
        Value = value;
    }

    public static UserId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
