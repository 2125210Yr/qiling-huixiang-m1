"""Pose math tests. No bpy.

Run:
  python -m pytest tests/test_pose_math.py -q
  python tests/test_pose_math.py
"""
from __future__ import annotations

import os
import sys
import unittest

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
if ROOT not in sys.path:
    sys.path.insert(0, ROOT)

from parts.pose_math import (
    BREATH_ACTION,
    HAIR_ACTION,
    PELVIS_ABOVE_STOOL,
    SIT_ACTION,
    STOOL_TOP_Z,
    action_transform_changed,
    bone_names,
    box_is_near_hand,
    sit_pelvis_world,
)


class PoseMathTests(unittest.TestCase):
    def test_sit_pelvis_world_z_uses_stool_constants(self):
        self.assertEqual(
            sit_pelvis_world()[2],
            STOOL_TOP_Z + PELVIS_ABOVE_STOOL,
        )

    def test_box_is_near_hand(self):
        self.assertTrue(box_is_near_hand())

    def test_breath_chest_changes_between_1_and_24(self):
        self.assertTrue(action_transform_changed(BREATH_ACTION, "chest", 1, 24))

    def test_sit_chest_does_not_change_between_1_and_24(self):
        self.assertFalse(action_transform_changed(SIT_ACTION, "chest", 1, 24))

    def test_hair_l_changes_between_1_and_17(self):
        self.assertTrue(action_transform_changed(HAIR_ACTION, "hair_L", 1, 17))

    def test_bone_names_include_core_bones(self):
        names = bone_names()
        for name in ("pelvis", "chest", "hand_R", "hair_L"):
            self.assertIn(name, names)


if __name__ == "__main__":
    unittest.main()
