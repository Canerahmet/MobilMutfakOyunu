# -*- coding: utf-8 -*-
"""Is the repository written in English?

WHY THIS EXISTS: the rule in CLAUDE.md ("everything a reader sees is in
English") is the kind of rule that decays silently. One Turkish variable
name in a hurry, one comment left untranslated, and a year later nobody
knows which half of the repository is which. A rule nobody measures is a
preference.

WHAT IT MEASURES, in three passes:

  1. NAMES - no file or folder name is a Turkish word.
  2. IDENTIFIERS - no Turkish word is used as an identifier in source.
  3. PROSE - no comment, docstring or Markdown line is Turkish.

HOW "TURKISH" IS DECIDED. Two signals, both cheap and both blunt:

  - Turkish-only letters (c-cedilla, g-breve, dotless i, ...). Precise:
    no English word contains them.
  - A word list. Turkish written without diacritics ("asci", "mutfak")
    looks like ASCII, so letters alone would miss most of it. The list
    is the domain vocabulary this project actually used.

Both are blunt on purpose. A blunt check that runs beats a clever one
that does not - and the exceptions are few enough to name.

WHAT IS EXEMPT, and why each one is content rather than source:

  - content/loc/tr.json and tools/content/languages/loc_tr*.py: the
    Turkish string table the player reads. Turkish is a shipped game
    language; those VALUES are data.
  - Dish and person names in the other language tables (Lahmacun,
    Hasan Usta): proper nouns, the same in every language.
  - vendor/: third-party files, left exactly as received.
  - Quoted Turkish inside docs: a quotation of the user's own request is
    a record. Quotes are recognised by their Markdown blockquote marker.

Usage:
    python tools/check_english.py
    python tools/check_english.py --list     every violation, not a sample

Exit code 0 clean, 1 something is still Turkish.
"""
from __future__ import print_function

import argparse
import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# Letters that exist in Turkish and in no English word.
TURKISH_LETTERS = set(u"çğıöşü"
                      u"ÇĞİÖŞÜ")

# Turkish written without its diacritics still reads as Turkish. This is
# the vocabulary this project actually used - grown from the inventory,
# not guessed.
#
# WORDS ENGLISH ALSO USES ARE NOT ON THIS LIST: menu, model, panel, son,
# sure, var (a C# keyword). Leaving them on it made the check shout at
# `IsOnMenu` and at ordinary English sentences - and a check that cries
# wolf gets switched off, which is the one outcome worse than not having
# it. `salon` and `moral` DO stay: here they are the domain's Turkish
# (front of house, staff morale) and the rename to `hall` / `morale` is
# exactly what this check is for.
TURKISH_WORDS = set(u"""
asci ascilar salon salonu mutfak mutfagi mutfaklar masa masalar masaya tabak
tabaklar yemek yemekler malzeme malzemeler musteri musteriler personel kadro
kadrosu gun gunu gunluk gunler aksam sabah servis kira ucret maas kasa defter
veresiye kombo hal nisan nisanlar karne mudahale zirve itibar memnuniyet
bozulan yuva kayit sahne oda odalar kamera ekipman istasyon tezgah ocak firin
izgara dolap sandalye kapi pencere duvar zemin yol sokak isik golge renk doku
parca parcalar oyuncu patron garson bulasikci temizlikci kasiyer huy
huylar moral deneyim kidem aday adaylar dugme dugmeler ekran ekranlar serit
kart kartlar kutu kutular satir satirlar sutun baslik metin metinler yazi
dil diller ceviri denetim denetci kontrol olcum olcer tur duman rapor sonuc
hata hatalar uyari tamam kirmizi yesil sayi sayisi deger degerler liste dizi
buyu genislet kucult yaz oku bul ara gez ciz basla bitir kapat yenile tazele
hazirla kosul kural kurallar gerek ayar ayarlar secim karar kararlar huysuz
sabirli hizli yavas titiz cirak tecrubeli dayanikli sakin suratsiz kirli temiz
dolu bos acik kapali yeni eski bekleyen calisan duran adet tane kisi hiz
oran yuzde tutar fiyat maliyet kar zarar gelir gider onceki sonraki ilk
secili gorunur gizli yerlesim olcek boyut genislik yukseklik derinlik konum
aci donus hareket adim yon taraf sol sag ust alt orta merkez kenar kose bolge
alan hacim agirlik yogunluk sicaklik zaman saat dakika saniye hafta ay yil
mevsim icin olan degil yok bir bu ve ile kac nasil neden cunku ama yani
gibi daha cok her hic sonra once kadar yapiyor ediyor oluyor geliyor veriyor
diyor bakiyor aliyor koyuyor cikiyor giriyor kaliyor gecen gecti olur olmaz
varsa yoksa ise diye demek sadece yalnizca ayni farkli butun hepsi bazi
kendi kendisi onun bunun sunun hangi nerede nereye buraya oraya simdi
""".split())

SOURCE_SUFFIXES = (".cs", ".py", ".ps1", ".json", ".md", ".uss", ".uxml")

SKIP_DIRS = {"Library", "Temp", "obj", "bin", "__pycache__", "build", "aab",
             "vendor", "node_modules", "render", ".git", ".vs", ".gradle",
             "Logs", "UserSettings"}

# THIS FILE IS ABOUT TURKISH WORDS, SO IT CONTAINS THEM.
#
# Exempting the checker from itself is the kind of exception that hides
# a real problem, so it is drawn as narrowly as it can be: one path, and
# the only Turkish in it is the vocabulary it matches against. Any other
# file claiming the same exemption would be wrong.
SELF = "tools/check_english.py"

# Paths whose Turkish is the game's Turkish string table, not source.
CONTENT_EXEMPT = (
    "content/loc/tr.json",
    "unity/Assets/Resources/content/loc/tr.json",
    "tools/content/languages/loc_tr.py",
    "tools/content/languages/loc_tr_ui.py",
)

# Proper nouns that are the same in every language.
PROPER_NOUNS = set(u"""
lokanta lahmacun doner iskender manti borek kadayif revani karniyarik
imambayildi cacik piyaz kofte ayran baklava kunefe pide lavas simit
menemen musakka ezogelin yayla tarhana sucuk pastirma kasar
hasan nazife selim rasim guler okan nurten ismail perihan mehmet deniz
burak elif cem melis ozan sevda tolga kaan yagmur usta teyze abla amca
hanim bey dede hoca abi sofor turk turkish adana urfa ankara
""".split())

WORD = re.compile(r"[A-Za-z_][A-Za-z0-9_]*")
CS_COMMENT = re.compile(r"//[^\n]*|/\*.*?\*/", re.S)
PY_COMMENT = re.compile(r"#[^\n]*")
IDENT_SPLIT = re.compile(r"[^A-Za-z]+|(?<=[a-z0-9])(?=[A-Z])")


def relative(path):
    return os.path.relpath(path, ROOT).replace("\\", "/")


def is_exempt(rel):
    if rel == SELF:
        return True
    return any(rel == e or rel.startswith(e) for e in CONTENT_EXEMPT)


def turkish_word(word):
    low = word.lower()
    if low in PROPER_NOUNS:
        return False
    return low in TURKISH_WORDS


def turkish_text(text):
    """Does this line read as Turkish? Returns the evidence, or None."""
    for ch in text:
        if ch in TURKISH_LETTERS:
            return ch
    hits = [w for w in WORD.findall(text) if turkish_word(w)]
    # One hit can be a coincidence: "kar" is half of "kart", "ay" shows
    # up inside a path. Two in one line is a sentence.
    if len(hits) >= 2:
        return " ".join(hits[:3])
    return None


def walk_files():
    for base, dirs, files in os.walk(ROOT):
        dirs[:] = [d for d in dirs if d not in SKIP_DIRS and not d.startswith(".")]
        for name in sorted(files):
            if name.endswith(SOURCE_SUFFIXES):
                yield os.path.join(base, name)


# ---------------------------------------------------------------- pass 1
def check_names():
    bad = []
    for base, dirs, files in os.walk(ROOT):
        dirs[:] = [d for d in dirs if d not in SKIP_DIRS and not d.startswith(".")]
        for name in list(dirs) + files:
            stem = os.path.splitext(name)[0]
            parts = [p for p in re.split(r"[-_. ]+", stem) if p]
            if any(turkish_word(p) for p in parts) or (set(name) & TURKISH_LETTERS):
                bad.append(relative(os.path.join(base, name)))
    return sorted(set(bad))


# ---------------------------------------------------------------- pass 2
def check_identifiers():
    bad = []
    for path in walk_files():
        if not path.endswith((".cs", ".py", ".ps1")):
            continue
        rel = relative(path)
        if is_exempt(rel):
            continue
        try:
            src = io.open(path, encoding="utf-8-sig").read()
        except Exception:
            continue
        src = CS_COMMENT.sub(" ", src)
        src = PY_COMMENT.sub(" ", src)
        src = re.sub(r'"(?:[^"' + chr(92) * 2 + r']|' + chr(92) * 2 + r'.)*"',
                     '""', src)
        src = re.sub(r"'(?:[^'" + chr(92) * 2 + r"]|" + chr(92) * 2 + r".)*'",
                     "''", src)
        seen = set()
        for m in WORD.finditer(src):
            name = m.group(0)
            if name in seen:
                continue
            for part in IDENT_SPLIT.split(name):
                if part and turkish_word(part):
                    seen.add(name)
                    bad.append("%s: %s" % (rel, name))
                    break
    return sorted(set(bad))


# ---------------------------------------------------------------- pass 3
def check_prose():
    bad = []
    for path in walk_files():
        rel = relative(path)
        if is_exempt(rel) or path.endswith(".json"):
            continue
        try:
            text = io.open(path, encoding="utf-8-sig").read()
        except Exception:
            continue
        markdown = path.endswith(".md")
        for n, line in enumerate(text.splitlines(), 1):
            stripped = line.strip()
            if markdown:
                # A quotation of the user's own words is a record.
                if stripped.startswith(">"):
                    continue
                target = line
            else:
                comments = CS_COMMENT.findall(line) + PY_COMMENT.findall(line)
                if not comments:
                    continue
                target = " ".join(c if isinstance(c, str) else c[0]
                                  for c in comments)
            evidence = turkish_text(target)
            if evidence:
                bad.append("%s:%d: %s" % (rel, n, evidence))
    return bad


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--list", action="store_true",
                    help="print every violation instead of a sample")
    args = ap.parse_args()

    names = check_names()
    idents = check_identifiers()
    prose = check_prose()

    print("names       : %d Turkish file or folder names" % len(names))
    print("identifiers : %d Turkish identifiers" % len(idents))
    print("prose       : %d Turkish comment or document lines" % len(prose))

    total = len(names) + len(idents) + len(prose)
    if not total:
        print("result      : the repository is written in English")
        return 0

    limit = None if args.list else 15
    for title, rows in (("NAME", names), ("IDENTIFIER", idents), ("PROSE", prose)):
        if not rows:
            continue
        print()
        for row in rows[:limit]:
            print("%-11s %s" % (title, row))
        if limit and len(rows) > limit:
            print("%-11s ... and %d more (--list)" % ("", len(rows) - limit))

    print()
    print("result      : %d still Turkish" % total)
    return 1


if __name__ == "__main__":
    sys.exit(main())
