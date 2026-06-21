namespace StuffInABox.Application.Settings;

public sealed record SettingsDto(string Theme, string Design);

/// <summary>Allowed values, shared by validation and the default factory.</summary>
public static class SettingsOptions
{
    public static readonly string[] Themes = ["light", "dark", "system"];
    public static readonly string[] Designs = ["standard", "atelier", "pop"];

    public const string DefaultTheme = "system";
    public const string DefaultDesign = "standard";
}
