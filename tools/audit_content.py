# -*- coding: utf-8 -*-
"""
Icerik-kod sozlesme denetleyicisi
============================================================================
Tek soruyu soruyor: **icerikteki her alanin simulasyonda karsiligi var mi?**

10 Eylul 2026'da bir gunde dort ayri hata bulundu ve dordu de ayni siniftan:
icerik bir sey vaat ediyor, simulasyon onu hic okumuyor.

  spoilDays        44 malzemenin raf omru yazilmisti, kod hepsini gece siliyordu
  tatli grubu      9 tatli yemegi vardi, PickOrder tatliya hic bakmiyordu
  yemek gruplari   mutfaga ozel tasarlanmisti, kod fast food sozlugunu sabitlemisti
  attendBp         docs/27 Karar D aylardir kagittaydi, mutfak uygulamamisti

Hicbiri derlemeyi bozmuyor, hicbiri testi kirmiyor, hepsi SESSIZCE olu.
Bu betik o sinifi mekanik olarak tariyor:

  1. content/*.json icindeki butun anahtarlar
  2. DTO'larin bagladigi anahtarlar  ([JsonProperty])
  3. Cekirdek tiplerinin actigi ozellikler
  4. O ozelliklerin cekirdekte GERCEKTEN okunup okunmadigi

Calistirma:  python tools/audit_content.py
Cikis kodu:  bulgu varsa 1
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

# Bu anahtarlar bilincli olarak kodda okunmuyor; sebebi yaninda.
IGNORED_KEYS = {
    "_comment": "uretilen dosya notu",
    "schemaVersion": "surum damgasi, henuz gocurme yok",
    "id": "kimlik, Def kurucusunda tasiniyor",
    "nameKey": "gorunum metni, cekirdek metin bilmiyor",
    "source": "altin veri kaynagi notu",
    "wardrobe": "sanat hatti, cekirdek disi (docs/24)",
    "cuisines": "yukleme suzgeci",
    "cuisine": "yukleme suzgeci",
    "tier": "TierIndex olarak baglaniyor",
    "tiers": "yapisal",
    "roles": "yapisal",
    "stations": "yapisal",
    "storage": "yapisal",
    "order": "yapisal",
    "staffing": "yapisal",
    "menuRoles": "yapisal",
    "main": "menuRoles yapisal",
    "side": "menuRoles yapisal",
    "drink": "menuRoles yapisal",
    "dessert": "menuRoles yapisal",
    "weeks": "altin veri, testte okunuyor",
    "toleranceCenti": "altin veri, testte okunuyor",

    # --- gorunum ve sanat: cekirdek bunlari bilmiyor, bilmemeli ---
    "plating": "tabak sunumu, sanat hatti (docs/24)",
    "toppings": "tabak sunumu, sanat hatti",
    "base": "tabak sunumu, sanat hatti",
    "unit": "malzeme birimi (kg), gorunum metni",

    # --- Dictionary olarak baglanan ic anahtarlar ---
    "dusuk": "qualityPriceMultiplierBp icindeki kalite anahtari",
    "standart": "qualityPriceMultiplierBp icindeki kalite anahtari",
    "yuksek": "qualityPriceMultiplierBp icindeki kalite anahtari",
    "ilkbahar": "seasonModifierBp icindeki mevsim anahtari",
    "yaz": "seasonModifierBp icindeki mevsim anahtari",
    "sonbahar": "seasonModifierBp icindeki mevsim anahtari",
    "kis": "seasonModifierBp icindeki mevsim anahtari",
    "fastfood": "cuisineStations icindeki mutfak anahtari",
    "turk": "cuisineStations icindeki mutfak anahtari",
    "cuisineStations": "yapisal",
    # Huy etkileri Dictionary<string,int> olarak baglaniyor; DTO'da alan
    # olarak gorunmuyorlar. Karsiligi TraitDef ozellikleri.
    "speedBp": "TraitDef.SpeedBp (effects sozlugu)",
    "satisfactionCenti": "TraitDef.SatisfactionCenti (effects sozlugu)",
    "wageBp": "TraitDef.WageBp (effects sozlugu)",
    "xpBp": "TraitDef.XpBp (effects sozlugu)",
    "peakPenaltyBp": "TraitDef.PeakPenaltyBp (effects sozlugu)",
    "fatiguePenaltyBp": "TraitDef.FatiguePenaltyBp (effects sozlugu)",
    "moraleAura": "TraitDef.MoraleAura (effects sozlugu)",
    "peakImmune": "TraitDef.PeakImmune (effects sozlugu)",
    "fatigueImmune": "TraitDef.FatigueImmune (effects sozlugu)",
    "qualityBp": "TraitDef.QualityBp (effects sozlugu)",
    "cleanlinessBp": "TraitDef.CleanlinessBp (effects sozlugu)",
    "ownerPool": "DEGISMEZ: ContentLoader 'salon' disini reddediyor (docs/14)",
    "unlockSeason": "DEGISMEZ: unlockDay'den turetiliyor, yuklemede dogrulaniyor",
    "weeklyWageMultiplierBp": "DEGISMEZ: weeklyXpWageGrowthBp'nin bilesikleri",
}

# TASARLANDI AMA YAZILMADI. Bunlar hata degil, KUYRUK. Denetleyici ayri
# bir baslikta gosteriyor ki "yok sayilanlar" ile karismasinlar; bir sistem
# yazildiginda buradan silinmeli.
PLANNED = {
    "unlockSeason": "mevsime bagli yemek kilidi (docs/09)",
    "kitchenMsPerPerson": "docs/27 turetimi; prepMs icerikten geliyor",
}

# --- 4. kontrolun sozlukleri --------------------------------------------
#
# docs/13 semasi Faz 0'DAN ONCE yazildi ve docs/23 2.2 sonradan butun
# birimleri tamsayiya cevirdi (saniye -> ms, ondalik -> baz puan). Yani
# semadaki bircok ad, UYGULANMIS bir alanin eski adi. Denetleyici bunlari
# "eksik" diye sayarsa gercek eksikler 63 satirin icinde kayboluyor -
# nitekim kayboluyordu.
SCHEMA_RENAMED = {
    # economy.json - docs/23 2.2 tamsayi birimleri
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
    "hourSplit": "slotDurationsBp (docs/28 Karar G: pay degil SURE)",
    "acilis": "slotDurationsBp[0]",
    "ogle": "slotDurationsBp[1]",
    "ogleden_sonra": "slotDurationsBp[2]",
    "aksam": "slotDurationsBp[3]",
}

# Semada YAZILI ama icerige yazilmayan, cunku ZATEN YUKLU alanlardan
# TURETILIYOR. docs/34 11: elle yazilan tablo, ayni karakteri ikinci kez
# yazmaktir ve iki yerde yazilan sey sessizce ayrisir.
SCHEMA_DERIVED = {
    "orderPreference": "priceSensitivityBp + tipChanceBp'den turetiliyor",
    "spendTendency": "priceSensitivityBp",
    "regularChance": "arketip weight + tierIndex",
    "perishableRatio": "malzemelerin perishable alanindan sayiliyor",
    "marketPrice": "basePrice x mevsim x gunluk hal oynamasi",
    "sulu": "orderPreference ornegindeki grup adi",
    "corba": "orderPreference ornegindeki grup adi",
    "pilav": "orderPreference ornegindeki grup adi",
}

# GERCEKTEN YAZILMAMIS. Kuyruk bu; her satir bir sistem.
SCHEMA_PLANNED = {
    # regulars/*.json - dosya hic yok
    "archetypeBase": "isimli duzenli musteri (docs/11)",
    "arrivesFromDay": "isimli duzenli musteri (docs/11)",
    "favouriteDish": "isimli duzenli musteri (docs/11)",
    "jobKey": "isimli duzenli musteri (docs/11)",
    "veresiyeEligible": "isimli duzenli musteri; su an SIK arketipten turetiliyor",
    "story": "duzenli musteri hikaye sahneleri (docs/11)",
    "beat": "duzenli musteri hikaye sahneleri",
    "requiresVisits": "duzenli musteri hikaye sahneleri",
    "requiresSatisfaction": "duzenli musteri hikaye sahneleri",
    "textKey": "duzenli musteri hikaye sahneleri",
    # staff-traits.json - dosya hic yok
    "effects": "personel huyu ve is yukseltmesi (docs/14)",
    "conflictsWith": "personel huyu (docs/14): birlikte olamayan huylar",
    "speed": "personel huyunun hiz etkisi (docs/14)",
    "cleanliness": "personel huyunun temizlik etkisi (docs/14)",
    # upgrades.json - dosya hic yok
    "capacityBonus": "is yukseltmesi (docs/13 upgrades.json)",
    "customerBaseBonus": "is yukseltmesi (docs/13 upgrades.json)",
    # digerleri
    "dailySpecial": "gunun yemegi; SetDailySpecial komutu var, isleyicisi yok",
    "scoreAxis": "yil sonu puanlama ekseni (docs/08)",
    "free": "mutfagin ucretsiz olup olmadigi; para kazanma katmani (docs/07)",
    "batchSize": "Japon corba suyu imza mekanigi; o mutfak yazilmadi",
    "tags": "yemek etiketleri; arama ve filtre, arayuz isi",
    "palette": "sanat hatti (docs/10 mutfak kimligi)",
    "wardrobeSet": "sanat hatti (docs/10)",
    "wardrobeTags": "sanat hatti (docs/10)",
}

# Cekirdekte okunmadigi HALDE sorun olmayan ozellikler.
IGNORED_PROPS = {
    "Id": "kimlik",
    "NameKey": "gorunum metni",
    "Cuisine": "kimlik",
    "MaxTier": "tureviyor",
    "TierCount": "tureviyor",
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
    """content/ altindaki butun JSON anahtarlari -> hangi dosyalarda."""
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

        # YERELLESTIRME TABLOSU ICERIK DEGIL, duz bir anahtar->metin
        # esleme. Denetci onu da tarayinca dort yuz iki "baglanmamis
        # anahtar" uydurdu ve gercek bulgular o gurultunun altinda kaldi.
        # Tablonun butunlugunu zaten gen_loc.py dogruluyor.
        if rel.startswith("content/loc/"):
            continue

        try:
            visit(json.loads(read(p)), rel)
        except ValueError as e:
            print("  ! JSON okunamadi: %s (%s)" % (rel, e))
    return found


def dto_bound():
    """DTO'larin [JsonProperty(...)] ile bagladigi anahtarlar."""
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
    """DTO alanlari: JSON anahtari -> C# ozellik adi."""
    out = {}
    for p in walk(DTO_DIR, ".cs"):
        for m in DTO_FIELD.finditer(read(p)):
            out[m.group(1)] = m.group(2)
    return out


def loader_text():
    """Yalnizca icerik yukleyici. DTO alaninin KULLANILDIGI yer burasi."""
    return chr(10).join(
        read(p) for p in walk(DTO_DIR, ".cs")
        if "Dto" not in os.path.basename(p))


PROP = re.compile(r"public\s+(?:readonly\s+)?[\w\[\]<>?]+\s+(\w+)\s*(?:\{\s*get|;)")


def core_props():
    """Cekirdek Content ve Economy tiplerinin actigi ozellikler."""
    props = {}
    for sub in ("Content", "Economy"):
        for p in walk(os.path.join(CORE, sub), ".cs"):
            rel = os.path.relpath(p, ROOT).replace("\\", "/")
            for m in PROP.finditer(read(p)):
                props.setdefault(m.group(1), rel)
    return props


def solution_text():
    """
    Butun C# kaynagi: cekirdek, icerik yukleyici, denge araci, testler.

    Ayrim onemli. Bir alanin CEKIRDEKTE okunmamasi tek basina hata degil:
    dilim sureleri cekirdek disinda TimingConfig'e veriliyor, kapasite
    yukleyicide dogrulaniyor. Asil bulgu HICBIR YERDE okunmayan alan;
    o, icerigin bos bir vaadi demek.
    """
    parts = []
    for root in (ASSETS, os.path.join(ROOT, "src"), os.path.join(ROOT, "tests")):
        if os.path.isdir(root):
            parts.extend(read(q) for q in walk(root, ".cs"))
    return chr(10).join(parts)


def core_text():
    """Yalnizca cekirdek."""
    return "\n".join(read(p) for p in walk(CORE, ".cs"))


# ---------------------------------------------------------------------------
def main():
    print("=" * 74)
    print("ICERIK-KOD SOZLESME DENETIMI")
    print("=" * 74)

    keys = json_keys()
    bound = dto_bound()
    props = core_props()
    body = core_text()
    everywhere = solution_text()

    problems = []

    # --- 1. Icerikte var, DTO baglamiyor -------------------------------
    print()
    print("1. Icerikte VAR, hicbir DTO baglamiyor")
    print("-" * 74)
    unbound = []
    for k in sorted(keys):
        if k in bound or k in IGNORED_KEYS or k in PLANNED:
            continue
        unbound.append(k)
        files = sorted(keys[k])
        print("   %-24s %s" % (k, files[0] + ("" if len(files) == 1 else
                                              " (+%d dosya)" % (len(files) - 1))))
    if not unbound:
        print("   yok")
    else:
        problems.append(("baglanmayan anahtar", len(unbound)))

    # --- 2. DTO bagliyor, cekirdek okumuyor ----------------------------
    #
    # Asil tehlikeli sinif bu: alan JSON'dan okunuyor, bellege giriyor,
    # ve orada duruyor. spoilDays tam olarak boyleydi.
    print()
    print("2. Cekirdek tipinde VAR, HICBIR YERDE okunmuyor")
    print("-" * 74)

    def reads(name, text):
        uses = len(re.findall(r"\b" + re.escape(name) + r"\b", text))
        decls = len(re.findall(r"public\s+(?:readonly\s+)?[\w\[\]<>?]+\s+"
                               + re.escape(name) + r"\b", text))
        # \b SART: bu satir once sinirsizdi ve "SpeedBp" sayacini
        # "_cookXpSpeedBp =" gibi UZUN adlarin atamalari sisiriyordu.
        # Sonuc: gercekten okunan bir alan "hicbir yerde okunmuyor"
        # diye rapor ediliyor, ve gercek bulgular arasinda kayboluyordu.
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
        print("   yok")
    else:
        problems.append(("hicbir yerde okunmayan ozellik", len(dead)))

    print()
    print("   yalnizca cekirdek DISINDA okunanlar (sorun degil):")
    print("   " + (", ".join(only_outside) if only_outside else "yok"))

    print()
    # --- 3. DTO bagliyor ama yukleyici hic kullanmiyor -----------------
    #
    # En sinsi sinif. Alan JSON'dan okunuyor, bellege giriyor ve ORADA
    # KALIYOR: cekirdek tipine hic aktarilmiyor. Iki kontrolun arasindan
    # kaciyor, cunku hem "baglanmis" hem "cekirdekte yok". seasonDays ve
    # unlockSeason tam boyleydi.
    print()
    print("3. DTO bagliyor ama YUKLEYICI hic kullanmiyor")
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
        print("   %-24s (DTO ozelligi: %s)" % (key, prop))
    if not stranded:
        print("   yok")
    else:
        problems.append(("yukleyiciye ulasmayan alan", len(stranded)))

    print()
    print("   TASARLANDI AMA YAZILMADI (hata degil, kuyruk):")
    for k in sorted(set(planned_seen) | (set(PLANNED) & set(keys) - set(bound))):
        print("      %-26s %s" % (k, PLANNED[k]))

    # --- 4. Sema belgesinde VAR, icerikte YOK --------------------------
    print()
    print("4. docs/13 semasinda VAR, uretilen icerikte YOK")
    print("-" * 74)
    schema = os.path.join(ROOT, "docs", "13-veri-semalari.md")
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
        print("   yok")
    else:
        problems.append(("semada olup icerikte olmayan", len(missing)))

    if renamed:
        print()
        print("   ADI DEGISTI (docs/23 2.2 tamsayi birimleri) - uygulanmis:")
        for k in renamed:
            print("      %-26s -> %s" % (k, SCHEMA_RENAMED[k]))
    if derived:
        print()
        print("   TURETILIYOR (docs/34 11) - icerige yazilmiyor:")
        for k in derived:
            print("      %-26s %s" % (k, SCHEMA_DERIVED[k]))
    if planned:
        print()
        print("   YAZILMAMIS (kuyruk):")
        for k in planned:
            print("      %-26s %s" % (k, SCHEMA_PLANNED[k]))

    # --- ozet ----------------------------------------------------------
    print()
    print("=" * 74)
    if not problems:
        print("TEMIZ: icerikteki her alanin karsiligi var.")
        return 0
    for name, n in problems:
        print("%-32s %d" % (name, n))
    print()
    print("Her biri ya UYGULANMALI ya da IGNORED listesine sebebiyle yazilmali.")
    return 1


if __name__ == "__main__":
    sys.exit(main())
