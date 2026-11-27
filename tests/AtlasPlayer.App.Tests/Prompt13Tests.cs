using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AtlasPlayer.App.Controls;
using AtlasPlayer.App.Views;
using AtlasPlayer.Core.Settings;

namespace AtlasPlayer.App.Tests;

/// <summary>
/// Prompt 13: normalization on at -3 dB by default; a CD rip folder with a log, a cue sheet and
/// pictures plays its music (it said "Can't play this: no audio or video data played"), with
/// cover.jpg or front.jpg as the picture.
/// </summary>
public class Prompt13Tests
{
    private static void Dispatch() => Dispatcher.UIThread.RunJobs();

    /// <summary>The Chinese Democracy rip: per-track FLACs, the log, a cue for the disc's one image file, the cover, scans.</summary>
    private static string RipFolder(Rig r)
    {
        var dir = Path.Combine(r.Root, "2008 - Chinese Democracy");
        Directory.CreateDirectory(Path.Combine(dir, "Covers"));
        foreach (var name in new[] { "01. Chinese Democracy.flac", "02. Shackler's Revenge.flac", "10. Riad N' The Bedouins.flac",
                     "GunsNRoses_ChineseDemocracy.cue", "GunsNRoses_ChineseDemocracy.log", "GunsNRoses_ChineseDemocracy.jpg",
                     "02. Shackler's Revenge.flac.sfk", "Covers/Front.jpg" })
            File.WriteAllText(Path.Combine(dir, name), "");
        File.WriteAllText(Path.Combine(dir, "GunsNRoses_ChineseDemocracy.cue"),
            "TITLE \"Chinese Democracy\"\r\nFILE \"GunsNRoses_ChineseDemocracy1.flac\" WAVE\r\n  TRACK 01 AUDIO\r\n    INDEX 01 00:00:00\r\n" +
            "  TRACK 02 AUDIO\r\n    INDEX 01 04:43:00\r\n");
        return dir;
    }

    [Fact]
    public async Task A_rip_folder_plays_its_music_never_its_cue_sheet()
    {
        using var r = new Rig();
        var dir = RipFolder(r);
        await r.Vm.OpenInputAsync(dir);
        Assert.Equal([
            $"Open {Path.Combine(dir, "01. Chinese Democracy.flac")}",
            $"Append {Path.Combine(dir, "02. Shackler's Revenge.flac")}",
            $"Append {Path.Combine(dir, "10. Riad N' The Bedouins.flac")}",
        ], r.Player.Calls.Where(c => c.StartsWith("Open ") || c.StartsWith("Append ")));
        Assert.DoesNotContain(r.Player.Calls, c => c.Contains(".cue") || c.Contains(".log") || c.Contains(".jpg"));
    }

    [Fact]
    public async Task The_cue_sheet_opened_itself_plays_the_folder_when_it_names_none_of_its_files()
    {
        using var r = new Rig();
        var dir = RipFolder(r);
        await r.Vm.OpenInputAsync(Path.Combine(dir, "GunsNRoses_ChineseDemocracy.cue"));
        Assert.Contains($"Open {Path.Combine(dir, "01. Chinese Democracy.flac")}", r.Player.Calls);
        Assert.DoesNotContain(r.Player.Calls, c => c.StartsWith("Open ") && c.Contains(".cue"));
    }

    [Fact]
    public async Task A_cue_sheets_tracks_play_with_the_folders_cover()
    {
        using var r = new Rig();
        var dir = Path.Combine(r.Root, "Dangerous");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "01 - Jam.flac"), "");
        File.WriteAllText(Path.Combine(dir, "cover.jpg"), "");
        File.WriteAllText(Path.Combine(dir, "Dangerous.log"), "");
        File.WriteAllText(Path.Combine(dir, "Dangerous.cue"), "TITLE \"Dangerous\"\nFILE \"01 - Jam.wav\" WAVE\n  TRACK 01 AUDIO\n    TITLE \"Jam\"\n    INDEX 01 00:00:00\n");
        await r.Vm.OpenInputAsync(dir);
        var open = Assert.Single(r.Player.Calls, c => c.StartsWith("Open "));
        Assert.StartsWith("Open edl://", open);
        Assert.EndsWith($"Dangerous [cover {Path.Combine(dir, "cover.jpg")}]", open);
    }

    [Fact]
    public async Task A_folder_with_nothing_to_play_says_so()
    {
        using var r = new Rig();
        var dir = Path.Combine(r.Root, "Scans");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "Booklet.jpg"), "");
        File.WriteAllText(Path.Combine(dir, "notes.txt"), "");
        await r.Vm.OpenInputAsync(dir);
        Assert.DoesNotContain(r.Player.Calls, c => c.StartsWith("Open "));
        Assert.Contains("Nothing in this folder can be played", r.Vm.StatusMessage);
    }

    [Fact]
    public async Task A_rip_folders_files_dropped_together_play_its_music_only()
    {
        using var r = new Rig();
        var dir = RipFolder(r);
        var dropped = Directory.GetFiles(dir).Order(StringComparer.Ordinal).Reverse().ToList(); // whatever order the file manager gives
        dropped.Insert(0, Path.Combine(dir, "GunsNRoses_ChineseDemocracy.log"));
        await r.Vm.DropAsync(dropped.Distinct().ToList(), null);
        // In track order, whatever order they came in.
        Assert.Equal([
            $"Open {Path.Combine(dir, "01. Chinese Democracy.flac")}",
            $"Append {Path.Combine(dir, "02. Shackler's Revenge.flac")}",
            $"Append {Path.Combine(dir, "10. Riad N' The Bedouins.flac")}",
        ], r.Player.Calls.Where(c => c.StartsWith("Open ") || c.StartsWith("Append ")));
    }

    [Fact]
    public async Task A_dropped_folder_after_the_first_queues_its_music()
    {
        using var r = new Rig();
        var dir = RipFolder(r);
        var first = r.MediaFile("intro.flac");
        await r.Vm.DropAsync([first, dir], null);
        Assert.Equal([
            $"Open {first}",
            $"Append {Path.Combine(dir, "01. Chinese Democracy.flac")}",
            $"Append {Path.Combine(dir, "02. Shackler's Revenge.flac")}",
            $"Append {Path.Combine(dir, "10. Riad N' The Bedouins.flac")}",
        ], r.Player.Calls.Where(c => c.StartsWith("Open ") || c.StartsWith("Append ")));
    }

    [Fact]
    public void Normalization_starts_on_at_minus_3_dB_and_its_level_applies_live()
    {
        using var r = new Rig(new PlayerSettings { SettingsVersion = PlayerSettings.CurrentVersion }); // as loaded
        Assert.True(r.Vm.NormalizeAudio);
        Assert.Equal(-3, r.Vm.NormalizeTargetDb);
        r.Vm.NormalizeTargetDb = -6.4;
        Assert.Equal(-6, r.Settings.NormalizeTargetDb);
        Assert.Contains("ApplyAudio", r.Player.Calls);
        r.Vm.NormalizeTargetDb = -30;
        Assert.Equal(PlayerSettings.MinNormalizeDb, r.Settings.NormalizeTargetDb);
        Assert.Equal(-12, new SettingsStore(r.Paths.SettingsFile).Load().NormalizeTargetDb);
    }

    [AvaloniaFact]
    public void The_audio_settings_show_normalization_on_with_its_level()
    {
        using var r = new Rig();
        var w = new SettingsWindow { DataContext = r.Vm };
        w.Show();
        w.GetLogicalDescendants().OfType<TabControl>().Single().SelectedItem = w.FindControl<TabItem>("AudioTab");
        Dispatch();
        Assert.True(w.FindControl<ToggleSwitch>("NormalizeSwitch")!.IsChecked);
        var level = w.FindControl<NumericUpDown>("NormalizeLevelBox")!;
        Assert.True(level.IsEffectivelyVisible);
        Assert.Equal(-3m, level.Value);
        var rows = w.GetVisualDescendants().OfType<SettingRow>().ToList();
        Assert.Contains("On (default)", rows.Single(row => row.Title == "Even out loud and quiet parts").Description);

        level.Value = -5;
        Dispatch();
        Assert.Equal(-5, r.Settings.NormalizeTargetDb);
        w.FindControl<ToggleSwitch>("NormalizeSwitch")!.IsChecked = false;
        Dispatch();
        Assert.False(level.IsEffectivelyVisible);
        w.Close();
    }
}
