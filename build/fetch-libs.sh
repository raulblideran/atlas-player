#!/usr/bin/env bash
# Stages native libraries into libs/<rid>:
#   win-x64   : libmpv-2.dll from shinchiro/mpv-winbuild-cmake (pinned) + atlas_torrent.dll (cross-built),
#               libVLC, yt-dlp, QuickJS, FFmpeg, flac and libdvdread (pinned or committed)
#   linux-x64 : libatlas_torrent.so (libmpv comes from the system / Homebrew)
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
# QTGMC for DVD rips (AtlasPlayer.Core/Ripping/Dvd/Qtgmc.cs): a "vapoursynth" folder with Python's
# embeddable build, VapourSynth's wheel (unpacked into Lib\site-packages, as its portable
# installer does), havsfunc r33 (its last release with QTGMC) with mvsfunc and adjust, and the
# plugins QTGMC needs, from the releases VapourSynth's package index (vsrepo) lists. Each pinned
# by SHA-256 (the wheel's matches PyPI's). VapourSynth needs MSVCP140.dll (the VC++ runtime):
# copied from the Windows build machine's System32 when there is one (the release is built on
# GitHub's Windows runner); the embeddable Python brings VCRUNTIME140(_1).dll itself.
PYTHON_VERSION=3.14.10
PYTHON_SHA256=90905e64a840557d8c1bc4f5b74db3d7ee85e3ffb217611cac1c7813acea4159
VS_WHEEL_URL=https://files.pythonhosted.org/packages/09/d7/f67e68a7bf35dc24bdff34b30d14baee5b726790c0bed8cba449790dbc33/vapoursynth-82-cp312-abi3-win_amd64.whl
VS_WHEEL_SHA256=dbde9930d4e85de4eb220e05a0314edb7469b9016c5c58a279844b90da4a32b1
MVTOOLS_SHA256=b9883003eed100d4ffd44344b658554076b89da6d45041b378e675720426bc95   # v24
ZNEDI3_SHA256=2f6512a971cbc94660b716f9689ee9a55ab6d9fd29484ea62d92a20151d56fd6    # r3.3, with nnedi3_weights.bin
REMOVEGRAIN_SHA256=dc408c560de537d273e78607a776f357ba80d9a7a0074d3c10bc9f3e3376d35f # R1
BESTSOURCE_SHA256=328daf65114863375f5b706b50ef2b3c55832795f10d5194a26462ddf4edd06c  # R23, msvc
HAVSFUNC_SHA256=4da2839544b1ce9382db670b069dc358228251d147dad91f740a860840e04924
MVSFUNC_COMMIT=865c7486ca860d323754ec4774bc4cca540a7076
MVSFUNC_SHA256=1adeb385d19bac1f8c541b655d1f98a38e18c9a0065d4483c25184c22908a984
MVSFUNC_INIT_SHA256=a08e2bc2d1b255ffa373ef52a63506da6a926e72a4d115aaabe5e95272016d89
MVSFUNC_META_SHA256=654d75ba830a969d5940e1d6a18a7801a0a8b9860fd9aa85688f954ee0d5bd53
ADJUST_COMMIT=a3af7cb57cb37747b0667346375536e65b1fed17
ADJUST_SHA256=22b25995ef37e6d003c2999e51e80d1570376413db9067f703fe4fcef4600bfa
QTGMC_BUILD="python-$PYTHON_VERSION vs-82 mv-v24 znedi3-r3.3 rg-R1 bs-R23 havsfunc-r33"
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
cd "$ROOT/native/atlas-torrent"

case "$RID" in
  win-x64)
    if [[ "${OSTYPE:-}" == msys* || "${OSTYPE:-}" == cygwin* ]]; then
      # On Windows (Git Bash, e.g. GitHub's Windows runners): the native MSVC toolchain.
      # .cargo/config.toml links the C runtime statically, so no VC++ redistributable is needed.
      cargo build --release
      cp target/release/atlas_torrent.dll "$OUT/"
    else
      # On Linux: cross-compiled with MinGW.
      cargo build --release --target x86_64-pc-windows-gnu
      cp target/x86_64-pc-windows-gnu/release/atlas_torrent.dll "$OUT/"
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
    # QTGMC: re-staged when a pin changes.
    VSDIR="$OUT/vapoursynth"
    if [ ! -f "$VSDIR/python.exe" ] || [ "$(cat "$ROOT/libs/$RID.qtgmc-build" 2>/dev/null)" != "$QTGMC_BUILD" ]; then
      rm -rf "$VSDIR"
      DL="$(mktemp -d)"
      fetch_checked "https://www.python.org/ftp/python/$PYTHON_VERSION/python-$PYTHON_VERSION-embed-amd64.zip" "$PYTHON_SHA256" "$DL/python.zip"
      fetch_checked "$VS_WHEEL_URL" "$VS_WHEEL_SHA256" "$DL/vs.whl"
      fetch_checked https://github.com/dubhatervapoursynth/vapoursynth-mvtools/releases/download/v24/vapoursynth-mvtools-v24-win64.7z \
        "$MVTOOLS_SHA256" "$DL/mv.7z"
      fetch_checked https://github.com/sekrit-twc/znedi3/releases/download/r3.3/znedi3_r3.3.7z "$ZNEDI3_SHA256" "$DL/znedi3.7z"
      fetch_checked https://github.com/vapoursynth/vs-removegrain/releases/download/R1/removegrain-r1.7z "$REMOVEGRAIN_SHA256" "$DL/rg.7z"
      fetch_checked https://github.com/vapoursynth/bestsource/releases/download/R23/BestSource-R23-win64-msvc.zip "$BESTSOURCE_SHA256" "$DL/bs.zip"
      fetch_checked https://raw.githubusercontent.com/HomeOfVapourSynthEvolution/havsfunc/r33/havsfunc.py "$HAVSFUNC_SHA256" "$DL/havsfunc.py"
      fetch_checked "https://raw.githubusercontent.com/HomeOfVapourSynthEvolution/mvsfunc/$MVSFUNC_COMMIT/mvsfunc/mvsfunc.py" "$MVSFUNC_SHA256" "$DL/mvsfunc.py"
      fetch_checked "https://raw.githubusercontent.com/HomeOfVapourSynthEvolution/mvsfunc/$MVSFUNC_COMMIT/mvsfunc/__init__.py" "$MVSFUNC_INIT_SHA256" "$DL/mvsfunc-init.py"
      fetch_checked "https://raw.githubusercontent.com/HomeOfVapourSynthEvolution/mvsfunc/$MVSFUNC_COMMIT/mvsfunc/_metadata.py" "$MVSFUNC_META_SHA256" "$DL/mvsfunc-meta.py"
      fetch_checked "https://raw.githubusercontent.com/dubhatervapoursynth/vapoursynth-adjust/$ADJUST_COMMIT/adjust.py" "$ADJUST_SHA256" "$DL/adjust.py"
      SITE="$VSDIR/Lib/site-packages"
      mkdir -p "$SITE/mvsfunc" "$VSDIR/plugins"
      (cd "$DL" && 7z x -y -opython python.zip >/dev/null && 7z x -y -ovs vs.whl >/dev/null \
        && 7z e -y -oplugins mv.7z libmvtools.dll >/dev/null && 7z e -y -oplugins znedi3.7z vsznedi3.dll nnedi3_weights.bin >/dev/null \
        && 7z e -y -oplugins rg.7z win64/RemoveGrainVS.dll >/dev/null && 7z e -y -oplugins bs.zip >/dev/null)
      cp -r "$DL/python/." "$VSDIR/"
      # The embeddable Python's search path: its own zip and folder, plus site-packages.
      printf 'Lib\\site-packages\r\n' >> "$(ls "$VSDIR"/python3*._pth | head -1)"
      cp -r "$DL/vs/vapoursynth" "$DL/vs/"vapoursynth-*.dist-info "$SITE/"
      # Debug symbols, headers and the VfW bridge aren't needed to run scripts.
      rm -rf "$SITE/vapoursynth/"*.pdb "$SITE/vapoursynth/include" "$SITE/vapoursynth/pkgconfig" "$SITE/vapoursynth/vsvfw.dll" "$SITE/vapoursynth/"*.pyi
      cp "$DL/plugins/"* "$VSDIR/plugins/"
      mv "$VSDIR/plugins/BestSource-R23-win64-msvc.dll" "$VSDIR/plugins/BestSource.dll"
      cp "$DL/havsfunc.py" "$DL/adjust.py" "$SITE/"
      cp "$DL/mvsfunc.py" "$SITE/mvsfunc/"
      cp "$DL/mvsfunc-init.py" "$SITE/mvsfunc/__init__.py"
      cp "$DL/mvsfunc-meta.py" "$SITE/mvsfunc/_metadata.py"
      rm -rf "$DL"
      echo "$QTGMC_BUILD" > "$ROOT/libs/$RID.qtgmc-build"
    fi
    SYS32="${SYSTEMROOT:-${WINDIR:-}}"
    if [ -n "$SYS32" ] && command -v cygpath >/dev/null; then SYS32="$(cygpath -u "$SYS32")"; fi
    SYS32="$SYS32/System32"
    if [ -f "$SYS32/msvcp140.dll" ]; then
      cp "$SYS32/msvcp140.dll" "$VSDIR/"
    elif [ ! -f "$VSDIR/msvcp140.dll" ]; then
      echo "warning: no msvcp140.dll here: QTGMC will need the VC++ runtime installed on the PC" >&2
    fi
    ;;
  linux-x64)
    cargo build --release
    cp target/release/libatlas_torrent.so "$OUT/"
    ;;
  *) echo "unsupported rid $RID" >&2; exit 1 ;;
esac
ls -la "$OUT"
