# -*- coding: utf-8 -*-
"""Measures the store listing texts against Play Console's LIMITS.

WHY A TOOL: the texts live in docs/44 and their lengths were written out
BY HAND. Two of them were wrong - one said 60 where it was 61. Being
wrong was harmless there because they are far below the limit; but that
same hand-counting says "79 characters" for a text close to the limit
and the text turns out to be 81. Play will not accept that text, and
that is one of the worst things to find out on submission day.

WHAT IT MEASURES:
  - Short description <= 80 characters (Play's limit)
  - Full description  <= 4000 characters (Play's limit)
  - Does every language HAVE both (if a language is added and the store
    text forgotten, the game opens in five languages and the listing
    shows one)
  - Do the character counts in the table match the REAL lengths

WHY IT READS FROM A FILE: docs/44 is the single source of these texts.
Writing a second copy in here would mean the two drifting apart.
"""
import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
DOCUMENT = os.path.join(ROOT, "docs", "44-store-texts.md")

# Play Console limits.
SHORT_LIMIT = 80
FULL_LIMIT = 4000

# The game's languages; must be the same set as Loc.Languages.
# Code in the table -> heading of the full text.
LANGUAGES = {
    "TR": "Türkçe",
    "EN": "English",
    "ES": "Español",
    "ZH": "简体中文",
    "AR": "العربية",
}


def read():
    if not os.path.exists(DOCUMENT):
        print("store document not found: " + DOCUMENT)
        sys.exit(1)
    return io.open(DOCUMENT, encoding="utf-8").read()


def main():
    s = read()
    errors = []

    # --- short descriptions ----------------------------------------------
    short = {}
    for code, text, claimed in re.findall(
            r"^\| (TR|EN|ES|ZH|AR) \| (.+?) \| (\d+) \|$", s, re.M):
        short[code] = text
        n = len(text)
        if n != int(claimed):
            errors.append("short/%s: the table says %s, the real length is %d"
                          % (code, claimed, n))
        if n > SHORT_LIMIT:
            errors.append("short/%s: %d characters, limit %d"
                          % (code, n, SHORT_LIMIT))

    # --- full descriptions -----------------------------------------------
    full = {}
    for heading, body in re.findall(r"\n### (.+?)\n\n```\n(.*?)\n```", s, re.S):
        full[heading.strip()] = body

    for code, heading in sorted(LANGUAGES.items()):
        if code not in short:
            errors.append("short/%s: missing" % code)
        if heading not in full:
            errors.append("full/%s: no text under the heading '%s'"
                          % (code, heading))
            continue
        n = len(full[heading])
        if n > FULL_LIMIT:
            errors.append("full/%s: %d characters, limit %d"
                          % (code, n, FULL_LIMIT))

    # --- report ------------------------------------------------------------
    print("short description (limit %d):" % SHORT_LIMIT)
    for code in sorted(LANGUAGES):
        if code in short:
            print("  %-3s %4d characters" % (code, len(short[code])))
    print("full description (limit %d):" % FULL_LIMIT)
    for code, heading in sorted(LANGUAGES.items()):
        if heading in full:
            print("  %-3s %4d characters" % (code, len(full[heading])))

    if errors:
        print()
        for e in errors:
            print("ERROR: " + e)
        print("result  : %d problems" % len(errors))
        sys.exit(1)

    print("result  : the store texts for %d languages are within the limits"
          % len(LANGUAGES))


if __name__ == "__main__":
    main()
