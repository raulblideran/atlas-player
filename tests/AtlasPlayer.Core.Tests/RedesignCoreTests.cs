using System.Text.Json;
using AtlasPlayer.Core.Settings;

namespace AtlasPlayer.Core.Tests;

/// <summary>2.0.0's settings: the theme, and the Simple bar for everyone who had every button.</summary>
public class RedesignCoreTests
{
    private static PlayerSettings Load(string json)
    {
        var dir = Path.Combine(Path.GetTempPath(), "atlas-core-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, "settings.json");
        File.WriteAllText(file, json);
        return new SettingsStore(file).Load();
    }

    private static void AssertBar(BarLayout expected, PlayerSettings s)
    {
        Assert.Equal(expected.Left, s.BarLeft);
        Assert.Equal(expected.Centre, s.BarCentre);
        Assert.Equal(expected.Right, s.BarRight);
        Assert.Null(s.HiddenControls);
    }

    [Fact]
    public void A_fresh_install_gets_the_simple_bar_and_the_default_theme()
    {
        var s = Load("{}");
        AssertBar(BarItems.Simple, s);
        Assert.Equal("atlas", s.Theme);
        Assert.Equal(PlayerSettings.CurrentVersion, s.SettingsVersion);
        AssertBar(BarItems.Simple, new PlayerSettings());
    }

    [Fact]
    public void Who_had_every_button_moves_to_the_simple_bar_once()
    {
        AssertBar(BarItems.Simple, Load("""{"settingsVersion": 4, "hiddenControls": []}"""));
        // Chose Everything with 2.0.0 (version 6): it stays.
        AssertBar(BarItems.Everything, Load("""{"settingsVersion": 6, "hiddenControls": []}"""));
    }

    [Fact]
    public void Who_has_the_first_simple_bar_gets_the_frame_steps_back()
    {
        AssertBar(BarItems.Simple, Load("""{"settingsVersion": 5, "hiddenControls": ["frames", "stop", "chapters", "screenshot", "download", "openlink", "open"]}"""));
        AssertBar(BarItems.Simple, Load("""{"settingsVersion": 6, "hiddenControls": ["stop", "chapters", "screenshot", "download", "openlink", "open"]}"""));
        // Their own choice since (frames off again) stays.
        var s = Load("""{"settingsVersion": 6, "hiddenControls": ["frames", "stop", "chapters", "screenshot", "download", "openlink", "open"]}""");
        AssertBar(BarItems.Simple.Without(["prevframe", "nextframe"]), s);
    }

    [Fact]
    public void Who_picked_their_own_buttons_keeps_them_as_pieces()
    {
        var s = Load("""{"settingsVersion": 6, "hiddenControls": ["stop", "badges", "files", "volume", "fullscreen"]}""");
        Assert.Empty(s.BarLeft); // the gap between the volume and the badges went with them
        Assert.Equal(["prevchapter", "prevframe", "play", "nextframe", "nextchapter"], s.BarCentre);
        Assert.Equal(["discmenu", "chaptermenu", "subtitles", "audio", "video", "screenshot", "download", "openlink", "open", "more", "settings"], s.BarRight);
        // Written again, the old list is gone.
        var json = JsonSerializer.Serialize(s, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        Assert.DoesNotContain("hiddenControls", json);
        Assert.Contains("\"barCentre\"", json);
    }

    [Fact]
    public void The_presets_hold_every_piece_once_and_never_leave_play_more_or_settings_off()
    {
        foreach (var layout in new[] { BarItems.Simple, BarItems.Everything })
        {
            var ids = layout.All.Where(i => !BarItems.IsRepeatable(i)).ToList();
            Assert.Equal(ids.Count, ids.Distinct().Count());
            Assert.All(layout.All, id => Assert.NotNull(BarItems.Find(id)));
            Assert.Contains(BarItems.Play, layout.Centre);
            Assert.Contains(BarItems.More, layout.Right);
            Assert.Contains(BarItems.Settings, layout.Right);
        }
        Assert.All(BarItems.SimpleLeavesOff, id => Assert.Contains(id, BarItems.Everything.All));
        Assert.DoesNotContain("subtitles", BarItems.SimpleLeavesOff);
        Assert.DoesNotContain("fullscreen", BarItems.SimpleLeavesOff);
        Assert.Contains("prevframe", BarItems.Simple.Centre); // the frame steps are on it
        Assert.Equal(["play", "more", "settings"], BarItems.All.Where(i => i.Required).Select(i => i.Id));
        Assert.Equal(BarItems.All.Count, BarItems.All.Select(i => i.Id).Distinct().Count());
    }

    [Fact]
    public void A_hand_edited_bar_is_made_sound()
    {
        var s = new PlayerSettings
        {
            BarLeft = ["clock", "spacer", "no-such-piece", "clock", "spacer"],
            BarCentre = ["stop", "clock"],
            BarRight = ["fullscreen", "separator", "separator"],
        };
        s.Normalize();
        Assert.Equal(["clock", "spacer", "spacer"], s.BarLeft); // spacers repeat; the clock only once
        Assert.Equal(["play", "stop"], s.BarCentre); // Play comes back, to the group's middle
        Assert.Equal(["fullscreen", "separator", "separator", "more", "settings"], s.BarRight);
        var nulls = new PlayerSettings { BarLeft = null!, BarCentre = null!, BarRight = null! };
        nulls.Normalize();
        AssertBar(BarItems.Simple, nulls);
    }

    [Fact]
    public void The_bar_goes_to_another_install_as_a_copy()
    {
        var old = new PlayerSettings { BarLeft = ["title"], BarCentre = ["play"], BarRight = ["more", "settings", "clock"] };
        var here = new PlayerSettings();
        here.CopyFrom(old);
        Assert.Equal(["title"], here.BarLeft);
        Assert.Equal(["more", "settings", "clock"], here.BarRight);
        here.BarLeft.Add("clock");
        Assert.Equal(["title"], old.BarLeft);
    }

    [Fact]
    public void The_theme_is_kept_tidy_and_carried_to_another_install()
    {
        Assert.Equal("gta-6", Load("""{"settingsVersion": 5, "theme": " GTA-6 "}""").Theme);
        Assert.Equal("atlas", Load("""{"settingsVersion": 5, "theme": ""}""").Theme);
        var here = new PlayerSettings();
        here.CopyFrom(new PlayerSettings { Theme = "halloween", SettingsVersion = 5 });
        Assert.Equal("halloween", here.Theme);
        Assert.DoesNotContain(nameof(PlayerSettings.Theme), PlayerSettings.MachineSettings);
        Assert.Contains("\"theme\"", JsonSerializer.Serialize(here, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}
