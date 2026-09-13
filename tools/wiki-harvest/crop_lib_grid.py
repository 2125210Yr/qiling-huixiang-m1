# -*- coding: utf-8 -*-
"""Crop Child portraits from Memorial CHARACTER LIBRARY grid screenshots."""
from __future__ import annotations

import hashlib
import json
import os
import re
import sys
import time

import numpy as np
from PIL import Image
from rapidocr_onnxruntime import RapidOCR
from zhconv import convert as zh_convert

SHOTS = r"F:\天命之子\docs\reference\mobile-archive\screenshots"
OUT_DIR = r"F:\天命之子\天命之子数据\_extract"
AV_DIR = r"F:\天命之子\天命之子数据\avatars_561"

# Grid pages that actually show the encyclopedia (not quit dialogs).
PAGES = {
    "5": [
        "080_archive.png",
        "082_archive_reopen.png",
        "083_archive_scroll_01.png",
        "083_archive_scroll_02.png",
        "160_charlib.png",
        "161_charlib_p01.png",
        "161_charlib_p02.png",
        "161_charlib_p03.png",
        "161_charlib_p04.png",
        "161_charlib_p05.png",
        "161_charlib_p06.png",
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
    "2": [
        "166_charlib_2star.png",
        "167_charlib_1star.png",
    ],
    "1": [
        "350_charlib_1star.png",
    ],
}

CJK_SPACE = re.compile(r"(?<=[\u4e00-\u9fff])\s+(?=[\u4e00-\u9fff])")
SKIP_WORDS = (
    "LIBRARY", "CHARACTER", "Child", "魂之歌牌", "人偶", "造型",
    "变更顺序", "變更順序", "要离开", "要離開",
)


def simp(s: str) -> str:
    s = CJK_SPACE.sub("", s or "")
    return zh_convert(re.sub(r"\s+", "", s), "zh-cn")


def bbox(pts):
    xs = [p[0] for p in pts]
    ys = [p[1] for p in pts]
    return min(xs), min(ys), max(xs), max(ys)


def phash(im: Image.Image) -> str:
    g = im.convert("L").resize((16, 16))
    arr = np.array(g, dtype=np.int32)
    avg = arr.mean()
    bits = (arr > avg).flatten()
    return hashlib.md5(bits.tobytes()).hexdigest()


def main() -> int:
    os.makedirs(AV_DIR, exist_ok=True)
    os.makedirs(OUT_DIR, exist_ok=True)
    ocr = RapidOCR()
    rows = []
    seen_hash = set()
    t0 = time.time()
    for rar, files in PAGES.items():
        for fn in files:
            path = os.path.join(SHOTS, fn)
            if not os.path.isfile(path):
                print("missing", fn, flush=True)
                continue
            im = Image.open(path).convert("RGB")
            w, h = im.size
            res, _ = ocr(np.array(im))
            if not res:
                print("no-ocr", fn, flush=True)
                continue
            names = []
            for item in res:
                pts, text, _conf = item[0], item[1], item[2] if len(item) > 2 else 0
                t = simp(str(text))
                if not t or len(t) < 1:
                    continue
                if any(k in t for k in SKIP_WORDS):
                    continue
                if re.fullmatch(r"[\d./+★☆]+", t):
                    continue
                if not re.search(r"[\u4e00-\u9fffA-Za-z]", t):
                    continue
                x0, y0, x1, y1 = bbox(pts)
                # names sit under tiles, not in the top chrome
                if y0 < 780 or y0 > h - 80:
                    continue
                if (x1 - x0) > 280 or (y1 - y0) > 80:
                    continue
                names.append((y0, x0, x1, y1, t))
            names.sort()
            n_ok = 0
            for y0, x0, x1, y1, t in names:
                cx = 0.5 * (x0 + x1)
                side = max(140, min(190, int((x1 - x0) * 2.2)))
                left = int(cx - side / 2)
                top = int(y0 - side - 6)
                left = max(0, min(left, w - side))
                top = max(0, min(top, h - side))
                crop = im.crop((left, top, left + side, top + side))
                if crop.size[0] < 80:
                    continue
                hx = phash(crop)
                if hx in seen_hash:
                    continue
                seen_hash.add(hx)
                fname = "g%s_%03d_%s.png" % (rar, len(rows), re.sub(r"[^\w\u4e00-\u9fff]+", "", t)[:16] or "x")
                dest = os.path.join(AV_DIR, fname)
                crop.save(dest)
                rows.append({
                    "rarity_key": rar,
                    "name_grid": t,
                    "file": "avatars_561/" + fname,
                    "source": fn,
                    "box": [left, top, left + side, top + side],
                })
                n_ok += 1
            print("page", fn, "names", len(names), "new", n_ok, "total", len(rows), flush=True)
    outp = os.path.join(OUT_DIR, "grid_avatars.jsonl")
    with open(outp, "w", encoding="utf-8") as f:
        for r in rows:
            f.write(json.dumps(r, ensure_ascii=False) + "\n")
    print("DONE avatars", len(rows), "in", "%.0fs" % (time.time() - t0), outp, flush=True)
    return 0


if __name__ == "__main__":
    sys.exit(main())
