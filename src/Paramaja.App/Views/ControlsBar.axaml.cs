using Avalonia.Controls;
using Paramaja.App.Controls;
using Paramaja.App.ViewModels;
using Paramaja.Core.Settings;

namespace Paramaja.App.Views;

public partial class ControlsBar : OverlayWindow
{
    public const double BarHeight = 118;

    /// <summary>Every piece the bar can hold (Pool in the XAML), by its <see cref="BarItems"/> id.</summary>
    private readonly Dictionary<string, Control> _items;
    private MainViewModel? _vm;

    public ControlsBar()
    {
        InitializeComponent();
        Classes.Add("bottom-bar"); // tooltips above its buttons (Styles/Controls.axaml)
        Seek.SeekRequested += t => (DataContext as MainViewModel)?.SeekTo(t);
        _items = Pool.Children.Where(c => BarRow.GetItem(c) is not null).ToDictionary(c => BarRow.GetItem(c)!);
        if (Root.ContextFlyout is { } barMenu) WatchMenu(barMenu);
        // Set from here rather than bound: a self-built 2.0.0-beta.6 showed the item beside the top
        // bar's download button, so it's decided again each time More opens as well.
        if (MoreButton.Flyout is Avalonia.Controls.Primitives.PopupFlyoutBase more) more.Opening += (_, _) => ShowDownloadItem();
        DataContextChanged += (_, _) =>
        {
            Unhook();
            _vm = DataContext as MainViewModel;
            if (_vm is not null)
            {
                _vm.BarLayoutChanged += ApplyLayout;
                _vm.PropertyChanged += OnVmChanged;
            }
            ApplyLayout();
            ShowDownloadItem();
        };
        Closed += (_, _) => Unhook();
    }

    private void Unhook()
    {
        if (_vm is null) return;
        _vm.BarLayoutChanged -= ApplyLayout;
        _vm.PropertyChanged -= OnVmChanged;
    }

    private void OnVmChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.DownloadInMore) or nameof(MainViewModel.CanDownload) or "" or null) ShowDownloadItem();
    }

    /// <summary>More's "Download video…": only where nothing else on screen downloads.</summary>
    private void ShowDownloadItem() => MoreDownloadItem.IsVisible = _vm?.DownloadInMore ?? true;

    /// <summary>The group a piece is in ("left", "centre", "right"), or null while it's off the bar.</summary>
    public string? GroupOf(string id) => _items.TryGetValue(id, out var c) ? c.Parent == LeftGroup ? "left"
        : c.Parent == Transport ? "centre" : c.Parent == RightGroup ? "right" : null : null;

    /// <summary>The ids in a group, in order (spacers and dividers included), as placed.</summary>
    public IReadOnlyList<string> IdsIn(StackPanel group) => [.. group.Children.Select(c => BarRow.GetItem(c) ?? "")];

    /// <summary>
    /// Moves each piece into its group, in the settings' order; the rest go back to the pool.
    /// The pieces are the same controls throughout, so their bindings, menus and names stay.
    /// </summary>
    private void ApplyLayout()
    {
        if (_vm is null) return;
        var settings = _vm.Settings;
        foreach (var group in new[] { LeftGroup, Transport, RightGroup })
        {
            foreach (var child in group.Children.ToList())
            {
                group.Children.Remove(child);
                if (BarRow.GetItem(child) is { } id && _items.ContainsKey(id)) Pool.Children.Add(child);
            }
        }
        Fill(LeftGroup, settings.BarLeft);
        Fill(Transport, settings.BarCentre);
        Fill(RightGroup, settings.BarRight);
    }

    private void Fill(StackPanel group, IEnumerable<string> ids)
    {
        foreach (var id in ids)
        {
            Control? piece = id switch
            {
                BarItems.Spacer => new Control { Width = 12 },
                BarItems.Separator => new Border { Classes = { "bar-divider" } },
                _ => _items.GetValueOrDefault(id),
            };
            if (piece is null) continue;
            BarRow.SetItem(piece, id);
            if (piece.Parent is Panel from) from.Children.Remove(piece);
            group.Children.Add(piece);
        }
    }
}
