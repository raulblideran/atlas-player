using Avalonia.Markup.Xaml;

namespace Paramaja.App.Views;

public partial class TopBar : OverlayWindow
{
    public const double BarHeight = 72;

    public TopBar()
    {
        InitializeComponent();
        Classes.Add("top-bar"); // tooltips below its buttons (Styles/Controls.axaml)
    }
}
