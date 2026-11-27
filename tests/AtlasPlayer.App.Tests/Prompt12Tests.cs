using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using AtlasPlayer.App.ViewModels;
using AtlasPlayer.App.Views;
using AtlasPlayer.Core.Ripping.Dvd;

namespace AtlasPlayer.App.Tests;

/// <summary>
/// Prompt 12: one button for a disc folder or image, a DVD title ripped whole or a file for each
/// ticked chapter, QTGMC saying why it didn't load, and Play's glyph centred in its button.
/// </summary>
public class Prompt12Tests
{
    private static RipDialogViewModel Dvd(Rig r, (QtgmcRuntime?, bool)? qtgmc = null, string? problem = null) =>
        new(r.Vm, [RipDialogTests.Dvd], RipDialogTests.Dvd.Root, _ => RipDialogTests.CdDrive(), _ => Prompt11Tests.Concert(), flac: "",
            ffmpeg: "/usr/bin/ffmpeg", online: false, ffprobe: "/usr/bin/ffprobe", encoders: new HashSet<string> { "libx264", "libx265" },
            qtgmc: qtgmc ?? (null, false), qtgmcProblem: problem);

    [Fact]
    public void One_file_per_title_is_the_whole_title_whatever_is_ticked()
    {
        using var r = new Rig();
        foreach (var mode in new[] { 0, 1 }) // MKV, audio only
        {
            var vm = Dvd(r);
            vm.DvdModeIndex = mode;
            vm.StepIndex = 2;
            Assert.False(vm.ChapterFiles);
            Assert.False(vm.ChapterTicksEnabled);
            vm.Chapters[0].IsChecked = false;
            vm.NoChaptersCommand.Execute(null);
            var job = vm.DvdOptions()!;
            Assert.Null(job.Chapters);
            Assert.False(job.ChapterFiles);
            Assert.Equal("The whole title · 1:10:00", vm.ChaptersSummary);
            Assert.True(vm.NextCommand.CanExecute(null)); // no chapter needs ticking
            Assert.Contains("Title 2: the whole title", vm.RipSummary);
        }
    }

    [Fact]
    public void One_file_per_chapter_rips_each_ticked_chapter_to_its_own_file()
    {
        using var r = new Rig();
        var vm = Dvd(r);
        vm.StepIndex = 2;
        vm.ChapterFiles = true;
        Assert.True(vm.ChapterTicksEnabled);
        Assert.Equal("All 3 chapters · 1:10:00, as 3 files", vm.ChaptersSummary);
        Assert.Equal([1, 2, 3], vm.DvdOptions()!.Chapters);
        Assert.True(vm.DvdOptions()!.ChapterFiles);

        vm.Chapters[1].IsChecked = false;
        vm.Chapters[2].IsChecked = false;
        Assert.Equal("1 of 3 chapters · 20:00, as 1 file", vm.ChaptersSummary);
        Assert.Equal([1], vm.DvdOptions()!.Chapters);
        Assert.True(vm.DvdOptions()!.ChapterFiles);

        vm.NoChaptersCommand.Execute(null);
        Assert.False(vm.NextCommand.CanExecute(null)); // split, a chapter must be ticked
        Assert.Equal("Tick a chapter", vm.TitleRows[1].Summary);
        vm.ChapterFiles = false; // back to the whole title: nothing to tick
        Assert.True(vm.NextCommand.CanExecute(null));
    }

    [AvaloniaFact]
    public void The_chapter_ticks_are_greyed_out_for_one_file_per_title()
    {
        using var r = new Rig();
        var vm = Dvd(r);
        vm.StepIndex = 2;
        var w = new RipDialog { DataContext = vm };
        w.Show();
        Dispatcher.UIThread.RunJobs();
        var list = w.FindControl<ListBox>("ChapterList")!;
        Assert.False(list.IsEnabled);
        Assert.Equal("One file per title", w.FindControl<RadioButton>("OneFile")!.Content);
        vm.ChapterFiles = true;
        Dispatcher.UIThread.RunJobs();
        Assert.True(list.IsEnabled);
        w.Close();
    }

    [AvaloniaFact]
    public void Open_disc_and_rip_disc_have_one_button_for_a_folder_or_an_image()
    {
        var disc = new OpenDiscDialog();
        disc.Show();
        var button = disc.FindControl<Button>("DiscSourceButton")!;
        Assert.Equal("Disc folder/image…", button.Content);
        Assert.Equal(["Folder…", "Image…"], ((MenuFlyout)button.Flyout!).Items.OfType<MenuItem>().Select(i => (string)i.Header!));
        disc.Close();

        using var r = new Rig();
        var rip = new RipDialog { DataContext = Dvd(r) };
        rip.Show();
        var dvd = rip.FindControl<Button>("DvdSourceButton")!;
        Assert.Equal("DVD folder/image…", dvd.Content);
        Assert.Equal(["Folder…", "Image…"], ((MenuFlyout)dvd.Flyout!).Items.OfType<MenuItem>().Select(i => (string)i.Header!));
        rip.Close();
    }

    [Fact]
    public void Qtgmc_says_why_it_did_not_load()
    {
        using var r = new Rig();
        var runtime = new QtgmcRuntime(@"C:\Atlas Player\vapoursynth\python.exe", "m", "p");
        var vm = Dvd(r, (runtime, false), "ImportError: DLL load failed while importing vapoursynth");
        Assert.False(vm.HasQtgmc);
        Assert.Equal("QTGMC didn't load: ImportError: DLL load failed while importing vapoursynth", vm.QtgmcTip);
        Assert.Contains("isn't there", Dvd(r).QtgmcTip); // no VapourSynth at all
    }

    /// <summary>
    /// Play's triangle sits in the middle of its round button: its ink's box centred within a
    /// pixel up and down, and a little right of centre across (a triangle's weight is a third of
    /// the way in from its flat side, so the box is nudged right to look centred).
    /// </summary>
    [AvaloniaFact]
    public void Plays_glyph_is_centred_in_its_button()
    {
        using var r = new Rig();
        r.Vm.IsIdle = false;
        r.Player.State.HasVideo = true;
        r.Player.RaiseState("has-video");
        r.Vm.IsPaused = true;
        var bar = new ControlsBar { DataContext = r.Vm, Width = 1280 };
        bar.Show();
        Dispatcher.UIThread.RunJobs();
        var play = bar.FindControl<Button>("PlayButton")!;
        Assert.True(r.Vm.ShowPlayIcon);
        using var frame = bar.CaptureRenderedFrame()!;
        var origin = play.TranslatePoint(default, bar)!.Value;
        var box = new PixelRect((int)origin.X, (int)origin.Y, (int)play.Bounds.Width, (int)play.Bounds.Height);
        var (ink, centroidX) = Ink(frame, box);
        var cx = box.Width / 2.0;
        var cy = box.Height / 2.0;
        var inkCx = ink.X + ink.Width / 2.0;
        var inkCy = ink.Y + ink.Height / 2.0;
        var report = $"button {box.Width}x{box.Height}, ink {ink}, ink centre ({inkCx:0.0}, {inkCy:0.0}), centroid x {centroidX:0.0}";
        Assert.True(Math.Abs(inkCy - cy) <= 1.0, report);
        // The visual weight (the centroid) within a pixel of the middle; the box a little right of it.
        Assert.True(Math.Abs(centroidX - cx) <= 1.5, report);
        Assert.True(inkCx > cx && inkCx - cx <= 3.5, report);
        bar.Close();
    }

    /// <summary>The glyph's pixels (the brightest/most different from the button's own fill) within the button.</summary>
    private static (PixelRect Ink, double CentroidX) Ink(WriteableBitmap frame, PixelRect box)
    {
        using var fb = frame.Lock();
        var stride = fb.RowBytes;
        var bytes = new byte[stride * fb.Size.Height];
        System.Runtime.InteropServices.Marshal.Copy(fb.Address, bytes, 0, bytes.Length);
        (int B, int G, int R) At(int x, int y)
        {
            var i = y * stride + x * 4;
            return (bytes[i], bytes[i + 1], bytes[i + 2]);
        }
        // The button's fill: what's at its middle-left, well inside the round but away from the glyph.
        var fill = At(box.X + box.Width / 6, box.Y + box.Height / 2);
        int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
        double sumX = 0, count = 0;
        for (var y = 0; y < box.Height; y++)
        for (var x = 0; x < box.Width; x++)
        {
            var p = At(box.X + x, box.Y + y);
            var d = Math.Abs(p.R - fill.R) + Math.Abs(p.G - fill.G) + Math.Abs(p.B - fill.B);
            // Inside the round only (the corners are the bar behind it).
            var dx = x - box.Width / 2.0 + 0.5;
            var dy = y - box.Height / 2.0 + 0.5;
            if (dx * dx + dy * dy > Math.Pow(box.Width / 2.0 - 3, 2) || d < 200) continue;
            minX = Math.Min(minX, x); maxX = Math.Max(maxX, x);
            minY = Math.Min(minY, y); maxY = Math.Max(maxY, y);
            sumX += x + 0.5;
            count++;
        }
        Assert.True(count > 0, "no glyph found in the Play button");
        return (new PixelRect(minX, minY, maxX - minX + 1, maxY - minY + 1), sumX / count);
    }
}
