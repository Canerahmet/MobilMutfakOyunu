# -*- coding: utf-8 -*-
"""
Light, camera and contact sheet.
============================================================================
docs/24-art-pipeline.md: every generation script renders from three angles and
prints the triangle count. I look at the renders MYSELF; the developer does
not know Blender and I cannot see a mesh directly, but I can read a PNG.
"""
import bpy
import math
import mathutils
import os


def lighting(warm=True):
    """
    docs/19 B4: one directional light is real-time, the rest is baked.
    The same setup for the render: one sun, one fill, a bright sky.
    """
    bpy.ops.object.light_add(type="SUN", location=(4, -5, 7))
    sun = bpy.context.active_object
    sun.data.energy = 3.4
    sun.data.angle = math.radians(10)          # a soft shadow edge
    if warm:
        sun.data.color = (1.0, 0.95, 0.86)
    sun.rotation_euler = (math.radians(52), 0, math.radians(35))

    bpy.ops.object.light_add(type="AREA", location=(-4, 3, 4))
    fill = bpy.context.active_object
    fill.data.energy = 110
    fill.data.size = 7
    fill.data.color = (0.86, 0.90, 1.0)        # a cool fill against the warm sun
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
    """A camera looking at the target. The angle is in degrees, anticlockwise."""
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
    Floor and wall. A render hanging in the void makes judging proportions
    impossible.

    The wall is ONLY for wall-mounted pieces. On the first run the hob's
    extractor hood and the counter's menu board were hanging in mid air; both
    are wall-mounted, but because there was no wall in the contact sheet they
    looked broken.
    """
    import prim
    made = []
    if floor:
        made.append(prim.box("Floor", (size, size, 0.06), (0, 0, -0.03), "floor", bevel=0))
    if wall:
        # The cameras are at 45, 135 and 250 degrees; the wall stands on the
        # +Y side.
        made.append(prim.box("Wall", (size * 0.7, 0.10, 3.0), (0, 2.0, 1.5),
                             "wall", bevel=0))
    return made


def contact_sheet(out_dir, name, target=(0, 0, 0.5), dist=4.6,
                  angles=(45, 135, 250)):
    """
    Renders from three angles. The files: <name>_a.png, _b.png, _c.png
    Returns: the list of paths produced.
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
    """An ASCII report. The console is cp1252; accented characters will not print."""
    status = "OK" if tris <= budget else "OVER BUDGET"
    print("  {:<16} objects {:>3}  triangles {:>5} / {:<5} {}".format(
        name, len(objects), tris, budget, status))
    return tris <= budget
