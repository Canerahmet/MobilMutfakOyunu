# -*- coding: utf-8 -*-
"""
Writes content/regulars/*.json: ten named regulars per cuisine.

docs/09: "Each one has a name, a face, a job and a story of three or
four beats. It opens as you serve them well enough." docs/11: "a named
customer is one single person, written by hand, with a story, and always
the same person. An archetype produces thousands of customers."

A regular TAKES AN ARCHETYPE AS ITS BASE and adds its own properties on
top (docs/13). That way the behaviour code follows one path: patience,
group size, price sensitivity, arrival hour - all of it comes from the
archetype. Three things belong to the regular alone:

    favouriteDish     disappointment when it is not on the menu
    arrivesFromDay    when they enter the campaign
    veresiyeEligible  whether they can go on the tab in Turkish cuisine

NO text here, only keys (textKey). The localisation file is separate;
this file has to stay structural and generatable.

The field name `veresiyeEligible` stays as it is: it is written into
content/ and both tools/balance/export.py and the C# loader read it by
that name. ("Veresiye" is the tab a neighbourhood restaurant keeps for
its regulars - the signature mechanic of the Turkish cuisine.)

Usage:
    python tools/content/gen_regulars.py
"""
from __future__ import print_function

import io
import json
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(HERE))
CONTENT = os.path.join(ROOT, "content")
OUT_DIR = os.path.join(CONTENT, "regulars")

ID_RE = re.compile(r"^[a-z0-9_]+$")

# The campaign is 60 days. Regulars enter it SPREAD OUT: if they all
# arrive in the first week there is neither a feeling of familiarity nor
# the moment of "someone new has become a regular". Same idea as the
# progression curve in docs/09.
CAMPAIGN_DAYS = 60

# ---------------------------------------------------------------------------
# (id, archetype base, favourite dish, day they arrive, tab)
#
# The choice of archetype has to mean something: the neighbouring
# shopkeeper is the prime candidate for the tab (docs/11), the health
# inspector never is, and a traveller does not become a regular at all.
# ---------------------------------------------------------------------------
TURK = [
    ("hasan_usta",      "esnaf_komsu",            "kuru_fasulye",     3,  True),
    ("nazife_teyze",    "emekli",                 "mercimek_corbasi", 5,  True),
    ("selim_bey",       "memur",                  "kofte",            8,  True),
    ("rasim_amca",      "insaat_iscisi",          "kuru_fasulye",     11, True),
    ("guler_hanim",     "esnaf_komsu",            "pirinc_pilavi",    14, True),
    ("okan",            "ogrenci",                "bulgur_pilavi",    18, False),
    ("nurten_abla",     "ogle_molasi_calisani",   "cacik",            23, True),
    ("ismail_sofor",    "uzun_yol_soforu",        "tavuk_sis",        29, False),
    ("perihan_hanim",   "titiz_musteri",          "coban_salata",     36, False),
    ("mehmet_dede",     "eski_musteri",           "nohut",            44, True),
]

FASTFOOD = [
    ("deniz",           "aceleci_ogrenci",        "hamburger",        3,  False),
    ("burak",           "ofis_grubu",             "cizburger",        6,  False),
    ("elif",            "alisveris_molasi",       "patates_kizartma", 9,  False),
    ("kaan_hoca",       "antrenman_sonrasi",      "tavuk_burger",     12, False),
    ("sevda",           "diyet_yapan",            "yesil_salata",     16, False),
    ("tolga",           "gece_vardiyasi",         "hot_dog",          21, False),
    ("melis",           "pazarlikci",             "nugget",           26, False),
    ("ozan",            "mac_grubu",              "duble_burger",     32, False),
    ("yagmur",          "gec_saat_musterisi",     "dondurma",         39, False),
    ("cem_abi",         "kurye",                  "baharatli_patates", 47, False),
]

CUISINES = [("turk", TURK), ("fastfood", FASTFOOD)]

# Story beats. docs/09 says "three or four beats"; the thresholds are
# THE SAME FOR EVERYONE because they are not a difficulty setting, they
# are a NARRATIVE tempo. The third beat asks for 15 visits: in sixty days
# only someone served well and regularly gets there.
STORY = [
    (1,  3, 7000),
    (2,  8, 7500),
    (3, 15, 8000),
]


def build(cuisine, rows):
    out = []
    for rid, base, dish, day, credit in rows:
        out.append({
            "id": rid,
            "cuisine": cuisine,
            "nameKey": "regular." + rid + ".name",
            "jobKey": "regular." + rid + ".job",
            "archetypeBase": base,
            "favouriteDish": dish,
            "arrivesFromDay": day,
            "veresiyeEligible": credit,
            "story": [
                {"beat": b, "requiresVisits": v, "requiresSatisfaction": s,
                 "textKey": "regular." + rid + ".beat" + str(b)}
                for b, v, s in STORY
            ],
        })
    return out


def load(path):
    return json.load(io.open(path, encoding="utf-8"))


def check(cuisine, rows, errors):
    dishes = set(d["id"] for d in load(
        os.path.join(CONTENT, "dishes", cuisine + ".json")))
    arch = set(a["id"] for a in load(
        os.path.join(CONTENT, "archetypes", "shared.json")))
    arch |= set(a["id"] for a in load(
        os.path.join(CONTENT, "archetypes", cuisine + ".json")))
    unlock = dict((d["id"], d["unlockDay"]) for d in load(
        os.path.join(CONTENT, "dishes", cuisine + ".json")))

    seen = set()
    days = []
    for r in rows:
        rid = r["id"]
        if not ID_RE.match(rid):
            errors.append(cuisine + " invalid id: " + rid)
        if rid in seen:
            errors.append(cuisine + " duplicate id: " + rid)
        seen.add(rid)

        if r["archetypeBase"] not in arch:
            errors.append(cuisine + "/" + rid + " archetype does not exist: " +
                          r["archetypeBase"])
        if r["favouriteDish"] not in dishes:
            errors.append(cuisine + "/" + rid + " dish does not exist: " +
                          r["favouriteDish"])
        elif unlock[r["favouriteDish"]] > r["arrivesFromDay"]:
            # If their favourite dish has NOT OPENED YET on the day
            # they arrive, the mechanic runs unfairly from day one: a
            # shortfall the player could do nothing about.
            errors.append(
                "%s/%s arrives on day %d but their favourite dish opens on day %d"
                % (cuisine, rid, r["arrivesFromDay"], unlock[r["favouriteDish"]]))

        d = r["arrivesFromDay"]
        if not (1 <= d <= CAMPAIGN_DAYS):
            errors.append(cuisine + "/" + rid + " invalid day: " + str(d))
        days.append(d)

        beats = [b["beat"] for b in r["story"]]
        if beats != sorted(beats) or len(set(beats)) != len(beats):
            errors.append(cuisine + "/" + rid + " the beat order is broken")
        visits = [b["requiresVisits"] for b in r["story"]]
        if visits != sorted(visits):
            errors.append(cuisine + "/" + rid +
                          " the visit thresholds of the beats do not increase")

    if len(rows) != 10:
        errors.append(cuisine + " must have ten regulars, there are " +
                      str(len(rows)))
    if sorted(days) != days:
        errors.append(cuisine + " the arrival days must be in increasing order")

    # Turkish cuisine must have CANDIDATES for the tab; fast food must
    # not. docs/07: the tab is the signature mechanic of the Turkish
    # cuisine, not of fast food.
    credit = [r["id"] for r in rows if r["veresiyeEligible"]]
    if cuisine == "turk" and len(credit) < 4:
        errors.append("turk: too few candidates for the tab (" +
                      str(len(credit)) + ")")
    if cuisine == "fastfood" and credit:
        errors.append("fastfood: the tab is the Turkish cuisine's mechanic, "
                      "there must be no candidates")

    return credit


def write(path, obj):
    text = json.dumps(obj, ensure_ascii=False, indent=2)
    io.open(path, "w", encoding="utf-8", newline="\n").write(text + "\n")
    print("written: " + os.path.relpath(path, ROOT).replace("\\", "/"))


def main():
    errors = []
    if not os.path.isdir(OUT_DIR):
        os.makedirs(OUT_DIR)

    for cuisine, table in CUISINES:
        rows = build(cuisine, table)
        credit = check(cuisine, rows, errors)
        print("")
        print("--- " + cuisine + " ---")
        print("id                  archetype                dish               day  tab")
        for r in rows:
            print("%-19s %-24s %-18s %3d  %s" % (
                r["id"], r["archetypeBase"], r["favouriteDish"],
                r["arrivesFromDay"], "yes" if r["veresiyeEligible"] else "-"))
        print("candidates for the tab: %d" % len(credit))
        if not errors:
            write(os.path.join(OUT_DIR, cuisine + ".json"), rows)

    if errors:
        print("")
        print("--- ERROR (%d) ---" % len(errors))
        for e in errors:
            print("  " + e)
        return 1

    print("")
    print("every rule passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
