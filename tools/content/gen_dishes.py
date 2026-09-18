# -*- coding: utf-8 -*-
"""
Dish and ingredient data writer - Phase 0
============================================================================
Writes three files:

  content/ingredients.json      the ingredient catalogue, shared by both cuisines
  content/dishes/fastfood.json  32 fast food dishes
  content/dishes/turk.json      32 Turkish restaurant dishes

Source documents:
  docs/09-content-inventory.md      menus, the opening menu, the season curve
  docs/12-economy.md 3              ingredient cost ~ 32% of the sale price
  docs/23-core-contract.md   8.3 the dish schema, 8.4 the integer unit rule
  docs/24-art-pipeline.md           modular plating, 6 bases + 8 toppings

Units follow docs/23 2.2 and 8.4, NO DECIMAL POINT:
  money       centi-coin    (1 coin = 100)
  ratio       basis point   (10000 = 1.0)
  time        milliseconds
  weight      grams
  ingredient  centi-coin / kilogram   (the "unit" field is "kg")

The price is DERIVED, not written by hand. Recipe weights and ingredient
kilo prices are the input; sale price = cost / target ingredient ratio,
rounded to 50 centi-coins. That way docs/12's "32% ingredients" rule
comes from a number that can be verified on every dish.

Run with:  python tools/content/gen_dishes.py
Exit code 1 means one of the balance rules broke; the report says why.
"""
import io
import json
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
CONTENT = os.path.join(ROOT, "content")
DISHES_DIR = os.path.join(CONTENT, "dishes")

BP = 10_000
ID_RE = re.compile(r"^[a-z0-9_]+$")
DECIMAL_RE = re.compile(r"[0-9]\.[0-9]")

# docs/24-art-pipeline.md: a plate = base + 0-3 toppings
BASES = ("bun", "plate_round", "plate_oval", "bowl", "tray", "paper", "cup")
TOPPINGS = ("patty", "slice", "sphere", "leaf", "strip", "sauce", "stick", "steam")
STATIONS = ("ocak", "fritoz", "izgara", "firin", "soguk", "icecek", "tatli")

# Named equipment SPECIFIC to a cuisine. It comes after the shared six,
# it DOES NOT EXIST at the start, and the dishes tied to it stay locked
# until it is bought. docs/09 asks for 10 per cuisine; for now there are
# three and two.
#
# This table is here because of a painful incident: the named stations
# had been written straight into content/dishes/*.json and THE GENERATOR
# DID NOT KNOW ABOUT THEM. Running gen_dishes.py silently turned every
# one of them back into "firin" (the oven). The station is now written on
# the dish row and the equipment's "opens" list is derived FROM HERE
# (tools/balance/export.py).
CUISINE_STATIONS = {
    "turk": ("tas_firin", "doner_ocagi", "pide_firini"),
    "fastfood": ("milkshake_makinesi", "waffle_makinesi"),
}

# Items that appear in no recipe but have to be in the kitchen. docs/09:
# tea is not a dish sold on the menu, it is a decision to offer something;
# docs/23 8.2 teaCostCenti reads its cost.
SERVICE_INGREDIENTS = ("cay",)

# The ingredient ratio band: docs/12 says 32%, the band is 28%-36%
RATIO_MIN = 2800
RATIO_MAX = 3600

# The target ingredient ratio per group. A main dish is filling and made
# of expensive ingredients, a drink has the highest margin. All of them
# sit comfortably inside the band.
# THE INGREDIENT RATIO TARGET IS KEYED ON CUISINE + GROUP.
#
# Measured, and the promise WAS NOT IN THE NUMBERS: fast food's gross
# margin was 64%, the Turkish one's 56%. That is, the cuisine sold as
# "cheap and crowded" was the MORE PROFITABLE one. In reality it is the
# other way round - chains earn little per unit and live on volume.
#
# Fast food was pulled to the TOP end of the band (high ingredient ratio
# = narrow margin). Turkish DID NOT CHANGE: what was wanted was fast
# food's per-unit earnings coming down.
#
# THE KEY IS CUISINE + GROUP, NOT group alone. On the first attempt I
# wrote it per group and the Turkish margin fell too (17,351 -> 16,009):
# the two cuisines SHARE the "icecek" (drink) and "tatli" (dessert)
# groups, so one target per group shifts both of them at once. Without
# splitting a shared group's target by cuisine, the distinction cannot be
# made at all.
#
# Both are inside docs/12's 28-36% band.
GROUP_TARGET_BP = {
    "fastfood": {
        "ana": 3550, "yan": 3400, "icecek": 3250, "tatli": 3450,
    },
    "turk": {
        "sulu": 3300, "izgara": 3300, "corba": 3050,
        "pilav": 3050, "meze": 3050, "icecek": 2950, "tatli": 3150,
    },
}

# A rough role mapping for the check: the opening menu must have a main,
# a side and a drink.
GROUP_ROLE = {
    "ana": "ana", "sulu": "ana", "izgara": "ana",
    "yan": "yan", "pilav": "yan", "meze": "yan", "corba": "yan",
    "icecek": "icecek", "tatli": "tatli",
}

# ---------------------------------------------------------------------------
# Season and quality profiles
#
# The profile NAMES below stay Turkish ("sabit" fixed, "sebze" vegetable,
# "yesillik" leafy, "meyve" fruit, "et" meat, "sut" dairy; "kahraman"
# hero, "orta" mid, "temel" basic). They are labels inside the same data
# rows as the content ids (`kuru_fasulye`, `ana`, `ocak`), which are
# fixed by content/ and by the C# loader - translating half of a data
# table reads worse than leaving it whole.
# ---------------------------------------------------------------------------
# 10000 = no change. docs/12: some ingredients are 20% cheaper or dearer
# in a given season.
SEASON = {
    "sabit":    (10000, 10000, 10000, 10000),
    "sebze":    (10200,  8600,  9000, 12000),
    "yesillik": ( 9200,  9600, 10400, 12400),
    "meyve":    (10600,  9000,  8800, 11600),
    "et":       (10000, 10500,  9500, 11500),
    "sut":      ( 9400, 10200, 10000, 11200),
}
# Quality tier price multiplier and satisfaction effect (centi-points).
# The minced meat example in docs/13-data-schemas.md corresponds to the
# "kahraman" (hero) profile.
QUALITY = {
    "kahraman": ((7500, 10000, 13500), (-2000, 0, 1500)),
    "orta":     ((8000, 10000, 12500), (-1200, 0,  800)),
    "temel":    ((8500, 10000, 11500), ( -500, 0,  300)),
}

FF = "fastfood"
TR = "turk"
BOTH = [FF, TR]

# ---------------------------------------------------------------------------
# Ingredients
#   (id, centi-coin/kg, perishable, spoil day, season, quality, cuisines)
# ---------------------------------------------------------------------------
# The basic pantry, docs/09: 12 items, present in every cuisine (shared = true)
PANTRY = [
    ("tuz",           900, False,  0, "sabit", "temel"),
    ("karabiber",   24000, False,  0, "sabit", "temel"),
    ("zeytinyagi",  14000, False,  0, "sabit", "orta"),
    ("aycicek_yagi", 5200, False,  0, "sabit", "temel"),
    ("un",           1800, False,  0, "sabit", "temel"),
    ("sogan",        1400, True,  20, "sebze", "temel"),
    ("sarimsak",     6000, True,  30, "sebze", "temel"),
    ("domates",      2600, True,   4, "sebze", "orta"),
    ("seker",        2000, False,  0, "sabit", "temel"),
    ("sut",          2800, True,   3, "sut",   "orta"),
    ("yumurta",      4200, True,  18, "sut",   "orta"),
    ("tereyagi",    15000, True,  25, "sut",   "orta"),
]

# Ingredients outside the pantry (shared = false). The "cuisines" field
# says which menu they appear in; the ones that appear in both enlarge the
# shared base that lowers the second cuisine's cost (docs/09 "the shared
# base").
#
# FAST FOOD IS FROZEN AND PACKAGED. docs/13 promised a different
# perishableRatio for the two cuisines (fast food 0.20 - Turkish 0.60)
# and it was measured: both were 0.58. So the promised difference DID NOT
# EXIST AT ALL; on that axis the two cuisines were exactly alike.
#
# The difference has to be real, because it is the most concrete
# difference in play between the cuisines: fast food FORGIVES (wings,
# fillet, patties, ice cream mix all come out of the freezer; sauce,
# pickles and jalapenos come in a jar), the Turkish kitchen DOES NOT
# FORGIVE (everything is fresh). You survive a bad day in fast food, you
# pay for it in the Turkish kitchen - and cold storage is a necessity far
# earlier there.
#
# The rule: an ingredient SPECIFIC to fast food does not perish if in a
# real restaurant it arrives frozen or in a jar. The ones that stay fresh
# are bread, lettuce, cabbage and apple - that is, the things that go ON
# TOP of the burger.
SPECIFIC = [
    # -- meat and chicken
    ("kiyma",             8500, True,   1, "et",       "kahraman", BOTH),
    ("tavuk_gogus",       5400, True,   2, "et",       "kahraman", BOTH),
    # tavuk_kanat (chicken wings) is fast food only now: once grilled
    # wings left the Turkish restaurant, it was left here with no recipe.
    ("tavuk_kanat",       4200, False,   0, "et",       "orta",     [FF]),
    ("doner_eti",         9200, True,   1, "et",       "kahraman", [TR]),
    ("balik_filetosu",    8200, False,   0, "et",       "kahraman", [FF]),
    ("sosis",             6400, False,   0, "et",       "orta",     [FF]),
    ("dana_kusbasi",     13000, True,   2, "et",       "kahraman", [TR]),
    ("kuzu_kusbasi",     17000, True,   2, "et",       "kahraman", [TR]),
    ("kuzu_pirzola_et",  19000, True,   2, "et",       "kahraman", [TR]),
    ("iskembe",           5000, True,   1, "et",       "orta",     [TR]),
    # -- bread and dough
    ("burger_ekmek",      2600, True,   3, "sabit",    "orta",     [FF]),
    ("hotdog_ekmek",      2800, True,   3, "sabit",    "orta",     [FF]),
    ("tost_ekmegi",       2800, True,   3, "sabit",    "orta",     [FF]),
    ("lavas",             2400, True,   4, "sabit",    "orta",     BOTH),
    ("yufka",             3200, True,   5, "sabit",    "orta",     [TR]),
    ("makarna",           2600, False,  0, "sabit",    "temel",    [TR]),
    ("galeta_unu",        2800, False,  0, "sabit",    "temel",    BOTH),
    ("misir_nisastasi",   3200, False,  0, "sabit",    "temel",    [TR]),
    ("kabartma_tozu",     9500, False,  0, "sabit",    "temel",    [FF]),
    ("maya",              8500, False,  0, "sabit",    "temel",    [FF]),
    ("irmik",             2600, False,  0, "sabit",    "temel",    [TR]),
    # -- dairy
    ("kasar",            12000, True,  12, "sut",      "orta",     BOTH),
    ("mozzarella",       14000, False,  0, "sut",      "orta",     [FF]),
    ("yogurt",            3200, True,   6, "sut",      "orta",     BOTH),
    ("beyaz_peynir",     13000, True,  12, "sut",      "orta",     [TR]),
    ("dondurma_karisimi", 6600, False,   0, "sut",      "orta",     [FF]),
    # -- vegetables and greens
    ("patates",           1900, True,  25, "sebze",    "temel",    BOTH),
    ("marul",             3000, True,   3, "yesillik", "orta",     [FF]),
    ("lahana",            1400, True,  12, "sebze",    "temel",    [FF]),
    ("havuc",             1600, True,  18, "sebze",    "temel",    BOTH),
    ("jalapeno",          5600, False,  0, "sebze",    "temel",    [FF]),
    ("tursu",             3400, False,  0, "sabit",    "temel",    [FF]),
    ("patlican",          2600, True,   6, "sebze",    "orta",     [TR]),
    ("yesil_biber",       2800, True,   5, "sebze",    "orta",     [TR]),
    ("kabak",             1900, True,   7, "sebze",    "temel",    [TR]),
    ("bamya",             5400, True,   4, "sebze",    "orta",     [TR]),
    ("taze_fasulye",      3000, True,   4, "sebze",    "orta",     [TR]),
    ("salatalik",         2200, True,   6, "sebze",    "temel",    [TR]),
    ("maydanoz",          2400, True,   3, "yesillik", "temel",    [TR]),
    # -- pulses and grains
    ("kuru_fasulye_tane", 3800, False,  0, "sabit",    "orta",     [TR]),
    ("nohut_tane",        3400, False,  0, "sabit",    "orta",     [TR]),
    ("mercimek",          3200, False,  0, "sabit",    "orta",     [TR]),
    ("bulgur",            2400, False,  0, "sabit",    "temel",    [TR]),
    ("pirinc",            3600, False,  0, "sabit",    "orta",     [TR]),
    # -- sauces, spices, sweeteners
    ("burger_sos",        7200, False,  0, "sabit",    "orta",     [FF]),
    ("acili_sos",         7600, False,  0, "sabit",    "orta",     [FF]),
    ("ketcap",            4400, False,  0, "sabit",    "temel",    [FF]),
    ("mayonez",           5800, False,  0, "sabit",    "temel",    [FF]),
    ("salca",             5200, True,  30, "sabit",    "orta",     [TR]),
    ("sirke",             3200, False,  0, "sabit",    "temel",    [TR]),
    ("baharat_karisimi", 16000, False,  0, "sabit",    "orta",     BOTH),
    ("kirmizi_biber",    18000, False,  0, "sabit",    "temel",    [TR]),
    ("kimyon",           20000, False,  0, "sabit",    "temel",    [TR]),
    ("nane",              3400, False,  0, "sabit",    "temel",    [TR]),
    ("tarcin",           26000, False,  0, "sabit",    "temel",    BOTH),
    # -- drink and dessert inputs
    ("gazoz_surubu",      6000, False,  0, "sabit",    "temel",    [FF]),
    ("kola_surubu",       6600, False,  0, "sabit",    "temel",    [FF]),
    ("cay",              24000, False,  0, "sabit",    "orta",     BOTH),
    ("limon",             2800, True,  15, "meyve",    "temel",    BOTH),
    ("elma",              2600, True,  20, "meyve",    "temel",    [FF]),
    ("cikolata",         18000, False,  0, "sabit",    "orta",     [FF]),
    ("kakao",            20000, False,  0, "sabit",    "orta",     [FF]),
    ("ceviz",            34000, False,  0, "sabit",    "orta",     [TR]),
    ("kadayif_tel",       4600, True,   8, "sabit",    "orta",     [TR]),
    ("vejetaryen_kofte",  5200, False,   0, "sabit",    "orta",     [FF]),
]

# ---------------------------------------------------------------------------
# Dishes
#   (id, group, station, complexity, prepMs, season, base, [toppings],
#    [(ingredient, grams), ...])
# Season 0 = the menu that is open on the campaign's first day (docs/09
# "the opening menu"); in the JSON it becomes unlockSeason 1 and takes
# unlockDay 1.
# ---------------------------------------------------------------------------
FASTFOOD_DISHES = [
    # ---- Mains, 12 items
    ("hamburger", "ana", "izgara", 1, 75000, 0, "bun", ["patty", "leaf", "slice"], [
        ("kiyma", 120), ("burger_ekmek", 80), ("marul", 15), ("domates", 25),
        ("sogan", 10), ("tursu", 10), ("burger_sos", 20)]),
    ("hot_dog", "ana", "izgara", 1, 60000, 0, "bun", ["strip", "sauce"], [
        ("sosis", 90), ("hotdog_ekmek", 70), ("ketcap", 15), ("mayonez", 15),
        ("sogan", 10)]),
    ("cizburger", "ana", "izgara", 1, 80000, 1, "bun", ["patty", "slice", "leaf"], [
        ("kiyma", 120), ("burger_ekmek", 80), ("kasar", 25), ("marul", 15),
        ("domates", 25), ("sogan", 10), ("burger_sos", 20)]),
    ("kasarli_tost", "ana", "izgara", 1, 70000, 1, "paper", ["slice"], [
        ("tost_ekmegi", 110), ("kasar", 60), ("tereyagi", 10)]),
    ("tavuk_burger", "ana", "izgara", 2, 105000, 1, "bun", ["patty", "leaf", "sauce"], [
        ("tavuk_gogus", 130), ("burger_ekmek", 80), ("marul", 20),
        ("mayonez", 25), ("tursu", 10)]),
    ("duble_burger", "ana", "izgara", 2, 110000, 2, "bun", ["patty", "slice", "leaf"], [
        ("kiyma", 220), ("burger_ekmek", 90), ("kasar", 25), ("marul", 15),
        ("domates", 25), ("sogan", 10), ("burger_sos", 25)]),
    ("acili_burger", "ana", "izgara", 2, 100000, 2, "bun", ["patty", "sauce", "leaf"], [
        ("kiyma", 120), ("burger_ekmek", 80), ("jalapeno", 20), ("acili_sos", 25),
        ("kasar", 20), ("marul", 15), ("sogan", 10)]),
    ("crispy_tavuk", "ana", "fritoz", 2, 130000, 2, "paper", ["patty", "sauce"], [
        ("tavuk_gogus", 160), ("galeta_unu", 40), ("un", 20), ("yumurta", 25),
        ("aycicek_yagi", 30), ("baharat_karisimi", 5)]),
    ("tavuk_durum", "ana", "izgara", 2, 120000, 2, "paper", ["strip", "leaf", "sauce"], [
        ("tavuk_gogus", 120), ("lavas", 90), ("marul", 20), ("domates", 25),
        ("sogan", 10), ("mayonez", 20)]),
    ("balik_burger", "ana", "fritoz", 2, 125000, 3, "bun", ["patty", "leaf", "sauce"], [
        ("balik_filetosu", 130), ("burger_ekmek", 80), ("galeta_unu", 25),
        ("marul", 15), ("mayonez", 20), ("limon", 10)]),
    ("vejetaryen_burger", "ana", "izgara", 2, 115000, 3, "bun", ["patty", "leaf", "slice"], [
        ("vejetaryen_kofte", 130), ("burger_ekmek", 80), ("marul", 20),
        ("domates", 25), ("sogan", 10), ("burger_sos", 20)]),
    ("et_durum", "ana", "izgara", 2, 135000, 3, "paper", ["strip", "leaf", "sauce"], [
        ("kiyma", 130), ("lavas", 90), ("marul", 20), ("domates", 25),
        ("sogan", 10), ("acili_sos", 15)]),
    # ---- Sides, 8 items
    ("patates_kizartma", "yan", "fritoz", 1, 55000, 0, "paper", ["strip"], [
        ("patates", 200), ("aycicek_yagi", 25), ("tuz", 2)]),
    ("nugget", "yan", "fritoz", 1, 65000, 0, "paper", ["sphere", "sauce"], [
        ("tavuk_gogus", 110), ("galeta_unu", 30), ("un", 15), ("yumurta", 15),
        ("aycicek_yagi", 20)]),
    ("baharatli_patates", "yan", "fritoz", 1, 60000, 1, "paper", ["strip", "sauce"], [
        ("patates", 200), ("aycicek_yagi", 25), ("baharat_karisimi", 6)]),
    ("yesil_salata", "yan", "soguk", 1, 45000, 1, "bowl", ["leaf", "slice"], [
        ("marul", 90), ("domates", 50), ("havuc", 30), ("zeytinyagi", 10)]),
    ("sogan_halkasi", "yan", "fritoz", 1, 70000, 2, "paper", ["sphere"], [
        ("sogan", 140), ("un", 30), ("galeta_unu", 25), ("yumurta", 20),
        ("aycicek_yagi", 25)]),
    ("acili_kanat", "yan", "fritoz", 2, 140000, 2, "paper", ["sphere", "sauce"], [
        ("tavuk_kanat", 180), ("acili_sos", 30), ("baharat_karisimi", 4),
        ("aycicek_yagi", 15)]),
    ("mozzarella_cubuk", "yan", "fritoz", 1, 80000, 3, "paper", ["stick", "sauce"], [
        ("mozzarella", 100), ("galeta_unu", 30), ("un", 15), ("yumurta", 15),
        ("aycicek_yagi", 20)]),
    ("coleslaw", "yan", "soguk", 1, 40000, 3, "bowl", ["strip"], [
        ("lahana", 120), ("havuc", 40), ("mayonez", 35), ("seker", 5),
        ("limon", 5)]),
    # ---- Drinks, 6 items
    ("gazoz", "icecek", "icecek", 1, 20000, 0, "cup", ["stick"], [
        ("gazoz_surubu", 60), ("seker", 20)]),
    ("kola", "icecek", "icecek", 1, 20000, 1, "cup", ["stick"], [
        ("kola_surubu", 60), ("seker", 20)]),
    ("limonata", "icecek", "icecek", 1, 35000, 1, "cup", ["slice", "stick"], [
        ("limon", 150), ("seker", 40)]),
    ("milkshake", "icecek", "milkshake_makinesi", 1, 50000, 2, "cup", ["sphere", "stick"], [
        ("sut", 200), ("dondurma_karisimi", 90), ("seker", 20)]),
    ("ayran", "icecek", "icecek", 1, 25000, 4, "cup", [], [
        ("yogurt", 150), ("tuz", 3)]),
    ("buzlu_cay", "icecek", "icecek", 1, 40000, 4, "cup", ["slice", "stick"], [
        ("cay", 8), ("seker", 30), ("limon", 20)]),
    # ---- Desserts, 6 items
    ("dondurma", "tatli", "tatli", 1, 30000, 0, "cup", ["sphere"], [
        ("dondurma_karisimi", 80), ("sut", 30), ("seker", 10)]),
    ("elmali_turta", "tatli", "firin", 3, 240000, 2, "plate_round", ["slice", "sauce"], [
        ("elma", 120), ("un", 80), ("tereyagi", 40), ("seker", 30),
        ("tarcin", 3), ("yumurta", 20)]),
    ("cikolatali_kek", "tatli", "firin", 3, 260000, 3, "plate_round", ["slice", "sauce"], [
        ("un", 80), ("cikolata", 50), ("seker", 40), ("yumurta", 40),
        ("tereyagi", 30), ("kakao", 10), ("kabartma_tozu", 3)]),
    ("donut", "tatli", "firin", 2, 160000, 4, "paper", ["slice"], [
        ("un", 90), ("seker", 35), ("maya", 4), ("sut", 40), ("yumurta", 20),
        ("aycicek_yagi", 30), ("cikolata", 15)]),
    ("brownie", "tatli", "firin", 2, 175000, 4, "plate_round", ["slice", "sauce"], [
        ("un", 55), ("cikolata", 35), ("kakao", 10), ("seker", 35),
        ("tereyagi", 30), ("yumurta", 25)]),
    ("waffle", "tatli", "waffle_makinesi", 2, 130000, 4, "plate_oval", ["slice", "sphere", "sauce"], [
        ("un", 70), ("sut", 60), ("yumurta", 25), ("seker", 20),
        ("tereyagi", 20), ("cikolata", 20)]),
]

TURK_DISHES = [
    # ---- Stews, 9 items
    ("kuru_fasulye", "sulu", "ocak", 3, 300000, 0, "bowl", ["sphere", "sauce"], [
        ("kuru_fasulye_tane", 130), ("dana_kusbasi", 70), ("salca", 25),
        ("sogan", 35), ("aycicek_yagi", 18), ("tuz", 3)]),
    ("nohut", "sulu", "ocak", 3, 290000, 1, "bowl", ["sphere", "sauce"], [
        ("nohut_tane", 130), ("dana_kusbasi", 60), ("salca", 25), ("sogan", 35),
        ("aycicek_yagi", 18)]),
    ("etli_turlu", "sulu", "ocak", 3, 320000, 2, "plate_round", ["sphere", "sauce"], [
        ("dana_kusbasi", 90), ("patlican", 50), ("kabak", 50), ("yesil_biber", 30),
        ("domates", 60), ("sogan", 30), ("zeytinyagi", 15)]),
    ("karniyarik", "sulu", "tas_firin", 3, 360000, 2, "plate_oval", ["slice", "sphere", "sauce"], [
        ("patlican", 180), ("kiyma", 80), ("domates", 50), ("yesil_biber", 25),
        ("sogan", 30), ("aycicek_yagi", 20), ("maydanoz", 5)]),
    ("taze_fasulye", "sulu", "ocak", 3, 280000, 2, "plate_round", ["strip", "sauce"], [
        ("taze_fasulye", 200), ("domates", 50), ("sogan", 30), ("zeytinyagi", 25),
        ("tuz", 3)]),
    ("imambayildi", "sulu", "ocak", 3, 330000, 3, "plate_oval", ["slice", "leaf"], [
        ("patlican", 180), ("sogan", 60), ("domates", 60), ("sarimsak", 8),
        ("zeytinyagi", 35), ("maydanoz", 5)]),
    ("musakka", "sulu", "tas_firin", 3, 340000, 3, "plate_round", ["slice", "sauce"], [
        ("patlican", 150), ("kiyma", 90), ("domates", 50), ("yesil_biber", 20),
        ("sogan", 30), ("sut", 40), ("un", 10), ("tereyagi", 10)]),
    ("etli_bamya", "sulu", "ocak", 3, 300000, 3, "bowl", ["sphere", "sauce"], [
        ("bamya", 140), ("kuzu_kusbasi", 70), ("domates", 50), ("sogan", 30),
        ("limon", 10), ("aycicek_yagi", 15)]),
    ("patlican_kebabi", "sulu", "tas_firin", 3, 350000, 4, "plate_oval", ["slice", "sphere", "sauce"], [
        ("patlican", 150), ("kiyma", 110), ("domates", 50), ("yesil_biber", 25),
        ("sarimsak", 5), ("aycicek_yagi", 15)]),
    # ---- Soups, 4 items
    ("mercimek_corbasi", "corba", "ocak", 2, 150000, 0, "bowl", ["steam", "slice"], [
        ("mercimek", 90), ("sogan", 30), ("havuc", 30), ("patates", 40),
        ("tereyagi", 12), ("un", 10), ("kirmizi_biber", 2), ("tuz", 3)]),
    ("ezogelin", "corba", "ocak", 2, 165000, 1, "bowl", ["steam"], [
        ("mercimek", 80), ("bulgur", 25), ("pirinc", 15), ("salca", 15),
        ("sogan", 30), ("nane", 3), ("tereyagi", 10)]),
    ("yayla_corbasi", "corba", "ocak", 2, 155000, 2, "bowl", ["steam", "leaf"], [
        ("yogurt", 120), ("pirinc", 30), ("un", 10), ("yumurta", 15),
        ("nane", 3), ("tereyagi", 10)]),
    ("iskembe_corbasi", "corba", "ocak", 3, 400000, 4, "bowl", ["steam", "sauce"], [
        ("iskembe", 180), ("un", 15), ("sarimsak", 8), ("sirke", 8),
        ("tereyagi", 12), ("limon", 8)]),
    # ---- Pilaf and pastry, 4 items
    ("pirinc_pilavi", "pilav", "ocak", 1, 80000, 0, "plate_round", ["sphere"], [
        ("pirinc", 90), ("makarna", 10), ("tereyagi", 15), ("tuz", 3)]),
    ("bulgur_pilavi", "pilav", "ocak", 1, 75000, 1, "plate_round", ["sphere"], [
        ("bulgur", 90), ("salca", 10), ("sogan", 20), ("tereyagi", 12),
        ("yesil_biber", 15)]),
    ("borek", "pilav", "tas_firin", 3, 300000, 2, "plate_oval", ["slice"], [
        ("yufka", 120), ("beyaz_peynir", 60), ("maydanoz", 6), ("yumurta", 25),
        ("sut", 40), ("aycicek_yagi", 20)]),
    ("manti", "pilav", "ocak", 3, 480000, 4, "bowl", ["sphere", "sauce"], [
        ("un", 90), ("kiyma", 60), ("sogan", 20), ("yogurt", 80),
        ("tereyagi", 15), ("kirmizi_biber", 2), ("nane", 2)]),
    # ---- Grill, 8 items (four of them tied to named equipment)
    ("kofte", "izgara", "izgara", 2, 120000, 0, "plate_oval", ["sphere", "leaf", "sauce"], [
        ("kiyma", 220), ("sogan", 25), ("galeta_unu", 15), ("yumurta", 10),
        ("karabiber", 2), ("kimyon", 2)]),
    ("tavuk_sis", "izgara", "izgara", 2, 140000, 1, "plate_oval", ["stick", "sphere", "leaf"], [
        ("tavuk_gogus", 200), ("yogurt", 25), ("yesil_biber", 25), ("sogan", 20),
        ("baharat_karisimi", 3)]),
    ("adana", "izgara", "izgara", 2, 150000, 2, "plate_oval", ["strip", "leaf", "sauce"], [
        ("kiyma", 200), ("kirmizi_biber", 4), ("kimyon", 3), ("sogan", 20),
        ("lavas", 60)]),
    # Doner and pide: the named equipment the user asked for. "Grill tier
    # 2 required" is abstract; "buy the doner grill and doner opens" reads.
    # Both are in the main role, so they touch the spine of the menu.
    ("doner", "izgara", "doner_ocagi", 2, 130000, 2, "plate_oval", ["strip", "leaf", "sauce"], [
        ("doner_eti", 180), ("lavas", 70), ("domates", 40), ("sogan", 25),
        ("maydanoz", 5), ("aycicek_yagi", 8)]),
    ("iskender", "izgara", "doner_ocagi", 3, 190000, 4, "plate_round", ["strip", "sauce", "sphere"], [
        ("doner_eti", 170), ("lavas", 90), ("salca", 30), ("yogurt", 70),
        ("tereyagi", 25), ("domates", 40)]),
    ("kiymali_pide", "izgara", "pide_firini", 2, 145000, 3, "tray", ["strip", "slice", "sauce"], [
        ("un", 110), ("kiyma", 90), ("kasar", 35), ("domates", 35),
        ("yesil_biber", 20), ("tereyagi", 12), ("yumurta", 15)]),
    ("lahmacun", "izgara", "pide_firini", 2, 110000, 4, "tray", ["strip", "leaf", "sauce"], [
        ("un", 80), ("kiyma", 60), ("domates", 45), ("sogan", 25),
        ("salca", 15), ("maydanoz", 6), ("kirmizi_biber", 3)]),
    ("kuzu_pirzola", "izgara", "izgara", 2, 130000, 3, "plate_oval", ["slice", "leaf"], [
        ("kuzu_pirzola_et", 220), ("tuz", 3), ("karabiber", 2), ("zeytinyagi", 10)]),
    # ---- Meze and salad, 3 items
    ("coban_salata", "meze", "soguk", 1, 50000, 1, "bowl", ["slice", "leaf"], [
        ("domates", 100), ("salatalik", 80), ("yesil_biber", 30), ("sogan", 25),
        ("maydanoz", 8), ("zeytinyagi", 12), ("limon", 8)]),
    ("cacik", "meze", "soguk", 1, 40000, 1, "bowl", ["sauce", "leaf"], [
        ("yogurt", 150), ("salatalik", 70), ("sarimsak", 5), ("nane", 2),
        ("zeytinyagi", 8)]),
    ("piyaz", "meze", "soguk", 1, 60000, 1, "plate_oval", ["sphere", "slice", "leaf"], [
        ("kuru_fasulye_tane", 90), ("sogan", 30), ("domates", 40), ("maydanoz", 6),
        ("sirke", 10), ("zeytinyagi", 15), ("yumurta", 30)]),
    # ---- Desserts, 3 items
    ("sutlac", "tatli", "tatli", 2, 175000, 0, "bowl", ["sauce"], [
        ("sut", 250), ("pirinc", 30), ("seker", 45), ("misir_nisastasi", 8),
        ("tarcin", 2)]),
    ("kadayif", "tatli", "tas_firin", 3, 220000, 2, "plate_round", ["strip", "sphere"], [
        ("kadayif_tel", 100), ("ceviz", 30), ("seker", 60), ("tereyagi", 30),
        ("limon", 5)]),
    ("revani", "tatli", "tas_firin", 3, 200000, 3, "plate_round", ["slice", "sauce"], [
        ("irmik", 80), ("un", 30), ("seker", 70), ("yumurta", 40),
        ("yogurt", 40), ("limon", 5)]),
    # ---- Drinks, 1 item. docs/09 does not define a drinks group for this
    # cuisine; docs/12 prices ayran and says "a typical order: stew + pilaf
    # + ayran".
    ("ayran", "icecek", "icecek", 1, 25000, 0, "cup", [], [
        ("yogurt", 150), ("tuz", 3)]),
]

# ---------------------------------------------------------------------------
# Generation
# ---------------------------------------------------------------------------
def ensure(d):
    if not os.path.isdir(d):
        os.makedirs(d)


def round50(x):
    """Round to the nearest 50 centi-coins. Half goes away from zero."""
    return ((x + 25) // 50) * 50


def build_ingredients():
    out = []
    for iid, price, perish, spoil, season, quality in PANTRY:
        out.append(one_ingredient(iid, price, perish, spoil, season, quality,
                                  BOTH, True))
    for iid, price, perish, spoil, season, quality, cuisines in SPECIFIC:
        out.append(one_ingredient(iid, price, perish, spoil, season, quality, cuisines, False))
    return out


def one_ingredient(iid, price, perish, spoil, season, quality, cuisines, shared):
    spring, summer, autumn, winter = SEASON[season]
    qp, qs = QUALITY[quality]
    return {
        "id": iid,
        "nameKey": "ingredient." + iid,
        "shared": shared,
        "cuisines": list(cuisines),
        # Centi-coin / kilogram. Every ingredient is in kg; the recipe's
        # gram weight works by dividing, so the cost comes out of a single
        # integer multiplication.
        "basePrice": price,
        "unit": "kg",
        "perishable": perish,
        "spoilDays": spoil,
        # The JSON field names stay as they are: content/ and the C#
        # loader read them by these names.
        "seasonModifierBp": {"ilkbahar": spring, "yaz": summer,
                             "sonbahar": autumn, "kis": winter},
        "qualityPriceMultiplierBp": {"dusuk": qp[0], "standart": qp[1], "yuksek": qp[2]},
        "qualitySatisfactionCenti": {"dusuk": qs[0], "standart": qs[1], "yuksek": qs[2]},
    }


def cost_milli(recipe, prices):
    """The recipe's cost, centi-coin * 1000. We do not divide, so that it
    stays an integer."""
    return sum(prices[iid] * grams for iid, grams in recipe)


def unlock_days(rows):
    """Spreads the unlock days out within the season.

    The opening menu (season field 0) is day 1. The rest of the first
    season spreads over 3..15, later seasons over their own 15-day
    window. docs/09: "a new dish every two days on average".
    """
    days = {}
    buckets = {1: [], 2: [], 3: [], 4: []}
    for row in rows:
        s = row[5]
        if s == 0:
            days[row[0]] = (1, 1)
        else:
            buckets[s].append(row[0])
    for s, ids in buckets.items():
        n = len(ids)
        for i, did in enumerate(ids):
            if s == 1:
                day = 1 + ((i + 1) * 14) // max(n, 1)
            else:
                day = (s - 1) * 15 + ((i + 1) * 15) // (n + 1)
            days[did] = (s, day)
    return days


# ---------------------------------------------------------------------------
# Deriving prepMs  (docs/23 8.3, the capacity model in docs/14)
# ---------------------------------------------------------------------------
# A service day is 480,000 ms; a cook's capacity is 28 PEOPLE per day. In
# the simulation each person orders one plate, so the kitchen budget per
# person is:
#     480,000 / 28 = 17,142 ms
# The menu's average prepMs is set equal to that number. Complexity only
# determines the ratio BETWEEN dishes, not the absolute value.
SERVICE_DAY_MS = 480_000
COOK_CAPACITY_PER_DAY = 28
KITCHEN_MS_PER_PERSON = SERVICE_DAY_MS // COOK_CAPACITY_PER_DAY

# How many PLATES are cooked per person. The order model is in
# content/economy.json: one main for certain, a side and a drink by
# chance. The budget is per person; the time per plate is found by
# dividing it.
def _avg_dishes_per_person():
    path = os.path.join(CONTENT, "economy.json")
    try:
        with io.open(path, encoding="utf-8") as f:
            eco = json.load(f)
        o = eco.get("order") or {}
        side = o.get("sideChanceBp", 3000)
        drink = o.get("drinkChanceBp", 4000)
        return 1.0 + side / 10000.0 + drink / 10000.0
    except (IOError, ValueError):
        return 1.7


AVG_DISHES_PER_PERSON = _avg_dishes_per_person()
MS_PER_DISH = int(KITCHEN_MS_PER_PERSON / AVG_DISHES_PER_PERSON)

# Relative weight by complexity
PREP_WEIGHT = {1: 70, 2: 100, 3: 150}


def derive_prep_ms(dishes):
    """
    Assigns prepMs so that the menu's average is KITCHEN_MS_PER_PERSON.
    Modifies in place and returns (min, average, max).
    """
    total_w = 0
    for d in dishes:
        total_w += PREP_WEIGHT[d["complexity"]]
    if total_w == 0:
        return (0, 0, 0)

    # scale = target_average * count / total_weight
    n = len(dishes)
    lo, hi, tot = None, None, 0
    for d in dishes:
        w = PREP_WEIGHT[d["complexity"]]
        ms = (MS_PER_DISH * n * w) // total_w
        ms = int(round(ms / 500.0)) * 500          # round to 500 ms
        if ms < 3000:
            ms = 3000                               # even a drink takes something
        d["prepMs"] = ms
        tot += ms
        lo = ms if lo is None or ms < lo else lo
        hi = ms if hi is None or ms > hi else hi
    return (lo, tot // n, hi)


# The lock thresholds. docs/34 4: a dish is locked by REPUTATION +
# EQUIPMENT, not by the CALENDAR. The rule works off the cuisine's OWN
# distribution, because an absolute threshold such as "complexity 3 ->
# tier 2" caught 2 of the 32 fast food dishes and 17 of the 32 Turkish
# ones. The dishes are sorted by (complexity, price) and split: the
# bottom 53% need no equipment, the next 28% tier 1, the top 19% tier 2.
# On 32 dishes that means 17 / 9 / 6 places. The thresholds look like odd
# decimals because they were chosen by the COUNT: the balancing was done
# with this split, and one dish changing tier shifts the measurement
# visibly (in fast food, a single cheeseburger moving to tier 1 produced
# the "a reasonable player cannot expand" violation).
TIER0_BP = 5300
TIER1_BP = 8125

# The reputation threshold is directly proportional to the day: 0 on
# opening day, then 0.9 points per day. 53.1 points on day sixty - so the
# last dishes open in a well-run restaurant and never open in a badly run
# one.
REP_PER_DAY_CENTI = 90


# HOW MANY TIERS a station has. It cannot be derived from
# equipment.json, because that file is FURTHER DOWN this generator's
# output chain; the values come from the peak table in docs/27 3.3 and
# export.py verifies them there.
#
# Without this table the generator can ask for a tier that does not
# exist: three fast food desserts asked for "oven tier 2" while the oven
# ladder ends at tier 1 - so those three dishes COULD NOT OPEN for sixty
# days and no validation caught it.
STATION_TIERS = {
    "ocak": 4,
    "izgara": 4,
    "firin": 2,
    "soguk": 2,
    "icecek": 2,
    "tatli": 2,
}

# Named kitchen equipment has two steps: absent / present.
NAMED_STATION_TIERS = 2


def max_tier(station):
    return STATION_TIERS.get(station, NAMED_STATION_TIERS) - 1


def unlock_gates(rows, cuisine, dishes, days):
    """
    Writes requiresStationTier and unlockReputationCenti in place.

    These two fields were once written by hand straight into
    content/dishes/*.json and THE GENERATOR DID NOT KNOW ABOUT THEM:
    running gen_dishes.py silently wiped the entire lock system. They are
    generated here now.
    """
    named = set(CUISINE_STATIONS.get(cuisine, ()))
    n = len(dishes)
    order = sorted(range(n), key=lambda i: (dishes[i]["complexity"],
                                            dishes[i]["price"],
                                            dishes[i]["id"]))
    for rank, i in enumerate(order):
        d = dishes[i]
        if rank * 10000 < n * TIER0_BP:
            tier = 0
        elif rank * 10000 < n * TIER1_BP:
            tier = 1
        else:
            tier = 2

        # Named equipment asks for EXACTLY tier 1: that ladder has two
        # steps (absent / present), there is no such thing as tier 2.
        if d["station"] in named:
            tier = 1

        # The opening menu asks for nothing, or the game starts locked.
        if d["unlockDay"] <= 1:
            if d["station"] in named:
                raise AssertionError(
                    cuisine + "/" + d["id"] +
                    ": named equipment cannot be in the opening menu")
            tier = 0

        # Capped at the highest tier the station actually HAS.
        top = max_tier(d["station"])
        if tier > top:
            tier = top

        d["requiresStationTier"] = tier
        d["unlockReputationCenti"] = (d["unlockDay"] - 1) * REP_PER_DAY_CENTI


def build_dishes(rows, cuisine, prices):
    days = unlock_days(rows)
    out = []
    for did, group, station, cx, prep, _season, base, tops, recipe in rows:
        cm = cost_milli(recipe, prices)
        target = GROUP_TARGET_BP[cuisine][group]
        price = round50(cm * 10 // target)
        season, day = days[did]
        out.append({
            "id": did,
            "nameKey": "dish." + did,
            "cuisine": cuisine,
            "group": group,
            "price": price,
            "prepMs": prep,
            "station": station,
            "complexity": cx,
            "unlockSeason": season,
            "unlockDay": day,
            "requiresStationTier": 0,
            "unlockReputationCenti": 0,
            "ingredients": [{"id": iid, "grams": g} for iid, g in recipe],
            "plating": {"base": base, "toppings": list(tops)},
        })
    unlock_gates(rows, cuisine, out, days)
    return out


# ---------------------------------------------------------------------------
# Checks
# ---------------------------------------------------------------------------
class Report(object):
    def __init__(self):
        self.errors = []

    def fail(self, msg):
        self.errors.append(msg)


def check_no_float(obj, path, rep):
    """8.4: no decimal point in the JSON. bool must be caught before
    number."""
    if isinstance(obj, bool):
        return
    if isinstance(obj, float):
        rep.fail("decimal value: " + path)
    elif isinstance(obj, dict):
        for k, v in obj.items():
            check_no_float(v, path + "." + str(k), rep)
    elif isinstance(obj, list):
        for i, v in enumerate(obj):
            check_no_float(v, path + "[" + str(i) + "]", rep)


def check_ingredients(ings, rep):
    seen = set()
    for ing in ings:
        iid = ing["id"]
        if not ID_RE.match(iid):
            rep.fail("invalid ingredient id: " + iid)
        if iid in seen:
            rep.fail("duplicate ingredient id: " + iid)
        seen.add(iid)
        if ing["basePrice"] <= 0:
            rep.fail("zero price: " + iid)
        if ing["perishable"] and ing["spoilDays"] <= 0:
            rep.fail("perishable but no spoilDays: " + iid)
        if not ing["perishable"] and ing["spoilDays"] != 0:
            rep.fail("not perishable but has spoilDays: " + iid)
    return seen


def check_dishes(dishes, cuisine, ing_ids, ing_by_id, prices, rep):
    """Checks the rules dish by dish. Also prints the ingredient ratio
    report."""
    seen = set()
    ratios = []
    season_count = {1: 0, 2: 0, 3: 0, 4: 0}
    day1_roles = set()
    day1 = 0
    used = set()

    print("")
    print("--- " + cuisine + " ---")
    print("id                     group   sea day  price  ingred.  ratio   prepMs  c")
    for d in dishes:
        did = d["id"]
        if not ID_RE.match(did):
            rep.fail(cuisine + " invalid dish id: " + did)
        if did in seen:
            rep.fail(cuisine + " duplicate dish id: " + did)
        seen.add(did)

        if (d["station"] not in STATIONS
                and d["station"] not in CUISINE_STATIONS.get(cuisine, ())):
            rep.fail(cuisine + "/" + did + " invalid station: " + d["station"])
        if d["plating"]["base"] not in BASES:
            rep.fail(cuisine + "/" + did + " invalid plate base")
        if len(d["plating"]["toppings"]) > 3:
            rep.fail(cuisine + "/" + did + " more than three toppings")
        for t in d["plating"]["toppings"]:
            if t not in TOPPINGS:
                rep.fail(cuisine + "/" + did + " invalid topping: " + t)
        if d["complexity"] not in (1, 2, 3):
            rep.fail(cuisine + "/" + did + " complexity outside 1-3")
        if d["unlockSeason"] not in (1, 2, 3, 4):
            rep.fail(cuisine + "/" + did + " season outside 1-4")

        # the prepMs - complexity tie
        cx, prep = d["complexity"], d["prepMs"]
        if cx == 1 and not prep < 90000:
            rep.fail(cuisine + "/" + did + " complexity 1 but prepMs " + str(prep))
        if cx == 2 and not (90000 <= prep <= 180000):
            rep.fail(cuisine + "/" + did + " complexity 2 but prepMs " + str(prep))
        if cx == 3 and not prep > 180000:
            rep.fail(cuisine + "/" + did + " complexity 3 but prepMs " + str(prep))

        recipe = [(x["id"], x["grams"]) for x in d["ingredients"]]
        if not recipe:
            rep.fail(cuisine + "/" + did + " has no ingredients")
        for iid, grams in recipe:
            used.add(iid)
            if iid not in ing_ids:
                rep.fail(cuisine + "/" + did + " unknown ingredient: " + iid)
            elif cuisine not in ing_by_id[iid]["cuisines"]:
                rep.fail(cuisine + "/" + did +
                         " ingredient not in this cuisine: " + iid)
            if grams <= 0:
                rep.fail(cuisine + "/" + did + " zero grams: " + iid)

        cm = cost_milli(recipe, prices)
        ratio = cm * 10 // d["price"]
        ratios.append((ratio, did))
        if not (RATIO_MIN <= ratio <= RATIO_MAX):
            rep.fail(cuisine + "/" + did + " ingredient ratio outside the band: "
                     + str(ratio) + " bp")

        season_count[d["unlockSeason"]] += 1
        if d["unlockDay"] == 1:
            day1 += 1
            day1_roles.add(GROUP_ROLE[d["group"]])
        if not (1 <= d["unlockDay"] <= 60):
            rep.fail(cuisine + "/" + did + " day outside 1-60")

        print("{:22s} {:7s} {:>2d} {:>4d} {:>6d} {:>8d}  {:>4d}bp {:>7d}  {:d}".format(
            did, d["group"], d["unlockSeason"], d["unlockDay"], d["price"],
            cm // 1000, ratio, prep, cx))

    if len(dishes) != 32:
        rep.fail(cuisine + " the dish count is not 32: " + str(len(dishes)))
    if day1 != 6:
        rep.fail(cuisine + " the opening menu is not 6: " + str(day1))
    for role in ("ana", "yan", "icecek"):
        if role not in day1_roles:
            rep.fail(cuisine + " no " + role + " in the opening menu")

    expected = {1: 13, 2: 8, 3: 6, 4: 5}
    if season_count != expected:
        rep.fail(cuisine + " season spread " + str(season_count) +
                 " expected " + str(expected))

    running = 0
    cumulative = []
    for s in (1, 2, 3, 4):
        running += season_count[s]
        cumulative.append(running)
    print("season spread     : " + str([season_count[s] for s in (1, 2, 3, 4)]) +
          "  day1 = " + str(day1))
    print("cumulative menu   : 6 -> " + " -> ".join(str(k) for k in cumulative))
    ratios.sort()
    print("ingredient ratio  : lowest {:d}bp ({:s})  highest {:d}bp ({:s})".format(
        ratios[0][0], ratios[0][1], ratios[-1][0], ratios[-1][1]))

    perish = [i for i in used if ing_by_id[i]["perishable"]]
    print("perishable ratio  : {:d}% ({:d}/{:d} ingredients)".format(
        len(perish) * 100 // len(used), len(perish), len(used)))
    return ratios, used


def write(path, obj):
    ensure(os.path.dirname(path))
    text = json.dumps(obj, ensure_ascii=False, indent=2)
    m = DECIMAL_RE.search(text)
    if m:
        print("ERROR: there is a decimal point -> "
              + text[max(0, m.start() - 40):m.end() + 10])
        sys.exit(1)
    with io.open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(text)
        f.write(u"\n")
    print("written: " + os.path.relpath(path, ROOT).replace("\\", "/"))


def main():
    rep = Report()

    ings = build_ingredients()
    ing_ids = check_ingredients(ings, rep)
    ing_by_id = dict((i["id"], i) for i in ings)
    prices = dict((i["id"], i["basePrice"]) for i in ings)

    ff = build_dishes(FASTFOOD_DISHES, FF, prices)
    tr = build_dishes(TURK_DISHES, TR, prices)

    check_no_float(ings, "ingredients", rep)
    check_no_float(ff, "fastfood", rep)
    check_no_float(tr, "turk", rep)

    r1, u1 = check_dishes(ff, FF, ing_ids, ing_by_id, prices, rep)
    r2, u2 = check_dishes(tr, TR, ing_ids, ing_by_id, prices, rep)

    print("")
    print("--- totals ---")
    print("ingredients       : {:d} ({:d} basic pantry, {:d} cuisine-specific)".format(
        len(ings), len(PANTRY), len(SPECIFIC)))
    print("fast food dishes  : {:d}".format(len(ff)))
    print("turkish dishes    : {:d}".format(len(tr)))
    all_ratios = sorted(r1 + r2)
    print("ingredient ratio  : {:d}bp ({:s}) .. {:d}bp ({:s}), target band {:d}-{:d}".format(
        all_ratios[0][0], all_ratios[0][1], all_ratios[-1][0], all_ratios[-1][1],
        RATIO_MIN, RATIO_MAX))
    print("average ratio     : {:d}bp".format(
        sum(r[0] for r in all_ratios) // len(all_ratios)))

    # The dead-content check: every ingredient outside the pantry must
    # appear in at least one recipe of the cuisine it claims to be in. The
    # only exception is the service items.
    for ing in ings:
        if ing["shared"] or ing["id"] in SERVICE_INGREDIENTS:
            continue
        for cuisine, used in ((FF, u1), (TR, u2)):
            if cuisine in ing["cuisines"] and ing["id"] not in used:
                rep.fail(cuisine + " ingredient in no recipe of this cuisine: " +
                         ing["id"])

    if rep.errors:
        print("")
        print("--- BALANCE ERROR ({:d}) ---".format(len(rep.errors)))
        for e in rep.errors:
            print("  " + e)
        sys.exit(1)

    ff_stats = derive_prep_ms(ff)
    tr_stats = derive_prep_ms(tr)
    print("")
    print("prepMs derived (from the capacity model):")
    print("  kitchen budget / person : {:d} ms".format(KITCHEN_MS_PER_PERSON))
    print("  average plates / person : {:.2f}".format(AVG_DISHES_PER_PERSON))
    print("  budget / plate          : {:d} ms".format(MS_PER_DISH))
    print("  fastfood  min/avg/max   : {:d} / {:d} / {:d} ms".format(*ff_stats))
    print("  turk      min/avg/max   : {:d} / {:d} / {:d} ms".format(*tr_stats))
    for name, stats in (("fastfood", ff_stats), ("turk", tr_stats)):
        drift = abs(stats[1] - MS_PER_DISH)
        if drift * 100 > MS_PER_DISH * 3:
            print("  ERROR: {:s} average drifts more than 3% from the budget"
                  .format(name))
            sys.exit(1)

    write(os.path.join(CONTENT, "ingredients.json"), ings)
    write(os.path.join(DISHES_DIR, "fastfood.json"), ff)
    write(os.path.join(DISHES_DIR, "turk.json"), tr)
    print("every balance rule passed.")


if __name__ == "__main__":
    main()
