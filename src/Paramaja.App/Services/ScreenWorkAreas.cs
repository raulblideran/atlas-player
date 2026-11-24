using System.Reflection;
using Avalonia.Controls;
using Avalonia.Platform;

namespace Paramaja.App.Services;

/// <summary>
/// Every popup (menus, tooltips, drop-down lists) and every window centred on another is fitted
/// into its monitor's working area. On X11 Avalonia takes that as the monitor's bounds cut by
/// _NET_WORKAREA, which is one rectangle for the whole desktop: with several monitors (Xwayland
/// under KWin) it ends hundreds of pixels above the bottom of a lower one (1174 px into a
/// 1440 px monitor here), so popups near the bottom were pushed far from where they belong.
/// With more than one monitor each one's working area is set to its own bounds instead: X11
/// can't tell where each monitor's panels are, and a popup over a panel's edge beats one that
/// floats away. One monitor keeps what the window manager says.
/// </summary>
public static class ScreenWorkAreas
{
    private static readonly PropertyInfo? WorkingArea =
        typeof(Screen).GetProperty(nameof(Screen.WorkingArea), BindingFlags.Instance | BindingFlags.Public);

    /// <summary>Corrects the working areas now, and again whenever the screens change.</summary>
    public static void Watch(TopLevel topLevel)
    {
        if (!OperatingSystem.IsLinux() || topLevel.Screens is not { } screens) return;
        Fix(screens.All);
        screens.Changed += (_, _) => Fix(screens.All);
    }

    /// <summary>Each screen's working area becomes its bounds, when there are several; how many were changed.</summary>
    public static int Fix(IReadOnlyList<Screen> all)
    {
        if (all.Count < 2 || WorkingArea?.GetSetMethod(nonPublic: true) is not { } set) return 0;
        var changed = 0;
        foreach (var screen in all)
        {
            if (screen.WorkingArea == screen.Bounds) continue;
            set.Invoke(screen, [screen.Bounds]);
            changed++;
        }
        return changed;
    }
}
