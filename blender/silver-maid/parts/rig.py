"""Sit armature. Bone coords and pelvis world come from pose_math only."""
from __future__ import annotations

from mathutils import Vector

from parts.pose_math import BONE_DEFS, armature_location, sit_bone_euler, sit_pelvis_world


def _get(built, bpy, name):
    ob = (built or {}).get(name)
    if ob is not None:
        return ob
    return bpy.data.objects.get(name)


def _iter_obs(built, bpy):
    seen = set()
    for src in ((built or {}).values(), bpy.data.objects):
        for ob in src:
            if ob is None:
                continue
            key = id(ob)
            if key in seen:
                continue
            seen.add(key)
            yield ob


def _object_mode(bpy):
    obj = getattr(bpy.context, "object", None)
    if obj is not None and getattr(obj, "mode", "OBJECT") != "OBJECT":
        bpy.ops.object.mode_set(mode="OBJECT")


def _mode(bpy, mode, ob):
    bpy.context.view_layer.objects.active = ob
    ob.select_set(True)
    if ob.mode != mode:
        bpy.ops.object.mode_set(mode=mode)


def _keep_world(bpy, ob, apply):
    mw = ob.matrix_world.copy()
    apply()
    bpy.context.view_layer.update()
    ob.matrix_world = mw


def _deselect(bpy):
    try:
        bpy.ops.object.select_all(action="DESELECT")
    except Exception:
        for ob in bpy.context.view_layer.objects:
            ob.select_set(False)


def _has_armature_mod(ob):
    return any(m.type == "ARMATURE" for m in getattr(ob, "modifiers", []))


def _hair_bone(name):
    n = name.lower()
    if "bangs" in n or "bang" in n:
        return "head"
    if n.endswith("_l") or "hair_l" in n or "mass_l" in n:
        return "hair_L"
    if n.endswith("_r") or "hair_r" in n or "mass_r" in n:
        return "hair_R"
    return "hair_root"


def _parent_bone(bpy, ob, arm, bone):
    if ob is None or arm.pose.bones.get(bone) is None:
        return
    def _set():
        ob.parent = arm
        ob.parent_type = "BONE"
        ob.parent_bone = bone
    try:
        _keep_world(bpy, ob, _set)
    except Exception:
        mw = ob.matrix_world.copy()
        ob.parent = None
        ob.matrix_world = mw
        con = next((c for c in ob.constraints if c.type == "CHILD_OF" and c.subtarget == bone), None)
        if con is None:
            con = ob.constraints.new("CHILD_OF")
        con.target = arm
        con.subtarget = bone
        bpy.context.view_layer.update()
        pb = arm.pose.bones[bone]
        con.inverse_matrix = (arm.matrix_world @ pb.matrix).inverted() @ ob.matrix_world


def _bind_mesh(bpy, ob, arm):
    if ob is None or getattr(ob, "type", None) != "MESH":
        return
    from mathutils import Vector
    mw = ob.matrix_world.copy()
    ob.parent = None
    ob.matrix_world = mw
    bpy.context.view_layer.update()
    arm_inv = arm.matrix_world.inverted()

    def seg_dist(p, a, b):
        p, a, b = Vector(p), Vector(a), Vector(b)
        ab = b - a
        denom = ab.length_squared
        if denom < 1e-12:
            return (p - a).length
        t = max(0.0, min(1.0, (p - a).dot(ab) / denom))
        return (p - (a + ab * t)).length

    groups = {}
    for name, _p, _h, _t in BONE_DEFS:
        groups[name] = ob.vertex_groups.get(name) or ob.vertex_groups.new(name=name)
    for v in ob.data.vertices:
        co = arm_inv @ (ob.matrix_world @ v.co)
        best, best_d = "pelvis", 1e9
        for name, _p, head, tail in BONE_DEFS:
            d = seg_dist(co, head, tail)
            if d < best_d:
                best, best_d = name, d
        groups[best].add([v.index], 1.0 / (1.0 + best_d * 8.0), "REPLACE")
    if not _has_armature_mod(ob):
        mod = ob.modifiers.new("Armature", "ARMATURE")
        mod.object = arm
        mod.use_vertex_groups = True
        mod.use_bone_envelopes = False
    else:
        for mod in ob.modifiers:
            if mod.type == "ARMATURE":
                mod.object = arm


def _parent_to_root(bpy, ob, root):
    if ob is None or root is None:
        return
    for mod in list(getattr(ob, "modifiers", [])):
        if mod.type == "ARMATURE":
            ob.modifiers.remove(mod)

    def _set():
        ob.parent = root
        ob.parent_type = "OBJECT"
        if hasattr(ob, "parent_bone"):
            ob.parent_bone = ""

    _keep_world(bpy, ob, _set)


def build_rig(bpy, root, built):
    """Create armature maid_armature at pose_math.armature_location() (parent to root).
    Edit-bones from pose_math.BONE_DEFS (name, parent, head, tail).
    Bind maid_body and maid_clothes (if present) with ARMATURE deform (automatic weights if possible, else envelope).
    Parent hair objects (name contains hair or bangs) to hair_root/hair_L/hair_R/head bones via bone parent or child-of.
    Parent maid_box to hand_R bone.
    Parent stool to root (not armature deform).
    Set pose bones to sit identity (pose_math.sit_bone_euler).
    Return dict armature=..., plus bound objects.
    """
    built = built or {}
    _object_mode(bpy)

    arm_data = bpy.data.armatures.new("maid_armature")
    arm = bpy.data.objects.new("maid_armature", arm_data)
    arm.name = "maid_armature"
    arm_data.name = "maid_armature"
    bpy.context.scene.collection.objects.link(arm)

    # Object lives at pelvis world; as a root child that is local offset (meshes are root-local).
    world = Vector(armature_location())
    pelvis = Vector(sit_pelvis_world())
    arm.parent = root
    if root is not None:
        arm.location = world - Vector(root.location)
    else:
        arm.location = pelvis
    arm.rotation_euler = (0.0, 0.0, 0.0)

    _mode(bpy, "EDIT", arm)
    ebs = arm.data.edit_bones
    created = {}
    for name, parent, head, tail in BONE_DEFS:
        eb = ebs.new(name)
        eb.head = head
        eb.tail = tail
        eb.use_connect = False
        eb.use_deform = True
        eb.envelope_distance = 0.14
        eb.head_radius = 0.045
        eb.tail_radius = 0.035
        created[name] = eb
        if parent and parent in created:
            eb.parent = created[parent]

    _mode(bpy, "OBJECT", arm)

    body = _get(built, bpy, "maid_body")
    clothes = _get(built, bpy, "maid_clothes")
    _bind_mesh(bpy, body, arm)
    _bind_mesh(bpy, clothes, arm)

    _mode(bpy, "POSE", arm)
    for pb in arm.pose.bones:
        pb.rotation_mode = "XYZ"
        pb.rotation_euler = sit_bone_euler(pb.name)
        pb.location = (0.0, 0.0, 0.0)
    _mode(bpy, "OBJECT", arm)

    out = {"armature": arm, "maid_armature": arm}
    if body is not None:
        out["maid_body"] = body
    if clothes is not None:
        out["maid_clothes"] = clothes

    for ob in _iter_obs(built, bpy):
        name = getattr(ob, "name", "") or ""
        n = name.lower()
        if ob is arm or ob.type == "ARMATURE":
            continue
        if "hair" in n or "bangs" in n:
            _parent_bone(bpy, ob, arm, _hair_bone(name))
            out[name] = ob

    box = _get(built, bpy, "maid_box")
    if box is None:
        box = next((ob for ob in _iter_obs(built, bpy) if "maid_box" in (ob.name or "").lower()), None)
    if box is not None:
        _parent_bone(bpy, box, arm, "hand_R")
        out["maid_box"] = box

    stool = _get(built, bpy, "maid_stool")
    if stool is None:
        stool = next((ob for ob in _iter_obs(built, bpy) if "stool" in (ob.name or "").lower()), None)
    if stool is not None:
        _parent_to_root(bpy, stool, root)
        out["maid_stool"] = stool

    return out
