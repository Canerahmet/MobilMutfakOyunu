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
# sure, once (Turkish 'before'), var (a C# keyword). Leaving them on it made the check shout at
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
gibi daha cok her hic sonra kadar yapiyor ediyor oluyor geliyor veriyor
diyor bakiyor aliyor koyuyor cikiyor giriyor kaliyor gecen gecti olur olmaz
varsa yoksa ise diye demek sadece yalnizca ayni farkli butun hepsi bazi
kendi kendisi onun bunun sunun hangi nerede nereye buraya oraya simdi
uretilen dosya elle degistirmeyin calistirin kosturun uretec uretecler
cihaz cihazlar tarama taramasi karakter karakterler dar genis yuva
""".split())

# THE LIST IS THE CHECK'S CEILING, AND IT WAS SHORT.
#
# Four Turkish names survived the repository's English rename for exactly one
# reason: the words were not on this list, and `check_names` splits a file name
# into words and asks this set. It reported nothing, so the rename looked
# complete.
#
#     tools/android/cihaz.ps1          -> device.ps1
#     tools/content/loc_tarama.py      -> loc_scan.py
#     tools/art/out/zh_karakterler.txt -> zh_characters.txt
#     tools/art/out/unity/floor_04_dar_*.png (44 files) -> _narrow_
#
# The last one had also produced a DUPLICATE: the rename moved the character
# list, `subset_font.py` still wrote the old name, and the repository carried
# two lists that disagreed while nothing read the renamed one.
#
# The lesson is not "add four words". It is that a word list can only find
# what somebody thought to put in it, so this pass is a net with a known mesh
# size - it catches the Turkish this project actually writes and it cannot
# promise more. What closes the gap for good is not the list but the SWEEP:
# splitting every tracked path on the slash and reading the distinct name
# tokens. That is how these four were found, and it takes a minute.

# THE LIST IS THE BLIND SPOT.
#
# It was (.cs .py .ps1 .json .md .uss .uxml) and the check reported the
# repository fully English while three files sat outside it, entirely in
# Turkish: unity/Assets/link.xml (the IL2CPP stripping guard, with a
# stale command in it), src/Lokanta.Core/Lokanta.Core.csproj, and the
# `_comment` line written into four generated content files.
#
# A gate is green over what it does not open. That is this project's
# oldest lesson, and it turned up inside the tool that exists to enforce
# the rule against it.
SOURCE_SUFFIXES = (".cs", ".py", ".ps1", ".json", ".md", ".uss", ".uxml",
                   ".xml", ".csproj", ".slnx")

# FILES WITH NO EXTENSION AT ALL, read by exact name.
#
# The suffix list is how this check decides what to open, and the files that
# CONFIGURE the repository have no suffix to match. `.gitignore` was written
# in Turkish from the first commit - four section headings and three
# paragraphs of reasoning - and every run of this check reported the
# repository fully English, because it never opened the file.
#
# It cost more than tidiness. The English rename moved `render/magaza` to
# `render/store` and the un-ignore line under `render/*` still said
# `!render/magaza/`, so for a day the store screenshots - the ones the
# comment three lines above calls a release asset - were silently ignored
# and stopped being committed. An ignore rule fails by doing nothing.
#
# The list is exact names rather than a pattern: a dotfile sweep would pull
# in `.gitattributes`, editor state and whatever else lands at the root, and
# a check that shouts at things nobody edits gets switched off.
SOURCE_NAMES = (".gitignore", ".gitattributes", ".editorconfig")

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
    # The Play Store listing in five languages, plus five privacy policies.
    # Published product text, the same class as the string table above: the
    # Turkish in it is what a Turkish shopper reads, not prose about the
    # project. tools/content/check_store_texts.py parses this file and
    # enforces Google's character limits on it.
    "docs/44-store-texts.md",
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

# Proper nouns as they are actually spelled, diacritics and all. The
# ASCII list above is what identifiers use; this one is what prose uses.
PROPER_NOUNS_TR = set(u"""
lahmacun döner işkembe çorbası mantı börek kadayıf karnıyarık
imambayıldı cacık köfte ayran künefe güveç ıspanak
ayşe İbrahim yağmur sıla şerife pınar şaban rıza şükran hakkı aslı
tarık şevval uğur barış tuğçe niğde Şanlıurfa
""".split())

WORD = re.compile(r"[A-Za-z_][A-Za-z0-9_]*")
CS_COMMENT = re.compile(r"//[^\n]*|/\*.*?\*/", re.S)
XML_COMMENT = re.compile(r"<!--.*?-->", re.S)
XML_SUFFIXES = (".xml", ".csproj", ".slnx", ".uxml", ".uss")
PY_COMMENT = re.compile(r"#[^\n]*")
IDENT_SPLIT = re.compile(r"[^A-Za-z]+|(?<=[a-z0-9])(?=[A-Z])")
# Inline code spans and quoted strings inside Markdown.
MD_CODE = re.compile("`[^`" + chr(10) + "]*`")


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
    """Does this line read as Turkish? Returns the evidence, or None.

    ONE TURKISH WORD IS AN EXAMPLE; TWO ARE A SENTENCE.

    The same threshold is used for both signals, and for the same
    reason. A line that names one Turkish string is almost always an
    English sentence quoting the game - "the label said `Hizlandir`" -
    and CLAUDE.md asks for exactly that. A line carrying two is prose
    nobody translated.

    Single characters do not count as words at all: `(g-breve, dotless
    i, S-cedilla)` is a list of letters being discussed, not Turkish.
    """
    marked = []
    for w in text.split():
        bare = w.strip(".,;:!?()[]{}*`\"'…’")
        if len(bare) < 2 or not (set(bare) & TURKISH_LETTERS):
            continue
        # A proper noun is a proper noun with its diacritics on, too.
        if bare.lower() in PROPER_NOUNS_TR:
            continue
        marked.append(bare)
    if len(marked) >= 2:
        return " ".join(marked[:3])

    hits = [w for w in WORD.findall(text) if turkish_word(w)]
    # One hit can be a coincidence: "kar" is half of "kart", "ay" shows
    # up inside a path. Two in one line is a sentence.
    if len(hits) >= 2:
        return " ".join(hits[:3])
    return None


def strip_quotes(line, in_quote):
    """Removes quoted text, ACROSS LINES, and says whether a quote is open.

    A quotation of game text often wraps:

        …says "SERVIS SIRASINDA SEN VARSIN" ("SERVICE IS WHERE YOU ARE",
        and the four decisions follow).

    Testing each line on its own flagged the second half of every wrapped
    quotation as untranslated prose, which pushes the writer towards
    paraphrasing the evidence - and the evidence is the whole point of
    quoting the string. So the quote is tracked from line to line.
    """
    out = []
    i = 0
    while i < len(line):
        ch = line[i]
        if in_quote:
            if ch in (u'"', u"”"):
                in_quote = False
            i += 1
            continue
        if ch in (u'"', u"“"):
            in_quote = True
            i += 1
            continue
        out.append(ch)
        i += 1
    return "".join(out), in_quote


def walk_files():
    for base, dirs, files in os.walk(ROOT):
        dirs[:] = [d for d in dirs if d not in SKIP_DIRS and not d.startswith(".")]
        for name in sorted(files):
            if name.endswith(SOURCE_SUFFIXES) or name in SOURCE_NAMES:
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
        # THE COMMENT SYNTAX FOLLOWS THE LANGUAGE, NOT BOTH AT ONCE.
        #
        # Both patterns used to be applied to every file. In C# that is
        # wrong and quietly harmful: `num.ToString("0.##")` contains a
        # `#`, so the Python rule cut the line in half INSIDE a string
        # literal and the string stripper lost its place for the rest of
        # the file. Words in ordinary literals were then reported as
        # identifiers - and, far worse, real Turkish after that point
        # could be swallowed. A measuring tool that hides what it is
        # measuring is the worst kind.
        if path.endswith(".cs"):
            src = CS_COMMENT.sub(" ", src)
        else:
            src = PY_COMMENT.sub(" ", src)
        # DOCSTRINGS ARE STRINGS TOO.
        #
        # The pass strips string literals so that a Turkish word quoted
        # as an example is not reported as an identifier. It stripped
        # single-quoted strings only, so the same example inside a
        # triple-quoted docstring - `ui.evening.wages ("Ucret")` - came
        # back as an identifier the author was told to rename.
        src = re.sub(r'"""(?:.|' + chr(10) + r')*?"""', '""', src)
        src = re.sub(r"'''(?:.|" + chr(10) + r")*?'''", "''", src)
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
        if is_exempt(rel):
            continue
        try:
            text = io.open(path, encoding="utf-8-sig").read()
        except Exception:
            continue
        markdown = path.endswith(".md")
        in_quote = False
        # A FENCED BLOCK IS A QUOTATION BY CONSTRUCTION.
        #
        # It holds code, data, or a program's own output - never prose.
        # The transcripts in docs/43 and docs/50 record what the tour
        # printed when it still printed Turkish; asking an author to
        # translate a transcript is asking them to falsify evidence.
        in_fence = False
        for n, line in enumerate(text.splitlines(), 1):
            stripped = line.strip()
            if markdown and stripped.startswith("```"):
                in_fence = not in_fence
                continue
            if markdown and in_fence:
                continue
            if markdown:
                # A quotation of the user's own words is a record.
                if stripped.startswith(">"):
                    continue
                # SO IS A QUOTED GAME STRING.
                #
                # CLAUDE.md says Turkish game text quoted as an example
                # stays, glossed in English - `ui.hud.menu` = "Menu",
                # **"Temizlikci"** ("Cleaner"). The Turkish in those lines
                # is the string the PLAYER reads; the sentence around it
                # is English. Marking the whole line as untranslated would
                # push the writer to paraphrase the evidence, which is the
                # one thing the document must not do.
                #
                # So the test runs on what is LEFT after the quotations:
                # inline code spans and quoted strings come out first.
                target = MD_CODE.sub(" ", line)
                target, in_quote = strip_quotes(target, in_quote)
            elif path.endswith(".json"):
                # A GENERATED FILE SAYS SO IN ITS OWN LANGUAGE.
                #
                # Content JSON is ids and numbers, and many ids are
                # Turkish on purpose (`ocak`, `kuru_fasulye`) - scanning
                # every value would drown the check. The one place prose
                # lives is the `_comment` line every generator writes at
                # the top, so that is the one place read.
                if '"_comment"' not in line:
                    continue
                target = line
            elif path.endswith(XML_SUFFIXES):
                # THE WHOLE LINE, not the comment markers on it.
                #
                # Pulling `<!-- ... -->` out per line finds nothing on
                # the INNER lines of a multi-line comment - and that is
                # the shape link.xml's Turkish is in, so the first
                # version of this branch reported the file clean. XML
                # here is directives and prose; element and attribute
                # names are English anyway, so the line is read whole.
                target = line
            elif os.path.basename(path) in SOURCE_NAMES:
                # A CONFIG FILE IS COMMENTS AND PATHS. The paths are names on
                # disk and `check_names` already covers those, so only the
                # `#` comments are prose here - the same rule as a .py file,
                # named separately because these have no extension to match.
                comments = PY_COMMENT.findall(line)
                if not comments:
                    continue
                target = " ".join(c if isinstance(c, str) else c[0]
                                  for c in comments)
            else:
                comments = (CS_COMMENT.findall(line) if path.endswith(".cs")
                            else PY_COMMENT.findall(line))
                if not comments:
                    continue
                target = " ".join(c if isinstance(c, str) else c[0]
                                  for c in comments)
                # A QUOTED NAME IN A COMMENT IS A RECORD, exactly as it is
                # in a document.
                #
                # The markdown branch already takes quoted strings out
                # before testing, because CLAUDE.md keeps Turkish quoted as
                # evidence and glossed in English. A code comment has no
                # blockquote to mark that with, so the quotation marks are
                # all there is - and ArtPrefabs.cs needs them: it records
                # that the asset folders "used to be Mobilya, Karakter and
                # Yemek", which is the one sentence that explains why a
                # stale key would silently scale every model to 1. Asking
                # the writer to translate those three words would delete
                # the evidence the comment exists for.
                #
                # Only DOUBLE quotes count. Turkish uses the apostrophe as
                # a suffix mark, so treating it as a quote would swallow
                # whole clauses.
                target, _ = strip_quotes(target, False)
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
