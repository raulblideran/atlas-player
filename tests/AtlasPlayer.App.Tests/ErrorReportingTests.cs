using System.Net;
using AtlasPlayer.App.Services;
using AtlasPlayer.App.ViewModels;
using AtlasPlayer.Core.Ripping.Cd;

namespace AtlasPlayer.App.Tests;

/// <summary>Failures are said and logged, never lost (2.0.0-beta.14).</summary>
public class ErrorReportingTests
{
    [Fact]
    public async Task A_task_nobody_waits_for_says_when_it_fails_and_logs_it()
    {
        using var r = new Rig();
        var failing = Task.Run(() => throw new InvalidOperationException("the encoder list came back empty"));
        r.Vm.Forget(failing);
        await Assert.ThrowsAsync<InvalidOperationException>(() => failing);
        for (var i = 0; i < 100 && !r.Vm.StatusMessage.StartsWith("Something went wrong"); i++) await Task.Delay(20);
        Assert.StartsWith("Something went wrong: the encoder list came back empty", r.Vm.StatusMessage);
        Assert.Contains("crash.log", r.Vm.StatusMessage);
        Assert.True(r.Vm.ToastVisible);
        Assert.Contains("the encoder list came back empty", File.ReadAllText(Path.Combine(r.Paths.Logs, CrashLog.FileName)));
    }

    [Fact]
    public async Task A_task_that_finishes_or_is_cancelled_says_nothing()
    {
        using var r = new Rig();
        r.Vm.Forget(Task.CompletedTask);
        r.Vm.Forget(Task.FromCanceled(new CancellationToken(true)));
        await Task.Delay(50);
        Assert.Equal("", r.Vm.StatusMessage);
        Assert.False(File.Exists(Path.Combine(r.Paths.Logs, CrashLog.FileName)));
    }

    /// <summary>A drive whose reads fail in a way nobody planned for.</summary>
    private sealed class BrokenDrive(ICdDrive inner) : ICdDrive
    {
        public DriveIdentity Identity => inner.Identity;
        public DriveFeatures Features => inner.Features;
        public CdToc ReadToc() => inner.ReadToc();
        public CdText? ReadCdText() => inner.ReadCdText();
        public string? ReadMcn() => inner.ReadMcn();
        public string? ReadIsrc(int track) => inner.ReadIsrc(track);
        public void ReadAudio(int lba, int sectors, Span<byte> buffer) => throw new System.ComponentModel.Win32Exception(5, "the drive went away");
        public SubQ? ReadSubQ(int lba) => inner.ReadSubQ(lba);
        public bool CanReadSubQ => inner.CanReadSubQ;
        public void Dispose() => inner.Dispose();
    }

    [Fact]
    public async Task An_unforeseen_rip_failure_is_said_and_logged()
    {
        using var r = new Rig();
        var toc = new CdToc([new CdTrack(1, 0, true), new CdTrack(2, 150, true)], 300);
        var drive = new BrokenDrive(new ImageCdDrive(new MemoryStream(new byte[300 * CdToc.SectorBytes]), toc));
        var disc = await r.Vm.CdRipper.ReadDiscAsync(drive, online: false);
        await r.Vm.StartCdRipAsync(drive, disc, new CdRipOptions
        {
            Tags = disc.DefaultTags, Format = CdRipFormat.Wav, OutputRoot = r.Paths.Music, Offset = 0,
        });
        Assert.False(r.Vm.IsRipping);
        Assert.Equal("Rip failed: the drive went away", r.Vm.RipResult);
        Assert.Contains("crash.log", r.Vm.StatusMessage);
        Assert.Contains("Win32Exception", File.ReadAllText(Path.Combine(r.Paths.Logs, CrashLog.FileName)));
    }

    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> answer) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(answer(request));
    }

    [Theory]
    [InlineData("timeout", "No cover found (timed out)")]
    [InlineData("offline", "No cover found")] // each size tried, none answered
    [InlineData("missing", "No cover found")]
    public async Task The_cover_square_says_why_there_is_no_cover(string what, string note)
    {
        using var r = new Rig();
        var vm = new RipDialogViewModel(r.Vm, [], online: false);
        Assert.Equal("No cover", vm.CoverNote);
        using var http = new HttpClient(new Handler(_ => what switch
        {
            "timeout" => throw new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout"),
            "offline" => throw new HttpRequestException("no route to host"),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound),
        }));
        await vm.LoadCoverAsync(new AlbumTags { ReleaseId = "6e1a9b9b-0000-4000-8000-000000000000" }, http);
        Assert.Equal(note, vm.CoverNote);
        Assert.Null(vm.CoverImage);
    }
}
