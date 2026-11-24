using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Paramaja.App.Views;
using Paramaja.Core.Playback;
using Paramaja.Core.Settings;

namespace Paramaja.App.Tests;

/// <summary>
/// The 2.0.0 controls bar: Simple by default, with a More menu holding whatever the bar leaves
/// off, so no action is ever out of reach.
/// </summary>
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
        using var r = new Rig(new PlayerSettings { HiddenControls = [.. PlayerSettings.SimpleBarHidden] });
        Playing(r, chapters: true);
        var bar = new ControlsBar { DataContext = r.Vm, Width = 1280 };
        bar.Show();
        Dispatcher.UIThread.RunJobs();
        foreach (var name in new[] { "StopButton", "FrameBackButton", "FrameNextButton", "PreviousChapterButton", "NextChapterButton",
                     "ScreenshotButton", "DownloadVideoButton", "OpenLinkButton", "OpenMenuButton" })
            Assert.False(bar.FindControl<Button>(name)!.IsVisible, name);
        foreach (var name in new[] { "PlayButton", "MoreButton", "SettingsButton", "FullscreenButton", "SubtitlesButton", "AudioMenuButton", "VideoMenuButton" })
            Assert.True(bar.FindControl<Button>(name)!.IsVisible, name);

        // Each one left off is in More, doing what its button did.
        Assert.Same(r.Vm.StopCommand, Item(bar, "Stop").Command);
        Assert.Same(r.Vm.FrameBackStepCommand, Item(bar, "Previous frame").Command);
        Assert.Same(r.Vm.FrameStepCommand, Item(bar, "Next frame").Command);
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
        foreach (var header in new[] { "Stop", "Previous frame", "Next chapter", "Screenshot", "Open file…", "Rip disc…" })
            Assert.True(Item(bar, header).IsVisible, header);
        bar.Close();
    }

    [AvaloniaFact]
    public void More_offers_only_what_applies_and_what_the_bar_doesnt_show()
    {
        using var r = new Rig(new PlayerSettings { HiddenControls = [.. PlayerSettings.SimpleBarHidden] });
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
        Assert.True(bar.FindControl<Button>("StopButton")!.IsVisible);
        Assert.False(Item(bar, "Stop").IsVisible);
        Assert.False(Item(bar, "Screenshot").IsVisible);
        Assert.False(r.Vm.HasPlaybackInMore);
        Assert.True(Item(bar, "Open file…").IsVisible); // the ways to open something are always there
        bar.Close();
    }

    [Fact]
    public void The_layout_presets_write_the_hidden_buttons_and_a_hand_picked_bar_is_custom()
    {
        using var r = new Rig(new PlayerSettings { HiddenControls = [.. PlayerSettings.SimpleBarHidden] });
        Assert.Equal(0, r.Vm.BarLayoutIndex);
        Assert.Contains("More", r.Vm.BarLayoutText);
        r.Vm.BarLayoutIndex = 1;
        Assert.Empty(r.Settings.HiddenControls);
        Assert.True(r.Vm.ControlChoices.All(c => c.IsShown));
        r.Vm.ControlChoices.Single(c => c.Id == "screenshot").IsShown = false;
        Assert.Equal(2, r.Vm.BarLayoutIndex);
        r.Vm.BarLayoutIndex = 2; // choosing Custom itself changes nothing
        Assert.Equal(["screenshot"], r.Settings.HiddenControls);
        r.Vm.BarLayoutIndex = 0;
        Assert.Equal(PlayerSettings.SimpleBarHidden.Order(), r.Settings.HiddenControls.Order());
        Assert.Equal(r.Vm.ControlChoices.Where(c => !c.IsShown).Select(c => c.Id).Order(), PlayerSettings.SimpleBarHidden.Order());
        Assert.Contains("\"stop\"", File.ReadAllText(r.Store.FilePath)); // saved
    }

    [AvaloniaFact]
    public void The_settings_layout_choice_follows_the_bar()
    {
        using var r = new Rig(new PlayerSettings { HiddenControls = [.. PlayerSettings.SimpleBarHidden] });
        var w = new SettingsWindow { DataContext = r.Vm };
        w.Show();
        var layout = w.FindControl<ComboBox>("BarLayout")!;
        Assert.Equal(0, layout.SelectedIndex);
        layout.SelectedIndex = 1;
        Assert.Empty(r.Settings.HiddenControls);
        r.Vm.ControlChoices.Single(c => c.Id == "stop").IsShown = false;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(2, layout.SelectedIndex);
        w.Close();
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
