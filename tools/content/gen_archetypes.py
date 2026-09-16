# -*- coding: utf-8 -*-
"""
Arketip uretici - Faz 0
============================================================================
docs/11-customer-system.md icindeki yirmi arketibi uc dosyaya yazar:

  content/archetypes/shared.json     8 paylasilan arketip (her mutfakta)
  content/archetypes/fastfood.json  12 fast food'a ozel
  content/archetypes/turk.json      12 Turk mutfagina ozel

Her mutfak kendi 12 arketibini paylasilan 8 ile birlestirip 20 slotu doldurur.
Sikilik kademesi dagilimi (docs/11): sik 8, orta 8, nadir 4.

BIRIMLER - docs/23-core-contract.md 2.2 ve 8.4 baglayici:
  sure            milisaniye      8 sn sabir = 8000
  oran, carpan    baz puan (bp)   10000 = 1,0
  memnuniyet      santi-puan      100 puan = 10000
  itibar agirligi baz puan        1,4 kat = 14000; elestirmen 8 kat = 80000
Ondalik nokta YOK. Dogrulayici JSON icinde nokta gorurse dosyayi reddeder.

SAYILARIN KAYNAGI - docs/12-economy.md:
  5.2 sabir      kurye 8, aceleci ogrenci 10, ogle molasi calisani 12,
                 ofis grubu 18, aile 30, emekli 40 saniye. Bunlar sabit,
                 digerleri ayni ruhta secildi.
  5.3 fiyat duyarliligi 0,4 - 2,5 araligi -> 4000 - 25000 bp
  5.5 itibar agirligi; paylasimci uc kat, elestirmen en yuksek
  5.6 saat dagilimi hedefleri:
        fast food  15 / 35 / 15 / 35
        turk       10 / 60 / 20 / 10

AGIRLIK TASARIMI:
  Her mutfagin 20 arketibinin agirlik toplami tam 10000. Kademe paylari
  docs/11 icindeki trafik payini tutuyor: sik 7000, orta 2500, nadir 500.
  Paylasilan 8 arketip iki mutfakta da ayni agirlikla gorunur; mutfaga ozel
  12 arketip kalan payi doldurur. Bu yuzden paylasilan profiller notr tutuldu,
  mutfak ritmini tasiyan kisim ozel arketipler.

Calistirma:  python gen_archetypes.py
"""
import io
import json
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUTDIR = os.path.join(ROOT, "content", "archetypes")

BP = 10_000          # 1,0 = 10000 baz puan
SLOTS = ("acilis", "ogle", "ogleden_sonra", "aksam")
TIERS = ("sik", "orta", "nadir")

# docs/12 bolum 5.6, baz puan cinsinden
HOURLY_TARGET = {
    "fastfood": (1500, 3500, 1500, 3500),
    "turk":     (1000, 6000, 2000, 1000),
}
SPLIT_TOL_BP = 500           # 5 yuzde puani
RARE_MAX_BP = 1000           # nadir kademesi toplam agirligin yuzde 10 altinda
SENS_MIN, SENS_MAX = 4000, 25000
PATIENCE_MIN_CEIL = 10_000   # en sabirsiz bunun altinda olmali
PATIENCE_MAX_FLOOR = 35_000  # en sabirlisi bunun ustunde olmali

# docs/12 bolum 5.2 icinde sayisi verilmis, degistirilemez arketipler
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
# Veri. Alan sirasi:
#   id, kademe, agirlik, sabirMs, fiyatDuyarliligiBp, grupMin, grupMax,
#   itibarAgirligi(bp), bahsisSansiBp, (acilis, ogle, ogleden_sonra, aksam),
#   gardirop
# ---------------------------------------------------------------------------

# Paylasilan sekiz. Agirlik toplami: sik 2600, orta 800, nadir 110 = 3510.
SHARED = [
    ("yalniz_musteri",     "sik",   800, 16_000, 10_000, 1, 1, 10_000, 1200,
     (1500, 3800, 2000, 2700), ["gunluk"]),
    ("cift",               "sik",   650, 20_000,  9_000, 2, 2, 10_000, 1800,
     (600, 3200, 2200, 4000), ["sik_gunluk", "gunluk"]),
    # Sabir 30 sn: docs/12 bolum 5.2
    ("aile",               "sik",   600, 30_000, 11_000, 3, 5, 14_000, 2000,
     (800, 4200, 1800, 3200), ["gunluk", "cocuk"]),
    # Sabir 8 sn: docs/12 bolum 5.2. Masa isgal etmez, tek kalem.
    ("kurye",              "sik",   550,  8_000, 12_000, 1, 1,  6_000,  500,
     (1100, 5200, 1500, 2200), ["kurye_ceket", "kask"]),
    ("cocuklu_ebeveyn",    "orta",  320, 13_000, 11_500, 2, 3, 12_000, 1500,
     (700, 4000, 3000, 2300), ["ebeveyn", "cocuk"]),
    # Tek seferlik, itibar agirligi dusuk (docs/11)
    ("yolcu",              "orta",  260, 15_000,  6_000, 1, 2,  4_000,  800,
     (3000, 3600, 2100, 1300), ["yol_cantasi", "gunluk"]),
    # docs/11: itibar agirligi uc kati
    ("paylasimci",         "orta",  220, 19_000,  8_000, 1, 3, 30_000, 2500,
     (900, 3800, 2300, 3000), ["sik_gunluk", "telefon"]),
    # docs/12 bolum 5.5 ornegi: agirlik 8
    ("yemek_elestirmeni",  "nadir", 110, 26_000,  4_000, 1, 2, 80_000, 3000,
     (600, 4200, 1700, 3500), ["ceket", "defter"]),
]

# Fast food'a ozel on iki. Agirlik: sik 4400, orta 1700, nadir 390 = 6490.
FASTFOOD = [
    # Sabir 10 sn: docs/12 bolum 5.2. Butce dar, duyarlilik yuksek.
    ("aceleci_ogrenci",     "sik",  1300, 10_000, 22_000, 1, 2,  8_000,  400,
     (2000, 5000, 1500, 1500), ["okul_cantasi", "kapusonlu"]),
    # Sabir 18 sn: docs/12 bolum 5.2. Keskin ogle zirvesi.
    ("ofis_grubu",          "sik",  1200, 18_000,  9_000, 2, 4, 15_000, 1600,
     (1000, 7000, 1000, 1000), ["ofis_gomlek", "yaka_karti"]),
    ("antrenman_sonrasi",   "sik",   900, 17_000,  6_500, 1, 2,  9_000, 1800,
     (500, 1000, 1000, 7500), ["spor_esofman", "spor_cantasi"]),
    ("alisveris_molasi",    "sik",  1000, 16_000,  8_500, 2, 3, 10_000, 1200,
     (1500, 3000, 2500, 3000), ["alisveris_posetli", "sik_gunluk"]),
    # docs/11: fiyat duyarliligi cok yuksek, araligin tavani
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
    # Memnun etmesi cok zor: sabir dusuk, itibar riski yuksek
    ("sikayetci_musteri",   "nadir", 140,  9_000, 20_000, 1, 2, 35_000,  100,
     (1500, 4000, 2000, 2500), ["ceket", "gunluk"]),
    ("toplu_siparis",       "nadir", 100, 22_000, 18_000, 1, 1, 20_000, 2800,
     (2000, 6500, 1000, 500), ["is_yelegi", "kutu_tasima"]),
]

# Turk mutfagina ozel on iki. Agirlik: sik 4400, orta 1700, nadir 390 = 6490.
TURK = [
    ("esnaf_komsu",           "sik",  1200, 22_000,  9_000, 1,  2, 18_000, 2600,
     (1000, 7000, 1500, 500), ["onluk", "esnaf_yelegi"]),
    # Sabir 12 sn: docs/12 bolum 5.2. En sert ogle zirvesi.
    ("ogle_molasi_calisani",  "sik",  1250, 12_000, 14_000, 1,  3, 12_000,  900,
     (500, 8500, 1000, 0), ["ofis_gomlek", "yaka_karti"]),
    ("insaat_iscisi",         "sik",  1050, 20_000, 15_000, 2,  4, 11_000, 1400,
     (1500, 7500, 1000, 0), ["baret", "is_tulumu"]),
    ("memur",                 "sik",   900, 29_000, 12_000, 1,  3, 14_000, 1700,
     (500, 8000, 1500, 0), ["memur_ceket", "evrak_cantasi"]),
    # Sabir 40 sn: docs/12 bolum 5.2. Harcama dusuk, fiyata duyarli.
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
    # Sonucu itibari sert oynatir; bahsis vermez.
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
# Yardimcilar
# ---------------------------------------------------------------------------
def rnd_div(n, d):
    """n / d, yarisi sifirdan uzaga. Sadece tamsayi; kayan nokta girmez."""
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
    print("yazildi: " + os.path.relpath(path, ROOT).replace("\\", "/"))


def hourly_split(archetypes):
    """Agirlikli ortalama gelis dagilimi, bp. Tamsayi aritmetigi."""
    total = sum(a["weight"] for a in archetypes)
    out = []
    for i in range(4):
        acc = sum(a["weight"] * a["arrivalWeightsBp"][i] for a in archetypes)
        out.append(rnd_div(acc, total))
    return out, total


def pp(bp_diff):
    """bp farkini yuzde puani olarak ascii yazar: 250 -> 2.50"""
    return "{}.{:02d}".format(bp_diff // 100, bp_diff % 100)


# ---------------------------------------------------------------------------
# Dogrulama
# ---------------------------------------------------------------------------
def check(by_file, errors):
    def fail(msg):
        errors.append(msg)

    print("")
    print("== 1. ADET ==")
    for name, want in (("shared", 8), ("fastfood", 12), ("turk", 12)):
        got = len(by_file[name])
        print("  {:<9s} {:>3d} / {:<3d} {}".format(
            name, got, want, "OK" if got == want else "HATA"))
        if got != want:
            fail("adet {}: {} beklenirken {}".format(name, want, got))

    print("")
    print("== 2. KIMLIK, BIRIM, ALAN ==")
    seen = {}
    bad = {"id": 0, "dup": 0, "int": 0, "ward": 0, "group": 0, "arrival": 0}
    for name in ("shared", "fastfood", "turk"):
        for a in by_file[name]:
            if not ID_RE.match(a["id"]):
                bad["id"] += 1
                fail("id bicimi: " + a["id"])
            if a["id"] in seen:
                bad["dup"] += 1
                fail("id cakismasi: {} ({} ve {})".format(
                    a["id"], seen[a["id"]], name))
            else:
                seen[a["id"]] = name
            if a["nameKey"] != "archetype." + a["id"]:
                fail("nameKey uyumsuz: " + a["id"])
            for k, v in a.items():
                vals = v if isinstance(v, list) else [v]
                for e in vals:
                    if isinstance(e, bool) or isinstance(e, float):
                        bad["int"] += 1
                        fail("tamsayi degil: {}.{}".format(a["id"], k))
            if not (1 <= len(a["wardrobe"]) <= 3):
                bad["ward"] += 1
                fail("gardirop adedi: " + a["id"])
            for w in a["wardrobe"]:
                if not ID_RE.match(w):
                    bad["ward"] += 1
                    fail("gardirop bicimi: {} / {}".format(a["id"], w))
            if not (1 <= a["groupSizeMin"] <= a["groupSizeMax"]):
                bad["group"] += 1
                fail("grup buyuklugu: " + a["id"])
            s = sum(a["arrivalWeightsBp"])
            if len(a["arrivalWeightsBp"]) != 4 or s != BP:
                bad["arrival"] += 1
                fail("arrivalWeightsBp toplami {}: {}".format(s, a["id"]))
            if a["tier"] not in TIERS:
                fail("bilinmeyen kademe: " + a["id"])
            if not (0 <= a["tipChanceBp"] <= BP):
                fail("bahsis sansi araligi: " + a["id"])
            if a["reputationWeight"] <= 0:
                fail("itibar agirligi sifir ya da eksi: " + a["id"])
    print("  benzersiz id        : {:>3d}  ({} cakisma)".format(
        len(seen), bad["dup"]))
    print("  id bicimi           : {}".format("OK" if not bad["id"] else "HATA"))
    print("  butun alanlar tamsayi: {}".format("OK" if not bad["int"] else "HATA"))
    print("  gardirop 1-3 ascii  : {}".format("OK" if not bad["ward"] else "HATA"))
    print("  grup min <= max     : {}".format("OK" if not bad["group"] else "HATA"))
    print("  gelis toplami 10000 : {}".format("OK" if not bad["arrival"] else "HATA"))

    allrows = [a for n in ("shared", "fastfood", "turk") for a in by_file[n]]

    print("")
    print("== 3. SABIR (docs/12 bolum 5.2) ==")
    pmin = min(a["patienceMs"] for a in allrows)
    pmax = max(a["patienceMs"] for a in allrows)
    pmin_id = [a["id"] for a in allrows if a["patienceMs"] == pmin][0]
    pmax_id = [a["id"] for a in allrows if a["patienceMs"] == pmax][0]
    print("  en dusuk  : {:>6d} ms  {:<22s} (< {} olmali)".format(
        pmin, pmin_id, PATIENCE_MIN_CEIL))
    print("  en yuksek : {:>6d} ms  {:<22s} (> {} olmali)".format(
        pmax, pmax_id, PATIENCE_MAX_FLOOR))
    if pmin >= PATIENCE_MIN_CEIL:
        fail("sabir tabani {} >= {}".format(pmin, PATIENCE_MIN_CEIL))
    if pmax <= PATIENCE_MAX_FLOOR:
        fail("sabir tavani {} <= {}".format(pmax, PATIENCE_MAX_FLOOR))
    locked_ok = True
    for aid in sorted(PATIENCE_LOCKED):
        want = PATIENCE_LOCKED[aid]
        got = [a["patienceMs"] for a in allrows if a["id"] == aid]
        if not got:
            locked_ok = False
            fail("docs/12 5.2 arketibi yok: " + aid)
        elif got[0] != want:
            locked_ok = False
            fail("docs/12 5.2 sabir sapmasi {}: {} != {}".format(
                aid, got[0], want))
    print("  belge sabit degerleri: {}  ({} arketip)".format(
        "OK" if locked_ok else "HATA", len(PATIENCE_LOCKED)))

    print("")
    print("== 4. FIYAT DUYARLILIGI (docs/12 bolum 5.3: 0,4 - 2,5) ==")
    smin = min(a["priceSensitivityBp"] for a in allrows)
    smax = max(a["priceSensitivityBp"] for a in allrows)
    print("  ulasilan aralik : {} - {} bp".format(smin, smax))
    print("  hedef aralik    : {} - {} bp".format(SENS_MIN, SENS_MAX))
    for a in allrows:
        if not (SENS_MIN <= a["priceSensitivityBp"] <= SENS_MAX):
            fail("duyarlilik araligi disi: " + a["id"])
    if smin != SENS_MIN or smax != SENS_MAX:
        fail("duyarlilik araligi tam kapanmadi: {}-{}".format(smin, smax))

    print("")
    print("== 5. KADEME PAYLARI (docs/11: sik ~70, orta ~25, nadir ~5) ==")
    print("  mutfak    kademe  adet   agirlik  pay(bp)")
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
                fail("{}: bos kademe {}".format(cuisine, tier))
            if tier == "nadir" and share >= RARE_MAX_BP:
                fail("{}: nadir payi {} bp, {} altinda olmali".format(
                    cuisine, share, RARE_MAX_BP))
        if len(covered) != len(rows):
            fail("{}: kademeler butun arketipleri kapsamiyor".format(cuisine))
        if len(rows) != 20:
            fail("{}: mutfak toplami 20 degil ({})".format(cuisine, len(rows)))

    print("")
    print("== 6. SAAT DAGILIMI (docs/12 bolum 5.6) ==")
    print("  mutfak    slot            hedef  ulasilan  fark(pp)  durum")
    for cuisine in ("fastfood", "turk"):
        got, total = hourly_split(full[cuisine])
        tgt = HOURLY_TARGET[cuisine]
        for i, slot in enumerate(SLOTS):
            diff = abs(got[i] - tgt[i])
            ok = diff <= SPLIT_TOL_BP
            print("  {:<9s} {:<14s} {:>5d} {:>9d} {:>9s}  {}".format(
                cuisine, slot, tgt[i], got[i], pp(diff), "OK" if ok else "HATA"))
            if not ok:
                fail("{} {} sapmasi {} bp, sinir {}".format(
                    cuisine, slot, diff, SPLIT_TOL_BP))
        print("  {:<9s} {:<14s} {:>5d} {:>9d}   (agirlik toplami {})".format(
            cuisine, "TOPLAM", sum(tgt), sum(got), total))

    return full


def scan_files(errors):
    print("")
    print("== 7. ONDALIK NOKTA VE ASCII TARAMASI ==")
    for name in ("shared", "fastfood", "turk"):
        p = os.path.join(OUTDIR, name + ".json")
        with io.open(p, "r", encoding="utf-8") as f:
            txt = f.read()
        hits = DECIMAL_RE.findall(txt)
        non_ascii = [c for c in txt if ord(c) > 127]
        print("  {:<14s} ondalik: {:<3d} ascii disi: {:<3d} {}".format(
            name + ".json", len(hits), len(non_ascii),
            "OK" if not hits and not non_ascii else "HATA"))
        if hits:
            errors.append("{}.json: ondalik nokta bulundu".format(name))
        if non_ascii:
            errors.append("{}.json: ascii disi karakter bulundu".format(name))


def main():
    by_file = {}
    for name, cuisines, rows in SETS:
        by_file[name] = [build(r, cuisines) for r in rows]

    print("== ARKETIP URETICI ==")
    print("kaynak: docs/11-customer-system.md, docs/12-economy.md 5.2-5.6")
    print("birim : docs/23-core-contract.md 2.2 ve 8.4, tamsayi")
    print("")
    for name in ("shared", "fastfood", "turk"):
        write(os.path.join(OUTDIR, name + ".json"), by_file[name])

    errors = []
    check(by_file, errors)
    scan_files(errors)

    print("")
    if errors:
        print("== SONUC: BASARISIZ, {} hata ==".format(len(errors)))
        for e in errors:
            print("  - " + e)
        sys.exit(1)
    print("== SONUC: BUTUN KONTROLLER GECTI ==")


if __name__ == "__main__":
    main()
