# -*- coding: utf-8 -*-
"""OCR short names under catalog tiles; save named icons for matching."""
from __future__ import annotations

import json
import os
import re

import numpy as np
from PIL import Image
from rapidocr_onnxruntime import RapidOCR
from zhconv import convert as zh_convert

SHOTS = r"F:\天命之子\docs\reference\mobile-archive\screenshots"
OUT = r"F:\天命之子\天命之子数据\icons"
META = r"F:\天命之子\天命之子数据\_extract\named_icons.jsonl"

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
}

X0, Y0, DX, DY, SIDE = 28, 778, 193, 218, 152
ROWS, COLS = 10, 6
CJK = re.compile(r"(?<=[\u4e00-\u9fff])\s+(?=[\u4e00-\u9fff])")
SKIP = ("LIBRARY", "CHARACTER", "Child", "变更", "變更", "561")


def simp(s: str) -> str:
    s = CJK.sub("", s or "")
    s = zh_convert(re.sub(r"\s+", "", s), "zh-cn")
    return s


def empty(im: Image.Image) -> bool:
    a = np.array(im.convert("L"), dtype=np.float32)
    return float(a.mean()) < 18 or float(a.std()) < 8


def thumb(im: Image.Image):
    return np.array(im.resize((24, 24)), dtype=np.float32)


def too_similar(a, b) -> bool:
    return float(np.mean((a - b) ** 2)) < 40


def main():
    os.makedirs(OUT, exist_ok=True)
    ocr = RapidOCR()
    rows = []
    for rar, files in PAGES.items():
        prev = []
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
                    if any(too_similar(th, old) for old in prev):
                        continue
                    ny0 = min(y + SIDE - 6, h - 40)
                    ny1 = min(y + DY - 2, h - 8)
                    name = ""
                    if ny1 > ny0 + 10:
                        strip = im.crop((x, ny0, min(x + SIDE, w - 6), ny1))
                        res, _ = ocr(np.array(strip))
                        if res:
                            texts = [simp(str(it[1])) for it in res if it and it[1]]
                            texts = [t for t in texts if t and t not in ("S", "U", "P", "E", "C")]
                            texts = [t for t in texts if re.search(r"[\u4e00-\u9fffA-Za-z]", t)]
                            texts = [t for t in texts if not any(k in t for k in SKIP)]
                            if texts:
                                name = texts[0]
                    fname = "N%s_%03d.png" % (rar, seq)
                    tile.save(os.path.join(OUT, fname))
                    rows.append({
                        "rarity_key": rar,
                        "seq": seq,
                        "name_grid": name,
                        "file": "icons/" + fname,
                        "source": fn,
                    })
                    page_thumbs.append(th)
                    seq += 1
                    n_new += 1
            prev.extend(page_thumbs)
            print("page", fn, "new", n_new, "seq", seq, "named", sum(1 for x in rows if x["rarity_key"]==rar and x["name_grid"]), flush=True)
        print("rarity", rar, "total", seq, flush=True)
    with open(META, "w", encoding="utf-8") as f:
        for r in rows:
            f.write(json.dumps(r, ensure_ascii=False) + "\n")
    print("DONE", len(rows), META)


if __name__ == "__main__":
    main()
