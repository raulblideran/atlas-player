using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using AtlasPlayer.App.Services;
using AtlasPlayer.App.ViewModels;
using AtlasPlayer.App.Views;
using AtlasPlayer.Core.Ripping.Cd;
using AtlasPlayer.Core.Ripping.Dvd;

namespace AtlasPlayer.App.Tests;

/// <summary>
/// Prompt 11: fullscreen's bars hide after 2 s, the welcome screen's lines, and the rip window as
/// steps: a CD's (tracks, format, cue sheet) and a DVD's (MKV, audio only or the VIDEO_TS folder;
/// titles, chapters and audio tracks; format).
/// </summary>
public class Prompt11Tests
{
    [Fact]
    public void In_fullscreen_the_bars_hide_after_2_seconds_in_a_window_after_2_and_a_half()
    {
        using var r = new Rig();
        r.Vm.IsIdle = false;
        var start = DateTime.UtcNow;
        r.Vm.IsFullscreen = true;
        r.Vm.NotifyActivity();
        r.Vm.TickAutoHide(start + TimeSpan.FromSeconds(1.8));
        Assert.True(r.Vm.ControlsVisible);
        r.Vm.TickAutoHide(DateTime.UtcNow + TimeSpan.FromSeconds(2.1));
        Assert.False(r.Vm.ControlsVisible);
        Assert.Equal(TimeSpan.FromSeconds(2), MainViewModel.CursorHideDelay);

        r.Vm.IsFullscreen = false;
        r.Vm.NotifyActivity();
        r.Vm.TickAutoHide(DateTime.UtcNow + TimeSpan.FromSeconds(2.1));
        Assert.True(r.Vm.ControlsVisible);
        r.Vm.TickAutoHide(DateTime.UtcNow + TimeSpan.FromSeconds(2.7));
        Assert.False(r.Vm.ControlsVisible);
    }

    [AvaloniaFact]
    public void The_welcome_screen_says_what_open_disc_and_rip_disc_take()
    {
        using var r = new Rig(attach: false);
        var w = new MainWindow { DataContext = r.Vm };
        w.Show();
        var texts = w.FindControl<Grid>("Welcome")!.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text).ToList();
        Assert.Equal("CD, DVD, BluRay", texts[texts.IndexOf("Open disc…") + 1]);
        Assert.Equal("CD, DVD, Audio from DVD", texts[texts.IndexOf("Rip disc…") + 1]);
        w.Close();
    }

    private static readonly OpticalDrive CdDrive = new("/dev/sr0", "sr0", "Audio CD");

    private static async Task<RipDialogViewModel> Cd(Rig r, bool qtgmc = false)
    {
        var vm = new RipDialogViewModel(r.Vm, [CdDrive], CdDrive.Root, _ => RipDialogTests.CdDrive(), flac: "", online: false,
            ffmpeg: "/usr/bin/ffmpeg", ffprobe: "/usr/bin/ffprobe", encoders: new HashSet<string>(), qtgmc: (null, qtgmc));
        for (var i = 0; i < 400 && !vm.IsCd; i++) await Task.Delay(25);
        Assert.True(vm.IsCd);
        return vm;
    }

    /// <summary>Two titles: a short extra, and the concert with three chapters, LPCM stereo and AC-3 5.1.</summary>
    internal static DvdInfo Concert() => new()
    {
        Name = "The One",
        Titles =
        [
            new DvdTitle(1, 1, 1, 1, TimeSpan.FromMinutes(3), [new DvdAudio("AC-3", 2, "en") { Number = 0 }], []),
            new DvdTitle(2, 2, 3, 1, TimeSpan.FromMinutes(70),
                [new DvdAudio("LPCM", 2, "en") { Number = 0 }, new DvdAudio("AC-3", 6, "en") { Number = 1 }], ["en"])
            {
                ChapterLengths = [TimeSpan.FromMinutes(20), TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(20)],
            },
        ],
        Files = [("VIDEO_TS.IFO", 2048L)],
    };

    private static RipDialogViewModel Dvd(Rig r, bool qtgmc = false) =>
        new(r.Vm, [RipDialogTests.Dvd], RipDialogTests.Dvd.Root, _ => RipDialogTests.CdDrive(), _ => Concert(), flac: "",
            ffmpeg: "/usr/bin/ffmpeg", online: false, ffprobe: "/usr/bin/ffprobe", encoders: new HashSet<string> { "libx264", "libx265" },
            qtgmc: (qtgmc ? new QtgmcRuntime("/usr/bin/python3", "/vs/python", "/vs/plugins") : null, qtgmc));

    [Fact]
    public async Task A_cd_goes_source_tracks_format_cue_sheet_rip()
    {
        using var r = new Rig();
        var vm = await Cd(r);
        Assert.Equal([RipStep.Source, RipStep.CdTracks, RipStep.CdFormat, RipStep.CdCue, RipStep.Rip], vm.Steps);
        Assert.Equal(RipStep.Source, vm.CurrentStep);
        Assert.False(vm.BackCommand.CanExecute(null));
        Assert.Equal(["1  Source", "2  Tracks", "3  Format", "4  Cue sheet", "5  Rip"], vm.StepItems.Select(s => s.Text));
        vm.NextCommand.Execute(null);
        Assert.True(vm.OnCdTracks);
        Assert.True(vm.StepItems[0].IsDone);
        // No track ticked: no going on.
        vm.NoTracksCommand.Execute(null);
        Assert.False(vm.NextCommand.CanExecute(null));
        vm.AllTracksCommand.Execute(null);
        vm.NextCommand.Execute(null);
        Assert.True(vm.OnCdFormat);
        vm.NextCommand.Execute(null);
        Assert.True(vm.OnCdCue);
        Assert.True(vm.WriteCue); // a whole disc
        vm.NextCommand.Execute(null);
        Assert.True(vm.IsLastStep);
        Assert.False(vm.ShowNext);
        Assert.Contains("with a cue sheet", vm.RipSummary);
        Assert.Equal(r.Paths.Music, vm.SaveFolder);
        vm.BackCommand.Execute(null);
        Assert.True(vm.OnCdCue);
        vm.Dispose();
    }

    [Fact]
    public async Task A_single_track_gets_no_cue_sheet_unless_asked_and_the_log_scores_100()
    {
        using var r = new Rig();
        var vm = await Cd(r);
        vm.NoTracksCommand.Execute(null);
        vm.Tracks[1].IsChecked = true;
        Assert.False(vm.WriteCue);
        vm.Tracks[0].IsChecked = true;
        Assert.False(vm.WriteCue);
        vm.Tracks[2].IsChecked = true;
        Assert.True(vm.WriteCue); // the whole disc again
        // Set by hand, it stays.
        vm.WriteCue = false;
        vm.Tracks[0].IsChecked = false;
        vm.Tracks[0].IsChecked = true;
        Assert.False(vm.WriteCue);
        vm.Tracks[0].IsChecked = false;
        vm.Tracks[2].IsChecked = false;

        vm.StartCommand.Execute(null);
        for (var i = 0; i < 400 && (r.Vm.IsRipping || r.Vm.RipResult.Length == 0); i++) await Task.Delay(25);
        Assert.StartsWith("Ripped track 2 of 3", r.Vm.RipResult);
        var folder = r.Vm.LastRipFolder!;
        Assert.Empty(Directory.GetFiles(folder, "*.cue"));
        var log = File.ReadAllText(Assert.Single(Directory.GetFiles(folder, "*.log")));
        Assert.Contains("Selected tracks 2 (no cue sheet)", log);
        vm.Dispose();

        // The cue sheet costs the log nothing: the same rip with one scores the same (a drive that
        // reads well scores 100 without one: Prompt11CoreTests).
        var score = System.Text.RegularExpressions.Regex.Match(r.Vm.RipResult, @"Log score (\d+)%").Groups[1].Value;
        var again = await Cd(r);
        again.NoTracksCommand.Execute(null);
        again.Tracks[1].IsChecked = true;
        again.WriteCue = true;
        again.StartCommand.Execute(null);
        await Task.Delay(50);
        for (var i = 0; i < 400 && (r.Vm.IsRipping || !r.Vm.RipResult.StartsWith("Ripped", StringComparison.Ordinal)); i++) await Task.Delay(25);
        Assert.Equal(score, System.Text.RegularExpressions.Regex.Match(r.Vm.RipResult, @"Log score (\d+)%").Groups[1].Value);
        again.Dispose();
    }

    [Fact]
    public async Task A_few_tracks_can_have_a_cue_sheet_of_their_own()
    {
        using var r = new Rig();
        var vm = await Cd(r);
        vm.Tracks[2].IsChecked = false;
        vm.WriteCue = true;
        vm.StartCommand.Execute(null);
        for (var i = 0; i < 400 && (r.Vm.IsRipping || r.Vm.RipResult.Length == 0); i++) await Task.Delay(25);
        var cue = File.ReadAllText(Assert.Single(Directory.GetFiles(r.Vm.LastRipFolder!, "*.cue")));
        Assert.Contains("TRACK 01", cue);
        Assert.Contains("TRACK 02", cue);
        Assert.DoesNotContain("TRACK 03", cue);
        Assert.Contains("(cue sheet of these tracks)", File.ReadAllText(Directory.GetFiles(r.Vm.LastRipFolder!, "*.log").Single()));
        vm.Dispose();
    }

    [Fact]
    public void A_dvds_steps_follow_what_its_ripped_to()
    {
        using var r = new Rig();
        var vm = Dvd(r);
        Assert.True(vm.IsDvd);
        Assert.Equal([RipStep.Source, RipStep.DvdMode, RipStep.DvdTitles, RipStep.DvdFormat, RipStep.Rip], vm.Steps);
        Assert.True(vm.IsMkv);
        vm.DvdModeIndex = 1;
        Assert.True(vm.IsAudioOnly);
        Assert.Equal(5, vm.Steps.Count);
        Assert.Equal("One file per title", vm.SplitOneLabel);
        vm.DvdModeIndex = 2;
        Assert.Equal([RipStep.Source, RipStep.DvdMode, RipStep.Rip], vm.Steps);
        Assert.Equal(DvdRipMode.Decrypt, Assert.Single(vm.DvdJobs()!).Mode);
        // From the mode step, the backup goes straight to the rip.
        vm.StepIndex = 1;
        vm.NextCommand.Execute(null);
        Assert.True(vm.OnRip);
        Assert.Contains("VIDEO_TS", vm.RipSummary);
        Assert.Equal(r.Paths.Videos, vm.SaveFolder);
        vm.DvdModeIndex = 1;
        Assert.Equal(r.Paths.Music, vm.SaveFolder); // audio goes to Music
    }

    [Fact]
    public void Titles_are_ticked_with_their_chapters_and_audio_tracks()
    {
        using var r = new Rig();
        var vm = Dvd(r);
        Assert.Equal([false, true], vm.TitleRows.Select(t => t.IsChecked)); // the longest
        Assert.Equal(2, vm.SelectedTitle!.Number);
        Assert.Equal(["English, LPCM, stereo", "English, AC-3, 5.1"], vm.AudioTracks.Select(a => a.Label));
        Assert.All(vm.AudioTracks, a => Assert.True(a.IsChecked));
        Assert.Null(vm.DvdOptions()!.AudioTracks); // every one, as always

        vm.StepIndex = 2;
        Assert.True(vm.OnDvdTitles);
        vm.AudioTracks[0].IsChecked = false;
        Assert.Equal([1], vm.DvdOptions()!.AudioTracks);
        vm.AudioTracks[1].IsChecked = false;
        Assert.False(vm.NextCommand.CanExecute(null)); // at least one track
        Assert.False(vm.StartCommand.CanExecute(null));
        Assert.Equal("Tick an audio track", vm.TitleRows[1].Summary);
        vm.AudioTracks[1].IsChecked = true;
        Assert.True(vm.NextCommand.CanExecute(null));

        // Both titles, each its own job; the first's chapter and track as they are.
        vm.TitleRows[0].IsChecked = true;
        vm.Chapters[0].IsChecked = false;
        var jobs = vm.DvdJobs()!;
        Assert.Equal([1, 2], jobs.Select(j => j.Title));
        Assert.Null(jobs[0].Chapters);
        Assert.Equal([2, 3], jobs[1].Chapters);
        Assert.Equal([1], jobs[1].AudioTracks);
        Assert.Contains("Title 1: the whole title", vm.RipSummary);

        foreach (var t in vm.TitleRows) t.IsChecked = false;
        Assert.False(vm.NextCommand.CanExecute(null)); // at least one title
        Assert.Equal("No titles ticked", vm.TitlesSummary);
    }

    [Fact]
    public void Audio_only_is_flac_or_wav_by_title_or_chapter_with_every_channel()
    {
        using var r = new Rig();
        var vm = Dvd(r);
        vm.DvdModeIndex = 1;
        var job = vm.DvdOptions()!;
        Assert.Equal(DvdRipMode.AudioOnly, job.Mode);
        Assert.Equal(AudioFileFormat.Flac, job.AudioFormat);
        Assert.False(job.Downmix);
        Assert.Equal(r.Paths.Music, job.OutputRoot);
        Assert.Contains("keeps all its channels", vm.AudioNote);
        vm.AudioFormatIndex = 1;
        vm.ChapterFiles = true;
        job = vm.DvdOptions()!;
        Assert.Equal(AudioFileFormat.Wav, job.AudioFormat);
        Assert.True(job.ChapterFiles);
        Assert.Equal([1, 2, 3], job.Chapters);
        Assert.True(vm.CanDownmix);
        vm.Downmix = true;
        Assert.True(vm.DvdOptions()!.Downmix);
        Assert.Contains("mixed down to stereo", vm.RipSummary);
    }

    [Fact]
    public void An_mkvs_audio_is_as_on_the_disc_flac_or_aac_and_mixed_down_only_when_converted()
    {
        using var r = new Rig();
        var vm = Dvd(r);
        Assert.Equal(DvdAudioCodec.Original, vm.DvdOptions()!.AudioCodec);
        Assert.False(vm.CanDownmix); // a copied AC-3 can't be
        vm.Downmix = true;
        Assert.False(vm.DvdOptions()!.Downmix);
        vm.AudioCodecIndex = 2;
        Assert.True(vm.CanDownmix);
        Assert.Equal(DvdAudioCodec.Aac, vm.DvdOptions()!.AudioCodec);
        Assert.True(vm.DvdOptions()!.Downmix);
        vm.AudioCodecIndex = 1;
        vm.Downmix = false;
        Assert.Equal(DvdAudioCodec.Flac, vm.DvdOptions()!.AudioCodec);
        Assert.Contains("5.1 stays 5.1", vm.AudioNote);
    }

    [Fact]
    public void Encoded_video_is_deinterlaced_by_bwdif_or_qtgmc_at_double_or_the_discs_rate()
    {
        using var r = new Rig();
        var vm = Dvd(r);
        Assert.False(vm.ShowDeinterlacing); // a copy keeps its fields
        vm.DvdVideoIndex = 1;
        Assert.True(vm.ShowDeinterlacing);
        var job = vm.DvdOptions()!;
        Assert.Equal(Deinterlacer.Bwdif, job.Deinterlacer);
        Assert.True(job.DoubleRate);
        vm.FrameRateIndex = 1;
        Assert.False(vm.DvdOptions()!.DoubleRate);
        Assert.Contains("disc's own rate", vm.MkvNote);
        // QTGMC isn't there: chosen anyway, the format step can't go on.
        Assert.False(vm.HasQtgmc);
        vm.StepIndex = 3;
        vm.DeinterlacerIndex = 1;
        Assert.False(vm.NextCommand.CanExecute(null));
        Assert.False(vm.StartCommand.CanExecute(null));
        Assert.Contains("isn't there", vm.QtgmcTip);
    }

    [Fact]
    public void Qtgmc_takes_its_preset_slower_to_begin_with()
    {
        using var r = new Rig();
        var vm = Dvd(r, qtgmc: true);
        vm.DvdVideoIndex = 2;
        vm.DeinterlacerIndex = 1;
        Assert.True(vm.HasQtgmc);
        var job = vm.DvdOptions()!;
        Assert.Equal(Deinterlacer.Qtgmc, job.Deinterlacer);
        Assert.Equal("Slower", job.QtgmcPreset);
        Assert.NotNull(job.QtgmcRuntime);
        vm.QtgmcPresetIndex = 4;
        Assert.Equal("Placebo", vm.DvdOptions()!.QtgmcPreset);
        Assert.Contains("QTGMC (Placebo)", vm.MkvNote);
        vm.StepIndex = 3;
        Assert.True(vm.NextCommand.CanExecute(null));
    }

    [AvaloniaFact]
    public void Each_step_shows_only_its_own_part()
    {
        using var r = new Rig();
        var vm = Dvd(r);
        var w = new RipDialog { DataContext = vm };
        w.Show();
        string[] parts = ["SourceStep", "DvdModeStep", "DvdTitlesStep", "DvdFormatStep", "RipStep"];
        for (var step = 0; step < vm.Steps.Count; step++)
        {
            vm.StepIndex = step;
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            Assert.Equal(parts[step], Assert.Single(parts, p => w.FindControl<Control>(p)!.IsVisible));
            Assert.Equal(step == vm.Steps.Count - 1, w.FindControl<Button>("StartButton")!.IsVisible);
            Assert.Equal(step > 0, w.FindControl<Button>("BackButton")!.IsVisible);
        }
        Assert.NotNull(w.CaptureRenderedFrame());
        w.Close();
    }
}
