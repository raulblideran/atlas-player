#!/usr/bin/env bash
# Builds the portable Windows release: dist/AtlasPlayer-<version>-portable-win-x64.zip
# Everything (settings.json, cache/, Screenshots/, portable_config/) lives next to AtlasPlayer.exe.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
export PATH="$HOME/.dotnet:$HOME/.cargo/bin:$PATH" DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1

"$ROOT/build/fetch-libs.sh" win-x64

VERSION=$(sed -n 's|.*<Version>\(.*\)</Version>.*|\1|p' "$ROOT/src/AtlasPlayer.App/AtlasPlayer.App.csproj")
# The commit goes into the exe's informational version and the in-app About, so a friend
# can say exactly which build they have. "-dirty" marks a build with uncommitted changes.
COMMIT=$(git -C "$ROOT" rev-parse --short HEAD 2>/dev/null || echo unknown)
if [ -n "$(git -C "$ROOT" status --porcelain 2>/dev/null)" ]; then COMMIT="$COMMIT-dirty"; fi
ZIP="AtlasPlayer-$VERSION-portable-win-x64.zip"

# The folder inside the zip stays "Atlas Player", so unzipping a new version over an old one
# keeps settings.json and the Screenshots folder.
STAGE="$ROOT/dist/Atlas Player"
rm -rf "$STAGE" && mkdir -p "$STAGE"
echo "Publishing with .NET SDK $(dotnet --version)"
dotnet publish "$ROOT/src/AtlasPlayer.App" -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=false \
  -p:DebugType=none -p:GenerateDocumentationFile=false -p:SourceRevisionId="$COMMIT" -o "$STAGE"

# A zip without these doesn't play anything (beta.2 and beta.3 shipped without them).
for f in AtlasPlayer.exe libmpv-2.dll atlas_torrent.dll libvlc/libvlc.dll libvlc/libvlccore.dll \
         libvlc/plugins/access/libdvdnav_plugin.dll ffmpeg.exe ffprobe.exe yt-dlp.exe qjs.exe \
         flac.exe libFLAC.dll libdvdread-8.dll libdvdcss-2.dll \
         vapoursynth/python.exe vapoursynth/Lib/site-packages/vapoursynth/vapoursynth.pyd vapoursynth/Lib/site-packages/havsfunc.py \
         vapoursynth/plugins/mvtools.dll vapoursynth/plugins/vsznedi3.dll vapoursynth/plugins/nnedi3_weights.bin \
         vapoursynth/plugins/RemoveGrainVS.dll vapoursynth/plugins/BestSource.dll vapoursynth/plugins/fmtconv.dll \
         vapoursynth/plugins/MiscFilters.dll vapoursynth/plugins/EEDI3m.dll vapoursynth/plugins/fft3dfilter.dll \
         vapoursynth/plugins/libfftw3f-3.dll; do
  [ -f "$STAGE/$f" ] || { echo "::error::$f is missing from the published folder" >&2; exit 1; }
done
# QTGMC runs from the published folder (beta.11's didn't: an API 3 MVTools that VapourSynth R82 refuses).
if [[ "${OSTYPE:-}" == msys* || "${OSTYPE:-}" == cygwin* ]]; then
  VS="$(cygpath -w "$STAGE/vapoursynth")"
  if ! CHECK="$("$STAGE/vapoursynth/python.exe" -I "$(cygpath -w "$ROOT/build/qtgmc-check.py")" "$VS\Lib\site-packages" "$VS\plugins" 2>&1)"; then
    echo "::error::QTGMC doesn't run from the published folder: $(echo "$CHECK" | tail -3 | tr '\n' ' ')" >&2
    exit 1
  fi
  echo "$CHECK"
fi

cp "$ROOT/README.md" "$STAGE/README.txt" 2>/dev/null || true
cp "$ROOT/CHANGELOG.md" "$STAGE/CHANGELOG.txt"
# CREDITS.md and LICENSE come with the publish output (AtlasPlayer.App.csproj).
cd "$ROOT/dist"
rm -f "$ZIP"
7z a -tzip -mx=7 "$ZIP" "Atlas Player" >/dev/null
echo "Built $ROOT/dist/$ZIP (version $VERSION, commit $COMMIT)"
ls -la "$STAGE"
