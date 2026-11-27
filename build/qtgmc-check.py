# Checks a bundled VapourSynth runs QTGMC: python -I qtgmc-check.py <modules> <plugins>
# (the folders Qtgmc.Find gives: AtlasPlayer.Core/Ripping/Dvd/Qtgmc.cs). It loads VapourSynth as the
# ripper does, checks every plugin namespace QTGMC calls (Qtgmc.Namespaces) and deinterlaces a
# blank interlaced PAL clip at every preset the rip window offers. Run by the Flatpak build, the
# Windows publish script and the Windows CI job; prints "qtgmc check ok" or fails with the reason.
import os
import sys

modules, plugins = sys.argv[1], sys.argv[2]
sys.path.insert(0, modules)
import vapoursynth as vs

core = vs.core
for name in sorted(os.listdir(plugins)):
    if name.lower().startswith('libfftw'):
        continue
    if name.lower().endswith(('.so', '.dll', '.dylib')):
        try:
            core.std.LoadPlugin(os.path.join(plugins, name))
        except vs.Error as e:
            if 'already loaded' not in str(e):
                raise
import havsfunc

NAMESPACES = ('mv', 'znedi3', 'rgvs', 'bs', 'fmtc', 'misc', 'eedi3m', 'fft3dfilter')
PRESETS = ('Medium', 'Slow', 'Slower', 'Very Slow', 'Placebo')

missing = [ns for ns in NAMESPACES if not hasattr(core, ns)]
if missing or not hasattr(havsfunc, 'QTGMC'):
    sys.exit('missing ' + ' '.join(missing or ['QTGMC']))

clip = core.std.BlankClip(format=vs.YUV420P8, width=720, height=576, length=10, fpsnum=25, fpsden=1, color=[16, 128, 128])
clip = core.std.SetFieldBased(clip, 2)
for preset in PRESETS:
    out = havsfunc.QTGMC(clip, Preset=preset, TFF=True, FPSDivisor=1)
    if out.num_frames != 20 or (out.width, out.height) != (720, 576):
        sys.exit(f'{preset}: {out.num_frames} frames of {out.width}x{out.height}, not 20 of 720x576')
    for n in range(out.num_frames):
        out.get_frame(n)
    print(f'{preset}: {out.num_frames} frames')
print('qtgmc check ok')
