using Avalonia;
using Avalonia.Controls;

namespace Paramaja.App.Controls;

/// <summary>
/// The controls bar's button row: its first child on the left, its second (play and the
/// transport buttons) in the middle, its third on the right. The middle group is placed so the
/// child marked <see cref="CentreOnProperty"/> (play) sits on the bar's centre, however many
/// buttons each side of it shows; on a narrow bar the group moves left of the right-hand group
/// rather than overlapping it, and the left-hand group is cut short instead.
/// </summary>
public sealed class BarRow : Panel
{
    /// <summary>Marks the middle group's child that goes on the bar's centre.</summary>
    public static readonly AttachedProperty<bool> CentreOnProperty =
        AvaloniaProperty.RegisterAttached<BarRow, Control, bool>("CentreOn");

    public static bool GetCentreOn(Control c) => c.GetValue(CentreOnProperty);
    public static void SetCentreOn(Control c, bool value) => c.SetValue(CentreOnProperty, value);

    /// <summary>Tags a piece of the controls bar with its id (Paramaja.Core.Settings.BarItems).</summary>
    public static readonly AttachedProperty<string?> ItemProperty =
        AvaloniaProperty.RegisterAttached<BarRow, Control, string?>("Item");

    public static string? GetItem(Control c) => c.GetValue(ItemProperty);
    public static void SetItem(Control c, string? value) => c.SetValue(ItemProperty, value);

    /// <summary>Where the marked child's centre is within a horizontal StackPanel, from its desired sizes.</summary>
    private static double? AnchorCentre(Control middle)
    {
        if (middle is not StackPanel { Orientation: Avalonia.Layout.Orientation.Horizontal } stack) return null;
        var x = 0.0;
        var first = true;
        foreach (var child in stack.Children)
        {
            if (!child.IsVisible) continue;
            if (!first) x += stack.Spacing;
            first = false;
            if (GetCentreOn(child)) return x + child.DesiredSize.Width / 2;
            x += child.DesiredSize.Width;
        }
        return null;
    }

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
        var middleX = middle is not null && AnchorCentre(middle) is { } anchor ? w / 2 - anchor : (w - middleWidth) / 2;
        var limit = rightWidth > 0 ? rightX - Gap : w;
        if (middleX + middleWidth > limit) middleX = limit - middleWidth;
        middleX = Math.Max(0, middleX);
        middle?.Arrange(new Rect(middleX, 0, middleWidth, h));

        // Too narrow for both (every button on a small window): the right group loses its first
        // buttons, under a clip, so fullscreen, Settings and More at its end stay.
        var hidden = middleX + middleWidth + Gap - rightX;
        if (right is not null)
            right.Clip = hidden > 0 ? new Avalonia.Media.RectangleGeometry(new Rect(hidden, 0, Math.Max(0, rightWidth - hidden), h)) : null;

        var leftRoom = Math.Max(0, (middleWidth > 0 ? middleX - Gap : limit));
        left?.Arrange(new Rect(0, 0, Math.Min(left.DesiredSize.Width, leftRoom), h));
        return finalSize;
    }
}
