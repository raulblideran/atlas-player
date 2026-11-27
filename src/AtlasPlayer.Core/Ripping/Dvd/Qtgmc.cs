using System.Text;

namespace AtlasPlayer.Core.Ripping.Dvd;

/// <summary>
/// Where QTGMC runs from: a Python with VapourSynth's module (<paramref name="Modules"/> also
/// holds havsfunc, mvsfunc and adjust, and on Linux MVTools, installed from its wheel into
/// VapourSynth's own plugin folder) and the plugins QTGMC needs (<paramref name="Plugins"/>; see
/// <see cref="Qtgmc.Namespaces"/>).
/// </summary>
public sealed record QtgmcRuntime(string Python, string Modules, string Plugins);

/// <summary>Whether QTGMC's runtime loads, and if not, why (Python's last lines).</summary>
public sealed record QtgmcProbe(bool Works, string? Reason);

/// <summary>
/// QTGMC, the deinterlacer of havsfunc (r33, its last with QTGMC), for DVD rips only; playback
/// keeps BWDIF. Python runs a small script that reads the title's copy with BestSource, runs
/// QTGMC and writes its frames as y4m to the encoder (<see cref="RipTools.RunPipedAsync"/>):
/// Python itself rather than vspipe, which needs VapourSynth configured for its Python first.
/// </summary>
public static class Qtgmc
{
    public const string DefaultPreset = "Slower";

    /// <summary>
    /// The plugins havsfunc's QTGMC calls, by namespace: MVTools (mv, API 4: v29), znedi3, RemoveGrain
    /// (rgvs), BestSource (bs, which reads the title), fmtconv (fmtc: its Bob), MiscFilters (misc:
    /// its AverageFrames), EEDI3m (looked up on every call, even unused) and FFT3DFilter (Very Slow
    /// and Placebo denoise with it). VapourSynth R82 loads no API 3 plugin.
    /// </summary>
    public static readonly IReadOnlyList<string> Namespaces = ["mv", "znedi3", "rgvs", "bs", "fmtc", "misc", "eedi3m", "fft3dfilter"];

    /// <summary>QTGMC's presets offered, fastest first.</summary>
    public static readonly IReadOnlyList<string> Presets = ["Medium", "Slow", "Slower", "Very Slow", "Placebo"];

    /// <summary>
    /// The bundled VapourSynth: a "vapoursynth" folder next to the app (Windows: its own Python,
    /// the modules in Lib\site-packages) or the Flatpak's /app/lib/vapoursynth (the runtime's
    /// python3, the modules in "python"); ATLAS_VAPOURSYNTH names another such folder. Null
    /// when there's none.
    /// </summary>
    public static QtgmcRuntime? Find(string? appDirectory = null)
    {
        var roots = new List<string>();
        if (Environment.GetEnvironmentVariable("ATLAS_VAPOURSYNTH") is { Length: > 0 } custom) roots.Add(custom);
        roots.Add(Path.Combine(appDirectory ?? AppContext.BaseDirectory, "vapoursynth"));
        if (!OperatingSystem.IsWindows()) roots.Add("/app/lib/vapoursynth");
        foreach (var root in roots)
        {
            var plugins = Path.Combine(root, "plugins");
            if (!Directory.Exists(plugins)) continue;
            var modules = new[] { Path.Combine(root, "python"), Path.Combine(root, "Lib", "site-packages") }.FirstOrDefault(Directory.Exists);
            if (modules is null) continue;
            var python = new[] { Path.Combine(root, "python.exe"), Path.Combine(root, "bin", "python3") }.FirstOrDefault(File.Exists)
                         ?? (OperatingSystem.IsWindows() ? null : RipTools.Find("python3"));
            if (python is not null) return new QtgmcRuntime(python, modules, plugins);
        }
        return null;
    }

    private static readonly Dictionary<QtgmcRuntime, QtgmcProbe> _probed = [];

    /// <summary>The runtime loads: VapourSynth imports, the plugins load and havsfunc has QTGMC (checked once).</summary>
    public static async Task<bool> AvailableAsync(QtgmcRuntime runtime, CancellationToken ct = default) =>
        (await ProbeAsync(runtime, ct).ConfigureAwait(false)).Works;

    /// <summary>
    /// Whether the runtime loads (checked once), and if not, why: Python's last lines (an
    /// ImportError, a plugin that didn't load), so the window can say it rather than only grey
    /// QTGMC out.
    /// </summary>
    public static async Task<QtgmcProbe> ProbeAsync(QtgmcRuntime runtime, CancellationToken ct = default)
    {
        lock (_probed)
            if (_probed.TryGetValue(runtime, out var known)) return known;
        var folder = Path.Combine(Path.GetTempPath(), $"atlas-qtgmc-{Environment.ProcessId}");
        Directory.CreateDirectory(folder);
        var probe = Path.Combine(folder, "probe.py");
        await File.WriteAllTextAsync(probe, ProbeScript, new UTF8Encoding(false), ct).ConfigureAwait(false);
        var output = new List<string>();
        QtgmcProbe result;
        try
        {
            await RipTools.RunAsync(runtime.Python, ["-I", probe, runtime.Modules, runtime.Plugins], ct,
                line => { lock (output) output.Add(line); }, line => { lock (output) output.Add(line); }).ConfigureAwait(false);
            lock (output)
                result = output.Contains("qtgmc ok") ? new QtgmcProbe(true, null) : new QtgmcProbe(false, LastLines(output));
        }
        catch (Exception e) when (e is IOException or System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            lock (output) result = new QtgmcProbe(false, output.Count > 0 ? LastLines(output) : e.Message);
        }
        finally
        {
            try { Directory.Delete(folder, recursive: true); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
        }
        lock (_probed) _probed[runtime] = result;
        return result;
    }

    /// <summary>The last few lines Python wrote, the error's own line last (a traceback ends with it).</summary>
    private static string LastLines(List<string> lines)
    {
        var useful = lines.Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
        return useful.Count == 0 ? "Python wrote nothing" : string.Join(" ", useful.TakeLast(3));
    }

    /// <summary>
    /// Loads VapourSynth, every plugin in the plugin folder (FFTW, which FFT3DFilter links, isn't
    /// one) and havsfunc. VapourSynth's own plugin folder (Linux's MVTools) loads on its own.
    /// </summary>
    private const string Prelude = """
        import os, sys
        modules, plugins = sys.argv[1], sys.argv[2]
        sys.path.insert(0, modules)
        import vapoursynth as vs
        core = vs.core
        for name in sorted(os.listdir(plugins)):
            if name.lower().startswith('libfftw'):
                continue
            if name.lower().endswith(('.so', '.dll', '.dylib')):
                try:
                    core.std.LoadPlugin(os.path.join(plugins, name))
                except vs.Error as e:
                    if 'already loaded' not in str(e):
                        raise
        import havsfunc
        """;

    /// <summary>Checks what QTGMC needs: the plugins' namespaces (<see cref="Namespaces"/>) and havsfunc's QTGMC.</summary>
    internal const string ProbeScript = Prelude + """

        missing = [ns for ns in ('mv', 'znedi3', 'rgvs', 'bs', 'fmtc', 'misc', 'eedi3m', 'fft3dfilter') if not hasattr(core, ns)]
        if missing or not hasattr(havsfunc, 'QTGMC'):
            print('missing ' + ' '.join(missing or ['QTGMC']))
            sys.exit(1)
        print('qtgmc ok')
        """;

    /// <summary>
    /// The title's copy, deinterlaced by QTGMC at a frame per field (or per frame), to stdout as y4m.
    /// BestSource reads it frame-exact; its index isn't kept (cachemode 0).
    /// </summary>
    internal const string Script = Prelude + """

        source, preset, order, divisor = sys.argv[3], sys.argv[4], sys.argv[5], int(sys.argv[6])
        try:
            clip = core.bs.VideoSource(source=source, cachemode=0)
        except vs.Error:
            clip = core.bs.VideoSource(source=source)
        clip = havsfunc.QTGMC(clip, Preset=preset, TFF=(order == 'tff'), FPSDivisor=divisor)
        clip.output(sys.stdout.buffer, y4m=True)
        """;

    /// <summary>Writes the script into the rip's work folder; returns its path.</summary>
    public static string WriteScript(string folder)
    {
        var path = Path.Combine(folder, "qtgmc.py");
        File.WriteAllText(path, Script, new UTF8Encoding(false));
        return path;
    }

    /// <summary>Python's arguments for the script: isolated (no user site, no PYTHONPATH), then the script's own.</summary>
    public static IReadOnlyList<string> Arguments(QtgmcRuntime runtime, string script, string source, string preset, VideoScan scan, bool doubleRate) =>
    [
        "-I", script, runtime.Modules, runtime.Plugins, source,
        Presets.Contains(preset) ? preset : DefaultPreset,
        scan == VideoScan.BottomFieldFirst ? "bff" : "tff",
        doubleRate ? "1" : "2",
    ];
}
