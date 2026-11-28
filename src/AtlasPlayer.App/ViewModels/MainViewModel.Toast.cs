using CommunityToolkit.Mvvm.ComponentModel;

namespace AtlasPlayer.App.ViewModels;

/// <summary>
/// Messages over the video (the volume, a screenshot saved, the subtitle delay…), drawn by the
/// app in the theme's colours (Views/ToastWindow) rather than by the engine in its own font:
/// mpv and libVLC each drew them their own way, over the picture's top-left corner.
/// </summary>
public sealed partial class MainViewModel
{
    public static readonly TimeSpan ToastDuration = TimeSpan.FromMilliseconds(1500);

    [ObservableProperty] private string _toastText = "";
    [ObservableProperty] private bool _toastVisible;
    /// <summary>The volume's toast: its icon and level bar instead of a line of text.</summary>
    [ObservableProperty] private bool _toastIsVolume;
    [ObservableProperty] private bool _toastIsMuted;
    /// <summary>The volume as a share of the slider (0-150 %), for the toast's level bar.</summary>
    [ObservableProperty] private double _toastLevel;

    private DateTime _toastUntil;

    /// <summary>Where 100 % sits on the toast's level bar (the slider goes to 150 %).</summary>
    public static double ToastUnityLevel => 100.0 / 150;

    /// <summary>Shows <paramref name="text"/> over the video for a moment.</summary>
    public void Toast(string text, TimeSpan? duration = null)
    {
        ClearToastAction();
        ToastIsVolume = false;
        ToastText = text;
        ShowToast(duration ?? ToastDuration);
    }

    /// <summary>The toast's button ("Resume from 12:34"), or null for a toast without one (the toast then ignores the mouse).</summary>
    [ObservableProperty] private string? _toastActionText;
    private Action? _toastAction;

    public bool ToastHasAction => ToastActionText is not null;
    partial void OnToastActionTextChanged(string? value) => OnPropertyChanged(nameof(ToastHasAction));

    /// <summary>A message with a button that does <paramref name="action"/>, for as long as it lasts.</summary>
    public void ToastWithAction(string text, string actionText, Action action, TimeSpan duration)
    {
        ToastIsVolume = false;
        ToastText = text;
        _toastAction = action;
        ToastActionText = actionText;
        ShowToast(duration);
    }

    /// <summary>The toast's button: its action, once, and the toast goes.</summary>
    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void RunToastAction()
    {
        var action = _toastAction;
        ClearToastAction();
        ToastVisible = false;
        action?.Invoke();
    }

    private void ClearToastAction()
    {
        _toastAction = null;
        ToastActionText = null;
    }

    /// <summary>The volume (or that it's muted), with a level bar.</summary>
    private void ToastVolume()
    {
        var volume = _player?.Settings.Volume ?? Volume;
        ToastIsMuted = _player?.Settings.Muted ?? IsMuted;
        ToastLevel = Math.Clamp(volume / 150, 0, 1);
        ToastText = ToastIsMuted ? "Muted" : $"{volume:0}%";
        ClearToastAction();
        ToastIsVolume = true;
        ShowToast(ToastDuration);
    }

    private void ShowToast(TimeSpan duration)
    {
        _toastUntil = DateTime.UtcNow + duration;
        ToastVisible = true;
    }

    /// <summary>From the view's tick: the toast goes when its time is up.</summary>
    private void TickToast(DateTime utcNow)
    {
        if (ToastVisible && utcNow >= _toastUntil)
        {
            ToastVisible = false;
            ClearToastAction(); // an offer lasts as long as its toast
        }
    }
}
