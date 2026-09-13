# -*- coding: utf-8 -*-
"""Harvest one GameKee wiki (dc / destinychild / dcj) into docs/reference/gamekee/<alias>/."""
from __future__ import annotations

import argparse
import json
import os
import sys
import time
import urllib.error
import urllib.request
from concurrent.futures import ThreadPoolExecutor, as_completed
from datetime import datetime, timezone

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SITES = {
    "dc": {"game_id": 202, "name": "天命之子（国际服）", "url": "https://www.gamekee.com/dc/"},
    "destinychild": {"game_id": 1069, "name": "天命之子韩服", "url": "https://www.gamekee.com/destinychild/"},
    "dcj": {"game_id": 1164, "name": "天命之子日服", "url": "https://www.gamekee.com/dcj/"},
}
DELAY_SEC = 0.04
RETRIES = 3
WORKERS = 10


def get(url: str, alias: str) -> dict:
    headers = {
        "User-Agent": "Mozilla/5.0 ResonanceHarvest/1.0",
        "Accept": "application/json",
        "Referer": "https://www.gamekee.com/%s/" % alias,
        "Origin": "https://www.gamekee.com",
        "Game-Alias": alias,
    }
    last = None
    for attempt in range(1, RETRIES + 1):
        try:
            req = urllib.request.Request(url, headers=headers)
            with urllib.request.urlopen(req, timeout=40) as resp:
                raw = resp.read().decode("utf-8")
            data = json.loads(raw)
            if not isinstance(data, dict):
                raise ValueError("not an object")
            return data
        except (urllib.error.URLError, TimeoutError, ValueError, json.JSONDecodeError) as exc:
            last = exc
            time.sleep(0.6 * attempt)
    raise RuntimeError("GET failed %s: %s" % (url, last))


def walk(nodes, acc):
    for n in nodes or []:
        if not isinstance(n, dict):
            continue
        cid = n.get("content_id")
        if cid:
            acc.append(int(cid))
        walk(n.get("child") or [], acc)


def flatten(nodes, path, rows):
    for n in nodes or []:
        if not isinstance(n, dict):
            continue
        name = n.get("name") or ""
        p = path + [name]
        cid = n.get("content_id")
        rows.append(
            {
                "path": " / ".join(p),
                "entry_id": n.get("id") or "",
                "content_id": cid if cid else "",
                "name": name,
                "section": p[0] if p else "",
            }
        )
        flatten(n.get("child") or [], p, rows)


def count_tree(nodes, depth=1, by_top=None, top=None):
    n_nodes = 0
    n_content = 0
    if by_top is None:
        by_top = {}
    for n in nodes or []:
        if not isinstance(n, dict):
            continue
        n_nodes += 1
        name = n.get("name") or ""
        cur_top = name if depth == 1 else top
        if cur_top not in by_top:
            by_top[cur_top] = {"nodes": 0, "content": 0}
        by_top[cur_top]["nodes"] += 1
        if n.get("content_id"):
            n_content += 1
            by_top[cur_top]["content"] += 1
        cn, cc = count_tree(n.get("child") or [], depth + 1, by_top, cur_top)
        n_nodes += cn
        n_content += cc
    return n_nodes, n_content


def write_index(out_dir, alias, tree):
    meta = SITES[alias]
    n_nodes, n_content = count_tree(tree)
    by_top = {}
    count_tree(tree, 1, by_top, None)
    lines = [
        "# GameKee %s" % meta["name"],
        "",
        "- Source: %s" % meta["url"],
        "- Game alias: `%s` / game_id `%s`" % (alias, meta["game_id"]),
        "- Harvested: %s" % datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M UTC"),
        "- Nodes: **%d**" % n_nodes,
        "- Pages with content_id: **%d**" % n_content,
        "",
        "| Section | Nodes | Content pages |",
        "|---|---:|---:|",
    ]
    for name, v in by_top.items():
        lines.append("| %s | %d | %d |" % (name, v["nodes"], v["content"]))
    lines.append("")
    path = os.path.join(out_dir, "INDEX.md")
    with open(path, "w", encoding="utf-8") as f:
        f.write("\n".join(lines))
    return n_nodes, n_content


def already_ok(pages, cid):
    path = os.path.join(pages, "%d.json" % cid)
    if not os.path.isfile(path):
        return False
    try:
        with open(path, encoding="utf-8") as f:
            existing = json.load(f)
        return existing.get("code") == 0 and bool(existing.get("data"))
    except (OSError, json.JSONDecodeError):
        return False


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--alias", required=True, choices=list(SITES))
    ap.add_argument("--workers", type=int, default=WORKERS)
    args = ap.parse_args()
    alias = args.alias
    meta = SITES[alias]
    out_dir = os.path.join(ROOT, "docs", "reference", "gamekee", alias)
    pages = os.path.join(out_dir, "pages")
    os.makedirs(pages, exist_ok=True)

    print("[%s] fetching tree game_id=%s" % (alias, meta["game_id"]), flush=True)
    payload = get("https://www.gamekee.com/v1/wiki/entry?id=%s" % meta["game_id"], alias)
    if payload.get("code") != 0:
        print("tree error", payload.get("msg"), file=sys.stderr)
        return 1
    entry_list = (payload.get("data") or {}).get("entry_list") or []
    with open(os.path.join(out_dir, "wiki_tree.json"), "w", encoding="utf-8") as f:
        json.dump(entry_list, f, ensure_ascii=False)

    import csv

    rows = []
    flatten(entry_list, [], rows)
    csv_path = os.path.join(out_dir, "catalog_index.csv")
    with open(csv_path, "w", encoding="utf-8-sig", newline="") as f:
        w = csv.DictWriter(f, fieldnames=["path", "entry_id", "content_id", "name", "section"])
        w.writeheader()
        w.writerows(rows)

    ids = []
    walk(entry_list, ids)
    seen, uniq = set(), []
    for i in ids:
        if i not in seen:
            seen.add(i)
            uniq.append(i)
    print("[%s] unique content ids: %d" % (alias, len(uniq)), flush=True)

    todo = [cid for cid in uniq if not already_ok(pages, cid)]
    ok = len(uniq) - len(todo)
    fail = []
    print("[%s] skip %d, fetch %d" % (alias, ok, len(todo)), flush=True)
    detail = "https://www.gamekee.com/v1/content/detail/{id}"

    def fetch_one(cid):
        time.sleep(DELAY_SEC)
        data = get(detail.format(id=cid), alias)
        path = os.path.join(pages, "%d.json" % cid)
        with open(path, "w", encoding="utf-8") as f:
            json.dump(data, f, ensure_ascii=False)
        title = ""
        if isinstance(data.get("data"), dict):
            title = data["data"].get("title") or ""
        return cid, data.get("code") == 0, title, data.get("msg")

    n = ok
    with ThreadPoolExecutor(max_workers=max(1, args.workers)) as pool:
        futs = [pool.submit(fetch_one, cid) for cid in todo]
        for fut in as_completed(futs):
            n += 1
            try:
                cid, good, title, msg = fut.result()
                if good:
                    ok += 1
                else:
                    fail.append(cid)
                if n % 40 == 0 or not good:
                    print("  [%s %d/%d] %s %s" % (alias, n, len(uniq), cid, title or msg), flush=True)
            except Exception as exc:
                fail.append("exc")
                print("  [%s] FAIL %s" % (alias, exc), file=sys.stderr, flush=True)

    nn, nc = write_index(out_dir, alias, entry_list)
    print("[%s] DONE ok=%d fail=%d nodes=%d content=%d" % (alias, ok, len(fail), nn, nc), flush=True)
    if fail:
        with open(os.path.join(out_dir, "harvest_fail.txt"), "w", encoding="utf-8") as f:
            f.write("\n".join(str(x) for x in fail))
    return 0 if not fail else 2


if __name__ == "__main__":
    sys.exit(main())
