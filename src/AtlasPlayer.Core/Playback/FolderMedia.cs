namespace AtlasPlayer.Core.Playback;

/// <summary>
/// What an opened folder plays, and the cover its music shows. A CD rip's folder also holds its
/// log, cue sheet, cover scans and leftovers of other programs (.sfk, .txt, .psd): only the audio
/// and video files play, in the order a person would number them, and its cover.jpg (or
/// front.jpg…) is the picture while they do.
/// </summary>
public static class FolderMedia
{
    /// <summary>mpv 0.42's audio-exts, which it plays, DSD (.dsf, .dff) included.</summary>
    public static readonly IReadOnlySet<string> AudioExtensions = Set(
        "aac", "ac3", "aiff", "aif", "alac", "ape", "au", "dff", "dsf", "dts", "eac3", "flac", "m4a", "mka", "mp1", "mp2", "mp3",
        "mpc", "oga", "ogg", "ogm", "opus", "tak", "thd", "tta", "wav", "wma", "wv");

    /// <summary>mpv 0.42's video-exts, plus a DVD's .vob and camcorders' .mts.</summary>
    public static readonly IReadOnlySet<string> VideoExtensions = Set(
        "3g2", "3gp", "avi", "flv", "ivf", "m2ts", "m4v", "mj2", "mkv", "mov", "mp4", "mpeg", "mpg", "mts", "mxf", "ogv", "rmvb",
        "ts", "vob", "webm", "wmv", "y4m");

    public static readonly IReadOnlySet<string> ImageExtensions = Set("jpg", "jpeg", "png", "webp", "bmp", "gif");

    private static HashSet<string> Set(params string[] exts) =>
        new(exts.Select(e => "." + e), StringComparer.OrdinalIgnoreCase);

    public static bool IsAudio(string path) => AudioExtensions.Contains(Path.GetExtension(path));
    public static bool IsVideo(string path) => VideoExtensions.Contains(Path.GetExtension(path));
    public static bool IsPlayable(string path) => IsAudio(path) || IsVideo(path);

    /// <summary>
    /// The folder's audio and video files, in natural order ("2" before "10"), without the files
    /// around them (logs, cue sheets, pictures…). A folder holding only sub-folders (a two-disc
    /// album's CD1 and CD2) plays theirs, one level down, disc by disc. Empty if nothing plays.
    /// </summary>
    public static IReadOnlyList<string> Playlist(string dir)
    {
        var top = Playable(dir);
        if (top.Count > 0) return top;
        return SubFolders(dir).SelectMany(Playable).ToList();
    }

    private static List<string> Playable(string dir)
    {
        try
        {
            return Directory.EnumerateFiles(dir).Where(IsPlayable).Where(f => !IsHidden(f))
                .Order(NaturalComparer.Instance).ToList();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return []; }
    }

    private static IEnumerable<string> SubFolders(string dir)
    {
        try { return Directory.EnumerateDirectories(dir).Where(d => !IsHidden(d)).Order(NaturalComparer.Instance).ToList(); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return []; }
    }

    /// <summary>Dot files: macOS's ._ shadows of every file, .folder.jpg and the like.</summary>
    private static bool IsHidden(string path) => Path.GetFileName(path).StartsWith('.');

    /// <summary>The names a cover goes by, best first.</summary>
    private static readonly string[] CoverNames = ["cover", "front", "folder", "albumart", "album", "albumartlarge", "albumartsmall"];

    /// <summary>Where rippers and scanners put the artwork, inside an album's folder.</summary>
    private static readonly string[] ArtworkFolders = ["covers", "cover", "artwork", "scans", "art", "images"];

    /// <summary>
    /// A music folder's cover: cover.jpg or front.jpg (or folder.jpg, albumart.jpg…, any letter
    /// case, .jpg/.jpeg/.png/.webp), else one in a Covers/Artwork/Scans folder inside it, else the
    /// folder's only picture, or one named like the album (the cue sheet's or the folder's name).
    /// Null if there's none.
    /// </summary>
    public static string? Cover(string dir, string? albumName = null)
    {
        var images = Images(dir);
        if (Named(images) is { } named) return named;
        foreach (var sub in SubFolders(dir).Where(d => ArtworkFolders.Contains(Path.GetFileName(d).ToLowerInvariant())))
            if (Named(Images(sub)) is { } inside) return inside;
        if (images.Count == 1) return images[0];
        var stems = new[] { albumName, Path.GetFileName(Path.TrimEndingDirectorySeparator(dir)) }
            .Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => Simplify(s!)).ToList();
        return images.FirstOrDefault(i => stems.Contains(Simplify(Path.GetFileNameWithoutExtension(i))));
    }

    private static string? Named(IReadOnlyList<string> images)
    {
        foreach (var name in CoverNames)
            if (images.FirstOrDefault(i => Path.GetFileNameWithoutExtension(i).Equals(name, StringComparison.OrdinalIgnoreCase)) is { } hit)
                return hit;
        // "Front.jpg"'s cousins: "Cover (front).jpg", "folder_front.png", "Album - Front.jpg".
        return images.FirstOrDefault(i => Path.GetFileNameWithoutExtension(i).Contains("front", StringComparison.OrdinalIgnoreCase));
    }

    private static List<string> Images(string dir)
    {
        try
        {
            return Directory.EnumerateFiles(dir)
                .Where(f => ImageExtensions.Contains(Path.GetExtension(f)) && !IsHidden(f))
                .Order(NaturalComparer.Instance).ToList();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return []; }
    }

    /// <summary>Letters and digits only, lower case: "GunsNRoses_ChineseDemocracy" and "GunsNRoses ChineseDemocracy" match.</summary>
    private static string Simplify(string s) => new(s.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    /// <summary>Names compared as a person reads them: runs of digits by their value ("Track 2" before "Track 10"), case ignored.</summary>
    public sealed class NaturalComparer : IComparer<string>
    {
        public static readonly NaturalComparer Instance = new();

        public int Compare(string? x, string? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x is null) return -1;
            if (y is null) return 1;
            int i = 0, j = 0;
            while (i < x.Length && j < y.Length)
            {
                if (char.IsAsciiDigit(x[i]) && char.IsAsciiDigit(y[j]))
                {
                    int si = i, sj = j;
                    while (i < x.Length && char.IsAsciiDigit(x[i])) i++;
                    while (j < y.Length && char.IsAsciiDigit(y[j])) j++;
                    var a = x[si..i].TrimStart('0');
                    var b = y[sj..j].TrimStart('0');
                    if (a.Length != b.Length) return a.Length.CompareTo(b.Length);
                    var c = string.CompareOrdinal(a, b);
                    if (c != 0) return c;
                    continue;
                }
                var d = char.ToUpperInvariant(x[i]).CompareTo(char.ToUpperInvariant(y[j]));
                if (d != 0) return d;
                i++;
                j++;
            }
            var rest = (x.Length - i).CompareTo(y.Length - j);
            return rest != 0 ? rest : string.CompareOrdinal(x, y);
        }
    }
}
