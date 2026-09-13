"""Strand plates: roots stay on the body, only hanging locks swing."""
from __future__ import annotations

import json
from pathlib import Path

import cv2
import numpy as np
from PIL import Image
from scipy import ndimage

SRC = Path(r"F:\天命之子\client\Assets\Resources\Art\Characters\C001\preview_new.png")
OUT = Path(r"F:\天命之子\client\Assets\Resources\Art\Characters\C001\PreviewLayers")
DBG = Path(r"F:\天命之子\art\characters\C001-焰刃\preview-layers")
MAGENTA = (255, 0, 220)


def save_rgba(name: str, rgb: np.ndarray, alpha: np.ndarray) -> None:
    a = np.clip(alpha * 255.0, 0, 255).astype(np.uint8)
    im = Image.fromarray(np.dstack([rgb, a]), "RGBA")
    OUT.mkdir(parents=True, exist_ok=True)
    DBG.mkdir(parents=True, exist_ok=True)
    im.save(OUT / name)
    im.save(DBG / name)
    bg = np.full_like(rgb, MAGENTA)
    af = (a.astype(np.float32) / 255.0)[..., None]
    prev = (rgb.astype(np.float32) * af + bg.astype(np.float32) * (1.0 - af)).astype(np.uint8)
    Image.fromarray(prev).save(DBG / ("preview_" + name))


def save_mask(name: str, m: np.ndarray) -> None:
    DBG.mkdir(parents=True, exist_ok=True)
    Image.fromarray((m.astype(np.uint8) * 255)).save(DBG / name)


def dist_alpha(mask: np.ndarray, inner: float = 1.6, outer: float = 4.5) -> np.ndarray:
    core = mask.astype(bool)
    dil = ndimage.binary_dilation(core, iterations=max(1, int(round(outer))))
    d_in = ndimage.distance_transform_edt(core)
    d_out = ndimage.distance_transform_edt(~core)
    a = np.zeros(mask.shape, np.float32)
    a[core] = 1.0
    edge = dil & ~core
    a[edge] = np.clip(1.0 - d_out[edge] / max(outer, 0.5), 0.0, 0.9)
    rim = core & (d_in < inner)
    a[rim] = np.clip(0.7 + 0.3 * (d_in[rim] / max(inner, 0.5)), 0.7, 1.0)
    a = ndimage.gaussian_filter(a, 0.7)
    a[core & (d_in >= inner)] = 1.0
    return np.clip(a, 0, 1)


def biggest(m: np.ndarray, min_px: int = 400) -> np.ndarray:
    lab, n = ndimage.label(m)
    if n == 0:
        return m
    sizes = ndimage.sum(m, lab, index=np.arange(1, n + 1))
    keep = np.where(sizes >= max(min_px, float(np.max(sizes)) * 0.04))[0] + 1
    return np.isin(lab, keep)


def top_pivot(mask: np.ndarray) -> list[float]:
    ys, xs = np.where(mask)
    if len(ys) == 0:
        return [0.5, 0.5]
    y_cut = np.percentile(ys, 12)
    band = mask & (np.arange(mask.shape[0])[:, None] <= y_cut + 8)
    bys, bxs = np.where(band)
    if len(bys) == 0:
        bys, bxs = ys, xs
    u = float(bxs.mean() / mask.shape[1])
    v = 1.0 - float(bys.mean() / mask.shape[0])
    return [round(u, 3), round(v, 3)]


def main() -> None:
    rgb = np.array(Image.open(SRC).convert("RGB"))
    h, w = rgb.shape[:2]
    r = rgb[:, :, 0].astype(np.int16)
    g = rgb[:, :, 1].astype(np.int16)
    bch = rgb[:, :, 2].astype(np.int16)
    lum = 0.299 * r + 0.587 * g + 0.114 * bch
    sat = np.maximum(np.maximum(r, g), bch) - np.minimum(np.minimum(r, g), bch)
    hsv = cv2.cvtColor(rgb, cv2.COLOR_RGB2HSV)
    hh, ss, vv = hsv[:, :, 0], hsv[:, :, 1], hsv[:, :, 2]
    yy, xx = np.mgrid[0:h, 0:w]
    nx = xx / float(w)
    ny = yy / float(h)

    mint = ((hh > 32) & (hh < 100) & (ss > 22) & (vv > 55) & (vv < 250)) | (
        (g > r + 6) & (g > 68) & (bch > 48) & (lum > 48) & (lum < 232)
    )
    platinum = ((vv > 145) & (ss < 80) & ((hh < 42) | (hh > 155))) | (
        (lum > 125) & (sat < 100) & (r > 125) & (g > 118) & (r > bch + 6)
    )
    hair_col = (mint | platinum) & (ny < 0.78)

    face = (nx > 0.48) & (nx < 0.72) & (ny > 0.11) & (ny < 0.33)
    bangs = hair_col & face & (nx > 0.50)
    scalp = hair_col & (nx > 0.48) & (nx < 0.64) & (ny > 0.10) & (ny < 0.21)
    sleeve = (lum > 150) & (sat < 48) & (nx > 0.38) & (nx < 0.52) & (ny > 0.26) & (ny < 0.54) & ~mint

    ax, ay, bx, by = 0.40 * w, 0.47 * h, 0.15 * w, 0.88 * h
    abx, aby = bx - ax, by - ay
    ab2 = abx * abx + aby * aby
    tline = np.clip(((xx - ax) * abx + (yy - ay) * aby) / ab2, 0.0, 1.0)
    sword_line = np.sqrt((xx - ax - tline * abx) ** 2 + (yy - ay - tline * aby) ** 2)
    blade = (sword_line < 20) & (ny > 0.46) & (nx < 0.50)

    # Pin the ponytail from the scrunchie down to the shoulder. Shape follows hair, no box.
    still_root = bangs | scalp | (hair_col & (ny < 0.30) & (nx < 0.58) & ~sleeve)
    hang = hair_col & ~bangs & ~scalp & ~sleeve & ~blade & (ny > 0.26) & (ny < 0.78)
    hang = ndimage.binary_closing(hang, iterations=2)

    left = biggest(hang & (nx < 0.54), 600)
    right = biggest(hang & (nx > 0.62), 300)

    outer = biggest((platinum & left) | (ndimage.binary_dilation(platinum & left, iterations=5) & left & mint & (nx < 0.30)), 400)
    inner = biggest((mint & left) | (ndimage.binary_dilation(mint & left, iterations=5) & left & platinum), 400)
    # Overlap the two locks so a small swing does not open a gap.
    outer = outer | (ndimage.binary_dilation(outer, iterations=8) & inner)
    inner = inner | (ndimage.binary_dilation(inner, iterations=8) & outer)

    save_mask("mask_still_root.png", still_root)
    save_mask("mask_outer.png", outer)
    save_mask("mask_inner.png", inner)
    save_mask("mask_front.png", right)

    save_rgba("layer_hair_back.png", rgb, dist_alpha(outer))
    save_rgba("layer_hair_tip.png", rgb, dist_alpha(inner))
    save_rgba("layer_hair_front.png", rgb, dist_alpha(right))

    # Body cutout is made in Photoshop from this still, then stamped below if present.
    Image.fromarray((still_root.astype(np.uint8) * 255)).save(DBG / "mask_hair_sub.png")
    hang_all = left | right
    Image.fromarray((hang_all.astype(np.uint8) * 255)).save(DBG / "mask_hang.png")

    piv = {
        "hairBack": top_pivot(outer),
        "hairTip": top_pivot(inner),
        "hairFront": top_pivot(right),
        "counts": {
            "outer": int(outer.sum()),
            "inner": int(inner.sum()),
            "right": int(right.sum()),
            "still": int(still_root.sum()),
        },
    }
    (OUT / "pivots.json").write_text(json.dumps(piv, indent=2), encoding="utf-8")
    (DBG / "pivots.json").write_text(json.dumps(piv, indent=2), encoding="utf-8")
    print("split", json.dumps(piv))


if __name__ == "__main__":
    main()
