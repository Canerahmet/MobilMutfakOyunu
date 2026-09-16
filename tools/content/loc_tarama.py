# -*- coding: utf-8 -*-
"""Is the interface text in the code or in the table — and does the
table hold DEAD keys?

Two checks, both run from inside `gen_loc.py`.

**1. Interface text embedded in the code.** `gen_loc.py`'s own docstring
*confessed* this for a year ("about twenty-five interface strings are
still embedded in the code") but no check broke; the number climbed from
25 to 40 and the cuisine-picking screen stayed entirely Turkish — someone
starting the game in English read Turkish **on the first screen**. A
known debt with no check on it is not a debt, it is a leak.

**2. Dead `ui.*` keys.** `SCREEN_KEY` exempts the whole `ui.*` family
from the "extra key" check — which is right, because the content does
not ask for them. But that exemption also made keys no screen asks for
invisible: text maintained in two languages, translated, never shown.

Both are SOURCE SCANS, so both are approximate. To avoid false alarms
both are kept narrow: the first looks only at the first argument of the
calls that build text, the second only at prefixes known not to be
assembled dynamically.
"""
from __future__ import print_function

import io
import os
import re

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
GAME = os.path.join(ROOT, "unity", "Assets", "Lokanta", "Game")

# The calls that BUILD text. If the first argument is a literal string
# containing a letter, that text appears on screen and belongs in the
# table.
BUILDER = re.compile(
    r'\b(?:Theme\.(?:Text|Btn|Title|Head|Field|Small)|Toast)\s*\(\s*"([^"]*)"')

# Does it contain a letter (things like "%", "—", "{0}" alone are not
# text).
LETTER = re.compile(r"[A-Za-zÇĞİÖŞÜçğıöşü]")

# EXCEPTION: literals that are NOT text. Both appear on screen but
# neither is something to translate.
NOT_TEXT = {
    "", " ", "—", "-", "·", "›", "×", "−", "+", "%", "¤", "…",
}

# Key prefixes that are assembled dynamically: the full key is never
# written in the code, it is put together piece by piece.
DYNAMIC = (
    "ui.phase.", "ui.end.plaque", "station.", "cuisine.", "role.",
    "trait.", "dish.", "ingredient.", "archetype.", "storage.",
    "regular.", "notice.", "score.", "ui.service.done_",
)


def _sources():
    for base, _dirs, files in os.walk(GAME):
        for f in files:
            if f.endswith(".cs"):
                yield os.path.join(base, f)


def embedded_texts():
    """Interface text left embedded in the code: [(path, line, text)]."""
    found = []
    for path in _sources():
        lines = io.open(path, encoding="utf-8").read().split("\n")
        for i, line in enumerate(lines):
            code = line.split("//")[0]
            for m in BUILDER.finditer(code):
                text = m.group(1)
                if text in NOT_TEXT:
                    continue
                if not LETTER.search(text):
                    continue
                found.append((os.path.relpath(path, ROOT), i + 1, text))
    return found


# LETTERS PECULIAR TO TURKISH. If a string carries one of these it was
# written in Turkish, and there is no right place for that in the
# interface folder - the text belongs in the table.
TURKISH = re.compile(u"[çğıöşü"
                     u"ÇĞİÖŞÜ]")

STRING = re.compile(r'"([^"\\]*)"')

UI_DIR = os.path.join(GAME, "Ui")


def turkish_strings():
    """Literal strings in the Ui folder carrying a Turkish-only letter.

    SEPARATE from the BUILDER scan, because that one only looks at the
    FIRST argument of `Theme.Text/Btn/...` calls. Text is very often
    assembled piece by piece:

        "Soğuk hava kademe " + App.Sim.StorageTier
        d.UnlockDay + ". günde açılıyor"

    ("Cold storage tier " + tier; tier + " opens on day ".)

    Both are Turkish that appears on screen and both were escaping that
    scan. The Turkish-letter test is blunt but cheap, and it raises no
    false alarms: no literal string in the interface folder carrying a
    Turkish-only letter is legitimate.
    """
    found = []
    for base, _dirs, files in os.walk(UI_DIR):
        for f in sorted(files):
            if not f.endswith(".cs"):
                continue
            path = os.path.join(base, f)
            lines = io.open(path, encoding="utf-8").read().split(chr(10))
            for i, line in enumerate(lines):
                if line.lstrip().startswith("//"):
                    continue
                code = line.split("//")[0]
                for m in STRING.finditer(code):
                    if TURKISH.search(m.group(1)):
                        found.append((os.path.relpath(path, ROOT), i + 1,
                                      m.group(1)))
    return found


def used_keys():
    """Strings that appear in the code AS A KEY.

    `Loc.T("...")` is not enough: keys sit not only at the call site but
    in tables and definition arrays too - `new Hint("servis",
    "ui.hint.service")`, for instance. A scan that does not see those
    reports a live key as "dead", and that makes the check itself
    untrustworthy.

    So the test is simple: does the key's FULL FORM appear anywhere in
    the source tree as a string literal?
    """
    literal = re.compile(r'"([A-Za-z][A-Za-z0-9_.]*\.[A-Za-z0-9_.]+)"')
    found = set()
    for path in _sources():
        text = io.open(path, encoding="utf-8").read()
        found.update(literal.findall(text))
    return found


def dead_keys(table):
    """ui.* keys that are in the table but that no screen asks for."""
    used = used_keys()
    dead = []
    for k in table:
        if not k.startswith("ui."):
            continue
        if k in used:
            continue
        if any(k.startswith(p) for p in DYNAMIC):
            continue
        dead.append(k)
    return sorted(dead)
