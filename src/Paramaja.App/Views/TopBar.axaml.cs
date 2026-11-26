using Avalonia;
using Avalonia.Styling;
using Paramaja.App.Services;

namespace Paramaja.App.Views;

public partial class TopBar : OverlayWindow
{
    /// <summary>A dark theme's bar, with room below the title for its fade into the picture.</summary>
    public const double FadingHeight = 72;
    /// <summary>A light theme's solid bar: snug around the title, so it covers less of the picture.</summary>
    public const double SolidHeight = 56;

    private static readonly Thickness FadingPadding = new(24, 14, 24, 18);
    private static readonly Thickness SolidPadding = new(24, 8, 24, 8);

    /// <summary>The light themes' bars are solid (no fade), the dark themes' fade into the picture.</summary>
    private static bool Solid => ThemeService.Current.Variant == ThemeVariant.Light;

    /// <summary>The bar's height in the current theme (placement, the engines' messages and toasts go by it).</summary>
    public static double BarHeight => Solid ? SolidHeight : FadingHeight;

    public TopBar()
    {
        InitializeComponent();
        Classes.Add("top-bar"); // tooltips below its buttons (Styles/Controls.axaml)
        Fit(ThemeService.Current);
        ThemeService.Changed += Fit;
        Closed += (_, _) => ThemeService.Changed -= Fit;
    }

    private void Fit(ThemeInfo _)
    {
        Height = BarHeight;
        Ground.Padding = Solid ? SolidPadding : FadingPadding;
    }
}
