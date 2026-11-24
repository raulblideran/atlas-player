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
            Settings.Theme = value.Id;
            ThemeService.Apply(value.Id);
            OnPropertyChanged();
            OnPropertyChanged(nameof(ThemeDescription));
            SaveSettings();
        }
    }

    public string ThemeDescription => ThemeRegistry.Find(Settings.Theme).Description;

    // ---- The controls bar: Simple, Everything or the user's own arrangement --------------------

    /// <summary>0 Simple, 1 Everything, 2 Custom (arranged in the editor below it).</summary>
    public int BarLayoutIndex
    {
        get
        {
            if (BarItems.Simple.SameAs(Settings.BarLeft, Settings.BarCentre, Settings.BarRight)) return 0;
            return BarItems.Everything.SameAs(Settings.BarLeft, Settings.BarCentre, Settings.BarRight) ? 1 : 2;
        }
        set
        {
            if (value == BarLayoutIndex) return;
            if (value == 0) UseSimpleBar();
            else if (value == 1) UseEverythingBar();
            // Custom: arranged piece by piece, below.
        }
    }

    public string BarLayoutText => BarLayoutIndex switch
    {
        0 => "Play, volume, the track and video menus, fullscreen and settings. The rest is in More (⋯).",
        1 => "Every button that applies to what's playing. More (⋯) also has the open actions.",
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
    /// <summary>The playback part of More has anything in it (its separator shows with it).</summary>
    public bool HasPlaybackInMore => StopInMore || PrevFrameInMore || NextFrameInMore || PrevChapterInMore || NextChapterInMore
        || PrevFileInMore || NextFileInMore || MuteInMore || DiscMenuInMore || FullscreenInMore || ScreenshotInMore;
}
