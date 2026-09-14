# -*- coding: utf-8 -*-
"""
Karakter hatti kaniti
============================================================================
docs/24-sanat-hatti.md kademe 2, iki haftalik zaman kutusu.

Kanitlanmasi gereken sey: 16 kiyafet x 3 vucut tipi = 96 mesh sorunu
gercekten yok oluyor mu.

Iddia:
  vucut tipi   tek mesh, kemik olcegi        -> mesh cogalmiyor
  kiyafet      skinsiz parca, kemige bagli   -> agirlik boyama yok
  renk         malzeme                       -> mesh cogalmiyor

Ve agirlik boyamanin gorsel yargi istedigi endisesi: alti test pozu
render ediliyor, dirsek ve diz cokusune BEN bakiyorum.

Calistirma:
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

BUDGET_BODY = 2000       # docs/24 karakter govdesi
BUDGET_ATTACH = 250      # kiyafet parcasi
BUDGET_HAIR = 300


# ---------------------------------------------------------------------------
def _seg(name, size, loc, material):
    """Pah kirmasiz kutu: govde parcalari sonradan birlestiriliyor."""
    return prim.box(name, size, loc, material, bevel=0.0)


# Govde parcalari: (ad, boyut, konum, KEMIK)
# Her parca tek kemige KATI bagli. Skinning yok, agirlik boyama yok.
# Eklemler CAKISAN geometriyle kapatiliyor: parcalar birbirine girecek
# kadar buyuk, boylece kemik donunce bosluk acilmiyor.
BODY_PARTS = [
    ("Kalca",    (0.31, 0.20, 0.20), (0.00, 0.00, 1.00), "hips"),
    ("Bel",      (0.28, 0.18, 0.20), (0.00, 0.00, 1.14), "spine"),
    ("Gogus",    (0.35, 0.21, 0.26), (0.00, 0.00, 1.31), "chest"),
    ("Boyun",    (0.12, 0.12, 0.13), (0.00, 0.00, 1.46), "neck"),
    ("Kafa",     (0.20, 0.21, 0.23), (0.00, 0.00, 1.63), "head"),
]

for _s, _tag in ((1, "L"), (-1, "R")):
    BODY_PARTS += [
        ("Omuz" + _tag,     (0.13, 0.16, 0.16), (_s * 0.19, 0, 1.39), "shoulder." + _tag),
        ("UstKol" + _tag,   (0.27, 0.13, 0.13), (_s * 0.30, 0, 1.40), "upperarm." + _tag),
        ("OnKol" + _tag,    (0.27, 0.12, 0.12), (_s * 0.54, 0, 1.40), "forearm." + _tag),
        ("El" + _tag,       (0.13, 0.10, 0.06), (_s * 0.72, 0, 1.40), "hand." + _tag),
        ("UstBacak" + _tag, (0.16, 0.17, 0.46), (_s * 0.11, 0, 0.745), "thigh." + _tag),
        ("AltBacak" + _tag, (0.14, 0.15, 0.48), (_s * 0.11, 0, 0.320), "shin." + _tag),
        ("Ayak" + _tag,     (0.13, 0.25, 0.08), (_s * 0.11, -0.06, 0.040), "foot." + _tag),
    ]


def build_body(arm):
    """
    Katı parcali low-poly govde. Her parca ayri nesne, tek kemige bagli.

    Ilk denemede parcalar tek mesh'te birlestirilip otomatik agirlik
    uygulanmisti; kaynaklanmadigi icin oturma pozunda bacaklar kalcadan
    ayrildi. Duzeltmenin yolu kaynaklama artı agirlik boyamaydi ve o is
    docs/24'un tespit ettigi yetenek bosluguna giriyordu.
    """
    made = []
    for name, size, loc, bone in BODY_PARTS:
        o = prim.box(name, size, loc, "ten", bevel=0.012)
        rig.bind_rigid(o, arm, bone)
        made.append(o)
    return made


# ---------------------------------------------------------------------------
def build_apron(arm):
    """Onluk: SKINSIZ, kalcaya bagli. Agirlik boyama yok."""
    o = prim.box("Onluk", (0.32, 0.04, 0.46), (0, -0.11, 1.02), "onluk", bevel=0.006)
    return rig.attach_to_bone(o, arm, "hips")


def build_cap(arm):
    """Sapka: SKINSIZ, kafaya bagli."""
    o = prim.box("Sapka", (0.225, 0.235, 0.06), (0, 0, 1.775), "sapka", bevel=0.008)
    return rig.attach_to_bone(o, arm, "head")


def build_hair(arm):
    """Sac: SKINSIZ, kafaya bagli. Sekiz cesit ayni sekilde takiliyor."""
    # Sac ince bir kep: ilk halinde 10 cm kalinliktaydi ve kafanin
    # ust yarisini yutuyordu, ten hic gorunmuyordu.
    o = prim.box("Sac", (0.215, 0.225, 0.05), (0, 0.01, 1.727), "sac", bevel=0.008)
    return rig.attach_to_bone(o, arm, "head")


# ---------------------------------------------------------------------------
def main():
    prim.clear_scene()

    # Karakter paleti
    prim.PALETTE["ten"] = (0.80, 0.62, 0.48)
    prim.PALETTE["onluk"] = (0.90, 0.90, 0.87)
    prim.PALETTE["sapka"] = (0.72, 0.18, 0.14)
    prim.PALETTE["sac"] = (0.20, 0.14, 0.10)

    print("")
    print("=" * 68)
    print("KARAKTER HATTI KANITI")
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
    ok &= stage.report("govde", body, BUDGET_BODY, body_tris)
    ok &= stage.report("kiyafet(2)", [apron, cap], BUDGET_ATTACH * 2, attach_tris)
    ok &= stage.report("sac", [hair], BUDGET_HAIR, hair_tris)

    print("")
    print("  mesh sayisi: govde {}, takilabilir 3, TOPLAM {}".format(
        len(body), len(body) + 3))
    print("  16 kiyafet x 3 vucut tipi icin gereken ek mesh: 0")

    stage.backdrop(floor=True, wall=False)
    stage.lighting()

    # --- Poz render'lari: agirlik cokusu burada gorunur -------------------
    print("")
    print("  poz render'lari:")
    for pose_name in ("t_poz", "dinlenme", "tasima", "oturma", "egilme", "yuruyus"):
        rig.apply_pose(arm, pose_name)
        stage.contact_sheet(OUT, "kar_" + pose_name,
                            target=(0, 0, 0.95), dist=3.9,
                            angles=(300, 235))
        print("    " + pose_name)

    # --- Vucut tipleri: ayni mesh, uc olcek --------------------------------
    print("")
    print("  vucut tipi render'lari:")
    rig.apply_pose(arm, "dinlenme")
    for kind in ("ince", "orta", "genis"):
        rig.apply_body_type(arm, kind)
        stage.contact_sheet(OUT, "kar_tip_" + kind,
                            target=(0, 0, 0.95), dist=3.9, angles=(300,))
        print("    " + kind)

    print("")
    print("  sonuc: " + ("BUTCELER TAMAM" if ok else "BUTCE ASIMI VAR"))
    print("=" * 68)


if __name__ == "__main__":
    main()
