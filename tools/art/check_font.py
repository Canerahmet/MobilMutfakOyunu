# -*- coding: utf-8 -*-
"""Yazi tipi kapsama denetcisi.

Oyunun ekranda gosterebilecegi HER karakterin yazi tipinde karsiligi var
mi diye bakar. Olmayan bir karakter oyuncuya bos kutu olarak gorunur ve
bu, yayindan sonra fark edilen turden bir hatadir.

Nereye bakiyor:
  - content/loc/tr.json           (butun arayuz metinleri)
  - content/*.json                (yemek, malzeme, arketip, duzenli adlari)
  - unity/Assets/Lokanta/Game/**  (koda gomulu dizeler)

Neden koda gomulu dizeler de: yerellestirme tablosu her metni tasimiyor -
"Patronsun, asci degil." gibi bazi metinler dogrudan ekran kodunda.
Denetci ikisini de taramazsa eksigi bulamaz.

Kullanim:
    python tools/art/check_font.py
    python tools/art/check_font.py --font vendor/rubik/Rubik-wght.ttf

Cikis kodu 0 temiz, 1 eksik karakter var.
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
# DENETLENEN DOSYA, OYUNA GIRENIN TA KENDISI OLMALI.
#
# Once vendor/ altindaki indirme kopyasi taraniyordu. Ikisi bugun ayni
# ama ayni KALACAGININ garantisi yoktu: birini guncelleyip digerini
# unutmak, "hepsi kapsaniyor" diyen bir denetim ile eksik karakterli bir
# oyun demek - ve denetim yesil oldugu icin kimse bakmaz.
DEFAULT_FONT = os.path.join(
    ROOT, "unity", "Assets", "Lokanta", "Art", "Yazi", "Rubik.ttf")
VENDOR_FONT = os.path.join(ROOT, "vendor", "rubik", "Rubik-wght.ttf")


def _same_bytes(a, b):
    """Iki dosya ayni mi. Yoksa ikisinden biri eskimis demektir."""
    try:
        with open(a, "rb") as fa, open(b, "rb") as fb:
            return fa.read() == fb.read()
    except IOError:
        return None

def _control(ch):
    """Ekranda gorunmeyen denetim karakteri mi.

    Kayit bicimi alanlari U+001F ile ayiriyor (SaveStore). O karakter
    dosyada duruyor, ekranda degil - denetcinin onu eksik saymasi
    yanlis alarm olurdu.
    """
    return ord(ch) < 0x20 or 0x7F <= ord(ch) <= 0x9F


# ---------------------------------------------------------------------------
def font_codepoints(path):
    """TTF'nin cmap format 4 tablosundan kapsanan araliklari okur."""
    data = open(path, "rb").read()
    num = struct.unpack(">H", data[4:6])[0]

    tables = {}
    for i in range(num):
        off = 12 + i * 16
        tag = data[off:off + 4].decode("latin1")
        o, ln = struct.unpack(">II", data[off + 8:off + 16])
        tables[tag] = (o, ln)

    if "cmap" not in tables:
        raise SystemExit("cmap tablosu yok: " + path)

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
        raise SystemExit("Okunabilir cmap alt tablosu yok: " + path)
    return ranges


def covered(ranges, cp):
    for s, e in ranges:
        if s <= cp <= e:
            return True
    return False


# ---------------------------------------------------------------------------
# C# dize sabitleri. Kacis dizileri cozuluyor: "ç" yazan bir kaynak
# ekranda ç gosterir, denetci onu da gormeli.
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

    # Yorum satirlarini at: yorumdaki bir karakter ekrana cikmiyor.
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
        print("  atlandi (bozuk json): %s - %s" % (path, e))
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
def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--font", default=DEFAULT_FONT)
    args = ap.parse_args()

    if not os.path.exists(args.font):
        raise SystemExit("Yazi tipi yok: " + args.font)

    ranges = font_codepoints(args.font)
    seen = {}

    loc = os.path.join(ROOT, "content", "loc")
    if os.path.isdir(loc):
        for name in sorted(os.listdir(loc)):
            if name.endswith(".json"):
                scan_json(os.path.join(loc, name), seen)

    content = os.path.join(ROOT, "content")
    for base, _dirs, files in os.walk(content):
        for name in sorted(files):
            if name.endswith(".json"):
                scan_json(os.path.join(base, name), seen)

    game = os.path.join(ROOT, "unity", "Assets", "Lokanta", "Game")
    for base, _dirs, files in os.walk(game):
        for name in sorted(files):
            if name.endswith(".cs"):
                scan_csharp(os.path.join(base, name), seen)

    missing = []
    for ch, where in sorted(seen.items()):
        if _control(ch):
            continue
        if not covered(ranges, ord(ch)):
            missing.append((ch, where))

    print("yazi tipi : %s" % os.path.relpath(args.font, ROOT))
    print("taranan   : %d benzersiz karakter" % len(seen))

    if not missing:
        # Indirme kopyasi ile oyuna giren kopya AYRISTI MI.
        same = _same_bytes(args.font, VENDOR_FONT)
        if same is False:
            print("sonuc     : hepsi kapsaniyor AMA vendor/ kopyasi FARKLI")
            print("            %s" % VENDOR_FONT)
            print("            Biri guncellenip digeri unutulmus; ikisini esitle.")
            return 1

        print("sonuc     : hepsi kapsaniyor")
        return 0

    print("sonuc     : %d karakter EKSIK" % len(missing))
    for ch, where in missing:
        try:
            name = unicodedata.name(ch)
        except ValueError:
            name = "?"
        print("  U+%04X  %-34s  %s" % (ord(ch), name, os.path.relpath(where, ROOT)))
    return 1


if __name__ == "__main__":
    sys.exit(main())
