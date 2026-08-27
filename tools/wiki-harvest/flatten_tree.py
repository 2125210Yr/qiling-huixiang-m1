# -*- coding: utf-8 -*-
"""Flatten wiki_tree.json to catalog_index.csv and verify 图鉴 counts."""
from __future__ import annotations

import csv
import json
import os
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
TREE = os.path.join(ROOT, "docs", "reference", "gamekee", "wiki_tree.json")
CSV_PATH = os.path.join(ROOT, "docs", "reference", "gamekee", "catalog_index.csv")


def walk(nodes, path, rows):
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
        walk(n.get("child") or [], p, rows)


def main() -> int:
    with open(TREE, encoding="utf-8") as f:
        tree = json.load(f)
    rows = []
    walk(tree, [], rows)
    os.makedirs(os.path.dirname(CSV_PATH), exist_ok=True)
    with open(CSV_PATH, "w", encoding="utf-8-sig", newline="") as f:
        w = csv.DictWriter(f, fieldnames=["path", "entry_id", "content_id", "name", "section"])
        w.writeheader()
        w.writerows(rows)
    print("wrote", CSV_PATH, "rows", len(rows))

    def under(prefix):
        return [r for r in rows if r["path"].startswith(prefix) and r["content_id"]]

    star5 = under("图鉴资料 / 5星天子") + [
        r
        for r in rows
        if r["section"] == "图鉴资料"
        and r["content_id"]
        and (
            r["path"].startswith("图鉴资料 / 暗 /")
            or r["path"].startswith("图鉴资料 / 火 /")
            or r["path"].startswith("图鉴资料 / 木 /")
            or r["path"].startswith("图鉴资料 / 水 /")
        )
        and "人偶" not in r["path"]
        and "歌牌" not in r["path"]
        and "4星天子" not in r["path"]
        and "4星 /" not in r["path"]
        and "3星 /" not in r["path"]
    ]
    # 5★ folders: "5星天子：光" plus sibling 暗/火/木/水 that sit next to it.
    star5_paths = [r for r in rows if r["content_id"] and "图鉴资料" in r["path"]]
    n5 = 0
    n4 = 0
    n_carta = 0
    n_puppet = 0
    for r in star5_paths:
        p = r["path"]
        if "歌牌" in p or p.startswith("图鉴资料 / 4星 /") or p.startswith("图鉴资料 / 3星 /"):
            if "歌牌" in p or "/ 4星 /" in p or p.endswith("/ 4星") is False:
                if "歌牌" in p:
                    n_carta += 1
                elif "/ 4星 /" in p and "天子" not in p:
                    n_carta += 1
                elif "/ 3星 /" in p and "天子" not in p and "人偶" not in p:
                    n_carta += 1
            continue
        if "人偶" in p or p.startswith("图鉴资料 / 史诗") or p.startswith("图鉴资料 / 稀有") or p.startswith("图鉴资料 / 罕见") or p.startswith("图鉴资料 / 常见"):
            n_puppet += 1
            continue
        if "4星天子" in p:
            n4 += 1
            continue
        if "5星天子" in p:
            n5 += 1
            continue
        # sibling element folders after 5星天子：光
        if any(p.startswith("图鉴资料 / %s /" % el) for el in ("暗", "火", "木", "水")) and "人偶" not in p and "歌牌" not in p and "4星天子" not in p:
            # distinguish 5★ vs 4★ sibling 暗 folders by seeing nearest parent — flatten uses full path.
            # 4★ 暗 is "图鉴资料 / 暗 /" after "4星天子：光". Both look the same.
            pass

    # Count using folder names stored on the row path more carefully.
    n5 = n4 = n_carta = n_puppet = 0
    mode = None
    for r in rows:
        if r["section"] != "图鉴资料":
            continue
        name = r["name"]
        parts = r["path"].split(" / ")
        if not r["content_id"]:
            if name.startswith("5星天子"):
                mode = "c5"
            elif name.startswith("4星天子"):
                mode = "c4"
            elif name.startswith("人偶"):
                mode = "puppet"
            elif name in ("史诗", "稀有", "罕见", "常见") and mode in ("puppet", "puppet_sub"):
                mode = "puppet"
            elif name.startswith("歌牌"):
                mode = "carta"
            elif name == "4星" and mode in ("carta", "carta_sub"):
                mode = "carta"
            elif name == "3星" and mode in ("carta", "carta_sub"):
                mode = "carta"
            elif name in ("暗", "火", "木", "水"):
                # keep current character-band mode
                pass
            elif name == "图鉴列表":
                mode = "list"
            continue
        if mode == "c5":
            n5 += 1
        elif mode == "c4":
            n4 += 1
        elif mode == "puppet":
            n_puppet += 1
        elif mode == "carta":
            n_carta += 1

    print("5star=%d 4star=%d carta=%d puppet=%d" % (n5, n4, n_carta, n_puppet))
    errors = []
    if n5 < 270:
        errors.append("5★ children %d < 270" % n5)
    if n_carta < 156:
        errors.append("soul carta %d < 156" % n_carta)
    if n_puppet < 218:
        errors.append("puppets %d < 218" % n_puppet)
    if errors:
        print("VERIFY FAIL:", "; ".join(errors), file=sys.stderr)
        return 1
    print("VERIFY OK")
    return 0


if __name__ == "__main__":
    sys.exit(main())
