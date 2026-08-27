# -*- coding: utf-8 -*-
"""Parse 5★/4★ child wiki pages into parsed/children.json (reference, original names OK)."""
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
OUT = os.path.join(ROOT, "docs", "reference", "gamekee", "parsed", "children.json")


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
        if tag in ("p", "li", "tr", "h1", "h2", "h3", "h4"):
            self.parts.append("\n")

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
    text = re.sub(r"[ \t]+", " ", text)
    text = re.sub(r"\n\s*\n+", "\n", text)
    return text.strip()


def page_text(cid: int) -> str:
    path = os.path.join(PAGES, "%d.json" % cid)
    with open(path, encoding="utf-8") as f:
        d = json.load(f)
    data = d.get("data") or {}
    return strip_html(data.get("content") or "")


def grab_after(text: str, labels, limit=80):
    for lab in labels:
        m = re.search(re.escape(lab) + r"\s*[:：|]?\s*(.+)", text)
        if m:
            return m.group(1).strip()[:limit]
    return None


def parse_one(name: str, cid: int, path: str) -> dict:
    raw = page_text(cid)
    el = None
    for token, key in (("火", "火"), ("水", "水"), ("木", "木"), ("光", "光"), ("暗", "暗"), ("闇", "暗")):
        if token + "属性" in raw or "| " + token + " |" in raw or "属性 | " + token in raw:
            el = key
            break
        if re.search(r"\b%s\b" % token, raw[:400]):
            el = key
            break
    role = None
    for token in ("攻击型", "攻擊型", "防御型", "防禦型", "干扰型", "干擾型", "治疗", "治療", "辅助", "輔助"):
        if token in raw:
            role = token
            break
    def skill_blob(keys):
        for k in keys:
            m = re.search(k + r"([\s\S]{0,800})", raw)
            if m:
                blob = re.sub(r"\s+", " ", m.group(0))
                if len(blob) > 12:
                    return blob[:800]
        return None

    stats = {"initial": {}, "uncap6": {}}
    m = re.search(r"初始[^\n]{0,40}?(\d{3,6}).{0,80}?(\d{3,6}).{0,40}?(\d{3,6}).{0,40}?(\d{3,6}).{0,40}?(\d{3,6}).{0,40}?(\d{3,6})", raw)
    # too brittle; keep numbers near 面板
    nums = re.findall(r"初始\s*\n?\s*\|?\s*(\d{3,6})\s*\|?\s*(\d{3,6})\s*\|?\s*(\d{3,6})\s*\|?\s*(\d{3,6})\s*\|?\s*(\d{3,6})\s*\|?\s*(\d{3,6})", raw)
    if nums:
        a = nums[0]
        stats["initial"] = {"cp": int(a[0]), "hp": int(a[1]), "atk": int(a[2]), "def": int(a[3]), "agl": int(a[4]), "crt": int(a[5])}
    nums2 = re.findall(r"满破\s*\n?\s*\|?\s*(\d{3,6})\s*\|?\s*(\d{3,6})\s*\|?\s*(\d{3,6})\s*\|?\s*(\d{3,6})\s*\|?\s*(\d{3,6})\s*\|?\s*(\d{3,6})", raw)
    if nums2:
        a = nums2[0]
        stats["uncap6"] = {"cp": int(a[0]), "hp": int(a[1]), "atk": int(a[2]), "def": int(a[3]), "agl": int(a[4]), "crt": int(a[5])}

    return {
        "content_id": cid,
        "name": name,
        "path": path,
        "element": el,
        "role": role,
        "auto": skill_blob(["普攻", "自动攻击"]),
        "tap": skill_blob(["重击", "TS", "NORMAL"]),
        "slide": skill_blob(["滑动", "SS", "SLIDE"]),
        "drive": skill_blob(["大招", "DS", "DRIVE"]),
        "leader": skill_blob(["队长技", "LEADER"]),
        "stats": stats,
        "raw_chars": len(raw),
    }


def child_rows():
    rows = []
    with open(CSV_PATH, encoding="utf-8-sig") as f:
        for r in csv.DictReader(f):
            if r["section"] != "图鉴资料" or not r["content_id"]:
                continue
            p = r["path"]
            if any(x in p for x in ("歌牌", "人偶", "图鉴列表", "史诗", "稀有", "罕见", "常见")):
                continue
            if "5星天子" in p or "4星天子" in p:
                rows.append(r)
                continue
            # sibling element folders counted as children when mode is character
            parts = p.split(" / ")
            if len(parts) == 3 and parts[1] in ("暗", "火", "木", "水", "光"):
                rows.append(r)
    return rows


def main() -> int:
    if not os.path.isfile(CSV_PATH):
        print("missing", CSV_PATH, file=sys.stderr)
        return 1
    items = []
    for r in child_rows():
        cid = int(r["content_id"])
        page = os.path.join(PAGES, "%d.json" % cid)
        if not os.path.isfile(page):
            items.append({"content_id": cid, "name": r["name"], "path": r["path"], "missing_page": True})
            continue
        items.append(parse_one(r["name"], cid, r["path"]))
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, "w", encoding="utf-8") as f:
        json.dump(items, f, ensure_ascii=False, indent=2)
    print("children", len(items), "->", OUT)

    # Thor / 索尔 assertions if present
    thor = next((x for x in items if x.get("name") == "索尔"), None)
    if thor:
        ok = True
        if thor.get("element") != "火":
            print("WARN 索尔 element", thor.get("element"))
            ok = False
        tap = thor.get("tap") or ""
        if "2243" not in tap:
            print("WARN 索尔 tap missing 2243")
            ok = False
        slide = thor.get("slide") or ""
        if "不死" not in slide:
            print("WARN 索尔 slide missing 不死")
            ok = False
        leader = thor.get("leader") or ""
        if "忍耐" not in leader:
            print("WARN 索尔 leader missing 忍耐")
            ok = False
        if ok:
            print("索尔 assertions OK")
    return 0


if __name__ == "__main__":
    sys.exit(main())
