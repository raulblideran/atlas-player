using AtlasPlayer.Core.Playback;
using AtlasPlayer.Core.Settings;

namespace AtlasPlayer.Core.Tests;

/// <summary>Where each local file was left (2.0.0-beta.14).</summary>
public class ResumePositionsTests
{
    private static string File(string dir, string name)
    {
        var path = Path.Combine(dir, name);
        System.IO.File.WriteAllText(path, "");
        return path;
    }

    [Fact]
    public void Only_files_on_this_machine_are_remembered()
    {
        var dir = TestEnvironment.TempDir();
        var film = File(dir, "film.mkv");
        Assert.Equal(film, ResumePositions.KeyFor(film));
        Assert.Equal(film, ResumePositions.KeyFor(Path.Combine(dir, ".", "film.mkv")));
        Assert.Null(ResumePositions.KeyFor(Path.Combine(dir, "gone.mkv")));
        Assert.Null(ResumePositions.KeyFor("https://example.com/a.mp4"));
        Assert.Null(ResumePositions.KeyFor("http://127.0.0.1:9/stream/1/1"));
        Assert.Null(ResumePositions.KeyFor("dvd://"));
        Assert.Null(ResumePositions.KeyFor(null));
        Assert.Null(ResumePositions.KeyFor(""));
    }

    [Fact]
    public void Positions_are_kept_between_runs()
    {
        var dir = TestEnvironment.TempDir();
        var file = Path.Combine(dir, "positions.json");
        var film = File(dir, "film.mkv");
        new ResumePositions(file).Set(film, 754.5, 3000, DateTime.UtcNow);
        var again = new ResumePositions(file);
        Assert.Equal(754.5, again.Get(film));
        Assert.Null(again.Get(Path.Combine(dir, "other.mkv")));
    }

    [Fact]
    public void Less_than_a_second_in_is_nothing_to_resume()
    {
        var dir = TestEnvironment.TempDir();
        var positions = new ResumePositions(Path.Combine(dir, "positions.json"));
        positions.Set("a", 0.4, 100, DateTime.UtcNow);
        Assert.Null(positions.Get("a"));
        positions.Set("a", double.NaN, 100, DateTime.UtcNow); // ignored
        positions.Set("a", 1, 100, DateTime.UtcNow);
        Assert.Equal(1, positions.Get("a"));
    }

    [Fact]
    public void The_200_most_recent_are_kept()
    {
        var dir = TestEnvironment.TempDir();
        var positions = new ResumePositions(Path.Combine(dir, "positions.json"));
        var start = new DateTime(2026, 11, 28, 0, 0, 0, DateTimeKind.Utc);
        for (var i = 0; i < ResumePositions.Capacity + 5; i++) positions.Set($"file{i}", 10, 100, start.AddMinutes(i));
        Assert.Equal(ResumePositions.Capacity, positions.Count);
        Assert.Null(positions.Get("file4")); // the oldest went
        Assert.Equal(10, positions.Get("file5"));
        Assert.Equal(ResumePositions.Capacity, new ResumePositions(Path.Combine(dir, "positions.json")).Count);
    }

    [Fact]
    public void A_damaged_file_starts_over()
    {
        var dir = TestEnvironment.TempDir();
        var file = Path.Combine(dir, "positions.json");
        System.IO.File.WriteAllText(file, "{ not json");
        var positions = new ResumePositions(file);
        Assert.Equal(0, positions.Count);
        positions.Set("a", 5, 10, DateTime.UtcNow);
        Assert.Equal(5, new ResumePositions(file).Get("a"));
    }

    [Fact]
    public void Resume_is_switched_on_for_everyone_once()
    {
        var path = Path.Combine(TestEnvironment.TempDir(), "settings.json");
        // beta.13 and before: an unused switch, off.
        System.IO.File.WriteAllText(path, """{ "settingsVersion": 7, "resumePlayback": false }""");
        var moved = new SettingsStore(path).Load();
        Assert.True(moved.ResumePlayback);
        Assert.Equal(8, moved.SettingsVersion);
        // Switched off since: it stays off.
        System.IO.File.WriteAllText(path, """{ "settingsVersion": 8, "resumePlayback": false }""");
        Assert.False(new SettingsStore(path).Load().ResumePlayback);
        Assert.Equal(Path.Combine(Path.GetDirectoryName(path)!, "positions.json"), new PortablePaths(Path.GetDirectoryName(path)!).ResumeFile);
    }
}
