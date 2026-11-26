#!/usr/bin/env bash
# Sets the app version everywhere it lives, so builds shared with friends are identifiable:
#   ./build/bump-version.sh 1.2.0 "Short summary of the release"
#   ./build/bump-version.sh 2.0.0-beta.1 "…"   (a pre-release: a development entry in the metainfo,
#                                               published as a pre-release by the Release workflow)
# Updates the project file (exe version, zip name, in-app About), adds a CHANGELOG.md
# section (or names the "## Unreleased" one, changes listed while there was no release yet)
# and a metainfo release entry (the Flatpak's version). Fill in the changelog after.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
VERSION="${1:?usage: bump-version.sh <major.minor.patch> [summary]}"
SUMMARY="${2:-}"
[[ "$VERSION" =~ ^[0-9]+\.[0-9]+\.[0-9]+(-[0-9A-Za-z.]+)?$ ]] || { echo "version must look like 1.2.3 or 1.2.3-beta.1" >&2; exit 1; }
DATE="$(date +%F)"
CSPROJ="$ROOT/src/Paramaja.App/Paramaja.App.csproj"
META="$ROOT/packaging/io.github.raulblideran.Paramaja.metainfo.xml"

CURRENT=$(sed -n 's|.*<Version>\(.*\)</Version>.*|\1|p' "$CSPROJ")
if [[ "$CURRENT" == "$VERSION" ]]; then echo "already at $VERSION"; exit 0; fi

sed -i "s|<Version>$CURRENT</Version>|<Version>$VERSION</Version>|" "$CSPROJ"

python3 - "$ROOT/CHANGELOG.md" "$META" "$VERSION" "$DATE" "$SUMMARY" <<'PY'
import sys, html
changelog, meta, version, date, summary = sys.argv[1:]
item = summary or "Describe this release"
s = open(changelog, encoding="utf-8").read()
unreleased = "\n## Unreleased\n"
if unreleased in s:
    # Changes written down before the release: they become its section.
    s = s.replace(unreleased, f"\n## {version} — {date}\n" + (f"\n- {summary}\n" if summary else ""), 1)
else:
    i = s.index("\n## ")
    s = s[:i] + f"\n## {version} — {date}\n\n- {item}\n" + s[i:]
open(changelog, "w", encoding="utf-8").write(s)
m = open(meta, encoding="utf-8").read()
kind = ' type="development"' if "-" in version else ""
m = m.replace("  <releases>\n", f'  <releases>\n    <release version="{version}" date="{date}"{kind}>\n'
              f'      <description>\n        <p>{html.escape(item)}</p>\n      </description>\n    </release>\n', 1)
open(meta, "w", encoding="utf-8").write(m)
PY
echo "Version $CURRENT -> $VERSION. Edit CHANGELOG.md to list the changes, then ./build/publish-win.sh"
