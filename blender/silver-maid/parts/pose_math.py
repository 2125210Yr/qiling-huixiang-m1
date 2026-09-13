"""Pure pose math for the silver-maid sit rig. No bpy.

Armature object lives at pelvis world. Bone heads/tails are armature-local.
Sit rest IS the still: identity bone transforms. Extra motion is breath + hair sway.
"""
from __future__ import annotations

import math
from typing import Dict, Iterable, List, Sequence, Tuple

Vec3 = Tuple[float, float, float]

STOOL_TOP_Z = 0.72
PELVIS_ABOVE_STOOL = 0.02
PELVIS_Y = 0.02
ROOT_LOCATION: Vec3 = (0.0, 0.0, STOOL_TOP_Z)

SIT_ACTION = "SitStill"
BREATH_ACTION = "IdleBreath"
HAIR_ACTION = "HairSway"

BREATH_PERIOD = 48
BREATH_AMP = 0.06  # chest X rotation radians
HAIR_PERIOD = 64
HAIR_AMP = 0.10

# Right hand (character +X) holds the box in the still.
HAND_R_WORLD: Vec3 = (0.16, -0.24, 0.92)
BOX_HOLD_WORLD: Vec3 = (0.14, -0.22, 0.94)
BOX_HAND_MAX_DIST = 0.25


def sit_pelvis_world(stool_top_z: float = STOOL_TOP_Z) -> Vec3:
    return (0.0, PELVIS_Y, stool_top_z + PELVIS_ABOVE_STOOL)


def armature_location() -> Vec3:
    return sit_pelvis_world()


def box_hold_world() -> Vec3:
    return BOX_HOLD_WORLD


def hand_r_world() -> Vec3:
    return HAND_R_WORLD


def dist(a: Sequence[float], b: Sequence[float]) -> float:
    return math.sqrt(sum((float(a[i]) - float(b[i])) ** 2 for i in range(3)))


def box_is_near_hand(box: Sequence[float] | None = None, hand: Sequence[float] | None = None) -> bool:
    return dist(box or box_hold_world(), hand or hand_r_world()) <= BOX_HAND_MAX_DIST


# name, parent or None, head, tail  (armature local, Z up)
BONE_DEFS: List[Tuple[str, str | None, Vec3, Vec3]] = [
    ("pelvis", None, (0.0, 0.0, 0.0), (0.0, 0.0, 0.08)),
    ("spine", "pelvis", (0.0, 0.0, 0.08), (0.0, 0.01, 0.22)),
    ("chest", "spine", (0.0, 0.01, 0.22), (0.0, -0.01, 0.38)),
    ("neck", "chest", (0.0, -0.01, 0.38), (0.0, -0.02, 0.52)),
    ("head", "neck", (0.0, -0.02, 0.52), (0.01, -0.03, 0.70)),
    ("shoulder_L", "chest", (-0.08, -0.01, 0.36), (-0.155, -0.02, 0.40)),
    ("upper_arm_L", "shoulder_L", (-0.155, -0.02, 0.40), (-0.38, -0.16, 0.32)),
    ("forearm_L", "upper_arm_L", (-0.38, -0.16, 0.32), (-0.52, -0.32, 0.28)),
    ("hand_L", "forearm_L", (-0.52, -0.32, 0.28), (-0.55, -0.36, 0.27)),
    ("shoulder_R", "chest", (0.08, -0.01, 0.36), (0.155, -0.02, 0.40)),
    ("upper_arm_R", "shoulder_R", (0.155, -0.02, 0.40), (0.28, -0.14, 0.30)),
    ("forearm_R", "upper_arm_R", (0.28, -0.14, 0.30), (0.18, -0.22, 0.22)),
    ("hand_R", "forearm_R", (0.18, -0.22, 0.22), (0.16, -0.24, 0.20)),
    ("thigh_L", "pelvis", (-0.07, 0.02, 0.00), (-0.08, -0.12, -0.38)),
    ("calf_L", "thigh_L", (-0.08, -0.12, -0.38), (-0.06, -0.10, -0.66)),
    ("foot_L", "calf_L", (-0.06, -0.10, -0.66), (-0.05, -0.12, -0.78)),
    ("thigh_R", "pelvis", (0.07, 0.02, 0.00), (0.09, -0.18, -0.36)),
    ("calf_R", "thigh_R", (0.09, -0.18, -0.36), (0.08, -0.14, -0.62)),
    ("foot_R", "calf_R", (0.08, -0.14, -0.62), (0.09, -0.26, -0.72)),
    ("hair_root", "head", (0.01, -0.03, 0.70), (0.02, 0.04, 0.88)),
    ("hair_L", "hair_root", (0.02, 0.04, 0.88), (-0.40, -0.25, 0.25)),
    ("hair_R", "hair_root", (0.02, 0.04, 0.88), (0.28, -0.22, 0.45)),
]


def bone_names() -> List[str]:
    return [row[0] for row in BONE_DEFS]


def sit_bone_euler(name: str) -> Vec3:
    """Sit rest: identity. Mesh is already in the still pose."""
    _ = name
    return (0.0, 0.0, 0.0)


def breath_chest_x(frame: int, period: int = BREATH_PERIOD, amp: float = BREATH_AMP) -> float:
    t = 2.0 * math.pi * (int(frame) - 1) / float(period)
    return amp * math.sin(t)


def hair_sway_z(frame: int, side: str, period: int = HAIR_PERIOD, amp: float = HAIR_AMP) -> float:
    sign = -1.0 if side.upper().startswith("L") else 1.0
    t = 2.0 * math.pi * (int(frame) - 1) / float(period)
    return sign * amp * math.sin(t)


def sample_action(action: str, bone: str, frame: int) -> Dict[str, float]:
    """Euler XYZ plus location. Sit is rest. Breath/HairSway add timed deltas."""
    ex, ey, ez = sit_bone_euler(bone)
    lx = ly = lz = 0.0
    if action == BREATH_ACTION and bone == "chest":
        ex = breath_chest_x(frame)
        lz = 0.008 * math.sin(2.0 * math.pi * (int(frame) - 1) / float(BREATH_PERIOD))
    elif action == HAIR_ACTION and bone in ("hair_L", "hair_R"):
        ez = hair_sway_z(frame, "L" if bone.endswith("L") else "R")
    elif action == SIT_ACTION:
        pass
    return {"x": ex, "y": ey, "z": ez, "lx": lx, "ly": ly, "lz": lz}


def action_transform_changed(action: str, bone: str, frame_a: int, frame_b: int, eps: float = 1e-6) -> bool:
    a = sample_action(action, bone, frame_a)
    b = sample_action(action, bone, frame_b)
    return any(abs(a[k] - b[k]) > eps for k in a)


def sit_keyframes(bones: Iterable[str] | None = None) -> Dict[str, Dict[int, Dict[str, float]]]:
    names = list(bones) if bones is not None else bone_names()
    return {name: {1: sample_action(SIT_ACTION, name, 1)} for name in names}


def breath_keyframes() -> Dict[int, Dict[str, float]]:
    return {frame: sample_action(BREATH_ACTION, "chest", frame) for frame in (1, 13, 25, 37, 49)}


def hair_keyframes(bone: str) -> Dict[int, Dict[str, float]]:
    return {frame: sample_action(HAIR_ACTION, bone, frame) for frame in (1, 17, 33, 49, 65)}
