namespace AtlasPlayer.App.Services;

/// <summary>
/// What Atlas Player is started with: files and folders (as paths, or as file:// URIs, which
/// file managers pass for %U), links and magnets.
/// </summary>
public static class LaunchArgs
{
    /// <summary>
    /// Files and folders as full paths (file:// URIs made paths, relative ones from this
    /// process's folder), so another process can open them; links and magnets as they are.
    /// </summary>
    public static IReadOnlyList<string> Normalize(IEnumerable<string> args) =>
        args.Where(a => !string.IsNullOrWhiteSpace(a)).Select(Normalize).ToList();

    private static string Normalize(string arg)
    {
        if (arg.StartsWith("file:", StringComparison.OrdinalIgnoreCase)
            && Uri.TryCreate(arg, UriKind.Absolute, out var uri) && uri.IsFile)
            return uri.LocalPath;
        try
        {
            if (File.Exists(arg) || Directory.Exists(arg)) return Path.GetFullPath(arg);
        }
        catch (Exception e) when (e is ArgumentException or NotSupportedException or PathTooLongException) { }
        return arg;
    }

    /// <summary>A file or folder on this machine (rather than a link, a magnet or an info hash).</summary>
    public static bool IsLocal(string arg) => Path.IsPathFullyQualified(arg) && (File.Exists(arg) || Directory.Exists(arg));
}
