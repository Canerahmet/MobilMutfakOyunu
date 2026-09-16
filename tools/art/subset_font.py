# -*- coding: utf-8 -*-
"""Generates the SUBSET of the Chinese font.

Noto Sans SC is 10.5 MB and contains twenty thousand glyphs; the game uses
close to a thousand of them. Putting the whole thing in the package means
adding ten megabytes to the APK.

WHY A TOOL: the first time, the subset was extracted BY HAND and which
characters were wanted was written down nowhere. The result was this - three
things that appear in the game in Chinese never made it into the subset:

  1. STAFF NAMES. They are not in the content/names.json localisation table,
     so the logic that says "scan the Chinese text" never looked at them.
     Sixteen names (Ayse, Ibrahim, Yagmur...) came out as empty boxes.
  2. SYMBOLS EMBEDDED IN THE CODE. The minus sign U+2212 in the evening
     report and the bullet U+2022 in the menu are written straight inside C#.
  3. Those two are LANGUAGE-INDEPENDENT: when the language is Chinese the
     whole tree is drawn with this font, not only the Chinese text.

So the question is not "which characters are in the Chinese text" but WHICH
CHARACTERS CAN APPEAR ON SCREEN WHILE THE LANGUAGE IS CHINESE. The character
set is now measured and the subset is generated from it; check_font.py audits
the same set.

Usage:
    python tools/art/subset_font.py
    python tools/art/subset_font.py --verify    (compare only)

Exit code 0 fine, 1 the subset is out of date (with --verify).
"""
from __future__ import print_function

import argparse
import io
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

import check_font  # noqa: E402

ROOT = check_font.ROOT
SOURCE = os.path.join(ROOT, "vendor", "noto-sans-sc", "NotoSansSC-Regular.ttf")
TARGET = check_font.CJK_FONT
# The file name is an artefact path that is committed to the repository, so it
# stays as it is.
LIST_PATH = os.path.join(HERE, "out", "zh_karakterler.txt")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--verify", action="store_true",
                    help="do not generate, only check whether the subset is sufficient")
    args = ap.parse_args()

    wanted = check_font.cjk_characters()
    print("wanted    : %d characters" % len(wanted))

    if args.verify:
        if not os.path.exists(TARGET):
            print("no subset: " + os.path.relpath(TARGET, ROOT))
            return 1
        ranges = check_font.font_codepoints(TARGET)
        missing = sorted(c for c in wanted
                         if not check_font.covered(ranges, ord(c)))
        if missing:
            print("MISSING   : %d characters -> %s"
                  % (len(missing), " ".join(missing[:40])))
            print("result    : the subset is out of date, run subset_font.py")
            return 1
        print("result    : the subset carries every character wanted")
        return 0

    if not os.path.exists(SOURCE):
        raise SystemExit("no source font: " + SOURCE)

    try:
        from fontTools import subset
    except ImportError:
        raise SystemExit("fonttools is required: pip install fonttools")

    # The text file is NOT DOCUMENTATION but EVIDENCE: what the subset was
    # generated from is thereby kept in the repository, and the difference can
    # be seen by eye.
    io.open(LIST_PATH, "w", encoding="utf-8", newline="").write(
        u"".join(wanted))

    options = subset.Options()
    # The shaping tables STAY. Chinese has no joining, but it does have
    # vertical writing (vert/vrt2) and composed forms (ccmp); throwing those
    # away would shrink the subset and break the text.
    options.layout_features = ["*"]
    options.name_IDs = ["*"]
    options.notdef_outline = True
    options.recalc_bounds = True
    options.drop_tables = []

    font = subset.load_font(SOURCE, options)
    subsetter = subset.Subsetter(options=options)
    subsetter.populate(text=u"".join(wanted))
    subsetter.subset(font)
    subset.save_font(font, TARGET, options)
    font.close()

    source_size = os.path.getsize(SOURCE)
    target_size = os.path.getsize(TARGET)
    print("source    : %s (%d bytes)" % (os.path.relpath(SOURCE, ROOT), source_size))
    print("subset    : %s (%d bytes, %.1f%%)"
          % (os.path.relpath(TARGET, ROOT), target_size,
             100.0 * target_size / source_size))
    return 0


if __name__ == "__main__":
    sys.exit(main())
