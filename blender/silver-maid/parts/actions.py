"""Sit / breath / hair actions. Values come from pose_math only. No homefile."""
from __future__ import annotations

from .pose_math import (
    BREATH_ACTION,
    HAIR_ACTION,
    SIT_ACTION,
    breath_keyframes,
    hair_keyframes,
    sample_action,
    sit_keyframes,
)


def _use_action(armature_obj, action):
    anim = armature_obj.animation_data_create()
    anim.action = action
    if getattr(anim, "action_slot", None) is None and hasattr(action, "slots"):
        slots = getattr(anim, "action_suitable_slots", None)
        anim.action_slot = (
            slots[0] if slots else action.slots.new(id_type="OBJECT", name=armature_obj.name)
        )
    return anim


def _get_action(bpy, name):
    action = bpy.data.actions.get(name) or bpy.data.actions.new(name)
    action.name = name
    action.use_fake_user = True
    return action


def _set_euler(bone, sample):
    bone.rotation_mode = "XYZ"
    bone.rotation_euler = (sample["x"], sample["y"], sample["z"])


def build_actions(bpy, armature_obj):
    """Create three actions on armature_obj:
    - pose_math.SIT_ACTION: frame 1 identity eulers for all pose bones (still sit)
    - pose_math.BREATH_ACTION: chest bone rotation_euler.x and location.z from breath_keyframes()
    - pose_math.HAIR_ACTION: hair_L and hair_R rotation_euler.z from hair_keyframes()
    Assign sit as the armature's animation_data.action.
    Frame range 1-65, fps 24.
    Return dict of action name -> action.
    """
    scene = bpy.context.scene
    scene.frame_start = 1
    scene.frame_end = 65
    scene.render.fps = 24

    pose = armature_obj.pose.bones
    names = [bone.name for bone in pose]
    out = {}

    sit = _get_action(bpy, SIT_ACTION)
    _use_action(armature_obj, sit)
    for name, frames in sit_keyframes(names).items():
        bone = pose[name]
        for frame in frames:
            _set_euler(bone, sample_action(SIT_ACTION, name, frame))
            bone.keyframe_insert(data_path="rotation_euler", frame=frame)
    out[SIT_ACTION] = sit

    breath = _get_action(bpy, BREATH_ACTION)
    _use_action(armature_obj, breath)
    chest = pose["chest"]
    for frame in breath_keyframes():
        sample = sample_action(BREATH_ACTION, "chest", frame)
        _set_euler(chest, sample)
        chest.location = (sample["lx"], sample["ly"], sample["lz"])
        chest.keyframe_insert(data_path="rotation_euler", frame=frame)
        chest.keyframe_insert(data_path="location", frame=frame)
    out[BREATH_ACTION] = breath

    hair = _get_action(bpy, HAIR_ACTION)
    _use_action(armature_obj, hair)
    for name in ("hair_L", "hair_R"):
        bone = pose[name]
        for frame in hair_keyframes(name):
            _set_euler(bone, sample_action(HAIR_ACTION, name, frame))
            bone.keyframe_insert(data_path="rotation_euler", frame=frame)
    out[HAIR_ACTION] = hair

    _use_action(armature_obj, sit)
    scene.frame_set(1)
    return out
