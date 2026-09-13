import datetime
import json
import os

p = r"F:\天命之子\DC_RECON_KIT\m1\G0\gt_search\_collect_round7.json"
d = json.load(open(p, encoding="utf-8"))
known = set()
for root, _, fns in os.walk(r"F:\天命之子\docs\reference\gl-shutdown-pve"):
    for fn in fns:
        known.add(fn)
        if "BV" in fn:
            i = fn.find("BV")
            known.add(fn[i : i + 12])

keys = ("天命", "Destiny Child", "destiny child", "国际服", "FEVER", "ROBIN", "Bathory", "天命之子")
rows = []
seen = set()
for h in d["hits"]:
    t = h.get("title") or ""
    b = h.get("bvid") or ""
    if not b or b in seen:
        continue
    if not any(k.lower() in t.lower() or k in t for k in keys):
        continue
    seen.add(b)
    pub = h.get("pubdate")
    ds = datetime.datetime.utcfromtimestamp(pub).strftime("%Y-%m-%d") if pub else "?"
    have = "HAVE" if any(b in k for k in known) else "NEW"
    dur = h.get("duration")
    rows.append("%s\t%s\t%s\t%s\t%s" % (have, b, ds, dur, t))

out = r"F:\天命之子\DC_RECON_KIT\m1\G0\gt_search\_collect_round7_hits.txt"
open(out, "w", encoding="utf-8").write("\n".join(rows))
print("rows", len(rows))
