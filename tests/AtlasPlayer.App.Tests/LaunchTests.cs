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
    public async Task Files_handed_over_while_the_first_still_opens_go_after_it()
    {
        // Explorer started three copies at once: this one opens its own file, the others' arrive meanwhile.
        using var r = new Rig();
        var (a, b, c) = (r.MediaFile("a.mkv"), r.MediaFile("b.mkv"), r.MediaFile("c.mkv"));
        await r.Vm.LaunchAsync([a]);
        var handedOver = r.Vm.QueueArgumentsAsync([b, c]);
        Assert.False(handedOver.IsCompleted); // waits for a.mkv
        ResumeTests.Playing(r, a, 0); // it has loaded
        await handedOver;
        Assert.Equal([$"Open {a}", $"Append {b}", $"Append {c}"], r.Player.Calls.Where(x => x.StartsWith("Open") || x.StartsWith("Append")));
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
        var address = NewAddress();
        Assert.False(SingleInstance.TrySend(address, ["nothing listens"]));

        var got = new TaskCompletionSource<IReadOnlyList<string>>();
        using (var first = SingleInstance.Listen(address, args => got.TrySetResult(args)))
        {
            Assert.NotNull(first);
            Assert.True(SingleInstance.TrySend(address, ["/music/01 One.flac", "magnet:?xt=urn:btih:abc"]));
            Assert.Equal(["/music/01 One.flac", "magnet:?xt=urn:btih:abc"], await got.Task.WaitAsync(TimeSpan.FromSeconds(5)));
            Assert.Null(SingleInstance.Listen(address, _ => { })); // one listener
            Assert.True(SingleInstance.TrySend(address, ["a second hand-over"])); // the pipe is there again at once
        }
        Assert.False(SingleInstance.TrySend(address, ["gone"]));
    }

    private static string NewAddress() => OperatingSystem.IsWindows()
        ? $"AtlasPlayerTest-{Guid.NewGuid():N}"
        : Path.Combine(Path.GetTempPath(), $"atlas-si-{Guid.NewGuid():N}"[..20] + ".sock");

    [Fact]
    public async Task Copies_started_together_end_up_in_one_window()
    {
        // Explorer starts a copy per file opened, all at once: one listens, the rest hand over.
        var address = NewAddress();
        var received = new System.Collections.Concurrent.ConcurrentQueue<string>();
        using var start = new Barrier(5);
        var copies = Enumerable.Range(1, 5).Select(i => Task.Run(() =>
        {
            start.SignalAndWait();
            var listener = SingleInstance.Claim(address, [$"file{i}.mkv"], args => received.Enqueue(args[0]), out var alone);
            Assert.False(alone);
            return (Listener: listener, File: $"file{i}.mkv");
        })).ToArray();
        var results = await Task.WhenAll(copies);
        var winners = results.Where(r => r.Listener is not null).ToList();
        try
        {
            Assert.Single(winners);
            for (var i = 0; i < 100 && received.Count < 4; i++) await Task.Delay(20);
            Assert.Equal(results.Where(r => r.Listener is null).Select(r => r.File).Order(), received.Order());
        }
        finally
        {
            foreach (var w in winners) w.Listener!.Dispose();
        }
    }

    [Fact]
    public void A_copy_with_nobody_to_hand_over_to_runs_alone_after_waiting()
    {
        if (OperatingSystem.IsWindows()) return;
        var address = Path.Combine(Path.GetTempPath(), "no-such-folder-" + Guid.NewGuid().ToString("N"), "x.sock");
        Assert.Null(SingleInstance.Claim(address, ["a"], _ => { }, out var alone, TimeSpan.FromMilliseconds(300)));
        Assert.True(alone);
    }

    [Fact]
    public void Files_handed_over_before_the_window_is_ready_are_held()
    {
        try
        {
            HandOvers.Detach();
            HandOvers.Receive(["early.mkv"]);
            var got = new List<string>();
            HandOvers.Attach(args => got.AddRange(args));
            HandOvers.Receive(["later.mkv"]);
            Assert.Equal(["early.mkv", "later.mkv"], got);
        }
        finally
        {
            HandOvers.Detach();
        }
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
