using AtlasPlayer.App.ViewModels;

namespace AtlasPlayer.App.Tests;

/// <summary>The torrent's line and its engine calls (2.0.0-beta.14).</summary>
public class TorrentStatusTests
{
    private const string Magnet = "magnet:?xt=urn:btih:08ada5a7a6183aae1e09d831df6748d566095a10&dn=Sintel";

    [Fact]
    public void The_line_says_what_is_buffered_ahead_and_what_has_downloaded()
    {
        Assert.Equal("↓ 4.2 MiB/s · 31 peers · 45 s ahead · 18% downloaded", MainViewModel.DescribeTorrent(4.2, 31, 45.4, 18.2));
        Assert.Equal("↓ 0.0 MiB/s · 0 peers · 0% downloaded", MainViewModel.DescribeTorrent(0, 0, null, 0)); // not playing yet
    }

    [Fact]
    public async Task Switching_file_never_holds_up_the_window()
    {
        using var r = new Rig();
        await r.Vm.OpenInputAsync(Magnet);
        using var gate = new ManualResetEventSlim();
        r.Torrents.SelectGate = gate; // the engine waits, as for a torrent still starting

        r.Vm.SelectedTorrentFile = r.Vm.TorrentFiles.Single(f => f.Index == 2); // returns at once
        Assert.Equal("Switching to Trailer.mkv…", r.Vm.StatusMessage);
        Assert.DoesNotContain(r.Player.Calls, c => c.Contains("Trailer"));

        gate.Set();
        await r.Vm.PendingTorrentWork;
        Assert.Contains("Open http://127.0.0.1:9/stream/1/2 Trailer.mkv [stream]", r.Player.Calls);
        Assert.Equal("Streaming Trailer.mkv", r.Vm.StatusMessage);
    }

    [Fact]
    public async Task A_file_picked_while_another_was_switching_wins()
    {
        using var r = new Rig();
        await r.Vm.OpenInputAsync(Magnet);
        using var gate = new ManualResetEventSlim();
        r.Torrents.SelectGate = gate;
        r.Vm.SelectedTorrentFile = r.Vm.TorrentFiles.Single(f => f.Index == 2);
        var first = r.Vm.PendingTorrentWork;
        r.Vm.SelectedTorrentFile = r.Vm.TorrentFiles.Single(f => f.Index == 1);
        gate.Set();
        await first;
        await r.Vm.PendingTorrentWork;
        Assert.DoesNotContain(r.Player.Calls, c => c.Contains("Trailer"));
        Assert.Contains("Open http://127.0.0.1:9/stream/1/1 Sintel.mp4 [stream]", r.Player.Calls);
    }

    [Fact]
    public async Task Reopening_a_torrent_just_closed_waits_for_it_to_go()
    {
        using var r = new Rig();
        await r.Vm.OpenInputAsync(Magnet);
        await r.Vm.OpenInputAsync(r.MediaFile()); // closes it, in the background
        await r.Vm.OpenInputAsync(Magnet);
        var calls = r.Torrents.Calls;
        Assert.True(calls.LastIndexOf("Remove 1 True") < calls.LastIndexOf($"Add {Magnet}"));
    }
}
