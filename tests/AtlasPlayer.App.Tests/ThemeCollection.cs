namespace AtlasPlayer.App.Tests;

/// <summary>
/// Tests that switch the app's theme (directly, or by importing settings), which is one for the whole test app. They run on their own,
/// never beside another class: a test pumping the UI thread (Dispatcher.UIThread.RunJobs) could
/// otherwise run another class's queued test in the middle of it, which put its own theme back
/// (CI, 2.0.0-beta.13: Play's glyph in Atlas's colour during the MPC-HC check).
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ThemeCollection
{
    public const string Name = "Themes";
}
