# -*- coding: utf-8 -*-
"""The value distribution of a frame, and the colours that fill it.

Written for docs/60, when the user said the floor and the back wall were
tiring to look at. A point sample answers "what is that pixel", which is the
wrong question: twice a sample landed on a rug or a table and sent the work at
the wrong surface. This answers the right one - how much of the frame sits in
each fifth of the value range, and which colours occupy the most of it.

WHAT IT CANNOT TELL YOU, and it took an hour to find out: a frame from
Editor/GameShot carries that tool's OWN clear colour, which the game never
draws. On those renders the biggest bucket in the report - about 45% of every
frame - is the grey field around the building, and it is not in the product.
Read this tool on the STORE frames when the question is about the whole
picture, and on the GameShot renders only for the pixels inside the building.

    python tools/art/tone.py render/hall_turk_noon.png
"""
import io
import struct
import sys
import zlib


def readpng(p):
    d = io.open(p, 'rb').read()
    pos, idat = 8, b''
    while pos < len(d):
        ln = struct.unpack('>I', d[pos:pos + 4])[0]
        typ = d[pos + 4:pos + 8]
        data = d[pos + 8:pos + 8 + ln]
        pos += 12 + ln
        if typ == b'IHDR':
            w, h, _, ct = struct.unpack('>IIBB', data[:10])
        elif typ == b'IDAT':
            idat += data
    raw = zlib.decompress(idat)
    ch = {0: 1, 2: 3, 4: 2, 6: 4}[ct]
    stride = w * ch
    out = bytearray()
    prev = bytearray(stride)
    i = 0
    for _ in range(h):
        f = raw[i]
        i += 1
        line = bytearray(raw[i:i + stride])
        i += stride
        for x in range(stride):
            a = line[x - ch] if x >= ch else 0
            b = prev[x]
            c = prev[x - ch] if x >= ch else 0
            if f == 1:
                line[x] = (line[x] + a) & 255
            elif f == 2:
                line[x] = (line[x] + b) & 255
            elif f == 3:
                line[x] = (line[x] + (a + b) // 2) & 255
            elif f == 4:
                pp = a + b - c
                pa, pb, pc = abs(pp - a), abs(pp - b), abs(pp - c)
                pr = a if (pa <= pb and pa <= pc) else (b if pb <= pc else c)
                line[x] = (line[x] + pr) & 255
        out += line
        prev = line
    return w, h, ch, bytes(out)


def report(path):
    w, h, ch, px = readpng(path)
    bands = [0] * 5
    counts = {}
    n = 0
    for y in range(0, h, 2):
        for x in range(0, w, 2):
            o = (y * w + x) * ch
            r, g, b = px[o], px[o + 1], px[o + 2]
            v = (r + g + b) // 3
            bands[min(4, v * 5 // 256)] += 1
            key = (r // 16 * 16, g // 16 * 16, b // 16 * 16)
            counts[key] = counts.get(key, 0) + 1
            n += 1
    print(path)
    labels = ['0-20%', '20-40%', '40-60%', '60-80%', '80-100%']
    for i, lab in enumerate(labels):
        share = bands[i] * 100.0 / n
        print('   %-8s %5.1f%%  %s' % (lab, share, '#' * int(share / 2)))
    top = sorted(counts.items(), key=lambda kv: -kv[1])[:6]
    print('   biggest colours:')
    for (r, g, b), c in top:
        print('      rgb(%3d,%3d,%3d)  %4.1f%%  value %d%%'
              % (r, g, b, c * 100.0 / n, (r + g + b) // 3 * 100 // 255))
    print()


for a in sys.argv[1:]:
    report(a)
