# -*- coding: utf-8 -*-
"""
Parameter search - Batch A
============================================================================
model.py holds the formulae; this script searches for the PARAMETERS to feed
those formulae, starting from the design targets. No tuning by hand.

The parameters searched for:
    s  - the scale multiplier on the role capacities
    o  - the owner's work-day contribution
    e  - the scale multiplier on the expansion cost
The rents are solved analytically from the target margin on every attempt.

Running it:  python solve.py
"""
from math import ceil
import itertools

# --- Fixed formula inputs (the same as model.py) -----------------------------
SEATS_TURNOVER = 4
INGREDIENT_RATE = 0.32
# The same as model.py. The realisation rate measured from the simulation.
# THE VALUE IN model.py IS THE CORRECT ONE. This copy is written by
# calibrate.py; if it is changed by hand this file and the game's content
# split apart, and running this file on its own (the manual flow in docs/12)
# produces THE WRONG RENT.
REALISATION_BP = 7000
WEEKEND_DAYS = 2
XP_WAGE_GROWTH = 0.022
START_CASH = 8000

BASE_CAP = dict(cook=30, waiter=26, dishwasher=48, cashier=70)
WAGE = dict(cook=140, waiter=110, dishwasher=90, cashier=100)
BASE_UPGRADE = {4: 0, 7: 2500, 10: 4500, 14: 8000}
# The margin BEFORE CAPITAL EXPENDITURE: equipment is NOT in this ledger (the
# reasoning is in model.py). Read as "net margin" it would mean the game's
# largest investment line has been left out - the equipment ladder costs about
# 37,600 coins at 14 tables, and its prices are tuned with the harness
# measurement, not with the closed-form model.
MARGIN_TARGETS = {4: 0.05, 7: 0.09, 10: 0.14, 14: 0.20}

PLAN = [
    dict(week=1, tables=4,  rep=35, ticket=50),
    dict(week=2, tables=4,  rep=45, ticket=52),
    dict(week=3, tables=7,  rep=52, ticket=56),
    dict(week=4, tables=7,  rep=60, ticket=58),
    dict(week=5, tables=10, rep=68, ticket=63),
    dict(week=6, tables=10, rep=75, ticket=66),
    dict(week=7, tables=14, rep=82, ticket=70),
    dict(week=8, tables=14, rep=88, ticket=75),
]


BP = 10_000
WEEKDAY_BP = 10_000
WEEKEND_BP = 12_500
DEMAND_BASE_BP = 5_000


def mul_div(a, b, c):
    """Halves away from zero. The same as model.py and C# Fx.MulDiv."""
    p = a * b
    ap, ac = abs(p), abs(c)
    q = ap // ac
    if (ap - q * ac) * 2 >= ac:
        q += 1
    return q if (p >= 0) == (c > 0) else -q


def customers(tables, rep, day_factor_bp=WEEKDAY_BP):
    demand_bp = DEMAND_BASE_BP + int(rep) * 100
    return mul_div(tables * SEATS_TURNOVER * demand_bp * day_factor_bp, 1, BP * BP)


def simulate(s, o, e, rents=None):
    cap = {k: v * s for k, v in BASE_CAP.items()}
    hall_load = 1.0 / cap["waiter"] + 1.0 / cap["dishwasher"] + 1.0 / cap["cashier"]
    shares = {k: (1.0 / cap[k]) / hall_load for k in ("waiter", "dishwasher", "cashier")}
    wage_hall = sum(shares[k] * WAGE[k] for k in shares)

    rows, prev, cash = [], 4, START_CASH
    for row in PLAN:
        weekday = customers(row["tables"], row["rep"], WEEKDAY_BP)
        weekend = customers(row["tables"], row["rep"], WEEKEND_BP)
        wk = weekday * (7 - WEEKEND_DAYS) + weekend * WEEKEND_DAYS

        cook = ceil(weekend / cap["cook"])
        hall_work = weekend * hall_load
        hall = max(0, ceil(hall_work - o))
        crew_total = cook + hall

        wages = (cook * WAGE["cook"] + hall * wage_hall) * 7 * \
                ((1 + XP_WAGE_GROWTH) ** (row["week"] - 1))
        revenue = mul_div(wk * row["ticket"], REALISATION_BP, BP)
        ingredients = revenue * INGREDIENT_RATE
        expansion = int(BASE_UPGRADE[row["tables"]] * e) if row["tables"] != prev else 0
        rent = rents[row["tables"]] if rents else 0

        net = revenue - ingredients - wages - rent - expansion
        cash += net
        rows.append(dict(week=row["week"], tables=row["tables"], weekday=weekday,
                         weekend=weekend, cook=cook, hall=hall, crew=crew_total,
                         revenue=revenue, ingredients=ingredients, wages=wages,
                         rent=rent, expansion=expansion, net=net, cash=cash,
                         margin=net / revenue, wage_share=wages / revenue,
                         rent_share=(rent / revenue) if revenue else 0))
        prev = row["tables"]
    return rows, wage_hall, hall_load


def solve_rents(s, o, e):
    rows, _, _ = simulate(s, o, e)
    mature = {}
    for r in rows:
        mature[r["tables"]] = r          # the last one = the mature week
    out = {}
    for tables, r in mature.items():
        target = MARGIN_TARGETS[tables]
        rent = r["revenue"] * (1 - INGREDIENT_RATE - target) - r["wages"]
        out[tables] = int(round(rent / 50.0) * 50)
    return out


def score(rows):
    """The design targets. A penalty per violation; 0 = flawless."""
    pen, fails = 0.0, []

    def bad(cond, weight, msg):
        nonlocal pen
        if not cond:
            pen += weight
            fails.append(msg)

    crews = [r["crew"] for r in rows]
    bad(crews[0] == 1, 10, "W1 crew is not 1 ({})".format(crews[0]))
    bad(crews[1] == 2, 6, "W2 crew is not 2 ({})".format(crews[1]))
    bad(all(crews[i] >= crews[i - 1] for i in range(1, 8)), 10, "the crew goes backwards")
    bad(8 <= crews[-1] <= 12, 8, "W8 crew outside 8-12 ({})".format(crews[-1]))
    bad(max(crews[i] - crews[i - 1] for i in range(1, 8)) <= 3, 5, "crew jump >3")

    last = rows[-1]
    bad(0.22 <= last["wage_share"] <= 0.34, 6,
        "W8 wage share {:.0f}%".format(100 * last["wage_share"]))
    bad(last["rent_share"] <= 0.26, 6,
        "W8 rent share {:.0f}%".format(100 * last["rent_share"]))
    bad(0.16 <= last["margin"] <= 0.24, 6,
        "W8 margin {:.1f}%".format(100 * last["margin"]))

    for r in rows:
        if r["expansion"]:
            bad(r["net"] < 0, 4, "W{} expansion week does not make a loss".format(r["week"]))
        else:
            bad(r["net"] > 0, 4, "W{} mature week makes a loss".format(r["week"]))

    mn = min(r["cash"] for r in rows)
    bad(800 <= mn <= 4000, 8, "lowest cash {:.0f}".format(mn))
    bad(all(r["cash"] > 0 for r in rows), 12, "cash goes negative")
    bad(all(r["rent"] > 0 for r in rows), 12, "rent solved negative")

    rents = sorted(set(r["rent"] for r in rows))
    bad(rents == sorted(rents), 4, "rent is not monotonic")
    return pen, fails


def main():
    best = None
    grid_s = [round(x * 0.05, 2) for x in range(14, 31)]      # 0.70 .. 1.50
    grid_o = [round(x * 0.1, 1) for x in range(8, 25)]        # 0.8 .. 2.4
    grid_e = [round(x * 0.1, 1) for x in range(10, 31)]       # 1.0 .. 3.0

    for s, o, e in itertools.product(grid_s, grid_o, grid_e):
        rents = solve_rents(s, o, e)
        if any(v <= 0 for v in rents.values()):
            continue
        rows, wage_hall, hall_load = simulate(s, o, e, rents)
        pen, fails = score(rows)
        if best is None or pen < best[0]:
            best = (pen, s, o, e, rents, rows, fails, wage_hall, hall_load)
            if pen == 0:
                break

    pen, s, o, e, rents, rows, fails, wage_hall, hall_load = best
    print("=" * 74)
    print("BEST PARAMETER SET          penalty = {:.1f}".format(pen))
    print("=" * 74)
    print("capacity scale  s = {}".format(s))
    print("owner work-day  o = {}".format(o))
    print("expansion scale e = {}".format(e))
    print()
    print("Role capacities (customers/day):")
    for k, v in BASE_CAP.items():
        print("   {:<10} {}".format(k, int(round(v * s))))
    print()
    print("Expansion costs:")
    for t in (7, 10, 14):
        print("   {:>2} tables  {}".format(t, int(BASE_UPGRADE[t] * e)))
    print()
    print("Weekly rents (solved from the target margin):")
    for t in sorted(rents):
        print("   {:>2} tables  {}".format(t, rents[t]))
    print()
    print("Hall weighted daily wage: {:.0f}".format(wage_hall))
    print("Hall workload / customer: {:.4f} work-days".format(hall_load))
    if fails:
        print("\nRemaining violations:")
        for f in fails:
            print("   - " + f)
    print()
    print("| Week | Tables | Cooks | Hall | Crew | Customers weekday/weekend | Revenue | Wages | Rent | Expansion | Net | Cash | Margin |")
    print("|---|---|---|---|---|---|---|---|---|---|---|---|---|")
    for r in rows:
        print("| {w} | {t} | {a} | {s} | **{c}** | {wd}/{we} | {rev:,.0f} | {wg:,.0f} | {rt:,.0f} | {ex} | **{net:+,.0f}** | {cash:,.0f} | {mg:.1f}% |".format(
            w=r["week"], t=r["tables"], a=r["cook"], s=r["hall"], c=r["crew"],
            wd=r["weekday"], we=r["weekend"], rev=r["revenue"], wg=r["wages"],
            rt=r["rent"], ex="{:,.0f}".format(r["expansion"]) if r["expansion"] else "—",
            net=r["net"], cash=r["cash"], mg=100 * r["margin"]).replace(",", "."))


if __name__ == "__main__":
    main()
