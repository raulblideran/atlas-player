using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using AtlasPlayer.App.Services;
using AtlasPlayer.App.Views;
using AtlasPlayer.Core.Settings;

namespace AtlasPlayer.App.Tests;

/// <summary>
/// Not a check: with ATLAS_SCREENSHOTS=&lt;folder&gt; it saves every window in every theme as
/// PNGs there, to look at after a UI change (ATLAS_SCREENSHOT_BACKDROP=&lt;png&gt; puts a video
/// frame behind the bars). Without the variable it does nothing.
/// </summary>
[Collection(ThemeCollection.Name)]
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
        if (Environment.GetEnvironmentVariable("ATLAS_SCREENSHOTS") is not { Length: > 0 } folder) return;
        Directory.CreateDirectory(folder);
        var backdrop = Environment.GetEnvironmentVariable("ATLAS_SCREENSHOT_BACKDROP") is { Length: > 0 } b && File.Exists(b)
            ? new ImageBrush(new Bitmap(b)) { Stretch = Stretch.UniformToFill, AlignmentY = AlignmentY.Bottom }
            : (IBrush)new SolidColorBrush(Color.Parse("#808080"));
        var only = Environment.GetEnvironmentVariable("ATLAS_SCREENSHOT_THEMES")?.Split(',') ?? [];
        try
        {
            foreach (var theme in ThemeRegistry.All.Where(t => only.Length == 0 || only.Contains(t.Id)))
            {
                using var r = new Rig(new PlayerSettings { Theme = theme.Id });
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
                r.Vm.BarLayoutIndex = 2;
                Save(bar, folder, $"{id}-bar-classic");
                // An arrangement of one's own: the title and the time left on the left, a divider, the clock.
                r.Settings.BarLeft = ["title", "separator", "elapsed", "remaining"];
                r.Settings.BarCentre = ["prevfile", "play", "nextfile"];
                r.Settings.BarRight = ["mute", "volume", "spacer", "badge-hdr", "badge-codec", "separator", "subtitles", "audio", "more", "settings", "clock"];
                r.Vm.ReloadBar();
                Save(bar, folder, $"{id}-bar-custom");
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
                    if (id != ThemeRegistry.Default.Id && Environment.GetEnvironmentVariable("ATLAS_SCREENSHOT_ALLTABS") != "1"
                        && tab.Name is not ("InterfaceTab" or "VideoTab" or "SubtitlesTab")) continue;
                    tabs.SelectedItem = tab;
                    Save(settings, folder, $"{id}-settings-{tab.Name}");
                }
                settings.Close();
                var barSettings = new SettingsWindow(Services.SettingsSection.ControlsBar) { DataContext = r.Vm };
                barSettings.Show();
                Dispatcher.UIThread.RunJobs();
                Save(barSettings, folder, $"{id}-settings-bar-editor");
                barSettings.Close();

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

                // Every step of the rip window: a DVD's (MKV, then audio only) and a CD's.
                var dvdVm = RipDialogTests.Dialog(r, RipDialogTests.Dvd);
                var dvd = new RipDialog { DataContext = dvdVm };
                dvd.Show();
                foreach (var mode in new[] { 0, 1 })
                {
                    dvdVm.StepIndex = 0;
                    dvdVm.DvdModeIndex = mode;
                    for (var step = 0; step < dvdVm.Steps.Count; step++)
                    {
                        dvdVm.StepIndex = step;
                        Dispatcher.UIThread.RunJobs();
                        Save(dvd, folder, $"{id}-rip-dvd{(mode == 1 ? "-audio" : "")}-{step + 1}-{dvdVm.CurrentStep}");
                    }
                }
                dvd.Close();
                var cdVm = new ViewModels.RipDialogViewModel(r.Vm, [new OpticalDrive("/dev/sr0", "sr0", "Audio CD")], "/dev/sr0",
                    _ => RipDialogTests.CdDrive(), flac: "/usr/bin/flac", online: false);
                for (var i = 0; i < 200 && !cdVm.IsCd; i++) { Thread.Sleep(10); Dispatcher.UIThread.RunJobs(); }
                var cd = new RipDialog { DataContext = cdVm };
                cd.Show();
                for (var step = 0; step < cdVm.Steps.Count; step++)
                {
                    cdVm.StepIndex = step;
                    Dispatcher.UIThread.RunJobs();
                    Save(cd, folder, $"{id}-rip-cd-{step + 1}-{cdVm.CurrentStep}");
                }
                cd.Close();
                cdVm.Dispose();
            }
        }
        finally
        {
            ThemeService.Apply(ThemeRegistry.Default.Id);
        }
    }
}
