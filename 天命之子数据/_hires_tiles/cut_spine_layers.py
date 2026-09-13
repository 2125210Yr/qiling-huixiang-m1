"""Cut C001 preview_new into BD2-style parts. Same canvas, RGBA."""
from __future__ import annotations

from pathlib import Path
import json
import numpy as np
from PIL import Image

SRC = Path(r"F:\天命之子\client\Assets\Resources\Art\Characters\C001\preview_new.png")
OUT = Path(r"F:\天命之子\client\Assets\Resources\Art\Characters\C001\SpineLayers")
PREV = Path(r"F:\天命之子\天命之子数据\_hires_tiles")


def rgb_to_hsv(r, g, b):
    mx = np.maximum(np.maximum(r, g), b)
    mn = np.minimum(np.minimum(r, g), b)
    df = mx - mn + 1e-6
    h = np.zeros_like(mx)
    mask = mx == r
    h[mask] = np.mod((g[mask] - b[mask]) / df[mask], 6.0)
    mask = mx == g
    h[mask] = (b[mask] - r[mask]) / df[mask] + 2.0
    mask = mx == b
    h[mask] = (r[mask] - g[mask]) / df[mask] + 4.0
    h = h * 60.0
    s = np.where(mx > 1e-5, df / (mx + 1e-6), 0.0)
    v = mx
    return h, s, v


def save_layer(base, mask, name):
    out = np.zeros_like(base)
    m = mask[..., None]
    out[..., :3] = np.where(m, base[..., :3], 0)
    out[..., 3] = np.where(mask, base[..., 3], 0)
    Image.fromarray(out, "RGBA").save(OUT / name)
    vis = np.zeros((*mask.shape, 3), np.uint8)
    vis[mask] = (255, 80, 180)
    Image.fromarray(vis).save(PREV / f"mask_{name}")
    print(name, int(mask.sum()), "px")


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    im = Image.open(SRC).convert("RGBA")
    a = np.array(im)
    hgt, wdt = a.shape[:2]
    rgb = a[..., :3].astype(np.float32) / 255.0
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    hh, ss, vv = rgb_to_hsv(r, g, b)
    alpha = a[..., 3] > 18
    yy, xx = np.mgrid[0:hgt, 0:wdt]
    xn, yn = xx / (wdt - 1.0), yy / (hgt - 1.0)

    mint = alpha & (hh > 140) & (hh < 185) & (ss > 0.14) & (vv > 0.28)
    plat = alpha & (ss < 0.22) & (vv > 0.58) & ~((xn > 0.38) & (xn < 0.74) & (yn > 0.18) & (yn < 0.50))
    hair_col = mint | (plat & alpha)

    # Outer locks only. Scalp / bangs / ponytail root stay on the body still.
    left_lock = hair_col & (xn < 0.42) & (yn > 0.16) & (yn < 0.78)
    right_lock = hair_col & (xn > 0.60) & (yn > 0.22) & (yn < 0.78)
    mid_mint = mint & (xn > 0.18) & (xn < 0.48) & (yn > 0.32) & (yn < 0.70)

    hair_back = left_lock & ~right_lock
    hair_front = right_lock
    hair_tip = mid_mint & ~hair_front

    # Blade: silvery, left of the hip, not mint locks.
    sword = alpha & (xn > 0.10) & (xn < 0.40) & (yn > 0.44) & (yn < 0.90)
    sword = sword & (b > 0.42) & (np.abs(r - g) < 0.18) & (ss < 0.35) & ~mint
    sword = sword & ~((xn > 0.36) & (yn > 0.72))
    hair_back = hair_back & ~sword
    hair_front = hair_front & ~sword
    hair_tip = hair_tip & ~sword

    moving = hair_back | hair_front | hair_tip | sword
    body_only = alpha & ~moving
    # never punch holes in the face / bust
    core = (xn > 0.38) & (xn < 0.74) & (yn > 0.10) & (yn < 0.58)
    body_only = body_only | (alpha & core & ~sword)

    save_layer(a, body_only, "layer_body.png")
    save_layer(a, hair_back, "layer_hair_back.png")
    save_layer(a, hair_front, "layer_hair_front.png")
    save_layer(a, hair_tip, "layer_hair_tip.png")
    save_layer(a, sword, "layer_sword.png")

    def centroid(mask):
        ys, xs = np.where(mask)
        if len(xs) == 0:
            return [0.5, 0.5]
        # Unity UV origin bottom-left
        return [float(xs.mean() / (wdt - 1)), float(1.0 - ys.mean() / (hgt - 1))]

    # pivots: scalp / hilt not centroid of whole lock
    piv = {
        "hairBack": [0.48, 0.78],
        "hairFront": [0.58, 0.74],
        "hairTip": [0.40, 0.70],
        "sword": [0.32, 0.46],
        "counts": {
            "body": int(body_only.sum()),
            "hairBack": int(hair_back.sum()),
            "hairFront": int(hair_front.sum()),
            "hairTip": int(hair_tip.sum()),
            "sword": int(sword.sum()),
        },
        "centroids": {
            "hairBack": centroid(hair_back),
            "hairFront": centroid(hair_front),
            "hairTip": centroid(hair_tip),
            "sword": centroid(sword),
        },
    }
    (OUT / "pivots.json").write_text(json.dumps(piv, indent=2), encoding="utf-8")
    print(json.dumps(piv, indent=2))


if __name__ == "__main__":
    main()
