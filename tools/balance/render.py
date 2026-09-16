# -*- coding: utf-8 -*-
"""
Document writer - Batch A
============================================================================
Writes the tables model.py produces into docs/12-economy.md and
docs/14-staff-system.md, between the markers.

Marker format:
    <!-- GENERATED: key -->
    ... table ...
    <!-- /GENERATED: key -->

Everything between the markers is deleted and rewritten on every run.
DO NOT CHANGE the numbers in those files BY HAND; change the parameter in
model.py and run this script.

Running it:  python render.py
"""
import io
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import model  # noqa: E402

DOCS = os.path.join(os.path.dirname(os.path.dirname(
    os.path.dirname(os.path.abspath(__file__)))), "docs")

ROWS = model.run()


def fmt(n):
    return "{:,.0f}".format(n).replace(",", ".")


# ---------------------------------------------------------------------------
# The tables. THE HEADINGS AND CELL LABELS STAY TURKISH, and deliberately so:
# these strings are not this script's output, they are the CONTENT of
# docs/12-economy.md, docs/14-staff-system.md and
# docs/32-equipment-and-rebalance.md. Translating them here would drop English
# tables into the middle of Turkish prose, in files this script is only a
# writer for. They follow when those documents are translated.
# ---------------------------------------------------------------------------

def t_rent():
    L = ["| Tier | Tables | Weekly rent | Cost of moving to this tier |",
         "|---|---|---|---|"]
    names = ["Starting", "Second", "Third", "Fourth"]
    for name, t in zip(names, model.TIERS):
        up = fmt(t["upgrade"]) if t["upgrade"] else "—"
        L.append("| {} | {} | {} | {} |".format(name, t["tables"], fmt(t["rent"]), up))
    return "\n".join(L)


def t_capacity():
    L = ["| Role | Daily capacity | Daily wage | Work per guest | Share of hall load |",
         "|---|---|---|---|---|"]
    rows = [("Cook", model.CAP_COOK, model.WAGE["cook"], None),
            ("Waiter", model.CAP_WAITER, model.WAGE["waiter"], "waiter"),
            ("Dishwasher", model.CAP_DISHWASHER, model.WAGE["dishwasher"], "dishwasher"),
            ("Cashier", model.CAP_CASHIER, model.WAGE["cashier"], "cashier")]
    for name, cap, wage, key in rows:
        if key is None:
            share = "separate pool"
        else:
            share = "%{:.0f}".format(100 * model._shares[key])
        L.append("| {} | {} guests | {} | {:.4f} person-days | {} |".format(
            name, cap, wage, 1.0 / cap, share))
    return "\n".join(L)


def t_crew():
    L = ["| Week | Peak guests/day | Cooks | Hall | Total crew | Cap | Hall workload | After the owner |",
         "|---|---|---|---|---|---|---|---|"]
    for r in ROWS:
        c = r["crew"]
        L.append("| {w} | {pk} | {a} | {s} | **{tot}** | {cap} | {lw:.2f} | {aft:.2f} |".format(
            w=r["week"], pk=r["weekend"], a=c["cook"], s=c["hall"], tot=c["total"],
            cap=r["cap"], lw=c["hall_work"],
            aft=max(0.0, c["hall_work"] - model.OWNER_WORK)))
    return "\n".join(L)


def t_growth():
    L = ["| Week | Tables | Crew | Cap | Reputation | Guests/day (weekday / weekend) | Avg ticket | Revenue | Stock | Wages | Rent | Expansion | Weekly net | Till |",
         "|---|---|---|---|---|---|---|---|---|---|---|---|---|---|"]
    for r in ROWS:
        exp = "−" + fmt(r["expansion"]) if r["expansion"] else "—"
        L.append("| {w} | {t} | {c} | {cap} | {rep} | {wd} / {we} | {tk} | {rev} | −{ing} | −{wg} | −{rt} | {ex} | **{net}** | {cash} |".format(
            w=r["week"], t=r["tables"], c=r["crew_total"], cap=r["cap"], rep=r["rep"],
            wd=r["weekday"], we=r["weekend"], tk=r["ticket"],
            rev=fmt(r["revenue"]), ing=fmt(r["ingredients"]), wg=fmt(r["wages"]),
            rt=fmt(r["rent"]), ex=exp,
            net=("+" if r["net"] >= 0 else "−") + fmt(abs(r["net"])),
            cash=fmt(r["cash"])))
    return "\n".join(L)


def t_margin():
    L = ["| Week | Stock | Wages | Rent | Expansion | Net margin |",
         "|---|---|---|---|---|---|"]
    for r in ROWS:
        rev = r["revenue"]
        L.append("| {w} | %{i:.0f} | %{m:.0f} | %{k:.0f} | %{e:.0f} | **%{n:.1f}** |".format(
            w=r["week"], i=100 * r["ingredients"] / rev, m=100 * r["wages"] / rev,
            k=100 * r["rent"] / rev, e=100 * r["expansion"] / rev,
            n=100 * r["margin"]).replace("%-", "−%"))
    return "\n".join(L)


def t_demand():
    """docs/12 5.1 - the formula's own examples, derived from the formula."""
    L = ["| Case | Sum | Weekday | Weekend |", "|---|---|---|---|"]
    for tables, rep in ((4, 35), (7, 52), (10, 68), (14, 88)):
        wd = model.customers(tables, rep, model.WEEKDAY_BP)
        we = model.customers(tables, rep, model.WEEKEND_BP)
        L.append("| {t} tables, reputation {r} | {t} × 4 × {f:.2f} | {wd} | {we} |".format(
            t=tables, r=rep, f=0.5 + rep / 100.0, wd=wd, we=we))
    return "\n".join(L)


def t_equipment():
    """
    The equipment ladder. docs/27 Decision D: a step either adds a slot or
    lowers attendBp, and never touches prepMs. The prices are derived from
    the rent.
    """
    L = ["| Station | `attendBp` | Tier | Slots | `attendBp` | Price | Needed at tables |",
         "|---|---|---|---|---|---|---|"]
    names = {"ocak": "Stove", "izgara": "Grill", "firin": "Oven",
             "soguk": "Cold", "icecek": "Drinks", "tatli": "Desserts"}
    total = 0
    for st in model.equipment():
        base = st["tiers"][0]["attend"]
        for i, t in enumerate(st["tiers"]):
            total += t["price"]
            L.append("| {} | {} | t{} | {} | {} | {} | {} |".format(
                names.get(st["id"], st["id"]) if i == 0 else "",
                base if i == 0 else "",
                t["tier"], t["slots"], t["attend"],
                fmt(t["price"]) if t["price"] else "—",
                t["needAt"] if t["needAt"] else "optional"))
    L.append("")
    L.append("The whole ladder is **{} coins**.".format(fmt(total)))
    return chr(10).join(L)


def t_storage():
    """
    The cold-storage ladder and the ingredients each tier REALLY saves.

    It had been written by hand and went stale: the table said
    2,340/3,480/10,000 while the content produced 1,860/2,700/8,000, and it
    said "44 perishable ingredients" when the count had dropped to 36. A
    generated table does not go stale.

    The threshold arithmetic matters and is counter-intuitive: life =
    spoilDays x keepBp, and a life of 1 and a life of 0 are THE SAME THING
    (both die that night). So for a tier really to save an ingredient the
    life has to reach 2 - the threshold is spoilDays >= 20000/keepBp.
    """
    import json as _json
    import os as _os
    root = _os.path.dirname(_os.path.dirname(_os.path.dirname(
        _os.path.abspath(__file__))))
    items = _json.load(io.open(
        _os.path.join(root, "content", "ingredients.json"), encoding="utf-8"))
    per = [i for i in items if i["perishable"]]

    L = ["| Tier | `keepBp` | Ingredients saved | Price |",
         "|---|---:|---:|---:|"]
    tiers = model.storage()
    total = 0
    for t in tiers:
        keep = t["keep"]
        total += t["price"]
        if keep <= 0:
            saved = 0
        else:
            saved = sum(1 for i in per if i["spoilDays"] * keep // 10000 >= 2)
        L.append("| t{} | {} | {} / {} | {} |".format(
            t["tier"], keep, saved, len(per),
            fmt(t["price"]) if t["price"] else "—"))
    L.append("")
    L.append("The whole ladder is **{} coins**. Perishable ingredients: "
             "**{}** items.".format(fmt(total), len(per)))
    L.append("")
    L.append("A tier only saves an ingredient if it can raise its life to "
             "**2 days**: a life of 1 and a life of 0 go in the bin the "
             "same night.")
    return chr(10).join(L)


# The keys are the marker names inside the documents; they are a contract with
# docs/*.md, not prose.
BLOCKS = {
    "equipment": t_equipment,
    "storage": t_storage,
    "rent": t_rent,
    "capacity": t_capacity,
    "crew": t_crew,
    "growth": t_growth,
    "margin": t_margin,
    "demand": t_demand,
}


# ---------------------------------------------------------------------------
# Writing
# ---------------------------------------------------------------------------

def splice(path, key, content):
    s = io.open(path, encoding="utf-8").read()
    start = "<!-- GENERATED: {} -->".format(key)
    end = "<!-- /GENERATED: {} -->".format(key)
    if start not in s:
        return False
    pat = re.compile(re.escape(start) + r".*?" + re.escape(end), re.S)
    s = pat.sub(start + "\n" + content + "\n" + end, s)
    io.open(path, "w", encoding="utf-8").write(s)
    return True


def main():
    targets = {
        "12-economy.md": ["rent", "growth", "demand"],
        "14-staff-system.md": ["capacity", "crew", "margin"],
        "32-equipment-and-rebalance.md": ["equipment", "storage"],
    }
    n = 0
    for fname, keys in targets.items():
        path = os.path.join(DOCS, fname)
        for k in keys:
            if splice(path, k, BLOCKS[k]()):
                n += 1
                print("written: {} -> {}".format(fname, k))
            else:
                print("NO MARKER: {} -> {}".format(fname, k))
    print("---")
    print("{} blocks written".format(n))


if __name__ == "__main__":
    main()
