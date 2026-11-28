using System.Text.Json;
using System.Text.Json.Serialization;

namespace AtlasPlayer.Core.Playback;

/// <summary>
/// Where each local file was left (positions.json, beside settings.json), so reopening one can
/// offer to carry on from there. Files on this machine only: not links, torrents or discs. The
/// most recent <see cref="Capacity"/> are kept; the file is rewritten whole, atomically, as
/// settings.json is.
/// </summary>
public sealed class ResumePositions
{
    public const int Capacity = 200;
    /// <summary>Less than this in, there's nothing to resume.</summary>
    public const double MinimumSeconds = 1;

    private readonly string _file;
    private readonly Dictionary<string, Entry> _entries;

    public sealed record Entry(
        [property: JsonPropertyName("position")] double Position,
        [property: JsonPropertyName("duration")] double Duration,
        [property: JsonPropertyName("saved")] DateTime Saved);

    public ResumePositions(string file)
    {
        _file = file;
        _entries = Load(file);
    }

    private static Dictionary<string, Entry> Load(string file)
    {
        try
        {
            if (File.Exists(file) && JsonSerializer.Deserialize<Dictionary<string, Entry>>(File.ReadAllText(file)) is { } read)
                return new Dictionary<string, Entry>(read, Comparer);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException) { } // unreadable: start over
        return new Dictionary<string, Entry>(Comparer);
    }

    /// <summary>Paths as Windows treats them there (any case), as Linux does here.</summary>
    private static StringComparer Comparer => OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    /// <summary>The key a file is remembered by (its full path), or null for what isn't a file on this machine.</summary>
    public static string? KeyFor(string? path)
    {
        if (string.IsNullOrEmpty(path) || path.Contains("://", StringComparison.Ordinal)) return null;
        try
        {
            var full = Path.GetFullPath(path);
            return File.Exists(full) ? full : null;
        }
        catch (Exception e) when (e is ArgumentException or NotSupportedException or PathTooLongException) { return null; }
    }

    public int Count => _entries.Count;

    /// <summary>Where <paramref name="key"/> was left, if anywhere worth resuming.</summary>
    public double? Get(string key) =>
        _entries.TryGetValue(key, out var e) && e.Position >= MinimumSeconds ? e.Position : null;

    /// <summary>Remembers where <paramref name="key"/> was left and saves; the oldest beyond <see cref="Capacity"/> go.</summary>
    public void Set(string key, double position, double duration, DateTime now)
    {
        if (!double.IsFinite(position) || position < 0) return;
        _entries[key] = new Entry(position, double.IsFinite(duration) ? duration : 0, now);
        foreach (var old in _entries.OrderByDescending(kv => kv.Value.Saved).Skip(Capacity).Select(kv => kv.Key).ToList())
            _entries.Remove(old);
        Save();
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_file))!);
            var tmp = _file + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(_entries, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(tmp, _file, overwrite: true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { } // a read-only folder: remembered until the app closes
    }
}
