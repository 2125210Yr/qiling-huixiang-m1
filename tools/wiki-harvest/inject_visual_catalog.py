# -*- coding: utf-8 -*-
"""Inject 561 / soul-carta / puppet / UI-film data into 视觉图鉴.html."""
from __future__ import annotations

import csv
import html
import json
import os
import re

ROOT = r"F:\天命之子"
USER = os.path.join(ROOT, "天命之子数据")
SHOT = "../docs/reference/mobile-archive/screenshots"
BIBLE = os.path.join(USER, "视觉图鉴.html")
CSV_CHILD = os.path.join(USER, "汇总表.csv")
CSV_CARTA = os.path.join(USER, "魂之歌牌.csv")
CSV_PUPPET = os.path.join(USER, "人偶.csv")
CSV_BUFF = os.path.join(USER, "buffs.csv")

UI_SHOTS = [
    ("001_home.png", "首页 Presenter"),
    ("010_character_team.png", "编队马赛克"),
    ("020_character_detail_overview.png", "详情全身"),
    ("021_character_detailed_stats.png", "详细能力叠字"),
    ("024_character_costume.png", "衣柜"),
    ("027_character_gallery_mode.png", "图库（无铬）"),
    ("030_character_skills.png", "技能清单"),
    ("036_skill_order_settings.png", "技能预约"),
    ("042_equipment_weapon.png", "空槽 NOTICE"),
    ("050_settings.png", "设置"),
    ("054_home_third_icon.png", "Enjoy Home + Deco"),
    ("070_hotspring.png", "魔界温泉"),
    ("073_hotspring_encyclopedia.png", "温泉图鉴"),
    ("076_hotspring_lib_01.png", "温泉 LIBRARY"),
    ("080_archive.png", "天子图鉴 561"),
    ("084_archive_soulcarta.png", "魂之歌牌 156"),
    ("085_archive_puppets.png", "人偶 232"),
    ("090_library.png", "黑卡蒂图书馆"),
    ("095_eve_adventure.png", "夏娃的冒险"),
    ("112_saaya_weapon.png", "装备卡 斧枪"),
    ("170_libview_base_stats.png", "图鉴查看 · 非人"),
    ("365_costumes_tab.png", "造型 315"),
    ("400_sc5_000.png", "魂卡详情"),
    ("432_L_000.png", "人偶详情"),
    ("500_teddy_ov.png", "泰迪总览"),
    ("502_teddy_weapon.png", "粉碎重锤"),
    ("510_saaya_ov.png", "纱彩总览"),
    ("511_saaya_armor.png", "帕拉斯的披风"),
    ("520_fuka_ov.png", "风花四槽"),
    ("530_lassie_ov.png", "拉西四槽"),
]


def e(s):
    return html.escape("" if s is None else str(s), quote=True)


def filled(rows, *keys):
    n = 0
    for r in rows:
        if any((r.get(k) or "").strip() for k in keys):
            n += 1
    return n


def load_csv(path):
    if not os.path.isfile(path):
        return []
    with open(path, encoding="utf-8-sig") as f:
        return list(csv.DictReader(f))


def existing_shot(name):
    p = os.path.join(ROOT, "docs", "reference", "mobile-archive", "screenshots", name)
    return os.path.isfile(p) and os.path.getsize(p) > 1000


_OCR_BAD = re.compile(
    r"SKILLINFORMATION|TIER\s*1|基碰能力|基碴能力|之一擎|INFORMATION|IOMAL",
    re.I,
)


def skill_clean(t: str) -> str:
    s = (t or "").strip()
    if not s or s.count("|") >= 3 or _OCR_BAD.search(s):
        return ""
    return s


def local_ok(rel: str) -> bool:
    if not rel:
        return False
    p = os.path.join(USER, rel.replace("\\", "/"))
    return os.path.isfile(p) and os.path.getsize(p) > 1500


def main():
    children = load_csv(CSV_CHILD)
    cartas = load_csv(CSV_CARTA)
    puppets = load_csv(CSV_PUPPET)
    buffs = load_csv(CSV_BUFF)

    n = len(children)
    by = {}
    for star in ("5星", "4星", "3星", "2星", "1星"):
        sub = [r for r in children if r.get("rarity") == star]
        by[star] = {
            "n": len(sub),
            "av": filled(sub, "avatar_file"),
            "el": filled(sub, "element"),
            "role": filled(sub, "role"),
            "hp": filled(sub, "hp", "hp_init"),
            "tap": filled(sub, "tap_text"),
            "auto": filled(sub, "auto_text"),
        }

    cov = [
        ("天子", n, 561),
        ("头像", filled(children, "avatar_file"), 561),
        ("属性", filled(children, "element"), 561),
        ("职业", filled(children, "role"), 561),
        ("面板 HP", filled(children, "hp", "hp_init"), 561),
        ("普攻/TS", filled(children, "auto_text", "tap_text"), 561),
        ("魂之歌牌", len(cartas), 156),
        ("人偶（wiki）", len(puppets), 232),
        ("Buff", len(buffs), 93),
    ]

    bars = []
    for label, have, total in cov:
        pct = 0 if not total else round(100 * have / total)
        bars.append(
            '<div class="cov"><div class="cov-h"><b>%s</b><span>%s / %s</span></div>'
            '<div class="cov-bar"><i style="width:%s%%"></i></div></div>'
            % (e(label), have, total, pct)
        )

    rarity_pills = []
    for star, d in by.items():
        rarity_pills.append(
            '<div class="card"><h3>%s %s</h3><p>头像 %s · 属性 %s · 职业 %s · HP %s · TS %s</p></div>'
            % (e(star), d["n"], d["av"], d["el"], d["role"], d["hp"], d["tap"])
        )

    # child gallery — never use memorial V* stats-panel crops
    child_cards = []
    payload = []
    for r in children:
        av = (r.get("avatar_file") or "").replace("\\", "/")
        if os.path.basename(av).startswith("V") or not local_ok(av):
            av = ""
        el = r.get("element") or ""
        rar = r.get("rarity") or ""
        name = r.get("name") or ""
        hay = " ".join([name, el, r.get("role") or "", rar]).lower()
        img = ('<img src="%s" alt="%s" loading="lazy">' % (e(av), e(name))) if av else '<div class="ph">无头像</div>'
        child_cards.append(
            '<button type="button" class="unit" data-el="%s" data-rar="%s" data-hay="%s" data-id="%s">'
            '%s<span>%s</span><small>%s %s</small></button>'
            % (e(el), e(rar), e(hay), e(r.get("id") or ""), img, e(name), e(el or "?"), e(rar))
        )
        payload.append({
            "id": r.get("id") or "",
            "name": name,
            "el": el,
            "role": r.get("role") or "",
            "rar": rar,
            "av": av,
            "cp": r.get("cp") or "",
            "hp": r.get("hp") or r.get("hp_init") or "",
            "atk": r.get("atk") or "",
            "def": r.get("def_") or "",
            "agl": r.get("agl") or "",
            "crt": r.get("crt") or "",
            "auto": skill_clean(r.get("auto_text") or ""),
            "tap": skill_clean(r.get("tap_text") or ""),
            "slide": skill_clean(r.get("slide_text") or ""),
            "drive": skill_clean(r.get("drive_text") or ""),
            "leader": skill_clean(r.get("leader_text") or ""),
            "cv": r.get("cv") or "",
        })

    carta_cards = []
    for r in cartas:
        av = r.get("avatar_file") or ""
        name = r.get("name") or ""
        rar = r.get("rarity") or ""
        el = r.get("element_gate") or r.get("stat_pair") or ""
        if av and not local_ok(av):
            av = ""
        img = ('<img src="%s" alt="%s" loading="lazy">' % (e(av), e(name))) if av else '<div class="ph">无卡面</div>'
        spec = (r.get("special") or "")[:80]
        carta_cards.append(
            '<div class="unit static" data-rar="%s" data-hay="%s"><div class="shot sq">%s</div>'
            "<span>%s</span><small>%s %s</small><p class='caption'>%s</p></div>"
            % (e(rar), e((name + " " + spec).lower()), img, e(name), e(rar), e(el), e(spec))
        )

    pup_cards = []
    for r in puppets:
        av = r.get("avatar_file") or ""
        name = r.get("name") or ""
        rar = r.get("rarity") or ""
        el = r.get("element") or ""
        img = ('<img src="%s" alt="%s" loading="lazy">' % (e(av), e(name))) if av else '<div class="ph">无</div>'
        pup_cards.append(
            '<div class="unit static" data-rar="%s" data-hay="%s">%s<span>%s</span><small>%s %s</small></div>'
            % (e(rar), e(name.lower()), img, e(name), e(el), e(rar))
        )

    film = []
    for fn, cap in UI_SHOTS:
        if not existing_shot(fn):
            continue
        film.append(
            '<figure class="shot"><img src="%s/%s" alt="%s" loading="lazy">'
            "<figcaption>%s</figcaption></figure>" % (SHOT, e(fn), e(cap), e(cap))
        )

    extra_css = """
.cov{margin:8px 0 14px}
.cov-h{display:flex;justify-content:space-between;font-size:13px;color:var(--sec)}
.cov-bar{height:8px;background:#1a1a1a;border-radius:99px;overflow:hidden;border:1px solid #333}
.cov-bar i{display:block;height:100%;background:linear-gradient(90deg,#cf9403,#ffc400);border-radius:99px}
.filters{display:flex;gap:8px;flex-wrap:wrap;margin:12px 0 16px}
.filters input,.filters select{background:#0e0e0e;color:#eee;border:1px solid #3a3018;border-radius:8px;padding:8px 10px}
.gcat{display:grid;grid-template-columns:repeat(auto-fill,minmax(118px,1fr));gap:10px}
@media (min-width:1100px){.gcat{grid-template-columns:repeat(8,minmax(0,1fr))}}
.gcat .unit{
  width:100%;height:auto;min-width:0;
  background:#121010;border:1px solid #2a2418;border-radius:12px;padding:8px;
  text-align:center;color:inherit;display:flex;flex-direction:column;gap:4px;
  writing-mode:horizontal-tb;white-space:normal;appearance:none;-webkit-appearance:none
}
.gcat .unit img,.gcat .unit .ph{
  width:100%;aspect-ratio:1;object-fit:cover;object-position:top center;
  border-radius:8px;background:#000;display:block
}
.gcat .unit .shot{aspect-ratio:1;cursor:zoom-in}
.gcat .unit .shot img{height:100%;object-position:center}
.gcat .unit span{
  font-size:12px;line-height:1.35;writing-mode:horizontal-tb;
  overflow:hidden;display:-webkit-box;-webkit-line-clamp:2;-webkit-box-orient:vertical;
  word-break:break-all
}
.gcat .unit small{font-size:11px;color:var(--mute)}
.gcat .unit.static{cursor:default}
.gcat .unit:not(.static):hover,.gcat .unit.on{border-color:var(--select)}
.gcat .unit:not(.static):focus-visible{outline:2px solid var(--select);outline-offset:2px}
.detail{
  position:sticky;bottom:12px;z-index:5;background:#121010ee;border:1px solid #5a4714;
  border-radius:14px;padding:14px 16px;margin-top:16px;backdrop-filter:blur(10px);display:none
}
.detail.on{display:grid;grid-template-columns:88px 1fr;gap:12px;align-items:start}
.detail img{width:88px;height:88px;object-fit:cover;object-position:top;border-radius:10px}
.detail .sk{font-size:12px;color:var(--sec);max-height:220px;overflow:auto;word-break:break-word}
.detail .sk div{margin:4px 0;padding:6px 8px;background:#0a0a0a;border-radius:8px}
.film{display:grid;grid-template-columns:repeat(auto-fill,minmax(160px,1fr));gap:10px}
.film .shot{aspect-ratio:9/16}
"""

    extra_html = f"""
<h2 id="data"><span class="en">10 · ARCHIVE</span>数据覆盖 · 561 对齐纪念版图鉴</h2>
<p class="sec-lead">纪念版图鉴是 561 名天子（282+77+99+53+50）。下面是<strong>这张总表现在填了多少</strong>。空着的格子多数是截图当时没弹出面板，或 5★ 词条名 OCR 对不上 wiki，不是网页漏画。点头像看技能。</p>
<div class="card gold">{"".join(bars)}</div>
<div class="grid g5" style="margin-top:14px">{"".join(rarity_pills)}</div>
<p class="caption">魂卡 wiki 156 张齐。人偶 wiki {len(puppets)} / 纪念版图鉴 232（差在低稀有度 wiki 没单独页）。完整可搜表格仍是 <a href="汇总表.html">汇总表.html</a>。</p>

<h2 id="children"><span class="en">11 · CHILDREN</span>天子图鉴 {n}</h2>
<p class="sec-lead">这是纪念版<strong>全部 {n} 名角色</strong>的头像检索墙，不是战斗画面，也不是攻略站图鉴。搜名字或筛属性/星级，<strong>点一张方头像</strong>，底下弹出这名角色的普攻 / TS（点按）/ SS（滑动）/ DS（Drive）/ 队长技。完整可排序表在 <a href="汇总表.html">汇总表.html</a>。伤害公式、点火石、魂卡/人偶数值在 <a href="战斗与数值.html">战斗与数值.html</a>。部分名字乱码、技能里有 <code>||</code>、CP/HP 空着，是纪念版截图 OCR 没认清，不是网页漏画。</p>
<div class="flow" style="margin:4px 0 16px">
  <span class="n">1 搜 / 筛</span>
  <span class="arrow">→</span>
  <span class="n">2 点头像</span>
  <span class="arrow">→</span>
  <span class="n">3 底下看技能</span>
</div>
<div class="filters">
  <input id="cq" placeholder="搜名字" oninput="filtChild()">
  <select id="cel" onchange="filtChild()"><option value="">属性</option><option>火</option><option>水</option><option>木</option><option>光</option><option>暗</option></select>
  <select id="cr" onchange="filtChild()"><option value="">稀有度</option><option>5星</option><option>4星</option><option>3星</option><option>2星</option><option>1星</option></select>
  <span id="cmeta" class="caption"></span>
</div>
<div class="gcat" id="cg">{"".join(child_cards)}</div>
<div class="detail" id="cdet"></div>
<script type="application/json" id="child-json">{json.dumps(payload, ensure_ascii=False)}</script>

<h2 id="cartas"><span class="en">12 · SOUL CARTA</span>魂之歌牌 {len(cartas)}</h2>
<p class="sec-lead">卡面是场景插画，不是虚空全身。点图放大。</p>
<div class="gcat">{"".join(carta_cards)}</div>

<h2 id="puppets"><span class="en">13 · PUPPETS</span>人偶 {len(puppets)}</h2>
<p class="sec-lead">Q 版继承本尊配色。纪念版图鉴 232，wiki 表 {len(puppets)}。</p>
<div class="gcat">{"".join(pup_cards)}</div>

<h2 id="uifilm"><span class="en">14 · UI FILM</span>界面截图走廊</h2>
<p class="sec-lead">纪念版真机 1200×2670。点开放大。战斗 HUD 仍见第 6 节重建图（客户端没有战斗）。</p>
<div class="film">{"".join(film)}</div>
"""

    extra_js = r"""
  const CHILD = JSON.parse(document.getElementById("child-json").textContent || "[]");
  const cmap = Object.fromEntries(CHILD.map(x => [x.id, x]));
  const esc = s => String(s||"").replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
  window.filtChild = function(){
    const q=(document.getElementById("cq").value||"").trim().toLowerCase();
    const el=document.getElementById("cel").value, r=document.getElementById("cr").value;
    let n=0;
    document.querySelectorAll("#cg .unit").forEach(u=>{
      const ok=(!q|| (u.dataset.hay||"").includes(q)) && (!el||u.dataset.el===el) && (!r||u.dataset.rar===r);
      u.style.display = ok ? "" : "none";
      if(ok) n++;
    });
    document.getElementById("cmeta").textContent = "显示 "+n+" / "+CHILD.length;
  };
  document.querySelectorAll("#cg .unit").forEach(u=>{
    const t=u.querySelector("span");
    if(t && !u.title) u.title=t.textContent.trim();
  });
  document.getElementById("cg").addEventListener("click", ev=>{
    const u = ev.target.closest(".unit");
    if(!u) return;
    const d = cmap[u.dataset.id];
    if(!d) return;
    document.querySelectorAll("#cg .unit.on").forEach(x=>{ x.classList.remove("on"); x.removeAttribute("aria-pressed"); });
    u.classList.add("on");
    u.setAttribute("aria-pressed","true");
    const box = document.getElementById("cdet");
    box.classList.add("on");
    box.innerHTML = `<img src="${esc(d.av)}" alt="${esc(d.name)}"><div>
      <b>${esc(d.name)}</b> · ${esc(d.el||"?")} ${esc(d.role||"?")} ${esc(d.rar)}
      <div>CP ${esc(d.cp||"—")}　HP ${esc(d.hp||"—")}　攻 ${esc(d.atk||"—")}　防 ${esc(d.def||"—")}　敏 ${esc(d.agl||"—")}　暴 ${esc(d.crt||"—")}</div>
      <div class="sk"><div><b>普攻</b> ${esc(d.auto||"—")}</div><div><b>TS 点按</b> ${esc(d.tap||"—")}</div><div><b>SS 滑动</b> ${esc(d.slide||"—")}</div><div><b>DS Drive</b> ${esc(d.drive||"—")}</div><div><b>队长</b> ${esc(d.leader||"—")}</div></div>
      <div class="caption">${esc(d.cv||"")}　空值和乱码来自纪念版截图 OCR。</div></div>`;
    box.scrollIntoView({block:"nearest"});
  });
  filtChild();
"""

    with open(BIBLE, encoding="utf-8") as f:
        html_doc = f.read()

    if extra_css.strip() not in html_doc:
        html_doc = html_doc.replace("</style>", extra_css + "\n</style>", 1)

    nav_add = (
        '<a href="#data">数据</a>\n      <a href="#children">天子</a>\n      '
        '<a href="#cartas">魂卡</a>\n      <a href="#puppets">人偶</a>\n      '
        '<a href="#uifilm">UI廊</a>'
    )
    if 'href="#data"' not in html_doc:
        html_doc = html_doc.replace(
            '<a href="#dont">禁区</a>',
            nav_add + '\n      <a href="#dont">禁区</a>',
        )

    ids = '["overview","art","registers","ui","tokens","components","battle","worlds","growth","data","children","cartas","puppets","uifilm","dont"]'
    html_doc = re.sub(
        r'\["overview".*?\]\.forEach\(id =>',
        ids + ".forEach(id =>",
        html_doc,
        count=1,
    )

    html_doc = re.sub(
        r"\n  const CHILD = JSON.parse[\s\S]*?filtChild\(\);\n",
        "\n",
        html_doc,
    )
    html_doc = html_doc.replace("  /* injected */\n", "")
    html_doc = html_doc.replace(
        "})();\n</script>",
        extra_js + "\n})();\n</script>",
        1,
    )

    marker = "<!-- CATALOG -->"
    if marker in html_doc:
        html_doc = re.sub(r"<!-- CATALOG -->.*</main>", marker + extra_html + "\n</main>", html_doc, flags=re.S)
    else:
        html_doc = html_doc.replace("</main>", marker + extra_html + "\n</main>")

    with open(BIBLE, "w", encoding="utf-8") as f:
        f.write(html_doc)
    print("wrote", BIBLE, "bytes", os.path.getsize(BIBLE), "children", n, "cartas", len(cartas), "puppets", len(puppets))


if __name__ == "__main__":
    main()
