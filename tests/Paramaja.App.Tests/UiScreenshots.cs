using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Paramaja.App.Services;
using Paramaja.App.Views;
using Paramaja.Core.Settings;

namespace Paramaja.App.Tests;

/// <summary>
/// Not a check: with PARAMAJA_SCREENSHOTS=&lt;folder&gt; it saves every window in every theme as
/// PNGs there, to look at after a UI change (PARAMAJA_SCREENSHOT_BACKDROP=&lt;png&gt; puts a video
/// frame behind the bars). Without the variable it does nothing.
/// </summary>
public class UiScreenshots
{
    private static void Dispatch() => Dispatcher.UIThread.RunJobs();

    private static void Save(TopLevel window, string folder, string name)
    {
        Dispatch();
        window.CaptureRenderedFrame()?.Save(Path.Combine(folder, name + ".png"));
    }

    [AvaloniaFact]
    public void Every_window_in_every_theme()
    {
        if (Environment.GetEnvironmentVariable("PARAMAJA_SCREENSHOTS") is not { Length: > 0 } folder) return;
        Directory.CreateDirectory(folder);
        var backdrop = Environment.GetEnvironmentVariable("PARAMAJA_SCREENSHOT_BACKDROP") is { Length: > 0 } b && File.Exists(b)
            ? new ImageBrush(new Bitmap(b)) { Stretch = Stretch.UniformToFill, AlignmentY = AlignmentY.Bottom }
            : (IBrush)new SolidColorBrush(Color.Parse("#808080"));
        var only = Environment.GetEnvironmentVariable("PARAMAJA_SCREENSHOT_THEMES")?.Split(',') ?? [];
        try
        {
            foreach (var theme in ThemeRegistry.All.Where(t => only.Length == 0 || only.Contains(t.Id)))
            {
                using var r = new Rig(new PlayerSettings { Theme = theme.Id, HiddenControls = [.. PlayerSettings.SimpleBarHidden] });
                ThemeService.Apply(theme.Id);
                var id = theme.Id;

                var main = new MainWindow { DataContext = r.Vm, Width = 1100, Height = 700 };
                main.Show();
                Save(main, folder, $"{id}-welcome");
                main.Close();

                // Playing: a folder's files, a 1080p HEVC HDR video, buffered a little ahead.
                r.Player.State.PlaylistCount = 4;
                r.Player.RaiseState("playlist-count");
                r.Player.State.VideoFormat = "hevc";
                r.Player.State.VideoCodec = "hevc (HEVC (High Efficiency Video Coding))";
                r.Player.RaiseState("video-format");
                r.Player.State.Transfer = "pq";
                r.Player.RaiseState("video-params/gamma");
                r.Player.State.HwdecCurrent = "vaapi";
                r.Player.RaiseState("hwdec-current");
                r.Vm.IsIdle = false;
                r.Vm.MediaTitle = "Scream Live, Cape Town (1997).mp4";
                r.Vm.Duration = 5400;
                r.Vm.Position = 1880;
                r.Vm.BufferedUntil = 2300;

                var bar = new ControlsBar { DataContext = r.Vm, Width = 1280, Background = backdrop };
                bar.Show();
                Save(bar, folder, $"{id}-bar-simple");
                r.Vm.BarLayoutIndex = 1;
                Save(bar, folder, $"{id}-bar-everything");
                r.Vm.BarLayoutIndex = 0;
                bar.Close();

                var top = new TopBar { DataContext = r.Vm, Width = 1280, Background = backdrop };
                top.Show();
                Save(top, folder, $"{id}-topbar");
                top.Close();

                var settings = new SettingsWindow { DataContext = r.Vm };
                settings.Show();
                var tabs = settings.GetLogicalDescendants().OfType<TabControl>().Single();
                foreach (var tab in tabs.Items.OfType<TabItem>())
                {
                    if (id != ThemeRegistry.Default.Id && tab.Name is not ("InterfaceTab" or "VideoTab" or "SubtitlesTab")) continue;
                    tabs.SelectedItem = tab;
                    Save(settings, folder, $"{id}-settings-{tab.Name}");
                }
                settings.Close();

                var link = new OpenLinkDialog(new ViewModels.OpenLinkDialogViewModel("magnet:?xt=urn:btih:08ada5a7a6183aae1e09d831df6748d566095a10"));
                link.Show();
                Save(link, folder, $"{id}-openlink");
                link.Close();
                var disc = new OpenDiscDialog();
                disc.Show();
                Save(disc, folder, $"{id}-opendisc");
                disc.Close();
                var download = new DownloadDialog { DataContext = new ViewModels.DownloadDialogViewModel(r.Vm, null) };
                download.Show();
                Save(download, folder, $"{id}-download");
                download.Close();
                var rip = new RipDialog { DataContext = new ViewModels.RipDialogViewModel(r.Vm, [], null) };
                rip.Show();
                Save(rip, folder, $"{id}-rip");
                rip.Close();
            }
        }
        finally
        {
            ThemeService.Apply(ThemeRegistry.Default.Id);
        }
    }
}
