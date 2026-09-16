# -*- coding: utf-8 -*-
"""
Lokanta time model - Batch B
============================================================================
Purpose: derive every duration in docs/27-time-model.md from a formula and
verify the internal consistency with a single command. No hand-written ms.

Why it exists: three files contradicted one another.
  1. docs/23 1.3   -> the service day is 480,000 ms
  2. docs/14       -> a cook's capacity is 28 customers/day
  3. content/dishes/fastfood.json -> hamburger prepMs 75,000

  480,000 / 28 = 17,143 ms of cook's time per customer. If a single hamburger
  takes 75,000 ms then a cook makes 6 hamburgers a day, not 28 customers. The
  generated prepMs values and the capacity model were roughly 6 times apart.

The fix has two parts:
  - Task durations are DERIVED from the capacity (section 2 below).
  - prepMs is defined as "wall clock", not "cook busy"; the cook's busy time
    is prepMs x attendBp (section 4, simultaneity).

Running it:
    python timing.py            -> markdown tables
    python timing.py --check    -> consistency tests (PLAIN ASCII report)
"""
import sys

# ============================================================================
# 1. BASE CONSTANTS
# ============================================================================

TICK_MS = 100                    # docs/23 1.2, fixed step
SERVICE_TICKS = 4800             # DECISION A: unchanged
SERVICE_DAY_MS = SERVICE_TICKS * TICK_MS          # 480,000
PREP_TICKS = 1200                # morning counter + evening accounts, docs/23 7.3
DAY_TICKS = SERVICE_TICKS + PREP_TICKS
CAMPAIGN_DAYS = 60               # content/economy.json
TOUCH_BUDGET = 60                # docs/16, the daily touch ceiling

# --- Capacities. THESE ARE THIS TOOL'S OWN, AND THEY NO LONGER MATCH
#     THE SHIPPED ECONOMY. The comment here used to claim they were "the
#     same as model.py, which is the single source of truth". They are
#     not, and nothing was checking.
#
# They cannot simply be replaced by model.py's: this file's millisecond
# budgets were derived FROM these capacities (B1: waiter_ms x 25 is
# exactly the service day), so importing model.py's 26 breaks five of
# this tool's own invariants. Re-deriving the budgets is a balance
# decision, not a tidy-up, so the drift is REPORTED instead - see
# check_against_model() at the bottom of the checks.
import os as _os
import sys as _sys
_sys.path.insert(0, _os.path.dirname(_os.path.abspath(__file__)))
import model as _model  # noqa: E402

CAP_COOK = 28
CAP_WAITER = 25
CAP_DISHWASHER = 46
CAP_CASHIER = 66
HALL_LOAD = 1.0 / CAP_WAITER + 1.0 / CAP_DISHWASHER + 1.0 / CAP_CASHIER

# --- The peak day: the week 8 weekend row of model.py
PEAK_CUSTOMERS = 97
PEAK_COOKS = 4
PEAK_HALL = 7
OWNER_WORK = 1.4                 # the owner's hall work-day contribution
PEAK_TABLES = 14

# --- Values measured over content/archetypes/*.json (the fast food pool)
#     Measurement: weighted by weight x average party size.
GROUP_SIZE = 2.1195              # all archetypes, heads/party
GROUP_SIZE_SEATED = 2.1847       # excluding the courier (a courier takes no table)
TAKEAWAY_HEAD_SHARE = 0.0259     # the courier's share of heads
PATIENCE_MIN_MS = 8000           # the courier
PATIENCE_MAX_MS = 30000          # the family (in the fast food pool)

# --- Menu measurement: content/dishes/fastfood.json
#     The order mix is 1 main + 0.6 side + 0.5 drink + 0.1 dessert = 2.2 items.
#     Verification: 2.2 items x the group average prices = 74.2 coins, and
#     model.py's week 8 target ticket is 75 coins.
ORDER_MIX = [
    # (group,    count/head, menu average complexity, average price in centi-coins)
    ("ana",      1.0, 1.6667, 4516.7),
    ("yan",      0.6, 1.1250, 2618.8),
    ("icecek",   0.5, 1.0000, 1883.3),
    ("tatli",    0.1, 2.1667, 3925.0),
]
DISHES_PER_CUSTOMER = sum(m[1] for m in ORDER_MIX)              # 2.2
COMPLEXITY_UNITS = sum(m[1] * m[2] for m in ORDER_MIX)          # 3.0584
TICKET_TARGET = 7500             # centi-coins, model.py week 8

# --- Arrival slots: docs/12 5.6
SLOT_COUNT = 4
SLOT_SHARE_CONTENT = [0.111, 0.375, 0.162, 0.353]   # measured, fast food
SLOT_SHARE_TURK = [0.094, 0.606, 0.175, 0.125]      # measured, Turkish
SLOT_SHARE_PROPOSED = [0.20, 0.30, 0.20, 0.30]      # DECISION F

# --- The patience distribution: content/archetypes (the fast food pool),
#     weighted by heads
PATIENCE_HEADS = [
    (8000, 550.0), (9000, 210.0), (10000, 1950.0), (13000, 800.0),
    (14000, 495.0), (15000, 390.0), (16000, 3300.0), (17000, 1350.0),
    (18000, 3600.0), (19000, 440.0), (20000, 1300.0), (21000, 570.0),
    (22000, 100.0), (24000, 1700.0), (25000, 300.0), (26000, 165.0),
    (27000, 1050.0), (28000, 525.0), (30000, 2400.0),
]
PATIENCE_HEAD_TOTAL = sum(h for _, h in PATIENCE_HEADS)   # 21195

# --- The record of the contradiction: content/dishes/fastfood.json, the
#     morning of 10 September 2026. These two numbers are why this file was
#     written. The content was edited during the analysis; the values below
#     DO NOT CHANGE, they stand as the record of the contradiction.
CONFLICT_AVG_PREP_MS = 92656      # the average of the 32 dishes
CONFLICT_BURGER_PREP_MS = 75000   # the hamburger

# --- The interim state the content reached at the end of the analysis (also
#     not derived): prepMs tied only to complexity, the station dropped.
#     fast food 8000 / 11500 / 17500, Turkish 6000 / 8500 / 12500.
#     With the order mix that is 20,688 ms per customer; counted as the cook's
#     time, 480,000 / 20,688 = 23.2 customers/day, while the capacity model
#     says 28. A 17% deviation.
INTERIM_KITCHEN_MS = 20688

# --- Plates per day per station (from the menu's station distribution)
#     12 mains: grill 10 (4 at k=1, 6 at k=2), hob 2 (k=2)
#     8 sides:  hob 6 (5 at k=1, 1 at k=2), cold 2 (k=1)
#     6 drinks (k=1), 6 desserts: dessert 2 (k=1, k=2), oven 4 (2 at k=2, 2 at k=3)
STATION_DISH_MIX = [
    # (station, items/customer, list of average prepMs keys)
    ("izgara", 1.0 * 10 / 12, [("izgara", 1)] * 4 + [("izgara", 2)] * 6),
    ("ocak", 1.0 * 2 / 12, [("ocak", 2)] * 2),
    ("ocak", 0.6 * 6 / 8, [("ocak", 1)] * 5 + [("ocak", 2)]),
    ("soguk", 0.6 * 2 / 8, [("soguk", 1)] * 2),
    ("icecek", 0.5, [("icecek", 1)] * 6),
    ("tatli", 0.1 * 2 / 6, [("tatli", 1), ("tatli", 2)]),
    ("firin", 0.1 * 4 / 6, [("firin", 2)] * 2 + [("firin", 3)] * 2),
]


# ============================================================================
# 2. TASK DURATIONS - derived from the capacity
# ============================================================================
# The rule: the total ms a role spends on one customer = the day / the
# capacity. That way the sentence "25 customers a day" and the sentence
# "19,200 ms per customer" are the same sentence.

def role_ms(capacity):
    """A role's total time per customer, in ms. Halves away from zero."""
    return (2 * SERVICE_DAY_MS + capacity) // (2 * capacity)


KITCHEN_MS = role_ms(CAP_COOK)          # 17,143
WAITER_MS = role_ms(CAP_WAITER)         # 19,200 (divides exactly)
DISHWASHER_MS = role_ms(CAP_DISHWASHER)  # 10,435
CASHIER_MS = role_ms(CAP_CASHIER)       # 7,273
HALL_MS = WAITER_MS + DISHWASHER_MS + CASHIER_MS   # 36,908

# --- The waiter's time split into three parts (per head, summing to WAITER_MS)
T_SEAT = 3200      # greeting and seating
T_ORDER = 7000     # taking the order; the longest waiter job, the menu
                   # decision happens here
T_SERVE = 9000     # carrying the tray to the table and handing it out
# --- The dishwasher's time in two parts (summing to DISHWASHER_MS)
T_BUS = 4435       # clearing the table - THIS BLOCKS THE TABLE
T_WASH = 6000      # washing up at the sink - the table is free
# --- The cashier
T_PAY = CASHIER_MS  # 7,273, closing the bill and taking payment

# --- Durations that consume no staff
T_EAT_PARTY = 38000   # how long a table takes to eat; a cuisine parameter
                      # (fast food is short; it will get longer in an
                      # Italian cuisine)

# The staff time per head that blocks the table = the hall time minus the washing up
TABLE_STAFF_MS = HALL_MS - T_WASH   # 30,908


# ============================================================================
# 3. DERIVING prepMs
# ============================================================================
# What consumes the cook pool is not prepMs but the time the cook is BUSY WITH
# THAT DISH. The two numbers are not the same: while the cake is in the oven
# the cook does something else.
#
#   cookBusyMs(complexity) = UNIT_BUSY_MS x complexity
#   prepMs(station, complexity) = cookBusyMs x 10000 / attendBp(station)
#
# UNIT_BUSY_MS was solved from this equation:
#   the DISHES_PER_CUSTOMER-weighted complexity total x UNIT_BUSY = KITCHEN_MS
#   3.0584 x UNIT_BUSY = 17,143  ->  UNIT_BUSY = 5,605
# 5,600 was chosen: prepMs comes out as a whole number at every station, and
# the deviation is 0.09%.

UNIT_BUSY_MS = 5600

# attendBp: what percentage of the wall clock passes in the cook's HANDS.
# This number is the physical nature of the station; an equipment tier can
# lower it.
ATTEND_BP = {
    "icecek": 10000,   # fills the glass, no gap
    "soguk": 10000,    # chopping salad, entirely by hand
    "tatli": 8000,     # dressing the plate
    "izgara": 5600,    # put it on, turn it, take it off; there are gaps
    "ocak": 3500,      # drops the basket in and leaves it
    "firin": 2000,     # puts it in, closes the door, walks away
}
STATION_ORDER = ["icecek", "soguk", "tatli", "izgara", "ocak", "firin"]


def cook_busy_ms(complexity):
    """How long the cook is busy with one dish. This is the number that
    consumes the pool."""
    return UNIT_BUSY_MS * complexity


def prep_ms(station, complexity):
    """The dish's wall-clock duration. This is the prepMs written into JSON."""
    return cook_busy_ms(complexity) * 10000 // ATTEND_BP[station]


def kitchen_ms_achieved():
    """The cook's time per customer, worked back from the derived prepMs values."""
    return UNIT_BUSY_MS * COMPLEXITY_UNITS


def menu_avg_prep_ms():
    """The average derived prepMs over the 32 dishes. For comparison with the
    content."""
    all_dishes = [v for _, _, variants in STATION_DISH_MIX for v in variants]
    return sum(prep_ms(s, c) for s, c in all_dishes) / float(len(all_dishes))


# The wall-clock wait of the reference order: a table's food comes out
# together, so the wait = the SLOWEST first-course dish in the order.
# A typical order: the main on the grill at complexity 2 -> 20,000 ms; the
# side on the hob at complexity 1 -> 16,000 ms; the drink 5,600 ms. The
# slowest is the main.
COOK_WAIT_REF_MS = prep_ms("izgara", 2)   # 20,000


# ============================================================================
# 4. TABLE OCCUPANCY
# ============================================================================

def table_ms_per_party(group=GROUP_SIZE_SEATED, cook_wait=COOK_WAIT_REF_MS,
                       eat=T_EAT_PARTY):
    """
    One table turn: seating + ordering + waiting for the food + service +
    eating + paying + clearing. The washing up does not block the table, so
    it is left out.
    """
    return int(round(group * TABLE_STAFF_MS + cook_wait + eat))


TABLE_TURN_MS = table_ms_per_party()


# ============================================================================
# 5. POOL UTILISATION
# ============================================================================

def utilisation(customers=PEAK_CUSTOMERS, cooks=PEAK_COOKS,
                hall=PEAK_HALL, tables=PEAK_TABLES, day_ms=SERVICE_DAY_MS):
    """Each pool's day-average utilisation on the peak day."""
    kitchen_need = customers * KITCHEN_MS
    kitchen_have = cooks * day_ms
    hall_need = customers * HALL_MS
    hall_have = (hall + OWNER_WORK) * day_ms
    seated = customers * (1.0 - TAKEAWAY_HEAD_SHARE)
    parties = seated / GROUP_SIZE_SEATED
    table_need = parties * TABLE_TURN_MS
    table_have = tables * day_ms
    return dict(
        kitchen=kitchen_need / kitchen_have,
        hall=hall_need / hall_have,
        table=table_need / table_have,
        parties=parties,
        kitchen_need=kitchen_need, kitchen_have=kitchen_have,
        hall_need=hall_need, hall_have=hall_have,
        table_need=table_need, table_have=table_have,
    )


def slot_queue(shares, customers=PEAK_CUSTOMERS, cooks=PEAK_COOKS,
               hall=PEAK_HALL, tables=PEAK_TABLES):
    """
    A fluid queue per slot. A slot is 1/4 of the day; so is the capacity.
    If a slot's share is greater than 0.25 then work piles up in that slot;
    the backlog is the idle waiting time of the last customer to arrive at
    the end of the slot.
    """
    u = utilisation(customers, cooks, hall, tables)
    slot_ms = SERVICE_DAY_MS // SLOT_COUNT
    out = []
    for i, w in enumerate(shares):
        k_need = w * u["kitchen_need"]
        k_have = cooks * slot_ms
        s_need = w * u["hall_need"]
        s_have = (hall + OWNER_WORK) * slot_ms
        t_need = w * u["table_need"]
        t_have = tables * slot_ms
        k_wait = max(0.0, k_need - k_have) / cooks
        s_wait = max(0.0, s_need - s_have) / (hall + OWNER_WORK)
        t_wait = max(0.0, t_need - t_have) / tables
        out.append(dict(
            slot=i + 1, share=w,
            kitchen=k_need / k_have, hall=s_need / s_have, table=t_need / t_have,
            wait_max=k_wait + s_wait + t_wait,
            wait_avg=(k_wait + s_wait + t_wait) / 2.0,
            wait_takeaway=s_wait / 2.0,   # the courier joins neither the table
                                          # queue nor the kitchen queue
        ))
    return out


def max_slot_share():
    """The maximum slot share for which no pool exceeds 100% within the slot."""
    u = utilisation()
    return 0.25 / max(u["kitchen"], u["hall"], u["table"])


# ============================================================================
# 6. PATIENCE
# ============================================================================
# DECISION E: the patience counter only runs during IDLE WAITING. Cooking time
# is not counted in full, it is counted with the weight COOK_PATIENCE_BP.
#
#   wait_score = idle_wait_ms + cooking_ms x COOK_PATIENCE_BP / 10000
#
# Idle waiting = the time during which nobody is attending to the customer and
# their food has not started either: the table queue, the waiter queue, the
# kitchen queue, a plate waiting at the pass.

COOK_PATIENCE_BP = 2500


def patience_spent(idle_ms, cook_ms=COOK_WAIT_REF_MS):
    return idle_ms + cook_ms * COOK_PATIENCE_BP / 10000.0


def zero_load_wall_clock(group=1, station="izgara", complexity=1):
    """The WALL-CLOCK time from the door to the first mouthful at zero load."""
    return (T_SEAT + T_ORDER + prep_ms(station, complexity) + T_SERVE) * 1


def zero_load_takeaway(station="izgara", complexity=1):
    """The courier: no table, no sitting down, no eating."""
    return T_ORDER + prep_ms(station, complexity) + T_SERVE


def lost_head_share(idle_ms, idle_takeaway_ms=None, cook_ms=COOK_WAIT_REF_MS):
    """
    The share of heads whose patience runs out and who walk away at the given
    idle wait.

    The courier (the PATIENCE_MIN_MS step) does not join the table queue or
    the kitchen queue, only the hall queue; they are evaluated with a separate
    waiting time.
    """
    if idle_takeaway_ms is None:
        idle_takeaway_ms = idle_ms
    spent = patience_spent(idle_ms, cook_ms)
    spent_ta = patience_spent(idle_takeaway_ms, prep_ms("izgara", 1))
    lost = 0.0
    for pat, h in PATIENCE_HEADS:
        if pat == PATIENCE_MIN_MS:
            if pat <= spent_ta:
                lost += h
        elif pat <= spent:
            lost += h
    return lost / PATIENCE_HEAD_TOTAL


def daily_loss_share(shares=SLOT_SHARE_PROPOSED):
    """The share of customers lost across the day, each slot with its own queue."""
    tot = 0.0
    for r in slot_queue(shares):
        tot += r["share"] * lost_head_share(r["wait_avg"], r["wait_takeaway"])
    return tot


# ============================================================================
# 6b. STATION SLOTS
# ============================================================================
# Because simultaneity is allowed, how many plates can sit at one station at
# the same time is a separate constraint. An equipment tier raises that number.

def station_slots(shares=SLOT_SHARE_PROPOSED, customers=PEAK_CUSTOMERS):
    peak_share = max(shares)
    slot_ms = SERVICE_DAY_MS // SLOT_COUNT
    need = {}
    for station, per_customer, variants in STATION_DISH_MIX:
        dishes = customers * per_customer * peak_share
        avg_prep = sum(prep_ms(s, c) for s, c in variants) / float(len(variants))
        need[station] = need.get(station, 0.0) + dishes * avg_prep / slot_ms
    return need


# ============================================================================
# 7. CAMPAIGN LENGTH
# ============================================================================

def campaign_hours(speed=1):
    return CAMPAIGN_DAYS * DAY_TICKS * TICK_MS / 1000.0 / 3600.0 / speed


# ============================================================================
# 8. OUTPUT
# ============================================================================

def table_tasks():
    rows = [
        ("Waiting for a table", "-", "none", 0, "Queue only; patience burns here"),
        ("Seating", "head", "hall/waiter", T_SEAT, "Blocks the table"),
        ("Taking the order", "head", "hall/waiter", T_ORDER, "Blocks the table"),
        ("Cooking (the wait)", "table", "kitchen/cook", COOK_WAIT_REF_MS,
         "Wall clock; the slowest first-course dish"),
        ("Service", "head", "hall/waiter", T_SERVE, "Blocks the table"),
        ("Eating", "table", "none", T_EAT_PARTY, "Blocks the table"),
        ("Payment", "head", "hall/cashier", T_PAY, "Blocks the table"),
        ("Clearing the table", "head", "hall/dishwasher", T_BUS, "Blocks the table"),
        ("Washing up", "head", "hall/dishwasher", T_WASH, "The table is free"),
    ]
    L = ["| Task | Unit | Pool | ms | Note |", "|---|---|---|---|---|"]
    for r in rows:
        L.append("| {} | {} | {} | {} | {} |".format(r[0], r[1], r[2], r[3], r[4]))
    return "\n".join(L)


def table_prep():
    L = ["| Station | attendBp | k=1 | k=2 | k=3 |", "|---|---|---|---|---|"]
    for s in STATION_ORDER:
        L.append("| {} | {} | {} | {} | {} |".format(
            s, ATTEND_BP[s], prep_ms(s, 1), prep_ms(s, 2), prep_ms(s, 3)))
    return "\n".join(L)


def table_util():
    u = utilisation()
    L = ["| Pool | ms needed | ms available | Utilisation |", "|---|---|---|---|"]
    L.append("| Kitchen (4 cooks) | {:.0f} | {:.0f} | {:.1f}% |".format(
        u["kitchen_need"], u["kitchen_have"], 100 * u["kitchen"]))
    L.append("| Hall (7 + owner 1.4) | {:.0f} | {:.0f} | {:.1f}% |".format(
        u["hall_need"], u["hall_have"], 100 * u["hall"]))
    L.append("| Tables (14) | {:.0f} | {:.0f} | {:.1f}% |".format(
        u["table_need"], u["table_have"], 100 * u["table"]))
    return "\n".join(L)


def table_slots(shares, label):
    L = ["| Slot | Share | Kitchen | Hall | Tables | Max idle wait ms |",
         "|---|---|---|---|---|---|"]
    for r in slot_queue(shares):
        L.append("| {} | {:.1f}% | {:.1f}% | {:.1f}% | {:.1f}% | {:.0f} |".format(
            r["slot"], 100 * r["share"], 100 * r["kitchen"], 100 * r["hall"],
            100 * r["table"], r["wait_max"]))
    return label + "\n\n" + "\n".join(L)


# ============================================================================
# 9. CONSISTENCY TESTS
# ============================================================================

def checks():
    p = []
    D = SERVICE_DAY_MS

    # --- A: the length of the day
    p.append(("A1 service day 4800 ticks = 480,000 ms", D == 480000))
    p.append(("A2 campaign is exactly 10 hours at 1x speed",
              abs(campaign_hours(1) - 10.0) < 1e-9))
    p.append(("A3 touch interval in the 6-12 s band",
              6000 <= D / TOUCH_BUDGET <= 12000))

    # --- B: task durations reproduce the capacity exactly
    p.append(("B1 waiter ms x 25 = the service day (EXACTLY)", WAITER_MS * 25 == D))
    p.append(("B2 kitchen ms x 28 = the service day (+-28)",
              abs(KITCHEN_MS * CAP_COOK - D) <= CAP_COOK))
    p.append(("B3 dishwasher ms x 46 = the service day (+-46)",
              abs(DISHWASHER_MS * CAP_DISHWASHER - D) <= CAP_DISHWASHER))
    p.append(("B4 cashier ms x 66 = the service day (+-66)",
              abs(CASHIER_MS * CAP_CASHIER - D) <= CAP_CASHIER))
    p.append(("B5 seating+order+service = waiter ms",
              T_SEAT + T_ORDER + T_SERVE == WAITER_MS))
    p.append(("B6 clearing+washing = dishwasher ms", T_BUS + T_WASH == DISHWASHER_MS))
    p.append(("B7 payment = cashier ms", T_PAY == CASHIER_MS))
    p.append(("B8 hall total = the sum of the three roles",
              HALL_MS == WAITER_MS + DISHWASHER_MS + CASHIER_MS))
    p.append(("B9 hall total = the day x HALL_LOAD",
              abs(HALL_MS - D * HALL_LOAD) <= 1.0))
    p.append(("B10 the time blocking the table = the hall minus the washing up",
              TABLE_STAFF_MS == HALL_MS - T_WASH))
    for cap, ms, nm in ((CAP_COOK, KITCHEN_MS, "cook"), (CAP_WAITER, WAITER_MS, "waiter"),
                        (CAP_DISHWASHER, DISHWASHER_MS, "dishwasher"),
                        (CAP_CASHIER, CASHIER_MS, "cashier")):
        p.append(("B11 day/{}_ms gives the {} capacity back".format(nm, nm),
                  int(round(D / float(ms))) == cap))

    # --- C: deriving prepMs
    p.append(("C1 items/customer = 2.2", abs(DISHES_PER_CUSTOMER - 2.2) < 1e-9))
    p.append(("C2 the derived cook time is within +-50 ms of the capacity",
              abs(kitchen_ms_achieved() - KITCHEN_MS) <= 50))
    p.append(("C3 every prepMs value is a positive whole number",
              all(prep_ms(s, c) * ATTEND_BP[s] == cook_busy_ms(c) * 10000
                  for s in STATION_ORDER for c in (1, 2, 3))))
    p.append(("C4 prepMs is linear in complexity",
              all(prep_ms(s, 2) == 2 * prep_ms(s, 1) and
                  prep_ms(s, 3) == 3 * prep_ms(s, 1) for s in STATION_ORDER)))
    ticket = sum(m[1] * m[3] for m in ORDER_MIX)
    p.append(("C5 2.2 items x the menu prices is within 10% of the target ticket",
              abs(ticket - TICKET_TARGET) / TICKET_TARGET < 0.10))
    p.append(("C6 record of the contradiction: the old content average was 4x the derived one",
              CONFLICT_AVG_PREP_MS / menu_avg_prep_ms() > 4.0))
    p.append(("C7 record of the contradiction: with the old hamburger value a cook "
              "would make fewer than 7 hamburgers a day",
              D / float(CONFLICT_BURGER_PREP_MS) < 7))
    p.append(("C8 the interim content does not hit the capacity either (over 10% deviation)",
              abs(INTERIM_KITCHEN_MS - KITCHEN_MS) / float(KITCHEN_MS) > 0.10))

    # --- D: simultaneity
    p.append(("D1 prepMs >= cookBusy at every station (busy <= wall clock)",
              all(prep_ms(s, c) >= cook_busy_ms(c)
                  for s in STATION_ORDER for c in (1, 2, 3))))
    p.append(("D2 without simultaneity the average prepMs would fall below 8 s",
              KITCHEN_MS / DISHES_PER_CUSTOMER < 8000))
    slots = station_slots()
    p.append(("D3 no station wants more than 4 slots at the peak",
              max(slots.values()) <= 4.0))
    p.append(("D4 the simultaneous plate count is greater than the cook count "
              "(simultaneity's visible consequence)",
              sum(slots.values()) > PEAK_COOKS))

    # --- E: patience
    p.append(("E1 at zero load the idle wait is zero", patience_spent(0, 0) == 0))
    fastest = PATIENCE_MIN_MS
    p.append(("E2 the least patient archetype survives at zero load (courier, grill k=1)",
              patience_spent(0, prep_ms("izgara", 1)) < fastest))
    p.append(("E3 at zero load the TOTAL wall-clock wait exceeds the patience "
              "(proof that the old definition was broken)",
              zero_load_takeaway("izgara", 1) > fastest))
    peak = slot_queue(SLOT_SHARE_PROPOSED)[1]
    p.append(("E4 on the proposed profile the peak average stays under the 10 s patience",
              patience_spent(peak["wait_avg"], prep_ms("izgara", 1)) < 10000))
    p.append(("E5 on the content profile the peak average exceeds EVEN the 30 s patience",
              patience_spent(slot_queue(SLOT_SHARE_CONTENT)[1]["wait_avg"],
                             COOK_WAIT_REF_MS) > PATIENCE_MAX_MS))
    p.append(("E6 the courier survives the peak on average (it joins neither the "
              "table nor the kitchen queue)",
              patience_spent(peak["wait_takeaway"], prep_ms("izgara", 1))
              < PATIENCE_MIN_MS))
    p.append(("E7 on the proposed profile the daily customer loss is under 10%",
              daily_loss_share(SLOT_SHARE_PROPOSED) < 0.10))
    p.append(("E8 on the content profile the daily loss is over 50% (broken)",
              daily_loss_share(SLOT_SHARE_CONTENT) > 0.50))

    # --- F: peak feasibility
    u = utilisation()
    for k in ("kitchen", "hall", "table"):
        p.append(("F1 {} day average is under 100%".format(k), u[k] < 1.0))
    p.append(("F2 the table turn is in the 120-130 s band (consistent with docs/23 1.3)",
              120000 <= TABLE_TURN_MS <= 130000))
    p.append(("F3 the maximum slot share is around 28%",
              0.27 <= max_slot_share() <= 0.29))
    p.append(("F4 the content profile (37.5%) is NOT feasible",
              max(slot_queue(SLOT_SHARE_CONTENT)[1][k]
                  for k in ("kitchen", "hall", "table")) > 1.20))
    p.append(("F5 the proposed profile (30%) is under 110% in every pool",
              max(slot_queue(SLOT_SHARE_PROPOSED)[1][k]
                  for k in ("kitchen", "hall", "table")) < 1.10))
    p.append(("F6 the Turkish profile (60.6%) is not feasible with any crew",
              0.606 / 0.25 * u["hall"] > 2.0))
    wd = slot_queue(SLOT_SHARE_PROPOSED, customers=77)
    p.append(("F7 on a weekday (77 customers) there is no queue in any slot",
              all(r["wait_max"] == 0 for r in wd)))
    # --- G. DOES THIS TOOL STILL DESCRIBE THE SHIPPED GAME? ------------
    #
    # docs/27 section 9 tells the reader these constants must be
    # identical to model.py's. Nothing ever ran that instruction, and by
    # the time anyone looked they had drifted: 28/25/46/66 here against
    # 30/26/48/70 there, OWNER_WORK 1.4 against 1.3, so HALL_LOAD is
    # 0.0769 here and 0.0736 in the economy the game actually ships.
    #
    # Every timing conclusion below is therefore computed on capacities
    # the content does not use. The tool's OWN arithmetic is consistent -
    # its millisecond budgets were derived from these numbers - which is
    # exactly why the fix is not a one-line import: it is a balance
    # decision about which set is right.
    #
    # So the disagreement is measured and reported rather than hidden.
    # A red line here means "re-derive the budgets or move the economy",
    # not "the tool is broken".
    for name, mine, theirs in (
            ("CAP_COOK", CAP_COOK, _model.CAP_COOK),
            ("CAP_WAITER", CAP_WAITER, _model.CAP_WAITER),
            ("CAP_DISHWASHER", CAP_DISHWASHER, _model.CAP_DISHWASHER),
            ("CAP_CASHIER", CAP_CASHIER, _model.CAP_CASHIER),
            ("OWNER_WORK", OWNER_WORK, _model.OWNER_WORK)):
        p.append(("G1 %s agrees with model.py (%s vs %s)"
                  % (name, mine, theirs), mine == theirs))

    p.append(("F8 the loss is weekend-only; the effect on weekly revenue is under 4%",
              daily_loss_share(SLOT_SHARE_PROPOSED) * 2 * 97 / (5 * 77 + 2 * 97)
              < 0.04))
    return p


def report():
    D = SERVICE_DAY_MS
    u = utilisation()
    print("=" * 74)
    print("LOKANTA TIME MODEL - derived constants")
    print("=" * 74)
    print("Service day      : {} ticks = {} ms = {:.1f} min".format(
        SERVICE_TICKS, D, D / 60000.0))
    print("Day total        : {} ticks = {:.1f} min (service + morning/evening)".format(
        DAY_TICKS, DAY_TICKS * TICK_MS / 60000.0))
    print("Campaign         : {} days = {:.2f} hours (1x), {:.2f} hours (2x)".format(
        CAMPAIGN_DAYS, campaign_hours(1), campaign_hours(2)))
    print("Touch interval   : {:.0f} ms (60 touches / day)".format(D / TOUCH_BUDGET))
    print()
    print("-- Role times (per customer) -----------------------------------------")
    for nm, cap, ms in (("cook", CAP_COOK, KITCHEN_MS), ("waiter", CAP_WAITER, WAITER_MS),
                        ("dishwasher", CAP_DISHWASHER, DISHWASHER_MS),
                        ("cashier", CAP_CASHIER, CASHIER_MS)):
        print("  {:<10} capacity {:>3}  ->  {:>6} ms   ({} x {} = {}, day {})".format(
            nm, cap, ms, ms, cap, ms * cap, D))
    print("  {:<10} {:>13}  ->  {:>6} ms".format("HALL", "total", HALL_MS))
    print("  hall load bp     : {:.6f} work-days/customer".format(HALL_LOAD))
    print()
    print("-- Task durations (ms) -----------------------------------------------")
    print("  seating {:>6} | order   {:>6} | service {:>6}  = waiter {}".format(
        T_SEAT, T_ORDER, T_SERVE, WAITER_MS))
    print("  clear   {:>6} | washing {:>6}                  = dishwasher {}".format(
        T_BUS, T_WASH, DISHWASHER_MS))
    print("  payment {:>6}                                  = cashier {}".format(
        T_PAY, CASHIER_MS))
    print("  eating  {:>6} (per table) | cooking wait {:>6} (per table)".format(
        T_EAT_PARTY, COOK_WAIT_REF_MS))
    print("  table turn: {:.4f} x {} + {} + {} = {} ms".format(
        GROUP_SIZE_SEATED, TABLE_STAFF_MS, COOK_WAIT_REF_MS, T_EAT_PARTY,
        TABLE_TURN_MS))
    print()
    print("-- prepMs (wall clock, ms) -------------------------------------------")
    print("  UNIT_BUSY = {} ms x complexity = the cook's busy time".format(
        UNIT_BUSY_MS))
    print("  {:<8} {:>8} {:>8} {:>8} {:>8}".format("station", "attendBp", "k=1", "k=2", "k=3"))
    for s in STATION_ORDER:
        print("  {:<8} {:>8} {:>8} {:>8} {:>8}".format(
            s, ATTEND_BP[s], prep_ms(s, 1), prep_ms(s, 2), prep_ms(s, 3)))
    print("  derived cook time/customer: {:.0f} ms, target {} ms, deviation {:.2f}%".format(
        kitchen_ms_achieved(), KITCHEN_MS,
        100 * abs(kitchen_ms_achieved() - KITCHEN_MS) / KITCHEN_MS))
    print()
    print("-- The record of the contradiction (morning of 10 September 2026) -----")
    print("  that day's content average prepMs              : {} ms".format(
        CONFLICT_AVG_PREP_MS))
    print("  the derived average over 32 dishes             : {:.0f} ms".format(
        menu_avg_prep_ms()))
    print("  ratio                                          : {:.2f} times".format(
        CONFLICT_AVG_PREP_MS / menu_avg_prep_ms()))
    print("  hamburger: {} ms -> derived {} ms ({:.1f} times)".format(
        CONFLICT_BURGER_PREP_MS, prep_ms("izgara", 1),
        CONFLICT_BURGER_PREP_MS / float(prep_ms("izgara", 1))))
    print("  if that value counted as cook time: a cook makes {:.1f} hamburgers a day"
          .format(D / float(CONFLICT_BURGER_PREP_MS)))
    print("  THE INTERIM STATE (stationless, complexity-based content):")
    print("    cook time per customer {} ms, target {} ms, deviation {:.1f}%".format(
        INTERIM_KITCHEN_MS, KITCHEN_MS,
        100.0 * (INTERIM_KITCHEN_MS - KITCHEN_MS) / KITCHEN_MS))
    print("    the station dimension has been dropped; regeneration is required for Decision D")
    print()
    print("-- Station slots needed at the peak -----------------------------------")
    for s in STATION_ORDER:
        v = station_slots().get(s, 0.0)
        print("  {:<8} {:.2f} simultaneous plates -> {} slots".format(
            s, v, max(1, int(v) + (1 if v > int(v) else 0))))
    print("  total simultaneous plates: {:.2f} (with {} cooks)".format(
        sum(station_slots().values()), PEAK_COOKS))
    print()
    print("-- Peak day utilisation (97 customers, 4 cooks, 7 hall, 14 tables) ----")
    print("  kitchen : {:.1f}%   ({:.0f} / {:.0f} ms)".format(
        100 * u["kitchen"], u["kitchen_need"], u["kitchen_have"]))
    print("  hall    : {:.1f}%   ({:.0f} / {:.0f} ms)".format(
        100 * u["hall"], u["hall_need"], u["hall_have"]))
    print("  tables  : {:.1f}%   ({:.0f} / {:.0f} ms, {:.1f} turns)".format(
        100 * u["table"], u["table_need"], u["table_have"], u["parties"]))
    print("  maximum slot share: {:.1f}%".format(100 * max_slot_share()))
    print()
    print("-- Slot queues -------------------------------------------------------")
    for label, shares in (("content ", SLOT_SHARE_CONTENT),
                          ("proposed", SLOT_SHARE_PROPOSED),
                          ("turkish ", SLOT_SHARE_TURK)):
        r = slot_queue(shares)[1]
        print("  {} slot2 share {:.1f}% -> kitchen {:.0f}% hall {:.0f}% tables {:.0f}%"
              "  idle wait avg {:.0f} ms".format(
                  label, 100 * r["share"], 100 * r["kitchen"], 100 * r["hall"],
                  100 * r["table"], r["wait_avg"]))
    print()
    print("-- Patience -----------------------------------------------------------")
    print("  zero load, seated customer, grill k=1 wall clock : {} ms".format(
        zero_load_wall_clock()))
    print("  zero load, courier (no table) wall clock         : {} ms".format(
        zero_load_takeaway()))
    print("  patience spent at zero load (idle=0)             : {:.0f} ms".format(
        patience_spent(0, prep_ms("izgara", 1))))
    pk = slot_queue(SLOT_SHARE_PROPOSED)[1]
    print("  peak average patience spent (proposed profile)   : {:.0f} ms".format(
        patience_spent(pk["wait_avg"], COOK_WAIT_REF_MS)))
    print("  peak maximum patience spent (proposed profile)   : {:.0f} ms".format(
        patience_spent(pk["wait_max"], COOK_WAIT_REF_MS)))
    print("  the least patient archetype (the courier)        : {} ms".format(
        PATIENCE_MIN_MS))
    print("  peak, courier idle wait (hall queue only)        : {:.0f} ms".format(
        pk["wait_takeaway"]))
    print("  peak, courier patience spent                     : {:.0f} ms".format(
        patience_spent(pk["wait_takeaway"], prep_ms("izgara", 1))))
    print("  share of heads lost in the peak slot (proposed)  : {:.1f}%".format(
        100 * lost_head_share(pk["wait_avg"], pk["wait_takeaway"])))
    print("  DAILY loss, proposed profile                     : {:.1f}%".format(
        100 * daily_loss_share(SLOT_SHARE_PROPOSED)))
    print("  DAILY loss, content profile                      : {:.1f}%".format(
        100 * daily_loss_share(SLOT_SHARE_CONTENT)))



# ============================================================================
# 9. THE PEAK DECISION - docs/28-peak-decision.md
# ============================================================================
# This section is an ADDITION. None of the constants or decisions above changed.
#
# It brings two new things:
#   1. Slot durations do not have to be equal (DECISION G). A slot takes up
#      the share d_i of the day; sum(d_i) = 1. The capacity is distributed in
#      the same proportion. What determines feasibility is not the slot SHARE
#      but the DENSITY: w_i / d_i.
#   2. The queue is a CLOSED LOOP. The slot_queue of section 5 is an open
#      loop: it assumes nobody leaves, so the backlog grows without bound. In
#      reality a customer whose patience runs out leaves and SHORTENS the wait
#      of the person behind them. The fluid model below carries that feedback.

# The patience distribution of the Turkish archetype pool
# (content/archetypes/shared+turk). For the fast food pool, see PATIENCE_HEADS
# above.
PATIENCE_HEADS_TURK = [
    (8000, 550.0), (11000, 450.0), (12000, 2500.0), (13000, 800.0),
    (15000, 390.0), (16000, 1220.0), (19000, 440.0), (20000, 4450.0),
    (22000, 1800.0), (23000, 760.0), (26000, 165.0), (29000, 1800.0),
    (30000, 2530.0), (31000, 180.0), (34000, 1120.0), (36000, 1530.0),
    (40000, 600.0),
]
PATIENCE_HEAD_TOTAL_TURK = sum(h for _, h in PATIENCE_HEADS_TURK)   # 21285
GROUP_SIZE_SEATED_TURK = 2.1942   # content/archetypes measurement, courier excluded

# The measured arrival shares (the same numbers as docs/27 5.2, at full precision)
SLOT_SHARE_CONTENT_FF = [0.1105, 0.3749, 0.1620, 0.3526]
SLOT_SHARE_CONTENT_TR = [0.0937, 0.6063, 0.1753, 0.1247]

# DECISION G: slot durations, cuisine-specific. They sum to 1.0 and divide
# exactly into ticks.
SLOT_DUR_EQUAL = [0.25, 0.25, 0.25, 0.25]
SLOT_DUR_FF = [0.20, 0.30, 0.20, 0.30]     # 960/1440/960/1440 ticks
SLOT_DUR_TR = [0.12, 0.48, 0.25, 0.15]     # 576/2304/1200/720 ticks


def slot_ticks(durs):
    """The slot durations in ticks. Raises an error if any is not a whole number."""
    out = []
    for d in durs:
        t = d * SERVICE_TICKS
        if abs(t - round(t)) > 1e-9:
            raise ValueError("slot duration does not divide into ticks: %r" % (d,))
        out.append(int(round(t)))
    return out


def pool_needs(customers, cooks, hall, tables, group_seated=GROUP_SIZE_SEATED,
               table_turn=None):
    """The ms the three pools need across the day, and the rate at which each
    can consume it."""
    tt = TABLE_TURN_MS if table_turn is None else table_turn
    parties = customers * (1.0 - TAKEAWAY_HEAD_SHARE) / group_seated
    return [
        dict(name="kitchen", need=customers * KITCHEN_MS, rate=float(cooks),
             takeaway=False),
        dict(name="hall", need=customers * HALL_MS, rate=hall + OWNER_WORK,
             takeaway=True),
        dict(name="table", need=parties * tt, rate=float(tables),
             takeaway=False),
    ]


def retain_share(idle_ms, idle_takeaway_ms, heads=None, total=None,
                 cook_ms=COOK_WAIT_REF_MS):
    """The share of heads who STAY at the given idle wait. The inverse of
    lost_head_share, except that it can take the archetype pool (fast food /
    Turkish) from outside."""
    if heads is None:
        heads, total = PATIENCE_HEADS, PATIENCE_HEAD_TOTAL
    spent = patience_spent(idle_ms, cook_ms)
    spent_ta = patience_spent(idle_takeaway_ms, prep_ms("izgara", 1))
    keep = 0.0
    for pat, h in heads:
        if pat == PATIENCE_MIN_MS:
            if pat > spent_ta:
                keep += h
        elif pat > spent:
            keep += h
    return keep / total


def flow_day(shares, durs, customers=PEAK_CUSTOMERS, cooks=PEAK_COOKS,
             hall=PEAK_HALL, tables=PEAK_TABLES, heads=None, total=None,
             group_seated=GROUP_SIZE_SEATED, table_turn=None,
             cook_ms=COOK_WAIT_REF_MS, steps=4000):
    """
    A fluid queue, including balking and including spillover between slots.

    For each pool p there is a backlog B_p (ms of work). A new arrival's idle
    wait is W = sum(B_p / rate_p). Those whose patience cannot bear W DO NOT
    JOIN THE QUEUE:

        dB_p/dt = remaining(W) x arrival_rate_p - rate_p      (B_p >= 0)

    Equilibrium settles where the arrival rate falls to the capacity; the
    backlog saturates at the marginal archetype's patience. Because
    slot_queue (section 5) does not carry that term, it grows the backlog
    without bound on the same profile.
    """
    if heads is None:
        heads, total = PATIENCE_HEADS, PATIENCE_HEAD_TOTAL
    if abs(sum(durs) - 1.0) > 1e-9:
        raise ValueError("the slot durations do not sum to 1.0")
    P = pool_needs(customers, cooks, hall, tables, group_seated, table_turn)
    B = [0.0] * len(P)
    rows = []
    arrived = lost = 0.0
    for i, (w, d) in enumerate(zip(shares, durs)):
        L = d * SERVICE_DAY_MS
        dt = L / float(steps)
        lam = [w * p["need"] / L for p in P]
        a_i = l_i = wsum = wmax = 0.0
        for _ in range(steps):
            W = sum(B[k] / P[k]["rate"] for k in range(len(P)))
            Wta = sum(B[k] / P[k]["rate"] for k in range(len(P))
                      if P[k]["takeaway"])
            r = retain_share(W, Wta, heads, total, cook_ms)
            for k in range(len(P)):
                B[k] = max(0.0, B[k] + (r * lam[k] - P[k]["rate"]) * dt)
            n = w * customers * dt / L
            a_i += n
            l_i += n * (1.0 - r)
            wsum += W * dt
            if W > wmax:
                wmax = W
        arrived += a_i
        lost += l_i
        rows.append(dict(
            slot=i + 1, share=w, dur=d, density=w / d,
            arrivals=a_i, lost=l_i, loss=(l_i / a_i if a_i else 0.0),
            idle_avg=wsum / L, idle_max=wmax,
            util=[w * p["need"] / (p["rate"] * L) for p in P],
        ))
    return dict(rows=rows, arrivals=arrived, lost=lost,
                daily_loss=(lost / arrived if arrived else 0.0),
                residual_idle=sum(B[k] / P[k]["rate"] for k in range(len(P))))


def max_density():
    """The maximum density (share / duration) for a zero queue. The
    generalisation of section 5.3: with equal slots the maximum share is
    0.25 x max_density()."""
    u = utilisation()
    return 1.0 / max(u["kitchen"], u["hall"], u["table"])


def peak_report():
    """Every number inside docs/28-peak-decision.md."""
    print("DENSITY CEILING")
    u = utilisation()
    print("  peak day utilisation kitchen/hall/tables    : {:.1f}% / {:.1f}% / {:.1f}%".format(
        100 * u["kitchen"], 100 * u["hall"], 100 * u["table"]))
    print("  maximum density for a zero queue (share/dur) : {:.4f}".format(
        max_density()))
    print("  its equivalent with equal slots              : {:.2f}%".format(
        25 * max_density()))
    print()
    cases = [
        ("FF content   , equal slots", SLOT_SHARE_CONTENT_FF, SLOT_DUR_EQUAL,
         PATIENCE_HEADS, PATIENCE_HEAD_TOTAL, GROUP_SIZE_SEATED),
        ("TR content   , equal slots", SLOT_SHARE_CONTENT_TR, SLOT_DUR_EQUAL,
         PATIENCE_HEADS_TURK, PATIENCE_HEAD_TOTAL_TURK, GROUP_SIZE_SEATED_TURK),
        ("FF Decision F, equal slots", SLOT_SHARE_PROPOSED, SLOT_DUR_EQUAL,
         PATIENCE_HEADS, PATIENCE_HEAD_TOTAL, GROUP_SIZE_SEATED),
        ("FF DECISION G, 20/30/20/30", SLOT_SHARE_CONTENT_FF, SLOT_DUR_FF,
         PATIENCE_HEADS, PATIENCE_HEAD_TOTAL, GROUP_SIZE_SEATED),
        ("TR DECISION G, 12/48/25/15", SLOT_SHARE_CONTENT_TR, SLOT_DUR_TR,
         PATIENCE_HEADS_TURK, PATIENCE_HEAD_TOTAL_TURK, GROUP_SIZE_SEATED_TURK),
    ]
    for tag, w, d, hh, tt, gs in cases:
        r = flow_day(w, d, heads=hh, total=tt, group_seated=gs)
        print(tag + "   ticks " + "/".join(str(x) for x in slot_ticks(d)))
        print("    slot  share    dur  densty kitchen   hall  table  "
              "avg wait     max    loss")
        for x in r["rows"]:
            print("   {:5d} {:5.1f}% {:5.1f}% {:6.3f} {:6.1f}% {:6.1f}% "
                  "{:6.1f}% {:9.0f} {:7.0f} {:6.1f}%".format(
                      x["slot"], 100 * x["share"], 100 * x["dur"], x["density"],
                      100 * x["util"][0], 100 * x["util"][1], 100 * x["util"][2],
                      x["idle_avg"], x["idle_max"], 100 * x["loss"]))
        print("   DAILY LOSS {:.2f}%   queue left at the end of the day {:.0f} ms".format(
            100 * r["daily_loss"], r["residual_idle"]))
        print()

if __name__ == "__main__":
    if "--check" in sys.argv:
        cs = checks()
        bad = 0
        for name, ok in cs:
            print(("PASS " if ok else "FAIL ") + name)
            bad += 0 if ok else 1
        print("---")
        print("{} checks, {} failed".format(len(cs), bad))
        sys.exit(1 if bad else 0)

    if "--peak" in sys.argv:
        peak_report()
        sys.exit(0)

    if "--md" in sys.argv:
        print("### Tasks\n")
        print(table_tasks())
        print("\n### prepMs\n")
        print(table_prep())
        print("\n### Peak utilisation\n")
        print(table_util())
        print()
        print(table_slots(SLOT_SHARE_CONTENT, "### Content profile"))
        print()
        print(table_slots(SLOT_SHARE_PROPOSED, "### Proposed profile"))
        sys.exit(0)

    report()
