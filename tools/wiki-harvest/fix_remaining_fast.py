# -*- coding: utf-8 -*-
"""Fast 5★ recrop (no OCR), fill empties, unique names, 3★ leftover wiki."""
from __future__ import annotations

import csv
import os

import numpy as np
from PIL import Image

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
DUP5 = {61: 60, 71: 70, 111: 110, 131: 130, 171: 170, 181: 180, 221: 220, 251: 250, 275: 161, 281: 280}


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


def recrop_5() -> list[str]:
    os.makedirs(os.path.join(USER, "icons"), exist_ok=True)
    out = []
    prev = []
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
            if y + SIDE > h - 4:
                break
            for c in range(COLS):
                x = X0 + c * DX
                tile = im.crop((x, y, x + SIDE, y + SIDE))
                if empty_tile(tile):
                    continue
                th = thumb(tile)
                if any(too_similar(th, p) for p in prev[-18:]):
                    continue
                rel = "icons/C5_%03d.png" % len(out)
                tile.save(os.path.join(USER, rel))
                out.append(rel)
                prev.append(th)
                n_new += 1
        print(" ", fn, "+", n_new, "total", len(out), flush=True)
    return out


def garbage(n: str) -> bool:
    n = (n or "").strip()
    if len(n) < 2:
        return True
    if n.endswith("：") or n.endswith(":"):
        return True
    if any(x in n for x in ("我可是完全", "周题", "田圜", "正羲", "神檐", "蛇燮")):
        return True
    return False


def main():
    print("recrop 5-star…", flush=True)
    tiles = recrop_5()
    print("C5", len(tiles), flush=True)

    with open(CSV_CHILD, encoding="utf-8-sig") as f:
        rows = list(csv.DictReader(f))
        fields = list(rows[0].keys())

    by5 = {}
    for r in rows:
        if (r.get("rarity") or "").startswith("5"):
            try:
                by5[int(r["idx"])] = r
            except (TypeError, ValueError):
                pass

    n_av = n_name = 0
    # costume-pair reuse
    for dst, src in DUP5.items():
        a, b = by5.get(dst), by5.get(src)
        if not a or not b:
            continue
        if not file_ok(a.get("avatar_file") or "") and file_ok(b.get("avatar_file") or ""):
            a["avatar_file"] = b["avatar_file"]
            n_av += 1
        if garbage(a.get("name") or "") and b.get("name"):
            a["name"] = b["name"]
            n_name += 1

    # C5 fill remaining empty 5★
    for ix, r in by5.items():
        if file_ok(r.get("avatar_file") or ""):
            continue
        if 0 <= ix < len(tiles):
            r["avatar_file"] = tiles[ix]
            n_av += 1

    # unique names (skip costume-pair seconds)
    seconds = set(DUP5)
    seen = {}
    for r in rows:
        rar = r.get("rarity") or ""
        name = (r.get("name") or "").strip()
        try:
            ix = int(r.get("idx"))
        except (TypeError, ValueError):
            ix = -1
        if rar.startswith("5") and ix in seconds:
            continue
        key = (rar, name)
        if not name or garbage(name) or key in seen:
            alt = (r.get("wiki_name") or "").strip()
            el = r.get("element") or ""
            if alt and (rar, alt) not in seen and not garbage(alt):
                name = alt
            elif name and el:
                name = name + "·" + el
            elif name:
                name = name + "·" + str(ix)
            elif el:
                name = "天子" + str(ix) + "·" + el
            else:
                name = "天子" + str(ix)
            r["name"] = name
            n_name += 1
        seen[(rar, name)] = r

    # 3★ leftover
    for r in rows:
        if r.get("name") == "深夜追擎者" and not file_ok(r.get("avatar_file") or ""):
            if file_ok("avatars/169406.png"):
                r["avatar_file"] = "avatars/169406.png"
                r["name"] = "声音传讯者"
                r["wiki_id"] = "169406"
                r["wiki_name"] = "声音传讯者"
                n_av += 1
                n_name += 1

    print("filled av", n_av, "renamed", n_name)
    empty = [(r.get("id"), r.get("name"), r.get("avatar_file")) for r in rows if not file_ok(r.get("avatar_file") or "")]
    print("still empty", len(empty), empty)

    with open(CSV_CHILD, "w", encoding="utf-8-sig", newline="") as f:
        w = csv.DictWriter(f, fieldnames=fields, extrasaction="ignore")
        w.writeheader()
        w.writerows(rows)


if __name__ == "__main__":
    main()
