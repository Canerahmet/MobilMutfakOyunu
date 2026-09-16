# -*- coding: utf-8 -*-
"""
vendor/*.zip icinden SECILMIS modelleri Unity projesine kopyalar.

Neden hepsini degil: Food Kit tek basina 600 model. Hepsini almak APK'yi
sisiriyor ve Unity'nin iceri alma suresini dakikalara cikariyor. Oyunun
gercekten gosterdigi sey belli - masa, sandalye, tabak, birkac yemek ve
on iki figur - o yuzden liste ELLE yaziliyor.

Lisans dosyasi da kopyalaniyor. vendor/ATTRIBUTION.md'deki kural bu: License.txt
modellerle birlikte gider, yoksa alti ay sonra bu modellerin nereden
geldigini kimse bilmez.

Calistirma:
    python tools/art/import_vendor.py
"""
from __future__ import print_function

import io
import os
import shutil
import sys
import zipfile

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(HERE))
VENDOR = os.path.join(ROOT, "vendor")
ART = os.path.join(ROOT, "unity", "Assets", "Lokanta", "Art")

# (zip, hedef klasor, model listesi)
PICKS = [
    ("kenney_furniture-kit.zip", "Furniture", [
        # salon
        "table", "tableRound", "tableCloth", "tableCrossCloth",
        "chair", "chairCushion", "chairRounded", "benchCushion",
        "stoolBar", "kitchenBar", "kitchenBarEnd",
        # mutfak
        "kitchenStove", "kitchenFridge", "kitchenFridgeLarge",
        "kitchenSink", "kitchenCabinet", "kitchenCabinetDrawer",
        "kitchenCabinetUpper", "kitchenMicrowave", "kitchenCoffeeMachine",
        # depo ve dekor
        "bookcaseClosedDoors", "pottedPlant", "rugRectangle", "rugRounded",
        "lampSquareCeiling", "doorwayOpen", "wallDoorway",
    ]),
    ("kenney_mini-characters.zip", "Characters", [
        "character-female-a", "character-female-b", "character-female-c",
        "character-female-d", "character-female-e", "character-female-f",
        "character-male-a", "character-male-b", "character-male-c",
        "character-male-d", "character-male-e", "character-male-f",
    ]),
    ("kenney_food-kit.zip", "Food", [
        # servis kaplari
        "plate", "plate-deep", "plate-dinner", "bowl", "bowl-soup",
        "cup", "cup-tea", "cup-saucer", "glass", "soda-glass",
        # fast food
        "burger", "burger-cheese", "fries", "meat-patty",
        # turk
        "meat-cooked", "rice-ball", "bread", "salad", "cake",
        # hal
        "tomato", "onion", "egg", "cheese",
    ]),
]


def extract(zip_name, folder, names):
    src = os.path.join(VENDOR, zip_name)
    if not os.path.exists(src):
        print("  ATLANDI (zip yok): " + zip_name)
        return 0

    dest = os.path.join(ART, folder)
    os.makedirs(dest, exist_ok=True)
    os.makedirs(os.path.join(dest, "Textures"), exist_ok=True)

    wanted = set(n + ".fbx" for n in names)
    found = set()
    textures = 0

    with zipfile.ZipFile(src) as z:
        for item in z.namelist():
            base = os.path.basename(item)
            if not base:
                continue

            # Modeller
            if "FBX format/" in item and base in wanted:
                with z.open(item) as f, io.open(os.path.join(dest, base), "wb") as out:
                    shutil.copyfileobj(f, out)
                found.add(base)

            # Dokular - hepsi, cunku hangi model hangisini kullaniyor
            # zipten anlasilmiyor ve toplam boyutlari kucuk.
            elif "/Textures/" in item and base.lower().endswith((".png", ".jpg")):
                with z.open(item) as f, \
                     io.open(os.path.join(dest, "Textures", base), "wb") as out:
                    shutil.copyfileobj(f, out)
                textures += 1

            # Lisans - vendor/ATTRIBUTION.md kurali
            elif base.lower() == "license.txt":
                with z.open(item) as f, \
                     io.open(os.path.join(dest, "License.txt"), "wb") as out:
                    shutil.copyfileobj(f, out)

    missing = sorted(w[:-4] for w in wanted - found)
    print("  %-22s %2d model, %d doku" % (folder, len(found), textures))
    if missing:
        print("     BULUNAMADI: " + ", ".join(missing))
    return len(found)


def main():
    if not os.path.isdir(VENDOR):
        print("vendor/ klasoru yok")
        return 1

    print("Varlik iceri alma (kaynak: vendor/, lisans: vendor/ATTRIBUTION.md)")
    total = 0
    for zip_name, folder, names in PICKS:
        total += extract(zip_name, folder, names)

    # Atif dosyasi da projeye girsin: oyunu acan biri lisansi gormeli.
    shutil.copy(os.path.join(VENDOR, "ATTRIBUTION.md"), os.path.join(ART, "ATTRIBUTION.md"))

    print("")
    print("toplam %d model -> unity/Assets/Lokanta/Art/" % total)
    return 0


if __name__ == "__main__":
    sys.exit(main())
