# 53 — The pending decisions closed: dishwasher, combo, the staff's voice

*15 September 2026.* The user handed over the pending design decisions:

> *"Tüm bu bekleyen kararlar için farklı agentlar ile ayrı ayrı düşün ve en iyi
> yolu kendin uygula. Repliklerin doğal olmasına dikkat et, bunun için
> internetteki kaynaklardan faydalanabilirsin. Bulaşıkçı için de benzer oyun
> varsa oradaki mekanikleri ek olarak incelemek isteyebilirsin."*
>
> *("For all these pending decisions, think about each one separately with
> different agents and implement the best path yourself. Take care that the lines
> sound natural — you can draw on sources on the internet for that. For the
> dishwasher, if there is a similar game you may also want to look at the
> mechanics there.")*

Two rounds of research ran (the mechanics of twelve shipped games; game writing
and a record of Turkish restaurant/chain speech), and then measurement. **The real
subject of this document is the place where the measurement misled me twice.**

---

## 1. The "improvement" I measured was not there — the builds were different

[49](49-unreachable-mechanics.md) had written that the dishwasher is a dominated
option: waiting with no plate, `planci` 263 against `bulasikci` **349**.

I wrote two changes and saw **349 → 275 → 229** over 12 seeds. It was a clean
improvement story and it was **wrong**: the number 349 came from docs/49, that is,
from a build *before* self service, before the sharpening of the day, before the
rent and before the washing speed. I had never measured the "before" on my own
build.

I measured it. Same build, 32 seeds:

| strategy | HEAD | crisis handover | + idle-return |
|---|---:|---:|---:|
| `makul` | 255 | **240** | 240 |
| `planci` | 179 | 180 | 180 |
| `imzaci` | 193 | **186** | 186 |
| **`bulasikci`** | **198** | **197** | **216** |

Two things came out at once:

**(a) The open decision had closed itself.** In today's build the dishwasher is
198 against 179 — nothing like the 349-against-263 chasm docs/49 wrote down. The
changes in between (self service, the sharp day, the specialist's washing speed)
have already largely dissolved the problem. *Setting out to solve an open decision
without re-measuring it is treating a disease that is not there.*

**(b) My "fix" was making it worse.** The idle-return arm took the dishwasher from
197 to 216.

---

## 2. The research was right, but mine was not the case

Twelve games were swept (RimWorld, Dwarf Fortress, Two Point Hospital, PlateUp!,
Overcooked, Prison Architect, Tavern Keeper, Supermarket Simulator…). The finding
is unambiguous: **in no shipped game does hiring a specialist silently switch off
the general pool.** The exclusion is either a mode the player chooses (Dwarf
Fortress's *Everybody / Nobody / Only selected* trio), or a checkbox, or it sits
between coarse staff classes. Its name in queueing theory is **de-pooling**: a
separate queue is worse than a shared pool.

Supermarket Simulator shipped exactly this mistake: sacking the cashier and
working the till yourself is more profitable, and the forum's most repeated
request is "let the idle staff help".

I did the same — and **the measurement said no.** Two attempts, both reasonable:

| attempt | rationale | result | scale |
|---|---|---:|---|
| return to the hall when there is nothing to wash | close the de-pooling | 197 → **216** | 32 seeds, against HEAD |
| do not wash until the pile passes a threshold | do not send the specialist to the sink for a single plate | 229 → **293** | 12 seeds, **not the same scale** |

The second row only shows the **direction**; the figure 229 comes off the end of
the 12-seed chain refuted in §1 and cannot be compared with the 32-seed table.
Putting them side by side in the same table would have been a repeat of the error
this document complains about — hence the scale column.

Both break the same thing: **the specialist's whole value is in washing without a
break and immediately.** A dishwasher who returns to the hall is tied to a customer
task when a plate gets dirty, and comes back to the sink late. A dishwasher put on
hold, meanwhile, grows the pile.

The user's rule (*"bulaşıkçı alınca herkes kendi işini yapar"* — "once you take on
a dishwasher everybody does their own job") stands — but now **as a measurement,
not as an assumption.**

*However solid a research finding is, applying it without measuring that it holds
in your case is writing a protection by reasoning.*

---

## 3. The one change that remained and was kept: the hall goes to the sink when the kitchen stalls

When the clean plates run out the plate-filling loop **stops completely**: cooked
food waits on the counter. At that moment a waiter taking a new order is worthless
work.

That branch now comes **before** customer work.

The same idea was tried in docs/49 and gave 351 → 351. The reason: the exception
was written *after* customer work and looked for a "free person"; because the hall
is full at the peak it never fired. **Looking for a free person was the wrong
question** — the right question is "is the work being done right now worth
anything".

### My first version had three faults; the critique round found them

1. **The flag latched.** `PlateUp()`'s "no cooked party" exit did not clear
   `_plateStalled`. When the blocked party got impatient and left (`LeaveAngry`
   does not zero `_pCooked`) the flag stayed stuck, the hall locked into the crisis
   branch, and because nobody was taking orders no new cooked party formed either —
   the flag was feeding itself. Worse: in that state **the only yardstick I was
   reporting falls**, because the departing customer takes the blockage with them.
   The measurement was blind to its own worst case.
2. **There was no threshold.** Five servers went to the sink for a single dirty
   plate, four of them coming back empty-handed.
3. **The counter was being muddled.** Crisis washes were being added to
   `_hallRushWashes` — the counter that carries `PlateTests`'s dishwasher claim. The
   very mixing its own comment forbids.

All three were fixed; the flag is now a **derived value** recomputed every tick, so
it need not enter the save or the determinism hash.

### After the fix, every axis (32 seeds, Turkish cuisine)

| strategy | waited, no plate | parties served | end cash | angry |
|---|---:|---:|---:|---:|
| `makul` | 255 → **240** | 1883 → 1883 | 18,439 → 18,442 | 7 → 7 |
| `planci` | 179 → **178** | 2567 → 2552 | 23,443 → 23,490 | 16 → 16 |
| `imzaci` | 193 → **186** | 1849 → 1849 | 15,074 → 15,075 | 7 → 7 |
| `bulasikci` | 198 → **195** | 2547 → 2534 | 23,607 → 23,749 | 15 → 15 |

No strategy is now behind HEAD. But two things remain honest:

- **The cost shows up in parties served:** `planci` −15, `bulasikci` −13 parties
  (0.6%). That is what pulling a server off customers costs. The trade was accepted
  because the till still went up, but saying there is no trade would be wrong.
- **The distribution was not measured.** There is no stddev and no confidence
  interval; small differences cannot be told from noise.

I gave this table with a single column in my first version. *Measuring the axis I
improved and not measuring the one I put at risk is the very thing this document
complains about.*

RimWorld's **fire** behaviour is exactly this pattern: a rare, heavy, sweeping
condition overrides normal priority.

---

## 4. The combo: self service inverted the answer

[45](45-design-review.md) §18 had written that the combo axis "protects a strategy
that does not exist": `zirvede_kapat` and `imzaci` were serving **exactly the same
2016 parties**, because the combo's kitchen load did not bite.

That measurement was **from before self service.** Once fast food's hall load
halved, the bottleneck moved into the kitchen. Re-measured:

**12 seeds, fast food, 60 days — and measured with the crisis branch absent:**

| strategy | end cash | served | combo% | waited, no plate |
|---|---:|---:|---:|---:|
| `makul` (no combo) | 22,492 | 2617 | 0.0% | 846 |
| `imzaci` (always on) | 22,163 | 2559 | 18.1% | 1018 |
| **`zirvede_kapat`** | **23,474** | 2564 | 16.8% | **792** |

Two warnings, both following from this document's own thesis:

- **12 seeds.** §1 describes exactly how a 12-seed result was refuted at 32. This
  table is exposed to that risk and should not count as closed until it is
  re-measured.
- **The no-plate column cannot be compared with §1's** (846 vs 240): a different
  cuisine, a different seed count, and the crisis branch not yet present.

The combo now **costs 58 parties** and closing at the peak beats keeping it on all
the time by **+1,311** (at this scale +5.9% and −2.2%; the distribution was not
measured).

The **"it tires the kitchen"** half of the promise is supported (no-plate 1018 >
846). For the **"it raises the average ticket"** half there is no column in the
table — back-computing from the till gives +0.8%, but end cash is after costs, so
it is not the ticket. So half of what I called "right for the first time" is still
unmeasured.

**The balance was not touched** — the written band (`imzaci/makul` 90–130%) is
already met: 98.5% (always on) and 104.4% (close at the peak).

**The axis stays flat too, and that is no longer a shortcoming:** the good game's
share is *lower* (16.8% < 18.1%). No ratio-based target can fix that; raising the
target would reward the worse player.

### But the button was never pressed

The combo button was in `GameScreen` and **the tour never pressed it.** Exactly the
same empty coverage has come up twice in this project (`SetQuality`,
`CollectCredit`): the mechanic complete in the core, a button on the screen, nobody
measuring that the command gets through.

On top of that the combo is now **the only door to the game's best game.** Had the
command not got through, that game could not be played and nothing would have said
so. The tour now presses in both directions: turns it on, verifies, turns it back.

---

## 5. The staff's voice: twenty regulars had sixty lines, the staff had zero

| | regular | staff (before) |
|---|---|---|
| string keys | 5 (name, job, 3 beats) | **0** |
| on screen | a full-screen evening beat | name + role + two dry labels |

The only semi-narrative staff string in the whole game was `ui.staff.inherited`.

### What was built: the voice of the trait

The research's highest-leverage finding was Two Point Hospital's structural trick:
**the mechanical name and the human sentence are two separate strings.** The
mechanic `Cheap`, the text *"Will work for peanuts"*.

`trait.<id>.desc` goes on describing the mechanic ("slows down in the last quarter
of the day"). The new `trait.<id>.voice` describes the person:

```
Çabuk Yorulan   .desc  "Günün son çeyreğinde yavaşlar."
                       ("Slows down in the last quarter of the day.")
                .voice "Akşama doğru ayakları konuşmaya başlıyor."
                       ("Towards evening their feet start talking.")

Huysuz          .desc  "Ekibin moralini aşağı çeker."
                       ("Drags the team's morale down.")
                .voice "Herkesle bir derdi var. Çoğunda da haklı."
                       ("Has a problem with everyone. Right about most of them.")

Tecrübeli       .desc  "Pahalıdır, hızlıdır, daha fazla gelişmez."
                       ("Expensive, fast, will not improve further.")
                .voice "Otuz yıldır bu iş. Öğretilecek bir şey kalmamış."
                       ("Thirty years at this. Nothing left to teach them.")
```

Twelve traits × two languages. It sits on the candidate card, immediately under the
role heading, and comes from the **first** trait — when two voices stack up you read
a list, not a person. That is the one moment the player reads a staff member
carefully.

### The rules of the voice (not invented, derived)

- **Name the behaviour, not the person.** Dwarf Fortress does not say "lazy", it
  says *"finds obligations confining"*. "Huysuz"'s line does not declare them bad,
  it finds them right about most of it.
- **Withhold the explanation.** RimWorld: *"Somehow, HE survived."* The regulars'
  voice already works like this: *"Oturuyor, sen biliyorsun."* ("They sit down, you
  know the rest.")
- **Plain and short.** The single shared warning of shipped barks: a line that
  reaches for cuteness sours in the third hour and is unbearable in the tenth.
- **Never say anything the simulation can contradict** — Ludeon's own writing
  guide's rule, and the same as this project's "a field its call sites lie to" rule.

### What was not done, and why

**No phonetic dialect.** `Geliyom`, `napıyon`, `uşağum` (rural-accented spellings of
"I'm coming", "what are you doing", "my lad") — none of it. That is precisely the
documented marker of mockery. The staff speak standard written Turkish, with class
syntax.

**No "Evet, Şef!"** ("Yes, Chef!"). That is a "Yes, Chef!" wearing a fez. The
documented reply is an unexcited *"Tamam şef"* ("OK chef"), or in a restaurant
*"tamam usta"* ("OK boss") or *"eyvallah"* ("right you are").

**The three named staff were not written.** [14](14-staff-system.md) designed this
— three hand-written people per cuisine, fixed traits, their own beats — and it has
**never been implemented**: no content file, no key, no code path. The regulars'
infrastructure (`StoryBeat`, the gate, `StoryScreen`) can be used as is; what is
missing is the content, and that content is the user's voice.

**The long-tenure moment was not written.** The research flagged this as *the
strongest unclaimed moment*, and it matches the real complaint in the Turkish
sources: the dishwasher calls himself "the heart of the restaurant" but says "it
looks like we do nothing". So **a line that says "see me" lands harder than one
that says "give me a raise".** But tracking tenure raises `SaveVersion`, and
raising the version without a migration path wipes sixty-day campaigns
([README](README.md)). That is the next job.

---

## 6. There was a third copy inside the audit itself

When the `.voice` family was added the text audit went red: *"not in LocTests.cs :
.voice"* — even though I had added it.

The reason: the family list sat in **three places**. The check comparing the two
copies (`gen_loc.py` ↔ `LocTests.cs`) was keeping its own probe list by hand. A
check that audits two copies cannot be built on a third copy.

Both sides are now read **from the source**. Verified by mutation: when `.voice` is
taken out of the test, generation refuses.

---

## 7. The critique round: two agents, twenty-one findings, six real faults

The user had said:

> *"sonrasında farklı agentlarla eleştir ve karara bağla"*
>
> *("afterwards, critique it with different agents and settle it")*

Two hostile eyes ran — one on the mechanic and the measurement, one on the Turkish
lines. Both were useful, and **both showed their real worth by finding lies in the first version of
this very document.**

### Faults that went down into the code

**The flag latched** (described in §3) — the most serious one, and the measurement
itself was blind to it.

**The tour's check was a tautology.** `Loc.T` returns `[key]` for a missing key, and
the card makes the same call. So **when the translation was absent entirely, both
sides produced the same wrong string and the check passed green.** My own mutation
could not have caught this, because I changed the card's string, not the key. Both
conditions are looked for now, and the `Every_trait_has_a_voice` test was added as
well: were a thirteenth trait added, none of the thirteen audits would have spoken.

**The audit itself had turned into a proxy.** In §6 I had changed the family list
to be read from the source — and that meant replacing a behavioural probe with a
**textual** one: it was asking "is it written in the source", not "is it really
exempt". Now it is both: the families are extracted from the source, and then each
one is **asked** of `SCREEN_KEY`. Verified by mutation in both directions.

**An unmeasured claim in a comment.** The crisis branch's comment said "a crisis
does not form anyway when there is a dishwasher". The measurement says the opposite
(the dishwasher arm moves, so the branch fires). The comment was corrected: the
branch fires independently of the dishwasher **and it should** — in a stopped
kitchen the dishwasher is already behind.

### In the lines: two of them said things the simulation contradicts

The rule I had written into this document — *"never say anything the simulation can
contradict"* — I broke twice in the same session:

- `CleanlinessBp` is read **only** through `TraitSum(1, ...)`, that is, while
  clearing tables in the hall. "Hızlı ama Dağınık" ("Fast but Messy") falling to a
  cook costs nothing at all — but my line pointed at the kitchen counter, the very
  place where the effect *demonstrably does not exist*.
- `MoraleAura` is summed from both pools, so it falls to the dishwasher too — but my
  line said "when they are in the kitchen".

Nine lines were rewritten. Two language errors also came out: *"elleri birbirine
dolanıyor"* was a half-remembered idiom (the correct one is **eli ayağına
dolaşmak**, "to get all in a fluster"), and *"ayakları konuşmaya başlıyor"* was the
mould of an English idiom. An ownerless possessive had leaked into the English
table too (*"the hands get tangled"*) and in two lines the pronoun was attached to
the wrong antecedent.

Three lines were left exactly as they were — the best of them is `suratsiz`:
*"İşini yapar, konuşmaz. Bazı masalar üstüne alınıyor."* ("Does the work, says
nothing. Some tables take it personally.") It does not judge the person, it matches
the mechanic exactly, and it puts the reaction where the simulation puts it — on the
customer.

*Breaking your own rule in the document where you wrote it shows the rule works: what
found it was the rule itself.*

---

## 8. The save migration path: the rule had been written, it had been applied in two places

Writing the staff's long-tenure moment required raising `SaveVersion`, and
[README](README.md) forbade it: *"No content patch should be shipped before this is
written."*

Looking closer, the rule was written in **two files** — `StateIO.cs` and
`Simulation.Save.cs` both said *"new fields are read through `Has()` and left at
their default when absent"*. Counting: applied in **2 of 126 reads**. Again a
protection written by reasoning and never run.

### `Has()` was the wrong tool anyway

Wrapping every read in `Has()` would have set up the mechanism, but it would also
have destroyed something: **`Has()` always treats a field's absence as
legitimate.** So it cannot tell a genuinely corrupt save from an old one. If
`badges` is missing from a version 21 save, that save is corrupt and blowing up is
*correct*.

The right tool is a **version gate**:

```csharp
if (version >= 21) { ...the fields added in 21... }
```

It is skipped in an old save, and it still blows up if it is missing from a version
21 save. The two are kept apart.

### And it was proved the mechanism runs

`An_old_version_save_opens` produces a real version 21 save, deletes the six fields
added in 21, sets the version to 20 and loads it — that is, exactly the real
situation after release. The yardstick is two-sided: the save **must open** *and*
the missing fields **must stay at their defaults**; asking only the first would have
passed a migration path that reset everything.

`A_version_that_is_too_old_is_rejected` says that the gate is still a gate —
otherwise an implementation of the form "accept every version, leave the fields
empty" would have passed too.

**The validation line I put inside the test stopped me once:** in my first version I
looked for the fields in the `header` node, whereas the version is there but the
fields are in `restaurant`. Without that line the test would have passed green
without deleting anything and without exercising the mechanism at all — *a migration
test whose "old save" is not real is not a migration test.*

Verified by mutation too: with the gate made `if (true)`, the test goes red with
`a field that should be in a version 21 save is missing: badges`.

`MinReadableVersion = 20` — one step back. Anything older **would be invented**:
what versions 9–14 changed is undocumented, so nobody can write the right gate for
them. And there is no released save either.

With that, the obstacle in front of the staff's long-tenure moment was cleared.

---

## 9. The lie found while building the migration path: "worked 0 days"

Looking at tenure in order to write the long-tenure moment turned up
`StaffDaysWorked` — and **it was not returning tenure, it was returning
experience** (`_cookXpDays`). Experience depends on the trait:

| trait | `XpBp` | shown on screen |
|---|---:|---|
| `tecrubeli` | 0 | a person who has worked sixty days: **"0 gün"** ("0 days") |
| `cirak` | 20000 (2×) | a person who has worked thirty days: **"60 gün"** ("60 days") |

The staff card says `"Seviye {level} ({days} gün)"` ("Level {level} ({days}
days)"). So the player was being told that somebody who had been in the shop for
sixty days had never worked. **The code-side sibling of the error I fixed in the
lines in the same session:** writing a number on screen that the simulation
contradicts.

Tenure is now tracked separately (`_cookTenure` / `_hallTenure`), independent of the
trait, +1 for every day worked. It was added to the shift-down on dismissal too —
tenure goes everywhere XP goes, or the dismissed person's tenure would stick to
whoever replaced them.

`SaveVersion` 21 → 22, and this is **the first real use of the migration path just
written**. There is no tenure in an old save; it starts counting from zero. Making
it up (deriving it from XP, say) would have brought the very lie being fixed back in
another disguise.

### The test was written weakly twice

The first one went into an **infinite loop**: I wrote `while (day < 12) { sim.Tick(); }`,
whereas the day only turns over with `OpenService` + `CloseDay` + `AdvanceToNextDay`.

The second **measured nothing**: it only looked at the inherited cook, and that cook
has no trait, so their experience rises at the same rate as their tenure — a
regression tying `StaffDaysWorked` back to XP comes out **equal** there and the test
would have passed silently. Now the test looks for a `tecrubeli` in the candidate
pools, hires them, and carries a separate arm saying *"no 'tecrubeli' candidate
appeared in fifty days - the test could not measure"*.

Verified by mutation: when `StaffDaysWorked` returns XP again, the test goes red.

### And it was seen on screen — but the first frame I looked at was the wrong frame

The code was fixed, the test passed, the mutation proved it. Then I looked at the
tour frame: the staff card said **"Seviye 0 (0 gün)"** ("Level 0 (0 days)").

For a moment I thought the fix had not held. It had — **the frame was the morning of
day 1**, and there "0 days" is correct: nobody has worked a day yet.

What this really means is: **the bug would have been invisible in that frame
anyway.** Even if the card had said "0 days" for months, the first day's frame would
show the same thing. So a check that catches this bug has to look further into the
day.

The tour now opens the staff screen on day 5 and its yardstick is the text on
screen:

```
ok   : Tenure advances (day 5, 4 days)
ok   : The staff card shows the real tenure number
```

*To see that a number is right you have to look at the right moment; the wrong
moment makes the wrong answer look right too.*

---

## 10. The long-tenure moment: the unclaimed moment was written

The research had flagged this as *"the strongest unclaimed moment"* — **none** of
the twelve games it swept has a line written for long tenure. And it matches the
real grievance in the Turkish sources: the dishwasher calls himself "the heart of
the restaurant" but says "it looks like we do nothing". **A line that says "see me"
lands harder than one that says "give me a raise".**

On the thirtieth day, a **recognition** from the same family as the badges: the
player does not do something, something is said because something *is*.

| cuisine | line |
|---|---|
| tradesman's restaurant (`notice.tenure_lokanta`) | *"{ad} 30 gündür burada. Artık sormadan biliyor."* ("{name} has been here 30 days. Doesn't need to ask any more.") |
| chain (`notice.tenure_zincir`) | *"{ad} 30 gündür vardiyada. Yeni gelenler ona soruyor."* ("{name} has worked 30 days of shifts. The new starters come to them with their questions.") |

**Two separate lines**, because the research said the two places take pride in and
complain about different things: in a restaurant the relationship is **to the boss
and to the craft**, in a chain **to the shift and to the system**. A single line
would have made both of them generic.

The threshold is thirty days — not invented, derived from two limits. Had it been
too small (seven, say) one would come out for somebody every week and it would stop
being recognition and turn into noise; the badges did exactly this once
([47](47-recognition-and-report-card.md)). Had it been too large (fifty, say) it
would only come out for a crew hired on day one and never changed, so it would have
nothing to do with the player's decisions.

### Three measurements, each asking a different question

1. **Does it fire in the core** — a unit test, and **exactly once per person**.
   Saying "at least once" would have passed a version treating the threshold like
   `>=` too, and that would have filled the notice strip with a single sentence.
2. **Does it reach the player** — the tour, scanning the notice strip. "The mechanic
   is complete in the core and never reaches the player" has come up **three times**
   in this project.
3. **Does it speak the right record** — the two cuisines were run separately.

### I wrote both tests wrong, and both said so themselves

**The unit test** kept a **total** for "exactly once" and went red: on day 30 **two
people** cross the threshold at once (the inherited cook and the one in the hall).
Two notices is correct — two separate human beings. What was wrong was the
expectation; the counter was turned into a per-person one.

**The tour's check** built the expected text from `StaffName(0, 0)` and went red. The
diagnostic showed the notice **was there**, but with the hall person's name (*"Aslı
30 gündür burada"* — "Aslı has been here 30 days") — a three-slot strip leaves out
one of two lines arriving on the same day. Which **name** survives is not the tour's
business; the question is *"did a line like this reach the player"*. The check was
tied to the **moment**, not the person.

*Both errors are from the same family: tying the yardstick to the person, when the
thing you want to measure is the moment, makes the wrong answer look right too.*
