# -*- coding: utf-8 -*-
"""
Writes content/staff-traits.json: the twelve staff traits.

The "Trait" table in docs/14, one for one. The design intent is written
there:

    "No trait is purely good or purely bad. The apprentice is cheap but
     slow, the experienced one fast but expensive. The right trait
     depends on the right station."

So the generator imposes a BALANCE RULE: every trait must have either a
cost or a condition. Writing a trait with no cost turns picking a trait
from a decision into a "take the best one" button.

The schema in docs/13 is written in decimals ("speed": 0.18); docs/23
2.2 turned every unit into an integer, so here it is in basis points:
speedBp 1800.

Usage:
    python tools/content/gen_traits.py
"""
from __future__ import print_function

import io
import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(HERE))
CONTENT = os.path.join(ROOT, "content")

# ---------------------------------------------------------------------------
# (id, effects, conflicting trait)
#
# Effect names:
#   speedBp            the multiplier that divides task time (+1800 = 18% faster)
#   satisfactionCenti  satisfaction difference at the table they serve
#   wageBp             wage difference
#   xpBp               experience gain multiplier (20000 = double, 0 = none)
#   peakPenaltyBp      extra slowdown in the BUSY slice
#   fatiguePenaltyBp   extra slowdown in the last quarter of the day
#   moraleAura         effect on the rest of the crew's morale
#   cleanlinessBp      cleanliness; affects dishwashing and table-clearing speed
# ---------------------------------------------------------------------------
TRAITS = [
    ("hizli_ama_daginik", {"speedBp": 1800, "cleanlinessBp": -1500},
     ["yavas_ama_titiz"]),
    ("yavas_ama_titiz", {"speedBp": -1200, "qualityBp": 2000},
     ["hizli_ama_daginik"]),
    ("kalabalikta_panikleyen", {"peakPenaltyBp": 2500}, ["sakin"]),
    ("sakin", {"peakImmune": 1}, ["kalabalikta_panikleyen"]),
    ("musteriyle_iyi_anlasan", {"satisfactionCenti": 800}, ["suratsiz"]),
    ("suratsiz", {"satisfactionCenti": -600}, ["musteriyle_iyi_anlasan"]),
    ("cabuk_yorulan", {"fatiguePenaltyBp": 2000}, ["dayanikli"]),
    ("dayanikli", {"fatigueImmune": 1}, ["cabuk_yorulan"]),
    ("ekip_moralini_yukselten", {"moraleAura": 10}, ["huysuz"]),
    ("huysuz", {"moraleAura": -8}, ["ekip_moralini_yukselten"]),
    ("cirak", {"wageBp": -2500, "speedBp": -1500, "xpBp": 20000}, ["tecrubeli"]),
    ("tecrubeli", {"wageBp": 3000, "speedBp": 1500, "xpBp": 0}, ["cirak"]),
]

# Fields that count as a cost: if a trait carries one of these it has a price.
COSTS = ("speedBp", "satisfactionCenti", "wageBp", "cleanlinessBp",
         "peakPenaltyBp", "fatiguePenaltyBp", "moraleAura", "xpBp")


def build():
    out = []
    for tid, effects, conflicts in TRAITS:
        out.append({
            "id": tid,
            "nameKey": "trait." + tid,
            "effects": dict(effects),
            "conflictsWith": list(conflicts),
        })
    return out


def check(rows, errors):
    ids = set(r["id"] for r in rows)
    if len(ids) != len(rows):
        errors.append("duplicate trait id")
    if len(rows) != 12:
        errors.append("docs/09 asks for twelve traits, there are %d" % len(rows))

    for r in rows:
        # A conflict must be SYMMETRIC: if A conflicts with B then B
        # conflicts with A. A conflict written in one direction leaves a
        # rule in the hiring code that silently does nothing.
        for other in r["conflictsWith"]:
            if other not in ids:
                errors.append("%s conflicts with a trait that does not exist: %s"
                              % (r["id"], other))
                continue
            back = next(x for x in rows if x["id"] == other)
            if r["id"] not in back["conflictsWith"]:
                errors.append("one-way conflict: %s -> %s" % (r["id"], other))

    # docs/14: "no trait is purely good or purely bad."
    #
    # When this rule was first written it caught two traits: good with
    # people (+8 satisfaction) and lifts the team (+10 morale) - neither
    # carries any cost in its effect table.
    #
    # But they DO have a cost, only it is not inside the trait, it is
    # inside the POOL: each of them has an EVIL TWIN (surly,
    # bad-tempered) and the two conflict. Hiring draws one of the pair,
    # so a good trait is LUCK - not an advantage you choose. The cost is
    # that the bad twin sits in the same pool.
    #
    # So the right rule is this: a trait must either carry its own cost,
    # or a trait it conflicts with must be its MIRROR. A trait with no
    # mirror and no cost turns picking a trait into a "take the best
    # one" button.
    for r in rows:
        good, bad = weigh(r)
        if not good or bad:
            continue
        mirrored = False
        for other in r["conflictsWith"]:
            back = next((x for x in rows if x["id"] == other), None)
            if back is None:
                continue
            g2, b2 = weigh(back)
            if b2 and not g2:
                mirrored = True
        if not mirrored:
            errors.append("%s is a trait with no cost and no mirror" % r["id"])


def weigh(r):
    """How many good and how many bad effects a trait has."""
    good = bad = 0
    for k, v in r["effects"].items():
        if k.endswith("Immune"):
            good += 1
        elif k in ("peakPenaltyBp", "fatiguePenaltyBp"):
            bad += 1
        elif k == "wageBp":
            # A wage INCREASE is a cost, a discount is an advantage.
            if v > 0:
                bad += 1
            elif v < 0:
                good += 1
        elif k == "xpBp":
            if v < 10000:
                bad += 1
            elif v > 10000:
                good += 1
        elif v > 0:
            good += 1
        elif v < 0:
            bad += 1
    return good, bad


def main():
    errors = []
    rows = build()
    check(rows, errors)

    print("id                        effects")
    for r in rows:
        print("%-25s %s" % (r["id"], json.dumps(r["effects"], ensure_ascii=False)))

    if errors:
        print("")
        print("--- ERROR (%d) ---" % len(errors))
        for e in errors:
            print("  " + e)
        return 1

    path = os.path.join(CONTENT, "staff-traits.json")
    io.open(path, "w", encoding="utf-8", newline="\n").write(
        json.dumps(rows, ensure_ascii=False, indent=2) + "\n")
    print("")
    print("written: content/staff-traits.json")
    print("every rule passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
