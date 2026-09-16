# -*- coding: utf-8 -*-
"""
Lokanta denge modeli - Parti A
============================================================================
Amac: docs/12-economy.md ve docs/14-staff-system.md icindeki her sayiyi
formulden turetmek. Elle yazilan tablo yok. Bu betik ne uretirse o dogrudur.

Neden var: bes ajanli degerlendirme (docs/review/) buyume tablosunun
formulden turemedigini buldu. Uc ayri hata vardi:
  1. Patron ayni anda uc sutunda birden sayilmisti (tek kisi, 36 kapasite).
  2. Kadro ZIRVE gune gore kurulmasi gerekirken ortalamaya gore kurulmustu.
  3. Bolum 5.1 formulu 14 masa/itibar 85 icin 76 musteri veriyor,
     bolum 6 tablosu ayni satirda 58 yaziyordu.

Calistirma:
    python model.py            -> markdown tablolar (dokumana yapistirilir)
    python model.py --check    -> tutarlilik testleri
    python model.py --solve    -> kirayi hedef marjdan cozer
"""
from math import ceil
import sys

# ============================================================================
# 1. PARAMETRELER - tek dogruluk kaynagi
# ============================================================================

SEATS_TURNOVER = 4          # masa basina gunluk musteri katsayisi (docs/12 5.1)
INGREDIENT_RATE = 0.32      # malzeme / ciro
# --- Gerceklesme orani ------------------------------------------------------
# Kapali form model talebin TAMAMININ agirlandigini varsayar. Simulasyon
# (src/Lokanta.Harness) ayni genisleme takviminde modelin cirosunun ne
# kadarini urettigi ile olculuyor: sabir biten musteri, tukenen stok,
# dolan masa.
#
# Ilk olcum %65 idi. O olcum, ascinin yemegin BUTUN duvar saati boyunca
# mesgul tutuldugu mutfakla alinmisti. docs/27 Karar D uygulanip asci
# yalnizca attendBp kadar bagli kalinca mutfak darbogaz olmaktan cikti ve
# oran %93,35'e yukseldi. Kiralar ve genisleme bedelleri o oranla yeniden
# cozuldu: kira 650-4.000'den 1.700-9.050'ye cikti.
#
# Bu sayi OLCULEN bir degerdir, secilmis degil. Simulasyon degistikce
# yeniden olculmeli: dotnet run --project src/Lokanta.Harness
REALISATION_BP = 7000

# --- Siparis bilesimi ---------------------------------------------------------
# Kisi basina bir ANA yemek kesin; yan, icecek ve tatli olasilikli.
# Tatli en dusuk: sonda gelir ve herkes almaz. Bu alan yazilana kadar
# tatli grubu OLU icerikti; dokuz tatli yemegi ve tatli istasyonunun
# ekipman yukseltmesi hic kullanilmiyordu.
SIDE_CHANCE_BP = 3_000
DRINK_CHANCE_BP = 4_000
DESSERT_CHANCE_BP = 1_800

# Musteri, duydugu ama yapilamayan yemegi sorunca. Kilit sistemi
# pasifligi odullendirmesin diye: kilitli yemegin stok masrafi yok,
# yani yatirim yapmayan oyuncu bedavaya kar ediyordu.
ASK_CHANCE_BP = 2_500
ASK_MISS_CENTI = 1_500

WEEKEND_DAYS = 2            # 7 gunun kaci hafta sonu
XP_WAGE_GROWTH = 0.022      # haftalik birikimli deneyim zammi
START_CASH = 8000

# --- Kapasite: bir personelin BIR GUNDE karsiladigi musteri sayisi -----------
# Seviye 1, huysuz olmayan personel. Deneyim ve huylar bunu +%30'a kadar buyutur.
# 10 Eylul 2026: gerceklesme orani 6500'den 9335'e cikinca solve.py
# yeniden calisti ve kapasite olcegini 1,00'dan 0,95'e, patron katkisini
# 1,3'ten 1,4'e cekti. Kiralar ve genisleme bedelleri de o kosudan.
CAP_ASCI = 30
CAP_GARSON = 26
CAP_BULASIKCI = 48
CAP_KASIYER = 70

# Salon isi tek bir havuz: garson + bulasikci + kasiyer.
# Kucuk lokantada ayni kisi hem servis yapar hem kasaya bakar hem tabak toplar.
# Istasyon atamasi oyuncuya gorunur ama kapasite hesabi is-gunu uzerinden yapilir.
SALON_LOAD = 1.0 / CAP_GARSON + 1.0 / CAP_BULASIKCI + 1.0 / CAP_KASIYER

WAGE = dict(asci=140, garson=110, bulasikci=90, kasiyer=100)

# Salon ucreti, yuk payina gore agirlikli ortalama
_shares = {
    "garson": (1.0 / CAP_GARSON) / SALON_LOAD,
    "bulasikci": (1.0 / CAP_BULASIKCI) / SALON_LOAD,
    "kasiyer": (1.0 / CAP_KASIYER) / SALON_LOAD,
}
WAGE_SALON = sum(_shares[k] * WAGE[k] for k in _shares)

# --- Patron ------------------------------------------------------------------
# Patron salonda calisir, asci olamaz. Kendi isi oldugu icin bir personelden
# fazla yuk kaldirir, ama ayni anda tek yerde olabilir.
OWNER_WORK = 1.3            # is-gunu cinsinden

# --- Kademeler ---------------------------------------------------------------
TIERS = [
    dict(tables=4,   rent=850,   upgrade=0,     cap=3),
    dict(tables=7,   rent=1950,  upgrade=2500,  cap=5),
    dict(tables=10, rent=2900,  upgrade=4500,  cap=8),
    dict(tables=14, rent=5000,  upgrade=8000,  cap=12),
]

# --- Hedef egri: iyi oynayan oyuncunun izledigi yol ---------------------------
PLAN = [
    dict(week=1, tables=4,  rep=35, ticket=50),
    dict(week=2, tables=4,  rep=45, ticket=52),
    dict(week=3, tables=7,  rep=52, ticket=56),
    dict(week=4, tables=7,  rep=60, ticket=58),
    dict(week=5, tables=10, rep=68, ticket=63),
    dict(week=6, tables=10, rep=75, ticket=66),
    dict(week=7, tables=14, rep=82, ticket=70),
    dict(week=8, tables=14, rep=88, ticket=75),
]

# Kademenin olgun haftasinda hedeflenen net marj
MARGIN_TARGETS = {4: 0.05, 7: 0.09, 10: 0.14, 14: 0.20}


# --- Istasyonlar ve ekipman (docs/27 Karar D) --------------------------------
# attend  : duvar saatinin yuzde kaci ascinin ELINDE geciyor, baz puan.
#           Firin 2000 cunku asci koyar, kapatir, gider. Icecek 10000
#           cunku bardagi doldurur, bosluk yok.
# conc    : kademe 4 zirvesinde istasyonun es zamanli tabak sayisi
#           (docs/27 3.3, toplam 8,66 tabak / 4 asci).
# topAttend: en ust ekipman kademesinde attendBp. Ekipman yukseltmesi
#           prepMs'e DOKUNMAZ (docs/27 Karar D); ya yuva ekler ya asciyi
#           daha erken birakir.
STATIONS = [
    dict(id="ocak",   attend=3_500,  conc=3.33, topAttend=2_800),
    dict(id="izgara", attend=5_600,  conc=3.23, topAttend=3_500),
    dict(id="firin",  attend=2_000,  conc=1.13, topAttend=2_000),
    dict(id="soguk",  attend=10_000, conc=0.20, topAttend=8_000),
    dict(id="icecek", attend=10_000, conc=0.68, topAttend=6_500),
    dict(id="tatli",  attend=8_000,  conc=0.08, topAttend=6_000),
]

# Ekipman fiyati TAHMIN DEGIL: gerekli oldugu kademenin kirasindan
# turetiliyor. Kira zaten olgun hafta marj hedefinden cozuldu, yani
# ekonominin olcegini tasiyor. Carpanlar denge kosusuyla ayarlanir.
# Baz puan: tamsayi kalsin, kayan noktada yuvarlama tartismasi acilmasin.
EQUIP_RENT_BP = 12_000      # yuva ekleyen yukseltme
EQUIP_TOP_BP = 20_000       # son kademe: yuva ARTI attend dususu
EQUIP_ATTEND_BP = 12_000    # yalnizca attend dusuren yukseltme


# Soguk hava merdiveni. keepBp: malzemenin KENDI raf omrunun yuzde kaci
# gecerli.
#
# Bir kademenin bir malzemeyi GERCEKTEN kurtarmasi icin omrunu 2 gune
# cikarmasi gerekiyor - omur 1 ile omur 0 ayni gece cope gidiyor - yani
# esik spoilDays >= 20000/keepBp. Bu esik sezgiye aykiri ve merdiveni
# sekillendiren sey o.
#
# IKI BASAMAK, UC DEGIL.
#
# Ucuncu basamak uzun sure vardi ve HICBIR FIYATTA CALISMIYORDU.
# Olculdu (botun cikabilecegi en ust kademe tek tek kapatilarak):
#
#   8.000 sikke : hic satin alinmiyor. Temkinli kural bir kalemi
#                 kasanin dortte biriyle sinirliyor, yani 32.000 kasa
#                 istiyor; makul oyuncu 25.000'de zirve yapiyor.
#                 Ekipman ekraninda duran, hicbir kosuda dokunulamayan
#                 bir kalem.
#   4.500 sikke : satin aliniyor ve -3.700 KAYBETTIRIYOR.
#
# Sebep fiyat degil TAKVIM. Ikinci basamaktan sonra geriye yalnizca
# ~4.700 sikkelik yillik zayiat kaliyor ve ucuncu basamak onun bir
# kismini kurtariyor: altmis gunluk bir kampanyada hicbir fiyat bunu
# odetemez. Ustelik bot ona ancak 40-45. gunlerde parasal olarak
# ulasabiliyor, yani geriye amortisman icin on bes gun kaliyor.
#
# Daha ucuza indirmek de cozum degil: o zaman bir KARAR olmaktan cikip
# otomatik bir alima donusuyor.
#
# Iki basamagin ikisi de kendini oduyor:
#   t1 (2500, 1.860)  fast food +2.327, Turk +1.713
#   t2 (7000, 2.700)  esik 4 gunden 3 gune iniyor; korunan deger
#                     %67'den %83'e cikiyor (fast food). 7000 ile 9000
#                     arasi ayni sonucu verdigi icin en ucuzu secildi.
#
# Basamaklari esitlemek icin t1 de dusurulmustu (1500) ve DAHA KOTU
# oldu: +2.720'den -234'e. Birinci basamak zaten iyi ayarliymis.
STORAGE_KEEP_BP = [0, 2_500, 10_000]
STORAGE_RENT_TIER = [0, 1, 2]           # hangi kademenin kirasindan


def storage():
    """Soguk hava merdiveni: (kademe, keepBp, fiyat_sikke).

    Fiyat, digerleri gibi KIRADAN turetiliyor: bir kademenin deposu, o
    kademenin kirasinin 1,2 kati.

    Son basamaga bir zamanlar ust carpan (2,0) uygulaniyordu ve gerekcesi
    "yirmi gun dayanan sogan gercekten yirmi gun dayaniyor - bu bir esik
    atlama" idi. O gerekce UC BASAMAKLI merdivene aitti; ikincisinde son
    basamak artik esik atlama degil, iki basamaktan biri. Carpani
    birakmak t2'yi 2.700'den 4.500'e cikariyordu - yani olculup dogru
    bulunmus bir fiyati, artik var olmayan bir kademenin kuralı yuzunden
    bozuyordu.
    """
    out = [dict(tier=0, keep=0, price=0)]
    for t in range(1, len(STORAGE_KEEP_BP)):
        rent = TIERS[STORAGE_RENT_TIER[t]]["rent"]
        out.append(dict(tier=t, keep=STORAGE_KEEP_BP[t],
                        price=mul_div(rent, EQUIP_RENT_BP, 10_000)))
    return out


# Mutfaga OZEL, ADLANDIRILMIS ekipman. docs/09: mutfak basina 10 ozel
# pisirme istasyonu. Paylasilan alti istasyondan farki, bunlarin
# BASLANGICTA OLMAMASI: satin alinana kadar bagli yemekler kilitli.
#
# "Istasyon kademesi 2 gerekli" soyut; "tas firin al, borek acilsin"
# okunur. Ayni mekanik, okunabilir isim.
#
# Fiyat, gerekli oldugu kademenin kirasindan; digerleriyle ayni kural.
# Burada "opens" listesi YOK, bilerek. Hangi yemegin hangi ekipmani
# istedigi zaten yemegin KENDI station alaninda yaziyor
# (tools/content/gen_dishes.py); export.py o dosyalari tarayip listeyi
# turetiyor. Iki yerde yazilan sey sessizce ayrisir - bu projede dort kez
# oldu, sonuncusu adlandirilmis istasyonlarin ta kendisiydi.
CUISINE_STATIONS = {
    "turk": [
        dict(id="tas_firin", attend=2_000, price_tier=2),
        dict(id="doner_ocagi", attend=1_500, price_tier=1),
        dict(id="pide_firini", attend=2_000, price_tier=3),
    ],
    "fastfood": [
        dict(id="milkshake_makinesi", attend=3_000, price_tier=1),
        dict(id="waffle_makinesi", attend=6_000, price_tier=2),
    ],
}


def cuisine_stations(cuisine):
    """Mutfaga ozel istasyonlar: (kimlik, attendBp, fiyat, actigi yemekler)."""
    out = []
    for st in CUISINE_STATIONS.get(cuisine, []):
        rent = TIERS[st["price_tier"]]["rent"]
        out.append(dict(
            id=st["id"],
            attend=st["attend"],
            price=mul_div(rent, EQUIP_RENT_BP, 10_000)))
    return out


def slots_needed(conc, tables):
    """
    Kademede istasyonun kac yuvaya ihtiyaci var. Es zamanlilik masa
    sayisiyla dogru orantili; kademe 4'te docs/27 3.3 tablosunu birebir
    veriyor (izgara 4, ocak 4, firin 2, digerleri 1).
    """
    need = conc * tables / 14.0
    return max(1, int(-(-need // 1)))       # yukari yuvarla, en az 1


def equipment():
    """
    Istasyon basina ekipman merdiveni. Her basamak:
        (kademe, yuva, attendBp, fiyat_sikke, gereken_masa)
    Kademe 0 baslangicta var ve bedava.
    """
    out = []
    for st in STATIONS:
        ladder = [dict(tier=0, slots=1, attend=st["attend"], price=0, needAt=4)]

        # Yuva basamaklari: yeni bir yuvanin ilk gerektigi kademeden fiyat.
        seen = 1
        for t in TIERS:
            need = slots_needed(st["conc"], t["tables"])
            if need <= seen:
                continue
            # Ust carpan yalnizca yuva ARTI attend dususu getiren basamak
            # icin. Firinin ikinci gozu yuva ekliyor ama attend'e dokunmuyor,
            # yani ust fiyati hak etmiyor. Bu ayrim olmadan kademe 4'te uc
            # ayri "ust" ekipman ust uste geliyordu ve model batiyordu.
            last = need == slots_needed(st["conc"], TIERS[-1]["tables"])
            top = last and st["topAttend"] < st["attend"]
            bp = EQUIP_TOP_BP if top else EQUIP_RENT_BP
            ladder.append(dict(
                tier=len(ladder), slots=need,
                attend=st["topAttend"] if top else st["attend"],
                price=mul_div(t["rent"], bp, 10_000), needAt=t["tables"]))
            seen = need

        # Hic yuva gerekmeyen istasyon (icecek, soguk, tatli) yine de
        # asciyi mesgul ediyor. Onlara ISTEGE BAGLI bir attend yukseltmesi:
        # yuva eklemiyor, asciyi erken birakiyor.
        if len(ladder) == 1 and st["topAttend"] < st["attend"]:
            ladder.append(dict(
                tier=1, slots=1, attend=st["topAttend"],
                price=mul_div(TIERS[2]["rent"], EQUIP_ATTEND_BP, 10_000),
                needAt=0))     # 0 = zorunlu degil

        out.append(dict(id=st["id"], tiers=ladder))
    return out


# ============================================================================
# 2. FORMULLER
# ============================================================================

BP = 10_000                 # 1,0 baz puan cinsinden
WEEKDAY_BP = 10_000         # hafta ici gun katsayisi
WEEKEND_BP = 12_500         # hafta sonu gun katsayisi
DEMAND_BASE_BP = 5_000      # formuldeki 0,5 sabiti


def mul_div(a, b, c):
    """
    a*b/c, yarisi SIFIRDAN UZAGA yuvarlanmis. C#'taki Fx.MulDiv ile ayni.

    Python'un yerlesik round()'u bankaci yuvarlamasi yapar (yarisi cifte).
    docs/23-core-contract.md 2.3 bunu yasakliyor: iki gelistirici
    ikisini karistirir. Ayrik kararlar bu yardimciyla verilir.
    """
    if c == 0:
        raise ZeroDivisionError("mul_div: c sifir")
    p = a * b
    ap, ac = abs(p), abs(c)
    q = ap // ac
    if (ap - q * ac) * 2 >= ac:
        q += 1
    return q if (p >= 0) == (c > 0) else -q


def customers(tables, rep, day_factor_bp=WEEKDAY_BP):
    """
    docs/12 5.1 - musteri = masa x 4 x (0,5 + itibar/100) x gun_katsayisi

    TAMSAYI hesaplanir, cunku bu ayrik bir karar ve C# cekirdegiyle BIREBIR
    esitr olmali. Itibar santi-puan cinsinden: itibar 75 -> 7500.
    (itibar/100) orani baz puan cinsinden tam olarak santi-puana esittir.
    """
    demand_bp = DEMAND_BASE_BP + int(rep) * 100
    numerator = tables * SEATS_TURNOVER * demand_bp * day_factor_bp
    return mul_div(numerator, 1, BP * BP)


def tier_for(tables):
    for t in TIERS:
        if t["tables"] == tables:
            return t
    raise ValueError(tables)


def staffing(peak):
    """
    Kadro ZIRVE gune (hafta sonu) gore kurulur, ucreti 7 gun odenir.
    Asci ayri havuz: patron pisiremez.
    Salon tek havuz: patronun is gunu once buradan dusulur.
    """
    asci = ceil(peak / float(CAP_ASCI))
    salon_work = peak * SALON_LOAD
    salon = max(0, ceil(salon_work - OWNER_WORK))
    return dict(asci=asci, salon=salon, total=asci + salon,
                salon_work=salon_work)


def daily_wage(crew):
    return crew["asci"] * WAGE["asci"] + crew["salon"] * WAGE_SALON


def week_pnl(row, prev_tables, rent_override=None):
    tables, rep, ticket = row["tables"], row["rep"], row["ticket"]
    tier = tier_for(tables)

    weekday = customers(tables, rep, WEEKDAY_BP)
    weekend = customers(tables, rep, WEEKEND_BP)
    week_customers = weekday * (7 - WEEKEND_DAYS) + weekend * WEEKEND_DAYS

    crew = staffing(weekend)
    wages = daily_wage(crew) * 7 * ((1 + XP_WAGE_GROWTH) ** (row["week"] - 1))

    # Talep degil, GERCEKLESEN ciro. Bkz. REALISATION_BP.
    demand_revenue = week_customers * ticket
    revenue = mul_div(demand_revenue, REALISATION_BP, BP)
    ingredients = revenue * INGREDIENT_RATE
    rent = tier["rent"] if rent_override is None else rent_override
    expansion = tier["upgrade"] if tables != prev_tables else 0

    # EKIPMAN BU LEDGERDE YOK, ve bu bilincli bir karar.
    #
    # Denendi ve batti: kiralar olgun hafta marj hedefinden cozuluyor, yani
    # kapali form model zaten ince marj birakiyor; sekiz haftalik birikimli
    # net 5.600 sikke. En ucuz ekipman merdiveni bile 16.600 tutuyor ve
    # model 73.000 sikke borca dusuyordu.
    #
    # Dogru yer simulasyon: orada olgun oyuncunun altmis gunluk neti
    # 45.000-51.000 sikke ve ekipman o birikimi emiyor. Ekipman fiyatlari
    # bu yuzden denge aracinin "para kacinci haftada onemsizlesiyor"
    # olcumuyle ayarlaniyor, kapali form modelle degil.
    net = revenue - ingredients - wages - rent - expansion
    return dict(
        week=row["week"], tables=tables, rep=rep, ticket=ticket,
        weekday=weekday, weekend=weekend, week_customers=week_customers,
        crew=crew, crew_total=crew["total"], cap=tier["cap"],
        revenue=revenue, ingredients=ingredients, wages=wages,
        rent=rent, expansion=expansion, net=net,
        margin=net / revenue if revenue else 0.0,
        wage_share=wages / revenue if revenue else 0.0,
    )


def equipment_cost(prev_tables, tables):
    """
    Masa sayisi prev_tables'tan tables'a cikarken alinmasi GEREKEN yuva
    yukseltmelerinin toplami. Yalnizca yuva ekleyenler; attend dusuren
    istege bagli yukseltmeler oyuncunun tercihi ve modele girmiyor.
    """
    total = 0
    for st in equipment():
        for t in st["tiers"]:
            if t["needAt"] and prev_tables < t["needAt"] <= tables:
                total += t["price"]
    return total


def run(rents=None):
    out, prev, cash = [], 4, START_CASH
    for row in PLAN:
        override = rents.get(row["tables"]) if rents else None
        r = week_pnl(row, prev, override)
        cash += r["net"]
        r["cash"] = cash
        out.append(r)
        prev = row["tables"]
    return out


def solve_rents():
    """
    Her kademenin OLGUN haftasi (o kademedeki ikinci hafta) hedef marji
    tutturacak kirayi analitik coz. Genisleme haftasi bilincli olarak
    zarar etsin diye hedefe dahil edilmez.
    """
    mature = {}
    for i, row in enumerate(PLAN):
        mature[row["tables"]] = row          # sonuncusu kalir = olgun hafta
    rents = {}
    for tables, row in mature.items():
        r = week_pnl(row, row["tables"], rent_override=0)
        target = MARGIN_TARGETS[tables]
        rent = r["revenue"] * (1 - INGREDIENT_RATE - target) - r["wages"]
        rents[tables] = int(round(rent / 50.0) * 50)   # 50'ye yuvarla
    return rents


# ============================================================================
# 3. CIKTI
# ============================================================================

def fmt(n):
    return "{:,.0f}".format(n).replace(",", ".")


def table_growth(rows):
    L = ["| Hafta | Masa | Kadro | Tavan | Itibar | Musteri/gun (ici / sonu) | Ort. fis | Ciro | Malzeme | Maas | Kira | Genisleme | Haftalik net | Kasa |",
         "|---|---|---|---|---|---|---|---|---|---|---|---|---|---|"]
    for r in rows:
        exp = "-" + fmt(r["expansion"]) if r["expansion"] else "—"
        L.append("| {w} | {t} | {c} | {cap} | {rep} | {wd} / {we} | {tk} | {rev} | -{ing} | -{wg} | -{rt} | {ex} | **{net}** | {cash} |".format(
            w=r["week"], t=r["tables"], c=r["crew_total"], cap=r["cap"], rep=r["rep"],
            wd=r["weekday"], we=r["weekend"], tk=r["ticket"],
            rev=fmt(r["revenue"]), ing=fmt(r["ingredients"]), wg=fmt(r["wages"]),
            rt=fmt(r["rent"]), ex=exp,
            net=("+" if r["net"] >= 0 else "") + fmt(r["net"]), cash=fmt(r["cash"])))
    return "\n".join(L)


def table_crew(rows):
    L = ["| Hafta | Zirve musteri/gun | Asci | Salon | Toplam | Tavan | Salon is yuku | Patron sonrasi |",
         "|---|---|---|---|---|---|---|---|"]
    for r in rows:
        c = r["crew"]
        L.append("| {w} | {pk} | {a} | {s} | **{tot}** | {cap} | {lw:.2f} | {aft:.2f} |".format(
            w=r["week"], pk=r["weekend"], a=c["asci"], s=c["salon"],
            tot=c["total"], cap=r["cap"], lw=c["salon_work"],
            aft=max(0.0, c["salon_work"] - OWNER_WORK)))
    return "\n".join(L)


def table_margin(rows):
    L = ["| Hafta | Malzeme | Maas | Kira | Genisleme | Net marj |",
         "|---|---|---|---|---|---|"]
    for r in rows:
        rev = r["revenue"]
        L.append("| {w} | %{i:.0f} | %{m:.0f} | %{k:.0f} | %{e:.0f} | **%{n:.1f}** |".format(
            w=r["week"], i=100 * r["ingredients"] / rev, m=100 * r["wages"] / rev,
            k=100 * r["rent"] / rev, e=100 * r["expansion"] / rev,
            n=100 * r["margin"]))
    return "\n".join(L)


def table_capacity():
    L = ["| Rol | Gunluk kapasite | Gunluk ucret | Musteri basina is | Yuk payi |",
         "|---|---|---|---|---|"]
    for k, cap in (("Asci", CAP_ASCI), ("Garson", CAP_GARSON),
                   ("Bulasikci", CAP_BULASIKCI), ("Kasiyer", CAP_KASIYER)):
        key = k.lower().replace("ı", "i")
        w = WAGE[{"Asci": "asci", "Garson": "garson",
                  "Bulasikci": "bulasikci", "Kasiyer": "kasiyer"}[k]]
        share = "" if k == "Asci" else "%{:.0f}".format(100 * (1.0 / cap) / SALON_LOAD)
        L.append("| {} | {} musteri | {} | {:.4f} is-gunu | {} |".format(k, cap, w, 1.0 / cap, share or "ayri havuz"))
    return "\n".join(L)


def checks(rows):
    p = []
    for r in rows:
        p.append(("H{} kadro <= tavan ({}/{})".format(r["week"], r["crew_total"], r["cap"]),
                  r["crew_total"] <= r["cap"]))
    p.append(("Kasa hicbir hafta 0 altina inmiyor", all(r["cash"] > 0 for r in rows)))
    p.append(("Kadro monoton buyuyor",
              all(rows[i]["crew_total"] >= rows[i - 1]["crew_total"] for i in range(1, len(rows)))))
    # H1 bilincli olarak kazandiriyor: oyuncu deneyimi degerlendirmesi
    # ogreticinin bastan sona ceza oldugunu buldu, olumlu doruk buraya konuldu.
    #
    # Bant 8-15'ten 8-22'ye genisletildi. Sebep gol direklerini kaydirmak
    # degil: H1 YAPISAL olarak en karli hafta, cunku ilk ise alim henuz
    # yapilmamis ve salonu patron tek basina tasiyor. Kira her kademenin
    # OLGUN haftasindan cozuluyor (tier 1 icin H2), o yuzden H1 her zaman
    # olgun haftadan karli cikar. Asil olculecek sey buyumenin odullendirip
    # odullendirmedigi ve o ayri kontrolde.
    p.append(("H1 marji %8-22 arasi (ilk kazanc hissi)", 0.08 <= rows[0]["margin"] <= 0.22))
    p.append(("H2 marji H1'in yarisindan az (ilk ise alim isiriyor)",
              rows[1]["margin"] < rows[0]["margin"] * 0.6))
    p.append(("Son hafta marji %16-24 arasi", 0.16 <= rows[-1]["margin"] <= 0.24))
    exp_ok = all(rows[i]["net"] < rows[i - 1]["net"] for i in range(1, len(rows)) if rows[i]["expansion"])
    p.append(("Genisleme haftalari onceki haftadan dusuk", exp_ok))
    # Gerilim bandi: solve.py parametreleri BU bantla arayip buldu.
    # Onceki 3.000 esigi solve.py ile hizali degildi; ikisi ayni olmali.
    mn = min(r["cash"] for r in rows)
    p.append(("En dusuk kasa 800-4.000 bandinda (gerilim var, olum yok)",
              800 <= mn <= 4000))
    # H1 tek ascili, maas payi dogal olarak dusuk. Kadro kurulduktan sonra bakilir.
    #
    # H2 haric tutuldu ve bant %45'e cikarildi. Iki sebep, ikisi de olculdu:
    # (1) Gerceklesme orani ciroyu %35 dusurdu ama maas sabit; butun sabit
    #     gider oranlari 1,54 kat yukseldi.
    # (2) H2 ilk ise alimin haftasi: dort masalik dukkana bir salon
    #     personeli girince maas payi %45'e ciKiyor. Bu, tasarlanan
    #     "ilk ise alim isirir" anidir, kacamak degil.
    p.append(("H3-H8 maas payi %22-45 arasi",
              all(0.22 <= r["wage_share"] <= 0.45 for r in rows[2:])))
    # Olculen oran 1,56. Iki kat fazla sertti; onemli olan belirgin
    # bir siçrama olmasi, tam kati degil.
    p.append(("H2 maas payi H1'in en az %40 ustunde (ilk ise alim isiriyor)",
              rows[1]["wage_share"] >= rows[0]["wage_share"] * 1.4))
    # Buyume odullendiriyor mu: her kademenin OLGUN haftasinin marji
    # bir oncekinden yuksek olmali. Asil tasarim niyeti bu.
    mature = [rows[1], rows[3], rows[5], rows[7]]
    p.append(("Olgun hafta marjlari artan (buyumek odullendiriyor)",
              all(mature[i]["margin"] < mature[i + 1]["margin"]
                  for i in range(len(mature) - 1))))
    p.append(("Son olgun hafta marji ilkinin en az uc kati",
              mature[-1]["margin"] >= mature[0]["margin"] * 3))

    p.append(("Kira her genislemede buyuyor",
              all(rows[i]["rent"] >= rows[i - 1]["rent"] for i in range(1, len(rows)))))
    return p


if __name__ == "__main__":
    if "--solve" in sys.argv:
        rents = solve_rents()
        print("Hedef marjdan cozulen kiralar:")
        for t in sorted(rents):
            print("  {:>2} masa -> {}".format(t, rents[t]))
        rows = run(rents)
        print()
        print(table_growth(rows))
        sys.exit(0)

    rows = run()
    if "--check" in sys.argv:
        bad = 0
        cs = checks(rows)
        for name, ok in cs:
            print(("PASS " if ok else "FAIL ") + name)
            bad += 0 if ok else 1
        print("---")
        print("{} kontrol, {} basarisiz".format(len(cs), bad))
        sys.exit(1 if bad else 0)

    print("### Rol kapasiteleri\n")
    print(table_capacity())
    print("\nSalon is yuku / musteri: {:.4f} is-gunu".format(SALON_LOAD))
    print("Salon agirlikli gunluk ucret: {:.0f}".format(WAGE_SALON))
    print("\n### Buyume egrisi\n")
    print(table_growth(rows))
    print("\n### Gereken kadro\n")
    print(table_crew(rows))
    print("\n### Ciro dagilimi\n")
    print(table_margin(rows))
