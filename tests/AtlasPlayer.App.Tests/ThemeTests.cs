using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using AtlasPlayer.App.Services;
using AtlasPlayer.App.Views;
using AtlasPlayer.Core.Settings;

namespace AtlasPlayer.App.Tests;

/// <summary>Themes (2.0.0): each complete, readable, switched live, and kept with the settings.</summary>
[Collection(ThemeCollection.Name)]
public class ThemeTests
{
    private static void Dispatch() => Dispatcher.UIThread.RunJobs();

    private static string Root([System.Runtime.CompilerServices.CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", ".."));

    private static Color ColorOf(ResourceDictionary d, string key) =>
        Assert.IsAssignableFrom<ISolidColorBrush>(d[key]).Color;

    [AvaloniaFact]
    public void Every_theme_defines_every_key_and_nothing_else()
    {
        Assert.Equal(ThemeRegistry.All.Count, ThemeRegistry.All.Select(t => t.Id).Distinct().Count());
        Assert.Equal(PlayerSettings.DefaultTheme, ThemeRegistry.Default.Id);
        foreach (var theme in ThemeRegistry.All)
        {
            var d = ThemeService.Load(theme);
            Assert.True(ThemeRegistry.Keys.All(d.ContainsKey), $"{theme.Id} lacks {string.Join(", ", ThemeRegistry.Keys.Where(k => !d.ContainsKey(k)))}");
            Assert.Equal(ThemeRegistry.Keys.Count, d.Count);
            Assert.All(ThemeRegistry.PaletteKeys, k => Assert.IsType<Color>(d[ThemeService.PalettePrefix + k]));
        }
        var files = Directory.GetFiles(Path.Combine(Root(), "src", "AtlasPlayer.App", "Themes"), "*.axaml").Select(Path.GetFileNameWithoutExtension);
        Assert.Equal(ThemeRegistry.All.Select(t => t.File).Order(), files.Order()); // no theme file left out of the registry
    }

    /// <summary>WCAG 2 contrast ratio.</summary>
    private static double Contrast(Color a, Color b)
    {
        static double L(Color c)
        {
            static double F(byte v) { var s = v / 255.0; return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4); }
            return 0.2126 * F(c.R) + 0.7152 * F(c.G) + 0.0722 * F(c.B);
        }
        var (la, lb) = (L(a), L(b));
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    [AvaloniaFact]
    public void Every_themes_text_is_readable()
    {
        foreach (var theme in ThemeRegistry.All)
        {
            var d = ThemeService.Load(theme);
            void Check(string fg, string bg, double need)
            {
                var c = Contrast(ColorOf(d, fg), ColorOf(d, bg));
                Assert.True(c >= need, $"{theme.Id}: {fg} on {bg} is {c:0.00}:1, needs {need}:1");
            }
            foreach (var ground in new[] { "ThemeBg", "ThemeSurface", "ThemeRaised" })
            {
                Check("ThemeText", ground, 4.5);
                Check("ThemeTextMuted", ground, 4.5);
            }
            Check("ThemeOnAccent", "ThemeAccent", 4.5);
            Check("ThemeHighlight", "ThemeSurface", 4.5);
            Check("ThemePositive", "ThemeSurface", 4.5);
            Check("ThemeAccent", "ThemeSurface", 3); // a switch's or a button's fill
        }
    }

    [AvaloniaFact]
    public void Views_reach_theme_keys_only_live_and_hold_no_colours_of_their_own()
    {
        var views = Directory.GetFiles(Path.Combine(Root(), "src", "AtlasPlayer.App"), "*.axaml", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}Themes{Path.DirectorySeparatorChar}") && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));
        foreach (var file in views)
        {
            var xaml = File.ReadAllText(file);
            Assert.DoesNotMatch(new Regex(@"StaticResource Theme"), xaml); // a StaticResource wouldn't follow a theme switch
            Assert.DoesNotMatch(new Regex(@"(Background|Foreground)=""#"), xaml); // colours come from the theme
        }
    }

    [AvaloniaFact]
    public void A_theme_switches_every_open_window_at_once()
    {
        try
        {
            using var r = new Rig();
            var w = new MainWindow { DataContext = r.Vm };
            w.Show();
            ThemeService.Apply("atlas");
            Dispatch();
            var before = ((ISolidColorBrush)w.Background!).Color;

            var gta = ThemeService.Apply("gta-6");
            Dispatch();
            Assert.Equal("gta-6", ThemeService.Current.Id);
            Assert.Equal(ColorOf(ThemeService.Load(gta), "ThemeBg"), ((ISolidColorBrush)w.Background!).Color);
            Assert.NotEqual(before, ((ISolidColorBrush)w.Background!).Color);
            // Fluent's own controls follow too.
            Assert.True(Application.Current!.TryGetResource("SystemAccentColor", ThemeVariant.Dark, out var accent));
            Assert.Equal(Color.Parse("#FF4F8B"), accent);

            ThemeService.Apply("atlas-light");
            Dispatch();
            Assert.Equal(ThemeVariant.Light, Application.Current.ActualThemeVariant);
            w.Close();
        }
        finally
        {
            ThemeService.Apply(ThemeRegistry.Default.Id);
        }
    }

    [AvaloniaFact]
    public void An_unknown_theme_is_the_default_one()
    {
        try
        {
            ThemeService.Apply("gta-6");
            Assert.Equal(ThemeRegistry.Default, ThemeService.Apply("from-a-newer-version"));
            Assert.Equal(ThemeRegistry.Default, ThemeService.Current);
            Assert.Equal(ThemeRegistry.Find("halloween"), ThemeRegistry.Find(" HALLOWEEN "));
        }
        finally
        {
            ThemeService.Apply(ThemeRegistry.Default.Id);
        }
    }

    [AvaloniaFact]
    public void Choosing_a_theme_in_settings_wears_and_saves_it()
    {
        try
        {
            using var r = new Rig();
            Assert.Equal("atlas", r.Vm.SelectedTheme!.Id);
            r.Vm.SelectedTheme = r.Vm.ThemeChoices.Single(c => c.Id == "red-dead-redemption-2");
            Assert.Equal("red-dead-redemption-2", r.Settings.Theme);
            Assert.Equal("red-dead-redemption-2", ThemeService.Current.Id);
            Assert.Equal("red-dead-redemption-2", r.Store.Load().Theme); // saved
            Assert.Contains("western", r.Vm.ThemeDescription);
        }
        finally
        {
            ThemeService.Apply(ThemeRegistry.Default.Id);
        }
    }

    [AvaloniaFact]
    public async Task A_theme_travels_with_the_settings_file()
    {
        try
        {
            using var r = new Rig();
            r.Vm.SelectedTheme = r.Vm.ThemeChoices.Single(c => c.Id == "synthwave");
            var file = Path.Combine(r.Root, "out", SettingsStore.ExportName);
            Assert.True(r.Vm.ExportSettings(file));
            Assert.Equal("synthwave", SettingsStore.ReadFile(file)!.Theme);

            using var other = new Rig();
            Assert.True(await other.Vm.ImportSettingsAsync(file, ask: false));
            Dispatch();
            Assert.Equal("synthwave", other.Settings.Theme);
            Assert.Equal("synthwave", ThemeService.Current.Id);
        }
        finally
        {
            ThemeService.Apply(ThemeRegistry.Default.Id);
        }
    }

    [AvaloniaFact]
    public void The_theme_picker_shows_every_theme_in_its_own_colours_and_title_type()
    {
        using var r = new Rig();
        Assert.Equal(ThemeRegistry.All.Select(t => t.Name), r.Vm.ThemeChoices.Select(c => c.Name));
        foreach (var choice in r.Vm.ThemeChoices)
        {
            Assert.NotNull(choice.Background);
            // The heading fonts are bundled: each resolves to itself, not a fallback, on every OS.
            Assert.True(FontManager.Current.TryGetGlyphTypeface(new Typeface(choice.HeadingFont), out var glyphs), choice.Id);
            var expected = choice.HeadingFont.Name.Split('#').Last();
            Assert.Equal(expected, glyphs!.FamilyName);
        }
    }

    [AvaloniaFact]
    public void The_settings_window_and_dialogs_open_in_every_theme()
    {
        try
        {
            foreach (var theme in ThemeRegistry.All)
            {
                ThemeService.Apply(theme.Id);
                using var r = new Rig();
                var w = new SettingsWindow { DataContext = r.Vm };
                w.Show();
                var tabs = w.GetLogicalDescendants().OfType<TabControl>().Single();
                foreach (var tab in tabs.Items.OfType<TabItem>())
                {
                    tabs.SelectedItem = tab;
                    Dispatch();
                }
                Assert.NotNull(w.CaptureRenderedFrame());
                w.Close();
                var link = new OpenLinkDialog(new ViewModels.OpenLinkDialogViewModel());
                link.Show();
                Assert.NotNull(link.CaptureRenderedFrame());
                link.Close();
            }
        }
        finally
        {
            ThemeService.Apply(ThemeRegistry.Default.Id);
        }
    }
}
