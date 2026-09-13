"""Show what the model's current physics3.json is missing vs the authored spec."""
from __future__ import annotations

import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from spec import Rig

LAB = Path(__file__).resolve().parent.parent
name = sys.argv[1] if len(sys.argv) > 1 else "elevator-red"
rig = Rig.load(LAB / name, name)

built = rig.physics_built_src
if built is None:
    print("no built physics3.json found")
    raise SystemExit(1)
b = json.loads(built.read_text(encoding="utf-8"))

spec_names = {g.name for g in rig.physics}
built_groups = b.get("PhysicsSettings", [])
built_names = {s.get("Id") for s in built_groups}

print("=" * 74)
print("built %s" % built.name)
print("  %d groups, Meta: %s" % (len(built_groups), json.dumps(b.get("Meta", {}), ensure_ascii=False)))
print("spec rig/physics_groups.json")
print("  %d groups, Meta.PhysicsSettingCount=%d"
      % (len(rig.physics), rig.physics_raw["Meta"]["PhysicsSettingCount"]))
print("=" * 74)

print("\ngroups in the spec but NOT in the built file:")
for g in rig.physics:
    if g.id not in built_names:
        print("   %-22s %-18s %d out" % (g.id, g.name, len(g.outputs)))

print("\nparameter ids the built file addresses that do not exist in parameters.md:")
dangling = set()
for s in built_groups:
    for i in s.get("Input", []):
        pid = i["Source"]["Id"]
        if pid not in rig.params:
            dangling.add(("input", pid))
    for o in s.get("Output", []):
        pid = o["Destination"]["Id"]
        if pid not in rig.params:
            dangling.add(("output", pid))
if dangling:
    for kind, pid in sorted(dangling):
        print("   %-7s %s   <- Cubism will ignore this group silently" % (kind, pid))
else:
    print("   none")

print("\nMeta fields the built file is missing:")
meta = b.get("Meta", {})
for k in ("EffectiveForces", "PhysicsDictionary"):
    if k not in meta:
        print("   %s   (Cubism physics needs EffectiveForces; without it the"
              "\n              chain has no gravity and just floats)" % k)
