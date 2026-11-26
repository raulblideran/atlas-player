using Avalonia.Controls;
using Avalonia.Media;
using AtlasPlayer.App.Services;

namespace AtlasPlayer.App.Views;

/// <summary>
/// The message over the video (MainViewModel.Toast). Like the bars it's a window of its own,
/// since nothing can be drawn over the native video surface; it never takes the keyboard or the
/// mouse. Placed by MainWindow.UpdateOverlays.
/// </summary>
public partial class ToastWindow : Window
{
    public ToastWindow()
    {
        SystemDecorations = SystemDecorations.None;
        ShowInTaskbar = false;
        CanResize = false;
        ShowActivated = false;
        Focusable = false;
        IsHitTestVisible = false;
        Background = Brushes.Transparent;
        TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
        InitializeComponent(); // after the transparency, as the bars (OverlayWindow) have it
        // Above a fullscreen video, as the bars are (a dock window on X11).
        if (TryGetPlatformHandle() is { HandleDescriptor: "XID" } x11) X11WindowType.MakeDock(x11.Handle);
    }
}
