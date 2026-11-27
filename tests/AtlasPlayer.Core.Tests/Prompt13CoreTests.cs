using System.Text;
using AtlasPlayer.Core.Input;
using AtlasPlayer.Core.Playback;
using AtlasPlayer.Core.Settings;

namespace AtlasPlayer.Core.Tests;

/// <summary>
/// Prompt 13: volume normalization is on by default, to -3 dB. A CD rip folder holding more than
/// its music (the ripper's log, a cue sheet naming the disc's image file, scans) said "Can't play
/// this: no audio or video data played": the cue went to mpv as it was. Such a folder plays its
/// music now, with cover.jpg (or front.jpg…) as the picture.
/// </summary>
public class Prompt13CoreTests
{
    [Theory]
    [InlineData(-3, "p=0.708")]
    [InlineData(0, "p=1]")]
    [InlineData(-6, "p=0.501")]
    [InlineData(-12, "p=0.251")]
    [InlineData(-40, "p=0.251")] // clamped
    public void The_normalizer_brings_peaks_to_the_level_set(double db, string expected) =>
        Assert.Contains(expected, MpvOptionsBuilder.NormalizeFilter(db));

    [Fact]
    public void Normalization_is_on_at_minus_3_dB_by_default_and_for_existing_users()
    {
        var fresh = new PlayerSettings();
        Assert.True(fresh.NormalizeAudio);
        Assert.Equal(-3, fresh.NormalizeTargetDb);
        Assert.Equal(MpvOptionsBuilder.NormalizeFilter(-3), MpvOptionsBuilder.AudioFilters(fresh, 2));

        // beta.12's settings (version 7) with it off, the old default: moved to the new one.
        var dir = TestEnvironment.TempDir();
        var path = Path.Combine(dir, "settings.json");
        File.WriteAllText(path, """{ "settingsVersion": 7, "normalizeAudio": false }""");
        var migrated = new SettingsStore(path).Load();
        Assert.True(migrated.NormalizeAudio);
        Assert.Equal(-3, migrated.NormalizeTargetDb);
        Assert.Equal(PlayerSettings.CurrentVersion, migrated.SettingsVersion);

        // Switched off (or set to another level) after that: it stays so.
        File.WriteAllText(path, $$"""{ "settingsVersion": {{PlayerSettings.CurrentVersion}}, "normalizeAudio": false, "normalizeTargetDb": -6 }""");
        var kept = new SettingsStore(path).Load();
        Assert.False(kept.NormalizeAudio);
        Assert.Equal(-6, kept.NormalizeTargetDb);

        // Hand-edited nonsense is brought into range.
        var odd = new PlayerSettings { SettingsVersion = PlayerSettings.CurrentVersion, NormalizeTargetDb = 7 };
        odd.Normalize();
        Assert.Equal(0, odd.NormalizeTargetDb);
        odd.NormalizeTargetDb = double.NaN;
        odd.Normalize();
        Assert.Equal(-3, odd.NormalizeTargetDb);
    }

    [Fact]
    public void A_folder_plays_its_music_in_order_without_logs_cue_sheets_or_pictures()
    {
        var dir = TestEnvironment.Media("cd_rip_folder");
        Assert.Equal(["01. Opening.flac", "2. Middle.flac", "10. Closing.flac"], FolderMedia.Playlist(dir).Select(Path.GetFileName));
    }

    [Fact]
    public void A_two_disc_album_plays_disc_by_disc()
    {
        var dir = TestEnvironment.TempDir();
        foreach (var (disc, track) in new[] { ("CD2", "01 b.flac"), ("CD1", "02 a.flac"), ("CD1", "01 a.flac") })
        {
            Directory.CreateDirectory(Path.Combine(dir, disc));
            File.WriteAllText(Path.Combine(dir, disc, track), "");
        }
        File.WriteAllText(Path.Combine(dir, "cover.jpg"), "");
        Assert.Equal(["CD1/01 a.flac", "CD1/02 a.flac", "CD2/01 b.flac"],
            FolderMedia.Playlist(dir).Select(p => Path.GetRelativePath(dir, p).Replace('\\', '/')));
        Assert.Empty(FolderMedia.Playlist(Path.Combine(dir, "nothing here")));
    }

    [Fact]
    public void Names_sort_as_people_number_them()
    {
        string[] names = ["Track 10.flac", "track 2.flac", "Track 1.flac", "Track 02b.flac", "Bonus.flac"];
        Assert.Equal(["Bonus.flac", "Track 1.flac", "track 2.flac", "Track 02b.flac", "Track 10.flac"],
            names.Order(FolderMedia.NaturalComparer.Instance));
    }

    [Fact]
    public void A_folder_whose_cue_sheet_names_none_of_its_files_is_a_folder_of_music()
    {
        // The real case: Chinese Democracy's sheet names "GunsNRoses_ChineseDemocracy1.flac", the
        // folder holds 01. Chinese Democracy.flac and so on.
        var dir = TestEnvironment.Media("cd_rip_folder");
        Assert.Equal((InputKind.Folder, dir), InputClassifier.Classify(dir));
        Assert.Null(CueSheet.PlayableUrl(Path.Combine(dir, "Folder Album.cue")));

        // An EAC rip whose sheet names the .wav files it ripped to still plays the sheet.
        var rip = TestEnvironment.Media("cd_rip");
        Assert.Equal((InputKind.File, Path.Combine(rip, "rip.cue")), InputClassifier.Classify(rip));
    }

    [Fact]
    public void A_folder_with_several_cue_sheets_takes_the_one_naming_most_of_its_files()
    {
        var dir = TestEnvironment.TempDir();
        File.WriteAllText(Path.Combine(dir, "01 One.flac"), "");
        File.WriteAllText(Path.Combine(dir, "02 Two.flac"), "");
        File.WriteAllText(Path.Combine(dir, "a.cue"), "FILE \"Image.flac\" WAVE\n  TRACK 01 AUDIO\n    INDEX 01 00:00:00\n");
        File.WriteAllText(Path.Combine(dir, "b.cue"),
            "FILE \"01 One.wav\" WAVE\n  TRACK 01 AUDIO\n    INDEX 01 00:00:00\n" +
            "FILE \"02 Two.wav\" WAVE\n  TRACK 02 AUDIO\n    INDEX 01 00:00:00\n" +
            "FILE \"03 Gone.wav\" WAVE\n  TRACK 03 AUDIO\n    INDEX 01 00:00:00\n");
        Assert.Equal(Path.Combine(dir, "b.cue"), InputClassifier.CueSheet(dir));
        var url = CueSheet.PlayableUrl(Path.Combine(dir, "b.cue"))!;
        Assert.Contains("01 One.flac", url);
        Assert.Contains("02 Two.flac", url);
        Assert.DoesNotContain("Gone", url);
        Assert.Contains("title=%7%Track 2", url); // numbered as the sheet numbers them
    }

    [Theory]
    [InlineData("utf-16le-bom")]
    [InlineData("utf-16be-bom")]
    [InlineData("utf-16le")]
    public void Utf16_cue_sheets_are_read(string encoding)
    {
        const string text = "TITLE \"Café\"\r\nFILE \"01 Ça.wav\" WAVE\r\n  TRACK 01 AUDIO\r\n    TITLE \"Ça\"\r\n    INDEX 01 00:00:00\r\n";
        var bytes = encoding switch
        {
            "utf-16le-bom" => [0xFF, 0xFE, .. Encoding.Unicode.GetBytes(text)],
            "utf-16be-bom" => [0xFE, 0xFF, .. Encoding.BigEndianUnicode.GetBytes(text)],
            _ => Encoding.Unicode.GetBytes(text),
        };
        var path = Path.Combine(TestEnvironment.TempDir(), "rip.cue");
        File.WriteAllBytes(path, bytes);
        var sheet = CueSheet.Read(path);
        Assert.Equal("Café", sheet.Title);
        Assert.Equal("01 Ça.wav", Assert.Single(sheet.Tracks).File);
        Assert.Equal("Ça", sheet.Tracks[0].Title);
        // UTF-8 and Latin-1 sheets read as before.
        Assert.Equal("Café", CueSheet.Decode(Encoding.UTF8.GetBytes("Café")));
        Assert.Equal("Café", CueSheet.Decode(Encoding.Latin1.GetBytes("Café")));
    }

    [Fact]
    public void The_cover_is_cover_jpg_or_front_jpg_else_one_from_a_covers_folder_or_the_only_picture()
    {
        var rip = TestEnvironment.Media("cd_rip_folder");
        Assert.Equal(Path.Combine(rip, "cover.jpg"), FolderMedia.Cover(rip));

        var dir = TestEnvironment.TempDir();
        Assert.Null(FolderMedia.Cover(dir));
        Directory.CreateDirectory(Path.Combine(dir, "Covers"));
        File.WriteAllText(Path.Combine(dir, "Covers", "Back.jpg"), "");
        File.WriteAllText(Path.Combine(dir, "Covers", "Front.jpg"), "");
        File.WriteAllText(Path.Combine(dir, "booklet 1.jpg"), "");
        File.WriteAllText(Path.Combine(dir, "booklet 2.jpg"), "");
        Assert.Equal(Path.Combine(dir, "Covers", "Front.jpg"), FolderMedia.Cover(dir));
        File.WriteAllText(Path.Combine(dir, "FRONT.PNG"), "");
        Assert.Equal(Path.Combine(dir, "FRONT.PNG"), FolderMedia.Cover(dir));
        File.WriteAllText(Path.Combine(dir, "Cover.jpeg"), "");
        Assert.Equal(Path.Combine(dir, "Cover.jpeg"), FolderMedia.Cover(dir)); // cover before front

        // No name to go by: the only picture, or the one named like the album.
        var lone = TestEnvironment.TempDir();
        File.WriteAllText(Path.Combine(lone, "R-418668-1337373996-1103.jpeg.jpg"), "");
        Assert.Equal(Path.Combine(lone, "R-418668-1337373996-1103.jpeg.jpg"), FolderMedia.Cover(lone));
        var named = TestEnvironment.TempDir();
        File.WriteAllText(Path.Combine(named, "GunsNRoses_ChineseDemocracy.jpg"), "");
        File.WriteAllText(Path.Combine(named, "inlay.jpg"), "");
        Assert.Equal(Path.Combine(named, "GunsNRoses_ChineseDemocracy.jpg"), FolderMedia.Cover(named, "GunsNRoses ChineseDemocracy"));
        Assert.Null(FolderMedia.Cover(named));
    }

    [Fact]
    public void Music_files_get_their_folders_cover_and_videos_dont()
    {
        var rip = TestEnvironment.Media("cd_rip_folder");
        Assert.Equal(Path.Combine(rip, "cover.jpg"), PlayerService.CoverFor(Path.Combine(rip, "2. Middle.flac")));
        Assert.Null(PlayerService.CoverFor(Path.Combine(rip, "concert.mkv")));
        Assert.Null(PlayerService.CoverFor("https://example.com/song.mp3"));
        Assert.Null(PlayerService.CoverFor("edl://%3%a.flac"));
        var cover = "/music/A, B; C/cover.jpg";
        Assert.Equal($"cover-art-files-append=%{Encoding.UTF8.GetByteCount(cover)}%{cover}", PlayerService.CoverOption(cover));
        Assert.Null(PlayerService.CoverOption(null));
        Assert.Contains(MpvOptionsBuilder.Core(new PortablePaths(TestEnvironment.TempDir()), null),
            o => o.Key == "cover-art-auto" && o.Value == "no");
    }
}

/// <summary>
/// Prompt 13: every project the app is built from is credited, with its authors and licence
/// (CREDITS.md), and the app says it's a free, non-commercial project.
/// </summary>
public class CreditsTests
{
    private static string Credits => File.ReadAllText(Path.Combine(TestEnvironment.RepoRoot, "CREDITS.md"));
    private static string Repo(params string[] path) => File.ReadAllText(Path.Combine([TestEnvironment.RepoRoot, .. path]));

    [Fact]
    public void Every_flatpak_module_is_credited()
    {
        var manifest = Repo("packaging", "io.github.raulblideran.AtlasPlayer.yaml");
        var modules = System.Text.RegularExpressions.Regex.Matches(manifest, @"^  - name: (\S+)", System.Text.RegularExpressions.RegexOptions.Multiline)
            .Select(m => m.Groups[1].Value).Except(["atlas-player", "atlas-torrent", "qtgmc-check"]).ToList();
        Assert.NotEmpty(modules);
        foreach (var module in modules) Assert.Contains(module, Credits, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Every_downloaded_project_is_credited()
    {
        var sources = Repo("packaging", "io.github.raulblideran.AtlasPlayer.yaml") + Repo("build", "fetch-libs.sh") + Repo("build", "fetch-qtgmc-win.sh");
        var repos = System.Text.RegularExpressions.Regex.Matches(sources, @"github(?:usercontent)?\.com/([\w.-]+)/([\w.-]+?)(?:\.git)?[/\s""]")
            .Select(m => m.Groups[2].Value).Distinct().ToList();
        Assert.Contains("mpv-winbuild-cmake", repos);
        foreach (var repo in repos) Assert.Contains(repo, Credits, StringComparison.OrdinalIgnoreCase);
        foreach (var host in new[] { "videolan", "xiph", "python.org", "x265" })
            if (sources.Contains(host, StringComparison.OrdinalIgnoreCase)) Assert.Contains(host, Credits, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Every_nuget_package_and_rust_crate_is_credited()
    {
        var projects = Directory.GetFiles(Path.Combine(TestEnvironment.RepoRoot, "src"), "*.csproj", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(Path.Combine(TestEnvironment.RepoRoot, "tests"), "*.csproj", SearchOption.AllDirectories));
        foreach (var package in projects.SelectMany(p => System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(p), @"PackageReference Include=""([^""]+)""").Select(m => m.Groups[1].Value)).Distinct())
        {
            var name = package.Split('.')[0] is "Avalonia" ? "Avalonia" : package;
            Assert.Contains(name, Credits, StringComparison.OrdinalIgnoreCase);
        }
        var lockFile = Repo("native", "atlas-torrent", "Cargo.lock");
        var crates = System.Text.RegularExpressions.Regex.Matches(lockFile, "name = \"([^\"]+)\"\nversion = \"([^\"]+)\"\nsource")
            .Select(m => $"| {m.Groups[2].Value} |").ToList();
        Assert.True(crates.Count > 100);
        Assert.All(crates, c => Assert.Contains(c, Credits));
    }

    [Fact]
    public void Credits_and_the_licence_ship_beside_the_app()
    {
        var csproj = Repo("src", "AtlasPlayer.App", "AtlasPlayer.App.csproj");
        Assert.Contains(@"..\..\CREDITS.md;..\..\LICENSE", csproj);
        Assert.Contains("non-commercial", Credits);
        Assert.Contains("CREDITS.md", Repo("README.md"));
        Assert.Contains("non-commercial", Repo("src", "AtlasPlayer.App", "Views", "SettingsWindow.axaml"));
    }
}

/// <summary>Prompt 13 with a real libmpv: a CD rip folder plays, with its cover.</summary>
public class Prompt13PlaybackTests
{
    private static string? ExternalCover(PlayerHarness h)
    {
        var count = h.Player.Mpv.GetLong("track-list/count") ?? 0;
        for (var i = 0; i < count; i++)
            if (h.Player.Mpv.GetFlag($"track-list/{i}/external") == true && h.Player.Mpv.GetString($"track-list/{i}/type") == "video")
                return h.Player.Mpv.GetString($"track-list/{i}/external-filename");
        return null;
    }

    [MpvFact]
    public void A_cd_rip_folders_music_plays_with_its_cover()
    {
        using var h = new PlayerHarness();
        var errors = new List<string?>();
        h.Player.EndFile += (reason, error) => { if (reason == Mpv.EndFileReason.Error) errors.Add(error); };
        var files = FolderMedia.Playlist(TestEnvironment.Media("cd_rip_folder"));
        h.Player.Open(files[0]);
        foreach (var f in files.Skip(1)) h.Player.Append(f);
        PlayerHarness.WaitUntil(() => h.Player.State.PlaylistCount == 3, "the folder's three tracks");
        PlayerHarness.WaitUntil(() => ExternalCover(h) is not null, "the cover");
        Assert.Equal(TestEnvironment.Media(Path.Combine("cd_rip_folder", "cover.jpg")), ExternalCover(h));
        PlayerHarness.WaitUntil(() => h.Player.State.HasVideo is not null, "tracks");
        Assert.False(h.Player.State.HasVideo); // music, with a picture
        Assert.Contains(h.Player.Tracks, t => t.Type == TrackType.Video && t.AlbumArt && t.Selected);
        // On to the next track: its cover comes with it.
        h.Player.PlaylistStep(1);
        PlayerHarness.WaitUntil(() => h.Player.State.PlaylistPos == 1 && ExternalCover(h) is not null, "the second track, with the cover");
        Assert.Empty(errors);
    }

    [MpvFact]
    public void The_folders_cover_shows_rather_than_the_one_inside_the_file()
    {
        // music_cover.flac carries a 200×200 test picture; the folder's cover.jpg is 300×300.
        var dir = TestEnvironment.TempDir();
        File.Copy(TestEnvironment.Media("music_cover.flac"), Path.Combine(dir, "01 song.flac"));
        File.Copy(TestEnvironment.Media(Path.Combine("cd_rip_folder", "cover.jpg")), Path.Combine(dir, "cover.jpg"));
        using var h = new PlayerHarness();
        h.OpenAndWait(Path.Combine(dir, "01 song.flac"));
        PlayerHarness.WaitUntil(() => h.Player.Mpv.GetLong("current-tracks/video/demux-w") == 300, "the folder's cover on screen");
        Assert.True(h.Player.Mpv.GetFlag("current-tracks/video/external"));
    }

    [MpvFact]
    public void A_cue_sheets_timeline_shows_the_folders_cover()
    {
        var rip = TestEnvironment.Media("cd_rip");
        var dir = TestEnvironment.TempDir();
        foreach (var f in Directory.GetFiles(rip)) File.Copy(f, Path.Combine(dir, Path.GetFileName(f)));
        File.Copy(TestEnvironment.Media(Path.Combine("cd_rip_folder", "cover.jpg")), Path.Combine(dir, "Front.jpg"));
        using var h = new PlayerHarness();
        var cover = FolderMedia.Cover(dir);
        Assert.Equal(Path.Combine(dir, "Front.jpg"), cover);
        h.Player.Open(CueSheet.PlayableUrl(Path.Combine(dir, "rip.cue"))!, "Test Album", coverArt: cover);
        PlayerHarness.WaitUntil(() => ExternalCover(h) == cover, "the cover with the timeline");
        PlayerHarness.WaitUntil(() => h.Player.Chapters.Count == 2, "the tracks as chapters");
    }
}
