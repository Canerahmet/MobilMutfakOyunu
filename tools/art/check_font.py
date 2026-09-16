# -*- coding: utf-8 -*-
"""Font coverage auditor.

Checks whether EVERY character the game can put on screen has a glyph in the
font. A character that is missing shows up to the player as an empty box, and
that is the kind of fault that gets noticed after release.

Where it looks:
  - content/loc/tr.json           (all the interface text)
  - content/*.json                (dish, ingredient, archetype and regular names)
  - unity/Assets/Lokanta/Game/**  (strings embedded in the code)

Why the embedded strings too: the localisation table does not carry every
piece of text - some strings, like "Patronsun, asci degil.", are written
straight into the screen code. If the auditor does not scan both, it cannot
find what is missing.

Usage:
    python tools/art/check_font.py
    python tools/art/check_font.py --font vendor/rubik/Rubik-wght.ttf

Exit code 0 clean, 1 there are missing characters.
"""
from __future__ import print_function

import argparse
import io
import json
import os
import re
import struct
import sys
import unicodedata

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
# THE FILE BEING AUDITED MUST BE THE VERY ONE THAT GOES INTO THE GAME.
#
# The download copy under vendor/ used to be the one scanned. The two are the
# same today, but there was no guarantee they would STAY the same: updating
# one and forgetting the other means an audit that says "all covered" and a
# game with missing characters - and because the audit is green, nobody looks.
DEFAULT_FONT = os.path.join(
    ROOT, "unity", "Assets", "Lokanta", "Art", "Fonts", "Rubik.ttf")
VENDOR_FONT = os.path.join(ROOT, "vendor", "rubik", "Rubik-wght.ttf")

# CHINESE IS A SEPARATE FONT.
#
# Rubik carries Latin, Cyrillic, Hebrew and ARABIC (including the shaping
# tables) but not CJK - it failed to cover 765 characters in the Chinese
# table. Noto Sans SC (SIL OFL 1.1) was added, subset down to the characters
# the game USES: 10.5 MB -> 223 KB.
CJK_FONT = os.path.join(
    ROOT, "unity", "Assets", "Lokanta", "Art", "Fonts", "NotoSansSC-Lokanta.ttf")

# WHICH FONT DRAWS WHICH FILE.
#
# At first there was a single font and every string was asked of it. With two
# fonts, "it is fine if either one has it" would be WRONG: the fact that the
# Chinese font contains a Turkish letter does not mean the Turkish screen will
# be drawn with the Chinese font. Every file is compared against the font that
# will REALLY draw it in the game.
def _is_cjk(ch):
    """Is it from a CJK block. The game draws these with a separate font."""
    cp = ord(ch)
    return (0x3000 <= cp <= 0x303F      # CJK punctuation
            or 0x3400 <= cp <= 0x4DBF   # extension A
            or 0x4E00 <= cp <= 0x9FFF   # common ideographs
            or 0xF900 <= cp <= 0xFAFF   # compatibility
            or 0xFF00 <= cp <= 0xFFEF)  # full-width forms


# THE SAME table as Loc.PersonName. Whoever changes one has to change both;
# the audit already shows this (the subset comes out incomplete).
FOLDING = {
    u"ğ": u"g", u"Ğ": u"G",
    u"ı": u"i", u"İ": u"I",
    u"ş": u"s", u"Ş": u"S",
}


def _is_arabic(ch):
    cp = ord(ch)
    return 0x0600 <= cp <= 0x06FF or 0x0750 <= cp <= 0x077F         or 0xFB50 <= cp <= 0xFDFF or 0xFE70 <= cp <= 0xFEFF


def _font_for(path):
    return CJK_FONT if os.path.basename(path) == "zh.json" else None


def _same_bytes(a, b):
    """Are the two files identical. If not, one of them is stale."""
    try:
        with open(a, "rb") as fa, open(b, "rb") as fb:
            return fa.read() == fb.read()
    except IOError:
        return None

def _control(ch):
    """Is it a control character that does not appear on screen.

    The save format separates its fields with U+001F (SaveStore). That
    character is in the file, not on the screen - counting it as missing would
    be a false alarm.
    """
    return ord(ch) < 0x20 or 0x7F <= ord(ch) <= 0x9F


# ---------------------------------------------------------------------------
def font_codepoints(path):
    """Reads the covered ranges from the TTF's cmap format 4 table."""
    data = open(path, "rb").read()
    num = struct.unpack(">H", data[4:6])[0]

    tables = {}
    for i in range(num):
        off = 12 + i * 16
        tag = data[off:off + 4].decode("latin1")
        o, ln = struct.unpack(">II", data[off + 8:off + 16])
        tables[tag] = (o, ln)

    if "cmap" not in tables:
        raise SystemExit("no cmap table: " + path)

    co = tables["cmap"][0]
    ntab = struct.unpack(">H", data[co + 2:co + 4])[0]

    ranges = []
    for i in range(ntab):
        pid, _eid, off = struct.unpack(">HHI", data[co + 4 + i * 8:co + 12 + i * 8])
        sub = co + off
        fmt = struct.unpack(">H", data[sub:sub + 2])[0]

        if fmt == 4 and pid == 3:
            segx2 = struct.unpack(">H", data[sub + 6:sub + 8])[0]
            seg = segx2 // 2
            ends = struct.unpack(">%dH" % seg, data[sub + 14:sub + 14 + segx2])
            starts = struct.unpack(">%dH" % seg,
                                   data[sub + 16 + segx2:sub + 16 + 2 * segx2])
            ranges.extend(zip(starts, ends))

        elif fmt == 12:
            n = struct.unpack(">I", data[sub + 12:sub + 16])[0]
            for g in range(n):
                o = sub + 16 + g * 12
                s, e, _gid = struct.unpack(">III", data[o:o + 12])
                ranges.append((s, e))

    if not ranges:
        raise SystemExit("no readable cmap subtable: " + path)
    return ranges


def covered(ranges, cp):
    for s, e in ranges:
        if s <= cp <= e:
            return True
    return False


# ---------------------------------------------------------------------------
# C# string constants. Escape sequences are decoded: a source that writes
# a U+00E7 escape shows a c-cedilla on screen, and the auditor has to see that too.
CS_STRING = re.compile(r'"((?:[^"\\\n]|\\.)*)"')
CS_ESCAPE = re.compile(r'\\u([0-9A-Fa-f]{4})|\\(.)')


def unescape(s):
    def sub(m):
        if m.group(1):
            return chr(int(m.group(1), 16))
        return {"n": "\n", "t": "\t", "r": "\r",
                '"': '"', "\\": "\\"}.get(m.group(2), m.group(2))
    return CS_ESCAPE.sub(sub, s)


def scan_csharp(path, seen):
    src = io.open(path, encoding="utf-8").read()

    # Drop the comment lines: a character in a comment never reaches the screen.
    src = re.sub(r"//[^\n]*", "", src)
    src = re.sub(r"/\*.*?\*/", "", src, flags=re.S)

    for m in CS_STRING.finditer(src):
        text = unescape(m.group(1))
        for ch in text:
            seen.setdefault(ch, path)


def scan_json(path, seen):
    try:
        data = json.load(io.open(path, encoding="utf-8"))
    except ValueError as e:
        print("  skipped (broken json): %s - %s" % (path, e))
        return

    def walk(node):
        if isinstance(node, dict):
            for v in node.values():
                walk(v)
        elif isinstance(node, list):
            for v in node:
                walk(v)
        elif isinstance(node, str):
            for ch in node:
                seen.setdefault(ch, path)

    walk(data)


# ---------------------------------------------------------------------------
# WHAT CAN APPEAR ON SCREEN WHILE THE LANGUAGE IS CHINESE.
#
# At first the question asked was "which characters are in the Chinese table",
# and the audit was green. But when the language is Chinese,
# UiRoot.FontForLanguage draws THE WHOLE TREE with this font - not only the
# Chinese text. Three things were therefore going out as empty boxes:
#
#   - Staff names: they are not in the content/names.json localisation table,
#     and sixteen of the ninety-six names (Ayse, Ibrahim, Yagmur, Sila...)
#     were not in the subset at all.
#   - The minus sign U+2212 in the evening report and the bullet U+2022 in the
#     menu: both straight inside C#, in no table at all.
#   - The Arabic button in the language picker: that is a separate problem and
#     it was solved IN THE CODE (Rubik is supplied locally), because Noto Sans
#     SC does not carry Arabic - it cannot be added to the subset.
#
# The set is defined HERE, in one place: subset_font.py generates the subset
# from it and this auditor looks for the same set. Two separate lists would
# have diverged, and divergence again means empty boxes.
def cjk_characters():
    seen = {}
    zh = os.path.join(ROOT, "content", "loc", "zh.json")
    if os.path.exists(zh):
        scan_json(zh, seen)
    names_path = os.path.join(ROOT, "content", "names.json")
    if os.path.exists(names_path):
        scan_json(names_path, seen)
    game = os.path.join(ROOT, "unity", "Assets", "Lokanta", "Game")
    for base, _dirs, files in os.walk(game):
        for name in sorted(files):
            if name.endswith(".cs"):
                scan_csharp(os.path.join(base, name), seen)
    # ARABIC LETTERS ARE NOT LOOKED FOR IN THIS POOL.
    #
    # They enter the pool from exactly one place: the Arabic language name
    # inside Loc.LanguageNames, that is, the button in the language picker.
    # That button is drawn LOCALLY with Rubik in MenuScreens, because Noto
    # Sans SC does not carry Arabic and it cannot be added to the subset - you
    # cannot subset a glyph that does not exist.
    #
    # So this is not an excuse but an exception with a counterpart in the
    # code: if the exception is removed the button becomes an empty box, and
    # the language-picker check in the tour will see it.
    # THE AUDIT DOES WHATEVER THE CODE DOES.
    #
    # When the language is Chinese, Loc.PersonName folds these five letters
    # from Latin Extended-A, because they are not in Noto Sans SC and they are
    # not in the SOURCE font either - they cannot be solved by adding them to
    # the subset. If the folding is not applied here as well, the audit
    # reports a shortfall that can never be resolved.
    #
    # It is applied to the WHOLE pool: these letters enter the pool from two
    # places - staff names (which are folded) and the single-letter strings in
    # the tour's file-name helper (which never reach the screen). Both end up
    # in the same place once folded.
    return sorted({FOLDING.get(ch, ch) for ch in seen
                   if not _control(ch) and not _is_arabic(ch)})


# ---------------------------------------------------------------------------
def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--font", default=DEFAULT_FONT)
    args = ap.parse_args()

    if not os.path.exists(args.font):
        raise SystemExit("no such font: " + args.font)

    ranges = font_codepoints(args.font)
    seen = {}

    # CHINESE IS COLLECTED SEPARATELY: it will be compared against a separate font.
    seen_cjk = {}

    loc = os.path.join(ROOT, "content", "loc")
    if os.path.isdir(loc):
        for name in sorted(os.listdir(loc)):
            if not name.endswith(".json"):
                continue
            path = os.path.join(loc, name)
            scan_json(path, seen_cjk if _font_for(path) else seen)

    content = os.path.join(ROOT, "content")
    for base, _dirs, files in os.walk(content):
        for name in sorted(files):
            if not name.endswith(".json"):
                continue
            path = os.path.join(base, name)
            # THIS WALK GOES THROUGH THE loc/ FOLDER TOO.
            #
            # It has already been routed file by file above; if it is scanned
            # again here the Chinese text enters the Rubik pool A SECOND TIME
            # and gets reported as "missing" - even though its own font has
            # it. That is exactly what happened in the first version.
            scan_json(path, seen_cjk if _font_for(path) else seen)

    game = os.path.join(ROOT, "unity", "Assets", "Lokanta", "Game")
    for base, _dirs, files in os.walk(game):
        for name in sorted(files):
            if name.endswith(".cs"):
                scan_csharp(os.path.join(base, name), seen)

    # CJK CHARACTERS EMBEDDED IN THE CODE GO TO THE CJK FONT AS WELL.
    #
    # `Loc.LanguageNames` carries the language names in their own scripts, and
    # "中文" is a plain C# string there. In the game that button is drawn with
    # the CJK font (MenuScreens: the language picker writes each language in
    # the font it can be read in), so asking Rubik would be the wrong question.
    #
    # The rule is NARROW: only the CJK block. A Turkish letter being embedded
    # in the code does not move it to the CJK font.
    for ch in [c for c in seen if _is_cjk(c)]:
        seen_cjk.setdefault(ch, seen.pop(ch))

    # THE CHINESE POOL ALSO CONTAINS LANGUAGE-INDEPENDENT TEXT.
    #
    # Because staff names and the symbols embedded in the code reach the
    # screen whatever the language is, this font draws them too when the
    # language is Chinese. One source: cjk_characters().
    for ch in cjk_characters():
        seen_cjk.setdefault(ch, "language-independent")

    missing = []
    for ch, where in sorted(seen.items()):
        if _control(ch):
            continue
        if not covered(ranges, ord(ch)):
            missing.append((ch, where))

    # --- THE SECOND FONT: CHINESE -----------------------------------------
    if seen_cjk:
        if not os.path.exists(CJK_FONT):
            print("no Chinese font: " + os.path.relpath(CJK_FONT, ROOT))
            return 1
        cjk_ranges = font_codepoints(CJK_FONT)
        for ch, where in sorted(seen_cjk.items()):
            if _control(ch):
                continue
            if not covered(cjk_ranges, ord(ch)):
                missing.append((ch, where))
        print("chinese   : %s (%d unique characters)"
              % (os.path.relpath(CJK_FONT, ROOT), len(seen_cjk)))

    print("font      : %s" % os.path.relpath(args.font, ROOT))
    print("scanned   : %d unique characters" % len(seen))

    if not missing:
        # HAVE the download copy and the copy that goes into the game DIVERGED.
        same = _same_bytes(args.font, VENDOR_FONT)
        if same is False:
            print("result    : all covered BUT the vendor/ copy is DIFFERENT")
            print("            %s" % VENDOR_FONT)
            print("            One was updated and the other forgotten; make them equal.")
            return 1

        print("result    : all covered")
        return 0

    print("result    : %d characters MISSING" % len(missing))
    for ch, where in missing:
        try:
            name = unicodedata.name(ch)
        except ValueError:
            name = "?"
        print("  U+%04X  %-34s  %s" % (ord(ch), name, os.path.relpath(where, ROOT)))
    return 1


if __name__ == "__main__":
    sys.exit(main())
