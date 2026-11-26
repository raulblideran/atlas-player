using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AtlasPlayer.App.Services;
using AtlasPlayer.Core.Ripping;
using AtlasPlayer.Core.Ripping.Cd;
using AtlasPlayer.Core.Ripping.Dvd;

namespace AtlasPlayer.App.ViewModels;

public enum RipDiscKind { None, Cd, Dvd }

/// <summary>The rip window's steps; which of them a rip goes through depends on its disc and, for a DVD, its mode.</summary>
public enum RipStep { Source, CdTracks, CdFormat, CdCue, DvdMode, DvdTitles, DvdFormat, Rip }

/// <summary>What a DVD is ripped to.</summary>
public enum DvdRipChoice
{
    /// <summary>Titles as MKVs: video and audio.</summary>
    Full,
    /// <summary>Titles' audio as FLAC or WAV.</summary>
    AudioOnly,
    /// <summary>The whole VIDEO_TS folder, decrypted, menus and all.</summary>
    Backup,
}

/// <summary>A step in the window's header: "2 Tracks", the one shown or one before it.</summary>
public sealed record RipStepItem(int Number, string Name, bool IsCurrent, bool IsDone)
{
    public string Text => $"{Number}  {Name}";
}

/// <summary>A track's tags as the rip window lets them be edited, and whether it's ripped.</summary>
public sealed partial class RipTrackRow(int number, string title, string artist, TimeSpan? length = null) : ObservableObject
{
    public int Number { get; } = number;
    public TimeSpan? Length { get; } = length;
    [ObservableProperty] private string _title = title;
    [ObservableProperty] private string _artist = artist;
    [ObservableProperty] private bool _isChecked = true;
}

/// <summary>A chapter of a DVD title, ticked or not.</summary>
public sealed partial class RipChapterRow(int number, TimeSpan? length) : ObservableObject
{
    public int Number { get; } = number;
    public TimeSpan? Length { get; } = length;
    public string Label => Length is { } l ? $"Chapter {Number} · {RipDialogViewModel.Duration(l)}" : $"Chapter {Number}";
    [ObservableProperty] private bool _isChecked = true;
}

/// <summary>An audio track of a DVD title ("English, AC-3, 5.1"), ticked or not.</summary>
public sealed partial class RipAudioRow(DvdAudio audio) : ObservableObject
{
    public DvdAudio Audio { get; } = audio;
    public int Number => Audio.Number;
    public string Label => Audio.Description;
    [ObservableProperty] private bool _isChecked = true;
}

/// <summary>A DVD title to rip or not, with its chapters, its audio tracks and how it's split into files.</summary>
public sealed partial class RipTitleRow : ObservableObject
{
    public RipTitleRow(DvdTitle title)
    {
        Title = title;
        for (var c = 1; c <= title.Chapters; c++)
        {
            var row = new RipChapterRow(c, title.ChapterLength(c));
            row.PropertyChanged += (_, _) => Changed();
            Chapters.Add(row);
        }
        foreach (var a in title.Audio)
        {
            var row = new RipAudioRow(a);
            row.PropertyChanged += (_, _) => Changed();
            Audio.Add(row);
        }
    }

    public DvdTitle Title { get; }
    public int Number => Title.Number;
    public ObservableCollection<RipChapterRow> Chapters { get; } = [];
    public ObservableCollection<RipAudioRow> Audio { get; } = [];

    /// <summary>Something about the title changed: what it rips, or whether it can.</summary>
    public event Action? Edited;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Summary))]
    private bool _isChecked;

    /// <summary>Each ticked chapter as its own file, rather than all of them in one.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ChaptersSummary))]
    private bool _chapterFiles;

    partial void OnIsCheckedChanged(bool value) => Edited?.Invoke();
    partial void OnChapterFilesChanged(bool value) => Edited?.Invoke();

    /// <summary>"Title 1 · 1:05:12 · 12 chapters" (what's ticked of it, audio tracks too, is the <see cref="Summary"/> below).</summary>
    public string Label => $"Title {Number} · {RipDialogViewModel.Duration(Title.Duration)} · "
                           + (Title.Chapters == 1 ? "1 chapter" : $"{Title.Chapters} chapters");

    public bool HasChapterChoice => Chapters.Count > 1;
    public bool HasAudio => Audio.Count > 0;

    public IReadOnlyList<int> CheckedChapters => Chapters.Where(c => c.IsChecked).Select(c => c.Number).ToList();
    public IReadOnlyList<int> CheckedAudio => Audio.Where(a => a.IsChecked).Select(a => a.Number).ToList();

    /// <summary>A ticked chapter (if it has any to choose) and a ticked audio track (if it has any).</summary>
    public bool IsComplete => (Chapters.Count == 0 || Chapters.Any(c => c.IsChecked)) && (Audio.Count == 0 || Audio.Any(a => a.IsChecked));

    /// <summary>"All 12 chapters · 1:05:12", "3 of 12 chapters · 12:34, as 3 files".</summary>
    public string ChaptersSummary
    {
        get
        {
            var picked = Chapters.Where(c => c.IsChecked).ToList();
            if (picked.Count == 0) return "No chapters ticked";
            var count = picked.Count == Chapters.Count ? $"All {Chapters.Count} chapters" : $"{picked.Count} of {Chapters.Count} chapters";
            var length = picked.All(c => c.Length is not null) ? $" · {RipDialogViewModel.Duration(TimeSpan.FromTicks(picked.Sum(c => c.Length!.Value.Ticks)))}" : "";
            var files = ChapterFiles && picked.Count > 1 ? $", as {picked.Count} files" : "";
            return count + length + files;
        }
    }

    /// <summary>"2 of 3 audio tracks", for the list of titles.</summary>
    public string AudioSummary
    {
        get
        {
            var n = Audio.Count(a => a.IsChecked);
            return Audio.Count == 0 ? "No audio" : n == 0 ? "No audio track ticked" : n == Audio.Count
                ? Audio.Count == 1 ? "Its audio track" : $"All {Audio.Count} audio tracks"
                : $"{n} of {Audio.Count} audio tracks";
        }
    }

    /// <summary>What's ticked of it, under the title in the list.</summary>
    public string Summary => !IsChecked ? "Not ripped" : !IsComplete ? (Audio.Count > 0 && !Audio.Any(a => a.IsChecked) ? "Tick an audio track" : "Tick a chapter")
        : $"{(Chapters.Count > 1 ? ChaptersSummary : "The whole title")} · {AudioSummary}";

    private void Changed()
    {
        OnPropertyChanged(nameof(ChaptersSummary));
        OnPropertyChanged(nameof(AudioSummary));
        OnPropertyChanged(nameof(Summary));
        OnPropertyChanged(nameof(IsComplete));
        Edited?.Invoke();
    }

    [RelayCommand]
    private void AllChapters()
    {
        foreach (var c in Chapters) c.IsChecked = true;
    }

    [RelayCommand]
    private void NoChapters()
    {
        foreach (var c in Chapters) c.IsChecked = false;
    }

    /// <summary>The chapters to rip: null for the whole title in one file, read as it always was.</summary>
    internal IReadOnlyList<int>? ChaptersToRip => Chapters.Count > 1 && (ChapterFiles || Chapters.Any(c => !c.IsChecked)) ? CheckedChapters : null;
}

/// <summary>
/// The ripper's window, a step at a time: the disc (a drive, or for a DVD an image or folder), then
/// for an Audio CD its tags and tracks, the format and the cue sheet; for a DVD what to rip it to
/// (MKVs, audio only, or the VIDEO_TS folder), its titles, chapters and audio tracks, and the
/// format; then the rip. An Audio CD is looked up on MusicBrainz and AccurateRip as it's read; the
/// rip itself runs in the main view model.
/// </summary>
public sealed partial class RipDialogViewModel : ObservableObject, IDisposable
{
    private readonly Func<string, ICdDrive> _openCd;
    private readonly Func<string, DvdInfo> _readDvd;
    private ICdDrive? _drive;
    private CdDisc? _disc;
    private DvdInfo? _dvd;
    private CancellationTokenSource? _loading;
    /// <summary>Look discs up (MusicBrainz, AccurateRip); tests stay offline.</summary>
    private readonly bool _online;

    /// <param name="qtgmc">QTGMC's runtime and whether it works; null looks for the bundled one.</param>
    public RipDialogViewModel(MainViewModel main, IReadOnlyList<OpticalDrive> drives, string? drive = null,
        Func<string, ICdDrive>? openCd = null, Func<string, DvdInfo>? readDvd = null, string? flac = null, string? ffmpeg = null,
        bool online = true, string? ffprobe = null, IReadOnlySet<string>? encoders = null, (QtgmcRuntime? Runtime, bool Works)? qtgmc = null)
    {
        Main = main;
        _online = online;
        Drives = drives;
        _openCd = openCd ?? CdDrives.Open;
        _readDvd = readDvd ?? DvdInfo.Read;
        // Null finds the encoder; "" means there's none (tests).
        Flac = flac is null ? FlacEncoder.Find() : flac.Length == 0 ? null : flac;
        Ffmpeg = ffmpeg ?? RipTools.Find("ffmpeg");
        Ffprobe = ffprobe ?? RipTools.Find("ffprobe", Ffmpeg is null ? null : Path.GetDirectoryName(Ffmpeg));
        CdFolder = main.Paths.Music;
        AudioFolder = main.Paths.Music;
        DvdFolder = main.Paths.Videos;
        // Null asks this ffmpeg which encoders it has (tests say).
        if (encoders is not null) UseEncoders(encoders);
        else if (Ffmpeg is { } ff) _ = FindEncodersAsync(ff);
        if (qtgmc is { } q)
        {
            Qtgmc = q.Runtime;
            HasQtgmc = q.Works;
        }
        else if (Core.Ripping.Dvd.Qtgmc.Find() is { } runtime)
        {
            Qtgmc = runtime;
            _ = CheckQtgmcAsync(runtime);
        }
        main.PropertyChanged += OnMainChanged;
        SelectedDrive = drives.FirstOrDefault(d => drive is not null && d.Root == drive) ?? drives.FirstOrDefault(d => d.HasDisc);
    }

    public MainViewModel Main { get; }
    public IReadOnlyList<OpticalDrive> Drives { get; }
    public bool HasDrives => Drives.Count > 0;
    internal string? Flac { get; }
    internal string? Ffmpeg { get; }
    internal string? Ffprobe { get; }
    internal QtgmcRuntime? Qtgmc { get; }

    [ObservableProperty] private OpticalDrive? _selectedDrive;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCd), nameof(IsDvd))]
    [NotifyCanExecuteChangedFor(nameof(StartCommand), nameof(NextCommand))]
    private RipDiscKind _kind;

    public bool IsCd => Kind == RipDiscKind.Cd;
    public bool IsDvd => Kind == RipDiscKind.Dvd;

    partial void OnKindChanged(RipDiscKind value)
    {
        StepIndex = 0;
        StepsChanged();
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand), nameof(NextCommand))]
    private bool _isBusy;

    [ObservableProperty] private string _message = "";
    [ObservableProperty] private string _source = "";

    // ---- Steps ------------------------------------------------------------------------------

    /// <summary>The steps this rip goes through: they follow the disc, and for a DVD what it's ripped to.</summary>
    public IReadOnlyList<RipStep> Steps => Kind switch
    {
        RipDiscKind.Cd => [RipStep.Source, RipStep.CdTracks, RipStep.CdFormat, RipStep.CdCue, RipStep.Rip],
        RipDiscKind.Dvd when DvdChoice == DvdRipChoice.Backup => [RipStep.Source, RipStep.DvdMode, RipStep.Rip],
        RipDiscKind.Dvd => [RipStep.Source, RipStep.DvdMode, RipStep.DvdTitles, RipStep.DvdFormat, RipStep.Rip],
        // Before a disc is read, the steps a CD or DVD would take aren't known yet.
        _ => [RipStep.Source],
    };

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextCommand), nameof(BackCommand))]
    private int _stepIndex;

    partial void OnStepIndexChanged(int value) => StepsChanged();

    public RipStep CurrentStep => Steps[Math.Clamp(StepIndex, 0, Steps.Count - 1)];
    public bool IsLastStep => Kind != RipDiscKind.None && StepIndex >= Steps.Count - 1;
    public bool ShowNext => !IsLastStep;
    public bool ShowBack => StepIndex > 0;

    public bool OnSource => CurrentStep == RipStep.Source;
    public bool OnCdTracks => CurrentStep == RipStep.CdTracks;
    public bool OnCdFormat => CurrentStep == RipStep.CdFormat;
    public bool OnCdCue => CurrentStep == RipStep.CdCue;
    public bool OnDvdMode => CurrentStep == RipStep.DvdMode;
    public bool OnDvdTitles => CurrentStep == RipStep.DvdTitles;
    public bool OnDvdFormat => CurrentStep == RipStep.DvdFormat;
    public bool OnRip => CurrentStep == RipStep.Rip;

    /// <summary>The header's steps: "1 Source", "2 Tracks"… (a CD's or DVD's, once one is in).</summary>
    public IReadOnlyList<RipStepItem> StepItems => Steps.Select((s, i) => new RipStepItem(i + 1, StepName(s), i == StepIndex, i < StepIndex)).ToList();

    private static string StepName(RipStep step) => step switch
    {
        RipStep.Source => "Source",
        RipStep.CdTracks => "Tracks",
        RipStep.CdFormat => "Format",
        RipStep.CdCue => "Cue sheet",
        RipStep.DvdMode => "Rip mode",
        RipStep.DvdTitles => "Titles",
        RipStep.DvdFormat => "Format",
        _ => "Rip",
    };

    /// <summary>The step's heading.</summary>
    public string StepTitle => CurrentStep switch
    {
        RipStep.Source => "Choose the disc",
        RipStep.CdTracks => "Tags and tracks",
        RipStep.CdFormat => "Output format",
        RipStep.CdCue => "Cue sheet",
        RipStep.DvdMode => "What to rip",
        RipStep.DvdTitles => DvdChoice == DvdRipChoice.AudioOnly ? "Titles, chapters and audio tracks" : "Titles, chapters and audio tracks",
        RipStep.DvdFormat => "Output format",
        _ => "Ready to rip",
    };

    /// <summary>A line on what the step is for.</summary>
    public string StepNote => CurrentStep switch
    {
        RipStep.Source =>
            "Pick a drive with an Audio CD or a DVD in it, or open a DVD image (.iso) or VIDEO_TS folder. The disc is recognised on its own, and the next steps follow from it.",
        RipStep.CdTracks =>
            "The tags come from MusicBrainz, or the disc's CD-Text, and can be edited. Untick the tracks you don't want: the rest of the album isn't read.",
        RipStep.CdFormat =>
            "Every track is read securely (each block read until two reads agree) and checked against AccurateRip, whichever format it's saved in.",
        RipStep.CdCue =>
            "A cue sheet records the disc's layout (track order, gaps, CD-Text) so the album can be burned or played back exactly as on the disc. A single track doesn't need one.",
        RipStep.DvdMode =>
            "Save the disc's films as MKV videos, only their audio as FLAC or WAV files, or the whole disc as it is.",
        RipStep.DvdTitles => DvdChoice == DvdRipChoice.AudioOnly
            ? "Tick the titles to rip. For each title: the chapters, whether they're one file or one each, and the audio tracks. Every audio track ticked becomes files of its own."
            : "Tick the titles to rip: each becomes its own MKV. For each title: the chapters, whether they're one file or one each, and the audio tracks the MKV keeps.",
        RipStep.DvdFormat => DvdChoice == DvdRipChoice.AudioOnly
            ? "FLAC is lossless and smaller; WAV is uncompressed. Both keep every channel of the track, 5.1 included."
            : "Keep the disc's video as it is, or encode it to look the same in less space. Then choose the audio's format.",
        _ => "Check what's going to be written, and where, then start the rip. You can close the window while it runs.",
    };

    private void StepsChanged()
    {
        if (StepIndex >= Steps.Count) StepIndex = Steps.Count - 1;
        OnPropertyChanged(nameof(Steps));
        OnPropertyChanged(nameof(StepItems));
        OnPropertyChanged(nameof(CurrentStep));
        OnPropertyChanged(nameof(IsLastStep));
        OnPropertyChanged(nameof(ShowNext));
        OnPropertyChanged(nameof(ShowBack));
        OnPropertyChanged(nameof(StepTitle));
        OnPropertyChanged(nameof(StepNote));
        foreach (var name in new[] { nameof(OnSource), nameof(OnCdTracks), nameof(OnCdFormat), nameof(OnCdCue), nameof(OnDvdMode),
                     nameof(OnDvdTitles), nameof(OnDvdFormat), nameof(OnRip) })
            OnPropertyChanged(name);
        OnPropertyChanged(nameof(RipSummary));
        OnPropertyChanged(nameof(SaveFolder));
        NextCommand.NotifyCanExecuteChanged();
        BackCommand.NotifyCanExecuteChanged();
    }

    /// <summary>The step's choices allow going on.</summary>
    internal bool StepComplete(RipStep step) => step switch
    {
        RipStep.Source => !IsBusy && Kind != RipDiscKind.None,
        RipStep.CdTracks => Tracks.Any(t => t.IsChecked),
        RipStep.DvdMode => DvdChoice switch
        {
            DvdRipChoice.Backup => CanDecrypt && !IsFolderSource,
            _ => CanMkv,
        },
        RipStep.DvdTitles => TitleRows.Any(t => t.IsChecked) && TitleRows.Where(t => t.IsChecked).All(t => t.IsComplete),
        RipStep.DvdFormat => DvdChoice == DvdRipChoice.AudioOnly || CanEncode && (!UsesQtgmc || HasQtgmc),
        _ => true,
    };

    private bool CanNext() => !IsLastStep && StepComplete(CurrentStep);

    [RelayCommand(CanExecute = nameof(CanNext))]
    private void Next()
    {
        if (CurrentStep == RipStep.CdTracks && !_cueSet) SetCue(CheckedTracks is null);
        StepIndex++;
    }

    private bool CanBack() => StepIndex > 0;

    [RelayCommand(CanExecute = nameof(CanBack))]
    private void Back() => StepIndex--;

    // ---- Audio CD ---------------------------------------------------------------------------

    public ObservableCollection<AlbumTags> Releases { get; } = [];
    public ObservableCollection<RipTrackRow> Tracks { get; } = [];
    [ObservableProperty] private AlbumTags? _selectedRelease;
    [ObservableProperty] private string _albumArtist = "";
    [ObservableProperty] private string _album = "";
    [ObservableProperty] private string _date = "";
    [ObservableProperty] private string _genre = "";
    [ObservableProperty] private byte[]? _cover;
    [ObservableProperty] private Avalonia.Media.Imaging.Bitmap? _coverImage;
    /// <summary>0 FLAC (level 8), 1 WAV.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RipSummary))]
    private int _formatIndex;
    [ObservableProperty] private string _cdFolder;
    /// <summary>The drive's read offset, in samples ("+48"); empty to leave it to AccurateRip.</summary>
    [ObservableProperty] private string _offsetText = "";
    [ObservableProperty] private string _offsetNote = "";
    [ObservableProperty] private string _discNote = "";

    /// <summary>Write a cue sheet of the tracks ripped: ticked for a whole disc, not for a few tracks, unless set.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RipSummary), nameof(CueNote))]
    private bool _writeCue = true;

    // Set by hand: the tracks ticked no longer decide it.
    private bool _cueSet;
    private bool _settingCue;

    partial void OnWriteCueChanged(bool value)
    {
        if (!_settingCue) _cueSet = true;
    }

    private void SetCue(bool value)
    {
        _settingCue = true;
        WriteCue = value;
        _settingCue = false;
    }

    public string CueNote => WriteCue
        ? CheckedTracks is null ? "The cue sheet describes the whole disc, with its gaps between tracks, its catalogue number and the tags."
            : "The cue sheet lists only the tracks ripped, each with its gap."
        : "No cue sheet: the tracks are saved with their tags, the cover and the log.";

    public bool HasFlac => Flac is not null;

    /// <summary>"All 14 tracks · 77:12", "3 of 14 tracks · 12:34".</summary>
    public string TracksSummary
    {
        get
        {
            var picked = Tracks.Where(t => t.IsChecked).ToList();
            if (picked.Count == 0) return "No tracks ticked";
            var count = picked.Count == Tracks.Count ? $"All {Tracks.Count} tracks"
                : picked.Count == 1 ? $"Track {picked[0].Number} of {Tracks.Count}"
                : $"{picked.Count} of {Tracks.Count} tracks";
            var length = picked.All(t => t.Length is not null) ? $" · {Duration(TimeSpan.FromTicks(picked.Sum(t => t.Length!.Value.Ticks)))}" : "";
            var some = picked.Count < Tracks.Count ? ", in the album's folder" : "";
            return count + length + some;
        }
    }

    /// <summary>The tracks ticked; null when every one is (the whole disc).</summary>
    internal IReadOnlyList<int>? CheckedTracks => Tracks.All(t => t.IsChecked) ? null : Tracks.Where(t => t.IsChecked).Select(t => t.Number).ToList();

    [RelayCommand]
    private void AllTracks()
    {
        foreach (var t in Tracks) t.IsChecked = true;
    }

    [RelayCommand]
    private void NoTracks()
    {
        foreach (var t in Tracks) t.IsChecked = false;
    }

    private void OnTrackChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(RipTrackRow.IsChecked)) return;
        OnPropertyChanged(nameof(TracksSummary));
        if (!_cueSet) SetCue(CheckedTracks is null);
        OnPropertyChanged(nameof(CueNote));
        OnPropertyChanged(nameof(RipSummary));
        StartCommand.NotifyCanExecuteChanged();
        NextCommand.NotifyCanExecuteChanged();
    }

    private void ClearTracks()
    {
        foreach (var t in Tracks) t.PropertyChanged -= OnTrackChanged;
        Tracks.Clear();
    }
    /// <summary>Releases to choose from: several, or only similar discs (none preselected).</summary>
    public bool HasReleases => Releases.Count > 1 || Releases.Any(r => !r.ExactDisc);

    // ---- DVD --------------------------------------------------------------------------------------

    public ObservableCollection<RipTitleRow> TitleRows { get; } = [];

    /// <summary>The title shown on the right of the list: its chapters and audio tracks.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedTitle), nameof(Chapters), nameof(ChaptersSummary), nameof(ShowChapters), nameof(ChapterFiles),
        nameof(AudioTracks))]
    private RipTitleRow? _selectedTitleRow;

    /// <summary>The title shown (for its chapters and tracks).</summary>
    public DvdTitle? SelectedTitle => SelectedTitleRow?.Title;

    private static readonly ObservableCollection<RipChapterRow> NoChapterRows = [];
    private static readonly ObservableCollection<RipAudioRow> NoAudioRows = [];

    /// <summary>The selected title's chapters.</summary>
    public ObservableCollection<RipChapterRow> Chapters => SelectedTitleRow?.Chapters ?? NoChapterRows;

    /// <summary>The selected title's audio tracks.</summary>
    public ObservableCollection<RipAudioRow> AudioTracks => SelectedTitleRow?.Audio ?? NoAudioRows;

    /// <summary>The selected title's chapters as files of their own.</summary>
    public bool ChapterFiles
    {
        get => SelectedTitleRow?.ChapterFiles ?? false;
        set
        {
            if (SelectedTitleRow is { } row) row.ChapterFiles = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ChaptersSummary));
        }
    }

    public string ChaptersSummary => SelectedTitleRow?.ChaptersSummary ?? "";

    /// <summary>Chapters to choose from: a title with more than one.</summary>
    public bool ShowChapters => SelectedTitleRow?.HasChapterChoice == true;

    [RelayCommand]
    private void AllChapters() => SelectedTitleRow?.AllChaptersCommand.Execute(null);

    [RelayCommand]
    private void NoChapters() => SelectedTitleRow?.NoChaptersCommand.Execute(null);

    internal static string Duration(TimeSpan t) =>
        t.TotalHours >= 1 ? t.ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture) : t.ToString(@"m\:ss", CultureInfo.InvariantCulture);

    private void OnTitleEdited()
    {
        OnPropertyChanged(nameof(ChaptersSummary));
        OnPropertyChanged(nameof(ChapterFiles));
        OnPropertyChanged(nameof(TitlesSummary));
        OnPropertyChanged(nameof(RipSummary));
        StartCommand.NotifyCanExecuteChanged();
        NextCommand.NotifyCanExecuteChanged();
    }

    /// <summary>"2 of 5 titles", for the step's heading line.</summary>
    public string TitlesSummary
    {
        get
        {
            var n = TitleRows.Count(t => t.IsChecked);
            return n == 0 ? "No titles ticked" : n == TitleRows.Count ? TitleRows.Count == 1 ? "The disc's only title" : $"All {n} titles" : $"{n} of {TitleRows.Count} titles";
        }
    }

    /// <summary>What the DVD is ripped to: 0 MKV, 1 audio only, 2 the VIDEO_TS folder.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMkv), nameof(IsAudioOnly), nameof(IsBackup), nameof(DvdChoice), nameof(MkvNote), nameof(CanDownmix),
        nameof(StepNote), nameof(SplitOneLabel), nameof(TitlesSummary))]
    [NotifyCanExecuteChangedFor(nameof(StartCommand), nameof(NextCommand))]
    private int _dvdModeIndex;

    partial void OnDvdModeIndexChanged(int value) => StepsChanged();

    public DvdRipChoice DvdChoice => (DvdRipChoice)Math.Clamp(DvdModeIndex, 0, 2);
    public bool IsMkv => DvdChoice == DvdRipChoice.Full;
    public bool IsAudioOnly => DvdChoice == DvdRipChoice.AudioOnly;
    public bool IsBackup => DvdChoice == DvdRipChoice.Backup;

    /// <summary>The split choice's first option, which differs by mode.</summary>
    public string SplitOneLabel => IsAudioOnly ? "One file per title" : "One file";

    /// <summary>The MKV's video: 0 copied, 1 x264, 2 x265 (<see cref="DvdVideo"/>).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MkvNote), nameof(ShowDeinterlacing), nameof(RipSummary))]
    [NotifyCanExecuteChangedFor(nameof(StartCommand), nameof(NextCommand))]
    private int _dvdVideoIndex;

    /// <summary>0 BWDIF, 1 QTGMC.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MkvNote), nameof(UsesQtgmc), nameof(RipSummary))]
    [NotifyCanExecuteChangedFor(nameof(StartCommand), nameof(NextCommand))]
    private int _deinterlacerIndex;

    /// <summary>QTGMC's preset, among <see cref="QtgmcPresets"/>: Slower to begin with.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MkvNote))]
    private int _qtgmcPresetIndex = 2;

    /// <summary>0 doubles the frame rate (a frame per field), 1 keeps the disc's.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MkvNote), nameof(RipSummary))]
    private int _frameRateIndex;

    /// <summary>The MKV's audio: 0 as on the disc, 1 FLAC, 2 AAC.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDownmix), nameof(AudioNote), nameof(RipSummary))]
    private int _audioCodecIndex;

    /// <summary>Audio-only files: 0 FLAC (level 8), 1 WAV.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AudioNote), nameof(RipSummary))]
    private int _audioFormatIndex;

    /// <summary>Mix every converted track down to stereo; off, every channel is kept.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AudioNote), nameof(RipSummary))]
    private bool _downmix;

    public IReadOnlyList<string> QtgmcPresets => Core.Ripping.Dvd.Qtgmc.Presets;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(QtgmcTip))]
    [NotifyCanExecuteChangedFor(nameof(StartCommand), nameof(NextCommand))]
    private bool _hasQtgmc;

    public string QtgmcTip => HasQtgmc ? "QTGMC (through VapourSynth): slower than BWDIF, with cleaner edges and steadier detail"
        : "QTGMC needs the VapourSynth that comes with Atlas Player, which isn't there or doesn't load";

    private async Task CheckQtgmcAsync(QtgmcRuntime runtime)
    {
        var works = await Core.Ripping.Dvd.Qtgmc.AvailableAsync(runtime);
        Avalonia.Threading.Dispatcher.UIThread.Post(() => HasQtgmc = works);
    }

    /// <summary>Deinterlacing choices apply to encoded video; a copy keeps the disc's fields.</summary>
    public bool ShowDeinterlacing => Video != DvdVideo.Copy;
    public bool UsesQtgmc => DeinterlacerIndex == 1;

    /// <summary>Mixing down applies to converted audio: audio-only files, or an MKV's FLAC or AAC.</summary>
    public bool CanDownmix => IsAudioOnly || AudioCodecIndex != 0;

    [ObservableProperty] private string _dvdFolder;
    [ObservableProperty] private string _audioFolder;
    private string? _dvdSource;

    /// <summary>
    /// The DVD is a VIDEO_TS folder picked in the window: already decrypted, so it isn't copied as
    /// a whole again (that would just copy it).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowDecrypt))]
    [NotifyCanExecuteChangedFor(nameof(StartCommand), nameof(NextCommand))]
    private bool _isFolderSource;

    public bool ShowDecrypt => !IsFolderSource;

    public bool CanMkv => Ffmpeg is not null && Ffprobe is not null;
    public bool CanDecrypt => DvdRead.Available;
    public DvdVideo Video => (DvdVideo)Math.Clamp(DvdVideoIndex, 0, 2);

    [ObservableProperty] private bool _hasX264;
    [ObservableProperty] private bool _hasX265;

    private async Task FindEncodersAsync(string ffmpeg)
    {
        var found = await DvdRipper.FfmpegEncodersAsync(ffmpeg);
        Avalonia.Threading.Dispatcher.UIThread.Post(() => UseEncoders(found));
    }

    private void UseEncoders(IReadOnlySet<string> encoders)
    {
        HasX264 = encoders.Contains("libx264");
        HasX265 = encoders.Contains("libx265");
        if (Video == DvdVideo.X264 && !HasX264 || Video == DvdVideo.X265 && !HasX265) DvdVideoIndex = 0;
        StartCommand.NotifyCanExecuteChanged();
        NextCommand.NotifyCanExecuteChanged();
    }

    private bool CanEncode => Video switch
    {
        DvdVideo.X264 => HasX264,
        DvdVideo.X265 => HasX265,
        _ => true,
    };

    private string Deinterlacing
    {
        get
        {
            var rate = FrameRateIndex == 0
                ? "each field becomes a frame (50 a second, 59.94 for NTSC), so motion stays as smooth as on the disc"
                : "at the disc's own rate (25 frames a second, 29.97 for NTSC)";
            return UsesQtgmc
                ? $"Interlaced video is deinterlaced with QTGMC ({QtgmcPresets[Math.Clamp(QtgmcPresetIndex, 0, QtgmcPresets.Count - 1)]}): {rate}. QTGMC is slow, often slower than the encode itself, but gives the cleanest result."
                : $"Interlaced video is deinterlaced with BWDIF, as Atlas Player plays DVDs: {rate}.";
        }
    }

    /// <summary>What the MKV will hold, for the choice made.</summary>
    public string MkvNote => Video switch
    {
        DvdVideo.X264 =>
            "The video is encoded with x264 (CRF 16): it looks the same as the disc's in less space; interlaced video at CRF 14, which keeps "
            + $"the look but saves little space (75 to 93% of a concert DVD's video). For space, x265. {Deinterlacing} x264 is quick: on a recent "
            + "PC, a fraction of the film's length.",
        DvdVideo.X265 =>
            "The video is encoded with x265 (CRF 18): it looks the same as the disc's in about half the space; interlaced video at CRF 16, in "
            + $"about two thirds of the space. {Deinterlacing} At 50 frames a second x265 takes about twice as long as the film, longer on an older PC.",
        _ =>
            "The MKV keeps the MPEG-2 video as it is, interlaced or not, and the chapters, with every subtitle track.",
    };

    /// <summary>What the audio will be, for the choice made.</summary>
    public string AudioNote
    {
        get
        {
            var channels = Downmix ? "Every track is mixed down to stereo." : "Every track keeps all its channels: 5.1 stays 5.1, nothing is mixed down.";
            if (IsAudioOnly)
                return (AudioFormatIndex == 1 ? "WAV, uncompressed: 24-bit for AC-3 and DTS (decoded) and for 24-bit LPCM, 16-bit LPCM as it is. "
                    : "FLAC, level 8: lossless; AC-3 and DTS are decoded to 24-bit, LPCM keeps its bits. ") + channels;
            return AudioCodecIndex switch
            {
                1 => "FLAC, level 8: every track lossless (AC-3 and DTS decoded to 24-bit, LPCM bit for bit). " + channels,
                2 => "AAC at 160 kb/s a channel (320 for stereo, 960 for 5.1): as close to the disc as AAC gets, in far less space. " + channels,
                _ => "As on the disc: AC-3, DTS and MPEG audio copied untouched, LPCM as FLAC (the same audio bit for bit). Every channel is kept.",
            };
        }
    }

    // ---- The last step ----------------------------------------------------------------------

    /// <summary>Where the rip is saved: Music for CDs and DVD audio, Videos for MKVs and the VIDEO_TS folder.</summary>
    public string SaveFolder
    {
        get => IsCd ? CdFolder : IsAudioOnly ? AudioFolder : DvdFolder;
        set
        {
            if (IsCd) CdFolder = value;
            else if (IsAudioOnly) AudioFolder = value;
            else DvdFolder = value;
            OnPropertyChanged();
        }
    }

    /// <summary>The rip in a few lines, on its last step.</summary>
    public string RipSummary
    {
        get
        {
            if (IsCd)
            {
                var format = FormatIndex == 1 || !HasFlac ? "WAV" : "FLAC (level 8)";
                var cue = WriteCue ? "a cue sheet" : "no cue sheet";
                var where = CheckedTracks is null ? "in a folder of their own, Artist / Album (Year)" : "in the album's folder, Artist / Album (Year)";
                return $"{TracksSummary.Split(',')[0]} as {format}, with {cue}, the cover and a rip log, {where}.";
            }
            if (!IsDvd) return "";
            if (IsBackup) return $"Every file of the disc's VIDEO_TS folder, menus included, decrypted, in a folder named after the disc ({_dvd?.Name}). It plays like the disc.";
            var titles = TitleRows.Where(t => t.IsChecked).ToList();
            if (titles.Count == 0) return "No titles ticked.";
            var lines = titles.Select(t => $"Title {t.Number}: {(t.HasChapterChoice ? t.ChaptersSummary : "the whole title")}"
                                           + (t.Audio.Count == 0 ? "" : $", {t.AudioSummary.ToLowerInvariant()}")).ToList();
            string what;
            if (IsAudioOnly)
            {
                var format = AudioFormatIndex == 1 ? "WAV" : "FLAC (level 8)";
                what = $"Audio only, as {format}: a file for each audio track ticked, of each title or chapter, in a folder named after the disc"
                       + (Downmix ? ", mixed down to stereo." : ", every channel kept.");
            }
            else
            {
                var video = Video switch
                {
                    DvdVideo.X264 => "video encoded with x264",
                    DvdVideo.X265 => "video encoded with x265",
                    _ => "the disc's video as it is",
                };
                if (Video != DvdVideo.Copy)
                    video += $" (interlaced video deinterlaced with {(UsesQtgmc ? "QTGMC" : "BWDIF")}, {(FrameRateIndex == 0 ? "a frame per field" : "at the disc's rate")})";
                var audio = AudioCodecIndex switch { 1 => "the audio as FLAC", 2 => "the audio as AAC", _ => "the audio as on the disc" };
                what = $"An MKV for each title, with {video}, {audio}{(Downmix && CanDownmix ? " mixed down to stereo" : "")}, every subtitle track and the chapters.";
            }
            return what + "\n" + string.Join("\n", lines);
        }
    }

    // ---- Loading a disc ------------------------------------------------------------------------------

    partial void OnSelectedDriveChanged(OpticalDrive? value)
    {
        if (value is null) return;
        if (!value.HasDisc) { Reset(RipDiscKind.None, "No disc in this drive."); return; }
        if (value.DiscLabel == "Audio CD") _ = LoadCdAsync(value);
        else LoadDvd(value.Root, RipSource(value));
    }

    /// <summary>What to rip from: "D:" on Windows, the raw drive on Linux (a mount point doesn't take libdvdcss's keys).</summary>
    internal static string RipSource(OpticalDrive drive) => OperatingSystem.IsWindows() ? drive.Name : "/dev/" + drive.Name;

    /// <summary>A DVD image (.iso) or a VIDEO_TS folder, picked in the window.</summary>
    public void UseDvdSource(string path)
    {
        SelectedDrive = null;
        // A drive's mount point is a folder too, but drives come through OnSelectedDriveChanged.
        LoadDvd(path, path, folder: Directory.Exists(path));
    }

    private void Reset(RipDiscKind kind, string message)
    {
        _loading?.Cancel();
        _drive?.Dispose();
        _drive = null;
        _disc = null;
        _dvd = null;
        Releases.Clear();
        ClearTracks();
        _cueSet = false;
        SetCue(true);
        OnPropertyChanged(nameof(TracksSummary));
        foreach (var row in TitleRows) row.Edited -= OnTitleEdited;
        TitleRows.Clear();
        SelectedTitleRow = null;
        Cover = null;
        CoverImage = null;
        DiscNote = "";
        Kind = kind;
        Message = message;
        StepIndex = 0;
        StepsChanged();
        OnPropertyChanged(nameof(HasReleases));
    }

    private async Task LoadCdAsync(OpticalDrive drive)
    {
        Reset(RipDiscKind.None, "Reading the CD and looking it up (MusicBrainz, AccurateRip)…");
        var loading = _loading = new CancellationTokenSource();
        IsBusy = true;
        Source = drive.Description;
        try
        {
            var device = OperatingSystem.IsWindows() ? drive.Root : "/dev/" + drive.Name;
            var cd = await Task.Run(() => _openCd(device), loading.Token);
            if (loading.IsCancellationRequested) { cd.Dispose(); return; }
            _drive = cd;
            var disc = await Main.CdRipper.ReadDiscAsync(cd, _online, loading.Token);
            if (loading.IsCancellationRequested) return;
            _disc = disc;
            foreach (var r in disc.Releases) Releases.Add(r);
            OnPropertyChanged(nameof(HasReleases));
            Apply(disc.DefaultTags);
            SelectedRelease = disc.Releases.FirstOrDefault(r => r.ExactDisc);
            OffsetText = disc.ListedOffset is { } o ? o.ToString("+0;-0;0", CultureInfo.InvariantCulture) : "";
            OffsetNote = disc.ListedOffset is not null ? $"{disc.Drive.Name}, from AccurateRip's drive list"
                : disc.AccurateRip.Count > 0 ? $"{disc.Drive.Name} isn't in AccurateRip's list: the offset is found from this disc when the rip starts"
                : $"{disc.Drive.Name} isn't in AccurateRip's list, nor this disc: type the drive's offset if you know it";
            DiscNote = $"Audio CD: {disc.Toc.AudioTrackCount} tracks, {CdToc.Time(disc.Toc.AudioTracks.Sum(disc.Toc.Sectors))}"
                       + (disc.AccurateRip.Count > 0 ? $" · in AccurateRip ({disc.AccurateRip.Count} pressings)" : " · not in AccurateRip")
                       + (disc.Releases.Any(r => r.ExactDisc) ? " · found on MusicBrainz"
                          : disc.Releases.Count > 0 ? " · MusicBrainz only knows similar discs: pick one if it's this one"
                          : disc.CdText is not null ? " · tags from its CD-Text" : " · not found on MusicBrainz: type the tags in");
            Kind = RipDiscKind.Cd;
            Message = HasFlac ? "" : "FLAC needs the flac encoder, which isn't installed: WAV only.";
            if (!HasFlac) FormatIndex = 1;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidDataException or PlatformNotSupportedException
                                      or DllNotFoundException)
        {
            Reset(RipDiscKind.None, $"Can't read this CD: {e.Message}");
        }
        catch (OperationCanceledException) { }
        finally
        {
            if (ReferenceEquals(_loading, loading)) IsBusy = false;
        }
    }

    partial void OnSelectedReleaseChanged(AlbumTags? value)
    {
        if (value is not null) Apply(value);
    }

    private void Apply(AlbumTags tags)
    {
        AlbumArtist = tags.AlbumArtist;
        Album = tags.Album;
        Date = tags.Date ?? "";
        Genre = tags.Genre ?? "";
        // Another release's tags keep the tracks ticked as they were.
        var unticked = Tracks.Where(t => !t.IsChecked).Select(t => t.Number).ToHashSet();
        ClearTracks();
        foreach (var t in tags.Tracks)
        {
            TimeSpan? length = _disc?.Toc.AudioTracks.FirstOrDefault(a => a.Number == t.Number) is { } track
                ? TimeSpan.FromSeconds(_disc.Toc.Sectors(track) / (double)CdToc.SectorsPerSecond) : null;
            var row = new RipTrackRow(t.Number, t.Title, t.Artist, length) { IsChecked = !unticked.Contains(t.Number) };
            row.PropertyChanged += OnTrackChanged;
            Tracks.Add(row);
        }
        OnPropertyChanged(nameof(TracksSummary));
        StartCommand.NotifyCanExecuteChanged();
        NextCommand.NotifyCanExecuteChanged();
        _ = LoadCoverAsync(tags);
    }

    private int _coverRequest;

    private async Task LoadCoverAsync(AlbumTags tags)
    {
        var request = ++_coverRequest;
        Cover = null;
        CoverImage = null;
        if (tags.ReleaseId is null) return;
        var image = _online ? await CdMetadata.CoverAsync(MainViewModel.RipHttp, tags) : null;
        if (request != _coverRequest) return; // another release was picked meanwhile
        Cover = image;
        try { CoverImage = image is null ? null : new Avalonia.Media.Imaging.Bitmap(new MemoryStream(image)); }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException or NotSupportedException) { CoverImage = null; }
    }

    /// <summary>The tags as edited, over the release they started from (its IDs, label, ISRCs).</summary>
    internal AlbumTags EditedTags()
    {
        var start = SelectedRelease ?? _disc?.DefaultTags ?? new AlbumTags();
        string? Clean(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
        var byNumber = start.Tracks.ToDictionary(t => t.Number);
        return start with
        {
            AlbumArtist = Clean(AlbumArtist) ?? "Unknown Artist",
            Album = Clean(Album) ?? "Unknown Album",
            Date = Clean(Date),
            Genre = Clean(Genre),
            Tracks = Tracks.Select(row => (byNumber.TryGetValue(row.Number, out var t) ? t : new TrackTags(row.Number, "", "")) with
            {
                Title = Clean(row.Title) ?? $"Track {row.Number:00}",
                Artist = Clean(row.Artist) ?? Clean(AlbumArtist) ?? "Unknown Artist",
            }).ToList(),
        };
    }

    /// <summary>The offset typed in ("+48", "-472"), if any; null leaves it to AccurateRip.</summary>
    internal int? Offset => int.TryParse(OffsetText.Trim().TrimStart('+'), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var o)
                            && (_disc?.ListedOffset != o)
        ? o
        : null;

    private void LoadDvd(string infoSource, string ripSource, bool folder = false)
    {
        Reset(RipDiscKind.None, "");
        IsFolderSource = folder;
        Source = ripSource;
        try
        {
            var dvd = _readDvd(infoSource);
            if (dvd.Titles.Count == 0) throw new InvalidDataException("it lists no titles");
            _dvd = dvd;
            _dvdSource = ripSource;
            foreach (var t in dvd.Titles)
            {
                var row = new RipTitleRow(t) { IsChecked = t.Number == dvd.MainTitle?.Number };
                row.Edited += OnTitleEdited;
                TitleRows.Add(row);
            }
            SelectedTitleRow = TitleRows.FirstOrDefault(r => r.IsChecked) ?? TitleRows.FirstOrDefault();
            DvdModeIndex = CanMkv || folder ? 0 : 2;
            DiscNote = $"DVD-Video: {dvd.Name} · {(dvd.Titles.Count == 1 ? "1 title" : $"{dvd.Titles.Count} titles")}"
                       + (dvd.MainTitle is { } main ? $", the longest {Duration(main.Duration)} (title {main.Number})" : "");
            Kind = RipDiscKind.Dvd;
            Message = folder ? (CanMkv ? "" : "This folder is already a decrypted VIDEO_TS; ripping its titles needs FFmpeg, which isn't installed.")
                : !CanMkv && !CanDecrypt ? "This build can't rip DVDs: it has neither FFmpeg's DVD reader nor libdvdread."
                : !CanMkv ? "Ripping titles needs FFmpeg, which isn't installed: the whole-disc backup only."
                : !CanDecrypt ? "The whole-disc backup needs libdvdread, which isn't installed." : "";
            OnTitleEdited();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException)
        {
            Reset(RipDiscKind.None, $"That isn't an Audio CD or a DVD-Video: {e.Message}");
        }
    }

    // ---- Starting -------------------------------------------------------------------------------------

    /// <summary>Everything chosen allows a rip (whichever step is shown; the Rip button is on the last).</summary>
    private bool CanStart() => !IsBusy && !Main.IsRipping && Kind switch
    {
        RipDiscKind.Cd => _disc is not null && _drive is not null && Tracks.Any(t => t.IsChecked),
        RipDiscKind.Dvd => _dvd is not null && DvdChoice switch
        {
            DvdRipChoice.Backup => CanDecrypt && !IsFolderSource,
            DvdRipChoice.AudioOnly => CanMkv && StepComplete(RipStep.DvdTitles),
            _ => CanMkv && StepComplete(RipStep.DvdTitles) && StepComplete(RipStep.DvdFormat),
        },
        _ => false,
    };

    [RelayCommand(CanExecute = nameof(CanStart))]
    private void Start()
    {
        if (Kind == RipDiscKind.Cd && _drive is { } drive && _disc is { } disc)
        {
            _drive = null; // the rip has it now, and closes it
            _ = Main.StartCdRipAsync(drive, disc, new CdRipOptions
            {
                Tags = EditedTags(),
                Cover = Cover,
                Format = FormatIndex == 1 || !HasFlac ? CdRipFormat.Wav : CdRipFormat.Flac,
                OutputRoot = CdFolder,
                Offset = Offset,
                AppVersion = AppInfo.Display,
                Flac = Flac,
                Tracks = CheckedTracks,
                WriteCue = WriteCue,
            });
        }
        else if (Kind == RipDiscKind.Dvd && _dvd is { } dvd && DvdJobs() is { Count: > 0 } jobs)
        {
            _ = Main.StartDvdRipAsync(dvd, jobs);
        }
        StartCommand.NotifyCanExecuteChanged();
    }

    /// <summary>The DVD rip as chosen in the window, a job per title ticked (one for the VIDEO_TS folder); null without a DVD.</summary>
    internal IReadOnlyList<DvdRipOptions>? DvdJobs()
    {
        if (_dvd is not { } dvd || _dvdSource is not { } source) return null;
        var common = new DvdRipOptions
        {
            Source = source,
            Mode = DvdChoice switch { DvdRipChoice.Backup => DvdRipMode.Decrypt, DvdRipChoice.AudioOnly => DvdRipMode.AudioOnly, _ => DvdRipMode.Mkv },
            Video = IsMkv ? Video : DvdVideo.Copy,
            Title = dvd.MainTitle?.Number ?? 1,
            OutputRoot = IsAudioOnly ? AudioFolder : DvdFolder,
            Ffmpeg = Ffmpeg,
            Ffprobe = Ffprobe,
            AudioCodec = IsMkv ? (DvdAudioCodec)Math.Clamp(AudioCodecIndex, 0, 2) : DvdAudioCodec.Original,
            AudioFormat = AudioFormatIndex == 1 ? AudioFileFormat.Wav : AudioFileFormat.Flac,
            Deinterlacer = UsesQtgmc ? Deinterlacer.Qtgmc : Deinterlacer.Bwdif,
            QtgmcPreset = QtgmcPresets[Math.Clamp(QtgmcPresetIndex, 0, QtgmcPresets.Count - 1)],
            DoubleRate = FrameRateIndex == 0,
            Downmix = Downmix && CanDownmix,
            QtgmcRuntime = Qtgmc,
        };
        if (IsBackup) return [common];
        return TitleRows.Where(t => t.IsChecked).Select(t => common with
        {
            Title = t.Number,
            // Every chapter in one file is the whole title, read as it always was.
            Chapters = t.ChaptersToRip,
            ChapterFiles = t.HasChapterChoice && t.ChapterFiles,
            // Every track (or a title with none listed) is all of them, mapped as always.
            AudioTracks = t.Audio.Count == 0 || t.Audio.All(a => a.IsChecked) ? null : t.CheckedAudio,
        }).ToList();
    }

    /// <summary>The first title's rip (or the VIDEO_TS folder's), as chosen; null without a DVD.</summary>
    internal DvdRipOptions? DvdOptions() => DvdJobs() is { Count: > 0 } jobs ? jobs[0] : null;

    private void OnMainChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsRipping)) StartCommand.NotifyCanExecuteChanged();
    }

    public void Dispose()
    {
        Main.PropertyChanged -= OnMainChanged;
        _loading?.Cancel();
        _drive?.Dispose(); // read but not ripped
        _drive = null;
    }
}
