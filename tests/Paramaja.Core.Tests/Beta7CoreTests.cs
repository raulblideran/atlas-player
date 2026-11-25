using System.Diagnostics;
using Paramaja.Core.Playback;
using Paramaja.Core.Ripping;

namespace Paramaja.Core.Tests;

/// <summary>2.0.0-beta.7: audio codec names, screenshots of interlaced video.</summary>
public class Beta7CoreTests
{
    [Theory]
    [InlineData("flac", "FLAC")]
    [InlineData("alac", "ALAC")]
    [InlineData("mp3float", "MP3")]
    [InlineData("aac", "AAC")]
    [InlineData("opus", "Opus")]
    [InlineData("vorbis", "Vorbis")]
    [InlineData("pcm_s24le", "PCM")]
    [InlineData("pcm_f32le", "PCM")]
    [InlineData("dsd_lsbf_planar", "DSD")]
    [InlineData("ac3", "AC-3")]
    [InlineData("eac3", "E-AC-3")]
    [InlineData("truehd", "TrueHD")]
    [InlineData("dts", "DTS")]
    [InlineData("wavpack", "WavPack")]
    [InlineData("ape", "APE")]
    [InlineData("wmalossless", "WMA Lossless")]
    [InlineData("somethingnew", "SOMETHINGNEW")]
    [InlineData(null, "")]
    [InlineData(" ", "")]
    public void Audio_codecs_have_their_familiar_names(string? codec, string expected) =>
        Assert.Equal(expected, CodecLabels.DescribeAudio(codec));

    [Fact]
    public void The_tooltip_says_what_the_codec_is()
    {
        Assert.Equal("FLAC, lossless", CodecLabels.AboutAudio("flac"));
        Assert.Equal("Dolby Digital (AC-3)", CodecLabels.AboutAudio("ac3"));
        Assert.Equal("PCM, uncompressed", CodecLabels.AboutAudio("pcm_s16le"));
    }

    private static string Ffmpeg(string args, string output)
    {
        var psi = new ProcessStartInfo("ffmpeg") { RedirectStandardError = true, UseShellExecute = false };
        foreach (var a in System.Text.RegularExpressions.Regex.Matches(args, "\"[^\"]*\"|\\S+").Select(m => m.Value.Trim('"'))) psi.ArgumentList.Add(a);
        psi.ArgumentList.Add(output);
        using var p = Process.Start(psi)!;
        var err = p.StandardError.ReadToEnd();
        p.WaitForExit();
        Assert.True(p.ExitCode == 0, err);
        return output;
    }

    /// <summary>A bright bar moving 16 pixels a field: woven into frames, every edge combs.</summary>
    private const string MovingBar = "-f lavfi -i \"color=c=black:s=720x576:r=50,format=gray,geq=lum='if(lt(mod(X+N*16\\,720)\\,360)\\,220\\,30)'\"";

    private static string CombedPng(string dir) =>
        Ffmpeg($"-v error -y {MovingBar} -vf tinterlace=interleave_top -frames:v 1 -update 1 -compression_level 0", Path.Combine(dir, "combed.png"));

    private static string ProgressivePng(string dir) =>
        Ffmpeg("-v error -y -f lavfi -i testsrc=size=720x576:rate=25 -frames:v 1 -update 1 -compression_level 0", Path.Combine(dir, "progressive.png"));

    private static bool HasFfmpeg => RipTools.Find("ffmpeg") is not null;

    [Fact]
    public void Woven_fields_measure_as_combed_and_a_progressive_picture_doesnt()
    {
        if (!HasFfmpeg) return;
        var dir = TestEnvironment.TempDir();
        Assert.True(InterlacedScreenshots.Combing(CombedPng(dir)) > InterlacedScreenshots.CombedAbove);
        Assert.True(InterlacedScreenshots.Combing(ProgressivePng(dir)) < InterlacedScreenshots.CombedAbove);
        Assert.Equal((720, 576), InterlacedScreenshots.PngSize(Path.Combine(dir, "combed.png")));
        Assert.Null(InterlacedScreenshots.PngSize(Path.Combine(dir, "nothing.png")));
    }

    [Fact]
    public void A_combed_screenshot_is_deinterlaced_in_place_and_stays_an_uncompressed_png()
    {
        if (!HasFfmpeg) return;
        var dir = TestEnvironment.TempDir();
        var combed = CombedPng(dir);
        Assert.True(InterlacedScreenshots.DeinterlaceIfCombed(combed));
        Assert.True(InterlacedScreenshots.Combing(combed) < 1.0, $"still combed: {InterlacedScreenshots.Combing(combed)}");
        var png = PngInfo.Read(combed);
        Assert.Equal((720, 576), (png.Width, png.Height));
        Assert.True(png.StoredDeflateBlocks);
        Assert.Single(Directory.GetFiles(dir, "*.png"), f => f.Contains("combed")); // no temporary file left

        var progressive = ProgressivePng(dir);
        var before = File.ReadAllBytes(progressive);
        Assert.False(InterlacedScreenshots.DeinterlaceIfCombed(progressive));
        Assert.Equal(before, File.ReadAllBytes(progressive));
    }

    [Fact]
    public void A_second_screenshot_of_the_same_moment_gets_its_own_name()
    {
        var dir = TestEnvironment.TempDir();
        var now = new DateTime(2026, 11, 25, 20, 30, 0);
        var first = PlayerService.ScreenshotPath(dir, "Film", 61.5, now);
        File.WriteAllText(first, "");
        var second = PlayerService.ScreenshotPath(dir, "Film", 61.5, now);
        Assert.Equal(first.Replace(".png", " (2).png"), second);
        File.WriteAllText(second, "");
        Assert.EndsWith(" (3).png", PlayerService.ScreenshotPath(dir, "Film", 61.5, now));
    }

    [MpvFact]
    public void A_screenshot_of_interlaced_video_is_the_deinterlaced_picture()
    {
        if (!HasFfmpeg) return;
        var dir = TestEnvironment.TempDir();
        var file = Environment.GetEnvironmentVariable("PARAMAJA_COMB_FILE")
            ?? Ffmpeg($"-v error -y {MovingBar} -t 3 -vf tinterlace=interleave_top,setfield=tff,format=yuv420p -c:v mpeg2video -q:v 2 -flags +ilme+ildct",
                Path.Combine(dir, "moving_576i.mpg"));
        var at = double.TryParse(Environment.GetEnvironmentVariable("PARAMAJA_COMB_AT"), out var t) ? t : 1.0;
        using var h = new PlayerHarness();
        h.OpenAndWait(file, paused: true);
        PlayerHarness.WaitUntil(() => h.Player.State.DeinterlaceActive, "deinterlacer on");
        h.Player.SeekAbsolute(at);
        h.WaitRestart();
        var on = h.Player.Screenshot(false);
        PlayerHarness.WaitUntil(() => File.Exists(on) && new FileInfo(on).Length > 0, "the screenshot");
        Thread.Sleep(500); // the combing check, had it been needed, is done by now

        h.Player.SetDeinterlacing(false);
        PlayerHarness.WaitUntil(() => !h.Player.State.DeinterlaceActive, "deinterlacer off");
        h.Player.SeekAbsolute(at);
        h.WaitRestart();
        var off = h.Player.Screenshot(false);
        PlayerHarness.WaitUntil(() => File.Exists(off) && new FileInfo(off).Length > 0, "the second screenshot");

        Assert.NotEqual(on, off); // the second didn't replace the first
        var (combedOn, combedOff) = (InterlacedScreenshots.Combing(on)!.Value, InterlacedScreenshots.Combing(off)!.Value);
        Console.WriteLine($"COMB on={combedOn:0.000} off={combedOff:0.000}");
        // A real interlaced video (PARAMAJA_COMB_FILE, e.g. a concert DVD's VOB) shows the woven
        // frame combing once the deinterlacer is off; the made-up clip here comes out of mpv unwoven.
        if (Environment.GetEnvironmentVariable("PARAMAJA_COMB_FILE") is not null)
            Assert.True(combedOff > InterlacedScreenshots.CombedAbove, $"the woven frame should comb: {combedOff:0.00}");
        Assert.True(combedOn < InterlacedScreenshots.CombedAbove, $"the deinterlaced screenshot combs: {combedOn:0.00} (woven {combedOff:0.00})");
    }
}
