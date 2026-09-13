"""elevator-red Live2D retarget: seam close + parent-touch pivots.

Ports the geometry from C026-白丝/live2d-export/retarget_live2d.py
(and split_contour.py helpers). It does NOT read or recolor C026 plates —
that latex body is the wrong look for this qipao still.

Reads, in order:
  1. elevator-red layers/<part>.png as seed plates, if they match the body canvas
  2. a rembg silhouette (layers/body.png, cutout_2x.png, body_original.png, cutout.png)
  3. rembg of reference.jpg / original-still.png if no body exists yet

Weak / missing plates are rebuilt from rembg ∩ UV envelopes (GIF still or 16:9
original). Leftover opaque pixels are handed to the nearest plate so a deformed
mesh cannot show cracks. Each pivot is the centroid of the band where the part
actually touches its parent.

Outputs land in layers/export/ only. Source layers are never written.
"""
from __future__ import annotations

import json
from collections import Counter
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy import ndimage as ndi

ROOT = Path(__file__).resolve().parent
LAYERS = ROOT / "layers"
OUT = LAYERS / "export"
QA = OUT / "_qa"

ALPHA_ON = 8
WEAK_PX = 1500
DILATE_STEPS = (1, 2, 3, 4, 6, 8, 12, 16, 24, 32)

# name, part_id, source plate (or synthesized name), parent, pivot rule.
# Draw order back -> front. hair_back hangs on root, not head (elevator mirror).
PARTS = [
    ("back_hair", "PartHairBack", "back_hair", None, {"rule": "contact", "with": "face"}),
    ("torso", "PartTorso", "torso", None, {"rule": "contact_any", "with": ["leg_stand", "leg_raise_thigh"]}),
    ("chest", "PartChest", "chest", "torso", {"rule": "contact", "with": "torso"}),
    ("dress_body", "PartDress", "dress_body", "torso", {"rule": "contact", "with": "torso"}),
    ("leg_stand", "PartLegStand", "leg_stand", "torso", {"rule": "contact", "with": "torso"}),
    ("dress_slit_panel", "PartDressSlitPanelBack", "dress_slit_panel", "dress_body", {"rule": "contact", "with": "dress_body"}),
    ("leg_raise_thigh", "PartLegRaise", "leg_raise_thigh", "torso", {"rule": "contact", "with": "torso"}),
    ("leg_raise_calf", "PartLegRaise", "leg_raise_calf", "leg_raise_thigh", {"rule": "contact", "with": "leg_raise_thigh"}),
    ("foot_raise", "PartLegRaise", "foot_raise", "leg_raise_calf", {"rule": "contact", "with": "leg_raise_calf"}),
    ("anklet", "PartAnkletR", "anklet", "foot_raise", {"rule": "contact", "with": "foot_raise"}),
    ("arm_upper_hip", "PartArmL", "arm_upper_hip", "chest", {"rule": "contact", "with": "chest"}),
    ("arm_forearm_hip", "PartArmL", "arm_forearm_hip", "arm_upper_hip", {"rule": "contact", "with": "arm_upper_hip"}),
    ("hand_hip", "PartHandL", "hand_hip", "arm_forearm_hip", {"rule": "contact", "with": "arm_forearm_hip"}),
    ("neck", "PartNeck", "neck", "chest", {"rule": "contact", "with": "chest"}),
    ("face", "PartFace", "face", "neck", {"rule": "contact", "with": "neck"}),
    ("eyes", "PartFace", "eyes", "face", {"rule": "centroid"}),
    ("front_hair", "PartHairFront", "front_hair", "face", {"rule": "contact", "with": "face"}),
    ("horns", "PartHorns", "horns", "face", {"rule": "contact", "with": "face"}),
]

OPTIONAL = {"anklet", "eyes", "horns"}

# UV envelopes traced off the GIF still (top-left origin). Same family as
# split_elevator_red.py / web-puppet placeholders — no C026 geometry.
GIF_ENVELOPES = {
    "back_hair": ("ellipses", [(0.430, 0.300, 0.090, 0.170), (0.455, 0.420, 0.055, 0.220)]),
    "front_hair": ("ellipse", 0.412, 0.175, 0.055, 0.055),
    "horns": ("ellipses", [(0.378, 0.118, 0.028, 0.040), (0.448, 0.112, 0.028, 0.040)]),
    "face": ("ellipse", 0.412, 0.205, 0.055, 0.085),
    "neck": ("ellipse", 0.420, 0.285, 0.032, 0.048),
    "chest": ("ellipse", 0.418, 0.365, 0.070, 0.075),
    "torso": ("poly", [(0.368, 0.300), (0.470, 0.295), (0.490, 0.520), (0.400, 0.560), (0.370, 0.430)]),
    "dress_body": ("poly", [(0.365, 0.300), (0.472, 0.292), (0.500, 0.620), (0.478, 0.820), (0.400, 0.830), (0.368, 0.560)]),
    "dress_slit_panel": ("poly", [(0.390, 0.500), (0.430, 0.520), (0.418, 0.880), (0.355, 0.870), (0.368, 0.560)]),
    "leg_stand": ("poly", [(0.420, 0.560), (0.500, 0.560), (0.495, 0.930), (0.430, 0.945), (0.418, 0.720)]),
    "leg_raise_thigh": ("poly", [(0.300, 0.300), (0.430, 0.320), (0.445, 0.560), (0.300, 0.500)]),
    "leg_raise_calf": ("poly", [(0.200, 0.500), (0.360, 0.360), (0.380, 0.560), (0.250, 0.720), (0.190, 0.680)]),
    "foot_raise": ("poly", [(0.175, 0.640), (0.280, 0.620), (0.300, 0.780), (0.200, 0.840), (0.165, 0.760)]),
    "arm_hip": ("poly", [(0.450, 0.300), (0.545, 0.300), (0.540, 0.500), (0.455, 0.560), (0.440, 0.420)]),
    "eyes": ("ellipses", [(0.399, 0.198, 0.012, 0.014), (0.428, 0.196, 0.012, 0.014)]),
    "anklet": ("ellipse", 0.220, 0.700, 0.028, 0.022),
}

GIF_JOINTS = {
    "shoulder": (0.462, 0.325),
    "elbow": (0.506, 0.442),
    "wrist": (0.476, 0.532),
    "hip_raise": (0.418, 0.545),
    "knee_raise": (0.352, 0.366),
    "ankle_raise": (0.295, 0.660),
}

# 16:9 original-still (centered figure, raised leg on image-right, hair to the left).
WIDE_ENVELOPES = {
    "back_hair": ("poly", [
        (0.48, 0.14), (0.57, 0.16), (0.56, 0.30), (0.40, 0.40),
        (0.18, 0.46), (0.10, 0.42), (0.12, 0.30), (0.32, 0.26), (0.46, 0.18),
    ]),
    "front_hair": ("ellipse", 0.523, 0.205, 0.040, 0.032),
    "horns": ("ellipses", [(0.505, 0.172, 0.020, 0.024), (0.545, 0.170, 0.020, 0.024)]),
    "face": ("ellipse", 0.525, 0.235, 0.032, 0.055),
    "neck": ("ellipse", 0.528, 0.295, 0.018, 0.028),
    "chest": ("ellipse", 0.530, 0.365, 0.048, 0.050),
    "torso": ("poly", [(0.495, 0.340), (0.570, 0.340), (0.580, 0.540), (0.505, 0.560), (0.490, 0.440)]),
    "dress_body": ("poly", [(0.490, 0.330), (0.575, 0.325), (0.595, 0.620), (0.575, 0.860), (0.500, 0.875), (0.478, 0.600)]),
    "dress_slit_panel": ("poly", [(0.495, 0.500), (0.540, 0.520), (0.530, 0.860), (0.465, 0.850), (0.478, 0.560)]),
    "leg_stand": ("poly", [(0.515, 0.560), (0.575, 0.560), (0.568, 0.940), (0.518, 0.955), (0.508, 0.720)]),
    "leg_raise_thigh": ("poly", [(0.530, 0.400), (0.640, 0.280), (0.700, 0.400), (0.560, 0.540)]),
    "leg_raise_calf": ("poly", [(0.630, 0.300), (0.730, 0.380), (0.750, 0.560), (0.620, 0.540)]),
    "foot_raise": ("poly", [(0.680, 0.480), (0.760, 0.480), (0.775, 0.640), (0.680, 0.650)]),
    "arm_hip": ("poly", [(0.430, 0.340), (0.510, 0.330), (0.520, 0.500), (0.455, 0.540), (0.415, 0.440)]),
    "eyes": ("ellipses", [(0.512, 0.228, 0.009, 0.010), (0.540, 0.226, 0.009, 0.010)]),
    "anklet": ("ellipse", 0.705, 0.500, 0.020, 0.014),
}

WIDE_JOINTS = {
    "shoulder": (0.490, 0.355),
    "elbow": (0.455, 0.420),
    "wrist": (0.470, 0.475),
    "hip_raise": (0.555, 0.490),
    "knee_raise": (0.645, 0.330),
    "ankle_raise": (0.705, 0.500),
}

SOURCE_ALIASES = {
    "arm_upper_hip": ["arm_upper_hip", "arm_hip"],
    "face": ["face", "head"],
}


def poly(h: int, w: int, uvs: list[tuple[float, float]]) -> np.ndarray:
    pts = [(int(u * w), int(v * h)) for u, v in uvs]
    m = Image.new("L", (w, h), 0)
    ImageDraw.Draw(m).polygon(pts, fill=255)
    return np.asarray(m) > 0


def ellipse(h: int, w: int, cu: float, cv: float, ru: float, rv: float) -> np.ndarray:
    yy, xx = np.ogrid[:h, :w]
    return ((xx - cu * w) / (ru * w)) ** 2 + ((yy - cv * h) / (rv * h)) ** 2 <= 1.0


def envelope_mask(h: int, w: int, spec) -> np.ndarray:
    kind = spec[0]
    if kind == "ellipse":
        return ellipse(h, w, *spec[1:])
    if kind == "ellipses":
        out = np.zeros((h, w), bool)
        for e in spec[1]:
            out |= ellipse(h, w, *e)
        return out
    if kind == "poly":
        return poly(h, w, spec[1])
    raise ValueError(spec)


def checker(h: int, w: int, cell: int = 16) -> np.ndarray:
    yy, xx = np.indices((h, w))
    ch = np.zeros((h, w, 3), np.uint8)
    ch[((yy // cell) + (xx // cell)) % 2 == 0] = (86, 86, 86)
    ch[((yy // cell) + (xx // cell)) % 2 == 1] = (48, 48, 48)
    return ch


def flatten(rgba: np.ndarray) -> np.ndarray:
    h, w = rgba.shape[:2]
    a = rgba[:, :, 3:4].astype(np.float32) / 255.0
    return (rgba[:, :, :3] * a + checker(h, w) * (1 - a)).astype(np.uint8)


def bbox(mask: np.ndarray):
    if not mask.any():
        return None
    ys, xs = np.where(mask)
    return [int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1]


def centroid(mask: np.ndarray):
    ys, xs = np.where(mask)
    return float(xs.mean()), float(ys.mean())


def bbox_anchor(mask: np.ndarray, at: str):
    x0, y0, x1, y1 = bbox(mask)
    cx = (x0 + x1 - 1) / 2.0
    if at == "top_center":
        return cx, float(y0)
    if at == "bottom_center":
        return cx, float(y1 - 1)
    return cx, (y0 + y1 - 1) / 2.0


def snap_xy(mask: np.ndarray, x: float, y: float) -> tuple[int, int]:
    x, y = int(round(x)), int(round(y))
    if not mask.any():
        return x, y
    h, w = mask.shape
    if 0 <= y < h and 0 <= x < w and mask[y, x]:
        return x, y
    ys, xs = np.where(mask)
    i = int(np.argmin((xs - x) ** 2 + (ys - y) ** 2))
    return int(xs[i]), int(ys[i])


def component_at(mask: np.ndarray, seed) -> np.ndarray:
    x, y = snap_xy(mask, seed[0], seed[1])
    if not mask.any():
        return np.zeros_like(mask)
    lab, _ = ndi.label(mask)
    return lab == lab[y, x]


def halfplane_across(xx: np.ndarray, yy: np.ndarray, root, joint) -> np.ndarray:
    """Pixels on the root side of the plane through `joint`, perpendicular to root→joint."""
    rx, ry = root
    jx, jy = joint
    dx, dy = jx - rx, jy - ry
    return (xx - jx) * dx + (yy - jy) * dy <= 0


def recut_joint(masks: dict, spec: dict) -> None:
    """Re-split two adjacent plates at a joint, in place.

    Walks geodesic distance out from `root` inside the merged silhouette and keeps
    everything at or below the distance of `joint`. Distance follows the limb, so a
    folded arm/leg splits at the elbow/knee instead of at whatever is Euclidean-nearest.
    """
    upper, lower = spec["upper"], spec["lower"]
    whole = masks.get(upper, False)
    if not isinstance(whole, np.ndarray):
        whole = np.zeros_like(next(v for v in masks.values() if isinstance(v, np.ndarray)))
    if lower in masks and isinstance(masks[lower], np.ndarray):
        whole = whole | masks[lower]
    if not whole.any():
        print(f"  skip recut {upper}/{lower}: empty union")
        return
    rx, ry = snap_xy(whole, *spec["root"])
    jx, jy = snap_xy(whole, *spec["joint"])
    if (rx - jx) ** 2 + (ry - jy) ** 2 < 400:
        print(f"  recut {upper}/{lower}: snapped seeds collapsed to ({rx},{ry})/({jx},{jy}), using halfplane")
        yy, xx = np.indices(whole.shape)
        prox = whole & halfplane_across(xx, yy, spec["root"], spec["joint"])
        masks[upper], masks[lower] = prox, whole & ~prox
        print(f"    -> {int(masks[upper].sum())} / {int(masks[lower].sum())} px")
        return
    prox = None
    try:
        from skimage.graph import MCP_Geometric

        dist, _ = MCP_Geometric(np.where(whole, 1.0, np.inf)).find_costs([[ry, rx]])
        cut = dist[jy, jx]
        if np.isfinite(cut):
            prox = component_at(whole & np.isfinite(dist) & (dist <= cut), (rx, ry))
            print(f"  recut {upper}/{lower} geodesic {float(cut):.0f}px from ({rx},{ry})")
    except Exception as exc:
        print(f"  geodesic recut fallback for {upper}/{lower}: {exc}")
    if prox is None or not prox.any() or np.array_equal(prox, whole):
        yy, xx = np.indices(whole.shape)
        prox = whole & halfplane_across(xx, yy, (rx, ry), (jx, jy))
        print(f"  recut {upper}/{lower} halfplane at ({jx},{jy})")
    masks[upper], masks[lower] = prox, whole & ~prox
    print(f"    -> {int(masks[upper].sum())} / {int(masks[lower].sum())} px")


def contact(a: np.ndarray, b: np.ndarray, min_pix: int = 8):
    """Centroid of the band where two plates meet, plus the dilation it needed."""
    if not a.any() or not b.any():
        return None
    for it in DILATE_STEPS:
        band = ndi.binary_dilation(a, iterations=it) & ndi.binary_dilation(b, iterations=it)
        if int(band.sum()) >= min_pix:
            cx, cy = centroid(band)
            return cx, cy, it, int(band.sum())
    return None


def solve_pivot(name: str, spec: dict, masks: dict, src_masks: dict):
    """-> (x, y, parent_override, note)."""
    rule = spec["rule"]
    if rule == "centroid":
        cx, cy = centroid(masks[name])
        return cx, cy, None, "plate centroid"
    if rule == "bbox":
        cx, cy = bbox_anchor(masks[name], spec["at"])
        return cx, cy, None, f"bbox {spec['at']}"
    if rule == "ref_centroid":
        ref = spec["of"]
        if src_masks.get(ref) is not None and src_masks[ref].any():
            cx, cy = centroid(src_masks[ref])
            return cx, cy, None, f"centroid of source plate '{ref}'"
        cx, cy = bbox_anchor(masks[name], "top_center")
        return cx, cy, None, f"fallback bbox top_center ('{ref}' empty)"
    if rule == "contact":
        hit = contact(masks[name], masks[spec["with"]])
        if hit:
            cx, cy, it, n = hit
            return cx, cy, None, f"contact with '{spec['with']}' (grow {it}px, {n}px band)"
        cx, cy = bbox_anchor(masks[name], "top_center")
        return cx, cy, None, f"fallback bbox top_center (no contact with '{spec['with']}')"
    if rule == "contact_any":
        present = [k for k in spec["with"] if k in masks and masks[k].any()]
        if present:
            other = np.logical_or.reduce([masks[k] for k in present])
            hit = contact(masks[name], other)
            if hit:
                cx, cy, it, n = hit
                return cx, cy, None, f"contact with {'+'.join(present)} (grow {it}px, {n}px band)"
        cx, cy = bbox_anchor(masks[name], "bottom_center")
        return cx, cy, None, "fallback bbox bottom_center"
    if rule == "contact_best":
        best = None
        for cand in spec["with"]:
            if cand not in masks:
                continue
            hit = contact(masks[name], masks[cand])
            if hit and (best is None or hit[3] > best[1][3]):
                best = (cand, hit)
        if best:
            cand, (cx, cy, it, n) = best
            return cx, cy, cand, f"contact with '{cand}' (grow {it}px, {n}px band; best of {spec['with']})"
        cx, cy = centroid(masks[name])
        return cx, cy, spec["with"][0], "fallback plate centroid (no contact)"
    raise ValueError(rule)


def pick_profile(w: int, h: int) -> tuple[str, dict, dict]:
    aspect = w / max(1, h)
    gif_err = abs(aspect - (546 / 292))
    wide_err = abs(aspect - (16 / 9))
    if wide_err + 0.04 < gif_err:
        return "wide-original", WIDE_ENVELOPES, WIDE_JOINTS
    return "gif-still", GIF_ENVELOPES, GIF_JOINTS


def majority_layer_canvas() -> tuple[int, int] | None:
    sizes: list[tuple[int, int]] = []
    names = {p[0] for p in PARTS} | {"arm_hip", "head"}
    for name in names:
        path = LAYERS / f"{name}.png"
        if not path.is_file():
            continue
        im = Image.open(path)
        if im.size != (2048, 2048):
            sizes.append(im.size)
    return Counter(sizes).most_common(1)[0][0] if sizes else None


def resolve_rgba() -> tuple[np.ndarray, Path]:
    """elevator-red rembg / still only. Never opens C026 presenter.png."""
    want = majority_layer_canvas()
    opened: list[tuple[Path, Image.Image]] = []
    for p in (
        LAYERS / "body_original.png",
        LAYERS / "body.png",
        LAYERS / "cutout_2x.png",
        LAYERS / "cutout.png",
    ):
        if p.is_file():
            opened.append((p, Image.open(p).convert("RGBA")))
    if want:
        for p, im in opened:
            if im.size == want:
                print(f"body: {p} {im.size} (matches layer canvas)")
                return np.array(im), p
    if opened:
        p, im = max(opened, key=lambda t: t[1].size[0] * t[1].size[1])
        print(f"body: {p} {im.size}")
        return np.array(im), p
    for p in (ROOT / "original-still.png", ROOT / "reference.jpg"):
        if p.is_file():
            from rembg import remove

            print(f"rembg {p} (in-memory, source layers not overwritten)")
            cut = remove(Image.open(p).convert("RGBA"))
            return np.array(cut.convert("RGBA")), p
    raise SystemExit("no elevator-red layers or rembg body (and no still to rembg)")


def plate_usable(mask: np.ndarray, fig: np.ndarray, envelope: np.ndarray | None = None) -> bool:
    """Drop inverted backgrounds, whole-figure dumps, and plates far from their envelope."""
    on = int(mask.sum())
    if on < 80:
        return False
    if on > 0.35 * mask.size:
        return False
    inside = int((mask & fig).sum())
    if inside < 0.5 * on:
        return False
    if inside > 0.70 * int(fig.sum()):
        return False
    if envelope is not None and envelope.any():
        hit = int((mask & envelope).sum())
        if hit < 0.20 * on:
            return False
    return True


def load_plate_mask(
    name: str,
    h: int,
    w: int,
    fig: np.ndarray | None = None,
    envelopes: dict | None = None,
) -> np.ndarray | None:
    names = SOURCE_ALIASES.get(name, [name])
    for cand in names:
        path = LAYERS / f"{cand}.png"
        if not path.is_file():
            continue
        im = Image.open(path).convert("RGBA")
        if im.size == (2048, 2048):
            continue
        if im.size != (w, h):
            if abs(im.size[0] / im.size[1] - w / h) > 0.08:
                print(f"  skip {cand}.png {im.size} (canvas is {w}x{h})")
                continue
            im = im.resize((w, h), Image.Resampling.NEAREST)
        m = np.array(im)[:, :, 3] > ALPHA_ON
        env = None
        if envelopes and name in envelopes:
            env = envelope_mask(h, w, envelopes[name])
        elif envelopes and cand in envelopes:
            env = envelope_mask(h, w, envelopes[cand])
        if fig is not None and m.any() and not plate_usable(m, fig, env):
            print(f"  skip {cand}.png (inverted, whole-figure, or off-envelope)")
            continue
        if m.any():
            return m
    return None


def synthesize(name: str, fig: np.ndarray, envelopes: dict, seed_uv=None) -> np.ndarray:
    spec = envelopes.get(name)
    if spec is None:
        return np.zeros(fig.shape, bool)
    walk = fig & envelope_mask(*fig.shape, spec)
    if not walk.any():
        return walk
    if seed_uv is None:
        if spec[0] == "ellipse":
            seed_uv = (spec[1], spec[2])
        elif spec[0] == "ellipses":
            seed_uv = spec[1][0][:2]
        elif spec[0] == "poly":
            pts = spec[1]
            seed_uv = (sum(p[0] for p in pts) / len(pts), sum(p[1] for p in pts) / len(pts))
    h, w = fig.shape
    return component_at(walk, (seed_uv[0] * w, seed_uv[1] * h))


def write_contact_sheet(plates_dir: Path, order, path: Path) -> None:
    cols = 5
    rows = (len(order) + cols - 1) // cols
    cell_w, cell_h = 300, 380
    sheet = np.full((rows * cell_h, cols * cell_w, 3), 28, np.uint8)
    try:
        font = ImageFont.truetype("arial.ttf", 15)
    except Exception:
        font = ImageFont.load_default()
    for i, name in enumerate(order):
        rr, cc = divmod(i, cols)
        plate = np.array(Image.open(plates_dir / f"{name}.png"))
        m = plate[:, :, 3] > ALPHA_ON
        cell = checker(cell_h - 28, cell_w, 12)
        if m.any():
            x0, y0, x1, y1 = bbox(m)
            crop = plate[max(0, y0 - 6) : y1 + 6, max(0, x0 - 6) : x1 + 6]
            ch, cw = crop.shape[:2]
            scale = min((cell_w - 16) / cw, (cell_h - 44) / ch)
            nw, nh = max(1, int(cw * scale)), max(1, int(ch * scale))
            crop_np = np.array(Image.fromarray(crop).resize((nw, nh), Image.Resampling.LANCZOS))
            ox, oy = (cell_w - nw) // 2, (cell_h - 28 - nh) // 2
            aa = crop_np[:, :, 3:4].astype(np.float32) / 255.0
            region = cell[oy : oy + nh, ox : ox + nw]
            cell[oy : oy + nh, ox : ox + nw] = (crop_np[:, :, :3] * aa + region * (1 - aa)).astype(np.uint8)
        tile = np.full((cell_h, cell_w, 3), 22, np.uint8)
        tile[28:] = cell
        tile_im = Image.fromarray(tile)
        ImageDraw.Draw(tile_im).text((8, 6), f"{i:02d} {name}", fill=(230, 230, 230), font=font)
        sheet[rr * cell_h : (rr + 1) * cell_h, cc * cell_w : (cc + 1) * cell_w] = np.array(tile_im)
    Image.fromarray(sheet).save(path, quality=93)


def write_pivot_overlay(rgba: np.ndarray, pivots: dict, path: Path) -> None:
    im = Image.fromarray(flatten(rgba))
    d = ImageDraw.Draw(im, "RGBA")
    try:
        font = ImageFont.truetype("arial.ttf", 11)
    except Exception:
        font = ImageFont.load_default()
    for name, info in pivots.items():
        parent = info["parent"]
        if parent and parent in pivots:
            px, py = pivots[parent]["pivot_px"]
            x, y = info["pivot_px"]
            d.line([(x, y), (px, py)], fill=(255, 255, 255, 90), width=1)
    for name, info in pivots.items():
        x, y = info["pivot_px"]
        col = (255, 90, 60, 255) if info["parent"] is None else (70, 220, 255, 255)
        d.ellipse([x - 4, y - 4, x + 4, y + 4], fill=col, outline=(20, 20, 20, 255))
        d.text((x + 6, y - 6), name, fill=(255, 255, 255, 235), font=font)
    im.save(path)


def uv_of(px: list[float], w: int, h: int) -> list[float]:
    return [round(px[0] / w, 5), round(px[1] / h, 5)]


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    QA.mkdir(parents=True, exist_ok=True)
    for p in list(OUT.glob("*.png")) + list(QA.glob("*.png")) + list(QA.glob("*.jpg")):
        p.unlink()

    rgba, body_path = resolve_rgba()
    h, w = rgba.shape[:2]
    fig = rgba[:, :, 3] > ALPHA_ON
    profile_name, envelopes, joints = pick_profile(w, h)
    print(f"canvas {w}x{h} profile={profile_name} opaque={int(fig.sum())}")
    print("pixel source is elevator-red rembg/still — C026 is not read or recolored")

    src_masks: dict[str, np.ndarray] = {}
    masks: dict[str, np.ndarray] = {}
    origin: dict[str, str] = {}

    def take(name: str, mask: np.ndarray, how: str) -> None:
        masks[name] = mask
        src_masks[name] = mask
        origin[name] = how
        print(f"  seed {name:20s} {int(mask.sum()):7d}  {how}")

    # ----- load or synthesize every plate except the arm chain (recut later) -----
    preload = [
        "back_hair",
        "torso",
        "chest",
        "dress_body",
        "leg_stand",
        "dress_slit_panel",
        "leg_raise_thigh",
        "leg_raise_calf",
        "foot_raise",
        "anklet",
        "neck",
        "face",
        "eyes",
        "front_hair",
        "horns",
    ]
    for name in preload:
        plate = load_plate_mask(name, h, w, fig, envelopes)
        if plate is not None and int(plate.sum()) >= WEAK_PX:
            if name in envelopes:
                env = envelope_mask(h, w, envelopes[name])
                clipped = plate & ndi.binary_dilation(env, iterations=14)
                if int(clipped.sum()) >= 80:
                    plate = clipped
            take(name, plate, f"layer {name}.png")
            continue
        syn = synthesize(name, fig, envelopes)
        if syn.any():
            how = "rembg∩envelope"
            if plate is not None:
                how += f" (replaced weak {int(plate.sum())}px plate)"
            take(name, syn, how)
        elif plate is not None:
            take(name, plate, f"layer {name}.png (kept, no envelope hit)")

    # arm: prefer a split if those plates already exist and are strong, else recut
    arm_parts = ("arm_upper_hip", "arm_forearm_hip", "hand_hip")
    split_masks = {n: load_plate_mask(n, h, w, fig, envelopes) for n in arm_parts}
    have_split = all(m is not None and int(m.sum()) >= WEAK_PX for m in split_masks.values())
    if have_split:
        for n in arm_parts:
            take(n, split_masks[n], f"layer {n}.png")
    else:
        union = load_plate_mask("arm_hip", h, w, fig, envelopes)
        env_arm = synthesize("arm_hip", fig, envelopes)
        how = "rembg∩envelope arm_hip"
        if union is not None and env_arm.any():
            clipped = union & ndi.binary_dilation(env_arm, iterations=10)
            if int(clipped.sum()) >= 2500:
                union = clipped
                how = "layer arm_hip.png ∩ envelope"
            else:
                union = env_arm
        if union is None or int(union.sum()) < 200:
            union = env_arm
            how = "rembg∩envelope arm_hip"
        if union is not None and union.any():
            take("arm_upper_hip", union.copy(), how)
            take("arm_forearm_hip", np.zeros((h, w), bool), "recut placeholder")
            take("hand_hip", np.zeros((h, w), bool), "recut placeholder")
            recut_joint(
                masks,
                {
                    "upper": "arm_upper_hip",
                    "lower": "arm_forearm_hip",
                    "root": (joints["shoulder"][0] * w, joints["shoulder"][1] * h),
                    "joint": (joints["elbow"][0] * w, joints["elbow"][1] * h),
                },
            )
            masks["hand_hip"] = masks["arm_forearm_hip"].copy()
            recut_joint(
                masks,
                {
                    "upper": "arm_forearm_hip",
                    "lower": "hand_hip",
                    "root": (joints["elbow"][0] * w, joints["elbow"][1] * h),
                    "joint": (joints["wrist"][0] * w, joints["wrist"][1] * h),
                },
            )
            if (
                int(masks["arm_upper_hip"].sum()) < 800
                or int(masks["arm_forearm_hip"].sum()) < 200
                or int(masks["hand_hip"].sum()) < 200
            ):
                print("  arm recut degenerate — retry from rembg envelope")
                union = env_arm if env_arm.any() else (
                    masks["arm_upper_hip"] | masks["arm_forearm_hip"] | masks["hand_hip"]
                )
                masks["arm_upper_hip"] = union.copy()
                masks["arm_forearm_hip"] = np.zeros((h, w), bool)
                recut_joint(
                    masks,
                    {
                        "upper": "arm_upper_hip",
                        "lower": "arm_forearm_hip",
                        "root": (joints["shoulder"][0] * w, joints["shoulder"][1] * h),
                        "joint": (joints["elbow"][0] * w, joints["elbow"][1] * h),
                    },
                )
                masks["hand_hip"] = masks["arm_forearm_hip"].copy()
                recut_joint(
                    masks,
                    {
                        "upper": "arm_forearm_hip",
                        "lower": "hand_hip",
                        "root": (joints["elbow"][0] * w, joints["elbow"][1] * h),
                        "joint": (joints["wrist"][0] * w, joints["wrist"][1] * h),
                    },
                )
                how = "rembg∩envelope arm_hip (retry)"
            origin["arm_upper_hip"] = how + " + elbow recut"
            origin["arm_forearm_hip"] = how + " + elbow/wrist recut"
            origin["hand_hip"] = how + " + wrist recut"

    def covers_uv(mask: np.ndarray, uv, r: int = 24) -> bool:
        x, y = int(uv[0] * w), int(uv[1] * h)
        return bool(mask[max(0, y - r) : y + r + 1, max(0, x - r) : x + r + 1].any())

    # sibling splits sometimes dump the whole raised leg onto thigh
    if "leg_raise_thigh" in masks and covers_uv(masks["leg_raise_thigh"], joints["ankle_raise"]):
        print("  leg_raise_thigh plate covers ankle — using it as raised-leg union")
        whole_leg = masks["leg_raise_thigh"]
        masks["leg_raise_calf"] = whole_leg.copy()
        masks["foot_raise"] = np.zeros((h, w), bool)
        origin["leg_raise_thigh"] = origin.get("leg_raise_thigh", "") + " (whole-leg union)"

    # raised-leg chain: clean polygon overlap at knee / ankle
    if all(n in masks and masks[n].any() for n in ("leg_raise_thigh", "leg_raise_calf")):
        recut_joint(
            masks,
            {
                "upper": "leg_raise_thigh",
                "lower": "leg_raise_calf",
                "root": (joints["hip_raise"][0] * w, joints["hip_raise"][1] * h),
                "joint": (joints["knee_raise"][0] * w, joints["knee_raise"][1] * h),
            },
        )
    if all(n in masks and masks[n].any() for n in ("leg_raise_calf", "foot_raise")):
        recut_joint(
            masks,
            {
                "upper": "leg_raise_calf",
                "lower": "foot_raise",
                "root": (joints["knee_raise"][0] * w, joints["knee_raise"][1] * h),
                "joint": (joints["ankle_raise"][0] * w, joints["ankle_raise"][1] * h),
            },
        )

    # drop empty optional plates before seam close so they do not claim leftovers
    for name in list(masks):
        if name in OPTIONAL and not masks[name].any():
            print(f"  drop empty optional {name}")
            del masks[name]

    order = [p[0] for p in PARTS if p[0] in masks and masks[p[0]].any()]
    if not order:
        raise SystemExit("no plates to export — need elevator-red layers or a rembg body")

    # ===== seam close: hand every unowned opaque pixel to its nearest plate =====
    union = np.logical_or.reduce([masks[k] for k in order])
    leftover = fig & ~union
    print(f"seam pixels before close: {int(leftover.sum())}")
    fill_counts = {k: 0 for k in order}
    max_fill_dist = 0.0
    if leftover.any():
        owner = np.zeros((h, w), np.int32)
        for i, name in enumerate(order):
            owner[masks[name] & (owner == 0)] = i + 1
        dist, (iy, ix) = ndi.distance_transform_edt(~union, return_indices=True)
        near = owner[iy, ix]
        max_fill_dist = float(dist[leftover].max())
        for i, name in enumerate(order):
            add = leftover & (near == i + 1)
            if add.any():
                masks[name] = masks[name] | add
                fill_counts[name] = int(add.sum())
    print(f"seam close: max distance {max_fill_dist:.1f}px, assigned {sum(fill_counts.values())}")

    # ===== pivots =====
    parts_by_name = {p[0]: p for p in PARTS}
    pivots: dict[str, dict] = {}
    parents: dict[str, str | None] = {}
    for i, name in enumerate(order):
        _n, pid, src, parent, spec = parts_by_name[name]
        if parent is not None and parent not in masks:
            parent = None
        cx, cy, override, note = solve_pivot(name, spec, masks, src_masks)
        if parent == "auto":
            parent = override or None
        parents[name] = parent
        pivots[name] = {
            "part_id": pid,
            "draw_index": i,
            "parent": parent,
            "pivot_px": [round(cx, 1), round(cy, 1)],
            "pivot_uv": [round(cx / w, 5), round(cy / h, 5)],
            "rule": note,
            "bbox": bbox(masks[name]),
            "centroid_px": [round(v, 1) for v in centroid(masks[name])],
            "opaque_px": int(masks[name].sum()),
            "source": origin.get(name, src),
        }

    # ===== write plates =====
    z = np.zeros((h, w, 4), np.uint8)
    for name in order:
        m = masks[name]
        plate = z.copy()
        plate[m] = rgba[m]
        Image.fromarray(plate).save(OUT / f"{name}.png")
        print(f"  {name:22s} {int(m.sum()):7d}  (+{fill_counts[name]} seam)")

    comp = z.copy()
    for name in order:
        m = masks[name]
        comp[m] = rgba[m]
    Image.fromarray(comp).save(QA / "composite.png")
    diff = np.abs(comp.astype(np.int16) - rgba.astype(np.int16)).sum(axis=2)
    residual = int((fig & (comp[:, :, 3] <= ALPHA_ON)).sum())
    print(f"composite: max diff {int(diff.max())}, mean on opaque {float(diff[fig].mean()):.4f}, unowned {residual}")
    Image.fromarray(np.clip(diff, 0, 255).astype(np.uint8)).save(QA / "composite_diff.png")

    print("\npivots (back -> front):")
    for name in order:
        p = pivots[name]
        print(f"  {name:22s} parent={str(p['parent']):20s} {str(p['pivot_px']):16s} {p['rule']}")

    write_contact_sheet(OUT, order, QA / "contact-sheet.jpg")
    write_pivot_overlay(rgba, pivots, QA / "pivots-overlay.png")

    puppet_uv = {name: pivots[name]["pivot_uv"] for name in order}
    if "face" in puppet_uv:
        puppet_uv["head"] = puppet_uv["face"]
    if "arm_upper_hip" in puppet_uv:
        puppet_uv["arm_hip"] = puppet_uv["arm_upper_hip"]

    payload = {
        "character": "elevator-red",
        "note": (
            "Seam-closed plates and parent-touch pivots, adapted from "
            "C026 retarget_live2d.py. Pixels are the elevator-red rembg still, "
            "not a C026-to-qipao recolor."
        ),
        "source": str(body_path),
        "profile": profile_name,
        "canvas": {"width": w, "height": h},
        "coordinate_space": "pixels, top-left origin, +y down; pivot_uv = [x/width, y/height]",
        "side_convention": "L/R are character-own (pose_lock.json); raised leg is R, hip-hand is L",
        "root": "torso",
        "parts": pivots,
        "puppet_uv": puppet_uv,
        "size": [w, h],
    }
    # puppet-flat aliases used by web-puppet / layers/pivots.json
    for key, src in (
        ("head", "face"),
        ("chest", "chest"),
        ("torso", "torso"),
        ("dress_body", "dress_body"),
        ("dress_slit_panel", "dress_slit_panel"),
        ("leg_stand", "leg_stand"),
        ("leg_raise_thigh", "leg_raise_thigh"),
        ("leg_raise_calf", "leg_raise_calf"),
        ("foot_raise", "foot_raise"),
        ("arm_hip", "arm_upper_hip"),
        ("back_hair", "back_hair"),
        ("front_hair", "front_hair"),
        ("horns", "horns"),
        ("eyes", "eyes"),
    ):
        if src in pivots:
            payload[key] = pivots[src]["pivot_uv"]

    (OUT / "pivots.json").write_text(json.dumps(payload, indent=2, ensure_ascii=False), encoding="utf-8")
    (OUT / "parts.json").write_text(
        json.dumps(
            {
                "character": "elevator-red",
                "source_body": str(body_path),
                "source_layers": str(LAYERS),
                "not_c026_recolor": True,
                "canvas": {"width": w, "height": h},
                "plate_space": "every PNG is full canvas, so parts align by direct overlay — no offsets needed",
                "draw_order_back_to_front": order,
                "parts": [
                    {
                        "name": name,
                        "part_id": parts_by_name[name][1],
                        "draw_index": i,
                        "parent": parents[name],
                        "opaque_px": pivots[name]["opaque_px"],
                        "seam_px_added": fill_counts[name],
                        "bbox": pivots[name]["bbox"],
                        "source": origin.get(name, ""),
                    }
                    for i, name in enumerate(order)
                ],
                "qa": {
                    "seam_px_before_close": int(leftover.sum()),
                    "seam_max_fill_distance_px": round(max_fill_dist, 2),
                    "unowned_opaque_px_after_close": residual,
                    "composite_max_channel_sum_diff": int(diff.max()),
                    "composite_mean_diff_on_opaque": round(float(diff[fig].mean()), 5),
                },
            },
            indent=2,
            ensure_ascii=False,
        ),
        encoding="utf-8",
    )
    print("wrote", OUT)


if __name__ == "__main__":
    main()
