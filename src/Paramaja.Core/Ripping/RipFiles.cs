using System.Diagnostics;
using System.Text;

namespace Paramaja.Core.Ripping;

/// <summary>Where a rip is.</summary>
/// <param name="Fraction">Of the whole disc, 0 to 1.</param>
public sealed record RipProgress(string Stage, int Track, int Tracks, double Fraction, double Speed = 0, int Rereads = 0);

/// <summary>File and folder names for rips.</summary>
public static class RipNames
{
    private static readonly char[] Invalid = ['\\', '/', ':', '*', '?', '"', '<', '>', '|'];

    /// <summary>
    /// A name any system takes: no path separators or characters Windows refuses (a colon or
    /// slash becomes a dash, the rest go), no control characters, no trailing dots or spaces.
    /// </summary>
    public static string Safe(string name, int max = 120)
    {
        var b = new StringBuilder(name.Length);
        foreach (var c in name)
        {
            if (char.IsControl(c)) continue;
            if (c is ':' or '/' or '\\' or '|') b.Append(" - ");
            else if (Array.IndexOf(Invalid, c) < 0) b.Append(c);
        }
        var safe = string.Join(' ', b.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries)).Replace(" - - ", " - ");
        if (safe.Length > max) safe = safe[..max];
        safe = safe.TrimEnd('.', ' ');
        return safe.Length == 0 ? "_" : safe;
    }

    /// <summary>A folder under <paramref name="parent"/> no rip has used: "Name", else "Name (1)" and so on.</summary>
    public static string NewFolder(string parent, string name)
    {
        var path = Path.Combine(parent, name);
        for (var n = 1; Directory.Exists(path) || File.Exists(path); n++) path = Path.Combine(parent, $"{name} ({n})");
        return path;
    }

    /// <summary>A file name in <paramref name="folder"/> nothing has: "Name.ext", else "Name (1).ext" and so on.</summary>
    public static string NewFile(string folder, string stem, string extension)
    {
        var path = Path.Combine(folder, stem + extension);
        for (var n = 1; File.Exists(path) || Directory.Exists(path); n++) path = Path.Combine(folder, $"{stem} ({n}){extension}");
        return path;
    }
}

/// <summary>Finds the tools a rip runs: bundled next to the app first, then on the PATH.</summary>
public static class RipTools
{
    public static string? Find(string name, string? appDirectory = null)
    {
        var exe = OperatingSystem.IsWindows() ? name + ".exe" : name;
        var bundled = Path.Combine(appDirectory ?? AppContext.BaseDirectory, exe);
        if (File.Exists(bundled)) return bundled;
        foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Combine(dir, exe);
            if (File.Exists(candidate)) return candidate;
        }
        return null;
    }

    /// <summary>Runs a tool to the end; throws <see cref="IOException"/> with its last words if it fails.</summary>
    public static async Task RunAsync(string exe, IEnumerable<string> arguments, CancellationToken ct,
        Action<string>? onStdout = null, Action<string>? onStderr = null)
    {
        var start = new ProcessStartInfo(exe)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var a in arguments) start.ArgumentList.Add(a);
        using var process = Process.Start(start) ?? throw new IOException($"couldn't start {Path.GetFileName(exe)}");
        var errors = new Queue<string>();
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) onStdout?.Invoke(e.Data); };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is null) return;
            onStderr?.Invoke(e.Data);
            lock (errors)
            {
                errors.Enqueue(e.Data);
                if (errors.Count > 8) errors.Dequeue();
            }
        };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        try
        {
            await process.WaitForExitAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) { }
            throw;
        }
        if (process.ExitCode != 0)
        {
            string last;
            lock (errors) last = string.Join(" ", errors.Where(l => l.Trim().Length > 0).TakeLast(3));
            throw new IOException($"{Path.GetFileNameWithoutExtension(exe)} failed ({process.ExitCode}): {last}");
        }
    }

    /// <summary>
    /// Two tools as a pipe: what <paramref name="producer"/> writes goes into <paramref name="consumer"/>'s
    /// input (QTGMC's frames into the encoder). Cancelling, or either failing, stops both; the
    /// error names the one that failed first, with its last words.
    /// </summary>
    public static async Task RunPipedAsync(string producer, IEnumerable<string> producerArguments,
        string consumer, IEnumerable<string> consumerArguments, CancellationToken ct,
        Action<string>? onConsumerStdout = null, IReadOnlyDictionary<string, string>? producerEnvironment = null)
    {
        static ProcessStartInfo Start(string exe, IEnumerable<string> arguments)
        {
            var start = new ProcessStartInfo(exe) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true };
            foreach (var a in arguments) start.ArgumentList.Add(a);
            return start;
        }

        static void Keep(Queue<string> lines, string? line)
        {
            if (line is null) return;
            lock (lines)
            {
                lines.Enqueue(line);
                if (lines.Count > 8) lines.Dequeue();
            }
        }

        static string Last(Queue<string> lines)
        {
            lock (lines) return string.Join(" ", lines.Where(l => l.Trim().Length > 0).TakeLast(3));
        }

        static void Kill(Process p)
        {
            try { if (!p.HasExited) p.Kill(entireProcessTree: true); }
            catch (Exception e) when (e is InvalidOperationException or System.ComponentModel.Win32Exception) { }
        }

        var from = Start(producer, producerArguments);
        from.RedirectStandardOutput = true;
        if (producerEnvironment is not null)
            foreach (var (k, v) in producerEnvironment) from.Environment[k] = v;
        var to = Start(consumer, consumerArguments);
        to.RedirectStandardInput = true;
        to.RedirectStandardOutput = true;

        using var write = Process.Start(to) ?? throw new IOException($"couldn't start {Path.GetFileName(consumer)}");
        Process read;
        try { read = Process.Start(from) ?? throw new IOException($"couldn't start {Path.GetFileName(producer)}"); }
        catch
        {
            Kill(write);
            throw;
        }
        using var _ = read;
        var readErrors = new Queue<string>();
        var writeErrors = new Queue<string>();
        read.ErrorDataReceived += (_, e) => Keep(readErrors, e.Data);
        write.ErrorDataReceived += (_, e) => Keep(writeErrors, e.Data);
        write.OutputDataReceived += (_, e) => { if (e.Data is not null) onConsumerStdout?.Invoke(e.Data); };
        read.BeginErrorReadLine();
        write.BeginErrorReadLine();
        write.BeginOutputReadLine();

        bool? writeFailedFirst = null;
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var pump = Task.Run(async () =>
        {
            try
            {
                await read.StandardOutput.BaseStream.CopyToAsync(write.StandardInput.BaseStream, 1 << 20, stop.Token).ConfigureAwait(false);
            }
            catch (IOException) { } // the encoder stopped reading: its own exit code tells why
            finally
            {
                try { write.StandardInput.Close(); }
                catch (IOException) { }
            }
        }, CancellationToken.None);
        try
        {
            var readDone = read.WaitForExitAsync(stop.Token);
            var writeDone = write.WaitForExitAsync(stop.Token);
            var first = await Task.WhenAny(readDone, writeDone).ConfigureAwait(false);
            await first.ConfigureAwait(false);
            // The first to finish failing ends the other, which would wait on it for ever.
            if (first == readDone && read.ExitCode != 0)
            {
                Kill(write);
                writeFailedFirst = false;
            }
            else if (first == writeDone && write.ExitCode != 0)
            {
                Kill(read);
                writeFailedFirst = true;
            }
            await Task.WhenAll(readDone, writeDone).ConfigureAwait(false);
            await pump.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            Kill(read);
            Kill(write);
            await stop.CancelAsync().ConfigureAwait(false);
            try { await pump.ConfigureAwait(false); }
            catch (OperationCanceledException) { }
            throw;
        }
        if (read.ExitCode != 0 && writeFailedFirst != true)
            throw new IOException($"{Path.GetFileNameWithoutExtension(producer)} failed ({read.ExitCode}): {Last(readErrors)}");
        if (write.ExitCode != 0)
            throw new IOException($"{Path.GetFileNameWithoutExtension(consumer)} failed ({write.ExitCode}): {Last(writeErrors)}");
    }
}
