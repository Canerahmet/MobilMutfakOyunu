# -*- coding: utf-8 -*-
"""
Blender generation script - the verification test
============================================================================
Purpose: to settle the question "who is going to judge the model".

The developer does not know Blender, and I (the AI) cannot see a mesh
directly. But if I run Blender headless and produce a PNG, I can read that
PNG. This script sets up that loop: code -> mesh -> render -> eye.

Running it:
  "C:\\Program Files\\Blender Foundation\\Blender 5.2\\blender.exe" ^
      --background --factory-startup --python tools/art/gen_table.py

Output: tools/art/out/table_XX.png  (from three angles)
"""
import bpy
import os
import math
import sys

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "out")
os.makedirs(OUT, exist_ok=True)


# ---------------------------------------------------------------------------
# clearing the scene
# ---------------------------------------------------------------------------
def clear():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for block in (bpy.data.meshes, bpy.data.materials):
        for b in list(block):
            if b.users == 0:
                block.remove(b)


# ---------------------------------------------------------------------------
# material: low-poly flat colour, slight roughness
# ---------------------------------------------------------------------------
def mat(name, rgb, rough=0.7):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (rgb[0], rgb[1], rgb[2], 1.0)
    bsdf.inputs["Roughness"].default_value = rough
    return m


def cube(name, size, loc, material, bevel=0.012):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    o = bpy.context.active_object
    o.name = name
    o.scale = (size[0], size[1], size[2])
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        m = o.modifiers.new("bevel", "BEVEL")
        m.width = bevel
        m.segments = 2
        m.limit_method = "ANGLE"
        m.angle_limit = math.radians(40)
    o.data.materials.append(material)
    bpy.ops.object.shade_smooth_by_angle(angle=math.radians(30))
    return o


def cyl(name, r, h, loc, material, verts=12):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=r, depth=h, location=loc)
    o = bpy.context.active_object
    o.name = name
    o.data.materials.append(material)
    bpy.ops.object.shade_smooth_by_angle(angle=math.radians(30))
    return o


# ---------------------------------------------------------------------------
# the asset: a restaurant table + two chairs + a plate
# ---------------------------------------------------------------------------
def build():
    wood = mat("Wood", (0.42, 0.26, 0.15))
    wood_d = mat("WoodDark", (0.30, 0.18, 0.10))
    cloth = mat("Cloth", (0.85, 0.87, 0.82), rough=0.9)
    plate = mat("Plate", (0.94, 0.94, 0.92), rough=0.35)

    parts = []
    # the top
    parts.append(cube("TableTop", (0.90, 0.90, 0.055), (0, 0, 0.74), wood))
    # the cloth (a thin square on top of the table top)
    parts.append(cube("Cloth", (0.82, 0.82, 0.012), (0, 0, 0.775), cloth, bevel=0.004))
    # four legs
    for i, (x, y) in enumerate([(0.36, 0.36), (-0.36, 0.36), (0.36, -0.36), (-0.36, -0.36)]):
        parts.append(cube("Leg%d" % i, (0.055, 0.055, 0.71), (x, y, 0.355), wood_d))
    # two chairs
    for i, sy in enumerate([0.78, -0.78]):
        s = 1 if sy > 0 else -1
        parts.append(cube("ChairSeat%d" % i, (0.42, 0.42, 0.05), (0, sy, 0.45), wood))
        parts.append(cube("ChairBack%d" % i, (0.42, 0.05, 0.42), (0, sy + 0.19 * s, 0.70), wood))
        for j, (cx, cy) in enumerate([(0.17, 0.17), (-0.17, 0.17), (0.17, -0.17), (-0.17, -0.17)]):
            parts.append(cube("ChairLeg%d_%d" % (i, j), (0.04, 0.04, 0.43),
                              (cx, sy + cy, 0.215), wood_d))
    # the plate
    parts.append(cyl("Plate", 0.13, 0.022, (0, 0, 0.792), plate, verts=16))
    return parts


# ---------------------------------------------------------------------------
# light and camera: close to the game's 2.5D angle
# ---------------------------------------------------------------------------
def lighting():
    bpy.ops.object.light_add(type="SUN", location=(4, -5, 7))
    sun = bpy.context.active_object
    sun.data.energy = 3.2
    sun.data.angle = math.radians(12)          # a soft shadow
    sun.rotation_euler = (math.radians(50), 0, math.radians(35))

    bpy.ops.object.light_add(type="AREA", location=(-4, 3, 4))
    fill = bpy.context.active_object
    fill.data.energy = 90
    fill.data.size = 6
    fill.rotation_euler = (math.radians(-45), 0, math.radians(-140))

    w = bpy.context.scene.world
    if w is None:
        w = bpy.data.worlds.new("W")
        bpy.context.scene.world = w
    w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.93, 0.94, 0.91, 1)
    w.node_tree.nodes["Background"].inputs[1].default_value = 0.55


def camera(angle_deg, elev_deg=32, dist=4.6):
    bpy.ops.object.camera_add()
    cam = bpy.context.active_object
    a, e = math.radians(angle_deg), math.radians(elev_deg)
    cam.location = (dist * math.cos(a) * math.cos(e),
                    dist * math.sin(a) * math.cos(e),
                    dist * math.sin(e) + 0.35)
    # point it at the target
    import mathutils
    target = mathutils.Vector((0, 0, 0.55))
    d = target - cam.location
    cam.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()
    cam.data.lens = 62                      # slightly telephoto, for the 2.5D look
    bpy.context.scene.camera = cam
    return cam


def render(path, w=560, h=420):
    sc = bpy.context.scene
    for engine in ("BLENDER_EEVEE_NEXT", "BLENDER_EEVEE", "CYCLES"):
        try:
            sc.render.engine = engine
            break
        except TypeError:
            continue
    sc.render.resolution_x = w
    sc.render.resolution_y = h
    sc.render.resolution_percentage = 100
    sc.render.film_transparent = False
    sc.render.image_settings.file_format = "PNG"
    sc.render.filepath = path
    if sc.render.engine == "CYCLES":
        sc.cycles.samples = 48
    bpy.ops.render.render(write_still=True)
    return sc.render.engine


def poly_count(parts):
    tris = 0
    for o in parts:
        if o.type != "MESH":
            continue
        me = o.evaluated_get(bpy.context.evaluated_depsgraph_get()).to_mesh()
        tris += sum(max(0, len(p.vertices) - 2) for p in me.polygons)
    return tris


def main():
    clear()
    parts = build()
    lighting()
    tris = poly_count(parts)

    engine = None
    for name, ang in (("a", 45), ("b", 135), ("c", 250)):
        if bpy.context.scene.camera:
            bpy.data.objects.remove(bpy.context.scene.camera, do_unlink=True)
        camera(ang)
        engine = render(os.path.join(OUT, "table_%s.png" % name))

    print("")
    print("=" * 60)
    print("GENERATED")
    print("  object count   : %d" % len(parts))
    print("  triangle count : %d" % tris)
    print("  engine         : %s" % engine)
    print("  output         : %s" % OUT)
    print("=" * 60)


if __name__ == "__main__":
    main()
