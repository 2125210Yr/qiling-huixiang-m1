"""Emit a Cubism-ingestible physics3.json from the authored spec.

What this can and cannot generate, stated up front:

  CAN   physics3.json  (text, Cubism reads it)
        pose3.json     (text)
        model3.json    (text, runtime reference)
        motion3.json   (text, per-keyframe motion)
  CANNOT  .cmo3 / .moc3 -- the authoring project and its compiled model are
        closed binaries.  The deformer hierarchy, ArtMesh placement and
        parameter keyforms have to be built in Cubism Editor by hand; nothing
        outside Cubism writes them.

So the deliverable here is everything *around* the .cmo3: the physics the
Editor would otherwise make you hand-configure, the pose groups, and a gate
that refuses to emit a file with a dangling parameter id.

    python build.py elevator-red                 # dry run, writes to out/
    python build.py elevator-red --write         # also overwrite the model's file
"""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from spec import Rig

LAB = Path(__file__).resolve().parent.parent


def rebuild_meta(settings: list[dict], fps: int, forces: dict,
                 dictionary: list[dict]) -> dict:
    """Recompute every count from the settings themselves.

    Hand-maintained counts are exactly the kind of thing that silently goes
    stale when a group is added, and Cubism trusts them.
    """
    return {
        "PhysicsSettingCount": len(settings),
        "TotalInputCount": sum(len(s.get("Input", [])) for s in settings),
        "TotalOutputCount": sum(len(s.get("Output", [])) for s in settings),
        "VertexCount": sum(len(s.get("Vertices", [])) for s in settings),
        "Fps": fps,
        "EffectiveForces": forces,
        "PhysicsDictionary": dictionary,
    }


def gate(rig: Rig) -> list[str]:
    """Refuse to emit a file Cubism would silently half-ignore."""
    bad = []
    for g in rig.physics:
        for i in g.input_ids():
            if i not in rig.params:
                bad.append("%s: input %s is not a parameter" % (g.name, i))
        for o in g.outputs:
            if o["Destination"]["Id"] not in rig.params:
                bad.append("%s: output %s is not a parameter"
                           % (g.name, o["Destination"]["Id"]))
        n = len(g.vertices)
        for o in g.outputs:
            if not (0 <= int(o.get("VertexIndex", 0)) < n):
                bad.append("%s: VertexIndex %s out of range (%d vertices)"
                           % (g.name, o.get("VertexIndex"), n))
    return bad


def build(name: str, keep_comments: bool, write: bool) -> int:
    rig = Rig.load(LAB / name, name)
    if not rig.physics:
        print("no physics spec found for %s" % name)
        return 1

    problems = gate(rig)
    if problems:
        print("REFUSING to emit: %d problems" % len(problems))
        for p in problems:
            print("  -", p)
        return 2

    settings = []
    for g in rig.physics:
        s = {
            "Id": g.id,
            "Input": g.inputs,
            "Output": g.outputs,
            "Vertices": g.vertices,
            "Normalization": {"Position": g.norm_position, "Angle": g.norm_angle},
        }
        # Cubism Editor does not write Comment into physics3.json; keep the
        # design notes in a sidecar instead of risking an unknown field.
        if keep_comments and g.comment:
            s["Comment"] = g.comment
        settings.append(s)

    meta_src = rig.physics_raw.get("Meta", {})
    out = {
        "Version": rig.physics_raw.get("Version", 3),
        "Meta": rebuild_meta(
            settings,
            meta_src.get("Fps", 30),
            meta_src.get("EffectiveForces", {"Gravity": {"X": 0, "Y": -1},
                                             "Wind": {"X": 0, "Y": 0}}),
            meta_src.get("PhysicsDictionary",
                         [{"Id": g.id, "Name": g.name} for g in rig.physics]),
        ),
        "PhysicsSettings": settings,
    }

    dst = LAB / "_rigtools" / "out" / ("%s.physics3.json" % name.replace("-", "_"))
    dst.parent.mkdir(parents=True, exist_ok=True)
    dst.write_text(json.dumps(out, ensure_ascii=False, indent=2), encoding="utf-8")

    m = out["Meta"]
    print("wrote %s" % dst)
    print("  settings %d   inputs %d   outputs %d   vertices %d   fps %s"
          % (m["PhysicsSettingCount"], m["TotalInputCount"], m["TotalOutputCount"],
             m["VertexCount"], m["Fps"]))

    notes = LAB / "_rigtools" / "out" / ("%s.physics-notes.md" % name)
    with notes.open("w", encoding="utf-8") as fh:
        fh.write("# %s physics notes\n\n" % name)
        fh.write("Generated from `rig/physics_groups.json`. Cubism does not\n")
        fh.write("carry per-setting comments, so they live here.\n\n")
        for g in rig.physics:
            fh.write("## %s  (%s)\n\n" % (g.name, g.id))
            if g.comment:
                fh.write("%s\n\n" % g.comment)
            fh.write("- in : %s\n" % ", ".join(g.input_ids()))
            fh.write("- out: %s\n" % ", ".join(g.output_ids()))
            fh.write("- vertices: %d\n\n" % len(g.vertices))
    print("wrote %s" % notes)

    target = LAB / name / ("%s.physics3.json" % name.replace("-", "_"))
    if write:
        if target.exists():
            backup = target.with_suffix(".json.bak")
            backup.write_text(target.read_text(encoding="utf-8"), encoding="utf-8")
            print("backed up existing -> %s" % backup.name)
        target.write_text(json.dumps(out, ensure_ascii=False, indent=2),
                          encoding="utf-8")
        print("OVERWROTE %s" % target)
    else:
        print("\n(target is %s; pass --write to replace it)" % target)
    return 0


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("name", nargs="?", default="elevator-red")
    ap.add_argument("--write", action="store_true",
                    help="overwrite the model's physics3.json (backs it up first)")
    ap.add_argument("--keep-comments", action="store_true",
                    help="also inline Comment into the emitted json")
    a = ap.parse_args()
    return build(a.name, a.keep_comments, a.write)


if __name__ == "__main__":
    raise SystemExit(main())
