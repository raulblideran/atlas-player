using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using AtlasPlayer.App.ViewModels;
using AtlasPlayer.App.Views;
using AtlasPlayer.Core.Playback;
using AtlasPlayer.Core.Settings;

namespace AtlasPlayer.App.Tests;

/// <summary>
/// The 2.0.0 controls bar: Simple by default, with a More menu holding whatever the bar leaves
/// off, so no action is ever out of reach.
/// </summary>
[Collection(ThemeCollection.Name)]
public class RedesignTests
{
    private static void Playing(Rig r, bool video = true, bool chapters = false)
    {
        r.Vm.IsIdle = false;
        r.Player.State.HasVideo = video;
        r.Player.RaiseState("has-video");
        if (!chapters) return;
        r.Player.Chapters = [new ChapterInfo(0, "Opening", 0), new ChapterInfo(1, "Middle", 120)];
        r.Player.RaiseChapters();
    }

    /// <summary>More's items, with the menu open (its items are bound once it is).</summary>
    private static List<MenuItem> MoreItems(ControlsBar bar)
    {
        var button = bar.FindControl<Button>("MoreButton")!;
        var menu = (MenuFlyout)button.Flyout!;
        if (!menu.IsOpen) menu.ShowAt(button);
        Dispatcher.UIThread.RunJobs();
        return menu.Items.OfType<MenuItem>().ToList();
    }

    private static MenuItem Item(ControlsBar bar, string header) => MoreItems(bar).Single(i => i.Header as string == header);

    [AvaloniaFact]
    public void The_simple_bar_leaves_its_buttons_off_and_more_has_each_of_them()
    {
        using var r = new Rig();
        Playing(r, chapters: true);
        var bar = new ControlsBar { DataContext = r.Vm, Width = 1280 };
        bar.Show();
        Dispatcher.UIThread.RunJobs();
        foreach (var name in new[] { "StopButton", "PreviousChapterButton", "NextChapterButton",
                     "ScreenshotButton", "DownloadVideoButton", "OpenLinkButton", "OpenMenuButton" })
            Assert.False(bar.FindControl<Button>(name)!.IsEffectivelyVisible, name);
        foreach (var name in new[] { "PlayButton", "FrameBackButton", "FrameNextButton", "MoreButton", "SettingsButton", "FullscreenButton",
                     "SubtitlesButton", "AudioMenuButton", "VideoMenuButton" })
            Assert.True(bar.FindControl<Button>(name)!.IsEffectivelyVisible, name);

        // Each one left off is in More, doing what its button did.
        Assert.Same(r.Vm.StopCommand, Item(bar, "Stop").Command);
        Assert.False(Item(bar, "Previous frame").IsVisible); // on the bar itself (2.0.1)
        Assert.False(Item(bar, "Next frame").IsVisible);
        Assert.Same(r.Vm.PreviousChapterCommand, Item(bar, "Previous chapter").Command);
        Assert.Same(r.Vm.NextChapterCommand, Item(bar, "Next chapter").Command);
        Assert.Same(r.Vm.ScreenshotCommand, Item(bar, "Screenshot").Command);
        Assert.Equal(false, Item(bar, "Screenshot").CommandParameter);
        Assert.Equal(true, Item(bar, "Screenshot with subtitles").CommandParameter);
        Assert.Same(r.Vm.OpenDownloaderCommand, Item(bar, "Download video…").Command);
        Assert.Same(r.Vm.OpenLinkCommand, Item(bar, "Open link…").Command);
        Assert.Same(r.Vm.OpenFileCommand, Item(bar, "Open file…").Command);
        Assert.Same(r.Vm.OpenFolderCommand, Item(bar, "Open folder…").Command);
        Assert.Same(r.Vm.OpenDiscCommand, Item(bar, "Open disc…").Command);
        Assert.Same(r.Vm.OpenRipperCommand, Item(bar, "Rip disc…").Command);
        Assert.Same(r.Vm.OpenScreenshotsFolderCommand, Item(bar, "Screenshots folder").Command);
        foreach (var header in new[] { "Stop", "Next chapter", "Screenshot", "Open file…", "Rip disc…" })
            Assert.True(Item(bar, header).IsVisible, header);
        bar.Close();
    }

    [AvaloniaFact]
    public async Task More_has_no_download_while_the_top_bar_offers_one()
    {
        // A yt-dlp to find (it's never run here).
        var app = Path.Combine(Path.GetTempPath(), "atlas-app-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(app);
        File.WriteAllText(Path.Combine(app, OperatingSystem.IsWindows() ? "yt-dlp.exe" : "yt-dlp"), "");
        using var r = new Rig(ytdlp: new YtDlp(app));
        var bar = new ControlsBar { DataContext = r.Vm, Width = 1280 };
        bar.Show();
        Dispatcher.UIThread.RunJobs();
        Assert.True(Item(bar, "Download video…").IsVisible); // nothing playing: More is the way in
        await r.Vm.OpenInputAsync("https://youtube.com/watch?v=abc");
        Playing(r);
        Assert.True(r.Vm.CanDownload); // the top bar's download button is up
        Dispatcher.UIThread.RunJobs();
        Assert.False(Item(bar, "Download video…").IsVisible);
        bar.Close();
    }

    [AvaloniaFact]
    public void More_offers_only_what_applies_and_what_the_bar_doesnt_show()
    {
        using var r = new Rig();
        Playing(r, video: false); // music: no frames, no chapters
        var bar = new ControlsBar { DataContext = r.Vm, Width = 1280 };
        bar.Show();
        Dispatcher.UIThread.RunJobs();
        Assert.False(Item(bar, "Next frame").IsVisible);
        Assert.False(Item(bar, "Next chapter").IsVisible);
        Assert.False(Item(bar, "Screenshot").IsVisible); // no picture to save
        Assert.True(Item(bar, "Stop").IsVisible);

        r.Vm.BarLayoutIndex = 1; // Everything: the bar has them, More doesn't repeat them
        Dispatcher.UIThread.RunJobs();
        Assert.True(bar.FindControl<Button>("StopButton")!.IsEffectivelyVisible);
        Assert.False(Item(bar, "Stop").IsVisible);
        Assert.False(Item(bar, "Screenshot").IsVisible);
        Assert.False(r.Vm.HasPlaybackInMore);
        Assert.True(Item(bar, "Open file…").IsVisible); // the ways to open something are always there
        bar.Close();
    }

    [Fact]
    public void The_layout_presets_write_the_arrangement_and_a_hand_made_bar_is_custom()
    {
        using var r = new Rig();
        Assert.Equal(0, r.Vm.BarLayoutIndex);
        Assert.Contains("More", r.Vm.BarLayoutText);
        r.Vm.BarLayoutIndex = 1;
        Assert.Equal(BarItems.Everything.Right, r.Settings.BarRight);
        Assert.Equal(BarItems.Everything.Centre, r.Vm.BarZones[1].Slots.Select(s => s.Id));
        Bar.Off(r.Vm, "screenshot");
        Assert.Equal(3, r.Vm.BarLayoutIndex);
        r.Vm.BarLayoutIndex = 3; // choosing Custom itself changes nothing
        Assert.DoesNotContain("screenshot", r.Settings.BarRight);
        r.Vm.BarLayoutIndex = 0;
        Assert.True(BarItems.Simple.SameAs(r.Settings.BarLeft, r.Settings.BarCentre, r.Settings.BarRight));
        Assert.Equal(BarItems.Simple.Right, r.Vm.BarZones[2].Slots.Select(s => s.Id));
        // Only the order changed: still Custom.
        Bar.Move(r.Vm, "fullscreen", BarZoneKind.Right, 0);
        Assert.Equal(3, r.Vm.BarLayoutIndex);
        Assert.Contains("\"fullscreen\"", File.ReadAllText(r.Store.FilePath)); // saved
    }

    [AvaloniaFact]
    public void The_settings_layout_choice_follows_the_bar()
    {
        using var r = new Rig();
        var w = new SettingsWindow { DataContext = r.Vm };
        w.Show();
        var layout = w.FindControl<ComboBox>("BarLayout")!;
        Assert.Equal(0, layout.SelectedIndex);
        layout.SelectedIndex = 1;
        Assert.Equal(BarItems.Everything.Centre, r.Settings.BarCentre);
        Bar.Off(r.Vm, "stop");
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(3, layout.SelectedIndex);
        w.Close();
    }

    [AvaloniaFact]
    public void A_frame_step_left_off_the_bar_is_in_more()
    {
        using var r = new Rig(new PlayerSettings { BarCentre = [.. BarItems.Simple.Centre.Except(["prevframe", "nextframe"])] });
        Playing(r);
        var bar = new ControlsBar { DataContext = r.Vm, Width = 1280 };
        bar.Show();
        Dispatcher.UIThread.RunJobs();
        Assert.False(bar.FindControl<Button>("FrameNextButton")!.IsEffectivelyVisible);
        Assert.Same(r.Vm.FrameBackStepCommand, Item(bar, "Previous frame").Command);
        Assert.Same(r.Vm.FrameStepCommand, Item(bar, "Next frame").Command);
        Assert.True(Item(bar, "Next frame").IsVisible);
        bar.Close();
    }

    [AvaloniaTheory]
    [InlineData(1280, 0, false)]
    [InlineData(1280, 0, true)]
    [InlineData(1280, 1, true)]
    [InlineData(1920, 1, false)]
    [InlineData(720, 1, true)]
    public void Play_sits_on_the_bars_centre_whatever_shows_beside_it(double width, int layout, bool chapters)
    {
        using var r = new Rig();
        Playing(r, chapters: chapters);
        r.Player.State.PlaylistCount = 3;
        r.Player.RaiseState("playlist-count");
        r.Vm.BarLayoutIndex = layout;
        if (chapters && layout == 0) // chapter steps both sides
        {
            Bar.Move(r.Vm, "prevchapter", BarZoneKind.Centre, r.Settings.BarCentre.IndexOf("prevframe"));
            Bar.Move(r.Vm, "nextchapter", BarZoneKind.Centre, r.Settings.BarCentre.IndexOf("nextframe") + 1);
        }
        var bar = new ControlsBar { DataContext = r.Vm, Width = width };
        bar.Show();
        Dispatcher.UIThread.RunJobs();
        var transport = bar.FindControl<StackPanel>("Transport")!;
        var row = (Panel)transport.Parent!;
        var right = (Control)row.Children[2];
        var play = bar.FindControl<Button>("PlayButton")!;
        var playCentre = transport.Bounds.X + play.Bounds.Center.X;
        if (transport.Bounds.Width + right.Bounds.Width + 2 * Controls.BarRow.Gap < row.Bounds.Width / 2)
            Assert.InRange(playCentre, row.Bounds.Width / 2 - 1, row.Bounds.Width / 2 + 1);
        var rightShown = right.Bounds.Left + (right.Clip?.Bounds.Left ?? 0); // a narrow bar clips the right group's start
        Assert.True(transport.Bounds.Right <= rightShown - Controls.BarRow.Gap + 0.5, $"{transport.Bounds} overlaps {right.Bounds} from {rightShown}");
        Assert.True(right.Bounds.Right <= row.Bounds.Width + 0.5); // fullscreen at the end always shows
        Assert.True(((Control)row.Children[0]).Bounds.Right <= transport.Bounds.Left);
        // Play is in the group, between previous and next.
        var buttons = transport.Children.OfType<Button>().Where(b => b.IsVisible).Select(b => b.Name).ToList();
        if (chapters) Assert.Equal(["PreviousChapterButton", "FrameBackButton", "PlayButton", "FrameNextButton", "NextChapterButton"],
            buttons.Where(b => b!.Contains("Chapter") || b.Contains("Frame") || b == "PlayButton"));
        Assert.True(buttons.IndexOf("PreviousFileButton") < buttons.IndexOf("PlayButton"));
        Assert.True(buttons.IndexOf("PlayButton") < buttons.IndexOf("NextFileButton"));
        bar.Close();
    }

    [AvaloniaFact]
    public void A_clicked_bar_button_doesnt_take_the_keyboard()
    {
        using var r = new Rig();
        Playing(r);
        var main = new MainWindow { DataContext = r.Vm };
        main.Show();
        var bar = new ControlsBar { DataContext = r.Vm, Width = 1280 };
        bar.Show(main);
        Dispatcher.UIThread.RunJobs();
        foreach (var name in new[] { "PlayButton", "FullscreenButton", "MoreButton", "SettingsButton" })
            Assert.False(bar.FindControl<Button>(name)!.Focusable, name);
        var play = bar.FindControl<Button>("PlayButton")!;
        var at = play.TranslatePoint(new Point(play.Bounds.Width / 2, play.Bounds.Height / 2), bar)!.Value;
        bar.MouseDown(at, Avalonia.Input.MouseButton.Left);
        bar.MouseUp(at, Avalonia.Input.MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.False(play.IsFocused);
        Assert.Contains("TogglePause", r.Player.Calls); // the click still works
        bar.Close();
        main.Close();
    }

    /// <summary>
    /// A clicked button keeps the keyboard focus, and a focused button clicks on Space's key-up:
    /// Space paused and pressed the button too (fullscreen left, play toggled twice).
    /// </summary>
    [AvaloniaTheory]
    [InlineData("PlayButton")]
    [InlineData("FullscreenButton")]
    [InlineData("SubtitlesButton")]
    public void Space_after_clicking_a_bar_button_only_pauses(string name)
    {
        using var r = new Rig();
        Playing(r);
        var main = new MainWindow { DataContext = r.Vm };
        main.Show();
        var bar = new ControlsBar { DataContext = r.Vm, Width = 1280 };
        bar.Show(main);
        Dispatcher.UIThread.RunJobs();
        var button = bar.FindControl<Button>(name)!;
        var at = button.TranslatePoint(new Avalonia.Point(button.Bounds.Width / 2, button.Bounds.Height / 2), bar)!.Value;
        bar.MouseDown(at, Avalonia.Input.MouseButton.Left);
        bar.MouseUp(at, Avalonia.Input.MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        if (button.Flyout is { IsOpen: true } menu) menu.Hide();
        Dispatcher.UIThread.RunJobs();
        var fullscreen = r.Vm.IsFullscreen;
        var before = r.Player.Calls.Count(c => c == "TogglePause");

        bar.KeyPressQwerty(Avalonia.Input.PhysicalKey.Space, Avalonia.Input.RawInputModifiers.None);
        bar.KeyReleaseQwerty(Avalonia.Input.PhysicalKey.Space, Avalonia.Input.RawInputModifiers.None);
        main.KeyPressQwerty(Avalonia.Input.PhysicalKey.Space, Avalonia.Input.RawInputModifiers.None);
        main.KeyReleaseQwerty(Avalonia.Input.PhysicalKey.Space, Avalonia.Input.RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(before + 2, r.Player.Calls.Count(c => c == "TogglePause")); // once per Space, nothing more
        Assert.Equal(fullscreen, r.Vm.IsFullscreen);
        Assert.False(button.Flyout?.IsOpen == true);
        bar.Close();
        main.Close();
    }

    /// <summary>
    /// Fitted to the screen, a bar's menu used X11's one work area for the whole desktop: with
    /// several monitors (Xwayland under KWin) it ended far above a lower monitor's bottom, and the
    /// menu floated hundreds of pixels above its button. The bars' menus open unadjusted.
    /// </summary>
    [AvaloniaFact]
    public void The_bars_menus_open_against_their_buttons_unadjusted()
    {
        using var r = new Rig();
        Playing(r);
        foreach (var bar in new OverlayWindow[] { new ControlsBar { DataContext = r.Vm, Width = 1280 }, new TopBar { DataContext = r.Vm, Width = 1280 } })
        {
            bar.Show();
            Dispatcher.UIThread.RunJobs();
            foreach (var button in bar.GetLogicalDescendants().OfType<Button>().Where(b => b.Flyout is not null))
                Assert.Equal(Avalonia.Controls.Primitives.PopupPositioning.PopupPositionerConstraintAdjustment.None,
                    ((Avalonia.Controls.Primitives.PopupFlyoutBase)button.Flyout!).PlacementConstraintAdjustment);
            bar.Close();
        }
    }

    /// <summary>
    /// X11's one work area for a desktop of several monitors cut the lower ones far above their
    /// bottom (1174 px into a 1440 px monitor here), and every popup and centred window was fitted
    /// into that. With several monitors each one's working area is its own bounds.
    /// </summary>
    [Fact]
    public void Several_monitors_each_get_their_own_bounds_as_working_area()
    {
#pragma warning disable CS0618 // Screen's constructor: fine for a test
        var main = new Avalonia.Platform.Screen(1, new PixelRect(1920, 672, 2560, 1440), new PixelRect(1920, 672, 2560, 1174), true);
        var side = new Avalonia.Platform.Screen(1, new PixelRect(0, 816, 1920, 1080), new PixelRect(0, 816, 1920, 1030), false);
        var alone = new Avalonia.Platform.Screen(1, new PixelRect(0, 0, 1920, 1080), new PixelRect(0, 0, 1920, 1040), true);
#pragma warning restore CS0618
        Assert.Equal(2, Services.ScreenWorkAreas.Fix([main, side]));
        Assert.Equal(main.Bounds, main.WorkingArea);
        Assert.Equal(side.Bounds, side.WorkingArea);
        Assert.Equal(0, Services.ScreenWorkAreas.Fix([alone])); // one monitor: the window manager's panels stand
        Assert.Equal(new PixelRect(0, 0, 1920, 1040), alone.WorkingArea);
    }

    [AvaloniaFact]
    public void The_bars_tooltips_sit_next_to_their_buttons()
    {
        using var r = new Rig();
        Playing(r);
        var bottom = new ControlsBar { DataContext = r.Vm, Width = 1280 };
        var top = new TopBar { DataContext = r.Vm, Width = 1280 };
        bottom.Show();
        top.Show();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(PlacementMode.Top, ToolTip.GetPlacement(bottom.FindControl<Button>("SettingsButton")!));
        Assert.Equal(PlacementMode.Top, ToolTip.GetPlacement(bottom.FindControl<Border>("CodecBadge")!));
        Assert.Equal(PlacementMode.Bottom, ToolTip.GetPlacement(top.GetLogicalDescendants().OfType<Button>().First()));
        bottom.Close();
        top.Close();
    }

    [Fact]
    public void Messages_over_the_video_are_the_apps_own_and_go_after_a_moment()
    {
        using var r = new Rig();
        Playing(r);
        r.Vm.HandleKey("DOWN");
        Assert.True(r.Vm.ToastIsVolume);
        Assert.Equal("95%", r.Vm.ToastText);
        r.Vm.HandleKey("m");
        Assert.True(r.Vm.ToastIsMuted);
        Assert.Equal("Muted", r.Vm.ToastText);
        r.Vm.HandleKey("x");
        Assert.False(r.Vm.ToastIsVolume);
        Assert.Equal("Subtitles +0.1s", r.Vm.ToastText);
        Assert.DoesNotContain(r.Player.Calls, c => c.StartsWith("ShowText")); // not the engine's any more
        r.Vm.TickAutoHide(DateTime.UtcNow + ViewModels.MainViewModel.ToastDuration + TimeSpan.FromMilliseconds(100));
        Assert.False(r.Vm.ToastVisible);
    }

    [Fact]
    public void The_message_sits_at_the_videos_top_left_below_the_top_bar()
    {
        var area = new PixelRect(100, 50, 1280, 720);
        Assert.Equal(new PixelPoint(120, 70), OverlayPlacement.ToastAt(area, 0, 1));
        Assert.Equal(new PixelPoint(130, 50 + (int)Math.Round((TopBar.BarHeight + 20) * 1.5)), OverlayPlacement.ToastAt(area, TopBar.BarHeight, 1.5));
    }

    [AvaloniaFact]
    public void The_volume_message_shows_its_icon_level_and_percentage_in_every_theme()
    {
        try
        {
            foreach (var theme in Services.ThemeRegistry.All)
            {
                Services.ThemeService.Apply(theme.Id);
                using var r = new Rig();
                Playing(r);
                r.Vm.HandleKey("UP");
                var toast = new ToastWindow { DataContext = r.Vm };
                toast.Show();
                Dispatcher.UIThread.RunJobs();
                Assert.True(toast.FindControl<StackPanel>("VolumeToast")!.IsVisible);
                Assert.False(toast.FindControl<TextBlock>("MessageText")!.IsVisible);
                Assert.Equal(105.0 / 150, toast.FindControl<Controls.LevelBar>("VolumeLevel")!.Value, 3);
                Assert.True(toast.Bounds.Width is > 150 and < 400, $"{theme.Id}: {toast.Bounds}");
                Assert.NotNull(toast.CaptureRenderedFrame());
                toast.Close();
            }
        }
        finally
        {
            Services.ThemeService.Apply(Services.ThemeRegistry.Default.Id);
        }
    }

    [AvaloniaFact]
    public void Escape_closes_settings()
    {
        using var r = new Rig();
        var w = new SettingsWindow { DataContext = r.Vm };
        w.Show();
        w.RaiseEvent(new Avalonia.Input.KeyEventArgs { RoutedEvent = Avalonia.Input.InputElement.KeyDownEvent, Key = Avalonia.Input.Key.Escape });
        Dispatcher.UIThread.RunJobs();
        Assert.False(w.IsVisible);
    }

    [AvaloniaFact]
    public void The_welcome_screens_settings_button_opens_settings()
    {
        using var r = new Rig(attach: false);
        var w = new MainWindow { DataContext = r.Vm };
        w.Show();
        Assert.Same(r.Vm.OpenSettingsCommand, w.FindControl<Button>("SettingsAction")!.Command);
        w.Close();
    }
}
