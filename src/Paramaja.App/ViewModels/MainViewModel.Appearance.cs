using Avalonia.Media;
using Paramaja.App.Services;
using Paramaja.Core.Settings;

namespace Paramaja.App.ViewModels;

/// <summary>A tile of the theme picker: the theme's name drawn in its own colours and title type.</summary>
public sealed class ThemeChoice(ThemeInfo theme)
{
    private Avalonia.Controls.ResourceDictionary Resources => ThemeService.Load(theme);
    private T Get<T>(string key, T fallback) => Resources.TryGetValue(key, out var v) && v is T t ? t : fallback;

    public ThemeInfo Theme => theme;
    public string Id => theme.Id;
    public string Name => theme.Name;
    public string Description => theme.Description;
    public IBrush Background => Get<IBrush>("ThemeBg", Brushes.Black);
    public IBrush Surface => Get<IBrush>("ThemeRaised", Brushes.Black);
    public IBrush Text => Get<IBrush>("ThemeText", Brushes.White);
    public IBrush Title => Get<IBrush>("ThemeTitle", Brushes.White);
    public IBrush Accent => Get<IBrush>("ThemeAccent", Brushes.Gray);
    public IBrush Highlight => Get<IBrush>("ThemeHighlight", Brushes.Gray);
    public IBrush Positive => Get<IBrush>("ThemePositive", Brushes.Gray);
    public FontFamily HeadingFont => Get("ThemeHeadingFont", FontFamily.Default);
    public FontWeight HeadingWeight => Get("ThemeHeadingWeight", FontWeight.Bold);
    public Avalonia.CornerRadius Corners => Get("ThemeRadiusControl", new Avalonia.CornerRadius(8));
    public override string ToString() => Name;
}

/// <summary>Settings → Appearance: the theme, and which buttons the controls bar shows.</summary>
public sealed partial class MainViewModel
{
    private IReadOnlyList<ThemeChoice>? _themeChoices;

    public IReadOnlyList<ThemeChoice> ThemeChoices => _themeChoices ??= ThemeRegistry.All.Select(t => new ThemeChoice(t)).ToList();

    /// <summary>The theme the app wears; changing it switches every open window at once.</summary>
    public ThemeChoice? SelectedTheme
    {
        get => ThemeChoices.FirstOrDefault(c => c.Id == ThemeRegistry.Find(Settings.Theme).Id);
        set
        {
            if (value is null || value.Id == Settings.Theme) return;
            var from = Settings.Theme;
            Settings.Theme = value.Id;
            ThemeService.Apply(value.Id);
            BarFollowsTheme(from, value.Id);
            OnPropertyChanged();
            OnPropertyChanged(nameof(ThemeDescription));
            SaveSettings();
        }
    }

    /// <summary>The theme whose bar layout comes with it: MPC-HC's toolbar (BarItems.Classic).</summary>
    public const string ClassicThemeId = "mpc-hc";

    /// <summary>
    /// Picking MPC-HC gives the bar MPC-HC's layout, keeping the bar it had; picking another theme
    /// puts that back, unless the bar was rearranged in between (then it's the user's, and stays).
    /// Only a theme picked here: starting up or importing a settings file leaves the bar alone.
    /// </summary>
    private void BarFollowsTheme(string from, string to)
    {
        if (to == ClassicThemeId && from != ClassicThemeId)
        {
            if (!BarItems.Classic.SameAs(Settings.BarLeft, Settings.BarCentre, Settings.BarRight))
                Settings.BarBeforeClassic = new SavedBar { Left = [.. Settings.BarLeft], Centre = [.. Settings.BarCentre], Right = [.. Settings.BarRight] };
            ApplyBarPreset(BarItems.Classic);
        }
        else if (from == ClassicThemeId && to != ClassicThemeId)
        {
            var saved = Settings.BarBeforeClassic;
            Settings.BarBeforeClassic = null;
            if (saved is null || !BarItems.Classic.SameAs(Settings.BarLeft, Settings.BarCentre, Settings.BarRight)) return;
            Settings.BarLeft = [.. saved.Left];
            Settings.BarCentre = [.. saved.Centre];
            Settings.BarRight = [.. saved.Right];
            ReloadBar();
        }
    }

    public string ThemeDescription => ThemeRegistry.Find(Settings.Theme).Description;

    // ---- The interface font ----------------------------------------------------------------

    public const string DefaultUiFontLabel = "Default (Inter)";

    private IReadOnlyList<string>? _uiFonts;

    /// <summary>The fonts installed, the default (Inter) first.</summary>
    public IReadOnlyList<string> UiFonts => _uiFonts ??= LoadFonts(DefaultUiFontLabel, Settings.UiFont);

    /// <summary>The interface's font; changing it changes every open window at once.</summary>
    public string UiFontChoice
    {
        get => Settings.UiFont.Length == 0 ? DefaultUiFontLabel : Settings.UiFont;
        set
        {
            var font = value is null or DefaultUiFontLabel ? "" : value.Trim();
            if (font == Settings.UiFont) return;
            Settings.UiFont = font;
            FontService.Apply(font);
            OnPropertyChanged();
            SaveSettings();
        }
    }

    /// <summary>Settings → Appearance's "Default" button next to the font.</summary>
    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void ResetUiFont() => UiFontChoice = DefaultUiFontLabel;

    // ---- The controls bar: Simple, Everything or the user's own arrangement --------------------

    /// <summary>0 Simple, 1 Everything, 2 MPC-HC, 3 Custom (arranged in the editor below it).</summary>
    public int BarLayoutIndex
    {
        get
        {
            if (BarItems.Simple.SameAs(Settings.BarLeft, Settings.BarCentre, Settings.BarRight)) return 0;
            if (BarItems.Everything.SameAs(Settings.BarLeft, Settings.BarCentre, Settings.BarRight)) return 1;
            return BarItems.Classic.SameAs(Settings.BarLeft, Settings.BarCentre, Settings.BarRight) ? 2 : 3;
        }
        set
        {
            if (value == BarLayoutIndex) return;
            if (value == 0) UseSimpleBar();
            else if (value == 1) UseEverythingBar();
            else if (value == 2) UseClassicBar();
            // Custom: arranged piece by piece, below.
        }
    }

    public string BarLayoutText => BarLayoutIndex switch
    {
        0 => "Play, volume, the track and video menus, fullscreen and settings. The rest is in More (⋯).",
        1 => "Every button that applies to what's playing. More (⋯) also has the open actions.",
        2 => "Media Player Classic's toolbar: play and stop, the skip and frame buttons on the left, the menus and volume on the right.",
        _ => "Your own arrangement, below. What you leave off the bar is in More (⋯) where it fits.",
    };

    // ---- The More menu: whatever the bar leaves off, so nothing is out of reach -----------------

    public bool StopInMore => !OnBar("stop");
    public bool PrevFrameInMore => !OnBar("prevframe") && !IsAudioOnly;
    public bool NextFrameInMore => !OnBar("nextframe") && !IsAudioOnly;
    public bool PrevChapterInMore => !OnBar("prevchapter") && HasChapters;
    public bool NextChapterInMore => !OnBar("nextchapter") && HasChapters;
    public bool PrevFileInMore => !OnBar("prevfile") && HasPlaylist;
    public bool NextFileInMore => !OnBar("nextfile") && HasPlaylist;
    public bool MuteInMore => !OnBar("mute");
    public bool DiscMenuInMore => !OnBar("discmenu") && CanShowDiscMenu;
    public bool FullscreenInMore => !OnBar("fullscreen");
    /// <summary>Not for audio alone: there is no picture to save.</summary>
    public bool ScreenshotInMore => !OnBar("screenshot") && !IsAudioOnly;
    /// <summary>
    /// More's "Download video…": not while the bar has its own Download button, nor while a web
    /// video plays (the top bar's download button is up then).
    /// </summary>
    public bool DownloadInMore => !OnBar("download") && !CanDownload;
    /// <summary>The playback part of More has anything in it (its separator shows with it).</summary>
    public bool HasPlaybackInMore => StopInMore || PrevFrameInMore || NextFrameInMore || PrevChapterInMore || NextChapterInMore
        || PrevFileInMore || NextFileInMore || MuteInMore || DiscMenuInMore || FullscreenInMore || ScreenshotInMore;
}
