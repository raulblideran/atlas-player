using Paramaja.Core.Settings;

namespace Paramaja.Core.Tests;

/// <summary>2.0.0-beta.8: the bar MPC-HC's theme put aside, kept across restarts and imports.</summary>
public class Beta8CoreTests
{
    [Fact]
    public void The_bar_before_mpc_hc_survives_a_restart_and_an_import_and_loses_unknown_pieces()
    {
        var dir = Path.Combine(Path.GetTempPath(), "paramaja-core-tests", Guid.NewGuid().ToString("N"));
        var store = new SettingsStore(Path.Combine(dir, "settings.json"));
        var s = new PlayerSettings
        {
            BarBeforeClassic = new SavedBar { Left = ["mute", "warp-drive"], Centre = ["play"], Right = ["more", "settings"] },
        };
        store.Save(s);
        var back = store.Load();
        Assert.Equal(["mute"], back.BarBeforeClassic!.Left); // a piece this version doesn't know is dropped
        Assert.Equal(["play"], back.BarBeforeClassic.Centre);

        var other = new PlayerSettings();
        other.CopyFrom(back);
        Assert.NotSame(back.BarBeforeClassic, other.BarBeforeClassic);
        Assert.Equal(back.BarBeforeClassic.Right, other.BarBeforeClassic!.Right);

        store.Save(new PlayerSettings());
        Assert.DoesNotContain("barBeforeClassic", File.ReadAllText(Path.Combine(dir, "settings.json"))); // not written when unset
    }
}
