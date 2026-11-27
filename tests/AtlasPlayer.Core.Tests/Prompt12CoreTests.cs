using AtlasPlayer.Core.Ripping.Dvd;

namespace AtlasPlayer.Core.Tests;

/// <summary>
/// Prompt 12: QTGMC didn't load anywhere (beta.11's MVTools was an API 3 build, which VapourSynth
/// R82 refuses, and fmtconv, MiscFilters, EEDI3m and FFT3DFilter weren't there). Every plugin it
/// calls is checked now, and when the runtime doesn't load, the rip window says why.
/// </summary>
public class Prompt12CoreTests
{
    [Fact]
    public void The_probe_and_the_build_check_ask_for_every_plugin_qtgmc_calls()
    {
        Assert.Equal(["mv", "znedi3", "rgvs", "bs", "fmtc", "misc", "eedi3m", "fft3dfilter"], Qtgmc.Namespaces);
        var quoted = string.Join(", ", Qtgmc.Namespaces.Select(n => $"'{n}'"));
        Assert.Contains($"for ns in ({quoted})", Qtgmc.ProbeScript);
        var check = File.ReadAllText(Path.Combine(TestEnvironment.RepoRoot, "build", "qtgmc-check.py"));
        Assert.Contains($"NAMESPACES = ({quoted})", check);
        Assert.Contains($"PRESETS = ({string.Join(", ", Qtgmc.Presets.Select(p => $"'{p}'"))})", check);
        // FFTW sits beside FFT3DFilter on Windows, and isn't a plugin to load.
        Assert.Contains("startswith('libfftw')", Qtgmc.ProbeScript);
        Assert.Contains("startswith('libfftw')", Qtgmc.Script);
        Assert.Contains("startswith('libfftw')", check);
    }

    [Fact]
    public void Both_builds_ship_api_4_plugins_and_havsfunc_with_todays_keywords()
    {
        var win = File.ReadAllText(Path.Combine(TestEnvironment.RepoRoot, "build", "fetch-qtgmc-win.sh"));
        var flatpak = File.ReadAllText(Path.Combine(TestEnvironment.RepoRoot, "packaging", "io.github.raulblideran.AtlasPlayer.yaml"));
        foreach (var text in new[] { win, flatpak })
        {
            Assert.Contains("vapoursynth_mvtools-29-", text);
            Assert.DoesNotContain("mvtools-v24", text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("s/\\b_lambda=/lambda_=/g; s/\\b_global=/global_=/g", text);
        }
        foreach (var plugin in new[] { "fmtconv", "MiscFilters", "EEDI3m.dll", "fft3dfilter.dll", "libfftw3f-3.dll" })
            Assert.Contains(plugin, win);
        foreach (var plugin in new[] { "libfmtconv.so", "libmiscfilters.so", "libeedi3m.so", "libfft3dfilter.so", "qtgmc-check.py" })
            Assert.Contains(plugin, flatpak);
    }

    [Fact]
    public async Task A_runtime_that_does_not_load_says_why()
    {
        if (OperatingSystem.IsWindows()) return; // the stand-in Python is a shell script
        var folder = Directory.CreateTempSubdirectory("atlas-qtgmc-test").FullName;
        try
        {
            var python = Path.Combine(folder, "python3");
            await File.WriteAllTextAsync(python, """
                #!/bin/sh
                echo 'Traceback (most recent call last):' >&2
                echo '  File "probe.py", line 9, in <module>' >&2
                echo 'vapoursynth.Error: Plugin libmvtools.dll uses API 3, which is no longer supported.' >&2
                exit 1
                """.Replace("\r", ""));
            File.SetUnixFileMode(python, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            var probe = await Qtgmc.ProbeAsync(new QtgmcRuntime(python, folder, folder));
            Assert.False(probe.Works);
            Assert.EndsWith("vapoursynth.Error: Plugin libmvtools.dll uses API 3, which is no longer supported.", probe.Reason);

            var missing = await Qtgmc.ProbeAsync(new QtgmcRuntime(Path.Combine(folder, "no-python"), folder, folder));
            Assert.False(missing.Works);
            Assert.False(string.IsNullOrEmpty(missing.Reason));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    /// <summary>
    /// The real runtime, through the app's own lookup and probe: CI's Windows job stages it with
    /// build/fetch-qtgmc-win.sh and names it (ATLAS_VAPOURSYNTH); skipped without it.
    /// </summary>
    [Fact]
    public async Task The_bundled_runtime_loads()
    {
        if (Environment.GetEnvironmentVariable("ATLAS_VAPOURSYNTH") is not { Length: > 0 }) return;
        var runtime = Qtgmc.Find();
        Assert.NotNull(runtime);
        var probe = await Qtgmc.ProbeAsync(runtime);
        Assert.True(probe.Works, $"QTGMC didn't load from {runtime.Python}: {probe.Reason}");
    }
}
