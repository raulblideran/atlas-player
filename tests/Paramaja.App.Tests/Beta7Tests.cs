using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Paramaja.App.Services;
using Paramaja.App.ViewModels;
using Paramaja.App.Views;
using Paramaja.Core.Playback;
using Paramaja.Core.Settings;

namespace Paramaja.App.Tests;

/// <summary>2.0.0-beta.7: More's download item, the interface font, audio codec badges, fullscreen's way back.</summary>
public class Beta7Tests
{
    private static void Dispatch() => Dispatcher.UIThread.RunJobs();

    private static void Playing(Rig r, bool video = true)
    {
        r.Vm.IsIdle = false;
        r.Player.State.HasVideo = video;
        r.Player.RaiseState("has-video");
    }

    private static YtDlp FakeYtDlp()
    {
        var app = Path.Combine(Path.GetTempPath(), "paramaja-app-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(app);
        File.WriteAllText(Path.Combine(app, OperatingSystem.IsWindows() ? "yt-dlp.exe" : "yt-dlp"), "");
        return new YtDlp(app);
    }

    /// <summary>More's "Download video…" as the open menu shows it: its realized item, on screen or not.</summary>
    private static bool DownloadShownInMore(ControlsBar bar)
    {
        var button = bar.FindControl<Button>("MoreButton")!;
        var menu = (MenuFlyout)button.Flyout!;
        if (!menu.IsOpen) menu.ShowAt(button);
        Dispatch();
        var item = menu.Items.OfType<MenuItem>().Single(i => i.Header as string == "Download video…");
        Assert.NotNull(item.GetVisualRoot()); // realized, in the open menu
        var shown = item.IsEffectivelyVisible;
        menu.Hide();
        Dispatch();
        return shown;
    }

    [AvaloniaFact]
    public async Task More_opened_first_during_a_web_video_has_no_download_item()
    {
        using var r = new Rig(ytdlp: FakeYtDlp());
        await r.Vm.OpenInputAsync("https://youtube.com/watch?v=abc");
        Playing(r);
        Assert.True(r.Vm.CanDownload);
        // The bar comes up with the video already playing, as in the app; More opens only now.
        var bar = new ControlsBar { DataContext = r.Vm, Width = 1280 };
        bar.Show();
        Dispatch();
        Assert.False(DownloadShownInMore(bar));
        Assert.False(DownloadShownInMore(bar)); // and again
        r.Vm.StopCommand.Execute(null);
        r.Vm.IsIdle = true;
        Dispatch();
        Assert.True(DownloadShownInMore(bar)); // nothing playing: More is the way in again
        bar.Close();
    }

    [AvaloniaFact]
    public void More_has_no_download_item_when_the_bar_has_the_download_button()
    {
        using var r = new Rig(ytdlp: FakeYtDlp());
        var bar = new ControlsBar { DataContext = r.Vm, Width = 1280 };
        bar.Show();
        Dispatch();
        Assert.True(DownloadShownInMore(bar));
        r.Vm.UseEverythingBarCommand.Execute(null); // it has the Download button
        Dispatch();
        Assert.Equal("right", bar.GroupOf("download") ?? "right");
        Assert.NotNull(bar.GroupOf("download"));
        Assert.False(r.Vm.DownloadInMore);
        Assert.False(DownloadShownInMore(bar));
        r.Vm.UseSimpleBarCommand.Execute(null);
        Dispatch();
        Assert.True(DownloadShownInMore(bar));
        bar.Close();
    }

    // ---- The interface font ------------------------------------------------------------------

    private static FontFamily? AppFont(string key) =>
        Application.Current!.Resources.TryGetValue(key, out var v) ? v as FontFamily : null;

    [AvaloniaFact]
    public void The_interface_font_changes_every_window_and_menu_and_is_saved()
    {
        try
        {
            ThemeService.Apply("graphite");
            using var r = new Rig();
            Assert.Equal(MainViewModel.DefaultUiFontLabel, r.Vm.UiFonts[0]);
            Assert.Equal(MainViewModel.DefaultUiFontLabel, r.Vm.UiFontChoice);
            var w = new Window { Content = new TextBlock { Text = "Hello" } };
            w.Show();
            r.Vm.UiFontChoice = "DejaVu Serif";
            Dispatch();
            Assert.Equal("DejaVu Serif", r.Settings.UiFont);
            Assert.Equal("DejaVu Serif", ((TextBlock)w.Content!).FontFamily.Name);
            Assert.Equal("DejaVu Serif", AppFont("ThemeHeadingFont")?.Name); // Graphite's titles are Inter: they follow
            Assert.Contains("\"uiFont\": \"DejaVu Serif\"", File.ReadAllText(r.Paths.SettingsFile));

            // A menu (a popup of its own) has it too.
            var button = new Button { Content = "Menu" };
            var menu = new MenuFlyout { Items = { new MenuItem { Header = "One" } } };
            button.Flyout = menu;
            w.Content = new StackPanel { Children = { button } };
            menu.ShowAt(button);
            Dispatch();
            var item = (MenuItem)menu.Items[0]!;
            Assert.Equal("DejaVu Serif", item.FontFamily.Name);
            menu.Hide();

            // A theme with a title font of its own keeps it.
            ThemeService.Apply("gta-6");
            Dispatch();
            Assert.Null(AppFont("ThemeHeadingFont"));
            Assert.Equal("DejaVu Serif", AppFont(FontService.ResourceKey)?.Name); // the body text still has it

            r.Vm.ResetUiFontCommand.Execute(null);
            Dispatch();
            Assert.Equal("", r.Settings.UiFont);
            Assert.Null(AppFont(FontService.ResourceKey)); // Fluent's own again: Inter
            var plain = new Window { Content = new TextBlock() };
            plain.Show();
            Assert.Equal("Inter", ((TextBlock)plain.Content!).FontFamily.Name);
            plain.Close();
            Assert.Null(AppFont("ThemeHeadingFont"));
            w.Close();
        }
        finally
        {
            FontService.Apply("");
            ThemeService.Apply(ThemeRegistry.Default.Id);
        }
    }

    [AvaloniaFact]
    public void A_font_from_another_machine_stays_in_the_list()
    {
        using var r = new Rig(new PlayerSettings { UiFont = "Some Font Not Here" });
        Assert.Contains("Some Font Not Here", r.Vm.UiFonts);
        Assert.Equal("Some Font Not Here", r.Vm.UiFontChoice);
    }

    // ---- Audio files get a codec badge -------------------------------------------------------

    [AvaloniaFact]
    public void An_audio_file_shows_its_codec_with_the_rate_in_the_tooltip()
    {
        using var r = new Rig();
        Playing(r, video: false);
        r.Player.State.AudioCodec = "flac";
        r.Player.State.SourceSampleRate = 96000;
        r.Player.RaiseState("audio-codec-name");
        Assert.Equal("FLAC", r.Vm.CodecBadge);
        Assert.Equal("FLAC, lossless · 96 kHz", r.Vm.CodecTip);

        r.Player.State.AudioCodec = "pcm_s24le";
        r.Player.State.SourceSampleRate = 44100;
        r.Player.RaiseState("audio-params/samplerate");
        r.Player.RaiseState("audio-codec-name");
        Assert.Equal("PCM", r.Vm.CodecBadge);
        Assert.Equal("PCM, uncompressed · 44.1 kHz", r.Vm.CodecTip);

        // A video's badge is still its video codec.
        r.Player.State.HasVideo = true;
        r.Player.State.VideoFormat = "hevc";
        r.Player.RaiseState("has-video");
        Assert.Equal("HEVC", r.Vm.CodecBadge);
    }

    // ---- Fullscreen goes back to the window it came from ---------------------------------------

    [AvaloniaFact]
    public void Leaving_fullscreen_restores_a_normal_window_where_it_was()
    {
        using var r = new Rig();
        var w = new MainWindow { DataContext = r.Vm, Width = 900, Height = 600, WindowStartupLocation = WindowStartupLocation.Manual };
        w.Show();
        w.Position = new PixelPoint(100, 120);
        Dispatch();
        w.SetFullscreen(null);
        Dispatch();
        Assert.Equal(WindowState.FullScreen, w.WindowState);
        Assert.Equal((WindowState.Normal, new PixelPoint(100, 120), new Size(900, 600)), w.BeforeFullscreen);
        w.SetFullscreen(false);
        Dispatch();
        Assert.Equal(WindowState.Normal, w.WindowState);
        Assert.Equal(new PixelPoint(100, 120), w.Position);
        Assert.Equal(900, w.Width);
        Assert.Equal(600, w.Height);
        w.Close();
    }

    [AvaloniaFact]
    public void Leaving_fullscreen_restores_a_maximized_window_maximized()
    {
        using var r = new Rig();
        var w = new MainWindow { DataContext = r.Vm };
        w.Show();
        w.WindowState = WindowState.Maximized;
        Dispatch();
        r.Vm.ToggleFullscreenCommand.Execute(null);
        Dispatch();
        Assert.Equal(WindowState.FullScreen, w.WindowState);
        r.Vm.ToggleFullscreenCommand.Execute(null);
        Dispatch();
        Assert.Equal(WindowState.Maximized, w.WindowState);
        w.Close();
    }
}
