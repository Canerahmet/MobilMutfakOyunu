# -*- coding: utf-8 -*-
"""
Copies SELECTED models out of vendor/*.zip into the Unity project.

Why not all of them: the Food Kit alone has 600 models. Taking them all bloats
the APK and pushes Unity's import time into the minutes. What the game
actually shows is known - tables, chairs, plates, a handful of dishes and a
dozen figures - so the list is written BY HAND.

The licence file is copied too. That is the rule in vendor/ATTRIBUTION.md:
License.txt travels with the models, otherwise in six months nobody will know
where these models came from.

Running it:
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

# (zip, destination folder, model list)
PICKS = [
    ("kenney_furniture-kit.zip", "Furniture", [
        # the hall
        "table", "tableRound", "tableCloth", "tableCrossCloth",
        "chair", "chairCushion", "chairRounded", "benchCushion",
        "stoolBar", "kitchenBar", "kitchenBarEnd",
        # the kitchen
        "kitchenStove", "kitchenFridge", "kitchenFridgeLarge",
        "kitchenSink", "kitchenCabinet", "kitchenCabinetDrawer",
        "kitchenCabinetUpper", "kitchenMicrowave", "kitchenCoffeeMachine",
        # the store room and decoration
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
        # serving ware
        "plate", "plate-deep", "plate-dinner", "bowl", "bowl-soup",
        "cup", "cup-tea", "cup-saucer", "glass", "soda-glass",
        # fast food
        "burger", "burger-cheese", "fries", "meat-patty",
        # Turkish
        "meat-cooked", "rice-ball", "bread", "salad", "cake",
        # the market
        "tomato", "onion", "egg", "cheese",
    ]),
]


def extract(zip_name, folder, names):
    src = os.path.join(VENDOR, zip_name)
    if not os.path.exists(src):
        print("  SKIPPED (no zip): " + zip_name)
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

            # The models
            if "FBX format/" in item and base in wanted:
                with z.open(item) as f, io.open(os.path.join(dest, base), "wb") as out:
                    shutil.copyfileobj(f, out)
                found.add(base)

            # The textures - all of them, because the zip does not say which
            # model uses which, and their total size is small.
            elif "/Textures/" in item and base.lower().endswith((".png", ".jpg")):
                with z.open(item) as f, \
                     io.open(os.path.join(dest, "Textures", base), "wb") as out:
                    shutil.copyfileobj(f, out)
                textures += 1

            # The licence - the vendor/ATTRIBUTION.md rule
            elif base.lower() == "license.txt":
                with z.open(item) as f, \
                     io.open(os.path.join(dest, "License.txt"), "wb") as out:
                    shutil.copyfileobj(f, out)

    missing = sorted(w[:-4] for w in wanted - found)
    print("  %-22s %2d models, %d textures" % (folder, len(found), textures))
    if missing:
        print("     NOT FOUND: " + ", ".join(missing))
    return len(found)


def main():
    if not os.path.isdir(VENDOR):
        print("there is no vendor/ folder")
        return 1

    print("Asset import (source: vendor/, licence: vendor/ATTRIBUTION.md)")
    total = 0
    for zip_name, folder, names in PICKS:
        total += extract(zip_name, folder, names)

    # THE LEDGER IS NO LONGER COPIED, AND THAT IS THE POINT.
    #
    # This line used to do `shutil.copy(vendor/ATTRIBUTION.md ->
    # Art/ATTRIBUTION.md)`, treating the vendor copy as the master. The two
    # files were the same document once. They are not any more: the Art copy
    # carries the FOLDER MAPPING TABLE that tools/check_licenses.py parses,
    # plus the audio and Noto Sans SC sections; the vendor copy carries the
    # engine-components table and the animation section.
    #
    # So running this importer would have silently deleted the `| Icons |`
    # row and the whole mapping - and the licence check would have gone red
    # with no clue why. Two ledgers that must BOTH be right cannot be kept
    # right by overwriting one with the other.
    #
    # Both are now edited by hand, and check_licenses.py is what proves the
    # Art copy still says what the folders on disk say.
    print("")
    print("REMINDER: a new folder under Art/ needs a row in BOTH ledgers")
    print("  unity/Assets/Lokanta/Art/ATTRIBUTION.md  (the folder mapping table)")
    print("  vendor/ATTRIBUTION.md                    (where it was downloaded from)")
    print("  then: python tools/check_licenses.py")

    print("")
    print("%d models in total -> unity/Assets/Lokanta/Art/" % total)
    return 0


if __name__ == "__main__":
    sys.exit(main())
