using System.Diagnostics;
using System.Text.Json;
using AtlasPlayer.Core.Playback;
using AtlasPlayer.Core.Ripping;
using AtlasPlayer.Core.Ripping.Cd;
using AtlasPlayer.Core.Ripping.Dvd;

namespace AtlasPlayer.Core.Tests;

/// <summary>
/// Prompt 11: the buffering lines, a DVD's audio tracks picked and converted (FLAC, AAC, audio-only
/// FLAC and WAV, every channel kept), several titles in one rip, QTGMC, and a CD's cue sheet by choice.
/// </summary>
public class Prompt11CoreTests
{
    // The One (2003): LPCM stereo as stream 0 (id 0xA0), AC-3 5.1 as stream 1 (id 0x81), as ffprobe reads its title 1.
    private const string Probe = """
        {"streams": [
          {"index": 0, "codec_name": "mpeg2video", "codec_type": "video", "field_order": "tt", "id": "0x1e0", "display_aspect_ratio": "4:3"},
          {"index": 1, "codec_name": "pcm_dvd", "codec_type": "audio", "channels": 2, "id": "0xa0", "bits_per_raw_sample": "16"},
          {"index": 2, "codec_name": "ac3", "codec_type": "audio", "channels": 6, "id": "0x81"},
          {"index": 3, "codec_name": "dvd_subtitle", "codec_type": "subtitle", "id": "0x20"}],
         "format": {"duration": "4200.0"}}
        """;

    private static readonly DvdTitle Title1 = new(1, 1, 3, 1, TimeSpan.FromMinutes(70),
        [new DvdAudio("LPCM", 2, "en") { Number = 0 }, new DvdAudio("AC-3", 6, "en") { Number = 1 }], ["en"])
    {
        ChapterLengths = [TimeSpan.FromMinutes(20), TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(20)],
    };

    private static readonly DvdRipOptions Options = new() { Source = "/x.iso", Title = 1, OutputRoot = "/out" };

    [Fact]
    public void The_buffering_lines_trail_off()
    {
        Assert.Contains("Bruney…", BufferingQuips.All);
        Assert.Contains("Big fast, many speed…", BufferingQuips.All);
        Assert.DoesNotContain("Bruney", BufferingQuips.All);
    }

    [Fact]
    public void An_audio_track_reads_as_language_codec_and_layout()
    {
        Assert.Equal("English, AC-3, 5.1", new DvdAudio("AC-3", 6, "en").Description);
        Assert.Equal("French, DTS, 5.1", new DvdAudio("DTS", 6, "fr").Description);
        Assert.Equal("Unknown language, LPCM, stereo", new DvdAudio("LPCM", 2, null).Description);
        Assert.Equal("English AC-3 5.1", new DvdAudio("AC-3", 6, "en").FileLabel);
        Assert.Equal("Track 2 LPCM stereo", new DvdAudio("LPCM", 2, null) { Number = 1 }.FileLabel);
        Assert.Equal("7.1", DvdAudio.Layout(8));
        Assert.Equal("mono", DvdAudio.Layout(1));
        Assert.Equal("4 channels", DvdAudio.Layout(4));
        Assert.Equal("Russian", DvdAudio.LanguageName("ru"));
        Assert.Equal(0x81, new DvdAudio("AC-3", 6, "en") { Number = 1 }.StreamId);
        Assert.Equal(0xA0, new DvdAudio("LPCM", 2, "en").StreamId);
        Assert.Equal(0x89, new DvdAudio("DTS", 6, "en") { Number = 1 }.StreamId);
    }

    [Fact]
    public void Tracks_are_matched_to_the_ifo_by_stream_id_not_place()
    {
        var streams = DvdRipper.ParseStreams(Probe);
        Assert.Equal(new ProbedStream("audio", "ac3", 0x81, 6, 0), streams[2]);
        Assert.Equal(1, DvdRipper.IfoNumber(0x81));
        Assert.Equal(3, DvdRipper.IfoNumber(0xA3));
        Assert.Null(DvdRipper.IfoNumber(0x1E0));
        // All of them, in FFmpeg's order.
        var all = DvdRipper.AudioPlan(Title1, streams, null);
        Assert.Equal([0, 1], all.Select(a => a.Input));
        Assert.Equal(16, all[0].Bits);
        Assert.Equal("English AC-3 5.1", all[1].Label);
        // The AC-3 only: FFmpeg's second audio stream.
        var ac3 = Assert.Single(DvdRipper.AudioPlan(Title1, streams, [1]));
        Assert.Equal(1, ac3.Input);
        Assert.Equal(6, ac3.Channels);
        Assert.True(ac3.Deep);
        // A track the program chain left out: FFmpeg has the AC-3 alone, as its first audio stream.
        var lone = DvdRipper.ParseStreams("""{"streams": [{"codec_type": "audio", "codec_name": "ac3", "channels": 6, "id": "0x81"}]}""");
        Assert.Equal(0, Assert.Single(DvdRipper.AudioPlan(Title1, lone, [1])).Input);
        Assert.Throws<ArgumentException>(() => DvdRipper.AudioPlan(Title1, lone, [0]));
        Assert.Equal("4:3", DvdRipper.ParseDisplayAspect(Probe));
    }

    [Fact]
    public void An_mkv_keeps_only_the_tracks_picked_each_its_own_stream()
    {
        var plan = DvdRipper.AudioPlan(Title1, DvdRipper.ParseStreams(Probe), null);
        var args = DvdRipper.MkvArguments(Options, preindex: false, lpcm: [0], "T", "/out/T.mkv", audio: plan).ToList();
        Assert.Equal(["-map", "0:v:0", "-map", "0:a:0", "-map", "0:a:1", "-map", "0:s?", "-c", "copy",
                "-c:a:0", "flac", "-compression_level:a:0", "8", "-metadata", "title=T", "-f", "matroska", "/out/T.mkv"],
            args.SkipWhile(a => a != "-map"));
        // The AC-3 alone, as on the disc: copied, nothing else to do.
        var ac3 = DvdRipper.AudioPlan(Title1, DvdRipper.ParseStreams(Probe), [1]);
        Assert.Equal(["-map", "0:v:0", "-map", "0:a:1", "-map", "0:s?", "-c", "copy", "-metadata"],
            DvdRipper.MkvArguments(Options, false, [0], "T", "/o.mkv", audio: ac3).SkipWhile(a => a != "-map").Take(9));
    }

    [Fact]
    public void Flac_and_aac_convert_every_track_and_keep_its_channels()
    {
        var plan = DvdRipper.AudioPlan(Title1, DvdRipper.ParseStreams(Probe), null);
        Assert.Equal(["-c:a:0", "flac", "-compression_level:a:0", "8",
                "-c:a:1", "flac", "-compression_level:a:1", "8", "-sample_fmt:a:1", "s32", "-bits_per_raw_sample:a:1", "24"],
            DvdRipper.AudioArguments(plan, DvdAudioCodec.Flac, downmix: false, AudioStage.Final));
        Assert.Equal(["-c:a:0", "aac", "-b:a:0", "320k", "-c:a:1", "aac", "-b:a:1", "960k"],
            DvdRipper.AudioArguments(plan, DvdAudioCodec.Aac, downmix: false, AudioStage.Final));
        // Nothing mixes down unless asked.
        Assert.DoesNotContain(DvdRipper.AudioArguments(plan, DvdAudioCodec.Aac, false, AudioStage.Final), a => a.StartsWith("-ac", StringComparison.Ordinal));
        Assert.Equal(["-c:a:1", "aac", "-b:a:1", "320k", "-ac:a:1", "2"],
            DvdRipper.AudioArguments(plan, DvdAudioCodec.Aac, downmix: true, AudioStage.Final).Skip(6));
        // A copied AC-3 can't be mixed down: as on the disc, only LPCM is touched.
        Assert.Equal(["-c:a:0", "flac", "-compression_level:a:0", "8"], DvdRipper.AudioArguments(plan, DvdAudioCodec.Original, true, AudioStage.Final));
    }

    [Fact]
    public void Parts_keep_converted_tracks_as_pcm_and_the_join_encodes_them()
    {
        var plan = DvdRipper.AudioPlan(Title1, DvdRipper.ParseStreams(Probe), null);
        Assert.Equal(["-c:a:0", "pcm_s16le", "-c:a:1", "pcm_s24le"], DvdRipper.AudioArguments(plan, DvdAudioCodec.Aac, false, AudioStage.Part));
        Assert.Equal(["-c:a:0", "pcm_s16le", "-ac:a:0", "2", "-c:a:1", "pcm_s24le", "-ac:a:1", "2"],
            DvdRipper.AudioArguments(plan, DvdAudioCodec.Flac, true, AudioStage.Part));
        // Mixed down in the parts already: the join only encodes (at stereo's bitrate).
        Assert.Equal(["-c:a:0", "aac", "-b:a:0", "320k", "-c:a:1", "aac", "-b:a:1", "320k"],
            DvdRipper.AudioArguments(plan, DvdAudioCodec.Aac, true, AudioStage.Join));
        var join = DvdRipper.JoinArguments("/p.txt", "/c.txt", "/o.mkv", [0], plan, DvdAudioCodec.Aac).ToList();
        Assert.Equal(["-c", "copy", "-c:a:0", "aac"], join.SkipWhile(a => a != "-c").Take(4));
        // As on the disc, the AC-3 is copied in the parts too.
        Assert.Equal(["-c:a:0", "pcm_s16le"], DvdRipper.AudioArguments(plan, DvdAudioCodec.Original, false, AudioStage.Part));
    }

    [Fact]
    public void Bwdif_doubles_the_rate_or_keeps_it_and_qtgmc_gets_the_same_encoder()
    {
        Assert.Equal("bwdif=mode=send_field:parity=tff:deint=all", DvdRipper.VideoArguments(DvdVideo.X264, VideoScan.TopFieldFirst).ElementAt(1));
        Assert.Equal("bwdif=mode=send_frame:parity=bff:deint=all",
            DvdRipper.VideoArguments(DvdVideo.X264, VideoScan.BottomFieldFirst, doubleRate: false).ElementAt(1));
        // QTGMC's frames are deinterlaced already: no filter, the interlaced settings (CRF 14, 16).
        Assert.Equal(["-c:v", "libx264", "-preset", "slow", "-crf", "14"],
            DvdRipper.VideoArguments(DvdVideo.X264, VideoScan.TopFieldFirst, true, Deinterlacer.Qtgmc));
        Assert.Equal(["-c:v", "libx265", "-preset", "slow", "-crf", "16", "-x265-params", "no-sao=1:aq-mode=3:log-level=error"],
            DvdRipper.VideoArguments(DvdVideo.X265, VideoScan.TopFieldFirst, false, Deinterlacer.Qtgmc));
        Assert.Empty(DvdRipper.VideoArguments(DvdVideo.Copy, VideoScan.TopFieldFirst, true, Deinterlacer.Qtgmc));
    }

    [Fact]
    public void Qtgmcs_encode_takes_the_pipe_and_the_intermediates_tracks()
    {
        var plan = DvdRipper.AudioPlan(Title1, DvdRipper.ParseStreams(Probe), null);
        var o = Options with { Video = DvdVideo.X264, Deinterlacer = Deinterlacer.Qtgmc, AudioCodec = DvdAudioCodec.Flac };
        var args = DvdRipper.QtgmcEncodeArguments(o, "/w/title.mkv", plan, VideoScan.TopFieldFirst, "4:3", "T", "/out/T.mkv", part: false).ToList();
        Assert.Equal(["-f", "yuv4mpegpipe", "-i", "pipe:0", "-i", "/w/title.mkv", "-map", "0:v:0", "-map", "1:a?", "-map", "1:s?",
            "-map_chapters", "1", "-c", "copy", "-c:a:0", "flac"], args.SkipWhile(a => a != "-f").Take(18));
        Assert.Equal(["-aspect", "4:3", "-metadata", "title=T", "-f", "matroska", "/out/T.mkv"], args.TakeLast(7));
        Assert.DoesNotContain("-filter:v", args);
        // A part keeps the PCM for the join.
        Assert.DoesNotContain("flac", DvdRipper.QtgmcEncodeArguments(o, "/w/t.mkv", plan, VideoScan.TopFieldFirst, null, "T", "/p.mkv", part: true));
    }

    [Fact]
    public void Qtgmcs_script_runs_havsfunc_on_the_copy_with_the_preset_and_rate()
    {
        var runtime = new QtgmcRuntime("/usr/bin/python3", "/app/lib/vapoursynth/python", "/app/lib/vapoursynth/plugins");
        Assert.Equal(["-I", "/w/qtgmc.py", "/app/lib/vapoursynth/python", "/app/lib/vapoursynth/plugins", "/w/title.mkv", "Very Slow", "bff", "2"],
            Qtgmc.Arguments(runtime, "/w/qtgmc.py", "/w/title.mkv", "Very Slow", VideoScan.BottomFieldFirst, doubleRate: false));
        Assert.Equal("Slower", Qtgmc.Arguments(runtime, "/s", "/t", "Nonsense", VideoScan.TopFieldFirst, true)[5]);
        Assert.Equal(["Medium", "Slow", "Slower", "Very Slow", "Placebo"], Qtgmc.Presets);
        Assert.Contains("havsfunc.QTGMC(clip, Preset=preset, TFF=(order == 'tff'), FPSDivisor=divisor)", Qtgmc.Script);
        Assert.Contains("clip.output(sys.stdout.buffer, y4m=True)", Qtgmc.Script);
        Assert.Contains("core.bs.VideoSource", Qtgmc.Script);
        Assert.Contains("('mv', 'znedi3', 'rgvs', 'bs')", Qtgmc.ProbeScript);
        // Python's own indentation survives the C# literal.
        Assert.Contains("\n        try:\n            core.std.LoadPlugin", Qtgmc.Script);
    }

    [Fact]
    public void The_bundled_vapoursynth_is_found_next_to_the_app()
    {
        var app = TestEnvironment.TempDir();
        if (Environment.GetEnvironmentVariable("ATLAS_VAPOURSYNTH") is null && !Directory.Exists("/app/lib/vapoursynth"))
            Assert.Null(Qtgmc.Find(app));
        var root = Path.Combine(app, "vapoursynth");
        Directory.CreateDirectory(Path.Combine(root, "plugins"));
        Directory.CreateDirectory(Path.Combine(root, "Lib", "site-packages"));
        File.WriteAllText(Path.Combine(root, "python.exe"), "");
        var found = Qtgmc.Find(app);
        Assert.NotNull(found);
        Assert.Equal(Path.Combine(root, "python.exe"), found.Python);
        Assert.Equal(Path.Combine(root, "Lib", "site-packages"), found.Modules);
        Assert.Equal(Path.Combine(root, "plugins"), found.Plugins);
    }

    [Fact]
    public void Audio_files_are_flac_or_wav_with_every_channel()
    {
        var ac3 = new AudioOut(1, "ac3", 0, 6, "English AC-3 5.1");
        var lpcm = new AudioOut(0, "pcm_dvd", 16, 2, "English LPCM stereo");
        Assert.Equal(["-af", "aresample=async=1:first_pts=0", "-c:a", "flac", "-compression_level", "8", "-sample_fmt", "s32", "-bits_per_raw_sample", "24"],
            DvdRipper.AudioFileArguments(ac3, AudioFileFormat.Flac, downmix: false));
        Assert.Equal(["-c:a", "flac", "-compression_level", "8"], DvdRipper.AudioFileArguments(lpcm, AudioFileFormat.Flac, false, fillGaps: false));
        Assert.Equal(["-c:a", "pcm_s24le", "-rf64", "auto"], DvdRipper.AudioFileArguments(ac3, AudioFileFormat.Wav, false, fillGaps: false));
        Assert.Equal(["-c:a", "pcm_s16le", "-ac", "2", "-rf64", "auto"], DvdRipper.AudioFileArguments(lpcm, AudioFileFormat.Wav, true, fillGaps: false));
        Assert.Equal("The One - Title 01 - Chapter 03 - English AC-3 5.1.flac",
            DvdRipper.AudioFileName("The One", 1, "Chapter 03", ac3, severalTracks: true, AudioFileFormat.Flac));
        Assert.Equal("The One - Title 01.wav", DvdRipper.AudioFileName("The One", 1, null, ac3, severalTracks: false, AudioFileFormat.Wav));
    }

    [Fact]
    public void One_read_writes_every_picked_track_to_its_own_file()
    {
        var o = Options with { Mode = DvdRipMode.AudioOnly, AudioFormat = AudioFileFormat.Flac };
        var ac3 = new AudioOut(1, "ac3", 0, 6, "a");
        var lpcm = new AudioOut(0, "pcm_dvd", 16, 2, "b");
        var args = DvdRipper.AudioArgumentsFor(o, new ChapterRun(2, 2), [(lpcm, "/m/b.flac", 2, "Chapter 2"), (ac3, "/m/a.flac", 2, "Chapter 2")],
            "The One", part: false).ToList();
        Assert.Equal(1, args.Count(a => a == "-i"));
        Assert.Equal(["-chapter_start", "2", "-chapter_end", "2"], args.SkipWhile(a => a != "-chapter_start").Take(4));
        Assert.Equal(["-map", "0:a:0", "-af", "aresample=async=1:first_pts=0", "-c:a", "flac"], args.SkipWhile(a => a != "-map").Take(6));
        Assert.Equal(["-map", "0:a:1"], args.SkipWhile(a => a != "/m/b.flac").Skip(1).Take(2));
        Assert.Contains("album=The One", args);
        Assert.Contains("title=Chapter 2", args);
        Assert.Equal("/m/a.flac", args[^1]);
        // Parts to join: PCM in WAV, untagged.
        var parts = DvdRipper.AudioArgumentsFor(o, new ChapterRun(1, 1), [(ac3, "/p/0.wav", 0, "")], "The One", part: true).ToList();
        Assert.Equal(["-c:a", "pcm_s24le", "-rf64", "auto", "-f", "wav", "/p/0.wav"], parts.SkipWhile(a => a != "-c:a"));
        Assert.Contains("aresample=async=1:first_pts=0", parts); // the gaps filled in the parts already
    }

    [Fact]
    public void A_cd_writes_a_cue_sheet_as_asked_by_default_for_a_whole_disc()
    {
        var o = new CdRipOptions { Tags = new AlbumTags(), OutputRoot = "/m" };
        Assert.True(o.CueFor(wholeDisc: true));
        Assert.False(o.CueFor(wholeDisc: false));
        Assert.False((o with { WriteCue = false }).CueFor(true));
        Assert.True((o with { WriteCue = true }).CueFor(false));
    }

    [Fact]
    public void A_rip_without_a_cue_sheet_still_scores_100()
    {
        var disc = new CdDisc(new CdToc([new CdTrack(1, 0, true)], 300), new DriveIdentity("PIONEER", "DVD-RW DVR-107D", "1.00"),
            new DriveFeatures(AccurateStream: true, C2Pointers: false, BufferKb: 2048), null, null, [], [], 48, new Dictionary<int, int>());
        // Nothing a log checker deducts has to do with the cue sheet: offset, accurate stream, gaps, reads.
        Assert.Empty(RipLog.Deduct(disc, [], OffsetSource.DriveList, new Dictionary<int, int>()));
    }

    [Fact]
    public async Task Two_tools_run_as_a_pipe()
    {
        if (OperatingSystem.IsWindows()) return;
        var lines = new List<string>();
        await RipTools.RunPipedAsync("/bin/sh", ["-c", "printf 'hello world'"], "/bin/sh", ["-c", "wc -c"], CancellationToken.None, lines.Add);
        Assert.Equal("11", Assert.Single(lines).Trim());
        // The one that fails is named.
        var e = await Assert.ThrowsAsync<IOException>(() => RipTools.RunPipedAsync("/bin/sh", ["-c", "echo broken >&2; exit 3"], "/bin/sh",
            ["-c", "cat > /dev/null"], CancellationToken.None));
        Assert.Contains("sh failed (3): broken", e.Message);
        e = await Assert.ThrowsAsync<IOException>(() => RipTools.RunPipedAsync("/bin/sh", ["-c", "yes"], "/bin/sh",
            ["-c", "head -c 10 > /dev/null; echo full >&2; exit 4"], CancellationToken.None));
        Assert.Contains("(4): full", e.Message);
        // Cancelling stops both.
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => RipTools.RunPipedAsync("/bin/sh", ["-c", "yes"], "/bin/sh",
            ["-c", "cat > /dev/null"], cts.Token));
    }
}

/// <summary>
/// Real rips of a real DVD image (ATLAS_DVD_ISO, e.g. "The One (2003).iso": AC-3 5.1 + LPCM
/// stereo); skipped without one. Short chapters keep them quick.
/// </summary>
public class Prompt11DvdRipTests
{
    private static string? Iso => Environment.GetEnvironmentVariable("ATLAS_DVD_ISO") is { Length: > 0 } p && File.Exists(p) ? p : null;
    private static string? Ffmpeg => RipTools.Find("ffmpeg");
    private static string? Ffprobe => RipTools.Find("ffprobe");

    private static (string Codec, int Channels)[] Audio(string file)
    {
        var psi = new ProcessStartInfo(Ffprobe!) { RedirectStandardOutput = true, UseShellExecute = false };
        foreach (var a in new[] { "-v", "error", "-select_streams", "a", "-show_entries", "stream=codec_name,channels", "-of", "json", file })
            psi.ArgumentList.Add(a);
        using var p = Process.Start(psi)!;
        var json = p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("streams").EnumerateArray()
            .Select(s => (s.GetProperty("codec_name").GetString()!, s.GetProperty("channels").GetInt32())).ToArray();
    }

    private static double Duration(string file)
    {
        var psi = new ProcessStartInfo(Ffprobe!) { RedirectStandardOutput = true, UseShellExecute = false };
        foreach (var a in new[] { "-v", "error", "-show_entries", "format=duration", "-of", "csv=p=0", file }) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi)!;
        var text = p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        return double.Parse(text.Trim(), System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>A short chapter of the main title (of a few minutes at least), to rip quickly.</summary>
    private static (DvdInfo Info, DvdTitle Title, int Chapter) Disc()
    {
        var info = DvdInfo.Read(Iso!);
        var title = info.MainTitle!;
        var chapter = Enumerable.Range(1, title.Chapters).Where(c => title.ChapterLength(c) > TimeSpan.FromMinutes(2))
            .OrderBy(c => title.ChapterLength(c)).First();
        return (info, title, chapter);
    }

    [Fact]
    public async Task Audio_only_keeps_every_channel_in_flac_and_wav_a_file_per_track()
    {
        if (Iso is null || Ffmpeg is null || Ffprobe is null) return;
        var (info, title, chapter) = Disc();
        var output = TestEnvironment.TempDir();
        foreach (var format in new[] { AudioFileFormat.Flac, AudioFileFormat.Wav })
        {
            var r = await DvdRipper.RipAsync(info, [new DvdRipOptions
            {
                Source = Iso!, Mode = DvdRipMode.AudioOnly, Title = title.Number, Chapters = [chapter], ChapterFiles = true,
                AudioFormat = format, OutputRoot = output, Ffmpeg = Ffmpeg, Ffprobe = Ffprobe,
            }]);
            var files = Directory.GetFiles(r.Output, format == AudioFileFormat.Flac ? "*.flac" : "*.wav");
            Assert.Equal(title.Audio.Count, files.Length);
            foreach (var a in title.Audio)
            {
                var file = Assert.Single(files, f => f.Contains(a.FileLabel, StringComparison.Ordinal));
                var (codec, channels) = Assert.Single(Audio(file));
                // As long as the chapter: gaps in a track (The One's AC-3) are silence, not closed up.
                Assert.InRange(Duration(file), title.ChapterLength(chapter)!.Value.TotalSeconds - 1.5, title.ChapterLength(chapter)!.Value.TotalSeconds + 1.5);
                Assert.Equal(format == AudioFileFormat.Flac ? "flac" : a.Codec != "LPCM" ? "pcm_s24le" : "pcm_s16le", codec);
                Assert.Equal(a.Channels, channels);
            }
        }
    }

    [Fact]
    public async Task An_mkv_keeps_the_tracks_picked_as_aac_with_their_channels()
    {
        if (Iso is null || Ffmpeg is null || Ffprobe is null) return;
        var (info, title, chapter) = Disc();
        var output = TestEnvironment.TempDir();
        var r = await DvdRipper.RipAsync(info, [new DvdRipOptions
        {
            Source = Iso!, Mode = DvdRipMode.Mkv, Title = title.Number, Chapters = [chapter], AudioCodec = DvdAudioCodec.Aac,
            OutputRoot = output, Ffmpeg = Ffmpeg, Ffprobe = Ffprobe,
        }]);
        var audio = Audio(r.Output);
        Assert.Equal(title.Audio.Count, audio.Length);
        Assert.All(audio, a => Assert.Equal("aac", a.Codec));
        Assert.Equal(title.Audio.Select(a => a.Channels).Order(), audio.Select(a => a.Channels).Order());
        // One track only: the last one the IFO lists.
        var one = await DvdRipper.RipAsync(info, [new DvdRipOptions
        {
            Source = Iso!, Mode = DvdRipMode.Mkv, Title = title.Number, Chapters = [chapter], AudioTracks = [title.Audio[^1].Number],
            OutputRoot = output, Ffmpeg = Ffmpeg, Ffprobe = Ffprobe,
        }]);
        Assert.Equal(title.Audio[^1].Channels, Assert.Single(Audio(one.Output)).Channels);
    }
}
