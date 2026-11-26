using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using AtlasPlayer.App.ViewModels;

namespace AtlasPlayer.App.Views;

/// <summary>
/// Settings → Appearance's bar editor. A chip is dragged with the pointer captured (no
/// platform drag and drop: it stays inside this control), and dropped before the chip under the
/// pointer, at the end of a band, or off the bar. A click without a drag selects it.
/// </summary>
public partial class BarEditor : UserControl
{
    /// <summary>An Icon* resource's geometry, for a chip.</summary>
    public static readonly IValueConverter IconOf = new FuncValueConverter<string?, Geometry?>(key =>
        key is not null && Application.Current?.TryGetResource(key, null, out var g) == true ? g as Geometry : null);

    /// <summary>How far the pointer goes before a press becomes a drag.</summary>
    public const double DragThreshold = 4;

    private BarSlot? _pressed;
    private Border? _pressedChip;
    private Point _pressedAt;
    private bool _dragging;
    private (BarZone Zone, int Index)? _target;
    private Border? _dropBand;

    public BarEditor()
    {
        InitializeComponent();
        AddHandler(PointerPressedEvent, OnPressed, RoutingStrategies.Bubble, handledEventsToo: true);
        AddHandler(PointerMovedEvent, OnMoved, RoutingStrategies.Bubble, handledEventsToo: true);
        AddHandler(PointerReleasedEvent, OnReleased, RoutingStrategies.Bubble, handledEventsToo: true);
        AddHandler(PointerCaptureLostEvent, (_, _) => Cancel());
    }

    private MainViewModel? Vm => DataContext as MainViewModel;

    /// <summary>Escape gives up a drag (and doesn't close Settings).</summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        TopLevel.GetTopLevel(this)?.AddHandler(KeyDownEvent, OnKey, RoutingStrategies.Tunnel);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        TopLevel.GetTopLevel(this)?.RemoveHandler(KeyDownEvent, OnKey);
        base.OnDetachedFromVisualTree(e);
    }

    private void OnKey(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || _pressed is null) return;
        e.Handled = true;
        Cancel();
    }

    private static Border? ChipOf(object? source) => (source as Visual)?.GetSelfAndVisualAncestors().OfType<Border>()
        .FirstOrDefault(b => b.Classes.Contains("bar-chip") && b.DataContext is BarSlot);

    private void OnPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || ChipOf(e.Source) is not { DataContext: BarSlot slot } chip) return;
        _pressed = slot;
        _pressedChip = chip;
        _pressedAt = e.GetPosition(this);
        _dragging = false;
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    private void OnMoved(object? sender, PointerEventArgs e)
    {
        if (_pressed is null) return;
        var p = e.GetPosition(this);
        if (!_dragging)
        {
            if (Math.Abs(p.X - _pressedAt.X) < DragThreshold && Math.Abs(p.Y - _pressedAt.Y) < DragThreshold) return;
            _dragging = true;
            _pressedChip?.Classes.Add("dragged");
            GhostLabel.Text = _pressed.Label;
            Ghost.IsVisible = true;
        }
        Canvas.SetLeft(Ghost, p.X - 39);
        Canvas.SetTop(Ghost, p.Y - 27);
        ShowTarget(p);
    }

    private void OnReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_pressed is not { } slot) return;
        var dragged = _dragging;
        var target = dragged ? Target(e.GetPosition(this)) : null;
        Cancel();
        e.Pointer.Capture(null);
        e.Handled = true;
        if (Vm is not { } vm) return;
        if (!dragged) vm.SelectedBarSlot = vm.SelectedBarSlot == slot ? null : slot;
        else if (target is { } t) vm.MoveBarSlot(slot, t.Zone.Kind, t.Index);
    }

    private void Cancel()
    {
        _pressedChip?.Classes.Remove("dragged");
        _pressed = null;
        _pressedChip = null;
        _dragging = false;
        _target = null;
        Ghost.IsVisible = false;
        Caret.IsVisible = false;
        _dropBand?.Classes.Remove("drop");
        _dropBand = null;
    }

    /// <summary>The bands (left, centre, right, off) as drawn, each with its zone.</summary>
    private IEnumerable<(Border Band, BarZone Zone, Rect Rect)> Bands() =>
        this.GetVisualDescendants().OfType<Border>()
            .Where(b => b.Classes.Contains("bar-zone") && b.DataContext is BarZone && b.IsEffectivelyVisible)
            .Select(b => (b, (BarZone)b.DataContext!, RectOf(b)));

    private Rect RectOf(Visual v) => v.TranslatePoint(default, this) is { } at ? new Rect(at, v.Bounds.Size) : default;

    /// <summary>The chips of a band, in their zone's order, where they're drawn.</summary>
    private List<(BarSlot Slot, Rect Rect)> Chips(Border band, BarZone zone) =>
        [.. band.GetVisualDescendants().OfType<Border>()
            .Where(b => b.Classes.Contains("bar-chip") && b.DataContext is BarSlot s && zone.Slots.Contains(s))
            .Select(b => ((BarSlot)b.DataContext!, RectOf(b)))
            .OrderBy(c => zone.Slots.IndexOf(c.Item1))];

    /// <summary>Where a drop at <paramref name="p"/> goes: the band under it, before the first chip after it in reading order.</summary>
    public (BarZone Zone, int Index)? Target(Point p)
    {
        if (_pressed is null) return null;
        foreach (var (band, zone, rect) in Bands())
        {
            if (!rect.Contains(p)) continue;
            if (zone.Kind == BarZoneKind.Off) return _pressed.Required ? null : (zone, 0);
            var chips = Chips(band, zone);
            var index = chips.FindIndex(c => p.Y < c.Rect.Top || (p.Y < c.Rect.Bottom && p.X < c.Rect.Center.X));
            return (zone, index < 0 ? chips.Count : index);
        }
        return null;
    }

    private void ShowTarget(Point p)
    {
        _target = Target(p);
        var band = _target is { } t ? Bands().FirstOrDefault(b => b.Zone == t.Zone).Band : null;
        if (band != _dropBand)
        {
            _dropBand?.Classes.Remove("drop");
            band?.Classes.Add("drop");
            _dropBand = band;
        }
        if (_target is not { Zone.Kind: not BarZoneKind.Off } at || band is null)
        {
            Caret.IsVisible = false;
            return;
        }
        var chips = Chips(band, at.Zone);
        Rect caret;
        if (chips.Count == 0)
        {
            var r = RectOf(band);
            caret = new Rect(r.X + 76, r.Y + 8, 3, r.Height - 16);
        }
        else if (at.Index < chips.Count)
        {
            var c = chips[at.Index].Rect;
            caret = new Rect(c.X - 3, c.Y, 3, c.Height);
        }
        else
        {
            var c = chips[^1].Rect;
            caret = new Rect(c.Right + 1, c.Y, 3, c.Height);
        }
        Canvas.SetLeft(Caret, caret.X);
        Canvas.SetTop(Caret, caret.Y);
        Caret.Height = caret.Height;
        Caret.IsVisible = true;
    }
}
