using Avalonia;
using AtlasPlayer.App.Services;
using AtlasPlayer.Core.Settings;

namespace AtlasPlayer.App;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) => WriteCrash(e.ExceptionObject as Exception);
        if (OperatingSystem.IsLinux() && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY")))
        {
            Tell(NoDisplay);
            return 1;
        }
        // Already running: the files go to its window (and it comes forward), and this one goes.
        if (SingleInstance.Address() is { } address && SingleInstance.TrySend(address, LaunchArgs.Normalize(args)))
            return 0;
        try
        {
            return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception e)
        {
            var log = WriteCrash(e);
            Tell($"Atlas Player couldn't start: {e.Message}" + (log is null ? "" : $"\nDetails: {log}"));
            return 1;
        }
    }

    /// <summary>Matches the desktop file, so the taskbar groups the window under the right icon.</summary>
    public const string AppId = "io.github.raulblideran.AtlasPlayer";

    /// <summary>
    /// The window is X11 (Avalonia 11 has no Wayland backend): a Wayland session needs Xwayland,
    /// which KDE and GNOME always run, but some compositors (niri, sway or Hyprland set up
    /// without it) don't.
    /// </summary>
    internal const string NoDisplay =
        "Atlas Player needs an X11 display (on a Wayland desktop, Xwayland), and there's none (DISPLAY isn't set). "
        + "KDE and GNOME run Xwayland by themselves; on sway, Hyprland or niri, install and enable Xwayland "
        + "(xwayland-satellite for niri), then start Atlas Player again.";

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new X11PlatformOptions
            {
                WmClass = AppId,
                // GLX first, as before; where it fails (a GPU driver the Flatpak can't reach) EGL,
                // then drawing on the CPU, rather than no window at all.
                RenderingMode = [X11RenderingMode.Glx, X11RenderingMode.Egl, X11RenderingMode.Software],
            })
            .WithInterFont()
            .LogToTrace();

    /// <summary>The exception in logs/crash.log (next to the app, or the Flatpak's cache); its path, or null.</summary>
    internal static string? WriteCrash(Exception? e)
    {
        if (e is null) return null;
        try { return CrashLog.Write(PortablePaths.ForApp().Logs, e); }
        catch (Exception) { return null; } // no paths (an unwritable folder): stderr has it
    }

    /// <summary>Says it on stderr, and as a desktop notification where there's notify-send (no window can open).</summary>
    private static void Tell(string message)
    {
        Console.Error.WriteLine(message);
        try
        {
            using var _ = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("notify-send", ["Atlas Player", message])
            {
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            });
        }
        catch (Exception) { } // no notify-send: stderr is all there is
    }
}
