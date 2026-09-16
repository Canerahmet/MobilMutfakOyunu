# -*- coding: utf-8 -*-
"""
Content and golden-data writer - Phase 0
============================================================================
Writes the parameters from model.py into two places:

  content/economy.json        the constants the game reads, in INTEGER units
  content/staff-roles.json    role capacities and wages
  tests/golden/weekly.json    the eight-week table, at FULL precision

The third is the golden data the C# test compares against. That way the
Python model and the C# core share one source, and if they diverge the test
breaks.

Units per docs/23-core-contract.md 2.2:
  money       centi-coins   (1 coin = 100)
  ratio       basis points  (10000 = 1.0)
  reputation  centi-points  (0..10000)
  duration    milliseconds
  labour      micro-work-days (1e-6)

Running it:  python export.py
"""
import io
import json
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import model  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
CONTENT = os.path.join(ROOT, "content")
GOLDEN = os.path.join(ROOT, "tests", "golden")

COIN = 100          # 1 coin = 100 centi-coins
BP = 10_000         # 1.0 = 10000 basis points
MICRO = 1_000_000   # 1 work-day = 1,000,000 micro-work-days


def rnd(x):
    """Round halves away from zero. Python's banker's rounding is not used."""
    return int(x + 0.5) if x >= 0 else -int(-x + 0.5)


def ensure(d):
    if not os.path.isdir(d):
        os.makedirs(d)


# ---------------------------------------------------------------------------
# content/staff-roles.json
# ---------------------------------------------------------------------------
def staff_roles():
    rows = [
        ("asci",      "kitchen", model.CAP_COOK,       model.WAGE["cook"],       ["ocak", "izgara", "firin"]),
        ("garson",    "hall",    model.CAP_WAITER,     model.WAGE["waiter"],     ["hall"]),
        ("bulasikci", "hall",    model.CAP_DISHWASHER, model.WAGE["dishwasher"], ["bulasik"]),
        ("kasiyer",   "hall",    model.CAP_CASHIER,    model.WAGE["cashier"],    ["kasa"]),
    ]
    out = []
    for rid, pool, cap, wage, stations in rows:
        out.append({
            "id": rid,
            "nameKey": "role." + rid,
            "pool": pool,
            "capacityPerDay": cap,
            # The work one customer loads onto this role, in micro-work-days.
            # The core could derive this from the capacity; writing it here
            # guarantees that both sides of the test use the same rounding.
            "workPerCustomerMicro": rnd(MICRO / float(cap)),
            "dailyWage": wage * COIN,
            "stations": stations,
            "xpSpeedBp": [BP, 11000, 12000, 13000],
        })
    return out


# ---------------------------------------------------------------------------
# content/economy.json
# ---------------------------------------------------------------------------
def economy():
    # The reputation CEILING depends on the tier.
    #
    # Measured: 85% of 176 runs ended with reputation in the 0-30 or 90-100
    # band; only 15% were in between. The threshold is a number nobody sees
    # (average satisfaction ~62) and a reasonable player crossed it on day
    # 21-25 and spent the remaining 35 days at the ceiling. From that moment
    # on, tea, the owner's attention, quality ingredients, trait selection and
    # both signature mechanics become MATHEMATICALLY invisible: the reward is
    # paid into a saturated axis.
    #
    # Tying the ceiling to the tier solves two problems at once. When
    # reputation saturates, the only way out is to GROW (the plateau is
    # lifted), and below the ceiling the satisfaction work stays measurable.
    # This is docs/08's idea that "expansion is progress itself".
    REP_CAP = [5500, 7500, 9000, 10000]

    tiers = []
    for i, t in enumerate(model.TIERS):
        tiers.append({
            "tables": t["tables"],
            "rent": t["rent"] * COIN,
            "upgrade": t["upgrade"] * COIN,
            "staffCap": t["cap"],
            "reputationCapCenti": REP_CAP[i] if i < len(REP_CAP) else 10000,
            # PLATE COUNT: six per table.
            #
            # Plates are counted and they circulate (clean -> in use -> dirty
            # -> clean). When the clean ones run out the cook cannot plate up
            # what has been cooked and service stops - docs/14's dishwasher
            # bottleneck.
            #
            # FOUR: one tableful. Chosen BY MEASUREMENT, not by guesswork.
            #
            # The first attempt was SIX per table and the bottleneck NEVER
            # bit: over fourteen days, zero ticks waiting for a plate, lowest
            # clean count 28/42. The reason is physical - the number of plates
            # in use at any one time is limited by the TABLE COUNT (at most
            # four people per table), so if plates are above tables x 4 they
            # can never run out by definition.
            #
            # Four did not bite either (again zero ticks): at most four people
            # sit at a table but the AVERAGE party is three, so four times the
            # plates still stays above physics.
            #
            # THREE TIMES PLUS EIGHT. Both parts came from measurement.
            #
            # "Three times" was tried on its own and the AUTOMATED TOUR locked
            # the restaurant up on day one: four tables, twelve plates, two
            # parties of four eat through the stock, a third party waits for a
            # plate, its patience runs out and the day closes with angry
            # customers. The balance tool hid this because its bots grow fast
            # and averages swallow the first tier.
            #
            # The fixed buffer is the WASHING PIPELINE's share and it does not
            # grow with the table count: at any moment some are dirty and some
            # are in the sink. That buffer is a fixed cost; scaling it with
            # tables left the first tier with no buffer at all.
            #
            # SMALL COEFFICIENT, BIG BUFFER (2x + 12), and that is deliberate:
            # the bottleneck should be in the BIG shop, not the SMALL one. A
            # four-table restaurant has no plate crisis; a fourteen-table one
            # does, because plates grow linearly with tables while service
            # speed grows with tables x turnover. Floor 20 (the first tier is
            # comfortable), ceiling 40 (genuinely tight at the fourth tier).
            #
            # The pressure comes from the COUNT, not the DURATION. Increasing
            # the washing time would have counted the dishwasher's share
            # TWICE: ClearMs (clearing the table) already carries that share
            # (6,000 / 19,200 = 31%, and 28% in docs/14).
            "plates": t["tables"] * 2 + 6,
        })

    # The experience rise compounds weekly. The campaign is 60 days = a limit
    # of 9 weeks.
    weeks = 10
    mult = []
    for w in range(weeks):
        mult.append(rnd((1.0 + model.XP_WAGE_GROWTH) ** w * BP))

    return {
        "schemaVersion": 1,
        "_comment": "URETILEN DOSYA. Elle degistirmeyin; tools/balance/export.py calistirin.",

        "startingCash": model.START_CASH * COIN,
        "startingReputationCenti": 3000,
        "campaignDays": 60,
        "seasonDays": 15,
        "rentDayInterval": 7,
        "weekendDaysPerWeek": model.WEEKEND_DAYS,

        "reputationDecayPerDayCenti": 30,
        "satisfactionNeutralCenti": 6000,

        "customerBasePerTable": model.SEATS_TURNOVER,
        "weekdayMultiplierBp": model.WEEKDAY_BP,
        "weekendMultiplierBp": model.WEEKEND_BP,

        "ingredientRateBp": rnd(model.INGREDIENT_RATE * BP),
        # docs/23 1.3 and docs/27: the service day is 4,800 ticks = 480,000 ms.
        # This said 120,000; when docs/27 changed the day length the content
        # was not updated and stayed four times out of step with the code.
        # The code was right, the content was stale. tools/audit_content.py
        # found it.
        "serviceMs": 480_000,
        "interventionsPerDay": 4,

        "loanMultiplierBp": 13500,
        "loanWeeks": 8,
        "loanOptions": [5000 * COIN, 10000 * COIN, 20000 * COIN],

        # The order model. One MAIN dish per head is certain, the side and the
        # drink are probabilistic. Average plates = 1 + side + drink.
        # This is the base state of the docs/07 combo mechanic; the combo
        # raises these ratios.
        "order": {
            "sideChanceBp": model.SIDE_CHANCE_BP,
            "drinkChanceBp": model.DRINK_CHANCE_BP,
            "dessertChanceBp": model.DESSERT_CHANCE_BP,
            "askChanceBp": model.ASK_CHANCE_BP,
            "askMissCenti": model.ASK_MISS_CENTI,
            "kitchenMsPerPerson": 480_000 // 28,
        },

        # The realisation rate measured from the simulation. The same as in
        # model.py.
        # Named regular customers. docs/11: an archetype produces thousands of
        # customers, a regular is a single person. The numbers are here
        # because the mechanic is in code and the numbers are in data
        # (docs/23 8.2).
# Morale. The docs/14 "Morale" table and its thresholds.
"morale": {
    "starting": 70,
    "lowThreshold": 30,
    "quitThreshold": 15,
    "quitChanceBp": 1000,
    "slowPenaltyBp": 2000,
    "paidDelta": 5,
    "lateDelta": -25,
    "busyDelta": -3,
    # The docs/14 morale table is a list of EVENTS, not a drift model.
    # Applying only the events makes the ladder go one way, downwards, and
    # even in a well-run shop the whole crew resigns within a month -
    # measurement caught this. A calm day really does let them recover.
    "recoveryDelta": 2,
},

# The owner's intervention. docs/12 5.4.
#
# All three numbers once sat IN THE CODE - and inside EconomyConfig's
# WithMorale() constructor at that, so even the name was in the wrong place.
# The balance tool can touch content but not code; while they were there
# these three numbers DID NOT EXIST as far as calibrate.py was concerned,
# and they were exactly the ones that needed tuning.
"intervention": {
    # The owner attended personally: satisfaction, in centi.
    #
    # MEASURED. These two numbers (the reward and the patience multiplier)
    # are tuned together because they are tuning a TRADE-OFF. Three
    # variants were run, 16 seeds:
    #
    #   patience x3, reward 1200  ff served -56 reputation -1.6 cash -1,897
    #                             tr served +32 reputation +1.3 cash   -120
    #   patience x1, reward 3000  ff served -33 reputation -0.4 cash   -312
    #                             tr served +102 reputation +5.2 cash  +116
    #   patience x2, reward 2400  ff served -32 reputation -0.4 cash   -392
    #                             tr served +159 reputation +5.5 cash +1,859  <-- chosen
    #
    # With the patience extension at 3x, a player who intervened served
    # FEWER customers than one who never did, because the extension
    # occupies the table: the party you rescue takes the place of somebody
    # else who could have been served. Moving the reward onto satisfaction
    # fixed the trade-off - there is room on the satisfaction axis (a
    # reasonable player's reputation is 75, not the ceiling), and none on
    # the patience axis.
    "attentionSatisfactionCenti": 2400,
    # Tea / a small treat: satisfaction, in centi.
    "treatSatisfactionCenti": 900,
    # The share cut from the remaining duration of a rushed job, in basis points.
    "rushCutBp": 4000,
    # The patience extension, as a multiple of the seat-to-order duration.
    #
    # This number tunes a TRADE-OFF: the extension holds a party that was
    # about to leave, but it also occupies the table for longer. An
    # occupied table means somebody else cannot be served, so extending is
    # NOT FREE.
    "attentionPatienceMult": 2,
    "treatPatienceMult": 1,
},
        "regulars": {
            # The chance of dropping in on a given day once you have met:
            # about 3 days a week.
            "visitChanceBp": 4500,
            # The satisfaction penalty if they cannot find their favourite
            # dish on the menu.
            "missedFavouriteCenti": 900,
            # Below this, if they leave they stay away for a while.
            "upsetCenti": 5000,
            "awayDays": 3,
        },
        "realisationBp": model.REALISATION_BP,

        "priceVolatilityBp": 2500,
        "underpriceFloorBp": 8500,
        # THE DIRECT EFFECT OF PRICE ON DEMAND. 9000 = a 10% rise -> about 9%
        # fewer customers. Before this channel was added, price had NO route
        # to demand at all and a raise was free for a player at the
        # reputation ceiling; it was measured, and a bot that raised prices
        # by 10% beat every strategy.
        "priceElasticityBp": 9000,
        # DAILY DEMAND VOLATILITY. 1000 = -10% to +10%.
        #
        # Demand was entirely deterministic: every Tuesday at the same
        # reputation and table count brought exactly the same customers, so
        # the market's suggestion was ALWAYS exactly right and the morning
        # stock decision was a button, not a judgement. The deviation is ONLY
        # in what is realised; the forecast shows the expectation.
        "demandVarianceBp": 1000,
        # WHEN THE OWNER ATTENDS, THE NEXT PIECE OF HALL WORK IS HALVED.
        #
        # When the intervention was measured it came out NEUTRAL: in a
        # properly staffed restaurant a crisis almost never happens (0.8
        # interventions a day), so the mechanic was a safety net - while the
        # store text sells it as a core mechanic. What was missing was the
        # hall side: attention extended patience and sped up the KITCHEN, but
        # the bottleneck is usually in the hall.
        "attendWorkCutBp": 5000,
        # The price CEILING: the satisfaction penalty saturates at zero and
        # demand never sees the price, so without a ceiling profit is
        # unbounded. 25000 = 2.5 times the market price.
        "overpriceCeilingBp": 25000,

        "staffing": {
            "ownerPool": "hall",
            "ownerWorkMicro": rnd(model.OWNER_WORK * MICRO),
            "weeklyXpWageGrowthBp": rnd(model.XP_WAGE_GROWTH * BP),
            "weeklyWageMultiplierBp": mult,
            "tiers": tiers,
        },
    }


# ---------------------------------------------------------------------------
# content/cuisines/*.json
# ---------------------------------------------------------------------------
# docs/28-peak-decision.md Decision G: it is the slot DURATIONS, not the slot
# SHARES, that vary by cuisine. 60% of a Turkish restaurant's customers arrive
# in the lunch slot and that is an identity pillar; it could not be served in
# an equal slot. Once the lunch slot takes up 48% of the day the same share
# becomes feasible.
#
# Daily loss: fast food 3.15%, Turkish 5.08%. The effect on weekly revenue is
# 1.06% and 1.70%. To verify: python tools/balance/timing.py --peak
#
# MENU ROLES. Dish groups are cuisine-specific (docs/13): main/side in fast
# food, stew/soup/rice/grill/meze in a Turkish restaurant. The core, however,
# builds an order as main + side + drink, so it has to know which group plays
# which role.
#
# Until this mapping was written the simulation hard-coded the fast food
# dictionary and NO customer in the Turkish cuisine could find a main dish:
# all eight strategies went under with zero customers.
# SLOT DURATIONS: THE SHARPNESS OF THE DAY.
#
# These are not shares but DURATIONS (docs/28 Decision G). The arrival weights
# live in the archetypes; a slot's DENSITY = arrival share / duration share.
#
# Measured, and both were nearly FLAT: the busiest moment of the day was only
# 1.24-1.25 times the average. The crew is sized against the TOTAL daily work
# (StaffingModel.Required), so a 1.25x peak was absorbed comfortably - and
# every run of the tour reported "0 angry, 0 critical tables, 0 waiting
# tables". The intervention, the tea, the crisis strip and the signature
# mechanics are all built on top of that pressure; with no pressure they are
# all decoration.
#
# For the Turkish cuisine the content CONTRADICTED THE DESIGN as well: the
# cuisine's own description says "a hard lunch peak", but lunch was the
# LONGEST slot, taking up 48% of the day - so the peak was a flat stretch
# spread over half a day.
#
# The DURATION is what changes, not the WEIGHT: the daily customer total stays
# exactly the same, only the same customers now squeeze into a narrower
# window. This matters - the rent and margin calibration (solve.py) is solved
# over the daily TOTAL, so it stays valid.
#
# The new densities (arrival share / duration share):
#   fastfood  0.52 / 2.09 / 0.46 / 1.50   two peaks: lunch and evening
#   turk      0.88 / 2.12 / 0.40 / 0.82   one hard lunch peak
#
# The two cuisines now differ in RHYTHM as well: fast food fills up twice
# across the day, the Turkish one explodes at lunch and dies in the afternoon.
CUISINES = [
    ("fastfood", [2500, 1800, 3500, 2200], {
        "main":    ["ana"],
        "side":    ["yan"],
        "drink":   ["icecek"],
        "dessert": ["tatli"],
    }),
    ("turk", [1200, 2800, 4500, 1500], {
        "main":    ["sulu", "izgara"],
        "side":    ["corba", "pilav", "meze"],
        "drink":   ["icecek"],
        "dessert": ["tatli"],
    }),
]


def cuisines():
    out = []
    for cid, slots, roles in CUISINES:
        assert len(slots) == 4, cid
        assert sum(slots) == BP, cid + " slot total " + str(sum(slots))
        ticks = 480_000 // 100
        for s in slots:
            assert (ticks * s) % BP == 0, cid + " slot does not divide into whole ticks"

        # The roles must cover EVERY group in the dish file, and no group may
        # fall into two roles at once. Without this check a group quietly
        # becomes impossible to order.
        path = os.path.join(CONTENT, "dishes", cid + ".json")
        if os.path.exists(path):
            dishes = json.load(io.open(path, encoding="utf-8"))
            have = set(d["group"] for d in dishes)
            mapped = []
            for k in ("main", "side", "drink", "dessert"):
                mapped.extend(roles[k])
            assert len(mapped) == len(set(mapped)), cid + " a group falls into two roles"
            missing = have - set(mapped)
            assert not missing, cid + " group with no role: " + ", ".join(sorted(missing))
            extra = set(mapped) - have
            assert not extra, cid + " a role given to a group that does not exist: " + ", ".join(sorted(extra))
            assert any(d["group"] in roles["main"] and d.get("unlockDay", 0) <= 1
                       for d in dishes), cid + " no main dish open on day one"

        out.append((cid, {
            "id": cid,
            "nameKey": "cuisine." + cid,
            "_comment": "URETILEN DOSYA. tools/balance/export.py",
            "slotDurationsBp": slots,
            "eatMs": 38_000,
            # SELF SERVICE: in fast food no waiter comes to the table.
            #
            # The biggest STRUCTURAL difference between the cuisines. It is
            # true in real life too: you order at the counter and pay there,
            # the customer carries the tray and finds their own table. The
            # work left in the hall is counter + clearing + washing up - that
            # is, not a waiter but a CLEANER.
            "selfService": cid == "fastfood",
            # HALL ROLES BY CUISINE.
            #
            # In fast food there is NO waiter - the cashier is at the counter
            # and the cleaner clears the tables. A Turkish restaurant has all
            # three.
            #
            # This is not just appearance but a NUMBER: a waiter means 38,462
            # micro-work per customer, that is more than half of the hall
            # load. Take them off the list and both the crew model and the
            # wage bill fall.
            "hallRoles": (["kasiyer", "bulasikci"] if cid == "fastfood"
                           else ["garson", "bulasikci", "kasiyer"]),
            # VOLUME: fast food brings more people to the same table.
            # 30% was chosen BY MEASUREMENT. 11500 and 12000 were tried too
            # and both made fast food RICHER (23,386 / 24,318 / 22,473): at
            # low volume the shop stays lean, at high volume it expands and
            # pays rent and wages. So volume is NOT a balance lever for total
            # cash - it changes the shape. 13000 gives the most distinct
            # shape: +43% party size, a crew of 5, 16 lost.
            "customerMultiplierBp": 13000 if cid == "fastfood" else 10000,
            # RENT: the price of that volume, and a small increase in
            # difficulty.
            #
            # Realistic: chains sit in expensive, high-traffic places.
            #
            # SWEPT (the reasonable bot, against Turkish 17,351):
            #   no multiplier   22,473   difference +29.5%
            #   x1.15           22,263   difference +28.3%   <- chosen
            #   x1.25           23,493   difference +35.4%
            #
            # So RAISING the rent RAISES the cash: the bot answers the cost by
            # not expanding, and not expanding is more profitable. Final cash
            # is NOT a difficulty measure for this bot; the measure is the
            # DIFFERENCE between the two cuisines, and that is narrowest at
            # 11500.
            #
            # The remaining difference for a good player is already +13% (the
            # planner). The big differences come from TURKISH's weaknesses:
            # waste of 14,121 against 5,487, and the tab loses money
            # (docs/51 §7) - those are separate work and not things to paper
            # over with rent.
            "rentMultiplierBp": 11500 if cid == "fastfood" else 10000,
            "menuRoles": roles,
            "signature": SIGNATURE[cid],
            "scoreAxis": SCORE_AXIS[cid],
        }))
    return out


# The CUISINE-SPECIFIC axis of the year-end evaluation (docs/08).
#
# Why a separate axis per cuisine: six of the seven axes are the same in every
# cuisine. Only this last one rewards the signature mechanic, which means the
# cuisines differ from each other not only in PLAY but in OUTCOME.
#
# target: the value at which the axis awards 100 points.
#
# FAST FOOD: what percentage of main-dish orders turned into a combo, in
# per-mille.
#
# For a while the axis was `peakCovers` (the day's highest cover count) and it
# was MEASURED to follow not the signature but EXPANSION: a bot that opened
# the combo every morning and one that never opened it scored the same (38 /
# 38), and the highest scores belonged to whoever opened the most tables. So
# the axis was a copy of the "Venue" axis, while docs/08 said of it that it
# "rewards the signature mechanic DIRECTLY".
#
# The target comes FROM MEASUREMENT, it was not invented: a bot that opened
# the combo every morning turned **17.4%** of main dishes into combos (12
# seeds, 60 days); everybody who did not open it scored 0%. 15% gives full
# marks, so "open it most days" is enough - the combo also increases the
# kitchen load, so closing it at the peak is a legitimate way to play and the
# axis must not punish it.
#
# TURKISH: the tab collection rate, in basis points; 90% is full marks,
# because 100% can only be reached by never opening a tab at all, and that
# means never using the mechanic.
# TURKISH: what percentage of the tabs opened were collected, in per-mille.
#
# The target of 9000 was MEANINGLESS for a while: the collection chance was a
# flat 8500 and rose to 9500 with the tea bonus, so everybody who used the
# ledger got about 100 and anybody who did not got 0 - the axis was a
# participation badge.
#
# The chance now depends on the customer's visit count (trust) and is capped
# at 9500. Measured: a bot that gives credit to everybody scores 72, so the
# target really is hard and the axis asks "did you use it WELL" rather than
# "did you use it".
#
# On the FAST FOOD side the axis stays FLAT, and that is no longer a
# shortcoming but a MEASURED result (docs/53):
#
#   reasonable (no combo)   22,492 cash | 2617 parties | combo 0.0%  | no plate 846
#   signature (always on)   22,163 cash | 2559 parties | combo 18.1% | no plate 1018
#   closed_at_peak          23,474 cash | 2564 parties | combo 16.8% | no plate 792
#
# After self service emptied the hall the bottleneck moved to the kitchen and
# the combo's kitchen load bites for the FIRST TIME: the combo costs 58
# parties, and closing it at the peak beats keeping it open by +1,311. That
# is, the play the comment above calls "legitimate" is now the BEST play.
#
# And this is exactly why no RATIO-BASED target can fix this axis: THE GOOD
# PLAY HAS THE LOWER SHARE (16.8% < 18.1%). Raising the target would reward
# the worse player. The axis stays a participation badge ON PURPOSE; the skill
# difference is already visible on the net-worth axis (74 against 71).
SCORE_AXIS = {
    "fastfood": {"kind": "comboShare", "nameKey": "score.axis.combo", "target": 1500},
    "turk": {"kind": "creditCollected", "nameKey": "score.axis.credit", "target": 9000},
}


# docs/23 8.2: the mechanic in code, THE NUMBERS IN DATA. If the block is
# missing the cuisine does not load - the signature mechanic is the one thing
# that separates one cuisine from another (docs/07's "most important line"),
# so it has no silent default.
SIGNATURE = {
    # The combo: the right composition raises the average ticket but
    # increases the kitchen load. priceBp is the discount applied to the sum
    # of the three items; kitchenLoadBp is how much longer those three jobs
    # tie the cook up.
    #
    # KITCHEN LOAD 12000 -> 13500. Measured (32 seeds): a signature player was
    # beating a reasonable player by 35%, with a target band of 90%-130%. The
    # band is two-sided and there is a reason for it - the lower bound tests
    # that the mechanic is not a TRAP, the upper bound that it is not
    # COMPULSORY. Sitting 35% above means punishing a player who does not open
    # the combo.
    #
    # What was tuned was not the DISCOUNT but the LOAD: deepening the discount
    # would have weakened the combo but not moved where the trade-off sits.
    # The load makes the combo expensive at exactly the place it makes its
    # promise - at the kitchen's bottleneck.
    "fastfood": {
        "kind": "combo",
        "combo": {
            "items": ["hamburger", "patates_kizartma", "kola"],
            "priceBp": 8750,
            "kitchenLoadBp": 13500,
        },
    },
    # The tab: it disrupts cash flow, raises loyalty and reputation, and who
    # will pay is uncertain. docs/07, Turkish cuisine.
    "turk": {
        "kind": "credit",
        "credit": {
            "maxPerRegular": 300000,
            "dueDays": 7,
            # THE CHANCE NOW DEPENDS ON WHO YOU GIVE IT TO.
            #
            # It was a flat 8500 (9500 with tea) and whoever paid paid 112% of
            # the bill: the expected cash was 0.95 x 1.12 = 1.064 x the bill,
            # so a tab WAS MORE PROFITABLE THAN A CASH SALE. There was never a
            # day to refuse; the mechanic was not a ledger but a free bonus
            # button.
            #
            # The base is now the level of "somebody you know but have only
            # just met".
            "collectChanceBp": 6000,
            "teaCollectBonusBp": 1000,
            "defaultRepPenaltyCenti": 300,
            "loyaltyBonusCenti": 800,
            "teaCostCenti": 200,
            "loyaltyDemandBp": 60,
            "loyaltyCapBp": 1500,
            # The chance of asking for a tab. Before the regulars content
            # arrived this was 12% and the candidate pool was WIDE (every
            # frequently arriving archetype). Now the only candidates are
            # NAMED customers eligible for credit: seven people, each of whom
            # drops in on half the days. At the same rate the mechanic barely
            # fired at all - three accounts in sixty days.
            "askChanceBp": 4000,
            "refusedPenaltyCenti": 1200,
            # 1200 -> 800: it must not be profitable on its own. Its return is
            # not the money it adds but LOYALTY and satisfaction.
            "repayBonusBp": 800,
            # TRUST: every visit by the customer adds 400 bp to the chance, up
            # to 3000. So somebody who has been coming for years reaches 90%,
            # and somebody you have only just met stays at 60% - and the
            # question becomes not "shall I open a tab" but "shall I open one
            # for THIS MAN".
            "trustPerVisitBp": 400,
            "trustCapBp": 3000,
            # NO total certainty: a risk-free ledger produces no decision either.
            "chanceCapBp": 9500,
        },
    },
}


# ---------------------------------------------------------------------------
# content/equipment.json
# ---------------------------------------------------------------------------
def equipment():
    """
    Stations and the equipment ladder. docs/27 Decision D:
      - prepMs is the dish's wall-clock duration; equipment DOES NOT TOUCH it
      - the cook's occupancy = prepMs x attendBp / 10000
      - an upgrade either adds a slot or lowers attendBp

    The prices are derived from the rent inside model.equipment(); they are
    not entered by hand.
    """
    stations = []
    for st in model.equipment():
        tiers = []
        for t in st["tiers"]:
            tiers.append({
                "tier": t["tier"],
                "slots": t["slots"],
                "attendBp": t["attend"],
                "price": t["price"] * COIN,      # centi-coins
                "neededAtTables": t["needAt"],   # 0 = not compulsory
            })
        stations.append({
            "id": st["id"],
            "nameKey": "station." + st["id"],
            "tiers": tiers,
        })

    # At the tier 4 peak every station's slot count has to match docs/27 3.3
    # exactly. Without this check the conc values could quietly drift.
    want = {"ocak": 4, "izgara": 4, "firin": 2, "soguk": 1, "icecek": 1, "tatli": 1}
    for st in stations:
        top = max(t["slots"] for t in st["tiers"])
        assert top == want[st["id"]], (
            "{}: top slot count is {} but docs/27 says {}".format(st["id"], top, want[st["id"]]))
        # The price ladder must increase
        prices = [t["price"] for t in st["tiers"]]
        assert prices == sorted(prices), st["id"] + " prices do not increase"
        # attendBp must not increase at any step
        att = [t["attendBp"] for t in st["tiers"]]
        assert att == sorted(att, reverse=True), st["id"] + " attendBp increases"

    # The cold-storage ladder. keepBp: what percentage of an ingredient's OWN
    # shelf life still applies. t0 is zero, i.e. the designed baseline of
    # docs/12 3.
    storage_tiers = []
    for t in model.storage():
        storage_tiers.append({
            "tier": t["tier"],
            "keepBp": t["keep"],
            "price": t["price"] * COIN,
        })
    assert storage_tiers[0]["keepBp"] == 0, "storage t0 keepBp must be zero"
    assert storage_tiers[0]["price"] == 0, "storage t0 must be free"
    for i in range(1, len(storage_tiers)):
        assert storage_tiers[i]["keepBp"] > storage_tiers[i - 1]["keepBp"]
        assert storage_tiers[i]["price"] > storage_tiers[i - 1]["price"]

    # Cuisine-SPECIFIC named equipment. What sets it apart from the six shared
    # stations: it is NOT THERE at the start, and until it is bought the
    # dishes attached to it are locked.
    cuisine_stations = {}
    for cid, _, _ in CUISINES:
        # The "opens" list is DERIVED: a dish's own station field already says
        # which piece of equipment it wants. Keeping a second list by hand
        # quietly diverged once already in this project.
        opens = {}
        dish_path = os.path.join(CONTENT, "dishes", cid + ".json")
        if os.path.exists(dish_path):
            for d in json.load(io.open(dish_path, encoding="utf-8")):
                opens.setdefault(d["station"], []).append(d["id"])

        rows = []
        for st in model.cuisine_stations(cid):
            if not opens.get(st["id"]):
                raise AssertionError(
                    cid + ": " + st["id"] + " opens no dish at all; "
                    "either a dish must be written or the station deleted")
            rows.append({
                "id": st["id"],
                "nameKey": "station." + st["id"],
                "tiers": [
                    {"tier": 0, "slots": 1, "attendBp": st["attend"],
                     "price": 0, "neededAtTables": 0},
                    {"tier": 1, "slots": 2, "attendBp": st["attend"],
                     "price": st["price"] * COIN, "neededAtTables": 0},
                ],
                "opens": opens[st["id"]],
            })
        if rows:
            cuisine_stations[cid] = rows

    return {
        "schemaVersion": 1,
        "_comment": "URETILEN DOSYA. Elle degistirmeyin; tools/balance/export.py calistirin.",
        "stations": stations,
        "cuisineStations": cuisine_stations,
        "storage": {
            "nameKey": "storage.soguk_hava",
            "tiers": storage_tiers,
        },
    }


# ---------------------------------------------------------------------------
# tests/golden/weekly.json
# ---------------------------------------------------------------------------
def golden():
    rows = model.run()
    out = []
    for r in rows:
        c = r["crew"]
        out.append({
            "week": r["week"],
            "tables": r["tables"],
            "reputationCenti": r["rep"] * 100,
            "ticket": r["ticket"] * COIN,
            "weekdayCustomers": r["weekday"],
            "weekendCustomers": r["weekend"],
            "weekCustomers": r["week_customers"],
            "cooks": c["cook"],
            "hall": c["hall"],
            "crewTotal": c["total"],
            "staffCap": r["cap"],
            # Money fields are centi-coins, rounded from full precision
            "revenue": rnd(r["revenue"] * COIN),
            "ingredients": rnd(r["ingredients"] * COIN),
            "wages": rnd(r["wages"] * COIN),
            "rent": rnd(r["rent"] * COIN),
            "expansion": rnd(r["expansion"] * COIN),
            "net": rnd(r["net"] * COIN),
            "cash": rnd(r["cash"] * COIN),
        })
    return {
        "_comment": "URETILEN DOSYA. tools/balance/export.py. C# cekirdegi bunu tutturmak zorunda.",
        "source": "tools/balance/model.py",
        "toleranceCenti": 100,
        "weeks": out,
    }


def write(path, obj):
    ensure(os.path.dirname(path))
    with io.open(path, "w", encoding="utf-8", newline="\n") as f:
        json.dump(obj, f, ensure_ascii=False, indent=2)
        f.write("\n")
    print("written: " + os.path.relpath(path, ROOT))


def main():
    for cid, obj in cuisines():
        write(os.path.join(CONTENT, "cuisines", cid + ".json"), obj)
    write(os.path.join(CONTENT, "economy.json"), economy())
    write(os.path.join(CONTENT, "staff-roles.json"), staff_roles())
    write(os.path.join(CONTENT, "equipment.json"), equipment())
    write(os.path.join(GOLDEN, "weekly.json"), golden())
    print("---")
    print("hall workload / customer : {:.6f} work-days".format(model.HALL_LOAD))
    print("micro total              : {}".format(
        sum(rnd(MICRO / float(c)) for c in
            (model.CAP_WAITER, model.CAP_DISHWASHER, model.CAP_CASHIER))))
    print("hall daily wage          : {:.4f} coins".format(model.WAGE_HALL))
    for cid, slots, _roles in CUISINES:
        ticks = [480_000 // 100 * s // BP for s in slots]
        print("slot ticks {:<9}: {}".format(cid, ticks))


def render_docs():
    """
    Also refreshes the generated tables inside the documents.

    CHAINED to export.py because when the two are run separately they
    diverge, and the divergence is silent: an audit found docs/12 and
    content/economy.json a whole calibration generation apart - the document
    described rents of 850/1,950/2,900/5,000 and a realisation of 7000, while
    the content held 650/1,550/2,250/4,000 and 6500. The design's reference
    document was describing an economy that did not exist.
    """
    import subprocess
    import sys as _sys
    here = os.path.dirname(os.path.abspath(__file__))
    subprocess.check_call([_sys.executable, os.path.join(here, "render.py")])


if __name__ == "__main__":
    main()
    render_docs()
