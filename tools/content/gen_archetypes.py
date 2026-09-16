# -*- coding: utf-8 -*-
"""
Archetype generator - Phase 0
============================================================================
Writes the twenty archetypes in docs/11-customer-system.md into three files:

  content/archetypes/shared.json    8 shared archetypes (in every cuisine)
  content/archetypes/fastfood.json  12 specific to fast food
  content/archetypes/turk.json      12 specific to the Turkish restaurant

Each cuisine fills its 20 slots by combining its own 12 with the shared 8.
The spread across frequency tiers (docs/11): common 8, mid 8, rare 4.

UNITS - docs/23-core-contract.md 2.2 and 8.4 are binding:
  time             milliseconds   8 s of patience = 8000
  ratio, multiplier basis points (bp)  10000 = 1.0
  satisfaction     centi-points   100 points = 10000
  reputation weight basis points  1.4x = 14000; the critic 8x = 80000
NO decimal point. The validator rejects the file if it sees a dot in the JSON.

WHERE THE NUMBERS COME FROM - docs/12-economy.md:
  5.2 patience   courier 8, student in a hurry 10, lunch-break worker 12,
                 office group 18, family 30, pensioner 40 seconds. Those
                 are fixed; the rest were chosen in the same spirit.
  5.3 price sensitivity range 0.4 - 2.5 -> 4000 - 25000 bp
  5.5 reputation weight; the sharing group three times, the critic highest
  5.6 hourly spread targets:
        fast food  15 / 35 / 15 / 35
        turk       10 / 60 / 20 / 10

THE WEIGHT DESIGN:
  The weights of each cuisine's 20 archetypes total exactly 10000. The
  tier shares hold the traffic share in docs/11: common 7000, mid 2500,
  rare 500. The 8 shared archetypes appear with the same weight in both
  cuisines; the 12 cuisine-specific ones fill the rest. That is why the
  shared profiles were kept neutral - the part that carries a cuisine's
  rhythm is its own archetypes.

Run with:  python gen_archetypes.py
"""
import io
import json
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUTDIR = os.path.join(ROOT, "content", "archetypes")

BP = 10_000          # 1.0 = 10000 basis points
# The slot and tier ids stay as they are: the tier is written into
# content/ as a value ("tier": "sik") and the slot names are the arrival
# buckets the content uses. Opening / midday / afternoon / evening, and
# common / mid / rare.
SLOTS = ("acilis", "ogle", "ogleden_sonra", "aksam")
TIERS = ("sik", "orta", "nadir")

# docs/12 section 5.6, in basis points
HOURLY_TARGET = {
    "fastfood": (1500, 3500, 1500, 3500),
    "turk":     (1000, 6000, 2000, 1000),
}
SPLIT_TOL_BP = 500           # 5 percentage points
RARE_MAX_BP = 1000           # the rare tier stays under 10% of the total weight
SENS_MIN, SENS_MAX = 4000, 25000
PATIENCE_MIN_CEIL = 10_000   # the least patient must be below this
PATIENCE_MAX_FLOOR = 35_000  # the most patient must be above this

# Archetypes whose number is given in docs/12 section 5.2; these cannot
# be changed.
PATIENCE_LOCKED = {
    "kurye": 8_000,
    "aceleci_ogrenci": 10_000,
    "ogle_molasi_calisani": 12_000,
    "ofis_grubu": 18_000,
    "aile": 30_000,
    "emekli": 40_000,
}

ID_RE = re.compile(r"^[a-z0-9_]+$")
DECIMAL_RE = re.compile(r"[0-9]\.[0-9]")

# ---------------------------------------------------------------------------
# Data. Field order:
#   id, tier, weight, patienceMs, priceSensitivityBp, groupMin, groupMax,
#   reputationWeight(bp), tipChanceBp, (opening, midday, afternoon, evening),
#   wardrobe
# ---------------------------------------------------------------------------

# The shared eight. Weights total: common 2600, mid 800, rare 110 = 3510.
SHARED = [
    ("yalniz_musteri",     "sik",   800, 16_000, 10_000, 1, 1, 10_000, 1200,
     (1500, 3800, 2000, 2700), ["gunluk"]),
    ("cift",               "sik",   650, 20_000,  9_000, 2, 2, 10_000, 1800,
     (600, 3200, 2200, 4000), ["sik_gunluk", "gunluk"]),
    # Patience 30 s: docs/12 section 5.2
    ("aile",               "sik",   600, 30_000, 11_000, 3, 5, 14_000, 2000,
     (800, 4200, 1800, 3200), ["gunluk", "cocuk"]),
    # Patience 8 s: docs/12 section 5.2. Takes no table, one item only.
    ("kurye",              "sik",   550,  8_000, 12_000, 1, 1,  6_000,  500,
     (1100, 5200, 1500, 2200), ["kurye_ceket", "kask"]),
    ("cocuklu_ebeveyn",    "orta",  320, 13_000, 11_500, 2, 3, 12_000, 1500,
     (700, 4000, 3000, 2300), ["ebeveyn", "cocuk"]),
    # One-off, low reputation weight (docs/11)
    ("yolcu",              "orta",  260, 15_000,  6_000, 1, 2,  4_000,  800,
     (3000, 3600, 2100, 1300), ["yol_cantasi", "gunluk"]),
    # docs/11: three times the reputation weight
    ("paylasimci",         "orta",  220, 19_000,  8_000, 1, 3, 30_000, 2500,
     (900, 3800, 2300, 3000), ["sik_gunluk", "telefon"]),
    # The example in docs/12 section 5.5: weight 8
    ("yemek_elestirmeni",  "nadir", 110, 26_000,  4_000, 1, 2, 80_000, 3000,
     (600, 4200, 1700, 3500), ["ceket", "defter"]),
]

# The twelve specific to fast food. Weights: common 4400, mid 1700, rare 390 = 6490.
FASTFOOD = [
    # Patience 10 s: docs/12 section 5.2. Tight budget, high sensitivity.
    ("aceleci_ogrenci",     "sik",  1300, 10_000, 22_000, 1, 2,  8_000,  400,
     (2000, 5000, 1500, 1500), ["okul_cantasi", "kapusonlu"]),
    # Patience 18 s: docs/12 section 5.2. A sharp midday peak.
    ("ofis_grubu",          "sik",  1200, 18_000,  9_000, 2, 4, 15_000, 1600,
     (1000, 7000, 1000, 1000), ["ofis_gomlek", "yaka_karti"]),
    ("antrenman_sonrasi",   "sik",   900, 17_000,  6_500, 1, 2,  9_000, 1800,
     (500, 1000, 1000, 7500), ["spor_esofman", "spor_cantasi"]),
    ("alisveris_molasi",    "sik",  1000, 16_000,  8_500, 2, 3, 10_000, 1200,
     (1500, 3000, 2500, 3000), ["alisveris_posetli", "sik_gunluk"]),
    # docs/11: very high price sensitivity, the top of the range
    ("pazarlikci",          "orta",  380, 21_000, 25_000, 1, 2,  9_000,  200,
     (2000, 3500, 2000, 2500), ["gunluk", "eski_mont"]),
    ("gec_saat_musterisi",  "orta",  350, 28_000, 11_000, 1, 2,  7_000, 1000,
     (0, 500, 500, 9000), ["kapusonlu", "mont"]),
    ("mac_grubu",           "orta",  340, 24_000,  7_000, 4, 6, 16_000, 2200,
     (0, 500, 1000, 8500), ["taraftar_atki", "taraftar_forma"]),
    ("diyet_yapan",         "orta",  330, 14_000,  9_500, 1, 2, 11_000, 1100,
     (2500, 4500, 1500, 1500), ["spor_esofman", "sik_gunluk"]),
    ("gece_vardiyasi",      "orta",  300, 25_000, 13_000, 1, 1,  8_000,  900,
     (4500, 500, 500, 4500), ["is_yelegi", "termos"]),
    ("dogum_gunu_grubu",    "nadir", 150, 27_000,  5_500, 6, 8, 22_000, 3500,
     (500, 1500, 1500, 6500), ["parti_sapkasi", "sik_gunluk"]),
    # Very hard to please: low patience, high reputation risk
    ("sikayetci_musteri",   "nadir", 140,  9_000, 20_000, 1, 2, 35_000,  100,
     (1500, 4000, 2000, 2500), ["ceket", "gunluk"]),
    ("toplu_siparis",       "nadir", 100, 22_000, 18_000, 1, 1, 20_000, 2800,
     (2000, 6500, 1000, 500), ["is_yelegi", "kutu_tasima"]),
]

# The twelve specific to the Turkish kitchen. Weights: common 4400, mid
# 1700, rare 390 = 6490.
TURK = [
    ("esnaf_komsu",           "sik",  1200, 22_000,  9_000, 1,  2, 18_000, 2600,
     (1000, 7000, 1500, 500), ["onluk", "esnaf_yelegi"]),
    # Patience 12 s: docs/12 section 5.2. The hardest midday peak.
    ("ogle_molasi_calisani",  "sik",  1250, 12_000, 14_000, 1,  3, 12_000,  900,
     (500, 8500, 1000, 0), ["ofis_gomlek", "yaka_karti"]),
    ("insaat_iscisi",         "sik",  1050, 20_000, 15_000, 2,  4, 11_000, 1400,
     (1500, 7500, 1000, 0), ["baret", "is_tulumu"]),
    ("memur",                 "sik",   900, 29_000, 12_000, 1,  3, 14_000, 1700,
     (500, 8000, 1500, 0), ["memur_ceket", "evrak_cantasi"]),
    # Patience 40 s: docs/12 section 5.2. Spends little, price sensitive.
    ("emekli",                "orta",  400, 40_000, 21_000, 1,  2, 13_000,  600,
     (2000, 4000, 3500, 500), ["kasket", "hirka"]),
    ("ogrenci",               "orta",  380, 23_000, 23_000, 1,  3,  9_000,  300,
     (500, 6000, 3000, 500), ["okul_cantasi", "kapusonlu"]),
    ("hafta_sonu_ailesi",     "orta",  340, 36_000,  8_000, 3,  6, 17_000, 2400,
     (500, 6000, 2000, 1500), ["sik_gunluk", "cocuk"]),
    ("uzun_yol_soforu",       "orta",  300, 11_000, 10_000, 1,  2,  5_000, 1000,
     (3000, 4000, 2000, 1000), ["sofor_yelegi", "kasket"]),
    ("titiz_musteri",         "orta",  280, 16_000, 13_000, 1,  2, 25_000, 1300,
     (500, 6500, 2000, 1000), ["ceket", "gozluk"]),
    ("mahalle_toplu_yemegi",  "nadir", 140, 34_000, 16_000, 6, 10, 28_000, 3200,
     (0, 7000, 2000, 1000), ["gunluk", "esarp"]),
    # The outcome moves reputation hard; leaves no tip.
    ("denetim_gorevlisi",     "nadir", 130, 30_000,  5_000, 1,  1, 60_000,    0,
     (3500, 5000, 1500, 0), ["resmi_ceket", "pano"]),
    ("eski_musteri",          "nadir", 120, 31_000,  7_500, 1,  2, 24_000, 2900,
     (1000, 5000, 3000, 1000), ["mont", "gunluk"]),
]

SETS = (
    ("shared",   "shared",     SHARED),
    ("fastfood", ["fastfood"], FASTFOOD),
    ("turk",     ["turk"],     TURK),
)


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------
def rnd_div(n, d):
    """n / d, half away from zero. Integers only; no floating point enters."""
    if d == 0:
        raise ZeroDivisionError("rnd_div")
    neg = (n < 0) != (d < 0)
    n, d = abs(n), abs(d)
    q, r = n // d, n % d
    if r * 2 >= d:
        q += 1
    return -q if neg else q


def build(row, cuisines):
    (aid, tier, weight, patience, sens, gmin, gmax,
     rep, tip, arrival, wardrobe) = row
    return {
        "id": aid,
        "nameKey": "archetype." + aid,
        "cuisines": cuisines,
        "tier": tier,
        "weight": weight,
        "patienceMs": patience,
        "priceSensitivityBp": sens,
        "groupSizeMin": gmin,
        "groupSizeMax": gmax,
        "reputationWeight": rep,
        "tipChanceBp": tip,
        "arrivalWeightsBp": list(arrival),
        "wardrobe": list(wardrobe),
    }


def write(path, obj):
    d = os.path.dirname(path)
    if not os.path.isdir(d):
        os.makedirs(d)
    with io.open(path, "w", encoding="utf-8", newline="\n") as f:
        json.dump(obj, f, ensure_ascii=False, indent=2)
        f.write("\n")
    print("written: " + os.path.relpath(path, ROOT).replace("\\", "/"))


def hourly_split(archetypes):
    """The weighted average arrival spread, in bp. Integer arithmetic."""
    total = sum(a["weight"] for a in archetypes)
    out = []
    for i in range(4):
        acc = sum(a["weight"] * a["arrivalWeightsBp"][i] for a in archetypes)
        out.append(rnd_div(acc, total))
    return out, total


def pp(bp_diff):
    """Writes a bp difference as percentage points in ascii: 250 -> 2.50"""
    return "{}.{:02d}".format(bp_diff // 100, bp_diff % 100)


# ---------------------------------------------------------------------------
# Validation
# ---------------------------------------------------------------------------
def check(by_file, errors):
    def fail(msg):
        errors.append(msg)

    print("")
    print("== 1. COUNTS ==")
    for name, want in (("shared", 8), ("fastfood", 12), ("turk", 12)):
        got = len(by_file[name])
        print("  {:<9s} {:>3d} / {:<3d} {}".format(
            name, got, want, "OK" if got == want else "BAD"))
        if got != want:
            fail("count {}: expected {} but found {}".format(name, want, got))

    print("")
    print("== 2. IDS, UNITS, FIELDS ==")
    seen = {}
    bad = {"id": 0, "dup": 0, "int": 0, "ward": 0, "group": 0, "arrival": 0}
    for name in ("shared", "fastfood", "turk"):
        for a in by_file[name]:
            if not ID_RE.match(a["id"]):
                bad["id"] += 1
                fail("id format: " + a["id"])
            if a["id"] in seen:
                bad["dup"] += 1
                fail("id collision: {} ({} and {})".format(
                    a["id"], seen[a["id"]], name))
            else:
                seen[a["id"]] = name
            if a["nameKey"] != "archetype." + a["id"]:
                fail("nameKey does not match: " + a["id"])
            for k, v in a.items():
                vals = v if isinstance(v, list) else [v]
                for e in vals:
                    if isinstance(e, bool) or isinstance(e, float):
                        bad["int"] += 1
                        fail("not an integer: {}.{}".format(a["id"], k))
            if not (1 <= len(a["wardrobe"]) <= 3):
                bad["ward"] += 1
                fail("wardrobe count: " + a["id"])
            for w in a["wardrobe"]:
                if not ID_RE.match(w):
                    bad["ward"] += 1
                    fail("wardrobe format: {} / {}".format(a["id"], w))
            if not (1 <= a["groupSizeMin"] <= a["groupSizeMax"]):
                bad["group"] += 1
                fail("group size: " + a["id"])
            s = sum(a["arrivalWeightsBp"])
            if len(a["arrivalWeightsBp"]) != 4 or s != BP:
                bad["arrival"] += 1
                fail("arrivalWeightsBp totals {}: {}".format(s, a["id"]))
            if a["tier"] not in TIERS:
                fail("unknown tier: " + a["id"])
            if not (0 <= a["tipChanceBp"] <= BP):
                fail("tip chance out of range: " + a["id"])
            if a["reputationWeight"] <= 0:
                fail("reputation weight zero or negative: " + a["id"])
    print("  unique ids           : {:>3d}  ({} collisions)".format(
        len(seen), bad["dup"]))
    print("  id format            : {}".format("OK" if not bad["id"] else "BAD"))
    print("  every field integer  : {}".format("OK" if not bad["int"] else "BAD"))
    print("  wardrobe 1-3 ascii   : {}".format("OK" if not bad["ward"] else "BAD"))
    print("  group min <= max     : {}".format("OK" if not bad["group"] else "BAD"))
    print("  arrivals total 10000 : {}".format("OK" if not bad["arrival"] else "BAD"))

    allrows = [a for n in ("shared", "fastfood", "turk") for a in by_file[n]]

    print("")
    print("== 3. PATIENCE (docs/12 section 5.2) ==")
    pmin = min(a["patienceMs"] for a in allrows)
    pmax = max(a["patienceMs"] for a in allrows)
    pmin_id = [a["id"] for a in allrows if a["patienceMs"] == pmin][0]
    pmax_id = [a["id"] for a in allrows if a["patienceMs"] == pmax][0]
    print("  lowest  : {:>6d} ms  {:<22s} (must be < {})".format(
        pmin, pmin_id, PATIENCE_MIN_CEIL))
    print("  highest : {:>6d} ms  {:<22s} (must be > {})".format(
        pmax, pmax_id, PATIENCE_MAX_FLOOR))
    if pmin >= PATIENCE_MIN_CEIL:
        fail("patience floor {} >= {}".format(pmin, PATIENCE_MIN_CEIL))
    if pmax <= PATIENCE_MAX_FLOOR:
        fail("patience ceiling {} <= {}".format(pmax, PATIENCE_MAX_FLOOR))
    locked_ok = True
    for aid in sorted(PATIENCE_LOCKED):
        want = PATIENCE_LOCKED[aid]
        got = [a["patienceMs"] for a in allrows if a["id"] == aid]
        if not got:
            locked_ok = False
            fail("archetype from docs/12 5.2 is missing: " + aid)
        elif got[0] != want:
            locked_ok = False
            fail("patience drifts from docs/12 5.2 {}: {} != {}".format(
                aid, got[0], want))
    print("  values fixed by the document: {}  ({} archetypes)".format(
        "OK" if locked_ok else "BAD", len(PATIENCE_LOCKED)))

    print("")
    print("== 4. PRICE SENSITIVITY (docs/12 section 5.3: 0.4 - 2.5) ==")
    smin = min(a["priceSensitivityBp"] for a in allrows)
    smax = max(a["priceSensitivityBp"] for a in allrows)
    print("  range reached : {} - {} bp".format(smin, smax))
    print("  target range  : {} - {} bp".format(SENS_MIN, SENS_MAX))
    for a in allrows:
        if not (SENS_MIN <= a["priceSensitivityBp"] <= SENS_MAX):
            fail("sensitivity outside the range: " + a["id"])
    if smin != SENS_MIN or smax != SENS_MAX:
        fail("the sensitivity range is not fully covered: {}-{}".format(smin, smax))

    print("")
    print("== 5. TIER SHARES (docs/11: common ~70, mid ~25, rare ~5) ==")
    print("  cuisine   tier   count    weight  share(bp)")
    full = {}
    for cuisine in ("fastfood", "turk"):
        rows = by_file["shared"] + by_file[cuisine]
        full[cuisine] = rows
        total = sum(a["weight"] for a in rows)
        covered = set()
        for tier in TIERS:
            t = [a for a in rows if a["tier"] == tier]
            covered.update(a["id"] for a in t)
            tw = sum(a["weight"] for a in t)
            share = rnd_div(tw * BP, total)
            print("  {:<9s} {:<6s} {:>4d} {:>9d} {:>8d}".format(
                cuisine, tier, len(t), tw, share))
            if not t:
                fail("{}: empty tier {}".format(cuisine, tier))
            if tier == "nadir" and share >= RARE_MAX_BP:
                fail("{}: the rare share is {} bp, must be below {}".format(
                    cuisine, share, RARE_MAX_BP))
        if len(covered) != len(rows):
            fail("{}: the tiers do not cover every archetype".format(cuisine))
        if len(rows) != 20:
            fail("{}: the cuisine total is not 20 ({})".format(cuisine, len(rows)))

    print("")
    print("== 6. HOURLY SPREAD (docs/12 section 5.6) ==")
    print("  cuisine   slot           target   reached   diff(pp)  state")
    for cuisine in ("fastfood", "turk"):
        got, total = hourly_split(full[cuisine])
        tgt = HOURLY_TARGET[cuisine]
        for i, slot in enumerate(SLOTS):
            diff = abs(got[i] - tgt[i])
            ok = diff <= SPLIT_TOL_BP
            print("  {:<9s} {:<14s} {:>5d} {:>9d} {:>9s}  {}".format(
                cuisine, slot, tgt[i], got[i], pp(diff), "OK" if ok else "BAD"))
            if not ok:
                fail("{} {} drifts {} bp, the limit is {}".format(
                    cuisine, slot, diff, SPLIT_TOL_BP))
        print("  {:<9s} {:<14s} {:>5d} {:>9d}   (weights total {})".format(
            cuisine, "TOTAL", sum(tgt), sum(got), total))

    return full


def scan_files(errors):
    print("")
    print("== 7. DECIMAL POINT AND ASCII SCAN ==")
    for name in ("shared", "fastfood", "turk"):
        p = os.path.join(OUTDIR, name + ".json")
        with io.open(p, "r", encoding="utf-8") as f:
            txt = f.read()
        hits = DECIMAL_RE.findall(txt)
        non_ascii = [c for c in txt if ord(c) > 127]
        print("  {:<14s} decimals: {:<3d} non-ascii: {:<3d} {}".format(
            name + ".json", len(hits), len(non_ascii),
            "OK" if not hits and not non_ascii else "BAD"))
        if hits:
            errors.append("{}.json: a decimal point was found".format(name))
        if non_ascii:
            errors.append("{}.json: a non-ascii character was found".format(name))


def main():
    by_file = {}
    for name, cuisines, rows in SETS:
        by_file[name] = [build(r, cuisines) for r in rows]

    print("== ARCHETYPE GENERATOR ==")
    print("source: docs/11-customer-system.md, docs/12-economy.md 5.2-5.6")
    print("units : docs/23-core-contract.md 2.2 and 8.4, integers")
    print("")
    for name in ("shared", "fastfood", "turk"):
        write(os.path.join(OUTDIR, name + ".json"), by_file[name])

    errors = []
    check(by_file, errors)
    scan_files(errors)

    print("")
    if errors:
        print("== RESULT: FAILED, {} errors ==".format(len(errors)))
        for e in errors:
            print("  - " + e)
        sys.exit(1)
    print("== RESULT: EVERY CHECK PASSED ==")


if __name__ == "__main__":
    main()
