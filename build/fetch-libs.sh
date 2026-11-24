#!/usr/bin/env bash
# Stages native libraries into libs/<rid>:
#   win-x64   : libmpv-2.dll from shinchiro/mpv-winbuild-cmake (pinned) + paramaja_torrent.dll (cross-built),
#               libVLC, yt-dlp, QuickJS, FFmpeg, flac and libdvdread (pinned or committed)
#   linux-x64 : libparamaja_torrent.so (libmpv comes from the system / Homebrew)
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
RID="${1:-win-x64}"
# libmpv: a shinchiro build of mpv's master (it carries its own FFmpeg and libass), pinned by
# tag and SHA-256 (as GitHub's API gives it).
MPV_TAG=20261111
MPV_ASSET=mpv-dev-x86_64-20261111-git-bcde6b93cc.7z
MPV_SHA256=273c079b11ea5575822f972756f4c9a71149a5c10130353abeb8586738938452
# YouTube and other sites: yt-dlp, plus QuickJS-NG as the JavaScript runtime it needs for
# YouTube. Pinned by version and SHA-256 (yt-dlp's matches its published SHA2-256SUMS).
YTDLP_VERSION=2026.11.23
YTDLP_SHA256=622ed48023cac1daa52a3bf3a6ac38f03514de214b5f0fa069389fd0f6d17e5c
QJS_VERSION=v0.18.0
QJS_SHA256=ff03398d74ea6e6bd3a8e58058f7003225abcac229d7d57775b7aaae812a49c7
# ffmpeg.exe and ffprobe.exe: yt-dlp needs them to merge the separate video and audio streams
# of a download and to embed its cover art, and the DVD ripper reads DVDs with FFmpeg's DVD-Video
# reader. BtbN's GPL build has it (libdvdnav, libdvdread with libdvdcss built in); a month-end
# autobuild, which BtbN keeps, pinned by its name and SHA-256 (as GitHub's API gives it).
FFMPEG_TAG=autobuild-2026-10-31-12-59
FFMPEG_BUILD=ffmpeg-n9.0.2-39-g7adfbe40dc-win64-gpl-9.0
FFMPEG_SHA256=693f3e224bc184b40524a62ca47526cfebaf2504414021b92fe75f677ad868b9
# The reference FLAC encoder (flac -8) for CD rips, from Xiph's release (flac.exe needs its libFLAC.dll).
FLAC_VERSION=1.5.0
FLAC_SHA256=53f1500f0d6e7c61379d7fee50d4a9f7f504c650009506d9ba015530d76c0dde
# libVLC plays discs with their menus, and Audio CDs. Taken from VideoLAN's own VLC release for
# Windows, which (unlike the libVLC NuGet package) has the DVD and CD readers; pinned by version
# and SHA-256. Its deinterlacer is swapped for the BWDIF one (build/build-vlc-bwdif-win.sh).
LIBVLC_VERSION=3.0.24
LIBVLC_SHA256=1ed59c09152e78aff84663fa76efab26057716576db74332858810e5ef54ae1f
export PATH="$HOME/.cargo/bin:$PATH"

OUT="$ROOT/libs/$RID"

# Downloads a file once and refuses it if the SHA-256 doesn't match.
fetch_checked() {
  local url="$1" sha="$2" dest="$3"
  if [ -f "$dest" ] && echo "$sha  $dest" | sha256sum -c --quiet 2>/dev/null; then return; fi
  echo "Downloading $(basename "$dest")…"
  curl -fL --retry 3 -o "$dest.part" "$url"
  if ! echo "$sha  $dest.part" | sha256sum -c --quiet; then
    rm -f "$dest.part"; echo "checksum mismatch for $url" >&2; exit 1
  fi
  mv "$dest.part" "$dest"
}
mkdir -p "$OUT"
cd "$ROOT/native/paramaja-torrent"

case "$RID" in
  win-x64)
    if [[ "${OSTYPE:-}" == msys* || "${OSTYPE:-}" == cygwin* ]]; then
      # On Windows (Git Bash, e.g. GitHub's Windows runners): the native MSVC toolchain.
      # .cargo/config.toml links the C runtime statically, so no VC++ redistributable is needed.
      cargo build --release
      cp target/release/paramaja_torrent.dll "$OUT/"
    else
      # On Linux: cross-compiled with MinGW.
      cargo build --release --target x86_64-pc-windows-gnu
      cp target/x86_64-pc-windows-gnu/release/paramaja_torrent.dll "$OUT/"
    fi
    # libmpv and FFmpeg are re-fetched when their pin changes (libs/<rid>.*-build record the
    # build, outside the folder the app ships).
    if [ ! -f "$OUT/libmpv-2.dll" ] || [ "$(cat "$ROOT/libs/$RID.libmpv-build" 2>/dev/null)" != "$MPV_ASSET" ]; then
      DL="$(mktemp -d)"
      fetch_checked "https://github.com/shinchiro/mpv-winbuild-cmake/releases/download/$MPV_TAG/$MPV_ASSET" \
        "$MPV_SHA256" "$DL/mpv-dev.7z"
      # Relative paths: Git Bash on Windows doesn't translate a path glued to -o.
      (cd "$DL" && 7z e -y -ox mpv-dev.7z libmpv-2.dll >/dev/null)
      cp "$DL/x/libmpv-2.dll" "$OUT/"
      echo "$MPV_ASSET" > "$ROOT/libs/$RID.libmpv-build"
      rm -rf "$DL"
    fi
    fetch_checked "https://github.com/yt-dlp/yt-dlp/releases/download/$YTDLP_VERSION/yt-dlp.exe" \
      "$YTDLP_SHA256" "$OUT/yt-dlp.exe"
    fetch_checked "https://github.com/quickjs-ng/quickjs/releases/download/$QJS_VERSION/qjs-windows-x86_64.exe" \
      "$QJS_SHA256" "$OUT/qjs.exe"
    # Re-extracted when a reader added since is missing (libcdda: Audio CDs, 1.7.0).
    if [ ! -f "$OUT/libvlc/libvlc.dll" ] || [ ! -f "$OUT/libvlc/plugins/access/libcdda_plugin.dll" ]; then
      rm -rf "$OUT/libvlc"
      DL="$(mktemp -d)"
      fetch_checked "https://download.videolan.org/pub/videolan/vlc/$LIBVLC_VERSION/win64/vlc-$LIBVLC_VERSION-win64.7z" \
        "$LIBVLC_SHA256" "$DL/vlc.7z"
      (cd "$DL" && 7z x -y -ox vlc.7z "vlc-$LIBVLC_VERSION/libvlc*.dll" "vlc-$LIBVLC_VERSION/plugins/*" >/dev/null)
      VLC="$DL/x/vlc-$LIBVLC_VERSION"
      mkdir -p "$OUT/libvlc/plugins/access"
      cp "$VLC/libvlc.dll" "$VLC/libvlccore.dll" "$OUT/libvlc/"
      # Only what disc playback uses: no streaming output, visualizations, Lua, network
      # discovery, muxers or archives.
      for d in audio_filter audio_mixer audio_output codec d3d11 d3d9 demux misc packetizer spu \
               stream_filter text_renderer video_chroma video_filter video_output; do
        cp -r "$VLC/plugins/$d" "$OUT/libvlc/plugins/"
      done
      for p in libdvdnav libdvdread liblibbluray libfilesystem libcdda; do
        cp "$VLC/plugins/access/${p}_plugin.dll" "$OUT/libvlc/plugins/access/"
      done
      # Encoders and formats discs never carry (FFmpeg's decoder covers the rest).
      (cd "$OUT/libvlc/plugins/codec" && rm -f libx264* libx265* libx26410b* libaom* librav1e* libqsv* libzvbi* \
        libvpx* libtheora* libdaala* libschroedinger* libkate* libspeex* libtwolame* libshine* libfluidsynth*)
      # ARIB (Japanese TV) captions and descrambling: never on discs, and aribb24 has open CVEs.
      rm -f "$OUT/libvlc/plugins/codec/libaribsub_plugin.dll" "$OUT/libvlc/plugins/stream_filter/libaribcam_plugin.dll"
      rm -rf "$DL"
    fi
    # VLC's deinterlacer with BWDIF added (packaging/vlc/vlc-bwdif.patch), built for this libVLC.
    cp "$ROOT/packaging/vlc/win64/libdeinterlace_plugin.dll" "$OUT/libvlc/plugins/video_filter/"
    # Re-fetched when the pinned build changes.
    if [ ! -f "$OUT/ffmpeg.exe" ] || [ ! -f "$OUT/ffprobe.exe" ] || [ "$(cat "$ROOT/libs/$RID.ffmpeg-build" 2>/dev/null)" != "$FFMPEG_BUILD" ]; then
      DL="$(mktemp -d)"
      fetch_checked "https://github.com/BtbN/FFmpeg-Builds/releases/download/$FFMPEG_TAG/$FFMPEG_BUILD.zip" \
        "$FFMPEG_SHA256" "$DL/ffmpeg.zip"
      (cd "$DL" && 7z e -y -ox ffmpeg.zip "$FFMPEG_BUILD/bin/ffmpeg.exe" "$FFMPEG_BUILD/bin/ffprobe.exe" >/dev/null)
      cp "$DL/x/ffmpeg.exe" "$DL/x/ffprobe.exe" "$OUT/"
      echo "$FFMPEG_BUILD" > "$ROOT/libs/$RID.ffmpeg-build"
      rm -rf "$DL"
    fi
    if [ ! -f "$OUT/flac.exe" ] || [ ! -f "$OUT/libFLAC.dll" ]; then
      DL="$(mktemp -d)"
      fetch_checked "https://ftp.osuosl.org/pub/xiph/releases/flac/flac-$FLAC_VERSION-win.zip" "$FLAC_SHA256" "$DL/flac.zip"
      (cd "$DL" && 7z e -y -ox flac.zip "flac-$FLAC_VERSION-win/Win64/flac.exe" "flac-$FLAC_VERSION-win/Win64/libFLAC.dll" >/dev/null)
      cp "$DL/x/flac.exe" "$DL/x/libFLAC.dll" "$OUT/"
      rm -rf "$DL"
    fi
    # libdvdread and libdvdcss for the DVD ripper's VIDEO_TS copy (build/build-dvdread-win.sh).
    cp "$ROOT/packaging/dvd/win64/libdvdcss-2.dll" "$ROOT/packaging/dvd/win64/libdvdread-8.dll" "$OUT/"
    ;;
  linux-x64)
    cargo build --release
    cp target/release/libparamaja_torrent.so "$OUT/"
    ;;
  *) echo "unsupported rid $RID" >&2; exit 1 ;;
esac
ls -la "$OUT"
