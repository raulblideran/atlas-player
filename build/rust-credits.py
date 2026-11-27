#!/usr/bin/env python3
"""Lists every Rust crate atlas-torrent is built from (its Cargo.lock), with its licence and
authors, between the RUST-CRATES markers in CREDITS.md:
    python3 build/rust-credits.py
Reads each crate's Cargo.toml from cargo's registry cache (~/.cargo/registry/src), so run
`cargo fetch` in native/atlas-torrent first if a crate is missing."""
import glob
import os
import re
import sys
import tomllib

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LOCK = os.path.join(ROOT, "native", "atlas-torrent", "Cargo.lock")
CREDITS = os.path.join(ROOT, "CREDITS.md")
START, END = "<!-- RUST-CRATES:START -->", "<!-- RUST-CRATES:END -->"


def crate_info(name, version):
    for path in glob.glob(os.path.expanduser(f"~/.cargo/registry/src/*/{name}-{version}/Cargo.toml")):
        with open(path, "rb") as f:
            package = tomllib.load(f).get("package", {})
        licence = package.get("license") or ("see its " + package["license-file"] if package.get("license-file") else "?")
        authors = ", ".join(re.sub(r"\s*<[^>]*>", "", a) for a in package.get("authors", []) or [])
        return licence, authors, package.get("repository") or package.get("homepage") or ""
    return None


def main():
    with open(LOCK, "rb") as f:
        lock = tomllib.load(f)
    rows, missing = [], []
    for p in sorted(lock["package"], key=lambda p: (p["name"].lower(), p["version"])):
        if "source" not in p:  # atlas-torrent itself
            continue
        info = crate_info(p["name"], p["version"])
        if info is None:
            missing.append(f'{p["name"]}-{p["version"]}')
            continue
        licence, authors, repo = info
        name = f'[{p["name"]}]({repo})' if repo else p["name"]
        rows.append(f'| {name} | {p["version"]} | {licence} | {authors or "its contributors"} |')
    if missing:
        sys.exit("not in cargo's cache (run cargo fetch): " + ", ".join(missing))
    table = "\n".join(["| Crate | Version | Licence | Authors |", "|---|---|---|---|", *rows])
    with open(CREDITS, encoding="utf-8") as f:
        text = f.read()
    if START not in text or END not in text:
        sys.exit(f"CREDITS.md has no {START} … {END} section")
    head, rest = text.split(START, 1)
    _, tail = rest.split(END, 1)
    with open(CREDITS, "w", encoding="utf-8") as f:
        f.write(f"{head}{START}\n{table}\n{END}{tail}")
    print(f"{len(rows)} crates listed in CREDITS.md")


if __name__ == "__main__":
    main()
