# -*- coding: utf-8 -*-
"""
Lokanta zaman modeli - Parti B
============================================================================
Amac: docs/27-zaman-modeli.md icindeki her sureyi formulden turetmek ve
ic tutarliligi tek komutla dogrulamak. Elle yazilan ms yok.

Neden var: uc dosya birbiriyle celisiyordu.
  1. docs/23 1.3   -> servis gunu 480.000 ms
  2. docs/14       -> asci kapasitesi 28 musteri/gun
  3. content/dishes/fastfood.json -> hamburger prepMs 75.000

  480.000 / 28 = 17.143 ms/musteri asci mesaisi. Tek bir hamburger 75.000 ms
  tutuyorsa bir asci gunde 6 hamburger yapar, 28 musteri degil. Uretilen
  prepMs degerleri ile kapasite modeli yaklasik 6 kat ayriydi.

Cozum iki parcali:
  - Gorev sureleri kapasiteden TURETILIR (bolum 2 asagida).
  - prepMs "asci mesgul" degil "duvar saati" olarak tanimlanir; ascinin
    mesgul suresi prepMs x attendBp'dir (bolum 4, es zamanlilik).

Calistirma:
    python timing.py            -> markdown tablolar
    python timing.py --check    -> tutarlilik testleri (PLAIN ASCII rapor)
"""
import sys

# ============================================================================
# 1. TABAN SABITLER
# ============================================================================

TICK_MS = 100                    # docs/23 1.2, sabit adim
SERVICE_TICKS = 4800             # KARAR A: degismedi
SERVICE_DAY_MS = SERVICE_TICKS * TICK_MS          # 480.000
PREP_TICKS = 1200                # sabah tezgah + aksam hesap, docs/23 7.3
DAY_TICKS = SERVICE_TICKS + PREP_TICKS
CAMPAIGN_DAYS = 60               # content/economy.json
TOUCH_BUDGET = 60                # docs/16, gunluk dokunus tavani

# --- Kapasiteler: tools/balance/model.py ile ayni, tek dogruluk kaynagi orasi
CAP_ASCI = 28
CAP_GARSON = 25
CAP_BULASIKCI = 46
CAP_KASIYER = 66
SALON_LOAD = 1.0 / CAP_GARSON + 1.0 / CAP_BULASIKCI + 1.0 / CAP_KASIYER

# --- Zirve gun: model.py hafta 8 hafta sonu satiri
PEAK_CUSTOMERS = 97
PEAK_COOKS = 4
PEAK_SALON = 7
OWNER_WORK = 1.4                 # patron salon is-gunu katkisi
PEAK_TABLES = 14

# --- content/archetypes/*.json uzerinden olculen degerler (fast food havuzu)
#     Olcum: agirlik x ortalama grup buyuklugu ile agirliklandirilmis.
GROUP_SIZE = 2.1195              # tum arketipler, kisi/grup
GROUP_SIZE_SEATED = 2.1847       # kurye haric (kurye masa isgal etmez)
TAKEAWAY_HEAD_SHARE = 0.0259     # kurye kafa payi
PATIENCE_MIN_MS = 8000           # kurye
PATIENCE_MAX_MS = 30000          # aile (fast food havuzunda)

# --- Menu olcumu: content/dishes/fastfood.json
#     Siparis karisimi 1 ana + 0,6 yan + 0,5 icecek + 0,1 tatli = 2,2 kalem.
#     Dogrulama: 2,2 kalem x grup ortalama fiyatlari = 74,2 sikke,
#     model.py hafta 8 hedef fisi 75 sikke.
ORDER_MIX = [
    # (grup,     adet/kisi, menu ortalama karmasiklik, ortalama fiyat santi-sikke)
    ("ana",      1.0, 1.6667, 4516.7),
    ("yan",      0.6, 1.1250, 2618.8),
    ("icecek",   0.5, 1.0000, 1883.3),
    ("tatli",    0.1, 2.1667, 3925.0),
]
DISHES_PER_CUSTOMER = sum(m[1] for m in ORDER_MIX)              # 2.2
COMPLEXITY_UNITS = sum(m[1] * m[2] for m in ORDER_MIX)          # 3.0584
TICKET_TARGET = 7500             # santi-sikke, model.py hafta 8

# --- Geliss dilimleri: docs/12 5.6
SLOT_COUNT = 4
SLOT_SHARE_CONTENT = [0.111, 0.375, 0.162, 0.353]   # olculen, fast food
SLOT_SHARE_TURK = [0.094, 0.606, 0.175, 0.125]      # olculen, turk
SLOT_SHARE_PROPOSED = [0.20, 0.30, 0.20, 0.30]      # KARAR F

# --- Sabir dagilimi: content/archetypes (fast food havuzu), kafa agirlikli
PATIENCE_HEADS = [
    (8000, 550.0), (9000, 210.0), (10000, 1950.0), (13000, 800.0),
    (14000, 495.0), (15000, 390.0), (16000, 3300.0), (17000, 1350.0),
    (18000, 3600.0), (19000, 440.0), (20000, 1300.0), (21000, 570.0),
    (22000, 100.0), (24000, 1700.0), (25000, 300.0), (26000, 165.0),
    (27000, 1050.0), (28000, 525.0), (30000, 2400.0),
]
PATIENCE_HEAD_TOTAL = sum(h for _, h in PATIENCE_HEADS)   # 21195

# --- Celiskinin kaydi: content/dishes/fastfood.json, 10 Eylul 2026 sabahi.
#     Bu dosyanin yazilma sebebi bu iki sayi. Icerik analiz sirasinda
#     duzenlendi; asagidaki degerler DEGISMEZ, celiskinin kaydi olarak duruyor.
CONFLICT_AVG_PREP_MS = 92656      # 32 yemegin ortalamasi
CONFLICT_BURGER_PREP_MS = 75000   # hamburger

# --- Icerigin analiz bitiminde geldigi ara durum (yine turetilmis degil):
#     prepMs yalnizca karmasikliga baglanmis, istasyon dusurulmus.
#     fast food 8000 / 11500 / 17500, turk 6000 / 8500 / 12500.
#     Siparis karisimiyla musteri basi 20.688 ms; asci mesaisi sayilirsa
#     480.000 / 20.688 = 23,2 musteri/gun, kapasite modeli 28 diyor. %17 sapma.
INTERIM_KITCHEN_MS = 20688

# --- Istasyon basina gunluk tabak sayisi (menunun istasyon dagilimindan)
#     ana 12 yemek: izgara 10 (4 k=1, 6 k=2), ocak 2 (k=2)
#     yan  8 yemek: ocak 6 (5 k=1, 1 k=2), soguk 2 (k=1)
#     icecek 6 (k=1), tatli 6: tatli 2 (k=1, k=2), firin 4 (2 k=2, 2 k=3)
STATION_DISH_MIX = [
    # (istasyon, kalem/musteri, ortalama prepMs anahtar listesi)
    ("izgara", 1.0 * 10 / 12, [("izgara", 1)] * 4 + [("izgara", 2)] * 6),
    ("ocak", 1.0 * 2 / 12, [("ocak", 2)] * 2),
    ("ocak", 0.6 * 6 / 8, [("ocak", 1)] * 5 + [("ocak", 2)]),
    ("soguk", 0.6 * 2 / 8, [("soguk", 1)] * 2),
    ("icecek", 0.5, [("icecek", 1)] * 6),
    ("tatli", 0.1 * 2 / 6, [("tatli", 1), ("tatli", 2)]),
    ("firin", 0.1 * 4 / 6, [("firin", 2)] * 2 + [("firin", 3)] * 2),
]


# ============================================================================
# 2. GOREV SURELERI - kapasiteden turetiliyor
# ============================================================================
# Kural: bir rolun bir musteri icin harcadigi toplam ms = gun / kapasite.
# Boylece "gunde 25 musteri" cumlesi ile "musteri basina 19.200 ms" cumlesi
# ayni cumle olur.

def role_ms(capacity):
    """Bir rolun musteri basina toplam mesaisi, ms. Yariyi sifirdan uzaga."""
    return (2 * SERVICE_DAY_MS + capacity) // (2 * capacity)


KITCHEN_MS = role_ms(CAP_ASCI)        # 17.143
GARSON_MS = role_ms(CAP_GARSON)       # 19.200 (tam bolunuyor)
BULASIKCI_MS = role_ms(CAP_BULASIKCI)  # 10.435
KASIYER_MS = role_ms(CAP_KASIYER)     # 7.273
SALON_MS = GARSON_MS + BULASIKCI_MS + KASIYER_MS   # 36.908

# --- Garson mesaisinin uc parcaya bolunmesi (kisi basi, toplam = GARSON_MS)
T_SEAT = 3200      # karsilama ve oturtma
T_ORDER = 7000     # siparis alma; en uzun garson isi, menu karari burada
T_SERVE = 9000     # tepsiyi masaya tasima ve dagitma
# --- Bulasikci mesaisinin iki parcasi (toplam = BULASIKCI_MS)
T_BUS = 4435       # masayi toplama - MASAYI BLOKE EDER
T_WASH = 6000      # evyede yikama - masa serbest
# --- Kasiyer
T_PAY = KASIYER_MS  # 7.273, hesabi kesme ve tahsilat

# --- Personel harcamayan sureler
T_EAT_PARTY = 38000   # bir masanin yeme suresi, mutfak parametresi
                      # (fast food kisa; italyan mutfaginda uzayacak)

# Masayi bloke eden kisi basi personel suresi = salon mesaisi eksi yikama
TABLE_STAFF_MS = SALON_MS - T_WASH   # 30.908


# ============================================================================
# 3. prepMs TURETIMI
# ============================================================================
# Asci havuzunu tuketen sey prepMs degil, ascinin O YEMEK ICIN mesgul oldugu
# suredir. Bu iki sayi ayni degil: firindaki kek pisserken asci baska is yapar.
#
#   cookBusyMs(karmasiklik) = UNIT_BUSY_MS x karmasiklik
#   prepMs(istasyon, karmasiklik) = cookBusyMs x 10000 / attendBp(istasyon)
#
# UNIT_BUSY_MS su denklemden cozuldu:
#   DISHES_PER_CUSTOMER agirlikli karmasiklik toplami x UNIT_BUSY = KITCHEN_MS
#   3,0584 x UNIT_BUSY = 17.143  ->  UNIT_BUSY = 5.605
# 5.600 secildi: butun istasyonlarda prepMs tamsayi cikiyor, sapma %0,09.

UNIT_BUSY_MS = 5600

# attendBp: duvar saatinin yuzde kaci ascinin ELINDE geciyor.
# Bu sayi istasyonun fiziksel dogasi; ekipman kademesi bunu dusurebilir.
ATTEND_BP = {
    "icecek": 10000,   # bardagi doldurur, bosluk yok
    "soguk": 10000,    # salata dograma, tamamen elde
    "tatli": 8000,     # tabak susleme
    "izgara": 5600,    # koymak, cevirmek, almak; arada bosluk var
    "ocak": 3500,      # sepeti daldirir, birakir
    "firin": 2000,     # koyar, kapatir, gider
}
STATION_ORDER = ["icecek", "soguk", "tatli", "izgara", "ocak", "firin"]


def cook_busy_ms(complexity):
    """Ascinin bir yemek icin mesgul oldugu sure. Havuzu tuketen sayi budur."""
    return UNIT_BUSY_MS * complexity


def prep_ms(station, complexity):
    """Yemegin duvar saati suresi. JSON'a yazilan prepMs bu."""
    return cook_busy_ms(complexity) * 10000 // ATTEND_BP[station]


def kitchen_ms_achieved():
    """Turetilen prepMs'lerden geri hesaplanan musteri basi asci mesaisi."""
    return UNIT_BUSY_MS * COMPLEXITY_UNITS


def menu_avg_prep_ms():
    """32 yemegin turetilmis prepMs ortalamasi. Icerikle karsilastirmak icin."""
    all_dishes = [v for _, _, variants in STATION_DISH_MIX for v in variants]
    return sum(prep_ms(s, c) for s, c in all_dishes) / float(len(all_dishes))


# Referans siparisin duvar saati bekleme suresi: bir masanin yemegi birlikte
# cikar, yani bekleme = siparisteki EN YAVAS birinci kus yemek.
# Tipik siparis: ana yemek izgarada karmasiklik 2 -> 20.000 ms; yan yemek
# ocakta karmasiklik 1 -> 16.000 ms; icecek 5.600 ms. En yavasi ana yemek.
COOK_WAIT_REF_MS = prep_ms("izgara", 2)   # 20.000


# ============================================================================
# 4. MASA ISGALI
# ============================================================================

def table_ms_per_party(group=GROUP_SIZE_SEATED, cook_wait=COOK_WAIT_REF_MS,
                       eat=T_EAT_PARTY):
    """
    Bir masa devri: oturtma + siparis + yemek beklemesi + servis + yeme +
    odeme + toplama. Yikama masayi bloke etmez, disarida.
    """
    return int(round(group * TABLE_STAFF_MS + cook_wait + eat))


TABLE_TURN_MS = table_ms_per_party()


# ============================================================================
# 5. HAVUZ DOLULUKLARI
# ============================================================================

def utilisation(customers=PEAK_CUSTOMERS, cooks=PEAK_COOKS,
                salon=PEAK_SALON, tables=PEAK_TABLES, day_ms=SERVICE_DAY_MS):
    """Zirve gunde her havuzun gun ortalamasi dolulugu."""
    kitchen_need = customers * KITCHEN_MS
    kitchen_have = cooks * day_ms
    salon_need = customers * SALON_MS
    salon_have = (salon + OWNER_WORK) * day_ms
    seated = customers * (1.0 - TAKEAWAY_HEAD_SHARE)
    parties = seated / GROUP_SIZE_SEATED
    table_need = parties * TABLE_TURN_MS
    table_have = tables * day_ms
    return dict(
        kitchen=kitchen_need / kitchen_have,
        salon=salon_need / salon_have,
        table=table_need / table_have,
        parties=parties,
        kitchen_need=kitchen_need, kitchen_have=kitchen_have,
        salon_need=salon_need, salon_have=salon_have,
        table_need=table_need, table_have=table_have,
    )


def slot_queue(shares, customers=PEAK_CUSTOMERS, cooks=PEAK_COOKS,
               salon=PEAK_SALON, tables=PEAK_TABLES):
    """
    Dilim bazinda akiskan kuyruk. Bir dilim gunun 1/4'u; kapasitenin de 1/4'u.
    Dilim payi 0,25'ten buyukse o dilimde is birikir; birikim dilim sonunda
    en son gelen musterinin bos bekleme suresidir.
    """
    u = utilisation(customers, cooks, salon, tables)
    slot_ms = SERVICE_DAY_MS // SLOT_COUNT
    out = []
    for i, w in enumerate(shares):
        k_need = w * u["kitchen_need"]
        k_have = cooks * slot_ms
        s_need = w * u["salon_need"]
        s_have = (salon + OWNER_WORK) * slot_ms
        t_need = w * u["table_need"]
        t_have = tables * slot_ms
        k_wait = max(0.0, k_need - k_have) / cooks
        s_wait = max(0.0, s_need - s_have) / (salon + OWNER_WORK)
        t_wait = max(0.0, t_need - t_have) / tables
        out.append(dict(
            slot=i + 1, share=w,
            kitchen=k_need / k_have, salon=s_need / s_have, table=t_need / t_have,
            wait_max=k_wait + s_wait + t_wait,
            wait_avg=(k_wait + s_wait + t_wait) / 2.0,
            wait_takeaway=s_wait / 2.0,   # kurye masa ve mutfak kuyruguna girmez
        ))
    return out


def max_slot_share():
    """Hicbir havuzun dilim icinde %100'u asmamasi icin azami dilim payi."""
    u = utilisation()
    return 0.25 / max(u["kitchen"], u["salon"], u["table"])


# ============================================================================
# 6. SABIR
# ============================================================================
# KARAR E: sabir sayaci sadece BOS BEKLEMEDE isler. Pisirme suresi tam
# sayilmaz, COOK_PATIENCE_BP agirligiyla sayilir.
#
#   bekleme_puani = bos_bekleme_ms + pisirme_ms x COOK_PATIENCE_BP / 10000
#
# Bos bekleme = musteriyle kimsenin ilgilenmedigi ve yemeginin de baslamadigi
# sure: masa kuyrugu, garson kuyrugu, mutfak kuyrugu, pass'te bekleyen tabak.

COOK_PATIENCE_BP = 2500


def patience_spent(idle_ms, cook_ms=COOK_WAIT_REF_MS):
    return idle_ms + cook_ms * COOK_PATIENCE_BP / 10000.0


def zero_load_wall_clock(group=1, station="izgara", complexity=1):
    """Sifir yukte kapidan ilk lokmaya kadar gecen DUVAR SAATI suresi."""
    return (T_SEAT + T_ORDER + prep_ms(station, complexity) + T_SERVE) * 1


def zero_load_takeaway(station="izgara", complexity=1):
    """Kurye: masa yok, oturma yok, yeme yok."""
    return T_ORDER + prep_ms(station, complexity) + T_SERVE


def lost_head_share(idle_ms, idle_takeaway_ms=None, cook_ms=COOK_WAIT_REF_MS):
    """
    Verilen bos beklemede sabri tukenip cikip giden musterilerin kafa payi.

    Kurye (PATIENCE_MIN_MS basamagi) masa ve mutfak kuyruguna girmiyor,
    yalnizca salon kuyruguna giriyor; ayri bekleme suresiyle degerlendirilir.
    """
    if idle_takeaway_ms is None:
        idle_takeaway_ms = idle_ms
    spent = patience_spent(idle_ms, cook_ms)
    spent_ta = patience_spent(idle_takeaway_ms, prep_ms("izgara", 1))
    lost = 0.0
    for pat, h in PATIENCE_HEADS:
        if pat == PATIENCE_MIN_MS:
            if pat <= spent_ta:
                lost += h
        elif pat <= spent:
            lost += h
    return lost / PATIENCE_HEAD_TOTAL


def daily_loss_share(shares=SLOT_SHARE_PROPOSED):
    """Gun genelinde kaybedilen musteri payi: her dilimin kendi kuyruguyla."""
    tot = 0.0
    for r in slot_queue(shares):
        tot += r["share"] * lost_head_share(r["wait_avg"], r["wait_takeaway"])
    return tot


# ============================================================================
# 6b. ISTASYON YUVALARI
# ============================================================================
# Es zamanlilik kabul edildigi icin bir istasyonda ayni anda kac tabak
# durabilecegi ayri bir kisit. Ekipman kademesi bu sayiyi buyutur.

def station_slots(shares=SLOT_SHARE_PROPOSED, customers=PEAK_CUSTOMERS):
    peak_share = max(shares)
    slot_ms = SERVICE_DAY_MS // SLOT_COUNT
    need = {}
    for station, per_customer, variants in STATION_DISH_MIX:
        dishes = customers * per_customer * peak_share
        avg_prep = sum(prep_ms(s, c) for s, c in variants) / float(len(variants))
        need[station] = need.get(station, 0.0) + dishes * avg_prep / slot_ms
    return need


# ============================================================================
# 7. KAMPANYA SURESI
# ============================================================================

def campaign_hours(speed=1):
    return CAMPAIGN_DAYS * DAY_TICKS * TICK_MS / 1000.0 / 3600.0 / speed


# ============================================================================
# 8. CIKTI
# ============================================================================

def table_tasks():
    rows = [
        ("Masa bekleme", "-", "yok", 0, "Yalnizca kuyrukta; sabir burada yanar"),
        ("Oturtma", "kisi", "salon/garson", T_SEAT, "Masayi bloke eder"),
        ("Siparis alma", "kisi", "salon/garson", T_ORDER, "Masayi bloke eder"),
        ("Pisirme (bekleme)", "masa", "mutfak/asci", COOK_WAIT_REF_MS,
         "Duvar saati; en yavas birinci kus yemek"),
        ("Servis", "kisi", "salon/garson", T_SERVE, "Masayi bloke eder"),
        ("Yeme", "masa", "yok", T_EAT_PARTY, "Masayi bloke eder"),
        ("Odeme", "kisi", "salon/kasiyer", T_PAY, "Masayi bloke eder"),
        ("Masayi toplama", "kisi", "salon/bulasikci", T_BUS, "Masayi bloke eder"),
        ("Bulasik", "kisi", "salon/bulasikci", T_WASH, "Masa serbest"),
    ]
    L = ["| Gorev | Birim | Havuz | ms | Not |", "|---|---|---|---|---|"]
    for r in rows:
        L.append("| {} | {} | {} | {} | {} |".format(r[0], r[1], r[2], r[3], r[4]))
    return "\n".join(L)


def table_prep():
    L = ["| Istasyon | attendBp | k=1 | k=2 | k=3 |", "|---|---|---|---|---|"]
    for s in STATION_ORDER:
        L.append("| {} | {} | {} | {} | {} |".format(
            s, ATTEND_BP[s], prep_ms(s, 1), prep_ms(s, 2), prep_ms(s, 3)))
    return "\n".join(L)


def table_util():
    u = utilisation()
    L = ["| Havuz | Gereken ms | Var olan ms | Doluluk |", "|---|---|---|---|"]
    L.append("| Mutfak (4 asci) | {:.0f} | {:.0f} | %{:.1f} |".format(
        u["kitchen_need"], u["kitchen_have"], 100 * u["kitchen"]))
    L.append("| Salon (7 + patron 1,4) | {:.0f} | {:.0f} | %{:.1f} |".format(
        u["salon_need"], u["salon_have"], 100 * u["salon"]))
    L.append("| Masa (14) | {:.0f} | {:.0f} | %{:.1f} |".format(
        u["table_need"], u["table_have"], 100 * u["table"]))
    return "\n".join(L)


def table_slots(shares, label):
    L = ["| Dilim | Pay | Mutfak | Salon | Masa | Azami bos bekleme ms |",
         "|---|---|---|---|---|---|"]
    for r in slot_queue(shares):
        L.append("| {} | %{:.1f} | %{:.1f} | %{:.1f} | %{:.1f} | {:.0f} |".format(
            r["slot"], 100 * r["share"], 100 * r["kitchen"], 100 * r["salon"],
            100 * r["table"], r["wait_max"]))
    return label + "\n\n" + "\n".join(L)


# ============================================================================
# 9. TUTARLILIK TESTLERI
# ============================================================================

def checks():
    p = []
    D = SERVICE_DAY_MS

    # --- A: gun uzunlugu
    p.append(("A1 servis gunu 4800 tick = 480.000 ms", D == 480000))
    p.append(("A2 kampanya 1x hizda tam 10 saat",
              abs(campaign_hours(1) - 10.0) < 1e-9))
    p.append(("A3 dokunus araligi 6-12 s bandinda",
              6000 <= D / TOUCH_BUDGET <= 12000))

    # --- B: gorev sureleri kapasiteyi birebir uretiyor
    p.append(("B1 garson ms x 25 = servis gunu (TAM)", GARSON_MS * 25 == D))
    p.append(("B2 mutfak ms x 28 = servis gunu (+-28)",
              abs(KITCHEN_MS * CAP_ASCI - D) <= CAP_ASCI))
    p.append(("B3 bulasikci ms x 46 = servis gunu (+-46)",
              abs(BULASIKCI_MS * CAP_BULASIKCI - D) <= CAP_BULASIKCI))
    p.append(("B4 kasiyer ms x 66 = servis gunu (+-66)",
              abs(KASIYER_MS * CAP_KASIYER - D) <= CAP_KASIYER))
    p.append(("B5 oturtma+siparis+servis = garson ms",
              T_SEAT + T_ORDER + T_SERVE == GARSON_MS))
    p.append(("B6 toplama+bulasik = bulasikci ms", T_BUS + T_WASH == BULASIKCI_MS))
    p.append(("B7 odeme = kasiyer ms", T_PAY == KASIYER_MS))
    p.append(("B8 salon toplami = uc rolun toplami",
              SALON_MS == GARSON_MS + BULASIKCI_MS + KASIYER_MS))
    p.append(("B9 salon toplami = gun x SALON_LOAD",
              abs(SALON_MS - D * SALON_LOAD) <= 1.0))
    p.append(("B10 masayi bloke eden sure = salon eksi bulasik",
              TABLE_STAFF_MS == SALON_MS - T_WASH))
    for cap, ms, nm in ((CAP_ASCI, KITCHEN_MS, "asci"), (CAP_GARSON, GARSON_MS, "garson"),
                        (CAP_BULASIKCI, BULASIKCI_MS, "bulasikci"),
                        (CAP_KASIYER, KASIYER_MS, "kasiyer")):
        p.append(("B11 gun/{}_ms geri {} kapasitesini veriyor".format(nm, nm),
                  int(round(D / float(ms))) == cap))

    # --- C: prepMs turetimi
    p.append(("C1 kalem/musteri = 2,2", abs(DISHES_PER_CUSTOMER - 2.2) < 1e-9))
    p.append(("C2 turetilen asci mesaisi kapasiteyle +-50 ms icinde",
              abs(kitchen_ms_achieved() - KITCHEN_MS) <= 50))
    p.append(("C3 butun prepMs degerleri tamsayi ve pozitif",
              all(prep_ms(s, c) * ATTEND_BP[s] == cook_busy_ms(c) * 10000
                  for s in STATION_ORDER for c in (1, 2, 3))))
    p.append(("C4 prepMs karmasikliga dogrusal",
              all(prep_ms(s, 2) == 2 * prep_ms(s, 1) and
                  prep_ms(s, 3) == 3 * prep_ms(s, 1) for s in STATION_ORDER)))
    ticket = sum(m[1] * m[3] for m in ORDER_MIX)
    p.append(("C5 2,2 kalem x menu fiyatlari hedef fise %10 icinde",
              abs(ticket - TICKET_TARGET) / TICKET_TARGET < 0.10))
    p.append(("C6 celiski kaydi: eski icerik ortalamasi turetilenin 4 katiydi",
              CONFLICT_AVG_PREP_MS / menu_avg_prep_ms() > 4.0))
    p.append(("C7 celiski kaydi: eski hamburger degeriyle bir asci gunde "
              "7'den az hamburger yapardi",
              D / float(CONFLICT_BURGER_PREP_MS) < 7))
    p.append(("C8 ara durum icerigi de kapasiteyi tutturmuyor (%10 ustu sapma)",
              abs(INTERIM_KITCHEN_MS - KITCHEN_MS) / float(KITCHEN_MS) > 0.10))

    # --- D: es zamanlilik
    p.append(("D1 her istasyonda prepMs >= cookBusy (mesgul <= duvar saati)",
              all(prep_ms(s, c) >= cook_busy_ms(c)
                  for s in STATION_ORDER for c in (1, 2, 3))))
    p.append(("D2 es zamanlilik olmasa ortalama prepMs 8 s altina duserdi",
              KITCHEN_MS / DISHES_PER_CUSTOMER < 8000))
    slots = station_slots()
    p.append(("D3 hicbir istasyon zirvede 4 yuvadan fazla istemiyor",
              max(slots.values()) <= 4.0))
    p.append(("D4 es zamanli tabak sayisi asci sayisindan buyuk "
              "(es zamanliligin isi gorunur karsiligi)",
              sum(slots.values()) > PEAK_COOKS))

    # --- E: sabir
    p.append(("E1 sifir yukte bos bekleme sifir", patience_spent(0, 0) == 0))
    fastest = PATIENCE_MIN_MS
    p.append(("E2 en sabirsiz arketip sifir yukte hayatta (kurye, izgara k=1)",
              patience_spent(0, prep_ms("izgara", 1)) < fastest))
    p.append(("E3 sifir yukte TOPLAM duvar saati beklemesi sabiri asiyor "
              "(eski tanimin kirildiginin ispati)",
              zero_load_takeaway("izgara", 1) > fastest))
    peak = slot_queue(SLOT_SHARE_PROPOSED)[1]
    p.append(("E4 onerilen profilde zirve ortalamasi 10 s sabri gecmiyor",
              patience_spent(peak["wait_avg"], prep_ms("izgara", 1)) < 10000))
    p.append(("E5 icerik profilinde zirve ortalamasi 30 s sabri BILE asiyor",
              patience_spent(slot_queue(SLOT_SHARE_CONTENT)[1]["wait_avg"],
                             COOK_WAIT_REF_MS) > PATIENCE_MAX_MS))
    p.append(("E6 kurye zirvede ortalamada hayatta (masa ve mutfak kuyruguna "
              "girmiyor)",
              patience_spent(peak["wait_takeaway"], prep_ms("izgara", 1))
              < PATIENCE_MIN_MS))
    p.append(("E7 onerilen profilde gunluk musteri kaybi %10 altinda",
              daily_loss_share(SLOT_SHARE_PROPOSED) < 0.10))
    p.append(("E8 icerik profilinde gunluk kayip %50 ustunde (kirik)",
              daily_loss_share(SLOT_SHARE_CONTENT) > 0.50))

    # --- F: zirve fizibilitesi
    u = utilisation()
    for k in ("kitchen", "salon", "table"):
        p.append(("F1 {} gun ortalamasi %100 altinda".format(k), u[k] < 1.0))
    p.append(("F2 masa devri 120-130 s bandinda (docs/23 1.3 ile uyumlu)",
              120000 <= TABLE_TURN_MS <= 130000))
    p.append(("F3 azami dilim payi %28 civarinda",
              0.27 <= max_slot_share() <= 0.29))
    p.append(("F4 icerik profili (%37,5) fizibil DEGIL",
              max(slot_queue(SLOT_SHARE_CONTENT)[1][k]
                  for k in ("kitchen", "salon", "table")) > 1.20))
    p.append(("F5 onerilen profil (%30) her havuzda %110 altinda",
              max(slot_queue(SLOT_SHARE_PROPOSED)[1][k]
                  for k in ("kitchen", "salon", "table")) < 1.10))
    p.append(("F6 turk profili (%60,6) hicbir kadro ile fizibil degil",
              0.606 / 0.25 * u["salon"] > 2.0))
    wd = slot_queue(SLOT_SHARE_PROPOSED, customers=77)
    p.append(("F7 hafta ici (77 musteri) hicbir dilimde kuyruk yok",
              all(r["wait_max"] == 0 for r in wd)))
    p.append(("F8 kayip yalnizca hafta sonu; haftalik ciro etkisi %4 altinda",
              daily_loss_share(SLOT_SHARE_PROPOSED) * 2 * 97 / (5 * 77 + 2 * 97)
              < 0.04))
    return p


def report():
    D = SERVICE_DAY_MS
    u = utilisation()
    print("=" * 74)
    print("LOKANTA ZAMAN MODELI - turetilmis sabitler")
    print("=" * 74)
    print("Servis gunu      : {} tick = {} ms = {:.1f} dk".format(
        SERVICE_TICKS, D, D / 60000.0))
    print("Gun toplami      : {} tick = {:.1f} dk (servis + sabah/aksam)".format(
        DAY_TICKS, DAY_TICKS * TICK_MS / 60000.0))
    print("Kampanya         : {} gun = {:.2f} saat (1x), {:.2f} saat (2x)".format(
        CAMPAIGN_DAYS, campaign_hours(1), campaign_hours(2)))
    print("Dokunus araligi  : {:.0f} ms (60 dokunus / gun)".format(D / TOUCH_BUDGET))
    print()
    print("-- Rol mesaileri (musteri basi) --------------------------------------")
    for nm, cap, ms in (("asci", CAP_ASCI, KITCHEN_MS), ("garson", CAP_GARSON, GARSON_MS),
                        ("bulasikci", CAP_BULASIKCI, BULASIKCI_MS),
                        ("kasiyer", CAP_KASIYER, KASIYER_MS)):
        print("  {:<10} kapasite {:>3}  ->  {:>6} ms   ({} x {} = {}, gun {})".format(
            nm, cap, ms, ms, cap, ms * cap, D))
    print("  {:<10} {:>13}  ->  {:>6} ms".format("SALON", "toplam", SALON_MS))
    print("  salon yuku bp    : {:.6f} is-gunu/musteri".format(SALON_LOAD))
    print()
    print("-- Gorev sureleri (ms) -----------------------------------------------")
    print("  oturtma {:>6} | siparis {:>6} | servis {:>6}   = garson {}".format(
        T_SEAT, T_ORDER, T_SERVE, GARSON_MS))
    print("  toplama {:>6} | bulasik {:>6}                  = bulasikci {}".format(
        T_BUS, T_WASH, BULASIKCI_MS))
    print("  odeme   {:>6}                                  = kasiyer {}".format(
        T_PAY, KASIYER_MS))
    print("  yeme    {:>6} (masa basi) | pisirme beklemesi {:>6} (masa basi)".format(
        T_EAT_PARTY, COOK_WAIT_REF_MS))
    print("  masa devri: {:.4f} x {} + {} + {} = {} ms".format(
        GROUP_SIZE_SEATED, TABLE_STAFF_MS, COOK_WAIT_REF_MS, T_EAT_PARTY,
        TABLE_TURN_MS))
    print()
    print("-- prepMs (duvar saati, ms) ------------------------------------------")
    print("  UNIT_BUSY = {} ms x karmasiklik = ascinin mesgul suresi".format(
        UNIT_BUSY_MS))
    print("  {:<8} {:>8} {:>8} {:>8} {:>8}".format("istasyon", "attendBp", "k=1", "k=2", "k=3"))
    for s in STATION_ORDER:
        print("  {:<8} {:>8} {:>8} {:>8} {:>8}".format(
            s, ATTEND_BP[s], prep_ms(s, 1), prep_ms(s, 2), prep_ms(s, 3)))
    print("  turetilen asci mesaisi/musteri: {:.0f} ms, hedef {} ms, sapma {:.2f}%".format(
        kitchen_ms_achieved(), KITCHEN_MS,
        100 * abs(kitchen_ms_achieved() - KITCHEN_MS) / KITCHEN_MS))
    print()
    print("-- Celiskinin kaydi (10 Eylul 2026 sabahi) ---------------------------")
    print("  o gunku icerik ortalama prepMs               : {} ms".format(
        CONFLICT_AVG_PREP_MS))
    print("  turetilen 32 yemek ortalamasi                : {:.0f} ms".format(
        menu_avg_prep_ms()))
    print("  oran                                        : {:.2f} kat".format(
        CONFLICT_AVG_PREP_MS / menu_avg_prep_ms()))
    print("  hamburger: {} ms -> turetilen {} ms ({:.1f} kat)".format(
        CONFLICT_BURGER_PREP_MS, prep_ms("izgara", 1),
        CONFLICT_BURGER_PREP_MS / float(prep_ms("izgara", 1))))
    print("  o deger asci mesaisi sayilsa: bir asci gunde {:.1f} hamburger"
          .format(D / float(CONFLICT_BURGER_PREP_MS)))
    print("  ARA DURUM (istasyonsuz, karmasiklik-tabanli icerik):")
    print("    musteri basi asci mesaisi {} ms, hedef {} ms, sapma %{:.1f}".format(
        INTERIM_KITCHEN_MS, KITCHEN_MS,
        100.0 * (INTERIM_KITCHEN_MS - KITCHEN_MS) / KITCHEN_MS))
    print("    istasyon boyutu dusmus; Karar D icin yeniden uretim sart")
    print()
    print("-- Zirvede gereken istasyon yuvasi -----------------------------------")
    for s in STATION_ORDER:
        v = station_slots().get(s, 0.0)
        print("  {:<8} {:.2f} es zamanli tabak -> {} yuva".format(
            s, v, max(1, int(v) + (1 if v > int(v) else 0))))
    print("  toplam es zamanli tabak: {:.2f} ({} asci ile)".format(
        sum(station_slots().values()), PEAK_COOKS))
    print()
    print("-- Zirve gun doluluklari (97 musteri, 4 asci, 7 salon, 14 masa) ------")
    print("  mutfak : %{:.1f}   ({:.0f} / {:.0f} ms)".format(
        100 * u["kitchen"], u["kitchen_need"], u["kitchen_have"]))
    print("  salon  : %{:.1f}   ({:.0f} / {:.0f} ms)".format(
        100 * u["salon"], u["salon_need"], u["salon_have"]))
    print("  masa   : %{:.1f}   ({:.0f} / {:.0f} ms, {:.1f} devir)".format(
        100 * u["table"], u["table_need"], u["table_have"], u["parties"]))
    print("  azami dilim payi: %{:.1f}".format(100 * max_slot_share()))
    print()
    print("-- Dilim kuyruklari --------------------------------------------------")
    for label, shares in (("icerik  ", SLOT_SHARE_CONTENT),
                          ("onerilen", SLOT_SHARE_PROPOSED),
                          ("turk    ", SLOT_SHARE_TURK)):
        r = slot_queue(shares)[1]
        print("  {} dilim2 pay %{:.1f} -> mutfak %{:.0f} salon %{:.0f} masa %{:.0f}"
              "  bos bekleme ort {:.0f} ms".format(
                  label, 100 * r["share"], 100 * r["kitchen"], 100 * r["salon"],
                  100 * r["table"], r["wait_avg"]))
    print()
    print("-- Sabir --------------------------------------------------------------")
    print("  sifir yuk, oturan musteri, izgara k=1 duvar saati : {} ms".format(
        zero_load_wall_clock()))
    print("  sifir yuk, kurye (masasiz) duvar saati            : {} ms".format(
        zero_load_takeaway()))
    print("  sifir yuk harcanan sabir (bos=0)                  : {:.0f} ms".format(
        patience_spent(0, prep_ms("izgara", 1))))
    pk = slot_queue(SLOT_SHARE_PROPOSED)[1]
    print("  zirve ortalama harcanan sabir (onerilen profil)   : {:.0f} ms".format(
        patience_spent(pk["wait_avg"], COOK_WAIT_REF_MS)))
    print("  zirve azami harcanan sabir (onerilen profil)      : {:.0f} ms".format(
        patience_spent(pk["wait_max"], COOK_WAIT_REF_MS)))
    print("  en sabirsiz arketip (kurye)                       : {} ms".format(
        PATIENCE_MIN_MS))
    print("  zirve, kurye bos beklemesi (yalniz salon kuyrugu) : {:.0f} ms".format(
        pk["wait_takeaway"]))
    print("  zirve, kurye harcanan sabir                       : {:.0f} ms".format(
        patience_spent(pk["wait_takeaway"], prep_ms("izgara", 1))))
    print("  zirve dilimde kaybedilen kafa payi (onerilen)     : %{:.1f}".format(
        100 * lost_head_share(pk["wait_avg"], pk["wait_takeaway"])))
    print("  GUNLUK kayip, onerilen profil                     : %{:.1f}".format(
        100 * daily_loss_share(SLOT_SHARE_PROPOSED)))
    print("  GUNLUK kayip, icerik profili                      : %{:.1f}".format(
        100 * daily_loss_share(SLOT_SHARE_CONTENT)))



# ============================================================================
# 9. ZIRVE KARARI - docs/28-zirve-karari.md
# ============================================================================
# Bu bolum EKLEMEDIR. Yukaridaki hicbir sabit veya karar degismedi.
#
# Iki yeni sey getiriyor:
#   1. Dilim sureleri esit olmak zorunda degil (KARAR G). Bir dilim gunun
#      d_i payini kaplar; sum(d_i) = 1. Kapasite de o oranda dagilir.
#      Fizibiliteyi belirleyen sey dilim PAYI degil, YOGUNLUK: w_i / d_i.
#   2. Kuyruk KAPALI DONGU. Bolum 5'teki slot_queue acik dongudur: kimse
#      cikmaz varsayar, birikim sinirsiz buyur. Gercekte sabri tukenen
#      musteri gider ve arkasindakinin beklemesini KISALTIR. Asagidaki
#      akiskan model bu geri beslemeyi tasiyor.

# Turk arketip havuzunun sabir dagilimi (content/archetypes/shared+turk).
# Fast food havuzu icin PATIENCE_HEADS yukarida.
PATIENCE_HEADS_TURK = [
    (8000, 550.0), (11000, 450.0), (12000, 2500.0), (13000, 800.0),
    (15000, 390.0), (16000, 1220.0), (19000, 440.0), (20000, 4450.0),
    (22000, 1800.0), (23000, 760.0), (26000, 165.0), (29000, 1800.0),
    (30000, 2530.0), (31000, 180.0), (34000, 1120.0), (36000, 1530.0),
    (40000, 600.0),
]
PATIENCE_HEAD_TOTAL_TURK = sum(h for _, h in PATIENCE_HEADS_TURK)   # 21285
GROUP_SIZE_SEATED_TURK = 2.1942   # content/archetypes olcumu, kurye haric

# Olculen geliss paylari (docs/27 5.2 ile ayni sayilar, tam ondalikla)
SLOT_SHARE_CONTENT_FF = [0.1105, 0.3749, 0.1620, 0.3526]
SLOT_SHARE_CONTENT_TR = [0.0937, 0.6063, 0.1753, 0.1247]

# KARAR G: dilim sureleri, mutfaga ozel. Toplam 1,0; tick'e tam boluniyor.
SLOT_DUR_EQUAL = [0.25, 0.25, 0.25, 0.25]
SLOT_DUR_FF = [0.20, 0.30, 0.20, 0.30]     # 960/1440/960/1440 tick
SLOT_DUR_TR = [0.12, 0.48, 0.25, 0.15]     # 576/2304/1200/720 tick


def slot_ticks(durs):
    """Dilim surelerinin tick karsiligi. Tamsayi olmayan varsa hata."""
    out = []
    for d in durs:
        t = d * SERVICE_TICKS
        if abs(t - round(t)) > 1e-9:
            raise ValueError("dilim suresi tick'e bolunmuyor: %r" % (d,))
        out.append(int(round(t)))
    return out


def pool_needs(customers, cooks, salon, tables, group_seated=GROUP_SIZE_SEATED,
               table_turn=None):
    """Uc havuzun gun boyu gereken ms'si ve tuketebildigi kapasite hizi."""
    tt = TABLE_TURN_MS if table_turn is None else table_turn
    parties = customers * (1.0 - TAKEAWAY_HEAD_SHARE) / group_seated
    return [
        dict(name="mutfak", need=customers * KITCHEN_MS, rate=float(cooks),
             takeaway=False),
        dict(name="salon", need=customers * SALON_MS, rate=salon + OWNER_WORK,
             takeaway=True),
        dict(name="masa", need=parties * tt, rate=float(tables),
             takeaway=False),
    ]


def retain_share(idle_ms, idle_takeaway_ms, heads=None, total=None,
                 cook_ms=COOK_WAIT_REF_MS):
    """Verilen bos beklemede KALAN musterilerin kafa payi. lost_head_share'in
    tersi, ama arketip havuzunu (fast food / turk) disaridan alabiliyor."""
    if heads is None:
        heads, total = PATIENCE_HEADS, PATIENCE_HEAD_TOTAL
    spent = patience_spent(idle_ms, cook_ms)
    spent_ta = patience_spent(idle_takeaway_ms, prep_ms("izgara", 1))
    keep = 0.0
    for pat, h in heads:
        if pat == PATIENCE_MIN_MS:
            if pat > spent_ta:
                keep += h
        elif pat > spent:
            keep += h
    return keep / total


def flow_day(shares, durs, customers=PEAK_CUSTOMERS, cooks=PEAK_COOKS,
             salon=PEAK_SALON, tables=PEAK_TABLES, heads=None, total=None,
             group_seated=GROUP_SIZE_SEATED, table_turn=None,
             cook_ms=COOK_WAIT_REF_MS, steps=4000):
    """
    Akiskan kuyruk, balking dahil, dilimler arasi tasma dahil.

    Her havuz p icin birikim B_p (ms-is). Yeni gelenin bos beklemesi
    W = sum(B_p / rate_p). Sabri W'yi kaldirmayanlar KUYRUGA GIRMEZ:

        dB_p/dt = kalan(W) x gelis_hizi_p - rate_p      (B_p >= 0)

    Denge, gelis hizinin kapasiteye dustugu noktada kuruluyor; birikim
    marjinal arketipin sabrinda doyuyor. slot_queue (bolum 5) bu terimi
    tasimadigi icin ayni profilde birikimi sinirsiz buyutur.
    """
    if heads is None:
        heads, total = PATIENCE_HEADS, PATIENCE_HEAD_TOTAL
    if abs(sum(durs) - 1.0) > 1e-9:
        raise ValueError("dilim sureleri 1,0 etmiyor")
    P = pool_needs(customers, cooks, salon, tables, group_seated, table_turn)
    B = [0.0] * len(P)
    rows = []
    arrived = lost = 0.0
    for i, (w, d) in enumerate(zip(shares, durs)):
        L = d * SERVICE_DAY_MS
        dt = L / float(steps)
        lam = [w * p["need"] / L for p in P]
        a_i = l_i = wsum = wmax = 0.0
        for _ in range(steps):
            W = sum(B[k] / P[k]["rate"] for k in range(len(P)))
            Wta = sum(B[k] / P[k]["rate"] for k in range(len(P))
                      if P[k]["takeaway"])
            r = retain_share(W, Wta, heads, total, cook_ms)
            for k in range(len(P)):
                B[k] = max(0.0, B[k] + (r * lam[k] - P[k]["rate"]) * dt)
            n = w * customers * dt / L
            a_i += n
            l_i += n * (1.0 - r)
            wsum += W * dt
            if W > wmax:
                wmax = W
        arrived += a_i
        lost += l_i
        rows.append(dict(
            slot=i + 1, share=w, dur=d, density=w / d,
            arrivals=a_i, lost=l_i, loss=(l_i / a_i if a_i else 0.0),
            idle_avg=wsum / L, idle_max=wmax,
            util=[w * p["need"] / (p["rate"] * L) for p in P],
        ))
    return dict(rows=rows, arrivals=arrived, lost=lost,
                daily_loss=(lost / arrived if arrived else 0.0),
                residual_idle=sum(B[k] / P[k]["rate"] for k in range(len(P))))


def max_density():
    """Sifir kuyruk icin azami yogunluk (pay / sure). Bolum 5.3'un genellemesi:
    esit dilimde azami pay = 0,25 x max_density()."""
    u = utilisation()
    return 1.0 / max(u["kitchen"], u["salon"], u["table"])


def peak_report():
    """docs/28-zirve-karari.md icindeki butun sayilar."""
    print("YOGUNLUK TAVANI")
    u = utilisation()
    print("  zirve gun doluluklari mutfak/salon/masa     : %{:.1f} / %{:.1f} / %{:.1f}".format(
        100 * u["kitchen"], 100 * u["salon"], 100 * u["table"]))
    print("  sifir kuyruk icin azami yogunluk (pay/sure) : {:.4f}".format(
        max_density()))
    print("  esit dilimde karsiligi                      : %{:.2f}".format(
        25 * max_density()))
    print()
    cases = [
        ("FF icerik  , esit dilim ", SLOT_SHARE_CONTENT_FF, SLOT_DUR_EQUAL,
         PATIENCE_HEADS, PATIENCE_HEAD_TOTAL, GROUP_SIZE_SEATED),
        ("TR icerik  , esit dilim ", SLOT_SHARE_CONTENT_TR, SLOT_DUR_EQUAL,
         PATIENCE_HEADS_TURK, PATIENCE_HEAD_TOTAL_TURK, GROUP_SIZE_SEATED_TURK),
        ("FF Karar F , esit dilim ", SLOT_SHARE_PROPOSED, SLOT_DUR_EQUAL,
         PATIENCE_HEADS, PATIENCE_HEAD_TOTAL, GROUP_SIZE_SEATED),
        ("FF KARAR G , 20/30/20/30", SLOT_SHARE_CONTENT_FF, SLOT_DUR_FF,
         PATIENCE_HEADS, PATIENCE_HEAD_TOTAL, GROUP_SIZE_SEATED),
        ("TR KARAR G , 12/48/25/15", SLOT_SHARE_CONTENT_TR, SLOT_DUR_TR,
         PATIENCE_HEADS_TURK, PATIENCE_HEAD_TOTAL_TURK, GROUP_SIZE_SEATED_TURK),
    ]
    for tag, w, d, hh, tt, gs in cases:
        r = flow_day(w, d, heads=hh, total=tt, group_seated=gs)
        print(tag + "   tick " + "/".join(str(x) for x in slot_ticks(d)))
        print("   dilim   pay   sure  yogun  mutfak  salon   masa  "
              "ort bekl   azami   kayip")
        for x in r["rows"]:
            print("   {:5d} {:5.1f}% {:5.1f}% {:6.3f} {:6.1f}% {:6.1f}% "
                  "{:6.1f}% {:9.0f} {:7.0f} {:6.1f}%".format(
                      x["slot"], 100 * x["share"], 100 * x["dur"], x["density"],
                      100 * x["util"][0], 100 * x["util"][1], 100 * x["util"][2],
                      x["idle_avg"], x["idle_max"], 100 * x["loss"]))
        print("   GUNLUK KAYIP %{:.2f}   gun sonu kalan kuyruk {:.0f} ms".format(
            100 * r["daily_loss"], r["residual_idle"]))
        print()

if __name__ == "__main__":
    if "--check" in sys.argv:
        cs = checks()
        bad = 0
        for name, ok in cs:
            print(("PASS " if ok else "FAIL ") + name)
            bad += 0 if ok else 1
        print("---")
        print("{} kontrol, {} basarisiz".format(len(cs), bad))
        sys.exit(1 if bad else 0)

    if "--zirve" in sys.argv:
        peak_report()
        sys.exit(0)

    if "--md" in sys.argv:
        print("### Gorevler\n")
        print(table_tasks())
        print("\n### prepMs\n")
        print(table_prep())
        print("\n### Zirve doluluk\n")
        print(table_util())
        print()
        print(table_slots(SLOT_SHARE_CONTENT, "### Icerik profili"))
        print()
        print(table_slots(SLOT_SHARE_PROPOSED, "### Onerilen profil"))
        sys.exit(0)

    report()
