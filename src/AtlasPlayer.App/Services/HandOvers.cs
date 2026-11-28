namespace AtlasPlayer.App.Services;

/// <summary>
/// Files later copies handed over (SingleInstance). The listener starts with the program, before
/// the window exists, so what arrives early is held here until the window takes it.
/// </summary>
public static class HandOvers
{
    private static readonly Lock Gate = new();
    private static readonly List<IReadOnlyList<string>> Held = [];
    private static Action<IReadOnlyList<string>>? _deliver;

    /// <summary>From the listener (any thread): passed on, or held until <see cref="Attach"/>.</summary>
    public static void Receive(IReadOnlyList<string> args)
    {
        Action<IReadOnlyList<string>>? deliver;
        lock (Gate)
        {
            deliver = _deliver;
            if (deliver is null) { Held.Add(args); return; }
        }
        deliver(args);
    }

    /// <summary>The window is ready: what was held comes first, in order, then each new hand-over.</summary>
    public static void Attach(Action<IReadOnlyList<string>> deliver)
    {
        List<IReadOnlyList<string>> held;
        lock (Gate)
        {
            _deliver = deliver;
            held = [.. Held];
            Held.Clear();
        }
        foreach (var args in held) deliver(args);
    }

    /// <summary>Back to holding (tests).</summary>
    internal static void Detach()
    {
        lock (Gate) _deliver = null;
    }
}
