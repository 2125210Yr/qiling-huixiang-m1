# -*- coding: utf-8 -*-
"""Tidy 561 catalog HTML: rarity bands, equal square cards, gallery default."""
from __future__ import annotations

import csv
import html
import json
import os
import re
import shutil
from difflib import SequenceMatcher

ROOT = r"F:\天命之子"
USER = os.path.join(ROOT, "天命之子数据")
TABLES = os.path.join(ROOT, "docs", "reference", "gamekee", "tables")
GALLERY_CSS = os.path.join(USER, "_layout", "01_gallery_css.css")
TABLE_CSS = os.path.join(USER, "_layout", "02_table_css.css")
DETAIL_CSS = os.path.join(USER, "_layout", "12_detail.css")
DETAIL_JS = os.path.join(USER, "_layout", "12_detail.js")
NAME_FIX = os.path.join(USER, "_layout", "11_name_fix.json")
CJK_RUN = re.compile(r"[\u4e00-\u9fff]{2,}")

EL_CLASS = {"火": "el-fire", "水": "el-water", "木": "el-wood", "光": "el-light", "暗": "el-dark"}
BANDS = (("5星", "5★"), ("4星", "4★"), ("3星", "3★"), ("2星", "2★"), ("1星", "1★"))


def e(s):
    return html.escape("" if s is None else str(s), quote=True)


def rar_key(s: str) -> str:
    t = str(s or "")
    for ch in "54321":
        if t.startswith(ch):
            return ch
    return ""


def load_csv(path):
    with open(path, encoding="utf-8-sig") as f:
        return list(csv.DictReader(f))


HEART_FILES = {
    "紫": "icons/F4_slime_purple.png",
    "黄": "icons/F4_slime_yellow.png",
    "黃": "icons/F4_slime_yellow.png",
    "缘": "icons/F4_slime_green.png",
    "綠": "icons/F4_slime_green.png",
    "绿": "icons/F4_slime_green.png",
    "红": "icons/F4_slime_red.png",
    "紅": "icons/F4_slime_red.png",
    "蓝": "icons/F4_slime_blue.png",
    "藍": "icons/F4_slime_blue.png",
}


def _load_fill_json(name):
    p = os.path.join(USER, "_layout", name)
    if not os.path.isfile(p):
        return []
    with open(p, encoding="utf-8") as f:
        return json.loads(f.read())


def _exists(rel):
    return bool(rel) and os.path.isfile(os.path.join(USER, rel.replace("\\", "/")))


def _compact(s: str) -> str:
    return re.sub(r"[·・\s♥♡★☆]", "", s or "")


def names_related(ocr: str, wiki: str) -> bool:
    a, b = _compact(ocr), _compact(wiki)
    if not a or not b:
        return False
    if a == b:
        return True
    if "图鉴" in b or len(b) < 2:
        return False
    # 索尔 ⊂ 哈索尔
    shorter, longer = (a, b) if len(a) <= len(b) else (b, a)
    if longer.endswith(shorter) and len(longer) - len(shorter) <= 1:
        return False
    if SequenceMatcher(None, a, b).ratio() >= 0.72:
        return True
    if shorter in longer and len(shorter) >= 3 and len(shorter) / len(longer) >= 0.4:
        return True
    return False


# (rarity, idx) → official name from wiki/catalog (auditor easy wins + avatar-id hits)
HARD_NAMES = {
    ("5星", 15): "严谨的拉尼",
    ("5星", 24): "反击者格琳戴儿",

    ("5星", 51): "急驰的波利亚斯",
    ("5星", 67): "榴弹怀特",
    ("5星", 78): "仪式之剑罗宾",
    ("5星", 112): "暗罗汉",
    ("5星", 141): "孤剑提亚玛特",
    ("5星", 143): "驱魔师该隐",
    ("5星", 154): "爱恨交织妲比",
    ("5星", 171): "蜕变妲比",
    ("5星", 220): "学校泳装妲比",
    ("5星", 221): "学校泳装妲比",
    ("5星", 249): "魔王妲比",
    ("4星", 2): "赛车手妲比",
    ("4星", 5): "暗永恒之枪",
    ("4星", 8): "被遗弃的生物",
    ("4星", 12): "守护安卡",
    ("4星", 23): "芬芳的欧罗巴",
    ("4星", 26): "懒散的玛雅乌尔",
    ("4星", 62): "束缚的蕾蒂",
    ("4星", 65): "古老的骷髅",
    ("4星", 67): "草原的芙萝拉",
    ("4星", 74): "小绿爱心",
    ("3星", 6): "炮击机甲指挥官",
    ("3星", 9): "妲比",
    ("3星", 26): "牺牲的贞德",
    ("3星", 29): "诅咒的斯库尔德",
    ("3星", 31): "冰王苍龙",
    ("3星", 38): "破坏的塞赫麦特",
    ("3星", 42): "守护的传奇",
    ("3星", 51): "泛用型锁链杀手",
    ("3星", 65): "处容",
    ("3星", 73): "绿羽的草隆",
    ("3星", 90): "孤独的花束",
    ("3星", 91): "声音传讯者",
    ("3星", 96): "小绿噗比",
    ("2星", 2): "绿色的啪布",
    ("2星", 31): "绿球",
    ("2星", 39): "束缚的暗黑伽锁",
    ("2星", 40): "束缚的光伽锁",
    ("2星", 41): "束缚的伽锁",
    ("2星", 42): "束缚的红伽锁",
    ("2星", 46): "正义的根",
    ("2星", 50): "小绿噗噗",
    ("1星", 27): "绿色的福多",
    ("1星", 33): "怠惰小鸡",
    ("1星", 46): "小绿水滴",
    ("1星", 22): "绿宝石色的蛹",
}


def _set_name(r, new):
    ocr = (r.get("name") or "").strip()
    if not new or new == ocr:
        return False
    if not r.get("name_ocr"):
        r["name_ocr"] = ocr
    r["name"] = new
    return True


def apply_wiki_names(rows):
    """Use wiki_name only when it is the same child, not an off-by-one column."""
    fixes = {}
    for rec in _load_fill_json("11_name_fix.json"):
        key = rec.get("id") or "%s-%s" % (rec.get("rarity"), rec.get("idx"))
        if rec.get("name_new"):
            fixes[key] = rec
    wiki_by_cid = {}
    wpath = os.path.join(ROOT, "docs", "reference", "gamekee", "tables", "characters.csv")
    if os.path.isfile(wpath):
        with open(wpath, encoding="utf-8-sig") as f:
            for rec in csv.DictReader(f):
                cid = (rec.get("content_id") or "").strip()
                nm = (rec.get("name") or "").strip()
                if cid and nm:
                    wiki_by_cid[cid] = nm
    prev_name = ""
    prev_rar = ""
    nfix = 0
    for r in rows:
        ocr = (r.get("name") or "").strip()
        wiki = (r.get("wiki_name") or "").strip()
        rar = r.get("rarity") or ""
        try:
            ix = int(r.get("idx"))
        except (TypeError, ValueError):
            ix = None
        key = r.get("id") or "%s-%s" % (rar, r.get("idx"))
        hard = HARD_NAMES.get((rar, ix)) if ix is not None else None
        if hard and _set_name(r, hard):
            nfix += 1
        elif key in fixes:
            if not r.get("name_ocr"):
                r["name_ocr"] = ocr
            r["name"] = fixes[key]["name_new"]
            nfix += 1
        elif wiki and wiki != ocr and names_related(ocr, wiki):
            if prev_rar == rar and wiki == prev_name:
                pass  # wiki_name column shifted onto previous row
            else:
                if not r.get("name_ocr"):
                    r["name_ocr"] = ocr
                r["name"] = wiki
                nfix += 1
        else:
            av = (r.get("avatar_file") or "").replace("\\", "/")
            cid = os.path.splitext(os.path.basename(av))[0] if av.startswith("avatars/") else ""
            w2 = wiki_by_cid.get(cid) or ""
            if w2 and w2 != ocr and (("姐比" in ocr and "妲比" in w2) or names_related(ocr, w2)):
                if _set_name(r, w2):
                    nfix += 1
            elif "姐比" in ocr:
                if _set_name(r, ocr.replace("姐比", "妲比")):
                    nfix += 1
            elif "缘色" in ocr or ocr.startswith("缘羽") or ocr.startswith("小缘") or "缘宝石" in ocr or ocr == "缘球":
                neu = (
                    ocr.replace("缘色", "绿色")
                    .replace("缘羽", "绿羽")
                    .replace("小缘", "小绿")
                    .replace("缘宝石", "绿宝石")
                    .replace("缘球", "绿球")
                )
                if _set_name(r, neu):
                    nfix += 1
        prev_name = ocr
        prev_rar = rar
        # Drop GameKee title-padding / 韩文直译前缀，保留错字修正后的短名
        shown = (r.get("name") or "").strip()
        raw = (r.get("name_ocr") or ocr or "").strip()
        core = re.sub(r"[·・][光暗水火木0-9]+$", "", _compact(raw))
        neu = _compact(shown)
        if shown in ("白炎的赫斯提亚",) and "黑炎" in raw:
            if _set_name(r, "黑炎的赫斯提亚"):
                nfix += 1
        elif "、" in shown:
            alt = (r.get("wiki_name") or "").replace("、", "")
            if "提亚玛特" in shown or "提亚玛特" in alt:
                if _set_name(r, "孤剑提亚玛特"):
                    nfix += 1
        elif core and neu.endswith(core) and len(neu) - len(core) >= 2:
            # 诺特斯 → 独自跑的诺特斯；泽卡提 → 抵抗的泽卡提
            cleaned = re.sub(r"[·・][光暗水火木0-9]+$", "", raw)
            cleaned = cleaned.replace("姐比", "妲比").replace("缘色", "绿色").replace("缘羽", "绿羽")
            cleaned = cleaned.replace("小缘", "小绿").replace("反擎", "反击").replace(" ", "")
            if cleaned and cleaned != shown:
                if _set_name(r, cleaned):
                    nfix += 1
                    shown = r.get("name") or shown
        if "无法无天" in raw and "哈比" in shown and "无法无天" not in shown:
            if _set_name(r, "无法无天哈比"):
                nfix += 1
                shown = r.get("name") or shown
        for prefix in ("独自跑的", "浪费家", "小不点", "水中的", "通灵师"):
            if shown.startswith(prefix) and len(shown) > len(prefix) + 1:
                if _set_name(r, shown[len(prefix):]):
                    nfix += 1
                break
    print("display names updated", nfix)
    return rows


def display_name(r):
    return (r.get("name") or r.get("wiki_name") or "（无名）").strip() or "（无名）"


def apply_face_icons(rows):
    """Wiki heads first; keep C3 aligned crops; F* only for 2/1. Never V* stats panels."""
    by4 = {int(rec["idx"]): rec for rec in _load_fill_json("08_4star_fill.json")}
    by5 = {int(rec["idx"]): rec for rec in _load_fill_json("09_5star_fill.json")}
    for r in rows:
        av = (r.get("avatar_file") or "").replace("\\", "/")
        if av.startswith("avatars_561/V") or os.path.basename(av).startswith("V") or (
            rar_key(r.get("rarity")) == "5" and "/R5_" in av
        ):
            r["avatar_file"] = ""
            av = ""
        rar = rar_key(r.get("rarity"))
        try:
            ix = int(r.get("idx"))
        except (TypeError, ValueError):
            ix = None
        name = r.get("name") or ""

        if rar == "5" and ix in by5 and (not av or av.startswith("icons/R5")):
            f = by5[ix].get("avatar_file") or ""
            if _exists(f):
                r["avatar_file"] = f
                av = f
            wn = by5[ix].get("wiki_name") or ""
            if wn:
                r["wiki_name"] = wn

        if rar == "4" and ix in by4 and (not av or av.startswith("icons/R4")):
            rec = by4[ix]
            f = rec.get("avatar_file") or ""
            if not _exists(f):
                blob = (rec.get("name") or "") + name
                for k, hf in HEART_FILES.items():
                    if k in blob and _exists(hf):
                        f = hf
                        break
            if _exists(f):
                r["avatar_file"] = f
                av = f

        if rar == "4" and "爱心" in name:
            for k, f in HEART_FILES.items():
                if k in name and _exists(f):
                    r["avatar_file"] = f
                    av = f
                    break

        if av.startswith("avatars/") or av.startswith("avatars_wiki/") or "/C3" in av or "/C5" in av:
            continue
        if rar == "3":
            continue
        if rar not in ("2", "1") or ix is None:
            continue
        face_ix = 48 if (rar == "1" and ix >= 49) else ix
        face = "icons/F%s_%03d.png" % (rar, face_ix)
        if _exists(face):
            r["avatar_file"] = face
        else:
            fallback = "icons/R%s_%03d.png" % (rar, ix)
            if _exists(fallback):
                r["avatar_file"] = fallback
    # Second memorial screen of the same 5★ child: reuse the first card face.
    dup5 = {61: 60, 71: 70, 111: 110, 131: 130, 171: 170, 181: 180, 221: 220, 251: 250, 275: 161}
    by5idx = {}
    for r in rows:
        if rar_key(r.get("rarity")) == "5":
            try:
                by5idx[int(r.get("idx"))] = r
            except (TypeError, ValueError):
                pass
    for dst, src_i in dup5.items():
        a, b = by5idx.get(dst), by5idx.get(src_i)
        if not a or not b:
            continue
        if (a.get("avatar_file") or "").strip():
            continue
        src_av = (b.get("avatar_file") or "").strip()
        if src_av and _exists(src_av):
            a["avatar_file"] = src_av
            if b.get("wiki_name") and not a.get("wiki_name"):
                a["wiki_name"] = b.get("wiki_name")
    return rows


def _face(r):
    av = r.get("avatar_file") or ""
    name = r.get("name") or ""
    if av:
        return '<div class="face"><img src="%s" alt="%s" width="152" height="152" loading="lazy"></div>' % (
            e(av), e(name)
        )
    return '<div class="face"><div class="ph">无</div></div>'


def write_html(path, rows):
    n_by = {k: 0 for k, _ in BANDS}
    for r in rows:
        k = str(r.get("rarity") or "")
        if k in n_by:
            n_by[k] += 1

    trs = []
    bands = {lab: [] for lab, _ in BANDS}
    payload = []
    for r in rows:
        el = r.get("element") or ""
        rar = r.get("rarity") or ""
        role = r.get("role") or ""
        name = display_name(r)
        cid = r.get("id") or "M%s-%s" % (rar_key(rar) or "x", r.get("idx") or "")
        hay = " ".join(
            [
                name,
                r.get("name") or "",
                r.get("name_ocr") or "",
                el,
                role,
                rar,
                r.get("wiki_name") or "",
                r.get("auto_text") or "",
                r.get("tap_text") or "",
            ]
        ).lower()
        ds = 'data-element="%s" data-rarity="%s" data-role="%s" data-hay="%s" data-id="%s"' % (
            e(el), e(rar), e(role), e(hay), e(cid)
        )
        face = _face(r)
        el_cls = EL_CLASS.get(el, "")
        card = (
            '<article class="card" tabindex="0" role="button" %s>%s'
            '<div class="nm" title="%s">%s</div>'
            '<div class="meta"><span class="badge %s">%s</span><span class="badge">%s</span></div>'
            "</article>"
            % (ds, face, e(name), e(name or "（无名）"), el_cls, e(el or "—"), e(role or "—"))
        )
        payload.append({
            "id": cid,
            "name": name,
            "el": el,
            "role": role,
            "rar": rar,
            "av": r.get("avatar_file") or "",
            "cp": r.get("cp") or r.get("hp_init") or "",
            "hp": r.get("hp") or r.get("hp_init") or "",
            "atk": r.get("atk") or "",
            "def": r.get("def_") or "",
            "agl": r.get("agl") or "",
            "crt": r.get("crt") or "",
            "auto": r.get("auto_text") or "",
            "tap": r.get("tap_text") or "",
            "slide": r.get("slide_text") or "",
            "drive": r.get("drive_text") or "",
            "leader": r.get("leader_text") or "",
            "cv": r.get("cv") or "",
        })
        if rar in bands:
            bands[rar].append(card)
        else:
            bands["5星"].append(card)

        def sk(text):
            t = text or "—"
            return "<td class='col-sk' title='%s'><div class='sk'>%s</div></td>" % (e(t), e(t))

        trs.append(
            "<tr %s><td class='col-av'>%s</td>"
            "<td class='col-name'><b class='nm'>%s</b><div class='meta'>"
            "<span class='badge %s'>%s</span><span class='badge'>%s</span>"
            "<span class='badge'>%s</span></div></td>"
            "<td class='col-stats nums'>CP %s<br>HP %s　攻%s　防%s　敏%s　暴%s</td>"
            "%s%s%s%s%s</tr>"
            % (
                ds,
                face.replace('class="face"', 'class="face sm av"'),
                e(name),
                el_cls,
                e(el or "?"),
                e(role or "?"),
                e(rar),
                e(r.get("cp") or r.get("hp_init") or "—"),
                e(r.get("hp") or r.get("hp_init") or "—"),
                e(r.get("atk") or "—"),
                e(r.get("def_") or "—"),
                e(r.get("agl") or "—"),
                e(r.get("crt") or "—"),
                sk(r.get("auto_text")),
                sk(r.get("tap_text")),
                sk(r.get("slide_text")),
                sk(r.get("drive_text")),
                sk(r.get("leader_text")),
            )
        )

    gal_parts = []
    for lab, star in BANDS:
        items = bands.get(lab) or []
        gal_parts.append(
            '<section class="band" id="r%s" data-rarity="%s">'
            '<h2>%s <span class="cnt n" data-total="%d">%d</span></h2>'
            '<div class="grid gcat">%s</div></section>'
            % (lab[0], e(lab), star, n_by.get(lab, 0), len(items), "".join(items))
        )

    css = ""
    for css_path in (GALLERY_CSS, TABLE_CSS, DETAIL_CSS):
        if os.path.isfile(css_path):
            with open(css_path, encoding="utf-8") as f:
                css += f.read() + "\n"
    css += """
main{max-width:1280px;margin:0 auto;padding:8px 16px 80px}
.top-inner{max-width:1280px;margin:0 auto}
.stats{color:var(--gold);font-size:13px}
.bar{display:flex;flex-wrap:wrap;gap:8px;align-items:center}
button[aria-pressed="true"]{background:var(--gold);color:#1a1200;border-color:var(--gold)}
.gcat{display:grid;gap:8px;grid-template-columns:repeat(auto-fill,minmax(max(112px,calc((100% - 56px)/8)),1fr))}
.face.sm{width:56px;height:56px;aspect-ratio:auto;flex:none;border-width:1px;position:relative}
.wrap{overflow:auto;max-height:calc(100dvh - 128px);border:1px solid var(--line);border-radius:12px}
table{min-width:0!important}
.nums{font-variant-numeric:tabular-nums;white-space:nowrap;color:#ddd}
.sk{max-width:220px;display:-webkit-box;-webkit-line-clamp:3;-webkit-box-orient:vertical;overflow:hidden;color:#c8c4b8}
.rnav{display:flex;flex-wrap:wrap;gap:8px 12px;font-size:13px;margin-top:6px}
.rnav a{color:var(--gold);text-decoration:none}
.rnav a:hover,.rnav a:focus-visible{text-decoration:underline}
.band{scroll-margin-top:96px}
.views{display:inline-flex;gap:8px}
:focus-visible{outline:2px solid var(--gold);outline-offset:2px}
.card{cursor:pointer}
.card:hover{border-color:var(--gold,#cf9403)}
.card.on{border-color:#ffc400}
#cdet{
  position:sticky;bottom:10px;z-index:25;display:none;
  background:#121018f2;border:1px solid #5a4714;border-radius:14px;
  padding:14px 16px;margin:12px 16px 16px;backdrop-filter:blur(12px);
  max-width:1280px
}
#cdet.on{display:grid;grid-template-columns:88px 1fr auto;gap:12px;align-items:start}
#cdet img,#cdet .ph{width:88px;height:88px;object-fit:contain;background:#000;border-radius:10px}
#cdet .sk{font-size:13px;color:#c8c4b8;max-height:220px;overflow:auto}
#cdet .sk div{margin:4px 0;padding:6px 8px;background:#0a0a0a;border-radius:8px}
#cdet .x{
  min-width:40px;min-height:40px;border:1px solid #3a3018;border-radius:8px;
  background:#0e0e12;color:var(--gold,#cf9403);cursor:pointer
}
@media (max-width:640px){
  #cdet.on{grid-template-columns:64px 1fr;bottom:0;margin:0;border-radius:14px 14px 0 0}
  #cdet .x{grid-column:1/-1}
}

.mods{display:flex;gap:6px;flex-wrap:wrap;align-items:center}
.mods a{color:#CF9403;text-decoration:none;font-size:13px;line-height:1;padding:7px 14px;border-radius:999px;border:1px solid #3a3018;background:transparent;white-space:nowrap}
.mods a:hover{color:#fff;border-color:#5c4a10;background:#1a1408}
.mods a.on,.mods a[aria-current="page"]{color:#111;background:#FFC400;border-color:#FFC400;font-weight:700}
header .mods{flex:1}

"""
    html_doc = """<!DOCTYPE html>
<html lang="zh-CN">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>天命之子 · 图鉴设计 · 561</title>
<style>
__CSS__
</style>
</head>
<body>
<header class="top">
<div class="top-inner">
<nav class="mods" aria-label="设计模块"><a href="设计手册.html">手册</a><a href="视觉图鉴.html">UI</a><a href="战斗与数值.html">数值</a><a href="战斗动效.html">动效</a><a href="汇总表.html" class="on" aria-current="page">图鉴</a></nav>
<h1>图鉴设计 <small>纪念版 561 = 282+77+99+53+50</small></h1>
<div class="stats">5★ __N5__ · 4★ __N4__ · 3★ __N3__ · 2★ __N2__ · 1★ __N1__</div>
<div class="bar filters">
<input id="q" type="search" placeholder="搜索名字 / 技能" autocomplete="off" aria-label="搜索名字或技能" oninput="filt()">
<select id="fel" onchange="filt()" aria-label="属性">
  <option value="">属性</option><option>火</option><option>水</option><option>木</option><option>光</option><option>暗</option>
</select>
<select id="fr" onchange="filt()" aria-label="稀有度">
  <option value="">稀有度</option><option>5星</option><option>4星</option><option>3星</option><option>2星</option><option>1星</option>
</select>
<span class="views" role="group" aria-label="视图">
<button type="button" id="btn-gal" class="on" data-view="gal" aria-pressed="true" onclick="setView('gal')">图鉴</button>
<button type="button" id="btn-tbl" data-view="tbl" aria-pressed="false" onclick="setView('tbl')">表格</button>
</span>
<span id="meta"></span>
</div>
<nav class="rnav" aria-label="跳到稀有度">
<a href="#r5">5★</a><a href="#r4">4★</a><a href="#r3">3★</a><a href="#r2">2★</a><a href="#r1">1★</a>
</nav>
</div>
</header>
<main>
<div id="gal">__GAL__</div>
<div id="tbl" class="tbl-wrap hidden">
<table class="roster">
<thead><tr>
<th class="col-av">头像</th><th class="col-name">天子</th><th class="col-stats">面板</th>
<th class="col-sk">普攻</th><th class="col-sk">TS</th><th class="col-sk">SS</th>
<th class="col-sk">DS</th><th class="col-sk">队长</th>
</tr></thead>
<tbody>__TRS__</tbody>
</table>
</div>
<aside id="cdet" class="child-detail" role="dialog" aria-modal="true" aria-labelledby="cdet-name" hidden>
  <button type="button" class="cdet-close" id="cdet-close" aria-label="关闭详情">×</button>
  <div class="cdet-inner" id="cdet-inner"></div>
</aside>
<script type="application/json" id="child-json">__JSON__</script>
</main>
<script>
function setView(v, fromHash){
  const gal=document.getElementById('gal'), tbl=document.getElementById('tbl');
  const isGal=v!=='tbl';
  gal.classList.toggle('hidden', !isGal);
  tbl.classList.toggle('hidden', isGal);
  document.querySelectorAll('.views button').forEach(b=>{
    const on=(b.getAttribute('data-view')||'')===(isGal?'gal':'tbl');
    b.classList.toggle('on', on);
    b.setAttribute('aria-pressed', on?'true':'false');
  });
  if(!fromHash && history.replaceState) history.replaceState(null,'', isGal?'#gal':'#tbl');
  filt();
}
function filt(){
  const q=(document.getElementById('q').value||'').trim().toLowerCase();
  const el=document.getElementById('fel').value, r=document.getElementById('fr').value;
  const galOn=!document.getElementById('gal').classList.contains('hidden');
  let n=0;
  document.querySelectorAll('#tbl tbody tr, #gal .card').forEach(node=>{
    const ok=(!q|| (node.dataset.hay||'').includes(q))
      && (!el||node.dataset.element===el)
      && (!r||node.dataset.rarity===r);
    node.classList.toggle('hidden', !ok);
    const inGal=node.classList.contains('card');
    if(ok && (galOn?inGal:!inGal)) n++;
  });
  document.querySelectorAll('.band').forEach(band=>{
    const vis=[...band.querySelectorAll('.card')].filter(c=>!c.classList.contains('hidden')).length;
    const span=band.querySelector('.cnt, .n');
    if(span) span.textContent=vis+' / '+(span.dataset.total||vis);
    band.classList.toggle('hidden', vis===0);
  });
  document.getElementById('meta').textContent='显示 '+n+' / __TOTAL__';
}
function applyHash(){
  const h=(location.hash||'#gal').replace('#','');
  const isGal=h!=='tbl';
  setView(isGal?'gal':'tbl', true);
  if(isGal && /^r[1-5]$/.test(h)){
    const el=document.getElementById(h);
    if(el && !el.classList.contains('hidden')) el.scrollIntoView({block:'start'});
  }
}
window.addEventListener('hashchange', applyHash);
applyHash();
</script>
<script>
__DETAIL_JS__
</script>
</body></html>
"""
    html_doc = (
        html_doc.replace("__CSS__", css)
        .replace("__N5__", str(n_by.get("5星", 0)))
        .replace("__N4__", str(n_by.get("4星", 0)))
        .replace("__N3__", str(n_by.get("3星", 0)))
        .replace("__N2__", str(n_by.get("2星", 0)))
        .replace("__N1__", str(n_by.get("1星", 0)))
        .replace("__GAL__", "".join(gal_parts))
        .replace("__TRS__", "".join(trs))
        .replace("__TOTAL__", str(len(rows)))
        .replace("__JSON__", json.dumps(payload, ensure_ascii=False).replace("<", "\\u003c"))
        .replace("__DETAIL_JS__", open(DETAIL_JS, encoding="utf-8").read() if os.path.isfile(DETAIL_JS) else "")
    )
    os.makedirs(os.path.dirname(path) or ".", exist_ok=True)
    with open(path, "w", encoding="utf-8") as f:
        f.write(html_doc)
    print("wrote", path, "rows", len(rows))


def main():
    csv_path = os.path.join(USER, "汇总表.csv")
    rows = load_csv(csv_path)
    apply_face_icons(rows)
    apply_wiki_names(rows)
    if rows:
        fields = list(rows[0].keys())
        if any((r.get("name_ocr") or "").strip() for r in rows) and "name_ocr" not in fields:
            fields.insert(fields.index("name") + 1 if "name" in fields else 0, "name_ocr")
        with open(csv_path, "w", encoding="utf-8-sig", newline="") as f:
            w = csv.DictWriter(f, fieldnames=fields, extrasaction="ignore")
            w.writeheader()
            w.writerows(rows)
    html_path = os.path.join(USER, "汇总表.html")
    write_html(html_path, rows)
    dest = os.path.join(TABLES, "汇总表.html")
    try:
        shutil.copy2(html_path, dest)
        shutil.copy2(csv_path, os.path.join(TABLES, "汇总表.csv"))
    except OSError:
        pass


if __name__ == "__main__":
    main()
