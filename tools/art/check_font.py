# -*- coding: utf-8 -*-
"""Yazi tipi kapsama denetcisi.

Oyunun ekranda gosterebilecegi HER karakterin yazi tipinde karsiligi var
mi diye bakar. Olmayan bir karakter oyuncuya bos kutu olarak gorunur ve
bu, yayindan sonra fark edilen turden bir hatadir.

Nereye bakiyor:
  - content/loc/tr.json           (butun arayuz metinleri)
  - content/*.json                (yemek, malzeme, arketip, duzenli adlari)
  - unity/Assets/Lokanta/Game/**  (koda gomulu dizeler)

Neden koda gomulu dizeler de: yerellestirme tablosu her metni tasimiyor -
"Patronsun, asci degil." gibi bazi metinler dogrudan ekran kodunda.
Denetci ikisini de taramazsa eksigi bulamaz.

Kullanim:
    python tools/art/check_font.py
    python tools/art/check_font.py --font vendor/rubik/Rubik-wght.ttf

Cikis kodu 0 temiz, 1 eksik karakter var.
"""
from __future__ import print_function

import argparse
import io
import json
import os
import re
import struct
import sys
import unicodedata

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
# DENETLENEN DOSYA, OYUNA GIRENIN TA KENDISI OLMALI.
#
# Once vendor/ altindaki indirme kopyasi taraniyordu. Ikisi bugun ayni
# ama ayni KALACAGININ garantisi yoktu: birini guncelleyip digerini
# unutmak, "hepsi kapsaniyor" diyen bir denetim ile eksik karakterli bir
# oyun demek - ve denetim yesil oldugu icin kimse bakmaz.
DEFAULT_FONT = os.path.join(
    ROOT, "unity", "Assets", "Lokanta", "Art", "Fonts", "Rubik.ttf")
VENDOR_FONT = os.path.join(ROOT, "vendor", "rubik", "Rubik-wght.ttf")

# CINCE AYRI YAZI TIPI.
#
# Rubik Latin, Kiril, Ibrani ve ARAPCA tasiyor (sekillendirme tablolari
# dahil) ama CJK tasimiyor - Cince tablosunda 765 karakteri karsilamadi.
# Noto Sans SC (SIL OFL 1.1) oyunun KULLANDIGI karakterlere alt kume
# cikarilarak eklendi: 10,5 MB -> 223 KB.
CJK_FONT = os.path.join(
    ROOT, "unity", "Assets", "Lokanta", "Art", "Fonts", "NotoSansSC-Lokanta.ttf")

# HANGI DOSYAYI HANGI YAZI TIPI CIZIYOR.
#
# Once tek yazi tipi vardi ve butun metin ona soruluyordu. Iki yazi tipi
# olunca "herhangi birinde varsa tamam" demek YANLIS olurdu: Cince
# fontta Turkce harf bulunmasi, Turkce ekranin Cince fontla cizilecegi
# anlamina gelmez. Her dosya, oyunda onu GERCEKTEN cizecek yazi tipiyle
# karsilastiriliyor.
def _is_cjk(ch):
    """CJK blogundan mi. Oyun bunlari ayri yazi tipiyle ciziyor."""
    cp = ord(ch)
    return (0x3000 <= cp <= 0x303F      # CJK noktalama
            or 0x3400 <= cp <= 0x4DBF   # genisletme A
            or 0x4E00 <= cp <= 0x9FFF   # ortak ideogramlar
            or 0xF900 <= cp <= 0xFAFF   # uyumluluk
            or 0xFF00 <= cp <= 0xFFEF)  # tam genislik bicimler


# Loc.PersonName ile AYNI tablo. Degistiren iki yeri birden
# degistirmeli; denetim bunu zaten gosteriyor (alt kume eksik cikar).
KATLAMA = {
    u"ğ": u"g", u"Ğ": u"G",
    u"ı": u"i", u"İ": u"I",
    u"ş": u"s", u"Ş": u"S",
}


def _is_arabic(ch):
    cp = ord(ch)
    return 0x0600 <= cp <= 0x06FF or 0x0750 <= cp <= 0x077F         or 0xFB50 <= cp <= 0xFDFF or 0xFE70 <= cp <= 0xFEFF


def _font_for(path):
    return CJK_FONT if os.path.basename(path) == "zh.json" else None


def _same_bytes(a, b):
    """Iki dosya ayni mi. Yoksa ikisinden biri eskimis demektir."""
    try:
        with open(a, "rb") as fa, open(b, "rb") as fb:
            return fa.read() == fb.read()
    except IOError:
        return None

def _control(ch):
    """Ekranda gorunmeyen denetim karakteri mi.

    Kayit bicimi alanlari U+001F ile ayiriyor (SaveStore). O karakter
    dosyada duruyor, ekranda degil - denetcinin onu eksik saymasi
    yanlis alarm olurdu.
    """
    return ord(ch) < 0x20 or 0x7F <= ord(ch) <= 0x9F


# ---------------------------------------------------------------------------
def font_codepoints(path):
    """TTF'nin cmap format 4 tablosundan kapsanan araliklari okur."""
    data = open(path, "rb").read()
    num = struct.unpack(">H", data[4:6])[0]

    tables = {}
    for i in range(num):
        off = 12 + i * 16
        tag = data[off:off + 4].decode("latin1")
        o, ln = struct.unpack(">II", data[off + 8:off + 16])
        tables[tag] = (o, ln)

    if "cmap" not in tables:
        raise SystemExit("cmap tablosu yok: " + path)

    co = tables["cmap"][0]
    ntab = struct.unpack(">H", data[co + 2:co + 4])[0]

    ranges = []
    for i in range(ntab):
        pid, _eid, off = struct.unpack(">HHI", data[co + 4 + i * 8:co + 12 + i * 8])
        sub = co + off
        fmt = struct.unpack(">H", data[sub:sub + 2])[0]

        if fmt == 4 and pid == 3:
            segx2 = struct.unpack(">H", data[sub + 6:sub + 8])[0]
            seg = segx2 // 2
            ends = struct.unpack(">%dH" % seg, data[sub + 14:sub + 14 + segx2])
            starts = struct.unpack(">%dH" % seg,
                                   data[sub + 16 + segx2:sub + 16 + 2 * segx2])
            ranges.extend(zip(starts, ends))

        elif fmt == 12:
            n = struct.unpack(">I", data[sub + 12:sub + 16])[0]
            for g in range(n):
                o = sub + 16 + g * 12
                s, e, _gid = struct.unpack(">III", data[o:o + 12])
                ranges.append((s, e))

    if not ranges:
        raise SystemExit("Okunabilir cmap alt tablosu yok: " + path)
    return ranges


def covered(ranges, cp):
    for s, e in ranges:
        if s <= cp <= e:
            return True
    return False


# ---------------------------------------------------------------------------
# C# dize sabitleri. Kacis dizileri cozuluyor: "ç" yazan bir kaynak
# ekranda ç gosterir, denetci onu da gormeli.
CS_STRING = re.compile(r'"((?:[^"\\\n]|\\.)*)"')
CS_ESCAPE = re.compile(r'\\u([0-9A-Fa-f]{4})|\\(.)')


def unescape(s):
    def sub(m):
        if m.group(1):
            return chr(int(m.group(1), 16))
        return {"n": "\n", "t": "\t", "r": "\r",
                '"': '"', "\\": "\\"}.get(m.group(2), m.group(2))
    return CS_ESCAPE.sub(sub, s)


def scan_csharp(path, seen):
    src = io.open(path, encoding="utf-8").read()

    # Yorum satirlarini at: yorumdaki bir karakter ekrana cikmiyor.
    src = re.sub(r"//[^\n]*", "", src)
    src = re.sub(r"/\*.*?\*/", "", src, flags=re.S)

    for m in CS_STRING.finditer(src):
        text = unescape(m.group(1))
        for ch in text:
            seen.setdefault(ch, path)


def scan_json(path, seen):
    try:
        data = json.load(io.open(path, encoding="utf-8"))
    except ValueError as e:
        print("  atlandi (bozuk json): %s - %s" % (path, e))
        return

    def walk(node):
        if isinstance(node, dict):
            for v in node.values():
                walk(v)
        elif isinstance(node, list):
            for v in node:
                walk(v)
        elif isinstance(node, str):
            for ch in node:
                seen.setdefault(ch, path)

    walk(data)


# ---------------------------------------------------------------------------
# DIL CINCE IKEN EKRANDA NE CIKABILIR.
#
# Once soru "Cince tablosunda hangi karakter var" diye soruluyordu ve
# denetim yesildi. Oysa UiRoot.FontForLanguage dil Cince oldugunda
# BUTUN AGACI bu yazi tipiyle ciziyor - yalnizca Cince metni degil.
# Uc sey bu yuzden bos kutu olarak gidiyordu:
#
#   - Personel isimleri: content/names.json yerellestirme tablosunda
#     degil, doksan alti ismin on altisi (Ayse, Ibrahim, Yagmur, Sila...)
#     alt kumede hic yoktu.
#   - Aksam raporundaki eksi isareti U+2212 ve menudeki madde imi
#     U+2022: ikisi de dogrudan C# icinde, hicbir tabloda degil.
#   - Dil secicideki Arapca dugmesi: o ayri bir sorun ve KODDA cozuldu
#     (Rubik yerel olarak veriliyor), cunku Noto Sans SC Arapca
#     tasimiyor - alt kumeye eklenemez.
#
# Kume BURADA tek yerde tanimli: subset_font.py alt kumeyi bundan
# uretiyor, bu denetci ayni kumeyi ariyor. Iki ayri liste olsaydi
# ayrisirlardi ve ayrisma yine bos kutu demekti.
def cjk_characters():
    seen = {}
    zh = os.path.join(ROOT, "content", "loc", "zh.json")
    if os.path.exists(zh):
        scan_json(zh, seen)
    isimler = os.path.join(ROOT, "content", "names.json")
    if os.path.exists(isimler):
        scan_json(isimler, seen)
    game = os.path.join(ROOT, "unity", "Assets", "Lokanta", "Game")
    for base, _dirs, files in os.walk(game):
        for name in sorted(files):
            if name.endswith(".cs"):
                scan_csharp(os.path.join(base, name), seen)
    # ARAP HARFLERI BU HAVUZDA ARANMIYOR.
    #
    # Havuza yalnizca tek bir yerden giriyorlar: Loc.LanguageNames
    # icindeki Arapca dil adi, yani dil secicideki dugme. O dugme
    # MenuScreens'te YEREL OLARAK Rubik ile ciziliyor, cunku Noto Sans
    # SC Arapca tasimiyor ve alt kumeye eklenemez - olmayan bir glifin
    # alt kumesi cikarilamaz.
    #
    # Yani bu bir mazeret degil, kodda karsiligi olan bir istisna:
    # istisna kalkarsa dugme bos kutu olur ve turdaki dil secici
    # kontrolu bunu gorur.
    # KOD NE YAPIYORSA DENETIM DE ONU YAPAR.
    #
    # Loc.PersonName dil Cince'yken Latin Extended-A'daki bu bes harfi
    # katliyor, cunku Noto Sans SC'de yoklar ve KAYNAK fontta da yoklar
    # - alt kumeye eklenerek cozulemezler. Katlamayi burada da
    # uygulamazsak, denetim cozulemeyecek bir eksigi sonsuza kadar
    # rapor eder.
    #
    # Havuzun TAMAMINA uygulaniyor: bu harfler havuza iki yerden
    # giriyor - personel isimleri (katlaniyorlar) ve turun dosya adi
    # yardimcisindaki tek harflik dizeler (ekrana hic cikmiyorlar).
    # Ikisi de katlandiktan sonra ayni yere varir.
    return sorted({KATLAMA.get(ch, ch) for ch in seen
                   if not _control(ch) and not _is_arabic(ch)})


# ---------------------------------------------------------------------------
def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--font", default=DEFAULT_FONT)
    args = ap.parse_args()

    if not os.path.exists(args.font):
        raise SystemExit("Yazi tipi yok: " + args.font)

    ranges = font_codepoints(args.font)
    seen = {}

    # CINCE AYRI TOPLANIYOR: ayri yazi tipiyle karsilastirilacak.
    seen_cjk = {}

    loc = os.path.join(ROOT, "content", "loc")
    if os.path.isdir(loc):
        for name in sorted(os.listdir(loc)):
            if not name.endswith(".json"):
                continue
            yol = os.path.join(loc, name)
            scan_json(yol, seen_cjk if _font_for(yol) else seen)

    content = os.path.join(ROOT, "content")
    for base, _dirs, files in os.walk(content):
        for name in sorted(files):
            if not name.endswith(".json"):
                continue
            yol = os.path.join(base, name)
            # BU AGAC loc/ KLASORUNU DA GEZIYOR.
            #
            # Yukarida zaten dosya dosya yonlendirildi; burada tekrar
            # taranirsa Cince metin IKINCI KEZ, bu kez Rubik havuzuna
            # giriyor ve "eksik" diye raporlaniyor - oysa kendi yazi
            # tipinde var. Ilk yazimda tam bu oldu.
            scan_json(yol, seen_cjk if _font_for(yol) else seen)

    game = os.path.join(ROOT, "unity", "Assets", "Lokanta", "Game")
    for base, _dirs, files in os.walk(game):
        for name in sorted(files):
            if name.endswith(".cs"):
                scan_csharp(os.path.join(base, name), seen)

    # KODA GOMULU CJK KARAKTERLERI DE CJK FONTUNA GIDIYOR.
    #
    # `Loc.LanguageNames` dil adlarini KENDI yazilariyla tasiyor ve
    # "中文" orada duz bir C# dizesi. Oyunda o dugme CJK fontuyla
    # ciziliyor (MenuScreens: dil secici her dili okunabildigi yazi
    # tipiyle yaziyor), yani Rubik'e sormak yanlis soru olurdu.
    #
    # Kural DAR: yalnizca CJK blogu. Bir Turkce harfin koda gomulu
    # olmasi onu CJK fontuna tasimaz.
    for ch in [c for c in seen if _is_cjk(c)]:
        seen_cjk.setdefault(ch, seen.pop(ch))

    # CINCE HAVUZU DILDEN BAGIMSIZ METNI DE ICERIYOR.
    #
    # Personel isimleri ve koda gomulu simgeler dil ne olursa olsun
    # ekrana ciktigi icin, dil Cince'yken onlari da bu yazi tipi
    # ciziyor. Tek kaynak: cjk_characters().
    for ch in cjk_characters():
        seen_cjk.setdefault(ch, "dilden bagimsiz")

    missing = []
    for ch, where in sorted(seen.items()):
        if _control(ch):
            continue
        if not covered(ranges, ord(ch)):
            missing.append((ch, where))

    # --- IKINCI YAZI TIPI: CINCE ------------------------------------------
    if seen_cjk:
        if not os.path.exists(CJK_FONT):
            print("Cince yazi tipi yok: " + os.path.relpath(CJK_FONT, ROOT))
            return 1
        cjk_ranges = font_codepoints(CJK_FONT)
        for ch, where in sorted(seen_cjk.items()):
            if _control(ch):
                continue
            if not covered(cjk_ranges, ord(ch)):
                missing.append((ch, where))
        print("cince     : %s (%d benzersiz karakter)"
              % (os.path.relpath(CJK_FONT, ROOT), len(seen_cjk)))

    print("yazi tipi : %s" % os.path.relpath(args.font, ROOT))
    print("taranan   : %d benzersiz karakter" % len(seen))

    if not missing:
        # Indirme kopyasi ile oyuna giren kopya AYRISTI MI.
        same = _same_bytes(args.font, VENDOR_FONT)
        if same is False:
            print("sonuc     : hepsi kapsaniyor AMA vendor/ kopyasi FARKLI")
            print("            %s" % VENDOR_FONT)
            print("            Biri guncellenip digeri unutulmus; ikisini esitle.")
            return 1

        print("sonuc     : hepsi kapsaniyor")
        return 0

    print("sonuc     : %d karakter EKSIK" % len(missing))
    for ch, where in missing:
        try:
            name = unicodedata.name(ch)
        except ValueError:
            name = "?"
        print("  U+%04X  %-34s  %s" % (ord(ch), name, os.path.relpath(where, ROOT)))
    return 1


if __name__ == "__main__":
    sys.exit(main())
