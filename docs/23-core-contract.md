# The Core Contract

**Last updated:** 10 September 2026
**Register items:** B3 data schemas, B6 save format, A11 save system, and the review's "determinism is not designed" finding
**Status:** Batch B. This file is binding. Phase 0 code has to obey the rules here.
**Review source:** [review/02-technical-architecture.md](review/02-technical-architecture.md)

---

## Why this file exists

The architecture review found this: [04-architecture.md](04-architecture.md) **wants** determinism but does not **design** it. `Tick(deltaTime)` pushes a variable frame time into the core, the source of randomness is unclear, floating point can give three different results on three compilers, and the Turkish culture setting breaks the `ToUpper` call.

The save system, the balance tool and debugging all three lean on the assumption "same input, same output". That assumption does not hold by itself. The ten rules below make it hold.

Next to every rule is **how it is verified**. There is no rule that cannot be verified.

---

## 1. The determinism contract

### 1.1 Definition

**Same content, same seed, same command log → same state, byte for byte.** Platform, compiler, operating system, culture setting, frame rate and real time: none of them may change the result.

"Same state" means this: the state hash in section 9 is equal.

### 1.2 Fixed step

```csharp
public sealed class Simulation
{
    public const int TickMs = 100;          // one tick = 100 ms of simulation time
    public long TickIndex { get; private set; }

    public void Tick();                      // no parameters. Real time does not get in.
    public void Apply(in Command c);         // player input
    public IReadOnlyList<SimEvent> DrainEvents();
}
```

`Tick()` takes no parameters. The core never sees real time.

The Unity side accumulates:

```csharp
// Lokanta.App.SimDriver : MonoBehaviour
float _acc;
void Update()
{
    _acc += Time.unscaledDeltaTime * Speed;       // Speed = 1 or 2, the player picks
    int n = 0;
    while (_acc >= 0.1f && n < MaxTicksPerFrame)  // MaxTicksPerFrame = 5
    {
        _sim.Tick();
        _acc -= 0.1f;
        n++;
    }
    if (n == MaxTicksPerFrame) _acc = 0f;         // if frames drop, time slows down, it does not drift
}
```

**What happens when a frame drops:** the simulation slows down, it does not break. The five-tick ceiling prevents the "death spiral". The player waits one extra second, but the save file does not corrupt.

**Speed 2x:** the driver calls tick twice as often. The core knows nothing about speed. That is why a player on 2x and a player on 1x who issue the same commands on the same tick get the same result.

### 1.3 Time units

| Duration | Ticks | Note |
|---|---|---|
| One tick | 1 | 100 ms |
| One customer service | ~1,200 | 120 s, [12-economy.md](12-economy.md) §5.2 |
| One service day | 4,800 | 8 minutes at 1x speed |
| A sixty-day season | 288,000 | The balance tool finishes it in 15 seconds at 50 µs/tick |

The patience values ([12-economy.md](12-economy.md) §5.2) are written in seconds; in the core they are held as ms, 8 s = 8,000 ms = 80 ticks.

### 1.4 Verification

- **Replay test:** run twice with the same seed and command log, the hashes must be equal.
- **Frame independence test:** run the same commands with `MaxTicksPerFrame = 1` and `= 5`, the hashes must be equal.

---

## 2. Integer state

### 2.1 The rule

No type under `Lokanta.Core` holds a `float`, `double` or `decimal`. Field, parameter, return value, local variable, constant: none of them.

Reason: IL2CPP (Android), Mono (editor) and RyuJIT (the balance tool, .NET) can perform floating-point operations in a different order and at a different precision. `0.1f + 0.2f` can give three different bit patterns in three environments. Integer addition is the same everywhere.

### 2.2 Unit table

| Quantity | Unit | Type | Example |
|---|---|---|---|
| Time | millisecond | `int` (within a day), `long` (tickIndex) | 8 s of patience = 8000 |
| Money | **centi-coin**, 1 coin = 100 units | `long` | 45 coins = 4500 |
| Weight | gram | `int` | 120 g of patty mix |
| Satisfaction, reputation, morale | **centi-point**, 0..10000 | `int` | reputation 30 = 3000 |
| Rate, multiplier, percentage | **basis point (bp)**, 10000 = 1.0 | `int` | 32% ingredients = 3200; speed +18% = 11800 |
| Capacity | customers/day | `int` | waiter 25 |
| Owner labour | centi-work-day | `int` | 1.4 work-days = 140 |
| Counters | count | `int` | table 14 |

Money is `long`, because a 60-day total revenue in centi-coins can exceed 2³¹ (43,425 coins/week × 100 × 9 weeks ≈ 39 million, the limit is 2.1 billion; safe, but `int` overflows in the credit and year-end score multiplications).

### 2.3 Division and rounding

One helper class, no other way:

The implemented version is `src/Lokanta.Core/Fx.cs`:

```csharp
public static class Fx
{
    public const int  One   = 10_000;         // 1.0 bp
    public const int  Micro = 1_000_000;      // 1 work-day
    public const long Nano  = 1_000_000_000L; // the internal precision of compounding multipliers
    public const int  Coin  = 100;            // 1 coin = 100 centi-coins

    /// a * b / c, half away from zero. The intermediate product is checked:
    /// a silent overflow is worse than a wrong result.
    public static long MulDiv(long a, long b, long c)
    {
        if (c == 0) throw new DivideByZeroException();
        long p; checked { p = a * b; }
        long q = p / c;
        long r = p - q * c;
        if (r == 0) return q;
        long ar = r < 0 ? -r : r;          // not Math.Abs: it throws on long.MinValue
        long ac = c < 0 ? -c : c;
        long twice; checked { twice = ar * 2; }
        if (twice >= ac) q += ((p < 0) == (c < 0)) ? 1 : -1;
        return q;
    }

    public static long Bp(long value, int bp) => MulDiv(value, bp, One);
    public static int  CeilDiv(int a, int b);      // crew arithmetic
    public static long CeilDivL(long a, long b);
    public static long PowNano(long baseNano, int exp);   // compounding rise
    public static long BpToNano(int bp);
}
```

Why `PowNano` exists: exponentiating the experience raise in basis points produced roughly 0.03% of drift by the eighth week, that is, a few coins. Compounding multipliers are computed at nano scale, and the result comes down to centi-coins in a single step.

- `Math.Round`, `Math.Floor`, truncation with `(int)`: **forbidden.** Only `Fx`.
- Banker's rounding (half to even) is **forbidden**; `MulDiv` rounds half away from zero. Reason: `Math.Round`'s default is banker's, and two developers will mix the two up.
- The decimals in the formula documents are converted to bp: `(0.5 + reputation/100)` → `5000 + reputation_centi / 1`, that is `5000 + 3000 = 8000 bp = 0.8`.

### What this rule caught in practice

10 September 2026, while the first slice of the core was being written. The C# core and the Python model agreed all the way to the eighth week but **diverged in the sixth week**: weekend demand was 62 in Python and 63 in C#.

The reason was exactly the thing this section forbids.

| | Calculation | Result |
|---|---|---|
| Raw value | 10 tables × 4 × 1.25 × 1.25 | **62.5**, exactly halfway |
| Python `round(62.5)` | Banker's rounding, half goes to even | 62 |
| C# `Fx.MulDiv` | Half away from zero | 63 |

Worse, Python **was not even consistent.** In the fourth week the raw value should have been 38.5, but floating-point noise produced 38.500000000000007 and `round()` went up this time, to 39. So the same script was doing banker's rounding on one line and noise-dependent rounding on another.

**The fix:** discrete decisions (customer count, crew) moved to integer arithmetic on the Python side too. The `mul_div` function in `tools/balance/model.py` applies the same rule as `Fx.MulDiv`. Money fields may stay decimal; their tolerance is 1 coin, because in the end they are rounded and displayed.

**The lesson:** this bug would never have shown up if two independent implementations had not been compared. A single implementation confirms itself. That is the reason the cross-validation line in section 9 exists.

### 2.4 An example of translating a formula

[12-economy.md](12-economy.md) §5.1:

```
customers = tables × 4 × (0.5 + reputation/100) × day_factor
```

In the core:

```csharp
// src/Lokanta.Core/Economy/DemandModel.cs
public static int CustomersPerDay(int tables, int reputationCenti,
                                  int basePerTable, int dayFactorBp)
{
    long seats = (long)tables * basePerTable;
    long numerator = seats * (Fx.One / 2 + reputationCenti) * dayFactorBp;
    return (int)Fx.MulDiv(numerator, 1, (long)Fx.One * Fx.One);
}
```

**No rounding in an intermediate step.** The first draft called `Fx.Bp` twice, that is, it rounded twice; the Python model rounds a single time. The numerator is carried all the way without dividing, and the rounding happens at the end.

The balance tool `tools/balance/model.py` now uses the same integer path (see the finding above). **Phase 0's first test:** the Python model and the C# core must produce the same eight-week table. Discrete fields exactly, money fields within 1 coin. `tests/Lokanta.Core.Tests/GoldenWeeklyTests.cs`.

### 2.5 Verification

- **Reflection test:** all the field, property, parameter and return types of all the types in the `Lokanta.Core` assembly are scanned; if a `float`, `double` or `decimal` is seen, the test fails.
- **Analyzer (later):** in Phase 1 the same rule moves into a Roslyn analyzer and becomes a compile error.

---

## 3. Randomness

### 3.1 The generator: xoshiro128**

Chosen. Reasons: its state is four `uint`s (written straight into the save file), it does not allocate, it does not need 64-bit multiplication on 32-bit ARM, its reference implementation is ten lines, and its quality is more than enough for a game. PCG32 would have done as well; there is no difference, one was picked.

```csharp
public struct Rng
{
    uint s0, s1, s2, s3;

    public uint Next()
    {
        uint result = RotL(s1 * 5, 7) * 9;
        uint t = s1 << 9;
        s2 ^= s0; s3 ^= s1; s1 ^= s2; s0 ^= s3;
        s2 ^= t;
        s3 = RotL(s3, 11);
        return result;
    }

    /// [0, maxExclusive). Lemire multiply-shift. There is a very small bias,
    /// accepted because it is deterministic; irrelevant for a game.
    public int NextInt(int maxExclusive) => (int)(((ulong)Next() * (ulong)maxExclusive) >> 32);
    public int NextBp() => NextInt(Fx.One + 1);                 // 0..10000
    public bool Chance(int bp) => NextInt(Fx.One) < bp;

    static uint RotL(uint x, int k) => (x << k) | (x >> (32 - k));
}
```

### 3.2 Streams

Every subsystem has its own stream. Adding a call to one system does not shift another one's sequence.

```csharp
public enum RngStream
{
    Arrival,      // the customer's arrival time
    Archetype,    // which archetype
    Order,        // what they order
    StaffError,   // does the staff member make a mistake
    Market,       // the daily ingredient price
    Event,        // the daily event die
    Hiring,       // the candidate pool
    ReviewText,   // choosing the review template
    Count
}
```

Seeding:

```csharp
static Rng Seed(ulong master, RngStream stream)
{
    ulong z = master ^ ((ulong)(stream + 1) * 0x9E3779B97F4A7C15UL);
    // splitmix64, four times
    uint Mix() {
        z += 0x9E3779B97F4A7C15UL;
        ulong x = z;
        x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
        x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
        return (uint)(x ^ (x >> 31));
    }
    return new Rng { s0 = Mix(), s1 = Mix(), s2 = Mix(), s3 = Mix() };
}
```

The save file writes the four `uint`s of state for every stream. No call counter is kept: restoring the state is O(1), fast-forwarding with a counter is O(n).

### 3.3 Prohibitions

| Forbidden | Reason |
|---|---|
| `System.Random` | Seedable, but its algorithm changed between .NET versions |
| `Guid.NewGuid()` | Random and platform dependent |
| `string.GetHashCode()` | Different in every process on .NET Core; it even changes dictionary order |
| Order-dependent iteration over a `Dictionary` | The order is not guaranteed. Use a list, or a `SortedDictionary` with an ordinal key |
| Order-dependent iteration over a `HashSet` | Same |
| `DateTime.Now`, `Environment.TickCount` | Real time does not get into the core |
| `UnityEngine.Random` | Unreachable anyway, the asmdef blocks it |

### 3.4 Verification

- **Stream independence test:** add a thousand extra calls to the `Event` stream, the `Arrival` sequence must not change.
- **Known value test:** for seed 0 the first five `Next()` outputs are pinned and compared against a reference implementation.

---

## 4. Culture and text

### 4.1 The trap

Turkish has two separate letter i's: a dotless one (U+0131 lowercase, U+0049 uppercase) and a dotted one (U+0069 lowercase, U+0130 uppercase). So in the Turkish culture `"ID".ToLower()` does not give `"id"` — the capital I lowercases to the **dotless** i — and `"file".ToUpper()` does not give `"FILE"`, because the i uppercases to a **dotted** capital I. Every non-ordinal comparison, sort and search behaves differently on a Turkish device. The developer is Turkish, his device is Turkish, most of the players are not: **the bug is invisible precisely on the developer's machine.**

### 4.2 The rules

| Rule | How |
|---|---|
| Comparing identifiers | `string.Equals(a, b, StringComparison.Ordinal)` or `a == b` (already ordinal) |
| Sorting identifiers | `StringComparer.Ordinal` |
| Lowercasing | `ToLowerInvariant()`; it should not be needed in the core anyway, identifiers are lowercase ASCII |
| Number parsing | The core does not parse numbers. The content loader uses `CultureInfo.InvariantCulture` |
| Number formatting | The core does not produce text. It returns numbers and keys, the view layer formats them |
| Identifier alphabet | `[a-z0-9_]+`, validation rejects anything else |

`Lokanta.Core` carries no `CultureInfo` reference anywhere, because it does no culture-sensitive operation. This is stronger than the rule: there is no need.

### 4.3 Verification

- **Culture test:** the same 60-day run is executed in the `tr-TR`, `en-US` and `de-DE` cultures (by changing `CultureInfo.CurrentCulture`), the three hashes must be equal.
- **Identifier test:** if an identifier outside `[a-z0-9_]+` is seen while content is loading, the load is rejected.

---

## 5. The boundary of floating point

`float` is allowed here: `Lokanta.View`, `Lokanta.UI`. Position, animation, camera, interface transitions. These are appearance, not simulation.

**The boundary rule:** no `float` crosses a port or the `Simulation` surface. The view layer takes integers and converts them itself:

```csharp
// the View side
Vector3 pos = new Vector3(tableX * 1.0f, 0f, tableY * 1.0f);   // from the integer grid
float fill = patienceMs / (float)patienceMaxMs;                  // for the bar
```

The view layer never affects the simulation with a `float`. A touch position is converted into a table id and sent as a command; the coordinate does not get into the core.

---

## 6. Library and infrastructure decisions

### 6.1 Content loading: Newtonsoft, only at startup, only in the Content layer

The options were evaluated:

| Option | Status |
|---|---|
| Newtonsoft (`com.unity.nuget.newtonsoft-json`) | **Chosen.** Unity's official package, works under IL2CPP, needs type protection via `link.xml` |
| System.Text.Json source generation | The technically most correct answer, but it means carrying seven DLLs into Unity; not worth that friction in Phase 0 |
| A hand-written parser | Maintenance load as the schema changes; too much for eleven schemas |

The rules:

- Newtonsoft is referenced only in the `Lokanta.Content` assembly. `Lokanta.Core` knows nothing about JSON; it receives ready DTOs.
- Every DTO field is marked explicitly with `[JsonProperty("name")]`. Reflection does not derive the name.
- `link.xml` protects all DTO types. The "the field came back empty" bug caused by IL2CPP stripping is lived through once in Phase 0, and never again.
- Loading happens once at startup. No JSON work during service.
- Numeric fields are written as **integers** in JSON (centi-coins, bp, ms). If a decimal is seen, validation rejects it.

### 6.2 The save file: one walk, two outputs

[15-save-system.md](15-save-system.md) decided on JSON + gzip + checksum; the review approved it (B6). That decision stands. But **serialization is done with a hand-written walk, not with reflection**:

```csharp
public interface IStateWriter
{
    void Begin(string key);  void End();
    void Int(string key, int v);
    void Long(string key, long v);
    void Str(string key, string v);          // identifiers only
    void Arr(string key, int count);         // followed by count elements
}

public interface IStateReader { /* symmetric */ }

public interface ISerializable
{
    void Write(IStateWriter w);
}
```

Every state type (`TableState`, `StaffState`, `CustomerState`, `Inventory`, `Loan`, ...) implements `Write` and a static `Read`. The field order is fixed and numbered in a comment in the class.

`IStateWriter` has two implementations:

| Implementation | Where | Job |
|---|---|---|
| `JsonStateWriter` | `Lokanta.App` | Writes the save file |
| `HashStateWriter` | `Lokanta.Core` | Produces the state hash with FNV-1a 64 |

**The same walk** produces both the file and the hash. If a field is forgotten in the save, the hash will not see it either and the determinism test cannot catch it; that is why there is a "was it added to the walk" checklist item for every new field (§10).

### 6.3 The event mechanism

No C# `event` or `delegate` leaves the core. Reason: if the view layer subscribes to the core, the lifecycles get tangled and the core swallows the view's exceptions.

```csharp
public readonly struct SimEvent
{
    public readonly long Tick;
    public readonly SimEventKind Kind;
    public readonly int A, B, C, D;     // payload slots; the meaning depends on Kind, documented in the enum
}
```

- The core writes events into a ring buffer of capacity 4,096.
- The driver calls `DrainEvents()` after every batch of ticks and distributes them to the view layer.
- If the buffer fills up: an assertion in the debug build, the oldest is dropped in the release build. Producing 4,096 events in one batch of ticks is a design error to begin with.
- The event payload is integers. No text. The view converts the id into a key and the key into text.

### 6.4 The composition root

One place, fixed order. `Lokanta.App.Bootstrap`, the only `MonoBehaviour` in the first scene:

```
1. The platform ports are created   (ISaveStore, IStoreFront, IAnalytics ... Platform.Mobile)
2. The content is loaded            (Content.Loader → ContentSet)
3. The content is validated         (Content.Validator; on an error the game does not open, error screen)
4. A save slot is picked / read     (ISaveStore → SaveEnvelope)
5. Simulation is constructed        (ContentSet + seed + snapshot if any + command log)
6. The command log is replayed      (§7)
7. The View is built, bound to the event buffer
8. SimDriver starts
```

No `MonoBehaviour` does `new Simulation()` on its own. There is no `static` singleton. A test starts at step 5 and skips 7 and 8.

---

## 7. Saving mid-service: the command log

### 7.1 The model

The save file is three parts:

```
SaveEnvelope
├── header        version, cuisine, day, tickIndex, seed, checksum, time written (informational only)
├── snapshot      the full state at the start of the day (the §6.2 walk)
└── commands[]    the commands applied since the start of the day, in tick order
```

Loading: the snapshot is restored, and while the commands are applied in order the ticks in between are run at maximum speed. Because it is deterministic the result is byte for byte the same as uninterrupted play.

### 7.2 The command

```csharp
public readonly struct Command          // 20 bytes
{
    public readonly long Tick;
    public readonly CommandKind Kind;   // int
    public readonly int A, B, C;
}

public enum CommandKind
{
    OpenService,          // morning → service
    CloseDay,             // service → end of day
    SetPrice,             // A dish, B centi-coins
    SetMenuSlot,          // A slot, B dish (−1 empty)
    SetDailySpecial,      // A dish
    OrderIngredient,      // A ingredient, B grams
    Hire,                 // A candidate
    Fire,                 // A staff member
    AssignStation,        // A staff member, B station
    Intervene,            // A table, B intervention kind (apology, treat, the owner's attention)
    Expand,               // A tier
    BuyEquipment,         // A equipment
    TakeLoan,             // A loan
    ExtendCredit,         // A regular, B centi-coins  (a tab, Turkish cuisine)
    CollectCredit,        // A regular
    RefillBroth,          // (Japanese cuisine)
    Count
}
```

**What are not commands:** speed, pause, camera, screen transitions. These are view state; they do not get into the simulation and are not saved.

### 7.3 Limits

| Quantity | Value | Source |
|---|---|---|
| Maximum commands per day | 256 | Touch budget 60, [16-screens-and-tutorial.md](16-screens-and-tutorial.md); 4× headroom |
| Maximum ticks per day | 4,800 service + 1,200 morning/evening | §1.3 |
| Daily replay time | < 0.5 s | 6,000 ticks × 50 µs |
| Command log size | ≤ 5 KB | 256 × 20 bytes |
| Snapshot | ≤ 40 KB uncompressed | 14 tables, 12 staff, 30 customers, 26 ingredients, 32 dishes, 10 regulars |

If 256 is exceeded the command is rejected and an event is produced; in practice it is unreachable.

### 7.4 When it is written

| Moment | What is written |
|---|---|
| `CloseDay` | A new snapshot, the log is cleared. **Full save** |
| After every command | The log appendix only. Cheap; 20 bytes |
| Android `OnApplicationPause(true)` | The log; the snapshot already exists |
| Every 30 s of service | The log; even with no commands, tickIndex has moved on |

The write follows the rules in [15-save-system.md](15-save-system.md) and [19-technical-setup.md](19-technical-setup.md): write to a temporary file, `Flush(true)`, rename, keep a `.bak`, CRC32.

### 7.5 The day boundary

When `CloseDay` is applied: the end-of-day accounting is done, the state moves to the new day, the snapshot is written as **the start of the new day**, the log is reset. That way a save file carries at most one day's worth of commands.

### 7.6 Verification

- **Interruption test:** a 60-day run; save, load and continue at 200 random points. The final hash must equal the uninterrupted run.
- **Version test:** an old-version snapshot + log is loaded through the migration chain and replayed.

---

## 8. Schema additions

The review found four gaps in [13-data-schemas.md](13-data-schemas.md). All of them are written with integer units (§2.2).

### 8.1 Staff capacity and pool

[14-staff-system.md](14-staff-system.md) defines the two-pool model. Into the schema:

```json
{
  "id": "garson",
  "nameKey": "role.garson",
  "pool": "salon",
  "dailyWage": 11000,
  "capacityPerDay": 25,
  "stations": ["salon"],
  "xpSpeedBp": [10000, 11000, 12000, 13000]
}
```

Into `economy.json`:

```json
"staffing": {
  "ownerWorkCentiDays": 140,
  "ownerPool": "salon",
  "tiers": [
    { "tables": 4,  "rent": 195000,  "upgrade": 0,       "staffCap": 3 },
    { "tables": 7,  "rent": 435000,  "upgrade": 325000,  "staffCap": 5 },
    { "tables": 10, "rent": 685000,  "upgrade": 585000,  "staffCap": 8 },
    { "tables": 14, "rent": 1045000, "upgrade": 1040000, "staffCap": 12 }
  ],
  "weeklyXpWageGrowthBp": 220
}
```

These numbers are the output of `tools/balance/model.py`; the JSON is produced by the writer, not by hand (a Phase 0 job: a JSON target is added to `render.py`).

### 8.2 Signature mechanic parameters

The mechanic lives in code, the numbers live in data. A `signature` block per cuisine inside `cuisines.json`:

```json
"signature": { "kind": "combo",
  "combo": { "items": ["hamburger", "patates", "gazoz"], "priceBp": 8125, "kitchenLoadBp": 12000, "ticketBonusBp": 1500 } }

"signature": { "kind": "credit",
  "credit": { "maxPerRegular": 300000, "dueDays": 7, "collectChanceBp": 8500,
              "defaultRepPenaltyCenti": 300, "loyaltyBonusBp": 1500, "teaCostCenti": 200 } }

"signature": { "kind": "courses",
  "courses": { "gapMs": 45000, "toleranceMs": 15000, "onTimeBonusCenti": 800, "lateBonusCenti": -1200 } }

"signature": { "kind": "broth",
  "broth": { "potPortions": 40, "refillMs": 1800000, "soldOutPenaltyCenti": -3000, "freshBonusCenti": 500 } }
```

If the `kind` is unknown, validation rejects it. If the block is missing, the cuisine is not loaded.

### 8.3 Dish parameters

The review's shared finding: "32 dishes only mean something if every dish is parameterised." Four mandatory fields:

```json
{
  "id": "hamburger",
  "nameKey": "dish.hamburger",
  "cuisine": "fastfood",
  "price": 4500,
  "prepMs": 90000,
  "station": "izgara",
  "complexity": 1,
  "unlockSeason": 1,
  "ingredients": [ { "id": "kofte_harci", "grams": 120 }, { "id": "ekmek", "grams": 80 } ],
  "plating": { "base": "bun", "toppings": ["patty", "lettuce"] }
}
```

| Field | What it does |
|---|---|
| `prepMs` | Weights the cook's capacity per dish; if a heavy dish sells a lot the kitchen jams |
| `station` | Which equipment is needed; without it the dish cannot go on the menu |
| `complexity` | 1-3; the staff error probability and the apprentice penalty depend on it |
| `ingredients[].grams` | The ingredient cost derives from the gram weight, not from the price; market fluctuation works through here |
| `plating` | For the view layer; the simulation does not read it. See [24-art-pipeline.md](24-art-pipeline.md) |

### 8.4 Integer units, everywhere

Every decimal field in the existing schemas is converted:

| Old | New |
|---|---|
| `"baseSpeed": 1.0` | `"baseSpeedBp": 10000` |
| `"effects": { "speed": 0.18 }` | `"effects": { "speedBp": 1800 }` |
| `"dailyWage": 140` | `"dailyWage": 14000` (centi-coins) |
| `"patienceSec": 8` | `"patienceMs": 8000` |
| `"priceSensitivity": 2.5` | `"priceSensitivityBp": 25000` |

If the validator sees a decimal point in the JSON it rejects the file. No exceptions.

---

## 9. Verification and continuous integration

### 9.1 Startup validation

While the content is loading, before the game opens:

1. All ids are `[a-z0-9_]+` and unique within the file
2. All cross references resolve (dish → ingredient, role → station, regular → archetype)
3. There are no decimal numbers
4. Every cuisine has a `signature` and the `kind` is recognised
5. Every dish has the four mandatory parameters
6. `staffing.tiers` is increasing by table count, `staffCap` is increasing
7. Every `nameKey` has a counterpart in every language (a warning, not an error)

On an error the game does not open, it shows an error screen with which file and which line. No silent defaults.

### 9.2 The state hash

```csharp
public static ulong Hash(Simulation sim)
{
    var w = new HashStateWriter();        // FNV-1a 64; every int is 4 bytes little-endian, long is 8 bytes
    sim.Write(w);
    return w.Result;
}
```

The hash is not an object's `GetHashCode`; it is an explicit byte walk. The field order is the order inside `Write`.

### 9.3 The determinism line

| Test | Where | What |
|---|---|---|
| Golden run | `dotnet test`, desktop | Seed 20260909, the "good player" script, 60 days → the hash is pinned and written into the repository |
| Cross platform | Android IL2CPP build, debug scene | The same run on the device, the hash is written to logcat and compared with the desktop |
| Culture | `dotnet test` | tr-TR, en-US, de-DE |
| Frame independence | `dotnet test` | MaxTicksPerFrame 1 and 5 |
| Interruption | `dotnet test` | 200 random save/loads |
| Python match | `dotnet test` | The C# eight-week table, within ±1 of the `model.py` table |

If the golden hash changes, either a bug was fixed or a bug was added; either way it is explained in the commit message.

### 9.4 Without a device test

There is no Mac, the first release is Android. The cross-platform test is done with one Android device. An emulator is not accepted ([20-production-decisions.md](20-production-decisions.md)); it is IL2CPP's real ARM build that is being tested.

---

## 10. Acceptance criteria: when is Batch B done

- [ ] `Simulation.Tick()` takes no parameters; `SimDriver` accumulates, ceiling 5
- [x] **The `Lokanta.Core` reflection test passes:** no `float`/`double`/`decimal` in fields, properties, signatures or constructors; and no reference to Unity, Newtonsoft or System.Text.Json either
- [x] **The `Fx` class exists**, and there is no other division or rounding in the core. `MulDiv`, `Bp`, `CeilDiv`, `PowNano`
- [x] **`Rng` is xoshiro128**, eight streams.** The known value test matches not the C# output but an independent Python implementation (`tools/balance/rng_reference.py`)
- [ ] A code search test for the prohibition list (§3.3): `System.Random`, `GetHashCode()`, `DateTime.Now`, `Guid.NewGuid` do not appear in the core
- [x] **The culture test** gives the same result in the tr-TR, en-US, de-DE and ar-SA cultures
- [ ] Newtonsoft only in `Lokanta.Content`; `link.xml` protects the DTOs
- [ ] `IStateWriter` has two implementations; every state type has `Write`/`Read`; the new-field checklist is in the PR template
- [ ] The `SimEvent` ring buffer; no `event` leaves the core
- [ ] `Bootstrap` has eight steps, in order; no `static` singleton
- [ ] The command log: sixteen command kinds, the 256 limit, the interruption test passes
- [ ] The schemas are updated per §8; the validator applies the seven rules
- [ ] The golden hash is in the repository; the same hash was seen once on an Android device and recorded
- [x] **The C# core matches `tools/balance/model.py` on the eight-week table.** 10 September 2026. Discrete fields (customers, crew) exactly; money fields within 1 coin. 68 tests pass

The last item is Phase 0 itself: this file makes it possible, it does not replace it.

---

## Details awaiting a decision

1. Is a 4,800-tick service day (8 minutes at 1x) the right length; playability testing will say
2. Is `MaxTicksPerFrame = 5` enough on a low-end device; device testing will say
3. Is the Roslyn analyzer a Phase 1 job, or earlier
