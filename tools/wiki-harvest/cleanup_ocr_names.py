# -*- coding: utf-8 -*-
"""Keep only clean C5 OCR names; otherwise restore wiki_name."""
import csv, re, os
USER = r"F:\天命之子\天命之子数据"
CSV_CHILD = os.path.join(USER, "汇总表.csv")
BAD = re.compile(r"董藤|重斯|戈伊|雷斯尼|面$")
CJK = re.compile(r"^[\u4e00-\u9fffA-Za-z·]{2,10}$")

with open(CSV_CHILD, encoding="utf-8-sig") as f:
    rows = list(csv.DictReader(f))
    fields = list(rows[0].keys())
n = 0
for r in rows:
    av = r.get("avatar_file") or ""
    if "C5f_" not in av and "C3f_" not in av:
        continue
    name = (r.get("name") or "").strip()
    wiki = (r.get("wiki_name") or "").strip()
    if wiki and name and name in wiki:
        r["name"] = wiki
        n += 1
        continue
    if not CJK.match(name) or BAD.search(name):
        if wiki:
            r["name"] = wiki
            n += 1
with open(CSV_CHILD, "w", encoding="utf-8-sig", newline="") as f:
    w = csv.DictWriter(f, fieldnames=fields, extrasaction="ignore")
    w.writeheader()
    w.writerows(rows)
print("restored", n)
