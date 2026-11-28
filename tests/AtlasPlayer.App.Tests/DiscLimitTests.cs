using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using AtlasPlayer.App.ViewModels;
using AtlasPlayer.App.Views;

namespace AtlasPlayer.App.Tests;

/// <summary>On a disc with its menus (libVLC), what only mpv does is greyed out and says why (2.0.0-beta.14).</summary>
public class DiscLimitTests
{
    private static async Task<Rig> PlayingADvd()
    {
        var r = new Rig(discEngine: true);
        var dvd = Path.Combine(r.Root, "DISC", "VIDEO_TS");
        Directory.CreateDirectory(dvd);
        File.WriteAllText(Path.Combine(dvd, "VIDEO_TS.IFO"), "");
        await r.Vm.OpenInputAsync(Path.Combine(r.Root, "DISC"));
        Assert.True(r.Vm.IsDiscMode);
        return r;
    }

    [Fact]
    public async Task Previous_frame_statistics_and_the_subtitle_look_are_greyed_out_on_discs()
    {
        using var r = await PlayingADvd();
        Assert.False(r.Vm.EngineHasMpvFeatures);
        Assert.Equal(MainViewModel.NotOnDiscs, r.Vm.DiscLimitTip);
        Assert.Equal("Previous frame: not available while a disc plays with its menus", r.Vm.FrameBackTip);
        Assert.Equal(MainViewModel.NotOnDiscs, r.Vm.SubtitleSizeTip);

        // Their keys say so instead of doing nothing.
        r.Vm.HandleKey(",");
        Assert.Equal(MainViewModel.NotOnDiscs, r.Vm.ToastText);
        r.Vm.HandleKey("i");
        Assert.DoesNotContain("FrameBackStep", r.DiscPlayer!.Calls);
        Assert.DoesNotContain("ToggleStats", r.DiscPlayer.Calls);
    }

    [Fact]
    public async Task With_mpv_they_work_as_before()
    {
        using var r = new Rig();
        await r.Vm.OpenInputAsync(r.MediaFile());
        Assert.True(r.Vm.EngineHasMpvFeatures);
        Assert.Null(r.Vm.DiscLimitTip);
        Assert.Equal("Previous frame ( , )", r.Vm.FrameBackTip);
        Assert.Equal(r.Vm.SubtitleSizeText, r.Vm.SubtitleSizeTip);
        r.Vm.HandleKey(",");
        Assert.Contains("FrameBackStep", r.Player.Calls);
    }

    [AvaloniaFact]
    public async Task The_bar_button_and_the_settings_follow()
    {
        using var r = await PlayingADvd();
        var bar = new ControlsBar { DataContext = r.Vm };
        bar.Show();
        var settings = new SettingsWindow { DataContext = r.Vm };
        settings.Show();
        Dispatcher.UIThread.RunJobs();
        var back = bar.FindControl<Button>("FrameBackButton")!;
        Assert.False(back.IsEnabled);
        Assert.True(ToolTip.GetShowOnDisabled(back));
        Assert.False(settings.FindControl<Border>("SubtitleLook")!.IsEnabled);
        bar.Close();
        settings.Close();
    }
}
