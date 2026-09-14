# -*- coding: utf-8 -*-
"""
content/staff-traits.json uretir: on iki personel huyu.

docs/14 "Huy" tablosu birebir. Tasarim niyeti orada yazili:

    "Hicbir huy saf iyi veya saf kotu degil. Cirak ucuz ama yavas,
     tecrubeli hizli ama pahali. Dogru huy dogru istasyona bagli."

Bu yuzden uretec bir DENGE KURALI dayatiyor: her huyun ya bir bedeli ya
bir sarti olmali. Bedelsiz bir huy yazmak, huy secimini karar olmaktan
cikarip "en iyisini al" dugmesine cevirir.

docs/13 semasi ondalikli yaziyor ("speed": 0.18); docs/23 2.2 butun
birimleri tamsayiya cevirdi, o yuzden burada baz puan: speedBp 1800.

Calistirma:
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
# (id, etkiler, cakistigi huy)
#
# Etki adlari:
#   speedBp            gorev suresini bolen carpan farki (+1800 = %18 hizli)
#   satisfactionCenti  servis ettigi masada memnuniyet farki
#   wageBp             ucret farki
#   xpBp               deneyim kazanim carpani (20000 = iki kat, 0 = hic)
#   peakPenaltyBp      YOGUN dilimde ek yavaslama
#   fatiguePenaltyBp   gunun son ceyreginde ek yavaslama
#   moraleAura         diger personelin moraline etkisi
#   cleanlinessBp      temizlik; bulasik ve masa toplama hizini etkiler
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

# Bedelsiz sayilmayan alanlar: bunlardan biri varsa huyun bir bedeli var.
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
        errors.append("tekrarlanan huy kimligi")
    if len(rows) != 12:
        errors.append("docs/09 on iki huy istiyor, %d var" % len(rows))

    for r in rows:
        # Cakisma SIMETRIK olmali: A ile B cakisiyorsa B ile A da cakisir.
        # Tek yonlu yazilmis bir cakisma, ise alim kodunda sessizce
        # calismayan bir kural birakir.
        for other in r["conflictsWith"]:
            if other not in ids:
                errors.append("%s olmayan huyla cakisiyor: %s" % (r["id"], other))
                continue
            back = next(x for x in rows if x["id"] == other)
            if r["id"] not in back["conflictsWith"]:
                errors.append("cakisma tek yonlu: %s -> %s" % (r["id"], other))

    # docs/14: "hicbir huy saf iyi veya saf kotu degil."
    #
    # Bu kural ilk yazildiginda iki huyu yakaladi: musteriyle iyi anlasan
    # (+8 memnuniyet) ve ekip moralini yukselten (+10 moral) - ikisinin de
    # etki tablosunda hicbir bedeli yok.
    #
    # Ama bedelleri VAR, sadece huyun icinde degil HAVUZUN icinde: her
    # birinin bir KOTU IKIZI var (suratsiz, huysuz) ve ikisi cakisiyor.
    # Ise alim ikisinden birini cekiyor, yani iyi huy bir SANS - secilen
    # bir avantaj degil. Bedel, kotu ikizin ayni havuzda durmasi.
    #
    # Dogru kural su: bir huyun ya kendi icinde bedeli olacak, ya da
    # cakistigi bir huy onun AYNASI olacak. Aynasiz ve bedelsiz bir huy,
    # huy secimini "en iyisini al" dugmesine cevirir.
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
            errors.append("%s bedelsiz ve aynasiz bir huy" % r["id"])


def weigh(r):
    """Bir huyun kac iyi, kac kotu etkisi var."""
    good = bad = 0
    for k, v in r["effects"].items():
        if k.endswith("Immune"):
            good += 1
        elif k in ("peakPenaltyBp", "fatiguePenaltyBp"):
            bad += 1
        elif k == "wageBp":
            # Ucret ARTISI bedel, indirimi avantaj.
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

    print("id                        etkiler")
    for r in rows:
        print("%-25s %s" % (r["id"], json.dumps(r["effects"], ensure_ascii=False)))

    if errors:
        print("")
        print("--- HATA (%d) ---" % len(errors))
        for e in errors:
            print("  " + e)
        return 1

    path = os.path.join(CONTENT, "staff-traits.json")
    io.open(path, "w", encoding="utf-8", newline="\n").write(
        json.dumps(rows, ensure_ascii=False, indent=2) + "\n")
    print("")
    print("yazildi: content/staff-traits.json")
    print("butun kurallar gecti.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
