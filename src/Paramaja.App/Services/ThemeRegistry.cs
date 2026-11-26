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
        new("graphite-red", "Graphite Red", "Graphite's neutral greys with a bold red.", ThemeVariant.Dark, "GraphiteRed"),
        new("deep-sea", "Deep Sea", "Ink-blue depths with a teal light.", ThemeVariant.Dark, "DeepSea"),
        new("amber", "Amber", "Warm charcoal and lamplight amber.", ThemeVariant.Dark, "Amber"),
        new("forest", "Forest", "Deep green shade with moss-light accents.", ThemeVariant.Dark, "Forest"),
        new("apple-music", "Apple Music", "Near-black glass and pink-red, soft round corners.", ThemeVariant.Dark, "AppleMusic"),
        new("spotify", "Spotify", "Black and grey with a bright green, pill-shaped buttons.", ThemeVariant.Dark, "Spotify"),
        new("tidal", "Tidal", "Pure black and white with a cyan light, sharp edges.", ThemeVariant.Dark, "Tidal"),
        new("mpc-hc", "MPC-HC", "Media Player Classic's grey Windows toolbars, plain glyphs and Windows blue, square corners. Brings its bar layout along.", ThemeVariant.Light, "MpcHc"),
        new("windows-media-player", "Windows Media Player", "Glossy black and glowing blue.", ThemeVariant.Dark, "WindowsMediaPlayer"),
        new("call-of-duty", "Call of Duty", "Military olive and HUD yellow, square edges, stencil titles.", ThemeVariant.Dark, "CallOfDuty", Exotic: true),
        new("gta-6", "GTA 6", "Sunset pink and orange with neon teal, poster titles.", ThemeVariant.Dark, "Gta6", Exotic: true),
        new("red-dead-redemption-2", "Red Dead Redemption 2", "Saddle leather, bone and blood red, western titles.", ThemeVariant.Dark, "RedDeadRedemption2", Exotic: true),
        new("synthwave", "80s Synthwave", "Neon magenta and cyan over a midnight grid.", ThemeVariant.Dark, "Synthwave", Exotic: true),
        new("halloween", "Halloween", "Pumpkin orange and slime green on a purple night.", ThemeVariant.Dark, "Halloween", Exotic: true),
        new("vice-city-sunset", "Vice City Sunset", "Electric blue through neon violet and vice pink to golden hour, in gradients.", ThemeVariant.Dark, "ViceCitySunset", Exotic: true),
        new("golden-hour-escape", "Golden Hour Escape", "Midnight blue under a coral and peach sky.", ThemeVariant.Dark, "GoldenHourEscape", Exotic: true),
        new("neon-nights", "Neon Nights", "Criminal night and indigo lit by cyan, electric purple and pink.", ThemeVariant.Dark, "NeonNights", Exotic: true),
        new("tropical-vice", "Tropical Vice", "Lagoon and fresh mint under mango, sunburn and peachy pink.", ThemeVariant.Dark, "TropicalVice", Exotic: true),
        new("miami-marina", "Miami Marina", "Sunlit sand, sky blue and marina teal, on light grounds.", ThemeVariant.Light, "MiamiMarina", Exotic: true),
        new("vice-city-by-night", "Vice City by Night", "Deep night and nightfall blue under hot pink and lilac haze.", ThemeVariant.Dark, "ViceCityByNight", Exotic: true),
        new("midnight-pursuit", "Midnight Pursuit", "Street blue at night with police blue and siren red lights.", ThemeVariant.Dark, "MidnightPursuit", Exotic: true),
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
        "ThemeBarText", "ThemeBarScrimTop", "ThemeBarScrimBottom", "ThemeBarEdge", "ThemeBarButtonHover", "ThemeBarButtonPressed",
        "ThemePlay", "ThemePlayHover", "ThemeOnPlay",
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
