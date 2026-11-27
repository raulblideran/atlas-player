# Changelog

Versions follow `MAJOR.MINOR.PATCH`. The in-app version (welcome screen, Settings → About)
also shows the git commit a build came from, e.g. `1.1.0 (50919f7)`.

## 2.0.0-beta.13 — 2026-11-27

A **beta** of 2.0.0. CD rip folders play however much else is in them, with the album's cover
on screen, and Atlas Player now credits everyone whose work it is built on.

- **Volume normalization starts at -3 dB.** It is still off by default. When you switch on
  **Even out loud and quiet parts** (Settings → Audio, or the audio menu), the peaks are brought
  to 3 dB below full scale. Settings → Audio now has that level (-12 to 0 dB) right under the
  switch, to change it.
- **CD rip folders play,** even with their log, cue sheet, pictures and other files beside the
  music. A folder whose cue sheet named files that aren't there (one for the whole disc's image,
  next to a file per track, as in some rips) said "Can't play this: no audio or video data
  played". Now only the audio and video files in a folder play, in track order ("2" before
  "10"), and a cue sheet's tracks play as long as some of its files are there. A two-disc album's
  CD1 and CD2 folders play one after the other. Dropping all of a rip's files at once works the
  same way.
- **The album's cover shows while its music plays:** the folder's cover.jpg or front.jpg (or
  folder.jpg, a Front.jpg in a Covers or Scans folder, or the folder's only picture). It is shown
  instead of the small cover inside the files, and also for a cue sheet's tracks.
- UTF-16 cue sheets, as some EAC and foobar2000 setups write them, are read.
- **Credits for everyone whose work Atlas Player is built on.** The new CREDITS.md names every
  library, tool, font, icon set, service and Rust crate the app uses, with what it does, who made
  it and its licence. It ships next to the app, beside the app's own LICENSE, and the Flatpak also
  carries each project's licence text. Settings → About says the same in short, and that Atlas
  Player is a free, non-commercial project: no ads, no tracking, no accounts, no paid features.

## 2.0.0-beta.12 — 2026-11-27

A **beta** of 2.0.0. Mostly a fix for QTGMC, which beta.11 shipped broken: it now runs a
whole rip on Linux, and every preset is checked on Windows as the zip is built.

- **QTGMC works now, on Windows and Linux.** In 2.0.0-beta.11 it couldn't be picked on either:
  its MVTools was an old build that the bundled VapourSynth (R82) no longer loads, and four
  plugins it needs (fmtconv, MiscFilters, EEDI3m, FFT3DFilter) weren't included. Both builds now
  bring all of them, and every preset (Medium to Placebo) is tested when the app is built. If it
  still can't load, hovering over QTGMC in the rip window shows why.
- **One button for a disc folder or image.** Open Disc has **Disc folder/image…** and Rip Disc
  **DVD folder/image…**, in place of two buttons each; it asks which one you want.
- **"One file per title" rips the whole title.** The chapter ticks only apply to **One file per
  chapter**, so they're greyed out otherwise. With **One file per chapter**, each chapter you
  tick becomes its own file (all of them, to begin with). The whole title stays the default, for
  MKVs and audio only alike.
- **The Play button's triangle is centred,** rather than sitting a little to the left.

## 2.0.0-beta.11 — 2026-11-26

A **beta** of 2.0.0, and the first under its new name. Tried on Linux, not yet on Windows.
QTGMC is new and hasn't been through a full rip on either system yet: please report how it goes.

- **The app is now Atlas Player,** with a new icon (a strongman holding up a planet with a play
  symbol on it, in a gold coin) and a new default theme, **Atlas**: gold on deep navy, dark.
  **Atlas Light** is its light counterpart. The other themes are unchanged.
- **Linux:** the Flatpak's ID is now `io.github.raulblideran.AtlasPlayer`, so it installs as a
  new app. On its first start it copies your settings (and any `mpv.conf`, `input.conf` or
  shaders) over from the previous install; you can then uninstall the old one.
- **Windows:** the zip is `AtlasPlayer-<version>-portable-win-x64.zip` and the program
  `AtlasPlayer.exe`, in a folder named `Atlas Player`. To keep your settings, copy `settings.json`
  from the old folder into the new one.
- **Rip Disc is a step-by-step window.** It recognises the disc (an Audio CD, or a DVD from a
  drive, an `.iso` or a VIDEO_TS folder, as before), and each step shows only what applies to it.
  The steps are shown at the top; Back and Next move between them, and Rip is on the last.
- **An Audio CD's steps:** Source → Tracks (the tags and tracks, as before) → Format (FLAC level 8
  or WAV) → **Cue sheet** → Rip. The cue sheet is now a choice: ticked for a whole disc, not for a
  few tracks or a single one, and a few tracks can have one of their own. The log scores the same
  with or without one.
- **A DVD's steps:** Source → **Rip mode** → **Titles** → Format → Rip. The mode is **Full DVD**
  (MKVs), the new **Audio only**, or the **whole disc backup** (the VIDEO_TS folder, decrypted, as
  before; it skips straight to Rip).
- **Several titles in one rip.** Tick the titles you want; each becomes its own file. For each
  title: the chapters you tick, one file or one per chapter, as before.
- **Choose a title's audio tracks.** Each title lists its tracks as "English, AC-3, 5.1", all
  ticked; at least one has to be. In an MKV each track ticked is a stream of its own.
- **Audio only (new):** a DVD's sound as **FLAC** (level 8) or **WAV**: one file per title or one
  per chapter, and a file for each audio track ticked (FLAC and WAV hold one track each), in
  Music → the disc's name. AC-3 and DTS are decoded to 24-bit, LPCM keeps its bits. Where a track
  has gaps (stretches with no audio packets at all: The One's AC-3 lacks 68 s of chapter 2) they
  become silence, so a file is as long as its title or chapter and stays in time with it.
- **MKV audio:** **as on the disc** (AC-3 and DTS copied, LPCM as FLAC, as before), **FLAC** or
  **AAC** (160 kb/s a channel), for every track ticked.
- **Every channel is kept** (stereo, 5.1, 7.1) in FLAC, AAC and WAV. Nothing is mixed down unless
  you tick **Mix down to stereo** (for converted audio only).
- **QTGMC (new),** besides BWDIF, for x264 and x265 encodes of interlaced video: havsfunc's QTGMC
  through VapourSynth, which now comes with both builds for ripping only (playback keeps BWDIF).
  Its preset (Medium to Placebo, Slower by default) is chosen in the rip; the encoder settings are
  BWDIF's. On Windows it needs the VC++ runtime, which the zip brings along.
- **Frame rate:** a frame per field (50p, 59.94p for NTSC: the default, as before) or the disc's
  own rate (25p, 29.97p), with either deinterlacer.
- **In fullscreen the bars and the pointer hide after 2 seconds,** down from 2.5 (bars) and 3
  (pointer). In a window the bars still wait 2.5 s.
- The welcome screen says what **Open disc** (CD, DVD, BluRay) and **Rip disc** (CD, DVD, Audio
  from DVD) take. Two buffering lines trail off: "Bruney…", "Big fast, many speed…".
- Clearer descriptions in Open Disc, Open Link (now titled like the others) and Download Video,
  and on each step of Rip Disc.

## 2.0.0-beta.10 — 2026-11-26

- Describe this release

## 2.0.0-beta.10 — 2026-11-26

A **beta** of 2.0.0: the new look is finished and was tried on Linux, but not yet on Windows.
Please report anything that looks or works wrong there; 2.0.0 follows once it's been tried.

- **The dark themes' bars fade as they did in beta.7.** Beta.9's lighter fade washed out the
  themes' colours.
- **The light themes' title bar is shorter:** 56 px instead of 72. It's solid there, so it
  doesn't need the room a fade takes, and it covers less of the picture.

## 2.0.0-beta.9 — 2026-11-26

- Describe this release

## 2.0.0-beta.9 — 2026-11-26

A **beta** of 2.0.0: the new look is finished and was tried on Linux, but not yet on Windows.
Please report anything that looks or works wrong there; 2.0.0 follows once it's been tried.

- **The dark themes' bars fade into the picture again, much more lightly.** The fade is now 75%
  at the window's edge, down from 95%, and 40% halfway, down from 70%, so far less of the picture
  is dimmed. There's no line between the bar and the picture.
  The light themes (Atlas Light, MPC-HC and Miami Marina) keep beta.8's solid bars with a
  thin line, so there's no white haze over the video.

## 2.0.0-beta.8 — 2026-11-26

- Describe this release

## 2.0.0-beta.8 — 2026-11-26

A **beta** of 2.0.0: the new look is finished and was tried on Linux, but not yet on Windows.
Please report anything that looks or works wrong there; 2.0.0 follows once it's been tried.

- **Solid bars, in every theme.** The title bar and the controls bar no longer fade into the
  picture (a light theme left a white haze over the top and bottom of the video). Each is now a
  solid bar in its theme's colour with a thin line against the video, and still hides on its own.
- **MPC-HC looks more like Media Player Classic:** a plain black Play glyph instead of a filled
  button, Windows 7's light-blue boxes on hover, flat grey badges, a sunken grey seek track and a
  flat welcome screen.
- **New bar layout: MPC-HC.** Settings → Appearance → Controls bar → Layout → **MPC-HC**: play and
  stop, the skip and frame buttons on the left, the menus and the volume on the right, like Media
  Player Classic's toolbar. **Choosing the MPC-HC theme brings it along**; choosing another theme
  puts your previous bar back, unless you rearranged the bar in between (then your arrangement stays).
  It works with any theme too.
- **Fixed: Play's glyph ignored its theme's colour.** It took the body text's colour, so
  some themes drew a dark glyph on a dark button or a white one on a bright button. It now uses
  the theme's colour for text on its accent, which is checked for contrast.

## 2.0.0-beta.7 — 2026-11-25

A **beta** of 2.0.0: the new look is finished and was tried on Linux, but not yet on Windows.
Please report anything that looks or works wrong there; 2.0.0 follows once it's been tried.

- **Thirteen new themes** (Settings → Appearance), 24 in all:
  - **Graphite Red**: Graphite's greys with red instead of blue.
  - The looks of other players: **Apple Music** (near-black and pink-red, soft corners),
    **Spotify** (black and green, pill buttons), **Tidal** (black, white and cyan, sharp edges),
    **MPC-HC** (the classic light grey toolbars and Windows blue, square corners) and
    **Windows Media Player** (glossy black and glowing blue, a round play button). Colours and
    shapes only: no logos, and their own fonts are replaced by Inter.
  - Seven **gradient palettes**, in the welcome screen, the titles and the seek bar: **Vice City
    Sunset**, **Golden Hour Escape**, **Neon Nights**, **Tropical Vice**, **Miami Marina** (on
    light sand), **Vice City by Night** and **Midnight Pursuit**.
- **New: the interface font.** Settings → Appearance → Text → **Interface font**: any installed
  font, for every window, menu and bar at once. Titles follow it too, except in themes with a title
  font of their own (GTA 6, Synthwave…). **Default** goes back to Inter.
- **Audio files show their codec** in the codec badge: FLAC, ALAC, MP3, AAC, Opus, PCM, DSD,
  AC-3, TrueHD, DTS… Its tooltip says a little more, with the sample rate ("FLAC, lossless · 96 kHz").
- **Screenshots of interlaced video are the deinterlaced picture.** Both engines' screenshots come
  after the deinterlacer, but one that still combs (as reported on Windows) is now deinterlaced
  with FFmpeg as it's saved, still an uncompressed PNG at full size.
- **Fixed: screenshots of DVDs weren't saved on Linux.** The Flatpak's libVLC was built without
  the encoder its screenshots need: Atlas Player said "Screenshot saved" and nothing was. It's built
  in now, and a screenshot the disc player couldn't save says so.
- **Fixed: a second screenshot of the same paused moment replaced the first** (S, then Shift+S
  within the same second). The second is now "… (2).png".
- **Leaving fullscreen goes back to the window you had**: the same size in the same place, or
  maximized if it was. It always went to a maximized window before.
- **Fixed: More (⋯) still offered Download video…** with a download button on screen. It's left
  out while a web video plays (the top bar's button) and whenever the bar has its own **Download**
  button, and it's checked again each time More opens.
- **New buffering lines**, still with "Big fast, many speed" and "Bruney".

## 2.0.0-beta.6 — 2026-11-25

A **beta** of 2.0.0: the new look is finished and was tried on Linux, but not yet on Windows.
Please report anything that looks or works wrong there; 2.0.0 follows once it's been tried.

- **New: rip single tracks of an Audio CD.** Rip Disc lists the tracks with a tick box each (with
  **All** and **None**, and a summary such as "2 of 14 tracks · 9:41"). The whole disc is ripped as
  before. Tracks ripped on their own go in the album's folder, beside any ripped earlier, with a log
  of their own (it lists the tracks it covers) and no cue sheet, since a cue sheet of a few tracks
  doesn't describe the disc. Each track is still checked against its own AccurateRip entry, and the
  cover already in the folder is kept.
- **Fixed: a DVD's resolution badge read "0p".** libVLC says 0×0 for a moment as a DVD's picture
  starts (its menu, then each title); Atlas Player now waits for the real size. Interlaced video now
  shows as **576i** / **480i** / **1080i**, as it's broadcast and stored, rather than "p".
- **More (⋯) no longer offers Download video… while the top bar's download button is up** (a
  YouTube or other site video playing). It's still there the rest of the time.
- **Torrents only play.** The "Delete downloaded data on closing" switch and the "Cache folder"
  button are gone from Settings → Streaming and downloads: what a torrent fetches is always deleted
  when it closes and when Atlas Player exits. The top bar says how much is **buffered**. A settings file
  from an earlier version that kept the data still loads; the setting is just ignored.

## 2.0.0-beta.5 — 2026-11-24

A **beta** of 2.0.0: the new look is finished and was tried on Linux, but not yet on Windows.
Please report anything that looks or works wrong there; 2.0.0 follows once it's been tried.

- **New in beta.5: a controls bar you arrange yourself**, in the spirit of foobar2000's toolbars.
  Settings → Appearance → Controls bar shows the bar as three bands (left, centre, right) and what's
  off it; drag any piece where you want it, or click one and use **Earlier**, **Later**, **To
  left / centre / right** and **Take off the bar**. Every piece is its own: previous and next for
  files, chapters and frames, each badge, the mute button, the volume slider and its %. Play, More
  and Settings can move anywhere but stay on the bar. Right-click the bar for **Customize bar…**.
- **New pieces to place**: the time played, the time left, the length, a clock, the title (cut short
  with … when long), spaces and dividers (as many as you like).
- What you take off the bar is in **More (⋯)** wherever it fits there: now also previous / next file,
  mute, the disc menu and fullscreen. **Simple** (the default) and **Everything** set the whole
  arrangement in one go; anything else shows as **Custom**. Your bar from beta.4 or 1.x carries
  over: the buttons you had hidden stay off, the rest keep their places. The settings file keeps
  the arrangement as `barLeft`, `barCentre` and `barRight`, in place of `hiddenControls`.
- Checked by the automated tests (dragging in the editor, its buttons, the presets, the bar
  following at once, older settings files) and rendered in three themes; the editor hasn't yet
  been tried by hand in the running app.
- **Before that, beta.4: the Windows zip works again.** The beta.2 and beta.3 zips were missing
  everything Atlas Player plays with (libmpv, libVLC, FFmpeg, yt-dlp, the torrent engine): GitHub's
  build machine moved to the .NET 11 SDK, which leaves those files out of a single-file build
  where older SDKs put them beside it. They opened to an error and played nothing. The build now
  says where they go, and refuses to publish a zip without them.
- **Also fixed:** the subtitle colour choices and the seek bar's colours are made so any thread
  can use them (a test running off the UI thread failed with .NET 10).
- **Before that, beta.3: security updates.** Every library Atlas Player bundles was checked against the
  published vulnerabilities (CVEs), and the ones with fixes were updated (below).
- **.NET 10** (runtime 10.0.15): .NET 8 stopped getting security fixes on 10 November 2026. Both
  downloads carry their own .NET, now pinned to the newest patch, so the Flatpak no longer takes
  the older one Flathub's SDK ships (10.0.8, 100 fixes behind).
- **Linux: FFmpeg 8.0 → 9.1.** 8.0 had dozens of known holes, among them heap overflows in the
  VobSub (DVD subtitle) reader, the DVB subtitle parser and several decoders, and the 8.0 series
  has stopped. Also **mpv 0.42** (a crafted playlist page could add entries through its YouTube
  script), **libass 0.17.5** (crafted subtitles could write past a buffer), **libplacebo 7.387**
  (which mpv 0.42 needs) and **libbluray 1.5.1**.
- **Windows:** libmpv built from mpv 0.42, FFmpeg's 9.0 series with October's fixes, and libVLC
  without its ARIB (Japanese TV caption) plugins, which no disc uses and whose library has open
  holes.
- **Both:** yt-dlp 2026.11.23, QuickJS-NG 0.18.0, and the torrent engine's `url` crate.
- **Tried after the updates** on the Linux build: an HEVC file, a 4K AV1 file and an interlaced
  one (hardware decoded), HDR10 tonemapped to SDR with an ASS subtitle over it, a DVD image's
  menus, an Audio CD image, a YouTube link's formats up to 4K, and a DVD chapter ripped both
  copied and to H.264 (LPCM to FLAC, deinterlaced to 50p). The Windows zip was built but not run
  here.
- **A new look, and themes.** Settings → Appearance has eleven, switched at once in every
  window: the default theme (plum and orchid at the time) and its light version, **Graphite**,
  **Deep Sea**, **Amber**, **Forest**, and five with more character: **Call of Duty** (olive and
  HUD yellow, square edges, stencil titles), **GTA 6** (sunset pink and orange, neon teal, poster
  titles), **Red Dead Redemption 2** (leather, bone and blood red, western titles), **80s
  Synthwave** (magenta and cyan over a horizon grid) and **Halloween** (pumpkin and slime green).
  They borrow the games' colours and mood only: no game art or logos, and their title fonts are
  free look-alikes (SIL Open Font License), bundled so Windows and Linux look the same. A theme is
  colours, corners and one title font: nothing blurs or animates, so the player costs what it
  did. Body text stays Inter in every theme, and every theme's text passes the WCAG contrast
  ratio (4.5:1), checked by a test. Adding a theme is one file (see the README).
- **A simpler controls bar.** By default it now shows play, previous / next file, the frame
  steps, volume, the badges, the subtitle, audio and video menus, titles and chapters, the disc
  menu, Settings and fullscreen. A new **More (⋯)** menu holds whatever the bar leaves off (stop,
  the chapter steps, screenshots) and every way to open something, so nothing is out of reach and every
  shortcut still works. Whoever had every button showing moves to this **Simple** bar once;
  whoever had picked their own buttons keeps them. Settings → Appearance → Controls bar switches
  to **Everything** in one go, or arranges it piece by piece (**Custom**, above).
- **The welcome screen lists every way in**: Open file, Open folder, Open disc, Open link,
  Download video and Rip disc, each with its shortcut or what it takes, instead of an Open menu
  to open first. Settings is in the corner.
- **Settings, regrouped and reworded.** A sidebar of pages: Appearance, Playback, Video, Audio,
  Subtitles, **Streaming and downloads** (YouTube and other sites with torrents), **Backup and
  logs** (the settings file with the troubleshooting tools), Shortcuts and About. Each setting is
  one row: what it is, a sentence on what it does, and its control. Switches no longer change
  their own wording when flipped ("Deinterlacing off (this video)" / "BWDIF deinterlacing"):
  the row names what the switch does, and the switch says On or Off. Long drop-down entries
  became short ones with the explanation beside them. The Shortcuts page says what each key does
  in plain words ("Play or pause", "Subtitles 0.1 s earlier").
- **Dialogs** (Open link, Open disc, Download video, Rip disc) share one layout: title, what it
  does, then the buttons in a strip at the foot.
- **Play sits on the bar's centre**, with previous / next file, the frame steps, stop and the
  chapter steps as one group around it, whatever shows on each side; volume and the badges on
  the left, the menus on the right. On a narrow window the group moves aside rather than overlap
  the menus. The frame steps are on the Simple bar, with icons of their own (a frame with an
  arrow in it, not the skip-track look). The bars have more room at their edges and between
  their buttons, and the top bar's shade reaches further down so the title reads over a bright
  picture.
- **Space after clicking a bar button only pauses.** The clicked button kept the keyboard focus
  and clicked again on Space: after the fullscreen button, Space also left fullscreen; after
  play, it paused and played again. The bars' buttons no longer take the keyboard, and a key
  that runs a shortcut is the shortcut's alone. (This was so in 1.12.0 too.)
- **Menus, tooltips, drop-down lists and dialogs open where they belong on a desktop of several
  monitors** (Linux). They were fitted into the one work area X11 gives the whole desktop, which
  on three monitors here ended 1174 px into the main 1440 px one: the bar's menus floated far
  above the bar, tooltips away from their buttons, and Settings, the dialogs and the player
  itself opened pushed towards the top. Each monitor now counts as its own area.
- **Messages over the video follow the theme.** The volume shows a speaker, a level in the
  theme's colours (with a mark at 100 %) and the percentage; muted, a crossed speaker; the
  subtitle delay, a screenshot saved and the rest, a line of text in the same card. They were
  the engines' own white text (mpv's and libVLC's), each in its own font.
- **Settings have room for every theme's corners**: more padding in the rows than the roundest
  theme's corner (GTA 6's), a wider sidebar, and the selected page marked by its highlight alone.
- Nothing about playback, streaming or ripping changed: every setting, button and shortcut does
  what it did, with the same names in the settings file (plus `theme`). Every window and settings
  page was rendered in every theme and looked at, and the Linux build was played here: a file, a
  folder (previous / next), a DVD image with its menus, an Audio CD image, an SACD image, a magnet
  and a 4K YouTube link, through each of the bar's menus and More's actions. With the bars up it
  used the same CPU as 1.12.0 (9.1 to 9.3% of a core on the same 720p video, in any theme). MEGA
  app links weren't tried this time; their code didn't change.

## 2.0.0-beta.4 — 2026-11-24

- The Windows zip with its libraries again (beta.2 and beta.3 shipped without them). Its changes
  are all in 2.0.0-beta.5 above.

## 2.0.0-beta.3 — 2026-11-24

- Security updates. Its Windows zip was missing its libraries and played nothing; its
  changes are all in 2.0.0-beta.5 above, with a complete zip.

## 2.0.0-beta.2 — 2026-11-24

- The Linux Flatpak attached again: 2.0.0-beta.1's build failed on GitHub before it built
  anything (updating the build container's own runtimes). Its changes are all in 2.0.0-beta.5
  above, which adds security updates.

## 2.0.0-beta.1 — 2026-11-24

- Windows zip only: the Flatpak build failed on GitHub's side. Its changes are all in
  2.0.0-beta.5 above.

## 1.12.0 — 2026-11-08

- **MEGA desktop app links no longer buffer slowly.** The MEGA app's "Stream from MEGA" link
  (`http://127.0.0.1:4443/…`) sends about 4-5 MB/s per connection, less than a 4K film needs
  in its busy scenes, so they stalled, and each stall made the next wait longer. Atlas Player now
  reads the link six pieces at once, in order, and mpv plays from that: 18-25 MB/s here on a
  24 GB 2160p Dolby Vision remux, which started in under 4 s and never buffered again. The MEGA
  app keeps answering however often you seek. `mega.nz` links pasted into Atlas Player are read
  the same way (they too went over one connection).
- **Rip Disc: chapters.** Under the title, its chapters are listed with their lengths, all
  ticked; untick the ones you don't want. They become **one file** (chapters next to each other
  read in one go, others ripped apart and joined, each mark named after its chapter on the
  disc) or **one file per chapter**, in a folder. Checked on "The One": the lengths and marks
  are the disc's, and joined chapters decode cleanly across the join, their audio bit for bit
  the disc's (LPCM becomes FLAC once, over the whole file). Where two chapters that weren't next
  to each other meet, the AC-3 track has a gap of a fraction of a second (0.23 s there).
- **Rip Disc: x264 deinterlaces too**, with BWDIF, as x265 and Atlas Player's playback do: each
  field becomes a frame (50p, 59.94p for NTSC), in the field order found (the flags, confirmed by
  the picture). Interlaced titles are encoded at CRF 14: on a minute of "The One" (576i), against
  the disc's fields made frames by BWDIF, CRF 16 gave VMAF 95.9 at 51% of the disc's video size,
  CRF 14 97.0 at 75% (x265, CRF 16: 97.3 at 53%, five times slower). The whole 40-minute
  title: VMAF 97.2 at **93%** of the disc's video, in 12 minutes here (16 threads). So at 50p,
  x264 keeps the disc's look but saves little space on a busy concert; **x265 is the one for
  space** (68% for the same title, VMAF 97.6, 1.11.0's measure), x264 the one for speed.
- **Subtitles: their look**, in Settings → Subtitles: font (any installed one), bold, text and
  outline colours, outline and shadow thickness, a box behind the text instead of the outline (as
  on TV), how high they sit, and whether styled (ASS) subtitles take it too or keep their own.
  A preview shows it, and it applies at once to the video playing. Picture subtitles (DVDs,
  Blu-rays, PGS) can't be restyled.
- **Choose the controls bar's buttons**: Settings → Interface → Controls bar lists every button
  (and the badges, the volume); untick the ones you don't use. Play and Settings always stay.
- **Previous / Next for folders**: a folder of videos or songs (or several files dropped at once)
  gets the Previous / Next file buttons, as torrents had. Not for DVD and Blu-ray folders: their
  titles and chapters have their own buttons.
- **Settings in one file, for a new install or another PC**: Settings → Interface → Export
  settings… saves `atlas-player-settings.json`; drop it on the player of the new install (it asks
  first) or Import settings… it there. An old install's `settings.json` works the same way. The
  new machine's own graphics card, refresh rate, audio device and torrent port choices stay.
- **The Developer tab's options moved where they belong**: a new **Video** tab (rendering
  quality, frame timing, graphics card, HDR and tonemapping, this video's deinterlacing and
  hardware decoding switches), passthrough to **Audio**, the YouTube quality to **Playback**.
  Developer keeps the troubleshooting tools (Create log, the statistics overlay, Copy diagnostics).
- **No resolution badge for music**: a song's cover showed as "1440p" (or "1080p"), with "MJPEG"
  and "SW" beside it. Audio alone now shows only its audio badge. An Audio CD after a DVD no
  longer keeps the DVD's resolution.
- **The pointer comes back as soon as the mouse moves** in fullscreen, over the video too: any
  movement the system reports counts (it's also polled every 50 ms while hidden), and the video
  shows the arrow itself rather than whatever its parent window had. Tried here on Linux (mpv and
  discs); on Windows the arrow is set again too, **not yet tried there**.
- **The Linux build (Flatpak) on other distributions**: tried on CachyOS (installed fresh, the
  runtime downloaded by the bundle), Arch, Debian 13, Ubuntu 24.04 and Fedora 43, each with its
  own Flatpak (1.14 to 1.18), playing a video and a DVD with its menus. What can differ between
  machines is the graphics driver:
  - **NVIDIA's driver needs its Flatpak extension**, built for exactly that driver version
    (`org.freedesktop.Platform.GL.nvidia-590-44-01` for 590.44.01). Without it nothing can use
    the graphics card: video was black or crawled. Rolling distributions like CachyOS often run a
    driver Flathub hasn't built yet. Atlas Player now says so at start, with the command that installs
    it (and a button that copies it); Create log names the drivers too.
  - Video still plays without one: mpv falls back from Vulkan to OpenGL, and the window from
    OpenGL to drawing on the CPU, instead of a black picture or no window.
  - Without an X11 display (a Wayland desktop without Xwayland, as niri or sway can be set up),
    it says what's missing instead of quitting silently. A crash at start leaves `logs/crash.log`.

## 1.11.0 — 2026-10-31

- **Drag and drop** works on Linux, and over a playing video on Windows. Drop a video, several
  (they play one after the other, as a folder's do), a folder, a disc image, a cue sheet, a
  subtitle (it joins the video playing, or the one dropped with it) or a link or magnet from a
  browser, on the window or on the bars. Linux had none at all: the toolkit (Avalonia 11) has no
  drag and drop on X11, so Atlas Player now speaks X11's protocol (XDND) itself, the one KDE's
  Wayland apps (Dolphin) use to drop into X11 windows too. On Windows, mpv's video window
  took every drop and refused it; it no longer gets them.
- **The pointer hides in fullscreen on Windows.** Over the video it was left to mpv, whose window
  runs on its own thread and hid it only if it happened to be watching the mouse at that moment.
  Atlas Player now hides it for its whole window, the video included. **Not tried on Windows yet**
  (no Windows machine here); Settings → Developer → **Create log** now says when the pointer
  last hid and what showed it again, if it still stays.
- **Links start by themselves.** Each one waited for 10 s of buffer instead of 5 (the first wait
  counted as a stall and doubled it), with a black picture and a pause button, so Play pressed
  meanwhile really paused it. Now a link (YouTube, other sites, direct, MEGA, torrents) starts
  after 5 s, the top bar says **Opening…** and the play button spins until it does, and Play then
  never pauses it. A YouTube video starts in about 2 s here.
- **Rip Disc: DVDs to MKV in H.264 or H.265**, besides the lossless copy: the video encoded to
  look the same as the disc's in less space, everything else (every audio and subtitle track, the
  chapters) as before. **x264** (CRF 16, preset slow) keeps interlaced video interlaced, both
  fields as on the disc, in about ⅔ of the space. **x265** (CRF 18, preset slow) takes about half,
  but HEVC has no interlaced mode players handle (x265's field coding comes back with its field
  order swapping every frame), so an interlaced title is deinterlaced to 50p (59.94p) with BWDIF,
  as Atlas Player plays DVDs, at CRF 16: about ⅔. A title counts as interlaced when it's flagged so
  and 20 s of its picture agree (films flagged interlaced are common). Measured on a PAL concert
  DVD (576i, "The One"), against the disc's frames: on a minute of it, x264 66% of the video's
  size, VMAF 98.6; x265 50%, VMAF 98.7 as frames, 64% and VMAF 97.6 as 50p. The whole 40-minute
  title: x264 at 70% of the size, VMAF 98.9, in 10 minutes here (16 threads); x265 (50p) at 68%,
  VMAF 97.6, in 64 minutes. The progress shows the encoding speed.
- **Rip Disc: a VIDEO_TS folder** offers only the MKV: it's decrypted already.
- **The Rip Disc window fits the screen**: no taller or wider than the one it opens on; the form
  scrolls and the Rip button and the progress stay in view (a CD's form was taller than a laptop's
  screen).
- The Linux build's FFmpeg has x264 and x265 (the Windows zip's had them already).

## 1.10.1 — 2026-10-31

- **Crop to 16:9** chosen right after **Resize to 4:3** could crop by the stretched 4:3 shape
  instead of the video's own (cutting too little off a 1.25:1 picture): the player's last
  report of the stretch is no longer taken for the video's shape.
- The Linux build (Flatpak), which 1.10.0's release couldn't attach (a download failed while it
  was built). Everything in 1.10.0 is in it.

## 1.10.0 — 2026-10-31

- **Rip Disc** (Open → Rip Disc…, or a drive's **Rip…** button in Open Disc):
  - **Audio CDs** are read securely: every track twice (test and copy) with the drive's cache
    defeated, every block that reads differently read again until two reads agree. The drive's
    read offset comes from AccurateRip's list of drives (or, for a drive it doesn't list, from the
    disc itself), the gaps between tracks from the disc's sub-channel. Each track is checked
    against **AccurateRip** (v1 and v2). The tags (and the cover, 1200 px) come from **MusicBrainz**,
    or from the disc's CD-Text, and can be edited before ripping. Where MusicBrainz only knows
    discs that look like it (as many tracks, similar lengths), they're offered but never picked
    by themselves. Saved as **FLAC level 8** (the reference encoder, verified) or **WAV** (with
    RIFF tags, no ID3), in Music → Artist → Album (Year), with a cue sheet (gaps appended to the
    previous track), cover.jpg and a log.
  - **The log** has the layout rip logs use (settings, TOC, each track's test and copy CRCs,
    AccurateRip, the summary) and scores itself with the deductions tracker log checkers make:
    100% for a clean secure rip. It's Atlas Player's own: those checkers only accept EAC's, XLD's and
    whipper's logs, so it won't pass for one of them on a tracker.
  - Checked on the audio of the Dangerous rip in the Michael Jackson discography, read through a
    simulated drive with a +48 offset: the copy CRCs and AccurateRip checksums are exactly the
    ones in its EAC log, and all 14 tracks come out accurately ripped. **Not yet tried with a
    real drive**: no machine here has one.
  - **DVDs**, from a drive, an `.iso` or a VIDEO_TS folder: either **the VIDEO_TS folder as it
    is, decrypted** (menus and all, in Videos → the disc's name; it plays like the disc), or **one
    title as an MKV** (the longest, the main film, picked), nothing re-encoded: the MPEG-2 video,
    every audio and subtitle track with its language, and the chapters. LPCM audio becomes FLAC,
    bit for bit the same audio, as MakeMKV does. Checked on a DVD image: the copy is identical to
    its files, and the MKV's video frames, AC-3 and LPCM are the disc's own. Decrypting a
    store-bought (CSS) disc uses the same libdvdcss as playback, but wasn't tried here.
  - The rip carries on with its window closed, with its progress and Cancel at the top of the
    player. A cancelled rip leaves nothing behind. A disc playing stops first.
- **DSD and SACDs play at 192 kHz** (FFmpeg decodes DSD64 to 352.8 kHz, more than most DACs
  take); everything else plays at its own sample rate. On Windows, the system mixes every app at
  one rate (often 48 kHz): **Settings → Audio → Exclusive** gives the player the audio device to
  itself, so the DAC runs at the file's rate. Off by default (other apps go quiet). The audio
  badge's tooltip tells the rates.
- **CD and SACD badges** in the controls bar: an Audio CD (or CD image) shows "CD" where a video
  shows its codec and a DVD "DVD", an SACD "SACD" (its area in the tooltip). The downmix switch
  doesn't touch them: it's the channels badge beside them that it changes.
- **The channels badge** shows what the player does: "5.1" or "7.1" for the original channels, and
  "5.1 → stereo" / "7.1 → stereo" only with the downmix switch on. 7.1 is never downmixed unless
  asked: it plays as 8 channels. Where the audio device itself is set up with fewer speakers
  (Windows set to stereo), the tooltip says the system mixes it down for them. On discs the
  downmix switch now works too (it did nothing there), and a DVD's AC-3 or DTS 5.1 shows "5.1"
  (its channels come from the disc's IFO: libVLC only told them for LPCM).
- **Chapter buttons** show only while something with chapters plays: a file without chapters
  after one with them, a stopped player or a disc's menu no longer keep them.
- **In fullscreen the pointer hides after 3 seconds** without moving, paused or not; moving the
  mouse brings it back (a key doesn't). Not over the bars or with a menu open. On Linux it now
  also hides over discs.
- The Windows zip's FFmpeg is now BtbN's GPL build, which reads DVDs: the zip is larger.

## 1.9.0 — 2026-10-29

- **Disc images:** a DVD or Blu-ray `.iso` plays like the disc, menus included (Open Disc →
  **Disc image…**, or open / drop the file). So does an Audio CD image (`.bin` with its `.cue`),
  with its tracks as chapters, named as its cue sheet names them. Other `.iso` files are left
  alone.
- **SACD images:** the `.iso` a ripper makes of an SACD plays with Atlas Player's own SACD reader,
  the stereo area by default; the **Titles & chapters** menu has the multichannel (5.1) area,
  and the tracks are chapters, with the disc's titles. Played through on two Thriller SACDs
  (1999 and the 2022 MFSL), whose stereo areas are plain DSD. DST-compressed discs (most
  multichannel areas) are read too, but were only tested on images made for the purpose.
- **DSD files** (`.dsf`, `.dff`) play, and are picked up from folders.
- **Stop button** in the controls (Ctrl+S, as before).
- **Torrent playlists:** a torrent with several videos or songs gets **Previous** / **Next**
  buttons, and each file plays on into the next. Music counts now, not just videos; an album
  starts at its first track.
- **Resize to 4:3 / Crop to 16:9** in the video menu, for the playing video: resize for any
  picture that isn't 4:3, crop (the middle of it) for any that isn't 16:9. A DVD's shape comes
  from the disc itself (libVLC doesn't tell it), so a 4:3 DVD offers only the crop.
- **YouTube quality in the video menu:** while a YouTube video plays, the menu lists the
  qualities it offers and switches to one at once, from where it was, for that video. The
  default quality moved to Settings → Developer. Both are for YouTube only: other sites
  (archive.org, Vimeo…) play at their best, and direct links (MEGA's desktop app streaming
  included) are left to the player alone.
- **DVD badge:** a DVD shows "DVD" in the controls instead of its codec (MPEG-2 on hover).
- **5.1 stays 5.1 on discs:** DVDs and Blu-rays played their 5.1 tracks downmixed to stereo,
  as libVLC's default stereo mode does that. They now keep every channel (a stereo track is
  still picked first where a disc has one).
- The deinterlacing, hardware decoding, HDR and passthrough switches moved to **Settings →
  Developer** (the first two still for the playing video only); the player's menus keep the
  everyday options. The Video tab is gone (its options moved with them).
- The previous / next frame buttons, and the video menu, are hidden while only audio plays
  (music, Audio CDs, SACDs).
- Messages on the video (volume, subtitle timing, "Screenshot saved"…) start below the title
  bar instead of under it, and the buffering lines show in one place at a time.
- Linux: **Space** (and the other keys) stopped working after a click on the controls, until the
  window was minimized and restored: the hidden controls kept the keyboard. They give it back.
- Linux: **double-click** for fullscreen works, on discs too: clicks are read from the X server
  itself, as on Windows from Windows.
- Opening the same interlaced video twice in a row could leave the second without
  deinterlacing. Likewise, after two files alike (two stereo songs), the audio badge and the
  left / right channel choice could go missing, and the length shown could be the previous one's.

## 1.8.0 — 2026-10-28

- **Deinterlacing only where the video is interlaced.** The deinterlace badge showed for
  progressive videos too: discs reported it whatever they held, and it stayed up when a file
  played after a disc. Now each video is judged on its own:
  - Files: by their frames' interlace flags. Videos the CPU decodes are also checked by their
    picture (FFmpeg's idet), so tape captures saved without flags (MJPEG or lossless AVIs,
    some FLVs) get BWDIF too. Progressive video is left alone.
  - Discs: by libVLC's own detection.

  While an interlaced video plays, the video menu has an **on / off** switch for it (D flips it).
  Progressive videos have no switch. The Auto / On / Off setting is gone.
- **Smoother playback at 1080p and up on laptops.** This targets lag seen in High and Balanced
  on a Windows laptop with two GPUs and a high-refresh monitor:
  - Every profile now times frames by the audio clock, as Fast already did. The fixed 60 Hz
    display timing could clash with a 120–165 Hz monitor. Syncing to the display is now an
    option in Settings → Developer.
  - On laptops with two GPUs, playback and hardware decoding run on the high-performance one,
    instead of the power-saving one Windows hands out (Settings → Developer).
  - The controls bar no longer redraws with every video frame.

  If it still lags, play the video and use **Settings → Developer → Create log**, then send the
  file from the logs folder.
- **Stereo by default:** where a video or disc has several audio tracks (a concert disc's
  stereo mix beside 5.1 and DTS), the stereo one plays, within your preferred language if you
  set one
- **Left or right channel:** for a stereo source, the audio menu can play just its left or right
  channel on both speakers, for that video. The mono downmix is gone
- **Codec badge** in the controls: H.264, HEVC, AV1, MPEG-2… (the decoder's full name on hover)
- **Developer tab** in Settings: rendering quality, frame timing, tonemapping and the GPU choice
  moved there from the video menu and Settings → Video, with the troubleshooting tools
  (statistics overlay, Copy diagnostics, Create log)
- **Create log:** saves to the logs folder everything recorded since the playing video started:
  - every second's statistics: frames dropped and late, GPU time per frame, CPU use,
    refresh rates
  - the machine and its GPUs and displays
  - how the video is played, and the player's latest messages

  For benchmarking, or for whoever is helping
- The play / pause button no longer shows "play" while a video plays (after a disc, or a missed
  event)
- Menus no longer jump to the top-left corner of the screen when the pointer rests on them:
  the controls stay up while one of their menus is open
- The **Recent** list is gone from the welcome screen, and the app no longer keeps a history of
  what was played (the old list is dropped from settings.json)

## 1.7.0 — 2026-10-27

- **Simpler menus.** The Open menu holds **Open file**, **Open folder** and **Open Disc**;
  **Open Link** and **Download Video** are buttons of their own, on the welcome screen and in
  the controls. The Paste button and "Play from clipboard" are gone (Ctrl+V still plays a
  copied link)
- **Download Video:** the qualities appear as soon as a link is pasted (no more "Get
  qualities"), and **Audio only** saves just the best audio stream, as it is (no re-encoding)
- **BWDIF on discs too:** DVDs and Blu-rays (through libVLC) now deinterlace with BWDIF like
  files, folders and links do, frame for frame the same as FFmpeg's. Auto deinterlaces only
  what the disc flags as interlaced, instead of every disc
- **Audio CDs:** Open Disc plays them from the drive, with the tracks as chapters (Page Up /
  Page Down, the chapters menu), one after the other. A CD rip's folder with its `.cue` sheet
  (or the `.cue` itself) plays the same way. The drive list now also tells empty drives apart
- **Hardware acceleration, redone and working:** since 1.4.0 the app looked at the video
  before mpv had picked its track, so it never actually asked for hardware decoding: HEVC and
  1080p videos decoded on the CPU. Now each video is judged on its own: hardware decoding is on
  by default for HEVC and AV1 at any size and every codec from 1080p up, where the graphics card
  can decode them (the HW badge shows it), and its switch is in the video menu for that video
  only. Videos it doesn't apply to have no switch, and the setting is gone from Settings
- HDR → SDR tonemapping uses **mobius** by default; whoever was on the old default (spline)
  moves to it once
- **Windows: DVDs no longer lag and freeze when they first load.** libVLC opened all of its
  ~250 plugins on the first disc, on the window's thread; it now builds its plugin cache once
  in the background, and starts off that thread. DVDs decode on the CPU (they're standard
  definition), which also keeps the deinterlacer on the plain path
- Linux: libVLC's plugin cache in the Flatpak was always stale, so discs started slower than
  they should; it now matches the installed files

## 1.6.0 — 2026-10-27

- **DVDs and Blu-rays with their menus.** Open → **Open disc** lists your drives (or pick a
  VIDEO_TS / BDMV folder) and plays the disc with its own menus, through libVLC. Click the
  menu buttons or use the arrow keys and Enter. The new **Disc menu** button returns to the
  root menu at any time, and Titles & chapters jumps to any title or chapter
- Store-bought DVDs play (libdvdcss is included). Encrypted Blu-rays (AACS) and Blu-ray Java
  (BD-J) menus aren't supported; those discs fall back to titles and chapters where possible
- Linux: physical DVD and Blu-ray drives work in the Flatpak
- Downloads keep their **metadata**: title, uploader, date, description and chapters, plus the
  thumbnail as cover art
- Downloading the same video again never overwrites or skips it: the resolution is added to
  the name, and a counter if that exists too (`… 720p (1).mkv`)

## 1.5.0 — 2026-10-27

- **Downloader:** a Download button (welcome screen, the Open menus, and the top bar while a
  web video plays). Paste a link, pick a quality from the ones the site offers (2160p, 1080p,
  720p…), and it's saved to Downloads with the best audio merged in
- **Streaming quality** for YouTube and other sites: Settings → Playback, from best available
  down to 360p
- Separate **Open link** button with its own icon, on the welcome screen and in the controls
- Subtitle timing (Z / X) shows the total shift on screen with every press: "+0.1s", "+0.2s"…
- Hardware acceleration is on for HEVC at any size and for every codec from 1080p up
- Playback is timed for a 60 Hz display by default, which fixes stutter on monitors that report
  odd refresh rates. Settings → Video → Advanced has Auto and other rates
- Rendering is **Balanced** by default; whoever was on Auto moves to Balanced once. Only Auto
  switches to Fast by itself when frames keep dropping
- **Volume normalization** switch (off by default) in the audio menu and Settings
- **Mono downmix** for stereo sources only; the stereo downmix still applies to 5.1 / 7.1 only
- Leaving fullscreen goes back to a maximized window

## 1.4.0 — 2026-10-26

- **Titles & chapters** menu: jump to any chapter, or switch a DVD's or Blu-ray's title
  (main film, extras) at any moment. Previous / next chapter buttons and Page Up / Page Down.
  Works for any video with chapters, MKVs included. The discs' own interactive menus aren't
  shown yet
- The video menu's HDR options (tonemapping) appear only while an HDR video plays
- Hardware acceleration is used for HEVC (H.265) only; other codecs decode in software. Its
  switch appears in the video menu only while an HEVC video plays
- The "Downmix to stereo" switch appears only for 5.1 / 7.1 audio, not for mono or stereo
- Windows: double-click on the video for fullscreen now works; clicks are read from
  Windows itself, as the video window didn't reliably pass them on
- Releases go up as soon as the Windows zip is built; the Linux Flatpak is attached a few
  minutes later

## 1.3.3 — 2026-10-26

- Windows: closing the window right as it was being resized could raise an error
- Linux: works with an older system libmpv too (Ubuntu 24.04's 0.37): streams open and the
  player starts, where before they failed. The Flatpak bundles its own libmpv and wasn't
  affected
- Releases are now built and published by GitHub from the tagged commit, and every change is
  tested on Windows and Linux

## 1.3.2 — 2026-10-26

- Fullscreen on Linux (KDE): the controls bar now really sits on the bottom edge. KDE kept
  ordinary windows out of the space its taskbar reserves, even with the video fullscreen
  over the taskbar, which left a strip of video below the bar (1.3.1 didn't fix this).
  The bars are now dock windows, which may go there, and they hide while another app is
  in front

## 1.3.1 — 2026-10-26

- In fullscreen the controls bar sits on the bottom edge of the screen again, with no strip of video below it

## 1.3.0 — 2026-10-26

- One **Open** menu: Open file, Open folder (Ctrl+Shift+O) and Open link. Folders and
  drag & drop of folders: a DVD folder (VIDEO_TS) or Blu-ray folder (BDMV) plays as a
  disc from its main title; any other folder plays its videos as a playlist. Unencrypted
  discs only
- Uncompressed AVI (8-bit and 10-bit capture formats) and DVD VOB files are tested to play
- Download button at the top while a YouTube (or other site's) video plays: saves the best
  quality, video and audio merged into one MKV, to your Downloads folder, with progress and
  Cancel. The Windows build now includes ffmpeg.exe for this; the Linux Flatpak now
  includes yt-dlp, so YouTube links also play there
- HDR is automatic: on a monitor with HDR switched on (Windows), HDR video goes to it
  untouched; on an SDR monitor it is tonemapped. Always tonemap / always passthrough can
  still be chosen, and H still switches by hand. Tonemapping options moved under
  "Advanced", hidden on HDR monitors. Linux always tonemaps (X11 can't output HDR)
- Rendering quality (Auto / High / Balanced / Fast) moved under "Advanced"
- Bob deinterlacing removed: it squashed the picture, and BWDIF already gives one frame
  per field (50i becomes 50p)
- Double-click on the video enters and leaves fullscreen
- The mouse pointer hides together with the controls, including in fullscreen

## 1.2.0 — 2026-10-26

- Smooth 4K on weaker GPUs. 4K costs about five times as much to render as 1080p, which is
  why 4K could stutter while 1080p was fine. Settings → Video → Rendering:
  Auto (default) uses High below 4K and Balanced for 4K, and switches to Fast by itself if
  frames still drop; High, Balanced and Fast can be picked directly
- Slow connections: each time a stream has to stop and buffer, it now buffers longer
  before resuming (5 s, then 10, 20, 40, up to 60), so there are fewer interruptions
- Copy diagnostics (Ctrl+I, or Settings → About): copies the machine, the video, and why
  playback is struggling, to paste to whoever is helping

## 1.1.0 — 2026-10-26

- Controls come back when you move the mouse, including in fullscreen
- Deinterlacing: BWDIF by default, Bob as an option
- Subtitle size slider shows the font size
- Logarithmic volume slider (equal steps of loudness)
- Faster, smoother magnet streaming for 4K HDR: reads 256 MB ahead and buffers 5 s before starting
- Original audio by default; stereo downmix and AV-receiver passthrough are optional
- Play web links: YouTube and ~1800 other sites (via yt-dlp), MEGA file links, direct video URLs
- Buffering messages, including "Big fast, many speed" and "Bruney"
- Version shown in the app and in the download's file name

## 1.0.0 — 2026-10-26

- First release: HEVC/HDR playback with HDR→SDR tonemapping, magnet streaming, subtitles,
  uncompressed PNG screenshots, frame stepping, automatic deinterlacing, portable Windows build
