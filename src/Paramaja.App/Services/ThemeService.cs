using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;

namespace Paramaja.App.Services;

/// <summary>
/// Puts a theme on the whole app, live: its dictionary goes into the application's resources
/// (every view reaches theme keys through DynamicResource, so open windows follow at once), the
/// light or dark variant is requested, and Fluent's palette takes the theme's colours so its stock
/// controls match.
/// </summary>
public static class ThemeService
{
    public const string PalettePrefix = "ThemePalette";

    private static readonly Dictionary<string, ResourceDictionary> Loaded = [];
    private static ResourceDictionary? _applied;
    /// <summary>The application the dictionaries belong to: a resource dictionary has one owner.</summary>
    private static Application? _owner;

    /// <summary>The theme the app wears now.</summary>
    public static ThemeInfo Current { get; private set; } = ThemeRegistry.Default;

    public static event Action<ThemeInfo>? Changed;

    /// <summary>A theme's dictionary, loaded once (for the app; one only to read is loaded afresh).</summary>
    public static ResourceDictionary Load(ThemeInfo theme)
    {
        if (!ReferenceEquals(Application.Current, _owner)) return (ResourceDictionary)AvaloniaXamlLoader.Load(theme.Source);
        if (!Loaded.TryGetValue(theme.Id, out var dictionary))
            Loaded[theme.Id] = dictionary = (ResourceDictionary)AvaloniaXamlLoader.Load(theme.Source);
        return dictionary;
    }

    /// <summary>Wears the theme with this id (the default one for an id it doesn't know).</summary>
    public static ThemeInfo Apply(string? id)
    {
        var theme = ThemeRegistry.Find(id);
        if (Application.Current is not { } app) return theme;
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => Apply(id)); // the UI's to change, on its own thread
            return theme;
        }
        if (!ReferenceEquals(app, _owner))
        {
            Loaded.Clear();
            _applied = null;
            _owner = app;
        }
        var dictionary = Load(theme);
        if (!ReferenceEquals(dictionary, _applied))
        {
            var merged = app.Resources.MergedDictionaries;
            if (_applied is not null) merged.Remove(_applied);
            merged.Insert(0, dictionary);
            _applied = dictionary;
        }
        SetPalette(app, theme, dictionary);
        app.RequestedThemeVariant = theme.Variant;
        var changed = Current != theme;
        Current = theme;
        if (changed) Changed?.Invoke(theme);
        return theme;
    }

    private static void SetPalette(Application app, ThemeInfo theme, ResourceDictionary dictionary)
    {
        if (app.Styles.OfType<FluentTheme>().FirstOrDefault() is not { } fluent) return;
        if (!fluent.Palettes.TryGetValue(theme.Variant, out var palette))
            fluent.Palettes[theme.Variant] = palette = new ColorPaletteResources();
        Color Get(string key) => dictionary.TryGetValue(PalettePrefix + key, out var v) && v is Color c ? c : default;
        palette.Accent = Get("Accent");
        palette.RegionColor = Get("RegionColor");
        palette.AltHigh = Get("AltHigh");
        palette.ChromeLow = Get("ChromeLow");
        palette.ChromeMedium = Get("ChromeMedium");
        palette.ChromeMediumLow = Get("ChromeMediumLow");
        palette.BaseHigh = Get("BaseHigh");
    }

    /// <summary>A theme key's value as the app has it now, or <paramref name="fallback"/>.</summary>
    public static T Resource<T>(string key, T fallback) =>
        Application.Current?.TryGetResource(key, Application.Current.ActualThemeVariant, out var v) == true && v is T t ? t : fallback;
}
