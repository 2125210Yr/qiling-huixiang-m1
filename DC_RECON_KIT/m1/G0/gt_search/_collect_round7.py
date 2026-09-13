# Round-7: exact-title / uploader collect. No YouTube yt-dlp.
import json
import os
import subprocess
import urllib.parse
import urllib.request

OUT = r"F:\天命之子\DC_RECON_KIT\m1\G0\gt_search\_collect_round7.json"
DEST = r"F:\天命之子\docs\reference\gl-shutdown-pve"
KNOWN = set()

for root, _, fns in os.walk(DEST):
    for fn in fns:
        if fn.lower().endswith((".mp4", ".webm")):
            KNOWN.add(fn.lower())
            for part in fn.replace(".", "_").replace("-", "_").split("_"):
                if part.startswith("BV") and len(part) >= 10:
                    KNOWN.add(part)

UA = (
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 "
    "(KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36"
)

QUERIES = [
    "Destiny Child Gameplay English Version Part 1",
    "Eternal Vow Bathory Destiny Child",
    "Destiny Child Global ROBIN ND",
    "天命之子国际服 主线 FEVER",
    "天命之子国际服 日常 战斗 HUD",
    "aSbBuFD12HY",
    "IRDqNAhKKr4",
    "hgqXY5M9gFk",
]


def get_json(url: str):
    req = urllib.request.Request(url, headers={"User-Agent": UA, "Referer": "https://www.bilibili.com/"})
    with urllib.request.urlopen(req, timeout=20) as r:
        return json.loads(r.read().decode("utf-8", "replace"))


def search(q: str):
    url = (
        "https://api.bilibili.com/x/web-interface/wbi/search/type?"
        + urllib.parse.urlencode({"search_type": "video", "keyword": q, "page": 1})
    )
    try:
        data = get_json(url)
        return {"ok": True, "code": data.get("code"), "data": data}
    except Exception as e:
        url2 = (
            "https://api.bilibili.com/x/web-interface/search/type?"
            + urllib.parse.urlencode({"search_type": "video", "keyword": q, "page": 1})
        )
        try:
            data = get_json(url2)
            return {"ok": True, "code": data.get("code"), "data": data, "fallback": True}
        except Exception as e2:
            return {"ok": False, "err": str(e), "err2": str(e2)}


def uploader(mid: int):
    url = (
        "https://api.bilibili.com/x/space/wbi/arc/search?"
        + urllib.parse.urlencode({"mid": mid, "ps": 50, "pn": 1})
    )
    try:
        return {"ok": True, "data": get_json(url)}
    except Exception as e:
        return {"ok": False, "err": str(e)}


hits = []
report = {"queries": [], "uploader": None, "hits": []}
for q in QUERIES:
    res = search(q)
    report["queries"].append({"q": q, "ok": res.get("ok"), "code": res.get("code"), "err": res.get("err")})
    result = (((res.get("data") or {}).get("data") or {}).get("result")) or []
    for item in result:
        bvid = item.get("bvid") or ""
        title = (item.get("title") or "").replace("<em class=\"keyword\">", "").replace("</em>", "")
        pub = item.get("pubdate")
        hits.append({"q": q, "bvid": bvid, "title": title, "pubdate": pub, "duration": item.get("duration")})

report["uploader"] = uploader(11782165)
report["hits"] = hits

os.makedirs(os.path.dirname(OUT), exist_ok=True)
with open(OUT, "w", encoding="utf-8") as f:
    json.dump(report, f, ensure_ascii=False, indent=2)
print("wrote", OUT)
print("queries", [(x["q"], x.get("ok"), x.get("code"), x.get("err")) for x in report["queries"]])
print("hits", len(hits))
for h in hits[:30]:
    print(h.get("bvid"), h.get("title"))
