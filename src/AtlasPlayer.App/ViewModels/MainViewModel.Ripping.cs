using System.Globalization;
using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AtlasPlayer.App.Services;
using AtlasPlayer.Core.Input;
using AtlasPlayer.Core.Playback;
using AtlasPlayer.Core.Ripping;
using AtlasPlayer.Core.Ripping.Cd;
using AtlasPlayer.Core.Ripping.Dvd;
using AtlasPlayer.Core.Settings;

namespace AtlasPlayer.App.ViewModels;

/// <summary>
/// Ripping: the job runs here rather than in its window, so closing the window leaves it
/// running, with its progress (and Cancel) at the top of the player, as downloads do.
/// </summary>
public sealed partial class MainViewModel
{
    [ObservableProperty] private bool _isRipping;
    [ObservableProperty] private string _ripStatus = "";
    [ObservableProperty] private double _ripFraction;
    /// <summary>How the last rip went, for its window.</summary>
    [ObservableProperty] private string _ripResult = "";
    [ObservableProperty] private string? _lastRipFolder;

    private CancellationTokenSource? _ripCts;
    private Task? _ripTask;

    /// <summary>MusicBrainz asks for a User-Agent that names the app and where it's from.</summary>
    internal static readonly HttpClient RipHttp = MakeRipHttp();

    private static HttpClient MakeRipHttp()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        // A product token has no spaces: "AtlasPlayer/2.0.0".
        http.DefaultRequestHeaders.UserAgent.ParseAdd($"{AppInfo.Name.Replace(" ", "", StringComparison.Ordinal)}/{AppInfo.Version} ( {AppInfo.Homepage} )");
        return http;
    }

    internal CdRipper CdRipper => new(RipHttp, _paths.RipCache);
    internal PortablePaths Paths => _paths;

    [RelayCommand]
    private async Task OpenRipper()
    {
        if (Dialogs is not null) await Dialogs.ShowRipperAsync(null);
    }

    [RelayCommand] private void CancelRip() => _ripCts?.Cancel();

    [RelayCommand]
    private void OpenRipFolder()
    {
        if (LastRipFolder is { } folder) Dialogs?.OpenFolder(File.Exists(folder) ? Path.GetDirectoryName(folder)! : folder);
    }

    /// <summary>A disc playing can't be ripped at the same time: the player lets go of it.</summary>
    private void StopDiscPlayback()
    {
        if (!IsIdle && _openedKind is InputKind.AudioCd or InputKind.Dvd or InputKind.Bluray) Execute(PlayerAction.Stop);
    }

    /// <summary>Rips the CD in <paramref name="drive"/>, which is disposed when done.</summary>
    public Task StartCdRipAsync(ICdDrive drive, CdDisc disc, CdRipOptions options) =>
        RunRipAsync("CD", async (progress, ct) =>
        {
            try
            {
                var r = await CdRipper.RipAsync(drive, disc, options, progress, ct);
                var accurate = disc.AccurateRip.Count == 0 ? "not in AccurateRip"
                    : r.AllAccurate ? "all accurately ripped (AccurateRip)"
                    : $"{r.Tracks.Count(t => t.AccurateRip.Accurate)} of {r.Tracks.Count} accurately ripped";
                var errors = r.AllOk ? "" : $", {r.Tracks.Count(t => !t.Read.Ok)} with errors";
                var all = disc.Toc.AudioTrackCount;
                var count = r.Tracks.Count == all ? $"{all} tracks"
                    : r.Tracks.Count == 1 ? $"track {r.Tracks[0].Tags.Number} of {all}"
                    : $"{r.Tracks.Count} of {all} tracks";
                var verdict = r.Tracks.Count == 1 && disc.AccurateRip.Count > 0
                    ? (r.AllAccurate ? "accurately ripped (AccurateRip)" : "not verified as accurate") : accurate;
                return (r.Folder, $"Ripped {count} to {r.Folder}: {verdict}{errors}. Log score {r.Score}%.");
            }
            finally
            {
                drive.Dispose();
            }
        });

    public Task StartDvdRipAsync(DvdInfo info, DvdRipOptions options) => StartDvdRipAsync(info, [options]);

    /// <summary>Rips a DVD's titles, one job each (or its VIDEO_TS folder), as one rip with one progress bar.</summary>
    public Task StartDvdRipAsync(DvdInfo info, IReadOnlyList<DvdRipOptions> jobs) =>
        RunRipAsync("DVD", async (progress, ct) =>
        {
            var r = await DvdRipper.RipAsync(info, jobs, progress, ct);
            var problems = r.Problems.Count(p => !p.Contains("left out", StringComparison.Ordinal));
            var what = jobs.Count > 1 ? $"Ripped {jobs.Count} titles to {r.Output}" : $"Saved to {r.Output}";
            return (r.Output, what + (problems == 0 ? "." : $": {problems} file(s) had unreadable parts (see its log)."));
        });

    private async Task RunRipAsync(string what, Func<IProgress<RipProgress>, CancellationToken, Task<(string Output, string Summary)>> rip)
    {
        if (IsRipping) return;
        StopDiscPlayback();
        using var cts = _ripCts = new CancellationTokenSource();
        IsRipping = true;
        RipFraction = 0;
        RipResult = "";
        RipStatus = $"Ripping the {what}…";
        var progress = new Reporter(p => _ui.Post(() =>
        {
            if (!IsRipping) return;
            RipFraction = p.Fraction;
            RipStatus = DescribeRip(p);
        }));
        try
        {
            var task = Task.Run(() => rip(progress, cts.Token));
            _ripTask = task;
            var (output, summary) = await task;
            LastRipFolder = output;
            RipResult = summary;
            ShowStatus(summary);
        }
        catch (OperationCanceledException)
        {
            RipResult = "Rip cancelled: nothing was kept.";
            ShowStatus(RipResult);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException or InvalidDataException
                                      or ArgumentException or HttpRequestException or DllNotFoundException or EntryPointNotFoundException)
        {
            RipResult = $"Rip failed: {e.Message}";
            ShowStatus(RipResult);
        }
        catch (Exception e)
        {
            // Unforeseen (flac or ffmpeg that won't start, a reply that isn't JSON…): still said,
            // never a rip that just stops. Its stack goes to crash.log for a report.
            var log = CrashLog.Write(_paths.Logs, e);
            RipResult = $"Rip failed: {e.Message}";
            ShowStatus(RipResult + (log is null ? "" : CrashLogHint));
        }
        finally
        {
            IsRipping = false;
            RipStatus = "";
            _ripCts = null;
        }
    }

    /// <summary>"Track 3/14 · Copy 42% · 8.1×", "Writing title 1 · 37%".</summary>
    internal static string DescribeRip(RipProgress p)
    {
        var inv = CultureInfo.InvariantCulture;
        var percent = string.Create(inv, $"{p.Fraction * 100:0}%");
        var speed = p.Speed >= 0.1 ? string.Create(inv, $" · {p.Speed:0.0}×") : "";
        // A DVD's encode: its speed against the film's (1.0× takes as long as the film).
        if (p.Tracks == 0) return $"{p.Stage} · {percent}{speed}";
        if (p.Track == 0) return p.Stage + "…";
        return $"Track {p.Track}/{p.Tracks} · {p.Stage} · {percent}{speed}";
    }

    /// <summary>Reports straight away, on whatever thread the rip runs (Progress&lt;T&gt; would need a context).</summary>
    private sealed class Reporter(Action<RipProgress> report) : IProgress<RipProgress>
    {
        public void Report(RipProgress value) => report(value);
    }
}
