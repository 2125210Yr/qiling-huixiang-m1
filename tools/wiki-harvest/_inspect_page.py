# -*- coding: utf-8 -*-
import json, re, sys
from html.parser import HTMLParser

path = sys.argv[1]
d = json.load(open(path, encoding="utf-8"))
data = d.get("data") or {}
print("title", data.get("title"))
print("keys", sorted(data.keys()))
for k in ("content_json", "entry_data_bind", "content_tj_eq"):
    v = data.get(k)
    print(k, type(v).__name__, (len(v) if isinstance(v, (str, list, dict)) else v))
    if isinstance(v, str):
        print("  head", v[:200].replace("\n", " "))
    elif isinstance(v, dict):
        print("  subkeys", list(v.keys())[:20])
    elif isinstance(v, list) and v:
        print("  list0", str(v[0])[:200])

c = data.get("content") or ""
print("content_len", len(c))
print("tables", c.count("<table"), "tr", c.count("<tr"), "td", c.count("<td"))

class Cells(HTMLParser):
    def __init__(self):
        super().__init__()
        self.cell = None
        self.rows = []
        self.row = []
        self.in_td = 0
        self.in_tr = 0
    def handle_starttag(self, tag, attrs):
        if tag == "tr":
            self.in_tr += 1
            self.row = []
        if tag in ("td", "th"):
            self.in_td += 1
            self.cell = []
    def handle_endtag(self, tag):
        if tag in ("td", "th") and self.in_td:
            self.in_td -= 1
            text = re.sub(r"\s+", " ", "".join(self.cell)).strip()
            self.row.append(text)
            self.cell = None
        if tag == "tr" and self.in_tr:
            self.in_tr -= 1
            if any(self.row):
                self.rows.append(self.row)
    def handle_data(self, data):
        if self.in_td and self.cell is not None:
            self.cell.append(data)

p = Cells()
try:
    p.feed(c)
except Exception as e:
    print("parse err", e)
print("parsed rows", len(p.rows))
for i, r in enumerate(p.rows[:40]):
    print(i, r)
