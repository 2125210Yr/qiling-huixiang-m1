# -*- coding: utf-8 -*-
"""Build 战斗与数值.html from formulas + character/carta/puppet CSVs."""
from __future__ import annotations

import csv
import html
import os

ROOT = r"F:\天命之子"
USER = os.path.join(ROOT, "天命之子数据")
TABLES = os.path.join(ROOT, "docs", "reference", "gamekee", "tables")
DEST = os.path.join(USER, "战斗与数值.html")


def e(s):
    return html.escape("" if s is None else str(s), quote=True)


def load(path):
    if not os.path.isfile(path):
        return []
    with open(path, encoding="utf-8-sig") as f:
        return list(csv.DictReader(f))


def cell(r, *keys):
    for k in keys:
        v = (r.get(k) or "").strip()
        if v:
            return v
    return "—"


def main():
    children = load(os.path.join(USER, "汇总表.csv"))
    chars = load(os.path.join(TABLES, "characters.csv"))
    cartas = load(os.path.join(USER, "魂之歌牌.csv"))
    puppets = load(os.path.join(USER, "人偶.csv"))
    buffs = load(os.path.join(USER, "buffs.csv"))
    if not cartas:
        cartas = load(os.path.join(TABLES, "soul_cartas.csv"))
    if not puppets:
        puppets = load(os.path.join(TABLES, "puppets.csv"))
    if not buffs:
        buffs = load(os.path.join(TABLES, "buffs.csv"))

    # character rows with any number
    char_rows = []
    for r in children:
        hp = cell(r, "hp", "hp_init")
        atk = cell(r, "atk", "atk_init")
        if hp == "—" and atk == "—":
            continue
        char_rows.append(r)

    carta_trs = []
    for r in cartas:
        carta_trs.append(
            "<tr data-hay='%s'><td>%s</td><td>%s</td><td>%s</td><td>%s</td><td>%s</td>"
            "<td>%s</td><td>%s</td><td>%s</td></tr>"
            % (
                e(" ".join([(r.get("name") or ""), (r.get("rarity") or ""), (r.get("special") or "")]).lower()),
                e(r.get("name")),
                e(r.get("rarity")),
                e(r.get("stat_pair") or r.get("element_gate") or ""),
                e(r.get("element_gate") or ""),
                e(r.get("plain_hp_init") or ""),
                e(r.get("plain_atk_init") or ""),
                e(r.get("flash_hp_init") or r.get("flash_atk_init") or ""),
                e((r.get("special") or "")[:80]),
            )
        )

    pup_trs = []
    for r in puppets:
        pup_trs.append(
            "<tr data-hay='%s'><td>%s</td><td>%s</td><td>%s</td><td>%s</td><td>%s</td><td>%s</td></tr>"
            % (
                e(" ".join([(r.get("name") or ""), (r.get("rarity") or ""), (r.get("tap") or "")]).lower()),
                e(r.get("name")),
                e(r.get("element")),
                e(r.get("rarity")),
                e((r.get("tap") or "")[:60]),
                e((r.get("slide") or "")[:60]),
                e((r.get("drive") or "")[:60]),
            )
        )

    child_trs = []
    for r in children:
        child_trs.append(
            "<tr data-hay='%s'><td>%s</td><td>%s</td><td>%s</td><td>%s</td>"
            "<td>%s</td><td>%s</td><td>%s</td><td>%s</td><td>%s</td><td>%s</td></tr>"
            % (
                e(" ".join([r.get("name") or "", r.get("rarity") or "", r.get("element") or "", r.get("role") or ""]).lower()),
                e(r.get("name")),
                e(r.get("rarity")),
                e(r.get("element")),
                e(r.get("role")),
                e(cell(r, "cp")),
                e(cell(r, "hp", "hp_init")),
                e(cell(r, "atk", "atk_init")),
                e(cell(r, "def_", "def_init")),
                e(cell(r, "agl", "agl_init")),
                e(cell(r, "crt", "crt_init")),
            )
        )

    wiki_char_trs = []
    for r in chars:
        wiki_char_trs.append(
            "<tr data-hay='%s'><td>%s</td><td>%s</td><td>%s</td><td>%s</td>"
            "<td>%s</td><td>%s</td><td>%s</td><td>%s</td></tr>"
            % (
                e((r.get("name") or "").lower()),
                e(r.get("name")),
                e(r.get("rarity")),
                e(r.get("element")),
                e(r.get("role")),
                e(r.get("hp_init") or ""),
                e(r.get("atk_init") or ""),
                e(r.get("hp_max") or ""),
                e(r.get("atk_max") or ""),
            )
        )

    html_doc = f"""<!DOCTYPE html>
<html lang="zh-Hans">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>天命之子 · 战斗与数值</title>
<style>
:root{{--bg:#070707;--panel:#121010;--gold:#CF9403;--text:#F5F5F5;--mute:#8a8a8a;--line:#2a2418;--value:#FFC400}}
*{{box-sizing:border-box}}
body{{margin:0;background:var(--bg);color:var(--text);font:16px/1.6 "Microsoft YaHei","Noto Sans SC",sans-serif}}
header{{position:sticky;top:0;z-index:20;background:#0a0a0aee;border-bottom:1px solid var(--line);backdrop-filter:blur(10px)}}
.inner{{max-width:1180px;margin:0 auto;padding:10px 20px;display:flex;gap:12px;align-items:center;flex-wrap:wrap}}
.brand b{{color:var(--value);letter-spacing:.12em}}
nav a{{color:var(--gold);margin-right:10px;text-decoration:none;font-size:14px}}
nav a:hover{{color:#fff}}
main{{max-width:1180px;margin:0 auto;padding:28px 20px 80px}}
h1{{font-size:28px;color:var(--value);margin:8px 0 16px}}
h2{{font-size:22px;color:var(--gold);margin:48px 0 12px;letter-spacing:.08em}}
h3{{font-size:16px;margin:20px 0 8px}}
p,li{{color:#ddd}}
.lede{{color:#aaa;max-width:52em}}
.card{{background:linear-gradient(180deg,#161111,#0c0c0c);border:1px solid var(--line);border-radius:12px;padding:16px;margin:12px 0}}
code,pre{{font-family:Consolas,"JetBrains Mono",monospace;background:#0a0a0a;color:var(--value)}}
pre{{padding:14px;border-radius:10px;overflow:auto;border:1px solid var(--line);line-height:1.5;font-size:13px}}
table{{width:100%;border-collapse:collapse;font-size:13px}}
th,td{{border-bottom:1px dashed #333;padding:7px 8px;text-align:left;vertical-align:top}}
th{{color:var(--gold);font-weight:500;position:sticky;top:48px;background:#0e0e0e}}
.filters{{display:flex;gap:8px;flex-wrap:wrap;margin:10px 0}}
.filters input{{background:#0e0e0e;color:#eee;border:1px solid #3a3018;border-radius:8px;padding:8px 10px;min-width:220px}}
.caption{{font-size:12px;color:var(--mute)}}
.g2{{display:grid;grid-template-columns:1fr 1fr;gap:12px}}
@media(max-width:800px){{.g2{{grid-template-columns:1fr}}}}
.pill{{display:inline-block;border:1px solid #5a4714;border-radius:999px;padding:2px 10px;color:var(--gold);font-size:12px;margin:2px}}

.mods{{display:flex;gap:6px;flex-wrap:wrap;align-items:center}}
.mods a{{color:#CF9403;text-decoration:none;font-size:13px;line-height:1;padding:7px 14px;border-radius:999px;border:1px solid #3a3018;background:transparent;white-space:nowrap}}
.mods a:hover{{color:#fff;border-color:#5c4a10;background:#1a1408}}
.mods a.on,.mods a[aria-current="page"]{{color:#111;background:#FFC400;border-color:#FFC400;font-weight:700}}
header .mods{{flex:1}}
.page-nav{{display:flex;gap:8px 12px;flex-wrap:wrap;font-size:13px}}
.page-nav a{{color:#CF9403;text-decoration:none}}
.page-nav a:hover{{color:#fff}}

</style>
</head>
<body>
<header><div class="inner">
  <div class="brand"><b>DESTINY CHILD</b><div class="caption">02 · 数值设计</div></div>
  <nav class="mods" aria-label="设计模块"><a href="设计手册.html">手册</a><a href="视觉图鉴.html">UI</a><a href="战斗与数值.html" class="on" aria-current="page">数值</a><a href="战斗动效.html">动效</a><a href="汇总表.html">图鉴</a></nav>
  <nav class="page-nav">
    <a href="#cover">覆盖</a>
    <a href="#formula">公式</a>
    <a href="#ignition">点火</a>
    <a href="#children">天子数值</a>
    <a href="#wiki5">Wiki 面板</a>
    <a href="#cartas">魂卡</a>
    <a href="#puppets">人偶</a>
    <a href="#buffs">Buff</a>
  </nav>
</div></header>
<main>
<h1>战斗与数值</h1>
<p class="lede">UI 画风见 <a href="视觉图鉴.html">视觉图鉴.html</a>。这一页把国际服 GameKee 测出来的<strong>伤害公式</strong>、点火石、以及天子 / 魂卡 / 人偶的<strong>面板与技能数值</strong>收在一起。公式来源：<a href="https://www.gamekee.com/dc/52136.html" target="_blank" rel="noopener">国际服 TS/SS/FT 公式</a>、<a href="https://www.gamekee.com/dc/62504.html" target="_blank" rel="noopener">点火系统介绍</a>。日服 / 韩服 wiki 作对照，不直接当国际服数值。</p>

<h2 id="cover">覆盖情况（不能当完整游戏包）</h2>
<p>够用来<strong>对照研究</strong>和做 clean-room 切片，<strong>不能</strong>还原整局 Destiny Child 的每个数字。下面是按系统盘点。</p>
<div class="g2">
<div class="card">
<h3>已经收进来</h3>
<ul>
<li>TS / Fever / SS 伤害公式（玩家实测，非官方源码）</li>
<li>属性克制与暴击补正表</li>
<li>点火系统规则 + 红字收益（主核四种、装备条件）</li>
<li>纪念版 561 天子：名字/头像齐；属性 90%；HP 约 61%；技能文本约 46%；点火 U 约 30%</li>
<li>Wiki 5★+4★ 独立页 345 人：TS 文本 100%，点火 U 61%，满破 HP 仅 21%</li>
<li>魂卡 156：特效 98%，普/闪初始值约一半，满值更少</li>
<li>人偶 220 / 纪念版 232：技能文本 22–47%，没有成长曲线数字</li>
<li>Buff 名称 93 条（多数只有名字，没有具体百分比）</li>
</ul>
</div>
<div class="card">
<h3>游戏里有、这里没有或几乎空</h3>
<ul>
<li><strong>Drive（大招）伤害公式</strong>、普攻公式、充能/Fever 7 秒 70 hit 的数值证明</li>
<li><strong>装备</strong>武器/防具/饰品图鉴：wiki 几乎只有图，+15 面板没进表</li>
<li><strong>点火石实例</strong>：没有每颗核心的三条具体数值表，只有规则和评级文字</li>
<li><strong>温泉</strong>好感度加值表、附魔（Enchant）数值</li>
<li>敌方/Boss 面板、关卡波次、Raid / WB / PvP 的完整倍率表</li>
<li>造型（Costume）数值、还魂、地下城等模式专属系数</li>
<li>客户端 <code>catalog.json</code> 是 clean-room 切片，不是 561 人原作数值包</li>
</ul>
</div>
</div>

<h2 id="formula">伤害公式</h2>
<div class="card">
<p>玩家实测，不是官方源码。Fever 期间伤害系数 <code>0.6</code>。额外增伤下限 <code>0.1</code>。</p>
<h3>TS / Fever（点按）</h3>
<pre>[(额外攻击 × 125 + 技能伤害 × 400) × 属性暴击补正 / (0.15 × 防御 + 400) + 实际无视防御]
  × (1 + 额外增伤) × Fever补正
  + 加成伤害 × (暴击 ? 2 : 1)
  + 附魔与魂卡伤害</pre>
<p>Fever 补正：Fever 中为 <strong>0.6</strong>，否则 1。<br>
实际无视防御 = <code>pierce × 0.6 + pierce × 0.4 × 防御 / 20000</code></p>
<h3>SS（滑动）</h3>
<pre>[(额外攻击 × 120 + 技能伤害 × 400 + 敏捷项) × 属性暴击补正 / (0.2 × 防御 + 400) + 实际无视防御]
  × (1 + 额外增伤)
  + 加成伤害 + 附魔与魂卡</pre>
<p class="caption">敏捷项未完全测清。百分比技能用 (额外攻击+基础攻击)×125×百分比×补正 / (0.15×防御+400)。</p>
<h3>属性 × 暴击补正</h3>
<table>
<tr><th></th><th>不暴击</th><th>暴击</th></tr>
<tr><td>克制</td><td>1.4</td><td>2.4</td></tr>
<tr><td>中性</td><td>1.0</td><td>2.0</td></tr>
<tr><td>被克</td><td>0.7</td><td>1.7</td></tr>
</table>
<p>循环：火 → 木 → 水 → 火。光 ↔ 暗。</p>
<h3>额外攻击里有什么</h3>
<ul>
<li>百分比攻、固定攻、装备攻、温泉攻</li>
<li>同一图标 buff：Drive &gt; Slide &gt; Tap；同级后写覆盖前写</li>
<li>附魔与魂卡加在乘区之后，互不加</li>
</ul>
</div>

<h2 id="ignition">点火石</h2>
<div class="g2">
<div class="card">
<h3>系统</h3>
<ul>
<li>夜世界「赫菲斯托斯的锻造屋」，账号 Lv.7 可进</li>
<li>Child <strong>6★、Lv.60、觉醒 100%</strong> 才能装核心</li>
<li>四种主核：增幅攻击 / 防御 / 敏捷 / 暴击</li>
<li>打造消耗：该类型核心材料 ×99 + 原生 5★ Child + 15 万金币（进化/强化型不能当材料）</li>
<li>材料不足可 50 水晶买 1 个；核心卖出 7 红石</li>
<li>每颗核心 3 条：第 1 条固定主属性，第 2、3 条随机（不含主属性）</li>
</ul>
</div>
<div class="card">
<h3>红字收益（每 100 点）</h3>
<ul>
<li><strong>红攻</strong>：按攻击比例加伤，全场合。TS：<code>额外攻×125×(1+0.0015A) + 基础攻×125×0.0015A</code></li>
<li><strong>红暴</strong>：暴击时额外增伤，约 +30%/百点。WB / Raid</li>
<li><strong>红敏</strong>：弱点伤害，约 +20%/百点。Raid、需 Fever 的本</li>
<li><strong>红防</strong>：按比例减伤，PVP 优先</li>
<li><strong>白字 HP</strong>：直接加面板，PVP 几乎不亏</li>
</ul>
<p>经验排序（一般场合）：红攻 &gt; 红暴 &gt; 红敏；弱点 Fever 时红敏可超过白攻。</p>
<p class="caption">详见 <a href="https://www.gamekee.com/dc/68263.html" target="_blank">点火石挑选</a> · <a href="https://www.gamekee.com/dc/168169.html" target="_blank">点火教学</a></p>
</div>
</div>
<p><span class="pill">主核 400 红字</span><span class="pill">技能点火 U 改段数/目标</span><span class="pill">狗粮核心别乱点</span></p>

<h2 id="children">天子数值（纪念版 561）</h2>
<p class="caption">HP/攻/防来自纪念版截图 OCR + wiki。空着就是当时没扫到。完整技能点开汇总表。</p>
<div class="filters"><input id="q1" placeholder="搜天子名字 / 属性 / 星级" oninput="filt('q1','t1')"></div>
<div style="max-height:520px;overflow:auto;border:1px solid #2a2418;border-radius:10px">
<table id="t1">
<thead><tr><th>名字</th><th>稀有</th><th>属性</th><th>职业</th><th>CP</th><th>HP</th><th>攻</th><th>防</th><th>敏</th><th>暴</th></tr></thead>
<tbody>
{"".join(child_trs)}
</tbody>
</table>
</div>
<p class="caption">共 {len(children)} 行。有面板数字的约 {len(char_rows)} 人。</p>

<h2 id="wiki5">Wiki 5/4 星面板（国际服词条）</h2>
<p class="caption">GameKee 5★+4★ 独立页。初始 / 满破很多词条本身就空着。</p>
<div class="filters"><input id="q2" placeholder="搜 wiki 天子" oninput="filt('q2','t2')"></div>
<div style="max-height:420px;overflow:auto;border:1px solid #2a2418;border-radius:10px">
<table id="t2">
<thead><tr><th>名字</th><th>稀有</th><th>属性</th><th>职业</th><th>HP初</th><th>攻初</th><th>HP满</th><th>攻满</th></tr></thead>
<tbody>
{"".join(wiki_char_trs)}
</tbody>
</table>
</div>
<p class="caption">{len(chars)} 条。技能拆表见 tables/skills.csv（含点火 U）。</p>

<h2 id="cartas">魂之歌牌数值</h2>
<div class="filters"><input id="q3" placeholder="搜魂卡 / 特效" oninput="filt('q3','t3')"></div>
<div style="max-height:420px;overflow:auto;border:1px solid #2a2418;border-radius:10px">
<table id="t3">
<thead><tr><th>名字</th><th>稀有</th><th>双维</th><th>属性门</th><th>普HP</th><th>普攻</th><th>闪</th><th>特效</th></tr></thead>
<tbody>
{"".join(carta_trs)}
</tbody>
</table>
</div>
<p class="caption">{len(cartas)} 张。普卡 / 闪卡两套初始值。卡面见视觉图鉴「魂卡」。</p>

<h2 id="puppets">人偶技能</h2>
<div class="filters"><input id="q4" placeholder="搜人偶" oninput="filt('q4','t4')"></div>
<div style="max-height:420px;overflow:auto;border:1px solid #2a2418;border-radius:10px">
<table id="t4">
<thead><tr><th>名字</th><th>属性</th><th>稀有</th><th>Tap</th><th>Slide</th><th>Drive</th></tr></thead>
<tbody>
{"".join(pup_trs)}
</tbody>
</table>
</div>
<p class="caption">{len(puppets)} 只。Q 版继承本尊配色。纪念版图鉴 232，wiki 缺低稀有度单独页。</p>

<h2 id="buffs">Buff / Debuff 名录</h2>
<p class="caption">93 条。多数只有名字，没有具体百分比或持续时间。</p>
<table>
<thead><tr><th>类型</th><th>名称</th><th>效果</th></tr></thead>
<tbody>
{"".join(
    "<tr><td>%s</td><td>%s</td><td>%s</td></tr>" % (e(r.get("kind")), e(r.get("name")), e(r.get("effect")))
    for r in buffs
)}
</tbody>
</table>

<div class="card" style="margin-top:40px">
<h3>三站来源</h3>
<ul>
<li>国际服 <a href="https://www.gamekee.com/dc/" target="_blank">gamekee.com/dc</a> · 公式 52136 · 点火 62504 / 168169 / 68263</li>
<li>韩服 <a href="https://www.gamekee.com/destinychild/" target="_blank">destinychild</a> · 日服 <a href="https://www.gamekee.com/dcj/" target="_blank">dcj</a> 作对照，数值以国际服为准</li>
<li>本地表：<code>汇总表.csv</code> · <code>魂之歌牌.csv</code> · <code>人偶.csv</code> · <code>docs/reference/gamekee/tables/skills.csv</code></li>
</ul>
</div>
</main>
<script>
function filt(qid, tid){{
  const q=(document.getElementById(qid).value||"").trim().toLowerCase();
  document.querySelectorAll("#"+tid+" tbody tr").forEach(tr=>{{
    tr.style.display = (!q || (tr.dataset.hay||"").includes(q)) ? "" : "none";
  }});
}}
</script>
</body>
</html>
"""
    with open(DEST, "w", encoding="utf-8") as f:
        f.write(html_doc)
    print("wrote", DEST, "bytes", os.path.getsize(DEST),
          "children", len(children), "wiki", len(chars), "cartas", len(cartas), "puppets", len(puppets), "buffs", len(buffs))


if __name__ == "__main__":
    main()
