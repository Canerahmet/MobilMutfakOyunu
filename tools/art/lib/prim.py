# -*- coding: utf-8 -*-
"""
Low-poly primitive helpers.
============================================================================
docs/24-art-pipeline.md tier 1: furniture, equipment and environment are
procedural. No textures; everything is flat-colour material and bevelling.

All measurements are in METRES. The game is 2.5D but the scene is built at
real scale, so that the proportions do not break when the camera angle
changes.
"""
import bpy
import math

# ---------------------------------------------------------------------------
# The palette. docs/10-cuisine-identity.md fast food: warm, saturated, plastic.
# ---------------------------------------------------------------------------
PALETTE = {
    "wood":         (0.42, 0.26, 0.15),
    "wood_dark":    (0.30, 0.18, 0.10),
    "cloth":        (0.86, 0.82, 0.70),   # dirty beige: so a white plate reads on it
    "plate":        (0.95, 0.95, 0.93),
    "metal":        (0.62, 0.64, 0.66),
    "metal_dark":   (0.34, 0.36, 0.38),
    "plastic_red":    (0.72, 0.18, 0.14),
    "plastic_yellow": (0.90, 0.68, 0.12),
    "wall":         (0.88, 0.86, 0.80),
    "floor":        (0.55, 0.50, 0.45),
    "black":        (0.10, 0.10, 0.11),
    "glass":        (0.70, 0.82, 0.85),
}

_materials = {}


def clear_scene():
    """Empty the scene. No dirt should carry over between scripts."""
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for block in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras,
                  bpy.data.lights):
        for b in list(block):
            if b.users == 0:
                block.remove(b)
    _materials.clear()


def mat(name, rough=0.7, metallic=0.0):
    """A flat-colour material from the palette. Asking for the same name a
    second time reuses it."""
    if name in _materials:
        return _materials[name]

    rgb = PALETTE.get(name, (0.8, 0.8, 0.8))
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (rgb[0], rgb[1], rgb[2], 1.0)
    bsdf.inputs["Roughness"].default_value = rough
    if "Metallic" in bsdf.inputs:
        bsdf.inputs["Metallic"].default_value = metallic
    _materials[name] = m
    return m


def _finish(obj, material, bevel, smooth_angle=30.0):
    if bevel > 0:
        m = obj.modifiers.new("bevel", "BEVEL")
        m.width = bevel
        m.segments = 1          # low-poly: one segment is enough, the triangle
                                # budget matters
        m.limit_method = "ANGLE"
        m.angle_limit = math.radians(40)
    obj.data.materials.append(material)
    bpy.ops.object.shade_smooth_by_angle(angle=math.radians(smooth_angle))
    return obj


def box(name, size, loc, material_name, bevel=0.010, rot=None):
    """A box centred on loc. size = (x, y, z), the full dimensions."""
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    o = bpy.context.active_object
    o.name = name
    o.scale = size
    if rot:
        o.rotation_euler = rot
    bpy.ops.object.transform_apply(location=False, rotation=bool(rot), scale=True)
    return _finish(o, mat(material_name), bevel)


def cylinder(name, radius, height, loc, material_name, verts=12, bevel=0.006):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=radius,
                                        depth=height, location=loc)
    o = bpy.context.active_object
    o.name = name
    return _finish(o, mat(material_name), bevel)


def plane(name, size, loc, material_name):
    bpy.ops.mesh.primitive_plane_add(size=1, location=loc)
    o = bpy.context.active_object
    o.name = name
    o.scale = (size[0], size[1], 1.0)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    o.data.materials.append(mat(material_name))
    return o


def tri_count(objects):
    """The evaluated triangle count (with the modifiers applied)."""
    dg = bpy.context.evaluated_depsgraph_get()
    total = 0
    for o in objects:
        if o.type != "MESH":
            continue
        me = o.evaluated_get(dg).to_mesh()
        total += sum(max(0, len(p.vertices) - 2) for p in me.polygons)
    return total
