# -*- coding: utf-8 -*-
"""Attach 罕见/常见 wiki portraits to 3/2/1★ children; recrop aligned catalog tiles as fallback."""
from __future__ import annotations

import csv
import os
import re
from difflib import SequenceMatcher

from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
USER = os.path.join(ROOT, "天命之子数据")
INDEX = os.path.join(ROOT, "docs", "reference", "gamekee", "catalog_index.csv")
CSV_CHILD = os.path.join(USER, "汇总表.csv")
SHOTS = os.path.join(ROOT, "docs", "reference", "mobile-archive", "screenshots")
ICON_DIR = os.path.join(USER, "icons")

G_RE = re.compile(r"^g([54321])_(\d+)_(.+)\.png$", re.I)


def simp(s: str) -> str:
    t = (s or "").strip()
    t = t.replace("的", "").replace(" ", "").replace("　", "")
    t = re.sub(r"[♥♡★☆·・]", "", t)
    return t


def core(s: str) -> str:
    return simp(s)


ALIASES = {
    "恶萝曼提": "噩梦曼提",
    "球鞋门士": "球鞋斗土",
    "海市楼魔甘娜": "海市蜃楼魔甘娜",
    "破壤的卡利宇迦": "破坏的卡利宇迦",
    "缘头盔巴祖卡": "绿头盔巴祖卡",
    "黛人的宝箱怪": "惊人的宝箱怪",
    "D赫兹": "DJ赫兹",
    "毒药安": "毒药安瓿",
}


def score(a: str, b: str) -> int:
    a = ALIASES.get(a, a)
    ca, cb = core(a), core(b)
    if not ca or not cb or len(ca) < 2 or len(cb) < 2:
        return 0
    if ca == cb:
        return 200
    if ca.endswith(cb) or cb.endswith(ca):
        return 120 + min(len(ca), len(cb))
    if ca in cb or cb in ca:
        return 90 + min(len(ca), len(cb))
    r = SequenceMatcher(None, ca, cb).ratio()
    if r >= 0.78 and abs(len(ca) - len(cb)) <= 2:
        return 95
    return int(r * 80) if r >= 0.72 else 0


def file_ok(rel: str) -> bool:
    if not rel:
        return False
    p = os.path.join(USER, rel.replace("\\", "/"))
    return os.path.isfile(p) and os.path.getsize(p) > 1500


def load_wiki_lowstar():
    out = []
    with open(INDEX, encoding="utf-8-sig") as f:
        for r in csv.DictReader(f):
            path = r.get("path") or ""
            cid = (r.get("content_id") or "").strip()
            name = (r.get("name") or "").strip()
            if not cid or not name:
                continue
            if "罕见" not in path and "常见" not in path:
                continue
            if name in ("罕见", "常见"):
                continue
            av = ""
            for ext in (".png", ".jpg"):
                rel = "avatars/%s%s" % (cid, ext)
                if file_ok(rel):
                    av = rel
                    break
            out.append({"content_id": cid, "name": name, "avatar_file": av, "path": path})
    return out


def recrop_page(src_name: str, y0: int, n_rows: int, start_idx: int, dx=193, x0=28, side=152, dy=200):
    path = os.path.join(SHOTS, src_name)
    if not os.path.isfile(path):
        print("missing shot", src_name)
        return {}
    im = Image.open(path).convert("RGB")
    w, h = im.size
    mapping = {}
    os.makedirs(ICON_DIR, exist_ok=True)
    seq = start_idx
    for r in range(n_rows):
        y = y0 + r * dy
        if y + side > h - 8:
            break
        row_ok = 0
        for c in range(6):
            x = x0 + c * dx
            tile = im.crop((x, y, x + side, y + side))
            a = list(tile.getdata())
            mean = sum(p[0] + p[1] + p[2] for p in a) / (3 * len(a))
            if mean < 18:
                continue
            fn = "icons/C3_%03d.png" % seq
            tile.save(os.path.join(USER, fn))
            mapping[seq] = fn
            seq += 1
            row_ok += 1
        if row_ok == 0:
            break
    print("recrop", src_name, "y0", y0, "got", len(mapping), "from idx", start_idx)
    return mapping


def main():
    wiki = load_wiki_lowstar()
    print("wiki lowstar", len(wiki), "with av", sum(1 for w in wiki if w["avatar_file"]))
    with open(CSV_CHILD, encoding="utf-8-sig") as f:
        rows = list(csv.DictReader(f))
        fields = list(rows[0].keys())

    used = set()
    n_wiki = 0
    for r in rows:
        rar = (r.get("rarity") or "")[:1]
        if rar not in "321":
            continue
        name = r.get("name") or ""
        best, best_s = None, 0
        for w in wiki:
            if id(w) in used:
                continue
            s = score(name, w["name"])
            if s > best_s:
                best, best_s = w, s
        if best and best_s >= 88 and best["avatar_file"]:
            r["avatar_file"] = best["avatar_file"]
            r["wiki_id"] = best["content_id"]
            r["wiki_name"] = best["name"]
            if best_s >= 120:
                r["name"] = best["name"]
            used.add(id(best))
            n_wiki += 1
    print("3/2/1 wiki matched", n_wiki)

    # aligned recrop fallback for leftover 3★
    m164 = recrop_page("164_charlib_3star.png", y0=780, n_rows=9, start_idx=0, dy=200)
    m165 = recrop_page("165_charlib_3s_p01.png", y0=640, n_rows=8, start_idx=72, dy=200)
    crops = {}
    crops.update(m164)
    crops.update(m165)

    n_crop = 0
    for r in rows:
        if (r.get("rarity") or "")[:1] != "3":
            continue
        av = (r.get("avatar_file") or "").replace("\\", "/")
        wiki_av = av.startswith("avatars/") or av.startswith("avatars_wiki/")
        try:
            ix = int(r.get("idx"))
        except (TypeError, ValueError):
            continue
        if wiki_av and file_ok(av):
            continue
        if ix in crops and file_ok(crops[ix]):
            r["avatar_file"] = crops[ix]
            n_crop += 1
    print("3star recrop fallback", n_crop)

    empty3 = [r for r in rows if (r.get("rarity") or "").startswith("3") and not file_ok(r.get("avatar_file") or "")]
    print("3star still empty", len(empty3), [r.get("name") for r in empty3[:12]])

    with open(CSV_CHILD, "w", encoding="utf-8-sig", newline="") as f:
        w = csv.DictWriter(f, fieldnames=fields, extrasaction="ignore")
        w.writeheader()
        w.writerows(rows)
    print("wrote", CSV_CHILD)


if __name__ == "__main__":
    main()
