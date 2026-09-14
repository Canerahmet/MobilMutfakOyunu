# -*- coding: utf-8 -*-
"""
Parametre arayicisi - Parti A
============================================================================
model.py formulleri tutar; bu betik o formullere verilecek PARAMETRELERI
tasarim hedeflerinden arar. Elle ayar yok.

Aranan parametreler:
    s  - rol kapasitelerinin olcek carpani
    o  - patronun is-gunu katkisi
    e  - genisleme bedeli olcek carpani
Kiralar her denemede hedef marjdan analitik cozulur.

Calistirma:  python solve.py
"""
from math import ceil
import itertools

# --- Sabit formul girdileri (model.py ile ayni) ------------------------------
SEATS_TURNOVER = 4
INGREDIENT_RATE = 0.32
# model.py ile ayni. Simulasyondan olculen gerceklesme orani.
# DOGRUSU model.py'DEKI DEGER. Burasi calibrate.py tarafindan yaziliyor;
# elle degistirilirse oyunun icerigi ile bu dosya ayrilir ve bu dosyayi
# tek basina kosturmak (docs/12'deki elle akis) YANLIS KIRA uretir.
REALISATION_BP = 7000
WEEKEND_DAYS = 2
XP_WAGE_GROWTH = 0.022
START_CASH = 8000

BASE_CAP = dict(asci=30, garson=26, bulasikci=48, kasiyer=70)
WAGE = dict(asci=140, garson=110, bulasikci=90, kasiyer=100)
BASE_UPGRADE = {4: 0, 7: 2500, 10: 4500, 14: 8000}
# SERMAYE GIDERI ONCESI marj: ekipman bu defterde YOK (model.py'deki
# gerekce). "Net marj" diye okunursa oyunun en buyuk yatirim kalemi
# gorulmemis olur - ekipman merdiveni 14 masada ~37.600 sikke ve
# fiyatlari kapali form modelle degil, harness olcumuyle ayarlaniyor.
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
    """Yarisi sifirdan uzaga. model.py ve C# Fx.MulDiv ile ayni."""
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
    salon_load = 1.0 / cap["garson"] + 1.0 / cap["bulasikci"] + 1.0 / cap["kasiyer"]
    shares = {k: (1.0 / cap[k]) / salon_load for k in ("garson", "bulasikci", "kasiyer")}
    wage_salon = sum(shares[k] * WAGE[k] for k in shares)

    rows, prev, cash = [], 4, START_CASH
    for row in PLAN:
        weekday = customers(row["tables"], row["rep"], WEEKDAY_BP)
        weekend = customers(row["tables"], row["rep"], WEEKEND_BP)
        wk = weekday * (7 - WEEKEND_DAYS) + weekend * WEEKEND_DAYS

        asci = ceil(weekend / cap["asci"])
        salon_work = weekend * salon_load
        salon = max(0, ceil(salon_work - o))
        crew_total = asci + salon

        wages = (asci * WAGE["asci"] + salon * wage_salon) * 7 * \
                ((1 + XP_WAGE_GROWTH) ** (row["week"] - 1))
        revenue = mul_div(wk * row["ticket"], REALISATION_BP, BP)
        ingredients = revenue * INGREDIENT_RATE
        expansion = int(BASE_UPGRADE[row["tables"]] * e) if row["tables"] != prev else 0
        rent = rents[row["tables"]] if rents else 0

        net = revenue - ingredients - wages - rent - expansion
        cash += net
        rows.append(dict(week=row["week"], tables=row["tables"], weekday=weekday,
                         weekend=weekend, asci=asci, salon=salon, crew=crew_total,
                         revenue=revenue, ingredients=ingredients, wages=wages,
                         rent=rent, expansion=expansion, net=net, cash=cash,
                         margin=net / revenue, wage_share=wages / revenue,
                         rent_share=(rent / revenue) if revenue else 0))
        prev = row["tables"]
    return rows, wage_salon, salon_load


def solve_rents(s, o, e):
    rows, _, _ = simulate(s, o, e)
    mature = {}
    for r in rows:
        mature[r["tables"]] = r          # sonuncu = olgun hafta
    out = {}
    for tables, r in mature.items():
        target = MARGIN_TARGETS[tables]
        rent = r["revenue"] * (1 - INGREDIENT_RATE - target) - r["wages"]
        out[tables] = int(round(rent / 50.0) * 50)
    return out


def score(rows):
    """Tasarim hedefleri. Ihlal basina ceza; 0 = kusursuz."""
    pen, fails = 0.0, []

    def bad(cond, weight, msg):
        nonlocal pen
        if not cond:
            pen += weight
            fails.append(msg)

    crews = [r["crew"] for r in rows]
    bad(crews[0] == 1, 10, "H1 kadro 1 degil ({})".format(crews[0]))
    bad(crews[1] == 2, 6, "H2 kadro 2 degil ({})".format(crews[1]))
    bad(all(crews[i] >= crews[i - 1] for i in range(1, 8)), 10, "kadro geri gidiyor")
    bad(8 <= crews[-1] <= 12, 8, "H8 kadro 8-12 disinda ({})".format(crews[-1]))
    bad(max(crews[i] - crews[i - 1] for i in range(1, 8)) <= 3, 5, "kadro sicramasi >3")

    last = rows[-1]
    bad(0.22 <= last["wage_share"] <= 0.34, 6,
        "H8 maas payi %{:.0f}".format(100 * last["wage_share"]))
    bad(last["rent_share"] <= 0.26, 6,
        "H8 kira payi %{:.0f}".format(100 * last["rent_share"]))
    bad(0.16 <= last["margin"] <= 0.24, 6,
        "H8 marj %{:.1f}".format(100 * last["margin"]))

    for r in rows:
        if r["expansion"]:
            bad(r["net"] < 0, 4, "H{} genisleme haftasi zarar etmiyor".format(r["week"]))
        else:
            bad(r["net"] > 0, 4, "H{} olgun hafta zarar ediyor".format(r["week"]))

    mn = min(r["cash"] for r in rows)
    bad(800 <= mn <= 4000, 8, "en dusuk kasa {:.0f}".format(mn))
    bad(all(r["cash"] > 0 for r in rows), 12, "kasa eksiye dusuyor")
    bad(all(r["rent"] > 0 for r in rows), 12, "kira negatif cozuldu")

    rents = sorted(set(r["rent"] for r in rows))
    bad(rents == sorted(rents), 4, "kira monoton degil")
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
        rows, wage_salon, salon_load = simulate(s, o, e, rents)
        pen, fails = score(rows)
        if best is None or pen < best[0]:
            best = (pen, s, o, e, rents, rows, fails, wage_salon, salon_load)
            if pen == 0:
                break

    pen, s, o, e, rents, rows, fails, wage_salon, salon_load = best
    print("=" * 74)
    print("EN IYI PARAMETRE SETI      ceza = {:.1f}".format(pen))
    print("=" * 74)
    print("kapasite olcegi s = {}".format(s))
    print("patron is-gunu  o = {}".format(o))
    print("genisleme olcegi e = {}".format(e))
    print()
    print("Rol kapasiteleri (musteri/gun):")
    for k, v in BASE_CAP.items():
        print("   {:<10} {}".format(k, int(round(v * s))))
    print()
    print("Genisleme bedelleri:")
    for t in (7, 10, 14):
        print("   {:>2} masa  {}".format(t, int(BASE_UPGRADE[t] * e)))
    print()
    print("Haftalik kiralar (hedef marjdan cozuldu):")
    for t in sorted(rents):
        print("   {:>2} masa  {}".format(t, rents[t]))
    print()
    print("Salon agirlikli gunluk ucret: {:.0f}".format(wage_salon))
    print("Salon is yuku / musteri:      {:.4f} is-gunu".format(salon_load))
    if fails:
        print("\nKalan ihlaller:")
        for f in fails:
            print("   - " + f)
    print()
    print("| Hafta | Masa | Asci | Salon | Kadro | Musteri ici/sonu | Ciro | Maas | Kira | Genisleme | Net | Kasa | Marj |")
    print("|---|---|---|---|---|---|---|---|---|---|---|---|---|")
    for r in rows:
        print("| {w} | {t} | {a} | {s} | **{c}** | {wd}/{we} | {rev:,.0f} | {wg:,.0f} | {rt:,.0f} | {ex} | **{net:+,.0f}** | {cash:,.0f} | %{mg:.1f} |".format(
            w=r["week"], t=r["tables"], a=r["asci"], s=r["salon"], c=r["crew"],
            wd=r["weekday"], we=r["weekend"], rev=r["revenue"], wg=r["wages"],
            rt=r["rent"], ex="{:,.0f}".format(r["expansion"]) if r["expansion"] else "—",
            net=r["net"], cash=r["cash"], mg=100 * r["margin"]).replace(",", "."))


if __name__ == "__main__":
    main()
