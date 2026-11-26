#!/usr/bin/env bash
# Renders the app icon (Assets/atlas-player.svg, also packaging's) into the PNG and the multi-size ICO the app uses (needs ImageMagick).
set -euo pipefail
A="$(cd "$(dirname "$0")/.." && pwd)/src/AtlasPlayer.App/Assets"
T="$(mktemp -d)"
magick -background none -density 384 "$A/atlas-player.svg" -resize 256x256 "$A/atlas-player-256.png"
for s in 16 24 32 48 64 128; do magick "$A/atlas-player-256.png" -resize ${s}x${s} "$T/$s.png"; done
magick "$T"/{16,24,32,48,64,128}.png "$A/atlas-player-256.png" -define icon:format=png "$A/atlas-player.ico"
rm -rf "$T"
echo "wrote $A/atlas-player.ico and $A/atlas-player-256.png"
