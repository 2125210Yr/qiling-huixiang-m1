"""Joint QA: overlay the exported arm/leg parts on the presenter with a pixel grid.

Used to confirm that the source split's cut lines land on real anatomical joints
before trusting the pivots. Writes into _qa/ only.
"""
from __future__ import annotations

from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

OUT = Path(__file__).resolve().parent
ROOT = OUT.parent
QA = OUT / "_qa"

PRES = np.array(Image.open(ROOT / "presenter.png").convert("RGBA"))
COLORS = [(255, 120, 0), (0, 140, 255), (0, 230, 120), (255, 0, 200), (255, 230, 0)]


def mask(name: str) -> np.ndarray:
    return np.array(Image.open(OUT / f"{name}.png"))[:, :, 3] > 8


def render(parts, x0, x1, y0, y1, out_name, scale=3):
    vis = PRES[:, :, :3].copy()
    vis[PRES[:, :, 3] <= 8] = (255, 255, 255)
    for i, p in enumerate(parts):
        m = mask(p)
        vis[m] = (vis[m] * 0.35 + np.array(COLORS[i % len(COLORS)]) * 0.65).astype(np.uint8)
    crop = Image.fromarray(vis[y0:y1, x0:x1]).resize(((x1 - x0) * scale, (y1 - y0) * scale), Image.NEAREST)
    d = ImageDraw.Draw(crop)
    for x in range(x0 - x0 % 20, x1, 20):
        d.line([((x - x0) * scale, 0), ((x - x0) * scale, (y1 - y0) * scale)], fill=(190, 190, 190))
        d.text(((x - x0) * scale + 2, 2), str(x), fill=(0, 0, 0))
    for y in range(y0 - y0 % 20, y1, 20):
        d.line([(0, (y - y0) * scale), ((x1 - x0) * scale, (y - y0) * scale)], fill=(190, 190, 190))
        d.text((2, (y - y0) * scale + 2), str(y), fill=(0, 0, 0))
    crop.save(QA / out_name)
    for i, p in enumerate(parts):
        m = mask(p)
        ys, xs = np.where(m)
        print(f"  {COLORS[i % len(COLORS)]} {p:16s} {int(m.sum()):6d}px  bbox {xs.min()},{ys.min()} -> {xs.max()},{ys.max()}")


def strip(parts, x0, x1, y0, y1, out_name, scale=3):
    """Clean source crop followed by each plate on its own, same framing."""
    cw, ch = (x1 - x0) * scale, (y1 - y0) * scale
    sheet = Image.new("RGB", (cw * (len(parts) + 1), ch), (255, 255, 255))
    base = PRES[:, :, :3].copy()
    base[PRES[:, :, 3] <= 8] = (255, 255, 255)
    tiles = [Image.fromarray(base[y0:y1, x0:x1])]
    for p in parts:
        m = mask(p)
        v = np.full_like(base, 255)
        v[m] = PRES[:, :, :3][m]
        tiles.append(Image.fromarray(v[y0:y1, x0:x1]))
    for i, t in enumerate(tiles):
        t = t.resize((cw, ch), Image.NEAREST)
        d = ImageDraw.Draw(t)
        for x in range(x0 - x0 % 20, x1, 20):
            d.line([((x - x0) * scale, 0), ((x - x0) * scale, ch)], fill=(215, 215, 215))
            d.text(((x - x0) * scale + 2, 2), str(x), fill=(180, 40, 40))
        for y in range(y0 - y0 % 20, y1, 20):
            d.line([(0, (y - y0) * scale), (cw, (y - y0) * scale)], fill=(215, 215, 215))
            d.text((2, (y - y0) * scale + 2), str(y), fill=(180, 40, 40))
        d.text((6, ch - 20), (["source"] + list(parts))[i], fill=(0, 0, 0))
        sheet.paste(t, (i * cw, 0))
    sheet.save(QA / out_name)


def main() -> None:
    QA.mkdir(parents=True, exist_ok=True)
    strip(["arm_upper_r", "arm_lower_r", "hand_r"], 150, 370, 140, 380, "_strip_arm_r.png", 2)
    print("right arm (raised):")
    render(["arm_upper_r", "arm_lower_r", "hand_r"], 150, 370, 130, 380, "_zoom_arm_r.png")
    print("left arm (clutch):")
    render(["arm_upper_l", "arm_lower_l", "hand_l", "prop_clutch"], 400, 650, 300, 470, "_zoom_arm_l.png")
    print("legs:")
    render(["leg_upper_l", "leg_upper_r", "leg_lower_l", "leg_lower_r"], 240, 480, 380, 740, "_zoom_legs.png", 2)


if __name__ == "__main__":
    main()
