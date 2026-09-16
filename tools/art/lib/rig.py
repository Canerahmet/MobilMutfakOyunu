# -*- coding: utf-8 -*-
"""
Humanoid skeleton and pose helpers.
============================================================================
docs/24-art-pipeline.md tier 2.

The review's arithmetic said 16 outfits x 3 body types = 96 meshes. That
number disappears:
  body type   one mesh, three BONE SCALE presets
  outfit      a separate skinless mesh, parented to a bone; NO weight painting
  colour      a material swap

The bone names follow Unity's humanoid mapping. Quaternius Universal
Animation Library clips can be retargeted onto this rig.
"""
import bpy
import math
from mathutils import Vector

# ---------------------------------------------------------------------------
# The bone skeleton: (name, head, tail, parent)
# Measurements in metres, T-pose. The height is about 1.75 m.
# ---------------------------------------------------------------------------
BONES = [
    ("hips",        (0.00, 0.00, 0.95), (0.00, 0.00, 1.06), None),
    ("spine",       (0.00, 0.00, 1.06), (0.00, 0.00, 1.22), "hips"),
    ("chest",       (0.00, 0.00, 1.22), (0.00, 0.00, 1.42), "spine"),
    ("neck",        (0.00, 0.00, 1.42), (0.00, 0.00, 1.52), "chest"),
    ("head",        (0.00, 0.00, 1.52), (0.00, 0.00, 1.75), "neck"),

    ("shoulder.L",  (0.03, 0.00, 1.38), (0.17, 0.00, 1.40), "chest"),
    ("upperarm.L",  (0.17, 0.00, 1.40), (0.42, 0.00, 1.40), "shoulder.L"),
    ("forearm.L",   (0.42, 0.00, 1.40), (0.66, 0.00, 1.40), "upperarm.L"),
    ("hand.L",      (0.66, 0.00, 1.40), (0.78, 0.00, 1.40), "forearm.L"),

    ("shoulder.R",  (-0.03, 0.00, 1.38), (-0.17, 0.00, 1.40), "chest"),
    ("upperarm.R",  (-0.17, 0.00, 1.40), (-0.42, 0.00, 1.40), "shoulder.R"),
    ("forearm.R",   (-0.42, 0.00, 1.40), (-0.66, 0.00, 1.40), "upperarm.R"),
    ("hand.R",      (-0.66, 0.00, 1.40), (-0.78, 0.00, 1.40), "forearm.R"),

    ("thigh.L",     (0.11, 0.00, 0.95), (0.11, 0.00, 0.54), "hips"),
    ("shin.L",      (0.11, 0.00, 0.54), (0.11, 0.00, 0.10), "thigh.L"),
    ("foot.L",      (0.11, 0.00, 0.10), (0.11, -0.16, 0.03), "shin.L"),

    ("thigh.R",     (-0.11, 0.00, 0.95), (-0.11, 0.00, 0.54), "hips"),
    ("shin.R",      (-0.11, 0.00, 0.54), (-0.11, 0.00, 0.10), "thigh.R"),
    ("foot.R",      (-0.11, 0.00, 0.10), (-0.11, -0.16, 0.03), "shin.R"),
]


def build_armature(name="Skeleton"):
    """Builds a T-pose humanoid skeleton and returns the object."""
    arm_data = bpy.data.armatures.new(name)
    arm_obj = bpy.data.objects.new(name, arm_data)
    bpy.context.collection.objects.link(arm_obj)

    bpy.context.view_layer.objects.active = arm_obj
    bpy.ops.object.mode_set(mode="EDIT")

    made = {}
    for bname, head, tail, parent in BONES:
        b = arm_data.edit_bones.new(bname)
        b.head = Vector(head)
        b.tail = Vector(tail)
        if parent:
            b.parent = made[parent]
            b.use_connect = (Vector(head) - made[parent].tail).length < 0.001
        made[bname] = b

    bpy.ops.object.mode_set(mode="OBJECT")
    return arm_obj


# ---------------------------------------------------------------------------
# Body types: NOT MESHES, bone scales.
# Three presets, one mesh. docs/24: this is what makes the 96-mesh problem
# disappear.
#
# The preset keys are the artefact ids ("slim", "mid", "broad"); they become
# the render file names under tools/art/out/, so they are left as they are.
# ---------------------------------------------------------------------------
BODY_TYPES = {
    "slim":   {"chest": 0.86, "hips": 0.90, "upperarm": 0.88, "thigh": 0.92},
    "mid":   {"chest": 1.00, "hips": 1.00, "upperarm": 1.00, "thigh": 1.00},
    "broad":  {"chest": 1.22, "hips": 1.16, "upperarm": 1.14, "thigh": 1.10},
}


def apply_body_type(arm_obj, kind):
    """
    Applies a body type through bone scaling. The mesh does not multiply.
    On the Unity side the same thing will be done with the Avatar's bone
    scales.
    """
    preset = BODY_TYPES.get(kind, BODY_TYPES["mid"])
    bpy.context.view_layer.objects.active = arm_obj
    bpy.ops.object.mode_set(mode="POSE")

    for pb in arm_obj.pose.bones:
        base = pb.name.split(".")[0]
        s = preset.get(base)
        if s is None:
            continue
        # Scale on the THICKNESS axes, not along the bone's own axis: if the
        # length changes the skeleton's proportions break and clip retargeting
        # goes out of alignment.
        pb.scale = (s, 1.0, s)

    bpy.ops.object.mode_set(mode="OBJECT")


# ---------------------------------------------------------------------------
# Test poses. They were chosen to CATCH the weighting collapse.
# docs/24: I look at the elbow and knee collapse in the render.
#
# The pose keys are artefact ids as well - they become the render file names -
# so they are left as they are.
# ---------------------------------------------------------------------------
def _rot(pb, x=0.0, y=0.0, z=0.0):
    pb.rotation_mode = "XYZ"
    pb.rotation_euler = (math.radians(x), math.radians(y), math.radians(z))


# A note on the axes, measured over three rounds:
#   round 1  rotated about Z -> the arms did not come down, they swung
#            forwards and backwards
#   round 2  POSITIVE about X -> the arms went UP
#   round 3  NEGATIVE about X -> correct
# A bone's local Y axis runs along its length; to bring an outstretched arm
# down means rotating NEGATIVELY about the local X.
POSES = {
    # T-pose: the baseline.
    "t_pose": {},

    # Arms down, turned slightly inwards.
    "idle": {
        "upperarm.L": (-78, 0, 0), "upperarm.R": (-78, 0, 0),
        "forearm.L": (-12, 0, 0), "forearm.R": (-12, 0, 0),
    },

    # Elbow bend: carrying a tray in front.
    "carry": {
        "upperarm.L": (-62, 0, 0), "upperarm.R": (-62, 0, 0),
        # The elbow bend is on the local X too: once the arm has rotated down
        # the forearm's frame follows it, and the Z axis was misleading by
        # twisting the elbow.
        "forearm.L": (-85, 0, 0), "forearm.R": (-85, 0, 0),
    },

    # Sitting: hips and knees. The body is also lowered (see SIT_DROP).
    "sit": {
        "thigh.L": (-88, 0, 0), "thigh.R": (-88, 0, 0),
        "shin.L": (86, 0, 0), "shin.R": (86, 0, 0),
        "upperarm.L": (-70, 0, 0), "upperarm.R": (-70, 0, 0),
        "forearm.L": (-35, 0, 0), "forearm.R": (-35, 0, 0),
    },

    # Bending: waist and hips.
    "bend": {
        "spine": (34, 0, 0), "chest": (20, 0, 0),
        "upperarm.L": (-85, 0, 0), "upperarm.R": (-85, 0, 0),
        "thigh.L": (-14, 0, 0), "thigh.R": (-14, 0, 0),
    },

    # Mid-stride: the legs in opposition, the arms in counter-swing.
    "walk": {
        "thigh.L": (-28, 0, 0), "thigh.R": (24, 0, 0),
        "shin.L": (18, 0, 0), "shin.R": (10, 0, 0),
        "upperarm.L": (-66, 0, 0), "upperarm.R": (-88, 0, 0),
        "forearm.L": (-22, 0, 0), "forearm.R": (-10, 0, 0),
    },
}

# In the sitting pose the hips have to come down, otherwise the character
# hangs in the air.
SIT_DROP = {"sit": -0.42}


def apply_pose(arm_obj, pose_name):
    """Applies the pose. The previous pose is cleared, the body-type scale is
    preserved."""
    bpy.context.view_layer.objects.active = arm_obj
    bpy.ops.object.mode_set(mode="POSE")

    pose = POSES.get(pose_name, {})

    for pb in arm_obj.pose.bones:
        base_scale = tuple(pb.scale)
        _rot(pb, 0, 0, 0)
        pb.location = (0.0, 0.0, 0.0)
        pb.scale = base_scale

    for bone_name, angles in pose.items():
        pb = arm_obj.pose.bones.get(bone_name)
        if pb is None:
            continue
        base_scale = tuple(pb.scale)
        _rot(pb, *angles)
        pb.scale = base_scale

    # In the sitting pose the body comes down. Because a bone's local Y runs
    # along its length, the translation is on the hips' local axis.
    drop = SIT_DROP.get(pose_name)
    if drop:
        hips = arm_obj.pose.bones.get("hips")
        if hips is not None:
            hips.location = (0.0, drop, 0.0)

    bpy.ops.object.mode_set(mode="OBJECT")


def bind_rigid(obj, arm_obj, bone_name):
    """
    Binds a part RIGIDLY to a bone. No skinning, no weight painting.

    Why: automatic weights were tried and opened a gap at the joint (in the
    sitting pose the legs came away from the hips). The way to fix that was to
    weld the mesh, add an edge loop around the joint and paint weights; that
    work needs visual judgement, and docs/24 had established that there was
    nobody to give that judgement.

    The inverse matrix is NOT computed BY HAND. It was computed on the first
    attempt and the parts scattered across the scene: in Blender, bone
    parenting takes the bone's TAIL as its origin, not its head. The correct
    way is to store the world transform first and write it back after
    parenting; Blender derives the local matrix itself.
    """
    bpy.context.view_layer.update()
    world = obj.matrix_world.copy()

    obj.parent = arm_obj
    obj.parent_type = "BONE"
    obj.parent_bone = bone_name

    bpy.context.view_layer.update()
    obj.matrix_world = world
    return obj


def attach_to_bone(obj, arm_obj, bone_name):
    """
    Outfit, hair and accessories. THE SAME mechanism as the body parts: rigid
    bone parenting. 16 outfits, zero extra skinning.
    """
    return bind_rigid(obj, arm_obj, bone_name)
