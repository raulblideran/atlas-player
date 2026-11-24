using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Paramaja.App.Controls;

/// <summary>
/// A thin level, 0 to 1 (the volume toast's), with a mark (where 100 % is). Its colours are the
/// theme's seek bar's (Styles/Controls.axaml), so a gradient theme's level is a gradient too.
/// </summary>
public sealed class LevelBar : Control
{
    public static readonly StyledProperty<double> ValueProperty = AvaloniaProperty.Register<LevelBar, double>(nameof(Value));
    public static readonly StyledProperty<double> MarkProperty = AvaloniaProperty.Register<LevelBar, double>(nameof(Mark), double.NaN);
    public static readonly StyledProperty<IBrush?> TrackBrushProperty = AvaloniaProperty.Register<LevelBar, IBrush?>(nameof(TrackBrush), Brushes.Gray);
    public static readonly StyledProperty<IBrush?> FillBrushProperty = AvaloniaProperty.Register<LevelBar, IBrush?>(nameof(FillBrush), Brushes.White);
    public static readonly StyledProperty<IBrush?> MarkBrushProperty = AvaloniaProperty.Register<LevelBar, IBrush?>(nameof(MarkBrush), Brushes.White);

    static LevelBar() => AffectsRender<LevelBar>(ValueProperty, MarkProperty, TrackBrushProperty, FillBrushProperty, MarkBrushProperty);

    public double Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public double Mark { get => GetValue(MarkProperty); set => SetValue(MarkProperty, value); }
    public IBrush? TrackBrush { get => GetValue(TrackBrushProperty); set => SetValue(TrackBrushProperty, value); }
    public IBrush? FillBrush { get => GetValue(FillBrushProperty); set => SetValue(FillBrushProperty, value); }
    public IBrush? MarkBrush { get => GetValue(MarkBrushProperty); set => SetValue(MarkBrushProperty, value); }

    public override void Render(DrawingContext context)
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        var r = h / 2;
        context.DrawRectangle(TrackBrush, null, new RoundedRect(new Rect(0, 0, w, h), r));
        var filled = Math.Clamp(Value, 0, 1) * w;
        if (filled > 0) context.DrawRectangle(FillBrush, null, new RoundedRect(new Rect(0, 0, Math.Max(filled, h), h), r));
        if (!double.IsNaN(Mark) && Mark > 0 && Mark < 1)
            context.DrawRectangle(MarkBrush, null, new Rect(Math.Round(Mark * w) - 1, -2, 2, h + 4));
    }
}
