# -*- coding: utf-8 -*-
"""Personel isimleri.

Neden gerekli: `Simulation.cs` icindeki kendi yorumu soyluyordu —
*"SAYI KAYBETMEK SOYUT, ADINI BILDIGIN BIR CALISANIN ISTIFA ETMESI
SOMUT."* Teshis dogruydu ve tam tersi uygulanmisti: calisanlar "Asci 1",
"Garson 2" diye gorunuyor, adaylar yalnizca "Asci" yaziyordu. "Asci 2
istifa etti" cumlesi "kapasite -28" ile ayni sey.

Isimler UYDURULMADI, TURKIYE'DE YAYGIN olanlardan secildi ve mahalle
esnafi tonuna uygun bir karisim: hem genc hem orta yasli, hem resmi hem
samimi. Soyad YOK - bir lokanta mutfaginda kimse soyadiyla cagrilmaz.

Liste ozellikle UZUN (96 isim): on iki kisilik bir kadroda ayni ismin iki
kez cikmasi, oyuncunun "bu bir liste" oldugunu anlamasi demek.

Kullanim:
    python tools/content/gen_names.py
"""
from __future__ import print_function

import io
import json
import os

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "content", "names.json")


# Mutfak ve salon AYNI havuzdan: lokanta isi cinsiyete gore
# ayrilmiyor ve oyun da ayirmiyor.
NAMES = [
    # --- yaygin, orta yasli -------------------------------------------
    "Nurten", "Hasan", "Ayşe", "Mehmet", "Fatma", "Mustafa",
    "Emine", "Ahmet", "Hatice", "Ali", "Zeynep", "Hüseyin",
    "Şerife", "İbrahim", "Elif", "Osman", "Havva", "Yusuf",
    "Sultan", "Ramazan", "Meryem", "Kemal", "Gülsüm", "Halil",

    # --- genc kusak ----------------------------------------------------
    "Deniz", "Berk", "Ece", "Emre", "Selin", "Kaan",
    "Melis", "Arda", "Bade", "Onur", "Ceren", "Baran",
    "Yağmur", "Tolga", "Pınar", "Serkan", "Damla", "Umut",
    "Sıla", "Mert", "Gizem", "Batuhan", "Ayça", "Doruk",

    # --- esnaf tonu ----------------------------------------------------
    "Recep", "Sevim", "Şaban", "Hanife", "Bekir", "Nuray",
    "Cemal", "Hülya", "Rıza", "Sevgi", "Turgut", "Nazan",
    "Kadir", "Şükran", "Vedat", "Perihan", "Nuri", "Gülay",
    "Sabri", "Necla", "Hakkı", "Muazzez", "Zeki", "Türkan",

    # --- daha az yaygin, tekrar riskini dusuruyor -----------------------
    "Ferhat", "Bilge", "Okan", "Nehir", "Cenk", "Duygu",
    "Bora", "Esra", "Volkan", "Aslı", "Tarık", "Şevval",
    "Ozan", "Buse", "Sinan", "Dilek", "Uğur", "Merve",
    "Barış", "Özge", "Koray", "Nilay", "Alper", "Tuğçe",
]


def main():
    assert len(NAMES) == len(set(NAMES)), "tekrarlanan isim var"
    assert len(NAMES) >= 64, "havuz cok kucuk; ayni isim sik tekrarlar"

    doc = {
        "schemaVersion": 1,
        "_comment": "URETILEN DOSYA. tools/content/gen_names.py calistirin.",
        "staff": NAMES,
    }

    io.open(OUT, "w", encoding="utf-8", newline="\n").write(
        json.dumps(doc, ensure_ascii=False, indent=2) + "\n")

    print("yazildi: content/names.json")
    print("%d isim" % len(NAMES))


if __name__ == "__main__":
    main()
