# -*- coding: utf-8 -*-
"""
The content-code contract checker
============================================================================
It asks one question: **does every field in the content have something
that answers to it in the simulation?**

On 10 September 2026 four separate bugs were found in one day, and all
four were of the same class: the content promises something and the
simulation never reads it.

  spoilDays      the shelf life of 44 ingredients was written down; the
                 code deleted all of them overnight
  dessert group  there were 9 dessert dishes; PickOrder never looked at
                 desserts
  dish groups    they were designed per cuisine; the code had hard-coded
                 the fast food dictionary
  attendBp       docs/27 Decision D had been on paper for months; the
                 kitchen had not implemented it

None of these break the build, none of them break a test, all of them
are SILENTLY dead. This script scans for that class mechanically:

  1. every key in content/*.json
  2. the keys the DTOs bind  ([JsonProperty])
  3. the properties the core types expose
  4. whether those properties are REALLY read in the core

Run with:  python tools/audit_content.py
Exit code: 1 if there is a finding
"""
from __future__ import print_function

import io
import json
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CONTENT = os.path.join(ROOT, "content")
ASSETS = os.path.join(ROOT, "unity", "Assets", "Lokanta")
CORE = os.path.join(ASSETS, "Core")
DTO_DIR = os.path.join(ASSETS, "Content")

# These keys are deliberately not read in the code; the reason is next to each.
IGNORED_KEYS = {
    "_comment": "the generated-file note",
    "schemaVersion": "a version stamp, no migration yet",
    "id": "the identity, carried in the Def constructor",
    "nameKey": "display text; the core knows no text",
    "source": "a note on the golden data source",
    # STILL OPEN, AND THE REASON HAS CHANGED. "The art pipeline" is no
    # longer a future thing: Game/Wardrobe.cs dresses the STAFF by role -
    # chef's whites, the dishwasher's apron and gloves, the waiter's suit -
    # and the tour counts how many dressings completed. What it does not
    # dress is the GUESTS, and this field is the guests' side of it: 32
    # archetypes each carry a tag ("gunluk", "sik", ...) and nothing reads
    # one. Leaving the old reason in place would have let a closed pipeline
    # keep excusing an open field.
    "wardrobe": "guest clothing; Wardrobe.cs dresses staff only (docs/10, docs/24)",
    "cuisines": "a load filter",
    "cuisine": "a load filter",
    "tier": "bound as TierIndex",
    "tiers": "structural",
    "roles": "structural",
    "stations": "structural",
    "storage": "structural",
    "order": "structural",
    "staffing": "structural",
    "menuRoles": "structural",
    "main": "menuRoles, structural",
    "side": "menuRoles, structural",
    "drink": "menuRoles, structural",
    "dessert": "menuRoles, structural",
    "weeks": "golden data, read in the test",
    "toleranceCenti": "golden data, read in the test",

    # --- presentation and art: the core does not know these, and must not ---
    "plating": "plate presentation, the art pipeline (docs/24)",
    "toppings": "plate presentation, the art pipeline",
    "base": "plate presentation, the art pipeline",
    "unit": "the ingredient unit (kg), display text",

    # --- inner keys bound as a Dictionary ---
    "dusuk": "a quality key inside qualityPriceMultiplierBp (low)",
    "standart": "a quality key inside qualityPriceMultiplierBp (standard)",
    "yuksek": "a quality key inside qualityPriceMultiplierBp (high)",
    "ilkbahar": "a season key inside seasonModifierBp (spring)",
    "yaz": "a season key inside seasonModifierBp (summer)",
    "sonbahar": "a season key inside seasonModifierBp (autumn)",
    "kis": "a season key inside seasonModifierBp (winter)",
    "fastfood": "a cuisine key inside cuisineStations",
    "turk": "a cuisine key inside cuisineStations",
    "cuisineStations": "structural",
    # Trait effects are bound as a Dictionary<string,int>; they do not
    # appear as fields on the DTO. What answers to them are the TraitDef
    # properties.
    "speedBp": "TraitDef.SpeedBp (the effects dictionary)",
    "satisfactionCenti": "TraitDef.SatisfactionCenti (the effects dictionary)",
    "wageBp": "TraitDef.WageBp (the effects dictionary)",
    "xpBp": "TraitDef.XpBp (the effects dictionary)",
    "peakPenaltyBp": "TraitDef.PeakPenaltyBp (the effects dictionary)",
    "fatiguePenaltyBp": "TraitDef.FatiguePenaltyBp (the effects dictionary)",
    "moraleAura": "TraitDef.MoraleAura (the effects dictionary)",
    "peakImmune": "TraitDef.PeakImmune (the effects dictionary)",
    "fatigueImmune": "TraitDef.FatigueImmune (the effects dictionary)",
    "qualityBp": "TraitDef.QualityBp (the effects dictionary)",
    "cleanlinessBp": "TraitDef.CleanlinessBp (the effects dictionary)",
    "ownerPool": "FIXED: ContentLoader rejects anything but 'hall' (docs/14)",
    "unlockSeason": "FIXED: derived from unlockDay, verified on load",
    "weeklyWageMultiplierBp": "FIXED: the components of weeklyXpWageGrowthBp",
}

# DESIGNED BUT NOT BUILT. These are not errors, they are the QUEUE. The
# checker shows them under a separate heading so they do not get mixed up
# with the ignored ones; when a system is built it must be deleted from
# here.
PLANNED = {
    "unlockSeason": "the season-based dish lock (docs/09)",
    "kitchenMsPerPerson": "the docs/27 derivation; prepMs comes from the content",
}

# --- the dictionaries for check 4 ---------------------------------------
#
# The schema in docs/13 was written BEFORE Phase 0, and docs/23 2.2 later
# turned every unit into an integer (seconds -> ms, decimals -> basis
# points). So many of the names in the schema are the old names of a
# field that IS implemented. If the checker counted these as "missing",
# the real gaps would disappear among 63 lines - and they did.
SCHEMA_RENAMED = {
    # economy.json - the integer units of docs/23 2.2
    "serviceSeconds": "serviceMs",
    "startingReputation": "startingReputationCenti",
    "weekendMultiplier": "weekendMultiplierBp",
    "reputationDecayPerDay": "reputationDecayPerDayCenti",
    "satisfactionNeutral": "satisfactionNeutralCenti",
    "underpriceFloor": "underpriceFloorBp",
    "priceElasticity": "priceElasticityBp",
    "demandVariance": "demandVarianceBp",
    "attendWorkCut": "attendWorkCutBp",
    "overpriceCeiling": "overpriceCeilingBp",
    "loanMultiplier": "loanMultiplierBp",
    "priceVolatility": "priceVolatilityBp",
    # ingredients.json
    "seasonModifier": "seasonModifierBp",
    "qualityPriceMultiplier": "qualityPriceMultiplierBp",
    "qualitySatisfaction": "qualitySatisfactionCenti",
    # dishes/*.json
    "prepSeconds": "prepMs",
    "recipe": "ingredients",
    "ingredient": "ingredients[].id",
    "amount": "ingredients[].grams",
    # archetypes/*.json
    "patienceSeconds": "patienceMs",
    "priceSensitivity": "priceSensitivityBp",
    "arrivalWeights": "weight",
    "partySize": "groupSizeMin / groupSizeMax",
    "min": "groupSizeMin",
    "max": "groupSizeMax",
    # staff-roles.json
    "baseSpeed": "capacityPerDay + xpSpeedBp",
    # expansions.json -> economy.staffing.tiers
    "cost": "tiers[].upgrade",
    "weeklyRent": "tiers[].rent",
    # cuisines.json
    "signatureMechanic": "signature.kind",
    "teaService": "signature.credit.teaCostCenti",
    "hourSplit": "slotDurationsBp (docs/28 Decision G: a DURATION, not a share)",
    "acilis": "slotDurationsBp[0]",
    "ogle": "slotDurationsBp[1]",
    "ogleden_sonra": "slotDurationsBp[2]",
    "aksam": "slotDurationsBp[3]",
}

# WRITTEN in the schema but not written into the content, because it is
# DERIVED from fields that are ALREADY LOADED. docs/34 11: a hand-written
# table is writing the same character a second time, and what is written
# in two places drifts apart silently.
SCHEMA_DERIVED = {
    "orderPreference": "derived from priceSensitivityBp + tipChanceBp",
    "spendTendency": "priceSensitivityBp",
    "regularChance": "the archetype weight + tierIndex",
    "perishableRatio": "counted from the ingredients' perishable field",
    "marketPrice": "basePrice x season x the daily market swing",
    "sulu": "a group name in the orderPreference example (stew)",
    "corba": "a group name in the orderPreference example (soup)",
    "pilav": "a group name in the orderPreference example (pilaf)",
}

# GENUINELY NOT BUILT. This is the queue; every line is a system.
SCHEMA_PLANNED = {
    # regulars/*.json - the file does not exist at all
    "archetypeBase": "the named regular (docs/11)",
    "arrivesFromDay": "the named regular (docs/11)",
    "favouriteDish": "the named regular (docs/11)",
    "jobKey": "the named regular (docs/11)",
    "veresiyeEligible": "the named regular; for now derived from the common tier",
    "story": "the regular's story beats (docs/11)",
    "beat": "the regular's story beats",
    "requiresVisits": "the regular's story beats",
    "requiresSatisfaction": "the regular's story beats",
    "textKey": "the regular's story beats",
    # staff-traits.json - the file does not exist at all
    "effects": "staff traits and business upgrades (docs/14)",
    "conflictsWith": "staff traits (docs/14): traits that cannot coexist",
    "speed": "the speed effect of a staff trait (docs/14)",
    "cleanliness": "the cleanliness effect of a staff trait (docs/14)",
    # upgrades.json - the file does not exist at all
    "capacityBonus": "a business upgrade (docs/13 upgrades.json)",
    "customerBaseBonus": "a business upgrade (docs/13 upgrades.json)",
    # the rest
    "dailySpecial": "the dish of the day; the SetDailySpecial command exists, its handler does not",
    "scoreAxis": "a year-end scoring axis (docs/08)",
    "free": "whether the cuisine is free; the monetisation layer (docs/07)",
    "batchSize": "the Japanese broth signature mechanic; that cuisine was not built",
    "tags": "dish tags; search and filter, interface work",
    "palette": "the art pipeline (docs/10, cuisine identity)",
    "wardrobeSet": "the art pipeline (docs/10)",
    "wardrobeTags": "the art pipeline (docs/10)",
}

# Properties that are not read in the core and yet are not a problem.
IGNORED_PROPS = {
    "Id": "an identity",
    "NameKey": "display text",
    "Cuisine": "an identity",
    "MaxTier": "derived",
    "TierCount": "derived",
}


def read(path):
    return io.open(path, encoding="utf-8", errors="replace").read()


def walk(root, ext):
    for base, _, files in os.walk(root):
        if os.sep + "obj" + os.sep in base + os.sep:
            continue
        for f in files:
            if f.endswith(ext):
                yield os.path.join(base, f)


# ---------------------------------------------------------------------------
def json_keys():
    """Every JSON key under content/ -> which files it appears in."""
    found = {}

    def visit(node, path):
        if isinstance(node, dict):
            for k, v in node.items():
                found.setdefault(k, set()).add(path)
                visit(v, path)
        elif isinstance(node, list):
            for v in node:
                visit(v, path)

    for p in walk(CONTENT, ".json"):
        rel = os.path.relpath(p, ROOT).replace("\\", "/")

        # THE LOCALISATION TABLE IS NOT CONTENT, it is a flat key->text
        # mapping. When the checker scanned it too, it invented four
        # hundred and two "unbound keys" and the real findings were
        # buried under that noise. gen_loc.py already verifies the
        # table's integrity.
        if rel.startswith("content/loc/"):
            continue

        try:
            visit(json.loads(read(p)), rel)
        except ValueError as e:
            print("  ! JSON could not be read: %s (%s)" % (rel, e))
    return found


def dto_bound():
    """The keys the DTOs bind with [JsonProperty(...)]."""
    bound = set()
    for p in walk(DTO_DIR, ".cs"):
        for m in re.finditer(r'JsonProperty\("([^"]+)"\)', read(p)):
            bound.add(m.group(1))
    return bound


# [JsonProperty("x")] ... public T Name  -> (x, Name)
DTO_FIELD = re.compile(
    r'JsonProperty\("([^"]+)"\)\s*\]?\s*'
    r'public\s+[\w\[\]<>?,\s]+?\s+(\w+)\s*\{')


def dto_fields():
    """The DTO fields: JSON key -> C# property name."""
    out = {}
    for p in walk(DTO_DIR, ".cs"):
        for m in DTO_FIELD.finditer(read(p)):
            out[m.group(1)] = m.group(2)
    return out


def loader_text():
    """The content loader alone. This is where a DTO field is USED."""
    return chr(10).join(
        read(p) for p in walk(DTO_DIR, ".cs")
        if "Dto" not in os.path.basename(p))


PROP = re.compile(r"public\s+(?:readonly\s+)?[\w\[\]<>?]+\s+(\w+)\s*(?:\{\s*get|;)")


def core_props():
    """The properties the core Content and Economy types expose."""
    props = {}
    for sub in ("Content", "Economy"):
        for p in walk(os.path.join(CORE, sub), ".cs"):
            rel = os.path.relpath(p, ROOT).replace("\\", "/")
            for m in PROP.finditer(read(p)):
                props.setdefault(m.group(1), rel)
    return props


def solution_text():
    """
    All of the C# source: the core, the content loader, the balance tool,
    the tests.

    The distinction matters. A field not being read IN THE CORE is not by
    itself an error: the slot durations are handed to TimingConfig
    outside the core, and capacity is verified in the loader. The real
    finding is a field read NOWHERE AT ALL; that one means the content is
    making an empty promise.
    """
    parts = []
    for root in (ASSETS, os.path.join(ROOT, "src"), os.path.join(ROOT, "tests")):
        if os.path.isdir(root):
            parts.extend(read(q) for q in walk(root, ".cs"))
    return chr(10).join(parts)


def core_text():
    """The core alone."""
    return "\n".join(read(p) for p in walk(CORE, ".cs"))


# ---------------------------------------------------------------------------
def main():
    print("=" * 74)
    print("CONTENT-CODE CONTRACT CHECK")
    print("=" * 74)

    keys = json_keys()
    bound = dto_bound()
    props = core_props()
    body = core_text()
    everywhere = solution_text()

    problems = []

    # --- 1. Icerikte var, DTO baglamiyor -------------------------------
    print()
    print("1. IN the content, no DTO binds it")
    print("-" * 74)
    unbound = []
    for k in sorted(keys):
        if k in bound or k in IGNORED_KEYS or k in PLANNED:
            continue
        unbound.append(k)
        files = sorted(keys[k])
        print("   %-24s %s" % (k, files[0] + ("" if len(files) == 1 else
                                              " (+%d files)" % (len(files) - 1))))
    if not unbound:
        print("   none")
    else:
        problems.append(("unbound key", len(unbound)))

    # --- 2. the DTO binds it, the core does not read it -----------------
    #
    # This is the genuinely dangerous class: the field is read from JSON,
    # it reaches memory, and it just sits there. spoilDays was exactly
    # this.
    print()
    print("2. IN a core type, read NOWHERE AT ALL")
    print("-" * 74)

    def reads(name, text):
        uses = len(re.findall(r"\b" + re.escape(name) + r"\b", text))
        decls = len(re.findall(r"public\s+(?:readonly\s+)?[\w\[\]<>?]+\s+"
                               + re.escape(name) + r"\b", text))
        # THE \b IS REQUIRED: this line had no boundary at first, and
        # assignments to LONGER names such as "_cookXpSpeedBp =" inflated
        # the count for "SpeedBp". The result: a field that really was
        # read got reported as "read nowhere", and was lost among the
        # real findings.
        assigns = len(re.findall(r"\b" + re.escape(name) + r"\s*=[^=]", text))
        return uses - decls - assigns

    dead, only_outside = [], []
    for name in sorted(props):
        if name in IGNORED_PROPS:
            continue
        if reads(name, everywhere) <= 0:
            dead.append(name)
            print("   %-26s %s" % (name, props[name]))
        elif reads(name, body) <= 0:
            only_outside.append(name)
    if not dead:
        print("   none")
    else:
        problems.append(("property read nowhere", len(dead)))

    print()
    print("   read only OUTSIDE the core (not a problem):")
    print("   " + (", ".join(only_outside) if only_outside else "none"))

    print()
    # --- 3. the DTO binds it but the loader never uses it ---------------
    #
    # The sneakiest class. The field is read from JSON, reaches memory
    # and STAYS THERE: it is never carried over to the core type. It
    # escapes between the two checks, because it is both "bound" and "not
    # in the core". seasonDays and unlockSeason were exactly this.
    print()
    print("3. The DTO binds it but THE LOADER never uses it")
    print("-" * 74)
    fields = dto_fields()
    loader = loader_text()
    stranded, planned_seen = [], []
    for key, prop in sorted(fields.items()):
        if key in IGNORED_KEYS:
            continue
        if re.search(r"[.]\s*" + re.escape(prop) + "(?![A-Za-z0-9_])", loader):
            continue
        if key in PLANNED:
            planned_seen.append(key)
            continue
        stranded.append(key)
        print("   %-24s (DTO property: %s)" % (key, prop))
    if not stranded:
        print("   none")
    else:
        problems.append(("field that never reaches the loader", len(stranded)))

    print()
    print("   DESIGNED BUT NOT BUILT (not an error, the queue):")
    for k in sorted(set(planned_seen) | (set(PLANNED) & set(keys) - set(bound))):
        print("      %-26s %s" % (k, PLANNED[k]))

    # --- 4. in the schema document, NOT in the content ------------------
    print()
    print("4. IN the docs/13 schema, NOT in the generated content")
    print("-" * 74)
    schema = os.path.join(ROOT, "docs", "13-data-schemas.md")
    doc_keys = set()
    if os.path.exists(schema):
        for m in re.finditer(r'"(\w+)"\s*:', read(schema)):
            doc_keys.add(m.group(1))
    missing, renamed, derived, planned = [], [], [], []
    for k in sorted(doc_keys):
        if k in keys or k in IGNORED_KEYS:
            continue
        if k in SCHEMA_RENAMED:
            renamed.append(k)
        elif k in SCHEMA_DERIVED:
            derived.append(k)
        elif k in SCHEMA_PLANNED:
            planned.append(k)
        else:
            missing.append(k)

    for k in missing:
        print("   %s" % k)
    if not missing:
        print("   none")
    else:
        problems.append(("in the schema but not in the content", len(missing)))

    if renamed:
        print()
        print("   RENAMED (the integer units of docs/23 2.2) - implemented:")
        for k in renamed:
            print("      %-26s -> %s" % (k, SCHEMA_RENAMED[k]))
    if derived:
        print()
        print("   DERIVED (docs/34 11) - not written into the content:")
        for k in derived:
            print("      %-26s %s" % (k, SCHEMA_DERIVED[k]))
    if planned:
        print()
        print("   NOT BUILT (the queue):")
        for k in planned:
            print("      %-26s %s" % (k, SCHEMA_PLANNED[k]))

    # --- summary ---------------------------------------------------------
    print()
    print("=" * 74)
    if not problems:
        print("CLEAN: every field in the content has something answering to it.")
        return 0
    for name, n in problems:
        print("%-32s %d" % (name, n))
    print()
    print("Each one must either BE IMPLEMENTED or go on the IGNORED list with\n"
          "its reason.")
    return 1


if __name__ == "__main__":
    sys.exit(main())
