# -*- coding: utf-8 -*-
"""Crop official catalog card icons from Memorial library grids."""
from __future__ import annotations

import json
import os

import numpy as np
from PIL import Image

SHOTS = r"F:\天命之子\docs\reference\mobile-archive\screenshots"
OUT = r"F:\天命之子\天命之子数据\icons"
META = r"F:\天命之子\天命之子数据\_extract\catalog_icons.jsonl"

# page order = encyclopedia order (hash-dedupe overlapping scrolls)
PAGES = {
    "5": [
        "080_archive.png",
        "161_charlib_p01.png",
        "161_charlib_p02.png",
        "161_charlib_p03.png",
        "161_charlib_p04.png",
        "161_charlib_p05.png",
        "161_charlib_p06.png",
        "083_archive_scroll_01.png",
        "083_archive_scroll_02.png",
    ],
    "4": [
        "087_archive_4star.png",
        "162_charlib_4star.png",
        "163_charlib_4s_p01.png",
        "163_charlib_4s_p02.png",
        "163_charlib_4s_p03.png",
        "163_charlib_4s_p04.png",
    ],
    "3": [
        "164_charlib_3star.png",
        "165_charlib_3s_p01.png",
        "165_charlib_3s_p02.png",
        "165_charlib_3s_p03.png",
    ],
    "2": ["166_charlib_2star.png"],
    "1": ["350_charlib_1star.png"],
}

X0, Y0, DX, DY, SIDE = 28, 778, 193, 218, 152
ROWS, COLS = 10, 6
EXPECT = {"5": 282, "4": 77, "3": 99, "2": 53, "1": 50}


def empty(im: Image.Image) -> bool:
    a = np.array(im.convert("L"), dtype=np.float32)
    return float(a.mean()) < 18 or float(a.std()) < 8


def thumb(im: Image.Image):
    return np.array(im.resize((24, 24)), dtype=np.float32)


def too_similar(a: np.ndarray, b: np.ndarray) -> bool:
    return float(np.mean((a - b) ** 2)) < 40


def main() -> None:
    os.makedirs(OUT, exist_ok=True)
    rows = []
    for rar, files in PAGES.items():
        prev_thumbs = []
        seq = 0
        for fn in files:
            path = os.path.join(SHOTS, fn)
            if not os.path.isfile(path):
                print("missing", fn)
                continue
            im = Image.open(path).convert("RGB")
            w, h = im.size
            n_new = 0
            page_thumbs = []
            for r in range(ROWS):
                for c in range(COLS):
                    x = X0 + c * DX
                    y = Y0 + r * DY
                    if y >= h - 90 or x >= w - 90:
                        continue
                    x2 = min(x + SIDE, w - 6)
                    y2 = min(y + SIDE, h - 6)
                    if (x2 - x) < 120 or (y2 - y) < 120:
                        continue
                    tile = im.crop((x, y, x2, y2)).resize((SIDE, SIDE), Image.Resampling.BILINEAR)
                    if empty(tile):
                        continue
                    th = thumb(tile)
                    if any(too_similar(th, old) for old in prev_thumbs):
                        continue
                    fname = "R%s_%03d.png" % (rar, seq)
                    tile.save(os.path.join(OUT, fname))
                    rows.append({"rarity_key": rar, "idx": seq, "file": "icons/" + fname, "source": fn})
                    page_thumbs.append(th)
                    seq += 1
                    n_new += 1
            prev_thumbs.extend(page_thumbs)
            print("page", fn, "new", n_new, "seq", seq, flush=True)
        print("rarity", rar, "got", seq, "expect", EXPECT[rar], flush=True)
        if seq > EXPECT[rar]:
            print("  trimming extras", seq - EXPECT[rar], flush=True)
    with open(META, "w", encoding="utf-8") as f:
        for r in rows:
            f.write(json.dumps(r, ensure_ascii=False) + "\n")
    print("DONE", len(rows), META)


if __name__ == "__main__":
    main()
