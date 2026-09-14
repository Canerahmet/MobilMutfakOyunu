# -*- coding: utf-8 -*-
"""
Dokuman yazicisi - Parti A
============================================================================
model.py'nin urettigi tablolari docs/12-ekonomi.md ve docs/14-personel-sistemi.md
icine, isaretcilerin arasina yazar.

Isaretci bicimi:
    <!-- URETILEN: anahtar -->
    ... tablo ...
    <!-- /URETILEN: anahtar -->

Isaretciler arasindaki her sey her calistirmada silinip yeniden yazilir.
Bu dosyalardaki sayilari ELLE DEGISTIRMEYIN; model.py'deki parametreyi
degistirip bu betigi calistirin.

Calistirma:  python render.py
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
# Tablolar (Turkce basliklarla)
# ---------------------------------------------------------------------------

def t_rent():
    L = ["| Kademe | Masa | Haftalık kira | Bu kademeye geçiş bedeli |",
         "|---|---|---|---|"]
    names = ["Başlangıç", "İkinci", "Üçüncü", "Dördüncü"]
    for name, t in zip(names, model.TIERS):
        up = fmt(t["upgrade"]) if t["upgrade"] else "—"
        L.append("| {} | {} | {} | {} |".format(name, t["tables"], fmt(t["rent"]), up))
    return "\n".join(L)


def t_capacity():
    L = ["| Rol | Günlük kapasite | Günlük ücret | Müşteri başına iş | Salon yükü payı |",
         "|---|---|---|---|---|"]
    rows = [("Aşçı", model.CAP_ASCI, model.WAGE["asci"], None),
            ("Garson", model.CAP_GARSON, model.WAGE["garson"], "garson"),
            ("Bulaşıkçı", model.CAP_BULASIKCI, model.WAGE["bulasikci"], "bulasikci"),
            ("Kasiyer", model.CAP_KASIYER, model.WAGE["kasiyer"], "kasiyer")]
    for name, cap, wage, key in rows:
        if key is None:
            share = "ayrı havuz"
        else:
            share = "%{:.0f}".format(100 * model._shares[key])
        L.append("| {} | {} müşteri | {} | {:.4f} iş-günü | {} |".format(
            name, cap, wage, 1.0 / cap, share))
    return "\n".join(L)


def t_crew():
    L = ["| Hafta | Zirve müşteri/gün | Aşçı | Salon | Toplam kadro | Tavan | Salon iş yükü | Patron düşülünce |",
         "|---|---|---|---|---|---|---|---|"]
    for r in ROWS:
        c = r["crew"]
        L.append("| {w} | {pk} | {a} | {s} | **{tot}** | {cap} | {lw:.2f} | {aft:.2f} |".format(
            w=r["week"], pk=r["weekend"], a=c["asci"], s=c["salon"], tot=c["total"],
            cap=r["cap"], lw=c["salon_work"],
            aft=max(0.0, c["salon_work"] - model.OWNER_WORK)))
    return "\n".join(L)


def t_growth():
    L = ["| Hafta | Masa | Kadro | Tavan | İtibar | Müşteri/gün (içi / sonu) | Ort. fiş | Ciro | Malzeme | Maaş | Kira | Genişleme | Haftalık net | Kasa |",
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
    L = ["| Hafta | Malzeme | Maaş | Kira | Genişleme | Net marj |",
         "|---|---|---|---|---|---|"]
    for r in ROWS:
        rev = r["revenue"]
        L.append("| {w} | %{i:.0f} | %{m:.0f} | %{k:.0f} | %{e:.0f} | **%{n:.1f}** |".format(
            w=r["week"], i=100 * r["ingredients"] / rev, m=100 * r["wages"] / rev,
            k=100 * r["rent"] / rev, e=100 * r["expansion"] / rev,
            n=100 * r["margin"]).replace("%-", "−%"))
    return "\n".join(L)


def t_demand():
    """docs/12 5.1 - formulun kendi ornekleri, formulden turetilmis hali."""
    L = ["| Durum | Hesap | Hafta içi | Hafta sonu |", "|---|---|---|---|"]
    for tables, rep in ((4, 35), (7, 52), (10, 68), (14, 88)):
        wd = model.customers(tables, rep, model.WEEKDAY_BP)
        we = model.customers(tables, rep, model.WEEKEND_BP)
        L.append("| {t} masa, itibar {r} | {t} × 4 × {f:.2f} | {wd} | {we} |".format(
            t=tables, r=rep, f=0.5 + rep / 100.0, wd=wd, we=we))
    return "\n".join(L)


def t_equipment():
    """
    Ekipman merdiveni. docs/27 Karar D: basamak ya yuva ekler ya attendBp
    dusurur, prepMs'e dokunmaz. Fiyatlar kiradan turetiliyor.
    """
    L = ["| İstasyon | `attendBp` | Kademe | Yuva | `attendBp` | Fiyat | Gerekli olduğu masa |",
         "|---|---|---|---|---|---|---|"]
    names = {"ocak": "Ocak", "izgara": "Izgara", "firin": "Fırın",
             "soguk": "Soğuk", "icecek": "İçecek", "tatli": "Tatlı"}
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
                t["needAt"] if t["needAt"] else "isteğe bağlı"))
    L.append("")
    L.append("Merdivenin tamamı **{} sikke**.".format(fmt(total)))
    return chr(10).join(L)


def t_storage():
    """
    Soguk hava merdiveni ve her kademenin GERCEKTEN kurtardigi malzeme.

    Elle yazilmisti ve eskidi: tablo 2.340/3.480/10.000 yazarken icerik
    1.860/2.700/8.000 uretiyordu, ve "44 bozulabilir malzeme" derken
    sayi 36'ya inmisti. Uretilen bir tablo eskimez.

    Esik hesabi onemli ve sezgiye aykiri: omur = spoilDays x keepBp ve
    omur 1 ile omur 0 AYNI SEY (ikisi de o gece oluyor). Yani bir
    kademenin bir malzemeyi gercekten kurtarmasi icin omrun 2'ye
    ulasmasi gerekiyor - esik spoilDays >= 20000/keepBp.
    """
    import json as _json
    import os as _os
    root = _os.path.dirname(_os.path.dirname(_os.path.dirname(
        _os.path.abspath(__file__))))
    items = _json.load(io.open(
        _os.path.join(root, "content", "ingredients.json"), encoding="utf-8"))
    per = [i for i in items if i["perishable"]]

    L = ["| Kademe | `keepBp` | Kurtardığı malzeme | Fiyat |",
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
    L.append("Merdivenin tamamı **{} sikke**. Bozulabilir malzeme "
             "**{}** kalem.".format(fmt(total), len(per)))
    L.append("")
    L.append("Bir kademe bir malzemeyi ancak ömrünü **2 güne** çıkarabiliyorsa "
             "kurtarıyor: ömür 1 ile ömür 0 aynı gece çöpe gidiyor.")
    return chr(10).join(L)


BLOCKS = {
    "ekipman": t_equipment,
    "depo": t_storage,
    "kira": t_rent,
    "kapasite": t_capacity,
    "kadro": t_crew,
    "buyume": t_growth,
    "marj": t_margin,
    "talep": t_demand,
}


# ---------------------------------------------------------------------------
# Yazma
# ---------------------------------------------------------------------------

def splice(path, key, content):
    s = io.open(path, encoding="utf-8").read()
    start = "<!-- ÜRETİLEN: {} -->".format(key)
    end = "<!-- /ÜRETİLEN: {} -->".format(key)
    if start not in s:
        return False
    pat = re.compile(re.escape(start) + r".*?" + re.escape(end), re.S)
    s = pat.sub(start + "\n" + content + "\n" + end, s)
    io.open(path, "w", encoding="utf-8").write(s)
    return True


def main():
    targets = {
        "12-ekonomi.md": ["kira", "buyume", "talep"],
        "14-personel-sistemi.md": ["kapasite", "kadro", "marj"],
        "32-ekipman-ve-yeniden-denge.md": ["ekipman", "depo"],
    }
    n = 0
    for fname, keys in targets.items():
        path = os.path.join(DOCS, fname)
        for k in keys:
            if splice(path, k, BLOCKS[k]()):
                n += 1
                print("yazildi: {} -> {}".format(fname, k))
            else:
                print("ISARETCI YOK: {} -> {}".format(fname, k))
    print("---")
    print("{} blok yazildi".format(n))


if __name__ == "__main__":
    main()
