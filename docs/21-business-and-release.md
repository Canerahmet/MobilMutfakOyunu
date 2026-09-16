# Business and Release

**Last updated:** 9 September 2026
**Register items:** D2 price, D5 launch plan, D6 legal
**Status:** Written, awaiting decision

---

## D2. Price

### Mobile

| Item | Price | Note |
|---|---|---|
| The game | Free | The fast food cuisine, a full campaign |
| Turkish cuisine | The 4.99 dollar band | Available at launch |
| Italian | The 4.99 dollar band | An update |
| Japanese | The 4.99 dollar band | An update |
| The all-cuisines bundle | The 11.99 dollar band | Discounted against buying them one by one |

**The stores show the price in the player's own currency.** The dollar here is only a reference tier; the player sees it in their own money.

### Steam

| Item | Price |
|---|---|
| The game, all cuisines included | The 12.99 dollar band |

There is no content purchase on Steam. A single price, everything included. Steam players react badly to piecemeal selling.

### Reasoning

- The price of a single cuisine is in the price band of a coffee on mobile. The amount of content is a four to six hour campaign, so there is something behind it.
- The bundle discount: where buying three cuisines separately would be 14.97, it is 11.99. Roughly a 20 per cent discount.
- The Steam price is a little above the bundle price, because there it is a one-off sale and there is refund risk.

**The exact figures will be tuned with soft launch data.** These are a starting point.

---

## D5. Launch plan

### Four stages

| Stage | Duration | Purpose |
|---|---|---|
| Closed test | 2 weeks | Crashes and flow bugs |
| Organic closed test | 4-6 weeks | D1/D7 retention and economy telemetry. No paid installs, 20-50 hand-gathered testers |
| Global release | — | Mobile, two cuisines |
| Steam page | After the game is finished | Collects wishlists, $100. Decided 10 September 2026 |
| Steam version | After the Android release | Early access with two cuisines. See [22-answers-and-direction.md](22-answers-and-direction.md) §5 |

### The soft launch market

A small, cheap, English-speaking market is chosen. The aim is to collect real retention data with cheap user acquisition.

**Turkey is not used for the soft launch.** The reason: Turkish cuisine is our main hook and Turkey is one of the main markets. We do not want to burn it with a half-finished game.

### What will be measured

| Metric | Why |
|---|---|
| Day 1 retention | Does the tutorial work |
| Day 7 retention | Is the first rent day being got past |
| Day 30 retention | Is the campaign being finished |
| Campaign completion rate | Is sixty days the right length |
| Cuisine purchase rate | Does the revenue model work |
| Average session length | Does the three to six minute target hold |
| The day number where they quit | On which day do players stop |

**The two most critical: day seven retention and the day number where they quit.** The first tells us whether the tutorial and the first rent day work. The second tells us whether the mid-game plateau from the research is present in our game too.

### The store page

| Item | Who |
|---|---|
| Screenshots | Taken from the game, your operation |
| Trailer | Recording and editing, your operation |
| App icon | An image generation tool |
| Store copy | Mine |
| Keywords | Mine |

**Three things to lead with in the store copy:** no energy, no wait timers, no second currency. The absence of the most hated mechanics in the research is a marketing message on its own.

### Promotion

There is no budget, so organic routes:

- Communities that share gameplay clips
- The Turkish cuisine angle, a natural story for the local games press and communities
- A development diary; a one-person project draws interest
- The Steam page opens after the game is finished and collects wishlists (the 10 September 2026 decision; the proposal to open it early was not accepted)

---

## D6. Legal

### The mandatory items

| Item | Status |
|---|---|
| Privacy policy | **Mandatory.** Certain if there is analytics or advertising |
| KVKK and GDPR compliance | If data is collected, an explicit consent flow is required |
| Age rating | A separate application for each store |
| Terms of use | It is written explicitly that virtual currency cannot be converted to cash |
| Children's privacy | Additional obligations can arise depending on the age rating |

### The asset licence audit

**A mandatory step before release.** The source and licence of every asset are listed in a table.

| Column | Contents |
|---|---|
| Asset | The file name |
| Source | Procedural, public-domain pack, AI tool, purchased |
| Licence | CC0, a commercial plan, a purchase receipt |
| Evidence | A link or an invoice |

**The biggest risk:** an asset produced on an AI tool's free tier slipping through into the game. Those tiers are closed to commercial use. The audit exists to catch that.

### The data collection principle

**Data that is not needed is not collected.** Analytics records only the events needed for game balance: day number, till, reputation, the point of abandonment. No personal data is collected.

This is both ethical and practical. Data that is not collected cannot be leaked and creates no compliance burden.

### Warning

I can write a draft of the privacy policy and the terms of use, but **the final text has to be read by a lawyer.** This is an item that must not be waved through with a draft.

---

## Release audit — 11 September 2026

A publishing review opened the package and audited it **from the inside**: the
APK signature block was read, the binary `AndroidManifest.xml` was decoded, the
compression method of the `lib/` entries was inspected, and whether the licence
texts made it into the build was verified by searching for them. The table below
is the result of that audit and what was done afterwards.

### Ready

| Item | Evidence |
|---|---|
| Package name format | `com.ahmetakar.lokanta` — a proper reverse domain, not `DefaultCompany` |
| Target / minimum SDK | `targetSdk 36` (the Play threshold is 35), `minSdk 25` |
| 64-bit | Only `arm64-v8a` under `lib/`, IL2CPP |
| 16 KB page alignment | All seven `.so` files have `p_align=0x4000` — the Android 15+ requirement |
| Permissions | **Not a single permission** visible to the user; no INTERNET |
| Automatic backup | `allowBackup=false` — the patch was verified |
| Debug flag | No `android:debuggable` in the manifest |
| Data collected | **Zero.** No advertising/analytics/payment package, `UnityConnectSettings` all off, not a single network call in the code |
| Licence texts | Rubik OFL, Kenney CC0 (three packs) and the engine components **make it into the build** and the full text is readable on an in-game screen |
| **Native library packaging** | `useLegacyPackaging = false` — the install footprint dropped from 108 MB to **84 MB** (see below) |
| **AAB** | **There is none, and that is correct.** The first AAB produced (30.8 MB) was signed with the *debug* key — its certificate is `CN=Android Debug`, so Play rejects it at upload time. Because the file sat there named `build/android/Lokanta.aab` it was a trap open to being uploaded by accident; it was **deleted**. `BuildPlayer` now refuses to produce an unsigned AAB with a `BuildFailedException` (a warning is enough for an APK, so you can push one to a device). The first real AAB comes out once the keystore is ready |
| **Store icon** | `Art/Icons/store-icon-512.png`, 512×512 **RGBA**, 5 KB |
| **Version parameterised** | The `-lokanta-version-code` and `-lokanta-version` flags |

**The measurement of the packaging change.** The old behaviour compressed the
`.so` files and copied them to the install by **extracting** them: a 30.8 MB
package + 77.1 MB extracted = **107.9 MB**. In the new one the libraries sit raw
inside the package and are read in place: the install is **84.3 MB**, nothing
extracted. The APK on its own gets bigger (84.3 MB) but the store route is the
AAB and **the AAB stayed at 30.8 MB** — a small download, a small install.

### The user's decisions (there is no release without these)

| # | Item | Why it is the user's |
|---|---|---|
| 1 | **The upload key (keystore)** | A password. `BuildPlayer` now reads the `LOKANTA_KEYSTORE`, `LOKANTA_KEYSTORE_PASS`, `LOKANTA_KEYALIAS` and `LOKANTA_KEYALIAS_PASS` environment variables; if none of them are there it signs with the debug key and **prints a warning**. Play rejects a debug-signed package. If the key file is **lost the app can never be updated again** — it has to be backed up in two separate places |
| 2 | **The game name and a trademark search** | [25](25-game-name.md) §9: "no name counts as settled without a trademark search". The package name is **locked at the first upload** and never changes again; if the name changes later, `com.ahmetakar.lokanta` becomes a permanent inconsistency |
| 3 | **A Play Console account** | $25, one-off. Also, before going to production, **a 14-day uninterrupted closed test with 12 testers** — the longest item on the calendar, and it should start as soon as the key is ready |
| 4 | **A privacy policy URL** | Play asks for one from every app, even if no data is collected. Because what there is to say is one sentence, the text is short; GitHub Pages is enough |
| 5 | **The Data Safety form** | The declaration is ready: **"This app does not collect user data"** — verified by the evidence above. Filling it in takes minutes, but there is no release without it |
| 6 | **The IARC age rating** | No release without the questionnaire being filled in. There is nothing obstructive in the content (no violence; "veresiye" — the tab — is a credit mechanic, not gambling). Proposal: **13+ and "not directed at children"** — if "children" is picked by mistake because of Kenney's cartoon style, the Families Policy obligations open up |
| 7 | **The money model** | [D2](#d2-price) says $4.99 per cuisine, but `com.unity.purchasing` is **not in the project** and there is not a single line of purchasing in the code. There is no lock on the cuisine selection screen either: `CuisineScreen` wires both cards straight to `SlotScreen`. So the game could be released today as **free with TWO cuisines open**. A free→paid transition is **impossible**, so this decision is irreversible |
| 8 | **Store images and copy** | **Mostly closed** — [44](44-store-texts.md): the short description, the full description (TR+EN) and the privacy policy text are written; screenshots are generated automatically by `tour.ps1 -Store` at 2183×983 (the same dp layout, 20:9) and sit under `render/store/`. The icon is ready. **The one remaining visual job: the feature graphic (1024×500)** — it depends on a cover design and the name decision |

### The order

1. Generate the key, set the environment variables, take the first **signed** AAB
2. The name decision + trademark search → before the package name is locked
3. A Play Console account, start the closed test (14 days)
4. Privacy policy, Data Safety, IARC (half a day in total)
5. Store images and copy

### Two things to run before every release

They are not part of the daily check (`tools/check.py`) — one takes minutes, the
other needs a device. But a shipped binary should not go out without passing
both:

```
.\tools\unity\tour.ps1 -Build windows-il2cpp   # the stripping exam, no device needed
.\tools\android\device.ps1                    # a real phone, ARM64
```

**Why the first is mandatory:** Android is built with
`ManagedStrippingLevel.High` and the content loading uses reflection.
`unity/Assets/link.xml` protects that; if the protection falls short the game
**dies at startup** and no other test can see it — the daily tour runs Mono,
where there is no stripping. When a **new** assembly that uses reflection is
added, `link.xml` does not know about it; this run is the only thing that catches
that moment. Measured: [46](46-shipped-binary.md) §4.

**Why the second is mandatory:** the shipped binary is ARM64 and that code is
not produced by any desktop run. An emulator is no substitute — because the APK
carries only `arm64-v8a`, it cannot even be installed on an emulator.

---

## Details awaiting a decision

1. Which market the soft launch should be
2. Is the 4.99 band the right cuisine price
3. ~~When should the Steam page open~~ Closed 10 September 2026: after the game is finished
4. Should there be no analytics at all, or a minimum

---

## Update of 9 September 2026

- **Platform:** the first release is Android only. iOS once there is a Mac. See [22-answers-and-direction.md](22-answers-and-direction.md).
- **Budget:** zero for now; release fees will be paid when they are needed. No paid test acquisition; the soft launch was redefined as an "organic closed test".
- **Rewarded ads:** decided 10 September 2026, not in the first release. They are added in the last step before the game is completely finished and published; their frequency and reward are decided then. The analysis note is in [22-answers-and-direction.md](22-answers-and-direction.md) §5.
- **Steam:** decided 10 September 2026, both the page and the version come after the game is finished.
- **Name:** [25-game-name.md](25-game-name.md). The proposal is Last Seating, the second is Lokanta. No decision without a trademark search.
