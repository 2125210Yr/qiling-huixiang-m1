# -*- coding: utf-8 -*-
"""Download GameKee Destiny Child wiki tree + every content page."""
from __future__ import annotations

import json
import os
import sys
import time
import urllib.error
import urllib.request
from concurrent.futures import ThreadPoolExecutor, as_completed
from datetime import datetime, timezone

HEADERS = {
    "User-Agent": "Mozilla/5.0 ResonanceHarvest/1.0",
    "Accept": "application/json",
    "Referer": "https://www.gamekee.com/dc/",
    "Origin": "https://www.gamekee.com",
    "Game-Alias": "dc",
}
TREE_URL = "https://www.gamekee.com/v1/wiki/entry?id=20278"
DETAIL_URL = "https://www.gamekee.com/v1/content/detail/{id}"
DELAY_SEC = 0.05
RETRIES = 3
WORKERS = 8

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "docs", "reference", "gamekee")
PAGES = os.path.join(OUT, "pages")
TREE_PATH = os.path.join(OUT, "wiki_tree.json")
INDEX_PATH = os.path.join(OUT, "INDEX.md")


def get(url: str) -> dict:
    last = None
    for attempt in range(1, RETRIES + 1):
        try:
            req = urllib.request.Request(url, headers=HEADERS)
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


def write_index(tree: list) -> None:
    n_nodes, n_content = count_tree(tree)
    by_top = {}
    count_tree(tree, 1, by_top, None)
    lines = [
        "# GameKee Destiny Child wiki dump",
        "",
        "- Source: https://www.gamekee.com/dc/",
        "- Game alias: `dc` / game_id `202`",
        "- Harvested: %s" % datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M UTC"),
        "- Nodes: **%d**" % n_nodes,
        "- Pages with content_id: **%d**" % n_content,
        "",
        "| Section | Nodes | Content pages |",
        "|---|---:|---:|",
    ]
    for name, v in by_top.items():
        lines.append("| %s | %d | %d |" % (name, v["nodes"], v["content"]))
    lines += [
        "",
        "Original names in this folder are **reference only**. Do not copy them into `client/Assets/Content`.",
        "",
    ]
    os.makedirs(OUT, exist_ok=True)
    with open(INDEX_PATH, "w", encoding="utf-8") as f:
        f.write("\n".join(lines))


def main() -> int:
    os.makedirs(PAGES, exist_ok=True)
    print("Fetching wiki tree…")
    payload = get(TREE_URL)
    if payload.get("code") != 0:
        print("tree error", payload.get("msg"), file=sys.stderr)
        return 1
    entry_list = (payload.get("data") or {}).get("entry_list") or []
    with open(TREE_PATH, "w", encoding="utf-8") as f:
        json.dump(entry_list, f, ensure_ascii=False)
    ids = []
    walk(entry_list, ids)
    # unique, stable order
    seen = set()
    uniq = []
    for i in ids:
        if i not in seen:
            seen.add(i)
            uniq.append(i)
    print("Tree saved. Unique content ids:", len(uniq))

    def already_ok(cid):
        path = os.path.join(PAGES, "%d.json" % cid)
        if not os.path.isfile(path):
            return False
        try:
            with open(path, encoding="utf-8") as f:
                existing = json.load(f)
            return existing.get("code") == 0 and bool(existing.get("data"))
        except (OSError, json.JSONDecodeError):
            return False

    todo = [cid for cid in uniq if not already_ok(cid)]
    ok = len(uniq) - len(todo)
    fail = []
    print("skip %d, fetch %d" % (ok, len(todo)), flush=True)

    def fetch_one(cid):
        time.sleep(DELAY_SEC)
        data = get(DETAIL_URL.format(id=cid))
        path = os.path.join(PAGES, "%d.json" % cid)
        with open(path, "w", encoding="utf-8") as f:
            json.dump(data, f, ensure_ascii=False)
        title = ""
        if isinstance(data.get("data"), dict):
            title = data["data"].get("title") or ""
        return cid, data.get("code") == 0, title, data.get("msg")

    n = ok
    with ThreadPoolExecutor(max_workers=WORKERS) as pool:
        futs = [pool.submit(fetch_one, cid) for cid in todo]
        for fut in as_completed(futs):
            n += 1
            try:
                cid, good, title, msg = fut.result()
                if good:
                    ok += 1
                else:
                    fail.append(cid)
                if n % 50 == 0 or not good:
                    print("  [%d/%d] %s %s" % (n, len(uniq), cid, title or msg), flush=True)
            except Exception as exc:
                fail.append("exc")
                print("  FAIL", exc, file=sys.stderr, flush=True)

    write_index(entry_list)
    print("DONE ok=%d fail=%d index=%s" % (ok, len(fail), INDEX_PATH))
    if fail:
        with open(os.path.join(OUT, "harvest_fail.txt"), "w", encoding="utf-8") as f:
            f.write("\n".join(str(x) for x in fail))
        return 2 if ok < 1200 else 0
    return 0 if ok >= 1200 else 3


if __name__ == "__main__":
    sys.exit(main())
