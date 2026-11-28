using System.IO.Pipes;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AtlasPlayer.App.Services;

/// <summary>
/// One window: files opened while Atlas Player runs (from the file manager, a browser's magnet
/// link) go to the window already open instead of starting another. The first instance listens;
/// a later one hands its arguments over and exits. Linux: a Unix socket in the user's runtime
/// folder (inside the Flatpak, the app's own folder there, which every instance shares). Windows:
/// a named pipe for this user. Both are named after the app's folder, so two portable copies
/// (or a host build beside the Flatpak) stay separate.
/// </summary>
public sealed class SingleInstance : IDisposable
{
    private readonly CancellationTokenSource _stop = new();
    private readonly Socket? _socket;
    private readonly string? _socketPath;

    private SingleInstance(Socket? socket, string? socketPath)
    {
        _socket = socket;
        _socketPath = socketPath;
    }

    /// <summary>Where this copy of the app listens: a socket path (Linux), a pipe name (Windows); null where there's nowhere.</summary>
    public static string? Address()
    {
        var copy = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(AppContext.BaseDirectory)))[..12].ToLowerInvariant();
        if (OperatingSystem.IsWindows()) return $"AtlasPlayer-{copy}";
        var runtime = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
        if (string.IsNullOrEmpty(runtime) || !Directory.Exists(runtime)) return null; // no private folder: no single instance
        if (Environment.GetEnvironmentVariable("FLATPAK_ID") is { Length: > 0 } id && Directory.Exists(Path.Combine(runtime, "app", id)))
            runtime = Path.Combine(runtime, "app", id); // /run/user/N itself is the sandbox's own
        return Path.Combine(runtime, $"atlas-player-{copy}.sock");
    }

    /// <summary>
    /// Hands <paramref name="args"/> (already <see cref="LaunchArgs.Normalize"/>d: this process's
    /// folder means nothing to the other) to the copy running at <paramref name="address"/>.
    /// True if one took them; false if none runs (or a crashed one left its socket behind).
    /// </summary>
    public static bool TrySend(string address, IReadOnlyList<string> args)
    {
        var message = JsonSerializer.SerializeToUtf8Bytes(args);
        try
        {
            if (OperatingSystem.IsWindows())
            {
                if (!File.Exists($@"\\.\pipe\{address}")) return false; // none: no waiting for the timeout at every start
                using var pipe = new NamedPipeClientStream(".", address, PipeDirection.Out, PipeOptions.CurrentUserOnly);
                pipe.Connect(300);
                pipe.Write(message);
                pipe.Flush();
                return true;
            }
            if (!File.Exists(address)) return false;
            using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            socket.Connect(new UnixDomainSocketEndPoint(address));
            socket.Send(message);
            socket.Shutdown(SocketShutdown.Send);
            return true;
        }
        catch (Exception e) when (e is TimeoutException or IOException or SocketException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>
    /// Listens at <paramref name="address"/>; each hand-over's arguments go to
    /// <paramref name="received"/> (on a background thread). Null if another copy already listens.
    /// </summary>
    public static SingleInstance? Listen(string address, Action<IReadOnlyList<string>> received)
    {
        SingleInstance instance;
        if (OperatingSystem.IsWindows())
        {
            instance = new SingleInstance(null, null);
            _ = Task.Run(() => instance.ServePipeAsync(address, received));
            return instance;
        }
        var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        try
        {
            // A socket left by a copy that crashed: nothing answers it, so it goes.
            if (File.Exists(address) && !TryConnect(address)) File.Delete(address);
            socket.Bind(new UnixDomainSocketEndPoint(address));
            socket.Listen(4);
        }
        catch (Exception e) when (e is SocketException or IOException or UnauthorizedAccessException)
        {
            socket.Dispose();
            return null;
        }
        instance = new SingleInstance(socket, address);
        _ = Task.Run(() => instance.ServeSocketAsync(received));
        return instance;
    }

    private static bool TryConnect(string path)
    {
        try
        {
            using var probe = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            probe.Connect(new UnixDomainSocketEndPoint(path));
            return true;
        }
        catch (SocketException) { return false; }
    }

    private async Task ServeSocketAsync(Action<IReadOnlyList<string>> received)
    {
        while (!_stop.IsCancellationRequested)
        {
            try
            {
                using var client = await _socket!.AcceptAsync(_stop.Token);
                await using var stream = new NetworkStream(client);
                Deliver(await ReadAllAsync(stream), received);
            }
            catch (OperationCanceledException) { return; }
            catch (ObjectDisposedException) { return; }
            catch (Exception e) when (e is SocketException or IOException) { } // that one hand-over failed: the next may not
        }
    }

    private async Task ServePipeAsync(string name, Action<IReadOnlyList<string>> received)
    {
        while (!_stop.IsCancellationRequested)
        {
            try
            {
                await using var pipe = new NamedPipeServerStream(name, PipeDirection.In, 1, PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await pipe.WaitForConnectionAsync(_stop.Token);
                Deliver(await ReadAllAsync(pipe), received);
            }
            catch (OperationCanceledException) { return; }
            catch (IOException) { await Task.Delay(200); } // another copy has the name, or that hand-over broke
            catch (UnauthorizedAccessException) { return; }
        }
    }

    private async Task<byte[]> ReadAllAsync(Stream stream)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(_stop.Token);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, timeout.Token);
        return buffer.ToArray();
    }

    /// <summary>What a hand-over sent: its arguments, or nothing if it isn't a list of strings.</summary>
    internal static IReadOnlyList<string>? Parse(byte[] message)
    {
        try { return JsonSerializer.Deserialize<string[]>(message); }
        catch (JsonException) { return null; }
    }

    private static void Deliver(byte[] message, Action<IReadOnlyList<string>> received)
    {
        if (Parse(message) is { } args) received(args);
    }

    public void Dispose()
    {
        _stop.Cancel();
        _socket?.Dispose();
        if (_socketPath is not null)
        {
            try { File.Delete(_socketPath); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
        }
    }
}
