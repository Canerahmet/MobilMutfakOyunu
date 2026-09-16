# -*- coding: utf-8 -*-
"""Staff names.

Why this is needed: `Simulation.cs`'s own comment said it —
*"LOSING A NUMBER IS ABSTRACT, AN EMPLOYEE WHOSE NAME YOU KNOW RESIGNING
IS CONCRETE."* The diagnosis was right and the exact opposite had been
built: employees showed up as "Asci 1", "Garson 2" (Cook 1, Waiter 2)
and candidates read only "Asci". The sentence "Cook 2 resigned" is the
same thing as "capacity -28".

The names are NOT INVENTED, they are picked from names COMMON IN TURKEY,
mixed to suit the tone of a neighbourhood tradesman's restaurant: young
and middle-aged, formal and familiar. NO surnames - nobody in a
restaurant kitchen is called by their surname.

The list is deliberately LONG (96 names): the same name coming up twice
in a crew of twelve means the player works out that it is a list.

Usage:
    python tools/content/gen_names.py
"""
from __future__ import print_function

import io
import json
import os

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "content", "names.json")


# Kitchen and hall draw from the SAME pool: restaurant work is not
# divided by gender and the game does not divide it either.
NAMES = [
    # --- common, middle-aged ------------------------------------------
    "Nurten", "Hasan", "Ayşe", "Mehmet", "Fatma", "Mustafa",
    "Emine", "Ahmet", "Hatice", "Ali", "Zeynep", "Hüseyin",
    "Şerife", "İbrahim", "Elif", "Osman", "Havva", "Yusuf",
    "Sultan", "Ramazan", "Meryem", "Kemal", "Gülsüm", "Halil",

    # --- younger generation --------------------------------------------
    "Deniz", "Berk", "Ece", "Emre", "Selin", "Kaan",
    "Melis", "Arda", "Bade", "Onur", "Ceren", "Baran",
    "Yağmur", "Tolga", "Pınar", "Serkan", "Damla", "Umut",
    "Sıla", "Mert", "Gizem", "Batuhan", "Ayça", "Doruk",

    # --- tradesman tone ------------------------------------------------
    "Recep", "Sevim", "Şaban", "Hanife", "Bekir", "Nuray",
    "Cemal", "Hülya", "Rıza", "Sevgi", "Turgut", "Nazan",
    "Kadir", "Şükran", "Vedat", "Perihan", "Nuri", "Gülay",
    "Sabri", "Necla", "Hakkı", "Muazzez", "Zeki", "Türkan",

    # --- less common, lowers the risk of repeats ------------------------
    "Ferhat", "Bilge", "Okan", "Nehir", "Cenk", "Duygu",
    "Bora", "Esra", "Volkan", "Aslı", "Tarık", "Şevval",
    "Ozan", "Buse", "Sinan", "Dilek", "Uğur", "Merve",
    "Barış", "Özge", "Koray", "Nilay", "Alper", "Tuğçe",
]


def main():
    assert len(NAMES) == len(set(NAMES)), "there is a duplicate name"
    assert len(NAMES) >= 64, "the pool is too small; the same name repeats often"

    doc = {
        "schemaVersion": 1,
        "_comment": "GENERATED FILE. Run tools/content/gen_names.py.",
        "staff": NAMES,
    }

    io.open(OUT, "w", encoding="utf-8", newline="\n").write(
        json.dumps(doc, ensure_ascii=False, indent=2) + "\n")

    print("written: content/names.json")
    print("%d names" % len(NAMES))


if __name__ == "__main__":
    main()
