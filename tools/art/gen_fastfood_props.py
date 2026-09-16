# -*- coding: utf-8 -*-
"""
Fast food dukkani prop seti
============================================================================
docs/24-art-pipeline.md kademe 1: mobilya ve ekipman prosedurel uretiliyor.

Ilk masa denemesine bakip verdigim uc duzeltme burada uygulandi:
  1. Sandalye sirti oturagin ustunde bosluktaydi  -> sirt oturaga oturdu
  2. Beyaz tabak beyaz ortude kayboluyordu        -> ortu kirli bej
  3. Sandalyeler masadan on iki santim uzakti     -> iceri cekildi

Calistirma:
  "C:\\Program Files\\Blender Foundation\\Blender 5.2\\blender.exe" ^
      --background --factory-startup --python tools/art/gen_fastfood_props.py

Cikti: tools/art/out/<prop>_a.png, _b.png, _c.png
"""
import bpy
import os
import sys
import math

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "lib"))

import prim      # noqa: E402
import stage     # noqa: E402

OUT = os.path.join(HERE, "out")

# docs/24 ucgen butceleri
BUDGET_SMALL = 150
BUDGET_FURNITURE = 600
BUDGET_EQUIPMENT = 900


# ---------------------------------------------------------------------------
def build_table():
    """Iki kisilik masa. Sandalyeler iceri cekilmis (bos masa hali)."""
    parts = []
    parts.append(prim.box("Tabla", (0.86, 0.86, 0.05), (0, 0, 0.74), "ahsap"))
    # Duzeltme 2: ortu kirli bej, beyaz tabak uzerinde okunuyor
    parts.append(prim.box("Ortu", (0.80, 0.80, 0.012), (0, 0, 0.774), "ortu", bevel=0.004))
    for i, (x, y) in enumerate([(0.34, 0.34), (-0.34, 0.34), (0.34, -0.34), (-0.34, -0.34)]):
        parts.append(prim.box("Ayak%d" % i, (0.05, 0.05, 0.71), (x, y, 0.355), "ahsap_koyu"))
    parts.append(prim.cylinder("Tabak", 0.12, 0.020, (0, 0, 0.790), "tabak", verts=14))
    return parts


def build_chair(offset_y=0.0, flip=False):
    """
    Tek sandalye. Duzeltme 1: sirt oturagin USTUNE oturuyor, arada bosluk yok.
    Oturak ust yuzeyi 0.455, sirt tabani 0.455'ten basliyor.
    """
    s = -1 if flip else 1
    parts = []
    seat_z = 0.43
    seat_top = seat_z + 0.025
    parts.append(prim.box("Oturak", (0.40, 0.40, 0.05), (0, offset_y, seat_z), "plastik_kirmizi"))
    # Sirt: tabani tam oturak ustunde
    back_h = 0.40
    parts.append(prim.box("Sirt", (0.40, 0.05, back_h),
                          (0, offset_y + 0.175 * s, seat_top + back_h / 2),
                          "plastik_kirmizi"))
    for j, (cx, cy) in enumerate([(0.16, 0.16), (-0.16, 0.16), (0.16, -0.16), (-0.16, -0.16)]):
        parts.append(prim.box("SandalyeAyak%d" % j, (0.035, 0.035, 0.41),
                              (cx, offset_y + cy, 0.205), "metal_koyu"))
    return parts


def build_table_set():
    """Masa arti iki sandalye. Duzeltme 3: sandalyeler 0.78 -> 0.66."""
    parts = build_table()
    parts += build_chair(offset_y=0.66, flip=False)
    parts += build_chair(offset_y=-0.66, flip=True)
    return parts


def build_counter():
    """Kasa tezgahi. Ust tabla, govde, kasa kutusu."""
    parts = []
    parts.append(prim.box("TezgahGovde", (1.80, 0.60, 0.90), (0, 0, 0.45), "plastik_kirmizi"))
    parts.append(prim.box("TezgahTabla", (1.90, 0.68, 0.06), (0, 0, 0.93), "metal"))
    parts.append(prim.box("KasaGovde", (0.34, 0.28, 0.20), (0.55, 0.05, 1.06), "metal_koyu"))
    parts.append(prim.box("KasaEkran", (0.30, 0.03, 0.18), (0.55, -0.09, 1.22), "siyah",
                          rot=(math.radians(-18), 0, 0)))
    # Menu panosu duvara monte. Duvar arka planda; askida durmuyor.
    parts.append(prim.box("Menu", (1.40, 0.05, 0.45), (0, 1.70, 1.70), "siyah"))
    parts.append(prim.box("MenuCerceve", (1.48, 0.03, 0.53), (0, 1.74, 1.70), "metal_koyu"))
    return parts


def build_stove():
    """
    Izgara ve ocak. docs/23 8.3 istasyon: izgara.

    Duvara YASLI kuruluyor. Ilk kosuda ocak sahnenin ortasindaydi ve
    davlumbaz iki metre oteden duvara uzanamayip bosta duruyordu.
    Duvar yuzeyi y = 1,95; govde derinligi 0,70, yani merkez y = 1,60.
    """
    y = 1.60
    wall_face = 1.95
    parts = []
    parts.append(prim.box("OcakGovde", (1.20, 0.70, 0.86), (0, y, 0.43), "metal_koyu"))
    parts.append(prim.box("IzgaraYuzey", (1.10, 0.60, 0.05), (0, y, 0.885), "siyah"))
    for i in range(5):
        parts.append(prim.box("Cubuk%d" % i, (1.04, 0.03, 0.015),
                              (0, y - 0.20 + i * 0.10, 0.915), "metal"))

    # Davlumbaz duvara monte, bacasi duvarin onunden tavana cikiyor.
    parts.append(prim.box("Davlumbaz", (1.34, 0.78, 0.26), (0, y + 0.02, 1.76), "metal"))
    parts.append(prim.box("Baca", (0.50, 0.34, 1.00),
                          (0, wall_face - 0.20, 2.39), "metal"))

    for i in range(3):
        parts.append(prim.cylinder("Dugme%d" % i, 0.035, 0.05,
                                   (-0.35 + i * 0.35, y - 0.37, 0.70),
                                   "plastik_sari", verts=8))
    return parts


def build_fridge():
    parts = []
    parts.append(prim.box("DolapGovde", (0.80, 0.70, 1.90), (0, 0, 0.95), "metal"))
    parts.append(prim.box("DolapKapi", (0.76, 0.04, 0.90), (0, -0.36, 1.35), "metal_koyu"))
    parts.append(prim.box("DolapKapiAlt", (0.76, 0.04, 0.86), (0, -0.36, 0.45), "metal_koyu"))
    parts.append(prim.box("Kol", (0.05, 0.06, 0.60), (0.30, -0.41, 1.35), "metal"))
    return parts


def build_shelf():
    parts = []
    for i in range(3):
        parts.append(prim.box("Raf%d" % i, (1.20, 0.36, 0.04),
                              (0, 0, 0.50 + i * 0.45), "ahsap"))
    for x in (-0.56, 0.56):
        parts.append(prim.box("Dikme%s" % ("L" if x < 0 else "R"),
                              (0.06, 0.36, 1.50), (x, 0, 0.75), "ahsap_koyu"))
    return parts


def build_trash():
    parts = []
    parts.append(prim.cylinder("CopGovde", 0.26, 0.80, (0, 0, 0.40), "plastik_sari", verts=12))
    parts.append(prim.cylinder("CopKapak", 0.28, 0.06, (0, 0, 0.83), "metal_koyu", verts=12))
    return parts


def build_tray():
    parts = []
    parts.append(prim.box("Tepsi", (0.40, 0.30, 0.02), (0, 0, 0.01), "plastik_kirmizi", bevel=0.006))
    parts.append(prim.box("Kagit", (0.34, 0.24, 0.004), (0, 0, 0.022), "ortu", bevel=0.0))
    return parts


# ---------------------------------------------------------------------------
# ad, kurucu, ucgen butcesi, kamera hedefi, uzaklik, duvar gerekli mi
PROPS = [
    ("masa_seti", build_table_set, BUDGET_FURNITURE * 3, (0, 0, 0.55), 4.6, False),
    ("sandalye", lambda: build_chair(), BUDGET_FURNITURE, (0, 0, 0.45), 2.4, False),
    ("tezgah", build_counter, BUDGET_EQUIPMENT * 2, (0, 0.6, 1.05), 5.4, True),
    ("ocak", build_stove, BUDGET_EQUIPMENT * 2, (0, 1.6, 1.15), 5.2, True),
    ("dolap", build_fridge, BUDGET_EQUIPMENT, (0, 0, 0.95), 4.4, True),
    ("raf", build_shelf, BUDGET_FURNITURE, (0, 0, 0.80), 3.8, True),
    ("cop", build_trash, BUDGET_SMALL * 2, (0, 0, 0.42), 2.2, False),
    ("tepsi", build_tray, BUDGET_SMALL, (0, 0, 0.05), 1.2, False),
]


def main():
    print("")
    print("=" * 66)
    print("FAST FOOD PROP SETI")
    print("=" * 66)

    ok = True
    total = 0
    for name, builder, budget, target, dist, needs_wall in PROPS:
        prim.clear_scene()
        parts = builder()
        # Arka plan ucgen sayisina KATILMIYOR: sahne parcasi, prop degil.
        stage.backdrop(floor=True, wall=needs_wall)
        stage.lighting()
        tris = prim.tri_count(parts)
        total += tris
        if not stage.report(name, parts, budget, tris):
            ok = False
        # Duvar +Y'de duruyor. Duvarli proplarda kamera SADECE -Y
        # yarisindan bakmali; aksi halde duvarin arkasini goruyor ve
        # kare tamamen gri cikiyor. Ilk kosuda tam bunu yapti.
        angles = (250, 290, 330) if needs_wall else (45, 135, 250)
        stage.contact_sheet(OUT, name, target=target, dist=dist, angles=angles)

    print("-" * 66)
    print("  toplam ucgen: {}".format(total))
    print("  cikti       : {}".format(OUT))
    print("  sonuc       : {}".format("BUTCELER TAMAM" if ok else "BUTCE ASIMI VAR"))
    print("=" * 66)


if __name__ == "__main__":
    main()
