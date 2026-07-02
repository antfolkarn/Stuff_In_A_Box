namespace StuffInABox.Application.Settings;

public sealed record SettingsDto(string Theme, string Design, string? DisplayName);

/// <summary>Allowed values, shared by validation and the default factory.</summary>
public static class SettingsOptions
{
    public static readonly string[] Themes = ["light", "dark", "system"];
    public static readonly string[] Designs = ["standard", "atelier", "pop", "nord", "console", "ledger"];

    /// <summary>Designs available on every plan. The rest are gated behind the plan's
    /// <c>AllThemes</c> flag (enforced in <see cref="Commands.UpdateSettingsCommandHandler"/>).</summary>
    public static readonly string[] FreeDesigns = ["standard", "pop"];

    public const string DefaultTheme = "system";
    public const string DefaultDesign = "standard";
}
