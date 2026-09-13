# -*- coding: utf-8 -*-
"""Fill empty 5/3★ faces, unique-ify duplicate 5★ names from catalog-grid OCR."""
from __future__ import annotations

import csv
import os
import re

import numpy as np
from PIL import Image
from rapidocr_onnxruntime import RapidOCR
from zhconv import convert as zh_convert

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
USER = os.path.join(ROOT, "天命之子数据")
SHOTS = os.path.join(ROOT, "docs", "reference", "mobile-archive", "screenshots")
CSV_CHILD = os.path.join(USER, "汇总表.csv")

PAGES5 = [
    ("080_archive.png", 780),
    ("161_charlib_p01.png", 780),
    ("161_charlib_p02.png", 780),
    ("161_charlib_p03.png", 780),
    ("161_charlib_p04.png", 780),
    ("161_charlib_p05.png", 780),
    ("161_charlib_p06.png", 780),
    ("083_archive_scroll_01.png", 640),
    ("083_archive_scroll_02.png", 640),
]
X0, DX, SIDE, DY = 28, 193, 148, 200
ROWS, COLS = 10, 6


def file_ok(rel: str) -> bool:
    if not rel:
        return False
    p = os.path.join(USER, rel.replace("\\", "/"))
    return os.path.isfile(p) and os.path.getsize(p) > 1500


def thumb(im: Image.Image):
    return np.array(im.resize((24, 24), Image.Resampling.BILINEAR), dtype=np.float32)


def too_similar(a, b) -> bool:
    return float(np.mean((a - b) ** 2)) < 40


def empty_tile(im: Image.Image) -> bool:
    a = np.array(im.convert("L"), dtype=np.float32)
    return float(a.mean()) < 18 or float(a.std()) < 8


def ocr_name(ocr, im: Image.Image, x, y, w, h) -> str:
    band = im.crop((x, y, x + w, y + h))
    try:
        result, _ = ocr(np.array(band.convert("RGB")))
    except Exception:
        return ""
    parts = []
    if result:
        for item in result:
            t = item[1][0] if isinstance(item[1], (list, tuple)) else str(item[1])
            parts.append(t)
    s = zh_convert(re.sub(r"\s+", "", "".join(parts)), "zh-cn")
    s = re.sub(r"[A-Za-z0-9]", "", s)
    if s in ("LIBRARY", "Child", "变更顺序", "變更順序"):
        return ""
    return s if 2 <= len(s) <= 12 else ""


def recrop_5(ocr) -> list[dict]:
    os.makedirs(os.path.join(USER, "icons"), exist_ok=True)
    out = []
    prev = []
    seq = 0
    for fn, y0 in PAGES5:
        path = os.path.join(SHOTS, fn)
        if not os.path.isfile(path):
            print("missing", fn)
            continue
        im = Image.open(path).convert("RGB")
        w, h = im.size
        n_new = 0
        for r in range(ROWS):
            y = y0 + r * DY
            if y + SIDE + 36 > h:
                break
            for c in range(COLS):
                x = X0 + c * DX
                tile = im.crop((x, y, x + SIDE, y + SIDE))
                if empty_tile(tile):
                    continue
                th = thumb(tile)
                if any(too_similar(th, p) for p in prev[-18:]):
                    continue
                name = ocr_name(ocr, im, x - 4, y + SIDE - 4, SIDE + 8, 40)
                rel = "icons/C5_%03d.png" % seq
                tile.save(os.path.join(USER, rel))
                out.append({"idx": seq, "file": rel, "name": name, "source": fn})
                prev.append(th)
                seq += 1
                n_new += 1
        print(" ", fn, "new", n_new, "total", seq)
    return out


GARBAGE = re.compile(
    r"我可是完全|周题|服的|盛关|田圜|正羲|神檐|蛇燮|破壤|粼居|追擎"
)


def garbage(n: str) -> bool:
    n = (n or "").strip()
    if len(n) < 2:
        return True
    if n.endswith("：") or n.endswith(":"):
        return True
    if GARBAGE.search(n):
        return True
    if n.count("三") >= 3:
        return True
    return False


def main():
    print("ocr 5-star catalog…", flush=True)
    ocr = RapidOCR()
    tiles = recrop_5(ocr)
    print("C5 tiles", len(tiles), "named", sum(1 for t in tiles if t["name"]))

    with open(CSV_CHILD, encoding="utf-8-sig") as f:
        rows = list(csv.DictReader(f))
        fields = list(rows[0].keys())

    stars5 = [r for r in rows if (r.get("rarity") or "").startswith("5")]
    print("5star rows", len(stars5), "tiles", len(tiles))

    # align by idx: C5 sequence should equal encyclopedia order
    n_name = n_av = 0
    used_names = set()
    for r in stars5:
        try:
            ix = int(r.get("idx"))
        except (TypeError, ValueError):
            continue
        tile = tiles[ix] if 0 <= ix < len(tiles) else None
        cur = (r.get("name") or "").strip()
        wiki = (r.get("wiki_name") or "").strip()
        grid_n = (tile or {}).get("name") or ""

        # unique display name
        name = cur
        if garbage(name) or name in used_names:
            if wiki and wiki not in used_names and not garbage(wiki):
                name = wiki
            elif grid_n and grid_n not in used_names:
                name = grid_n
            elif name in used_names and name:
                name = name + "·" + str(ix)
            elif grid_n:
                name = grid_n + "·" + str(ix)
        if name:
            used_names.add(name)
        if name != cur:
            r["name"] = name
            n_name += 1

        av = (r.get("avatar_file") or "").replace("\\", "/")
        if not file_ok(av) and tile:
            r["avatar_file"] = tile["file"]
            n_av += 1

    # 3★ leftovers
    for r in rows:
        if r.get("name") == "深夜追擎者" and not file_ok(r.get("avatar_file") or ""):
            if file_ok("avatars/169406.png"):
                r["avatar_file"] = "avatars/169406.png"
                r["name"] = "声音传讯者"
                r["wiki_name"] = "声音传讯者"
                r["wiki_id"] = "169406"
                n_av += 1
                n_name += 1

    print("renamed", n_name, "filled av", n_av)
    empty = [r for r in rows if not file_ok(r.get("avatar_file") or "")]
    print("still empty", len(empty), [(r.get("id"), r.get("name")) for r in empty])

    with open(CSV_CHILD, "w", encoding="utf-8-sig", newline="") as f:
        w = csv.DictWriter(f, fieldnames=fields, extrasaction="ignore")
        w.writeheader()
        w.writerows(rows)


if __name__ == "__main__":
    main()
