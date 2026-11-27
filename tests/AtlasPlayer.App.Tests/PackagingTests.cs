using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace AtlasPlayer.App.Tests;

/// <summary>
/// The Flatpak manifest, desktop entry and metainfo are text that nothing compiles, so a typo
/// in one only shows up as an app that installs without an icon or never opens magnet links.
/// These tests read them and hold them to each other and to the project.
/// </summary>
public class PackagingTests
{
    private static readonly string Root = FindRoot();
    private static readonly string Packaging = Path.Combine(Root, "packaging");
    private const string Id = Program.AppId;

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AtlasPlayer.sln"))) dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("repo root not found");
    }

    private static string Read(string name) => File.ReadAllText(Path.Combine(Packaging, name));
    private static string Manifest => Read($"{Id}.yaml");

    private static Dictionary<string, string> DesktopEntry() =>
        Read($"{Id}.desktop").Split('\n')
            .Where(l => l.Contains('=') && !l.StartsWith('#'))
            .Select(l => l.Split('=', 2))
            .ToDictionary(p => p[0], p => p[1].Trim());

    private static string ProjectVersion() =>
        Regex.Match(File.ReadAllText(Path.Combine(Root, "src", "AtlasPlayer.App", "AtlasPlayer.App.csproj")),
            "<Version>(.*?)</Version>").Groups[1].Value;

    [Fact]
    public void Every_packaging_file_is_named_after_the_app_id()
    {
        foreach (var ext in new[] { "yaml", "desktop", "metainfo.xml", "svg" })
            Assert.True(File.Exists(Path.Combine(Packaging, $"{Id}.{ext}")), $"{Id}.{ext} missing");
        Assert.Contains($"app-id: {Id}", Manifest);
    }

    [Fact]
    public void Manifest_grants_what_playback_and_torrents_need()
    {
        var m = Manifest;
        foreach (var arg in new[] { "--socket=x11", "--device=dri", "--socket=pulseaudio", "--share=network",
                     "--filesystem=host:ro", "--filesystem=xdg-pictures", "--filesystem=xdg-music", "--filesystem=xdg-videos" })
            Assert.Contains($"- {arg}", m);
        Assert.Contains("command: atlas-player", m);
    }

    [Fact]
    public void Ffmpeg_reads_dvds_with_the_dvd_libraries_built_before_it()
    {
        var m = Manifest;
        Assert.Contains("- --enable-libdvdread", m);
        Assert.Contains("- --enable-libdvdnav", m);
        Assert.True(m.IndexOf("- name: libdvdread", StringComparison.Ordinal) < m.IndexOf("- name: ffmpeg", StringComparison.Ordinal));
        Assert.True(m.IndexOf("- name: libdvdnav", StringComparison.Ordinal) < m.IndexOf("- name: ffmpeg", StringComparison.Ordinal));
    }

    [Fact]
    public void Ffmpeg_encodes_with_x264_and_x265_built_before_it()
    {
        var m = Manifest;
        Assert.Contains("- --enable-libx264", m);
        Assert.Contains("- --enable-libx265", m);
        Assert.True(m.IndexOf("- name: x264", StringComparison.Ordinal) < m.IndexOf("- name: ffmpeg", StringComparison.Ordinal));
        Assert.True(m.IndexOf("- name: x265", StringComparison.Ordinal) < m.IndexOf("- name: ffmpeg", StringComparison.Ordinal));
    }

    [Fact]
    public void Every_downloaded_archive_is_pinned_by_hash_and_every_git_source_by_commit()
    {
        var lines = Manifest.Split('\n').Select(l => l.Trim()).ToList();
        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i] == "- type: archive")
                Assert.StartsWith("sha256: ", lines[i + 2]);
            if (lines[i] == "- type: git")
                Assert.Contains(lines.Skip(i + 1).Take(3), l => Regex.IsMatch(l, "^commit: [0-9a-f]{40}$"));
        }
    }

    [Fact]
    public void Qtgmc_comes_with_both_builds_where_the_ripper_looks()
    {
        var m = Manifest;
        // Flatpak: VapourSynth's wheel, havsfunc r33 and the plugins QTGMC needs, in /app/lib/vapoursynth (Qtgmc.Find).
        Assert.True(m.IndexOf("- name: vapoursynth", StringComparison.Ordinal) < m.IndexOf("- name: bestsource", StringComparison.Ordinal));
        Assert.True(m.IndexOf("- name: ffmpeg", StringComparison.Ordinal) < m.IndexOf("- name: bestsource", StringComparison.Ordinal));
        // The app's own module, whatever the checkout's line endings (CRLF on Windows).
        var app = Regex.Match(m, @"^  - name: atlas-player\r?$", RegexOptions.Multiline);
        Assert.True(app.Success);
        Assert.True(m.IndexOf("- name: bestsource", StringComparison.Ordinal) < app.Index);
        Assert.Contains("python3 -m zipfile -e vapoursynth.whl /app/lib/vapoursynth/python/", m);
        Assert.Contains("havsfunc/r33/havsfunc.py", m);
        foreach (var plugin in new[] { "vapoursynth-mvtools.whl", "libvsznedi3.so", "libremovegrain.so", "nnedi3_weights.bin",
                     "libfmtconv.so", "libmiscfilters.so", "libeedi3m.so", "libfft3dfilter.so" })
            Assert.Contains(plugin, m);
        // And the build runs QTGMC with them, after BestSource (prompt 12: beta.11's didn't load).
        Assert.True(m.IndexOf("- name: bestsource", StringComparison.Ordinal) < m.IndexOf("- name: qtgmc-check", StringComparison.Ordinal));
        Assert.Contains("python3 -I qtgmc-check.py /app/lib/vapoursynth/python /app/lib/vapoursynth/plugins", m);
        Assert.Contains("build/libbestsource.so /app/lib/vapoursynth/plugins/", m);
        // Every file it fetches is pinned too (the repo's own files aren't fetched).
        var lines = m.Split('\n').Select(l => l.Trim()).ToList();
        for (var i = 0; i < lines.Count; i++)
            if (lines[i] == "- type: file" && lines[i + 1].StartsWith("url: ", StringComparison.Ordinal))
                Assert.StartsWith("sha256: ", lines[i + 2]);
        // Windows: the same, in a "vapoursynth" folder next to AtlasPlayer.exe, checked by the publish script.
        Assert.Contains("build/fetch-qtgmc-win.sh\" \"$OUT\"", File.ReadAllText(Path.Combine(Root, "build", "fetch-libs.sh")));
        var fetch = File.ReadAllText(Path.Combine(Root, "build", "fetch-qtgmc-win.sh"));
        Assert.Contains("VSDIR=\"$OUT/vapoursynth\"", fetch);
        Assert.Matches("VS_WHEEL_SHA256=[0-9a-f]{64}", fetch);
        var publish = File.ReadAllText(Path.Combine(Root, "build", "publish-win.sh"));
        Assert.Contains("BestSource.dll", publish);
        Assert.Contains("build/qtgmc-check.py", publish);
    }

    [Fact]
    public void Wrapper_launches_the_folder_the_app_is_published_to()
    {
        var wrapper = Read(Path.Combine("flatpak", "atlas-player.sh"));
        Assert.Contains("exec /app/lib/atlas-player/AtlasPlayer \"$@\"", wrapper);
        Assert.Contains("-o /app/lib/atlas-player", Manifest);
        // The torrent engine has to sit beside the exe, where NativeLibraries looks first.
        Assert.Contains("libatlas_torrent.so -t /app/lib/atlas-player/", Manifest);
    }

    [Fact]
    public void Desktop_entry_opens_files_and_magnet_links()
    {
        var d = DesktopEntry();
        Assert.Equal("atlas-player %U", d["Exec"]);
        Assert.Equal(Id, d["Icon"]);
        Assert.Equal(Id, d["StartupWMClass"]);
        var mime = d["MimeType"].Split(';', StringSplitOptions.RemoveEmptyEntries);
        Assert.Contains("x-scheme-handler/magnet", mime);
        Assert.Contains("video/x-matroska", mime);
        Assert.Contains("video/mp4", mime);
    }

    [Fact]
    public void Metainfo_matches_the_app_and_the_current_version()
    {
        var x = XDocument.Load(Path.Combine(Packaging, $"{Id}.metainfo.xml")).Root!;
        Assert.Equal(Id, x.Element("id")!.Value);
        Assert.Equal($"{Id}.desktop", x.Element("launchable")!.Value);
        Assert.Equal("GPL-3.0-or-later", x.Element("project_license")!.Value);
        var latest = x.Element("releases")!.Elements("release").First();
        Assert.Equal(ProjectVersion(), latest.Attribute("version")!.Value);
    }

    [Fact]
    public void Packaged_icon_is_the_app_icon()
    {
        Assert.Equal(File.ReadAllText(Path.Combine(Root, "src", "AtlasPlayer.App", "Assets", "atlas-player.svg")), Read($"{Id}.svg"));
        Assert.True(File.Exists(Path.Combine(Root, "LICENSE")));
    }
}
