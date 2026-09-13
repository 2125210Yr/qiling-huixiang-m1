# -*- coding: utf-8 -*-
"""Build a combined inventory of dc + destinychild + dcj wiki dumps."""
from __future__ import annotations

import csv
import json
import os
import re
from collections import defaultdict

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
GK = os.path.join(ROOT, "docs", "reference", "gamekee")
USER = os.path.join(ROOT, "天命之子数据")

SITES = [
    ("dc", "国际服", os.path.join(GK, "catalog_index.csv"), os.path.join(GK, "pages")),
    ("destinychild", "韩服", os.path.join(GK, "destinychild", "catalog_index.csv"), os.path.join(GK, "destinychild", "pages")),
    ("dcj", "日服", os.path.join(GK, "dcj", "catalog_index.csv"), os.path.join(GK, "dcj", "pages")),
]


def load_index(path):
    if not os.path.isfile(path):
        return []
    with open(path, encoding="utf-8-sig") as f:
        return list(csv.DictReader(f))


def page_ok(pages, cid):
    if not cid:
        return False
    p = os.path.join(pages, "%s.json" % cid)
    if not os.path.isfile(p):
        return False
    try:
        with open(p, encoding="utf-8") as f:
            data = json.load(f)
        return data.get("code") == 0 and bool(data.get("data"))
    except Exception:
        return False


def page_title(pages, cid):
    p = os.path.join(pages, "%s.json" % cid)
    try:
        with open(p, encoding="utf-8") as f:
            data = json.load(f)
        return (data.get("data") or {}).get("title") or ""
    except Exception:
        return ""


def main():
    all_rows = []
    stats = []
    for alias, label, idx, pages in SITES:
        rows = load_index(idx)
        content = [r for r in rows if (r.get("content_id") or "").strip()]
        ok = sum(1 for r in content if page_ok(pages, r["content_id"]))
        sections = defaultdict(int)
        for r in content:
            sections[r.get("section") or "?"] += 1
        stats.append((alias, label, len(rows), len(content), ok, dict(sections), pages))
        for r in content:
            all_rows.append({
                "wiki": alias,
                "wiki_label": label,
                "path": r.get("path") or "",
                "section": r.get("section") or "",
                "name": r.get("name") or "",
                "content_id": r.get("content_id") or "",
                "ok": "1" if page_ok(pages, r["content_id"]) else "0",
            })

    os.makedirs(os.path.join(GK, "_combined"), exist_ok=True)
    comb = os.path.join(GK, "_combined", "all_pages.csv")
    with open(comb, "w", encoding="utf-8-sig", newline="") as f:
        w = csv.DictWriter(f, fieldnames=["wiki", "wiki_label", "section", "name", "content_id", "path", "ok"])
        w.writeheader()
        w.writerows(all_rows)

    # names unique to KR / JP
    def names(alias):
        return {r["name"].strip() for r in all_rows if r["wiki"] == alias and r["name"].strip()}

    dc, kr, jp = names("dc"), names("destinychild"), names("dcj")
    only_kr = sorted(kr - dc)
    only_jp = sorted(jp - dc)
    only_dc = sorted(dc - kr - jp)

    md = os.path.join(USER, "三站攻略汇总.md")
    html_path = os.path.join(USER, "三站攻略目录.html")
    lines = [
        "# 三个 GameKee wiki 收集结果",
        "",
        "| 站点 | 别名 | 词条（有正文） | 已下载 | 目录 |",
        "|---|---|---:|---:|---|",
    ]
    for alias, label, n_all, n_c, ok, sections, pages in stats:
        url = {
            "dc": "https://www.gamekee.com/dc/",
            "destinychild": "https://www.gamekee.com/destinychild/",
            "dcj": "https://www.gamekee.com/dcj/",
        }[alias]
        lines.append("| [%s](%s) | `%s` | %d | **%d** | `%s` |" % (label, url, alias, n_c, ok, pages.replace(ROOT + os.sep, "")))
    lines += [
        "",
        "## 各站栏目",
        "",
    ]
    for alias, label, n_all, n_c, ok, sections, pages in stats:
        lines.append("### %s" % label)
        lines.append("")
        lines.append("| 栏目 | 词条 |")
        lines.append("|---|---:|")
        for k, v in sections.items():
            lines.append("| %s | %d |" % (k, v))
        lines.append("")
    lines += [
        "## 名称差集（按词条标题，仅供查漏）",
        "",
        "- 韩服有、国际服标题对不上：**%d** 条" % len(only_kr),
        "- 日服有、国际服标题对不上：**%d** 条" % len(only_jp),
        "- 只在国际服：**%d** 条" % len(only_dc),
        "",
        "完整目录 CSV：`docs/reference/gamekee/_combined/all_pages.csv`",
        "",
        "打开 **`三站攻略目录.html`** 可按站筛选。",
        "",
    ]
    with open(md, "w", encoding="utf-8") as f:
        f.write("\n".join(lines))

    # HTML directory
    cards = []
    for r in all_rows:
        href = "https://www.gamekee.com/%s/%s.html" % (r["wiki"], r["content_id"])
        cards.append(
            '<tr data-w="%s" data-hay="%s"><td>%s</td><td>%s</td><td><a href="%s" target="_blank" rel="noopener">%s</a></td><td>%s</td></tr>'
            % (
                r["wiki"],
                html_esc((r["name"] + " " + r["path"]).lower()),
                r["wiki_label"],
                html_esc(r["section"]),
                href,
                html_esc(r["name"]),
                r["content_id"],
            )
        )
    html = """<!DOCTYPE html><html lang="zh-CN"><head><meta charset="utf-8">
<title>天命之子 · 三站攻略目录</title>
<style>
body{{margin:0;background:#10131a;color:#e8ecf4;font:14px/1.45 "Microsoft YaHei",sans-serif}}
header{{position:sticky;top:0;background:#1a2030;padding:12px 16px;border-bottom:1px solid #2a3144;z-index:5}}
input,select{{background:#0e121c;color:#e8ecf4;border:1px solid #2a3144;padding:6px 8px;border-radius:6px}}
table{{border-collapse:collapse;width:100%}}
th,td{{border-bottom:1px solid #2a3144;padding:6px 8px;text-align:left}}
th{{position:sticky;top:58px;background:#1e2433}}
a{{color:#f0c24b}}
.hidden{{display:none}}
</style></head><body>
<header>
<h1>三个 GameKee 词条目录</h1>
<div>
<input id="q" placeholder="搜标题/路径" oninput="filt()">
<select id="w" onchange="filt()">
<option value="">全部站点</option>
<option value="dc">国际服 dc</option>
<option value="destinychild">韩服 destinychild</option>
<option value="dcj">日服 dcj</option>
</select>
<span id="meta"></span>
</div></header>
<table><thead><tr><th>站点</th><th>栏目</th><th>词条</th><th>id</th></tr></thead>
<tbody>{body}</tbody></table>
<script>
function filt(){{
 const q=(document.getElementById('q').value||'').trim().toLowerCase();
 const w=document.getElementById('w').value; let n=0;
 document.querySelectorAll('tbody tr').forEach(tr=>{{
  const ok=(!w||tr.dataset.w===w)&&(!q||(tr.dataset.hay||'').includes(q));
  tr.classList.toggle('hidden',!ok); if(ok) n++;
 }});
 document.getElementById('meta').textContent='显示 '+n;
}}
filt();
</script></body></html>
""".format(body="\n".join(cards))
    with open(html_path, "w", encoding="utf-8") as f:
        f.write(html)
    print("wrote", comb, "rows", len(all_rows))
    print("wrote", md)
    print("wrote", html_path)
    print("only_kr", len(only_kr), "only_jp", len(only_jp), "only_dc", len(only_dc))
    for alias, label, n_all, n_c, ok, sections, pages in stats:
        print(alias, "content", n_c, "ok", ok)


def html_esc(s):
    return (
        str(s or "")
        .replace("&", "&amp;")
        .replace("<", "&lt;")
        .replace(">", "&gt;")
        .replace('"', "&quot;")
    )


if __name__ == "__main__":
    main()
