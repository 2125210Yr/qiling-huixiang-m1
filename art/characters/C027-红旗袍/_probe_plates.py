"""Crop each plate to its own bbox and tile them with labels, so a plate that
holds the wrong body part is obvious at a glance."""
from __future__ import annotations

import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent
LAYERS = ROOT / "layers"
OUT = ROOT / "_probe" / "plates_labeled.png"
TILE = 300


def main() -> None:
    names = sys.argv[1:] or [p.stem for p in sorted(LAYERS.glob("*.png")) if p.stem != "composite"]
    tiles = []
    try:
        font = ImageFont.truetype("arial.ttf", 20)
    except OSError:
        font = ImageFont.load_default()

    for name in names:
        p = LAYERS / f"{name}.png"
        if not p.exists():
            continue
        arr = np.asarray(Image.open(p).convert("RGBA"))
        m = arr[:, :, 3] > 40
        if not m.any():
            continue
        ys, xs = np.where(m)
        crop = arr[ys.min() : ys.max() + 1, xs.min() : xs.max() + 1]
        a = crop[:, :, 3:4].astype(np.float32) / 255.0
        vis = (crop[:, :, :3].astype(np.float32) * a + 30 * (1 - a)).astype(np.uint8)
        im = Image.fromarray(vis)
        im.thumbnail((TILE - 8, TILE - 30), Image.Resampling.LANCZOS)
        tile = Image.new("RGB", (TILE, TILE), (16, 16, 16))
        tile.paste(im, ((TILE - im.width) // 2, 26 + (TILE - 30 - im.height) // 2))
        d = ImageDraw.Draw(tile)
        d.text((6, 4), f"{name}  {int(m.sum())}px  {crop.shape[1]}x{crop.shape[0]}",
               fill=(0, 255, 170), font=font)
        tiles.append(tile)

    cols = 4
    rows = (len(tiles) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * TILE, rows * TILE), (0, 0, 0))
    for i, t in enumerate(tiles):
        r, c = divmod(i, cols)
        sheet.paste(t, (c * TILE, r * TILE))
    OUT.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(OUT)
    print(OUT, sheet.size, len(tiles), "plates")


if __name__ == "__main__":
    main()
