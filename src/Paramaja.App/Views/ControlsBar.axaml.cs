using Avalonia.Markup.Xaml;
using Paramaja.App.ViewModels;

namespace Paramaja.App.Views;

public partial class ControlsBar : OverlayWindow
{
    public const double BarHeight = 118;

    public ControlsBar()
    {
        InitializeComponent();
        Classes.Add("bottom-bar"); // tooltips above its buttons (Styles/Controls.axaml)
        Seek.SeekRequested += t => (DataContext as MainViewModel)?.SeekTo(t);
    }
}
