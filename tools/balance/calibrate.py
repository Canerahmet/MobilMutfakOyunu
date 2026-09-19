# -*- coding: utf-8 -*-
"""
Realisation rate calibration - Phase 0
============================================================================
Two models depend on each other and will not agree in a single shot:

  the closed-form model  ->  solves rent, capacity and expansion from the
                             realisation rate
  the simulation         ->  measures how many customers are actually served
                             with those parameters, and RE-DETERMINES the
                             realisation rate

So the rate changes the parameters and the parameters change the rate. A
one-off measurement is misleading: on 10 September 2026 the simulation
measured 93.35%, but that measurement was taken with the OLD cheap rents. Once
the new rents were applied, the same strategy could not expand and the rate
collapsed.

This script SEARCHES for the fixed point: for each candidate rate it runs the
whole chain (solve -> model -> export -> simulation) and scores it against the
design targets.

Running it:  python calibrate.py            tries every candidate
             python calibrate.py 8000        tries a single candidate and applies it
"""
from __future__ import print_function

import io
import os
import re
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(HERE))
MODEL = os.path.join(HERE, "model.py")
SOLVE = os.path.join(HERE, "solve.py")

sys.path.insert(0, HERE)

# The realisation rates to try. The lower bound is the old measurement, the
# upper bound the new one.
# EXTENDED DOWNWARDS ON 18 SEPTEMBER, BECAUSE THE WINNER SAT ON THE EDGE.
#
# The sweep of that morning picked 6500 - the lowest value in the list. An
# optimum at the boundary of the search is not an optimum, it is the search
# telling you the bracket is wrong: nothing below was ever tried, so nothing
# below was ever ruled out. 5500 and 6000 are here to find out whether the
# penalty turns round or keeps falling.
CANDIDATES = [5500, 6000, 6500, 7000, 7500, 8000, 8500, 9000, 9335]


# ---------------------------------------------------------------------------
def patch(path, pairs):
    s = io.open(path, encoding="utf-8").read()
    for pat, sub in pairs:
        s2, n = re.subn(pat, sub, s, count=1, flags=re.M)
        if n != 1:
            raise AssertionError("pattern not found: " + pat)
        s = s2
    io.open(path, "w", encoding="utf-8", newline="\n").write(s)


def solve_for(bp):
    """Runs solve.py at the given rate and returns the best parameters."""
    patch(SOLVE, [(r"^REALISATION_BP = \d+$", "REALISATION_BP = %d" % bp)])

    import importlib
    import solve
    importlib.reload(solve)

    best = None
    import itertools
    grid_s = [round(x * 0.05, 2) for x in range(14, 31)]
    grid_o = [round(x * 0.1, 1) for x in range(8, 25)]
    grid_e = [round(x * 0.1, 1) for x in range(10, 31)]
    for s, o, e in itertools.product(grid_s, grid_o, grid_e):
        rents = solve.solve_rents(s, o, e)
        if any(v <= 0 for v in rents.values()):
            continue
        rows, _, _ = solve.simulate(s, o, e, rents)
        pen, fails = solve.score(rows)
        if best is None or pen < best[0]:
            best = (pen, s, o, e, rents, fails)
            if pen == 0:
                break
    return best


# The starting cash IS NO LONGER A FREE PARAMETER, it is derived from the rent.
#
# The user's rule: a player who does nothing must not get through the first
# week. The first rent and wages are paid in one go on day 7, so the rule is:
#
#     starting cash  <  the first week's fixed cost
#
# A player who does nothing has zero income and therefore cannot cover that
# payment, and falls into debt on day 7. A player who works, on the other
# hand, produces revenue during the day and can carry the same payment.
START_CASH_BP = 8_500      # what percentage of the first week's cost


def start_cash_for(rent4):
    """The cash derived from the four-table rent and one cook's weekly wage."""
    week1 = rent4 + 7 * 140          # rent + the cook's daily wage
    return int(week1 * START_CASH_BP / 10_000)


def apply_to_model(bp, s, o, e, rents):
    import solve
    caps = dict((k, int(round(v * s))) for k, v in solve.BASE_CAP.items())
    ups = dict((t, int(solve.BASE_UPGRADE[t] * e)) for t in (4, 7, 10, 14))

    tiers = "TIERS = [\n"
    for t, cap in ((4, 3), (7, 5), (10, 8), (14, 12)):
        tiers += "    dict(tables=%-2d rent=%-5d upgrade=%-5d cap=%d),\n" % (
            t, rents[t], ups[t], cap)
    tiers = tiers.replace("tables=4 ", "tables=4,  ").replace("tables=7 ", "tables=7,  ")
    tiers = tiers.replace("tables=10 ", "tables=10, ").replace("tables=14 ", "tables=14, ")
    tiers = re.sub(r"rent=(\d+) ", lambda m: "rent=%s, " % m.group(1), tiers)
    tiers = re.sub(r"upgrade=(\d+) ", lambda m: "upgrade=%s, " % m.group(1), tiers)
    tiers += "]"

    # THE CHOSEN RATE IS WRITTEN BACK INTO SOLVE AS WELL.
    #
    # solve_for() patches SOLVE for every candidate during the sweep, so when
    # the sweep finishes the LAST CANDIDATE TRIED is left there - not the
    # chosen one. Two files carried the same constant with different values
    # (9335 / 7000) and, in the MANUAL flow docs/12 describes (python
    # solve.py), that would have produced the wrong rent. The calibration was
    # not broken; it had started to mislead.
    patch(SOLVE, [(r"^REALISATION_BP = \d+$", "REALISATION_BP = %d" % bp)])

    patch(MODEL, [
        (r"^REALISATION_BP = \d+$", "REALISATION_BP = %d" % bp),
        # CAP_COOK IS NOT WRITTEN HERE, AND THAT IS THE POINT.
        #
        # The other three capacities are balance knobs: how much of a hall
        # role's day one guest costs is a design choice. The cook's is not.
        # It falls out of the menu - the dish mix, the attend fractions and
        # the busy time give 17,127 ms a guest, which is 28 - so scaling it
        # by the staffing factor does not tune the game, it asserts something
        # about the dishes that the dishes do not say. timing.py's C2 fails
        # when it is scaled, and since 18 September C2 actually runs.
        #
        # This line used to write it anyway. Left in, every sweep ended with
        # a red check suite or, worse, the value it happened to land on.
        (r"^CAP_WAITER = \d+$", "CAP_WAITER = %d" % caps["waiter"]),
        (r"^CAP_DISHWASHER = \d+$", "CAP_DISHWASHER = %d" % caps["dishwasher"]),
        (r"^CAP_CASHIER = \d+$", "CAP_CASHIER = %d" % caps["cashier"]),
        (r"^OWNER_WORK = [\d.]+", "OWNER_WORK = %s" % o),
        (r"^TIERS = \[\n(?:.*\n)*?\]$", tiers),
    ])


def run(cmd, cwd=ROOT, check=True):
    """
    The exit code MATTERS. It used to be swallowed, and that hid two separate
    faults in a single day: the harness never running at all, and export.py
    crashing AFTER it had written the files. In both cases the calibration
    calmly wrote down a wrong answer.
    """
    p = subprocess.Popen(cmd, cwd=cwd, stdout=subprocess.PIPE,
                         stderr=subprocess.STDOUT, shell=False)
    out, _ = p.communicate()
    text = out.decode("utf-8", "replace")
    if check and p.returncode != 0:
        sys.stderr.write(text[-2000:] + chr(10))
        raise RuntimeError("command failed (%d): %s" % (p.returncode, " ".join(cmd)))
    return text


# | strategy | final cash | ledger | reputation | tables | crew | served | lost
# | emptied | first debt | trivial |
ROW = re.compile(r"^\|\s*(\w+)\s*\|\s*([-\d,]+)\s*\|\s*([-\d,]+)\s*\|"
                 r"\s*([\d.]+)\s*\|\s*([\d.]+)\s*\|"
                 r"\s*([\d.]+)\s*\|\s*(\d+)\s*\|\s*(\d+)\s*\|\s*([-\d]+)\s*\|"
                 r"\s*([-\d]+)\s*\|\s*([-\d.]+)\s*\|", re.M)


# An income-statement row: | strategy | revenue | ingredients | wages | rent | net | ...
INCOME = re.compile(r"^\|\s*(\w+)\s*\|\s*([\d,]+)\s*\|\s*([\d,]+)\s*\|"
                    r"\s*([\d,]+)\s*\|\s*([\d,]+)\s*\|\s*([-\d,]+)\s*\|", re.M)


def measured_bp(out):
    """
    The realisation rate the simulation PRODUCES: the planner's sixty-day
    revenue divided by the closed-form model's DEMAND revenue over the same
    calendar.

    CAREFUL: this number CAN GO ABOVE 100% and that is not an error. The
    closed-form model's demand depends on the reputation and ticket
    assumptions in the PLAN curve. In the simulation the planner pushes
    reputation to 100, beats the 88 the model planned for, and more customers
    arrive. So the number carries two things at once: "how much of the demand
    was served" and "did the player beat the plan curve".

    That is why it is REPORTED but IS NOT THE SELECTION CRITERION. Ties are
    broken on design grounds; see below.
    """
    import importlib
    import model
    importlib.reload(model)
    rows = model.run()
    demand56 = sum(r["revenue"] for r in rows) * 10_000.0 / model.REALISATION_BP
    demand60 = demand56 * 60.0 / 56.0

    for m in INCOME.finditer(out):
        if m.group(1) == "planci":
            rev = int(m.group(2).replace(",", ""))
            return int(round(rev / demand60 * 10_000))
    return 0


# Every cuisine. docs/33: measuring with one cuisine hid, for sixty days, the
# fact that the Turkish restaurant went under with zero customers in all eight
# strategies.
CUISINES = ["fastfood", "turk"]


def harness(cuisine="fastfood", seeds=None):
    # On this machine the Windows "Application Control" policy blocks
    # freshly written assemblies, and WHICH configuration it blocks changes
    # over time: Debug is blocked one day, Release the next. The first
    # version pinned it as "-c Release is required" and the next block
    # stopped the calibration completely.
    #
    # The right answer is not to PIN but to TRY: whichever works. If neither
    # works an error is raised - quietly returning an empty table opened the
    # way for the calibration to treat all candidates as equal.
    extra = ["--seeds", str(seeds)] if seeds else []
    out = ""

    # THE PROJECT ALREADY HAS THE SMART APP CONTROL WORKAROUND AND THIS FILE
    # WAS NOT USING IT.
    #
    # tools/dotnet_retry.py is the documented way to invoke dotnet in this
    # repository (CLAUDE.md rule 5) and it makes SIX attempts, changing the
    # module hash between each. The loop below makes three, and on 18
    # September that difference cost two whole calibration runs: SAC blocked
    # the harness, three attempts ran out, and calibrate died AFTER it had
    # already written a candidate's numbers into model.py and content/. The
    # working tree was left standing on a setting nobody had chosen, and
    # nothing said so.
    #
    # The loop underneath stays as the second line of defence; the first
    # attempt now goes through the tool that knows about this.
    out = run([sys.executable,
               os.path.join(ROOT, "tools", "dotnet_retry.py"),
               "run", "--project", "src/Lokanta.Harness", "--",
               "--cuisine", cuisine] + extra, check=False)
    if ROW.search(out):
        return _parse(out), out

    # First the normal way, then BY CHANGING THE HASH and REBUILDING.
    #
    # Both are needed, and this was written wrongly twice:
    # -p:Deterministic=false on its own does nothing, because if the source
    # has not changed MSBuild considers the build up to date and SKIPS it,
    # and the same blocked binary is used again. --no-incremental really does
    # repeat the build; only then does each attempt get a new module identity
    # and a new chance.
    #
    # Three attempts: a fresh hash can be blocked too.
    for attempt in range(3):
        if attempt > 0:
            # THE REBUILD IS A SEPARATE STEP.
            #
            # "dotnet run --no-incremental" is no use: run does not recognise
            # that flag and passes it on TO THE APPLICATION, and the harness
            # rejects it as an unknown flag (the correct behaviour). The build
            # has to be invoked separately.
            run(["dotnet", "build", "src/Lokanta.Harness",
                 "-c", "Release", "-v", "q", "--nologo",
                 "-p:Deterministic=false", "--no-incremental"], check=False)

        out = run(["dotnet", "run", "--project", "src/Lokanta.Harness",
                   "-c", "Release", "-v", "q", "--nologo"]
                  + (["--no-build"] if attempt > 0 else [])
                  + ["--", "--cuisine", cuisine] + extra, check=False)
        if ROW.search(out):
            break

    rows = _parse(out)
    if not rows:
        # If not even a single row could be read then the problem is not in
        # the balance but in the run. Applying a penalty and carrying on is
        # wrong: all the candidates come out equal and calibrate writes down
        # one of them at random as "the best".
        sys.stderr.write(out[-2000:] + chr(10))
        raise RuntimeError("harness output is empty: " + cuisine)
    return rows, out


SCORE_ROW = re.compile(r"^\|\s*(\w+)\s*\|\s*(\d+)\s*\|\s*(\d+)\s*\|\s*(\d+)\s*\|"
                       r"\s*(\d+)\s*\|\s*(\d+)\s*\|\s*(\d+)\s*\|\s*(\d+)\s*\|"
                       r"\s*(\d+)\s*\|\s*([\d.]+)\s*\|", re.M)


def _parse(out):
    """The strategy table, as {name: row}. Empty if the run produced none.

    THE SCORE TABLE IS READ TOO. The harness has printed the seven year-end
    axes for weeks and this tool never looked at them: every check was on
    the till, while the game tells the player seven axes and docs/12 8b says
    money is not the goal. docs/64 6 called that the contradiction to fix
    first - the crew axis rewarded hiring to the cap and nobody noticed
    because nothing here asserted on it.
    """
    rows = {}
    for m in ROW.finditer(out):
        name = m.group(1)
        book = m.group(3).replace(",", "")
        rows[name] = dict(
            cash=int(m.group(2).replace(",", "")),
            # Credit still on the books: not in the till, but not lost either.
            book=0 if book == "-" else int(book),
            rep=float(m.group(4)),
            tables=float(m.group(5)),
            crew=float(m.group(6)),
            served=int(m.group(7)),
            lost=int(m.group(8)),
            collapse=m.group(9),
            debt=m.group(10),
            trivial=m.group(11),
        )
    for m in SCORE_ROW.finditer(out):
        name = m.group(1)
        if name not in rows:
            continue
        rows[name].update(
            score=int(m.group(2)), axis_wealth=int(m.group(3)), axis_rep=int(m.group(4)),
            axis_regulars=int(m.group(5)), axis_crew=int(m.group(6)), axis_place=int(m.group(7)),
            axis_resilience=int(m.group(8)), axis_signature=int(m.group(9)))
    return rows


# ---------------------------------------------------------------------------
def evaluate(rows):
    """
    The design targets, docs/12 8 and docs/29. A penalty per violation.
    The targets are ORDERING targets: not an absolute number, but the order
    between the strategies.
    """
    pen, notes = 0.0, []

    def bad(cond, w, msg):
        nonlocal pen
        if not cond:
            pen += w
            notes.append(msg)

    need = ["pasif", "sadece_hal", "makul", "genislemeyen", "atilgan",
            "planci", "yuksek_fiyat", "fazla_kadro", "ucuz_malzeme",
            "mudahaleci", "imzaci"]
    for n in need:
        if n not in rows:
            return 999.0, ["strategy could not be read: " + n]

    # "Going under" IS NO LONGER "ENDING WITH NEGATIVE CASH".
    #
    # Since the bankruptcy ladder was written, cash does not stay buried below
    # zero: equipment is sold, the shop shrinks, and the remaining debt is
    # cleared at the cost of reputation. So even somebody who plays badly
    # finishes above zero - and the old criterion counted that as "did not go
    # under" and applied a penalty every time.
    #
    # The right question now is "DID IT GO DOWN THE LADDER": falling into debt
    # is an event with a price. The "debt" column carries the first day of
    # debt; "-" means it never happened.
    # THE bad() CONDITION IS THE DESIRED STATE: if it is false a penalty is
    # recorded.
    bad(rows["pasif"]["debt"] != "-", 12, "the passive player never falls into debt")
    bad(rows["atilgan"]["debt"] != "-", 10, "the reckless expander never falls into debt")
    bad(rows["makul"]["cash"] > 0, 12, "the reasonable player goes under")
    bad(rows["planci"]["cash"] > 0, 8, "the planner goes under")
    bad(rows["genislemeyen"]["cash"] > 0, 6, "the non-expander goes under")

    # GROWTH HAS TO PAY, AND IT MUST NOT BE A NO-BRAINER. docs/12: "Growing
    # pays, but by a factor of two, not eleven."
    #
    # THIS USED TO DIVIDE THE TILL, AND THE TILL INCLUDES THE STAKE. Both
    # bots start with the same 8,000, so the old ratio was
    # (8,000 + earned) / (8,000 + earned'): a number pulled toward 1 by a
    # constant that has nothing to do with growing. It read the design
    # BACKWARDS in both cuisines at once, on the same 32 seeds:
    #
    #     fast food   till 1.60  FAIL      earned 2.56   inside the band
    #     Turkish     till 2.25  pass      earned 35.1   "eleven", three times
    #
    # A week of calibration chased the fast food number through nine
    # realisation rates, and it could not move, because the game already met
    # the sentence it was being measured against.
    #
    # The design quantity is what growing EARNS over the campaign, so the
    # stake comes off both sides. That exposes the Turkish case the stake had
    # been hiding: standing still there nets 305 coins in sixty days, so
    # growth is not "rewarded", it is the only move - and a ratio on a base
    # of 305 is not a measurement, it is a division. When the base is under a
    # quarter of the stake the complaint is THAT, and it points at the actual
    # remedy (docs/52: spoilage and tabs) rather than at the rent. The
    # quarter is a judgment: fast food's non-expander earns 62% of the stake,
    # Turkish's 4%, and the line is drawn where it is not near either.
    import model
    stake = model.START_CASH
    earned_g = rows["genislemeyen"]["cash"] - stake
    earned_m = rows["makul"]["cash"] - stake
    if earned_g < stake // 4:
        bad(False, 10,
            "growth is a no-brainer: standing still earns %d on a stake of %d in "
            "sixty days (the grower earns %d), so there is no base to measure the "
            "multiplier against" % (earned_g, stake, earned_m))
    else:
        ratio = earned_m / float(earned_g)
        bad(1.8 <= ratio <= 4.0, 10,
            "growth multiplier %.2f on what the campaign earns (target 1.8-4.0)"
            % ratio)

    # Passivity has to lose visibly. "sadece_hal" does nothing at all: it does
    # not hire, does not manage the menu, does not expand, does not buy
    # equipment.
    #
    # The threshold is deliberately NOT "less than the non-expander". At four
    # tables the crew is sized against the WEEKEND PEAK (docs/12 5.1) and that
    # second person does not pay for themselves; so a non-expanding player
    # earns little even though they are following the model correctly. That is
    # a genuine economic statement, not a fault. What needs measuring is that
    # passivity does not beat GOOD PLAY.
    #
    # A threshold of "60%" was tried and turned out to be arbitrary: the
    # result oscillated right on that line (fast food 59%, Turkish 61%). What
    # needs measuring is not a percentage but an ORDER: a player who does
    # nothing must not beat one who follows the plan exactly.
    bad(rows["sadece_hal"]["cash"] < rows["planci"]["cash"], 10,
        "sadece_hal (%d) beats the planner (%d)"
        % (rows["sadece_hal"]["cash"], rows["planci"]["cash"]))
    bad(rows["sadece_hal"]["cash"] < rows["makul"]["cash"], 6,
        "sadece_hal beats good play")
    bad(rows["fazla_kadro"]["cash"] < rows["makul"]["cash"], 4,
        "overstaffing beats good play")

    # Skimping on ingredients has to be a TRAP: it lowers the unit cost but
    # wrecks the reputation, customers fall away and growth stops. Because the
    # content makes the six most sensitive ingredients MEAT, the chain bites
    # harder on a menu-wide basis.
    bad(rows["ucuz_malzeme"]["cash"] < rows["makul"]["cash"], 6,
        "cheap ingredients (%d) beat good play (%d)"
        % (rows["ucuz_malzeme"]["cash"], rows["makul"]["cash"]))

    # The docs/02 core loop: "during service you only intervene in crises".
    # Interventions are limited and offering tea costs money, but it still has
    # to BE WORTH SOMETHING; otherwise the only thing the player can do during
    # service is lose money.
    #
    # The measure is NOT FINAL CASH, and that is deliberate. It was first
    # measured with cash and in the Turkish cuisine it came out as
    # "intervening loses money": 24,058 against 28,638. But in the same run
    # the intervener's reputation was 81.3 (reasonable 56.5), tables 11 (8.4)
    # and customers served 2,152 (1,998). That is, they were running a BIGGER
    # business, not a smaller one; their cash was lower because they had spent
    # it on expansion and staff.
    #
    # What the intervention directly affects is reputation and customers
    # served. Cash depends on what the strategy DOES with that reputation and
    # cannot be the measure of this mechanic.
    # The signature mechanic has to be a CHOICE: using it must be neither a
    # trap nor an obligation. docs/07 describes the mechanic as the thing that
    # separates one cuisine from another; a mechanic that loses in every run
    # does not distinguish anything, and a mechanic that wins in every run is
    # not a decision but a button.
    #
    # The measure is CASH + LEDGER: a tab leaves money uncollected on the
    # sixtieth day and that money is not lost (docs/08, net worth).
    im = rows["imzaci"]["cash"] + rows["imzaci"]["book"]
    mk = rows["makul"]["cash"]
    ratio = (im / float(mk)) if mk > 0 else 0.0
    bad(0.90 <= ratio <= 1.30, 6,
        "the signature mechanic is unbalanced: imzaci/makul %.2f (target 0.90-1.30)" % ratio)
    bad(rows["imzaci"]["debt"] == "-", 6, "imzaci falls into debt")

    # The tolerance is 3 points, and the reason is THE REPUTATION CEILING.
    #
    # This check was written when the reasonable player's reputation was 56;
    # back then the intervention had room to raise it. Once staff traits
    # arrived, the reasonable player learned to fix its own crew and reached
    # 99.3 in fast food - that is, there was no headroom left for the
    # intervention and the mechanic LOOKED like it "does not work". In the
    # same run, in the Turkish cuisine, the intervener was at 100.0 and the
    # reasonable player at 96.1: where there is room, the mechanic WORKS.
    #
    # The right check is not "always better" but "never worse" - measuring a
    # mechanic on an axis where the player is already saturated condemns it
    # unfairly (docs/34 18).
    bad(rows["mudahaleci"]["rep"] >= rows["makul"]["rep"] - 3.0, 6,
        "the intervention lowers reputation (%.1f vs %.1f)"
        % (rows["mudahaleci"]["rep"], rows["makul"]["rep"]))
    bad(rows["mudahaleci"]["served"] >= rows["makul"]["served"] * 0.98, 4,
        "the intervention lowers the customer count (%d vs %d)"
        % (rows["mudahaleci"]["served"], rows["makul"]["served"]))
    bad(rows["mudahaleci"]["debt"] == "-", 6,
        "mudahaleci falls into debt on day %s" % rows["mudahaleci"]["debt"])

    # A NARROW MENU MUST NOT DOMINATE. An acceptance test, not a balance
    # target.
    #
    # For a long time a dish taken off the menu was not counted as "asked
    # for", which meant narrowing had ZERO cost on the demand side. Measured:
    # a player keeping a single main dish on the menu beat the reasonable
    # player by 12% in fast food and 38% in the Turkish cuisine. The cold
    # store's second reward (being able to carry menu width) was therefore
    # worthless, and the reason for a thirty-two dish inventory to exist
    # disappeared.
    #
    # This line stands so that that mistake never comes back.
    if "tek_yemek" in rows:
        bad(rows["tek_yemek"]["cash"] <= rows["makul"]["cash"], 12,
            "a narrow menu dominates: tek_yemek %d, makul %d"
            % (rows["tek_yemek"]["cash"], rows["makul"]["cash"]))

    # THE USER'S RULE: a player who makes the necessary investments ON TIME
    # must push their reputation above 80. "Otherwise there is no point doing
    # business."
    #
    # The measure is not "makul" but "planci". They are different players:
    # the reasonable one expands only when it can comfortably afford to, i.e.
    # cautiously; the planner follows the calendar, buys the equipment and
    # builds the crew. The user's description is the second one. The
    # difference in measurement is large: in fast food makul is 77.4, planci
    # 94.2.
    bad(rows["planci"]["rep"] >= 80.0, 10,
        "the on-time investor's reputation is %.1f (target 80+)" % rows["planci"]["rep"])

    # THE USER'S RULE, two-sided, and both sides have to hold:
    #
    #   "somebody who does nothing"  MUST NOT get through the first week
    #   "somebody who manages badly" MUST get through it, with difficulty
    #
    # The measure is NOT the day of going under but the day of EMPTYING. Two
    # reasons:
    #
    # 1. docs/08 rejects closure: "the save is not deleted, the game does not
    #    end". Going under is not an ending but a ladder. So "not getting
    #    through the first week" cannot mean the game ending.
    # 2. A player who does nothing has ZERO revenue; the day they go under is
    #    pure arithmetic (cash / weekly cost = day 42) and the only way to
    #    pull it back to seven is to cut the cash down to one week's cost.
    #    Measured: at that point the planner drops to -18,976 and a good
    #    player's reputation to 16.
    #
    # The thing that is both measurable and correct: WHEN DID THE SHOP EMPTY.
    # The demand curve steepens below 20 points, so when reputation falls
    # there the restaurant visibly empties out. That is the day the player
    # sees it is over; the money in the till only delays the funeral.
    empty = rows["pasif"]["collapse"]
    bad(empty != "-" and float(empty) <= 7, 12,
        "the do-nothing player's shop empties on day %s (target: the first week)" % empty)

    lazy = rows["sadece_hal"]["debt"]
    bad(lazy == "-" or float(lazy) > 8, 10,
        "the bad manager cannot get through the first week (day %s)" % lazy)

    # A good player's shop must never empty. The measure is "makul": a careful
    # player who spends by measuring what they earn.
    bad(rows["makul"]["collapse"] == "-", 8,
        "the good player's shop empties on day %s" % rows["makul"]["collapse"])

    # "planci" measures something different: whether the closed-form model's
    # CALENDAR can be afforded. It expands without looking at the cash, so it
    # deliberately lives on the edge; a late shock is legitimate, an early
    # collapse is not.
    pc = rows["planci"]["collapse"]
    bad(pc == "-" or float(pc) > 30, 6,
        "planci empties on day %s (it must not collapse in the first half)" % pc)

    bad(rows["makul"]["tables"] >= 7, 6, "the reasonable player cannot expand")
    bad(rows["planci"]["tables"] >= 13, 6, "planci cannot keep to the calendar")
    bad(rows["fazla_kadro"]["cash"] < rows["makul"]["cash"], 4, "overstaffing is not punished")

    # THE PLAQUE, NOT ONLY THE TILL. The first axis asserted is the one the
    # review caught lying: the overstaffer must not out-score the reasonable
    # player on the crew axis. It did - 85 against 75 - because the axis was
    # roster / cap.
    #
    # A NOTE, NOT A PENALTY - and it was a penalty for one afternoon. With
    # the roster half fixed the overstaffer still tops the axis (85 against
    # 78), by MORALE: an idle crew is a happy one, the reasonable player's is
    # busy and occasionally let go. The axis is telling the truth, and the
    # fault is docs/14's raise and day-off levers, which do not exist - the
    # reference player has no way to lift morale except hiring people it
    # does not need. The realisation rate cannot fix that, and a penalty for
    # a fault the sweep cannot reach steers it toward whatever masks the
    # symptom (docs/63 8). So it is printed on every run and weighs nothing.
    if "axis_crew" in rows["fazla_kadro"] and "axis_crew" in rows["makul"]:
        # A NOTE THAT WEIGHS NOTHING, FOR THE SECOND TIME, AND THIS TIME WITH
        # THE LEVERS IN PLACE. docs/14's raise and day off exist now and the
        # reasonable player uses them (measured: 4 raises per eight Turkish
        # campaigns, 33 per eight fast food ones, and no days off, because a
        # crew sized to its need has nobody to spare). The gap to the idle
        # overstaffer closed from 85-79 to 85-81 and no further: a raise is
        # fifteen points once against fifteen per cent of a wage for good,
        # and a working crew's morale erodes on busy days where an idle one's
        # does not. That is the design's own arithmetic, not a fault the
        # realisation rate can reach, so it is printed and not scored.
        bad(rows["fazla_kadro"]["axis_crew"] <= rows["makul"]["axis_crew"], 0,
            "note: the idle overstaffer still tops the crew axis (%d against %d) - "
            "raises fire, days off need slack a right-sized crew does not have"
            % (rows["fazla_kadro"]["axis_crew"], rows["makul"]["axis_crew"]))
    else:
        bad(False, 6, "the score table was not read, so the crew axis is unchecked")
    bad(rows["yuksek_fiyat"]["cash"] < rows["makul"]["cash"], 4, "high prices are not punished")

    # Money must not stop mattering before the eighth week
    for n in ("makul", "planci"):
        t = rows[n]["trivial"]
        if t != "-":
            bad(float(t) >= 8.0, 5, "%s: money stops mattering in week %s" % (n, t))

    return pen, notes


def _snapshot():
    """The files this tool rewrites, so a crash can be undone.

    IT WRITES BEFORE IT KNOWS. The search has to run the harness against real
    content, so every candidate is applied to model.py, solve.py and content/
    in turn; only at the end is the winner re-applied. That is the right
    shape for the search and the wrong shape for a crash: on 18 September the
    harness was blocked by Smart App Control twice, calibrate died mid-sweep,
    and the working tree was left standing on REALISATION_BP 9335 - the last
    candidate tried, not the best one, and verified by nobody.

    Nothing announced it. The next verification run would have called an
    unchosen economy green.
    """
    keep = {}

    # THE FIRST VERSION OF THIS LIST WAS THE LIST I COULD THINK OF, AND IT WAS
    # SHORT BY FOUR FILES. It covered model.py, solve.py and content/, because
    # those are what calibrate itself writes - and missed that every candidate
    # runs export.py, which ends in render_docs() and splices the tables into
    # three documents, and writes the weekly golden on the way. So a crash
    # restored the economy and left the DOCUMENTS standing on the last
    # candidate tried. That is worse than the bug it was written for: the
    # numbers and the prose describing them would disagree, and nothing
    # compares them.
    #
    # The list is therefore derived from what the writers name, not from
    # memory: export.py:773-777 and render.py:281-284.
    for rel in ("tools/balance/model.py", "tools/balance/solve.py",
                "docs/12-economy.md", "docs/14-staff-system.md",
                "docs/32-equipment-and-rebalance.md"):
        # NORMALISED, because the explicit entries are written with forward
        # slashes and os.walk yields the platform separator: on Windows the
        # same file could be held under two spellings, and then which copy
        # _restore writes back would depend on dictionary order.
        path = os.path.normpath(os.path.join(ROOT, rel))
        keep[path] = io.open(path, "rb").read()
    for folder in (os.path.join(ROOT, "content"),
                   os.path.join(ROOT, "tests", "golden")):
        for base, _dirs, files in os.walk(folder):
            for f in files:
                if f.endswith(".json"):
                    path = os.path.normpath(os.path.join(base, f))
                    keep[path] = io.open(path, "rb").read()
    return keep


def _restore(keep):
    for path, blob in keep.items():
        if io.open(path, "rb").read() != blob:
            io.open(path, "wb").write(blob)


def main():
    only = [int(sys.argv[1])] if len(sys.argv) > 1 else CANDIDATES
    results = []
    keep = _snapshot()
    try:
        _search(only, results)
    except BaseException:
        # A HALF-APPLIED SETTING IS WORSE THAN NO SETTING, because it looks
        # like a decision. Put the tree back and say so, then re-raise: the
        # failure is still a failure.
        _restore(keep)
        sys.stderr.write(chr(10) + "calibrate failed; the working tree has "
                         + "been put back as it was." + chr(10))
        raise
    _report(results)


def _search(only, results):
    for bp in only:
        best = solve_for(bp)
        if best is None:
            print("%5d  solve found no solution" % bp)
            continue
        pen0, s, o, e, rents, fails = best
        apply_to_model(bp, s, o, e, rents)
        run([sys.executable, os.path.join(HERE, "export.py")])
        pen, notes, got = 0.0, [], 0
        for cuisine in CUISINES:
            rows, out = harness(cuisine)
            p2, n2 = evaluate(rows)
            pen += p2
            notes.extend(cuisine + ": " + x for x in n2)
            if cuisine == "fastfood":
                got = measured_bp(out)
        drift = abs(got - bp)
        results.append((pen, drift, bp, s, o, e, rents, notes))
        print("%5d  s=%.2f o=%.1f e=%.1f  rent %s  penalty %.0f  measured %5d  %s" % (
            bp, s, o, e, [rents[t] for t in (4, 7, 10, 14)], pen, got,
            "; ".join(notes) if notes else "BOTH CUISINES CLEAN"))

def _report(results):
    if not results:
        return
    # Penalty first. On a tie, the HIGHEST rate, which means the highest rent.
    #
    # The reasoning is docs/12 2: the rent is deliberately the dominant fixed
    # cost, the "metronome of pressure". If all the design targets hold, a
    # higher rent means more tension, and tension is what is wanted. The drift
    # from the fixed point is reported but does not decide: that number can go
    # above 100%, and when it does what it measures is not realisation but the
    # player beating the plan curve.
    results.sort(key=lambda r: (r[0], -r[2]))
    pen, drift, bp, s, o, e, rents, notes = results[0]
    print()
    print("=" * 70)
    print("BEST: realisation %d, penalty %.0f, drift from the fixed point %d" % (bp, pen, drift))
    print("=" * 70)
    apply_to_model(bp, s, o, e, rents)
    run([sys.executable, os.path.join(HERE, "export.py")])
    print("model.py and content/ have been written to this setting.")

    # RE-MEASURE THE WINNER WITH MORE SEEDS.
    #
    # The sweep runs with eight seeds and that is right FOR THE SEARCH: the
    # penalty difference between candidates is tens of points and the noise
    # does not disturb it. But some of the checks have a TWO PER CENT
    # tolerance, and at that scale eight seeds are nothing but noise. It was
    # measured - same setting, same content:
    #
    #     seeds    makul   mudahaleci   ratio
    #         8     1922         1828   95.1%   (the check BREAKS)
    #        16     1953         1921   98.4%   (passes)
    #        32     1969         1941   98.6%   (passes)
    #
    # That is, the tool was showing a balance that ought to pass as red, and
    # chasing that red would have meant chasing a problem that does not exist.
    #
    # Cheap SEARCH, careful VERIFICATION: sweep with eight, the winner with
    # thirty-two.
    print()
    print("=" * 70)
    print("VERIFICATION (32 seeds)")
    vpen, vnotes = 0.0, []
    for cuisine in CUISINES:
        vrows, _vout = harness(cuisine, seeds=32)
        p2, n2 = evaluate(vrows)
        vpen += p2
        vnotes.extend(cuisine + ": " + x for x in n2)
    print("penalty %.0f  %s" % (
        vpen, "; ".join(vnotes) if vnotes else "BOTH CUISINES CLEAN"))
    print("=" * 70)


if __name__ == "__main__":
    main()
