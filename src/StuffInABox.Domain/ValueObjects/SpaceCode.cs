namespace StuffInABox.Domain.ValueObjects;

public sealed record SpaceCode
{
    public string Value { get; }

    public SpaceCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 3)
            throw new ArgumentException("Space code must be exactly 3 characters.", nameof(value));
        Value = value.ToUpperInvariant();
    }

    public static SpaceCode FromName(string name)
    {
        var normalized = name
            .Replace('å', 'a').Replace('Å', 'A')
            .Replace('ä', 'a').Replace('Ä', 'A')
            .Replace('ö', 'o').Replace('Ö', 'O');
        var letters = normalized.Where(char.IsAsciiLetter).ToArray();
        var code = letters.Length >= 3
            ? new string(letters[..3])
            : new string(letters).PadRight(3, 'X');
        return new SpaceCode(code);
    }

    public override string ToString() => Value;
}
