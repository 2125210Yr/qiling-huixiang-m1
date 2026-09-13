"""Generate placeholder layer PNGs for the elevator-red slot.

The real layers will be cut from reference.jpg in Cubism/PS. Until then this
writes flat silhouette stand-ins so web-puppet has something to animate, using
the layer names and z-order from ../../elevator-red/psd_cut_plan.json.

Pure stdlib on purpose: PIL is not installed in this repo's python.

    python art\live2d-lab\web-puppet\_tools\make_placeholder_layers.py

Output: art/live2d-lab/elevator-red/layers/*.png  (1098x574, 2x the reference)
Delete that folder and re-run to regenerate.
"""

import json
import math
import os
import struct
import zlib

W, H = 1098, 574  # 2x reference.jpg (549x287) so psd_cut_plan pivots map 1:1

OUT = os.path.abspath(
    os.path.join(os.path.dirname(__file__), "..", "..", "elevator-red", "layers")
)

SKIN = (232, 199, 176)
SKIN_DEEP = (198, 158, 138)
DRESS = (172, 30, 46)
DRESS_DEEP = (104, 14, 26)
HAIR = (44, 32, 52)
HORN = (62, 48, 64)
IRIS = (150, 36, 58)
METAL = (96, 92, 104)


# ---------------------------------------------------------------- png writing

def _chunk(tag, data):
    return (struct.pack(">I", len(data)) + tag + data
            + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF))


def write_png(path, buf):
    """buf: bytearray of W*H*4 RGBA."""
    rows = bytearray()
    stride = W * 4
    for y in range(H):
        rows.append(0)
        rows += buf[y * stride:(y + 1) * stride]
    png = (b"\x89PNG\r\n\x1a\n"
           + _chunk(b"IHDR", struct.pack(">IIBBBBB", W, H, 8, 6, 0, 0, 0))
           + _chunk(b"IDAT", zlib.compress(bytes(rows), 6))
           + _chunk(b"IEND", b""))
    with open(path, "wb") as fh:
        fh.write(png)


# ------------------------------------------------------------------ shapes
# Each primitive reports (coverage_distance_in_px, roundness 0..1) so a single
# sample per pixel still gives smooth edges (SDF antialiasing).

class Ellipse:
    def __init__(self, cu, cv, ru, rv):
        self.cx, self.cy = cu * W, cv * H
        self.rx, self.ry = ru * W, rv * H
        self.scale = min(self.rx, self.ry)

    def bbox(self):
        return (self.cx - self.rx - 2, self.cy - self.ry - 2,
                self.cx + self.rx + 2, self.cy + self.ry + 2)

    def at(self, x, y):
        q = math.hypot((x - self.cx) / self.rx, (y - self.cy) / self.ry)
        return (1.0 - q) * self.scale, 1.0 - q


class Capsule:
    """Tapered thick segment - limbs, hair strands, horns."""

    def __init__(self, u0, v0, u1, v1, r0, r1=None):
        self.x0, self.y0 = u0 * W, v0 * H
        self.x1, self.y1 = u1 * W, v1 * H
        self.r0 = r0 * W
        self.r1 = (r1 if r1 is not None else r0) * W
        self.dx, self.dy = self.x1 - self.x0, self.y1 - self.y0
        self.len2 = max(1e-6, self.dx * self.dx + self.dy * self.dy)

    def bbox(self):
        r = max(self.r0, self.r1) + 2
        return (min(self.x0, self.x1) - r, min(self.y0, self.y1) - r,
                max(self.x0, self.x1) + r, max(self.y0, self.y1) + r)

    def at(self, x, y):
        t = ((x - self.x0) * self.dx + (y - self.y0) * self.dy) / self.len2
        t = 0.0 if t < 0.0 else (1.0 if t > 1.0 else t)
        px, py = self.x0 + self.dx * t, self.y0 + self.dy * t
        d = math.hypot(x - px, y - py)
        r = self.r0 + (self.r1 - self.r0) * t
        return r - d, 1.0 - d / max(1e-6, r)


class Poly:
    def __init__(self, pts):
        self.pts = [(u * W, v * H) for u, v in pts]

    def bbox(self):
        xs = [p[0] for p in self.pts]
        ys = [p[1] for p in self.pts]
        return (min(xs) - 2, min(ys) - 2, max(xs) + 2, max(ys) + 2)

    def at(self, x, y):
        # signed distance to polygon boundary, positive inside
        inside = False
        best = 1e9
        n = len(self.pts)
        for i in range(n):
            ax, ay = self.pts[i]
            bx, by = self.pts[(i + 1) % n]
            if (ay > y) != (by > y):
                if x < (bx - ax) * (y - ay) / (by - ay) + ax:
                    inside = not inside
            ex, ey = bx - ax, by - ay
            t = ((x - ax) * ex + (y - ay) * ey) / max(1e-6, ex * ex + ey * ey)
            t = 0.0 if t < 0.0 else (1.0 if t > 1.0 else t)
            best = min(best, math.hypot(x - ax - ex * t, y - ay - ey * t))
        sd = best if inside else -best
        return sd, min(1.0, best / 26.0) if inside else 0.0


# ------------------------------------------------------------------ painting

def paint(prims, color, deep=None, alpha=1.0):
    """Rasterize primitives into a fresh RGBA buffer with soft edges."""
    buf = bytearray(W * H * 4)
    if deep is None:
        deep = tuple(int(c * 0.72) for c in color)
    x0 = y0 = 10 ** 9
    x1 = y1 = -10 ** 9
    for p in prims:
        bx0, by0, bx1, by1 = p.bbox()
        x0, y0 = min(x0, bx0), min(y0, by0)
        x1, y1 = max(x1, bx1), max(y1, by1)
    x0 = max(0, int(x0)); y0 = max(0, int(y0))
    x1 = min(W - 1, int(x1) + 1); y1 = min(H - 1, int(y1) + 1)
    for y in range(y0, y1 + 1):
        row = y * W * 4
        for x in range(x0, x1 + 1):
            sd = -1e9
            rnd = 0.0
            for p in prims:
                d, r = p.at(x + 0.5, y + 0.5)
                if d > sd:
                    sd = d
                if r > rnd:
                    rnd = r
            cov = sd + 0.5
            if cov <= 0.0:
                continue
            if cov > 1.0:
                cov = 1.0
            k = 0.74 + 0.30 * math.sqrt(max(0.0, min(1.0, rnd)))
            if k > 1.0:
                k = 1.0
            i = row + x * 4
            for c in range(3):
                v = deep[c] + (color[c] - deep[c]) * k
                buf[i + c] = int(max(0, min(255, v)))
            buf[i + 3] = int(255 * cov * alpha)
    return buf


def paint_bg():
    """Elevator interior plate: dark red car, door seam, right button panel."""
    buf = bytearray(W * H * 4)
    for y in range(H):
        row = y * W * 4
        fy = y / H
        for x in range(W):
            fx = x / W
            # red wash, brighter behind the figure, falls off to the edges
            glow = math.exp(-(((fx - 0.40) ** 2) / 0.09 + ((fy - 0.45) ** 2) / 0.55))
            r = 30 + 118 * glow
            g = 10 + 22 * glow
            b = 16 + 34 * glow
            if fx > 0.74:  # right wall panel is cooler metal
                r, g, b = 44 + 26 * glow, 42 + 22 * glow, 52 + 26 * glow
            if 0.185 < fx < 0.205 or 0.655 < fx < 0.675:  # door frame uprights
                r, g, b = r * 0.42, g * 0.42, b * 0.46
            if fy < 0.035 or fy > 0.965:
                r, g, b = r * 0.3, g * 0.3, b * 0.34
            i = row + x * 4
            buf[i] = int(min(255, r))
            buf[i + 1] = int(min(255, g))
            buf[i + 2] = int(min(255, b))
            buf[i + 3] = 255
    # button pad
    for by in range(6):
        for bx in range(2):
            cx = (0.80 + bx * 0.055) * W
            cy = (0.22 + by * 0.105) * H
            for y in range(int(cy - 11), int(cy + 12)):
                if y < 0 or y >= H:
                    continue
                for x in range(int(cx - 11), int(cx + 12)):
                    if x < 0 or x >= W:
                        continue
                    if math.hypot(x - cx, y - cy) > 10:
                        continue
                    i = (y * W + x) * 4
                    buf[i] = METAL[0]
                    buf[i + 1] = METAL[1]
                    buf[i + 2] = METAL[2]
    return buf


# ------------------------------------------------------------- layer recipes
# Coordinates traced off reference.jpg, normalized (u from left, v from TOP).

LAYERS = [
    ("bg_elevator", None, None, None),
    ("back_hair", [
        Ellipse(0.430, 0.300, 0.072, 0.150),
        Capsule(0.437, 0.215, 0.470, 0.615, 0.044, 0.018),
        Capsule(0.400, 0.220, 0.372, 0.520, 0.030, 0.012),
    ], HAIR, None),
    ("torso", [
        Poly([(0.376, 0.312), (0.456, 0.308), (0.474, 0.452),
              (0.488, 0.575), (0.402, 0.580), (0.392, 0.452)]),
    ], SKIN, SKIN_DEEP),
    ("chest", [
        Ellipse(0.408, 0.356, 0.030, 0.046),
        Ellipse(0.444, 0.352, 0.030, 0.046),
    ], SKIN, SKIN_DEEP),
    ("dress_body", [
        Poly([(0.372, 0.316), (0.459, 0.310), (0.479, 0.460),
              (0.493, 0.600), (0.480, 0.790), (0.408, 0.796),
              (0.393, 0.600), (0.378, 0.460)]),
    ], DRESS, DRESS_DEEP),
    ("leg_stand", [
        Capsule(0.455, 0.580, 0.462, 0.740, 0.026, 0.020),
        Capsule(0.462, 0.740, 0.458, 0.900, 0.020, 0.013),
        Ellipse(0.454, 0.938, 0.017, 0.030),
    ], SKIN, SKIN_DEEP),
    ("dress_slit_panel", [
        Poly([(0.400, 0.500), (0.420, 0.620), (0.408, 0.880),
              (0.366, 0.876), (0.370, 0.620)]),
    ], DRESS_DEEP, (72, 8, 18)),
    ("leg_raise_thigh", [
        Capsule(0.418, 0.545, 0.352, 0.366, 0.036, 0.028),
    ], SKIN, SKIN_DEEP),
    ("leg_raise_calf", [
        Capsule(0.352, 0.366, 0.295, 0.660, 0.028, 0.017),
    ], SKIN, SKIN_DEEP),
    ("foot_raise", [
        Capsule(0.295, 0.660, 0.248, 0.798, 0.017, 0.011),
        Ellipse(0.243, 0.812, 0.017, 0.026),
        # anklet stand-in (psd_cut_plan keeps it as its own layer)
        Ellipse(0.296, 0.652, 0.020, 0.013),
    ], SKIN, SKIN_DEEP),
    ("arm_hip", [
        Capsule(0.462, 0.325, 0.506, 0.442, 0.021, 0.017),
        Capsule(0.506, 0.442, 0.476, 0.532, 0.017, 0.013),
        Ellipse(0.472, 0.542, 0.015, 0.022),
    ], SKIN, SKIN_DEEP),
    ("neck", [
        Capsule(0.416, 0.252, 0.424, 0.312, 0.014),
    ], SKIN_DEEP, (168, 130, 114)),
    ("face", [
        Ellipse(0.412, 0.202, 0.040, 0.068),
    ], SKIN, SKIN_DEEP),
    ("eyes", [
        Ellipse(0.399, 0.198, 0.0075, 0.011),
        Ellipse(0.428, 0.196, 0.0075, 0.011),
    ], IRIS, (58, 12, 26)),
    ("front_hair", [
        Ellipse(0.412, 0.172, 0.045, 0.034),
        Capsule(0.378, 0.190, 0.370, 0.310, 0.013, 0.006),
        Capsule(0.448, 0.188, 0.458, 0.330, 0.013, 0.006),
    ], HAIR, (30, 22, 38)),
    ("horns", [
        Capsule(0.394, 0.156, 0.362, 0.070, 0.015, 0.004),
        Capsule(0.432, 0.154, 0.464, 0.066, 0.015, 0.004),
    ], HORN, (36, 26, 38)),
]


def main():
    os.makedirs(OUT, exist_ok=True)
    written = []
    for name, prims, color, deep in LAYERS:
        buf = paint_bg() if prims is None else paint(prims, color, deep)
        path = os.path.join(OUT, name + ".png")
        write_png(path, buf)
        written.append(name + ".png")
        print("  wrote", os.path.basename(path))

    # pivots.json in the same shape web-puppet / the C001 sets use.
    # v is measured from the TOP here, matching psd_cut_plan.json.
    pivots = {
        "_note": "placeholder pivots, v measured from TOP (pivotOrigin=top-left)",
        "size": [W, H],
        "head": [0.416, 0.258],
        "chest": [0.412, 0.330],
        "torso": [0.428, 0.560],
        "dress_body": [0.424, 0.330],
        "dress_slit_panel": [0.398, 0.520],
        "leg_stand": [0.455, 0.580],
        "leg_raise_thigh": [0.418, 0.545],
        "leg_raise_calf": [0.352, 0.366],
        "foot_raise": [0.295, 0.660],
        "arm_hip": [0.462, 0.325],
        "back_hair": [0.428, 0.200],
        "front_hair": [0.412, 0.150],
        "horns": [0.412, 0.170],
        "eyes": [0.413, 0.197],
    }
    with open(os.path.join(OUT, "pivots.json"), "w", encoding="utf-8") as fh:
        json.dump(pivots, fh, ensure_ascii=False, indent=2)
    print("  wrote pivots.json")
    print("%d layers -> %s" % (len(written), OUT))


if __name__ == "__main__":
    main()
