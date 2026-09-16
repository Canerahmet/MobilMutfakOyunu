# 56 — The repository is written in English

*17 September 2026.* The user's request:

> *"senden onemli bir istegim daha var o da projenin klasor yapisini ve proje
> icerigindeki dosyalari ve kodlari ingilizce olarak yazman, cunku ingilizce
> evrensel bir dil, bu yuzden github attigimiz klasor dosya ve oyun kaynak
> kodlarini ingilzce yazmamiz lazim. bunu bu projenin kurallarina da ekle."*
>
> *("One more important thing: write the project's folder structure, and the
> files and code in it, in English — English is a universal language, so the
> folders, files and game source code we put on GitHub have to be in English.
> Add this to the project's rules too.")*

Two months of work had been written in Turkish: folder names, identifiers,
comments, tool output and fifty-three design documents. All of it is now in
English, and **the rule that keeps it that way is measured on every run**.

---

## 1. The rule first, then the measurement, then the work

`CLAUDE.md` did not exist. It does now, and rule 1 is this one. It also names
what stays Turkish and why, because the exceptions are the part a rule gets
wrong:

| stays Turkish | because |
|---|---|
| `content/loc/tr.json`, `tools/content/languages/loc_tr*.py` | the Turkish string table the player reads |
| dish and person names in **all** tables | proper nouns |
| quoted user requests in `docs/` | a record — kept verbatim, with an English rendering under it |
| `vendor/` | third-party files, exactly as received |

`tools/check_english.py` was written **before** any translating started, because
a rule nobody measures is a preference. Three passes — names, identifiers,
prose — and the baseline it printed defined the job:

```
names       : 253 Turkish file or folder names
identifiers : 860 Turkish identifiers
prose       : 16,515 Turkish comment or document lines
```

It decides "Turkish" by two blunt signals: letters no English word contains, and
a word list grown from an inventory of what this repository actually used. Blunt
on purpose — **a blunt check that runs beats a clever one that does not.**

---

## 2. The checker was wrong five times, and each time it mattered

This is the part worth reading, because the tool that measures the work is the
easiest thing to get quietly wrong.

**It shouted at English.** The first word list carried `menu`, `model`, `panel`,
`son`, `sure`, `once` and `var` — all English words too. It flagged `IsOnMenu`
and ordinary sentences. A check that cries wolf gets switched off, which is the
one outcome worse than not having it.

**It read C# with Python's rules.** Both comment patterns were applied to every
file. `num.ToString("0.##")` contains a `#`, so the Python rule cut that line in
half *inside a string literal* and the string stripper lost its place for the
rest of the file. Words in ordinary literals were reported as identifiers — and,
far worse, real Turkish after that point was **swallowed**. Fixing it moved the
count from 390 to 21: most of the difference was noise, but the tool had been
hiding what it was measuring.

**It did not read docstrings as strings.** Turkish quoted as an example inside a
`"""..."""` came back as an identifier to rename.

**It broke quotations.** `CLAUDE.md` asks for Turkish game text to be kept and
glossed — `` `ui.hud.menu` = "Menü" ``. Flagging those lines pushes the writer
towards paraphrasing the evidence, which is the one thing the documents must not
do. The prose pass now skips quotations, tracks a quote **across lines**, and
treats a fenced block as a quotation by construction.

**Its letter rule was stricter than its word rule.** One Turkish word on a line
was allowed as an example; one Turkish *letter* was not. Now both say the same
thing: **one is an example, two are a sentence.**

---

## 3. What the checks caught that reasoning did not

Renaming is the kind of change that feels safe and is not. The existing checks
earned their keep immediately.

**`os.path.join("Yazi")`.** A textual search-and-replace over path strings cannot
see a path assembled from components. Three checks went red at once: the font
checker could not find Rubik, the generator could not find the language folder,
the licence ledger's folder column no longer matched the folders.

**A content token that was also a name.** The blanket `Salon` → `Hall` rename
changed `public const string SalonPool = "salon"` — **including its value**. The
loader then looked for a pool called `hall` in content that still said `salon`,
found no hall roles at all, and said nothing about it. One golden test caught it.
The fix was to move the token on *both* sides in one change: `export.py` writes
`"pool": "hall"`, `hallRoles`, and the DTO reads them.

**A localisation key inside a string.** The same pass rewrote `"ui.staff.salon"`
in the C# while the table still had the old key. The generator's own dead-key
scan found it.

**Two stale halves of a rename.** `\bSalon\b` does not match `salon_needed` — the
underscore is a word character — so three keys moved on one side only. And the
earlier asset rename had left `ArtPrefabs`, `BuildGameScene`, `FigureShot`,
`IconShot` and `ArtReport` loading `Art/Malzeme`, `Mobilya/…`, `Yemek/…` paths
that no longer existed: the scene would have been built with **no furniture, no
plates and no characters**. Nothing tests those editor paths, so they were found
by reading, not by running.

**A folder the sound loader could not find.** `Resources/ses` became
`Resources/audio` and `Sfx.cs` still asked for `ses/`. File-backed sounds could
never load and the tour's "sound files N/10" had been silently zero.

**A script that overwrites a machine-read contract.** `import_vendor.py` copied
`vendor/ATTRIBUTION.md` over `Art/ATTRIBUTION.md`, treating one as the master.
The two stopped being the same document long ago: only the Art copy carries the
folder mapping table the licence check parses. Running the importer would have
deleted it. Two ledgers that must *both* be right cannot be kept right by
overwriting one with the other — the copy is gone and both are edited by hand.

---

## 4. The contracts that had to move on both sides at once

A name that crosses a language boundary is a contract, and half a rename is
worse than none — it leaves a tool that ignores its argument and carries on.

- **The tour's protocol.** `Autopilot.cs` writes `ok   :` / `FAIL :` /
  `UNMEASURED:` and a `summary.txt` of `passed=`/`failed=`/`unmeasured=`;
  `tour.ps1` greps for exactly those and parses `^failed=(\d+)$`. Renamed in one
  edit and **verified by running**, then mutation-tested: with a deliberate bug
  in place the tour still reports the failure.
- **The editor log greps.** `run.ps1` and `shot.ps1` watch the editor's output
  for `SORUNLAR:` / `yazildi` / `OLCUM`. Translating those messages without the
  patterns would have turned both gates into no-ops that always pass — the exact
  failure [rule 4](../CLAUDE.md) is about.
- **Command-line flags.** `--mutfak`, `--strateji`, `--hizli`, `--zirve`,
  `-Magaza`, `-Yapi`, `-lokanta-tur`, `-lokanta-surum-kodu` and the rest, across
  Python, PowerShell, C# and the documents that tell you to type them. One
  change, 65 occurrences, 20 files.
- **Generated document blocks.** `render.py` writes tables into three documents
  between markers. The marker *name* is the contract, so `ÜRETİLEN: kira` became
  `GENERATED: rent` on both sides — and the generator's target list with them,
  which is what the first run caught by throwing `KeyError: 'kira'`.

---

## 5. The Turkish table moved to where it belonged

`CLAUDE.md` exempts `tools/content/languages/loc_tr*.py`. Those files did not
exist: the Turkish table lived **inside** `gen_loc.py`, mixed with the generator
logic, so the exemption pointed at nothing while the Turkish sat in a file that
was not exempt.

Turkish now has two files like the other four languages, and `gen_loc.py` is
translatable like everything else. The proof that nothing shifted is the only one
worth having: all five `content/loc/*.json` are **byte-identical** before and
after.

One guard had to follow the tables or it would have stopped guarding silently:
`_no_duplicates()` scanned its own file, which no longer holds any table. It now
scans all ten language modules — which also widens it, since a duplicated key
could always have happened in any language and was invisible in four of them.

---

## 6. What it cost and what it came to

Nine agents did the translating, one area each, with the contracts written into
their briefs. Their reports are why this document can be specific: each came back
with what it had deliberately left alone and why, and three of them found bugs in
their own area that had nothing to do with language.

| | before | after |
|---|---:|---:|
| Turkish file and folder names | 253 | **0** |
| Turkish identifiers | 860 | **0** |
| Turkish comment and document lines | 16,515 | **0** |

```
written in English       OK  result      : the repository is written in English
```

`tools/check.py` now runs **sixteen** checks, and that is the sixteenth. 245 core
tests pass; the tour plays the game to the end of the campaign with 177 checks
and no failures.

---

## What is still worth doing

- **A native reader for the English.** The same gap the five languages have
  ([55](55-translation-review.md)): the prose was written carefully and checked
  mechanically, but nobody has read it as a reader.
- **`tools/balance/out.md` is not valid UTF-8.** A generated artefact that no
  tool can read; it should be regenerated or deleted.
- **The checker cannot see a Turkish word it has never met.** The list is grown
  from an inventory, not from the language. It will miss a word nobody has used
  yet — the letters catch most of that, but not a word written without them.
