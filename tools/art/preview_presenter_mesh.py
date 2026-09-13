"""Preview the Home presenter mesh deform (same weights as Live2DIdle)."""
import math
from pathlib import Path

import numpy as np
from PIL import Image
from scipy.ndimage import map_coordinates

ROOT = Path(r"F:\天命之子")
SRC = ROOT / r"art\characters\C001-焰刃\presenter-v3.png"
OUT = ROOT / r"天命之子数据"
COLS, ROWS = 18, 28


def clamp01(x):
    return 0.0 if x < 0.0 else 1.0 if x > 1.0 else x


def smooth01(a, b, x):
    if abs(b - a) < 1e-4:
        return 1.0 if x >= b else 0.0
    t = clamp01((x - a) / (b - a))
    return t * t * (3.0 - 2.0 * t)


def blob(u, v, cx, cy, rx, ry):
    dx = (u - cx) / rx
    dy = (v - cy) / ry
    return smooth01(1.14, 0.70, dx * dx + dy * dy)


def pulse(x, lo, hi):
    m = (lo + hi) * 0.5
    r = (hi - lo) * 0.5
    if r <= 1e-4:
        return 0.0
    return smooth01(1.0, 0.52, abs(x - m) / r)


def live_mask(u, v):
    floor = smooth01(0.05, 0.14, v)
    body = blob(u, v, 0.53, 0.44, 0.20, 0.42)
    hair_l = blob(u, v, 0.30, 0.62, 0.34, 0.38)
    hair_r = blob(u, v, 0.78, 0.58, 0.24, 0.28)
    sword = blob(u, v, 0.40, 0.32, 0.16, 0.24)
    return max(body, hair_l, hair_r, sword) * floor


def hair_mask(u, v):
    top = smooth01(0.68, 0.86, v)
    side = smooth01(0.14, 0.26, abs(u - 0.52)) * smooth01(0.30, 0.50, v)
    return min(1.0, top * 0.90 + side)


def rotate(x, y, ang):
    c, s = math.cos(ang), math.sin(ang)
    return x * c - y * s, x * s + y * c


class Spring:
    def __init__(self):
        self.x = 0.0
        self.v = 0.0

    def step(self, target, freq, zeta, dt):
        omega = freq * 2.0 * math.pi
        acc = -2.0 * zeta * omega * self.v - omega * omega * (self.x - target)
        self.v += acc * dt
        self.x += self.v * dt
        return self.x


def sample(u, v, pose, w, h):
    live = live_mask(u, v)
    if live <= 0.001:
        return 0.0, 0.0
    hip_ang = pose["hip"] * 0.030
    bx, by = rotate(u - 0.52, v - 0.40, hip_ang)
    body_x = (bx + 0.52 - u) * w
    body_y = (by + 0.40 - v) * h
    chest = pulse(v, 0.48, 0.68) * pulse(u, 0.38, 0.64)
    breath = pose["breath"]
    cx = (u - 0.52) * breath * 0.014 * w * chest
    cy = (v - 0.48) * breath * 0.020 * h * chest
    head_m = pulse(v, 0.66, 0.84) * pulse(u, 0.38, 0.64)
    hx, hy = rotate(u - 0.52, v - 0.70, pose["head"] * 0.040)
    head_x = (hx + 0.52 - u) * w * head_m
    head_y = (hy + 0.70 - v) * h * head_m
    hair = hair_mask(u, v)
    side = pose["hair_l"] if u < 0.50 else pose["hair_r"]
    hair_x = side * (0.010 + 0.018 * v) * w * hair
    hair_y = pose["hair_y"] * (0.006 + 0.010 * v) * h * hair
    hover = pose["hover"] * 0.0075 * h
    lift = smooth01(0.10, 0.28, v)
    return (body_x + cx + head_x + hair_x) * live, (body_y + cy + head_y + hair_y + hover * lift) * live


def pose_at(t, hair_l, hair_r, hair_y):
    return {
        "hip": math.sin(t * 0.70),
        "breath": math.sin(t * 1.32),
        "head": math.sin(t * 0.92 + 0.55) * 0.70 + math.sin(t * 1.68) * 0.30,
        "hover": math.sin(t * 0.82),
        "hair_l": hair_l,
        "hair_r": hair_r,
        "hair_y": hair_y,
    }


def warp(arr, pose):
    h, w = arr.shape[0], arr.shape[1]
    yy, xx = np.mgrid[0:h, 0:w]
    u = xx / float(w - 1)
    v = 1.0 - yy / float(h - 1)
    # grid deform, then bilinear sample offsets
    us = np.linspace(0, 1, COLS + 1)
    vs = np.linspace(0, 1, ROWS + 1)
    ox = np.zeros((ROWS + 1, COLS + 1), np.float32)
    oy = np.zeros((ROWS + 1, COLS + 1), np.float32)
    for iy, vv in enumerate(vs):
        for ix, uu in enumerate(us):
            dx, dy = sample(uu, vv, pose, w, h)
            ox[iy, ix] = dx
            oy[iy, ix] = dy
    # bilinear interpolate grid offsets to pixels
    gx = u * COLS
    gy = v * ROWS
    x0 = np.clip(np.floor(gx).astype(np.int32), 0, COLS - 1)
    y0 = np.clip(np.floor(gy).astype(np.int32), 0, ROWS - 1)
    x1 = x0 + 1
    y1 = y0 + 1
    tx = gx - x0
    ty = gy - y0
    def lerp4(m):
        return (
            m[y0, x0] * (1 - tx) * (1 - ty)
            + m[y0, x1] * tx * (1 - ty)
            + m[y1, x0] * (1 - tx) * ty
            + m[y1, x1] * tx * ty
        )
    dx = lerp4(ox)
    dy = lerp4(oy)
    src_x = xx - dx
    src_y = yy + dy  # unity +y up is pil -y
    out = np.zeros_like(arr)
    for c in range(arr.shape[2]):
        out[..., c] = map_coordinates(arr[..., c], [src_y, src_x], order=1, mode="nearest")
    return out.astype(np.uint8)


def main():
    im = Image.open(SRC).convert("RGB").resize((480, 720), Image.Resampling.LANCZOS)
    arr = np.asarray(im, dtype=np.float32)
    h, w = arr.shape[:2]
    hair_l, hair_r, hair_y = Spring(), Spring(), Spring()
    dt = 1.0 / 24.0
    t = 0.0
    gust = 0.0
    frames = []
    picks = {}
    for i in range(48):
        hip = math.sin(t * 0.70)
        wind = math.sin(t * 0.46) + 0.38 * math.sin(t * 1.08 + 1.1)
        if i == 18:
            gust = 1.15
        gust = max(0.0, gust - dt * 1.15)
        hair_l.step(wind * 0.90 + hip * 0.22 + gust, 1.55, 0.30, dt)
        hair_r.step(wind * 0.62 + math.sin(t * 0.88 + 2.1) * 0.28 + gust * 0.7, 1.72, 0.34, dt)
        hair_y.step(math.sin(t * 1.32) * 0.22 + math.sin(t * 0.82) * 0.18, 1.40, 0.36, dt)
        pose = pose_at(t, hair_l.x, hair_r.x, hair_y.x)
        if i % 2 == 0:
            fr = Image.fromarray(warp(arr, pose))
            frames.append(fr.resize((360, 540), Image.Resampling.LANCZOS))
            if i in (0, 8, 16, 24, 32):
                picks[i] = fr
        t += dt
    gif = OUT / "_preview_live2d.gif"
    frames[0].save(gif, save_all=True, append_images=frames[1:], duration=83, loop=0)
    keys = [picks[k] for k in sorted(picks)]
    cw, ch = keys[0].size
    strip = Image.new("RGB", (cw * len(keys), ch))
    for i, imk in enumerate(keys):
        strip.paste(imk, (i * cw, 0))
    strip.resize((min(1920, 256 * len(keys)), 384), Image.Resampling.LANCZOS).save(OUT / "_preview_live2d_strip.png")
    # hair crop strip
    box = (0, int(ch * 0.08), int(cw * 0.55), int(ch * 0.62))
    crops = [k.crop(box).resize((280, 360), Image.Resampling.LANCZOS) for k in keys]
    hs = Image.new("RGB", (280 * len(crops), 360))
    for i, c in enumerate(crops):
        hs.paste(c, (i * 280, 0))
    hs.save(OUT / "_preview_live2d_hair.png")
    print("gif", gif, "frames", len(frames))
    print("hair amp L/R", round(hair_l.x, 3), round(hair_r.x, 3))


if __name__ == "__main__":
    main()
