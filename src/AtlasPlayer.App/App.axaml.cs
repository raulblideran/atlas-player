using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using AtlasPlayer.App.Services;
using AtlasPlayer.App.ViewModels;
using AtlasPlayer.App.Views;
using AtlasPlayer.Core.Playback;
using AtlasPlayer.Core.Settings;
using AtlasPlayer.Core.Torrent;

namespace AtlasPlayer.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        ThemeService.Apply(ThemeRegistry.Default.Id); // the settings' own theme follows once they're read
        FontService.Apply("");
    }

    /// <summary>
    /// The high-performance GPU on laptops with two (Windows), and with ATLAS_DEBUG=1 a
    /// verbose mpv log in the app's logs folder.
    /// </summary>
    private static IEnumerable<KeyValuePair<string, string>> ExtraOptions(PlayerSettings settings, PortablePaths paths)
    {
        if (OperatingSystem.IsWindows() && settings.PreferHighPerformanceGpu && GpuAdapters.PreferredD3D11Adapter() is { } adapter)
            yield return new("d3d11-adapter", adapter);
        if (Environment.GetEnvironmentVariable("ATLAS_DEBUG") != "1") yield break;
        Directory.CreateDirectory(paths.Logs);
        yield return new("log-file", Path.Combine(paths.Logs, "mpv.log"));
        yield return new("msg-level", "all=v");
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var paths = PortablePaths.ForApp();
            paths.MigrateFlatpakSettings(); // settings from before the rename, once
            var store = new SettingsStore(paths.SettingsFile);
            var settings = store.Load();
            ThemeService.Apply(settings.Theme);
            FontService.Apply(settings.UiFont);
            var vm = new MainViewModel(settings, store, paths,
                wid => new PlayerService(settings, paths, wid, ExtraOptions(settings, paths)),
                new TorrentService(), new AvaloniaDispatcher(),
                discPlayerFactory: wid => new VlcDiscPlayer(settings, paths, wid));

            CatchStrayErrors(vm, paths);

            // libVLC (discs) builds its plugin cache now, in the background, rather than when a disc opens.
            vm.Forget(VlcDiscPlayer.PrepareAsync());

            var window = new MainWindow { DataContext = vm };
            ScreenWorkAreas.Watch(window); // popups and dialogs fitted to each monitor (several monitors on X11)
            vm.Dialogs = new DialogService(window, () => vm);
            window.Closed += (_, _) => vm.Dispose();
            desktop.MainWindow = window;

            // One window: files opened while it runs join its queue (Program listens for them).
            HandOvers.Attach(args => Dispatcher.UIThread.Post(() =>
            {
                vm.Forget(vm.QueueArgumentsAsync(args));
                BringForward(window);
            }));

            if (desktop.Args is { Length: > 0 } launch) vm.Forget(vm.LaunchAsync(LaunchArgs.Normalize(launch)));
        }
        base.OnFrameworkInitializationCompleted();
    }

    private static void BringForward(Window window)
    {
        if (window.WindowState == WindowState.Minimized) window.WindowState = WindowState.Normal;
        window.Activate();
    }

    /// <summary>
    /// The last resort for errors nothing else caught. One on the window's thread (a click
    /// handler's, say) is written to crash.log and said on screen, and the app carries on rather
    /// than closing. A background task's that nobody looked at only turns up when memory is
    /// tidied, long after: it goes to crash.log without a message, which would come out of nowhere.
    /// </summary>
    private static void CatchStrayErrors(MainViewModel vm, PortablePaths paths)
    {
        Dispatcher.UIThread.UnhandledException += (_, e) =>
        {
            e.Handled = true;
            try { vm.ReportUnexpected(e.Exception); }
            catch (Exception) { CrashLog.Write(paths.Logs, e.Exception); } // the report itself failed: logged at least
        };
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            e.SetObserved();
            if (e.Exception.GetBaseException() is not OperationCanceledException)
                CrashLog.Write(paths.Logs, e.Exception);
        };
    }
}
