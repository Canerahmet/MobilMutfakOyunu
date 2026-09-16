# 51 — Fast food became self service

*15 September 2026.* The user's proposal:

> *"Fast food ve diğer mutfakları ayıran en büyük ayrım garson olabilir. Çünkü
> fast food restoranlarda genellikle self servis olur. Fast food için garson
> kısmını kaldıralım, temizlikçi olsun; o da insanların yemek yedikten sonra
> masada bıraktığı tabakları toplasın."*
>
> *("The biggest thing separating fast food from the other cuisines could be the
> waiter. Because fast food restaurants are usually self service. Let's remove the
> waiter for fast food and have a cleaner instead; they can collect the plates
> people leave on the table after eating.")*

And then:

> *"Fast food tarafına gelen müşteri sayısını artırıp baskıyı artırabiliriz,
> ayrıca fast food birim başına daha az kazanç modeline sahip olabilir — sonuç
> olarak gerçekte de fast food zincirler daha ucuz olur."*
>
> *("We could raise the number of customers coming to the fast food side and raise
> the pressure, and fast food could also have a lower earnings-per-unit model —
> after all, in real life fast food chains are cheaper too.")*

Both were implemented. This document records **what was measured** and where the
two cuisines now diverge.

---

## 1. The flow: two steps dropped

The customer state machine was built around table service:

```
waiting for a table -> waiting to order -> waiting for food -> eating -> waiting to pay
```

Under self service two steps **stop being staff work**:

| step | table service | self service |
|---|---|---|
| order | the waiter comes to the table | at the counter (cashier) |
| **serving** | the waiter brings the food | the customer carries their tray — **dropped** |
| **payment** | the waiter takes it at the table | cash up front at the counter — **dropped** |
| clearing | the waiter clears | **the cleaner's main job** |

**The accounting was not touched.** `CompletePayment` is still called:
satisfaction, reputation, the regular's record, revenue and **leaving the table
dirty** all live there. So the tray the cleaner will collect is sitting on the
table. The only thing that changed is that the player no longer *needs* to send a
waiter to the table.

## 2. The number: the waiter's wage dropped too

Changing the flow on its own would have been **inconsistent**.
`StaffingModel.Required` used a global hall load (waiter + dishwasher + cashier =
73,581 micro/customer), so the player would go on paying **the wage of a waiter
with no job** and the game would get easier for the wrong reason.

Hall roles now belong to the cuisine (`cuisines/*.json: hallRoles`):

| | hall pool | micro/customer |
|---|---|---:|
| fast food | cashier + dishwasher | **35,119** |
| Turkish | waiter + dishwasher + cashier | 73,581 |

The result: the `makul` bot's crew **4.0 → 2.0**, its wage bill **25,617 →
18,881**.

The name on screen changed too: in fast food the hall worker is
**"Temizlikçi"** ("Cleaner"). Its key is in the `ui.*` family, not `role.*` —
`role.*` names come from the content (`staff-roles.json: nameKey`) and there is
no cleaner role there; what is being chosen is not the role itself but the name
**shown** to the player.

---

## 3. Volume and margin: the promise was not in the numbers

[44](44-store-texts.md) sells it as "fast food: small ticket, a crowd". The
measurement showed **the opposite**:

| | parties | ticket | gross margin |
|---|---:|---:|---:|
| fastfood | 1945 | 56.7 | **64%** |
| turk | 1819 | 67.5 | 56% |

So the cuisine sold as the cheap one was both more profitable and at the same
volume.

Both arms were wired up:

- **Volume** — `customerMultiplierBp` belongs to the cuisine and passes through
  **a single gate** (`ExpectedCustomers`), so the crew suggestion, the market
  suggestion and the arrival plan all see the same number. Fast food 13000
  (+30%).
- **Margin** — the ingredient-ratio target in `gen_dishes.py` was pulled to the
  top end of the band for fast food. [12](12-economy.md) writes the band as
  **28–36%**; no number was invented, it stayed inside the written limit.

The result:

| | parties | ticket | crew | lost |
|---|---:|---:|---:|---:|
| fastfood | **2597** | **52.5** | 5.0 | **16** |
| turk | 1819 | 67.5 | 4.0 | 6 |

Volume +43%, ticket −22%, customers lost almost three times as many. "A crowd, a
small ticket, more pressure" is in the numbers for the first time.

---

## 4. I got it wrong twice, and the numbers caught both

**Inflation nobody asked for.** While opening the band in two directions I also
lowered Turkish's ingredient ratio, that is, *raised* its margin: `makul` 17,351 →
22,257. What was wanted was fast food's per-unit earnings to fall; nobody asked
for Turkish to be touched.

**A shared group.** When I reverted it Turkish **did not** return to its old value
(16,009). The reason: the two cuisines **share** the `icecek` and `tatli` groups,
so a single per-group target shifts both at once. The target is now keyed by
**cuisine + group**.

The verification yardstick is not a harness number but **the file itself**:
`content/dishes/turk.json` is among the unchanged files — only `fastfood.json`
changed.

---

## 5. Volume is not a lever for total cash

Lowering the multiplier made fast food **richer**:

| multiplier | cash | crew | tabl | lost |
|---:|---:|---:|---:|---:|
| 11500 | 23,386 | 3.3 | 7.8 | 10 |
| 12000 | 24,318 | 3.8 | 8.1 | 11 |
| **13000** | 22,473 | 5.0 | 10.3 | **16** |

The reason: at high volume the bot expands, rent and wages rise, customers are
lost; at low volume the shop stays lean and makes more profit. So volume changes
the **shape**, not the total. 13000 was kept because it gives the most
differentiated shape.

---

## 6. A test's premise collapsed — and it was right

`PlateTests.A_dishwasher_rescues_the_hall_from_the_sink` was running fast food and
its two arms came out **identical** (32/32, zero waiting with no plate): under
self service the hall's work halves, so the plate bottleneck no longer forms.

The test's own comment recorded that it had measured emptily like this before and
that it had been fixed once; this change had brought the same gap back.

The cure was not to weaken the test but **to move it to the right place**: the
question is meaningful in a **table-service** cuisine, where the waiter runs both
to the table and to the sink. It was moved onto the Turkish content and it passes.

---

## 7. An open balance question

Fast food is still ahead overall: `makul` 22,473 against Turkish 17,351. The
reason is structural and realistic — fast food's **spoilage is a third**
(5,487 against 14,121) and self service halved the hall cost. In real life chains
are cheap to run for the same reason.

But in the game's economy this means: **the free/starting cuisine earns more than
the paid one.** The reward ordering is inverted.

There are three paths, and which one is right is a design decision:

1. **Accept it** — fast food is the easy start, Turkish is a different game
   (signature mechanic, high ticket, less pressure).
2. **Split the rent** — in reality chains do sit in high-traffic, expensive spots.
   Rent is currently tied to the tier and independent of the cuisine.
3. **Strengthen Turkish** — but that is exactly the unasked-for inflation I
   reverted in §4; it should only be done if it is asked for.

The numbers are here; the decision is the user's.

---

## 8. The mechanic was right, the screen was wrong — twice

After the simulation change was finished, **two real faults on the visible side**
remained. The tour caught both and both are of the same class: *the game works
correctly, what the player sees is wrong.*

### "Temizlikçi" was written nowhere

The staff card shows the **name**; the role name only appeared as a fallback when
there was no name. Because staff have names (Sevgi, Nurten…), "Temizlikçi" was
never visible — so the player could not learn that the person in the hall was a
cleaner and **not** a waiter.

I had written "the screen says Temizlikçi" and I **had not looked**. The role name
is now on every card, next to the trait: *"Temizlikçi · Huysuz"* ("Cleaner ·
Bad-tempered").

### The washing-up sentence talked about a role that does not exist

```
ui.staff.sink_none = "Kimse lavaboda değil — bulaşık birikince garson geçer"
```

("Nobody at the sink — the waiter steps in when the plates pile up.") There is no
waiter in fast food. It was made role-neutral in both languages ("salondan biri" /
"someone from the floor").

**What found this was writing the check in both directions:** it looks for the
presence of the right label *and* the absence of the wrong one. Had it asked only
the first, it would have passed; because the second was there it caught an
inconsistency hiding in a sentence that has nothing to do with the role name.

---

## 9. The store image: not lowering the bar

Self service speeds up table turnover (no waiting for service or payment), so the
number of tables occupied at any one time falls. The store image's quality gate
("half the tables full") ran out of time at **4/14** on one run.

Lowering the bar was tempting and **would have been wrong**: the store image's job
is to show a full restaurant; saying "it is empty under self service anyway" would
have been settling the image for the game's quietest moment.

Besides, the threshold was reachable — other runs of the same build saw 8/14 and
13/14. What was missing was not the bar but **patience**: the search was raised
from 30 to 75 seconds and the result was **13/14**.
