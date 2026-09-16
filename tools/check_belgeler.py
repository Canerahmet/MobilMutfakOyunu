# -*- coding: utf-8 -*-
"""Belgelerdeki baglantilar bir yere gidiyor mu.

NEDEN: bu depoda elli iki numarali belge var ve birbirlerine ikiyuz
kusur baglantiyla bagliler. Bir dosyayi tasimak ya da yeniden
adlandirmak, o baglantilarin sessizce kirilmasi demek - kirik bir
baglanti hata vermiyor, yalnizca tiklayinca hicbir sey olmuyor.

Bu, klasor yapisini duzenlemeyi GUVENLI hale getiriyor: tasi, kos,
kirilani gor. Aracsiz bir tasima, elli iki dosyayi gozle taramak demek.

NE OLCUYOR:
  - README.md ve docs/ altindaki her .md dosyasindaki goreli
    baglantilar ve resimler var olan bir dosyayi gosteriyor mu
  - docs/ altindaki her numarali belge docs/README.md'de aniliyor mu
    (dizinden dusen bir belge, olmayan bir belgeye denk)

NE OLCMUYOR: http(s) baglantilari. Aglara cikmak bu denetimi yavas ve
kirilgan yapardi; disaridaki bir adresin bugun ayakta olmasi zaten
yarin ayakta olacagi anlamina gelmiyor.

KOD ICINDEKILER DE OLCULMUYOR. docs/43'te kirik bir baglantinin KENDISI
alintilaniyor - o belge zaten "su baglanti kiriktir" diyor. Kod icindeki
metni baglanti saymak, bir hatayi anlatan cumleyi hata saymak olurdu.
"""
from __future__ import print_function

import io
import os
import re
import sys

KOK = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# [metin](hedef) ve ![metin](hedef)
BAG = re.compile(r"!?\[[^\]]*\]\(([^)]+)\)")
# Numarali belge: 02-tasarim-onerisi.md
NUMARALI = re.compile(r"^\d\d-.*\.md$")
# ``` ile cevrili blok ve ` ile cevrili ara kod
KOD_BLOK = re.compile(r"```.*?```", re.S)
KOD_ARA = re.compile("`[^`" + chr(10) + "]*`")


def md_dosyalari():
    yollar = [os.path.join(KOK, "README.md")]
    for kok, _, dosyalar in os.walk(os.path.join(KOK, "docs")):
        for d in sorted(dosyalar):
            if d.endswith(".md"):
                yollar.append(os.path.join(kok, d))
    return yollar


def main():
    kirik = []
    sayi = 0

    for yol in md_dosyalari():
        s = io.open(yol, encoding="utf-8").read()
        s = KOD_BLOK.sub("", s)
        s = KOD_ARA.sub("", s)
        klasor = os.path.dirname(yol)
        for hedef in BAG.findall(s):
            hedef = hedef.strip()
            if hedef.startswith(("http://", "https://", "mailto:", "#")):
                continue
            # Ayni dosya icindeki cipa: dosya.md#bolum
            dosya = hedef.split("#", 1)[0]
            if not dosya:
                continue
            sayi += 1
            tam = os.path.normpath(os.path.join(klasor, dosya))
            if not os.path.exists(tam):
                kirik.append("%s -> %s" % (
                    os.path.relpath(yol, KOK).replace("\\", "/"), hedef))

    # Dizinde anilmayan belge
    docs = os.path.join(KOK, "docs")
    dizin = io.open(os.path.join(docs, "README.md"), encoding="utf-8").read()
    anilmayan = []
    for d in sorted(os.listdir(docs)):
        if NUMARALI.match(d) and d not in dizin:
            anilmayan.append(d)

    print("baglanti : %d goreli baglanti tarandi" % sayi)
    print("belge    : %d numarali belge" % len(
        [d for d in os.listdir(docs) if NUMARALI.match(d)]))

    if kirik or anilmayan:
        print()
        for k in kirik:
            print("KIRIK: " + k)
        for a in anilmayan:
            print("DIZINDE YOK: docs/" + a)
        print("sonuc    : %d sorun" % (len(kirik) + len(anilmayan)))
        sys.exit(1)

    print("sonuc    : butun baglantilar yerinde, her belge dizinde")


if __name__ == "__main__":
    main()
