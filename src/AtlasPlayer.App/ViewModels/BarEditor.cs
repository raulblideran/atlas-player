using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using AtlasPlayer.Core.Settings;

namespace AtlasPlayer.App.ViewModels;

/// <summary>Where a piece of the controls bar is: one of its three groups, or off it.</summary>
public enum BarZoneKind { Left, Centre, Right, Off }

/// <summary>
/// A chip of Settings → Appearance's bar editor: one placed piece of the bar (or, off the bar,
/// one that can be placed). Two spacers on the bar are two slots.
/// </summary>
public sealed partial class BarSlot(string id) : ObservableObject
{
    public string Id { get; } = id;
    public string Label => MainViewModel.BarItemLabels.GetValueOrDefault(Id, Id);
    /// <summary>The Icon* resource drawn on the chip, or null for a piece shown as <see cref="Text"/>.</summary>
    public string? IconKey => MainViewModel.BarItemIcons.GetValueOrDefault(Id);
    public string Text => MainViewModel.BarItemTexts.GetValueOrDefault(Id, "");
    public bool HasIcon => IconKey is not null;
    public bool Required => BarItems.IsRequired(Id);
    public string Tip => Required ? $"{Label} (always on the bar; it can move)" : Label;
    [ObservableProperty] private bool _isSelected;
    public override string ToString() => Id;
}

/// <summary>A band of the bar editor: the bar's left, centre or right group, or what's off the bar.</summary>
public sealed class BarZone(BarZoneKind kind, string title)
{
    public BarZoneKind Kind { get; } = kind;
    public string Title { get; } = title;
    public ObservableCollection<BarSlot> Slots { get; } = [];
    public override string ToString() => Title;
}
