# -*- coding: utf-8 -*-
import json
import os
import re
import urllib.request

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
GK = os.path.join(ROOT, "docs", "reference", "gamekee")


def get(alias, path):
    req = urllib.request.Request(
        "https://www.gamekee.com" + path,
        headers={
            "Game-Alias": alias,
            "User-Agent": "Mozilla/5.0",
            "Referer": "https://www.gamekee.com/%s/" % alias,
        },
    )
    with urllib.request.urlopen(req, timeout=30) as r:
        return json.loads(r.read().decode("utf-8"))


def walk(nodes, path, acc):
    for n in nodes or []:
        if not isinstance(n, dict):
            continue
        p = path + [n.get("name") or ""]
        if n.get("content_id"):
            acc.append({"path": " / ".join(p), "content_id": n["content_id"], "name": n.get("name") or ""})
        walk(n.get("child") or [], p, acc)


def show_page(alias, cid):
    d = get(alias, "/v1/content/detail/%s" % cid).get("data") or {}
    content = d.get("content") or ""
    imgs = re.findall(r"(?:data-real|src)=\"(//[^\"]+)\"", content)
    thumbs = d.get("thumb_list") or []
    print("PAGE", alias, cid, "title=", d.get("title"), "imgs", len(imgs), "thumbs", len(thumbs))
    for u in (thumbs[:3] + imgs[:5]):
        print("  ", u[:120])


def main():
    kr = json.load(open(os.path.join(GK, "wiki_tree_destinychild.json"), encoding="utf-8"))
    jp = json.load(open(os.path.join(GK, "wiki_tree_dcj.json"), encoding="utf-8"))
    acc_kr, acc_jp = [], []
    walk(kr, [], acc_kr)
    walk(jp, [], acc_jp)
    kr4 = [x for x in acc_kr if "四星" in x["path"]]
    kr5 = [x for x in acc_kr if "五星" in x["path"] and x["name"] not in ("五星光", "五星暗", "五星火", "五星木", "五星水")]
    kr5 = [x for x in kr5 if x["name"].strip() not in ("五星光", "五星暗", "五星火", "五星木", "五星水")]
    jp5 = [x for x in acc_jp if x["path"].startswith("五星天子图鉴") and "检索" not in x["path"]]
    print("KR4", len(kr4), kr4[:5])
    print("KR5", len(kr5), kr5[:5])
    print("JP5", len(jp5), jp5[:5])
    if kr4:
        show_page("destinychild", kr4[0]["content_id"])
    if kr5:
        show_page("destinychild", kr5[0]["content_id"])
    if jp5:
        show_page("dcj", jp5[0]["content_id"])
    # listing pages
    for name, cid in [("全天子文字版", 154546), ("天子定位图鉴", 153517), ("有用的3星4星", 154835)]:
        try:
            show_page("destinychild", cid)
        except Exception as e:
            print("fail", name, e)


if __name__ == "__main__":
    main()
