using Avalonia;
using Avalonia.Controls;

namespace Paramaja.App.Controls;

/// <summary>
/// The controls bar's button row: its first child on the left, its second (play and the
/// transport buttons) centred on the bar, its third on the right. The middle stays at the bar's
/// centre while there is room; on a narrow bar it moves left of the right-hand group rather than
/// overlapping it, and the left-hand group is cut short instead.
/// </summary>
public sealed class BarRow : Panel
{
    /// <summary>The least room kept between the groups.</summary>
    public const double Gap = 16;

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = 0.0;
        var height = 0.0;
        foreach (var child in Children)
        {
            child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
            width += child.DesiredSize.Width;
            height = Math.Max(height, child.DesiredSize.Height);
        }
        return new Size(double.IsInfinity(availableSize.Width) ? width : availableSize.Width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var left = Children.Count > 0 ? Children[0] : null;
        var middle = Children.Count > 1 ? Children[1] : null;
        var right = Children.Count > 2 ? Children[2] : null;
        var w = finalSize.Width;
        var h = finalSize.Height;

        var rightWidth = Math.Min(right?.IsVisible == true ? right.DesiredSize.Width : 0, w);
        var rightX = w - rightWidth;
        right?.Arrange(new Rect(rightX, 0, rightWidth, h));

        var middleWidth = middle?.IsVisible == true ? middle.DesiredSize.Width : 0;
        var middleX = (w - middleWidth) / 2;
        var limit = rightWidth > 0 ? rightX - Gap : w;
        if (middleX + middleWidth > limit) middleX = limit - middleWidth;
        middleX = Math.Max(0, middleX);
        middle?.Arrange(new Rect(middleX, 0, middleWidth, h));

        var leftRoom = Math.Max(0, (middleWidth > 0 ? middleX - Gap : limit));
        left?.Arrange(new Rect(0, 0, Math.Min(left.DesiredSize.Width, leftRoom), h));
        return finalSize;
    }
}
