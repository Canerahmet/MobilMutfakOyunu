# -*- coding: utf-8 -*-
"""Cince yazi tipinin ALT KUMESINI uretir.

Noto Sans SC 10,5 MB ve icinde yirmi bin glif var; oyun bunlarin
bine yakinini kullaniyor. Tamamini pakete koymak, APK'ya on megabayt
eklemek demek.

NEDEN BIR ARAC: alt kume ilk seferinde ELLE cikarilmisti ve hangi
karakterlerin istendigi hicbir yerde yazmiyordu. Sonucu su oldu -
oyuna Cince'de gorunen uc sey alt kumeye hic girmemisti:

  1. PERSONEL ISIMLERI. content/names.json yerellestirme tablosunda
     degil, yani "Cince metni tara" diyen mantik ona hic bakmadi.
     On alti isim (Ayse, Ibrahim, Yagmur...) ekranda bos kutu cikardi.
  2. KODA GOMULU SIMGELER. Aksam raporundaki eksi isareti U+2212 ve
     menudeki madde imi U+2022 dogrudan C# icinde yaziyor.
  3. Bu ikisi dilden BAGIMSIZ: dil Cince oldugunda butun agac bu yazi
     tipiyle ciziliyor, yalnizca Cince metin degil.

Yani soru "Cince metinde hangi karakter var" degil, DIL CINCE IKEN
EKRANDA HANGI KARAKTER CIKABILIR. Karakter kumesi artik olculuyor ve
alt kume ondan uretiliyor; check_font.py ayni kumeyi denetliyor.

Kullanim:
    python tools/art/subset_font.py
    python tools/art/subset_font.py --kontrol    (yalnizca karsilastir)

Cikis kodu 0 tamam, 1 alt kume guncel degil (--kontrol ile).
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
KAYNAK = os.path.join(ROOT, "vendor", "noto-sans-sc", "NotoSansSC-Regular.ttf")
HEDEF = check_font.CJK_FONT
LISTE = os.path.join(HERE, "out", "zh_karakterler.txt")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--kontrol", action="store_true",
                    help="uretme, yalnizca alt kume yeterli mi diye bak")
    args = ap.parse_args()

    istenen = check_font.cjk_characters()
    print("istenen   : %d karakter" % len(istenen))

    if args.kontrol:
        if not os.path.exists(HEDEF):
            print("alt kume yok: " + os.path.relpath(HEDEF, ROOT))
            return 1
        araliklar = check_font.font_codepoints(HEDEF)
        eksik = sorted(c for c in istenen
                       if not check_font.covered(araliklar, ord(c)))
        if eksik:
            print("EKSIK     : %d karakter -> %s"
                  % (len(eksik), " ".join(eksik[:40])))
            print("sonuc     : alt kume guncel degil, subset_font.py calistirin")
            return 1
        print("sonuc     : alt kume istenen her karakteri tasiyor")
        return 0

    if not os.path.exists(KAYNAK):
        raise SystemExit("Kaynak yazi tipi yok: " + KAYNAK)

    try:
        from fontTools import subset
    except ImportError:
        raise SystemExit("fonttools gerekli: pip install fonttools")

    # Metin dosyasi BELGE DEGIL, KANIT: alt kumenin neyden uretildigi
    # boylece depoda duruyor ve fark gozle gorulebiliyor.
    io.open(LISTE, "w", encoding="utf-8", newline="").write(
        u"".join(istenen))

    secenekler = subset.Options()
    # Sekillendirme tablolari KALIYOR. Cince'de birlestirme yok ama
    # dikey yazim (vert/vrt2) ve birlesik bicimler (ccmp) var; bunlari
    # atmak, alt kumeyi kucultup metni bozmak olurdu.
    secenekler.layout_features = ["*"]
    secenekler.name_IDs = ["*"]
    secenekler.notdef_outline = True
    secenekler.recalc_bounds = True
    secenekler.drop_tables = []

    font = subset.load_font(KAYNAK, secenekler)
    subsetter = subset.Subsetter(options=secenekler)
    subsetter.populate(text=u"".join(istenen))
    subsetter.subset(font)
    subset.save_font(font, HEDEF, secenekler)
    font.close()

    kaynak_boyut = os.path.getsize(KAYNAK)
    hedef_boyut = os.path.getsize(HEDEF)
    print("kaynak    : %s (%d bayt)" % (os.path.relpath(KAYNAK, ROOT), kaynak_boyut))
    print("alt kume  : %s (%d bayt, %%%.1f)"
          % (os.path.relpath(HEDEF, ROOT), hedef_boyut,
             100.0 * hedef_boyut / kaynak_boyut))
    return 0


if __name__ == "__main__":
    sys.exit(main())
