# 57 — An end-to-end audit: what the documents promised and the code did not do

*17 September 2026.* The user's request:

> *"oyunu farkl iagentlar ile bastan sona kada rkontro let, dokumanlarda olup da
> atladigimiz bir sey var mi?"*
>
> *("check the game end to end with different agents; is there anything in the
> documents that we skipped?")*

Five agents read the whole thing against the fifty-six documents — the
simulation, the view layer, the interface and localisation, the tour and its
measurements, and release readiness. None of them changed a file.

The answer to the question is **yes**, and the findings sort into a shape this
project has met before. Almost none of them were "a feature is missing". They
were **a mechanism that runs and reaches nobody**, **a measurement that moved
without anybody moving it**, and **a document that describes a decision that has
since been reversed**. Each section below is one of those, with what it cost.

---

## 1. The worst one: the busy slot was the empty slot

`Simulation.InPeakSlot()` returned the **longest** slot of the day.

That was correct when it was written. [48](48-day-sharpness.md) then sharpened
the day — and sharpening it meant making the busy slot **short and dense** and
the quiet slots long and thin. From that commit onward the longest slot was the
**emptiest** one, and nothing said so.

Measured arrival density per slot, guests per hundred ticks:

| cuisine | slot 0 | slot 1 | slot 2 | slot 3 | longest | densest |
|---|---:|---:|---:|---:|---:|---:|
| fastfood | 0.54 | **1.93** | 0.47 | 1.61 | 2 | **1** |
| turk | 0.98 | **1.90** | 0.45 | 0.97 | 2 | **1** |

In both cuisines the longest slot is slot 2, which is the **quietest** one.

**What it cost.** `InPeakSlot()` feeds the staff traits. Two of the twelve —
`kalabalikta_panikleyen` (panics in a crowd) and `sakin` (calm under pressure) —
were therefore running **exactly inverted**: the panicker held together through
the rush and fell apart in the lull, and the calm one the other way round. Both
traits are on the hiring card, both are priced into the pool's balance, and
neither behaved as its own description.

**The fix and the test.** `PeakSlotToday()` now takes the densest slot from the
day's own arrival plan, cached per day. The regression test does not check a
number; it checks the *relationship*:

```csharp
Assert.NotEqual(longestSlot, sim.PeakSlotIndex);
```

A test that asserted "the peak is slot 1" would have gone green again the next
time the day's shape moved. This one goes red.

**And making `PeakSlotIndex` public found a second bug in the same method.** The
answer is cached per day; the arrival plan is built by `OpenService` and cleared
by the day advance. So in the MORNING every slot holds zero guests, the loop
picks whichever comes first, and the cache pins slot 0 as the day's rush for the
whole of it. Nothing read it that early - and a public property is exactly how a
caller like that turns up. It now returns -1 before service and caches nothing,
with an arm in the same test that asks for it.

---

## 2. Buying a useless station raised the score

`RemainingPurchaseCostTotal()` is the denominator of the WEALTH axis: how much
of the equipment ladder is still unbought. It counted **every** station in the
economy table, including the ones a cuisine never uses — the Turkish stone oven
in a fast food restaurant, for instance.

So the denominator was inflated by a ladder the player could never usefully
climb, and `BuyEquipment` would happily sell them a step of it. A purchase that
does nothing at all **raised** the year-end score.

Both sides are closed: the cost walk skips a station the cuisine does not use
(`if (!IsStationUsed(i)) continue;`) and `BuyEquipment` rejects the purchase
with reason 4.

---

## 3. The save migration test slid forward with every release

`SaveTests.An_old_version_save_opens` built a current save, deleted the fields
added in the newest version and stamped `Simulation.SaveVersion - 1` on the
header.

It passed. It has always passed. And because the version it tested was written
as *an offset from the current one*, the test **moved forward with every
release**: at `SaveVersion = 22` it tested 22 → 21, and it had never once loaded
a version 20 save. `MinReadableVersion = 20` — the only number `Restore`
actually enforces — was a claim **no test backed**.

It now walks a table of what each version added and tests **every** version in
the readable range. Mutation-verified: with the version gate forced open
(`if (version >= 21)` → `if (true)`), the `version: 20` arm fails with
`Field missing from the save: badges` and the `version: 21` arm stays green —
which is the difference the old test could not see.

The second test is the one that matters longer:

```csharp
[Fact] public void Every_readable_version_is_covered()
```

It goes red the moment `SaveVersion` moves without a row being added to the
table. Without it, the new test is only as wide as somebody remembered to make
it — which is how the old one got into this state.

---

## 4. The save had one copy, and "atomic" is not "readable"

`SaveStore.WriteAtomic` was genuinely atomic: the data is forced to disk with
`fs.Flush(true)` and swapped in with a single rename, so a crash can never leave
half a file. The comment explaining that is correct and was written after a real
bug.

What it does not do — what it *cannot* do — is guarantee that the file it wrote
is a save worth having. Storage that lies about a flush, a truncating file
system, a bug in `Write`: each of those installs a **complete**, atomically
correct, unopenable save over a good one. `File.Replace(tmp, path, null)` was
throwing away the old file, and the third argument is where to keep it.

There is now one generation of backup per slot and `Load` falls back to it. One
generation is the honest promise: two saves after the damage the backup is
damaged too.

**It is measured in the tour**, because it lives in `Lokanta.Game` where the
core test project cannot reach it and it only means anything against real files
on a real disk. The tour saves, ticks the simulation sixty times by hand, saves
again, **tears the state file on purpose**, and loads:

```
ok   : The two saves differ - without this the backup check is empty
ok   : A torn save still opens - the backup is read
ok   : And it is the PREVIOUS save, not the torn one
```

The first line is the one that keeps the other two honest. The simulation is
ticked by hand rather than left to the clock because whether the clock is
running at that point in the tour depends on the pause state, the speed key and
whether the service has finished — and if the two saves come out identical, "it
opened the previous one" cannot be told from "it opened the torn one".

---

## 5. The signature mechanic had no moment

`SimEventKind.ComboOrdered` is emitted on every combo sold. Nothing listened to
it — not a sound, not a notice, not a badge. It was the last dead entry in
`GameApp`'s event switch.

The combo is not invisible: the share is an axis on the weekly report card and
at year end. But the **moment** was, and the moment is what teaches a player
that the toggle they flicked is doing something. Fast food's signature mechanic
gave no feedback at all between flicking the switch and the evening.

It now plays `kombo` — two notes up a fourth, short, and deliberately quieter
and shorter than `para` (the same guest pays a second later) and `seviye` (three
notes, a much bigger event).

---

## 6. Three sounds existed and both lists of sounds still said "ten"

`uyari` and `bitti` were added to `Sfx.cs` in an earlier round, `kombo` in this
one. `Art/ATTRIBUTION.md`'s audio table and `Resources/audio/README.md` both
still listed ten names.

That is not cosmetic. **Those lists are the brief.** `Sfx.Init` looks for a file
first and falls back to a synthesised tone, which is the right design and also
the reason a gap here is silent: a sound missing from the lists is a sound
nobody will be asked to record, and the game ships on the fallback for ever —
with the tour counting a complete sound.

`check_licenses.py` now reads the names out of `Sfx.cs` and fails if either list
has fallen behind. Both arms mutation-tested.

One detail worth keeping: the first version of that check printed
`sounds  13 names, ledger and README agree` on the same run as the `MISSING:`
line saying they did not. A report that contradicts itself is worse than no
report, because the eye reads the summary row and not the list.

---

## 7. Four Turkish names survived the English rename

[56](56-english-repository.md) moved the repository to English and
`check_english.py` reported it clean. It was not:

| was | is |
|---|---|
| `tools/android/cihaz.ps1` | `tools/android/device.ps1` |
| `tools/content/loc_tarama.py` | `tools/content/loc_scan.py` |
| `tools/art/out/zh_karakterler.txt` | `tools/art/out/zh_characters.txt` |
| `tools/art/out/unity/floor_04_dar_*.png` (44 files) | `..._narrow_...` |

The check's name pass does exactly the right thing — it splits a file name into
words and asks the Turkish vocabulary — and the vocabulary did not have `cihaz`,
`tarama`, `karakterler` or `dar`. **A word list can only find what somebody
thought to put in it.**

The last one had also produced a **duplicate that disagreed with itself**. The
rename moved the Chinese character list to `zh_characters.txt`;
`subset_font.py` still wrote `zh_karakterler.txt`, with a comment explaining
that an artefact path is not source and may stay as it is. The next run wrote
the old name back, and the repository then carried two character lists, four
characters apart, with **nothing reading the renamed one**.

The four words are on the list now, and so is the honest note that the list is
the check's ceiling. What actually found these was not the check but a one
minute sweep: split every tracked path on the slash, sort the distinct name
tokens, read them.

The extended vocabulary immediately found one more line, and that one turned out
to be **correct**: `ArtPrefabs.cs` records that the asset folders *"used to be
Mobilya, Karakter and Yemek"* — the single sentence explaining why a key left
behind by that rename would silently scale every model to 1. The markdown branch
of the check already strips quoted text before testing, because
[CLAUDE.md](../CLAUDE.md) keeps Turkish quoted as evidence and glossed in
English; a code comment has no blockquote to mark that with, so the quotation
marks are all there is. The comment branch now strips double-quoted text the
same way. Mutation-tested: an unquoted Turkish sentence in the same file is
still caught.

---

## 8. The change log had stopped six days earlier

[06-plan-status.md](06-plan-status.md) is the project's index of its own
history. Its last entry was 11 September. Twenty-two documents and six days were
missing — the whole view layer beyond its first slice, the street and the
lighting, the interface redesign, the plate cycle, self service, five languages,
the English repository, and every review round since.

Worse than missing: **wrong**. The header still said *"implementation will not
start before this list is finished"*. The status table still called the Unity
view layer a first slice. And a section headed *"The save migration chain: a
deliberate deferral"* still explained why the chain would not be written —
five days after it was written, tested and shipped.

A stale log is a slow failure and this is the mechanism: every one of those
sentences was true when written, and nothing in the repository disagreed with
them out loud.

All twenty-two rounds are in the log now, the two closed deferrals say they are
closed, and the header says the planning phase is over.

---

## 9. What was found and deliberately **not** changed

**Guest wardrobe.** All 32 archetypes carry a `wardrobe` tag and nothing reads
one. `tools/audit_content.py` had it in the "deliberately not read" table with
the reason *"the art pipeline, outside the core (docs/24)"* — and that reason had
gone stale: the art pipeline is no longer a future thing, `Game/Wardrobe.cs`
dresses the **staff** by role and the tour counts the dressings. What it does not
dress is the guests. The field stays unread, and the reason now says which half
is missing, so a closed pipeline cannot keep excusing an open field.

**The privacy policy's contact address.** [44](44-store-texts.md) still carries
`<email address>` in all five languages. That is a placeholder on purpose: it is
the user's decision whether a personal address or a dedicated one goes on a
public store page.

---

## 10. The two pre-release gates were stale

[21](21-business-and-release.md) lists two runs that are not part of the daily
check and that a shipped binary must not go out without:

```
.\tools\unity\tour.ps1 -Build windows-il2cpp   # the stripping exam, no device needed
.\tools\android\device.ps1                     # a real phone, ARM64
```

The IL2CPP build in `build/` was from **14 September** and the APK from
**15 September**, with more than eighty files changed since — including
`link.xml` and the whole DTO layer, which is exactly what the first gate exists
to catch.

This is not a bug, it is the gates doing their job: they are stale *by design*
between releases, and the only failure would be shipping without re-running
them.

---

## The state after the round

```
python tools/check.py              16/16 clean, 249 core tests
tour.ps1 -Cuisine fastfood         179 passed, 0 failed, 5 unmeasured
```

The tour was 175 checks before; the four new ones are the torn-save arms, and
they pass against the built game, not against a unit test's idea of a disk.

The five unmeasured lines are all fast food's, and all of them honest: the tab
is the TURKISH signature mechanic, so "the tab was opened during service" and
"the tab book was measured" have nothing to measure here. Those two used to
**vanish** from a fast food run rather than report - the flags were set and never
read, so the summary said 0 unmeasured while four checks quietly went missing.
They say so now. That is the whole difference between a check that is skipped and
a check that is silent.

---

## What this round is really about

Six of the eight findings above share one property: **the code was correct when
it was written, and something else moved.** The day's shape moved and inverted
two traits. The release number moved and slid a test off the version it claimed
to cover. A sound was added and two lists stayed still. The migration chain was
written and its deferral note stayed still.

The project's standing rule — *a check that does not run looks exactly like one
that passes* — has a sibling, and this round is what named it:

> **A check that runs can stop looking at the thing it was aimed at, and nothing
> about it changes on the day it does.**

The defence is the same in every case above: assert the **relationship**, not the
number. `PeakSlotIndex != longestSlot`. `Every_readable_version_is_covered`.
The sound names read out of `Sfx.cs` rather than typed into a document. The
`removed > 0` line in the migration test. None of those can be satisfied by a
stale constant.
