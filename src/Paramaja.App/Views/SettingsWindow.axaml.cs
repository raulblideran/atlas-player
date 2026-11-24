using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Paramaja.App.Services;
using Paramaja.Core.Input;

namespace Paramaja.App.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        Title = $"Settings — {AppInfo.Name} {AppInfo.Display}";
        // Escape closes Settings, as it cancels the other dialogs (an open drop-down takes it first).
        KeyDown += (_, e) =>
        {
            if (e.Key != Avalonia.Input.Key.Escape || e.Handled) return;
            e.Handled = true;
            Close();
        };
        ShortcutList.ItemTemplate = new FuncDataTemplate<(string Keys, PlayerAction Action)>((item, _) =>
        {
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("210,*"), Margin = new(0, 4) };
            grid.Children.Add(new TextBlock { Text = item.Keys, FontWeight = Avalonia.Media.FontWeight.SemiBold });
            var label = new TextBlock { Text = Describe(item.Action), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(label, 1);
            grid.Children.Add(label);
            return grid;
        });
    }

    private ViewModels.MainViewModel? Vm => DataContext as ViewModels.MainViewModel;

    private static readonly Avalonia.Platform.Storage.FilePickerFileType SettingsFiles = new("Paramaja settings")
    {
        Patterns = ["*.json"],
    };

    private async void OnExportSettings(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Vm is not { } vm) return;
        var file = await StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
        {
            Title = "Export settings",
            SuggestedFileName = Paramaja.Core.Settings.SettingsStore.ExportName,
            DefaultExtension = "json",
            FileTypeChoices = [SettingsFiles],
        });
        if (file?.TryGetLocalPath() is { } path) vm.ExportSettings(path);
    }

    private async void OnImportSettings(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Vm is not { } vm) return;
        var files = await StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
        {
            Title = "Import settings",
            AllowMultiple = false,
            FileTypeFilter = [SettingsFiles, Avalonia.Platform.Storage.FilePickerFileTypes.All],
        });
        // Picked on purpose, so not asked again.
        if (files.FirstOrDefault()?.TryGetLocalPath() is { } path) await vm.ImportSettingsAsync(path, ask: false);
    }

    private async void OnCopyVersion(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Clipboard is { } c) await c.SetTextAsync($"{AppInfo.Name} {AppInfo.Display}");
    }

    /// <summary>What a shortcut does, in plain words.</summary>
    internal static string Describe(PlayerAction action) => action switch
    {
        PlayerAction.TogglePause => "Play or pause",
        PlayerAction.SeekForward => "Jump forward (Settings, Playback sets how far)",
        PlayerAction.SeekBackward => "Jump back",
        PlayerAction.SeekForwardLong => "Jump forward a minute",
        PlayerAction.SeekBackwardLong => "Jump back a minute",
        PlayerAction.FrameStep => "Next frame (pauses)",
        PlayerAction.FrameBackStep => "Previous frame (pauses)",
        PlayerAction.NextChapter => "Next chapter",
        PlayerAction.PreviousChapter => "Previous chapter",
        PlayerAction.VolumeUp => "Louder",
        PlayerAction.VolumeDown => "Quieter",
        PlayerAction.ToggleMute => "Mute or unmute",
        PlayerAction.ToggleFullscreen => "Fullscreen on or off",
        PlayerAction.ExitFullscreen => "Leave fullscreen",
        PlayerAction.Screenshot => "Screenshot",
        PlayerAction.ScreenshotWithSubtitles => "Screenshot with the subtitles",
        PlayerAction.ToggleDeinterlace => "Deinterlacing on or off (this video)",
        PlayerAction.ToggleStats => "Statistics over the video",
        PlayerAction.ToggleHdrMode => "HDR: convert to SDR or send as HDR",
        PlayerAction.ToggleDownmix => "Downmix surround to stereo, on or off",
        PlayerAction.CycleAudioTrack => "Next audio track",
        PlayerAction.CycleSubtitleTrack => "Next subtitle track",
        PlayerAction.ToggleSubtitles => "Show or hide subtitles",
        PlayerAction.SubtitleDelayUp => "Subtitles 0.1 s later",
        PlayerAction.SubtitleDelayDown => "Subtitles 0.1 s earlier",
        PlayerAction.OpenFile => "Open a file",
        PlayerAction.OpenFolder => "Open a folder",
        PlayerAction.OpenLink => "Open a link",
        PlayerAction.PasteAndPlay => "Play the link or file on the clipboard",
        PlayerAction.Stop => "Stop",
        PlayerAction.CopyDiagnostics => "Copy diagnostics",
        _ => Humanize(action),
    };

    internal static string Humanize(PlayerAction action)
    {
        var s = action.ToString();
        var chars = new List<char>();
        for (var i = 0; i < s.Length; i++)
        {
            if (i > 0 && char.IsUpper(s[i])) chars.Add(' ');
            chars.Add(i > 0 && char.IsUpper(s[i]) ? char.ToLowerInvariant(s[i]) : s[i]);
        }
        return new string(chars.ToArray());
    }
}
