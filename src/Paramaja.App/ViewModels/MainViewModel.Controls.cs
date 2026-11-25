using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Paramaja.App.Services;
using Paramaja.Core.Playback;
using Paramaja.Core.Settings;

namespace Paramaja.App.ViewModels;

/// <summary>
/// The controls bar: which pieces it holds, where (Settings → Appearance arranges them, see
/// <see cref="BarItems"/>), each shown where it applies; the readouts it can carry; and Previous /
/// Next through a playlist: a torrent's files, or a folder's.
/// </summary>
public sealed partial class MainViewModel
{
    /// <summary>What the bar editor calls each piece of the bar.</summary>
    public static readonly IReadOnlyDictionary<string, string> BarItemLabels = new Dictionary<string, string>
    {
        ["mute"] = "Mute",
        ["volume"] = "Volume slider",
        ["volumetext"] = "Volume %",
        ["badge-hdr"] = "HDR badge",
        ["badge-codec"] = "Codec badge",
        ["badge-resolution"] = "Resolution badge",
        ["badge-decoder"] = "Decoder badge (HW / SW)",
        ["badge-audio"] = "Audio badge",
        ["badge-deinterlace"] = "Deinterlacing badge",
        ["stop"] = "Stop",
        ["prevfile"] = "Previous file",
        ["prevchapter"] = "Previous chapter",
        ["prevframe"] = "Previous frame",
        ["play"] = "Play / Pause",
        ["nextframe"] = "Next frame",
        ["nextchapter"] = "Next chapter",
        ["nextfile"] = "Next file",
        ["discmenu"] = "Disc menu",
        ["chaptermenu"] = "Titles & chapters",
        ["subtitles"] = "Subtitles",
        ["audio"] = "Audio",
        ["video"] = "Video",
        ["screenshot"] = "Screenshot",
        ["download"] = "Download Video",
        ["openlink"] = "Open Link",
        ["open"] = "Open",
        ["more"] = "More",
        ["settings"] = "Settings",
        ["fullscreen"] = "Fullscreen",
        ["elapsed"] = "Time played",
        ["remaining"] = "Time left",
        ["total"] = "Length",
        ["clock"] = "Clock",
        ["title"] = "Title",
        ["spacer"] = "Space",
        ["separator"] = "Divider",
    };

    /// <summary>The icon each button wears on the bar, drawn on its chip in the editor too.</summary>
    public static readonly IReadOnlyDictionary<string, string> BarItemIcons = new Dictionary<string, string>
    {
        ["mute"] = "IconVolume", ["stop"] = "IconStop",
        ["prevfile"] = "IconPlaylistPrev", ["prevchapter"] = "IconChapterPrev", ["prevframe"] = "IconFramePrev",
        ["play"] = "IconPlay", ["nextframe"] = "IconFrameNext", ["nextchapter"] = "IconChapterNext", ["nextfile"] = "IconPlaylistNext",
        ["discmenu"] = "IconHome", ["chaptermenu"] = "IconChapters", ["subtitles"] = "IconSubtitles", ["audio"] = "IconAudio",
        ["video"] = "IconVideo", ["screenshot"] = "IconCamera", ["download"] = "IconDownload", ["openlink"] = "IconLink",
        ["open"] = "IconFolder", ["more"] = "IconMore", ["settings"] = "IconSettings", ["fullscreen"] = "IconFullscreen",
    };

    /// <summary>What a piece without an icon shows on its chip: a sample of itself.</summary>
    public static readonly IReadOnlyDictionary<string, string> BarItemTexts = new Dictionary<string, string>
    {
        ["volume"] = "━━━○", ["volumetext"] = "100%",
        ["badge-hdr"] = "HDR", ["badge-codec"] = "HEVC", ["badge-resolution"] = "4K", ["badge-decoder"] = "HW",
        ["badge-audio"] = "5.1", ["badge-deinterlace"] = "BWDIF",
        ["elapsed"] = "0:42", ["remaining"] = "−1:23", ["total"] = "1:24", ["clock"] = "21:30", ["title"] = "Title…",
        ["spacer"] = "␣", ["separator"] = "│",
    };

    /// <summary>The bar's arrangement changed: the bar moves its pieces (Views/ControlsBar).</summary>
    public event Action? BarLayoutChanged;

    /// <summary>The piece is on the bar, in any of its groups.</summary>
    public bool OnBar(string id) => Settings.BarLeft.Contains(id) || Settings.BarCentre.Contains(id) || Settings.BarRight.Contains(id);

    // ---- Settings → Appearance: the bar editor ------------------------------------------------

    private IReadOnlyList<BarZone>? _barZones;
    private BarZone? _barOffZone;

    /// <summary>The bar's left, centre and right groups, as the editor shows them.</summary>
    public IReadOnlyList<BarZone> BarZones
    {
        get
        {
            if (_barZones is null)
            {
                _barZones = [new(BarZoneKind.Left, "Left"), new(BarZoneKind.Centre, "Centre"), new(BarZoneKind.Right, "Right")];
                LoadBarZones();
            }
            return _barZones;
        }
    }

    /// <summary>What can go on the bar and isn't on it (a space and a divider are always there to take).</summary>
    public BarZone BarOffZone
    {
        get
        {
            if (_barOffZone is null)
            {
                _barOffZone = new(BarZoneKind.Off, "Not on the bar");
                RebuildOffZone();
            }
            return _barOffZone;
        }
    }

    public BarZone Zone(BarZoneKind kind) => kind == BarZoneKind.Off ? BarOffZone : BarZones[(int)kind];

    private IEnumerable<BarZone> AllZones => BarZones.Append(BarOffZone);

    public BarZone? ZoneOf(BarSlot slot) => AllZones.FirstOrDefault(z => z.Slots.Contains(slot));

    /// <summary>The editor's zones from the settings (a preset, an import, or first use).</summary>
    private void LoadBarZones()
    {
        if (_barZones is null) return;
        Fill(_barZones[0], Settings.BarLeft);
        Fill(_barZones[1], Settings.BarCentre);
        Fill(_barZones[2], Settings.BarRight);
        RebuildOffZone();
        SelectedBarSlot = null;
        static void Fill(BarZone zone, IEnumerable<string> ids)
        {
            zone.Slots.Clear();
            foreach (var id in ids) zone.Slots.Add(new BarSlot(id));
        }
    }

    /// <summary>What's off the bar, in the catalogue's order; chips still off it stay the same chips.</summary>
    private void RebuildOffZone()
    {
        if (_barOffZone is null) return;
        var slots = _barOffZone.Slots;
        var want = BarItems.All.Where(i => i.Repeatable || !OnBar(i.Id)).Select(i => i.Id).ToList();
        for (var i = slots.Count - 1; i >= 0; i--)
            if (!want.Contains(slots[i].Id)) slots.RemoveAt(i);
        for (var i = 0; i < want.Count; i++)
            if (i >= slots.Count || slots[i].Id != want[i]) slots.Insert(i, new BarSlot(want[i]));
    }

    private BarSlot? _selectedBarSlot;

    /// <summary>The chip the editor's buttons act on.</summary>
    public BarSlot? SelectedBarSlot
    {
        get => _selectedBarSlot;
        set
        {
            if (_selectedBarSlot == value) return;
            if (_selectedBarSlot is { } old) old.IsSelected = false;
            _selectedBarSlot = value;
            if (value is not null) value.IsSelected = true;
            OnPropertyChanged();
            OnBarSelectionChanged();
        }
    }

    private void OnBarSelectionChanged()
    {
        OnPropertyChanged(nameof(SelectedBarSlotText));
        MoveSelectedEarlierCommand.NotifyCanExecuteChanged();
        MoveSelectedLaterCommand.NotifyCanExecuteChanged();
        MoveSelectedToCommand.NotifyCanExecuteChanged();
        TakeSelectedOffCommand.NotifyCanExecuteChanged();
    }

    public string SelectedBarSlotText => SelectedBarSlot is { } s && ZoneOf(s) is { } z
        ? z.Kind == BarZoneKind.Off ? $"{s.Label}: not on the bar" : $"{s.Label}: {z.Title.ToLowerInvariant()}, {z.Slots.IndexOf(s) + 1} of {z.Slots.Count}"
        : "Drag a piece where you want it, or click one and use these buttons.";

    /// <summary>
    /// Puts <paramref name="slot"/> in <paramref name="to"/> before the piece now at
    /// <paramref name="index"/> (counted with the slot where it is, as a drag sees the chips).
    /// Taken from off the bar, a space or divider leaves its chip there for the next one; Play,
    /// More and Settings won't go off the bar.
    /// </summary>
    public void MoveBarSlot(BarSlot slot, BarZoneKind to, int index)
    {
        if (ZoneOf(slot) is not { } from) return;
        var target = Zone(to);
        if (to == BarZoneKind.Off)
        {
            if (slot.Required || from.Kind == BarZoneKind.Off) return;
            from.Slots.Remove(slot);
            SelectedBarSlot = null;
        }
        else if (from.Kind == BarZoneKind.Off)
        {
            var placed = BarItems.IsRepeatable(slot.Id) ? new BarSlot(slot.Id) : slot;
            if (placed == slot) from.Slots.Remove(slot);
            target.Slots.Insert(Math.Clamp(index, 0, target.Slots.Count), placed);
            SelectedBarSlot = placed;
        }
        else
        {
            var old = from.Slots.IndexOf(slot);
            if (from == target)
            {
                if (index > old) index--;
                index = Math.Clamp(index, 0, from.Slots.Count - 1);
                if (index == old) return;
                from.Slots.Move(old, index);
            }
            else
            {
                from.Slots.RemoveAt(old);
                target.Slots.Insert(Math.Clamp(index, 0, target.Slots.Count), slot);
            }
        }
        CommitBar();
    }

    /// <summary>The editor's zones into the settings, and the bar after them.</summary>
    private void CommitBar()
    {
        Settings.BarLeft = [.. BarZones[0].Slots.Select(s => s.Id)];
        Settings.BarCentre = [.. BarZones[1].Slots.Select(s => s.Id)];
        Settings.BarRight = [.. BarZones[2].Slots.Select(s => s.Id)];
        RebuildOffZone();
        OnBarChanged();
        SaveSettings();
    }

    /// <summary>The bar, the More menu and the editor's preset follow the settings' arrangement.</summary>
    private void OnBarChanged()
    {
        BarLayoutChanged?.Invoke();
        foreach (var name in ShowNames) OnPropertyChanged(name);
        OnBarSelectionChanged();
        UpdateClock();
    }

    private bool SelectedOnBar => SelectedBarSlot is { } s && ZoneOf(s) is { Kind: not BarZoneKind.Off };

    private bool CanMoveSelectedEarlier() => SelectedOnBar && ZoneOf(SelectedBarSlot!)!.Slots.IndexOf(SelectedBarSlot!) > 0;
    private bool CanMoveSelectedLater() => SelectedOnBar && ZoneOf(SelectedBarSlot!)!.Slots is var s && s.IndexOf(SelectedBarSlot!) < s.Count - 1;
    private bool CanMoveSelectedTo(string? zone) => SelectedBarSlot is not null;
    private bool CanTakeSelectedOff() => SelectedOnBar && !SelectedBarSlot!.Required;

    [RelayCommand(CanExecute = nameof(CanMoveSelectedEarlier))]
    private void MoveSelectedEarlier()
    {
        if (SelectedBarSlot is { } s && ZoneOf(s) is { } z) MoveBarSlot(s, z.Kind, z.Slots.IndexOf(s) - 1);
    }

    [RelayCommand(CanExecute = nameof(CanMoveSelectedLater))]
    private void MoveSelectedLater()
    {
        if (SelectedBarSlot is { } s && ZoneOf(s) is { } z) MoveBarSlot(s, z.Kind, z.Slots.IndexOf(s) + 2);
    }

    /// <summary>The selected piece to the end of "Left", "Centre" or "Right".</summary>
    [RelayCommand(CanExecute = nameof(CanMoveSelectedTo))]
    private void MoveSelectedTo(string? zone)
    {
        if (SelectedBarSlot is not { } s || !Enum.TryParse<BarZoneKind>(zone, out var kind) || kind == BarZoneKind.Off) return;
        if (ZoneOf(s)?.Kind == kind) return;
        MoveBarSlot(s, kind, Zone(kind).Slots.Count);
    }

    [RelayCommand(CanExecute = nameof(CanTakeSelectedOff))]
    private void TakeSelectedOff()
    {
        if (SelectedBarSlot is { } s) MoveBarSlot(s, BarZoneKind.Off, 0);
    }

    /// <summary>The bar and its editor after <see cref="Settings"/>' arrangement was changed directly (an import).</summary>
    public void ReloadBar()
    {
        Settings.Normalize();
        LoadBarZones();
        OnBarChanged();
    }

    /// <summary>A preset arrangement, order and all.</summary>
    private void ApplyBarPreset(BarLayout layout)
    {
        Settings.BarLeft = [.. layout.Left];
        Settings.BarCentre = [.. layout.Centre];
        Settings.BarRight = [.. layout.Right];
        LoadBarZones();
        OnBarChanged();
        SaveSettings();
    }

    /// <summary>Every button that applies to what's playing.</summary>
    [RelayCommand] private void UseEverythingBar() => ApplyBarPreset(BarItems.Everything);

    /// <summary>The Simple bar: what it leaves off goes to the More menu.</summary>
    [RelayCommand] private void UseSimpleBar() => ApplyBarPreset(BarItems.Simple);

    /// <summary>Right-click on the bar: Settings, at the bar editor.</summary>
    [RelayCommand]
    private async Task CustomizeBar()
    {
        if (Dialogs is not null) await Dialogs.ShowSettingsAsync(SettingsSection.ControlsBar);
        SaveSettings();
    }

    private static readonly string[] ShowNames =
    [
        nameof(StopInMore), nameof(PrevFrameInMore), nameof(NextFrameInMore), nameof(PrevChapterInMore), nameof(NextChapterInMore),
        nameof(ScreenshotInMore), nameof(PrevFileInMore), nameof(NextFileInMore), nameof(MuteInMore), nameof(DiscMenuInMore),
        nameof(FullscreenInMore), nameof(HasPlaybackInMore), nameof(BarLayoutIndex), nameof(BarLayoutText), nameof(ShowsClock),
        nameof(DownloadInMore),
    ];

    // ---- Each piece shows where it applies (wherever it is on the bar) --------------------------

    public bool ShowFileButtons => HasPlaylist;
    public bool ShowFrameButtons => !IsAudioOnly;
    public bool ShowChapterButtons => HasChapters;
    public bool ShowDiscMenuButton => CanShowDiscMenu;
    public bool ShowChapterMenuButton => HasChapterMenu;
    public bool ShowVideoButton => !IsAudioOnly;

    // ---- Readouts -------------------------------------------------------------------------------

    /// <summary>Something with a length plays: the time left and the length mean something.</summary>
    public bool HasDuration => Duration > 0;
    public string RemainingText => "−" + TimeFormat.Format(Math.Max(0, Duration - Position), Duration >= 3600);

    private string _clockText = Now();
    private static string Now() => DateTime.Now.ToString("t", CultureInfo.CurrentCulture);

    /// <summary>The time of day, for the bar's clock.</summary>
    public string ClockText => _clockText;

    /// <summary>The clock is on the bar: only then is it kept up to date.</summary>
    public bool ShowsClock => OnBar("clock");

    /// <summary>Fed by the view's tick (a few times a second); changes once a minute.</summary>
    private void UpdateClock()
    {
        if (!ShowsClock) return;
        var now = Now();
        if (now == _clockText) return;
        _clockText = now;
        OnPropertyChanged(nameof(ClockText));
    }

    // ---- Previous / Next file -----------------------------------------------------------------

    /// <summary>
    /// Files to go back and forth through: a torrent's, or mpv's playlist (a folder's files, or
    /// several dropped at once). A disc's titles and chapters have their own buttons.
    /// </summary>
    public bool HasPlaylist => HasTorrentFiles || (!IsIdle && !IsDiscMode && (_player?.State.PlaylistCount ?? 0) > 1);

    public string PreviousFileTip => IsTorrent ? "Previous file in this torrent" : "Previous file";
    public string NextFileTip => IsTorrent ? "Next file in this torrent" : "Next file";

    [RelayCommand]
    private void PreviousFile()
    {
        if (IsTorrent) StepTorrentFile(-1);
        else _player?.PlaylistStep(-1);
    }

    [RelayCommand]
    private void NextFile()
    {
        if (IsTorrent) StepTorrentFile(1);
        else _player?.PlaylistStep(1);
    }

    /// <summary>What else changes with a property: the bar's buttons go with what they need.</summary>
    private static readonly Dictionary<string, string[]> Dependents = new()
    {
        [nameof(HasTorrentFiles)] = [nameof(HasPlaylist)],
        [nameof(IsIdle)] = [nameof(HasPlaylist)],
        [nameof(IsDiscMode)] = [nameof(HasPlaylist)],
        [nameof(IsTorrent)] = [nameof(PreviousFileTip), nameof(NextFileTip)],
        [nameof(HasPlaylist)] = [nameof(ShowFileButtons), nameof(PrevFileInMore), nameof(NextFileInMore), nameof(HasPlaybackInMore)],
        [nameof(IsAudioOnly)] = [nameof(ShowFrameButtons), nameof(ShowVideoButton), nameof(PrevFrameInMore), nameof(NextFrameInMore),
            nameof(ScreenshotInMore), nameof(HasPlaybackInMore)],
        [nameof(HasChapters)] = [nameof(ShowChapterButtons), nameof(PrevChapterInMore), nameof(NextChapterInMore), nameof(HasPlaybackInMore)],
        [nameof(CanShowDiscMenu)] = [nameof(ShowDiscMenuButton), nameof(DiscMenuInMore), nameof(HasPlaybackInMore)],
        [nameof(HasChapterMenu)] = [nameof(ShowChapterMenuButton)],
        [nameof(CanDownload)] = [nameof(DownloadInMore)],
        [nameof(PositionText)] = [nameof(RemainingText)],
        [nameof(DurationText)] = [nameof(RemainingText), nameof(HasDuration)],
    };

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName is { } name && Dependents.TryGetValue(name, out var more))
            foreach (var m in more) OnPropertyChanged(m);
    }
}
