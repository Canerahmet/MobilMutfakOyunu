# -*- coding: utf-8 -*-
"""
Icerik ve altin veri yazicisi - Faz 0
============================================================================
model.py'deki parametreleri iki yere yazar:

  content/economy.json        oyunun okudugu sabitler, TAMSAYI birimlerle
  content/staff-roles.json    rol kapasiteleri ve ucretler
  tests/golden/weekly.json    sekiz haftalik tablo, TAM hassasiyetle

Ucuncusu C# testinin karsilastirdigi altin veri. Boylece Python modeli ile
C# cekirdegi ayni kaynagi paylasir ve ayrisirlarsa test kirilir.

Birimler docs/23-cekirdek-sozlesmesi.md 2.2'ye gore:
  para        santi-sikke  (1 sikke = 100)
  oran        baz puan     (10000 = 1,0)
  itibar      santi-puan   (0..10000)
  sure        milisaniye
  is gucu     mikro-is-gunu (1e-6)

Calistirma:  python export.py
"""
import io
import json
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import model  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
CONTENT = os.path.join(ROOT, "content")
GOLDEN = os.path.join(ROOT, "tests", "golden")

COIN = 100          # 1 sikke = 100 santi-sikke
BP = 10_000         # 1,0 = 10000 baz puan
MICRO = 1_000_000   # 1 is-gunu = 1.000.000 mikro-is-gunu


def rnd(x):
    """Yarisi sifirdan uzaga yuvarla. Python'un banker's rounding'i kullanilmaz."""
    return int(x + 0.5) if x >= 0 else -int(-x + 0.5)


def ensure(d):
    if not os.path.isdir(d):
        os.makedirs(d)


# ---------------------------------------------------------------------------
# content/staff-roles.json
# ---------------------------------------------------------------------------
def staff_roles():
    rows = [
        ("asci",      "kitchen", model.CAP_ASCI,      model.WAGE["asci"],      ["ocak", "izgara", "firin"]),
        ("garson",    "salon",   model.CAP_GARSON,    model.WAGE["garson"],    ["salon"]),
        ("bulasikci", "salon",   model.CAP_BULASIKCI, model.WAGE["bulasikci"], ["bulasik"]),
        ("kasiyer",   "salon",   model.CAP_KASIYER,   model.WAGE["kasiyer"],   ["kasa"]),
    ]
    out = []
    for rid, pool, cap, wage, stations in rows:
        out.append({
            "id": rid,
            "nameKey": "role." + rid,
            "pool": pool,
            "capacityPerDay": cap,
            # Bir musterinin bu role yukledigi is, mikro-is-gunu cinsinden.
            # Cekirdek bunu kapasiteden turetebilir; burada yazmak testin
            # iki tarafinin da ayni yuvarlamayi kullanmasini garanti ediyor.
            "workPerCustomerMicro": rnd(MICRO / float(cap)),
            "dailyWage": wage * COIN,
            "stations": stations,
            "xpSpeedBp": [BP, 11000, 12000, 13000],
        })
    return out


# ---------------------------------------------------------------------------
# content/economy.json
# ---------------------------------------------------------------------------
def economy():
    # Itibar TAVANI kademeye bagli.
    #
    # Olculdu: 176 kosunun %85'i itibari 0-30 ya da 90-100 bandinda
    # bitiriyordu; ortada yalnizca %15. Esik kimsenin gormedigi bir sayi
    # (ortalama memnuniyet ~62) ve makul oyuncu onu 21-25. gunde asip
    # kalan 35 gunu tavanda geciriyordu. O andan sonra cay, patron
    # ilgisi, kaliteli malzeme, huy secimi ve iki imza mekanigi de
    # MATEMATIKSEL OLARAK gorunmez oluyor: odul doymus bir eksene
    # odeniyor.
    #
    # Tavani kademeye baglamak iki sorunu birden cozuyor. Itibar
    # doydugunda tek cikis yol BUYUMEK oluyor (plato kalkiyor), ve tavan
    # altindaki bolgede memnuniyet calismasi yeniden olculebilir
    # kaliyor. docs/08'in "genisleme ilerlemenin kendisi" fikri.
    REP_CAP = [5500, 7500, 9000, 10000]

    tiers = []
    for i, t in enumerate(model.TIERS):
        tiers.append({
            "tables": t["tables"],
            "rent": t["rent"] * COIN,
            "upgrade": t["upgrade"] * COIN,
            "staffCap": t["cap"],
            "reputationCapCenti": REP_CAP[i] if i < len(REP_CAP) else 10000,
            # TABAK SAYISI: masa basina alti.
            #
            # Tabak sayili ve doniyor (temiz -> kullanimda -> kirli ->
            # temiz). Temiz bitince asci pisen yemegi cikaramiyor ve
            # servis duruyor - docs/14'un bulasikci darbogazi.
            #
            # DORT: bir masa dolusu. OLCULEREK secildi, tahminle degil.
            #
            # Ilk deneme masa basina ALTI idi ve darbogaz HIC isirmadi:
            # on dort gun boyunca tabaksiz bekleme sifir tick, en az temiz
            # 28/42. Sebebi fiziksel - ayni anda kullanilan tabak sayisini
            # MASA SAYISI sinirliyor (masa basina en fazla dort kisi), yani
            # tabak masa x 4'un ustundeyse tanim geregi hic bitmez.
            #
            # Dort de isirmadi (yine sifir tick): masa basina en fazla
            # dort kisi oturuyor ama ORTALAMA grup uc kisilik, yani dort
            # kat tabak yine de fizigin ustunde kaliyor.
            #
            # UC KAT ARTI SEKIZ. Iki parcanin ikisi de olculerek geldi.
            #
            # "Uc kat" tek basina denendi ve OTOMATIK TUR birinci gunde
            # lokantayi kilitledi: dort masaya on iki tabak, dort kisilik
            # iki grup stogu tuketiyor, ucuncu grup tabak bekliyor, sabri
            # bitiyor, gun kizgin musterilerle kapaniyor. Denge araci bunu
            # gizledi cunku botlari hizla buyuyor ve ortalamalar birinci
            # kademeyi yutuyor.
            #
            # Sabit tampon YIKAMA BORUSUNUN payi ve masa sayisiyla
            # buyumuyor: her an bir kismi kirlide, bir kismi lavaboda.
            # O tampon sabit bir maliyet; masayla olceklemek birinci
            # kademeyi tamponsuz birakiyordu.
            #
            # KATSAYI KUCUK, TAMPON BUYUK (2x + 12) ve bu bilincli:
            # darbogaz KUCUK dukkanda degil BUYUK dukkanda olmali.
            # Dort masali bir lokantanin tabak krizi yok; on dort masali
            # bir lokantanin var, cunku tabak masayla dogrusal buyurken
            # servis hizi masa x devir ile buyuyor. Taban 20 (birinci
            # kademe rahat), tavan 40 (dorduncu kademede gercekten dar).
            #
            # Basinc SUREDEN degil SAYIDAN geliyor. Yikama suresini
            # buyutmek bulasikci payini IKI KEZ saymak olurdu: ClearMs
            # (masa toplama) zaten o payi tasiyor (6.000 / 19.200 = %31,
            # docs/14'te %28).
            "plates": t["tables"] * 2 + 6,
        })

    # Deneyim zammi haftalik birikimli. Kampanya 60 gun = 9 hafta siniri.
    weeks = 10
    mult = []
    for w in range(weeks):
        mult.append(rnd((1.0 + model.XP_WAGE_GROWTH) ** w * BP))

    return {
        "schemaVersion": 1,
        "_comment": "URETILEN DOSYA. Elle degistirmeyin; tools/balance/export.py calistirin.",

        "startingCash": model.START_CASH * COIN,
        "startingReputationCenti": 3000,
        "campaignDays": 60,
        "seasonDays": 15,
        "rentDayInterval": 7,
        "weekendDaysPerWeek": model.WEEKEND_DAYS,

        "reputationDecayPerDayCenti": 30,
        "satisfactionNeutralCenti": 6000,

        "customerBasePerTable": model.SEATS_TURNOVER,
        "weekdayMultiplierBp": model.WEEKDAY_BP,
        "weekendMultiplierBp": model.WEEKEND_BP,

        "ingredientRateBp": rnd(model.INGREDIENT_RATE * BP),
        # docs/23 1.3 ve docs/27: servis gunu 4.800 tick = 480.000 ms.
        # Burada 120.000 yaziyordu; docs/27 gun uzunlugunu degistirdiginde
        # icerik guncellenmemis ve kodla dort kat ayrisik kalmisti.
        # Kod dogruydu, icerik eskiydi. tools/audit_content.py buldu.
        "serviceMs": 480_000,
        "interventionsPerDay": 4,

        "loanMultiplierBp": 13500,
        "loanWeeks": 8,
        "loanOptions": [5000 * COIN, 10000 * COIN, 20000 * COIN],

        # Siparis modeli. Kisi basina bir ANA yemek kesin, yan ve icecek
        # olasilikli. Ortalama tabak = 1 + yan + icecek.
        # docs/07 kombo mekaniginin taban hali; kombo bu oranlari buyutuyor.
        "order": {
            "sideChanceBp": model.SIDE_CHANCE_BP,
            "drinkChanceBp": model.DRINK_CHANCE_BP,
            "dessertChanceBp": model.DESSERT_CHANCE_BP,
            "askChanceBp": model.ASK_CHANCE_BP,
            "askMissCenti": model.ASK_MISS_CENTI,
            "kitchenMsPerPerson": 480_000 // 28,
        },

        # Simulasyondan olculen gerceklesme orani. model.py ile ayni.
        # Isimli duzenli musteriler. docs/11: arketip binlerce musteri
        # uretir, duzenli musteri tek bir kisidir. Sayilar burada
        # cunku mekanik kodda, sayilar veride (docs/23 8.2).
# Moral. docs/14 "Moral" tablosu ve esikleri.
"morale": {
    "starting": 70,
    "lowThreshold": 30,
    "quitThreshold": 15,
    "quitChanceBp": 1000,
    "slowPenaltyBp": 2000,
    "paidDelta": 5,
    "lateDelta": -25,
    "busyDelta": -3,
    # docs/14 moral tablosu bir OLAY listesi, surukleniş modeli degil.
    # Yalnizca olaylari uygulayinca merdiven tek yonlu asagi gidiyor ve
    # iyi yonetilen bir dukkanda bile butun kadro bir ayda istifa ediyor -
    # olcum bunu yakaladi. Sakin bir gun gercekten toparlatir.
    "recoveryDelta": 2,
},

# Patron mudahalesi. docs/12 5.4.
#
# Uc sayi da bir zamanlar KODDA duruyordu - ustelik EconomyConfig'in
# WithMorale() kurucusunun icinde, yani adi bile yanlis bir yerde. Denge
# araci icerige dokunabiliyor, koda dokunamiyor; oradayken bu uc sayi
# calibrate.py icin YOK demekti ve tam da ayarlanmasi gerekenler onlardi.
"intervention": {
    # Patron bizzat ilgilendi: memnuniyet, santi.
    #
    # OLCULDU. Bu iki sayi (odul ve sabir kati) birlikte ayarlaniyor
    # cunku bir TAKASI ayarliyorlar. Uc varyant kosuldu, 16 tohum:
    #
    #   sabir x3, odul 1200  ff servis -56 itibar -1,6 kasa -1.897
    #                        tr servis +32 itibar +1,3 kasa   -120
    #   sabir x1, odul 3000  ff servis -33 itibar -0,4 kasa   -312
    #                        tr servis +102 itibar +5,2 kasa  +116
    #   sabir x2, odul 2400  ff servis -32 itibar -0,4 kasa   -392
    #                        tr servis +159 itibar +5,5 kasa +1.859  <-- secilen
    #
    # Sabir uzatmasi 3 kat iken mudahale eden oyuncu hic mudahale
    # etmeyenden DAHA AZ musteri agirliyordu, cunku uzatma masayi
    # isgal ediyor: kurtarilan grup, hizmet edilebilecek baskasinin
    # yerini aliyor. Odul memnuniyete kaydirilinca takas duzeldi -
    # memnuniyet ekseninde yer var (makul oyuncunun itibari 75, tavan
    # degil), sabir ekseninde yok.
    "attentionSatisfactionCenti": 2400,
    # Cay/ikram: memnuniyet, santi.
    "treatSatisfactionCenti": 900,
    # Acele ettirilen isin kalan suresinden silinen pay, baz puan.
    "rushCutBp": 4000,
    # Sabir uzatmasi, oturma-siparis suresinin kati olarak.
    #
    # Bu sayi bir TAKAS ayarliyor: uzatma, gitmek uzere olan grubu
    # tutuyor ama masayi da daha uzun isgal ediyor. Isgal edilen masa
    # baskasina hizmet edilememesi demek, yani uzatmak BEDAVA DEGIL.
    "attentionPatienceMult": 2,
    "treatPatienceMult": 1,
},
        "regulars": {
            # Tanistiktan sonra bir gunde ugrama sansi: haftada ~3 gun.
            "visitChanceBp": 4500,
            # Sevdigi yemegi menude bulamazsa memnuniyet cezasi.
            "missedFavouriteCenti": 900,
            # Bunun altinda ayrilirsa bir sure gelmiyor.
            "upsetCenti": 5000,
            "awayDays": 3,
        },
        "realisationBp": model.REALISATION_BP,

        "priceVolatilityBp": 2500,
        "underpriceFloorBp": 8500,
        # FIYATIN TALEBE DOGRUDAN ETKISI. 9000 = %10 zam -> ~%9 az
        # musteri. Kanal eklenmeden once fiyatin talebe HIC yolu
        # yoktu ve itibar tavanindaki oyuncu icin zam bedavaydi;
        # olculdu, %10 zamlayan bot her stratejiyi geciyordu.
        "priceElasticityBp": 9000,
        # GUNLUK TALEP OYNAKLIGI. 1000 = -%10 ile +%10.
        #
        # Talep tamamen belirlenimciydi: ayni itibar ve masadaki her sali
        # birebir ayni musteriyi getiriyordu, yani hal onerisi HER ZAMAN
        # tam dogruydu ve sabah stok karari bir yargi degil bir dugmeydi.
        # Sapma YALNIZCA gerceklesende; tahmin beklentiyi gosteriyor.
        "demandVarianceBp": 1000,
        # PATRON ILGILENINCE SIRADAKI SALON ISI YARIYA INIYOR.
        #
        # Mudahale olculdugunde NOTR cikti: kadrosu duzgun lokantada
        # kriz neredeyse hic olmuyor (gunde 0,8 mudahale), yani mekanik
        # bir emniyet agiydi - oysa magaza metni onu ana mekanik diye
        # satiyor. Eksik olan salon tarafiydi: ilgi sabri uzatip MUTFAGI
        # hizlandiriyordu ama darbogaz cogu zaman salonda.
        "attendWorkCutBp": 5000,
        # Fiyat TAVANI: memnuniyet cezasi sifirda doyuyor ve talep
        # fiyati hic gormuyor, yani tavan olmadan kar sinirsiz.
        # 25000 = piyasanin 2,5 kati.
        "overpriceCeilingBp": 25000,

        "staffing": {
            "ownerPool": "salon",
            "ownerWorkMicro": rnd(model.OWNER_WORK * MICRO),
            "weeklyXpWageGrowthBp": rnd(model.XP_WAGE_GROWTH * BP),
            "weeklyWageMultiplierBp": mult,
            "tiers": tiers,
        },
    }


# ---------------------------------------------------------------------------
# content/cuisines/*.json
# ---------------------------------------------------------------------------
# docs/28-zirve-karari.md Karar G: dilim PAYLARI degil dilim SURELERI
# mutfaga gore degisiyor. Turk lokantasinin musterilerinin %60'i ogle
# diliminde geliyor ve bu kimlik diregi; esit dilimde servis edilemiyordu.
# Ogle dilimi gunun %48'ini kaplayinca ayni pay fizibil oluyor.
#
# Gunluk kayip: fast food %3,15, Turk %5,08. Haftalik ciroya etkisi
# %1,06 ve %1,70. Dogrulama: python tools/balance/timing.py --zirve
#
# MENU ROLLERI. Yemek gruplari mutfaga ozel (docs/13): fast food'da
# ana/yan, Turk lokantasinda sulu/corba/pilav/izgara/meze. Cekirdek ise
# siparisi ana + yan + icecek diye kuruyor, yani hangi grubun hangi rolu
# oynadigini bilmek zorunda.
#
# Bu eslesme yazilana kadar simulasyon fast food sozlugunu sabit
# kodluyordu ve Turk mutfaginda HICBIR musteri ana yemek bulamiyordu:
# sekiz stratejinin hepsi sifir musteriyle batiyordu.
CUISINES = [
    ("fastfood", [2000, 3000, 2000, 3000], {
        "main":    ["ana"],
        "side":    ["yan"],
        "drink":   ["icecek"],
        "dessert": ["tatli"],
    }),
    ("turk", [1200, 4800, 2500, 1500], {
        "main":    ["sulu", "izgara"],
        "side":    ["corba", "pilav", "meze"],
        "drink":   ["icecek"],
        "dessert": ["tatli"],
    }),
]


def cuisines():
    out = []
    for cid, slots, roles in CUISINES:
        assert len(slots) == 4, cid
        assert sum(slots) == BP, cid + " dilim toplami " + str(sum(slots))
        ticks = 480_000 // 100
        for s in slots:
            assert (ticks * s) % BP == 0, cid + " dilim tick'e tam bolunmuyor"

        # Roller yemek dosyasindaki BUTUN gruplari kapsamali ve hicbir
        # grup iki role birden dusmemeli. Bu kontrol olmadan bir grup
        # sessizce siparis edilemez hale geliyor.
        path = os.path.join(CONTENT, "dishes", cid + ".json")
        if os.path.exists(path):
            dishes = json.load(io.open(path, encoding="utf-8"))
            have = set(d["group"] for d in dishes)
            mapped = []
            for k in ("main", "side", "drink", "dessert"):
                mapped.extend(roles[k])
            assert len(mapped) == len(set(mapped)), cid + " grup iki role birden dusuyor"
            missing = have - set(mapped)
            assert not missing, cid + " rolsuz grup: " + ", ".join(sorted(missing))
            extra = set(mapped) - have
            assert not extra, cid + " olmayan gruba rol verilmis: " + ", ".join(sorted(extra))
            assert any(d["group"] in roles["main"] and d.get("unlockDay", 0) <= 1
                       for d in dishes), cid + " ilk gun acik ana yemek yok"

        out.append((cid, {
            "id": cid,
            "nameKey": "cuisine." + cid,
            "_comment": "URETILEN DOSYA. tools/balance/export.py",
            "slotDurationsBp": slots,
            "eatMs": 38_000,
            "menuRoles": roles,
            "signature": SIGNATURE[cid],
            "scoreAxis": SCORE_AXIS[cid],
        }))
    return out


# Yil sonu degerlendirmesinin MUTFAGA OZEL ekseni (docs/08).
#
# Neden mutfak basina ayri bir eksen: yedi eksenin altisi her mutfakta
# ayni. Yalnizca bu sonuncusu imza mekanigini odullendiriyor, yani
# mutfaklar birbirinden sadece OYNANISTA degil SONUCTA da ayrisiyor.
#
# target: eksenin 100 puan verdigi deger.
#
# FAST FOOD: ana yemek siparislerinin yuzde kaci komboya dondu, bin-puan.
#
# Eksen bir sure `peakCovers` (gunun en yuksek kuveri) idi ve OLCULDU ki
# imzayi degil GENISLEMEYI izliyor: komboyu her sabah acan bot ile hic
# acmayan bot ayni puani aliyordu (38 / 38), en yuksek puanlar en cok
# masa acanlardaydi. Yani eksen "Mekan" ekseninin kopyasiydi ve docs/08
# onun icin "imza mekanigini DOGRUDAN odullendirir" diyordu.
#
# Hedef OLCUMDEN geliyor, uydurulmadi: komboyu her sabah acan bot
# ana yemeklerin **%17,4**'unu komboya cevirdi (12 tohum, 60 gun);
# acmayan herkeste %0. %15 tam puan veriyor, yani "cogu gun ac"
# yetiyor - kombo mutfak yukunu de artirdigi icin zirvede kapatmak
# mesru bir oyun ve eksen onu cezalandirmamali.
#
# TURK: veresiye tahsilat orani, baz puan olarak; %90 tam puan, cunku
# %100 ancak hic veresiye acmayarak tutturulur ve o da mekanigi hic
# kullanmamak demek.
# TURK: acilan veresiyenin yuzde kaci tahsil edildi, bin-puan.
#
# Hedef 9000 bir sure ANLAMSIZDI: tahsilat sansi sabit 8500 idi ve cay
# primiyle 9500'e cikiyordu, yani defteri kullanan herkes ~100 aliyor,
# hic kullanmayan 0 - eksen bir katilim rozetiydi.
#
# Sans artik musterinin ziyaret sayisina bagli (guven) ve tavani 9500.
# Olculdu: herkese yazan bot 72 aliyor, yani hedef gercekten zor ve
# eksen "kullandin mi" degil "IYI kullandin mi" diye soruyor.
#
# FAST FOOD tarafinda ayni sorun DURUYOR ve bilinerek duruyor: eksen
# kombo PAYINA bakiyor, ama "acik tut" ile "zirvede kapat" zit yonler.
# Hedefi yukseltmek, yukaridaki yorumun mesru dedigi oyunu cezalandirir.
# Duzeltmesi hedefi degil OLCULEN SEYI degistirmeyi gerektiriyor
# (ornegin kizgin musteri basina kombo cirosu) - acik madde, docs/45.
SCORE_AXIS = {
    "fastfood": {"kind": "comboShare", "nameKey": "score.axis.combo", "target": 1500},
    "turk": {"kind": "creditCollected", "nameKey": "score.axis.credit", "target": 9000},
}


# docs/23 8.2: mekanik kodda, SAYILAR VERIDE. Blok eksikse mutfak
# yuklenmiyor - imza mekanigi bir mutfagi digerinden ayiran tek sey
# (docs/07 "en onemli satir"), o yuzden sessiz varsayilani yok.
SIGNATURE = {
    # Kombo: dogru kurgu ortalama fisi yukseltir ama mutfak yukunu artirir.
    # priceBp uc kalemin toplamina uygulanan indirim; kitchenLoadBp o uc
    # isin asciyi ne kadar daha uzun bagladigi.
    #
    # MUTFAK YUKU 12000 -> 13500. Olculdu (32 tohum): imzaci oyuncu
    # makul oyuncuyu %35 geciyordu, hedef bant %90-%130. Bant iki
    # yonlu ve sebebi var - alt sinir mekanigin TUZAK olmadigini,
    # ust sinir MECBURIYET olmadigini siniyor. %35 ustte kalmak,
    # kombo acmayan oyuncuyu cezalandirmak demek.
    #
    # Ayarlanan sey INDIRIM degil YUK: indirimi derinlestirmek komboyu
    # zayiflatirdi ama takasin yerini degistirmezdi. Yuk, komboyu tam
    # da vaat ettigi yerden - mutfagin dar bogazindan - pahalilastiriyor.
    "fastfood": {
        "kind": "combo",
        "combo": {
            "items": ["hamburger", "patates_kizartma", "kola"],
            "priceBp": 8750,
            "kitchenLoadBp": 13500,
        },
    },
    # Veresiye: nakit akisini bozar, sadakati ve itibari yukseltir,
    # kimin odeyecegi belirsizdir. docs/07 Turk mutfagi.
    "turk": {
        "kind": "credit",
        "credit": {
            "maxPerRegular": 300000,
            "dueDays": 7,
            # SANS ARTIK KIME YAZDIGINA BAGLI.
            #
            # Sabit 8500 (cayla 9500) idi ve odeyen fisin %112'sini
            # odiyordu: beklenen nakit 0,95 x 1,12 = 1,064 x fis, yani
            # veresiye PESIN SATISTAN KARLIYDI. Reddetmek icin hicbir gun
            # yoktu; mekanik bir defter degil, bedava bir prim dugmesiydi.
            #
            # Taban artik "tanidigin ama yeni tanistigin biri" seviyesi.
            "collectChanceBp": 6000,
            "teaCollectBonusBp": 1000,
            "defaultRepPenaltyCenti": 300,
            "loyaltyBonusCenti": 800,
            "teaCostCenti": 200,
            "loyaltyDemandBp": 60,
            "loyaltyCapBp": 1500,
            # Veresiye isteme sansi. Duzenli musteri icerigi gelmeden once
            # bu %12 idi ve aday kitle GENISTI (butun sik gelen arketipler).
            # Artik aday yalnizca veresiyeye uygun ISIMLI musteri: yedi kisi,
            # her biri gunlerin yarisinda ugruyor. Ayni oranla mekanik
            # neredeyse hic islemiyordu - altmis gunde uc hesap.
            "askChanceBp": 4000,
            "refusedPenaltyCenti": 1200,
            # 1200 -> 800: tek basina karli olmasin. Getirisi
            # ustune koydugu para degil, SADAKAT ve memnuniyet.
            "repayBonusBp": 800,
            # GUVEN: musterinin her ziyareti sansa 400 bp ekliyor,
            # en fazla 3000. Yani yillardir gelen biri %90'a cikiyor,
            # yeni tanistigin %60'ta kaliyor - ve soru "veresiye
            # acayim mi" degil "BU ADAMA acayim mi" oluyor.
            "trustPerVisitBp": 400,
            "trustCapBp": 3000,
            # Tam kesinlik YOK: risksiz bir defter yine karar uretmez.
            "chanceCapBp": 9500,
        },
    },
}


# ---------------------------------------------------------------------------
# content/equipment.json
# ---------------------------------------------------------------------------
def equipment():
    """
    Istasyonlar ve ekipman merdiveni. docs/27 Karar D:
      - prepMs yemegin duvar saati suresi, ekipman ona DOKUNMAZ
      - asci mesguliyeti = prepMs x attendBp / 10000
      - yukseltme ya yuva ekler ya attendBp dusurur

    Fiyatlar model.equipment() icinde kiradan turetiliyor, elle konmuyor.
    """
    stations = []
    for st in model.equipment():
        tiers = []
        for t in st["tiers"]:
            tiers.append({
                "tier": t["tier"],
                "slots": t["slots"],
                "attendBp": t["attend"],
                "price": t["price"] * COIN,      # santi-sikke
                "neededAtTables": t["needAt"],   # 0 = zorunlu degil
            })
        stations.append({
            "id": st["id"],
            "nameKey": "station." + st["id"],
            "tiers": tiers,
        })

    # Kademe 4 zirvesinde her istasyonun yuvasi docs/27 3.3 ile birebir
    # olmali. Bu kontrol olmadan conc degerleri sessizce kayabilir.
    want = {"ocak": 4, "izgara": 4, "firin": 2, "soguk": 1, "icecek": 1, "tatli": 1}
    for st in stations:
        top = max(t["slots"] for t in st["tiers"])
        assert top == want[st["id"]], (
            "{}: en ust yuva {} ama docs/27 {} diyor".format(st["id"], top, want[st["id"]]))
        # Fiyat merdiveni artan olmali
        prices = [t["price"] for t in st["tiers"]]
        assert prices == sorted(prices), st["id"] + " fiyatlari artmiyor"
        # attendBp hicbir basamakta artmamali
        att = [t["attendBp"] for t in st["tiers"]]
        assert att == sorted(att, reverse=True), st["id"] + " attendBp artiyor"

    # Soguk hava merdiveni. keepBp: malzemenin KENDI raf omrunun yuzde
    # kaci gecerli. t0 sifir, yani docs/12 3'un tasarlanmis temeli.
    storage_tiers = []
    for t in model.storage():
        storage_tiers.append({
            "tier": t["tier"],
            "keepBp": t["keep"],
            "price": t["price"] * COIN,
        })
    assert storage_tiers[0]["keepBp"] == 0, "storage t0 keepBp sifir olmali"
    assert storage_tiers[0]["price"] == 0, "storage t0 bedava olmali"
    for i in range(1, len(storage_tiers)):
        assert storage_tiers[i]["keepBp"] > storage_tiers[i - 1]["keepBp"]
        assert storage_tiers[i]["price"] > storage_tiers[i - 1]["price"]

    # Mutfaga OZEL adlandirilmis ekipman. Paylasilan alti istasyondan
    # farki: baslangicta YOK, satin alinana kadar bagli yemekler kilitli.
    cuisine_stations = {}
    for cid, _, _ in CUISINES:
        # "opens" listesi TURETILIYOR: yemegin kendi station alani zaten
        # hangi ekipmani istedigini soyluyor. Elle ikinci bir liste tutmak
        # bu projede bir kez sessizce ayristi.
        opens = {}
        dish_path = os.path.join(CONTENT, "dishes", cid + ".json")
        if os.path.exists(dish_path):
            for d in json.load(io.open(dish_path, encoding="utf-8")):
                opens.setdefault(d["station"], []).append(d["id"])

        rows = []
        for st in model.cuisine_stations(cid):
            if not opens.get(st["id"]):
                raise AssertionError(
                    cid + ": " + st["id"] + " hicbir yemek acmiyor; "
                    "ya yemek yazilmali ya istasyon silinmeli")
            rows.append({
                "id": st["id"],
                "nameKey": "station." + st["id"],
                "tiers": [
                    {"tier": 0, "slots": 1, "attendBp": st["attend"],
                     "price": 0, "neededAtTables": 0},
                    {"tier": 1, "slots": 2, "attendBp": st["attend"],
                     "price": st["price"] * COIN, "neededAtTables": 0},
                ],
                "opens": opens[st["id"]],
            })
        if rows:
            cuisine_stations[cid] = rows

    return {
        "schemaVersion": 1,
        "_comment": "URETILEN DOSYA. Elle degistirmeyin; tools/balance/export.py calistirin.",
        "stations": stations,
        "cuisineStations": cuisine_stations,
        "storage": {
            "nameKey": "storage.soguk_hava",
            "tiers": storage_tiers,
        },
    }


# ---------------------------------------------------------------------------
# tests/golden/weekly.json
# ---------------------------------------------------------------------------
def golden():
    rows = model.run()
    out = []
    for r in rows:
        c = r["crew"]
        out.append({
            "week": r["week"],
            "tables": r["tables"],
            "reputationCenti": r["rep"] * 100,
            "ticket": r["ticket"] * COIN,
            "weekdayCustomers": r["weekday"],
            "weekendCustomers": r["weekend"],
            "weekCustomers": r["week_customers"],
            "cooks": c["asci"],
            "salon": c["salon"],
            "crewTotal": c["total"],
            "staffCap": r["cap"],
            # Para alanlari santi-sikke, tam hassasiyetten yuvarlanmis
            "revenue": rnd(r["revenue"] * COIN),
            "ingredients": rnd(r["ingredients"] * COIN),
            "wages": rnd(r["wages"] * COIN),
            "rent": rnd(r["rent"] * COIN),
            "expansion": rnd(r["expansion"] * COIN),
            "net": rnd(r["net"] * COIN),
            "cash": rnd(r["cash"] * COIN),
        })
    return {
        "_comment": "URETILEN DOSYA. tools/balance/export.py. C# cekirdegi bunu tutturmak zorunda.",
        "source": "tools/balance/model.py",
        "toleranceCenti": 100,
        "weeks": out,
    }


def write(path, obj):
    ensure(os.path.dirname(path))
    with io.open(path, "w", encoding="utf-8", newline="\n") as f:
        json.dump(obj, f, ensure_ascii=False, indent=2)
        f.write("\n")
    print("yazildi: " + os.path.relpath(path, ROOT))


def main():
    for cid, obj in cuisines():
        write(os.path.join(CONTENT, "cuisines", cid + ".json"), obj)
    write(os.path.join(CONTENT, "economy.json"), economy())
    write(os.path.join(CONTENT, "staff-roles.json"), staff_roles())
    write(os.path.join(CONTENT, "equipment.json"), equipment())
    write(os.path.join(GOLDEN, "weekly.json"), golden())
    print("---")
    print("salon is yuku / musteri : {:.6f} is-gunu".format(model.SALON_LOAD))
    print("mikro toplam            : {}".format(
        sum(rnd(MICRO / float(c)) for c in
            (model.CAP_GARSON, model.CAP_BULASIKCI, model.CAP_KASIYER))))
    print("salon gunluk ucret      : {:.4f} sikke".format(model.WAGE_SALON))
    for cid, slots, _roles in CUISINES:
        ticks = [480_000 // 100 * s // BP for s in slots]
        print("dilim tick {:<9}: {}".format(cid, ticks))


def render_docs():
    """
    Dokumanlardaki uretilen tablolari da tazeler.

    export.py'ye ZINCIRLENDI cunku ikisi ayri calistiginda ayrisiyorlar ve
    ayrisma sessiz: denetimde docs/12 ile content/economy.json bir
    kalibrasyon kusagi farkla bulundu - belge 850/1.950/2.900/5.000 kira
    ve 7000 gerceklesme anlatiyordu, icerikte 650/1.550/2.250/4.000 ve
    6500 vardi. Tasarimin referans belgesi var olmayan bir ekonomiyi
    anlatiyordu.
    """
    import subprocess
    import sys as _sys
    here = os.path.dirname(os.path.abspath(__file__))
    subprocess.check_call([_sys.executable, os.path.join(here, "render.py")])


if __name__ == "__main__":
    main()
    render_docs()
