using System.Runtime.InteropServices;

namespace AtlasPlayer.App.Services;

/// <summary>
/// logs/crash.log: errors nothing else caught (the app closing on one, or one the app carried on
/// after), with the version and system, so a report can say what happened.
/// </summary>
public static class CrashLog
{
    public const string FileName = "crash.log";

    /// <summary>Adds <paramref name="e"/> to crash.log in <paramref name="logsFolder"/>; its path, or null where it can't be written.</summary>
    public static string? Write(string logsFolder, Exception? e)
    {
        if (e is null) return null;
        try
        {
            Directory.CreateDirectory(logsFolder);
            var file = Path.Combine(logsFolder, FileName);
            File.AppendAllText(file, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {AppInfo.Name} {AppInfo.Display} on "
                                     + $"{RuntimeInformation.OSDescription}\n{e}\n\n");
            return file;
        }
        catch (Exception) { return null; } // nowhere to write: stderr has it
    }
}
