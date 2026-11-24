using Avalonia.Styling;
using Paramaja.Core.Settings;

namespace Paramaja.App.Services;

/// <summary>
/// One theme: <paramref name="File"/> names its dictionary in Themes/ (Themes/<c>File</c>.axaml),
/// which defines every key <see cref="ThemeRegistry.Keys"/> lists.
/// </summary>
public sealed record ThemeInfo(string Id, string Name, string Description, ThemeVariant Variant, string File, bool Exotic = false)
{
    public Uri Source => new($"avares://Paramaja/Themes/{File}.axaml");
}

/// <summary>
/// The themes, in the order Settings → Appearance shows them. Adding one is a new file in Themes/
/// (copied from another, so it has every key) and a line here.
/// </summary>
public static class ThemeRegistry
{
    public static IReadOnlyList<ThemeInfo> All { get; } =
    [
        new("paramaja", "Paramaja", "Paramaja's own plum and orchid, with leaf green for progress.", ThemeVariant.Dark, "Paramaja"),
        new("paramaja-light", "Paramaja Light", "Paramaja's colours on light grounds, for bright rooms.", ThemeVariant.Light, "ParamajaLight"),
        new("graphite", "Graphite", "Neutral greys with a clear blue.", ThemeVariant.Dark, "Graphite"),
        new("deep-sea", "Deep Sea", "Ink-blue depths with a teal light.", ThemeVariant.Dark, "DeepSea"),
        new("amber", "Amber", "Warm charcoal and lamplight amber.", ThemeVariant.Dark, "Amber"),
        new("forest", "Forest", "Deep green shade with moss-light accents.", ThemeVariant.Dark, "Forest"),
        new("call-of-duty", "Call of Duty", "Military olive and HUD yellow, square edges, stencil titles.", ThemeVariant.Dark, "CallOfDuty", Exotic: true),
        new("gta-6", "GTA 6", "Sunset pink and orange with neon teal, poster titles.", ThemeVariant.Dark, "Gta6", Exotic: true),
        new("red-dead-redemption-2", "Red Dead Redemption 2", "Saddle leather, bone and blood red, western titles.", ThemeVariant.Dark, "RedDeadRedemption2", Exotic: true),
        new("synthwave", "80s Synthwave", "Neon magenta and cyan over a midnight grid.", ThemeVariant.Dark, "Synthwave", Exotic: true),
        new("halloween", "Halloween", "Pumpkin orange and slime green on a purple night.", ThemeVariant.Dark, "Halloween", Exotic: true),
    ];

    public static ThemeInfo Default => All[0];

    /// <summary>The theme with this id; the default for one it doesn't know (a newer version's, or a typo).</summary>
    public static ThemeInfo Find(string? id) =>
        All.FirstOrDefault(t => string.Equals(t.Id, id?.Trim(), StringComparison.OrdinalIgnoreCase)) ?? Default;

    /// <summary>Fluent's palette colours a theme sets (ThemePalette&lt;name&gt;), for its stock controls.</summary>
    public static IReadOnlyList<string> PaletteKeys { get; } =
        ["Accent", "RegionColor", "AltHigh", "ChromeLow", "ChromeMedium", "ChromeMediumLow", "BaseHigh"];

    /// <summary>The keys every theme defines. Views reach them only through DynamicResource.</summary>
    public static IReadOnlyList<string> Keys { get; } =
    [
        "ThemeBg", "ThemeSurface", "ThemeRaised", "ThemeHover", "ThemeStroke", "ThemeNavSelected", "ThemeFlyout",
        "ThemeText", "ThemeTextMuted",
        "ThemeAccent", "ThemeAccentHover", "ThemeOnAccent", "ThemeHighlight", "ThemePositive",
        "ThemeBarText", "ThemeBarScrimTop", "ThemeBarScrimBottom", "ThemeBarButtonHover", "ThemeBarButtonPressed",
        "ThemeBadge", "ThemeBadgeHdr", "ThemeBadgeText",
        "ThemeSeekTrack", "ThemeSeekBuffered", "ThemeSeekProgress", "ThemeSeekThumb", "ThemeSeekLabel", "ThemeSeekLabelText",
        "ThemeWelcome", "ThemeTitle",
        "ThemeRadiusSmall", "ThemeRadiusControl", "ThemeRadiusLarge", "ThemeRadiusIconButton",
        "ThemeHeadingFont", "ThemeHeadingWeight", "ThemeHeadingSizeHero", "ThemeHeadingSizePage", "ThemeHeadingSizeDialog",
        .. PaletteKeys.Select(k => ThemeService.PalettePrefix + k),
        "ToggleSwitchKnobFillOn", "ToggleSwitchKnobFillOnPointerOver", "ToggleSwitchKnobFillOnPressed",
    ];

    /// <summary>Settings' default, kept in step with the registry's (a test checks it).</summary>
    internal static string SettingsDefault => PlayerSettings.DefaultTheme;
}
