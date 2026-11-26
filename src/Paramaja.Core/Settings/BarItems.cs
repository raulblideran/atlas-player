namespace Paramaja.Core.Settings;

/// <summary>
/// One piece of the controls bar: a button, a badge, a readout or a gap. <see cref="Required"/>
/// pieces (Play, More, Settings) can move but never leave the bar; <see cref="Repeatable"/> ones
/// (spacer, separator) can be placed any number of times.
/// </summary>
public sealed record BarItem(string Id, bool Required = false, bool Repeatable = false);

/// <summary>The controls bar's left, centre and right groups, each its items' ids in order.</summary>
public sealed record BarLayout(IReadOnlyList<string> Left, IReadOnlyList<string> Centre, IReadOnlyList<string> Right)
{
    public IEnumerable<string> All => Left.Concat(Centre).Concat(Right);

    public bool SameAs(IReadOnlyList<string> left, IReadOnlyList<string> centre, IReadOnlyList<string> right) =>
        Left.SequenceEqual(left) && Centre.SequenceEqual(centre) && Right.SequenceEqual(right);

    /// <summary>This layout with some items taken off (every occurrence).</summary>
    public BarLayout Without(IEnumerable<string> ids)
    {
        var off = ids.ToHashSet();
        return new([.. Left.Where(i => !off.Contains(i))], [.. Centre.Where(i => !off.Contains(i))], [.. Right.Where(i => !off.Contains(i))]);
    }
}

/// <summary>Everything the controls bar can hold (Settings → Appearance arranges it).</summary>
public static class BarItems
{
    public const string Play = "play", More = "more", Settings = "settings", Spacer = "spacer", Separator = "separator";

    /// <summary>The catalogue, in the Everything bar's order, then the extras no preset places.</summary>
    public static readonly IReadOnlyList<BarItem> All =
    [
        new("mute"), new("volume"), new("volumetext"),
        new("badge-hdr"), new("badge-codec"), new("badge-resolution"), new("badge-decoder"), new("badge-audio"), new("badge-deinterlace"),
        new("stop"), new("prevfile"), new("prevchapter"), new("prevframe"), new(Play, Required: true),
        new("nextframe"), new("nextchapter"), new("nextfile"),
        new("discmenu"), new("chaptermenu"), new("subtitles"), new("audio"), new("video"),
        new("screenshot"), new("download"), new("openlink"), new("open"),
        new(More, Required: true), new(Settings, Required: true), new("fullscreen"),
        new("elapsed"), new("remaining"), new("total"), new("clock"), new("title"),
        new(Spacer, Repeatable: true), new(Separator, Repeatable: true),
    ];

    private static readonly Dictionary<string, BarItem> ById = All.ToDictionary(i => i.Id);

    public static BarItem? Find(string id) => ById.GetValueOrDefault(id);
    public static bool IsRequired(string id) => Find(id)?.Required == true;
    public static bool IsRepeatable(string id) => Find(id)?.Repeatable == true;

    /// <summary>Every button that applies to what's playing.</summary>
    public static readonly BarLayout Everything = new(
        ["mute", "volume", "volumetext", Spacer,
         "badge-hdr", "badge-codec", "badge-resolution", "badge-decoder", "badge-audio", "badge-deinterlace"],
        ["stop", "prevfile", "prevchapter", "prevframe", Play, "nextframe", "nextchapter", "nextfile"],
        ["discmenu", "chaptermenu", "subtitles", "audio", "video", "screenshot", "download", "openlink", "open",
         More, Settings, "fullscreen"]);

    /// <summary>What the Simple bar (the default) leaves to the More menu.</summary>
    public static readonly IReadOnlyList<string> SimpleLeavesOff =
        ["stop", "prevchapter", "nextchapter", "screenshot", "download", "openlink", "open"];

    /// <summary>The default: play, volume, the track and video menus, fullscreen and settings.</summary>
    public static readonly BarLayout Simple = Everything.Without(SimpleLeavesOff);

    /// <summary>
    /// Media Player Classic's toolbar: play and stop, the skip and frame buttons on the left, the
    /// menus and the volume on the right, nothing in the middle and no badges. The MPC-HC theme
    /// brings it along (the app's SelectedTheme); it's a preset of its own too.
    /// </summary>
    public static readonly BarLayout Classic = new(
        [Play, "stop", Separator, "prevfile", "prevchapter", "nextchapter", "nextfile", Separator, "prevframe", "nextframe"],
        [],
        ["subtitles", "audio", "video", Separator, More, Settings, "fullscreen", Separator, "mute", "volume"]);

    /// <summary>The ids of 2.0.0's show-or-hide list (settings version 6 and before) as the pieces they are now.</summary>
    public static IEnumerable<string> Expand(string oldId) => oldId switch
    {
        "files" => ["prevfile", "nextfile"],
        "chapters" => ["prevchapter", "nextchapter"],
        "frames" => ["prevframe", "nextframe"],
        "volume" => ["mute", "volume", "volumetext"],
        "badges" => ["badge-hdr", "badge-codec", "badge-resolution", "badge-decoder", "badge-audio", "badge-deinterlace"],
        _ => [oldId],
    };
}
