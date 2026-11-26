using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace AtlasPlayer.Core.Settings;

/// <summary>
/// Where the app keeps its files. On Windows (and any plain copy of the app) everything lives
/// next to the executable, so the folder can be moved around and nothing touches AppData or the
/// registry. Inside a Flatpak the app folder is read-only, so the sandbox's own XDG directories
/// are used instead and screenshots go to the user's Pictures folder. Downloaded videos always
/// go to the user's Downloads folder, where people look for them.
/// </summary>
public sealed partial class PortablePaths
{
    public PortablePaths(string root)
        : this(root, Path.Combine(root, "Screenshots"), Path.Combine(root, "cache"), Path.Combine(root, "logs"))
    {
    }

    private PortablePaths(string root, string screenshots, string torrentCache, string logs)
    {
        Root = Path.GetFullPath(root);
        Screenshots = Path.GetFullPath(screenshots);
        TorrentCache = Path.GetFullPath(torrentCache);
        Logs = Path.GetFullPath(logs);
        // Beside the logs (the app folder, or the sandbox's cache): the torrent cache is emptied on exit.
        RipCache = Path.Combine(Path.GetDirectoryName(Logs)!, "ripping");
    }

    public static PortablePaths ForApp() =>
        Environment.GetEnvironmentVariable("FLATPAK_ID") is { Length: > 0 }
            ? ForFlatpak(
                Xdg("XDG_CONFIG_HOME", ".config"),
                Xdg("XDG_CACHE_HOME", ".cache"),
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) is { Length: > 0 } pics
                    ? pics
                    : Path.Combine(Home, "Pictures"))
            : new PortablePaths(AppContext.BaseDirectory);

    /// <param name="configHome">The sandbox's XDG_CONFIG_HOME (~/.var/app/&lt;id&gt;/config), already per-app.</param>
    /// <param name="cacheHome">The sandbox's XDG_CACHE_HOME, where torrent data belongs.</param>
    /// <param name="pictures">The user's Pictures folder; screenshots go in a subfolder of it.</param>
    public static PortablePaths ForFlatpak(string configHome, string cacheHome, string pictures) =>
        new(configHome,
            Path.Combine(pictures, "Atlas Player"),
            Path.Combine(cacheHome, "torrents"),
            Path.Combine(cacheHome, "logs"));

    /// <summary>
    /// The Flatpak ID the app had before it became Atlas Player. Flatpak keeps each app's data under
    /// its ID, so an update installs as a new app: its settings are carried over once
    /// (<see cref="MigrateFlatpakSettings"/>), through a read-only permission on that folder.
    /// </summary>
    public const string PreviousFlatpakId = "io.github.raulblideran.Paramaja";

    /// <summary>
    /// Inside the Flatpak, on the first start: settings.json and mpv's config folder copied over
    /// from the previous app ID's config, when this one has no settings yet. Returns whether it did.
    /// </summary>
    public bool MigrateFlatpakSettings() =>
        Environment.GetEnvironmentVariable("FLATPAK_ID") is { Length: > 0 }
        && MigrateSettings(Path.Combine(Home, ".var", "app", PreviousFlatpakId, "config"), Root);

    /// <summary>Copies settings.json and portable_config from <paramref name="from"/> into <paramref name="to"/>, if <paramref name="to"/> has no settings.</summary>
    public static bool MigrateSettings(string from, string to)
    {
        var settings = Path.Combine(from, "settings.json");
        if (!File.Exists(settings) || File.Exists(Path.Combine(to, "settings.json"))) return false;
        try
        {
            Directory.CreateDirectory(to);
            var mpv = Path.Combine(from, "portable_config");
            if (Directory.Exists(mpv))
                foreach (var file in Directory.EnumerateFiles(mpv, "*", SearchOption.AllDirectories))
                {
                    var target = Path.Combine(to, "portable_config", Path.GetRelativePath(mpv, file));
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    File.Copy(file, target, overwrite: false);
                }
            // Last, so a copy cut short is tried again next time.
            File.Copy(settings, Path.Combine(to, "settings.json"), overwrite: false);
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return false; // no access to the old folder: the app starts with its defaults
        }
    }

    private static string Home => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    private static string Xdg(string variable, string fallback) =>
        Environment.GetEnvironmentVariable(variable) is { Length: > 0 } v ? v : Path.Combine(Home, fallback);

    /// <summary>Holds settings.json and mpv's config dir.</summary>
    public string Root { get; }
    public string SettingsFile => Path.Combine(Root, "settings.json");
    public string Screenshots { get; }
    public string TorrentCache { get; }
    /// <summary>mpv's own config dir (mpv.conf, input.conf, shaders) — optional power-user hook.</summary>
    public string MpvConfig => Path.Combine(Root, "portable_config");
    public string Logs { get; }
    /// <summary>What ripping keeps between runs: AccurateRip's list of drive offsets.</summary>
    public string RipCache { get; }
    /// <summary>Where downloaded videos are saved: the system's Downloads folder.</summary>
    public string Downloads { get; init; } = SystemDownloads();
    /// <summary>Where ripped CDs go: the system's Music folder.</summary>
    public string Music { get; init; } = SystemFolder(FolderIdMusic, "XDG_MUSIC_DIR", "Music");
    /// <summary>Where ripped DVDs go: the system's Videos folder.</summary>
    public string Videos { get; init; } = SystemFolder(FolderIdVideos, "XDG_VIDEOS_DIR", "Videos");

    internal static string SystemDownloads() => SystemFolder(FolderIdDownloads, "XDG_DOWNLOAD_DIR", "Downloads");

    /// <summary>A user folder: Windows' known folder, or the XDG user directory, else ~/<paramref name="fallback"/>.</summary>
    private static string SystemFolder(Guid windowsId, string xdgKey, string fallback)
    {
        try
        {
            if (OperatingSystem.IsWindows() && WindowsFolder(windowsId) is { } win) return win;
            if (!OperatingSystem.IsWindows() && XdgUserDir(Path.Combine(Home, ".config", "user-dirs.dirs"), Home, xdgKey) is { } xdg) return xdg;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or DllNotFoundException) { }
        return Path.Combine(Home, fallback);
    }

    /// <summary>XDG_DOWNLOAD_DIR from user-dirs.dirs, e.g. <c>"$HOME/Téléchargements"</c>.</summary>
    internal static string? XdgDownloads(string userDirsFile, string home) => XdgUserDir(userDirsFile, home, "XDG_DOWNLOAD_DIR");

    internal static string? XdgUserDir(string userDirsFile, string home, string key)
    {
        if (!File.Exists(userDirsFile)) return null;
        var line = File.ReadLines(userDirsFile).FirstOrDefault(l => l.TrimStart().StartsWith(key + "=", StringComparison.Ordinal));
        if (line is null) return null;
        var value = line.Split('=', 2)[1].Trim().Trim('"').Replace("$HOME", home);
        return Path.IsPathRooted(value) && value.TrimEnd('/') != home.TrimEnd('/') ? value : null;
    }

    private static readonly Guid FolderIdDownloads = new("374DE290-123F-4565-9164-39C4925E467B");
    private static readonly Guid FolderIdMusic = new("4BD8D571-6D19-48D3-BE97-422220080E43");
    private static readonly Guid FolderIdVideos = new("18989B1D-99B5-455B-841C-AB7C74E4DDFC");

    [LibraryImport("shell32.dll")]
    private static partial int SHGetKnownFolderPath(in Guid id, uint flags, IntPtr token, out IntPtr path);

    [SupportedOSPlatform("windows")]
    private static string? WindowsFolder(Guid id)
    {
        if (SHGetKnownFolderPath(id, 0, IntPtr.Zero, out var p) < 0) return null;
        try { return Marshal.PtrToStringUni(p); }
        finally { Marshal.FreeCoTaskMem(p); }
    }

    public void EnsureCreated()
    {
        foreach (var d in new[] { Screenshots, TorrentCache, MpvConfig }) Directory.CreateDirectory(d);
    }
}
