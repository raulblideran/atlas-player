using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;
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

    // ---- The controls bar: Simple, Everything or the user's own choice -------------------------

    /// <summary>0 Simple, 1 Everything, 2 Custom (some other choice of buttons, made below it).</summary>
    public int BarLayoutIndex
    {
        get
        {
            var hidden = Settings.HiddenControls;
            if (hidden.Count == 0) return 1;
            return hidden.Count == PlayerSettings.SimpleBarHidden.Count && PlayerSettings.SimpleBarHidden.All(hidden.Contains) ? 0 : 2;
        }
        set
        {
            if (value == BarLayoutIndex) return;
            if (value == 0) UseSimpleBar();
            else if (value == 1) ShowAllControls();
            // Custom: the buttons are picked one by one, below.
        }
    }

    public string BarLayoutText => BarLayoutIndex switch
    {
        0 => "Play, volume, the track and video menus, fullscreen and settings. The rest is in More (⋯).",
        1 => "Every button that applies to what's playing. More (⋯) also has the open actions.",
        _ => "The buttons ticked below. Anything you leave off stays in More (⋯).",
    };

    /// <summary>The Simple bar: what it leaves off goes to the More menu.</summary>
    [RelayCommand]
    private void UseSimpleBar()
    {
        Settings.HiddenControls = PlayerSettings.SimpleBarHidden.ToList();
        foreach (var c in ControlChoices) c.IsShown = Shows(c.Id);
        OnControlsShownChanged();
        SaveSettings();
    }

    // ---- The More menu: whatever the bar leaves off, so nothing is out of reach -----------------

    public bool StopInMore => !Shows("stop");
    public bool FramesInMore => !Shows("frames") && !IsAudioOnly;
    public bool ChaptersInMore => !Shows("chapters") && HasChapters;
    /// <summary>Not for audio alone: there is no picture to save.</summary>
    public bool ScreenshotInMore => !Shows("screenshot") && !IsAudioOnly;
    /// <summary>The playback part of More has anything in it (its separator shows with it).</summary>
    public bool HasPlaybackInMore => StopInMore || FramesInMore || ChaptersInMore || ScreenshotInMore;
}
