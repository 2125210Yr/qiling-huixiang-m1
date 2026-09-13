# -*- coding: utf-8 -*-
"""Geometric 6-col crop of Memorial character library grids."""
from __future__ import annotations

import hashlib
import json
import os
import re

import numpy as np
from PIL import Image
from rapidocr_onnxruntime import RapidOCR
from zhconv import convert as zh_convert

SHOTS = r"F:\天命之子\docs\reference\mobile-archive\screenshots"
OUT_AV = r"F:\天命之子\天命之子数据\avatars_561"
OUT_JS = r"F:\天命之子\天命之子数据\_extract\grid_avatars.jsonl"

PAGES = {
    "5": [
        "080_archive.png", "082_archive_reopen.png",
        "161_charlib_p01.png", "161_charlib_p02.png", "161_charlib_p03.png",
        "161_charlib_p04.png", "161_charlib_p05.png", "161_charlib_p06.png",
        "083_archive_scroll_01.png", "083_archive_scroll_02.png",
    ],
    "4": [
        "087_archive_4star.png", "162_charlib_4star.png",
        "163_charlib_4s_p01.png", "163_charlib_4s_p02.png",
        "163_charlib_4s_p03.png", "163_charlib_4s_p04.png",
    ],
    "3": [
        "164_charlib_3star.png",
        "165_charlib_3s_p01.png", "165_charlib_3s_p02.png", "165_charlib_3s_p03.png",
    ],
    "2": ["166_charlib_2star.png", "167_charlib_1star.png"],
    "1": ["350_charlib_1star.png"],
}

X0, Y0, DX, DY, SIDE = 22, 772, 193, 218, 164
ROWS, COLS = 10, 6
CJK_SPACE = re.compile(r"(?<=[\u4e00-\u9fff])\s+(?=[\u4e00-\u9fff])")


def simp(s):
    s = CJK_SPACE.sub("", s or "")
    return zh_convert(re.sub(r"\s+", "", s), "zh-cn")


def empty(im: Image.Image) -> bool:
    a = np.array(im.convert("L"), dtype=np.float32)
    return float(a.mean()) < 28 or float(a.std()) < 12


def phash(im: Image.Image) -> str:
    g = np.array(im.convert("L").resize((16, 16)), dtype=np.int32)
    return hashlib.md5((g > g.mean()).tobytes()).hexdigest()


def main():
    os.makedirs(OUT_AV, exist_ok=True)
    ocr = RapidOCR()
    rows = []
    seen = set()
    for rar, files in PAGES.items():
        names_seen = set()
        allow_dup_name = rar in ("2", "1")
        for fn in files:
            path = os.path.join(SHOTS, fn)
            if not os.path.isfile(path):
                print("missing", fn)
                continue
            im = Image.open(path).convert("RGB")
            w, h = im.size
            n_new = 0
            for r in range(ROWS):
                for c in range(COLS):
                    x = X0 + c * DX
                    y = Y0 + r * DY
                    if x + SIDE > w or y + SIDE > h - 40:
                        continue
                    tile = im.crop((x, y, x + SIDE, y + SIDE))
                    if empty(tile):
                        continue
                    hx = phash(tile)
                    if hx in seen:
                        continue
                    # name strip under icon
                    ny0 = y + SIDE - 8
                    ny1 = min(h - 8, y + DY - 4)
                    name = ""
                    if ny1 > ny0 + 8:
                        strip = im.crop((x, ny0, x + SIDE, ny1))
                        res, _ = ocr(np.array(strip))
                        if res:
                            texts = [simp(str(it[1])) for it in res if it and it[1]]
                            texts = [t for t in texts if re.search(r"[\u4e00-\u9fffA-Za-z]", t)]
                            texts = [t for t in texts if t not in ("S", "U", "P", "E", "C")]
                            if texts:
                                name = texts[0]
                    key = (rar, name)
                    if name and (not allow_dup_name) and key in names_seen:
                        continue
                    if name:
                        names_seen.add(key)
                    seen.add(hx)
                    fname = "R%s_%03d_%s.png" % (
                        rar, len(rows), re.sub(r"[^\w\u4e00-\u9fff]+", "", name)[:16] or "x",
                    )
                    tile.save(os.path.join(OUT_AV, fname))
                    rows.append({
                        "rarity_key": rar,
                        "name_grid": name,
                        "file": "avatars_561/" + fname,
                        "source": fn,
                    })
                    n_new += 1
            print("page", fn, "new", n_new, "total", len(rows), flush=True)
    with open(OUT_JS, "w", encoding="utf-8") as f:
        for r in rows:
            f.write(json.dumps(r, ensure_ascii=False) + "\n")
    from collections import Counter
    print("DONE", len(rows), dict(Counter(r["rarity_key"] for r in rows)))


if __name__ == "__main__":
    main()
