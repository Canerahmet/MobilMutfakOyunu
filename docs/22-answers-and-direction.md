# Answers and Direction

**Last updated:** 10 September 2026
**Purpose:** to keep the answers to the review's nine questions, what those answers changed in the plan, and what is still open, all in one place.
**Source:** [review/00-synthesis.md](review/00-synthesis.md) §9

---

## 1. The answers

| # | Question | Answer | What changed | Where |
|---|---|---|---|---|
| 1 | Full time or evenings | Claude Code by remote control, the AI does the work | The work is asynchronous and in batches; the calendar multiplier is uncertain. §3 below | [05-production-plan.md](05-production-plan.md) |
| 2 | Is there a Mac | **No.** The first release is Android | iOS is out of the first release. The Apple 99 dollars is not paid. The Metal settings are deferred | [21-business-and-release.md](21-business-and-release.md), [20-production-decisions.md](20-production-decisions.md), [19-technical-setup.md](19-technical-setup.md) |
| 3 | Do you know Blender | **No**, it is handed over to the AI | The art pipeline was redesigned: a headless render loop, verified today | [24-art-pipeline.md](24-art-pipeline.md) |
| 4 | Is the budget zero | **Zero for now**; release fees will be paid when they are needed | The first-year cost dropped from 124 dollars to 25. No paid test acquisition; the soft launch is organic | §4 below |
| 5 | The game's name | **Not settled** | Ten candidates screened, a proposal is ready | [25-game-name.md](25-game-name.md) |
| 6 | 20 dishes or 32 | **32**, in favour of variety | The designer's condition is binding: every dish has four parameters. The art cost is held constant by modular plating | [23-core-contract.md](23-core-contract.md) §8.3, [24-art-pipeline.md](24-art-pipeline.md) |
| 7 | An end-of-day rewarded ad | **Not in the first release.** It is added in the last step before the game is completely finished and published; its frequency and reward are decided then | Decided 10 September 2026. The analysis stands as a note in §5 | [21-business-and-release.md](21-business-and-release.md) |
| 8 | Should Steam be brought forward | **No.** Both the page and the version are a stage after the game is finished | Decided 10 September 2026. The launch table was updated | [21-business-and-release.md](21-business-and-release.md) |
| 9 | Landscape | **Landscape** | Settled. The thumb-zone layout and left-hand mirroring are in Batch D | [16-screens-and-tutorial.md](16-screens-and-tutorial.md) |

---

## 2. What was found on the machine

Alongside the answers, the machine was scanned too:

| Tool | Status | Note |
|---|---|---|
| Blender | **5.2 LTS installed** | Headless render verified, three angles in 4 seconds |
| Unity | **6000.5.8f1 installed** | This is Unity 6.5, not LTS. The plan says 6.3 LTS. Below |
| Unity Hub | Installed | |
| Python | 3.13 | The balance tool runs on this |
| .NET SDK | Installed | For the core tests and the balance tool |
| Git | Installed | The project is not a repository yet |
| Node | Absent | Not needed |
| Disk | C 305 GB, D 152 GB free | Enough |

**The Unity version: decided 9 September 2026.** 6.3 LTS will be installed from the Hub and the project opened with it. The installed 6.5 can stay. The reasoning is in [19-technical-setup.md](19-technical-setup.md): a long-lived game is not kept on an experimental version.

---

## 3. The way of working: what remote control means

Claude Code by remote control shapes the work like this:

- **Asynchronous batches.** You give a goal, I work, and the result arrives in front of you as a PNG, a table or an APK. You do not have to be at the machine.
- **I can do everything on the command line:** Blender, Unity batch builds, tests, the balance tool. Today I ran Blender myself.
- **You do only three things:** web tools (Mixamo, the store consoles), installing an APK on a device, and the decision about whether you like it.

The calendar multiplier is still uncertain: how many hours a day you can do "give a goal, look at the result". The scope review said 13-14 person-months. How much of that is calendar months depends on your rhythm. Whatever happens, this holds: **the first release ships with two cuisines**, fast food and Turkish. Italian and Japanese arrive as updates. That was already written in [12-economy.md](12-economy.md) §9; now it is settled.

---

## 4. The zero budget plan

| Item | Old | New | When |
|---|---|---|---|
| Google Play developer | $25 | $25 | When the closed test starts |
| Apple developer | $99/year | **0** | Once there is a Mac |
| Steamworks | $100 | $100 | After the game is finished, when the Steam page opens |
| Unity | 0 | 0 | Personal, under 200 thousand dollars |
| Blender, Python, .NET | 0 | 0 | |
| Assets | 0 | 0 | CC0 and procedural |
| Test acquisition | Was not written | **0** | Once there is revenue |
| **First year, Android** | $124 | **$25** | |

**The meaning of the soft launch has changed.** The publishing review said "without 300-500 dollars of test acquisition a soft launch produces no data"; it is right, there are no paid installs. The new definition: **an organic closed test.** Twenty to fifty testers, gathered by hand from Turkish indie game communities, r/tycoon and Discord. What is measured is not the install cost but D1 and D7 retention and economy telemetry ("on which day did money stop being a problem"). That data can be produced on a zero budget and it is enough to tune the balance. For the conversion rate, we wait for the real release.

---

## 5. Ads and Steam: decided, the analysis stands as a note

**The decision of 10 September 2026.** Neither is in the first release. The rewarded ad is added in the last step before the game is completely finished and published, and its form is decided then. The Steam page and the Steam version are a stage after the game is finished. The analysis below stays as a note until that day; when the day comes, we carry on from here.

### The rewarded ad: **not in the first release, to be added in the last step before publishing**

| For | Against |
|---|---|
| The only revenue from the 98% who do not pay | An ad SDK brings a privacy burden: a GDPR consent form, the Play data safety declaration, a child-targeting check. A week for one person |
| It does not appear unless the player presses | "No energy, no timers, no ads" is the most loved promise in the research. Even one ad breaks that sentence |
| | The publisher's own finding: the audience is Steam-flavoured and hates ads |
| | On a small install base the return is trivial: a 10-15 dollar eCPM × a few hundred impressions a day |

When to look again: when the game is completely finished, right before publishing. Two things to decide that day, and today's note on each:

**Frequency.** The proposal is end of day, at most once a day, only if the player presses, on the day report screen.

| Frequency | Impressions per campaign | Return per player | Assessment |
|---|---|---|---|
| Weekly, on rent day | 8 | Close to zero | Not worth the SDK's privacy burden; it drifts into "watch to lower the rent" and makes the economy's core tension purchasable |
| Daily, end of day | 60 | Between 0.12 and 1.20 dollars, by region | The same order of magnitude as the per-install return of a 4.99 dollar cuisine sale |

**The reward.** A money reward breaks the balanced economy:

| Reward | Total over 60 days | As a share of the end-game till (11,630) |
|---|---|---|
| 50 coins a day | 3,000 | 26% |
| 100 coins a day | 6,000 | 52% |
| 2% of the daily revenue | ~3,300 | 29% |

So the proposal is a non-money reward: **seeing tomorrow's market prices in advance** (an information advantage, no coins enter the till) or raising a staff member's morale. If money is wanted, at most 1% of the daily revenue, and a "player who watches every day" scenario is added to `tools/balance/model.py` and the seventeen tests are re-run.

**Technical.** The ad provider sits behind an `IAdProvider` port; in the Steam build the port returns "none" and the Steam player sees no ads. On Android, the GDPR consent screen and the Play data safety form are roughly a week of work.

### The Steam page: **after the game is finished** (the end-of-Phase-1 proposal was not accepted)

The publisher is right: nine of the ten games in the research are from Steam, and the audience that hates timers is there. But the build order does not change, Android first. What changes is the marketing order:

| Step | When | Cost |
|---|---|---|
| The Steam page opens, wishlists are gathered | After the game is finished; at the earliest during the Android release, and it is looked at again then | $100 |
| A demo at Next Fest | The first suitable date | 0 |
| The Steam version | After the Android release, with two cuisines, early access at 9.99 | 0 |

What the page means: Steam allows a "Coming Soon" store page to be opened before the game is playable; its only button is add-to-wishlist. On the day the Steam version ships, everyone on that list gets a notification, and the first day's sales feed Steam's visibility algorithm. What is needed: Steamworks 100 dollars (refunded after the first 1,000 dollars of revenue), the game's name, five screenshots and a cover image (renders from Blender and Unity, I produce those), an optional short video, and a Valve review of three to five days. Next Fest runs three times a year, one entry per game, and requires a demo. The decision: the page after the game is finished.

---

## 6. What changed in the plan with these answers, in summary

| Document | Change |
|---|---|
| [12-economy.md](12-economy.md) | Batch A: all the tables are generated by `tools/balance` |
| [14-staff-system.md](14-staff-system.md) | Batch A: the two-pool capacity model, caps 3/5/8/12 |
| [23-core-contract.md](23-core-contract.md) | Batch B: new, binding |
| [24-art-pipeline.md](24-art-pipeline.md) | New: the render loop, three tiers, modular plating |
| [25-game-name.md](25-game-name.md) | New: ten candidates, the proposal is Last Seating, the second is Lokanta |
| [05-production-plan.md](05-production-plan.md) | Cost $25, Android, remote control |
| [20-production-decisions.md](20-production-decisions.md) | iOS is out of the device matrix |
| [21-business-and-release.md](21-business-and-release.md) | Android as the only platform, an organic closed test, ads and Steam after release |
| [16-screens-and-tutorial.md](16-screens-and-tutorial.md) | Landscape settled |
| [19-technical-setup.md](19-technical-setup.md) | A note on the installed version, the 6.3 LTS proposal |
| [09-content-inventory.md](09-content-inventory.md) | 32 settled, the four-parameter condition |
| [06-plan-status.md](06-plan-status.md) | A4 and A8 closed, Batches A and B done |

---

## 7. What is expected now

From you, in order:

1. ~~Install Unity 6.3 LTS from the Hub~~ **Decided**, to be installed
2. **Mixamo clips are the backup, at the implementation stage, and you download them.** The primary source is the Quaternius Universal Animation Library 1 and 2 (CC0, a downloadable pack, I wire it up); restaurant movements are procedural in Blender. See [24-art-pipeline.md](24-art-pipeline.md)
3. ~~Say if you object to the ad and Steam proposals~~ **Decided 10 September 2026:** both come after the game is finished
4. Do not decide on the **name** without doing the trademark search in [25-game-name.md](25-game-name.md) §9

From me: Batches A and B are closed, Phase 0 is open. The next job is the `Lokanta.Core` skeleton, `Fx`, `Rng`, the first schemas, and the test that matches the C# core against the Python model.
