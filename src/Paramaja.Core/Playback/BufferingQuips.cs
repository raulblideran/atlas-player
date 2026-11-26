namespace Paramaja.Core.Playback;

/// <summary>Something to read while the bytes arrive.</summary>
public static class BufferingQuips
{
    public static readonly IReadOnlyList<string> All =
    [
        "Big fast, many speed…",
        "Bruney…",
        "Bribing the router with cookies…",
        "Teaching the bytes to queue politely…",
        "Waking up the server. It's not a morning person…",
        "Rewinding the tape with a pencil…",
        "Blowing on the cartridge…",
        "Microwaving the popcorn…",
        "Dimming the lights…",
        "Shushing the people in the back row…",
        "Adjusting the rabbit ears…",
        "Asking the internet to hold still…",
        "Finding the remote in the sofa…",
        "Turning it off and on again…",
        "Loading the loading screen…",
        "Persuading the buffer it's big enough…",
        "Fetching the frames. Good boy…",
        "Untangling the headphone cable…",
        "Calibrating the dramatic pause…",
        "Counting to infinity, almost there…",
    ];

    private static readonly Random Rng = new();

    /// <summary>A random quip, never the same one twice in a row.</summary>
    public static string Next(string? previous = null)
    {
        if (All.Count < 2) return All[0];
        string pick;
        do pick = All[Rng.Next(All.Count)]; while (pick == previous);
        return pick;
    }
}
