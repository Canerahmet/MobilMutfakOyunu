# -*- coding: utf-8 -*-
"""
Proof of the character pipeline
============================================================================
docs/24-art-pipeline.md tier 2, a two-week time box.

What has to be proved: does the 16 outfits x 3 body types = 96 meshes problem
really disappear.

The claim:
  body type   one mesh, bone scaling       -> the mesh does not multiply
  outfit      a skinless part on a bone    -> no weight painting
  colour      a material                   -> the mesh does not multiply

And the worry that weight painting needs visual judgement: six test poses are
rendered, and I look at the elbow and knee collapse MYSELF.

Running it:
  "C:\\Program Files\\Blender Foundation\\Blender 5.2\\blender.exe" ^
      --background --factory-startup --python tools/art/gen_character.py
"""
import bpy
import os
import sys
import math

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "lib"))

import prim    # noqa: E402
import stage   # noqa: E402
import rig     # noqa: E402

OUT = os.path.join(HERE, "out")

BUDGET_BODY = 2000       # docs/24 character body
BUDGET_ATTACH = 250      # an outfit part
BUDGET_HAIR = 300


# ---------------------------------------------------------------------------
def _seg(name, size, loc, material):
    """An unbevelled box: the body parts are joined up afterwards."""
    return prim.box(name, size, loc, material, bevel=0.0)


# The body parts: (name, size, location, BONE)
# Each part is bound RIGIDLY to a single bone. No skinning, no weight
# painting. The joints are closed with OVERLAPPING geometry: the parts are
# large enough to push into each other, so no gap opens when the bone turns.
BODY_PARTS = [
    ("Hips",     (0.31, 0.20, 0.20), (0.00, 0.00, 1.00), "hips"),
    ("Waist",    (0.28, 0.18, 0.20), (0.00, 0.00, 1.14), "spine"),
    ("Chest",    (0.35, 0.21, 0.26), (0.00, 0.00, 1.31), "chest"),
    ("Neck",     (0.12, 0.12, 0.13), (0.00, 0.00, 1.46), "neck"),
    ("Head",     (0.20, 0.21, 0.23), (0.00, 0.00, 1.63), "head"),
]

for _s, _tag in ((1, "L"), (-1, "R")):
    BODY_PARTS += [
        ("Shoulder" + _tag, (0.13, 0.16, 0.16), (_s * 0.19, 0, 1.39), "shoulder." + _tag),
        ("UpperArm" + _tag, (0.27, 0.13, 0.13), (_s * 0.30, 0, 1.40), "upperarm." + _tag),
        ("Forearm" + _tag,  (0.27, 0.12, 0.12), (_s * 0.54, 0, 1.40), "forearm." + _tag),
        ("Hand" + _tag,     (0.13, 0.10, 0.06), (_s * 0.72, 0, 1.40), "hand." + _tag),
        ("Thigh" + _tag,    (0.16, 0.17, 0.46), (_s * 0.11, 0, 0.745), "thigh." + _tag),
        ("Shin" + _tag,     (0.14, 0.15, 0.48), (_s * 0.11, 0, 0.320), "shin." + _tag),
        ("Foot" + _tag,     (0.13, 0.25, 0.08), (_s * 0.11, -0.06, 0.040), "foot." + _tag),
    ]


def build_body(arm):
    """
    A rigid, segmented low-poly body. Each part is a separate object bound to
    a single bone.

    On the first attempt the parts were joined into a single mesh and
    automatic weights were applied; because it was not welded, the legs came
    away from the hips in the sitting pose. The way to fix that was welding
    plus weight painting, and that work fell into the skill gap docs/24 had
    identified.
    """
    made = []
    for name, size, loc, bone in BODY_PARTS:
        o = prim.box(name, size, loc, "skin", bevel=0.012)
        rig.bind_rigid(o, arm, bone)
        made.append(o)
    return made


# ---------------------------------------------------------------------------
def build_apron(arm):
    """The apron: SKINLESS, bound to the hips. No weight painting."""
    o = prim.box("Apron", (0.32, 0.04, 0.46), (0, -0.11, 1.02), "apron", bevel=0.006)
    return rig.attach_to_bone(o, arm, "hips")


def build_cap(arm):
    """The cap: SKINLESS, bound to the head."""
    o = prim.box("Cap", (0.225, 0.235, 0.06), (0, 0, 1.775), "cap", bevel=0.008)
    return rig.attach_to_bone(o, arm, "head")


def build_hair(arm):
    """The hair: SKINLESS, bound to the head. All eight varieties attach the
    same way."""
    # The hair is a thin cap: in its first version it was 10 cm thick and
    # swallowed the top half of the head, with no skin showing at all.
    o = prim.box("Hair", (0.215, 0.225, 0.05), (0, 0.01, 1.727), "hair", bevel=0.008)
    return rig.attach_to_bone(o, arm, "head")


# ---------------------------------------------------------------------------
def main():
    prim.clear_scene()

    # The character palette
    prim.PALETTE["skin"] = (0.80, 0.62, 0.48)
    prim.PALETTE["apron"] = (0.90, 0.90, 0.87)
    prim.PALETTE["cap"] = (0.72, 0.18, 0.14)
    prim.PALETTE["hair"] = (0.20, 0.14, 0.10)

    print("")
    print("=" * 68)
    print("CHARACTER PIPELINE PROOF")
    print("=" * 68)

    arm = rig.build_armature()
    body = build_body(arm)

    apron = build_apron(arm)
    cap = build_cap(arm)
    hair = build_hair(arm)

    body_tris = prim.tri_count(body)
    attach_tris = prim.tri_count([apron, cap])
    hair_tris = prim.tri_count([hair])

    ok = True
    ok &= stage.report("body", body, BUDGET_BODY, body_tris)
    ok &= stage.report("outfit(2)", [apron, cap], BUDGET_ATTACH * 2, attach_tris)
    ok &= stage.report("hair", [hair], BUDGET_HAIR, hair_tris)

    print("")
    print("  mesh count: body {}, attachable 3, TOTAL {}".format(
        len(body), len(body) + 3))
    print("  extra meshes needed for 16 outfits x 3 body types: 0")

    stage.backdrop(floor=True, wall=False)
    stage.lighting()

    # --- Pose renders: the weighting collapse shows up here ----------------
    # The pose names are artefact ids: they become the render file names under
    # tools/art/out/, so they stay as they are.
    print("")
    print("  pose renders:")
    for pose_name in ("t_pose", "idle", "carry", "sit", "bend", "walk"):
        rig.apply_pose(arm, pose_name)
        stage.contact_sheet(OUT, "kar_" + pose_name,
                            target=(0, 0, 0.95), dist=3.9,
                            angles=(300, 235))
        print("    " + pose_name)

    # --- Body types: the same mesh, three scales ---------------------------
    print("")
    print("  body type renders:")
    rig.apply_pose(arm, "idle")
    for kind in ("slim", "mid", "broad"):
        rig.apply_body_type(arm, kind)
        stage.contact_sheet(OUT, "kar_tip_" + kind,
                            target=(0, 0, 0.95), dist=3.9, angles=(300,))
        print("    " + kind)

    print("")
    print("  result: " + ("BUDGETS OK" if ok else "OVER BUDGET"))
    print("=" * 68)


if __name__ == "__main__":
    main()
