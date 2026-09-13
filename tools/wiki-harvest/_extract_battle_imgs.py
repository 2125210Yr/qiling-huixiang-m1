# -*- coding: utf-8 -*-
from __future__ import annotations

import json
import os
import re
import urllib.request

ROOT = r"F:\天命之子\docs\reference\gamekee"
OUT = os.path.join(ROOT, "_combined", "battle_refs")
os.makedirs(OUT, exist_ok=True)

PAGES = [
    (os.path.join(ROOT, "pages", "77671.json"), "sys"),
    (os.path.join(ROOT, "pages", "22652.json"), "raid_intro"),
    (os.path.join(ROOT, "pages", "22411.json"), "wb_intro"),
    (os.path.join(ROOT, "destinychild", "pages", "53131.json"), "kr_raid"),
    (os.path.join(ROOT, "destinychild", "pages", "169159.json"), "kr_wb"),
    (os.path.join(ROOT, "destinychild", "pages", "164372.json"), "kr_pvp"),
    (os.path.join(ROOT, "destinychild", "pages", "168406.json"), "kr_rumble"),
    (os.path.join(ROOT, "destinychild", "pages", "165459.json"), "kr_ign"),
    (os.path.join(ROOT, "dcj", "pages", "162007.json"), "jp_new"),
]

URL_RE = re.compile(r"(?:https?:)?//cdnimg[^\"'\s<>]+?\.(?:png|jpg|jpeg|webp|gif)", re.I)
UA = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/124.0.0.0 Safari/537.36"


def abs_url(u: str) -> str:
    u = u.replace("\\/", "/")
    if u.startswith("//"):
        return "https:" + u
    return u


n = 0
seen = set()
for path, prefix in PAGES:
    if not os.path.isfile(path):
        print("missing", prefix)
        continue
    raw = open(path, encoding="utf-8").read()
    title = ""
    try:
        title = (json.loads(raw).get("data") or {}).get("title") or ""
    except Exception:
        pass
    urls = []
    for m in URL_RE.findall(raw):
        u = abs_url(m)
        if u in seen:
            continue
        if any(x in u.lower() for x in ("emoji", "avatar", "icon_")):
            continue
        seen.add(u)
        urls.append(u)
    print(prefix, title, "imgs", len(urls))
    for i, u in enumerate(urls[:8], 1):
        try:
            req = urllib.request.Request(
                u,
                headers={"User-Agent": UA, "Referer": "https://www.gamekee.com/"},
            )
            with urllib.request.urlopen(req, timeout=25) as r:
                blob = r.read()
            if len(blob) < 4000:
                print("  skip small", u[-50:], len(blob))
                continue
            n += 1
            ext = ".jpg"
            for e in (".png", ".webp", ".jpeg", ".gif", ".jpg"):
                if e in u.lower():
                    ext = e
                    break
            out = os.path.join(OUT, "%s_%02d%s" % (prefix, i, ext))
            with open(out, "wb") as f:
                f.write(blob)
            print("  saved", os.path.basename(out), len(blob))
        except Exception as e:
            print("  fail", e, u[-60:])

print("DONE files", n)
print("dir", os.listdir(OUT))
