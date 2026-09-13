# -*- coding: utf-8 -*-
"""Recrop 2-star Memorial library card faces from 166_charlib_2star.png."""
from __future__ import annotations

import glob
import hashlib
import json
import os

import numpy as np
from PIL import Image

SHOT = r"F:\天命之子\docs\reference\mobile-archive\screenshots\166_charlib_2star.png"
OUT = r"F:\天命之子\天命之子数据\icons"
META = r"F:\天命之子\天命之子数据\_layout\04_recrop_2star.jsonl"
SHEET = r"F:\天命之子\天命之子数据\_layout\04_sheet.png"
PREFIX = "F2_"
EXPECT = 53
COLS = 6

# crop_catalog_icons.py: 28, 778, 193, 218, 152
# That Y0 is the gutter under the ★★ / 53/53 rule (cards start ~810).
# Pitch 193x218 drifts off this page's 180x190 lattice, so later rows
# hit names / skip the last five 噗噗. Measured face grid:
X0, Y0, DX, DY, SIDE = 85, 810, 180, 190, 152


def empty(im: Image.Image) -> bool:
    a = np.array(im.convert("L"), dtype=np.float32)
    return float(a.mean()) < 18 or float(a.std()) < 8


def tile_hash(im: Image.Image) -> str:
    g = np.array(im.resize((16, 16), Image.Resampling.BILINEAR), dtype=np.uint8)
    return hashlib.md5(g.tobytes()).hexdigest()


def contact_sheet(tiles: list[Image.Image], cols: int = COLS, gap: int = 4) -> Image.Image:
    n = len(tiles)
    rows = (n + cols - 1) // cols
    sheet = Image.new("RGB", (cols * (SIDE + gap) + gap, rows * (SIDE + gap) + gap), (16, 16, 16))
    for i, t in enumerate(tiles):
        r, c = divmod(i, cols)
        sheet.paste(t, (gap + c * (SIDE + gap), gap + r * (SIDE + gap)))
    return sheet


def main() -> None:
    os.makedirs(OUT, exist_ok=True)
    os.makedirs(os.path.dirname(META), exist_ok=True)
    im = Image.open(SHOT).convert("RGB")
    w, h = im.size
    print("shot", w, h, "geo", X0, Y0, DX, DY, SIDE, flush=True)

    for old in glob.glob(os.path.join(OUT, PREFIX + "*.png")):
        os.remove(old)

    seen: set[str] = set()
    rows_meta: list[dict] = []
    tiles: list[Image.Image] = []
    skipped_empty = skipped_partial = skipped_dup = 0
    r = 0
    while True:
        y = Y0 + r * DY
        if y + SIDE > h - 8:
            skipped_partial += COLS
            print("stop partial row", r, "y", y, "y+SIDE", y + SIDE, "lim", h - 8, flush=True)
            break
        for c in range(COLS):
            x = X0 + c * DX
            if x + SIDE > w:
                skipped_partial += 1
                continue
            tile = im.crop((x, y, x + SIDE, y + SIDE))
            if empty(tile):
                skipped_empty += 1
                print("empty r,c,x,y", r, c, x, y, flush=True)
                continue
            hx = tile_hash(tile)
            if hx in seen:
                skipped_dup += 1
                print("dup r,c,x,y", r, c, x, y, flush=True)
                continue
            seen.add(hx)
            idx = len(rows_meta)
            fname = "%s%03d.png" % (PREFIX, idx)
            tile.save(os.path.join(OUT, fname))
            tiles.append(tile)
            rows_meta.append({
                "rarity_key": "2",
                "idx": idx,
                "file": "icons/" + fname,
                "source": os.path.basename(SHOT),
                "row": r,
                "col": c,
                "x": x,
                "y": y,
                "side": SIDE,
                "hash": hx,
            })
        r += 1

    with open(META, "w", encoding="utf-8") as f:
        for rec in rows_meta:
            f.write(json.dumps(rec, ensure_ascii=False) + "\n")
    contact_sheet(tiles).save(SHEET)
    n = len(rows_meta)
    print(
        "got", n, "expect", EXPECT,
        "empty", skipped_empty, "partial", skipped_partial, "dup", skipped_dup,
        flush=True,
    )
    print("meta", META, flush=True)
    print("sheet", SHEET, flush=True)
    if n != EXPECT:
        raise SystemExit("count mismatch")


if __name__ == "__main__":
    main()
