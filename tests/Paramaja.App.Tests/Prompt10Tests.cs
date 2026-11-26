using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Paramaja.App.Controls;
using Paramaja.App.ViewModels;
using Paramaja.App.Views;
using Paramaja.Core.Native;
using Paramaja.Core.Playback;
using Paramaja.Core.Ripping.Dvd;
using Paramaja.Core.Settings;
using Paramaja.Core.Torrent;

namespace Paramaja.App.Tests;

/// <summary>Previous / Next through a folder, the bar's buttons by choice, badges for audio alone (1.12.0).</summary>
public class Prompt10BarTests
{
    private static void Playing(Rig r, bool video = true)
    {
        r.Vm.IsIdle = false;
        r.Player.State.HasVideo = video;
        r.Player.RaiseState("has-video");
    }

    [Fact]
    public void A_folder_of_several_files_gets_previous_and_next()
    {
        using var r = new Rig();
        Playing(r);
        Assert.False(r.Vm.HasPlaylist);
        r.Player.State.PlaylistCount = 3;
        r.Player.RaiseState("playlist-count");
        Assert.True(r.Vm.HasPlaylist);
        Assert.True(r.Vm.ShowFileButtons);
        Assert.Equal("Next file", r.Vm.NextFileTip);
        r.Vm.NextFileCommand.Execute(null);
        r.Vm.PreviousFileCommand.Execute(null);
        Assert.Equal(["PlaylistStep 1", "PlaylistStep -1"], r.Player.Calls.Where(c => c.StartsWith("PlaylistStep")));
        // Stopped: nothing to step through.
        r.Vm.IsIdle = true;
        Assert.False(r.Vm.ShowFileButtons);
    }

    [Fact]
    public void A_single_file_gets_no_previous_or_next()
    {
        using var r = new Rig();
        Playing(r);
        r.Player.State.PlaylistCount = 1;
        r.Player.RaiseState("playlist-count");
        Assert.False(r.Vm.ShowFileButtons);
    }

    [Fact]
    public void Pieces_off_the_bar_stay_off_and_the_rest_show_where_they_apply()
    {
        using var r = new Rig();
        Playing(r);
        Assert.True(r.Vm.OnBar("prevframe"));
        Bar.Off(r.Vm, "prevframe");
        Bar.Off(r.Vm, "nextframe");
        Assert.False(r.Vm.OnBar("prevframe"));
        Assert.DoesNotContain("prevframe", r.Settings.BarCentre);
        Assert.True(r.Vm.PrevFrameInMore);
        Assert.DoesNotContain("\"prevframe\"", File.ReadAllText(r.Store.FilePath)); // saved
        r.Vm.UseEverythingBarCommand.Execute(null);
        Assert.True(r.Vm.OnBar("prevframe"));
        Assert.False(r.Vm.PrevFrameInMore);
        Assert.Equal(1, r.Vm.BarLayoutIndex);
        // On the bar, but shown only where it applies: no frames to step through in music.
        Assert.True(r.Vm.ShowFrameButtons);
        Playing(r, video: false);
        Assert.False(r.Vm.ShowFrameButtons);
    }

    [Fact]
    public void A_songs_cover_gets_no_resolution_codec_or_decoder_badge()
    {
        using var r = new Rig();
        r.Vm.IsIdle = false;
        r.Player.State.VideoWidth = r.Player.State.VideoHeight = 1400; // a FLAC's cover, decoded as a picture
        r.Player.State.VideoFormat = "mjpeg";
        r.Player.State.HwdecCurrent = "no";
        r.Player.RaiseState("video-params/w");
        r.Player.RaiseState("video-format");
        r.Player.RaiseState("hwdec-current");
        Assert.Equal("1440p", r.Vm.ResolutionBadge); // not known yet whether it's a video
        r.Player.State.HasVideo = false;
        r.Player.State.SourceChannels = 2;
        r.Player.RaiseState("has-video");
        r.Player.RaiseState("audio-params/channel-count");
        Assert.Equal("", r.Vm.ResolutionBadge);
        Assert.Equal("", r.Vm.CodecBadge);
        Assert.Equal("", r.Vm.DecoderBadge);
        Assert.Equal("stereo", r.Vm.AudioBadge);
        // A video after it gets its badges back.
        r.Player.State.HasVideo = true;
        r.Player.RaiseState("has-video");
        Assert.Equal("1440p", r.Vm.ResolutionBadge);
        Assert.Equal("SW", r.Vm.DecoderBadge);
    }

    [AvaloniaFact]
    public void The_bar_leaves_off_what_the_settings_take_off()
    {
        using var r = new Rig();
        Playing(r);
        r.Vm.UseEverythingBarCommand.Execute(null);
        var bar = new ControlsBar { DataContext = r.Vm };
        bar.Show();
        Dispatcher.UIThread.RunJobs();
        var stop = bar.FindControl<Button>("StopButton")!;
        var download = bar.FindControl<Button>("DownloadVideoButton")!;
        Assert.True(stop.IsEffectivelyVisible);
        Assert.True(download.IsEffectivelyVisible);
        foreach (var id in new[] { "stop", "download", "badge-codec", "mute", "volume", "volumetext" }) Bar.Off(r.Vm, id);
        Dispatcher.UIThread.RunJobs();
        Assert.False(stop.IsEffectivelyVisible);
        Assert.False(download.IsEffectivelyVisible);
        Assert.Null(bar.GroupOf("stop"));
        Assert.False(bar.FindControl<Border>("CodecBadge")!.IsEffectivelyVisible);
        Assert.False(bar.FindControl<Slider>("VolumeSlider")!.IsEffectivelyVisible);
        Assert.True(bar.FindControl<Button>("PlayButton")!.IsEffectivelyVisible); // always
        Assert.True(bar.FindControl<Button>("SettingsButton")!.IsEffectivelyVisible);
        r.Player.State.PlaylistCount = 4;
        r.Player.RaiseState("playlist-count");
        Dispatcher.UIThread.RunJobs();
        Assert.True(bar.FindControl<Button>("NextFileButton")!.IsEffectivelyVisible);
        bar.Close();
    }
}

/// <summary>Settings carried to another install, subtitle looks, the Interface tab (1.12.0).</summary>
public class Prompt10SettingsTests
{
    private static string Exported(Action<PlayerSettings> set)
    {
        var s = new PlayerSettings();
        set(s);
        s.Normalize();
        var file = Path.Combine(Path.GetTempPath(), "paramaja-app-tests", Guid.NewGuid().ToString("N"), SettingsStore.ExportName);
        SettingsStore.Export(s, file);
        return file;
    }

    [Fact]
    public async Task A_dropped_settings_file_is_taken_over_once_agreed()
    {
        using var r = new Rig();
        r.Settings.DisplayFps = 100;
        var file = Exported(s => { s.SeekSeconds = 42; s.SubtitleColor = "#FFE600"; s.BarRight.Remove("download"); s.BarRight.Insert(0, "download"); s.DisplayFps = 144; });

        r.Dialogs.Confirm = false;
        await r.Vm.DropAsync([file], null);
        Assert.Single(r.Dialogs.Questions);
        Assert.Equal(5, r.Settings.SeekSeconds); // said no
        Assert.DoesNotContain(r.Player.Calls, c => c.StartsWith("Open"));

        r.Dialogs.Confirm = true;
        await r.Vm.DropAsync([file], null);
        Assert.Equal(42, r.Settings.SeekSeconds);
        Assert.Equal(42, r.Vm.SeekSeconds);
        Assert.Equal("#FFE600", r.Vm.SubtitleColor);
        Assert.Equal("download", r.Vm.BarZones[2].Slots[0].Id);
        Assert.Equal("download", r.Settings.BarRight[0]);
        Assert.Equal(100, r.Settings.DisplayFps); // this machine's
        Assert.Contains("ApplySubtitles", r.Player.Calls);
        Assert.Contains("\"seekSeconds\": 42", File.ReadAllText(r.Store.FilePath)); // saved here
        Assert.DoesNotContain(r.Player.Calls, c => c.StartsWith("Open"));
    }

    [Fact]
    public async Task Imported_from_settings_it_isnt_asked_and_a_bad_file_changes_nothing()
    {
        using var r = new Rig();
        Assert.True(await r.Vm.ImportSettingsAsync(Exported(s => s.SeekSeconds = 9), ask: false));
        Assert.Empty(r.Dialogs.Questions);
        Assert.Equal(9, r.Settings.SeekSeconds);

        var bad = Path.Combine(Path.GetTempPath(), "paramaja-app-tests", Guid.NewGuid().ToString("N") + ".json");
        Directory.CreateDirectory(Path.GetDirectoryName(bad)!);
        File.WriteAllText(bad, """{"settingsVersion": 4, "seekSeconds": "many"}""");
        Assert.False(await r.Vm.ImportSettingsAsync(bad, ask: false));
        Assert.Equal(9, r.Settings.SeekSeconds);
        Assert.Contains("isn't a Paramaja settings file", r.Vm.SettingsFileStatus);
    }

    [Fact]
    public void Exported_settings_read_back_the_same()
    {
        using var r = new Rig();
        r.Vm.SubtitleBold = true;
        var file = Path.Combine(r.Root, "out", SettingsStore.ExportName);
        Assert.True(r.Vm.ExportSettings(file));
        Assert.True(SettingsStore.ReadFile(file)!.SubtitleBold);
        Assert.Contains(file, r.Vm.SettingsFileStatus);
    }

    [Fact]
    public void The_subtitle_look_applies_at_once_and_resets()
    {
        using var r = new Rig();
        r.Vm.SubtitleColorChoice = MainViewModel.SubtitleColors.Single(c => c.Name == "Yellow");
        Assert.Equal("#FFE600", r.Settings.SubtitleColor);
        Assert.Contains("ApplySubtitles", r.Player.Calls);
        r.Vm.SubtitleColor = "not a colour"; // typed half-way: ignored
        Assert.Equal("#FFE600", r.Settings.SubtitleColor);
        r.Vm.SubtitleColor = "#123abc";
        Assert.Equal("#123ABC", r.Settings.SubtitleColor);
        Assert.Null(r.Vm.SubtitleColorChoice); // a custom one
        r.Vm.SubtitleFontChoice = MainViewModel.DefaultFontLabel;
        Assert.Equal("", r.Settings.SubtitleFont);
        r.Vm.SubtitleRaise = 10;
        Assert.Equal(90, r.Settings.SubtitlePosition);
        r.Vm.SubtitleBackground = true;
        r.Vm.ResetSubtitleStyleCommand.Execute(null);
        Assert.False(r.Settings.SubtitleBackground);
        Assert.Equal(100, r.Settings.SubtitlePosition);
        Assert.Equal(PlayerSettings.DefaultSubtitleColor, r.Vm.SubtitleColor);
        Assert.Equal(MainViewModel.DefaultFontLabel, r.Vm.SubtitleFonts[0]);
    }

    [AvaloniaFact]
    public void The_interface_tab_shows_every_piece_of_the_bar_and_the_settings_file()
    {
        using var r = new Rig();
        var w = new SettingsWindow { DataContext = r.Vm };
        w.Show();
        w.GetLogicalDescendants().OfType<TabControl>().Single().SelectedItem = w.FindControl<TabItem>("InterfaceTab");
        Dispatcher.UIThread.RunJobs();
        var chips = w.FindControl<BarEditor>("BarEditor")!.GetVisualDescendants().OfType<Border>()
            .Where(b => b.Classes.Contains("bar-chip") && b.DataContext is BarSlot).ToList();
        Assert.Equal(BarItems.Simple.All.Count() + r.Vm.BarOffZone.Slots.Count, chips.Count);
        Assert.Equal(BarItems.All.Count, chips.Select(c => ((BarSlot)c.DataContext!).Id).Distinct().Count()); // every piece is there
        Assert.NotNull(w.FindControl<Button>("ExportSettingsButton"));
        Assert.NotNull(w.FindControl<Button>("ImportSettingsButton"));
        w.Close();
    }

    [AvaloniaFact]
    public void The_subtitles_tab_previews_the_look()
    {
        using var r = new Rig();
        var w = new SettingsWindow { DataContext = r.Vm };
        w.Show();
        w.GetLogicalDescendants().OfType<TabControl>().Single().SelectedItem = w.FindControl<TabItem>("SubtitlesTab");
        Dispatcher.UIThread.RunJobs();
        var preview = w.FindControl<SubtitlePreview>("SubtitlePreview")!;
        Assert.Equal("#FFFFFF", preview.TextColor);
        r.Vm.SubtitleColor = "#FFE600";
        r.Vm.SubtitleBackground = true;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("#FFE600", preview.TextColor);
        Assert.True(preview.HasBox);
        Assert.NotNull(w.FindControl<ToggleSwitch>("SubtitleStyleAss"));
        w.Close();
    }
}

/// <summary>The MEGA app's links read ahead; the graphics driver check; DVD chapters in the Rip window (1.12.0).</summary>
public class Prompt10MiscTests
{
    [Fact]
    public async Task A_mega_app_link_the_engine_cant_read_ahead_plays_directly()
    {
        using var r = new Rig();
        r.Torrents.OnReadAhead = _ => null;
        const string link = "http://127.0.0.1:4443/HEpEyLrD/Michael%20%282026%29.mkv";
        await r.Vm.OpenInputAsync(link);
        Assert.Contains($"ReadAhead {link}", r.Torrents.Calls);
        Assert.Contains($"Open {link} Michael (2026).mkv [stream]", r.Player.Calls);
        Assert.False(r.Vm.CanDownload); // nothing else ever reads the link
    }

    [Fact]
    public async Task A_missing_driver_extension_is_told_with_the_command_to_copy()
    {
        using var r = new Rig();
        var problem = new GraphicsProblem("Your NVIDIA driver is 590.44.01, but…", "flatpak install flathub org.freedesktop.Platform.GL.nvidia-590-44-01");
        await r.Vm.CheckGraphicsAsync(() => problem);
        Assert.Contains(r.Dialogs.Questions, q => q.Contains(problem.Command) && q.Contains("590.44.01"));
        Assert.Equal(problem.Command, r.Dialogs.Clipboard2);
        r.Dialogs.Questions.Clear();
        await r.Vm.CheckGraphicsAsync(() => null);
        Assert.Empty(r.Dialogs.Questions);
    }

    private static RipDialogViewModel Dialog(Rig r) =>
        new(r.Vm, [RipDialogTests.Dvd], RipDialogTests.Dvd.Root, _ => RipDialogTests.CdDrive(), _ => new DvdInfo
        {
            Name = "Concert",
            Titles =
            [
                new DvdTitle(1, 1, 1, 1, TimeSpan.FromMinutes(2), [], []),
                new DvdTitle(2, 1, 4, 1, TimeSpan.FromMinutes(40), [], [])
                {
                    ChapterLengths = [TimeSpan.FromMinutes(11), TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(7), TimeSpan.FromMinutes(12)],
                },
            ],
            Files = [("VIDEO_TS.IFO", 2048L)],
        }, flac: "", ffmpeg: "/usr/bin/ffmpeg", online: false, ffprobe: "/usr/bin/ffprobe", encoders: new HashSet<string> { "libx264" });

    [Fact]
    public void The_rip_window_lists_the_titles_chapters_all_ticked()
    {
        using var r = new Rig();
        var vm = Dialog(r);
        Assert.Equal(2, vm.SelectedTitle!.Number);
        Assert.True(vm.ShowChapters);
        Assert.Equal(["Chapter 1 · 11:00", "Chapter 2 · 10:00", "Chapter 3 · 7:00", "Chapter 4 · 12:00"], vm.Chapters.Select(c => c.Label));
        Assert.Equal("All 4 chapters · 40:00", vm.ChaptersSummary);
        Assert.Null(vm.DvdOptions()!.Chapters); // the whole title, read as always

        vm.Chapters[1].IsChecked = false;
        Assert.Equal("3 of 4 chapters · 30:00", vm.ChaptersSummary);
        Assert.Equal([1, 3, 4], vm.DvdOptions()!.Chapters);
        Assert.False(vm.DvdOptions()!.ChapterFiles);
        vm.ChapterFiles = true;
        Assert.True(vm.DvdOptions()!.ChapterFiles);
        Assert.EndsWith("as 3 files", vm.ChaptersSummary);

        vm.NoChaptersCommand.Execute(null);
        Assert.False(vm.StartCommand.CanExecute(null));
        vm.AllChaptersCommand.Execute(null);
        Assert.True(vm.StartCommand.CanExecute(null));
        Assert.Equal([1, 2, 3, 4], vm.DvdOptions()!.Chapters); // every one, as its own file

        // A title of one chapter has nothing to choose; nor has the VIDEO_TS copy (no titles step at all).
        vm.SelectedTitleRow = vm.TitleRows[0];
        Assert.False(vm.ShowChapters);
        vm.SelectedTitleRow = vm.TitleRows[1];
        vm.DvdModeIndex = 2;
        Assert.DoesNotContain(RipStep.DvdTitles, vm.Steps);
        Assert.Equal(DvdRipMode.Decrypt, vm.DvdOptions()!.Mode);
        Assert.Null(vm.DvdOptions()!.Chapters);
    }
}
