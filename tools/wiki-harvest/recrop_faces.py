# -*- coding: utf-8 -*-
"""Recrop catalog faces with per-page Y0; skip name-strips and empty slots."""
from __future__ import annotations

import json
import os

import numpy as np
from PIL import Image

SHOTS = r"F:\天命之子\docs\reference\mobile-archive\screenshots"
OUT = r"F:\天命之子\天命之子数据\icons"
META = r"F:\天命之子\天命之子数据\_layout"

PAGES = {
    "3": [
        "164_charlib_3star.png",
        "165_charlib_3s_p01.png",
    ],
    "2": ["166_charlib_2star.png"],
    "1": ["350_charlib_1star.png"],
}
EXPECT = {"3": 99, "2": 53, "1": 49}
X0, DX, DY, SIDE = 28, 193, 204, 128
ROWS, COLS = 12, 6
Y0_BANNER = 778
Y0_CONT = 656
BANNER_FILES = {
    "164_charlib_3star.png",
    "166_charlib_2star.png",
    "350_charlib_1star.png",
    "332_charlib_3star.png",
}


def empty_like(a: np.ndarray) -> bool:
    g = a.mean(axis=2)
    return float(g.mean()) < 20 or float(g.std()) < 8


def looks_like_card(tile: Image.Image) -> bool:
    a = np.array(tile.convert("RGB"), dtype=np.float32)
    if a.shape[0] < 100 or a.shape[1] < 100:
        return False
    if empty_like(a):
        return False
    top = a[: int(a.shape[0] * 0.55)]
    if float(top.mean()) < 18:
        return False
    return True


def thumb(im: Image.Image):
    return np.array(im.resize((20, 20), Image.Resampling.BILINEAR), dtype=np.float32)


def too_similar(a: np.ndarray, b: np.ndarray) -> bool:
    # Only near-identical reshoots (overlapping scroll). Color slimes must NOT collapse.
    return float(np.mean((a - b) ** 2)) < 18


def find_y0(im: Image.Image) -> int:
    arr = np.array(im.convert("RGB"))
    h, w, _ = arr.shape
    best_y, best_n = 760, 0
    plateau = []
    for y in range(540, 920, 2):
        if y + SIDE > h - 2:
            break
        n = 0
        for c in range(COLS):
            x = X0 + c * DX
            if x + SIDE > w - 2:
                continue
            tile = Image.fromarray(arr[y : y + SIDE, x : x + SIDE])
            if looks_like_card(tile):
                n += 1
        if n >= 4:
            plateau.append((y, n))
        if n > best_n:
            best_n, best_y = n, y
    if plateau:
        # first full-ish row (top of first plateau with max n in first 80px cluster)
        nmax = max(p[1] for p in plateau)
        first = [p for p in plateau if p[1] >= max(4, nmax - 1)]
        best_y = first[0][0]
    return best_y


def contact_sheet(paths, dest, cols=10):
    tiles = []
    for p in paths:
        if os.path.isfile(p):
            tiles.append(Image.open(p).convert("RGB").resize((72, 72), Image.Resampling.BILINEAR))
    if not tiles:
        return
    rows = (len(tiles) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * 72, rows * 72), (12, 12, 16))
    for i, im in enumerate(tiles):
        sheet.paste(im, ((i % cols) * 72, (i // cols) * 72))
    sheet.save(dest)


def recrop_rarity(rar: str):
    os.makedirs(OUT, exist_ok=True)
    os.makedirs(META, exist_ok=True)
    prev = []
    rows = []
    seq = 0
    for fn in PAGES[rar]:
        path = os.path.join(SHOTS, fn)
        if not os.path.isfile(path):
            print("missing", fn)
            continue
        im = Image.open(path).convert("RGB")
        w, h = im.size
        y0 = Y0_BANNER if fn in BANNER_FILES else Y0_CONT
        print("page", fn, "y0", y0, flush=True)
        n_new = 0
        page_thumbs = []
        for r in range(ROWS):
            for c in range(COLS):
                if seq >= EXPECT[rar]:
                    break
                x = X0 + c * DX
                y = y0 + r * DY
                if x + SIDE > w - 2:
                    continue
                y2 = min(y + SIDE, h - 2)
                if y2 - y < 120:
                    continue
                tile = im.crop((x, y, min(x + SIDE, w - 2), y2))
                if tile.size != (SIDE, SIDE):
                    tile = tile.resize((SIDE, SIDE), Image.Resampling.BILINEAR)
                if not looks_like_card(tile):
                    continue
                a = np.array(tile.convert("RGB"), dtype=np.float32)
                if float(a.mean()) < 38 and float(a.std()) < 28:
                    continue
                th = thumb(tile)
                if any(too_similar(th, old) for old in prev):
                    continue
                fname = "F%s_%03d.png" % (rar, seq)
                tile.save(os.path.join(OUT, fname))
                rec = {"rarity_key": rar, "idx": seq, "file": "icons/" + fname, "source": fn, "x": x, "y": y}
                rows.append(rec)
                page_thumbs.append(th)
                seq += 1
                n_new += 1
            if seq >= EXPECT[rar]:
                break
        prev.extend(page_thumbs)
        print("  new", n_new, "seq", seq, flush=True)
    meta_path = os.path.join(META, "recrop_%sstar.jsonl" % rar)
    with open(meta_path, "w", encoding="utf-8") as f:
        for rec in rows:
            f.write(json.dumps(rec, ensure_ascii=False) + "\n")
    paths = [os.path.join(OUT, "F%s_%03d.png" % (rar, i)) for i in range(seq)]
    contact_sheet(paths, os.path.join(META, "sheet_%sstar.png" % rar))
    print("rarity", rar, "got", seq, "expect", EXPECT[rar], flush=True)
    return rows


def recrop_4_hearts():
    """Last 4★ page: 紫/黄/绿/红/蓝爱心."""
    fn = "163_charlib_4s_p04.png"
    path = os.path.join(SHOTS, fn)
    if not os.path.isfile(path):
        print("missing", fn)
        return []
    im = Image.open(path).convert("RGB")
    y0 = Y0_CONT
    x0 = 36
    keys = ["purple", "yellow", "green", "red", "blue"]
    out = []
    for c, key in enumerate(keys):
        x = x0 + c * DX
        tile = im.crop((x, y0, x + SIDE, y0 + SIDE)).resize((SIDE, SIDE), Image.Resampling.BILINEAR)
        fname = "F4_heart_%s.png" % key
        tile.save(os.path.join(OUT, fname))
        out.append({"key": key, "file": "icons/" + fname, "source": fn, "x": x, "y": y0})
    with open(os.path.join(META, "recrop_4hearts.json"), "w", encoding="utf-8") as f:
        json.dump(out, f, ensure_ascii=False, indent=2)
    print("4star hearts", len(out), "y0", y0, flush=True)
    return out


def main():
    recrop_4_hearts()
    n = 0
    for rar in ("3", "2", "1"):
        n += len(recrop_rarity(rar))
    print("DONE faces", n)


if __name__ == "__main__":
    main()
