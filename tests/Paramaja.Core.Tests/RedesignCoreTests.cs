using System.Text.Json;
using Paramaja.Core.Settings;

namespace Paramaja.Core.Tests;

/// <summary>2.0.0's settings: the theme, and the Simple bar for everyone who had every button.</summary>
public class RedesignCoreTests
{
    private static PlayerSettings Load(string json)
    {
        var dir = Path.Combine(Path.GetTempPath(), "paramaja-core-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, "settings.json");
        File.WriteAllText(file, json);
        return new SettingsStore(file).Load();
    }

    [Fact]
    public void A_fresh_install_gets_the_simple_bar_and_the_default_theme()
    {
        var s = Load("{}");
        Assert.Equal(PlayerSettings.SimpleBarHidden, s.HiddenControls);
        Assert.Equal("paramaja", s.Theme);
        Assert.Equal(PlayerSettings.CurrentVersion, s.SettingsVersion);
    }

    [Fact]
    public void Who_had_every_button_moves_to_the_simple_bar_once()
    {
        var s = Load("""{"settingsVersion": 4, "hiddenControls": []}""");
        Assert.Equal(PlayerSettings.SimpleBarHidden, s.HiddenControls);
        // Chose Everything afterwards: it stays.
        s.HiddenControls = [];
        s.Normalize();
        Assert.Empty(s.HiddenControls);
    }

    [Fact]
    public void Who_picked_their_own_buttons_keeps_them()
    {
        var s = Load("""{"settingsVersion": 4, "hiddenControls": ["stop", "badges"]}""");
        Assert.Equal(["stop", "badges"], s.HiddenControls);
    }

    [Fact]
    public void The_simple_bar_hides_only_buttons_that_exist_and_never_play_or_settings()
    {
        Assert.All(PlayerSettings.SimpleBarHidden, id => Assert.Contains(id, PlayerSettings.ControlIds));
        Assert.DoesNotContain("subtitles", PlayerSettings.SimpleBarHidden);
        Assert.DoesNotContain("fullscreen", PlayerSettings.SimpleBarHidden);
    }

    [Fact]
    public void The_theme_is_kept_tidy_and_carried_to_another_install()
    {
        Assert.Equal("gta-6", Load("""{"settingsVersion": 5, "theme": " GTA-6 "}""").Theme);
        Assert.Equal("paramaja", Load("""{"settingsVersion": 5, "theme": ""}""").Theme);
        var here = new PlayerSettings();
        here.CopyFrom(new PlayerSettings { Theme = "halloween", SettingsVersion = 5 });
        Assert.Equal("halloween", here.Theme);
        Assert.DoesNotContain(nameof(PlayerSettings.Theme), PlayerSettings.MachineSettings);
        Assert.Contains("\"theme\"", JsonSerializer.Serialize(here, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}
