# Project rules — Lokanta

## 1. The repository is written in English

**Everything a reader of this repository sees is in English:** folder names, file
names, identifiers, comments, docstrings, commit messages, tool output and
documentation.

Why: this repository is public and English is the language every developer
shares. A reader who cannot read Turkish should be able to follow the code, the
reasoning and the history without a translator.

This rule is **enforced, not remembered**: `python tools/check_english.py` fails
the build on a violation and runs as part of `tools/check.py`.

### What stays in Turkish — and why it is not an exception

Turkish is one of the five languages the **game** ships in. Turkish that is
*game content* is data, not source:

| stays Turkish | because |
|---|---|
| `content/loc/tr.json` | the Turkish string table the player reads |
| `tools/content/languages/loc_tr*.py` | the source of that table |
| dish and person names (`Lahmacun`, `Hasan Usta`) in **all** tables | proper nouns |
| quoted user requests inside `docs/` | a quotation is a record; it is kept verbatim **and** an English rendering follows it |
| `vendor/` | third-party files, left exactly as received |

Everything else — including the *keys* of those tables, the code that reads
them, and the comments explaining them — is English.

A Turkish quotation left alone is a wall for the reader this rule exists for, so
`docs/` keeps the original words and puts the English underneath:

> *"oyuna ingilizce ispanyolca cince ve arapca ekle"*
>
> *("add English, Spanish, Chinese and Arabic to the game")*

Turkish game text quoted as an example works the same way: keep the string the
player sees, gloss it where the point depends on the wording.

### Writing the English

Comments in this repository carry reasoning, not description. Keep that. A
comment says **why**, names the bug that made the line necessary, and quotes the
measurement. Translating one into a shorter, blander sentence loses the thing
that made it worth writing.

Identifiers use the domain's ordinary English: `cook`, `hall` (front of house),
`table`, `plate`, `shift`, `rent`, `wage`, `tab` (credit), `combo`, `regular`
(named customer), `trait`, `morale`, `tenure`, `intervention`, `peak`.

## 2. Content under `content/` is generated

Never hand-edit it. It comes from `tools/balance/export.py`,
`tools/content/gen_*.py`. `SyncContent.Run()` copies it into Unity's
`Resources` at build time, and that copy is generated too.

## 3. The core has no engine and no floating point

`Lokanta.Core` holds the simulation: integers only (`Fx` fixed point), no Unity
references, deterministic. Platform work sits behind a port.

## 4. A check that does not run looks exactly like one that passes

Every guard must be seen to fail. Change the thing it protects, watch it go red,
change it back. A guard argued for in a comment and never executed is not a
guard. Measure the mechanic itself, not a proxy for it, and never compare
numbers taken from two different builds.

## 5. Building

Use `python tools/dotnet_retry.py` for `dotnet` builds — Windows Smart App
Control blocks unsigned DLLs by hash and the retry works around it. **Never turn
Smart App Control off; it is a one-way switch.**

Verification, in order of cost:

```
python tools/check.py            16 checks, 245 core tests
.\tools\unity\tour.ps1           the game plays itself in a real build
```

## 6. Assets must be licensed for commercial release

Every asset carries its licence text and a row in
`unity/Assets/Lokanta/Art/ATTRIBUTION.md`. `tools/check_licenses.py` enforces it.
A second font means a second licence file — "the other one is OFL too" does not
satisfy a licence.
