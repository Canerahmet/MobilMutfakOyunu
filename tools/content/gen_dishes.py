# -*- coding: utf-8 -*-
"""
Yemek ve malzeme veri yazicisi - Faz 0
============================================================================
Uc dosya uretir:

  content/ingredients.json      malzeme katalogu, iki mutfak ortak
  content/dishes/fastfood.json  32 fast food yemegi
  content/dishes/turk.json      32 Turk mutfagi yemegi

Kaynak dokumanlar:
  docs/09-icerik-envanteri.md      menuler, acilis menusu, mevsim egrisi
  docs/12-ekonomi.md 3             malzeme maliyeti ~ satis fiyatinin %32'si
  docs/23-cekirdek-sozlesmesi.md   8.3 yemek semasi, 8.4 tamsayi birim kurali
  docs/24-sanat-hatti.md           moduler tabaklama, 6 taban + 8 ust parca

Birimler docs/23 2.2 ve 8.4'e gore, ONDALIK NOKTA YOK:
  para        santi-sikke   (1 sikke = 100)
  oran        baz puan      (10000 = 1,0)
  sure        milisaniye
  agirlik     gram
  malzeme     santi-sikke / kilogram   (unit alani "kg")

Fiyat TURETILIR, elle yazilmaz. Tarif gramajlari ve malzeme kilo fiyatlari
girdi; satis fiyati = maliyet / hedef malzeme orani, 50 santi-sikkeye
yuvarlanmis. Boylece docs/12'nin "%32 malzeme" kurali her yemekte
dogrulanabilir bir sayidan gelir.

Calistirma:  python tools/content/gen_dishes.py
Cikis kodu 1 ise denge kurallarindan biri bozulmustur, rapor sebebini yazar.
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

# docs/24-sanat-hatti.md: tabak = taban + 0-3 ust parca
BASES = ("bun", "plate_round", "plate_oval", "bowl", "tray", "paper", "cup")
TOPPINGS = ("patty", "slice", "sphere", "leaf", "strip", "sauce", "stick", "steam")
STATIONS = ("ocak", "izgara", "firin", "soguk", "icecek", "tatli")

# Mutfaga OZEL adlandirilmis ekipman. Paylasilan altinin ardina gelir,
# baslangicta YOKTUR, satin alinana kadar bagli yemekler kilitlidir.
# docs/09 mutfak basina 10 tane istiyor; simdilik uc ve iki tane var.
#
# Bu tablonun burada olmasinin sebebi aci bir olay: adlandirilmis
# istasyonlar dogrudan content/dishes/*.json icine yazilmisti ve URETEC
# ONLARI BILMIYORDU. gen_dishes.py'yi calistirmak hepsini sessizce
# "firin"a geri ceviriyordu. Artik istasyon yemek satirinda yaziyor ve
# ekipmanin "opens" listesi BURADAN turetiliyor (tools/balance/export.py).
CUISINE_STATIONS = {
    "turk": ("tas_firin", "doner_ocagi", "pide_firini"),
    "fastfood": ("milkshake_makinesi", "waffle_makinesi"),
}

# Tarifte gecmeyen ama mutfakta bulunmasi gereken kalemler. docs/09: cay
# menude satilan bir yemek degil, ikram kararidir; docs/23 8.2 teaCostCenti
# onun maliyetini okur.
SERVIS_MALZEMESI = ("cay",)

# Malzeme orani bandi: docs/12 %32 der, bant %28-%36
RATIO_MIN = 2800
RATIO_MAX = 3600

# Grup basina hedef malzeme orani. Ana yemek doyurucu ve pahali malzemeli,
# icecek en yuksek marjli. Hepsi banda rahat sigar.
# MALZEME ORANI HEDEFI: MUTFAK + GRUP.
#
# Olculdu ve vaat sayilarda YOKTU: fast food'un brut marji %64, Turk'un
# %56 idi. Yani "ucuz ve kalabalik" diye satilan mutfak DAHA KARLI
# olani. Gercekte tersi - zincirler birim basina az kazanip hacimle
# yasar.
#
# Fast food bandin UST ucuna cekildi (malzeme orani yuksek = marj dar).
# Turk DEGISMEDI: istenen sey fast food'un birim kazancinin dusmesiydi.
#
# ANAHTAR MUTFAK + GRUP, yalnizca grup DEGIL. Ilk denemede grup basina
# yazmistim ve Turk'un marji da dustu (17.351 -> 16.009): iki mutfak
# "icecek" ve "tatli" gruplarini PAYLASIYOR, yani grup basina bir hedef
# ikisini birden kaydiriyor. Paylasilan grubun hedefi mutfaga gore
# ayrilmadan bu ayrim yapilamaz.
#
# Ikisi de docs/12'nin %28-36 bandinin icinde.
GROUP_TARGET_BP = {
    "fastfood": {
        "ana": 3550, "yan": 3400, "icecek": 3250, "tatli": 3450,
    },
    "turk": {
        "sulu": 3300, "izgara": 3300, "corba": 3050,
        "pilav": 3050, "meze": 3050, "icecek": 2950, "tatli": 3150,
    },
}# Denetim icin kaba rol eslemesi: acilis menusunde ana, yan ve icecek sart
GROUP_ROLE = {
    "ana": "ana", "sulu": "ana", "izgara": "ana",
    "yan": "yan", "pilav": "yan", "meze": "yan", "corba": "yan",
    "icecek": "icecek", "tatli": "tatli",
}

# ---------------------------------------------------------------------------
# Mevsim ve kalite profilleri
# ---------------------------------------------------------------------------
# 10000 = degisim yok. docs/12: bazi malzemeler bir mevsim %20 ucuz ya da pahali.
SEASON = {
    "sabit":    (10000, 10000, 10000, 10000),
    "sebze":    (10200,  8600,  9000, 12000),
    "yesillik": ( 9200,  9600, 10400, 12400),
    "meyve":    (10600,  9000,  8800, 11600),
    "et":       (10000, 10500,  9500, 11500),
    "sut":      ( 9400, 10200, 10000, 11200),
}
# Kalite kademesi fiyat carpani ve memnuniyet etkisi (santi-puan).
# docs/13-veri-semalari.md kiyma ornegi "kahraman" profiline denk gelir.
QUALITY = {
    "kahraman": ((7500, 10000, 13500), (-2000, 0, 1500)),
    "orta":     ((8000, 10000, 12500), (-1200, 0,  800)),
    "temel":    ((8500, 10000, 11500), ( -500, 0,  300)),
}

FF = "fastfood"
TR = "turk"
IKI = [FF, TR]

# ---------------------------------------------------------------------------
# Malzemeler
#   (id, santi-sikke/kg, bozulur mu, bozulma gunu, mevsim, kalite, mutfaklar)
# ---------------------------------------------------------------------------
# Temel kiler, docs/09: 12 kalem, her mutfakta bulunur (shared = true)
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

# Kilerin disindaki malzemeler (shared = false). "mutfaklar" alani hangi
# menude gectigini soyler; ikisinde birden gecenler ikinci mutfagin maliyetini
# dusuren paylasilan tabani buyutur (docs/09 "paylasilan taban").
#
# FAST FOOD DONDURULMUS VE PAKETLI. docs/13 iki mutfak icin ayri bir
# perishableRatio vaat ediyordu (fast food 0,20 - Turk 0,60) ve olculdu:
# ikisi de 0,58 idi. Yani vaat edilen fark HIC YOKTU; iki mutfak bu
# eksende tipatip ayniydi.
#
# Fark gercek olmali, cunku mutfaklar arasindaki en somut oynanis farki
# bu: fast food AFFEDIYOR (kanat, fileto, kofte, dondurma karisimi hepsi
# dondurucudan cikiyor; sos, tursu, jalapeno kavanozda), Turk mutfagi
# AFFETMIYOR (her sey taze). Yanlis gunu fast food'da atlatirsin,
# Turk mutfaginda odersin - ve soguk hava deposu Turk mutfaginda cok
# daha erken bir zorunluluk.
#
# Kural: fast food'a OZEL bir malzeme gercek bir lokantada dondurulmus
# ya da kavanozda geliyorsa bozulmaz. Taze kalanlar ekmek, marul,
# lahana ve elma - yani burgerin USTUNE konan seyler.
SPECIFIC = [
    # -- et ve tavuk
    ("kiyma",             8500, True,   1, "et",       "kahraman", IKI),
    ("tavuk_gogus",       5400, True,   2, "et",       "kahraman", IKI),
    # tavuk_kanat artik yalnizca fast food: "tavuk kanat izgara" Turk
    # lokantasindan cikinca burada tarifsiz kaliyordu.
    ("tavuk_kanat",       4200, False,   0, "et",       "orta",     [FF]),
    ("doner_eti",         9200, True,   1, "et",       "kahraman", [TR]),
    ("balik_filetosu",    8200, False,   0, "et",       "kahraman", [FF]),
    ("sosis",             6400, False,   0, "et",       "orta",     [FF]),
    ("dana_kusbasi",     13000, True,   2, "et",       "kahraman", [TR]),
    ("kuzu_kusbasi",     17000, True,   2, "et",       "kahraman", [TR]),
    ("kuzu_pirzola_et",  19000, True,   2, "et",       "kahraman", [TR]),
    ("iskembe",           5000, True,   1, "et",       "orta",     [TR]),
    # -- ekmek ve hamur
    ("burger_ekmek",      2600, True,   3, "sabit",    "orta",     [FF]),
    ("hotdog_ekmek",      2800, True,   3, "sabit",    "orta",     [FF]),
    ("tost_ekmegi",       2800, True,   3, "sabit",    "orta",     [FF]),
    ("lavas",             2400, True,   4, "sabit",    "orta",     IKI),
    ("yufka",             3200, True,   5, "sabit",    "orta",     [TR]),
    ("makarna",           2600, False,  0, "sabit",    "temel",    [TR]),
    ("galeta_unu",        2800, False,  0, "sabit",    "temel",    IKI),
    ("misir_nisastasi",   3200, False,  0, "sabit",    "temel",    [TR]),
    ("kabartma_tozu",     9500, False,  0, "sabit",    "temel",    [FF]),
    ("maya",              8500, False,  0, "sabit",    "temel",    [FF]),
    ("irmik",             2600, False,  0, "sabit",    "temel",    [TR]),
    # -- sut urunu
    ("kasar",            12000, True,  12, "sut",      "orta",     IKI),
    ("mozzarella",       14000, False,  0, "sut",      "orta",     [FF]),
    ("yogurt",            3200, True,   6, "sut",      "orta",     IKI),
    ("beyaz_peynir",     13000, True,  12, "sut",      "orta",     [TR]),
    ("dondurma_karisimi", 6600, False,   0, "sut",      "orta",     [FF]),
    # -- sebze ve yesillik
    ("patates",           1900, True,  25, "sebze",    "temel",    IKI),
    ("marul",             3000, True,   3, "yesillik", "orta",     [FF]),
    ("lahana",            1400, True,  12, "sebze",    "temel",    [FF]),
    ("havuc",             1600, True,  18, "sebze",    "temel",    IKI),
    ("jalapeno",          5600, False,  0, "sebze",    "temel",    [FF]),
    ("tursu",             3400, False,  0, "sabit",    "temel",    [FF]),
    ("patlican",          2600, True,   6, "sebze",    "orta",     [TR]),
    ("yesil_biber",       2800, True,   5, "sebze",    "orta",     [TR]),
    ("kabak",             1900, True,   7, "sebze",    "temel",    [TR]),
    ("bamya",             5400, True,   4, "sebze",    "orta",     [TR]),
    ("taze_fasulye",      3000, True,   4, "sebze",    "orta",     [TR]),
    ("salatalik",         2200, True,   6, "sebze",    "temel",    [TR]),
    ("maydanoz",          2400, True,   3, "yesillik", "temel",    [TR]),
    # -- bakliyat ve tahil
    ("kuru_fasulye_tane", 3800, False,  0, "sabit",    "orta",     [TR]),
    ("nohut_tane",        3400, False,  0, "sabit",    "orta",     [TR]),
    ("mercimek",          3200, False,  0, "sabit",    "orta",     [TR]),
    ("bulgur",            2400, False,  0, "sabit",    "temel",    [TR]),
    ("pirinc",            3600, False,  0, "sabit",    "orta",     [TR]),
    # -- sos, baharat, tatlandirici
    ("burger_sos",        7200, False,  0, "sabit",    "orta",     [FF]),
    ("acili_sos",         7600, False,  0, "sabit",    "orta",     [FF]),
    ("ketcap",            4400, False,  0, "sabit",    "temel",    [FF]),
    ("mayonez",           5800, False,  0, "sabit",    "temel",    [FF]),
    ("salca",             5200, True,  30, "sabit",    "orta",     [TR]),
    ("sirke",             3200, False,  0, "sabit",    "temel",    [TR]),
    ("baharat_karisimi", 16000, False,  0, "sabit",    "orta",     IKI),
    ("kirmizi_biber",    18000, False,  0, "sabit",    "temel",    [TR]),
    ("kimyon",           20000, False,  0, "sabit",    "temel",    [TR]),
    ("nane",              3400, False,  0, "sabit",    "temel",    [TR]),
    ("tarcin",           26000, False,  0, "sabit",    "temel",    IKI),
    # -- icecek ve tatli girdisi
    ("gazoz_surubu",      6000, False,  0, "sabit",    "temel",    [FF]),
    ("kola_surubu",       6600, False,  0, "sabit",    "temel",    [FF]),
    ("cay",              24000, False,  0, "sabit",    "orta",     IKI),
    ("limon",             2800, True,  15, "meyve",    "temel",    IKI),
    ("elma",              2600, True,  20, "meyve",    "temel",    [FF]),
    ("cikolata",         18000, False,  0, "sabit",    "orta",     [FF]),
    ("kakao",            20000, False,  0, "sabit",    "orta",     [FF]),
    ("ceviz",            34000, False,  0, "sabit",    "orta",     [TR]),
    ("kadayif_tel",       4600, True,   8, "sabit",    "orta",     [TR]),
    ("vejetaryen_kofte",  5200, False,   0, "sabit",    "orta",     [FF]),
]

# ---------------------------------------------------------------------------
# Yemekler
#   (id, grup, istasyon, karmasiklik, prepMs, mevsim, taban, [ust parca],
#    [(malzeme, gram), ...])
# Mevsim 0 = kampanyanin ilk gunu acik olan menu (docs/09 "Acilis menusu"),
# JSON'da unlockSeason 1 olur, unlockDay 1 alir.
# ---------------------------------------------------------------------------
FASTFOOD_DISHES = [
    # ---- Ana, 12 kalem
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
    ("crispy_tavuk", "ana", "ocak", 2, 130000, 2, "paper", ["patty", "sauce"], [
        ("tavuk_gogus", 160), ("galeta_unu", 40), ("un", 20), ("yumurta", 25),
        ("aycicek_yagi", 30), ("baharat_karisimi", 5)]),
    ("tavuk_durum", "ana", "izgara", 2, 120000, 2, "paper", ["strip", "leaf", "sauce"], [
        ("tavuk_gogus", 120), ("lavas", 90), ("marul", 20), ("domates", 25),
        ("sogan", 10), ("mayonez", 20)]),
    ("balik_burger", "ana", "ocak", 2, 125000, 3, "bun", ["patty", "leaf", "sauce"], [
        ("balik_filetosu", 130), ("burger_ekmek", 80), ("galeta_unu", 25),
        ("marul", 15), ("mayonez", 20), ("limon", 10)]),
    ("vejetaryen_burger", "ana", "izgara", 2, 115000, 3, "bun", ["patty", "leaf", "slice"], [
        ("vejetaryen_kofte", 130), ("burger_ekmek", 80), ("marul", 20),
        ("domates", 25), ("sogan", 10), ("burger_sos", 20)]),
    ("et_durum", "ana", "izgara", 2, 135000, 3, "paper", ["strip", "leaf", "sauce"], [
        ("kiyma", 130), ("lavas", 90), ("marul", 20), ("domates", 25),
        ("sogan", 10), ("acili_sos", 15)]),
    # ---- Yan, 8 kalem
    ("patates_kizartma", "yan", "ocak", 1, 55000, 0, "paper", ["strip"], [
        ("patates", 200), ("aycicek_yagi", 25), ("tuz", 2)]),
    ("nugget", "yan", "ocak", 1, 65000, 0, "paper", ["sphere", "sauce"], [
        ("tavuk_gogus", 110), ("galeta_unu", 30), ("un", 15), ("yumurta", 15),
        ("aycicek_yagi", 20)]),
    ("baharatli_patates", "yan", "ocak", 1, 60000, 1, "paper", ["strip", "sauce"], [
        ("patates", 200), ("aycicek_yagi", 25), ("baharat_karisimi", 6)]),
    ("yesil_salata", "yan", "soguk", 1, 45000, 1, "bowl", ["leaf", "slice"], [
        ("marul", 90), ("domates", 50), ("havuc", 30), ("zeytinyagi", 10)]),
    ("sogan_halkasi", "yan", "ocak", 1, 70000, 2, "paper", ["sphere"], [
        ("sogan", 140), ("un", 30), ("galeta_unu", 25), ("yumurta", 20),
        ("aycicek_yagi", 25)]),
    ("acili_kanat", "yan", "ocak", 2, 140000, 2, "paper", ["sphere", "sauce"], [
        ("tavuk_kanat", 180), ("acili_sos", 30), ("baharat_karisimi", 4),
        ("aycicek_yagi", 15)]),
    ("mozzarella_cubuk", "yan", "ocak", 1, 80000, 3, "paper", ["stick", "sauce"], [
        ("mozzarella", 100), ("galeta_unu", 30), ("un", 15), ("yumurta", 15),
        ("aycicek_yagi", 20)]),
    ("coleslaw", "yan", "soguk", 1, 40000, 3, "bowl", ["strip"], [
        ("lahana", 120), ("havuc", 40), ("mayonez", 35), ("seker", 5),
        ("limon", 5)]),
    # ---- Icecek, 6 kalem
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
    # ---- Tatli, 6 kalem
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
    # ---- Sulu yemek, 9 kalem
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
    # ---- Corba, 4 kalem
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
    # ---- Pilav ve hamur, 4 kalem
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
    # ---- Izgara, 8 kalem (dordu adlandirilmis ekipmana bagli)
    ("kofte", "izgara", "izgara", 2, 120000, 0, "plate_oval", ["sphere", "leaf", "sauce"], [
        ("kiyma", 220), ("sogan", 25), ("galeta_unu", 15), ("yumurta", 10),
        ("karabiber", 2), ("kimyon", 2)]),
    ("tavuk_sis", "izgara", "izgara", 2, 140000, 1, "plate_oval", ["stick", "sphere", "leaf"], [
        ("tavuk_gogus", 200), ("yogurt", 25), ("yesil_biber", 25), ("sogan", 20),
        ("baharat_karisimi", 3)]),
    ("adana", "izgara", "izgara", 2, 150000, 2, "plate_oval", ["strip", "leaf", "sauce"], [
        ("kiyma", 200), ("kirmizi_biber", 4), ("kimyon", 3), ("sogan", 20),
        ("lavas", 60)]),
    # Doner ve pide: kullanicinin istedigi adlandirilmis ekipman.
    # "Izgara kademe 2 gerekli" soyut; "doner ocagini al, doner acilsin"
    # okunur. Ikisi de ana rolde, yani menunun omurgasina dokunuyorlar.
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
    # ---- Meze ve salata, 3 kalem
    ("coban_salata", "meze", "soguk", 1, 50000, 1, "bowl", ["slice", "leaf"], [
        ("domates", 100), ("salatalik", 80), ("yesil_biber", 30), ("sogan", 25),
        ("maydanoz", 8), ("zeytinyagi", 12), ("limon", 8)]),
    ("cacik", "meze", "soguk", 1, 40000, 1, "bowl", ["sauce", "leaf"], [
        ("yogurt", 150), ("salatalik", 70), ("sarimsak", 5), ("nane", 2),
        ("zeytinyagi", 8)]),
    ("piyaz", "meze", "soguk", 1, 60000, 1, "plate_oval", ["sphere", "slice", "leaf"], [
        ("kuru_fasulye_tane", 90), ("sogan", 30), ("domates", 40), ("maydanoz", 6),
        ("sirke", 10), ("zeytinyagi", 15), ("yumurta", 30)]),
    # ---- Tatli, 3 kalem
    ("sutlac", "tatli", "tatli", 2, 175000, 0, "bowl", ["sauce"], [
        ("sut", 250), ("pirinc", 30), ("seker", 45), ("misir_nisastasi", 8),
        ("tarcin", 2)]),
    ("kadayif", "tatli", "tas_firin", 3, 220000, 2, "plate_round", ["strip", "sphere"], [
        ("kadayif_tel", 100), ("ceviz", 30), ("seker", 60), ("tereyagi", 30),
        ("limon", 5)]),
    ("revani", "tatli", "tas_firin", 3, 200000, 3, "plate_round", ["slice", "sauce"], [
        ("irmik", 80), ("un", 30), ("seker", 70), ("yumurta", 40),
        ("yogurt", 40), ("limon", 5)]),
    # ---- Icecek, 1 kalem. docs/09 icecek grubu tanimlamiyor, docs/12 ayrani
    # fiyatliyor ve "tipik siparis: sulu yemek + pilav + ayran" diyor.
    ("ayran", "icecek", "icecek", 1, 25000, 0, "cup", [], [
        ("yogurt", 150), ("tuz", 3)]),
]

# ---------------------------------------------------------------------------
# Uretim
# ---------------------------------------------------------------------------
def ensure(d):
    if not os.path.isdir(d):
        os.makedirs(d)


def round50(x):
    """En yakin 50 santi-sikkeye yuvarla. Yarisi sifirdan uzaga."""
    return ((x + 25) // 50) * 50


def build_ingredients():
    out = []
    for iid, price, perish, spoil, season, quality in PANTRY:
        out.append(one_ingredient(iid, price, perish, spoil, season, quality, IKI, True))
    for iid, price, perish, spoil, season, quality, cuisines in SPECIFIC:
        out.append(one_ingredient(iid, price, perish, spoil, season, quality, cuisines, False))
    return out


def one_ingredient(iid, price, perish, spoil, season, quality, cuisines, shared):
    ilk, yaz, son, kis = SEASON[season]
    qp, qs = QUALITY[quality]
    return {
        "id": iid,
        "nameKey": "ingredient." + iid,
        "shared": shared,
        "cuisines": list(cuisines),
        # Santi-sikke / kilogram. Butun malzemeler kg; tarif gramaji bolerek
        # calisir, boylece maliyet tek bir tamsayi carpimiyla cikar.
        "basePrice": price,
        "unit": "kg",
        "perishable": perish,
        "spoilDays": spoil,
        "seasonModifierBp": {"ilkbahar": ilk, "yaz": yaz, "sonbahar": son, "kis": kis},
        "qualityPriceMultiplierBp": {"dusuk": qp[0], "standart": qp[1], "yuksek": qp[2]},
        "qualitySatisfactionCenti": {"dusuk": qs[0], "standart": qs[1], "yuksek": qs[2]},
    }


def cost_milli(recipe, prices):
    """Tarifin maliyeti, santi-sikke * 1000. Tamsayi kalsin diye bolmuyoruz."""
    return sum(prices[iid] * grams for iid, grams in recipe)


def unlock_days(rows):
    """Mevsim icindeki acilis gunlerini dagitir.

    Acilis menusu (mevsim alani 0) gun 1. Birinci mevsimin kalani 3..15
    arasina, sonraki mevsimler kendi 15 gunluk penceresine yayilir.
    docs/09: "ortalama iki gunde bir yeni yemek".
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
# prepMs turetme  (docs/23 8.3, docs/14 kapasite modeli)
# ---------------------------------------------------------------------------
# Bir servis gunu 480.000 ms; asci kapasitesi gunde 28 KISI. Simulasyonda
# her kisi bir tabak siparis ediyor, yani kisi basina mutfak butcesi:
#     480.000 / 28 = 17.142 ms
# Menunun ortalama prepMs'i bu sayiya esitlenir. Karmasiklik yalnizca
# yemekler ARASINDAKI orani belirler, mutlak degeri degil.
SERVICE_DAY_MS = 480_000
COOK_CAPACITY_PER_DAY = 28
KITCHEN_MS_PER_PERSON = SERVICE_DAY_MS // COOK_CAPACITY_PER_DAY

# Kisi basina kac TABAK pisiyor. Siparis modeli content/economy.json'da:
# bir ana yemek kesin, yan ve icecek olasilikli.
# Butce kisi basina; tabak basina sure buna bolunerek bulunur.
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

# Karmasikliga gore goreli agirlik
PREP_WEIGHT = {1: 70, 2: 100, 3: 150}


def derive_prep_ms(dishes):
    """
    Menunun ortalamasi KITCHEN_MS_PER_PERSON olacak sekilde prepMs atar.
    Yerinde degistirir ve (min, ort, max) doner.
    """
    total_w = 0
    for d in dishes:
        total_w += PREP_WEIGHT[d["complexity"]]
    if total_w == 0:
        return (0, 0, 0)

    # olcek = hedef_ortalama * adet / toplam_agirlik
    n = len(dishes)
    lo, hi, tot = None, None, 0
    for d in dishes:
        w = PREP_WEIGHT[d["complexity"]]
        ms = (MS_PER_DISH * n * w) // total_w
        ms = int(round(ms / 500.0)) * 500          # 500 ms'ye yuvarla
        if ms < 3000:
            ms = 3000                               # icecek bile bir sey aliyor
        d["prepMs"] = ms
        tot += ms
        lo = ms if lo is None or ms < lo else lo
        hi = ms if hi is None or ms > hi else hi
    return (lo, tot // n, hi)


# Kilit siniri. docs/34 4: yemek kilidi TAKVIM degil, ITIBAR + EKIPMAN.
# Kural mutfagin KENDI dagilimina gore, cunku "karmasiklik 3 -> kademe 2"
# gibi mutlak bir esik 32 fast food yemeginin 2'sini, 32 Turk yemeginin
# 17'sini yakaliyordu. Yemekler (karmasiklik, fiyat) ile siralanip
# boluuyor: alt %53 ekipman istemez, sonraki %28 kademe 1, ust %19 kademe 2.
# 32 yemekte bu 17 / 9 / 6 sira demek. Sinirlar ondalik gorunuyor cunku
# SAYIYA gore secildiler: dengeleme bu bolunmeyle yapildi ve bir yemegin
# kademe atlamasi olcumu gorunur bicimde kaydiriyor (fast food'da tek bir
# cizburger'in kademe 1'e gecmesi "makul oyuncu genisleyemiyor" ihlalini
# doguruyordu).
TIER0_BP = 5300
TIER1_BP = 8125

# Itibar esigi gunle dogru orantili: acilis gunu 0, sonra gun basina
# 0,9 puan. Altmisinci gunde 53,1 puan - yani son yemekler iyi yonetilen
# bir lokantada acilir, kotu yonetilende hic acilmaz.
REP_PER_DAY_CENTI = 90


# Istasyonun KAC KADEMESI var. equipment.json'dan turetilemez cunku o
# dosya bu uretecin cikti zincirinin ILERISINDE; degerler docs/27 3.3
# zirve tablosundan geliyor ve export.py orada dogruluyor.
#
# Bu tablo olmadan uretec var olmayan bir kademe isteyebiliyor: uc fast
# food tatlisi "firin kademe 2" istiyordu, firin merdiveni ise kademe
# 1'de bitiyor - yani o uc yemek altmis gun boyunca ACILAMIYORDU ve
# hicbir dogrulama bunu yakalamiyordu.
STATION_TIERS = {
    "ocak": 4,
    "izgara": 4,
    "firin": 2,
    "soguk": 2,
    "icecek": 2,
    "tatli": 2,
}

# Adlandirilmis mutfak ekipmani iki basamakli: yok / var.
NAMED_STATION_TIERS = 2


def max_tier(station):
    return STATION_TIERS.get(station, NAMED_STATION_TIERS) - 1


def unlock_gates(rows, cuisine, dishes, days):
    """
    requiresStationTier ve unlockReputationCenti'yi yerinde yazar.

    Bu iki alan bir zamanlar dogrudan content/dishes/*.json icine elle
    yazilmisti ve URETEC ONLARI BILMIYORDU: gen_dishes.py'yi calistirmak
    butun kilit sistemini sessizce siliyordu. Artik burada uretiliyorlar.
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

        # Adlandirilmis ekipman TAM OLARAK kademe 1 istiyor: o merdiven
        # iki basamakli (yok / var), kademe 2 diye bir sey yok.
        if d["station"] in named:
            tier = 1

        # Acilis menusu hicbir sey istemez, yoksa oyun kilitli baslar.
        if d["unlockDay"] <= 1:
            if d["station"] in named:
                raise AssertionError(
                    cuisine + "/" + d["id"] +
                    ": adlandirilmis ekipman acilis menusunde olamaz")
            tier = 0

        # Istasyonun sahip OLDUGU en ust kademeyle sinirli.
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
# Denetim
# ---------------------------------------------------------------------------
class Report(object):
    def __init__(self):
        self.errors = []

    def fail(self, msg):
        self.errors.append(msg)


def check_no_float(obj, path, rep):
    """8.4: JSON'da ondalik nokta yok. bool sayidan once yakalanmali."""
    if isinstance(obj, bool):
        return
    if isinstance(obj, float):
        rep.fail("ondalik deger: " + path)
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
            rep.fail("gecersiz malzeme id: " + iid)
        if iid in seen:
            rep.fail("tekrar eden malzeme id: " + iid)
        seen.add(iid)
        if ing["basePrice"] <= 0:
            rep.fail("sifir fiyat: " + iid)
        if ing["perishable"] and ing["spoilDays"] <= 0:
            rep.fail("bozulur ama spoilDays yok: " + iid)
        if not ing["perishable"] and ing["spoilDays"] != 0:
            rep.fail("bozulmaz ama spoilDays var: " + iid)
    return seen


def check_dishes(dishes, cuisine, ing_ids, ing_by_id, prices, rep):
    """Yemek basi kural denetimi. Malzeme orani raporunu da basar."""
    seen = set()
    ratios = []
    season_count = {1: 0, 2: 0, 3: 0, 4: 0}
    day1_roles = set()
    day1 = 0
    used = set()

    print("")
    print("--- " + cuisine + " ---")
    print("id                     grup    mvs gun  fiyat  malzeme   oran   prepMs  k")
    for d in dishes:
        did = d["id"]
        if not ID_RE.match(did):
            rep.fail(cuisine + " gecersiz yemek id: " + did)
        if did in seen:
            rep.fail(cuisine + " tekrar eden yemek id: " + did)
        seen.add(did)

        if (d["station"] not in STATIONS
                and d["station"] not in CUISINE_STATIONS.get(cuisine, ())):
            rep.fail(cuisine + "/" + did + " gecersiz istasyon: " + d["station"])
        if d["plating"]["base"] not in BASES:
            rep.fail(cuisine + "/" + did + " gecersiz tabak tabani")
        if len(d["plating"]["toppings"]) > 3:
            rep.fail(cuisine + "/" + did + " ucten fazla ust parca")
        for t in d["plating"]["toppings"]:
            if t not in TOPPINGS:
                rep.fail(cuisine + "/" + did + " gecersiz ust parca: " + t)
        if d["complexity"] not in (1, 2, 3):
            rep.fail(cuisine + "/" + did + " karmasiklik 1-3 disinda")
        if d["unlockSeason"] not in (1, 2, 3, 4):
            rep.fail(cuisine + "/" + did + " mevsim 1-4 disinda")

        # prepMs - karmasiklik bagi
        cx, prep = d["complexity"], d["prepMs"]
        if cx == 1 and not prep < 90000:
            rep.fail(cuisine + "/" + did + " karmasiklik 1 ama prepMs " + str(prep))
        if cx == 2 and not (90000 <= prep <= 180000):
            rep.fail(cuisine + "/" + did + " karmasiklik 2 ama prepMs " + str(prep))
        if cx == 3 and not prep > 180000:
            rep.fail(cuisine + "/" + did + " karmasiklik 3 ama prepMs " + str(prep))

        recipe = [(x["id"], x["grams"]) for x in d["ingredients"]]
        if not recipe:
            rep.fail(cuisine + "/" + did + " malzemesiz")
        for iid, grams in recipe:
            used.add(iid)
            if iid not in ing_ids:
                rep.fail(cuisine + "/" + did + " bilinmeyen malzeme: " + iid)
            elif cuisine not in ing_by_id[iid]["cuisines"]:
                rep.fail(cuisine + "/" + did + " malzeme bu mutfakta yok: " + iid)
            if grams <= 0:
                rep.fail(cuisine + "/" + did + " sifir gramaj: " + iid)

        cm = cost_milli(recipe, prices)
        ratio = cm * 10 // d["price"]
        ratios.append((ratio, did))
        if not (RATIO_MIN <= ratio <= RATIO_MAX):
            rep.fail(cuisine + "/" + did + " malzeme orani bant disi: " + str(ratio) + " bp")

        season_count[d["unlockSeason"]] += 1
        if d["unlockDay"] == 1:
            day1 += 1
            day1_roles.add(GROUP_ROLE[d["group"]])
        if not (1 <= d["unlockDay"] <= 60):
            rep.fail(cuisine + "/" + did + " gun 1-60 disinda")

        print("{:22s} {:7s} {:>2d} {:>4d} {:>6d} {:>8d}  {:>4d}bp {:>7d}  {:d}".format(
            did, d["group"], d["unlockSeason"], d["unlockDay"], d["price"],
            cm // 1000, ratio, prep, cx))

    if len(dishes) != 32:
        rep.fail(cuisine + " yemek sayisi 32 degil: " + str(len(dishes)))
    if day1 != 6:
        rep.fail(cuisine + " acilis menusu 6 degil: " + str(day1))
    for role in ("ana", "yan", "icecek"):
        if role not in day1_roles:
            rep.fail(cuisine + " acilis menusunde " + role + " yok")

    beklenen = {1: 13, 2: 8, 3: 6, 4: 5}
    if season_count != beklenen:
        rep.fail(cuisine + " mevsim dagilimi " + str(season_count) +
                 " beklenen " + str(beklenen))

    kum = 0
    kums = []
    for s in (1, 2, 3, 4):
        kum += season_count[s]
        kums.append(kum)
    print("mevsim dagilimi   : " + str([season_count[s] for s in (1, 2, 3, 4)]) +
          "  gun1 = " + str(day1))
    print("kumulatif menu    : 6 -> " + " -> ".join(str(k) for k in kums))
    ratios.sort()
    print("malzeme orani     : en dusuk {:d}bp ({:s})  en yuksek {:d}bp ({:s})".format(
        ratios[0][0], ratios[0][1], ratios[-1][0], ratios[-1][1]))

    perish = [i for i in used if ing_by_id[i]["perishable"]]
    print("bozulabilir oran  : {:d}% ({:d}/{:d} malzeme)".format(
        len(perish) * 100 // len(used), len(perish), len(used)))
    return ratios, used


def write(path, obj):
    ensure(os.path.dirname(path))
    text = json.dumps(obj, ensure_ascii=False, indent=2)
    m = DECIMAL_RE.search(text)
    if m:
        print("HATA: ondalik nokta var -> " + text[max(0, m.start() - 40):m.end() + 10])
        sys.exit(1)
    with io.open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(text)
        f.write(u"\n")
    print("yazildi: " + os.path.relpath(path, ROOT).replace("\\", "/"))


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
    print("--- toplam ---")
    print("malzeme           : {:d} ({:d} temel kiler, {:d} mutfaga ozel)".format(
        len(ings), len(PANTRY), len(SPECIFIC)))
    print("fast food yemek   : {:d}".format(len(ff)))
    print("turk yemek        : {:d}".format(len(tr)))
    hepsi = sorted(r1 + r2)
    print("malzeme orani     : {:d}bp ({:s}) .. {:d}bp ({:s}), hedef bant {:d}-{:d}".format(
        hepsi[0][0], hepsi[0][1], hepsi[-1][0], hepsi[-1][1], RATIO_MIN, RATIO_MAX))
    print("ortalama oran     : {:d}bp".format(sum(r[0] for r in hepsi) // len(hepsi)))

    # Olu icerik denetimi: kilerin disindaki her malzeme, bulundugunu soyledigi
    # mutfakta en az bir tarifte gecmeli. Tek istisna servis kalemleri.
    for ing in ings:
        if ing["shared"] or ing["id"] in SERVIS_MALZEMESI:
            continue
        for cuisine, used in ((FF, u1), (TR, u2)):
            if cuisine in ing["cuisines"] and ing["id"] not in used:
                rep.fail(cuisine + " mutfaginda hic tarifte gecmeyen malzeme: " +
                         ing["id"])

    if rep.errors:
        print("")
        print("--- DENGE HATASI ({:d}) ---".format(len(rep.errors)))
        for e in rep.errors:
            print("  " + e)
        sys.exit(1)

    ff_stats = derive_prep_ms(ff)
    tr_stats = derive_prep_ms(tr)
    print("")
    print("prepMs turetildi (kapasite modelinden):")
    print("  mutfak butcesi / kisi : {:d} ms".format(KITCHEN_MS_PER_PERSON))
    print("  ortalama tabak / kisi : {:.2f}".format(AVG_DISHES_PER_PERSON))
    print("  butce / tabak         : {:d} ms".format(MS_PER_DISH))
    print("  fastfood  min/ort/max : {:d} / {:d} / {:d} ms".format(*ff_stats))
    print("  turk      min/ort/max : {:d} / {:d} / {:d} ms".format(*tr_stats))
    for name, stats in (("fastfood", ff_stats), ("turk", tr_stats)):
        drift = abs(stats[1] - MS_PER_DISH)
        if drift * 100 > MS_PER_DISH * 3:
            print("  HATA: {:s} ortalamasi butceden %3'ten fazla sapiyor".format(name))
            sys.exit(1)

    write(os.path.join(CONTENT, "ingredients.json"), ings)
    write(os.path.join(DISHES_DIR, "fastfood.json"), ff)
    write(os.path.join(DISHES_DIR, "turk.json"), tr)
    print("butun denge kurallari gecti.")


if __name__ == "__main__":
    main()
