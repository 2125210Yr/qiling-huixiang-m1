# -*- coding: utf-8 -*-
"""Pull ignition / formula text from harvested GameKee JSON."""
from __future__ import annotations

import json
import os
import re
from html.parser import HTMLParser

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
PAGES = os.path.join(ROOT, "docs", "reference", "gamekee", "pages")
OUT = os.path.join(ROOT, "docs", "reference", "gamekee", "parsed")

IDS = {
    "ts_ss_ft": 52136,
    "dmg_method": 22457,
    "ignition_intro": 62504,
    "ignition_teach": 168169,
    "ignition_pick": 68263,
    "ignition_affix": 62628,
    "food": 65564,
}


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
    p = TextExtractor()
    try:
        p.feed(s or "")
    except Exception:
        s = re.sub(r"<[^>]+>", " ", s or "")
        return re.sub(r"[ \t]+", " ", s)
    text = "".join(p.parts)
    text = re.sub(r"\n{3,}", "\n\n", text)
    return text.strip()


def load_page(cid: int) -> str:
    path = os.path.join(PAGES, "%d.json" % cid)
    if not os.path.isfile(path):
        return ""
    with open(path, encoding="utf-8") as f:
        d = json.load(f)
    data = d.get("data") or {}
    content = data.get("content") or ""
    return strip_html(content)


def main():
    os.makedirs(OUT, exist_ok=True)
    chunks = []
    for key, cid in IDS.items():
        text = load_page(cid)
        print(key, cid, "chars", len(text))
        dest = os.path.join(OUT, "wiki_%s.txt" % key)
        with open(dest, "w", encoding="utf-8") as f:
            f.write(text[:80000])
        chunks.append((key, cid, text[:4000]))
    # print heads
    for key, cid, t in chunks:
        print("\n====", key, cid, "====")
        print(t[:1200].replace("\n", " | ")[:1200])


if __name__ == "__main__":
    main()
