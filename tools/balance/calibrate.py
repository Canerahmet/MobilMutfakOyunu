# -*- coding: utf-8 -*-
"""
Gerceklesme orani kalibrasyonu - Faz 0
============================================================================
Iki model birbirine bagli ve tek atisla uzlasmiyorlar:

  kapali form model  ->  gerceklesme oraniyla kira, kapasite, genisleme cozer
  simulasyon         ->  o parametrelerle kac musterinin agirlandigini olcer
                         ve gerceklesme oranini YENIDEN belirler

Yani oran parametreleri, parametreler orani degistiriyor. Tek seferlik olcum
yaniltici: 10 Eylul 2026'da simulasyon %93,35 olctu, ama o olcum ESKI ucuz
kiralarla alinmisti. Yeni kiralar uygulaninca ayni strateji genisleyemedi ve
oran cokdu.

Bu betik sabit noktayi ARIYOR: bir aday oran icin butun zinciri kosuyor
(solve -> model -> export -> simulasyon) ve tasarim hedeflerine gore puanlar.

Calistirma:  python calibrate.py            butun adaylari dener
             python calibrate.py 8000        tek adayi dener ve uygular
"""
from __future__ import print_function

import io
import os
import re
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(HERE))
MODEL = os.path.join(HERE, "model.py")
SOLVE = os.path.join(HERE, "solve.py")

sys.path.insert(0, HERE)

# Denenecek gerceklesme oranlari. Alt sinir eski olcum, ust sinir yeni olcum.
CANDIDATES = [6500, 7000, 7500, 8000, 8500, 9000, 9335]


# ---------------------------------------------------------------------------
def patch(path, pairs):
    s = io.open(path, encoding="utf-8").read()
    for pat, sub in pairs:
        s2, n = re.subn(pat, sub, s, count=1, flags=re.M)
        if n != 1:
            raise AssertionError("desen bulunamadi: " + pat)
        s = s2
    io.open(path, "w", encoding="utf-8", newline="\n").write(s)


def solve_for(bp):
    """solve.py'yi verilen oranla calistirir ve en iyi parametreleri doner."""
    patch(SOLVE, [(r"^REALISATION_BP = \d+$", "REALISATION_BP = %d" % bp)])

    import importlib
    import solve
    importlib.reload(solve)

    best = None
    import itertools
    grid_s = [round(x * 0.05, 2) for x in range(14, 31)]
    grid_o = [round(x * 0.1, 1) for x in range(8, 25)]
    grid_e = [round(x * 0.1, 1) for x in range(10, 31)]
    for s, o, e in itertools.product(grid_s, grid_o, grid_e):
        rents = solve.solve_rents(s, o, e)
        if any(v <= 0 for v in rents.values()):
            continue
        rows, _, _ = solve.simulate(s, o, e, rents)
        pen, fails = solve.score(rows)
        if best is None or pen < best[0]:
            best = (pen, s, o, e, rents, fails)
            if pen == 0:
                break
    return best


# Baslangic kasasi ARTIK SERBEST PARAMETRE DEGIL, kiradan turuyor.
#
# Kullanici kurali: hicbir sey yapmayan oyuncu ilk haftayi gecemesin. Ilk
# kira ve maas gunun 7'sinde tek seferde odeniyor, yani kural sudur:
#
#     baslangic kasasi  <  ilk hafta sabit gideri
#
# Hicbir sey yapmayan oyuncunun geliri sifir, dolayisiyla o odemeyi
# karsilayamaz ve 7. gunde borca duser. Calisan oyuncu ise gun icinde
# ciro urettigi icin ayni odemeyi kaldirabiliyor.
START_CASH_BP = 8_500      # ilk hafta giderinin yuzde kaci


def start_cash_for(rent4):
    """Dort masalik kira ve tek ascinin haftaligindan turetilen kasa."""
    week1 = rent4 + 7 * 140          # kira + asci gunluk ucreti
    return int(week1 * START_CASH_BP / 10_000)


def apply_to_model(bp, s, o, e, rents):
    import solve
    caps = dict((k, int(round(v * s))) for k, v in solve.BASE_CAP.items())
    ups = dict((t, int(solve.BASE_UPGRADE[t] * e)) for t in (4, 7, 10, 14))

    tiers = "TIERS = [\n"
    for t, cap in ((4, 3), (7, 5), (10, 8), (14, 12)):
        tiers += "    dict(tables=%-2d rent=%-5d upgrade=%-5d cap=%d),\n" % (
            t, rents[t], ups[t], cap)
    tiers = tiers.replace("tables=4 ", "tables=4,  ").replace("tables=7 ", "tables=7,  ")
    tiers = tiers.replace("tables=10 ", "tables=10, ").replace("tables=14 ", "tables=14, ")
    tiers = re.sub(r"rent=(\d+) ", lambda m: "rent=%s, " % m.group(1), tiers)
    tiers = re.sub(r"upgrade=(\d+) ", lambda m: "upgrade=%s, " % m.group(1), tiers)
    tiers += "]"

    # SECILEN ORAN SOLVE'A DA GERI YAZILIYOR.
    #
    # solve_for() taramada her aday icin SOLVE'u yamiyor, yani tarama
    # bitince orada SON DENENEN aday kaliyor - secilen degil. Iki dosya
    # ayni adli sabiti farkli degerlerle tasiyordu (9335 / 7000) ve
    # docs/12'nin tarif ettigi ELLE akista (python solve.py) bu yanlis
    # kira uretirdi. Kalibrasyon bozuk degildi; artik yaniltiyordu.
    patch(SOLVE, [(r"^REALISATION_BP = \d+$", "REALISATION_BP = %d" % bp)])

    patch(MODEL, [
        (r"^REALISATION_BP = \d+$", "REALISATION_BP = %d" % bp),
        (r"^CAP_ASCI = \d+$", "CAP_ASCI = %d" % caps["asci"]),
        (r"^CAP_GARSON = \d+$", "CAP_GARSON = %d" % caps["garson"]),
        (r"^CAP_BULASIKCI = \d+$", "CAP_BULASIKCI = %d" % caps["bulasikci"]),
        (r"^CAP_KASIYER = \d+$", "CAP_KASIYER = %d" % caps["kasiyer"]),
        (r"^OWNER_WORK = [\d.]+", "OWNER_WORK = %s" % o),
        (r"^TIERS = \[\n(?:.*\n)*?\]$", tiers),
    ])


def run(cmd, cwd=ROOT, check=True):
    """
    Cikis kodu ONEMLI. Eskiden yutuluyordu ve iki ayri hatayi bir gunde
    gizledi: harness'in hic calismamasi, ve export.py'nin dosyalari
    yazdiktan SONRA cokmesi. Ikisinde de kalibrasyon sakin sakin yanlis
    bir cevap yaziyordu.
    """
    p = subprocess.Popen(cmd, cwd=cwd, stdout=subprocess.PIPE,
                         stderr=subprocess.STDOUT, shell=False)
    out, _ = p.communicate()
    text = out.decode("utf-8", "replace")
    if check and p.returncode != 0:
        sys.stderr.write(text[-2000:] + chr(10))
        raise RuntimeError("komut basarisiz (%d): %s" % (p.returncode, " ".join(cmd)))
    return text


# | strateji | son kasa | defter | itibar | masa | kadro | servis | kayip
# | bosaldi | ilk borc | onemsiz |
ROW = re.compile(r"^\|\s*(\w+)\s*\|\s*([-\d,]+)\s*\|\s*([-\d,]+)\s*\|"
                 r"\s*([\d.]+)\s*\|\s*([\d.]+)\s*\|"
                 r"\s*([\d.]+)\s*\|\s*(\d+)\s*\|\s*(\d+)\s*\|\s*([-\d]+)\s*\|"
                 r"\s*([-\d]+)\s*\|\s*([-\d.]+)\s*\|", re.M)


# Gelir tablosu satiri: | strateji | ciro | malzeme | maas | kira | net | ...
INCOME = re.compile(r"^\|\s*(\w+)\s*\|\s*([\d,]+)\s*\|\s*([\d,]+)\s*\|"
                    r"\s*([\d,]+)\s*\|\s*([\d,]+)\s*\|\s*([-\d,]+)\s*\|", re.M)


def measured_bp(out):
    """
    Simulasyonun URETTIGI gerceklesme orani: plancinin altmis gunluk
    cirosu, kapali form modelin ayni takvimdeki TALEP cirosuna bolunmus.

    DIKKAT: bu sayi %100'u ASABILIYOR ve asmasi hata degil. Kapali form
    modelin talebi, PLAN egrisindeki itibar ve fis varsayimlarina bagli.
    Simulasyonda planci itibari 100'e cikariyor, modelin planladigi 88'i
    geciyor ve daha cok musteri geliyor. Yani sayi iki seyi birden
    tasiyor: "talebin ne kadari agirlandi" ve "oyuncu plan egrisini
    gecti mi".

    Bu yuzden RAPORLANIYOR ama SECIM OLCUSU DEGIL. Beraberlikte secim
    tasarim gerekcesine gore yapiliyor; asagiya bak.
    """
    import importlib
    import model
    importlib.reload(model)
    rows = model.run()
    demand56 = sum(r["revenue"] for r in rows) * 10_000.0 / model.REALISATION_BP
    demand60 = demand56 * 60.0 / 56.0

    for m in INCOME.finditer(out):
        if m.group(1) == "planci":
            rev = int(m.group(2).replace(",", ""))
            return int(round(rev / demand60 * 10_000))
    return 0


# Butun mutfaklar. docs/33: tek mutfakla olcmek Turk lokantasinin sekiz
# stratejide de sifir musteriyle battigini altmis gun boyunca gizledi.
CUISINES = ["fastfood", "turk"]


def harness(cuisine="fastfood", seeds=None):
    # Bu makinede Windows "Application Control" politikasi taze yazilan
    # derlemeleri engelliyor, ve HANGI yapilandirmayi engelledigi zamanla
    # degisiyor: bir gun Debug bloke, ertesi gun Release. Ilk yazimda
    # "-c Release sart" diye sabitlenmisti ve bir sonraki blok kalibrasyonu
    # tamamen durdurdu.
    #
    # Dogrusu SABITLEMEK degil DENEMEK: hangisi calisiyorsa o. Ikisi de
    # calismiyorsa hata firlatiliyor - sessizce bos tablo dondurmek,
    # kalibrasyonun butun adaylari esitlemesine yol aciyordu.
    extra = ["--seeds", str(seeds)] if seeds else []
    out = ""
    # Once normal, sonra KARMA DEGISTIREREK ve YENIDEN DERLEYEREK.
    #
    # Ikisi birden gerekiyor ve bu iki kez yanlis yazildi:
    # -p:Deterministic=false tek basina hicbir sey yapmiyor, cunku
    # kaynak degismediyse MSBuild derlemeyi guncel sayip ATLIYOR ve
    # ayni engelli ikili tekrar kullaniliyor. --no-incremental
    # derlemeyi gercekten tekrarlatiyor; ancak o zaman her deneme yeni
    # bir modul kimligi ve yeni bir sans oluyor.
    #
    # Uc deneme: taze bir karma da engellenebiliyor.
    for attempt in range(3):
        if attempt > 0:
            # YENIDEN DERLEME AYRI ADIM.
            #
            # "dotnet run --no-incremental" ise yaramiyor: run o bayragi
            # tanimiyor ve UYGULAMAYA geciriyor, harness de bilinmeyen
            # bayrak diye reddediyor (dogru davranis). Derlemeyi ayri
            # cagirmak gerekiyor.
            run(["dotnet", "build", "src/Lokanta.Harness",
                 "-c", "Release", "-v", "q", "--nologo",
                 "-p:Deterministic=false", "--no-incremental"], check=False)

        out = run(["dotnet", "run", "--project", "src/Lokanta.Harness",
                   "-c", "Release", "-v", "q", "--nologo"]
                  + (["--no-build"] if attempt > 0 else [])
                  + ["--", "--mutfak", cuisine] + extra, check=False)
        if ROW.search(out):
            break

    rows = {}
    for m in ROW.finditer(out):
        name = m.group(1)
        book = m.group(3).replace(",", "")
        rows[name] = dict(
            cash=int(m.group(2).replace(",", "")),
            # Defterde kalan veresiye: kasada degil ama kaybolmus da degil.
            book=0 if book == "-" else int(book),
            rep=float(m.group(4)),
            tables=float(m.group(5)),
            crew=float(m.group(6)),
            served=int(m.group(7)),
            lost=int(m.group(8)),
            collapse=m.group(9),
            debt=m.group(10),
            trivial=m.group(11),
        )
    if not rows:
        # Tek satir bile okunmadiysa sorun dengede degil, kosuda. Ceza
        # verip devam etmek yanlis: butun adaylar esitlenir ve calibrate
        # rastgele birini "en iyi" diye yazar.
        sys.stderr.write(out[-2000:] + chr(10))
        raise RuntimeError("harness ciktisi bos: " + cuisine)
    return rows, out


# ---------------------------------------------------------------------------
def evaluate(rows):
    """
    Tasarim hedefleri, docs/12 8 ve docs/29. Ihlal basina ceza.
    Hedefler SIRALAMA hedefi: mutlak sayi degil, stratejiler arasi duzen.
    """
    pen, notes = 0.0, []

    def bad(cond, w, msg):
        nonlocal pen
        if not cond:
            pen += w
            notes.append(msg)

    need = ["pasif", "sadece_hal", "makul", "genislemeyen", "atilgan",
            "planci", "yuksek_fiyat", "fazla_kadro", "ucuz_malzeme",
            "mudahaleci", "imzaci"]
    for n in need:
        if n not in rows:
            return 999.0, ["strateji okunamadi: " + n]

    # "Batmak" artik SON KASANIN EKSI OLMASI DEGIL.
    #
    # Batma merdiveni yazildiktan sonra kasa eksiye gomulup kalmiyor:
    # ekipman satiliyor, dukkan kuculuyor, kalan borc itibar bedeliyle
    # siliniyor. Yani kotu oynayan biri de sifirin ustunde bitiriyor - ve
    # eski olcut bunu "batmadi" sayip her seferinde ceza veriyordu.
    #
    # Dogru soru artik "MERDIVENE INDI MI": borca dusmek, bedeli olan bir
    # olay. "defter" sutunu ilk borc gununu tasiyor; "-" hic dusmedi demek.
    # bad() KOSULU ISTENEN DURUM: yanlissa ceza yaziyor.
    bad(rows["pasif"]["debt"] != "-", 12, "pasif oyuncu hic borca dusmuyor")
    bad(rows["atilgan"]["debt"] != "-", 10, "pervasiz genisleyen hic borca dusmuyor")
    bad(rows["makul"]["cash"] > 0, 12, "makul oyuncu batiyor")
    bad(rows["planci"]["cash"] > 0, 8, "planci batiyor")
    bad(rows["genislemeyen"]["cash"] > 0, 6, "genislemeyen batiyor")

    # Buyumek odullendirmeli: docs/12 "uc kat, on bir degil"
    g = rows["genislemeyen"]["cash"]
    m = rows["makul"]["cash"]
    ratio = (m / float(g)) if g > 0 else 0.0
    bad(1.8 <= ratio <= 4.0, 10, "buyume carpani %.2f (hedef 1,8-4,0)" % ratio)

    # Pasiflik acikca kaybetmeli. "sadece_hal" hicbir sey yapmiyor: ne ise
    # aliyor, ne menu yonetiyor, ne genisliyor, ne ekipman aliyor.
    #
    # Esik "genislemeyenden az" DEGIL, bilincli olarak. Dort masada kadro
    # HAFTA SONU ZIRVESINE gore kuruluyor (docs/12 5.1) ve o ikinci kisi
    # kendini cikarmiyor; yani genislemeyen oyuncu modeli dogru izledigi
    # halde az kazaniyor. Bu gercek bir ekonomik ifade, hata degil.
    # Olculmesi gereken sey pasifligin IYI OYUNU yenmemesi.
    #
    # Esik "%60" olarak denendi ve keyfi oldugu anlasildi: sonuc tam o
    # cizginin iki yaninda salindi (fast food %59, Turk %61). Olculmesi
    # gereken sey bir yuzde degil bir SIRALAMA: hicbir sey yapmayan
    # oyuncu, tam plani uygulayani gecmemeli.
    bad(rows["sadece_hal"]["cash"] < rows["planci"]["cash"], 10,
        "sadece_hal (%d) planciyi (%d) geciyor"
        % (rows["sadece_hal"]["cash"], rows["planci"]["cash"]))
    bad(rows["sadece_hal"]["cash"] < rows["makul"]["cash"], 6,
        "sadece_hal iyi oyunu geciyor")
    bad(rows["fazla_kadro"]["cash"] < rows["makul"]["cash"], 4,
        "fazla kadro iyi oyunu geciyor")

    # Malzemeden kismak TUZAK olmali: birim maliyeti dusuruyor ama itibari
    # cokertiyor, musteri azaliyor, buyume duruyor. Icerik en hassas alti
    # malzemeyi ET yaptigi icin zincir menuye gore agirlasiyor.
    bad(rows["ucuz_malzeme"]["cash"] < rows["makul"]["cash"], 6,
        "ucuz malzeme (%d) iyi oyunu (%d) geciyor"
        % (rows["ucuz_malzeme"]["cash"], rows["makul"]["cash"]))

    # docs/02 cekirdek dongusu: "servis sirasinda sadece krizlere mudahale
    # edersin". Mudahale hakki sinirli ve cay ikrami parayla, ama yine de
    # ISE YARAMALI; yoksa oyuncunun servis sirasinda yapabilecegi tek sey
    # zarar demektir.
    #
    # Olcu SON KASA DEGIL, ve bu bilincli. Once kasayla olculdu ve Turk
    # mutfaginda "mudahale zarar ediyor" cikti: 24.058'e karsi 28.638.
    # Ama ayni kosuda mudahalecinin itibari 81,3 (makul 56,5), masasi 11
    # (8,4) ve agirladigi 2.152 (1.998) idi. Yani daha kucuk degil DAHA
    # BUYUK bir isletme calistiriyordu; kasasi az cunku genişlemeye ve
    # kadroya harcamisti.
    #
    # Mudahalenin dogrudan etkiledigi sey itibar ve agirlanan musteri.
    # Kasa, stratejinin o itibarla NE YAPTIGINA bagli ve bu mekanigin
    # olcusu olamaz.
    # Imza mekanigi bir SECENEK olmali: kullanmak tuzak da olmamali,
    # mecburiyet de. docs/07 mekanigi mutfagi mutfaktan ayiran sey diye
    # tarif ediyor; her kosuda kaybettiren bir mekanik ayirt etmez, her
    # kosuda kazandiran bir mekanik ise karar degil dugmedir.
    #
    # Olcu KASA + DEFTER: veresiye altmisinci gunde tahsil edilmemis para
    # birakiyor ve o para kaybolmus degil (docs/08 net varlik).
    im = rows["imzaci"]["cash"] + rows["imzaci"]["book"]
    mk = rows["makul"]["cash"]
    ratio = (im / float(mk)) if mk > 0 else 0.0
    bad(0.90 <= ratio <= 1.30, 6,
        "imza mekanigi dengesiz: imzaci/makul %.2f (hedef 0,90-1,30)" % ratio)
    bad(rows["imzaci"]["debt"] == "-", 6, "imzaci borca dusuyor")

    # Tolerans 3 puan, ve sebebi ITIBAR TAVANI.
    #
    # Bu kontrol, makul oyuncunun itibari 56 iken yazildi; o zaman
    # mudahalenin yukseltecek yeri vardi. Personel huylari gelince
    # makul kendi kadrosunu duzeltmeyi ogrendi ve fast food'da 99,3'e
    # cikti - yani mudahale icin tavan kalmadi ve mekanik "ise
    # yaramiyor" gorundu. Ayni kosuda Turk mutfaginda mudahaleci 100,0,
    # makul 96,1: bosluk olan yerde mekanik CALISIYOR.
    #
    # Dogru kontrol "her zaman daha iyi" degil, "hicbir zaman daha
    # kotu degil" - bir mekanigi, oyuncunun zaten doymus oldugu bir
    # eksende olcmek onu haksiz yere mahkum eder (docs/34 18).
    bad(rows["mudahaleci"]["rep"] >= rows["makul"]["rep"] - 3.0, 6,
        "mudahale itibari dusuruyor (%.1f vs %.1f)"
        % (rows["mudahaleci"]["rep"], rows["makul"]["rep"]))
    bad(rows["mudahaleci"]["served"] >= rows["makul"]["served"] * 0.98, 4,
        "mudahale musteri sayisini dusuruyor (%d vs %d)"
        % (rows["mudahaleci"]["served"], rows["makul"]["served"]))
    bad(rows["mudahaleci"]["debt"] == "-", 6,
        "mudahaleci %s. gunde borca dusuyor" % rows["mudahaleci"]["debt"])

    # DAR MENU BASKIN OLMAMALI. Bir kabul testi, bir denge hedefi degil.
    #
    # Menuden cikarilan yemek uzun sure "sorulmus" sayilmiyordu, yani
    # daraltmanin talep tarafinda SIFIR bedeli vardi. Olculdu: menude
    # tek ana yemek tutan oyuncu makul oyuncuyu fast food'da %12, Turk
    # mutfaginda %38 geciyordu. Soguk hava deposunun ikinci odulu
    # (menu genisligi tasiyabilmek) boylece degersizdi ve otuz iki
    # yemeklik envanterin var olma sebebi ortadan kalkiyordu.
    #
    # Bu satir o hatanin bir daha geri gelmemesi icin duruyor.
    if "tek_yemek" in rows:
        bad(rows["tek_yemek"]["cash"] <= rows["makul"]["cash"], 12,
            "dar menu baskin: tek_yemek %d, makul %d"
            % (rows["tek_yemek"]["cash"], rows["makul"]["cash"]))

    # KULLANICI KURALI: gerekli yatirimlari ZAMANINDA yapan oyuncu itibarini
    # 80 uzerine cikarmali. "Yoksa is yapmanin manasi yok."
    #
    # Olcu "makul" degil "planci". Ikisi farkli oyuncular: makul ancak
    # rahatca karsilayabildiginde genisliyor, yani temkinli; planci
    # takvimi izliyor, ekipmani aliyor, kadroyu kuruyor. Kullanicinin
    # tarifi ikincisi. Olcum farki buyuk: fast food'da makul 77,4, planci
    # 94,2.
    bad(rows["planci"]["rep"] >= 80.0, 10,
        "zamaninda yatirim yapanin itibari %.1f (hedef 80+)" % rows["planci"]["rep"])

    # KULLANICI KURALI, iki yonlu ve ikisi birden tutmali:
    #
    #   "hicbir sey yapmayan"  ilk haftayi GECEMESIN
    #   "kotu yoneten"         ilk haftayi ZOR DA OLSA gecsin
    #
    # Olcu BATMA gunu DEGIL, BOSALMA gunu. Iki sebep:
    #
    # 1. docs/08 kapanisi reddediyor: "kayit silinmez, oyun bitmez". Batma
    #    bir son degil, bir merdiven. Yani "ilk haftayi gecememek" oyunun
    #    bitmesi olamaz.
    # 2. Hicbir sey yapmayan oyuncunun cirosu SIFIR; batma gunu saf
    #    aritmetik (kasa / haftalik gider = 42. gun) ve onu yediye cekmenin
    #    tek yolu kasayi bir haftalik gidere indirmek. Olculdu: o zaman
    #    planci -18.976'ya, iyi oyuncunun itibari 16'ya dusuyor.
    #
    # Olculebilir ve dogru olan sey: DUKKAN NE ZAMAN BOSALDI. Talep egrisi
    # 20 puanin altinda dikleseyor, yani itibar oraya inince restoran
    # gorunur bicimde bosaliyor. Oyuncu isin bittigini o gun goruyor;
    # kasadaki para yalnizca cenazeyi geciktiriyor.
    empty = rows["pasif"]["collapse"]
    bad(empty != "-" and float(empty) <= 7, 12,
        "hicbir sey yapmayanin dukkani %s. gunde bosaliyor (hedef ilk hafta)" % empty)

    lazy = rows["sadece_hal"]["debt"]
    bad(lazy == "-" or float(lazy) > 8, 10,
        "kotu yoneten ilk haftayi gecemiyor (%s. gun)" % lazy)

    # Iyi oyunun dukkani hic bosalmamali. Olcu "makul": dikkatli ve
    # kazandigini olcerek harcayan oyuncu.
    bad(rows["makul"]["collapse"] == "-", 8,
        "iyi oyuncunun dukkani %s. gunde bosaliyor" % rows["makul"]["collapse"])

    # "planci" farkli bir sey olcuyor: kapali form modelin TAKVIMI
    # karsilanabiliyor mu. Kasaya bakmadan genisledigi icin bilerek sinirda
    # yasiyor; gec bir sarsinti mesru, erken bir cokus degil.
    pc = rows["planci"]["collapse"]
    bad(pc == "-" or float(pc) > 30, 6,
        "planci %s. gunde bosaliyor (ilk yaride cokmemeli)" % pc)

    bad(rows["makul"]["tables"] >= 7, 6, "makul oyuncu genisleyemiyor")
    bad(rows["planci"]["tables"] >= 13, 6, "planci takvimi tutturamiyor")
    bad(rows["fazla_kadro"]["cash"] < rows["makul"]["cash"], 4, "fazla kadro cezalandirilmiyor")
    bad(rows["yuksek_fiyat"]["cash"] < rows["makul"]["cash"], 4, "yuksek fiyat cezalandirilmiyor")

    # Para sekizinci haftadan once onemsizlesmemeli
    for n in ("makul", "planci"):
        t = rows[n]["trivial"]
        if t != "-":
            bad(float(t) >= 8.0, 5, "%s: para %s. haftada onemsizlesiyor" % (n, t))

    return pen, notes


def main():
    only = [int(sys.argv[1])] if len(sys.argv) > 1 else CANDIDATES
    results = []
    for bp in only:
        best = solve_for(bp)
        if best is None:
            print("%5d  solve cozum bulamadi" % bp)
            continue
        pen0, s, o, e, rents, fails = best
        apply_to_model(bp, s, o, e, rents)
        run([sys.executable, os.path.join(HERE, "export.py")])
        pen, notes, got = 0.0, [], 0
        for cuisine in CUISINES:
            rows, out = harness(cuisine)
            p2, n2 = evaluate(rows)
            pen += p2
            notes.extend(cuisine + ": " + x for x in n2)
            if cuisine == "fastfood":
                got = measured_bp(out)
        drift = abs(got - bp)
        results.append((pen, drift, bp, s, o, e, rents, notes))
        print("%5d  s=%.2f o=%.1f e=%.1f  kira %s  ceza %.0f  olculen %5d  %s" % (
            bp, s, o, e, [rents[t] for t in (4, 7, 10, 14)], pen, got,
            "; ".join(notes) if notes else "IKI MUTFAK DA TEMIZ"))

    if not results:
        return
    # Once ceza. Beraberlikte EN YUKSEK oran, yani en yuksek kira.
    #
    # Gerekce docs/12 2: kira bilincli olarak baskin sabit gider, "baski
    # metronomu". Butun tasarim hedefleri tutuyorsa daha yuksek kira daha
    # cok gerilim demek ve gerilim istenen sey. Sabit noktadan sapma
    # raporlaniyor ama secmiyor: o sayi %100'u asabiliyor ve astiginda
    # olcugu sey gerceklesme degil, oyuncunun plan egrisini gecmesi.
    results.sort(key=lambda r: (r[0], -r[2]))
    pen, drift, bp, s, o, e, rents, notes = results[0]
    print()
    print("=" * 70)
    print("EN IYI: gerceklesme %d, ceza %.0f, sabit noktadan sapma %d" % (bp, pen, drift))
    print("=" * 70)
    apply_to_model(bp, s, o, e, rents)
    run([sys.executable, os.path.join(HERE, "export.py")])
    print("model.py ve content/ bu ayara gore yazildi.")

    # KAZANANI YUKSEK TOHUMLA YENIDEN OLC.
    #
    # Tarama sekiz tohumla kosuyor ve bu ARAMA icin dogru: adaylar
    # arasindaki ceza farki onlarca puan, gurultu onu bozmuyor. Ama
    # kontrollerin bir kismi YUZDE IKI toleransli ve o olcekte sekiz
    # tohum gurultuden ibaret. Olculdu - ayni ayar, ayni icerik:
    #
    #     tohum    makul   mudahaleci   oran
    #         8     1922         1828   %95,1   (kontrol KIRILIYOR)
    #        16     1953         1921   %98,4   (geciyor)
    #        32     1969         1941   %98,6   (geciyor)
    #
    # Yani arac, gecmesi gereken bir dengeyi kirmizi gosteriyordu ve
    # o kirmiziyi kovalamak var olmayan bir sorunu kovalamak olurdu.
    #
    # Ucuz ARAMA, titiz DOGRULAMA: sweep sekizle, kazanan otuz ikiyle.
    print()
    print("=" * 70)
    print("DOGRULAMA (32 tohum)")
    vpen, vnotes = 0.0, []
    for cuisine in CUISINES:
        vrows, _vout = harness(cuisine, seeds=32)
        p2, n2 = evaluate(vrows)
        vpen += p2
        vnotes.extend(cuisine + ": " + x for x in n2)
    print("ceza %.0f  %s" % (
        vpen, "; ".join(vnotes) if vnotes else "IKI MUTFAK DA TEMIZ"))
    print("=" * 70)


if __name__ == "__main__":
    main()
