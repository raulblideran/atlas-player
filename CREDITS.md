# Credits

Atlas Player is a free, non-commercial hobby project. It has no ads, no tracking, no accounts
and no paid features, and it never will. It is made in spare time, mostly for watching and
looking after concert recordings, and it is shared in the hope that it is useful to someone else.

Almost everything that makes it work was written by other people, who share their work freely.
Atlas Player only puts those pieces together. This page names every one of them, what it does in
the app, who made it and under which licence it is shared. Thank you to all of them.

Atlas Player itself is licensed under GPL-3.0-or-later (see [LICENSE](LICENSE)). The licences
below are the ones each project states; their full texts come with each project's source. The
Flatpak installs them under `/app/share/licenses/io.github.raulblideran.AtlasPlayer`, and the
Windows zip has this file and the app's LICENSE next to `AtlasPlayer.exe`. If a credit is missing
or wrong, please open an issue and it will be fixed.

Atlas Player is not affiliated with or endorsed by any of the projects, services or companies
named here. Product names and trademarks belong to their owners.

## Playback

| Component | What it does in Atlas Player | Made by | Licence |
|---|---|---|---|
| [mpv](https://mpv.io) (libmpv) | The player: decoding, rendering, audio, subtitles, playlists | The mpv developers, building on mplayer2 and MPlayer | GPL-2.0-or-later (this build; LGPL-2.1-or-later without GPL parts) |
| [FFmpeg](https://ffmpeg.org) | Decoders, demuxers and filters behind mpv. Also the DVD ripper's encoder and muxer, and joining downloads | The FFmpeg developers | LGPL-2.1-or-later; this build enables GPL and version 3 parts, so GPL-3.0-or-later |
| [libplacebo](https://code.videolan.org/videolan/libplacebo) | mpv's GPU renderer: scaling, HDR tone mapping, colour | Niklas Haas and contributors (VideoLAN) | LGPL-2.1-or-later |
| [libass](https://github.com/libass/libass) | Styled (ASS/SSA) subtitles | libass contributors | ISC |
| [LuaJIT](https://luajit.org) | mpv's scripting engine | Mike Pall and contributors | MIT |
| [libXpresent](https://gitlab.freedesktop.org/xorg/lib/libxpresent) | Frame timing for mpv on X11 (Flatpak) | Keith Packard, X.Org Foundation | MIT/X11 |
| [VLC media player](https://www.videolan.org/vlc/) / libVLC 3 | Plays DVDs and Blu-rays with their menus, and Audio CDs | VideoLAN and the VLC contributors | LGPL-2.1-or-later (libVLC), GPL-2.0-or-later (some modules) |
| BWDIF for VLC (`packaging/vlc/vlc-bwdif.patch`) | Lets VLC's deinterlacer use BWDIF, ported from FFmpeg's `vf_bwdif` | Thomas Mundt (BWDIF in FFmpeg); ported for Atlas Player | LGPL-2.1-or-later |
| [LibVLCSharp](https://code.videolan.org/videolan/LibVLCSharp) | .NET bindings for libVLC | VideoLAN | LGPL-2.1-or-later |
| [mpv-winbuild-cmake](https://github.com/shinchiro/mpv-winbuild-cmake) | The Windows build of libmpv, with its FFmpeg and libraries | shinchiro | Build scripts; the libraries keep their own licences (mpv: GPL) |

## Discs

| Component | What it does in Atlas Player | Made by | Licence |
|---|---|---|---|
| [libdvdcss](https://www.videolan.org/developers/libdvdcss.html) | Reads encrypted DVDs | VideoLAN | GPL-2.0-or-later |
| [libdvdread](https://www.videolan.org/developers/libdvdnav.html) | Reads DVD structures (titles, chapters, IFOs) | VideoLAN and contributors | GPL-2.0-or-later |
| [libdvdnav](https://www.videolan.org/developers/libdvdnav.html) | DVD menus and navigation | VideoLAN and contributors | GPL-2.0-or-later |
| [libbluray](https://www.videolan.org/developers/libbluray.html) | Reads Blu-ray discs and folders | VideoLAN and contributors | LGPL-2.1-or-later |
| [libudfread](https://code.videolan.org/videolan/libudfread) | Reads UDF disc images (Blu-ray ISOs) | VideoLAN and contributors | LGPL-2.1-or-later |

## Ripping and encoding

| Component | What it does in Atlas Player | Made by | Licence |
|---|---|---|---|
| [x264](https://www.videolan.org/developers/x264.html) | H.264 encoding for DVD rips | VideoLAN and the x264 developers | GPL-2.0-or-later |
| [x265](https://www.x265.org) | H.265 encoding for DVD rips | MulticoreWare and the x265 developers | GPL-2.0-or-later |
| [FLAC](https://xiph.org/flac/) | Encodes and verifies ripped audio | Xiph.Org Foundation and contributors | GPL-2.0-or-later (tools), BSD-3-Clause (libFLAC) |
| [FFmpeg-Builds](https://github.com/BtbN/FFmpeg-Builds) | The Windows build of the `ffmpeg` program | BtbN | MIT (build scripts); FFmpeg keeps its own licence |

## QTGMC deinterlacing (DVD rips)

| Component | What it does in Atlas Player | Made by | Licence |
|---|---|---|---|
| [VapourSynth](https://www.vapoursynth.com) | Runs the QTGMC script | Fredrik Mellbin and contributors | LGPL-2.1-or-later |
| QTGMC, in [havsfunc](https://github.com/HomeOfVapourSynthEvolution/havsfunc) | The deinterlacer itself | QTGMC by -Vit- (after TempGaussMC by Didée), ported by HolyWu and the HomeOfVapourSynthEvolution contributors | Unlicense (parts WTFPL) |
| [mvsfunc](https://github.com/HomeOfVapourSynthEvolution/mvsfunc) | Helper functions havsfunc calls | mawen1250 | No licence file upstream; used as published |
| [vapoursynth-adjust](https://github.com/dubhatervapoursynth/vapoursynth-adjust) | Colour tweak helper havsfunc calls | dubhater | WTFPL |
| [MVTools](https://github.com/dubhater/vapoursynth-mvtools) | Motion estimation | Manao, Fizick, and dubhater (VapourSynth port) | GPL-2.0 |
| [znedi3](https://github.com/sekrit-twc/znedi3) and the [nnedi3 weights](https://github.com/dubhatervapoursynth/vapoursynth-nnedi3) | Edge-directed interpolation | sekrit-twc; nnedi3 by tritical, VapourSynth port by dubhater | GPL-2.0 |
| [EEDI3](https://github.com/HomeOfVapourSynthEvolution/VapourSynth-EEDI3) | Edge-directed interpolation | tritical; VapourSynth port by HolyWu | GPL-2.0 |
| [FFT3DFilter](https://github.com/myrsloik/VapourSynth-FFT3DFilter) | Frequency-domain denoising | A.G. Balakhnin (Fizick), Yusuke Nakamura | GPL-2.0 |
| [FFTW](https://www.fftw.org) | Fourier transforms for FFT3DFilter | Matteo Frigo and Steven G. Johnson | GPL-2.0-or-later |
| [fmtconv](https://gitlab.com/EleonoreMizo/fmtconv) | Format and bit-depth conversion | Laurent de Soras (EleonoreMizo) | WTFPL |
| [RemoveGrain](https://github.com/vapoursynth/vs-removegrain) and [MiscFilters](https://github.com/vapoursynth/vs-miscfilters-obsolete) | Spatial filters QTGMC uses | Fredrik Mellbin and the VapourSynth contributors (after Kassandro's RemoveGrain) | LGPL-2.1 (MiscFilters); no licence file in the RemoveGrain repository |
| [BestSource](https://github.com/vapoursynth/bestsource) | Frame-accurate source reader | Fredrik Mellbin | MIT |
| [libp2p](https://github.com/sekrit-twc/libp2p) | Pixel packing, used by BestSource | sekrit-twc | WTFPL |
| [vs-plugin-build](https://github.com/Stefan-Olt/vs-plugin-build) | The Linux builds of the plugins above | Stefan-Olt | LGPL-2.1 (build scripts); plugins keep their own licences |
| [Python](https://www.python.org) (embeddable, Windows) | Runs VapourSynth's scripts on Windows | Python Software Foundation | PSF-2.0 |

## Links, downloads and torrents

| Component | What it does in Atlas Player | Made by | Licence |
|---|---|---|---|
| [yt-dlp](https://github.com/yt-dlp/yt-dlp) | Plays and downloads videos from YouTube and other sites | The yt-dlp contributors (after youtube-dl) | Unlicense |
| [QuickJS-NG](https://github.com/quickjs-ng/quickjs) | The JavaScript runtime yt-dlp needs for YouTube | Fabrice Bellard, Charlie Gordon and the QuickJS-NG contributors | MIT |
| [librqbit](https://github.com/ikatson/rqbit) | Streams torrents while they download | Igor Katson | Apache-2.0 |
| Rust crates (listed at the end) | The torrent and MEGA helper (`native/atlas-torrent`) | Their authors | Mostly MIT and/or Apache-2.0; see the table |

## Interface

| Component | What it does in Atlas Player | Made by | Licence |
|---|---|---|---|
| [.NET](https://dotnet.microsoft.com) | Runtime and libraries | Microsoft and the .NET Foundation contributors | MIT |
| [Avalonia](https://avaloniaui.net) (with its Fluent theme, Skia and X11/Win32 backends) | The user interface | The Avalonia Team and contributors | MIT |
| [SkiaSharp](https://github.com/mono/SkiaSharp) and [HarfBuzzSharp](https://github.com/mono/SkiaSharp) | Drawing and text shaping, around Google's Skia and HarfBuzz | Microsoft; Skia by Google; HarfBuzz by its contributors | MIT (bindings); BSD-3-Clause (Skia); MIT (HarfBuzz) |
| [ANGLE](https://chromium.googlesource.com/angle/angle) (`av_libglesv2.dll`, Windows) | OpenGL ES for Avalonia's rendering on Windows | Google and the ANGLE contributors | BSD-3-Clause |
| [Tmds.DBus.Protocol](https://github.com/tmds/Tmds.DBus) and MicroCom.Runtime | Linux desktop integration and Windows COM, used by Avalonia | Tom Deseyn; the MicroCom authors | MIT |
| [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) | The app's view models | .NET Foundation and contributors | MIT |
| [Inter](https://rsms.me/inter/) | The interface font | Rasmus Andersson | OFL-1.1 |
| [Black Ops One](https://fonts.google.com/specimen/Black+Ops+One), [Anton](https://fonts.google.com/specimen/Anton), [Rye](https://fonts.google.com/specimen/Rye), [Audiowide](https://fonts.google.com/specimen/Audiowide), [Creepster](https://fonts.google.com/specimen/Creepster) | Some themes' title fonts | The Black-Ops Project Authors; The Anton Project Authors; Sorkin Type Co; Brian J. Bonislawsky (Astigmatic); Font Diner, Inc. | OFL-1.1 (texts in `src/AtlasPlayer.App/Assets/Fonts`) |
| [Material Design Icons](https://pictogrammers.com/library/mdi/) | The icons on buttons and menus | Pictogrammers | Apache-2.0 |

The app's icon (a strongman holding up a planet, in a gold coin) was drawn for Atlas Player as
an SVG, and is under the app's own licence.

## Services and data

These are free services run by communities and companies who let others use them. Atlas Player
only contacts them when you rip a disc or open a link.

| Service | What Atlas Player asks it | Run by |
|---|---|---|
| [MusicBrainz](https://musicbrainz.org) | A CD's album, artist and track names, from its disc ID | MetaBrainz Foundation and its community of editors (core data CC0) |
| [Cover Art Archive](https://coverartarchive.org) | The album cover for a ripped CD | MetaBrainz Foundation with the Internet Archive |
| [AccurateRip](http://www.accuraterip.com) | Whether a rip matches other people's rips of the same disc | Illustrate (Spoon), makers of dBpoweramp |
| YouTube and other video sites | Videos you choose to play or download, through yt-dlp | Their owners |
| [MEGA](https://mega.nz) | Files you choose to play from MEGA links | MEGA Limited |

The way AccurateRip's disc IDs are computed follows [CUETools](http://cue.tools)' open-source
implementation (Grigory Chudov). The FreeDB/CDDB disc ID follows the long-documented FreeDB
algorithm. Rip logs follow the layout of Exact Audio Copy's (by Andre Wiethoff), so they are
easy to read for anyone used to it. They say they are Atlas Player's, and never pass themselves
off as EAC, XLD or whipper logs.

## Building and testing

| Tool | Used for | Made by | Licence |
|---|---|---|---|
| [Freedesktop SDK](https://freedesktop-sdk.io) (org.freedesktop.Platform 25.08 and its .NET and Rust extensions) | The Flatpak's base, with dav1d, OpenSSL, Mesa, PipeWire and many more | The Freedesktop SDK contributors | Each component's own (mostly MIT, LGPL, GPL) |
| [Flatpak](https://flatpak.org) and flatpak-builder | Packaging for Linux | The Flatpak contributors | LGPL-2.1-or-later |
| [Meson](https://mesonbuild.com) | Builds BestSource and libplacebo | Jussi Pakkanen and contributors | Apache-2.0 |
| [glad](https://github.com/Dav1dde/glad), [fast_float](https://github.com/fastfloat/fast_float), [Vulkan-Headers](https://github.com/KhronosGroup/Vulkan-Headers), [Jinja](https://jinja.palletsprojects.com), [MarkupSafe](https://markupsafe.palletsprojects.com) | Built into or used while building libplacebo | David Herberth; Daniel Lemire and contributors; The Khronos Group; Pallets | MIT; Apache-2.0/MIT/BSL-1.0; Apache-2.0/MIT; BSD-3-Clause; BSD-3-Clause |
| [Rust](https://www.rust-lang.org) and Cargo | Builds the torrent helper | The Rust Project | MIT/Apache-2.0 |
| [xUnit.net](https://xunit.net) (xunit, xunit.runner.visualstudio), Microsoft.NET.Test.Sdk, [GitHubActionsTestLogger](https://github.com/Tyrrrz/GitHubActionsTestLogger), Avalonia.Headless.XUnit | Tests | The xUnit.net team; Microsoft; Oleksii Holub (Tyrrrz); the Avalonia Team | Apache-2.0; MIT; MIT; MIT |
| [ImageMagick](https://imagemagick.org) | Renders the icon's PNG and ICO | ImageMagick Studio LLC | ImageMagick License |
| [7-Zip](https://www.7-zip.org) | Packs the Windows zip | Igor Pavlov | LGPL-2.1-or-later (with unRAR restriction) |
| [Wine](https://www.winehq.org) and [Podman](https://podman.io) | Trying Windows builds and other Linux distributions | The Wine and Podman contributors | LGPL-2.1-or-later; Apache-2.0 |
| [GitHub Actions](https://github.com/features/actions) | Builds and tests every change, and makes the releases | GitHub | Service |

Atlas Player was developed with the help of [Claude Code](https://claude.com/claude-code)
(Anthropic), an AI coding assistant. Commits it helped write carry a `Co-Authored-By` line.

## Pictures

The screenshots in the README show [Big Buck Bunny](https://peach.blender.org),
© Blender Foundation, licensed CC BY 3.0.

## Rust crates in the torrent helper

Every crate in `native/atlas-torrent/Cargo.lock`, with the licence and authors it declares.
Regenerate the table with `python3 build/rust-credits.py`.

<!-- RUST-CRATES:START -->
| Crate | Version | Licence | Authors |
|---|---|---|---|
| [adler2](https://github.com/oyvindln/adler2) | 2.0.1 | 0BSD OR MIT OR Apache-2.0 | Jonas Schievink, oyvindln |
| [aes](https://github.com/RustCrypto/block-ciphers) | 0.8.4 | MIT OR Apache-2.0 | RustCrypto Developers |
| [aho-corasick](https://github.com/BurntSushi/aho-corasick) | 1.1.5 | Unlicense OR MIT | Andrew Gallant |
| [allocator-api2](https://github.com/zakarumych/allocator-api2) | 0.2.21 | MIT OR Apache-2.0 | Zakarum |
| [android_system_properties](https://github.com/nical/android_system_properties) | 0.1.6 | MIT OR Apache-2.0 | Nicolas Silva |
| [anyhow](https://github.com/dtolnay/anyhow) | 1.0.104 | MIT OR Apache-2.0 | David Tolnay |
| [arc-swap](https://github.com/vorner/arc-swap) | 1.9.3 | MIT OR Apache-2.0 | Michal 'vorner' Vaner |
| [arrayvec](https://github.com/bluss/arrayvec) | 0.7.8 | MIT OR Apache-2.0 | bluss |
| [assert_cfg](https://github.com/rodrimati1992/assert_cfg/) | 0.1.0 | Zlib | rodrimati1992 |
| [async-compression](https://github.com/Nullus157/async-compression) | 0.4.51 | MIT OR Apache-2.0 | Wim Looman, Allen Bui |
| [async-stream](https://github.com/tokio-rs/async-stream) | 0.3.6 | MIT | Carl Lerche |
| [async-stream-impl](https://github.com/tokio-rs/async-stream) | 0.3.6 | MIT | Carl Lerche |
| [async-trait](https://github.com/dtolnay/async-trait) | 0.1.92 | MIT OR Apache-2.0 | David Tolnay |
| [atoi](https://github.com/pacman82/atoi-rs) | 3.1.1 | MIT | Markus Klein |
| [atomic-waker](https://github.com/smol-rs/atomic-waker) | 1.1.2 | Apache-2.0 OR MIT | Stjepan Glavina, Contributors to futures-rs |
| [autocfg](https://github.com/cuviper/autocfg) | 1.5.1 | Apache-2.0 OR MIT | Josh Stone |
| [aws-lc-rs](https://github.com/aws/aws-lc-rs) | 1.18.2 | ISC AND (Apache-2.0 OR ISC) | AWS-LibCrypto |
| [aws-lc-sys](https://github.com/aws/aws-lc-rs) | 0.46.0 | ISC AND (Apache-2.0 OR ISC) AND Apache-2.0 AND MIT AND BSD-3-Clause AND (Apache-2.0 OR ISC OR MIT) AND (Apache-2.0 OR ISC OR MIT-0) | AWS-LC |
| [axum](https://github.com/tokio-rs/axum) | 0.8.9 | MIT | its contributors |
| [axum-core](https://github.com/tokio-rs/axum) | 0.5.6 | MIT | its contributors |
| [axum-extra](https://github.com/tokio-rs/axum) | 0.12.6 | MIT | its contributors |
| [backon](https://github.com/Xuanwo/backon) | 1.6.0 | Apache-2.0 | its contributors |
| [base64](https://github.com/marshallpierce/rust-base64) | 0.22.1 | MIT OR Apache-2.0 | Marshall Pierce |
| [base64](https://github.com/marshallpierce/rust-base64) | 0.23.1 | MIT OR Apache-2.0 | Marshall Pierce |
| [bitflags](https://github.com/bitflags/bitflags) | 1.3.2 | MIT/Apache-2.0 | The Rust Project Developers |
| [bitflags](https://github.com/bitflags/bitflags) | 2.13.2 | MIT OR Apache-2.0 | The Rust Project Developers |
| [bitvec](https://github.com/bitvecto-rs/bitvec) | 1.1.1 | MIT | its contributors |
| [block-padding](https://github.com/RustCrypto/utils) | 0.3.3 | MIT OR Apache-2.0 | RustCrypto Developers |
| [bs58](https://github.com/Nullus157/bs58-rs) | 0.5.1 | MIT/Apache-2.0 | its contributors |
| [bstr](https://github.com/BurntSushi/bstr) | 1.13.1 | MIT OR Apache-2.0 | Andrew Gallant |
| [bumpalo](https://github.com/fitzgen/bumpalo) | 3.20.3 | MIT OR Apache-2.0 | Nick Fitzgerald |
| [byteorder](https://github.com/BurntSushi/byteorder) | 1.5.0 | Unlicense OR MIT | Andrew Gallant |
| [bytes](https://github.com/tokio-rs/bytes) | 1.12.1 | MIT | Carl Lerche, Sean McArthur |
| [cbc](https://github.com/RustCrypto/block-modes) | 0.1.2 | MIT OR Apache-2.0 | RustCrypto Developers |
| [cc](https://github.com/rust-lang/cc-rs) | 1.10.0 | MIT OR Apache-2.0 | its contributors |
| [cfg-if](https://github.com/rust-lang/cfg-if) | 1.0.5 | MIT OR Apache-2.0 | Alex Crichton |
| [cfg_aliases](https://github.com/katharostech/cfg_aliases) | 0.2.2 | MIT | Zicklag |
| [chacha20](https://github.com/RustCrypto/stream-ciphers) | 0.10.2 | MIT OR Apache-2.0 | RustCrypto Developers |
| [chardetng](https://github.com/hsivonen/chardetng) | 1.0.0 | Apache-2.0 OR MIT | Henri Sivonen |
| [chrono](https://github.com/chronotope/chrono) | 0.4.45 | MIT OR Apache-2.0 | its contributors |
| [cipher](https://github.com/RustCrypto/traits) | 0.4.4 | MIT OR Apache-2.0 | RustCrypto Developers |
| [cmake](https://github.com/rust-lang/cmake-rs) | 0.1.58 | MIT OR Apache-2.0 | Alex Crichton |
| [combine](https://github.com/Marwes/combine) | 4.6.9 | MIT | Markus Westerlind |
| [compression-codecs](https://github.com/Nullus157/async-compression) | 0.4.46 | MIT OR Apache-2.0 | Wim Looman, Allen Bui |
| [compression-core](https://github.com/Nullus157/async-compression) | 0.4.34 | MIT OR Apache-2.0 | Wim Looman, Allen Bui |
| [const_panic](https://github.com/rodrimati1992/const_panic/) | 0.2.17 | Zlib | rodrimati1992 |
| [core-foundation](https://github.com/servo/core-foundation-rs) | 0.10.1 | MIT OR Apache-2.0 | The Servo Project Developers |
| [core-foundation-sys](https://github.com/servo/core-foundation-rs) | 0.8.7 | MIT OR Apache-2.0 | The Servo Project Developers |
| [core_detect](https://github.com/thomcc/core_detect) | 1.0.0 | MIT/Apache-2.0 | Thom Chiovoloni |
| [cpufeatures](https://github.com/RustCrypto/utils) | 0.2.17 | MIT OR Apache-2.0 | RustCrypto Developers |
| [cpufeatures](https://github.com/RustCrypto/utils) | 0.3.1 | MIT OR Apache-2.0 | RustCrypto Developers |
| [crc32fast](https://github.com/srijs/rust-crc32fast) | 1.5.3 | MIT OR Apache-2.0 | Sam Rijs, Alex Crichton |
| [crossbeam-utils](https://github.com/crossbeam-rs/crossbeam) | 0.8.23 | MIT OR Apache-2.0 | its contributors |
| [crypto-common](https://github.com/RustCrypto/traits) | 0.1.7 | MIT OR Apache-2.0 | RustCrypto Developers |
| [ctr](https://github.com/RustCrypto/block-modes) | 0.9.2 | MIT OR Apache-2.0 | RustCrypto Developers |
| [darling](https://github.com/TedDriggs/darling) | 0.24.1 | MIT | Ted Driggs |
| [darling_core](https://github.com/TedDriggs/darling) | 0.24.1 | MIT | Ted Driggs |
| [darling_macro](https://github.com/TedDriggs/darling) | 0.24.1 | MIT | Ted Driggs |
| [dashmap](https://github.com/xacrimon/dashmap) | 6.2.1 | MIT | Acrimon |
| [data-encoding](https://github.com/ia0/data-encoding) | 2.11.1 | MIT | its contributors |
| [defmt](https://github.com/knurling-rs/defmt) | 1.2.0 | MIT OR Apache-2.0 | The Knurling-rs developers |
| [defmt-macros](https://github.com/knurling-rs/defmt) | 1.2.0 | MIT OR Apache-2.0 | The Knurling-rs developers |
| [defmt-parser](https://github.com/knurling-rs/defmt) | 1.1.0 | MIT OR Apache-2.0 | The Knurling-rs developers |
| [deranged](https://github.com/jhpratt/deranged) | 0.5.9 | MIT OR Apache-2.0 | Jacob Pratt |
| [directories](https://github.com/soc/directories-rs) | 6.0.0 | MIT OR Apache-2.0 | Simon Ochsenreither |
| [dirs-sys](https://github.com/dirs-dev/dirs-sys-rs) | 0.5.0 | MIT OR Apache-2.0 | Simon Ochsenreither |
| [displaydoc](https://github.com/yaahc/displaydoc) | 0.2.7 | MIT OR Apache-2.0 | Jane Lusby |
| [dontfrag](https://github.com/nicbn/dontfrag-rs) | 1.0.1 | MIT OR Apache-2.0 | N. Nattis |
| [dunce](https://gitlab.com/kornelski/dunce) | 1.0.5 | CC0-1.0 OR MIT-0 OR Apache-2.0 | Kornel |
| [dyn-clone](https://github.com/dtolnay/dyn-clone) | 1.0.20 | MIT OR Apache-2.0 | David Tolnay |
| [either](https://github.com/rayon-rs/either) | 1.19.0 | MIT OR Apache-2.0 | its contributors |
| [encoding_rs](https://github.com/hsivonen/encoding_rs) | 0.8.42 | (Apache-2.0 OR MIT) AND BSD-3-Clause | Henri Sivonen |
| [equivalent](https://github.com/indexmap-rs/equivalent) | 1.0.3 | Apache-2.0 OR MIT | its contributors |
| [errno](https://github.com/lambda-fairy/rust-errno) | 0.3.14 | MIT OR Apache-2.0 | Chris Wong, Dan Gohman |
| [fastrand](https://github.com/smol-rs/fastrand) | 2.5.0 | Apache-2.0 OR MIT | Stjepan Glavina |
| [find-msvc-tools](https://github.com/rust-lang/cc-rs) | 0.1.15 | MIT OR Apache-2.0 | its contributors |
| [flate2](https://github.com/rust-lang/flate2-rs) | 1.1.10 | MIT OR Apache-2.0 | Alex Crichton, Josh Triplett |
| [foldhash](https://github.com/orlp/foldhash) | 0.2.0 | Zlib | Orson Peters |
| [form_urlencoded](https://github.com/servo/rust-url) | 1.2.3 | MIT OR Apache-2.0 | The rust-url developers |
| [fs_extra](https://github.com/webdesus/fs_extra) | 1.3.0 | MIT | Denis Kurilenko |
| [funty](https://github.com/myrrlyn/funty) | 2.0.0 | MIT | myrrlyn |
| [futures](https://github.com/rust-lang/futures-rs) | 0.3.34 | MIT OR Apache-2.0 | its contributors |
| [futures-channel](https://github.com/rust-lang/futures-rs) | 0.3.34 | MIT OR Apache-2.0 | its contributors |
| [futures-core](https://github.com/rust-lang/futures-rs) | 0.3.34 | MIT OR Apache-2.0 | its contributors |
| [futures-executor](https://github.com/rust-lang/futures-rs) | 0.3.34 | MIT OR Apache-2.0 | its contributors |
| [futures-io](https://github.com/rust-lang/futures-rs) | 0.3.34 | MIT OR Apache-2.0 | its contributors |
| [futures-macro](https://github.com/rust-lang/futures-rs) | 0.3.34 | MIT OR Apache-2.0 | its contributors |
| [futures-sink](https://github.com/rust-lang/futures-rs) | 0.3.34 | MIT OR Apache-2.0 | its contributors |
| [futures-task](https://github.com/rust-lang/futures-rs) | 0.3.34 | MIT OR Apache-2.0 | its contributors |
| [futures-timer](https://github.com/async-rs/futures-timer) | 3.0.4 | MIT/Apache-2.0 | Alex Crichton |
| [futures-util](https://github.com/rust-lang/futures-rs) | 0.3.34 | MIT OR Apache-2.0 | its contributors |
| [generic-array](https://github.com/fizyk20/generic-array.git) | 0.12.4 | MIT | Bartłomiej Kamiński, Aaron Trent |
| [generic-array](https://github.com/fizyk20/generic-array.git) | 0.14.7 | MIT | Bartłomiej Kamiński, Aaron Trent |
| [getrandom](https://github.com/rust-random/getrandom) | 0.2.17 | MIT OR Apache-2.0 | The Rand Project Developers |
| [getrandom](https://github.com/rust-random/getrandom) | 0.3.4 | MIT OR Apache-2.0 | The Rand Project Developers |
| [getrandom](https://github.com/rust-random/getrandom) | 0.4.3 | MIT OR Apache-2.0 | The Rand Project Developers |
| [gloo-timers](https://github.com/rustwasm/gloo/tree/master/crates/timers) | 0.3.0 | MIT OR Apache-2.0 | Rust and WebAssembly Working Group |
| [governor](https://github.com/boinkor-net/governor.git) | 0.10.4 | MIT | Andreas Fuchs |
| [hashbrown](https://github.com/rust-lang/hashbrown) | 0.12.3 | MIT OR Apache-2.0 | Amanieu d'Antras |
| [hashbrown](https://github.com/rust-lang/hashbrown) | 0.14.5 | MIT OR Apache-2.0 | Amanieu d'Antras |
| [hashbrown](https://github.com/rust-lang/hashbrown) | 0.16.1 | MIT OR Apache-2.0 | Amanieu d'Antras |
| [hashbrown](https://github.com/rust-lang/hashbrown) | 0.17.1 | MIT OR Apache-2.0 | its contributors |
| [hex](https://github.com/KokaKiwi/rust-hex) | 0.4.3 | MIT OR Apache-2.0 | KokaKiwi |
| [http](https://github.com/hyperium/http) | 1.5.0 | MIT OR Apache-2.0 | Alex Crichton, Carl Lerche, Sean McArthur |
| [http-body](https://github.com/hyperium/http-body) | 1.1.0 | MIT | Carl Lerche, Lucio Franco, Sean McArthur |
| [http-body-util](https://github.com/hyperium/http-body) | 0.1.5 | MIT | Carl Lerche, Lucio Franco, Sean McArthur |
| [httparse](https://github.com/seanmonstar/httparse) | 1.10.1 | MIT OR Apache-2.0 | Sean McArthur |
| [httpdate](https://github.com/pyfisch/httpdate) | 1.0.3 | MIT OR Apache-2.0 | Pyfisch |
| [hyper](https://github.com/hyperium/hyper) | 1.12.0 | MIT | Sean McArthur |
| [hyper-rustls](https://github.com/rustls/hyper-rustls) | 0.27.10 | Apache-2.0 OR ISC OR MIT | its contributors |
| [hyper-util](https://github.com/hyperium/hyper-util) | 0.1.21 | MIT | Sean McArthur |
| [iana-time-zone](https://github.com/strawlab/iana-time-zone) | 0.1.65 | MIT OR Apache-2.0 | Andrew Straw, René Kijewski, Ryan Lopopolo |
| [iana-time-zone-haiku](https://github.com/strawlab/iana-time-zone) | 0.1.2 | MIT OR Apache-2.0 | René Kijewski |
| [icu_collections](https://github.com/unicode-org/icu4x) | 2.4.0 | Unicode-3.0 | The ICU4X Project Developers |
| [icu_locale_core](https://github.com/unicode-org/icu4x) | 2.4.0 | Unicode-3.0 | The ICU4X Project Developers |
| [icu_normalizer](https://github.com/unicode-org/icu4x) | 2.4.0 | Unicode-3.0 | The ICU4X Project Developers |
| [icu_normalizer_data](https://github.com/unicode-org/icu4x) | 2.4.0 | Unicode-3.0 | The ICU4X Project Developers |
| [icu_properties](https://github.com/unicode-org/icu4x) | 2.4.0 | Unicode-3.0 | The ICU4X Project Developers |
| [icu_properties_data](https://github.com/unicode-org/icu4x) | 2.4.0 | Unicode-3.0 | The ICU4X Project Developers |
| [icu_provider](https://github.com/unicode-org/icu4x) | 2.4.0 | Unicode-3.0 | The ICU4X Project Developers |
| [ident_case](https://github.com/TedDriggs/ident_case) | 1.0.1 | MIT/Apache-2.0 | Ted Driggs |
| [idna](https://github.com/servo/rust-url/) | 1.1.0 | MIT OR Apache-2.0 | The rust-url developers |
| [idna_adapter](https://github.com/hsivonen/idna_adapter) | 1.2.2 | Apache-2.0 OR MIT | The rust-url developers |
| [indexmap](https://github.com/bluss/indexmap) | 1.9.3 | Apache-2.0 OR MIT | its contributors |
| [indexmap](https://github.com/indexmap-rs/indexmap) | 2.14.2 | Apache-2.0 OR MIT | its contributors |
| [inout](https://github.com/RustCrypto/utils) | 0.1.4 | MIT OR Apache-2.0 | RustCrypto Developers |
| [intervaltree](https://github.com/main--/rust-intervaltree) | 0.2.7 | MIT | main() |
| [ipnet](https://github.com/krisprice/ipnet) | 2.12.2 | MIT OR Apache-2.0 | Kris Price |
| [itertools](https://github.com/rust-itertools/itertools) | 0.15.0 | MIT OR Apache-2.0 | bluss |
| [itoa](https://github.com/dtolnay/itoa) | 1.0.18 | MIT OR Apache-2.0 | David Tolnay |
| [jiff](https://github.com/BurntSushi/jiff) | 0.2.38 | Unlicense OR MIT | Andrew Gallant |
| [jiff-core](https://github.com/BurntSushi/jiff) | 0.1.1 | Unlicense OR MIT | Andrew Gallant |
| [jiff-static](https://github.com/BurntSushi/jiff) | 0.2.38 | Unlicense OR MIT | Andrew Gallant |
| [jiff-tzdb](https://github.com/BurntSushi/jiff) | 0.1.9 | Unlicense OR MIT | Andrew Gallant |
| [jiff-tzdb-platform](https://github.com/BurntSushi/jiff) | 0.1.3 | Unlicense OR MIT | Andrew Gallant |
| [jni](https://github.com/jni-rs/jni-rs) | 0.22.4 | MIT OR Apache-2.0 | jni team |
| [jni-macros](https://github.com/jni-rs/jni-rs) | 0.22.4 | MIT OR Apache-2.0 | its contributors |
| [jni-sys](https://github.com/jni-rs/jni-sys) | 0.4.1 | MIT OR Apache-2.0 | Steven Fackler, Robert Bragg |
| [jni-sys-macros](https://github.com/jni-rs/jni-sys) | 0.4.1 | MIT OR Apache-2.0 | Robert Bragg |
| [jobserver](https://github.com/rust-lang/jobserver-rs) | 0.1.35 | MIT OR Apache-2.0 | Alex Crichton |
| [js-sys](https://github.com/wasm-bindgen/wasm-bindgen/tree/master/crates/js-sys) | 0.3.106 | MIT OR Apache-2.0 | The wasm-bindgen Developers |
| [lazy_static](https://github.com/rust-lang-nursery/lazy-static.rs) | 1.5.1 | MIT OR Apache-2.0 | Marvin Löbel |
| [leaky-bucket](https://github.com/udoprog/leaky-bucket) | 1.1.2 | MIT OR Apache-2.0 | John-John Tedro |
| [libc](https://github.com/rust-lang/libc) | 0.2.191 | MIT OR Apache-2.0 | its contributors |
| [libredox](https://gitlab.redox-os.org/redox-os/libredox.git) | 0.1.25 | MIT | 4lDO2 |
| [librqbit-bencode](https://github.com/ikatson/rqbit) | 9.0.1 | Apache-2.0 | its contributors |
| librqbit-buffers | 9.0.1 | Apache-2.0 | its contributors |
| librqbit-clone-to-owned | 9.0.1 | Apache-2.0 | its contributors |
| [librqbit-core](https://github.com/ikatson/rqbit) | 9.0.1 | Apache-2.0 | its contributors |
| [librqbit-dht](https://github.com/ikatson/rqbit) | 9.0.1 | Apache-2.0 | its contributors |
| [librqbit-dualstack-sockets](https://github.com/ikatson/librqbit-dualstack-sockets) | 0.7.0 | Apache-2.0 | its contributors |
| [librqbit-lsd](https://github.com/ikatson/rqbit) | 9.0.1 | Apache-2.0 | its contributors |
| [librqbit-peer-protocol](https://github.com/ikatson/rqbit) | 9.0.1 | Apache-2.0 | its contributors |
| [librqbit-sha1-wrapper](https://github.com/ikatson/rqbit) | 9.0.1 | Apache-2.0 | its contributors |
| [librqbit-tracker-comms](https://github.com/ikatson/rqbit) | 9.0.1 | Apache-2.0 | its contributors |
| [librqbit-upnp](https://github.com/ikatson/rqbit) | 9.0.1 | Apache-2.0 | Igor Katson |
| [librqbit-utp](https://github.com/ikatson/librqbit-utp) | 0.7.0 | Apache-2.0 | its contributors |
| [linux-raw-sys](https://github.com/sunfishcode/linux-raw-sys) | 0.12.1 | Apache-2.0 WITH LLVM-exception OR Apache-2.0 OR MIT | Dan Gohman |
| [litemap](https://github.com/unicode-org/icu4x) | 0.8.3 | Unicode-3.0 | The ICU4X Project Developers |
| [lock_api](https://github.com/Amanieu/parking_lot) | 0.4.14 | MIT OR Apache-2.0 | Amanieu d'Antras |
| [log](https://github.com/rust-lang/log) | 0.4.34 | MIT OR Apache-2.0 | The Rust Project Developers |
| [lru-slab](https://github.com/Ralith/lru-slab) | 0.1.3 | MIT OR Apache-2.0 OR Zlib | Benjamin Saunders |
| [matchit](https://github.com/ibraheemdev/matchit) | 0.8.4 | MIT AND BSD-3-Clause | Ibraheem Ahmed |
| [memchr](https://github.com/BurntSushi/memchr) | 2.8.3 | Unlicense OR MIT | Andrew Gallant, bluss |
| [memmap2](https://github.com/RazrFalcon/memmap2-rs) | 0.9.12 | MIT OR Apache-2.0 | Dan Burkert, Yevhenii Reizner, The Contributors |
| [metrics](https://github.com/metrics-rs/metrics) | 0.24.6 | MIT | Toby Lawrence |
| [mime](https://github.com/hyperium/mime) | 0.3.17 | MIT OR Apache-2.0 | Sean McArthur |
| [mime_guess](https://github.com/abonander/mime_guess) | 2.0.5 | MIT | Austin Bonander |
| [miniz_oxide](https://github.com/Frommi/miniz_oxide/tree/master/miniz_oxide) | 0.9.1 | MIT OR Zlib OR Apache-2.0 | Frommi, oyvindln, Rich Geldreich richgel99@gmail.com |
| [mio](https://github.com/tokio-rs/mio) | 1.2.4 | MIT | Carl Lerche, Thomas de Zeeuw, Tokio Contributors |
| [multiversion_no_op](https://github.com/hsivonen/multiversion_no_op) | 1.0.0 | Apache-2.0 OR MIT | Henri Sivonen |
| [network-interface](https://github.com/EstebanBorai/network-interface) | 2.0.5 | MIT OR Apache-2.0 | Esteban Borai |
| [nix](https://github.com/nix-rust/nix) | 0.30.1 | MIT | The nix-rust Project Developers |
| [nonzero_ext](https://github.com/antifuchs/nonzero_ext) | 0.3.0 | Apache-2.0 | Andreas Fuchs |
| [num](https://github.com/rust-num/num) | 0.2.1 | MIT/Apache-2.0 | The Rust Project Developers |
| [num-complex](https://github.com/rust-num/num-complex) | 0.2.4 | MIT/Apache-2.0 | The Rust Project Developers |
| [num-conv](https://github.com/jhpratt/num-conv) | 0.2.2 | MIT OR Apache-2.0 | Jacob Pratt |
| [num-integer](https://github.com/rust-num/num-integer) | 0.1.47 | MIT OR Apache-2.0 | The Rust Project Developers |
| [num-iter](https://github.com/rust-num/num-iter) | 0.1.46 | MIT OR Apache-2.0 | The Rust Project Developers |
| [num-rational](https://github.com/rust-num/num-rational) | 0.2.4 | MIT/Apache-2.0 | The Rust Project Developers |
| [num-traits](https://github.com/rust-num/num-traits) | 0.2.19 | MIT OR Apache-2.0 | The Rust Project Developers |
| [once_cell](https://github.com/matklad/once_cell) | 1.21.4 | MIT OR Apache-2.0 | Aleksey Kladov |
| [openssl-probe](https://github.com/rustls/openssl-probe) | 0.2.1 | MIT OR Apache-2.0 | Alex Crichton |
| [option-ext](https://github.com/soc/option-ext.git) | 0.2.0 | MPL-2.0 | Simon Ochsenreither |
| [parking_lot](https://github.com/Amanieu/parking_lot) | 0.12.5 | MIT OR Apache-2.0 | Amanieu d'Antras |
| [parking_lot_core](https://github.com/Amanieu/parking_lot) | 0.9.12 | MIT OR Apache-2.0 | Amanieu d'Antras |
| [percent-encoding](https://github.com/servo/rust-url/) | 2.3.2 | MIT OR Apache-2.0 | The rust-url developers |
| [pin-project-lite](https://github.com/taiki-e/pin-project-lite) | 0.2.17 | Apache-2.0 OR MIT | its contributors |
| [pkg-config](https://github.com/rust-lang/pkg-config-rs) | 0.3.34 | MIT OR Apache-2.0 | Alex Crichton |
| [portable-atomic](https://github.com/taiki-e/portable-atomic) | 1.15.0 | Apache-2.0 OR MIT | its contributors |
| [portable-atomic-util](https://github.com/taiki-e/portable-atomic-util) | 0.2.8 | Apache-2.0 OR MIT | its contributors |
| [potential_utf](https://github.com/unicode-org/icu4x) | 0.1.6 | Unicode-3.0 | The ICU4X Project Developers |
| [powerfmt](https://github.com/jhpratt/powerfmt) | 0.2.1 | MIT OR Apache-2.0 | Jacob Pratt |
| [ppv-lite86](https://github.com/cryptocorrosion/cryptocorrosion) | 0.2.21 | MIT OR Apache-2.0 | The CryptoCorrosion Contributors |
| [proc-macro2](https://github.com/dtolnay/proc-macro2) | 1.0.107 | MIT OR Apache-2.0 | David Tolnay, Alex Crichton |
| [quanta](https://github.com/metrics-rs/quanta) | 0.12.6 | MIT | Toby Lawrence |
| [quick-xml](https://github.com/tafia/quick-xml) | 0.41.0 | MIT | its contributors |
| [quinn](https://github.com/quinn-rs/quinn) | 0.11.12 | MIT OR Apache-2.0 | its contributors |
| [quinn-proto](https://github.com/quinn-rs/quinn) | 0.11.19 | MIT OR Apache-2.0 | its contributors |
| [quinn-udp](https://github.com/quinn-rs/quinn) | 0.5.16 | MIT OR Apache-2.0 | its contributors |
| [quote](https://github.com/dtolnay/quote) | 1.0.47 | MIT OR Apache-2.0 | David Tolnay |
| [r-efi](https://github.com/r-efi/r-efi) | 5.3.0 | MIT OR Apache-2.0 OR LGPL-2.1-or-later | its contributors |
| [r-efi](https://github.com/r-efi/r-efi) | 6.0.0 | MIT OR Apache-2.0 OR LGPL-2.1-or-later | its contributors |
| [radium](https://github.com/bitvecto-rs/radium) | 0.7.0 | MIT | Nika Layzell, myrrlyn |
| [rand](https://github.com/rust-random/rand) | 0.10.3 | MIT OR Apache-2.0 | The Rand Project Developers, The Rust Project Developers |
| [rand](https://github.com/rust-random/rand) | 0.9.5 | MIT OR Apache-2.0 | The Rand Project Developers, The Rust Project Developers |
| [rand_chacha](https://github.com/rust-random/rand) | 0.9.0 | MIT OR Apache-2.0 | The Rand Project Developers, The Rust Project Developers, The CryptoCorrosion Contributors |
| [rand_core](https://github.com/rust-random/rand_core) | 0.10.1 | MIT OR Apache-2.0 | The Rand Project Developers |
| [rand_core](https://github.com/rust-random/rand) | 0.9.5 | MIT OR Apache-2.0 | The Rand Project Developers, The Rust Project Developers |
| [rand_pcg](https://github.com/rust-random/rngs) | 0.10.2 | MIT OR Apache-2.0 | The Rand Project Developers |
| [rapidhash](https://github.com/hoxxep/rapidhash) | 4.5.1 | MIT OR Apache-2.0 | Liam Gray |
| [raw-cpuid](https://github.com/gz/rust-cpuid) | 11.6.0 | MIT | Gerd Zellweger |
| [redox_syscall](https://gitlab.redox-os.org/redox-os/syscall) | 0.5.18 | MIT | Jeremy Soller |
| [redox_users](https://gitlab.redox-os.org/redox-os/users) | 0.5.3 | MIT | Jose Narvaez, Wesley Hershberger |
| [ref-cast](https://github.com/dtolnay/ref-cast) | 1.0.27 | MIT OR Apache-2.0 | David Tolnay |
| [ref-cast-impl](https://github.com/dtolnay/ref-cast) | 1.0.27 | MIT OR Apache-2.0 | David Tolnay |
| [regex](https://github.com/rust-lang/regex) | 1.13.1 | MIT OR Apache-2.0 | The Rust Project Developers, Andrew Gallant |
| [regex-automata](https://github.com/rust-lang/regex) | 0.4.18 | MIT OR Apache-2.0 | The Rust Project Developers, Andrew Gallant |
| [regex-syntax](https://github.com/rust-lang/regex) | 0.8.11 | MIT OR Apache-2.0 | The Rust Project Developers, Andrew Gallant |
| [reqwest](https://github.com/seanmonstar/reqwest) | 0.12.28 | MIT OR Apache-2.0 | Sean McArthur |
| [reqwest](https://github.com/seanmonstar/reqwest) | 0.13.5 | MIT OR Apache-2.0 | Sean McArthur |
| [ring](https://github.com/briansmith/ring) | 0.17.14 | Apache-2.0 AND ISC | its contributors |
| [ringbuf](https://github.com/agerasev/ringbuf.git) | 0.4.8 | MIT/Apache-2.0 | Alexey Gerasev |
| [rlimit](https://github.com/Nugine/rlimit/) | 0.11.0 | MIT | Nugine |
| [rustc-hash](https://github.com/rust-lang/rustc-hash) | 2.1.3 | Apache-2.0 OR MIT | The Rust Project Developers |
| [rustc_version](https://github.com/djc/rustc-version-rs) | 0.4.1 | MIT OR Apache-2.0 | its contributors |
| [rustix](https://github.com/bytecodealliance/rustix) | 1.1.6 | Apache-2.0 WITH LLVM-exception OR Apache-2.0 OR MIT | Dan Gohman, Jakub Konka |
| [rustls](https://github.com/rustls/rustls) | 0.23.46 | Apache-2.0 OR ISC OR MIT | its contributors |
| [rustls-native-certs](https://github.com/rustls/rustls-native-certs) | 0.8.5 | Apache-2.0 OR ISC OR MIT | its contributors |
| [rustls-pki-types](https://github.com/rustls/pki-types) | 1.15.1 | MIT OR Apache-2.0 | its contributors |
| [rustls-platform-verifier](https://github.com/rustls/rustls-platform-verifier) | 0.7.1 | MIT OR Apache-2.0 | its contributors |
| [rustls-platform-verifier-android](https://github.com/rustls/rustls-platform-verifier) | 0.2.0 | MIT OR Apache-2.0 | its contributors |
| [rustls-webpki](https://github.com/rustls/webpki) | 0.103.16 | ISC | its contributors |
| [rustversion](https://github.com/dtolnay/rustversion) | 1.0.23 | MIT OR Apache-2.0 | David Tolnay |
| [ryu](https://github.com/dtolnay/ryu) | 1.0.23 | Apache-2.0 OR BSL-1.0 | David Tolnay |
| [same-file](https://github.com/BurntSushi/same-file) | 1.0.6 | Unlicense/MIT | Andrew Gallant |
| [schannel](https://github.com/steffengy/schannel-rs) | 0.1.30 | MIT | Steven Fackler, Steffen Butzer |
| [schemars](https://github.com/GREsau/schemars) | 0.9.0 | MIT | Graham Esau |
| [schemars](https://github.com/GREsau/schemars) | 1.2.3 | MIT | Graham Esau |
| [scopeguard](https://github.com/bluss/scopeguard) | 1.2.0 | MIT OR Apache-2.0 | bluss |
| [security-framework](https://github.com/kornelski/rust-security-framework) | 3.7.0 | MIT OR Apache-2.0 | Steven Fackler, Kornel |
| [security-framework-sys](https://github.com/kornelski/rust-security-framework) | 2.17.0 | MIT OR Apache-2.0 | Steven Fackler, Kornel |
| [semver](https://github.com/dtolnay/semver) | 1.0.28 | MIT OR Apache-2.0 | David Tolnay |
| [serde](https://github.com/serde-rs/serde) | 1.0.229 | MIT OR Apache-2.0 | Erick Tryzelaar, David Tolnay |
| [serde_core](https://github.com/serde-rs/serde) | 1.0.229 | MIT OR Apache-2.0 | Erick Tryzelaar, David Tolnay |
| [serde_derive](https://github.com/serde-rs/serde) | 1.0.229 | MIT OR Apache-2.0 | Erick Tryzelaar, David Tolnay |
| [serde_html_form](https://github.com/jplatte/serde_html_form) | 0.2.8 | MIT | its contributors |
| [serde_json](https://github.com/serde-rs/json) | 1.0.154 | MIT OR Apache-2.0 | its contributors |
| [serde_path_to_error](https://github.com/dtolnay/path-to-error) | 0.1.20 | MIT OR Apache-2.0 | David Tolnay |
| [serde_urlencoded](https://github.com/nox/serde_urlencoded) | 0.7.1 | MIT/Apache-2.0 | Anthony Ramine |
| [serde_with](https://github.com/jonasbb/serde_with/) | 3.24.0 | MIT OR Apache-2.0 | Jonas Bushart, Marcin Kaźmierczak |
| [serde_with_macros](https://github.com/jonasbb/serde_with/) | 3.24.0 | MIT OR Apache-2.0 | Jonas Bushart |
| [shlex](https://github.com/comex/rust-shlex) | 2.0.1 | MIT OR Apache-2.0 | comex, Fenhl, Adrian Taylor, Alex Touchet, Daniel Parks, Garrett Berg |
| [signal-hook-registry](https://github.com/vorner/signal-hook) | 1.4.8 | MIT OR Apache-2.0 | Michal 'vorner' Vaner, Masaki Hara |
| [simd-adler32](https://github.com/mcountryman/simd-adler32) | 0.3.10 | MIT | Marvin Countryman |
| [simd_cesu8](https://github.com/seancroach/simd_cesu8) | 1.2.0 | Apache-2.0 OR MIT | Sean C. Roach |
| [simdutf8](https://github.com/rusticstuff/simdutf8) | 0.1.5 | MIT OR Apache-2.0 | Hans Kratz |
| [size_format](https://github.com/aticu/size_format) | 1.0.2 | MIT OR Apache-2.0 | aticu |
| [slab](https://github.com/tokio-rs/slab) | 0.4.12 | MIT | Carl Lerche |
| [smallvec](https://github.com/servo/rust-smallvec) | 1.16.4 | MIT OR Apache-2.0 | The Servo Project Developers |
| [socket2](https://github.com/rust-lang/socket2) | 0.6.5 | MIT OR Apache-2.0 | Alex Crichton, Thomas de Zeeuw |
| [spinning_top](https://github.com/rust-osdev/spinning_top) | 0.3.0 | MIT/Apache-2.0 | Philipp Oppermann |
| [stable_deref_trait](https://github.com/storyyeller/stable_deref_trait) | 1.2.1 | MIT OR Apache-2.0 | Robert Grosse |
| [strsim](https://github.com/rapidfuzz/strsim-rs) | 0.11.1 | MIT | Danny Guo, maxbachmann |
| [subtle](https://github.com/dalek-cryptography/subtle) | 2.6.1 | BSD-3-Clause | Isis Lovecruft, Henry de Valence |
| [syn](https://github.com/dtolnay/syn) | 2.0.119 | MIT OR Apache-2.0 | David Tolnay |
| [syn](https://github.com/dtolnay/syn) | 3.0.9 | MIT OR Apache-2.0 | David Tolnay |
| [sync_wrapper](https://github.com/Actyx/sync_wrapper) | 1.0.2 | Apache-2.0 | Actyx AG |
| [synstructure](https://github.com/mystor/synstructure) | 0.14.0 | MIT | Nika Layzell |
| [tap](https://github.com/myrrlyn/tap) | 1.0.1 | MIT | Elliott Linder, myrrlyn |
| [tempfile](https://github.com/Stebalien/tempfile) | 3.27.0 | MIT OR Apache-2.0 | Steven Allen, The Rust Project Developers, Ashley Mannix, Jason White |
| [thiserror](https://github.com/dtolnay/thiserror) | 1.0.69 | MIT OR Apache-2.0 | David Tolnay |
| [thiserror](https://github.com/dtolnay/thiserror) | 2.0.21 | MIT OR Apache-2.0 | David Tolnay |
| [thiserror-impl](https://github.com/dtolnay/thiserror) | 1.0.69 | MIT OR Apache-2.0 | David Tolnay |
| [thiserror-impl](https://github.com/dtolnay/thiserror) | 2.0.21 | MIT OR Apache-2.0 | David Tolnay |
| [time](https://github.com/time-rs/time) | 0.3.55 | MIT OR Apache-2.0 | Jacob Pratt, Time contributors |
| [time-core](https://github.com/time-rs/time) | 0.1.9 | MIT OR Apache-2.0 | Jacob Pratt, Time contributors |
| [time-macros](https://github.com/time-rs/time) | 0.2.32 | MIT OR Apache-2.0 | Jacob Pratt, Time contributors |
| [tinystr](https://github.com/unicode-org/icu4x) | 0.8.5 | Unicode-3.0 | The ICU4X Project Developers |
| [tinyvec](https://github.com/Lokathor/tinyvec) | 1.13.3 | Zlib OR Apache-2.0 OR MIT | Lokathor |
| [tokio](https://github.com/tokio-rs/tokio) | 1.54.1 | MIT | Tokio Contributors |
| [tokio-macros](https://github.com/tokio-rs/tokio) | 2.7.2 | MIT | Tokio Contributors |
| [tokio-rustls](https://github.com/rustls/tokio-rustls) | 0.26.6 | MIT OR Apache-2.0 | its contributors |
| [tokio-socks](https://github.com/sticnarf/tokio-socks) | 0.5.3 | MIT | Yilin Chen |
| [tokio-stream](https://github.com/tokio-rs/tokio) | 0.1.19 | MIT | Tokio Contributors |
| [tokio-util](https://github.com/tokio-rs/tokio) | 0.7.20 | MIT | Tokio Contributors |
| [tower](https://github.com/tower-rs/tower) | 0.5.3 | MIT | Tower Maintainers |
| [tower-http](https://github.com/tower-rs/tower-http) | 0.6.11 | MIT | Tower Maintainers |
| [tower-layer](https://github.com/tower-rs/tower) | 0.3.3 | MIT | Tower Maintainers |
| [tower-service](https://github.com/tower-rs/tower) | 0.3.3 | MIT | Tower Maintainers |
| [tracing](https://github.com/tokio-rs/tracing) | 0.1.44 | MIT | Eliza Weisman, Tokio Contributors |
| [tracing-attributes](https://github.com/tokio-rs/tracing) | 0.1.31 | MIT | Tokio Contributors, Eliza Weisman, David Barsky |
| [tracing-core](https://github.com/tokio-rs/tracing) | 0.1.36 | MIT | Tokio Contributors |
| [try-lock](https://github.com/seanmonstar/try-lock) | 0.2.5 | MIT | Sean McArthur |
| [typenum](https://github.com/paholg/typenum) | 1.20.1 | MIT OR Apache-2.0 | its contributors |
| [typewit](https://github.com/rodrimati1992/typewit/) | 1.15.2 | Zlib | rodrimati1992 |
| [unicase](https://github.com/seanmonstar/unicase) | 2.10.0 | MIT OR Apache-2.0 | Sean McArthur |
| [unicode-ident](https://github.com/dtolnay/unicode-ident) | 1.0.27 | (MIT OR Apache-2.0) AND Unicode-3.0 | David Tolnay |
| [untrusted](https://github.com/briansmith/untrusted) | 0.7.1 | ISC | Brian Smith |
| [untrusted](https://github.com/briansmith/untrusted) | 0.9.0 | ISC | Brian Smith |
| [url](https://github.com/servo/rust-url) | 2.5.9 | MIT OR Apache-2.0 | The rust-url developers |
| [urlencoding](https://github.com/kornelski/rust_urlencoding) | 2.1.3 | MIT | Kornel, Bertram Truong |
| [utf8_iter](https://github.com/hsivonen/utf8_iter) | 1.0.4 | Apache-2.0 OR MIT | Henri Sivonen |
| [uuid](https://github.com/uuid-rs/uuid) | 1.29.0 | Apache-2.0 OR MIT | Ashley Mannix, Dylan DPC, Hunar Roop Kahlon |
| [version_check](https://github.com/SergioBenitez/version_check) | 0.9.5 | MIT/Apache-2.0 | Sergio Benitez |
| [walkdir](https://github.com/BurntSushi/walkdir) | 2.5.0 | Unlicense/MIT | Andrew Gallant |
| [want](https://github.com/seanmonstar/want) | 0.3.2 | MIT | Sean McArthur |
| [wasi](https://github.com/bytecodealliance/wasi) | 0.11.1+wasi-snapshot-preview1 | Apache-2.0 WITH LLVM-exception OR Apache-2.0 OR MIT | The Cranelift Project Developers |
| [wasip2](https://github.com/bytecodealliance/wasi-rs) | 1.0.4+wasi-0.2.12 | Apache-2.0 WITH LLVM-exception OR Apache-2.0 OR MIT | its contributors |
| [wasm-bindgen](https://github.com/wasm-bindgen/wasm-bindgen) | 0.2.129 | MIT OR Apache-2.0 | The wasm-bindgen Developers |
| [wasm-bindgen-futures](https://github.com/wasm-bindgen/wasm-bindgen/tree/master/crates/futures) | 0.4.79 | MIT OR Apache-2.0 | The wasm-bindgen Developers |
| [wasm-bindgen-macro](https://github.com/wasm-bindgen/wasm-bindgen/tree/master/crates/macro) | 0.2.129 | MIT OR Apache-2.0 | The wasm-bindgen Developers |
| [wasm-bindgen-macro-support](https://github.com/wasm-bindgen/wasm-bindgen/tree/main/crates/macro-support) | 0.2.129 | MIT OR Apache-2.0 | The wasm-bindgen Developers |
| [wasm-bindgen-shared](https://github.com/wasm-bindgen/wasm-bindgen/tree/master/crates/shared) | 0.2.129 | MIT OR Apache-2.0 | The wasm-bindgen Developers |
| [wasm-streams](https://github.com/MattiasBuelens/wasm-streams/) | 0.4.2 | MIT OR Apache-2.0 | Mattias Buelens |
| [wasm-streams](https://github.com/MattiasBuelens/wasm-streams/) | 0.5.0 | MIT OR Apache-2.0 | Mattias Buelens |
| [web-sys](https://github.com/wasm-bindgen/wasm-bindgen/tree/master/crates/web-sys) | 0.3.106 | MIT OR Apache-2.0 | The wasm-bindgen Developers |
| [web-time](https://github.com/daxpedda/web-time) | 1.1.0 | MIT OR Apache-2.0 | its contributors |
| [webpki-root-certs](https://github.com/rustls/webpki-roots) | 1.0.11 | CDLA-Permissive-2.0 | its contributors |
| [webpki-roots](https://github.com/rustls/webpki-roots) | 1.0.11 | CDLA-Permissive-2.0 | its contributors |
| [winapi](https://github.com/retep998/winapi-rs) | 0.3.9 | MIT/Apache-2.0 | Peter Atashian |
| [winapi-i686-pc-windows-gnu](https://github.com/retep998/winapi-rs) | 0.4.0 | MIT/Apache-2.0 | Peter Atashian |
| [winapi-util](https://github.com/BurntSushi/winapi-util) | 0.1.11 | Unlicense OR MIT | Andrew Gallant |
| [winapi-x86_64-pc-windows-gnu](https://github.com/retep998/winapi-rs) | 0.4.0 | MIT/Apache-2.0 | Peter Atashian |
| [windows](https://github.com/microsoft/windows-rs) | 0.62.2 | MIT OR Apache-2.0 | its contributors |
| [windows-collections](https://github.com/microsoft/windows-rs) | 0.3.2 | MIT OR Apache-2.0 | its contributors |
| [windows-core](https://github.com/microsoft/windows-rs) | 0.62.2 | MIT OR Apache-2.0 | its contributors |
| [windows-future](https://github.com/microsoft/windows-rs) | 0.3.2 | MIT OR Apache-2.0 | its contributors |
| [windows-implement](https://github.com/microsoft/windows-rs) | 0.60.2 | MIT OR Apache-2.0 | its contributors |
| [windows-interface](https://github.com/microsoft/windows-rs) | 0.59.3 | MIT OR Apache-2.0 | its contributors |
| [windows-link](https://github.com/microsoft/windows-rs) | 0.2.1 | MIT OR Apache-2.0 | its contributors |
| [windows-numerics](https://github.com/microsoft/windows-rs) | 0.3.1 | MIT OR Apache-2.0 | its contributors |
| [windows-result](https://github.com/microsoft/windows-rs) | 0.4.1 | MIT OR Apache-2.0 | its contributors |
| [windows-strings](https://github.com/microsoft/windows-rs) | 0.5.1 | MIT OR Apache-2.0 | its contributors |
| [windows-sys](https://github.com/microsoft/windows-rs) | 0.52.0 | MIT OR Apache-2.0 | Microsoft |
| [windows-sys](https://github.com/microsoft/windows-rs) | 0.61.2 | MIT OR Apache-2.0 | its contributors |
| [windows-targets](https://github.com/microsoft/windows-rs) | 0.52.6 | MIT OR Apache-2.0 | Microsoft |
| [windows-threading](https://github.com/microsoft/windows-rs) | 0.2.1 | MIT OR Apache-2.0 | its contributors |
| [windows_aarch64_gnullvm](https://github.com/microsoft/windows-rs) | 0.52.6 | MIT OR Apache-2.0 | Microsoft |
| [windows_aarch64_msvc](https://github.com/microsoft/windows-rs) | 0.52.6 | MIT OR Apache-2.0 | Microsoft |
| [windows_i686_gnu](https://github.com/microsoft/windows-rs) | 0.52.6 | MIT OR Apache-2.0 | Microsoft |
| [windows_i686_gnullvm](https://github.com/microsoft/windows-rs) | 0.52.6 | MIT OR Apache-2.0 | Microsoft |
| [windows_i686_msvc](https://github.com/microsoft/windows-rs) | 0.52.6 | MIT OR Apache-2.0 | Microsoft |
| [windows_x86_64_gnu](https://github.com/microsoft/windows-rs) | 0.52.6 | MIT OR Apache-2.0 | Microsoft |
| [windows_x86_64_gnullvm](https://github.com/microsoft/windows-rs) | 0.52.6 | MIT OR Apache-2.0 | Microsoft |
| [windows_x86_64_msvc](https://github.com/microsoft/windows-rs) | 0.52.6 | MIT OR Apache-2.0 | Microsoft |
| [wit-bindgen](https://github.com/bytecodealliance/wit-bindgen) | 0.57.1 | Apache-2.0 WITH LLVM-exception OR Apache-2.0 OR MIT | Alex Crichton |
| [writeable](https://github.com/unicode-org/icu4x) | 0.6.4 | Unicode-3.0 | The ICU4X Project Developers |
| [wyz](https://github.com/myrrlyn/wyz) | 0.5.1 | MIT | myrrlyn |
| [yoke](https://github.com/unicode-org/icu4x) | 0.8.4 | Unicode-3.0 | Manish Goregaokar |
| [yoke-derive](https://github.com/unicode-org/icu4x) | 0.8.4 | Unicode-3.0 | Manish Goregaokar |
| [zerocopy](https://github.com/google/zerocopy) | 0.8.63 | BSD-2-Clause OR Apache-2.0 OR MIT | its contributors |
| [zerocopy-derive](https://github.com/google/zerocopy) | 0.8.63 | BSD-2-Clause OR Apache-2.0 OR MIT | its contributors |
| [zerofrom](https://github.com/unicode-org/icu4x) | 0.1.8 | Unicode-3.0 | The ICU4X Project Developers |
| [zerofrom-derive](https://github.com/unicode-org/icu4x) | 0.1.8 | Unicode-3.0 | Manish Goregaokar |
| [zeroize](https://github.com/RustCrypto/utils) | 1.9.1 | Apache-2.0 OR MIT | The RustCrypto Project Developers |
| [zerotrie](https://github.com/unicode-org/icu4x) | 0.2.5 | Unicode-3.0 | The ICU4X Project Developers |
| [zerovec](https://github.com/unicode-org/icu4x) | 0.11.9 | Unicode-3.0 | The ICU4X Project Developers |
| [zerovec-derive](https://github.com/unicode-org/icu4x) | 0.11.7 | Unicode-3.0 | Manish Goregaokar |
| [zlib-rs](https://github.com/trifectatechfoundation/zlib-rs) | 0.6.8 | Zlib | its contributors |
| [zmij](https://github.com/dtolnay/zmij) | 1.0.23 | MIT | David Tolnay |
<!-- RUST-CRATES:END -->
