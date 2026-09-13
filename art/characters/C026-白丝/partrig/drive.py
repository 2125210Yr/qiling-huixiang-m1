"""Drive the part rig.

The point of this file is the thing a single-plate warp cannot do: every part
moves on its own authority.

  head        rigid.  Its children (hair_front, choker, earrings, hair_side,
              hair ties) ride along rigidly, so a head turn never stretches a
              face -- the face is a different part from the chest.
  chest       its own spring, its own squash.  Nothing else inherits it.
  hair_back   pendulum about the tail root, phase-lagged behind the head.
  legs        counter-rotated at the hip so the feet stay planted while the
              body sways.  Feet do not jiggle.

    python drive.py                 # 12 s showcase -> out/C026_rig.mp4
    python drive.py --pose          # pose sheet instead
"""
from __future__ import annotations

import argparse
import math
import subprocess
import sys
import time
from pathlib import Path

import cv2
import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
from partrig import Model, Skeleton, render, imread_unicode

HERE = Path(__file__).resolve().parent
OUT = HERE / "out"

FPS = 60
LOOP = 12.0


# ------------------------------------------------------------------ physics
class Spring:
    """Critically-ish damped 2D spring, same shape as the one in the C001 rig."""

    def __init__(self, k: float, c: float) -> None:
        self.k, self.c = k, c
        self.p = np.zeros(2)
        self.v = np.zeros(2)

    def step(self, drive: np.ndarray, dt: float) -> np.ndarray:
        a = -self.k * self.p - self.c * self.v - drive
        self.v += a * dt
        self.p += self.v * dt
        return self.p


class Pendulum:
    """Delayed-follow one-liner: the tip replays the driver from tau ago.

    The motion driving it is harmonic, so a bucket table is exact enough and
    costs nothing per part.
    """

    def __init__(self, tau: float, gain: float) -> None:
        self.tau = tau
        self.gain = gain
        self.hist: list[float] = []

    def push(self, v: float) -> None:
        self.hist.append(v)
        if len(self.hist) > 4000:
            self.hist.pop(0)

    def at(self, t: float, dt: float) -> float:
        i = len(self.hist) - 1 - int(round(self.tau / dt))
        if i < 0:
            i = 0
        return self.hist[i] * self.gain


# ------------------------------------------------------------------- motion
def smoothstep(x, a, b):
    t = max(0.0, min(1.0, (x - a) / (b - a)))
    return t * t * (3 - 2 * t)


def motion(t: float) -> dict:
    """Every oscillator is an integer harmonic of LOOP so the cycle seals."""
    h = lambda k, ph=0.0: math.sin(2 * math.pi * k * t / LOOP + ph)
    # Amplitudes are deliberately conservative.  This split was cut for
    # compositing, not for deformation: psd_cut_plan.json lists eight underpaint
    # pieces (scalp behind the bangs, thigh inside the slit, hip crease on the
    # raised leg ...) that the actual layers/ folder does not contain.  Without
    # that underpaint, a part that swings away exposes the hole it left, so the
    # rig can only move as far as the parts overlap.
    return dict(
        breath=h(3),
        sway=h(2) * 1.8 + h(1, 1.1) * 1.2,
        sway_v=h(3, 0.7) * 0.8,
        body_rot=h(2, 0.4) * 0.005,
        look_x=h(2, 1.9) * 0.055 + h(1, 0.3) * 0.022,
        look_y=h(3, 2.4) * 0.035,
        head_tilt=h(2, 0.9) * 0.016,
        hop=max(0.0, math.sin(2 * math.pi * 3 * t / LOOP)) ** 8 * 2.2,
    )


class Driver:
    def __init__(self, model: Model) -> None:
        self.m = model
        self.sk = Skeleton(model)
        self.chest = Spring(210.0, 7.4)
        self.head_spring = Spring(150.0, 12.0)
        self.hair = {
            "hair_back_l": Pendulum(0.17, 1.0),
            "hair_back_r": Pendulum(0.19, 1.0),
            "hair_side_l": Pendulum(0.09, 1.0),
            "hair_side_r": Pendulum(0.10, 1.0),
        }
        self.prev = np.zeros(2)
        self.vel = np.zeros(2)
        self.acc = np.zeros(2)

        # where each foot rests, in canvas px: bottom centre of the lower leg
        self.foot: dict[str, np.ndarray] = {}
        for leg, low in (("leg_upper_l", "leg_lower_l"),
                         ("leg_upper_r", "leg_lower_r")):
            x0, y0, x1, y1 = model.parts[low].bbox
            self.foot[leg] = np.array([(x0 + x1) / 2.0, float(y1)])

    def rest(self) -> None:
        for p in self.m.parts.values():
            p.rot = 0.0
            p.sx = p.sy = 1.0
            p.tx = p.ty = p.shear = 0.0

    def step(self, t: float, dt: float) -> None:
        m = motion(t)
        P = self.m.parts
        self.rest()

        # ---- pelvis ---------------------------------------------------------
        # A rig swings at its JOINTS.  Translating a garment part independently
        # slides it off the body underneath -- the suit has to stay where the
        # torso is.  So the pelvis barely travels and the sway is expressed as
        # rotation further up the chain, where contact is preserved.
        body = P["body"]
        body.tx = m["sway"] * 0.18
        body.ty = m["sway_v"] * 0.20 - m["hop"] * 0.10
        body.rot = m["body_rot"] * 0.3

        # ---- chest: own spring, own squash, nothing else feels it -----------
        drive = np.array([m["sway"] * 6.0, m["hop"] * 90.0])
        q = self.chest.step(drive, dt)
        q = np.clip(q, -7.0, 7.0)
        strain = float(np.clip(-self.chest.v[1] * 0.00045, -0.10, 0.10))
        ch = P["chest"]
        # lean at the waist rather than sliding: the torso is a joint, and the
        # spring only adds the small bounce of the soft body itself
        ch.rot = m["sway"] * 0.0075 + m["body_rot"] * 0.7
        ch.tx = q[0] * 0.5
        ch.ty = q[1] * 0.6 - m["breath"] * 1.1
        ch.sx = 1.0 + strain * 0.55
        ch.sy = 1.0 - strain
        ch.rot = m["body_rot"]

        # ---- head: rigid, and it carries the face ---------------------------
        hs = self.head_spring
        tgt = np.array([m["look_x"] * 26.0, -m["look_y"] * 18.0])
        hs.step(-(hs.p - tgt) * 40.0, dt)
        hd = P["head"]
        # the neck is the joint: turn there, and only let a little travel creep
        # in so the head still reads as rigid
        hd.rot = m["look_x"] * 0.9 + m["head_tilt"] + m["sway"] * 0.004
        hd.tx = hs.p[0] * 0.35
        hd.ty = hs.p[1] * 0.5

        # ---- hair: chain bend, not a rigid swing ---------------------------
        # Rotating a whole 950 px tail about its tie root flings the tip metres
        # and tears a hole where the root was.  Real hair bends: the root stays
        # put and the deflection grows toward the tip, which is exactly what a
        # shear about the root pivot does.  Rotation stays small.
        for name, pend in self.hair.items():
            pend.push(hd.rot)
            lag = pend.at(t, dt) - hd.rot          # delayed minus current
            big = name.startswith("hair_back")
            P[name].rot = lag * (0.30 if big else 0.70)
            P[name].shear = lag * (1.35 if big else 0.55)
            P[name].ty = -abs(lag) * 2.0

        # ---- arms ride the chest, hands/prop ride the arm -------------------
        for name in ("arm_upper_l", "arm_upper_r", "arm_lower_l", "arm_lower_r"):
            P[name].rot = m["body_rot"] * 0.5
            P[name].ty = m["breath"] * -0.5

        # ---- legs: counter-rotate at the hip so the feet stay planted -------
        self._plant_feet(m)

        self.sk.resolve()

    def _plant_feet(self, m: dict) -> None:
        """One degree of freedom per leg: rotate the thigh about the hip far
        enough to cancel the body's drift, measured at the foot."""
        P = self.m.parts
        # must track what the pelvis actually does, or the correction is wrong
        hip_shift = np.array([P["body"].tx, P["body"].ty])

        for leg in ("leg_upper_l", "leg_upper_r"):
            part = P[leg]
            hip = np.array(part.pivot)
            foot = self.foot[leg] + hip_shift
            lever = foot - hip
            L2 = float(lever @ lever)
            if L2 < 1.0:
                continue
            # desired correction: push the foot back by -hip_shift
            want = -hip_shift * 0.85
            ang = (lever[0] * want[1] - lever[1] * want[0]) / L2
            part.rot += ang
            # the thigh's own pivot motion is already inherited from `body`


# ------------------------------------------------------------------ output
def contact_sheet(model: Model, drv: Driver, times: list[float]) -> None:
    tiles = []
    for t in times:
        dt = 1 / FPS
        for i in range(int(t * FPS) + 1):
            drv.step(i / FPS, dt)
        c = render(model, drv.sk)
        rgb = np.clip(c[..., :3] * c[..., 3:4] * 255, 0, 255).astype(np.uint8)
        cv2.putText(rgb, "t=%.2f" % t, (14, 34), cv2.FONT_HERSHEY_SIMPLEX, 0.9,
                    (60, 255, 255), 2, cv2.LINE_AA)
        tiles.append(rgb)
        p = model.parts
        drv.rest()
    blank = np.zeros_like(tiles[0])
    rows = []
    for i in range(0, len(tiles), 3):
        row = tiles[i:i + 3]
        while len(row) < 3:
            row.append(blank)
        rows.append(np.hstack(row))
    sheet = np.vstack(rows)
    scale = min(1.0, 1500.0 / sheet.shape[1])
    sheet = cv2.resize(sheet, None, fx=scale, fy=scale, interpolation=cv2.INTER_AREA)
    OUT.mkdir(exist_ok=True)
    cv2.imencode(".png", sheet)[1].tofile(str(OUT / "pose_sheet.png"))
    print("wrote out/pose_sheet.png  %dx%d" % (sheet.shape[1], sheet.shape[0]))


def video(model: Model, drv: Driver, seconds: float, sup: int, dst: Path) -> None:
    W, H = model.W * sup, model.H * sup
    ff = subprocess.Popen(
        ["ffmpeg", "-y", "-loglevel", "error",
         "-f", "rawvideo", "-pix_fmt", "rgb24", "-s", "%dx%d" % (W, H), "-r", str(FPS),
         "-i", "-",
         "-c:v", "libx264", "-crf", "15", "-preset", "medium", "-pix_fmt", "yuv420p",
         "-movflags", "+faststart", str(dst)],
        stdin=subprocess.PIPE)
    n = int(seconds * FPS)
    dt = 1 / FPS
    t0 = time.time()
    for i in range(n):
        t = i / FPS
        drv.step(t, dt)
        c = render(model, drv.sk)
        rgb = np.clip(c[..., :3] * c[..., 3:4] * 255, 0, 255).astype(np.uint8)
        if sup != 1:
            rgb = cv2.resize(rgb, (W, H), interpolation=cv2.INTER_LANCZOS4)
        ff.stdin.write(rgb.tobytes())
        if i % 120 == 0:
            print("  %4d/%d  %.0f ms/frame" % (i, n, 1000 * (time.time() - t0) / max(i + 1, 1)))
    ff.stdin.close()
    ff.wait()
    print("wrote %s (%.1f MB)" % (dst, dst.stat().st_size / 1e6))


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--pose", action="store_true")
    ap.add_argument("--seconds", type=float, default=LOOP)
    ap.add_argument("--sup", type=int, default=2)
    a = ap.parse_args()

    model = Model()
    drv = Driver(model)
    OUT.mkdir(exist_ok=True)

    if a.pose:
        contact_sheet(model, drv, [0.0, 1.5, 3.0, 4.5, 6.0, 7.5, 9.0, 11.85])
        return 0

    video(model, drv, a.seconds, a.sup, OUT / "C026_rig.mp4")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
