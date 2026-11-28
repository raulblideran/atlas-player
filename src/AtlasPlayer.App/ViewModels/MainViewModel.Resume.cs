using AtlasPlayer.Core.Playback;

namespace AtlasPlayer.App.ViewModels;

/// <summary>
/// Carrying on where a file was left: each local file's position is remembered when it stops
/// playing (another file, Stop, closing the app), and reopening it plays from the start with a
/// toast offering "Resume from 12:34" for a few seconds. Files on this machine only.
/// </summary>
public sealed partial class MainViewModel
{
    public static readonly TimeSpan ResumeOfferDuration = TimeSpan.FromSeconds(8);

    private ResumePositions? _resumeStore;
    internal ResumePositions ResumeStore => _resumeStore ??= new ResumePositions(_paths.ResumeFile);

    /// <summary>The local file playing (its full path), and where it is.</summary>
    private string? _resumeKey;
    private double _resumeAt, _resumeLength;

    public bool ResumePlayback
    {
        get => Settings.ResumePlayback;
        set
        {
            if (Settings.ResumePlayback == value) return;
            Settings.ResumePlayback = value;
            OnPropertyChanged();
            if (!value) ClearToastAction(); // an offer on screen goes with it
            SaveSettings();
        }
    }

    /// <summary>From each change of state: follows which local file plays, and where.</summary>
    private void TrackResume(string name, PlayerState s)
    {
        if (name is not ("path" or "time-pos" or "duration" or "idle-active")) return;
        var key = s.IsIdle || IsDiscMode ? null : ResumePositions.KeyFor(s.Path);
        if (key != _resumeKey)
        {
            RememberWhereLeft();
            (_resumeKey, _resumeAt, _resumeLength) = (key, 0, s.Duration);
            return;
        }
        if (key is null) return;
        // Between files mpv has no position or length, which read as 0: where the file was left stays.
        if (name == "time-pos" && s.Position > 0) _resumeAt = s.Position;
        else if (name == "duration" && s.Duration > 0) _resumeLength = s.Duration;
    }

    /// <summary>The file playing until now: where it was left, if it was played at all.</summary>
    private void RememberWhereLeft()
    {
        if (!Settings.ResumePlayback || _resumeKey is not { } key || _resumeAt < ResumePositions.MinimumSeconds) return;
        ResumeStore.Set(key, _resumeAt, _resumeLength, DateTime.UtcNow);
        _resumeAt = 0;
    }

    /// <summary>A file has loaded: if it's a local file left part way before, the toast offers to go back there.</summary>
    private void OfferResume()
    {
        if (_player is null) return;
        var s = _player.State;
        var key = IsDiscMode ? null : ResumePositions.KeyFor(s.Path);
        // The same file opened again: where it was left counts before it starts over.
        if (key == _resumeKey) RememberWhereLeft();
        else TrackResume("path", s);
        if (!Settings.ResumePlayback || key is null || ResumeStore.Get(key) is not { } at) return;
        var hours = at >= 3600 || s.Duration >= 3600;
        ToastWithAction("Playing from the start", $"Resume from {TimeFormat.Format(at, hours)}", () =>
        {
            if (_player is not null && ResumePositions.KeyFor(_player.State.Path) == key) _player.SeekAbsolute(at);
        }, ResumeOfferDuration);
    }
}
