"""Drive the See-Through layer rig.

Different part names from the hand-cut set, so this is its own driver.  The
things that were impossible before are possible now because the face is
separated and every part carries underpaint:

  blink   eyelash travels down while eyewhite/irides compress vertically.
          Three real layers, not a synthesised lid.
  eyes    irides translates inside eyewhite, which is 100% behind it.
  head    rotates about the neck; face/eyes/brows ride it rigidly.
  hair    back hair lags far behind, front hair close behind.
  legs    counter-rotated at the hip so the feet stay planted.
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
from partrig import Skeleton, render
import seerig as S
from drive import Spring, Pendulum

HERE = Path(__file__).resolve().parent
OUT = HERE / "out"
FPS = 60
LOOP = 12.0


def motion(t: float) -> dict:
    h = lambda k, ph=0.0: math.sin(2 * math.pi * k * t / LOOP + ph)
    return dict(
        breath=h(3),
        sway=h(2) * 2.2 + h(1, 1.1) * 1.4,
        sway_v=h(3, 0.7) * 0.9,
        body_rot=h(2, 0.4) * 0.006,
        look_x=h(2, 1.9) * 0.10 + h(1, 0.3) * 0.04,
        look_y=h(3, 2.4) * 0.05,
        head_tilt=h(2, 0.9) * 0.02,
    )


class SeeDriver:
    def __init__(self, model) -> None:
        self.m = model
        self.sk = Skeleton(model)
        self.head_spring = Spring(150.0, 12.0)
        self.eye_spring = Spring(220.0, 14.0)
        self.chest = Spring(200.0, 8.0)
        self.hair = {
            "back hair": Pendulum(0.19, 1.0),
            "front hair": Pendulum(0.08, 1.0),
        }
        self.blink_start = -1.0
        self.next_blink = 1.4
        #: foot anchor = bottom centre of legwear, per side (viewer l/r)
        self.feet = {}
        a = model.parts["legwear"].img[..., 3] > 0.03
        ys, xs = np.nonzero(a)
        mid = (xs.min() + xs.max()) / 2
        for tag, sel in (("l", xs < mid), ("r", xs >= mid)):
            if sel.sum():
                self.feet[tag] = np.array([xs[sel].mean(), ys[sel].max()])
        self.hip = {
            "l": np.array([xs[xs < mid].mean(), ys.min() + (ys.max() - ys.min()) * 0.05]),
            "r": np.array([xs[xs >= mid].mean(), ys.min() + (ys.max() - ys.min()) * 0.05]),
        }

    def rest(self) -> None:
        for p in self.m.parts.values():
            p.rot = 0.0
            p.sx = p.sy = 1.0
            p.tx = p.ty = p.shear = 0.0

    # ---- blink -----------------------------------------------------------
    def blink(self, t: float) -> float:
        if self.blink_start >= 0:
            u = (t - self.blink_start) / 0.15
            if u >= 1:
                self.blink_start = -1.0
                self.next_blink = t + 2.4 + 0.7 * math.sin(t * 7.3)
                return 0.0
            return math.sin(math.pi * u)
        if t >= self.next_blink:
            self.blink_start = t
        return 0.0

    def step(self, t: float, dt: float) -> None:
        m = motion(t)
        P = self.m.parts
        self.rest()

        # ---- torso ---------------------------------------------------------
        tor = P["topwear"]
        tor.tx = m["sway"]
        tor.ty = m["sway_v"] - m["breath"] * 0.8
        tor.rot = m["body_rot"]
        q = self.chest.step(np.array([m["sway"] * 4.0, 0.0]), dt)
        tor.tx += float(np.clip(q[0], -3, 3)) * 0.4

        # ---- head (carries face, eyes, brows, hair roots) ------------------
        hs = self.head_spring
        tgt = np.array([m["look_x"] * 20.0, -m["look_y"] * 13.0])
        hs.step(-(hs.p - tgt) * 40.0, dt)
        nk = P["neck"]
        nk.rot = m["look_x"] * 0.25 + m["body_rot"] * 0.5
        hd = P["head"]
        hd.rot = m["look_x"] * 0.75 + m["head_tilt"]
        hd.tx = hs.p[0] * 0.5
        hd.ty = hs.p[1] * 0.5

        # ---- eyes ----------------------------------------------------------
        self.eye_spring.step(-(self.eye_spring.p
                               - np.array([m["look_x"] * 3.2, -m["look_y"] * 2.2])) * 90.0, dt)
        for n in ("irides",):
            P[n].tx = self.eye_spring.p[0]
            P[n].ty = self.eye_spring.p[1]

        b = self.blink(t)
        if b > 0.001:
            # squash the eye vertically and bring the lash down over it
            k = 1.0 - 0.92 * b
            for n in ("eyewhite", "irides"):
                P[n].sy = k
                P[n].ty += (1.0 - k) * 3.0
            lash = P["eyelash"]
            lash.ty = b * 4.2
            lash.sy = 1.0 + b * 0.5
            P["eyebrow"].ty = b * 1.4

        # ---- hair: lag the head -------------------------------------------
        for name, pend in self.hair.items():
            pend.push(hd.rot + hd.tx * 0.004)
            lag = pend.at(t, dt) - (hd.rot + hd.tx * 0.004)
            big = name == "back hair"
            P[name].rot = lag * (0.35 if big else 0.9)
            P[name].shear = lag * (1.5 if big else 0.7)
            P[name].ty = -abs(lag) * 3.0

        # ---- arms follow the torso -----------------------------------------
        P["handwear"].rot = m["body_rot"] * 0.6
        P["handwear"].ty = m["breath"] * -0.4
        P["objects"].rot = m["body_rot"] * 0.6

        # ---- legs: keep the feet where they are ----------------------------
        # `legwear` is ONE plate covering both legs, so there is a single
        # rotation to share: correct about the mean hip toward the mean foot.
        # Splitting it per leg would need two plates, which this set does not
        # provide.
        P = self.m.parts
        shift = np.array([P["topwear"].tx, P["topwear"].ty])
        hip = np.array([np.mean([self.hip[t][0] for t in self.hip]),
                        np.mean([self.hip[t][1] for t in self.hip])])
        foot = np.mean([self.feet[t] for t in self.feet], axis=0) + shift
        lever = foot - hip
        L2 = float(lever @ lever)
        if L2 > 4.0:
            want = -shift * 0.75
            P["legwear"].rot += (lever[0] * want[1] - lever[1] * want[0]) / L2

        self.sk.resolve()


def run(seconds: float, sup: int, dst: Path, sheet: bool = False) -> None:
    m = S.load_see_model()
    drv = SeeDriver(m)
    OUT.mkdir(exist_ok=True)
    dt = 1 / FPS
    W, H = m.W * sup, m.H * sup

    if sheet:
        times = [0.0, 1.5, 3.0, 4.5, 6.0, 7.5, 9.0, 10.5]
        tiles = []
        for tt in times:
            for i in range(int(tt * FPS) + 1):
                drv.step(i / FPS, dt)
            c = render(m, drv.sk)
            rgb = np.clip(c[..., :3] * 255, 0, 255).astype(np.uint8)
            if sup != 1:
                rgb = cv2.resize(rgb, (W, H), interpolation=cv2.INTER_LANCZOS4)
            rgb = cv2.resize(rgb, (384, 384), interpolation=cv2.INTER_AREA)
            cv2.putText(rgb, "t=%.1f" % tt, (8, 24), cv2.FONT_HERSHEY_SIMPLEX,
                        0.6, (60, 255, 255), 2)
            tiles.append(rgb)
        rows = [np.hstack(tiles[i:i + 4]) for i in range(0, len(tiles), 4)]
        s = np.vstack(rows)
        # the whole pipeline is BGR (cv2), and imencode wants BGR -- swapping
        # here is what turned the skin blue in the first pass
        cv2.imencode(".png", s)[1].tofile(str(OUT / "see_pose_sheet.png"))
        print("wrote out/see_pose_sheet.png %dx%d" % (s.shape[1], s.shape[0]))
        return

    ff = subprocess.Popen(
        ["ffmpeg", "-y", "-loglevel", "error", "-f", "rawvideo", "-pix_fmt", "rgb24",
         "-s", "%dx%d" % (W, H), "-r", str(FPS), "-i", "-",
         "-c:v", "libx264", "-crf", "15", "-preset", "medium", "-pix_fmt", "yuv420p",
         "-movflags", "+faststart", str(dst)], stdin=subprocess.PIPE)
    n = int(seconds * FPS)
    t0 = time.time()
    for i in range(n):
        drv.step(i / FPS, dt)
        c = render(m, drv.sk)
        rgb = np.clip(c[..., :3] * 255, 0, 255).astype(np.uint8)
        if sup != 1:
            rgb = cv2.resize(rgb, (W, H), interpolation=cv2.INTER_LANCZOS4)
        ff.stdin.write(rgb[..., ::-1].tobytes())   # BGR buffer -> rgb24 pipe
        if i % 120 == 0:
            print("  %4d/%d  %.0f ms/frame" % (i, n, 1000 * (time.time() - t0) / max(i + 1, 1)))
    ff.stdin.close()
    ff.wait()
    print("wrote %s (%.1f MB)" % (dst, dst.stat().st_size / 1e6))


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--pose", action="store_true")
    ap.add_argument("--seconds", type=float, default=LOOP)
    ap.add_argument("--sup", type=int, default=1)
    a = ap.parse_args()
    run(a.seconds, a.sup, OUT / "C026_see_rig.mp4", sheet=a.pose)
