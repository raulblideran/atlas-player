#!/usr/bin/env bash
# Stages QTGMC for the Windows build (AtlasPlayer.Core/Ripping/Dvd/Qtgmc.cs) in <out>/vapoursynth:
#   build/fetch-qtgmc-win.sh [<out>]   (default libs/win-x64; build/fetch-libs.sh calls it)
# Python's embeddable build, VapourSynth's wheel (unpacked into Lib\site-packages, as its
# portable installer does), havsfunc r33 (its last release with QTGMC) with mvsfunc and adjust,
# and every plugin QTGMC calls (Qtgmc.Namespaces). VapourSynth R82 loads API 4 plugins only:
# MVTools v29 and fmtconv r31 come from their PyPI wheels (self-contained); znedi3, RemoveGrain,
# BestSource, MiscFilters, EEDI3m and FFT3DFilter (with its FFTW) from the releases VapourSynth's
# package index (vsrepo) lists. Each pinned by SHA-256. build/qtgmc-check.py runs QTGMC with them.
# VapourSynth needs MSVCP140.dll (the VC++ runtime, 14.50 or later): copied from the Windows
# build machine's System32 when there is one (the release is built on GitHub's Windows runner);
# the embeddable Python brings VCRUNTIME140(_1).dll itself.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
OUT="${1:-$ROOT/libs/win-x64}"
PYTHON_VERSION=3.14.10
PYTHON_SHA256=90905e64a840557d8c1bc4f5b74db3d7ee85e3ffb217611cac1c7813acea4159
VS_WHEEL_URL=https://files.pythonhosted.org/packages/09/d7/f67e68a7bf35dc24bdff34b30d14baee5b726790c0bed8cba449790dbc33/vapoursynth-82-cp312-abi3-win_amd64.whl
VS_WHEEL_SHA256=dbde9930d4e85de4eb220e05a0314edb7469b9016c5c58a279844b90da4a32b1
MVTOOLS_WHEEL_URL=https://files.pythonhosted.org/packages/28/f5/1697a4c7238f442a24982a3e5b7b4cbc54d411b40bf3f83b955ab0ed3cc2/vapoursynth_mvtools-29-py3-none-win_amd64.whl
MVTOOLS_SHA256=d64d6525d88ce52572f93e04598083c7cff2a92bf6bc269168130fd7a5227746   # v29
FMTCONV_WHEEL_URL=https://files.pythonhosted.org/packages/c1/df/9698d8e2f0dfed016ddd1f1dbcb3c464c5e68a09b2aefb0e5287d415955c/vapoursynth_fmtconv-31-py3-none-win_amd64.whl
FMTCONV_SHA256=9761744768e1ee1543acbf372092f95d37aeee985ef1bd6dc025cb1804a08293   # r31
ZNEDI3_SHA256=2f6512a971cbc94660b716f9689ee9a55ab6d9fd29484ea62d92a20151d56fd6    # r3.3, with nnedi3_weights.bin
REMOVEGRAIN_SHA256=dc408c560de537d273e78607a776f357ba80d9a7a0074d3c10bc9f3e3376d35f # R1
BESTSOURCE_SHA256=328daf65114863375f5b706b50ef2b3c55832795f10d5194a26462ddf4edd06c  # R23, msvc
MISC_SHA256=54cf54c4d66151c01c1c663ed0d47ca99c7f1b0a94927bd99b35362c02172bd2       # R2
EEDI3_SHA256=fa8515e0aa711ca979a87d812860c8582c7789fd805df2be10760748c0a9c486      # r8
FFT3D_SHA256=ebc2c2d8a437c8ecae656778221882d6fe2b5b7723404dd57b1e2b092962eb09      # R2, with libfftw3f-3.dll
HAVSFUNC_SHA256=4da2839544b1ce9382db670b069dc358228251d147dad91f740a860840e04924
MVSFUNC_COMMIT=865c7486ca860d323754ec4774bc4cca540a7076
MVSFUNC_SHA256=1adeb385d19bac1f8c541b655d1f98a38e18c9a0065d4483c25184c22908a984
MVSFUNC_INIT_SHA256=a08e2bc2d1b255ffa373ef52a63506da6a926e72a4d115aaabe5e95272016d89
MVSFUNC_META_SHA256=654d75ba830a969d5940e1d6a18a7801a0a8b9860fd9aa85688f954ee0d5bd53
ADJUST_COMMIT=a3af7cb57cb37747b0667346375536e65b1fed17
ADJUST_SHA256=22b25995ef37e6d003c2999e51e80d1570376413db9067f703fe4fcef4600bfa
QTGMC_BUILD="python-$PYTHON_VERSION vs-82 mv-v29 fmtc-r31 znedi3-r3.3 rg-R1 bs-R23 misc-R2 eedi3m-r8 fft3d-R2 havsfunc-r33-kw"

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
STAMP="$OUT.qtgmc-build"
VSDIR="$OUT/vapoursynth"
# Re-staged when a pin changes.
if [ ! -f "$VSDIR/python.exe" ] || [ "$(cat "$STAMP" 2>/dev/null)" != "$QTGMC_BUILD" ]; then
  rm -rf "$VSDIR"
  DL="$(mktemp -d)"
  fetch_checked "https://www.python.org/ftp/python/$PYTHON_VERSION/python-$PYTHON_VERSION-embed-amd64.zip" "$PYTHON_SHA256" "$DL/python.zip"
  fetch_checked "$VS_WHEEL_URL" "$VS_WHEEL_SHA256" "$DL/vs.whl"
  fetch_checked "$MVTOOLS_WHEEL_URL" "$MVTOOLS_SHA256" "$DL/mv.whl"
  fetch_checked "$FMTCONV_WHEEL_URL" "$FMTCONV_SHA256" "$DL/fmtc.whl"
  fetch_checked https://github.com/sekrit-twc/znedi3/releases/download/r3.3/znedi3_r3.3.7z "$ZNEDI3_SHA256" "$DL/znedi3.7z"
  fetch_checked https://github.com/vapoursynth/vs-removegrain/releases/download/R1/removegrain-r1.7z "$REMOVEGRAIN_SHA256" "$DL/rg.7z"
  fetch_checked https://github.com/vapoursynth/bestsource/releases/download/R23/BestSource-R23-win64-msvc.zip "$BESTSOURCE_SHA256" "$DL/bs.zip"
  fetch_checked https://github.com/vapoursynth/vs-miscfilters-obsolete/releases/download/R2/miscfilters-r2.7z "$MISC_SHA256" "$DL/misc.7z"
  fetch_checked https://github.com/HomeOfVapourSynthEvolution/VapourSynth-EEDI3/releases/download/r8/EEDI3-r8.7z "$EEDI3_SHA256" "$DL/eedi3.7z"
  fetch_checked https://github.com/myrsloik/VapourSynth-FFT3DFilter/releases/download/R2/FFT3DFilter-r2.7z "$FFT3D_SHA256" "$DL/fft3d.7z"
  fetch_checked https://raw.githubusercontent.com/HomeOfVapourSynthEvolution/havsfunc/r33/havsfunc.py "$HAVSFUNC_SHA256" "$DL/havsfunc.py"
  fetch_checked "https://raw.githubusercontent.com/HomeOfVapourSynthEvolution/mvsfunc/$MVSFUNC_COMMIT/mvsfunc/mvsfunc.py" "$MVSFUNC_SHA256" "$DL/mvsfunc.py"
  fetch_checked "https://raw.githubusercontent.com/HomeOfVapourSynthEvolution/mvsfunc/$MVSFUNC_COMMIT/mvsfunc/__init__.py" "$MVSFUNC_INIT_SHA256" "$DL/mvsfunc-init.py"
  fetch_checked "https://raw.githubusercontent.com/HomeOfVapourSynthEvolution/mvsfunc/$MVSFUNC_COMMIT/mvsfunc/_metadata.py" "$MVSFUNC_META_SHA256" "$DL/mvsfunc-meta.py"
  fetch_checked "https://raw.githubusercontent.com/dubhatervapoursynth/vapoursynth-adjust/$ADJUST_COMMIT/adjust.py" "$ADJUST_SHA256" "$DL/adjust.py"
  SITE="$VSDIR/Lib/site-packages"
  mkdir -p "$SITE/mvsfunc" "$VSDIR/plugins"
  # Relative paths: Git Bash on Windows doesn't translate a path glued to -o.
  (cd "$DL" && 7z x -y -opython python.zip >/dev/null && 7z x -y -ovs vs.whl >/dev/null \
    && 7z e -y -oplugins mv.whl vapoursynth/plugins/mvtools.dll >/dev/null && 7z e -y -oplugins fmtc.whl vapoursynth/plugins/fmtconv.dll >/dev/null \
    && 7z e -y -oplugins znedi3.7z vsznedi3.dll nnedi3_weights.bin >/dev/null \
    && 7z e -y -oplugins rg.7z win64/RemoveGrainVS.dll >/dev/null && 7z e -y -oplugins bs.zip >/dev/null \
    && 7z e -y -oplugins misc.7z win64/MiscFilters.dll >/dev/null && 7z e -y -oplugins eedi3.7z EEDI3m.dll >/dev/null \
    && 7z e -y -oplugins fft3d.7z x64/fft3dfilter.dll x64/libfftw3f-3.dll >/dev/null)
  cp -r "$DL/python/." "$VSDIR/"
  # The embeddable Python's search path: its own zip and folder, plus site-packages.
  printf 'Lib\\site-packages\r\n' >> "$(ls "$VSDIR"/python3*._pth | head -1)"
  cp -r "$DL/vs/vapoursynth" "$DL/vs/"vapoursynth-*.dist-info "$SITE/"
  # Debug symbols, headers and the VfW bridge aren't needed to run scripts.
  rm -rf "$SITE/vapoursynth/"*.pdb "$SITE/vapoursynth/include" "$SITE/vapoursynth/pkgconfig" "$SITE/vapoursynth/vsvfw.dll" "$SITE/vapoursynth/"*.pyi
  cp "$DL/plugins/"* "$VSDIR/plugins/"
  mv "$VSDIR/plugins/BestSource-R23-win64-msvc.dll" "$VSDIR/plugins/BestSource.dll"
  # VapourSynth R82 no longer takes a Python keyword with a leading underscore (_lambda); havsfunc
  # r33 passes two to MVTools. The trailing one (lambda_) is how it's spelt now.
  sed -E 's/\b_lambda=/lambda_=/g; s/\b_global=/global_=/g' "$DL/havsfunc.py" > "$SITE/havsfunc.py"
  [ "$(grep -cE '\b_(lambda|global)=' "$SITE/havsfunc.py")" = 0 ] || { echo "havsfunc keyword patch failed" >&2; exit 1; }
  cp "$DL/adjust.py" "$SITE/"
  cp "$DL/mvsfunc.py" "$SITE/mvsfunc/"
  cp "$DL/mvsfunc-init.py" "$SITE/mvsfunc/__init__.py"
  cp "$DL/mvsfunc-meta.py" "$SITE/mvsfunc/_metadata.py"
  rm -rf "$DL"
  echo "$QTGMC_BUILD" > "$STAMP"
fi
SYS32="${SYSTEMROOT:-${WINDIR:-}}"
if [ -n "$SYS32" ] && command -v cygpath >/dev/null; then SYS32="$(cygpath -u "$SYS32")"; fi
SYS32="$SYS32/System32"
if [ -f "$SYS32/msvcp140.dll" ]; then
  cp "$SYS32/msvcp140.dll" "$VSDIR/"
elif [ ! -f "$VSDIR/msvcp140.dll" ]; then
  echo "warning: no msvcp140.dll here: QTGMC will need the VC++ runtime installed on the PC" >&2
fi
