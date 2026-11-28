using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using AtlasPlayer.App.ViewModels;
using AtlasPlayer.App.Views;
using AtlasPlayer.Core.Playback;
using AtlasPlayer.Core.Settings;

namespace AtlasPlayer.App.Tests;

/// <summary>Reopening a local file offers to carry on where it was left (2.0.0-beta.14).</summary>
public class ResumeTests
{
    /// <summary>mpv has loaded <paramref name="file"/> and is <paramref name="at"/> seconds in.</summary>
    internal static void Playing(Rig r, string? file, double at, double duration = 3000)
    {
        var s = r.Player.State;
        s.IsIdle = false;
        s.Path = file;
        s.Position = 0;
        s.Duration = duration;
        r.Player.RaiseState("idle-active");
        r.Player.RaiseState("path");
        r.Player.RaiseState("duration");
        r.Player.RaiseLoaded();
        s.Position = at;
        r.Player.RaiseState("time-pos");
    }

    /// <summary>Stop: mpv goes idle, and its position (no longer there) reads 0 first.</summary>
    internal static void Stopped(Rig r)
    {
        var s = r.Player.State;
        s.Position = 0;
        r.Player.RaiseState("time-pos"); // between files: still the old path, position gone
        s.IsIdle = true;
        s.Path = null;
        r.Player.RaiseState("path");
        r.Player.RaiseState("idle-active");
    }

    [Fact]
    public void A_file_left_part_way_is_offered_from_there_when_reopened()
    {
        using var r = new Rig();
        var file = r.MediaFile();
        Playing(r, file, 754);
        Assert.Null(r.Vm.ToastActionText); // first time: nothing to resume
        Stopped(r);
        Assert.Equal(754, new ResumePositions(r.Paths.ResumeFile).Get(file)); // on disk, beside the settings

        Playing(r, file, 0.2);
        Assert.True(r.Vm.ToastVisible);
        Assert.Equal("Playing from the start", r.Vm.ToastText);
        Assert.Equal("Resume from 12:34", r.Vm.ToastActionText);
        Assert.DoesNotContain(r.Player.Calls, c => c.StartsWith("SeekAbsolute"));

        r.Vm.RunToastActionCommand.Execute(null);
        Assert.Contains("SeekAbsolute 754", r.Player.Calls);
        Assert.False(r.Vm.ToastVisible);
        Assert.Null(r.Vm.ToastActionText);
    }

    [Fact]
    public void The_offer_lasts_eight_seconds_and_goes_with_its_toast()
    {
        Assert.Equal(TimeSpan.FromSeconds(8), MainViewModel.ResumeOfferDuration);
        using var r = new Rig();
        var file = r.MediaFile();
        Playing(r, file, 100);
        Stopped(r);
        Playing(r, file, 0.5);
        Assert.NotNull(r.Vm.ToastActionText);
        r.Vm.HandleKey("UP"); // the volume's toast takes its place
        Assert.Null(r.Vm.ToastActionText);
        Assert.True(r.Vm.ToastIsVolume);
    }

    [Fact]
    public void Moving_to_the_next_file_remembers_the_one_before()
    {
        using var r = new Rig();
        var (first, second) = (r.MediaFile("01.flac"), r.MediaFile("02.flac"));
        Playing(r, first, 200, 240);
        Playing(r, second, 3, 300); // a folder's next track
        Playing(r, first, 0.1, 240);
        Assert.Equal("Resume from 03:20", r.Vm.ToastActionText);
        Assert.Equal(3, r.Vm.ResumeStore.Get(second));
    }

    [Fact]
    public void Opening_the_same_file_again_counts_where_it_was()
    {
        using var r = new Rig();
        var file = r.MediaFile();
        Playing(r, file, 4000, 7200);
        Playing(r, file, 0.1, 7200); // opened again, straight from the file manager
        Assert.Equal("Resume from 1:06:40", r.Vm.ToastActionText);
    }

    [Fact]
    public void Even_a_finished_file_is_offered()
    {
        using var r = new Rig();
        var file = r.MediaFile();
        Playing(r, file, 2999.5, 3000);
        Stopped(r);
        Playing(r, file, 0.1);
        Assert.Equal("Resume from 49:59", r.Vm.ToastActionText);
    }

    [Fact]
    public void Links_streams_and_discs_are_not_remembered()
    {
        using var r = new Rig();
        Playing(r, "https://example.com/video.mp4", 600);
        Stopped(r);
        Playing(r, "dvd://", 600);
        Stopped(r);
        Assert.Equal(0, r.Vm.ResumeStore.Count);
        Assert.False(File.Exists(r.Paths.ResumeFile));
    }

    [Fact]
    public void Switched_off_nothing_is_remembered_or_offered()
    {
        using var r = new Rig(new PlayerSettings { SettingsVersion = PlayerSettings.CurrentVersion }); // as loaded
        var file = r.MediaFile();
        Playing(r, file, 100);
        Stopped(r);
        r.Vm.ResumePlayback = false;
        Assert.False(new SettingsStore(r.Paths.SettingsFile).Load().ResumePlayback); // saved
        Playing(r, file, 50);
        Assert.Null(r.Vm.ToastActionText);
        Stopped(r);
        Assert.Equal(100, r.Vm.ResumeStore.Get(file)); // left where it was
    }

    [Fact]
    public void Closing_the_app_remembers_the_file_playing()
    {
        using var r = new Rig();
        var file = r.MediaFile();
        Playing(r, file, 42);
        r.Vm.Dispose();
        Assert.Equal(42, new ResumePositions(r.Paths.ResumeFile).Get(file));
    }

    [Fact]
    public void It_is_on_by_default()
    {
        Assert.True(new PlayerSettings().ResumePlayback);
    }

    [AvaloniaFact]
    public void The_toast_takes_the_mouse_only_while_it_offers_something()
    {
        using var r = new Rig();
        var file = r.MediaFile();
        Playing(r, file, 100);
        Stopped(r);
        Playing(r, file, 0.1);
        var toast = new ToastWindow { DataContext = r.Vm };
        toast.Show();
        Dispatcher.UIThread.RunJobs();
        var button = toast.FindControl<Button>("ToastAction")!;
        Assert.True(button.IsVisible);
        Assert.Equal("Resume from 01:40", button.Content);
        Assert.True(toast.FindControl<Border>("Card")!.IsHitTestVisible);

        r.Vm.Toast("Screenshot saved");
        Dispatcher.UIThread.RunJobs();
        Assert.False(button.IsVisible);
        Assert.False(toast.FindControl<Border>("Card")!.IsHitTestVisible); // clicks go nowhere, as before
        toast.Close();
    }

    [AvaloniaFact]
    public void Settings_playback_has_the_switch()
    {
        using var r = new Rig();
        var w = new SettingsWindow { DataContext = r.Vm };
        w.Show();
        Dispatcher.UIThread.RunJobs();
        var toggle = w.FindControl<ToggleSwitch>("ResumePlaybackSwitch")!;
        Assert.True(toggle.IsChecked);
        toggle.IsChecked = false;
        Dispatcher.UIThread.RunJobs();
        Assert.False(r.Settings.ResumePlayback);
        w.Close();
    }
}
