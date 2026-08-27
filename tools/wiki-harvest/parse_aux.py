# -*- coding: utf-8 -*-
"""Parse soul cartas, puppets, 5★ gear, and buffs from harvested pages."""
from __future__ import annotations

import csv
import html as htmlmod
import json
import os
import re
import sys
from html.parser import HTMLParser

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
CSV_PATH = os.path.join(ROOT, "docs", "reference", "gamekee", "catalog_index.csv")
PAGES = os.path.join(ROOT, "docs", "reference", "gamekee", "pages")
PARSED = os.path.join(ROOT, "docs", "reference", "gamekee", "parsed")


class TextExtractor(HTMLParser):
    def __init__(self):
        super().__init__()
        self.parts = []
        self.skip = 0

    def handle_starttag(self, tag, attrs):
        if tag in ("script", "style"):
            self.skip += 1
        if tag in ("p", "br", "li", "tr", "h1", "h2", "h3", "h4", "div"):
            self.parts.append("\n")
        if tag == "td":
            self.parts.append(" | ")

    def handle_endtag(self, tag):
        if tag in ("script", "style") and self.skip:
            self.skip -= 1

    def handle_data(self, data):
        if self.skip:
            return
        t = data.strip()
        if t:
            self.parts.append(t + " ")


def strip_html(s: str) -> str:
    if not s:
        return ""
    text = re.sub(r"<script[\s\S]*?</script>", " ", s, flags=re.I)
    text = re.sub(r"<style[\s\S]*?</style>", " ", text, flags=re.I)
    text = re.sub(r"<br\s*/?>", "\n", text, flags=re.I)
    text = re.sub(r"</(p|tr|h1|h2|h3|h4|div)>", "\n", text, flags=re.I)
    text = re.sub(r"<[^>]+>", " ", text)
    text = htmlmod.unescape(text)
    return re.sub(r"[ \t]+", " ", text)


def load_text(cid: int) -> str:
    path = os.path.join(PAGES, "%d.json" % cid)
    with open(path, encoding="utf-8") as f:
        d = json.load(f)
    return strip_html((d.get("data") or {}).get("content") or "")


def rows():
    with open(CSV_PATH, encoding="utf-8-sig") as f:
        return list(csv.DictReader(f))


def dump(name, obj):
    os.makedirs(PARSED, exist_ok=True)
    path = os.path.join(PARSED, name)
    with open(path, "w", encoding="utf-8") as f:
        json.dump(obj, f, ensure_ascii=False, indent=2)
    print(name, len(obj) if hasattr(obj, "__len__") else "?", "->", path)


def parse_cartas(all_rows):
    items = []
    mode = None
    for r in all_rows:
        if r["section"] != "图鉴资料":
            continue
        if not r["content_id"]:
            if (r["name"] or "").startswith("歌牌") or r["name"] in ("4星", "3星"):
                if (r["name"] or "").startswith("歌牌"):
                    mode = "carta5"
                elif r["name"] == "4星":
                    mode = "carta4"
                elif r["name"] == "3星":
                    mode = "carta3"
            elif (r["name"] or "").startswith("5星天子") or (r["name"] or "").startswith("人偶"):
                mode = None
            continue
        if not mode or not mode.startswith("carta"):
            continue
        cid = int(r["content_id"])
        text = load_text(cid) if os.path.isfile(os.path.join(PAGES, "%d.json" % cid)) else ""
        rarity = 5 if mode == "carta5" else 4 if mode == "carta4" else 3
        items.append(
            {
                "content_id": cid,
                "name": r["name"],
                "rarity": rarity,
                "text": text[:1200],
                "stat_pair": None,
                "element_gate": None,
                "role_gate": None,
                "mode_gate": None,
                "plain": None,
                "flash": None,
                "special": None,
            }
        )
        t = text
        for lab, key in (("血攻", "stat_pair"), ("血防", "stat_pair"), ("血暴", "stat_pair"), ("血敏", "stat_pair"), ("攻暴", "stat_pair"), ("攻敏", "stat_pair"), ("防敏", "stat_pair")):
            if lab in t:
                items[-1][key] = lab
                break
        if "PVP" in t or "pvp" in t:
            items[-1]["mode_gate"] = "pvp"
        elif "巨型boss" in t or "5人" in t:
            items[-1]["mode_gate"] = "raid_or_wb"
        items[-1]["special"] = t[:300] if t else None
    return items


def parse_puppets(all_rows):
    items = []
    mode = False
    for r in all_rows:
        if r["section"] != "图鉴资料":
            continue
        if not r["content_id"]:
            n = r["name"] or ""
            if n.startswith("人偶") or n in ("史诗", "稀有", "罕见", "常见"):
                mode = True
            elif n.startswith("歌牌") or n.startswith("5星天子") or n.startswith("4星天子"):
                mode = False
            continue
        if not mode:
            continue
        cid = int(r["content_id"])
        text = load_text(cid) if os.path.isfile(os.path.join(PAGES, "%d.json" % cid)) else ""
        rarity = "unknown"
        p = r["path"]
        if "传奇" in p:
            rarity = "legendary"
        elif "史诗" in p:
            rarity = "epic"
        elif "稀有" in p:
            rarity = "rare"
        elif "罕见" in p:
            rarity = "uncommon"
        elif "常见" in p:
            rarity = "common"
        items.append(
            {
                "content_id": cid,
                "name": r["name"],
                "rarity": rarity,
                "element": None,
                "tap": None,
                "slide": None,
                "drive": None,
                "leader": None,
                "text": text[:800],
            }
        )
    return items


def parse_gear():
    out = {}
    for cid, slot in ((20925, "weapon"), (20926, "armor"), (20930, "accessory")):
        text = load_text(cid)
        nums = re.findall(r"\((\+15)\)\s*\|?\s*([0-9]+)\s*([0-9]+)?", text)
        out[slot] = {"content_id": cid, "text_chars": len(text), "plus15_samples": nums[:20], "raw_head": text[:1500]}
    return out


def parse_buffs():
    text = load_text(20902)
    items = []
    kind = "buff"
    if "Debuff" in text:
        pre, post = text.split("Debuff", 1)
    else:
        pre, post = text, ""
    def harvest(blob, kind):
        found = []
        for m in re.finditer(r"([^\n|]{2,16})\s+([^\n]{4,80})", blob):
            name, effect = m.group(1).strip(" |"), m.group(2).strip(" |")
            if any(x in name for x in ("图标", "名称", "效果", "Buff", "注")):
                continue
            if "增加" in effect or "减少" in effect or "造成" in effect or "无法" in effect or "赋予" in effect or "免疫" in effect or "回复" in effect:
                found.append({"name": name, "effect": effect, "kind": kind, "stack_group": name})
        return found
    items.extend(harvest(pre, "buff"))
    items.extend(harvest(post, "debuff"))
    # de-dup
    seen = set()
    uniq = []
    for it in items:
        if it["name"] in seen:
            continue
        seen.add(it["name"])
        uniq.append(it)
    return uniq


def main() -> int:
    all_rows = rows()
    cartas = parse_cartas(all_rows)
    puppets = parse_puppets(all_rows)
    dump("soul_cartas.json", cartas)
    dump("puppets.json", puppets)
    dump("equipment.json", parse_gear())
    buffs = parse_buffs()
    dump("buffs.json", buffs)
    errors = []
    if len(cartas) < 156:
        errors.append("cartas %d < 156" % len(cartas))
    if len(puppets) < 218:
        errors.append("puppets %d < 218" % len(puppets))
    if errors:
        print("WARN", "; ".join(errors))
        return 0
    print("aux OK")
    return 0


if __name__ == "__main__":
    sys.exit(main())
