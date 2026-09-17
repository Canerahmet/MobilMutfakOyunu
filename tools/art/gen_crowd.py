# -*- coding: utf-8 -*-
"""THE CROWD WEARS THE CUISINE.

The staff get a wardrobe (Wardrobe.cs) and the furniture gets a per-cuisine
material copy (RestaurantView.Decor.Tinted). The customers got neither: the
same twelve stock figures filled a burger bar and a neighbourhood lokanta in
exactly the same clothes, so the one part of the picture that is MOSTLY
PEOPLE said nothing about where you were.

WHY A TEXTURE AND NOT A TINT. All twelve characters share ONE material
(Character_colormap.mat) and one 512x512 texture, so there is no shirt
submesh to paint: a `_BaseColor` tint would multiply the WHOLE figure, skin
included. That is the one thing this project does not do - docs/25 settles
it: identity comes from the environment, the light, the silhouette and the
clothes, NEVER from a face.

WHAT THE TEXTURE ACTUALLY IS. It is an indexed PNG holding a grid of flat
gradient swatches - seventeen of them, 64 px wide and 128 px tall, which the
meshes' UVs point at. So "recolour the clothes and leave the skin alone" is
not image processing at all: it is picking rectangles. The four skin swatches
and the hair/shoe darks are left BYTE-IDENTICAL, and this tool asserts that
rather than trusting it.

THE TWO CROWDS, and the reason is figure/ground, not decoration:

  fast food  the room is cold and dark (wall 0.173, 0.184, 0.212). A crowd
             that is louder and brighter than the room reads off it, and
             "loud" is what a burger bar's crowd is anyway: chroma up, a
             small warm rotation.

  turkish    the room is warm brown wood with copper (wall 0.290, 0.196,
             0.137). Saturated warm clothes would DISAPPEAR into it - the
             same trap the daytime sky fell into (docs/58): a warm subject
             on a warm ground. So the crowd goes washed and lighter and
             turns very slightly cool: separation by VALUE, which survives
             at the 32 dp a figure occupies in the overview, where a hue
             difference does not.

Neither transform touches the hue ORDER, so twelve characters stay twelve
different people in both.

Run: python tools/art/gen_crowd.py
"""
import binascii
import hashlib
import io
import os
import struct
import sys
import zlib

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
TEX = os.path.join(ROOT, "unity", "Assets", "Lokanta", "Art", "Characters",
                   "Textures")
SOURCE = os.path.join(TEX, "colormap.png")

SIZE = 512
SWATCH_W = 64
SWATCH_H = 128


def _utf8_stdout():
    """The swatch table prints hex, but the docstring above does not - and a
    console in cp1252 turns a stray non-ASCII character into a crash that
    hides the finding. tools/check_english.py learned this the hard way."""
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except AttributeError:
        pass


# ---------------------------------------------------------------- the swatches
#
# (name, x, y) of each 64x128 swatch, measured off the source image - see the
# table this tool prints, which is how they were found in the first place.
#
# KEEP means the rectangle is copied through untouched and then CHECKED to be
# untouched. The four skin tones are the pack's four skin tones; the darks are
# hair, shoes and eyes; the white is shirt collars and eye whites, and it has
# nowhere to go.
SWATCHES = [
    ("violet",      0, 128, "dress"),
    ("skin-light",  0, 256, "keep"),
    ("green",      64, 256, "dress"),
    ("yellow",    128, 256, "dress"),
    ("orange",    192, 256, "dress"),
    ("red",       256, 256, "dress"),
    ("blue",      320, 256, "dress"),
    ("pale-blue", 384, 256, "dress"),
    ("purple",    448, 256, "dress"),
    ("slate",       0, 384, "keep"),
    ("grey-blue",  64, 384, "dress"),
    ("grey-dark", 128, 384, "keep"),
    ("grey-pale", 192, 384, "dress"),
    ("white",     256, 384, "keep"),
    ("skin-tan",  320, 384, "keep"),
    ("skin-deep", 384, 384, "keep"),
    ("skin-mid",  448, 384, "keep"),
]

# (chroma, value, hue rotation in degrees) per cuisine, applied to the "dress"
# swatches only.
CUISINE = {
    "fastfood": (1.30, 1.06, +10.0),
    "turk":     (0.62, 1.02, -12.0),
}


# ---------------------------------------------------------------- PNG in / out
def read_png(path):
    """Decodes an 8-bit indexed PNG to a list of (r, g, b) rows.

    Hand-written on purpose: this repository has no image dependency and
    tools/art/check_icons.py already carries the same decoder, for the same
    reason - a build tool that needs `pip install` is a build tool that stops
    working on a fresh clone.
    """
    data = io.open(path, "rb").read()
    if data[:8] != b"\x89PNG\r\n\x1a\n":
        raise SystemExit("%s is not a PNG" % path)

    w, h = struct.unpack(">II", data[16:24])
    depth, colour = data[24], data[25]
    if depth != 8 or colour != 3:
        raise SystemExit("%s is %d-bit colour type %d; expected 8-bit indexed"
                         % (path, depth, colour))

    palette = None
    idat = b""
    i = 8
    while i < len(data):
        length = struct.unpack(">I", data[i:i + 4])[0]
        kind = data[i + 4:i + 8]
        body = data[i + 8:i + 8 + length]
        if kind == b"PLTE":
            palette = body
        elif kind == b"IDAT":
            idat += body
        i += 12 + length
    if palette is None:
        raise SystemExit("%s has no palette" % path)

    raw = zlib.decompress(idat)
    rows = []
    prev = bytearray(w)
    pos = 0
    for _ in range(h):
        f = raw[pos]
        pos += 1
        line = raw[pos:pos + w]
        pos += w
        cur = bytearray(w)
        if f == 0:
            cur[:] = line
        elif f == 1:
            for x in range(w):
                cur[x] = (line[x] + (cur[x - 1] if x else 0)) & 255
        elif f == 2:
            for x in range(w):
                cur[x] = (line[x] + prev[x]) & 255
        elif f == 3:
            for x in range(w):
                cur[x] = (line[x] + (((cur[x - 1] if x else 0) + prev[x]) >> 1)) & 255
        elif f == 4:
            for x in range(w):
                a = cur[x - 1] if x else 0
                b = prev[x]
                c = prev[x - 1] if x else 0
                p = a + b - c
                pa, pb, pc = abs(p - a), abs(p - b), abs(p - c)
                pr = a if (pa <= pb and pa <= pc) else (b if pb <= pc else c)
                cur[x] = (line[x] + pr) & 255
        else:
            raise SystemExit("unknown PNG filter %d" % f)
        prev = cur
        rows.append([tuple(palette[v * 3:v * 3 + 3]) for v in cur])
    return w, h, rows


def _chunk(kind, body):
    return (struct.pack(">I", len(body)) + kind + body
            + struct.pack(">I", binascii.crc32(kind + body) & 0xFFFFFFFF))


def write_png(path, rows):
    """Writes a truecolour PNG.

    The source is indexed and the output is not, because the transform
    produces more than 256 colours across the gradients. It costs nothing that
    matters: these are flat vertical ramps, so zlib takes them down to a few
    tens of kilobytes, and Unity re-compresses the texture on import anyway.
    """
    h = len(rows)
    w = len(rows[0])
    raw = bytearray()
    for row in rows:
        raw.append(0)                       # filter: none
        for r, g, b in row:
            raw += bytes((r, g, b))
    out = b"\x89PNG\r\n\x1a\n"
    out += _chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 2, 0, 0, 0))
    out += _chunk(b"IDAT", zlib.compress(bytes(raw), 9))
    out += _chunk(b"IEND", b"")
    io.open(path, "wb").write(out)


# ---------------------------------------------------------------- the transform
def rgb_to_hsv(r, g, b):
    r, g, b = r / 255.0, g / 255.0, b / 255.0
    hi, lo = max(r, g, b), min(r, g, b)
    v = hi
    d = hi - lo
    s = 0.0 if hi == 0 else d / hi
    if d == 0:
        h = 0.0
    elif hi == r:
        h = 60.0 * (((g - b) / d) % 6.0)
    elif hi == g:
        h = 60.0 * (((b - r) / d) + 2.0)
    else:
        h = 60.0 * (((r - g) / d) + 4.0)
    return h, s, v


def hsv_to_rgb(h, s, v):
    h = h % 360.0
    c = v * s
    x = c * (1.0 - abs((h / 60.0) % 2.0 - 1.0))
    m = v - c
    if h < 60:
        rr, gg, bb = c, x, 0.0
    elif h < 120:
        rr, gg, bb = x, c, 0.0
    elif h < 180:
        rr, gg, bb = 0.0, c, x
    elif h < 240:
        rr, gg, bb = 0.0, x, c
    elif h < 300:
        rr, gg, bb = x, 0.0, c
    else:
        rr, gg, bb = c, 0.0, x
    return (int(round(min(1.0, rr + m) * 255)),
            int(round(min(1.0, gg + m) * 255)),
            int(round(min(1.0, bb + m) * 255)))


def dress(rgb, chroma, value, rotate):
    h, s, v = rgb_to_hsv(*rgb)
    return hsv_to_rgb(h + rotate, min(1.0, s * chroma), min(1.0, v * value))


def hexes(rows, x, y):
    """The swatch's flat tone and the bottom of its ramp, for the table."""
    flat = rows[y + 4][x + 4]
    bottom = rows[y + SWATCH_H - 5][x + SWATCH_W - 5]
    return ("#%02x%02x%02x" % flat, "#%02x%02x%02x" % bottom)


# ---------------------------------------------------------------- the meta file
def write_meta(png_path, source_meta):
    """A Unity .meta beside the new texture, with the SOURCE's import settings.

    Copied rather than written: mipmaps, sRGB, compression and the platform
    overrides all have to match the texture this one stands in for, and a
    hand-written importer block is a silent way to ship a crowd at a different
    filtering from the street.

    The guid is derived from the file name, so re-running this tool does not
    break the scene's reference to the texture it produced last time.
    """
    text = io.open(source_meta, encoding="utf-8").read()
    guid = hashlib.md5(("lokanta/crowd/"
                        + os.path.basename(png_path)).encode("utf-8")).hexdigest()
    out = []
    for line in text.splitlines():
        if line.startswith("guid: "):
            line = "guid: " + guid
        out.append(line)
    io.open(png_path + ".meta", "w", encoding="utf-8",
            newline="\n").write("\n".join(out) + "\n")
    return guid


# ---------------------------------------------------------------- main
def main():
    _utf8_stdout()
    w, h, rows = read_png(SOURCE)
    if (w, h) != (SIZE, SIZE):
        raise SystemExit("colormap.png is %dx%d; the swatch table assumes %d"
                         % (w, h, SIZE))

    # THE SWATCH TABLE IS CHECKED, NOT ASSUMED. If the pack is ever updated
    # and the grid moves, every rectangle below would recolour the wrong
    # thing - silently, because a recoloured texture always LOOKS like a
    # recoloured texture. A swatch that is not one flat gradient is a swatch
    # whose coordinates are wrong.
    # A swatch is TWO 32 px columns: a flat tone on the left and a top-to-bottom
    # ramp of the same hue on the right. (That is not a guess - it is what
    # broke the first version of this check, which assumed one flat 64 px
    # column and found the violet swatch changing halfway across.)
    for name, x, y, _ in SWATCHES:
        flat = rows[y + 4][x + 4]
        for dy in (4, SWATCH_H // 2, SWATCH_H - 5):
            for dx in (4, SWATCH_W // 2 - 5):
                here = rows[y + dy][x + dx]
                if here != flat:
                    raise SystemExit(
                        "swatch '%s' at (%d,%d): the left half is not one flat "
                        "tone (%s at +%d,+%d vs %s) - the grid has moved"
                        % (name, x, y, here, dx, dy, flat))
        for dy in (4, SWATCH_H // 2, SWATCH_H - 5):
            left = rows[y + dy][x + SWATCH_W // 2 + 4]
            right = rows[y + dy][x + SWATCH_W - 5]
            # 20, not 0: the ramp leans very slightly down-and-right, so a
            # row is not perfectly constant. The question this asks is
            # whether the rectangle straddles TWO swatches, and neighbouring
            # swatches are different hues - they differ by a hundred or more.
            if max(abs(a - b) for a, b in zip(left, right)) > 20:
                raise SystemExit(
                    "swatch '%s' at (%d,%d): the right half spans two tones "
                    "(%s vs %s) - the grid has moved"
                    % (name, x, y, left, right))
        top = rows[y + 4][x + SWATCH_W - 5]
        bottom = rows[y + SWATCH_H - 5][x + SWATCH_W - 5]
        if top == bottom:
            raise SystemExit("swatch '%s' at (%d,%d): the right half has no "
                             "gradient - the grid has moved" % (name, x, y))

    failures = 0
    for cuisine, (chroma, value, rotate) in sorted(CUISINE.items()):
        out = [list(r) for r in rows]
        for name, x, y, role in SWATCHES:
            if role != "dress":
                continue
            for dy in range(SWATCH_H):
                row = out[y + dy]
                for dx in range(SWATCH_W):
                    row[x + dx] = dress(row[x + dx], chroma, value, rotate)

        path = os.path.join(TEX, "colormap-crowd-%s.png" % cuisine)
        write_png(path, out)
        guid = write_meta(path, SOURCE + ".meta")

        print("%s  ->  %s  (%d bytes, guid %s)"
              % (cuisine, os.path.basename(path),
                 os.path.getsize(path), guid))
        print("     chroma x%.2f  value x%.2f  hue %+.0f deg"
              % (chroma, value, rotate))

        # THE SKIN HAS TO COME THROUGH UNTOUCHED, and "I did not write to that
        # rectangle" is not the check - the check is that the BYTES are the
        # same, because an off-by-one in the swatch table would look exactly
        # like a correct run.
        for name, x, y, role in SWATCHES:
            before = hexes(rows, x, y)
            after = hexes(out, x, y)
            same = all(out[y + dy][x + dx] == rows[y + dy][x + dx]
                       for dy in range(SWATCH_H) for dx in range(SWATCH_W))
            if role == "keep" and not same:
                print("     FAIL  %-10s was to be kept and changed %s -> %s"
                      % (name, before, after))
                failures += 1
            elif role == "dress" and same:
                print("     FAIL  %-10s was to be dressed and did not move (%s)"
                      % (name, before))
                failures += 1
            else:
                mark = "keep " if role == "keep" else "dress"
                print("     %s %-10s %s %s -> %s %s"
                      % (mark, name, before[0], before[1], after[0], after[1]))

    if failures:
        raise SystemExit("%d swatch(es) went the wrong way" % failures)
    print("ok: two crowd colormaps, skin and hair byte-identical to the source")


if __name__ == "__main__":
    main()
