# -*- coding: utf-8 -*-
"""Makes the store screenshots legal for Play, without re-shooting them.

THE PROBLEM. The tour shoots at the phone frame, 2183 x 983, which is
**2.2208 : 1**. Google Play has a long-standing rule that no side of a
screenshot may be more than twice the other, and 2183 is 217 px over
2 x 983. The listing is rejected at upload.

WHY NOT JUST RE-SHOOT AT 2 : 1. Because the frame is not a picture, it is a
MEASUREMENT. 873 x 393 dp is the phone frame every UI number in
[41-ui-and-venue.md] was taken against - the 48 dp touch floor, the bottom
strip's budget, the layout check's four edges. Re-shooting at a different
aspect changes the layout and invalidates all of it, to fix a container.

WHY NOT CROP. 217 px off the sides takes the ends of the top and bottom
strips with it - the day/rent plate on one side and a verb button on the
other. Cropping a UI screenshot removes UI.

SO IT IS PADDED, AND THE PADDING IS THE IMAGE'S OWN EDGE ROWS. Not a letterbox
bar in some chosen colour: the top row and the bottom row are repeated
outwards. Both of those rows are the flat card of a HUD strip, so the result
reads as the strips being a little taller rather than as bars stuck on - and
there is no colour to choose, which means no colour to get wrong when the
theme changes.

2183 x 1120 = 1.949 : 1. The exact 2 : 1 height is 1091.5, so 1092 would pass
by half a pixel; 1120 leaves a real margin against a rounding rule nobody
published.

Nothing is resampled, so the pixels Play shows are the pixels the game drew.

Run: python tools/store/compose.py
"""
from __future__ import print_function

import binascii
import io
import os
import struct
import sys
import zlib

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
STORE = os.path.join(ROOT, "render", "store")

# The folder the composed images go into, beside the raw ones. Kept separate so
# the raw frames stay exactly as the tour produced them - they are the record
# the UI measurements were taken from, and overwriting them in place would lose
# it.
OUT = "play"

TARGET_HEIGHT = 1120
MAX_RATIO = 2.0


def _utf8_stdout():
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except AttributeError:
        pass


def read_png(path):
    """Decodes an 8-bit PNG to (width, height, channels, rows of bytes).

    Hand-written for the same reason as tools/art/gen_crowd.py and
    tools/art/check_icons.py: this repository has no image dependency, and a
    build tool that needs `pip install` is one that stops working on a fresh
    clone.
    """
    data = io.open(path, "rb").read()
    if data[:8] != b"\x89PNG\r\n\x1a\n":
        raise SystemExit("%s is not a PNG" % path)

    w, h = struct.unpack(">II", data[16:24])
    depth, colour = data[24], data[25]
    channels = {0: 1, 2: 3, 4: 2, 6: 4}.get(colour)
    if depth != 8 or channels is None:
        raise SystemExit("%s is %d-bit colour type %d; this tool handles 8-bit "
                         "grey/RGB/RGBA" % (path, depth, colour))

    idat = b""
    i = 8
    while i < len(data):
        length = struct.unpack(">I", data[i:i + 4])[0]
        if data[i + 4:i + 8] == b"IDAT":
            idat += data[i + 8:i + 8 + length]
        i += 12 + length

    raw = zlib.decompress(idat)
    stride = w * channels
    rows = []
    prev = bytearray(stride)
    pos = 0
    for _ in range(h):
        f = raw[pos]
        pos += 1
        line = raw[pos:pos + stride]
        pos += stride
        cur = bytearray(stride)
        for x in range(stride):
            a = cur[x - channels] if x >= channels else 0
            b = prev[x]
            c = prev[x - channels] if x >= channels else 0
            v = line[x]
            if f == 0:
                cur[x] = v
            elif f == 1:
                cur[x] = (v + a) & 255
            elif f == 2:
                cur[x] = (v + b) & 255
            elif f == 3:
                cur[x] = (v + ((a + b) >> 1)) & 255
            elif f == 4:
                p = a + b - c
                pa, pb, pc = abs(p - a), abs(p - b), abs(p - c)
                pr = a if (pa <= pb and pa <= pc) else (b if pb <= pc else c)
                cur[x] = (v + pr) & 255
            else:
                raise SystemExit("unknown PNG filter %d in %s" % (f, path))
        prev = cur
        rows.append(cur)
    return w, h, channels, rows


def _chunk(kind, body):
    return (struct.pack(">I", len(body)) + kind + body
            + struct.pack(">I", binascii.crc32(kind + body) & 0xFFFFFFFF))


def write_png(path, w, channels, rows):
    colour = {1: 0, 2: 4, 3: 2, 4: 6}[channels]
    raw = bytearray()
    for row in rows:
        raw.append(0)
        raw += row
    out = b"\x89PNG\r\n\x1a\n"
    out += _chunk(b"IHDR",
                  struct.pack(">IIBBBBB", w, len(rows), 8, colour, 0, 0, 0))
    out += _chunk(b"IDAT", zlib.compress(bytes(raw), 9))
    out += _chunk(b"IEND", b"")
    io.open(path, "wb").write(out)


def compose(path, out_path):
    w, h, channels, rows = read_png(path)
    if h >= TARGET_HEIGHT:
        # Already tall enough: written through unchanged rather than skipped.
        #
        # Skipping it would leave a HOLE in the play/ folder, and a listing is
        # uploaded as a set - the one frame missing would be the one nobody
        # noticed was missing. It is re-encoded rather than shrunk, because
        # shrinking would be resampling and this tool does not resample.
        write_png(out_path, w, channels, rows)
        return w, h, h, False

    extra = TARGET_HEIGHT - h
    top = extra // 2
    bottom = extra - top
    padded = ([bytearray(rows[0])] * top + rows
              + [bytearray(rows[-1])] * bottom)
    write_png(out_path, w, channels, padded)
    return w, h, TARGET_HEIGHT, True


def main():
    _utf8_stdout()
    if not os.path.isdir(STORE):
        print("There is no render/store - run the tour with -Store first.")
        return 1

    done = 0
    bad = []
    for base, dirs, files in os.walk(STORE):
        dirs[:] = [d for d in dirs if d != OUT]
        pngs = sorted(f for f in files if f.endswith(".png"))
        if not pngs:
            continue
        out_dir = os.path.join(base, OUT)
        if not os.path.isdir(out_dir):
            os.makedirs(out_dir)

        for name in pngs:
            w, h, new_h, changed = compose(os.path.join(base, name),
                                           os.path.join(out_dir, name))
            ratio = w / float(new_h)
            if ratio > MAX_RATIO:
                bad.append("%s is %.4f : 1 after padding (%d x %d)"
                           % (name, ratio, w, new_h))
            done += 1

        rel = os.path.relpath(out_dir, ROOT).replace("\\", "/")
        print("%-46s %2d images, %d x %d, %.3f : 1"
              % (rel, len(pngs), w, new_h, w / float(new_h)))

    print()
    print("composed    : %d images" % done)
    if bad:
        for row in bad:
            print("STORE       %s" % row)
        print("result      : %d still over %.1f : 1" % (len(bad), MAX_RATIO))
        return 1
    print("result      : every store image is within Play's %.0f : 1 rule"
          % MAX_RATIO)
    return 0


if __name__ == "__main__":
    sys.exit(main())
