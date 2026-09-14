# -*- coding: utf-8 -*-
"""
content/regulars/*.json uretir: mutfak basina on isimli duzenli musteri.

docs/09: "Her birinin adi, yuzu, meslegi ve uc ile dort sahnelik hikayesi
var. Yeterince iyi hizmet verdikce acilir." docs/11: "isimli musteri tek
bir kisidir, elle yazilmistir, hikayesi vardir ve hep ayni kisidir.
Arketip ise binlerce musteri uretir."

Duzenli musteri bir ARKETIPI TABAN ALIR ve ustune kendi ozelliklerini
ekler (docs/13). Boylece davranis kodu tek yol izliyor: sabir, grup
buyuklugu, fiyat duyarliligi, gelis saati - hepsi arketipten geliyor.
Duzenli musterinin kendine ait olan sey uc sey:

    favouriteDish     menude yoksa hayal kirikligi
    arrivesFromDay    kampanyaya ne zaman girer
    veresiyeEligible  Turk mutfaginda deftere yazilabilir mi

Metin YOK, yalnizca anahtar (textKey). Yerellestirme dosyasi ayri; bu
dosya yapisal ve uretilebilir kalmali.

Calistirma:
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

# Kampanya 60 gun. Duzenli musteriler kampanyaya YAYILARAK giriyor:
# hepsi ilk hafta gelirse ne tanidiklik duygusu olusuyor ne de "yeni biri
# duzenlimiz oldu" ani. docs/09 ilerleme egrisiyle ayni fikir.
CAMPAIGN_DAYS = 60

# ---------------------------------------------------------------------------
# (id, arketip tabani, sevdigi yemek, geldigi gun, veresiye)
#
# Arketip secimi anlamli olmali: esnaf komsu veresiye defterinin ana adayi
# (docs/11), denetim gorevlisi asla degil, yolcu zaten duzenli olmaz.
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

# Hikaye sahneleri. docs/09 "uc ile dort sahne" diyor; esik degerleri
# HERKES ICIN AYNI cunku bunlar bir zorluk ayari degil, bir ANLATI temposu.
# Ucuncu sahne 15 ziyaret istiyor: altmis gunde ancak duzenli olarak iyi
# agirlanan biri oraya varir.
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
            errors.append(cuisine + " gecersiz kimlik: " + rid)
        if rid in seen:
            errors.append(cuisine + " tekrarlanan kimlik: " + rid)
        seen.add(rid)

        if r["archetypeBase"] not in arch:
            errors.append(cuisine + "/" + rid + " olmayan arketip: " +
                          r["archetypeBase"])
        if r["favouriteDish"] not in dishes:
            errors.append(cuisine + "/" + rid + " olmayan yemek: " +
                          r["favouriteDish"])
        elif unlock[r["favouriteDish"]] > r["arrivesFromDay"]:
            # Geldigi gun sevdigi yemek daha ACILMAMISSA mekanik ilk
            # gunden haksiz calisir: oyuncunun elinde olmayan bir eksik.
            errors.append(
                "%s/%s %d. gunde geliyor ama sevdigi yemek %d. gunde aciliyor"
                % (cuisine, rid, r["arrivesFromDay"], unlock[r["favouriteDish"]]))

        d = r["arrivesFromDay"]
        if not (1 <= d <= CAMPAIGN_DAYS):
            errors.append(cuisine + "/" + rid + " gecersiz gun: " + str(d))
        days.append(d)

        beats = [b["beat"] for b in r["story"]]
        if beats != sorted(beats) or len(set(beats)) != len(beats):
            errors.append(cuisine + "/" + rid + " sahne sirasi bozuk")
        visits = [b["requiresVisits"] for b in r["story"]]
        if visits != sorted(visits):
            errors.append(cuisine + "/" + rid + " sahne ziyaret esikleri artmiyor")

    if len(rows) != 10:
        errors.append(cuisine + " on duzenli musteri olmali, " +
                      str(len(rows)) + " var")
    if sorted(days) != days:
        errors.append(cuisine + " gelis gunleri artan sirada olmali")

    # Turk mutfaginda veresiye ADAYI olmali; fast food'da olmamali.
    # docs/07: veresiye Turk mutfaginin imza mekanigi, fast food'un degil.
    credit = [r["id"] for r in rows if r["veresiyeEligible"]]
    if cuisine == "turk" and len(credit) < 4:
        errors.append("turk: veresiye adayi az (" + str(len(credit)) + ")")
    if cuisine == "fastfood" and credit:
        errors.append("fastfood: veresiye Turk mutfaginin mekanigi, aday olmamali")

    return credit


def write(path, obj):
    text = json.dumps(obj, ensure_ascii=False, indent=2)
    io.open(path, "w", encoding="utf-8", newline="\n").write(text + "\n")
    print("yazildi: " + os.path.relpath(path, ROOT).replace("\\", "/"))


def main():
    errors = []
    if not os.path.isdir(OUT_DIR):
        os.makedirs(OUT_DIR)

    for cuisine, table in CUISINES:
        rows = build(cuisine, table)
        credit = check(cuisine, rows, errors)
        print("")
        print("--- " + cuisine + " ---")
        print("id                  arketip                  yemek              gun  veresiye")
        for r in rows:
            print("%-19s %-24s %-18s %3d  %s" % (
                r["id"], r["archetypeBase"], r["favouriteDish"],
                r["arrivesFromDay"], "evet" if r["veresiyeEligible"] else "-"))
        print("veresiye adayi: %d" % len(credit))
        if not errors:
            write(os.path.join(OUT_DIR, cuisine + ".json"), rows)

    if errors:
        print("")
        print("--- HATA (%d) ---" % len(errors))
        for e in errors:
            print("  " + e)
        return 1

    print("")
    print("butun kurallar gecti.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
