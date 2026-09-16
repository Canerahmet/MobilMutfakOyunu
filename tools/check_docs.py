# -*- coding: utf-8 -*-
"""Do the links in the documents go anywhere?

WHY: this repository has fifty-two numbered documents and they are tied
to each other by two hundred-odd links. Moving or renaming a file means
those links break silently - a broken link raises no error, clicking it
simply does nothing.

This makes tidying the folder structure SAFE: move, run, see what broke.
Moving without a tool means reading fifty-two files by eye.

WHAT IT MEASURES:
  - do the relative links and images in README.md and in every .md file
    under docs/ point at a file that exists
  - is every numbered document under docs/ mentioned in docs/README.md
    (a document that falls out of the index is as good as one that does
    not exist)

WHAT IT DOES NOT MEASURE: http(s) links. Going out to the network would
make this check slow and brittle; an outside address being up today does
not mean it will be up tomorrow anyway.

LINKS INSIDE CODE ARE NOT MEASURED EITHER. docs/43 quotes a broken link
ITSELF - that document already says "this link is broken". Counting text
inside code as a link would mean counting a sentence about a bug as a
bug.
"""
from __future__ import print_function

import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# [text](target) and ![text](target)
LINK = re.compile(r"!?\[[^\]]*\]\(([^)]+)\)")
# A numbered document: 02-design-proposal.md
NUMBERED = re.compile(r"^\d\d-.*\.md$")
# A block fenced by ``` and inline code fenced by `
CODE_BLOCK = re.compile(r"```.*?```", re.S)
CODE_INLINE = re.compile("`[^`" + chr(10) + "]*`")


def md_files():
    paths = [os.path.join(ROOT, "README.md")]
    for base, _, files in os.walk(os.path.join(ROOT, "docs")):
        for f in sorted(files):
            if f.endswith(".md"):
                paths.append(os.path.join(base, f))
    return paths


def main():
    broken = []
    count = 0

    for path in md_files():
        s = io.open(path, encoding="utf-8").read()
        s = CODE_BLOCK.sub("", s)
        s = CODE_INLINE.sub("", s)
        folder = os.path.dirname(path)
        for target in LINK.findall(s):
            target = target.strip()
            if target.startswith(("http://", "https://", "mailto:", "#")):
                continue
            # An anchor inside the same file: file.md#section
            f = target.split("#", 1)[0]
            if not f:
                continue
            count += 1
            full = os.path.normpath(os.path.join(folder, f))
            if not os.path.exists(full):
                broken.append("%s -> %s" % (
                    os.path.relpath(path, ROOT).replace("\\", "/"), target))

    # A document the index does not mention
    docs = os.path.join(ROOT, "docs")
    index = io.open(os.path.join(docs, "README.md"), encoding="utf-8").read()
    unlisted = []
    for f in sorted(os.listdir(docs)):
        if NUMBERED.match(f) and f not in index:
            unlisted.append(f)

    print("links    : %d relative links scanned" % count)
    print("documents: %d numbered documents" % len(
        [f for f in os.listdir(docs) if NUMBERED.match(f)]))

    if broken or unlisted:
        print()
        for k in broken:
            print("BROKEN: " + k)
        for a in unlisted:
            print("NOT IN THE INDEX: docs/" + a)
        print("result   : %d problems" % (len(broken) + len(unlisted)))
        sys.exit(1)

    print("result   : every link resolves, every document is in the index")


if __name__ == "__main__":
    main()
