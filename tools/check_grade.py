# -*- coding: utf-8 -*-
"""Is the colour grade wired, and is it still the LIGHT one docs/19 allowed?

docs/19 permits **"at most a light colour grading table"**. Two different
things can go wrong with that sentence and this checks both.

**It can stay unspent.** That is what docs/58 found: `postProcessData:
{fileID: 0}`, an untouched default volume profile, and a game shipping Unity's
raw output. Nothing failed, because nothing was looking.

**It can quietly stop being light.** Every post-processing override lives in
one shared asset with `m_OverrideState: 1` already set on all of it, so
turning bloom on is not adding anything - it is typing a number into a field
that is already there. In URP 17 tonemapping, colour adjustments, white
balance and split toning fold into the uber pass and stay on-tile; **bloom and
depth of field resolve to memory and do not**, and the Android benchmark in
docs/58 takes a frame from 25 ms to 60.5 ms with bloom on. That is the frame
budget twice over, from one number.

So the expensive effects are listed by name with the value that means "off",
and any other value is a failure with the reason printed next to it.

WHAT THIS CANNOT TELL YOU. Post-processing forces the camera through an
intermediate render target, and on a tiler that is bandwidth. No desktop run
measures it. The device check is an open item in docs/21; this file only
guarantees that what a device would be asked to measure is the light version.

Exit code 0 clean, 1 something is off.
"""
from __future__ import print_function

import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PROFILE = os.path.join(ROOT, "unity", "Assets", "DefaultVolumeProfile.asset")
RENDERER = os.path.join(ROOT, "unity", "Assets", "Settings",
                        "LokantaURP_Renderer.asset")
SCENE = os.path.join(ROOT, "unity", "Assets", "Lokanta", "Game.unity")

# The grade itself: the values that have to be there, or the allowance is
# still unspent. These are not "at least" thresholds - an exact match, so that
# a value drifting in EITHER direction is seen.
GRADE = [
    ("ColorAdjustments", "contrast", "8"),
    ("ColorAdjustments", "saturation", "6"),
    ("WhiteBalance", "temperature", "6"),
]

# Every override the profile is allowed to contain. A CLOSED list, because
# the failure this file exists to prevent is a silent one: an effect that
# nobody decided about costs exactly as much as one somebody turned on.
# Anything listed here and nowhere else is on-tile and left at its default;
# anything NOT here at all fails and has to be classified by hand.
KNOWN = set([
    "PaniniProjection", "Vignette", "DepthOfField", "LiftGammaGain",
    "FilmGrain", "ColorLookup", "ColorCurves", "LensDistortion",
    "ProbeVolumesOptions", "SplitToning", "MotionBlur", "Bloom",
    "ColorAdjustments", "WhiteBalance", "Tonemapping",
    "ShadowsMidtonesHighlights", "ScreenSpaceLensFlare", "ChannelMixer",
    "ChromaticAberration",
])

# The effects that do not run on-tile, with the value that means "off" and
# why each one is refused.
EXPENSIVE = [
    ("Tonemapping", "mode", "0",
     "ACES (2) is a fitted curve evaluated per pixel; the grade was authored "
     "against None and Neutral is the only other value that may be chosen"),
    ("ColorLookup", "contribution", "0",
     "a third texture read per pixel, and the grade is already three numbers "
     "the uber pass folds in for nothing"),
    ("Bloom", "intensity", "0",
     "resolves to memory; 25 ms -> 60.5 ms in the Android benchmark in docs/58"),
    ("DepthOfField", "mode", "0",
     "a second full-screen blur on a scene with no focal subject"),
    ("MotionBlur", "intensity", "0",
     "needs motion vectors, and the camera barely moves"),
    ("ScreenSpaceLensFlare", "intensity", "0",
     "samples the bloom pyramid, so it drags bloom in with it"),
    ("FilmGrain", "intensity", "0",
     "a full-screen texture read every frame for noise on flat low-poly colour"),
    ("ChromaticAberration", "intensity", "0",
     "multi-tap; and it would smear the UI's own edges"),
    ("PaniniProjection", "distance", "0",
     "a lens warp on a fixed 22 degree camera has nothing to correct"),
    ("LensDistortion", "intensity", "0", "same"),
    ("Vignette", "intensity", "0",
     "cheap, but it darkens the corners of a 20:9 frame the hall is already "
     "using every pixel of"),
]


def _utf8_stdout():
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except AttributeError:
        pass


def blocks(text):
    """The profile's overrides, as {name: body}."""
    out = {}
    for part in text.split("--- !u!114"):
        m = re.search(r"^  m_Name: (\S+)\s*$", part, re.M)
        if m:
            out[m.group(1)] = part
    return out


def is_active(body):
    """Is the override itself switched on?

    A block with `active: 0` is in the file, has every value written into it,
    and does NOTHING. Reading only m_Value cannot tell the two apart - which
    is the bug this function exists for: the check was green either way.
    """
    m = re.search(r"^  active: (\d+)\s*$", body, re.M)
    return m is not None and m.group(1) == "1"


def value_of(body, field):
    """(overridden?, value) for one field, or (False, None) if it is absent.

    m_OverrideState is the second switch. A value with the state at 0 is a
    number sitting in an asset that the volume system steps straight over, so
    "contrast: 8" can be written, committed and read back and still not reach
    a single pixel. Both switches are returned; the caller decides what each
    one means, because they mean OPPOSITE things for the grade (it must be
    on) and for the expensive effects (off is a fine way of being off).
    """
    m = re.search(r"^  %s:\n    m_OverrideState: (\d+)\n    m_Value: (.+)$"
                  % re.escape(field), body, re.M)
    if m is None:
        return False, None
    return m.group(1) == "1", m.group(2).strip()


def main():
    _utf8_stdout()
    bad = []

    text = io.open(PROFILE, encoding="utf-8").read()
    found = blocks(text)

    for name, field, want in GRADE:
        body = found.get(name)
        if body is None:
            bad.append("%s is not in the profile at all" % name)
            continue
        if not is_active(body):
            bad.append("%s has active: 0 - every value in it is written and "
                       "none of it reaches a pixel" % name)
            continue
        overridden, got = value_of(body, field)
        if not overridden:
            bad.append("%s.%s has m_OverrideState: 0 - the value is %s and "
                       "the volume system steps over it" % (name, field, got))
            continue
        if got != want:
            bad.append("%s.%s is %s, expected %s - the grade docs/19 allowed "
                       "has drifted" % (name, field, got, want))

    for name, field, off, why in EXPENSIVE:
        body = found.get(name)
        if body is None:
            continue                      # absent is off
        if not is_active(body):
            continue                      # switched off at the block
        overridden, got = value_of(body, field)
        if not overridden:
            continue                      # written but not applied
        if got != off:
            bad.append("%s.%s is %s and has to be %s: %s"
                       % (name, field, got, off, why))

    # THE CLOSED LIST. Every other check in this file asks about an effect BY
    # NAME, so an effect nobody has named is invisible to all of them: URP
    # gaining one more override would add a cost this file would go on
    # reporting as clean.
    for name in sorted(found):
        if name == "DefaultVolumeProfile":
            continue                      # the asset's own object
        if name not in KNOWN:
            bad.append("%s is in the profile and nothing here classifies it - "
                       "decide whether it resolves on-tile, then add it to "
                       "KNOWN, EXPENSIVE or GRADE" % name)

    # The pass has to be wired at both ends: the renderer needs the data asset
    # and the camera has to ask for it. Either one missing and the profile
    # above is a file nobody reads.
    renderer = io.open(RENDERER, encoding="utf-8").read()
    m = re.search(r"^  postProcessData: \{fileID: (\d+)", renderer, re.M)
    if m is None:
        bad.append("the renderer has no postProcessData field at all")
    elif m.group(1) == "0":
        bad.append("postProcessData is 0 on the renderer - the grade is "
                   "written but nothing runs it (run 'Lokanta/Apply the "
                   "project settings')")

    scene = io.open(SCENE, encoding="utf-8").read()
    if re.search(r"^  m_RenderPostProcessing: 1\s*$", scene, re.M) is None:
        bad.append("the camera does not ask for post-processing in Game.unity "
                   "(run 'Lokanta/Build the game scene')")

    print("grade       : contrast %s, saturation %s, white balance %s"
          % (GRADE[0][2], GRADE[1][2], GRADE[2][2]))
    print("off-tile    : %d effects checked and refused" % len(EXPENSIVE))
    print("closed list : %d overrides classified" % len(KNOWN))
    if not bad:
        print("result      : the colour grade is wired and still the light one")
        return 0

    print()
    for row in bad:
        print("GRADE       %s" % row)
    print()
    print("result      : %d problem(s)" % len(bad))
    return 1


if __name__ == "__main__":
    sys.exit(main())
