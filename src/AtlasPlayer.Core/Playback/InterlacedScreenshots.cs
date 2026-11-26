using System.Buffers.Binary;
using System.Diagnostics;
using AtlasPlayer.Core.Ripping;

namespace AtlasPlayer.Core.Playback;

/// <summary>
/// A screenshot taken while the deinterlacer runs should show the picture as it's seen, not the
/// two fields woven together. Both engines' screenshots come after their deinterlacer (mpv's
/// filters, libVLC's static filter chain), but a combed one was reported on Windows, so the saved
/// picture is checked: if its lines still alternate between two moments, one field is kept and the
/// lines between are interpolated (FFmpeg's edge-directed estdif), in place, still an uncompressed PNG.
/// </summary>
public static class InterlacedScreenshots
{
    /// <summary>
    /// Above this, a picture is combed. Measured on a concert DVD (2.0.0-beta.7): the woven frame
    /// 2.04, mpv's deinterlaced screenshot of it 0.55. A still or progressive picture is near or under 1.
    /// </summary>
    public const double CombedAbove = 1.25;

    /// <summary>
    /// How combed a picture is: the difference between neighbouring lines (one from each field)
    /// over the difference between lines two apart (the same field). Null if it can't be read.
    /// </summary>
    public static double? Combing(string png, string? ffmpeg = null)
    {
        ffmpeg ??= RipTools.Find("ffmpeg");
        if (ffmpeg is null || PngSize(png) is not var (w, h) || w < 2 || h < 4) return null;
        var gray = Run(ffmpeg, ["-v", "error", "-i", png, "-f", "rawvideo", "-pix_fmt", "gray", "-"]);
        if (gray is null || gray.Length != w * h) return null;
        return Combing(gray, w, h);
    }

    internal static double Combing(ReadOnlySpan<byte> gray, int w, int h)
    {
        long near = 0, far = 0;
        for (var y = 0; y + 2 < h; y++)
        {
            var row = gray.Slice(y * w, w);
            var next = gray.Slice((y + 1) * w, w);
            var second = gray.Slice((y + 2) * w, w);
            for (var x = 0; x < w; x++)
            {
                near += Math.Abs(row[x] - next[x]);
                far += Math.Abs(row[x] - second[x]);
            }
        }
        return (double)near / Math.Max(far, 1);
    }

    /// <summary>
    /// Deinterlaces the screenshot in place if it's combed. True if it was; false if it wasn't,
    /// or there's no FFmpeg to do it with (the screenshot then stays as the engine saved it).
    /// </summary>
    public static bool DeinterlaceIfCombed(string png, string? ffmpeg = null)
    {
        ffmpeg ??= RipTools.Find("ffmpeg");
        if (ffmpeg is null || Combing(png, ffmpeg) is not > CombedAbove) return false;
        var temp = Path.Combine(Path.GetDirectoryName(png)!, "." + Path.GetFileNameWithoutExtension(png) + ".deint.png");
        try
        {
            // The top field kept, the lines between it found along the picture's edges. Which field
            // is first in time doesn't matter for a still: either is a whole moment.
            if (!Write(ffmpeg, png, temp, "estdif=mode=frame:parity=tff:deint=all")
                // An FFmpeg older than 4.4 has no estdif: the field alone, stretched back to full height.
                && !Write(ffmpeg, png, temp, "field=top,scale=iw:ih*2:flags=bicubic"))
                return false;
            File.Move(temp, png, overwrite: true);
            return true;
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
        }
    }

    private static bool Write(string ffmpeg, string input, string output, string filter) =>
        Run(ffmpeg, ["-v", "error", "-y", "-i", input, "-vf", filter, "-frames:v", "1", "-update", "1", "-c:v", "png", "-compression_level", "0", output]) is not null
        && File.Exists(output) && new FileInfo(output).Length > 0;

    /// <summary>The width and height in a PNG's header, or null if it isn't one.</summary>
    internal static (int Width, int Height)? PngSize(string png)
    {
        try
        {
            Span<byte> head = stackalloc byte[24];
            using var f = File.OpenRead(png);
            if (f.ReadAtLeast(head, 24, throwOnEndOfStream: false) < 24) return null;
            if (head[0] != 0x89 || head[1] != (byte)'P' || head[12] != (byte)'I' || head[13] != (byte)'H') return null;
            return (BinaryPrimitives.ReadInt32BigEndian(head[16..]), BinaryPrimitives.ReadInt32BigEndian(head[20..]));
        }
        catch (IOException) { return null; }
    }

    /// <summary>Runs FFmpeg; its output, or null if it failed or took too long.</summary>
    private static byte[]? Run(string ffmpeg, IEnumerable<string> arguments)
    {
        var psi = new ProcessStartInfo(ffmpeg)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var a in arguments) psi.ArgumentList.Add(a);
        try
        {
            using var p = Process.Start(psi);
            if (p is null) return null;
            p.ErrorDataReceived += (_, _) => { }; // drained, so a chatty FFmpeg never blocks
            p.BeginErrorReadLine();
            using var output = new MemoryStream();
            var copy = p.StandardOutput.BaseStream.CopyToAsync(output);
            if (!p.WaitForExit(TimeSpan.FromSeconds(20)))
            {
                p.Kill(entireProcessTree: true);
                return null;
            }
            copy.Wait(TimeSpan.FromSeconds(5));
            return p.ExitCode == 0 ? output.ToArray() : null;
        }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException or IOException) { return null; }
    }
}
