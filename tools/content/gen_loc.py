# -*- coding: utf-8 -*-
"""
Writes content/loc/tr.json: the game's string TABLE.

NOT "every string in the game", and it must not claim to be: about
twenty-five interface strings are still embedded in the code
("Muedavimler", "Yukselt - ", "Bekleyen masa yok" - Regulars, Upgrade -,
No table waiting - the credits screen...). They work in a single-language
build, but because they are not in the table they cannot be reviewed from
one place.

NO RISK to font coverage: tools/art/check_font.py also scans the .cs
files under unity/Assets/Lokanta/Game, so a character written in code is
checked too.

Two sources are merged:

  1. The keys the content asks for. Every dish, ingredient, archetype,
     trait and station has a nameKey, and regulars add a jobKey and
     story beats on top. This tool SCANS the content and works out which
     keys are needed by itself - keeping the list by hand silently
     leaves text missing as soon as the content changes.

  2. Interface strings. Written by hand, because they are not in the
     content.

Validation is strict: if a key the content asks for is missing, or the
table holds a key the content does not ask for, the tool FAILS. Missing
text means a button in the game that reads "dish.hamburger".

Usage:
    python tools/content/gen_loc.py
"""
from __future__ import print_function

import io
import json
import os
import re
import sys

import loc_tarama

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
# THE LANGUAGE TABLES LIVE IN THEIR OWN FOLDER.
#
# Two files per language (content + interface) and five languages means
# ten files - they were getting lost between six generators and two
# checkers. The folder name says what they are; the naming pattern
# (loc_<language>[_ui].py) stayed the same, so references in the docs
# changed by exactly one folder.
sys.path.insert(0, os.path.join(HERE, "languages"))
ROOT = os.path.dirname(os.path.dirname(HERE))
CONTENT = os.path.join(ROOT, "content")
OUT = os.path.join(CONTENT, "loc", "tr.json")
OUT_EN = os.path.join(CONTENT, "loc", "en.json")


# Every table sits in its own module, but they all go through THE SAME
# GENERATOR: the comparison below requires that the tables carry the same
# keys and the same formatting placeholders. With a separate tool per
# language the tables would drift apart silently - and drift in the text
# means a button that reads "[ui.staff.hire]".
import loc_tr  # noqa: E402
import loc_en  # noqa: E402
import loc_es  # noqa: E402
import loc_zh  # noqa: E402
import loc_ar  # noqa: E402

# EVERY LANGUAGE, TURKISH INCLUDED, IN THE SAME SHAPE.
#
# The Turkish table used to sit INSIDE this file, and that made two
# things false at once: the exemption in CLAUDE.md pointed at a file
# that did not exist, and gen_loc.py could not be translated because it
# carried the player's text. All five languages are now two files
# (content + interface) read through one generator.
LANGUAGES = [loc_tr, loc_en, loc_es, loc_zh, loc_ar]

# EXTRA LANGUAGES: one list.
#
# Adding a language is one line. A separate `build_*` and a separate
# `OUT_*` per language was exactly the pattern that drifted silently five
# times in this project: a new family added to one and not to the other.
#
# The keys must match the `Languages` array in Loc.cs EXACTLY - the game
# looks for "loc/<code>.json".
EXTRA = [
    ("es", loc_es),
    ("zh", loc_zh),
    ("ar", loc_ar),
]


# ---------------------------------------------------------------------------
def SCREEN_KEY(k):
    """Is this a key the content does not ask for but a SCREEN uses?

    Five families: "ui." interface strings, "notice." event notices,
    "score." year-end evaluation axes, "badge." badge names, and the
    DESCRIPTION strings that end in ".desc".

    "badge.": the badges belong to Badges.cs - the content files do not
    ask for them and MUST NOT. If a badge moved into the content, its
    condition would be in code and its text in content, and the two
    could drift apart silently. The content files do not ask for these -
    the code does. If the checker does not separate the two, it either
    treats the notices as "extra" and deletes them, or the code finds no
    text at all.

    ".desc": the content asks for a trait's NAME (nameKey), not what it
    does. The description only appears on the hiring card and is the
    ONLY way the player can tell two candidates apart.

    ".voice": same reason, different job. ".desc" describes the
    mechanic, ".voice" the person. Both are text the code asks for - the
    content files ask for neither.
    """
    return (k.startswith("ui.") or k.startswith("notice.")
            or k.startswith("score.") or k.startswith("badge.")
            or k.endswith(".desc") or k.endswith(".voice"))


def _screen_families_must_not_drift():
    """Do SCREEN_KEY and LocTests.cs recognise the same families?

    This list exists IN TWO LANGUAGES - Python here, C# in the test -
    and it DRIFTED twice: first ".desc" was added to the generator and
    not to the test (twelve strings were counted as "extra"), then
    "badge." the same way. Something that happens twice the same way
    happens a third time.

    So the C# file is now READ and the two lists are compared. A single
    source (writing the list into the generated file, say) would be
    cleaner, but it would make the test depend on the generation - the
    test would then CONFORM to what we generate instead of verifying it.
    """
    tests = os.path.join(ROOT, "tests", "Lokanta.Core.Tests", "LocTests.cs")
    if not os.path.exists(tests):
        return                              # no test, no check

    src = io.open(tests, encoding="utf-8").read()

    # ONLY that block is scanned. Scanning the whole file was wrong: an
    # EndsWith("Key") somewhere else in the test was taken for a family
    # and the check lit itself red.
    start = src.find("List<string> orphan")
    end = src.find(".OrderBy", start)
    if start < 0 or end < 0:
        print("orphan block not found in LocTests.cs - the check cannot run")
        sys.exit(1)
    block = src[start:end]

    csharp = set(re.findall(r'StartsWith\("([^"]+)"\)', block))
    csharp |= set(re.findall(r'EndsWith\("([^"]+)"\)', block))

    # BOTH SIDES COME FROM SOURCE, BUT THE PYTHON SIDE IS VERIFIED BY
    # BEHAVIOUR.
    #
    # There used to be a hand-written list of families here, and it was
    # the THIRD copy of the family list: when ".voice" was added the
    # check raised a FALSE alarm ("not in LocTests.cs") - what was
    # missing was its own probe list.
    #
    # Then I changed it to read the list from source, and this time it
    # became a TEXTUAL proxy: it asked "does the source say
    # endswith(\".voice\")", not "is SCREEN_KEY(\"x.voice\") true". A
    # change that leaves a pattern inert (an early return, a short
    # circuit that moves) would have left the text where it was.
    #
    # Now it is both: the families are extracted from source, then each
    # one is ASKED of SCREEN_KEY. If the text is there but the behaviour
    # is not, the family does not join the "here" set and the drift is
    # reported.
    own = io.open(os.path.abspath(__file__), encoding="utf-8").read()
    fstart = own.find("def SCREEN_KEY")
    fend = own.find(chr(10) + "def ", fstart + 1)
    fblock = own[fstart:fend if fend > 0 else len(own)]
    bstart = fblock.find("return (")
    if fstart < 0 or bstart < 0:
        print("SCREEN_KEY body not found in gen_loc.py - the check cannot run")
        sys.exit(1)
    fblock = fblock[bstart:]

    in_text = set(re.findall(r'startswith\("([^"]+)"\)', fblock))
    in_text |= set(re.findall(r'endswith\("([^"]+)"\)', fblock))

    # BEHAVIOUR PROBE: is every family named in the text REALLY exempt?
    here = set()
    for family in in_text:
        sample = (family + "x") if family.endswith(".") else ("x" + family)
        if SCREEN_KEY(sample):
            here.add(family)
        else:
            print("")
            print("--- SCREEN_KEY TEXT AND BEHAVIOUR HAVE DRIFTED ---")
            print("  written in the source but inert:", family)
            sys.exit(1)

    if csharp != here:
        missing = here - csharp
        extra = csharp - here
        print("")
        print("--- SCREEN FAMILIES HAVE DRIFTED ---")
        if missing:
            print("  not in LocTests.cs :", ", ".join(sorted(missing)))
        if extra:
            print("  not in gen_loc.py  :", ", ".join(sorted(extra)))
        print("  The two must match; if one changes the other must too.")
        sys.exit(1)


def required_keys():
    """Every string key the content asks for."""
    keys = set()

    def walk(o):
        if isinstance(o, dict):
            for k, v in o.items():
                if k.endswith("Key") and isinstance(v, str):
                    keys.add(v)
                walk(v)
        elif isinstance(o, list):
            for v in o:
                walk(v)

    for root, _, files in os.walk(CONTENT):
        if os.path.basename(root) == "loc":
            continue
        for f in files:
            if f.endswith(".json"):
                walk(json.load(io.open(os.path.join(root, f), encoding="utf-8")))
    return keys


def _language_sources():
    """The files the tables actually LIVE IN: each language module and
    its _ui partner."""
    out = []
    for mod in LANGUAGES:
        path = os.path.abspath(mod.__file__)
        out.append(path)
        ui = path[:-3] + "_ui.py"
        if os.path.exists(ui):
            out.append(ui)
    return out


def _no_duplicates():
    """Catches DUPLICATE dictionary keys in the source.

    If the same key is written twice in a Python dictionary, Python
    takes the LAST value and silently drops the first. This tool caught
    a missing key, blank text and extra keys, but could not see a
    duplicate - because by the time the file is loaded into Python the
    duplicate is already gone.

    Measured: `ui.evening.wages` ("Ucret" / "Maas odendi" - "Wage" /
    "Wages paid") and `ui.evening.rent` ("Kira" / "Kira odendi" - "Rent" /
    "Rent paid") had each been written twice; the first text of each never
    reached the game. Silently lost text is more dangerous than missing
    text: the check stays green.

    So it is the SOURCE that is scanned, not the dictionary.

    SCOPE: a duplicate is looked for WITHIN THE SAME DICTIONARY, not
    across the whole file. The whole file was scanned at first, and when
    TRAIT_DESC was added it raised twelve false alarms - `TRAITS["sakin"]`
    and `TRAIT_DESC["sakin"]` are separate dictionaries and become
    separate keys (`trait.sakin`, `trait.sakin.desc`). A check that cries
    wolf is an invitation to switch it off; the scope was narrowed.

    WHICH FILES: it used to read `__file__`, because the Turkish table
    lived in this generator - so it only ever protected Turkish. Now the
    tables sit in tools/content/languages/ and all ten files are read.
    The measured bug (`ui.evening.wages` written twice) can happen in
    any of the five languages, and in four of them it could not be seen.
    """
    import re as _re

    dup = []
    for path in _language_sources():
        text = io.open(path, encoding="utf-8").read()
        # Dictionary bounds: UPPER_CASE = { ... } starting at column 0
        for block in _re.finditer(r'^[A-Z_][A-Z0-9_]*\s*=\s*\{(.*?)^\}',
                                  text, _re.M | _re.S):
            seen = set()
            for m in _re.finditer(r'^\s*"([a-z0-9_.]+)"\s*:',
                                  block.group(1), _re.M):
                k = m.group(1)
                if k in seen:
                    dup.append(os.path.basename(path) + ": " + k)
                seen.add(k)
    return dup


def write(path, table):
    """Writes the table as JSON. One place instead of the same three
    lines in three places: if the format changes, no language is left
    behind."""
    io.open(path, "w", encoding="utf-8", newline="\n").write(
        json.dumps(table, ensure_ascii=False, indent=2, sort_keys=True) + "\n")


def build_from(mod):
    """Builds a table FROM A LANGUAGE MODULE.

    This function was once called `build_en` and was bound to `loc_en`.
    Five copies for five languages was exactly the pattern that drifted
    silently five times in this project: a new family added to one and
    not to the other. The module comes from outside, the body is one.

    A missing table BLOWS UP here (AttributeError) - and that is right:
    if a language has no "TRAIT_VOICE" table, that language must not be
    generated half-finished, the generation must stop.
    """
    table = {}
    for k, v in mod.INGREDIENTS.items():
        table["ingredient." + k] = v
    for k, v in mod.DISHES.items():
        table["dish." + k] = v
    for k, v in mod.ARCHETYPES.items():
        table["archetype." + k] = v
    for k, v in mod.TRAITS.items():
        table["trait." + k] = v
    for k, v in mod.TRAIT_DESC.items():
        table["trait." + k + ".desc"] = v
    for k, v in mod.TRAIT_VOICE.items():
        table["trait." + k + ".voice"] = v
    for k, v in mod.ROLES.items():
        table["role." + k] = v
    for k, v in mod.STATIONS.items():
        table["station." + k] = v
    for k, v in mod.CUISINES.items():
        table["cuisine." + k] = v
    for k, v in mod.STORAGE.items():
        table["storage." + k] = v
    for rid, (name, job, beats) in mod.REGULARS.items():
        table["regular." + rid + ".name"] = name
        table["regular." + rid + ".job"] = job
        for i, text in enumerate(beats):
            table["regular." + rid + ".beat" + str(i + 1)] = text
    table.update(mod.UI)
    return table


def build_en():
    """Kept for the old name. The body is in build_from."""
    return build_from(loc_en)


def _placeholders(text):
    """HOW MANY TIMES {0}, {1}... appear in the text.

    It used to return a set, and "{0} ... {0}" counted the same as
    "{0}". That is exactly what happened in English: the tenure notice
    printed the staff member's name twice ("The new ones ask for {0}")
    and the gate said nothing. Formatting does not fail - the .NET
    indices are written out - but the text prints the same name twice
    and reads like a bug.
    """
    out = {}
    i = 0
    while True:
        a = text.find("{", i)
        if a < 0:
            return out
        b = text.find("}", a)
        if b < 0:
            return out
        key = text[a:b + 1]
        out[key] = out.get(key, 0) + 1
        i = b + 1


def _format_counts(counts):
    """A readable summary such as {0}x2."""
    if not counts:
        return "-"
    return " ".join(k + ("x%d" % n if n > 1 else "")
                    for k, n in sorted(counts.items()))


def compare(reference, other):
    """
    Verifies that two tables HAVE NOT DRIFTED APART.

    Two things are tested and both of them produce silent errors:

      1. KEY difference - a missing key means a button in the game that
         reads "[ui.staff.hire]".
      2. PLACEHOLDER difference - a string with {0} in the reference and
         none in the translation is NOT a formatting error, it silently
         drops a number (like printing "Day" and never the day number).
    """
    problems = []

    missing = sorted(k for k in reference if k not in other)
    extra = sorted(k for k in other if k not in reference)
    for k in missing:
        problems.append("missing: " + k)
    for k in extra:
        problems.append("extra (not in the reference): " + k)

    for k in sorted(set(reference) & set(other)):
        a, b = _placeholders(reference[k]), _placeholders(other[k])
        if a != b:
            problems.append("placeholder differs: %s  reference=%s  here=%s"
                            % (k, _format_counts(a), _format_counts(b)))

    blank = sorted(k for k, v in other.items() if not v or not v.strip())
    for k in blank:
        problems.append("blank text: " + k)
    return problems


def build():
    _screen_families_must_not_drift()

    dup = _no_duplicates()
    if dup:
        print("DUPLICATE KEY (text disappears silently):")
        for k in sorted(set(dup)):
            print("  " + k)
        sys.exit(1)

    return build_from(loc_tr)


def main():
    table = build()
    needed = required_keys()

    missing = sorted(k for k in needed if k not in table)
    # Interface keys never appear in the content; we do not count them
    # as extra.
    extra = sorted(k for k in table
                   if k not in needed and not SCREEN_KEY(k))

    print("content asks for : %d keys" % len(needed))
    print("in the table     : %d keys (%d interface)"
          % (len(table), sum(1 for k in table if SCREEN_KEY(k))))

    if missing:
        print("")
        print("--- MISSING TEXT (%d) ---" % len(missing))
        for k in missing:
            print("  " + k)
    if extra:
        print("")
        print("--- KEY NOT IN THE CONTENT (%d) ---" % len(extra))
        for k in extra:
            print("  " + k)
    if missing or extra:
        return 1

    # Blank text is sneakier than missing text: the key looks present.
    blank = sorted(k for k, v in table.items() if not v or not v.strip())
    if blank:
        print("")
        print("--- BLANK TEXT (%d) ---" % len(blank))
        for k in blank:
            print("  " + k)
        return 1

    # --- TEXT EMBEDDED IN CODE, AND DEAD KEYS ------------------------------
    #
    # These two checks did not exist for years and the cost of both was
    # measured: the docstring CONFESSED that "about twenty-five interface
    # strings are still embedded in the code" but nothing broke, and the
    # number climbed to 40; and because SCREEN_KEY exempts the whole ui.*
    # family from the extra-key check, 15 dead keys were maintained in
    # two languages and never shown at all.
    embedded = loc_tarama.embedded_texts()
    if embedded:
        print("")
        print("--- INTERFACE TEXT EMBEDDED IN CODE (%d) ---" % len(embedded))
        for path, line, text in embedded:
            print("  %s:%d  %s" % (path, line, text))
        return 1

    turkish = loc_tarama.turkish_strings()
    if turkish:
        print("")
        print("--- TURKISH LITERAL IN THE INTERFACE (%d) ---" % len(turkish))
        for path, line, text in turkish:
            print("  %s:%d  %s" % (path, line, text))
        return 1

    dead = loc_tarama.dead_keys(table)
    if dead:
        print("")
        print("--- KEY NO SCREEN ASKS FOR (%d) ---" % len(dead))
        for k in dead:
            print("  " + k)
        return 1

    # --- SECOND LANGUAGE ---------------------------------------------------
    en = build_en()
    drift = compare(table, en)
    if drift:
        print("")
        print("--- LANGUAGES HAVE DRIFTED (%d) ---" % len(drift))
        for x in drift:
            print("  " + x)
        return 1

    # --- EXTRA LANGUAGES ---------------------------------------------------
    #
    # Each one goes through THE SAME comparison against the Turkish
    # table. A missing or extra key STOPS the generation: half a language
    # means a button in the game that reads "[ui.staff.hire]".
    extras = []
    for code, module in EXTRA:
        t = build_from(module)
        diff = compare(table, t)
        if diff:
            print("")
            print("--- %s HAS DRIFTED (%d) ---" % (code.upper(), len(diff)))
            for x in diff[:20]:
                print("  " + x)
            return 1
        extras.append((code, t))

    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    write(OUT, table)
    write(OUT_EN, en)
    for code, t in extras:
        write(os.path.join(CONTENT, "loc", code + ".json"), t)

    languages = ["tr", "en"] + [k for k, _ in extras]
    print("")
    print("written: %d keys x %d languages (%s)"
          % (len(table), len(languages), ", ".join(languages)))
    print("all strings complete.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
