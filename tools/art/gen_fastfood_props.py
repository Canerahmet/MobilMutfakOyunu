# -*- coding: utf-8 -*-
"""
The fast food shop prop set
============================================================================
docs/24-art-pipeline.md tier 1: furniture and equipment are generated
procedurally.

The three corrections I gave after looking at the first table attempt are
applied here:
  1. The chair back floated above the seat      -> the back now sits on the seat
  2. A white plate disappeared on a white cloth -> the cloth is a dirty beige
  3. The chairs were twelve centimetres from the table -> pulled in

Running it:
  "C:\\Program Files\\Blender Foundation\\Blender 5.2\\blender.exe" ^
      --background --factory-startup --python tools/art/gen_fastfood_props.py

Output: tools/art/out/<prop>_a.png, _b.png, _c.png
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

# docs/24 triangle budgets
BUDGET_SMALL = 150
BUDGET_FURNITURE = 600
BUDGET_EQUIPMENT = 900


# ---------------------------------------------------------------------------
def build_table():
    """A table for two. The chairs are pulled in (the empty-table state)."""
    parts = []
    parts.append(prim.box("TableTop", (0.86, 0.86, 0.05), (0, 0, 0.74), "wood"))
    # Correction 2: the cloth is a dirty beige, so a white plate reads on it
    parts.append(prim.box("Cloth", (0.80, 0.80, 0.012), (0, 0, 0.774), "cloth", bevel=0.004))
    for i, (x, y) in enumerate([(0.34, 0.34), (-0.34, 0.34), (0.34, -0.34), (-0.34, -0.34)]):
        parts.append(prim.box("Leg%d" % i, (0.05, 0.05, 0.71), (x, y, 0.355), "wood_dark"))
    parts.append(prim.cylinder("Plate", 0.12, 0.020, (0, 0, 0.790), "plate", verts=14))
    return parts


def build_chair(offset_y=0.0, flip=False):
    """
    A single chair. Correction 1: the back sits ON TOP OF the seat, with no
    gap between them. The top surface of the seat is at 0.455 and the base of
    the back starts at 0.455.
    """
    s = -1 if flip else 1
    parts = []
    seat_z = 0.43
    seat_top = seat_z + 0.025
    parts.append(prim.box("Seat", (0.40, 0.40, 0.05), (0, offset_y, seat_z), "plastic_red"))
    # The back: its base sits exactly on top of the seat
    back_h = 0.40
    parts.append(prim.box("Back", (0.40, 0.05, back_h),
                          (0, offset_y + 0.175 * s, seat_top + back_h / 2),
                          "plastic_red"))
    for j, (cx, cy) in enumerate([(0.16, 0.16), (-0.16, 0.16), (0.16, -0.16), (-0.16, -0.16)]):
        parts.append(prim.box("ChairLeg%d" % j, (0.035, 0.035, 0.41),
                              (cx, offset_y + cy, 0.205), "metal_dark"))
    return parts


def build_table_set():
    """A table plus two chairs. Correction 3: the chairs move from 0.78 to 0.66."""
    parts = build_table()
    parts += build_chair(offset_y=0.66, flip=False)
    parts += build_chair(offset_y=-0.66, flip=True)
    return parts


def build_counter():
    """The till counter. A top, a body and the till itself."""
    parts = []
    parts.append(prim.box("CounterBody", (1.80, 0.60, 0.90), (0, 0, 0.45), "plastic_red"))
    parts.append(prim.box("CounterTop", (1.90, 0.68, 0.06), (0, 0, 0.93), "metal"))
    parts.append(prim.box("TillBody", (0.34, 0.28, 0.20), (0.55, 0.05, 1.06), "metal_dark"))
    parts.append(prim.box("TillScreen", (0.30, 0.03, 0.18), (0.55, -0.09, 1.22), "black",
                          rot=(math.radians(-18), 0, 0)))
    # The menu board is wall-mounted. The wall is in the backdrop; it is not
    # left hanging.
    parts.append(prim.box("Menu", (1.40, 0.05, 0.45), (0, 1.70, 1.70), "black"))
    parts.append(prim.box("MenuFrame", (1.48, 0.03, 0.53), (0, 1.74, 1.70), "metal_dark"))
    return parts


def build_stove():
    """
    The grill and the hob. docs/23 8.3 station: izgara.

    It is built AGAINST the wall. On the first run the hob was in the middle
    of the scene and the extractor hood could not reach two metres across to
    the wall, so it hung in mid air. The wall face is at y = 1.95; the body is
    0.70 deep, so its centre is at y = 1.60.
    """
    y = 1.60
    wall_face = 1.95
    parts = []
    parts.append(prim.box("StoveBody", (1.20, 0.70, 0.86), (0, y, 0.43), "metal_dark"))
    parts.append(prim.box("GrillSurface", (1.10, 0.60, 0.05), (0, y, 0.885), "black"))
    for i in range(5):
        parts.append(prim.box("Bar%d" % i, (1.04, 0.03, 0.015),
                              (0, y - 0.20 + i * 0.10, 0.915), "metal"))

    # The extractor hood is wall-mounted; its flue rises to the ceiling in
    # front of the wall.
    parts.append(prim.box("Hood", (1.34, 0.78, 0.26), (0, y + 0.02, 1.76), "metal"))
    parts.append(prim.box("Flue", (0.50, 0.34, 1.00),
                          (0, wall_face - 0.20, 2.39), "metal"))

    for i in range(3):
        parts.append(prim.cylinder("Knob%d" % i, 0.035, 0.05,
                                   (-0.35 + i * 0.35, y - 0.37, 0.70),
                                   "plastic_yellow", verts=8))
    return parts


def build_fridge():
    parts = []
    parts.append(prim.box("FridgeBody", (0.80, 0.70, 1.90), (0, 0, 0.95), "metal"))
    parts.append(prim.box("FridgeDoor", (0.76, 0.04, 0.90), (0, -0.36, 1.35), "metal_dark"))
    parts.append(prim.box("FridgeDoorLower", (0.76, 0.04, 0.86), (0, -0.36, 0.45), "metal_dark"))
    parts.append(prim.box("Handle", (0.05, 0.06, 0.60), (0.30, -0.41, 1.35), "metal"))
    return parts


def build_shelf():
    parts = []
    for i in range(3):
        parts.append(prim.box("Shelf%d" % i, (1.20, 0.36, 0.04),
                              (0, 0, 0.50 + i * 0.45), "wood"))
    for x in (-0.56, 0.56):
        parts.append(prim.box("Upright%s" % ("L" if x < 0 else "R"),
                              (0.06, 0.36, 1.50), (x, 0, 0.75), "wood_dark"))
    return parts


def build_trash():
    parts = []
    parts.append(prim.cylinder("BinBody", 0.26, 0.80, (0, 0, 0.40), "plastic_yellow", verts=12))
    parts.append(prim.cylinder("BinLid", 0.28, 0.06, (0, 0, 0.83), "metal_dark", verts=12))
    return parts


def build_tray():
    parts = []
    parts.append(prim.box("Tray", (0.40, 0.30, 0.02), (0, 0, 0.01), "plastic_red", bevel=0.006))
    parts.append(prim.box("Paper", (0.34, 0.24, 0.004), (0, 0, 0.022), "cloth", bevel=0.0))
    return parts


# ---------------------------------------------------------------------------
# name, builder, triangle budget, camera target, distance, does it need a wall
#
# The names are artefact ids: they become the render file names under
# tools/art/out/, so they stay as they are.
PROPS = [
    ("table_set", build_table_set, BUDGET_FURNITURE * 3, (0, 0, 0.55), 4.6, False),
    ("chair", lambda: build_chair(), BUDGET_FURNITURE, (0, 0, 0.45), 2.4, False),
    ("counter", build_counter, BUDGET_EQUIPMENT * 2, (0, 0.6, 1.05), 5.4, True),
    ("stove", build_stove, BUDGET_EQUIPMENT * 2, (0, 1.6, 1.15), 5.2, True),
    ("cabinet", build_fridge, BUDGET_EQUIPMENT, (0, 0, 0.95), 4.4, True),
    ("shelf", build_shelf, BUDGET_FURNITURE, (0, 0, 0.80), 3.8, True),
    ("bin", build_trash, BUDGET_SMALL * 2, (0, 0, 0.42), 2.2, False),
    ("tray", build_tray, BUDGET_SMALL, (0, 0, 0.05), 1.2, False),
]


def main():
    print("")
    print("=" * 66)
    print("FAST FOOD PROP SET")
    print("=" * 66)

    ok = True
    total = 0
    for name, builder, budget, target, dist, needs_wall in PROPS:
        prim.clear_scene()
        parts = builder()
        # The backdrop DOES NOT COUNT towards the triangles: it is scenery,
        # not a prop.
        stage.backdrop(floor=True, wall=needs_wall)
        stage.lighting()
        tris = prim.tri_count(parts)
        total += tris
        if not stage.report(name, parts, budget, tris):
            ok = False
        # The wall stands at +Y. For props with a wall the camera must look
        # ONLY from the -Y half; otherwise it sees the back of the wall and
        # the frame comes out entirely grey. That is exactly what happened on
        # the first run.
        angles = (250, 290, 330) if needs_wall else (45, 135, 250)
        stage.contact_sheet(OUT, name, target=target, dist=dist, angles=angles)

    print("-" * 66)
    print("  total triangles: {}".format(total))
    print("  output         : {}".format(OUT))
    print("  result         : {}".format("BUDGETS OK" if ok else "OVER BUDGET"))
    print("=" * 66)


if __name__ == "__main__":
    main()
