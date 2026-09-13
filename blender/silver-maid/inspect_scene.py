"""Headless scene dump.

  blender --background --python inspect_scene.py

After assemble: loads out/maid.blend.
Second phase: if MaidRoot / an armature is already in memory, inspect that scene.
Always writes out/scene-inspect.txt and exits 0.
"""
from __future__ import annotations

import os
import sys
import traceback

ROOT = os.path.dirname(os.path.abspath(__file__))
if ROOT not in sys.path:
    sys.path.insert(0, ROOT)

OUT_DIR = os.path.join(ROOT, "out")
BLEND_PATH = os.path.join(OUT_DIR, "maid.blend")
REPORT_PATH = os.path.join(OUT_DIR, "scene-inspect.txt")

_LINES: list[str] = []


def log(msg: object = "") -> None:
    text = str(msg)
    _LINES.append(text)
    print(text)


def _missing(what: str) -> None:
    log(f"MISSING {what}")


def _fmt_vec(v) -> str:
    try:
        return "({:.6f}, {:.6f}, {:.6f})".format(float(v[0]), float(v[1]), float(v[2]))
    except Exception:
        return repr(v)


def _write_report() -> None:
    os.makedirs(OUT_DIR, exist_ok=True)
    with open(REPORT_PATH, "w", encoding="utf-8") as fh:
        fh.write("\n".join(_LINES) + "\n")
    log(f"WROTE {REPORT_PATH}")


def _scene_already_assembled(bpy) -> bool:
    if bpy.data.objects.get("MaidRoot") is not None:
        return True
    return any(ob.type == "ARMATURE" for ob in bpy.data.objects)


def _ensure_scene(bpy) -> None:
    if _scene_already_assembled(bpy):
        log("PHASE: in-memory scene (second phase / already assembled)")
        log(f"FILE: {bpy.data.filepath or '(unsaved)'}")
        return
    if os.path.isfile(BLEND_PATH):
        log(f"LOAD {BLEND_PATH}")
        bpy.ops.wm.open_mainfile(filepath=BLEND_PATH)
        log(f"FILE: {bpy.data.filepath or BLEND_PATH}")
        return
    _missing(f"out/maid.blend ({BLEND_PATH})")
    log("PHASE: current blender scene (no blend to load)")


def _armature_objects(bpy):
    return [ob for ob in bpy.data.objects if ob.type == "ARMATURE"]


def _pick_armature(bpy):
    arms = _armature_objects(bpy)
    if not arms:
        return None
    for name in ("maid_armature", "Armature", "maid_rig"):
        hit = bpy.data.objects.get(name)
        if hit is not None and hit.type == "ARMATURE":
            return hit
    return arms[0]


def _action_by_name(bpy, name: str):
    act = bpy.data.actions.get(name)
    if act is not None:
        return act
    lower = name.lower()
    for act in bpy.data.actions:
        if act.name.lower() == lower:
            return act
    return None


def _box_object(bpy):
    hit = bpy.data.objects.get("maid_box")
    if hit is not None:
        return hit
    for ob in bpy.data.objects:
        n = ob.name.lower()
        if "box" in n and ob.type in {"MESH", "EMPTY", "CURVE"}:
            return ob
    return None


def _assign_action(obj, action) -> None:
    adt = obj.animation_data_create()
    adt.action = action
    slots = getattr(action, "slots", None)
    if not slots or not hasattr(adt, "action_slot"):
        return
    if getattr(adt, "action_slot", None) is not None:
        return
    try:
        adt.action_slot = slots[0]
    except Exception:
        pass


def _update(bpy, frame: int) -> None:
    scene = bpy.context.scene
    scene.frame_set(int(frame))
    bpy.context.view_layer.update()
    dg = bpy.context.evaluated_depsgraph_get()
    if dg is not None:
        dg.update()


def _pose_bone(arm, name: str):
    if arm is None:
        return None
    pb = arm.pose.bones.get(name)
    if pb is not None:
        return pb
    lower = name.lower()
    for bone in arm.pose.bones:
        if bone.name.lower() == lower:
            return bone
    return None


def _bone_world(arm, pb):
    return (arm.matrix_world @ pb.matrix).translation.copy()


def _sample_pose(bpy, arm, action, bone_name: str, frame: int):
    if arm is None:
        return None, "no armature object"
    if action is None:
        return None, f"action for bone {bone_name}"
    pb = _pose_bone(arm, bone_name)
    if pb is None:
        return None, f"bone {bone_name} on {arm.name}"
    _assign_action(arm, action)
    _update(bpy, frame)
    pb = _pose_bone(arm, bone_name)
    world = _bone_world(arm, pb)
    euler = pb.rotation_euler.copy()
    mat_eul = (arm.matrix_world @ pb.matrix).to_euler("XYZ")
    return {
        "bone": pb.name,
        "world": world,
        "rotation_euler": euler,
        "matrix_euler": mat_eul,
        "location": pb.location.copy(),
    }, None


def inspect() -> None:
    import bpy

    from parts.pose_math import (
        BOX_HAND_MAX_DIST,
        dist,
        box_hold_world,
        hand_r_world,
        sit_pelvis_world,
    )

    log("=== silver-maid scene inspect ===")
    _ensure_scene(bpy)

    arms = _armature_objects(bpy)
    if arms:
        log("ARMATURES: " + ", ".join(ob.name for ob in arms))
        for ob in arms:
            data = ob.data
            bones = list(data.bones) if data is not None else []
            if bones:
                log(f"BONES ({ob.name}): " + ", ".join(b.name for b in bones))
            else:
                _missing(f"bones on armature {ob.name}")
    else:
        _missing("armature objects")
        _missing("bone names (no armature)")

    bound = []
    for ob in bpy.data.objects:
        for mod in getattr(ob, "modifiers", []):
            if mod.type == "ARMATURE":
                target = getattr(mod, "object", None)
                tname = target.name if target is not None else "(none)"
                bound.append(f"{ob.name} (mod={mod.name}, object={tname})")
    if bound:
        log("OBJECTS_WITH_ARMATURE_MOD: " + "; ".join(bound))
    else:
        _missing("objects with armature modifiers")

    actions = list(bpy.data.actions)
    if actions:
        log("ACTIONS: " + ", ".join(a.name for a in actions))
    else:
        _missing("actions")

    sit = _action_by_name(bpy, "SitStill")
    breath = _action_by_name(bpy, "IdleBreath")
    hair = _action_by_name(bpy, "HairSway")
    if sit is None:
        _missing("action SitStill")
    if breath is None:
        _missing("action IdleBreath")
    if hair is None:
        _missing("action HairSway")

    arm = _pick_armature(bpy)
    if arm is None:
        _missing("armature to sample poses")
    else:
        log(f"SAMPLE_ARMATURE: {arm.name}")

    sit_sample, err = _sample_pose(bpy, arm, sit, "pelvis", 1)
    if sit_sample is None:
        _missing(f"SitStill pelvis world frame 1 ({err})")
    else:
        log(f"SitStill pelvis world frame 1: {_fmt_vec(sit_sample['world'])}")

    b1, err1 = _sample_pose(bpy, arm, breath, "chest", 1)
    b24, err24 = _sample_pose(bpy, arm, breath, "chest", 24)
    if b1 is None:
        _missing(f"IdleBreath chest rotation_euler.x frame 1 ({err1})")
    else:
        log(f"IdleBreath chest rotation_euler.x frame 1: {float(b1['rotation_euler'].x):.6f}")
        log(f"IdleBreath chest matrix_euler.x frame 1: {float(b1['matrix_euler'].x):.6f}")
    if b24 is None:
        _missing(f"IdleBreath chest rotation_euler.x frame 24 ({err24})")
    else:
        log(f"IdleBreath chest rotation_euler.x frame 24: {float(b24['rotation_euler'].x):.6f}")
        log(f"IdleBreath chest matrix_euler.x frame 24: {float(b24['matrix_euler'].x):.6f}")

    h1, herr1 = _sample_pose(bpy, arm, hair, "hair_L", 1)
    h17, herr17 = _sample_pose(bpy, arm, hair, "hair_L", 17)
    if h1 is None:
        _missing(f"HairSway hair_L z frame 1 ({herr1})")
    else:
        log(f"HairSway hair_L rotation_euler.z frame 1: {float(h1['rotation_euler'].z):.6f}")
        log(f"HairSway hair_L world.z frame 1: {float(h1['world'].z):.6f}")
    if h17 is None:
        _missing(f"HairSway hair_L z frame 17 ({herr17})")
    else:
        log(f"HairSway hair_L rotation_euler.z frame 17: {float(h17['rotation_euler'].z):.6f}")
        log(f"HairSway hair_L world.z frame 17: {float(h17['world'].z):.6f}")

    box = _box_object(bpy)
    if box is None:
        _missing("box object (maid_box)")
        box_world = None
    else:
        bpy.context.view_layer.update()
        box_world = box.matrix_world.translation.copy()
        log(f"BOX object: {box.name} world {_fmt_vec(box_world)}")

    hand = None
    hand_err = "no armature"
    if arm is not None:
        # Prefer evaluated pose; SitStill if present, else current pose.
        hs, herr = _sample_pose(bpy, arm, sit or breath or hair, "hand_R", 1)
        if hs is None:
            pb = _pose_bone(arm, "hand_R")
            if pb is None:
                hand_err = herr or "bone hand_R"
            else:
                _update(bpy, 1)
                hand = _bone_world(arm, pb)
        else:
            hand = hs["world"]
            hand_err = None
    if hand is None:
        _missing(f"hand_R bone world ({hand_err})")
    else:
        log(f"hand_R bone world: {_fmt_vec(hand)}")

    if box_world is not None and hand is not None:
        scene_dist = dist(box_world, hand)
        log(f"SCENE box-hand distance: {scene_dist:.6f} (max {BOX_HAND_MAX_DIST})")
    else:
        _missing("scene box-hand distance (need box object and hand_R bone)")

    pelvis = sit_pelvis_world()
    math_box = box_hold_world()
    math_hand = hand_r_world()
    math_dist = dist(math_box, math_hand)
    log(f"MATH sit_pelvis_world: {_fmt_vec(pelvis)}")
    log(f"MATH box_hold_world: {_fmt_vec(math_box)}")
    log(f"MATH hand_r_world: {_fmt_vec(math_hand)}")
    log(f"MATH box-hand distance: {math_dist:.6f} (max {BOX_HAND_MAX_DIST})")


def main() -> int:
    try:
        inspect()
    except Exception as exc:
        log(f"ERROR {type(exc).__name__}: {exc}")
        log(traceback.format_exc())
    finally:
        try:
            _write_report()
        except Exception as exc:
            print(f"MISSING report write: {exc}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
