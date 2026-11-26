using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Paramaja.App.Services;
using Paramaja.App.ViewModels;
using Paramaja.App.Views;
using Paramaja.Core.Settings;

namespace Paramaja.App.Tests;

/// <summary>2.0.0-beta.8: solid bars, Play's own colours, and the MPC-HC theme's bar layout.</summary>
public class Beta8Tests
{
    private static bool IsClassic(PlayerSettings s) => BarItems.Classic.SameAs(s.BarLeft, s.BarCentre, s.BarRight);
    private static ThemeChoice Theme(Rig r, string id) => r.Vm.ThemeChoices.Single(c => c.Id == id);

    [AvaloniaFact]
    public void Light_themes_have_solid_bars_dark_ones_a_light_fade_and_both_read()
    {
        static Color Over(Color top, Color under)
        {
            var a = top.A / 255.0;
            byte Mix(byte t, byte u) => (byte)Math.Round(t * a + u * (1 - a));
            return Color.FromRgb(Mix(top.R, under.R), Mix(top.G, under.G), Mix(top.B, under.B));
        }
        static double Contrast(Color x, Color y)
        {
            static double L(Color c)
            {
                static double F(byte v) { var s = v / 255.0; return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4); }
                return 0.2126 * F(c.R) + 0.7152 * F(c.G) + 0.0722 * F(c.B);
            }
            var (a, b) = (L(x), L(y));
            return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
        }
        foreach (var theme in ThemeRegistry.All)
        {
            var d = ThemeService.Load(theme);
            Color C(string key) => Assert.IsAssignableFrom<ISolidColorBrush>(d[key]).Color;
            // Where the bar's ground is densest, over a mid-grey picture (a fade's text sits there).
            Color Ground(string key)
            {
                if (theme.Variant == Avalonia.Styling.ThemeVariant.Light)
                {
                    var solid = C(key);
                    Assert.Equal(255, solid.A); // light themes: no white haze over the picture
                    return solid;
                }
                var stops = Assert.IsAssignableFrom<IGradientBrush>(d[key]).GradientStops;
                var densest = stops.MaxBy(g => g.Color.A)!.Color;
                Assert.InRange(densest.A, 0x80, 0xBF); // a light fade: well under 2.0.0-beta.7's 95%
                Assert.Equal(0, stops.MinBy(g => g.Color.A)!.Color.A); // gone by the picture's side
                return Over(densest, Color.FromRgb(0x80, 0x80, 0x80));
            }
            Assert.Equal(theme.Variant == Avalonia.Styling.ThemeVariant.Light, C("ThemeBarEdge").A == 255); // the line only where the bar is solid
            foreach (var ground in new[] { "ThemeBarScrimTop", "ThemeBarScrimBottom" })
                Assert.True(Contrast(C("ThemeBarText"), Ground(ground)) >= 4.5, $"{theme.Id}: bar text on {ground}");
            var bar = Ground("ThemeBarScrimBottom");
            var badge = Over(C("ThemeBadge"), bar);
            Assert.True(Contrast(C("ThemeBadgeText"), badge) >= 4.5, $"{theme.Id}: badge text");
            foreach (var ground in new[] { "ThemePlay", "ThemePlayHover" })
            {
                var c = Contrast(C("ThemeOnPlay"), Over(C(ground), bar));
                Assert.True(c >= 3, $"{theme.Id}: Play's glyph on {ground} is {c:0.00}:1");
            }
        }
    }

    [AvaloniaFact]
    public void Mpc_hc_plays_with_a_plain_glyph_and_the_others_keep_their_filled_button()
    {
        var mpc = ThemeService.Load(ThemeRegistry.Find("mpc-hc"));
        Assert.Equal(0, ((ISolidColorBrush)mpc["ThemePlay"]!).Color.A);
        var graphite = ThemeService.Load(ThemeRegistry.Find("graphite"));
        Assert.Equal(((ISolidColorBrush)graphite["ThemeAccent"]!).Color, ((ISolidColorBrush)graphite["ThemePlay"]!).Color);
    }

    [AvaloniaTheory]
    [InlineData("paramaja-light")]
    [InlineData("vice-city-sunset")]
    [InlineData("mpc-hc")]
    public void Plays_glyph_is_in_the_themes_play_colour_not_fluents_text_colour(string theme)
    {
        try
        {
            ThemeService.Apply(theme);
            using var r = new Rig();
            r.Vm.IsIdle = false;
            var bar = new ControlsBar { DataContext = r.Vm, Width = 1280 };
            bar.Show();
            Dispatcher.UIThread.RunJobs();
            var play = bar.FindControl<Button>("PlayButton")!;
            var glyph = play.GetVisualDescendants().OfType<PathIcon>().First(i => i.IsVisible);
            var want = ((ISolidColorBrush)ThemeService.Load(ThemeRegistry.Find(theme))["ThemeOnPlay"]!).Color;
            Assert.Equal(want, ((ISolidColorBrush)play.Foreground!).Color);
            Assert.Equal(want, ((ISolidColorBrush)glyph.Foreground!).Color);
            bar.Close();
        }
        finally { ThemeService.Apply(PlayerSettings.DefaultTheme); }
    }

    [Fact]
    public void The_classic_preset_is_sound_as_it_is()
    {
        var s = new PlayerSettings { BarLeft = [.. BarItems.Classic.Left], BarCentre = [], BarRight = [.. BarItems.Classic.Right] };
        s.Normalize();
        Assert.True(IsClassic(s));
        Assert.Empty(s.BarCentre);
        Assert.Equal(BarItems.Play, s.BarLeft[0]);
    }

    [AvaloniaFact]
    public void Picking_mpc_hc_brings_its_bar_and_another_theme_puts_the_old_one_back()
    {
        try
        {
            using var r = new Rig();
            Bar.Off(r.Vm, "fullscreen"); // a bar of the user's own
            var own = (r.Settings.BarLeft.ToList(), r.Settings.BarCentre.ToList(), r.Settings.BarRight.ToList());
            r.Vm.SelectedTheme = Theme(r, "mpc-hc");
            Assert.True(IsClassic(r.Settings));
            Assert.Equal(2, r.Vm.BarLayoutIndex);
            Assert.Equal(BarItems.Classic.Left, r.Vm.BarZones[0].Slots.Select(s => s.Id));
            Assert.NotNull(r.Settings.BarBeforeClassic);
            Assert.Contains("barBeforeClassic", File.ReadAllText(r.Store.FilePath)); // kept across restarts

            r.Vm.SelectedTheme = Theme(r, "graphite");
            Assert.Equal(own.Item1, r.Settings.BarLeft);
            Assert.Equal(own.Item2, r.Settings.BarCentre);
            Assert.Equal(own.Item3, r.Settings.BarRight);
            Assert.Null(r.Settings.BarBeforeClassic);
            Assert.Equal(3, r.Vm.BarLayoutIndex);
        }
        finally { ThemeService.Apply(PlayerSettings.DefaultTheme); }
    }

    [AvaloniaFact]
    public void A_bar_rearranged_under_mpc_hc_stays_when_the_theme_changes()
    {
        try
        {
            using var r = new Rig();
            r.Vm.SelectedTheme = Theme(r, "mpc-hc");
            Bar.Off(r.Vm, "volume");
            var mine = r.Settings.BarRight.ToList();
            r.Vm.SelectedTheme = Theme(r, "paramaja-light");
            Assert.Equal(mine, r.Settings.BarRight);
            Assert.Null(r.Settings.BarBeforeClassic);
        }
        finally { ThemeService.Apply(PlayerSettings.DefaultTheme); }
    }

    [AvaloniaFact]
    public void The_classic_layout_is_a_preset_of_its_own_and_starting_with_mpc_hc_leaves_the_bar_alone()
    {
        try
        {
            using var r = new Rig(new PlayerSettings { Theme = "mpc-hc" });
            Assert.True(BarItems.Simple.SameAs(r.Settings.BarLeft, r.Settings.BarCentre, r.Settings.BarRight));
            r.Vm.BarLayoutIndex = 2;
            Assert.True(IsClassic(r.Settings));
            Assert.Contains("Media Player Classic", r.Vm.BarLayoutText);

            var w = new SettingsWindow { DataContext = r.Vm };
            w.Show();
            Assert.Equal("MPC-HC", ((ComboBoxItem)w.FindControl<ComboBox>("BarLayout")!.SelectedItem!).Content);
            w.Close();
        }
        finally { ThemeService.Apply(PlayerSettings.DefaultTheme); }
    }

    [AvaloniaFact]
    public void The_classic_bar_shows_play_on_the_left_and_the_volume_on_the_right()
    {
        using var r = new Rig();
        r.Vm.BarLayoutIndex = 2;
        r.Vm.IsIdle = false;
        r.Player.State.HasVideo = true;
        r.Player.RaiseState("has-video");
        var bar = new ControlsBar { DataContext = r.Vm, Width = 1280 };
        bar.Show();
        Dispatcher.UIThread.RunJobs();
        var play = bar.FindControl<Button>("PlayButton")!;
        var mute = bar.FindControl<Button>("MuteButton")!;
        Assert.True(play.IsEffectivelyVisible);
        Assert.True(play.TranslatePoint(default, bar)!.Value.X < 200);
        Assert.True(mute.TranslatePoint(default, bar)!.Value.X > 1000);
        bar.Close();
    }
}
