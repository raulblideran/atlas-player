using Avalonia.Controls;
using Avalonia.Media;
using AtlasPlayer.App.Services;

namespace AtlasPlayer.App.Views;

/// <summary>
/// The message over the video (MainViewModel.Toast). Like the bars it's a window of its own,
/// since nothing can be drawn over the native video surface; it never takes the keyboard, and the
/// mouse only for its button when it has one (MainViewModel.ToastWithAction). Placed by MainWindow.UpdateOverlays.
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
        Background = Brushes.Transparent;
        TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
        InitializeComponent(); // after the transparency, as the bars (OverlayWindow) have it
        // Its button clicked: the keyboard goes back to the player (Space, the arrows) at once.
        ToastAction.Click += (_, _) => Avalonia.Threading.Dispatcher.UIThread.Post(() => Owner?.Activate());
        // Above a fullscreen video, as the bars are (a dock window on X11).
        if (TryGetPlatformHandle() is { HandleDescriptor: "XID" } x11) X11WindowType.MakeDock(x11.Handle);
    }
}
