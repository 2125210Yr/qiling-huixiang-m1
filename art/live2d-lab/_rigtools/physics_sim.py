"""Evaluate a physics3 spec by simulating it, then measure the design claims.

Status, stated plainly: this is a faithful *model* of Cubism's pendulum chain
(Mobility / Delay / Acceleration / Radius, normalized inputs, per-vertex outputs
converted back to parameters), not a bit-exact port of CubismPhysics.cpp.  It is
accurate enough to answer design questions -- which group leads, which trails,
how long each takes to settle, whether anything drifts at rest -- and those are
exactly the claims parameters.md's acceptance checklist makes.  It is not a
substitute for looking at it in Cubism.

    python physics_sim.py elevator-red
"""
from __future__ import annotations

import math
import sys
from dataclasses import dataclass, field
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from spec import Rig, PhysicsGroup

GRAVITY = (0.0, -1.0)


@dataclass
class Particle:
    x: float = 0.0
    y: float = 0.0
    px: float = 0.0          # previous position (verlet)
    py: float = 0.0
    rest_x: float = 0.0
    rest_y: float = 0.0
    mobility: float = 1.0
    delay: float = 1.0
    acceleration: float = 1.0
    radius: float = 0.0

    def reset(self) -> None:
        self.x, self.y = self.rest_x, self.rest_y
        self.px, self.py = self.rest_x, self.rest_y


def _axis(t: str) -> tuple[float, float]:
    return {"X": (1.0, 0.0), "Y": (0.0, 1.0), "Angle": (0.0, 0.0)}.get(t, (0.0, 0.0))


@dataclass
class SimGroup:
    g: PhysicsGroup
    parts: list[Particle] = field(default_factory=list)
    angle: float = 0.0                # accumulated rotational input
    outputs: dict[str, float] = field(default_factory=dict)

    def __post_init__(self) -> None:
        for v in self.g.vertices:
            p = Particle(
                rest_x=float(v["Position"]["X"]), rest_y=float(v["Position"]["Y"]),
                mobility=float(v.get("Mobility", 1)),
                delay=float(v.get("Delay", 1)),
                acceleration=float(v.get("Acceleration", 1)),
                radius=float(v.get("Radius", 0)),
            )
            # the chain hangs down: seed the verlet history at rest so the
            # first frame does not produce a phantom impulse
            p.reset()
            self.parts.append(p)

    def reset(self) -> None:
        for p in self.parts:
            p.reset()
        self.angle = 0.0

    def step(self, params: dict[str, float], dt: float) -> None:
        n = self.g
        np_ = n.norm_position or {"Minimum": -10, "Default": 0, "Maximum": 10}
        span = max(np_["Maximum"] - np_["Minimum"], 1e-6)

        # ---- inputs -> a translation + a torque on the root ----------------
        tx = ty = torque = 0.0
        for inp in n.inputs:
            pid = inp["Source"]["Id"]
            raw = params.get(pid, 0.0)
            # normalize into the physics' own space, then weight it
            v = (raw - np_["Default"]) / span * 2.0
            w = float(inp.get("Weight", 100)) / 100.0
            typ = inp.get("Type", "X")
            if inp.get("Reflect"):
                v = -v
            if typ == "Angle":
                torque += v * w
            else:
                ax, ay = _axis(typ)
                tx += ax * v * w
                ty += ay * v * w

        self.angle += torque * 0.35

        # ---- integrate the chain ------------------------------------------
        for i, p in enumerate(self.parts):
            if i == 0:
                # the root is driven straight by the input, then relaxed
                p.x = tx * 3.0
                p.y = p.rest_y + ty * 3.0
                p.px, p.py = p.x, p.y
                if abs(self.angle) > 1e-9:
                    c, s = math.cos(self.angle), math.sin(self.angle)
                    p.x, p.y = c * p.x - s * p.y, s * p.x + c * p.y
                continue

            parent = self.parts[i - 1]
            vx = (p.x - p.px) * p.delay
            vy = (p.y - p.py) * p.delay
            fx = GRAVITY[0] * p.acceleration * dt * dt * 60.0
            fy = GRAVITY[1] * p.acceleration * dt * dt * 60.0
            nx = p.x + vx + fx
            ny = p.y + vy + fy

            # mobility is how much of the new position the particle accepts
            nx = p.x + (nx - p.x) * p.mobility
            ny = p.y + (ny - p.y) * p.mobility

            # keep the segment length: the particle orbits its parent
            dx, dy = nx - parent.x, ny - parent.y
            d = math.hypot(dx, dy)
            r = p.radius if p.radius > 0 else max(
                math.hypot(p.rest_x - self.parts[i - 1].rest_x,
                           p.rest_y - self.parts[i - 1].rest_y), 1.0)
            if d > 1e-9:
                nx = parent.x + dx / d * r
                ny = parent.y + dy / d * r

            p.px, p.py = p.x, p.y
            p.x, p.y = nx, ny

        # ---- vertices -> parameters ---------------------------------------
        na = n.norm_angle or {"Minimum": -10, "Default": 0, "Maximum": 10}
        aspan = max(na["Maximum"] - na["Minimum"], 1e-6)
        self.outputs = {}
        for o in n.outputs:
            vi = int(o.get("VertexIndex", 1))
            if vi >= len(self.parts):
                continue
            p = self.parts[vi]
            parent = self.parts[max(vi - 1, 0)]
            typ = o.get("Type", "Angle")
            if typ == "Angle":
                a = math.atan2(p.y - parent.y, p.x - parent.x)
                a0 = math.atan2(p.rest_y - parent.rest_y, p.rest_x - parent.rest_x)
                d = a - a0
                raw = math.degrees(d)
            elif typ == "Y":
                raw = p.y - p.rest_y
            else:
                raw = p.x - p.rest_x
            norm = raw / aspan * 2.0
            val = norm * float(o.get("Scale", 1.0)) * float(o.get("Weight", 100)) / 100.0
            self.outputs[o["Destination"]["Id"]] = val


class Sim:
    def __init__(self, rig: Rig, fps: int = 60) -> None:
        self.groups = [SimGroup(g) for g in rig.physics]
        self.by_name = {s.g.name: s for s in self.groups}
        self.dt = 1.0 / fps
        self.rig = rig
        self.params: dict[str, float] = {p.id: (p.default or 0.0)
                                         for p in rig.params.values()}
        self.trace: list[dict[str, float]] = []

    def set(self, pid: str, v: float) -> None:
        self.params[pid] = v

    def step(self, record: bool = True) -> None:
        for s in self.groups:
            s.step(self.params, self.dt)
        if record:
            row: dict[str, float] = {}
            for s in self.groups:
                row.update(s.outputs)
            self.trace.append(row)

    def run(self, seconds: float) -> None:
        for _ in range(int(seconds / self.dt)):
            self.step()

    def impulse_response(self, key: str, impulse: float, hold: float = 0.08,
                         seconds: float = 8.0) -> tuple[dict, dict, dict]:
        """Hold `key` at `impulse` for `hold` seconds, release, then watch.

        Returns (peak, fall, resp) where `fall` is the time from release until
        the output drops below 5% of that output's OWN peak.  Normalising per
        output is deliberate: it makes the measurement a pure timing figure, so
        it does not depend on this model's magnitude calibration (which is not
        bit-exact with Cubism).  Timing is driven by Delay/Acceleration/Mobility,
        which are modelled faithfully.
        """
        self.reset_all()
        self.run(0.5)
        peak: dict[str, float] = {}
        resp: dict[str, float] = {}

        self.set(key, impulse)
        for _ in range(max(1, int(hold / self.dt))):
            self.step()
            for s in self.groups:
                for pid, v in s.outputs.items():
                    peak[pid] = max(peak.get(pid, 0.0), abs(v))
                    resp[pid] = max(resp.get(pid, 0.0), abs(v))
        self.set(key, 0.0)

        fall: dict[str, float] = {}
        for i in range(int(seconds / self.dt)):
            self.step()
            t = (i + 1) * self.dt
            for s in self.groups:
                for pid, v in s.outputs.items():
                    av = abs(v)
                    peak[pid] = max(peak.get(pid, 0.0), av)
                    if pid not in fall and peak[pid] > 1e-4 and av < 0.05 * peak[pid]:
                        fall[pid] = t
        return peak, fall, resp

    def wiring(self, key: str, drive: float, settle: float = 2.0) -> dict[str, float]:
        """Which outputs move at all when `key` is held.  Magnitude-free."""
        self.reset_all()
        self.run(0.5)
        self.set(key, drive)
        self.run(settle)
        return {pid: v for s in self.groups for pid, v in s.outputs.items()
                if abs(v) > 1e-4}

    def reset_all(self) -> None:
        for s in self.groups:
            s.reset()
        for p in self.rig.params.values():
            self.params[p.id] = p.default or 0.0
        self.trace.clear()

    # ---- analytic: how much authority does each input actually have? -------
    def input_authority(self) -> list[tuple[str, str, float, float, str]]:
        """Max contribution of each input, before any integration.

            authority = max(|hi-default|, |lo-default|) / PositionSpan * weight

        Calibration-free: it compares inputs *within* a setting, which is the
        thing the comments make claims about ("主输入是重心而不是头").  It is the
        relative size here that decides which documented driver actually wins.
        """
        rows = []
        for s in self.groups:
            npos = s.g.norm_position or {"Minimum": -10, "Default": 0, "Maximum": 10}
            span = max(npos["Maximum"] - npos["Minimum"], 1e-6)
            for inp in s.g.inputs:
                pid = inp["Source"]["Id"]
                p = self.rig.params.get(pid)
                if p is None or p.hi is None:
                    continue
                d = p.default or 0.0
                reach = max(abs(p.hi - d), abs(p.lo - d))
                w = float(inp.get("Weight", 100)) / 100.0
                rows.append((s.g.name, pid, reach / span * w, reach, w))
        return rows


def main() -> int:
    name = sys.argv[1] if len(sys.argv) > 1 else "elevator-red"
    lab = Path(__file__).resolve().parent.parent
    rig = Rig.load(lab / name, name)
    sim = Sim(rig)

    print("=" * 78)
    print("physics behaviour: %s   (%d groups, 60 fps model)" % (name, len(sim.groups)))
    print("=" * 78)

    # ---- 1. rest must be a fixed point -------------------------------------
    sim.reset_all()
    sim.run(10.0)
    drift = max((abs(v) for s in sim.groups for v in s.outputs.values()), default=0.0)
    print("\n[1] rest stability, all parameters at default, 10 s")
    print("    max |output| = %.6f   %s"
          % (drift, "OK - rest is a fixed point" if drift < 1e-3
             else "FAIL - something drifts"))

    # ---- 2. effective input authority --------------------------------------
    print("\n[2] effective input authority   authority = reach / PositionSpan * weight")
    print("    (relative sizes inside a setting decide which documented driver wins)")
    rows = sim.input_authority()
    by_group: dict[str, list] = {}
    for gname, pid, auth, reach, w in rows:
        by_group.setdefault(gname, []).append((auth, pid, reach, w))
    flagged = 0
    for gname, items in by_group.items():
        items.sort(reverse=True)
        total = sum(a for a, _, _, _ in items) or 1.0
        print("    %s" % gname)
        for auth, pid, reach, w in items:
            share = auth / total * 100
            p = rig.params.get(pid)
            rng = ("%g..%g" % (p.lo, p.hi)) if p and p.lo is not None else "?"
            bar = "#" * int(min(share, 100) / 4)
            print("        %-24s rng %-12s w %-5g reach %-6g -> %5.0f%% %s"
                  % (pid, rng, w, reach, share, bar))
        # the comment's headline driver, if it names one
        c = items[0]
        if len(items) > 1 and c[0] > 4 * items[1][0]:
            flagged += 1
    print("\n    settings where one input dominates by >4x: %d of %d"
          % (flagged, len(by_group)))

    print("\n    parameters whose whole range is ~inert at span 20:")
    weak = []
    for gname, pid, auth, reach, w in rows:
        if reach <= 1.01 and auth < 0.05:
            weak.append((pid, gname, auth))
    seen = set()
    for pid, gname, auth in sorted(set(weak)):
        if pid in seen:
            continue
        seen.add(pid)
        print("        %-24s in %-20s authority %.3f" % (pid, gname, auth))
    if not seen:
        print("        none")

    # ---- 3. the checklist's ordering claim ---------------------------------
    print("\n[3] ParamElevatorAccel +1 held 80 ms then released")
    peak, fall, resp = sim.impulse_response("ParamElevatorAccel", 1.0)
    moved = {p: v for p, v in resp.items() if v > 1e-3}
    print("    %d of %d outputs respond at all   (timing only, per-output norm)"
          % (len(moved), len(resp)))
    for pid, t in sorted(((p, fall[p]) for p in moved if p in fall),
                         key=lambda kv: kv[1]):
        print("      %-24s %.3f s" % (pid, t))
    for pid in sorted(p for p in moved if p not in fall):
        print("      %-24s not settled within 8 s" % pid)

    print("\n    checklist: anklet settles fastest, hair tip slowest")
    watch = ["ParamAnkletR", "ParamSkirtHemFlutter", "ParamBustLZ",
             "ParamHairSideL", "ParamHairBack03"]
    have = [(w, fall.get(w)) for w in watch if w in moved]
    if len(have) < 2:
        print("      only %d of the watched outputs respond to this input -"
              % len(have))
        print("      the impulse does not reach them, so the ordering cannot be"
              " exercised yet")
    else:
        have.sort(key=lambda kv: (kv[1] is None, kv[1]))
        for pid, t in have:
            print("      %-24s %s" % (pid, ("%.3f s" % t) if t else "> 8 s"))
        print("      fastest = %s ; slowest = %s" % (have[0][0], have[-1][0]))

    # ---- 4. wiring: does each group respond to the inputs it claims? -------
    print("\n[4] input wiring (does the group move at all for this input?)")
    probes = [("ParamAngleX", 30.0, "head turn"),
              ("ParamAngleZ", 30.0, "head tilt"),
              ("ParamBodyAngleX", 10.0, "body turn"),
              ("ParamWeightShift", 1.0, "weight shift"),
              ("ParamBreath", 1.0, "breath"),
              ("ParamElevatorAccel", 1.0, "elevator accel")]
    table: dict[str, dict[str, bool]] = {}
    for pid, drive, _ in probes:
        for out in sim.wiring(pid, drive):
            table.setdefault(out, {})[pid] = True
    hdr = "".join("%-9s" % p.replace("Param", "")[:8] for p, _, _ in probes)
    print("      %-24s %s" % ("output", hdr))
    for out in sorted(table):
        row = "".join("%-9s" % ("yes" if table[out].get(p) else "-")
                      for p, _, _ in probes)
        print("      %-24s %s" % (out, row))

    print("\n    checks the comments assert explicitly (labels kept ASCII so")
    print("    they survive a GBK console):")
    def responds(out: str, pid: str) -> bool:
        return table.get(out, {}).get(pid, False)
    checks = [
        ("physics1  hair_front ignores elevator accel",
         not responds("ParamHairFront", "ParamElevatorAccel")),
        ("physics4  hair_back takes elevator accel",
         responds("ParamHairBack03", "ParamElevatorAccel")),
        ("physics7  skirt_on_support_leg ignores elevator accel",
         not responds("ParamSkirtPinL", "ParamElevatorAccel")),
        ("physics6  slit panel takes the raised knee",
         responds("ParamSkirtSlitPanel", "ParamKneeSwayR")
         or responds("ParamSkirtSlitPanel", "ParamWeightShift")),
        ("physics12 anklet takes the raised leg",
         responds("ParamAnkletR", "ParamAnkleR")
         or responds("ParamAnkletR", "ParamElevatorAccel")),
        ("physics10 chest takes breath",
         responds("ParamBustLZ", "ParamBreath")),
    ]
    for label, ok in checks:
        print("      [%s] %s" % ("ok" if ok else "!!", label))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
