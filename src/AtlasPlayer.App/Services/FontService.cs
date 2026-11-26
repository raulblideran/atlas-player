using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;

namespace AtlasPlayer.App.Services;

/// <summary>
/// The interface's font, live. Fluent's windows and controls take their font from
/// <see cref="ResourceKey"/> (Inter, as Atlas Player sets it up); a font chosen in Settings goes in
/// the application's own resources under that key, which come first, so every open window, bar
/// and menu changes at once, and "Default" takes it out again. Titles follow it too in themes
/// whose title type is plain Inter; a theme with a display font of its own (GTA 6's Anton,
/// Synthwave's Audiowide…) keeps it.
/// </summary>
public static class FontService
{
    /// <summary>Fluent's font for its windows and controls. Not FontFamily.Default: that's the system's font.</summary>
    public const string ResourceKey = "ContentControlThemeFontFamily";
    private const string HeadingKey = "ThemeHeadingFont";

    private static bool _followsThemes;

    /// <summary>The font chosen: an installed family's name, or "" for the default (Inter).</summary>
    public static string Current { get; private set; } = "";

    public static void Apply(string? name)
    {
        Current = name?.Trim() ?? "";
        if (Application.Current is not { } app) return;
        if (!Dispatcher.UIThread.CheckAccess())
        {
            var chosen = Current;
            Dispatcher.UIThread.Post(() => Apply(chosen)); // the UI's to change, on its own thread
            return;
        }
        if (!_followsThemes)
        {
            // A theme switch puts its own title font back: decide again whether ours replaces it.
            ThemeService.Changed += _ => Apply(Current);
            _followsThemes = true;
        }
        if (Current.Length == 0)
        {
            app.Resources.Remove(ResourceKey);
            app.Resources.Remove(HeadingKey);
            return;
        }
        var family = new FontFamily(Current);
        app.Resources[ResourceKey] = family;
        if (HasPlainTitles(ThemeService.Current)) app.Resources[HeadingKey] = family;
        else app.Resources.Remove(HeadingKey);
    }

    /// <summary>The theme's titles are in Inter, the body's font, rather than a display face of its own.</summary>
    internal static bool HasPlainTitles(ThemeInfo theme) =>
        ThemeService.Load(theme).TryGetValue(HeadingKey, out var v) && v is FontFamily f && IsInter(f);

    private static bool IsInter(FontFamily f) =>
        f.Name == "Inter" || f.ToString().EndsWith("#Inter", StringComparison.Ordinal);
}
