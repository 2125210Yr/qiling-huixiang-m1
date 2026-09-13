# -*- coding: utf-8 -*-
import json, sys
sys.path.insert(0, r"F:\天命之子\tools\wiki-harvest")
from _inspect_page import Cells
from html.parser import HTMLParser
import re

p = r"F:\天命之子\docs\reference\gamekee\pages\20925.json"
d = json.load(open(p, encoding="utf-8"))
c = d["data"]["content"]
parser = Cells()
parser.feed(c)
print("weapon rows", len(parser.rows))
for i, r in enumerate(parser.rows[:25]):
    print(i, r[:8])
print("---")
p2 = r"F:\天命之子\docs\reference\gamekee\pages\169197.json"
d2 = json.load(open(p2, encoding="utf-8"))
print("puppet title", d2["data"]["title"])
parser2 = Cells()
parser2.feed(d2["data"]["content"])
print("puppet rows", len(parser2.rows))
for i, r in enumerate(parser2.rows[:20]):
    print(i, r)
