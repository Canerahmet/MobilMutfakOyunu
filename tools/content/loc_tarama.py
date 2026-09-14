# -*- coding: utf-8 -*-
"""Arayüz metni kodda mı, tabloda mı — ve tabloda ÖLÜ anahtar var mı.

İki denetim, ikisi de `gen_loc.py` içinden koşuyor.

**1. Kodda gömülü arayüz metni.** `gen_loc.py`'nin kendi docstring'i
bunu bir yıl boyunca *itiraf etti* ("yirmi beş kadar arayüz dizesi hâlâ
kodun içinde gömülü") ama hiçbir denetim kırmıyordu; sayı 25'ten 40'a
çıktı ve mutfak seçimi ekranı tamamen Türkçe kaldı — oyuna İngilizce
başlayan biri **ilk ekranda** Türkçe okuyordu. Bilinen bir borcun
denetimi yoksa borç değil, sızıntıdır.

**2. Ölü `ui.*` anahtarı.** `SCREEN_KEY` bütün `ui.*` ailesini
"fazlalık" denetiminden muaf tutuyor — mantıklı, çünkü içerik onları
istemiyor. Ama o muafiyet, hiçbir ekranın istemediği anahtarları da
görünmez yapıyordu: iki dilde bakımı yapılan, çevrilen, hiç
gösterilmeyen metin.

Her ikisi de KAYNAK TARAMASI, yani yaklaşık. Yanlış alarm vermemek
için ikisi de dar tutuldu: birincisi yalnızca metin kuran çağrıların
ilk argümanına, ikincisi yalnızca dinamik kurulmadığı bilinen
öneklere bakıyor.
"""
from __future__ import print_function

import io
import os
import re

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
GAME = os.path.join(ROOT, "unity", "Assets", "Lokanta", "Game")

# Metin KURAN cagrilar. Ilk argumani harf iceren bir sabit dizeyse,
# o metin ekranda gorunuyor ve tabloda olmali.
KURUCU = re.compile(
    r'\b(?:Theme\.(?:Text|Btn|Title|Head|Field|Small)|Toast)\s*\(\s*"([^"]*)"')

# Harf iceriyor mu (yalnizca "%", "—", "{0}" gibi seyler metin degil).
HARF = re.compile(r"[A-Za-zÇĞİÖŞÜçğıöşü]")

# ISTISNA: metin OLMAYAN sabitler. Ikisi de ekranda gorunuyor ama
# cevrilecek bir sey degil.
BEYAZ_METIN = {
    "", " ", "—", "-", "·", "›", "×", "−", "+", "%", "¤", "…",
}

# Dinamik kurulan anahtar onekleri: kodda tam hali hicbir zaman
# yazmiyor, parca parca birlestiriliyor.
DINAMIK = (
    "ui.phase.", "ui.end.plaque", "station.", "cuisine.", "role.",
    "trait.", "dish.", "ingredient.", "archetype.", "storage.",
    "regular.", "notice.", "score.", "ui.service.done_",
)


def _kaynaklar():
    for kok, _dirs, files in os.walk(GAME):
        for f in files:
            if f.endswith(".cs"):
                yield os.path.join(kok, f)


def gomulu_metinler():
    """Kodda gomulu kalmis arayuz metinleri: [(dosya, satir, metin)]."""
    bulunan = []
    for yol in _kaynaklar():
        satirlar = io.open(yol, encoding="utf-8").read().split("\n")
        for i, satir in enumerate(satirlar):
            kod = satir.split("//")[0]
            for m in KURUCU.finditer(kod):
                metin = m.group(1)
                if metin in BEYAZ_METIN:
                    continue
                if not HARF.search(metin):
                    continue
                bulunan.append((os.path.relpath(yol, ROOT), i + 1, metin))
    return bulunan


# TURKCEYE OZGU HARFLER. Bir dize bunlardan birini tasiyorsa Turkce
# yazilmis demektir ve arayuz klasorunde bunun tek dogru yeri yoktur -
# metin tabloya ait.
TURKCE = re.compile(u"[çğıöşü"
                    u"ÇĞİÖŞÜ]")

DIZE = re.compile(r'"([^"\\]*)"')

UI_DIR = os.path.join(GAME, "Ui")


def turkce_dizeler():
    """Ui klasorunde Turkceye ozgu harf tasiyan metin sabitleri.

    KURUCU taramasindan AYRI, cunku o yalnizca
    `Theme.Text/Btn/...` cagrilarinin ILK argumanina bakiyor. Metin
    cogu zaman parca parca birlesiyor:

        "Soğuk hava kademe " + App.Sim.StorageTier
        d.UnlockDay + ". günde açılıyor"

    Ikisi de ekranda gorunen Turkce ve ikisi de o taramadan kaciyordu.
    Turkce harf olcutu kaba ama ucuz ve yanlis alarm vermiyor: arayuz
    klasorunde Turkceye ozgu bir harf tasiyan hicbir sabit dize
    mesru degil.
    """
    bulunan = []
    for kok, _dirs, files in os.walk(UI_DIR):
        for f in sorted(files):
            if not f.endswith(".cs"):
                continue
            yol = os.path.join(kok, f)
            satirlar = io.open(yol, encoding="utf-8").read().split(chr(10))
            for i, satir in enumerate(satirlar):
                if satir.lstrip().startswith("//"):
                    continue
                kod = satir.split("//")[0]
                for m in DIZE.finditer(kod):
                    if TURKCE.search(m.group(1)):
                        bulunan.append((os.path.relpath(yol, ROOT), i + 1, m.group(1)))
    return bulunan


def kullanilan_anahtarlar():
    """Kodda ANAHTAR OLARAK gecen dizeler.

    `Loc.T("...")` yetmiyor: anahtarlar yalnizca cagri yerinde degil,
    tablo ve tanim dizilerinde de duruyor - `new Hint("servis",
    "ui.hint.service")` gibi. Onlari gormeyen bir tarama, canli
    anahtari "olu" diye bildirir ve bu, denetimin kendisini
    guvenilmez yapar.

    O yuzden olcut basit: anahtarin TAM HALI kaynak agacinda bir metin
    sabiti olarak geciyor mu.
    """
    sabit = re.compile(r'"([A-Za-z][A-Za-z0-9_.]*\.[A-Za-z0-9_.]+)"')
    bulunan = set()
    for yol in _kaynaklar():
        metin = io.open(yol, encoding="utf-8").read()
        bulunan.update(sabit.findall(metin))
    return bulunan


def olu_anahtarlar(table):
    """Tabloda olan ama hicbir ekranin istemedigi ui.* anahtarlari."""
    kullanilan = kullanilan_anahtarlar()
    olu = []
    for k in table:
        if not k.startswith("ui."):
            continue
        if k in kullanilan:
            continue
        if any(k.startswith(p) for p in DINAMIK):
            continue
        olu.append(k)
    return sorted(olu)
