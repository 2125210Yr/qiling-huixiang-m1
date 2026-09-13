# -*- coding: utf-8 -*-
"""Match CN/JP/KR wiki portraits onto the 561 memorial table; rebuild 汇总表."""
from __future__ import annotations

import csv
import os
import re
import shutil
from collections import defaultdict
from difflib import SequenceMatcher

from zhconv import convert as zh_convert

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
USER = os.path.join(ROOT, "天命之子数据")
GK = os.path.join(ROOT, "docs", "reference", "gamekee")
TABLES = os.path.join(GK, "tables")

CJK = re.compile(r"(?<=[\u4e00-\u9fff])\s+(?=[\u4e00-\u9fff])")


def simp(s: str) -> str:
    s = CJK.sub("", s or "")
    return zh_convert(re.sub(r"\s+", "", s), "zh-cn").strip()


def core(s: str) -> str:
    s = simp(s)
    s = s.replace("的", "")
    s = re.sub(r"[♥♡★☆·・\(\)（）\s]", "", s)
    # JP titles like 狄刻奶光
    return s


def load_csv(path: str):
    if not os.path.isfile(path):
        return []
    with open(path, encoding="utf-8-sig") as f:
        return list(csv.DictReader(f))


def score(a: str, b: str) -> int:
    ca, cb = core(a), core(b)
    if not ca or not cb or len(ca) < 2 or len(cb) < 2:
        return 0
    if ca == cb:
        return 200
    # 黑炎/白炎 这类反义前缀不要当成同人
    if ca[-3:] == cb[-3:] and len(ca) >= 4 and len(cb) >= 4:
        if {ca[0], cb[0]} <= set("黑白红蓝") and ca[0] != cb[0]:
            return 0
    best = 0
    if ca.endswith(cb) or cb.endswith(ca):
        shorter, longer = (ca, cb) if len(ca) <= len(cb) else (cb, ca)
        if len(shorter) >= 3 and len(shorter) / float(len(longer)) >= 0.35:
            best = max(best, 120 + len(shorter))
    if ca in cb or cb in ca:
        best = max(best, 90 + min(len(ca), len(cb)))
    for n in range(min(len(ca), len(cb), 6), 2, -1):
        if ca[-n:] == cb[-n:]:
            best = max(best, 50 + n * 8)
            break
    sa, sb = simp(a), simp(b)
    pref = 0
    for x, y in zip(sa, sb):
        if x == y:
            pref += 1
        else:
            break
    if pref >= 3:
        best = max(best, 72 + pref)
    if best:
        return best
    return int(SequenceMatcher(None, ca, cb).ratio() * 80)


def unique_assign(left, right, left_name, right_name, min_score=70):
    pairs = []
    for i, L in enumerate(left):
        for j, R in enumerate(right):
            s = score(left_name(L), right_name(R))
            if s >= min_score:
                pairs.append((s, i, j))
    pairs.sort(key=lambda x: -x[0])
    used_l, used_r = set(), set()
    mapping = {}
    for s, i, j in pairs:
        if i in used_l or j in used_r:
            continue
        used_l.add(i)
        used_r.add(j)
        mapping[i] = right[j]
    return mapping


def main():
    table = load_csv(os.path.join(USER, "汇总表.csv"))
    cn = load_csv(os.path.join(TABLES, "characters.csv"))
    cross = load_csv(os.path.join(GK, "cross_index.csv"))
    jp = [r for r in cross if r.get("alias") == "dcj"]
    kr = [r for r in cross if r.get("alias") == "destinychild"]
    kr4 = [r for r in kr if "四星" in (r.get("path") or "")]
    kr5 = [r for r in kr if "五星" in (r.get("path") or "")]
    kr3 = [r for r in kr if "三星" in (r.get("path") or "")]
    print("table", len(table), "cn", len(cn), "jp", len(jp), "kr5", len(kr5), "kr4", len(kr4), "kr3", len(kr3))

    t5 = [r for r in table if (r.get("rarity") or "").startswith("5")]
    t4 = [r for r in table if (r.get("rarity") or "").startswith("4")]
    t3 = [r for r in table if (r.get("rarity") or "").startswith("3")]

    # CN match 5/4
    m_cn5 = unique_assign(t5, cn, lambda x: x.get("name") or "", lambda x: x.get("name") or "", 64)
    m_cn4 = unique_assign(t4, cn, lambda x: x.get("name") or "", lambda x: x.get("name") or "", 60)
    # KR 4-star (Chinese names, 77/77)
    m_kr4 = unique_assign(t4, kr4, lambda x: x.get("name") or "", lambda x: x.get("title") or x.get("name") or "", 60)
    m_kr5 = unique_assign(t5, kr5, lambda x: x.get("name") or "", lambda x: x.get("title") or x.get("name") or "", 64)
    m_jp5 = unique_assign(t5, jp, lambda x: x.get("name") or "", lambda x: x.get("title") or x.get("name") or "", 64)
    m_kr3 = unique_assign(t3, kr3, lambda x: x.get("name") or "", lambda x: x.get("title") or x.get("name") or "", 60)

    print("match CN5", len(m_cn5), "CN4", len(m_cn4), "KR4", len(m_kr4), "KR5", len(m_kr5), "JP5", len(m_jp5), "KR3", len(m_kr3))

    def apply_avatar(row, src, tag):
        f = (src or {}).get("avatar_file") or ""
        if not f:
            cid = (src or {}).get("content_id")
            if cid:
                for folder, prefix in ((os.path.join(USER, "avatars"), ""), (os.path.join(USER, "avatars_wiki"), tag + "_")):
                    for ext in (".png", ".jpg", ".webp"):
                        p = os.path.join(folder, "%s%s%s" % (prefix, cid, ext) if prefix else "%s%s" % (cid, ext))
                        if os.path.isfile(p):
                            rel = os.path.relpath(p, USER).replace("\\", "/")
                            row["avatar_file"] = rel
                            row["avatar_source"] = tag
                            return True
            return False
        p = os.path.join(USER, f.replace("/", os.sep))
        if os.path.isfile(p):
            row["avatar_file"] = f.replace("\\", "/")
            row["avatar_source"] = tag
            return True
        return False

    # reset screenshot / idx-based icons for 5/4
    for r in table:
        av = r.get("avatar_file") or ""
        if av.startswith("avatars_561/") or "/R5_" in av or "/R4_" in av or av.startswith("icons/R5") or av.startswith("icons/R4"):
            r["avatar_file"] = ""

    hits = defaultdict(int)
    for i, row in enumerate(t5):
        if i in m_cn5 and apply_avatar(row, m_cn5[i], "cn"):
            cid = m_cn5[i].get("content_id")
            p = os.path.join(USER, "avatars", "%s.png" % cid)
            if os.path.isfile(p):
                row["avatar_file"] = "avatars/%s.png" % cid
                row["avatar_source"] = "cn"
                hits["cn5"] += 1
                continue
            p = os.path.join(USER, "avatars", "%s.jpg" % cid)
            if os.path.isfile(p):
                row["avatar_file"] = "avatars/%s.jpg" % cid
                row["avatar_source"] = "cn"
                hits["cn5"] += 1
                continue
        if i in m_kr5 and apply_avatar(row, m_kr5[i], "destinychild"):
            hits["kr5"] += 1
            continue
        if i in m_jp5 and apply_avatar(row, m_jp5[i], "dcj"):
            hits["jp5"] += 1
            continue
        hits["miss5"] += 1

    for i, row in enumerate(t4):
        if i in m_cn4:
            cid = m_cn4[i].get("content_id")
            p = os.path.join(USER, "avatars", "%s.png" % cid)
            if os.path.isfile(p):
                row["avatar_file"] = "avatars/%s.png" % cid
                row["avatar_source"] = "cn"
                hits["cn4"] += 1
                continue
        if i in m_kr4 and apply_avatar(row, m_kr4[i], "destinychild"):
            hits["kr4"] += 1
            continue
        hits["miss4"] += 1

    for i, row in enumerate(t3):
        if i in m_kr3 and apply_avatar(row, m_kr3[i], "destinychild"):
            hits["kr3"] += 1

    # leftover 5/4: only unmatched rows vs unused JP/KR pages
    def taken_files():
        s = set()
        for r in table:
            av = r.get("avatar_file") or ""
            if av:
                s.add(os.path.basename(av))
        return s

    taken = taken_files()
    used_jp = {id(j) for j in jp if os.path.basename(j.get("avatar_file") or "") in taken}
    used_kr5 = {id(j) for j in kr5 if os.path.basename(j.get("avatar_file") or "") in taken}
    used_kr4 = {id(j) for j in kr4 if os.path.basename(j.get("avatar_file") or "") in taken}
    left5 = [r for r in t5 if not (r.get("avatar_file") or "").startswith("avatars")]
    left4 = [r for r in t4 if not (r.get("avatar_file") or "").startswith("avatars")]
    jp_left = [j for j in jp if id(j) not in used_jp]
    kr5_left = [j for j in kr5 if id(j) not in used_kr5]
    kr4_left = [j for j in kr4 if id(j) not in used_kr4]
    m2jp = unique_assign(left5, jp_left, lambda x: x.get("name") or "", lambda x: x.get("title") or x.get("name") or "", 55)
    m2k5 = unique_assign(left5, kr5_left, lambda x: x.get("name") or "", lambda x: x.get("title") or x.get("name") or "", 55)
    m2k4 = unique_assign(left4, kr4_left, lambda x: x.get("name") or "", lambda x: x.get("title") or x.get("name") or "", 55)
    print("leftover match JP", len(m2jp), "KR5", len(m2k5), "KR4", len(m2k4))
    for i, row in enumerate(left5):
        if (row.get("avatar_file") or "").startswith("avatars"):
            continue
        if i in m2jp and apply_avatar(row, m2jp[i], "dcj"):
            hits["jp5b"] += 1
        elif i in m2k5 and apply_avatar(row, m2k5[i], "destinychild"):
            hits["kr5b"] += 1
    for i, row in enumerate(left4):
        if (row.get("avatar_file") or "").startswith("avatars"):
            continue
        if i in m2k4 and apply_avatar(row, m2k4[i], "destinychild"):
            hits["kr4b"] += 1

    # 3/2/1 keep named/catalog icons if present
    named_path = os.path.join(USER, "_extract", "named_icons.jsonl")
    named = []
    if os.path.isfile(named_path):
        import json
        for line in open(named_path, encoding="utf-8"):
            if line.startswith("{"):
                named.append(json.loads(line))
    used = set()
    for row in table:
        rar = (row.get("rarity") or "")[:1]
        av = row.get("avatar_file") or ""
        if av.startswith("avatars/") or av.startswith("avatars_wiki/"):
            continue
        if rar in ("2", "1"):
            ix = row.get("idx")
            try:
                ix = int(ix)
            except Exception:
                continue
            p = os.path.join(USER, "icons", "R%s_%03d.png" % (rar, ix))
            if os.path.isfile(p):
                row["avatar_file"] = "icons/R%s_%03d.png" % (rar, ix)
                row["avatar_source"] = "catalog"
            continue
        best, best_s = None, 0
        for g in named:
            if g.get("rarity_key") != rar or g.get("file") in used:
                continue
            s = score(row.get("name") or "", g.get("name_grid") or "")
            if s > best_s:
                best_s, best = s, g
        if best and best_s >= 70:
            row["avatar_file"] = best["file"]
            row["avatar_source"] = "catalog-name"
            used.add(best["file"])
        elif rar == "3":
            try:
                ix = int(row.get("idx"))
            except Exception:
                ix = -1
            p = os.path.join(USER, "icons", "R3_%03d.png" % ix)
            if os.path.isfile(p):
                row["avatar_file"] = "icons/R3_%03d.png" % ix
                row["avatar_source"] = "catalog"

    print("avatar hits", dict(hits))
    print("final avatars", sum(1 for r in table if r.get("avatar_file")))
    print("sources", defaultdict(int, {r.get("avatar_source") or "?": 0 for r in table}))
    src = defaultdict(int)
    for r in table:
        src[r.get("avatar_source") or ("empty" if not r.get("avatar_file") else "file")] += 1
    print("src", dict(src))

    # write csv then call merge html? write csv in place and invoke merge_561 is heavy.
    # Update 汇总表.csv avatars only then regenerate html via merge_561 which reloads jsonl...
    # Simpler: write csv and a small html rewriter, or import merge write_html.

    fields = list(table[0].keys()) if table else []
    out_csv = os.path.join(USER, "汇总表.csv")
    with open(out_csv, "w", encoding="utf-8-sig", newline="") as f:
        w = csv.DictWriter(f, fieldnames=fields, extrasaction="ignore")
        w.writeheader()
        w.writerows(table)
    print("updated", out_csv)
    from merge_561 import write_html
    html_path = os.path.join(USER, "汇总表.html")
    write_html(html_path, table)
    shutil.copy2(out_csv, os.path.join(TABLES, "汇总表.csv"))
    shutil.copy2(html_path, os.path.join(TABLES, "汇总表.html"))
    print("wrote", html_path)

    # cross-check report
    rep = os.path.join(USER, "_extract", "cross_check.md")
    lines = [
        "# 三站交叉核对",
        "",
        "| 来源 | 5★ | 4★ | 3★ | 说明 |",
        "|---|---:|---:|---:|---|",
        "| 纪念版图鉴 | 282 | 77 | 99 | 游戏内 561 |",
        "| GameKee 国服 dc | %d | %d | - | 词条全名+技能 |" % (sum(1 for r in cn if r.get("rarity")=="5星"), sum(1 for r in cn if r.get("rarity")=="4星")),
        "| GameKee 韩服 destinychild | %d | %d | %d | 四星齐全 77 |" % (len(kr5), len(kr4), len(kr3)),
        "| GameKee 日服 dcj | %d | - | - | 仅五星图鉴 |" % len(jp),
        "",
        "| 对齐 | 数量 |",
        "|---|---:|",
        "| 国服5★对上汇总 | %d |" % len(m_cn5),
        "| 国服4★对上汇总 | %d |" % len(m_cn4),
        "| 韩服4★对上汇总 | %d |" % len(m_kr4),
        "| 韩服5★对上汇总 | %d |" % len(m_kr5),
        "| 日服5★对上汇总 | %d |" % len(m_jp5),
        "",
    ]
    with open(rep, "w", encoding="utf-8") as f:
        f.write("\n".join(lines))
    print("report", rep)


if __name__ == "__main__":
    main()
