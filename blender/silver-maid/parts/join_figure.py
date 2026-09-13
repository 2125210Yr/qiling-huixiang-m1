"""Join body/clothes into maid_body and maid_clothes. No armature."""

import bmesh
from mathutils import Vector

_BODY = frozenset((
    "hips", "torso", "chest", "neck", "head", "arm", "hand", "thigh", "calf", "foot",
    "eye", "iris", "pupil", "lid", "lidlow", "brow", "nose", "lip", "mouth", "ear",
    "teeth", "tooth", "eyehilight", "face", "finger", "palm", "toe",
))
_CLOTH = frozenset((
    "collar", "bodice", "skirt", "belt", "sleeve", "sleeves", "tights",
    "puff", "hem", "strap", "straps", "buckle", "buckles", "ruffle", "cuff", "apron",
))
_KEEP = frozenset(("hair", "headset", "box", "stool", "heel", "rifle", "bang", "bangs"))
_JOINTS = (
    ("shoulder_L", 0.050, "maid_chest", "maid_arm_upper_L", (-0.155, -0.02, 0.40)),
    ("shoulder_R", 0.050, "maid_chest", "maid_arm_upper_R", (0.155, -0.02, 0.40)),
    ("elbow_L", 0.040, "maid_arm_upper_L", "maid_arm_forearm_L", (-0.38, -0.16, 0.32)),
    ("elbow_R", 0.040, "maid_arm_upper_R", "maid_arm_forearm_R", (0.28, -0.14, 0.30)),
    ("wrist_L", 0.028, "maid_arm_forearm_L", "maid_hand_L", (-0.52, -0.32, 0.28)),
    ("wrist_R", 0.028, "maid_arm_forearm_R", "maid_hand_R", (0.18, -0.22, 0.22)),
    ("hip_L", 0.082, "maid_hips", "maid_thigh_L", (-0.07, 0.02, 0.00)),
    ("hip_R", 0.082, "maid_hips", "maid_thigh_R", (0.07, 0.02, 0.00)),
    ("knee_L", 0.056, "maid_thigh_L", "maid_calf_L", (-0.08, -0.12, -0.38)),
    ("knee_R", 0.056, "maid_thigh_R", "maid_calf_R", (0.09, -0.18, -0.36)),
    ("ankle_L", 0.036, "maid_calf_L", "maid_foot_L", (-0.06, -0.10, -0.66)),
    ("ankle_R", 0.036, "maid_calf_R", "maid_foot_R", (0.08, -0.14, -0.62)),
    ("waist", 0.095, "maid_hips", "maid_torso", (0.0, 0.015, 0.10)),
    ("sternum", 0.100, "maid_torso", "maid_chest", (0.0, -0.005, 0.29)),
    ("throat", 0.046, "maid_chest", "maid_neck", (0.0, -0.008, 0.455)),
    ("atlas", 0.040, "maid_neck", "maid_head", (0.0, -0.02, 0.575)),
)


def _alive(ob):
    try:
        return ob is not None and ob.name is not None
    except (ReferenceError, AttributeError):
        return False


def _mesh(ob):
    return _alive(ob) and getattr(ob, "type", "") == "MESH" and getattr(ob, "data", None)


def _kind(name):
    if not name or not name.startswith("maid_"):
        return "other"
    if name in ("maid_body", "maid_clothes"):
        return name
    toks = set(name.lower().replace("-", "_").split("_"))
    if toks & _KEEP:
        return "keep"
    if toks & _CLOTH:
        return "clothes"
    return "body" if toks & _BODY else "keep"


def _mode(bpy):
    try:
        if bpy.context.object is not None and bpy.context.object.mode != "OBJECT":
            bpy.ops.object.mode_set(mode="OBJECT")
    except Exception:
        pass


def _smooth(ob):
    try:
        if getattr(ob, "data", None) is not None:
            ob.data.shade_smooth()
    except Exception:
        pass


def _anchors(ob):
    pts = [ob.matrix_world.translation.copy()]
    verts = getattr(ob.data, "vertices", None)
    if verts:
        zs = [v.co.z for v in verts]
        mw = ob.matrix_world
        pts += [mw @ Vector((0.0, 0.0, min(zs))), mw @ Vector((0.0, 0.0, max(zs)))]
    return pts


def _joint_local(root, objs, a, b, fallback):
    oa, ob = objs.get(a), objs.get(b)
    if not (_mesh(oa) and _mesh(ob)):
        return Vector(fallback)
    pa, pb = _anchors(oa), _anchors(ob)
    p, q = min(((x, y) for x in pa for y in pb), key=lambda t: (t[0] - t[1]).length)
    try:
        return root.matrix_world.inverted() @ ((p + q) * 0.5)
    except Exception:
        return Vector(fallback)


def _ico(bpy, name, loc, radius, parent, mat):
    me = bpy.data.meshes.new(name)
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=2, radius=float(radius))
    bm.to_mesh(me)
    bm.free()
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    ob.parent, ob.location = parent, Vector(loc)
    if mat is not None:
        me.materials.append(mat)
    _smooth(ob)
    return ob


def _apply_solidify(bpy, ob):
    names = [m.name for m in list(ob.modifiers) if m.type == "SOLIDIFY"]
    if not names:
        return
    _mode(bpy)
    try:
        bpy.context.view_layer.objects.active = ob
        ob.select_set(True)
        for n in names:
            with bpy.context.temp_override(object=ob, active_object=ob):
                bpy.ops.object.modifier_apply(modifier=n)
    except Exception:
        pass


def _join(bpy, meshes, name, root):
    seen, uniq = set(), []
    for ob in meshes:
        if _mesh(ob) and ob.name not in seen:
            seen.add(ob.name)
            uniq.append(ob)
    if not uniq:
        return None
    _mode(bpy)
    vl = bpy.context.view_layer
    for ob in list(vl.objects):
        try:
            ob.select_set(False)
        except Exception:
            pass
    active = uniq[0]
    for ob in uniq:
        try:
            ob.hide_set(False)
            ob.hide_viewport = False
            ob.hide_select = False
            ob.select_set(True)
        except Exception:
            pass
    vl.objects.active = active
    if len(uniq) > 1:
        kw = dict(
            scene=bpy.context.scene, view_layer=vl, active_object=active, object=active,
            selected_objects=uniq, selected_editable_objects=uniq,
        )
        try:
            with bpy.context.temp_override(**kw):
                bpy.ops.object.join()
        except Exception:
            try:
                bpy.ops.object.join()
            except Exception:
                pass
    joined = vl.objects.active if _mesh(vl.objects.active) else (active if _mesh(active) else None)
    if not _mesh(joined):
        return None
    try:
        joined.name = name
        joined.data.name = name
        joined.parent = root
    except Exception:
        pass
    _smooth(joined)
    try:
        if not any(m.type == "SUBSURF" for m in joined.modifiers):
            md = joined.modifiers.new("Subsurf", "SUBSURF")
            md.levels, md.render_levels = 2, 3
    except Exception:
        pass
    return joined


def _catalog(bpy, built):
    found = {}
    for key, ob in list((built or {}).items()):
        if _alive(ob) and ob.name != "MaidRoot":
            found[ob.name] = ob
        elif isinstance(key, str) and key in bpy.data.objects and key != "MaidRoot":
            found[key] = bpy.data.objects[key]
    for ob in bpy.data.objects:
        if ob.name.startswith("maid_") and ob.type in {"MESH", "CURVE"}:
            found.setdefault(ob.name, ob)
    return found


def join_figure(bpy, root, built):
    """Join body parts into maid_body with overlapping joint connectors so limbs are not floating islands.
    Join dress/clothes meshes into maid_clothes.
    Return dict with maid_body, maid_clothes, plus remaining hair/accessory objects.
    Parent results to root. Do not delete MaidRoot. Do not read_homefile.
    """
    body, clothes, rest = [], [], {}
    joined_body = joined_clothes = None
    for name, ob in _catalog(bpy, built).items():
        kind = _kind(name)
        if kind == "maid_body":
            joined_body = ob
        elif kind == "maid_clothes":
            joined_clothes = ob
        elif kind == "body" and _mesh(ob):
            body.append(ob)
        elif kind == "clothes" and _mesh(ob):
            clothes.append(ob)
        elif _alive(ob):
            rest[name] = ob
    prefer = {o.name: o for o in body}
    body.sort(key=lambda o: 0 if "hips" in o.name else 1)
    clothes.sort(key=lambda o: 0 if "bodice" in o.name else 1)
    if joined_body is None and body:
        mat = next((s for o in body for s in o.data.materials if s), None) or bpy.data.materials.get("maid_skin")
        for tag, rad, a, b, fb in _JOINTS:
            if _mesh(prefer.get(a)) or _mesh(prefer.get(b)):
                try:
                    body.append(_ico(bpy, "maid_joint_" + tag, _joint_local(root, prefer, a, b, fb), rad, root, mat))
                except Exception:
                    pass
        joined_body = _join(bpy, body, "maid_body", root)
    if joined_clothes is None and clothes:
        for ob in clothes:
            _apply_solidify(bpy, ob)
        joined_clothes = _join(bpy, clothes, "maid_clothes", root)
    out = {}
    for key, ob in (("maid_body", joined_body), ("maid_clothes", joined_clothes)):
        if _mesh(ob):
            ob.parent = root
            out[key] = ob
    for ob in rest.values():
        if _alive(ob) and ob.name not in {"maid_body", "maid_clothes", "MaidRoot"}:
            try:
                ob.parent = root
            except Exception:
                pass
            out[ob.name] = ob
    return out
