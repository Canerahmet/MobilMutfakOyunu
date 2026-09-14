# -*- coding: utf-8 -*-
"""
Insansi iskelet ve poz yardimcilari.
============================================================================
docs/24-sanat-hatti.md kademe 2.

Degerlendirmenin matematigi 16 kiyafet x 3 vucut tipi = 96 mesh diyordu.
Bu sayi yok oluyor:
  vucut tipi   tek mesh, uc KEMIK OLCEGI on ayari
  kiyafet      skinsiz ayri mesh, kemige bagli; agirlik boyama YOK
  renk         malzeme degisimi

Kemik adlari Unity insansi eslemesine uygun. Quaternius Universal
Animation Library klipleri bu yapiya yeniden hedeflenebiliyor.
"""
import bpy
import math
from mathutils import Vector

# ---------------------------------------------------------------------------
# Kemik iskeleti: (ad, bas, kuyruk, ebeveyn)
# Olculer metre, T-poz. Boy yaklasik 1,75 m.
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


def build_armature(name="Iskelet"):
    """T-poz insansi iskelet kurar ve nesneyi doner."""
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
# Vucut tipleri: MESH DEGIL, kemik olcegi.
# Uc on ayar, tek mesh. docs/24: 96 mesh sorununu yok eden sey bu.
# ---------------------------------------------------------------------------
BODY_TYPES = {
    "ince":   {"chest": 0.86, "hips": 0.90, "upperarm": 0.88, "thigh": 0.92},
    "orta":   {"chest": 1.00, "hips": 1.00, "upperarm": 1.00, "thigh": 1.00},
    "genis":  {"chest": 1.22, "hips": 1.16, "upperarm": 1.14, "thigh": 1.10},
}


def apply_body_type(arm_obj, kind):
    """
    Kemik olcegiyle vucut tipi uygular. Mesh cogalmiyor.
    Unity tarafinda ayni sey Avatar'in kemik olcekleriyle yapilacak.
    """
    preset = BODY_TYPES.get(kind, BODY_TYPES["orta"])
    bpy.context.view_layer.objects.active = arm_obj
    bpy.ops.object.mode_set(mode="POSE")

    for pb in arm_obj.pose.bones:
        base = pb.name.split(".")[0]
        s = preset.get(base)
        if s is None:
            continue
        # Kemigin ekseni boyunca degil, KALINLIK eksenlerinde olcekle:
        # uzunluk degisirse iskelet oranlari bozulur ve klip yeniden
        # hedeflemesi kayar.
        pb.scale = (s, 1.0, s)

    bpy.ops.object.mode_set(mode="OBJECT")


# ---------------------------------------------------------------------------
# Test pozlari. Agirlik cokusunu YAKALAMAK icin secildiler.
# docs/24: dirsek ve diz cokusunu render'da ben goruyorum.
# ---------------------------------------------------------------------------
def _rot(pb, x=0.0, y=0.0, z=0.0):
    pb.rotation_mode = "XYZ"
    pb.rotation_euler = (math.radians(x), math.radians(y), math.radians(z))


# Eksen notu, iki turda olculdu:
#   1. tur  Z etrafinda donduruldu -> kollar asagi inmedi, one arkaya salindi
#   2. tur  X etrafinda POZITIF -> kollar YUKARI kalkti
#   3. tur  X etrafinda NEGATIF -> dogru
# Kemigin yerel Y ekseni uzunlugu boyunca; yana uzanan kolu asagi indirmek
# yerel X etrafinda NEGATIF donmek demek.
POSES = {
    # T-poz: taban.
    "t_poz": {},

    # Kollar asagi, hafif ice.
    "dinlenme": {
        "upperarm.L": (-78, 0, 0), "upperarm.R": (-78, 0, 0),
        "forearm.L": (-12, 0, 0), "forearm.R": (-12, 0, 0),
    },

    # Dirsek bukumu: onde tepsi tasiyor.
    "tasima": {
        "upperarm.L": (-62, 0, 0), "upperarm.R": (-62, 0, 0),
        # Dirsek bukumu de yerel X: kol asagi dondukten sonra on kolun
        # cercevesi onu takip ediyor, Z ekseni dirsegi burarak yaniltiyordu.
        "forearm.L": (-85, 0, 0), "forearm.R": (-85, 0, 0),
    },

    # Oturma: kalca ve diz. Govde ayrica asagi aliniyor (bkz. SIT_DROP).
    "oturma": {
        "thigh.L": (-88, 0, 0), "thigh.R": (-88, 0, 0),
        "shin.L": (86, 0, 0), "shin.R": (86, 0, 0),
        "upperarm.L": (-70, 0, 0), "upperarm.R": (-70, 0, 0),
        "forearm.L": (-35, 0, 0), "forearm.R": (-35, 0, 0),
    },

    # Egilme: bel ve kalca.
    "egilme": {
        "spine": (34, 0, 0), "chest": (20, 0, 0),
        "upperarm.L": (-85, 0, 0), "upperarm.R": (-85, 0, 0),
        "thigh.L": (-14, 0, 0), "thigh.R": (-14, 0, 0),
    },

    # Yuruyus ortasi: bacaklar zit yonde, kollar karsi salinim.
    "yuruyus": {
        "thigh.L": (-28, 0, 0), "thigh.R": (24, 0, 0),
        "shin.L": (18, 0, 0), "shin.R": (10, 0, 0),
        "upperarm.L": (-66, 0, 0), "upperarm.R": (-88, 0, 0),
        "forearm.L": (-22, 0, 0), "forearm.R": (-10, 0, 0),
    },
}

# Oturma pozunda kalca asagi inmeli, yoksa karakter havada duruyor.
SIT_DROP = {"oturma": -0.42}


def apply_pose(arm_obj, pose_name):
    """Pozu uygular. Onceki poz temizlenir, vucut tipi olcegi korunur."""
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

    # Oturmada govde asagi iniyor. Kemik yerel Y'si uzunlugu boyunca
    # oldugu icin kalcanin yerel ekseninde tasiniyor.
    drop = SIT_DROP.get(pose_name)
    if drop:
        hips = arm_obj.pose.bones.get("hips")
        if hips is not None:
            hips.location = (0.0, drop, 0.0)

    bpy.ops.object.mode_set(mode="OBJECT")


def bind_rigid(obj, arm_obj, bone_name):
    """
    Parcayi kemige KATI baglar. Skinning yok, agirlik boyama yok.

    Neden: otomatik agirlik denendi ve eklemde bosluk acti (oturma
    pozunda bacaklar kalcadan ayrildi). Duzeltmenin yolu mesh'i
    kaynaklayip eklem bolgesine kenar dongusu eklemek ve agirlik
    boyamakti; o is gorsel yargi istiyor ve docs/24 o yargiyi verecek
    kimse olmadigini tespit etmisti.

    Ters matris ELLE hesaplanmiyor. Ilk denemede hesaplandi ve parcalar
    sahneye dagildi: Blender'da kemik ebeveynligi kemigin KUYRUGUNU
    baslangic aliyor, basini degil. Dogru yol, dunya donusumunu once
    saklayip ebeveynlikten sonra geri yazmak; yerel matrisi Blender
    kendisi cikariyor.
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
    Kiyafet, sac ve aksesuar. Govde parcalariyla AYNI mekanizma:
    katı kemik ebeveynligi. 16 kiyafet, sifir ek skinning.
    """
    return bind_rigid(obj, arm_obj, bone_name)
