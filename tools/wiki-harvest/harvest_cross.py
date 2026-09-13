# -*- coding: utf-8 -*-
"""Harvest JP (dcj) + KR (destinychild) character pages and portraits for cross-check."""
from __future__ import annotations

import csv
import json
import os
import re
import time
import urllib.error
import urllib.request
from concurrent.futures import ThreadPoolExecutor, as_completed

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
GK = os.path.join(ROOT, "docs", "reference", "gamekee")
OUT_PAGES = os.path.join(GK, "pages_cross")
OUT_CSV = os.path.join(GK, "cross_index.csv")
AV = os.path.join(ROOT, "天命之子数据", "avatars_wiki")

IMG_TAG = re.compile(r"<img\b([^>]*)>", re.I)
ATTR = re.compile(r"""([\w:-]+)\s*=\s*(['"])(.*?)\2""", re.S)
URL_W = re.compile(r"/w_(\d+)/")
URL_H = re.compile(r"/h_(\d+)/")

UA = "Mozilla/5.0 ResonanceHarvest/1.0"
WORKERS = 8


def headers(alias: str) -> dict:
    return {
        "User-Agent": UA,
        "Accept": "application/json",
        "Referer": "https://www.gamekee.com/%s/" % alias,
        "Origin": "https://www.gamekee.com",
        "Game-Alias": alias,
    }


def get_json(alias: str, url: str) -> dict:
    last = None
    for attempt in range(1, 4):
        try:
            req = urllib.request.Request(url, headers=headers(alias))
            with urllib.request.urlopen(req, timeout=40) as resp:
                return json.loads(resp.read().decode("utf-8"))
        except Exception as e:
            last = e
            time.sleep(0.5 * attempt)
    raise RuntimeError("%s %s" % (url, last))


def walk(nodes, path, acc):
    for n in nodes or []:
        if not isinstance(n, dict):
            continue
        p = path + [n.get("name") or ""]
        cid = n.get("content_id")
        if cid:
            acc.append({"path": " / ".join(p), "entry_id": n.get("id") or "", "content_id": int(cid), "name": n.get("name") or ""})
        walk(n.get("child") or [], p, acc)


def want(alias: str, rec: dict) -> bool:
    p = rec["path"]
    if alias == "dcj":
        return p.startswith("五星天子图鉴") and "检索" not in p
    if alias == "destinychild":
        if "魂卡" in p or "玩偶" in p or "史诗" in p or "稀有" in p or "罕见" in p or "普通" in p:
            return False
        return any(k in p for k in ("五星", "四星", "有价值的三星"))
    return False


def abs_url(u: str) -> str:
    u = (u or "").strip()
    if not u:
        return ""
    if u.startswith("//"):
        return "https:" + u
    if u.startswith("http://"):
        return "https://" + u[7:]
    return u


def score_head(url: str, w: int, h: int) -> float:
    u = (url or "").strip().lower()
    if not u or ".gif" in u.split("?")[0]:
        return -1
    if "w_100/h_25" in u or (w == 100 and h == 25):
        return -1
    if h and h <= 40 and w and w >= 80:
        return -1
    if "h_250,w_250" in u or "/w_250/h_250/" in u:
        return 100
    if "/w_256/h_256/" in u:
        return 98
    if "/w_128/h_128/" in u:
        return 96
    if w and h and 0.82 <= (w / float(h)) <= 1.22 and 100 <= w <= 280:
        return 92
    if u.split("?")[0].endswith(".png"):
        return 40
    return 10


def pick_avatar(content: str, thumbs: list) -> str:
    best = ("", -1.0)
    for m in IMG_TAG.finditer(content or ""):
        attrs = {k.lower(): v for k, _, v in ATTR.findall(m.group(1))}
        src = attrs.get("data-real") or attrs.get("src") or ""
        try:
            w = int(attrs.get("data-width") or 0)
        except ValueError:
            w = 0
        try:
            h = int(attrs.get("data-height") or 0)
        except ValueError:
            h = 0
        s = score_head(src, w, h)
        if s > best[1]:
            best = (src, s)
        if s >= 98:
            break
    if best[1] >= 40:
        return abs_url(best[0])
    for t in thumbs or []:
        if score_head(t, 0, 0) >= 40:
            return abs_url(t)
    return ""


def is_image(data: bytes) -> bool:
    return data[:8] == b"\x89PNG\r\n\x1a\n" or data[:2] == b"\xff\xd8" or (data[:4] == b"RIFF" and data[8:12] == b"WEBP")


def download(url: str, dest: str) -> str:
    if os.path.isfile(dest) and os.path.getsize(dest) > 64:
        return "exists"
    os.makedirs(os.path.dirname(dest), exist_ok=True)
    alts = [url]
    alts.append(url.replace("cdnimg.gamekee.com", "cdnimg-v2.gamekee.com"))
    alts.append(url.replace("cdnimg-v2.gamekee.com", "cdnimg.gamekee.com"))
    if "?" in url:
        alts.append(url.split("?")[0])
    seen = set()
    for u in alts:
        if not u or u in seen:
            continue
        seen.add(u)
        try:
            req = urllib.request.Request(u, headers={"User-Agent": UA, "Referer": "https://www.gamekee.com/", "Accept": "image/*"})
            with urllib.request.urlopen(req, timeout=25) as resp:
                data = resp.read()
            if not is_image(data):
                continue
            tmp = dest + ".part"
            with open(tmp, "wb") as f:
                f.write(data)
            os.replace(tmp, dest)
            return "ok"
        except Exception:
            continue
    return "fail"


def fetch_one(alias: str, rec: dict) -> dict:
    cid = rec["content_id"]
    page_path = os.path.join(OUT_PAGES, alias, "%s.json" % cid)
    os.makedirs(os.path.dirname(page_path), exist_ok=True)
    if os.path.isfile(page_path):
        data = json.load(open(page_path, encoding="utf-8")).get("data") or {}
    else:
        raw = get_json(alias, "https://www.gamekee.com/v1/content/detail/%s" % cid)
        json.dump(raw, open(page_path, "w", encoding="utf-8"), ensure_ascii=False)
        data = raw.get("data") or {}
    title = (data.get("title") or rec["name"] or "").strip()
    url = pick_avatar(data.get("content") or "", data.get("thumb_list") or [])
    rec = dict(rec)
    rec["alias"] = alias
    rec["title"] = title
    rec["avatar_url"] = url
    rec["wiki_url"] = "https://www.gamekee.com/%s/%s.html" % (alias, cid)
    rec["avatar_file"] = ""
    if url:
        ext = ".png"
        base = url.split("?")[0].lower()
        for e in (".png", ".jpg", ".jpeg", ".webp"):
            if base.endswith(e):
                ext = e
                break
        dest = os.path.join(AV, "%s_%s%s" % (alias, cid, ext))
        st = download(url, dest)
        if st in ("ok", "exists"):
            rec["avatar_file"] = "avatars_wiki/%s_%s%s" % (alias, cid, ext)
    return rec


def main():
    rows = []
    for alias, fn in (("dcj", "wiki_tree_dcj.json"), ("destinychild", "wiki_tree_destinychild.json")):
        tree = json.load(open(os.path.join(GK, fn), encoding="utf-8"))
        acc = []
        walk(tree, [], acc)
        acc = [r for r in acc if want(alias, r)]
        print(alias, "targets", len(acc), flush=True)
        done = 0
        with ThreadPoolExecutor(max_workers=WORKERS) as ex:
            futs = [ex.submit(fetch_one, alias, r) for r in acc]
            for fut in as_completed(futs):
                rec = fut.result()
                rows.append(rec)
                done += 1
                if done % 40 == 0 or done == len(acc):
                    print(" ", alias, done, "/", len(acc), "avatars", sum(1 for x in rows if x.get("avatar_file") and x["alias"]==alias), flush=True)
    os.makedirs(AV, exist_ok=True)
    fields = ["alias", "content_id", "name", "title", "path", "avatar_file", "avatar_url", "wiki_url"]
    with open(OUT_CSV, "w", encoding="utf-8-sig", newline="") as f:
        w = csv.DictWriter(f, fieldnames=fields, extrasaction="ignore")
        w.writeheader()
        for r in rows:
            w.writerow(r)
    print("wrote", OUT_CSV, "rows", len(rows), "with avatar", sum(1 for r in rows if r.get("avatar_file")))


if __name__ == "__main__":
    main()
