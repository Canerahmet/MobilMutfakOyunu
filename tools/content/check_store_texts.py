# -*- coding: utf-8 -*-
"""Magaza metinlerini Play Console'un SINIRLARINA gore olcer.

NEDEN BIR ARAC: metinler docs/44'te duruyor ve uzunluklari ELLE
yazilmisti. Iki tanesi yanlisti - biri 60 diyordu, 61'di. Yanlis olmasi
zararsizdi cunku sinirin cok altindalar; ama ayni elle yazma, sinira
yaklasan bir metinde "79 karakter" der ve metin 81 olur. Play o metni
kabul etmez ve bu, gonderim gununde ogrenilecek en kotu seylerden biri.

NE OLCUYOR:
  - Kisa aciklama <= 80 karakter (Play siniri)
  - Tam aciklama  <= 4000 karakter (Play siniri)
  - Her dilin ikisi de VAR mi (bir dil eklenip magaza metni unutulursa,
    oyun bes dilde acilir ve listede tek dil gorunur)
  - Tablodaki karakter sayilari GERCEK uzunlukla ayni mi

NEDEN DOSYADAN OKUYOR: metinlerin tek kaynagi docs/44. Buraya ikinci bir
kopya yazmak, ikisinin ayrilmasi demekti.
"""
import io
import os
import re
import sys

KOK = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
BELGE = os.path.join(KOK, "docs", "44-store-texts.md")

# Play Console sinirlari.
KISA_SINIR = 80
TAM_SINIR = 4000

# Oyunun dilleri; Loc.Languages ile ayni kume olmali.
# Tablodaki kod -> tam metin basligi.
DILLER = {
    "TR": "Türkçe",
    "EN": "English",
    "ES": "Español",
    "ZH": "简体中文",
    "AR": "العربية",
}


def oku():
    if not os.path.exists(BELGE):
        print("magaza belgesi yok: " + BELGE)
        sys.exit(1)
    return io.open(BELGE, encoding="utf-8").read()


def main():
    s = oku()
    hata = []

    # --- kisa aciklamalar ------------------------------------------------
    kisa = {}
    for kod, metin, iddia in re.findall(
            r"^\| (TR|EN|ES|ZH|AR) \| (.+?) \| (\d+) \|$", s, re.M):
        kisa[kod] = metin
        n = len(metin)
        if n != int(iddia):
            hata.append("kisa/%s: tabloda %s yaziyor, gercek %d" % (kod, iddia, n))
        if n > KISA_SINIR:
            hata.append("kisa/%s: %d karakter, sinir %d" % (kod, n, KISA_SINIR))

    # --- tam aciklamalar -------------------------------------------------
    tam = {}
    for baslik, govde in re.findall(r"\n### (.+?)\n\n```\n(.*?)\n```", s, re.S):
        tam[baslik.strip()] = govde

    for kod, baslik in sorted(DILLER.items()):
        if kod not in kisa:
            hata.append("kisa/%s: yok" % kod)
        if baslik not in tam:
            hata.append("tam/%s: '%s' baslikli metin yok" % (kod, baslik))
            continue
        n = len(tam[baslik])
        if n > TAM_SINIR:
            hata.append("tam/%s: %d karakter, sinir %d" % (kod, n, TAM_SINIR))

    # --- rapor -----------------------------------------------------------
    print("kisa aciklama (sinir %d):" % KISA_SINIR)
    for kod in sorted(DILLER):
        if kod in kisa:
            print("  %-3s %4d karakter" % (kod, len(kisa[kod])))
    print("tam aciklama (sinir %d):" % TAM_SINIR)
    for kod, baslik in sorted(DILLER.items()):
        if baslik in tam:
            print("  %-3s %4d karakter" % (kod, len(tam[baslik])))

    if hata:
        print()
        for h in hata:
            print("HATA: " + h)
        print("sonuc   : %d sorun" % len(hata))
        sys.exit(1)

    print("sonuc   : %d dilin magaza metni sinirlar icinde" % len(DILLER))


if __name__ == "__main__":
    main()
