using AtlasPlayer.App.Services;

namespace AtlasPlayer.App.Tests;

/// <summary>Every file Atlas Player is started with plays, in one window (2.0.0-beta.14).</summary>
public class LaunchTests
{
    [Fact]
    public void File_uris_and_relative_paths_become_full_paths_and_links_stay()
    {
        var dir = Directory.CreateTempSubdirectory("atlas-launch").FullName;
        var file = Path.Combine(dir, "My Film (1999).mkv");
        File.WriteAllText(file, "");
        var uri = new Uri(file).AbsoluteUri; // file:///…/My%20Film%20(1999).mkv, as a file manager passes it
        const string magnet = "magnet:?xt=urn:btih:08ada5a7a6183aae1e09d831df6748d566095a10";
        var relative = Path.GetRelativePath(Environment.CurrentDirectory, file);
        Assert.Equal([file, file, magnet, "https://youtu.be/x"], LaunchArgs.Normalize([uri, relative, magnet, "https://youtu.be/x", " "]));
        Assert.True(LaunchArgs.IsLocal(file));
        Assert.False(LaunchArgs.IsLocal(magnet));
    }

    [Fact]
    public async Task Every_file_passed_plays_the_first_first()
    {
        using var r = new Rig();
        var (a, b, c) = (r.MediaFile("a.mkv"), r.MediaFile("b.mkv"), r.MediaFile("c.mkv"));
        await r.Vm.OpenArgumentsAsync([a, b, c]);
        Assert.Equal([$"Open {a}", $"Append {b}", $"Append {c}"], r.Player.Calls.Where(x => x.StartsWith("Open") || x.StartsWith("Append")));
    }

    [Fact]
    public async Task A_link_on_its_own_opens()
    {
        using var r = new Rig();
        await r.Vm.OpenArgumentsAsync(["magnet:?xt=urn:btih:08ada5a7a6183aae1e09d831df6748d566095a10"]);
        Assert.Contains(r.Torrents.Calls, c => c.StartsWith("Add magnet:"));
    }

    [Fact]
    public async Task Files_opened_while_something_plays_join_the_queue()
    {
        using var r = new Rig();
        var playing = r.MediaFile("playing.mkv");
        await r.Vm.OpenArgumentsAsync([playing]);
        ResumeTests.Playing(r, playing, 10);
        var folder = Path.Combine(r.Root, "Album");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "02 Two.flac"), "");
        File.WriteAllText(Path.Combine(folder, "01 One.flac"), "");
        File.WriteAllText(Path.Combine(folder, "rip.log"), "");
        var next = r.MediaFile("next.mkv");
        r.Player.Calls.Clear();

        await r.Vm.QueueArgumentsAsync([next, folder]);
        Assert.Equal([$"Append {next}", $"Append {Path.Combine(folder, "01 One.flac")}", $"Append {Path.Combine(folder, "02 Two.flac")}"],
            r.Player.Calls);
        Assert.Equal("Added 3 files to the queue", r.Vm.StatusMessage);
    }

    [Fact]
    public async Task With_nothing_playing_they_simply_play()
    {
        using var r = new Rig();
        var file = r.MediaFile();
        await r.Vm.QueueArgumentsAsync([file]);
        Assert.Contains($"Open {file}", r.Player.Calls);
        await r.Vm.QueueArgumentsAsync([]); // started again with nothing: nothing changes
        Assert.Single(r.Player.Calls, c => c.StartsWith("Open"));
    }

    [Fact]
    public async Task A_second_start_hands_its_files_to_the_first()
    {
        var address = OperatingSystem.IsWindows()
            ? $"AtlasPlayerTest-{Guid.NewGuid():N}"
            : Path.Combine(Path.GetTempPath(), $"atlas-si-{Guid.NewGuid():N}"[..20] + ".sock");
        Assert.False(SingleInstance.TrySend(address, ["nothing listens"]));

        var got = new TaskCompletionSource<IReadOnlyList<string>>();
        using (var first = SingleInstance.Listen(address, args => got.TrySetResult(args)))
        {
            Assert.NotNull(first);
            Assert.True(SingleInstance.TrySend(address, ["/music/01 One.flac", "magnet:?xt=urn:btih:abc"]));
            Assert.Equal(["/music/01 One.flac", "magnet:?xt=urn:btih:abc"], await got.Task.WaitAsync(TimeSpan.FromSeconds(5)));
            if (!OperatingSystem.IsWindows()) Assert.Null(SingleInstance.Listen(address, _ => { })); // one listener
        }
        Assert.False(SingleInstance.TrySend(address, ["gone"]));
    }

    [Fact]
    public void A_socket_left_by_a_crash_is_taken_over()
    {
        if (OperatingSystem.IsWindows()) return; // pipes go with their process
        var address = Path.Combine(Path.GetTempPath(), $"atlas-si-{Guid.NewGuid():N}"[..20] + ".sock");
        // .NET removes a socket's file when it closes; a crash leaves it, with nothing answering.
        File.WriteAllText(address, "");
        Assert.False(SingleInstance.TrySend(address, ["x"]));
        using var second = SingleInstance.Listen(address, _ => { });
        Assert.NotNull(second);
    }

    [Fact]
    public void Each_copy_of_the_app_has_its_own_address()
    {
        var address = SingleInstance.Address();
        if (OperatingSystem.IsWindows()) Assert.StartsWith("AtlasPlayer-", address);
        else if (Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR") is { Length: > 0 }) Assert.EndsWith(".sock", address);
    }

    [Fact]
    public void Junk_sent_to_the_socket_is_ignored()
    {
        Assert.Null(SingleInstance.Parse("not json"u8.ToArray()));
        Assert.Null(SingleInstance.Parse("{\"a\":1}"u8.ToArray()));
        Assert.Equal(["x"], SingleInstance.Parse("[\"x\"]"u8.ToArray()));
    }
}
