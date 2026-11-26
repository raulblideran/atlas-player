#!/usr/bin/env bash
# Builds the portable Windows release: dist/Paramaja-portable-win-x64.zip
# Everything (settings.json, cache/, Screenshots/, portable_config/) lives next to Paramaja.exe.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
export PATH="$HOME/.dotnet:$HOME/.cargo/bin:$PATH" DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1

"$ROOT/build/fetch-libs.sh" win-x64

VERSION=$(sed -n 's|.*<Version>\(.*\)</Version>.*|\1|p' "$ROOT/src/Paramaja.App/Paramaja.App.csproj")
# The commit goes into the exe's informational version and the in-app About, so a friend
# can say exactly which build they have. "-dirty" marks a build with uncommitted changes.
COMMIT=$(git -C "$ROOT" rev-parse --short HEAD 2>/dev/null || echo unknown)
if [ -n "$(git -C "$ROOT" status --porcelain 2>/dev/null)" ]; then COMMIT="$COMMIT-dirty"; fi
ZIP="Paramaja-$VERSION-portable-win-x64.zip"

# The folder inside the zip stays "Paramaja", so unzipping a new version over an old one
# keeps settings.json and the Screenshots folder.
STAGE="$ROOT/dist/Paramaja"
rm -rf "$STAGE" && mkdir -p "$STAGE"
echo "Publishing with .NET SDK $(dotnet --version)"
dotnet publish "$ROOT/src/Paramaja.App" -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=false \
  -p:DebugType=none -p:GenerateDocumentationFile=false -p:SourceRevisionId="$COMMIT" -o "$STAGE"

# A zip without these doesn't play anything (beta.2 and beta.3 shipped without them).
for f in Paramaja.exe libmpv-2.dll paramaja_torrent.dll libvlc/libvlc.dll libvlc/libvlccore.dll \
         libvlc/plugins/access/libdvdnav_plugin.dll ffmpeg.exe ffprobe.exe yt-dlp.exe qjs.exe \
         flac.exe libFLAC.dll libdvdread-8.dll libdvdcss-2.dll \
         vapoursynth/python.exe vapoursynth/Lib/site-packages/vapoursynth/vapoursynth.pyd vapoursynth/Lib/site-packages/havsfunc.py \
         vapoursynth/plugins/libmvtools.dll vapoursynth/plugins/vsznedi3.dll vapoursynth/plugins/nnedi3_weights.bin \
         vapoursynth/plugins/RemoveGrainVS.dll vapoursynth/plugins/BestSource.dll; do
  [ -f "$STAGE/$f" ] || { echo "::error::$f is missing from the published folder" >&2; exit 1; }
done

cp "$ROOT/README.md" "$STAGE/README.txt" 2>/dev/null || true
cp "$ROOT/CHANGELOG.md" "$STAGE/CHANGELOG.txt"
cd "$ROOT/dist"
rm -f "$ZIP"
7z a -tzip -mx=7 "$ZIP" Paramaja >/dev/null
echo "Built $ROOT/dist/$ZIP (version $VERSION, commit $COMMIT)"
ls -la "$STAGE"
