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
  5. Does every sound name in Sfx.cs appear in BOTH sound-name lists
     (the ledger's audio table and Resources/audio/README.md). Those
     lists are the brief for the files that have not been made yet; a
     name missing from them is a sound nobody will be asked to record.

Exit code 0 clean, 1 at least one thing is missing.
"""
from __future__ import print_function

import io
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
ART = os.path.join(ROOT, "unity", "Assets", "Lokanta", "Art")
AUDIO = os.path.join(ROOT, "unity", "Assets", "Lokanta", "Resources", "audio")
LICENSES = os.path.join(ROOT, "unity", "Assets", "Lokanta", "Resources", "licenses")
ATTRIBUTION = os.path.join(ART, "ATTRIBUTION.md")
AUDIO_README = os.path.join(AUDIO, "README.md")
SFX = os.path.join(ROOT, "unity", "Assets", "Lokanta", "Game", "Sfx.cs")

# Folders this project WRITES rather than downloads. They are exempt from
# needing a License.txt of their own - and from nothing else.
#
# THEY USED TO BE SKIPPED ENTIRELY, AND TWO OF THEM ARE NOT OURS. `Mesh/`
# holds character body meshes extracted from the Kenney FBX files and
# `Prefab/` wraps those same models: the container is the project's, the
# GEOMETRY IS SOMEBODY ELSE'S. Kenney's packs are CC0, so nothing here was
# ever a legal risk - but "skip the folder" is not a judgement about the
# licence, it is the absence of one, and the next pack to arrive this way
# might not be CC0. Every folder needs a row that says where its contents
# came from; only the licence FILE is excused.
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


def _ascii(text):
    """Lowercased, with the two Turkish letters the old ledger used folded."""
    return text.replace("ü", "u").replace("ı", "i").lower()


def _row_for(attribution, name):
    """The folder's own line in the mapping table, or None.

    A row is `| Folder | Package | Licence |`; the folder is the first cell.
    Matching the CELL rather than the line keeps `Characters` from matching
    the derived-texture row underneath it.
    """
    for line in attribution.splitlines():
        if not line.startswith("|"):
            continue
        cells = [c.strip() for c in line.strip().strip("|").split("|")]
        if cells and cells[0] == name:
            return line
    return None


def read(path):
    try:
        return io.open(path, encoding="utf-8", errors="replace").read()
    except IOError:
        return ""


def _summary(row):
    """The package and licence columns of a ledger row, as one line."""
    cells = [c.strip() for c in row.strip().strip("|").split("|")]
    if len(cells) >= 3:
        return "%s (%s)" % (cells[1], cells[2])
    return row.strip()


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
        if not os.path.isdir(path):
            continue

        if name in GENERATED:
            # No licence file, but still a row: the row is what records
            # whether the bytes in here are ours or derived from a package,
            # and it is the only place anybody could find that out later.
            row = _row_for(attribution, name)
            if row is None:
                problems.append("Art/%s: no row in the folder table of "
                                "ATTRIBUTION.md - say whether it is the "
                                "project's own work or derived from a "
                                "package, and under which licence" % name)
            else:
                rows.append("  %-10s %s" % (name, _summary(row)))
            continue

        # No third-party licence is looked for in a folder the project
        # MADE ITSELF; being marked that way in the ATTRIBUTION table is
        # enough. The ledger is English now (CLAUDE.md rule 1): the mark
        # looked for is "own work", not the old Turkish one. The Turkish
        # letter normalisation stays - it is harmless, and it works
        # again if the ledger ever carries those letters.
        # THE ESCAPE HATCH WAS PERMANENTLY OPEN.
        #
        # The second half of this test searched the WHOLE ledger for "own
        # work", and the ledger has a heading called "Our own work". So it was
        # true for every folder, forever, and the check collapsed to "does
        # this folder have a row": delete a real CC0 License.txt and the run
        # still printed "the project's own work" and exited 0. That is the
        # commercial-release risk CLAUDE.md rule 6 exists for, defended by a
        # substring search over the wrong string.
        #
        # It asks the folder's OWN ROW now.
        row = _row_for(attribution, name)
        own_work = row is not None and "own work" in _ascii(row)

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

    # --- 5. the sound names ------------------------------------------------
    #
    # THE TWO LISTS HAD ALREADY DRIFTED. `uyari` and `bitti` were added to
    # Sfx.cs and neither the ledger table nor Resources/audio/README.md moved,
    # so both said "ten sounds" while the code asked for twelve. Nothing
    # noticed, because nothing was reading Sfx.cs.
    #
    # That is not cosmetic: the lists ARE the brief. A sound missing from them
    # is a sound nobody will ever be asked to record, and the game ships on the
    # synthesised fallback for ever - silently, because the fallback is a
    # complete sound and the tour counts it as one.
    sfx = read(SFX)
    before = len(problems)
    wanted = re.findall(r'Prefer\("([^"]+)"', sfx)
    if not wanted:
        problems.append("no Prefer(\"...\") call found in Sfx.cs - this check "
                        "cannot see the sound names any more")
    for name in wanted:
        if ("`" + name + "`") not in attribution:
            problems.append("the sound '%s' is in Sfx.cs and has no row in the "
                            "audio table of ATTRIBUTION.md" % name)
        if name not in read(AUDIO_README):
            problems.append("the sound '%s' is in Sfx.cs and is not in the "
                            "name list of Resources/audio/README.md" % name)
    # THE ROW MUST NOT CLAIM WHAT THE CHECK JUST DENIED. The first version
    # printed "ledger and README agree" on the same run as the MISSING line
    # that said they did not - a report that contradicts itself is worse than
    # no report, because the eye reads the row and not the list.
    if wanted:
        rows.append("  %-10s %d names, %s" % ("sounds", len(wanted),
                    "ledger and README agree" if len(problems) == before
                    else "%d not in both lists" % (len(problems) - before)))

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
