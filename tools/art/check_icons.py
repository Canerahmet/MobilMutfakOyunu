# -*- coding: utf-8 -*-
"""Does the icon have a picture in it?

WHY THIS EXISTS. docs/21's release audit listed the store icon under
"Ready", with the evidence "512x512 RGBA, 5 KB". Every one of those facts
was true. The file was a UNIFORM GREY SQUARE - 262,144 pixels, one colour,
and 5 KB is what a blank square compresses to.

The cause was not the generator. `IconShot.Run` builds a table, four
chairs, a seated guest and a plate, lights them and renders; the code is
careful and its comments argue every decision. It was run through
`tools/unity/run.ps1`, which passes `-nographics` (run.ps1:38). That flag
turns rendering OFF. `tools/unity/shot.ps1` exists for exactly this reason
and says so in its own header - the icon was simply run through the wrong
one, and nothing afterwards opened the file.

So this check does the one thing the audit did not: it looks at the
pixels. It is the smallest possible answer to CLAUDE.md rule 4 - measure
the mechanic, not a proxy for it. Dimensions, colour mode and byte count
are all proxies for "there is a picture here", and all three passed.

WHAT IT REQUIRES, and why each one:

  distinct colours >= 64   A blank square has 1. A gradient has many. The
                           low bar is deliberate: this check's job is to
                           catch NOTHING, not to have opinions about art.
  no colour over 92%       A near-blank square with a few stray pixels
                           would clear the first bar. This one says the
                           ground cannot be the whole image.
  the middle is not flat   Android crops an adaptive icon to a circle, a
                           square or a teardrop and only the middle ~66%
                           survives. An icon whose subject missed the safe
                           area passes the first two bars and still ships
                           as a blank circle on the player's home screen.

The store icon must also be FULLY OPAQUE: Play refuses a 512 icon with
transparency, and the generator's own comment (IconShot.cs:70-80) records
that this was got wrong once already - the anti-aliased edge left pixels
at alpha 205 and the listing would have been rejected for them.
"""
import io
import os
import struct
import sys
import zlib
from collections import Counter

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
ICONS = os.path.join(ROOT, "unity", "Assets", "Lokanta", "Art", "Icons")

# name, required size, must be fully opaque
WANTED = [
    ("app-icon.png", 1024, False),
    ("store-icon-512.png", 512, True),
]

MIN_COLOURS = 64
MAX_SHARE = 0.92
# The adaptive mask keeps the middle 66%; this is the box it is measured in.
SAFE = 0.66


def read_png(path):
    """Decodes an 8-bit RGB/RGBA PNG. Returns (width, height, channels, pixels).

    Written by hand because the project has no image dependency and adding
    one for a check is a poor trade. It handles the five filter types and
    nothing else - if a PNG ever arrives here interlaced or 16-bit, this
    raises rather than guessing, which is the right failure.
    """
    data = io.open(path, "rb").read()
    if data[:8] != b"\x89PNG\r\n\x1a\n":
        raise ValueError("not a PNG: " + path)
    i = 8
    idat = b""
    w = h = depth = colour = None
    while i < len(data):
        (length,) = struct.unpack(">I", data[i:i + 4])
        kind = data[i + 4:i + 8]
        chunk = data[i + 8:i + 8 + length]
        i += 12 + length
        if kind == b"IHDR":
            w, h, depth, colour, _, _, interlace = struct.unpack(">IIBBBBB", chunk[:13])
            if depth != 8:
                raise ValueError("%s: %d-bit PNG, this reader handles 8" % (path, depth))
            if interlace:
                raise ValueError(path + ": interlaced PNG")
        elif kind == b"IDAT":
            idat += chunk
        elif kind == b"IEND":
            break
    channels = {0: 1, 2: 3, 3: 1, 4: 2, 6: 4}[colour]
    raw = zlib.decompress(idat)
    stride = w * channels
    out = bytearray(w * h * channels)
    prev = bytearray(stride)
    p = 0
    for y in range(h):
        f = raw[p]
        p += 1
        line = bytearray(raw[p:p + stride])
        p += stride
        if f == 1:
            for x in range(channels, stride):
                line[x] = (line[x] + line[x - channels]) & 255
        elif f == 2:
            for x in range(stride):
                line[x] = (line[x] + prev[x]) & 255
        elif f == 3:
            for x in range(stride):
                a = line[x - channels] if x >= channels else 0
                line[x] = (line[x] + ((a + prev[x]) >> 1)) & 255
        elif f == 4:
            for x in range(stride):
                a = line[x - channels] if x >= channels else 0
                c = prev[x - channels] if x >= channels else 0
                b = prev[x]
                pa, pb, pc = abs(b - c), abs(a - c), abs(a + b - 2 * c)
                pr = a if (pa <= pb and pa <= pc) else (b if pb <= pc else c)
                line[x] = (line[x] + pr) & 255
        elif f != 0:
            raise ValueError("%s: unknown filter %d" % (path, f))
        out[y * stride:(y + 1) * stride] = line
        prev = line
    return w, h, channels, bytes(out)


def colours(w, h, ch, px, box=None):
    """Counts distinct colours, optionally inside a centred box (0..1)."""
    if box is None:
        x0, x1, y0, y1 = 0, w, 0, h
    else:
        m = (1.0 - box) / 2.0
        x0, x1 = int(w * m), int(w * (1.0 - m))
        y0, y1 = int(h * m), int(h * (1.0 - m))
    c = Counter()
    for y in range(y0, y1):
        base = y * w * ch
        for x in range(x0, x1):
            i = base + x * ch
            c[(px[i], px[i + 1], px[i + 2])] += 1
    return c


def main():
    problems = []
    for name, size, opaque in WANTED:
        path = os.path.join(ICONS, name)
        rel = os.path.relpath(path, ROOT).replace("\\", "/")
        if not os.path.exists(path):
            problems.append(rel + ": missing")
            continue

        w, h, ch, px = read_png(path)
        if (w, h) != (size, size):
            problems.append("%s: %dx%d, expected %dx%d" % (rel, w, h, size, size))

        whole = colours(w, h, ch, px)
        top, count = whole.most_common(1)[0]
        share = float(count) / (w * h)
        inner = colours(w, h, ch, px, SAFE)

        print("  %-20s %4dx%-5d %6d colours, most common %-18s %5.1f%%   "
              "middle %d colours"
              % (name, w, h, len(whole), str(top), 100.0 * share, len(inner)))

        if len(whole) < MIN_COLOURS:
            problems.append("%s: %d distinct colours - THERE IS NO PICTURE IN IT "
                            "(the render did not run; see this file's header)"
                            % (rel, len(whole)))
        if share > MAX_SHARE:
            problems.append("%s: one colour fills %.1f%% of it - effectively blank"
                            % (rel, 100.0 * share))
        if len(inner) < MIN_COLOURS:
            problems.append("%s: the middle %d%% holds %d colours - the subject is "
                            "outside the adaptive mask's safe area and the player's "
                            "launcher will crop it away"
                            % (rel, int(SAFE * 100), len(inner)))

        if opaque and ch == 4:
            low = min(px[i + 3] for i in range(0, w * h * ch, ch))
            if low != 255:
                problems.append("%s: lowest alpha is %d - Play refuses a store icon "
                                "with transparency (IconShot.cs:70-80)" % (rel, low))

    if problems:
        print("")
        for p in problems:
            print("  PROBLEM: " + p)
        print("result    : %d icon problem(s)" % len(problems))
        return 1

    print("result    : every icon has a picture in it")
    return 0


if __name__ == "__main__":
    sys.exit(main())
