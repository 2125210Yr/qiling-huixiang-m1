"""Headless assemble: mesh → join → rig → actions → save → render."""
import os
import sys

ROOT = os.path.dirname(os.path.abspath(__file__))
if ROOT not in sys.path:
    sys.path.insert(0, ROOT)

import bpy

from parts.body import build as build_body
from parts.face import build as build_face
from parts.hair import build as build_hair
from parts.dress import build as build_dress
from parts.accessories import build as build_accessories
from parts.join_figure import join_figure
from parts.rig import build_rig
from parts.actions import build_actions
from parts.look import setup as setup_look, render_views


def _clear():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for block in (bpy.data.meshes, bpy.data.curves, bpy.data.materials, bpy.data.cameras, bpy.data.lights, bpy.data.armatures, bpy.data.actions):
        for item in list(block):
            try:
                block.remove(item)
            except Exception:
                pass


def main():
    _clear()
    bpy.ops.object.empty_add(type="PLAIN_AXES", location=(0.0, 0.0, 0.72))
    root = bpy.context.active_object
    root.name = "MaidRoot"
    built = {}
    for fn in (build_body, build_face, build_hair, build_dress, build_accessories):
        built.update(fn(bpy, root) or {})
    built = join_figure(bpy, root, built)
    rigged = build_rig(bpy, root, built)
    arm = rigged.get("maid_armature")
    acts = {}
    if arm is not None:
        acts = build_actions(bpy, arm)
    setup_look(bpy, root, built)
    os.makedirs(os.path.join(ROOT, "out"), exist_ok=True)
    blend = os.path.join(ROOT, "out", "maid.blend")
    bpy.ops.wm.save_as_mainfile(filepath=blend)
    render_views(bpy, os.path.join(ROOT, "out"))
    print("SAVED", blend)
    print("OBJECTS", sorted(o.name for o in bpy.data.objects))
    print("ACTIONS", sorted(acts.keys()))
    print("ARMATURE", arm.name if arm else "MISSING")


if __name__ == "__main__":
    main()
