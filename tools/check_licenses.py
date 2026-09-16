# -*- coding: utf-8 -*-
"""
Lisans denetimi - ticari yayin kapisi
============================================================================
Bu proje TICARI olarak yayinlanacak. Kullanilan her varlik paketinin
ticari kullanima izin veren bir lisansi olmali, lisans metni oyunla
birlikte dagitilmali ve atif defterinde bir satiri bulunmali.

NEDEN MAKINE DENETLIYOR:

Projenin kendi kutugu "en buyuk risk: yapay zeka araclarinin ucretsiz
katmaniyla uretilmis bir varligin gozden kacmasi - o katmanlar ticari
kullanima kapali" diyor. Ama o yolda SIFIR SURTUNME vardi: bir .ogg'yi
Resources/audio/ klasorune atmak kod degisikligi istemiyor, lisans metni
istemiyor ve hicbir denetciyi tetiklemiyordu. Sfx.Init klasordeki HER
klibi sorgusuz caliyor.

Google Play bunu yakalamaz; telif sahibi yakalar.

Ayrica iki ayri ATIF defteri vardi (vendor/ATTRIBUTION.md ve Art/ATTRIBUTION.md) ve
ikisi ayrismisti - biri motor bilesenleri tablosunu tasiyor, oteki
tasimiyordu. Ayni kural iki yere yazildiginda bu projede bes kez
sessizce ayristi.

Denetlenenler:
  1. Art/ altindaki her varlik klasorunde License.txt var mi.
  2. O lisans TICARI kullanima aciksa bilinen bir lisans mi (CC0, OFL,
     MIT, Apache, CC-BY).
  3. Resources/audio/ altinda ses dosyasi varsa lisansi da var mi.
  4. Art/ATTRIBUTION.md her klasoru aniyor mu.

Cikis kodu 0 temiz, 1 en az bir eksik.
"""
from __future__ import print_function

import io
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
ART = os.path.join(ROOT, "unity", "Assets", "Lokanta", "Art")
SES = os.path.join(ROOT, "unity", "Assets", "Lokanta", "Resources", "audio")
LISANS = os.path.join(ROOT, "unity", "Assets", "Lokanta", "Resources", "licenses")
ATIF = os.path.join(ART, "ATTRIBUTION.md")

# Varlik TASIMAYAN klasorler: uretilmis ya da projenin kendi ciktilari.
URETILEN = {"Materials", "Prefab", "Animator", "Mesh"}

# Ticari kullanima acik oldugunu bildigimiz lisanslar. Metinde bu
# damgalardan biri geciyorsa tamam; gecmiyorsa INSAN bakmali.
TANINAN = [
    ("CC0", "CC0 1.0 - kamu malina birakilmis, ticari kullanim serbest"),
    ("Creative Commons Zero", "CC0 1.0"),
    ("SIL OPEN FONT LICENSE", "OFL 1.1 - ticari kullanim serbest, yazi tipi satilamaz"),
    ("MIT License", "MIT"),
    ("Apache License", "Apache 2.0"),
    ("CC-BY", "CC-BY - ATIF ZORUNLU"),
    ("Attribution 4.0", "CC-BY 4.0 - ATIF ZORUNLU"),
]

SES_UZANTI = (".ogg", ".wav", ".mp3", ".aiff", ".aif")


def read(path):
    try:
        return io.open(path, encoding="utf-8", errors="replace").read()
    except IOError:
        return ""


def main():
    sorun = []
    satir = []

    if not os.path.isdir(ART):
        print("HATA: Art klasoru yok: " + ART)
        return 1

    atif = read(ATIF)
    if not atif:
        sorun.append("Art/ATTRIBUTION.md yok - atif defteri yayin oncesi zorunlu")

    # --- 1-2. varlik klasorleri ------------------------------------------
    for ad in sorted(os.listdir(ART)):
        yol = os.path.join(ART, ad)
        if not os.path.isdir(yol) or ad in URETILEN:
            continue

        # PROJENIN KENDI URETTIGI klasorde ucuncu taraf lisansi
        # aranmaz; ATIF tablosunda oyle isaretli olmasi yeter.
        kendi = ("| " + ad + " |") in atif and "kendi uretimi" in atif.replace(
            "ü", "u").replace("ı", "i").lower()

        lisans = os.path.join(yol, "License.txt")
        if not os.path.isfile(lisans):
            if kendi:
                satir.append("  %-10s projenin kendi uretimi" % ad)
                continue
            sorun.append("Art/%s: License.txt YOK" % ad)
            continue

        metin = read(lisans)
        tanindi = None
        for damga, aciklama in TANINAN:
            if damga.lower() in metin.lower():
                tanindi = aciklama
                break

        if tanindi is None:
            sorun.append("Art/%s: lisans TANINMADI - insan bakmali" % ad)
        else:
            satir.append("  %-10s %s" % (ad, tanindi))

        if ("| " + ad + " |") not in atif:
            sorun.append("Art/%s: ATTRIBUTION.md klasor eslemesinde satiri yok" % ad)

    # --- 3. sesler --------------------------------------------------------
    sesler = []
    if os.path.isdir(SES):
        for kok, _, dosyalar in os.walk(SES):
            for d in dosyalar:
                if d.lower().endswith(SES_UZANTI):
                    sesler.append(os.path.join(kok, d))

    if sesler:
        lisans_metni = ""
        if os.path.isdir(LISANS):
            for d in os.listdir(LISANS):
                if d.endswith(".txt"):
                    lisans_metni += read(os.path.join(LISANS, d))

        if "audio" not in atif.lower():
            sorun.append("Resources/audio/ dolu (%d dosya) ama ATTRIBUTION.md'de ses "
                         "bolumu yok" % len(sesler))
        if not lisans_metni:
            sorun.append("Resources/audio/ dolu ama Resources/licenses/ bos")
        satir.append("  %-10s %d dosya" % ("audio", len(sesler)))
    else:
        satir.append("  %-10s yok (risk sifir)" % "audio")

    # --- rapor -------------------------------------------------------------
    for s in satir:
        print(s)

    if sorun:
        print("")
        for s in sorun:
            print("  EKSIK: " + s)
        print("sonuc    : %d eksik" % len(sorun))
        return 1

    print("sonuc    : butun varliklar lisansli ve atif defterinde")
    return 0


if __name__ == "__main__":
    sys.exit(main())
