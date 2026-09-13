"""Cross-check the four rig files and report every disagreement.

Nothing here rewrites anything.  The point is to catch the class of mistake that
is silent inside Cubism: a physics group addressing a parameter that does not
exist, a pose3 group whose Part was never cut, a layer with no pivot, a physics
output that a hand-keyed clip also writes.

    python validate.py elevator-red
"""
from __future__ import annotations

import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from spec import Rig, layer_to_part

# ±1-range inputs that must not share a wide Normalization.Position with
# head/body (±30 / ±10).  FINDINGS.md option (a): they live on *_unit settings.
UNIT_INPUT_IDS = {"ParamElevatorAccel", "ParamBreath", "ParamWeightShift"}
UNIT_POS_SPAN = 2.0  # -1 .. 1

LAB = Path(__file__).resolve().parent.parent

OK, WARN, ERR = "OK", "WARN", "ERROR"


class Report:
    def __init__(self) -> None:
        self.rows: list[tuple[str, str, str]] = []

    def add(self, level: str, where: str, msg: str) -> None:
        self.rows.append((level, where, msg))

    def count(self, level: str) -> int:
        return sum(1 for r in self.rows if r[0] == level)

    def dump(self) -> None:
        sym = {OK: "  ok ", WARN: " warn", ERR: " ERR "}
        for level, where, msg in self.rows:
            print("%s %-34s %s" % (sym[level], where, msg))
        print()
        print("  %d ok, %d warn, %d error"
              % (self.count(OK), self.count(WARN), self.count(ERR)))


def _span_split_pair(writers: list[str]) -> bool:
    """True when the only writers are a parent setting and its *_unit companion."""
    if len(writers) != 2:
        return False
    bases = {w[:-5] if w.endswith("_unit") else w for w in writers}
    return len(bases) == 1 and any(w.endswith("_unit") for w in writers)


def check_parameter_coverage(rig: Rig, rep: Report) -> None:
    """Every parameter in the table should be driven by something, and every
    physics output should be a real parameter."""
    seen: dict[str, list[str]] = {}
    for g in rig.physics:
        for oid in g.output_ids():
            seen.setdefault(oid, []).append(g.name)
    driven = set(seen)

    for p in rig.params.values():
        if p.drivable_by_physics:
            if p.id not in driven:
                rep.add(ERR, p.id, "marked 物理输出 in parameters.md but no "
                                   "physics group writes it (line %d)" % p.line)
            elif _span_split_pair(seen[p.id]):
                rep.add(OK, p.id, "physics-driven, span-split pair (%s)"
                        % ", ".join(seen[p.id]))
            else:
                rep.add(OK, p.id, "physics-driven, one writer")
        elif p.id not in driven:
            rep.add(OK, p.id, "not physics-driven (%s)" % (p.driver or "-"))

    # two unrelated groups writing one param is a real Cubism conflict;
    # a parent + *_unit pair is the intended option-(a) split
    for oid, writers in sorted(seen.items()):
        if len(writers) > 1 and not _span_split_pair(writers):
            rep.add(ERR, oid, "written by %d physics groups: %s"
                    % (len(writers), ", ".join(writers)))


def check_physics_ids(rig: Rig, rep: Report) -> None:
    unknown_in, unknown_out = set(), set()
    for g in rig.physics:
        for i in g.input_ids():
            if i not in rig.params:
                unknown_in.add((g.name, i))
        for o in g.output_ids():
            if o not in rig.params:
                unknown_out.add((g.name, o))
    for name, i in sorted(unknown_in):
        rep.add(ERR, name, "input parameter %s is not in parameters.md" % i)
    for name, o in sorted(unknown_out):
        rep.add(ERR, name, "output parameter %s is not in parameters.md" % o)
    if not unknown_in and not unknown_out:
        rep.add(OK, "physics ids", "all %d input / %d output ids resolve"
                % (sum(len(g.inputs) for g in rig.physics),
                   sum(len(g.outputs) for g in rig.physics)))


def check_physics_ordering(rig: Rig, rep: Report) -> None:
    """A group that consumes another group's output must run after it."""
    producer: dict[str, str] = {}
    for g in rig.physics:
        for oid in g.output_ids():
            producer.setdefault(oid, g.name)
    problems = 0
    for g in rig.physics:
        for iid in g.input_ids():
            src = producer.get(iid)
            if src is None:
                continue
            if src == g.name:
                continue
            upstream = next(x for x in rig.physics if x.name == src)
            if upstream.order > g.order:
                problems += 1
                rep.add(ERR, g.name, "reads %s from %s but runs BEFORE it "
                                     "(order %d < %d)"
                        % (iid, src, g.order, upstream.order))
    if problems == 0:
        rep.add(OK, "physics order", "every cross-group dependency is ordered")


def check_unit_spans(rig: Rig, rep: Report) -> None:
    """±1 inputs must sit on a setting whose Position span is ±1.

    Sharing a span-20 Position with ParamAngleX (±30) makes accel ~30x inert;
    Cubism Weight is 0-100 and cannot recover that.  See FINDINGS.md option (a).
    """
    mixed = 0
    unit_ok = 0
    for g in rig.physics:
        npos = g.norm_position or {"Minimum": -10, "Default": 0, "Maximum": 10}
        span = float(npos["Maximum"] - npos["Minimum"])
        unit_here = [i for i in g.input_ids() if i in UNIT_INPUT_IDS]
        if not unit_here:
            if g.name.endswith("_unit"):
                rep.add(WARN, g.name, "_unit setting has no ±1 inputs")
            continue
        if span > UNIT_POS_SPAN + 0.01:
            mixed += 1
            rep.add(ERR, g.name,
                    "%s in Position span %g (inert; belongs on a ±1 setting)"
                    % (", ".join(unit_here), span))
        else:
            unit_ok += 1
            rep.add(OK, g.name,
                    "±1 inputs %s on Position span %g"
                    % (", ".join(unit_here), span))
    if unit_ok and not mixed:
        rep.add(OK, "unit span",
                "%d companion setting(s) match ±1 Position" % unit_ok)


def check_vertex_ranges(rig: Rig, rep: Report) -> None:
    """VertexIndex must address a vertex that exists."""
    bad = 0
    for g in rig.physics:
        n = len(g.vertices)
        for o in g.outputs:
            vi = o.get("VertexIndex", 0)
            if not (0 <= vi < n):
                bad += 1
                rep.add(ERR, g.name, "output %s uses VertexIndex %d but the "
                                     "chain has %d vertices"
                        % (o["Destination"]["Id"], vi, n))
    if bad == 0:
        rep.add(OK, "vertex index", "all outputs address an existing vertex")


def check_normalization(rig: Rig, rep: Report) -> None:
    """Compare each group's output gain: Normalization.Angle span x Scale.

    Cubism's own editor default for Normalization.Angle is +-10 regardless of
    the parameter's range, so a wide span here is NOT by itself a defect -- the
    parameter's own range clamps the result.  What is worth reading is the
    *relative* gain between groups, because that is what decides which part
    leads and which trails.  Reported as information, not as a verdict.
    """
    rows = []
    for g in rig.physics:
        na, npos = g.norm_angle, g.norm_position
        if not na:
            continue
        aspan = na["Maximum"] - na["Minimum"]
        pspan = ((npos["Maximum"] - npos["Minimum"]) if npos else 0.0)
        for o in g.outputs:
            rows.append((g.name, o["Destination"]["Id"], aspan,
                         o.get("Scale", 1.0), o.get("Type", "Angle"),
                         aspan * o.get("Scale", 1.0), pspan))
    if not rows:
        return
    rep.add(OK, "normalization", "%d outputs; gain = AngleSpan x Scale" % len(rows))
    rows.sort(key=lambda r: -r[5])
    for name, pid, aspan, sc, typ, gain, pspan in rows:
        rep.add(WARN if gain > 40 else OK, name,
                "%-22s Angle +-%g x Scale %-4g = gain %-6.1f  (input span %g, %s)"
                % (pid, aspan / 2, sc, gain, pspan, typ))
    print()
    print("  gain ranking (what leads vs trails): ")
    for name, pid, aspan, sc, typ, gain, pspan in rows:
        print("    %-22s %-10s %6.1f" % (pid, name, gain))


# Layers that ride fully on a parent and never need their own pivot.
FACE_PARTS = {"brow_l", "brow_r", "eyewhite_l", "eyewhite_r", "iris_l", "iris_r",
              "lash_l", "lash_r", "nose", "mouth_upper", "mouth_lower",
              "mouth_inside", "face"}


def check_cut_plan(rig: Rig, rep: Report) -> None:
    layers = rig.cut_plan.get("layers", [])
    pivots = rig.cut_plan.get("pivots", {})
    if not layers:
        rep.add(WARN, "psd_cut_plan", "no layers listed")
        return
    rep.add(OK, "cut plan", "%d layers, %d pivots, %d underpaint notes"
            % (len(layers), len(pivots), len(rig.cut_plan.get("underpaint", []))))

    # a part that swings, hangs or is physics-driven needs a pivot; a face part
    # is carried by the head deformer and does not.
    need = [l for l in layers
            if l not in pivots and l not in FACE_PARTS
            and l not in {"torso", "back_hair"}]
    for l in need:
        rep.add(WARN, l, "no pivot, but is not a face part carried by the head")
    rep.add(OK, "pivot coverage",
            "%d/%d layers have a pivot (%d face parts inherit the head)"
            % (len(pivots) + len(FACE_PARTS & set(layers)), len(layers),
               len(FACE_PARTS & set(layers))))

    extra = [p for p in pivots if p not in layers]
    for p in extra:
        if p == "head" and "face" in layers:
            rep.add(ERR, p, "pivot key `head` vs layer `face`")
            continue
        closest = "-"
        if layers:
            closest = "face" if p == "head" and "face" in layers else min(
                layers, key=lambda l: abs(len(l) - len(p)))
        rep.add(ERR, p, "pivot defined but no such layer in the plan "
                        "(closest: %s)" % closest)

    # placeholder pivots.json uses bone names; `head` must alias layer `face`
    lp_path = rig.root / "layers" / "pivots.json"
    if lp_path.exists():
        try:
            lp = json.loads(lp_path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError):
            lp = {}
        if isinstance(lp, dict) and "head" in lp and "face" in layers:
            if "face" in lp:
                rep.add(OK, "layers/pivots.json",
                        "head aliases face (%s)" % list(lp["face"]))
            else:
                rep.add(ERR, "head", "pivot key `head` vs layer `face`")


def check_pose3(rig: Rig, rep: Report) -> None:
    """pose3 groups swap between two drawn variants of a limb.

    The ids here (PartArmLFront / PartArmLBack ...) are the Cubism convention for
    that swap and are NOT layer names, so they are not expected to appear in the
    cut plan.  What IS checkable: a swap needs *two* variants drawn, and the plan
    currently lists one per limb.
    """
    layers = set(rig.cut_plan.get("layers", []))
    groups = rig.pose3.get("Groups", [])
    if not rig.pose3:
        return

    swaps = []
    for gi, group in enumerate(groups):
        ids = [e["Id"] for e in group]
        links = sorted({l for e in group for l in e.get("Link", [])})
        swaps.append((gi, ids, links))
    rep.add(OK, "pose3", "%d swap groups declared" % len(swaps))

    for gi, ids, links in swaps:
        base = [i for i in ids if i.endswith("Front") or i.endswith("Back")]
        if len(base) < 2:
            rep.add(WARN, "pose3#%d" % gi,
                    "group %s has no Front/Back pair - pose3 will do nothing"
                    % (ids or "?"))
            continue
        stem = base[0].rsplit("Front", 1)[0].rsplit("Back", 1)[0]
        # does the plan contain two variants for this limb?
        cands = [l for l in layers if stem.replace("Part", "").lower() in
                 l.replace("_", "").lower()]
        if len(cands) < 2:
            rep.add(WARN, "pose3#%d" % gi,
                    "%s swaps two variants but the cut plan has %d matching "
                    "layer(s) %s - the second variant is not drawn"
                    % (stem, len(cands), cands or "[]"))
        else:
            rep.add(OK, "pose3#%d" % gi, "%s has %d layer variants" % (stem, len(cands)))
        for l in links:
            if l.replace("Part", "").lower() not in \
                    "".join(layers).replace("_", "").lower():
                rep.add(WARN, "pose3#%d" % gi,
                        "linked part %s has no obvious layer" % l)


def check_driver_conflicts(rig: Rig, rep: Report) -> None:
    """The lab's own rule: a parameter driven by physics must not also be
    hand-keyed, or the two writers fight."""
    conflicts = 0
    for p in rig.params.values():
        if p.drivable_by_physics and p.hand_keyed:
            conflicts += 1
            rep.add(ERR, p.id, "driver says '%s' but it is also marked 物理输出"
                    % p.driver)
    if conflicts == 0:
        rep.add(OK, "driver conflicts", "no parameter is both physical and keyed")


def main() -> int:
    name = sys.argv[1] if len(sys.argv) > 1 else "elevator-red"
    rig = Rig.load(LAB / name, name)

    print("=" * 78)
    print("rig validation: %s" % name)
    print("  parameters.md : %d parameters" % len(rig.params))
    print("  physics       : %d groups (%d in / %d out)"
          % (len(rig.physics),
             sum(len(g.inputs) for g in rig.physics),
             sum(len(g.outputs) for g in rig.physics)))
    print("  cut plan      : %d layers" % len(rig.cut_plan.get("layers", [])))
    print("  pose3         : %d groups" % len(rig.pose3.get("Groups", [])))
    print("=" * 78)

    rep = Report()
    check_physics_ids(rig, rep)
    check_parameter_coverage(rig, rep)
    check_physics_ordering(rig, rep)
    check_vertex_ranges(rig, rep)
    check_unit_spans(rig, rep)
    check_normalization(rig, rep)
    check_cut_plan(rig, rep)
    check_pose3(rig, rep)
    check_driver_conflicts(rig, rep)
    rep.dump()
    return 1 if rep.count(ERR) else 0


if __name__ == "__main__":
    raise SystemExit(main())
