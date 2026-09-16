# -*- coding: utf-8 -*-
"""
Isik, kamera ve temas sayfasi.
============================================================================
docs/24-art-pipeline.md: her uretim betigi uc acidan render alir ve ucgen
sayisini basar. Render'a BEN bakiyorum; gelistirici Blender bilmiyor ve
ben mesh'i dogrudan goremiyorum, ama PNG'yi okuyabiliyorum.
"""
import bpy
import math
import mathutils
import os


def lighting(warm=True):
    """
    docs/19 B4: bir yonlu isik gercek zamanli, gerisi pisirilmis.
    Render icin ayni kurulum: bir gunes, bir dolgu, acik gokyuzu.
    """
    bpy.ops.object.light_add(type="SUN", location=(4, -5, 7))
    sun = bpy.context.active_object
    sun.data.energy = 3.4
    sun.data.angle = math.radians(10)          # yumusak golge kenari
    if warm:
        sun.data.color = (1.0, 0.95, 0.86)
    sun.rotation_euler = (math.radians(52), 0, math.radians(35))

    bpy.ops.object.light_add(type="AREA", location=(-4, 3, 4))
    fill = bpy.context.active_object
    fill.data.energy = 110
    fill.data.size = 7
    fill.data.color = (0.86, 0.90, 1.0)        # soguk dolgu, sicak gunese karsi
    fill.rotation_euler = (math.radians(-45), 0, math.radians(-140))

    world = bpy.context.scene.world
    if world is None:
        world = bpy.data.worlds.new("W")
        bpy.context.scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs[0].default_value = (0.93, 0.94, 0.91, 1)
    bg.inputs[1].default_value = 0.5


def camera(angle_deg, target=(0, 0, 0.5), elev_deg=32, dist=4.6, lens=62):
    """Hedefe bakan kamera. Aci derece, saat yonunun tersine."""
    for o in list(bpy.data.objects):
        if o.type == "CAMERA":
            bpy.data.objects.remove(o, do_unlink=True)

    bpy.ops.object.camera_add()
    cam = bpy.context.active_object
    a, e = math.radians(angle_deg), math.radians(elev_deg)
    cam.location = (dist * math.cos(a) * math.cos(e),
                    dist * math.sin(a) * math.cos(e),
                    dist * math.sin(e) + target[2] * 0.5)

    d = mathutils.Vector(target) - cam.location
    cam.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()
    cam.data.lens = lens
    bpy.context.scene.camera = cam
    return cam


def _engine():
    sc = bpy.context.scene
    for name in ("BLENDER_EEVEE_NEXT", "BLENDER_EEVEE", "CYCLES"):
        try:
            sc.render.engine = name
            return name
        except TypeError:
            continue
    return sc.render.engine


def render(path, w=560, h=440):
    sc = bpy.context.scene
    engine = _engine()
    sc.render.resolution_x = w
    sc.render.resolution_y = h
    sc.render.resolution_percentage = 100
    sc.render.film_transparent = False
    sc.render.image_settings.file_format = "PNG"
    sc.render.filepath = path
    if engine == "CYCLES":
        sc.cycles.samples = 48
    bpy.ops.render.render(write_still=True)
    return engine


def backdrop(floor=True, wall=False, size=8.0):
    """
    Zemin ve duvar. Bosluga asili render, oran yargisini imkansiz kiliyor.

    Duvar SADECE duvara monte parcalar icin. Ilk kosuda ocagin davlumbazi
    ve tezgahin menu panosu bosta duruyordu; ikisi de duvara monte, ama
    temas sayfasinda duvar olmadigi icin kirik gorunuyorlardi.
    """
    import prim
    made = []
    if floor:
        made.append(prim.box("Zemin", (size, size, 0.06), (0, 0, -0.03), "zemin", bevel=0))
    if wall:
        # Kameralar 45, 135 ve 250 derecede; duvar +Y tarafinda duruyor.
        made.append(prim.box("Duvar", (size * 0.7, 0.10, 3.0), (0, 2.0, 1.5),
                             "duvar", bevel=0))
    return made


def contact_sheet(out_dir, name, target=(0, 0, 0.5), dist=4.6,
                  angles=(45, 135, 250)):
    """
    Uc acidan render alir. Dosyalar: <name>_a.png, _b.png, _c.png
    Doner: uretilen yol listesi.
    """
    if not os.path.isdir(out_dir):
        os.makedirs(out_dir)
    paths = []
    for suffix, ang in zip("abc", angles):
        camera(ang, target=target, dist=dist)
        p = os.path.join(out_dir, "{}_{}.png".format(name, suffix))
        render(p)
        paths.append(p)
    return paths


def report(name, objects, budget, tris):
    """ASCII rapor. Konsol cp1252; Turkce aksanli karakter basilmaz."""
    status = "TAMAM" if tris <= budget else "BUTCE ASILDI"
    print("  {:<16} nesne {:>3}  ucgen {:>5} / {:<5} {}".format(
        name, len(objects), tris, budget, status))
    return tris <= budget
