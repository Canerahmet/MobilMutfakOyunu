# -*- coding: utf-8 -*-
"""
Licence check - the gate for a commercial release
============================================================================
This project will be released COMMERCIALLY. Every asset pack used must
have a licence that permits commercial use, the licence text must ship
with the game, and it must have a row in the attribution ledger.

WHY A MACHINE CHECKS THIS:

The project's own log says "the biggest risk: an asset produced with the
free tier of an AI tool slipping through - those tiers are closed to
commercial use". But there was ZERO FRICTION on that path: dropping an
.ogg into the Resources/audio/ folder needs no code change, needs no
licence text and triggered no checker. Sfx.Init plays EVERY clip in the
folder without asking.

Google Play will not catch this; the copyright holder will.

There were also two separate ATTRIBUTION ledgers (vendor/ATTRIBUTION.md
and Art/ATTRIBUTION.md) and they had drifted - one carried the table of
engine components, the other did not. Writing the same rule in two
places has drifted silently five times in this project.

What is checked:
  1. Does every asset folder under Art/ have a License.txt.
  2. If it does, is it a known licence that is open to COMMERCIAL use
     (CC0, OFL, MIT, Apache, CC-BY).
  3. If there are audio files under Resources/audio/, is there a licence
     for them too.
  4. Does Art/ATTRIBUTION.md mention every folder.

Exit code 0 clean, 1 at least one thing is missing.
"""
from __future__ import print_function

import io
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
ART = os.path.join(ROOT, "unity", "Assets", "Lokanta", "Art")
AUDIO = os.path.join(ROOT, "unity", "Assets", "Lokanta", "Resources", "audio")
LICENSES = os.path.join(ROOT, "unity", "Assets", "Lokanta", "Resources", "licenses")
ATTRIBUTION = os.path.join(ART, "ATTRIBUTION.md")

# Folders that carry NO assets: generated, or the project's own output.
GENERATED = {"Materials", "Prefab", "Animator", "Mesh"}

# Licences we know to be open to commercial use. If one of these marks
# appears in the text it is fine; if none does, a HUMAN must look.
RECOGNISED = [
    ("CC0", "CC0 1.0 - public domain, commercial use free"),
    ("Creative Commons Zero", "CC0 1.0"),
    ("SIL OPEN FONT LICENSE", "OFL 1.1 - commercial use free, the font may not be sold"),
    ("MIT License", "MIT"),
    ("Apache License", "Apache 2.0"),
    ("CC-BY", "CC-BY - ATTRIBUTION REQUIRED"),
    ("Attribution 4.0", "CC-BY 4.0 - ATTRIBUTION REQUIRED"),
]

AUDIO_SUFFIXES = (".ogg", ".wav", ".mp3", ".aiff", ".aif")


def read(path):
    try:
        return io.open(path, encoding="utf-8", errors="replace").read()
    except IOError:
        return ""


def main():
    problems = []
    rows = []

    if not os.path.isdir(ART):
        print("ERROR: no Art folder: " + ART)
        return 1

    attribution = read(ATTRIBUTION)
    if not attribution:
        problems.append("Art/ATTRIBUTION.md is missing - the attribution "
                        "ledger is required before release")

    # --- 1-2. asset folders ----------------------------------------------
    for name in sorted(os.listdir(ART)):
        path = os.path.join(ART, name)
        if not os.path.isdir(path) or name in GENERATED:
            continue

        # No third-party licence is looked for in a folder the project
        # MADE ITSELF; being marked that way in the ATTRIBUTION table is
        # enough. The ledger is English now (CLAUDE.md rule 1): the mark
        # looked for is "own work", not the old Turkish one. The Turkish
        # letter normalisation stays - it is harmless, and it works
        # again if the ledger ever carries those letters.
        own_work = ("| " + name + " |") in attribution and "own work" in \
            attribution.replace("ü", "u").replace("ı", "i").lower()

        licence = os.path.join(path, "License.txt")
        if not os.path.isfile(licence):
            if own_work:
                rows.append("  %-10s the project's own work" % name)
                continue
            problems.append("Art/%s: NO License.txt" % name)
            continue

        text = read(licence)
        recognised = None
        for mark, description in RECOGNISED:
            if mark.lower() in text.lower():
                recognised = description
                break

        if recognised is None:
            problems.append("Art/%s: licence NOT RECOGNISED - a human must look"
                            % name)
        else:
            rows.append("  %-10s %s" % (name, recognised))

        if ("| " + name + " |") not in attribution:
            problems.append("Art/%s: no row in the folder table of "
                            "ATTRIBUTION.md" % name)

    # --- 3. audio ---------------------------------------------------------
    clips = []
    if os.path.isdir(AUDIO):
        for base, _, files in os.walk(AUDIO):
            for f in files:
                if f.lower().endswith(AUDIO_SUFFIXES):
                    clips.append(os.path.join(base, f))

    if clips:
        licence_text = ""
        if os.path.isdir(LICENSES):
            for f in os.listdir(LICENSES):
                if f.endswith(".txt"):
                    licence_text += read(os.path.join(LICENSES, f))

        if "audio" not in attribution.lower():
            problems.append("Resources/audio/ is not empty (%d files) but "
                            "ATTRIBUTION.md has no audio section" % len(clips))
        if not licence_text:
            problems.append("Resources/audio/ is not empty but "
                            "Resources/licenses/ is")
        rows.append("  %-10s %d files" % ("audio", len(clips)))
    else:
        rows.append("  %-10s none (zero risk)" % "audio")

    # --- report ------------------------------------------------------------
    for r in rows:
        print(r)

    if problems:
        print("")
        for p in problems:
            print("  MISSING: " + p)
        print("result   : %d missing" % len(problems))
        return 1

    print("result   : every asset is licensed and in the attribution ledger")
    return 0


if __name__ == "__main__":
    sys.exit(main())
