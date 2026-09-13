"""Probe the elevator reference frame: crop letterbox, upscale, dump coordinate grid + color masks.

Read-only w.r.t. the source. Writes only into _probe/ so split_layers.py can be tuned
against real pixel coordinates instead of guesses.
"""
from __future__ import annotations

import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent
SRC = ROOT / "reference-src.jpg"
WORK = ROOT / "reference-work.png"
OUT = ROOT / "_probe"

SCALE = 4  # 546x292 is too small to trace; work at 4x with LANCZOS

# Work-space boxes, read off grid50.png.
ZOOMS = {
    "head": (760, 40, 1180, 420),
    "torso_arms": (900, 300, 1360, 640),
    "raised_leg": (520, 380, 1020, 1060),
    "hem_legs": (900, 700, 1300, 1168),
}

# Work-space probe points, one per material we must separate.
SAMPLES = {
    "hair_crown": (950, 130),
    "hair_side_r": (1060, 250),
    "face_skin": (955, 265),
    "chest_skin": (960, 400),
    "thigh_raised": (760, 500),
    "foot_top": (620, 930),
    "leg_standing": (1060, 950),
    "arm_upper_hip": (1250, 420),
    "dress_bright": (1060, 600),
    "dress_dark_chest": (1030, 430),
    "bg_door_red": (1300, 250),
    "bg_door_dark": (1500, 700),
    "bg_panel": (1900, 300),
    "bg_left_wall": (250, 500),
    "bg_between_legs": (940, 950),
}


def letterbox_crop(rgb: np.ndarray) -> tuple[int, int, int, int]:
    """Trim the near-black video bars. Returns (x0, y0, x1, y1)."""
    luma = rgb.astype(np.float32).mean(axis=2)
    rows = np.where(luma.mean(axis=1) > 12)[0]
    cols = np.where(luma.mean(axis=0) > 12)[0]
    if len(rows) == 0 or len(cols) == 0:
        return 0, 0, rgb.shape[1], rgb.shape[0]
    return int(cols[0]), int(rows[0]), int(cols[-1]) + 1, int(rows[-1]) + 1


def grid(img: Image.Image, step: int, path: Path) -> None:
    vis = img.convert("RGB").copy()
    d = ImageDraw.Draw(vis, "RGBA")
    try:
        font = ImageFont.truetype("arial.ttf", 13)
    except OSError:
        font = ImageFont.load_default()
    w, h = vis.size
    for x in range(0, w, step):
        heavy = x % (step * 5) == 0
        d.line([(x, 0), (x, h)], fill=(0, 255, 255, 200 if heavy else 70), width=2 if heavy else 1)
        if heavy:
            d.text((x + 3, 3), str(x), fill=(0, 255, 255), font=font)
    for y in range(0, h, step):
        heavy = y % (step * 5) == 0
        d.line([(0, y), (w, y)], fill=(255, 255, 0, 200 if heavy else 70), width=2 if heavy else 1)
        if heavy:
            d.text((3, y + 3), str(y), fill=(255, 255, 0), font=font)
    vis.save(path)


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    src = Image.open(SRC).convert("RGB")
    arr = np.asarray(src)
    x0, y0, x1, y1 = letterbox_crop(arr)
    crop = src.crop((x0, y0, x1, y1))
    work = crop.resize((crop.width * SCALE, crop.height * SCALE), Image.Resampling.LANCZOS)
    work.save(WORK)

    a = np.asarray(work).astype(np.int16)
    r, g, b = a[:, :, 0], a[:, :, 1], a[:, :, 2]
    luma = a.mean(axis=2)
    chroma = a.max(axis=2) - a.min(axis=2)
    h, w = luma.shape

    # Skin reads far brighter than anything else in this frame.
    skin = (luma > 92) & (r > g + 14) & (g >= b - 6) & (chroma > 18)
    # Cheongsam silk: saturated red, darker than skin.
    dress = (r > b + 40) & (r > g + 40) & (luma < 120) & (r > 55)
    # Hair / horns: near-neutral dark with a faint blue cast.
    hair = (luma < 70) & (b >= r - 10) & (chroma < 55)

    for name, m in (("skin", skin), ("dress", dress), ("hair", hair)):
        Image.fromarray((m.astype(np.uint8) * 255)).save(OUT / f"mask_{name}.png")

    tint = np.asarray(work).copy()
    tint[skin] = (tint[skin] * 0.35 + np.array([0, 255, 90]) * 0.65).astype(np.uint8)
    tint[dress] = (tint[dress] * 0.35 + np.array([255, 60, 255]) * 0.65).astype(np.uint8)
    tint[hair] = (tint[hair] * 0.35 + np.array([80, 160, 255]) * 0.65).astype(np.uint8)
    Image.fromarray(tint).save(OUT / "mask_overlay.png")

    grid(work, 50, OUT / "grid50.png")
    grid(Image.fromarray(tint), 50, OUT / "grid50_masks.png")

    # Zoom tiles: the frame is only 546px wide, so tracing needs 2x on top of SCALE.
    for name, (zx0, zy0, zx1, zy1) in ZOOMS.items():
        tile = work.crop((zx0, zy0, zx1, zy1))
        tile = tile.resize((tile.width * 2, tile.height * 2), Image.Resampling.LANCZOS)
        vis = tile.convert("RGB")
        d = ImageDraw.Draw(vis, "RGBA")
        try:
            font = ImageFont.truetype("arial.ttf", 14)
        except OSError:
            font = ImageFont.load_default()
        for wx in range(zx0 - zx0 % 25 + 25, zx1, 25):
            px = (wx - zx0) * 2
            heavy = wx % 100 == 0
            d.line([(px, 0), (px, vis.height)], fill=(0, 255, 255, 210 if heavy else 60), width=2 if heavy else 1)
            if heavy:
                d.text((px + 3, 3), str(wx), fill=(0, 255, 255), font=font)
        for wy in range(zy0 - zy0 % 25 + 25, zy1, 25):
            py = (wy - zy0) * 2
            heavy = wy % 100 == 0
            d.line([(0, py), (vis.width, py)], fill=(255, 255, 0, 210 if heavy else 60), width=2 if heavy else 1)
            if heavy:
                d.text((3, py + 3), str(wy), fill=(255, 255, 0), font=font)
        vis.save(OUT / f"zoom_{name}.png")

    samples = {}
    for name, (sx, sy) in SAMPLES.items():
        patch = a[max(0, sy - 4) : sy + 5, max(0, sx - 4) : sx + 5].reshape(-1, 3)
        samples[name] = {
            "at": [sx, sy],
            "mean_rgb": [round(float(v), 1) for v in patch.mean(axis=0)],
            "luma": round(float(patch.mean()), 1),
            "chroma": round(float((patch.max(axis=1) - patch.min(axis=1)).mean()), 1),
        }

    ys, xs = np.where(skin)
    report = {
        "samples": samples,
        "source_size": [src.width, src.height],
        "letterbox_crop": [x0, y0, x1, y1],
        "work_size": [w, h],
        "scale": SCALE,
        "skin_bbox": [int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max())] if len(xs) else None,
        "counts": {"skin": int(skin.sum()), "dress": int(dress.sum()), "hair": int(hair.sum())},
    }
    (OUT / "probe.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
