# -*- coding: utf-8 -*-
"""Crop battle video panes and write 战斗动效.html + inject a summary into 视觉图鉴.html."""
from __future__ import annotations

import os
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(r"F:\天命之子")
USER = ROOT / "天命之子数据"
KEYS = ROOT / "docs" / "reference" / "gamekee" / "_combined" / "battle_vids" / "keys"
CROP = ROOT / "docs" / "reference" / "gamekee" / "_combined" / "battle_vids" / "crops"
REL = "../docs/reference/gamekee/_combined/battle_vids/crops"
REFS = "../docs/reference/gamekee/_combined/battle_refs"

CROP.mkdir(parents=True, exist_ok=True)


def save_jpg(im: Image.Image, name: str, q: int = 86) -> str:
    p = CROP / name
    rgb = im.convert("RGB")
    rgb.save(p, "JPEG", quality=q, optimize=True)
    return name


def crop_center_third(im: Image.Image) -> Image.Image:
    w, h = im.size
    x0 = int(w * 0.333)
    x1 = int(w * 0.667)
    # trim a few px of gutter
    return im.crop((x0 + 4, 0, x1 - 4, h))


def crop_nonblack(im: Image.Image, thresh: int = 24) -> Image.Image:
    """Keep the brightest portrait pane (letterboxed phone capture)."""
    g = im.convert("L")
    w, h = g.size
    px = g.load()
    col = []
    step_y = 4
    n = max(1, h // step_y)
    for x in range(w):
        s = 0
        for y in range(0, h, step_y):
            s += px[x, y]
        col.append(s / n)
    best = (0, 0)
    start = None
    for i, m in enumerate(col + [0]):
        if m > thresh:
            if start is None:
                start = i
        elif start is not None:
            if i - start > best[1] - best[0]:
                best = (start, i)
            start = None
    left, right = best
    if right - left < 80:
        return im
    row = []
    n2 = max(1, (right - left) // 3)
    for y in range(h):
        s = 0
        for x in range(left, right, 3):
            s += px[x, y]
        row.append(s / n2)
    top = next((i for i, m in enumerate(row) if m > thresh), 0)
    bot = h - 1 - next((i for i, m in enumerate(reversed(row)) if m > thresh), 0)
    pad = 2
    return im.crop((max(0, left - pad), max(0, top - pad), min(w, right + pad), min(h, bot + pad)))


def open_key(vid: str, name: str) -> Image.Image:
    return Image.open(KEYS / vid / name).convert("RGB")


def pane(vid: str, name: str, letterbox: bool = False) -> Image.Image:
    im = open_key(vid, name)
    if letterbox:
        return crop_nonblack(im)
    return crop_center_third(im)


def filmstrip(frames: list[Image.Image], height: int = 220, name: str = "strip.jpg") -> str:
    rs = []
    for im in frames:
        r = height / im.height
        w = max(40, int(im.width * r))
        rs.append(im.resize((w, height), Image.Resampling.LANCZOS))
    gap = 4
    total = sum(i.width for i in rs) + gap * (len(rs) - 1)
    canvas = Image.new("RGB", (total, height), (8, 8, 8))
    x = 0
    for i, im in enumerate(rs):
        canvas.paste(im, (x, 0))
        x += im.width + gap
    return save_jpg(canvas, name, q=80)


def burst_frames(vid: str, folder: str, picks: list[str], letterbox: bool = False) -> list[Image.Image]:
    out = []
    for fn in picks:
        im = Image.open(KEYS / vid / folder / fn).convert("RGB")
        out.append(crop_nonblack(im) if letterbox else crop_center_third(im))
    return out


def build_crops() -> dict[str, str]:
    """Return semantic name -> filename."""
    R, K, W = "mJrT2conPCI", "Vdf4V693IcU", "89jpoNqAwa8"
    jobs = {
        "ragna_hud.jpg": pane(R, "11_176s.jpg"),
        "ragna_hud_drive.jpg": pane(R, "10_168s.jpg"),
        "ragna_hud_slide.jpg": pane(R, "14_296s.jpg"),
        "ragna_showtime.jpg": pane(R, "05_040s.jpg"),
        "ragna_showtime_mei.jpg": pane(R, "15_308s.jpg"),
        "ragna_ready.jpg": pane(R, "08_100s.jpg"),
        "ragna_perfect.jpg": pane(R, "07_092s.jpg"),
        "ragna_great.jpg": pane(R, "06_056s.jpg"),
        "ragna_warn.jpg": pane(R, "09_164s.jpg"),
        "ragna_qte.jpg": pane(R, "12_236s.jpg"),
        "ragna_fever.jpg": pane(R, "13_240s.jpg"),
        "ragna_result.jpg": pane(R, "16_376s.jpg"),
        "kr_lobby.jpg": pane(K, "01_000s.jpg"),
        "kr_boss.jpg": pane(K, "03_012s.jpg"),
        "kr_showtime.jpg": pane(K, "07_068s.jpg"),
        "kr_showtime2.jpg": pane(K, "04_020s.jpg"),
        "kr_perfect.jpg": pane(K, "05_036s.jpg"),
        "kr_perfect2.jpg": pane(K, "08_072s.jpg"),
        "kr_hud.jpg": pane(K, "10_120s.jpg"),
        "kr_hud2.jpg": pane(K, "16_236s.jpg"),
        "kr_fever.jpg": pane(K, "09_080s.jpg"),
        "kr_fever2.jpg": pane(K, "15_212s.jpg"),
        "kr_crit.jpg": pane(K, "17_248s.jpg"),
        "kr_complete.jpg": pane(K, "18_256s.jpg"),
        "wb_lobby.jpg": pane(W, "01_000s.jpg", True),
        "wb_intro.jpg": pane(W, "06_064s.jpg", True),
        "wb_drive.jpg": pane(W, "07_084s.jpg", True),
        "wb_hud.jpg": pane(W, "08_100s.jpg", True),
        "wb_hud2.jpg": pane(W, "10_180s.jpg", True),
        "wb_fever.jpg": pane(W, "09_132s.jpg", True),
        "wb_fever2.jpg": pane(W, "11_212s.jpg", True),
        "wb_fever3.jpg": pane(W, "12_220s.jpg", True),
        "wb_result.jpg": pane(W, "13_224s.jpg", True),
        "drive_qte.jpg": crop_center_third(open_key(R, os.path.join("burst_drive", "f18.jpg"))),
        "drive_ready.jpg": crop_center_third(open_key(R, os.path.join("burst_drive", "f01.jpg"))),
        "tap_hud.jpg": crop_center_third(open_key(R, os.path.join("burst_cutin", "f08.jpg"))),
        "kr_weak.jpg": crop_center_third(open_key(K, os.path.join("burst_qte", "f10.jpg"))),
        "wb_combo.jpg": crop_nonblack(open_key(W, os.path.join("burst_warn", "f08.jpg"))),
    }
    names = {}
    for fn, im in jobs.items():
        save_jpg(im, fn)
        names[fn] = fn

    filmstrip(
        burst_frames(R, "burst_tap", [f"f{i:02d}.jpg" for i in (1, 6, 11, 16, 22, 28)]),
        name="strip_showtime.jpg",
    )
    filmstrip(
        burst_frames(R, "burst_drive", [f"f{i:02d}.jpg" for i in (1, 8, 12, 15, 18, 22, 26)]),
        name="strip_drive.jpg",
    )
    filmstrip(
        burst_frames(K, "burst_qte", [f"f{i:02d}.jpg" for i in (1, 6, 12, 18, 24, 30)]),
        name="strip_hit.jpg",
    )
    filmstrip(
        burst_frames(K, "burst_warn", [f"f{i:02d}.jpg" for i in (1, 7, 13, 19, 24)]),
        name="strip_warn.jpg",
    )
    filmstrip(
        burst_frames(W, "burst_drive", [f"f{i:02d}.jpg" for i in (1, 6, 12, 18, 24, 30)], letterbox=True),
        name="strip_wb.jpg",
    )
    names.update(
        {
            "strip_showtime.jpg": "strip_showtime.jpg",
            "strip_drive.jpg": "strip_drive.jpg",
            "strip_hit.jpg": "strip_hit.jpg",
            "strip_warn.jpg": "strip_warn.jpg",
            "strip_wb.jpg": "strip_wb.jpg",
        }
    )
    return names


def fig(src: str, cap: str, cls: str = "shot fit") -> str:
    return (
        f'<figure class="{cls}"><img src="{REL}/{src}" alt="{cap}" loading="lazy">'
        f"<figcaption>{cap}</figcaption></figure>"
    )


def strip(src: str, cap: str) -> str:
    return (
        f'<figure class="shot strip"><img src="{REL}/{src}" alt="{cap}" loading="lazy">'
        f'<figcaption>{cap}</figcaption></figure>'
    )


HTML = r'''<!DOCTYPE html>
<html lang="zh-Hans">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>天命之子 · 战斗动效</title>
<style>
:root{--bg:#050505;--panel:#121010;--gold:#CF9403;--value:#FFC400;--text:#F5F5F5;--mute:#8a8a8a;--line:#2a2418;--ok:#7dba6a;--miss:#e07070}
*{box-sizing:border-box}
html{scroll-behavior:smooth}
body{margin:0;background:var(--bg);color:var(--text);font:16px/1.6 "Microsoft YaHei","Noto Sans SC",sans-serif}
header{position:sticky;top:0;z-index:20;background:#0a0a0aee;border-bottom:1px solid var(--line);backdrop-filter:blur(10px)}
.inner{max-width:1180px;margin:0 auto;padding:10px 20px;display:flex;gap:12px;align-items:center;flex-wrap:wrap}
.brand b{color:var(--value);letter-spacing:.12em}
nav a{color:var(--gold);margin-right:10px;text-decoration:none;font-size:13px}
nav a:hover{color:#fff}
main{max-width:1180px;margin:0 auto;padding:28px 20px 80px}
h1{font-size:28px;color:var(--value);margin:8px 0 16px}
h2{font-size:22px;color:var(--gold);margin:48px 0 12px;letter-spacing:.08em}
h3{font-size:16px;margin:20px 0 8px;color:#fff}
p,li{color:#ddd}
.lede{color:#aaa;max-width:54em}
.card{background:linear-gradient(180deg,#161111,#0c0c0c);border:1px solid var(--line);border-radius:12px;padding:16px;margin:12px 0}
.gold{border-color:#5a4714}
.caption{font-size:12px;color:var(--mute)}
.grid{display:grid;gap:12px}
.g2{grid-template-columns:1fr 1fr}
.g3{grid-template-columns:repeat(3,1fr)}
.g4{grid-template-columns:repeat(4,1fr)}
.g5{grid-template-columns:repeat(5,1fr)}
@media(max-width:900px){.g2,.g3,.g4,.g5{grid-template-columns:1fr}}
.shot{position:relative;border-radius:12px;overflow:hidden;border:1px solid #2a2418;cursor:zoom-in;background:#000}
.shot.fit{aspect-ratio:9/16}
.shot.fit img{width:100%;height:100%;object-fit:contain;background:#000}
.shot.strip{aspect-ratio:auto}
.shot.strip img{width:100%;height:auto;display:block;object-fit:contain}
.shot figcaption{position:absolute;left:0;right:0;bottom:0;padding:16px 8px 8px;background:linear-gradient(transparent,#000d);font-size:11px;color:#eee;text-align:center}
table{width:100%;border-collapse:collapse;font-size:13px}
th,td{border-bottom:1px dashed #333;padding:7px 8px;text-align:left;vertical-align:top}
th{color:var(--gold);font-weight:500}
.ok{color:var(--ok)} .miss{color:var(--miss)}
.pill{display:inline-block;border:1px solid #5a4714;border-radius:999px;padding:2px 10px;color:var(--gold);font-size:12px;margin:2px}
.flow{display:flex;flex-wrap:wrap;gap:8px;align-items:center;margin:10px 0 16px}
.flow .n{background:#1a1408;border:1px solid #5a4714;color:var(--value);padding:6px 12px;border-radius:999px;font-size:13px}
.flow .arrow{color:var(--gold)}
.lb{position:fixed;inset:0;background:#000e;z-index:80;display:none;align-items:center;justify-content:center;padding:24px}
.lb.on{display:flex}
.lb img{max-width:min(92vw,420px);max-height:92vh;border-radius:12px;border:1px solid #5a4714}
.lb .x{position:absolute;top:16px;right:20px;color:#fff;font-size:28px;background:none;border:0;cursor:pointer}
code{font-family:Consolas,monospace;color:var(--value)}
.k{font-size:12px;color:var(--mute)}
</style>
</head>
<body>
<header><div class="inner">
  <div class="brand"><b>DESTINY CHILD</b><div class="caption">03 · 动效设计</div></div>
  <nav class="mods" aria-label="设计模块"><a href="设计手册.html">手册</a><a href="视觉图鉴.html">UI</a><a href="战斗与数值.html">数值</a><a href="战斗动效.html" class="on" aria-current="page">动效</a><a href="汇总表.html">图鉴</a></nav>
  <nav class="page-nav">
    <a href="#cover">覆盖</a>
    <a href="#cover">覆盖</a>
    <a href="#hud">HUD</a>
    <a href="#modes">模式</a>
    <a href="#vfx">打击</a>
    <a href="#seq">时间轴</a>
    <a href="#ragna">Ragna</a>
    <a href="#raid">Raid</a>
    <a href="#wb">世界王</a>
  </nav>
</div></header>
<main>
<h1>战斗动效 · 模式对照</h1>
<p class="lede">三支 YouTube 按关键时刻抽了静帧和 12fps 连拍。战斗核是同一套：<em>圆台 + 拱形 Boss 血 + 底栏圆头像</em>。换的是人数、大厅皮、Boss 体量和全屏演出词。下面所有图都是录像中缝裁出来的战斗屏，左右 Home / 立绘是录屏分栏，不是游戏 UI。</p>

<h2 id="cover">这三支片实际拍到了什么</h2>
<div class="card gold">
<p>你点名要的五种战场：普通主线、困难主线、星云、Raid、世界王。<strong>这三支录像只覆盖了 Ragna:Break（国际服 + 韩服）和世界王。</strong> 标题里的「Raid Season 12」结算屏写的是 <code>RAGNA:BREAK COMPLETE</code>，韩服常把拉格纳爆破叫成 Raid。普通主线只在 GameKee 静帧里有一张「第7關 · PHASE 1/3」。困难主线和星云<strong>没有录像、也没有单独战场静帧</strong>——铬和主线同一套，换的是入口和数值。</p>
</div>
<table>
<tr><th>录像</th><th>片长</th><th>实际模式</th><th>操作档</th><th>没拍到</th></tr>
<tr><td><a href="https://www.youtube.com/watch?v=mJrT2conPCI">mJrT2conPCI</a> Global Ragna:Break S3</td><td>~383s 横屏三栏</td><td>5 人打 Equinoctial Bari（月夜圆台）</td><td>MANUAL · ×2</td><td>主线 / 星云</td></tr>
<tr><td><a href="https://www.youtube.com/watch?v=Vdf4V693IcU">Vdf4V693IcU</a> KR「Raid S12」</td><td>~262s 三栏</td><td>结算是 Ragna:Break，Boss 창기사 루인</td><td>FULL AUTO · ×3</td><td>5 人公会 Raid 大厅（若和拉格纳不是同一入口）</td></tr>
<tr><td><a href="https://www.youtube.com/watch?v=89jpoNqAwa8">89jpoNqAwa8</a> 100th WB</td><td>~394s 竖屏</td><td>World Boss Trial · Aurora King</td><td>FULL AUTO · ×3 · 20 头像</td><td>主线 / 星云；后半是抽卡</td></tr>
</table>

<h2 id="hud">共用战斗核（五种模式都不换这套铬）</h2>
<p>主线静帧和 Boss 录像对得上：角色站在<strong>圆形石台</strong>上，不是棋盘。UI 贴顶贴底，中间留给人。</p>
<div class="grid g4">
__HUD_FIGS__
</div>
<div class="grid g2">
<article class="card">
<h3>顶条（所有模式）</h3>
<ul>
<li>左：<code>&gt;&gt; ×2 SPEED</code> / <code>×3 SPEED</code>，金字，可点</li>
<li>中：红色拱形 Boss / 关卡总血。数字是 <code>当前 / 上限</code>，下面一行 <code>89%</code>、<code>DAMAGE</code> 累计、计时 <code>02:47</code>、<code>PAUSE</code></li>
<li>右：<code>MANUAL</code> / <code>FULL AUTO</code> / 半自动。Boss 战国际服这支是手动，韩服 Ragna 和 WB 是全自动</li>
<li>主线多一行关卡名 + <code>PHASE 1/3</code>（wiki「第7關 第二次死亡」）。Ragna / WB 没有 PHASE，只有一条百万级总血</li>
</ul>
</article>
<article class="card">
<h3>底栏才是操作面</h3>
<ul>
<li>常规 5 个<strong>圆头像</strong>沿底弧排列，不是编队竖条</li>
<li>环 = 充能。黄环亮 + 头像上浮字 <code>TAP FULL</code> = 可点普技；绿环 / <code>SLIDE SKILL READY</code> = 可上滑</li>
<li>翅膀标 + <code>DRIVE SKILL READY</code> = 这人能吃全队 Drive</li>
<li>冷却中头像压暗，中央大数字 <code>COOL TIME 7</code></li>
<li>头像下：等级小标（常是 60 MAX）+ 短名。头像上可叠 Buff 图标（绿十字 Regen、红骷髅、Vampirism）</li>
<li>正中绿条 = 全队 HP%；绿条下 <code>FEVER 40%</code>。Fever 窗口改成粉紫彩虹条 + <code>FEVER TIME</code></li>
<li>世界王把 5 圆头换成<strong>两排约 20 个小圆头</strong>，环和字还在，只是缩小</li>
</ul>
</article>
</div>

<h2 id="modes">五种战场：换皮对照</h2>
<div class="grid g2">
<article class="card">
<h3>普通主线 <span class="pill">静帧</span></h3>
<p class="ok">有图：国际服 wiki「游戏系统介绍」裁切。</p>
<p>5 人对 3～5 个小体型敌人，圆台 + 关卡画背景（森林 / 地牢）。顶条是<strong>关卡名 + PHASE</strong>，不是百万 Boss 血。底栏仍是 5 圆头。敌人各自头顶血条和属性标。自动三档都能用，狗粮关通常 FULL AUTO。</p>
<div class="grid g2" style="margin-top:8px">
<figure class="shot fit"><img src="../docs/reference/gamekee/_combined/battle_refs/crop_inbattle.png" alt="主线场上 PHASE" loading="lazy"><figcaption>主线 · 第7關 PHASE 1/3</figcaption></figure>
<figure class="shot fit"><img src="../docs/reference/gamekee/_combined/battle_refs/crop_hud.png" alt="主线底栏五圆头" loading="lazy"><figcaption>主线底栏 · 五圆头 / Fever</figcaption></figure>
</div>
</article>
<article class="card">
<h3>困难主线 <span class="pill">无录像</span></h3>
<p class="miss">这三支片、现有 battle_refs 都没有单独的困难主线战场图。</p>
<p>入口会换难度签，战场铬与普通主线同一套：PHASE、5 圆头、圆台。差别在敌面板和掉落，不在 UI 动效。复刻时不要为困难模式另做一套 HUD。</p>
</article>
<article class="card">
<h3>星云 / 空间唤醒 <span class="pill">无录像</span></h3>
<p class="miss">三支片没有星云。wiki 内容地图有入口，没有战场逐帧。</p>
<p>战斗核仍是 5 人圆台。布景换成星云/空间主题，顶条语法同主线（关卡名或波次，不是世界王那种 20 头像）。在补到录像之前，按主线 HUD + 换背景处理。</p>
</article>
<article class="card gold">
<h3>Ragna:Break / 韩服口中的 Raid <span class="pill">两支录像</span></h3>
<p>5 人打<strong>一个巨大 Boss</strong>。大厅是 Boss 车列表（BOSS LEVEL、剩余时间、助战 CALL、20 人上限）。进战顶条是百万级一条血。演出词：<code>THE MASTER OF DESIRE BOSS</code> 开场 → <code>IT'S SHOWTIME!!</code> → <code>READY TO RUMBLE?</code> / Drive Crush → 红屏 <code>WARNING !! ENEMY DRIVE SKILL</code> → 结算金环 <code>RAGNA:BREAK COMPLETE</code> 擊/滅。</p>
</article>
<article class="card gold">
<h3>世界王 World Boss Trial <span class="pill">一支录像</span></h3>
<p>大厅：<code>WORLD BOSS TRIAL</code>、期数（12st）、<code>TRIAL READY</code>、今日挑战次数。战场是金色圆形竞技场 + 棋盘边。底栏<strong>约 20 个圆头像两排</strong>。Boss 体量占满圆台（Aurora King）。Drive 仍走 DRIVE CRUSH 切镜。Fever 时 20 头一起喷数字。结束是伤害总量叠在 Boss 身上（<code>100TH HIT · 3,308,051</code>），不是 Ragna 那种擊滅金环。</p>
</article>
</div>

<h2 id="vfx">打击数字 · 全屏词 · 特效层</h2>
<p>数字是多层叠字，不是一条 HUD。Fever 时同一帧能看到总伤、WeakPoint、单 hit、COMBO 四层。</p>
<table>
<tr><th>层</th><th>样子（录像里）</th><th>何时</th></tr>
<tr><td>单 hit</td><td>白 / 浅黄实心数字，约 3 位到 5 位。属性色小字（绿木、青水）贴在右下角</td><td>普攻、TAP、技能每一击</td></tr>
<tr><td>WeakPoint</td><td>橙黄立体字，同一词叠 2～3 层错位，下面跟一个更大的数字</td><td>打在弱点 / 克制部位，Fever 里几乎每下都有</td></tr>
<tr><td>Critical</td><td>红字 <code>Critical</code> + 大红数字（例 15,301 / 118,147）</td><td>暴击。Drive 后第一击常见</td></tr>
<tr><td>COMBO + 总伤</td><td>顶上蓝或金：<code>17 COMBO</code> + <code>383,040 DAMAGE</code>。Fever 时金、WB 低连段有时蓝</td><td>Fever 窗口。数字一直跳</td></tr>
<tr><td>判定条</td><td>紫 <code>PERFECT !</code> + 白 <code>DAMAGE 150%</code> + 绿 <code>83% TO FEVER</code></td><td>Drive QTE 结束。Great 是绿字</td></tr>
<tr><td>命中闪光</td><td>技能色爆点打在 Boss 身上：火环、青斩、绿毒潭、金爆。Fever 加放射状速度线</td><td>与数字同时。不要做成另一套游戏的刀光</td></tr>
<tr><td>Buff 飘字</td><td>头像顶：<code>Regen</code> 绿、<code>Vampirism</code>、<code>Debuff Blast</code>、<code>ATK Stack</code> 青条</td><td>技能命中队友/自己后 0.4s 内弹出</td></tr>
</table>

<h3>全屏演出词（按出现顺序）</h3>
<div class="flow">
  <span class="n">THE MASTER OF DESIRE · BOSS</span><span class="arrow">→</span>
  <span class="n">IT'S SHOWTIME!!</span><span class="arrow">→</span>
  <span class="n">READY TO RUMBLE?</span><span class="arrow">→</span>
  <span class="n">DRIVE CRUSH + GOOD BUTTON</span><span class="arrow">→</span>
  <span class="n">PERFECT 150%</span><span class="arrow">→</span>
  <span class="n">FEVER TIME</span><span class="arrow">→</span>
  <span class="n">WARNING !!</span><span class="arrow">→</span>
  <span class="n">COMPLETE / 伤害结算</span>
</div>
<div class="grid g2">
<article class="card">
<h3>IT'S SHOWTIME!!（Slide / 大招切镜）</h3>
<p>红底 + 网点半色调 + 斜切。角色<strong>立绘特写</strong>不是战斗体。右上白描边立体字 <code>IT'S SHOWTIME!!</code> 带星。下方金圆标 <code>SLIDE SKILL</code> + 技能名（Ambush / Full Moon / 만월 / 화신 강림）+ <code>RANK 5 LV 5/10</code>。韩服会在底部再滚一行技能说明。约 1.0～1.4 秒，底栏头像还在，只是被红幕压暗。</p>
</article>
<article class="card">
<h3>Drive：READY → CRUSH → QTE</h3>
<p><code>READY TO RUMBLE?</code> 红斜切，和 SHOWTIME 同语法。接着切到角色/武器大图，斜向金闪电标 <code>DRIVE CRUSH</code>，左下 <code>DRIVE SKILL</code> 徽章 + 技能名（Catastrophe II / True Domination / Lupus Fang）。底中一颗<strong>橙圆 GOOD BUTTON</strong>，星光扫过时点。中了：白闪 + 紫 <code>PERFECT !</code>。录像里 Perfect 固定带 <code>DAMAGE 150%</code>，和视觉图鉴表一致。Great 是绿字，没有 150% 那行。</p>
</article>
<article class="card">
<h3>WARNING !!（敌 Drive）</h3>
<p>整屏血红，白无衬线 <code>WARNING !!</code>，下面透出 <code>ENEMY DRIVE SKILL</code>。我方头像变暗。这和开场 Boss 介绍不是一张皮：开场是橙金海报 <code>THE MASTER OF DESIRE BOSS</code> + 黑色爪影擦过 + 左下红条 <code>WARNING · 名字</code>。</p>
</article>
<article class="card">
<h3>FEVER TIME</h3>
<p>不换 HUD。底栏正中出现粉紫渐变条和 <code>FEVER TIME</code> 字，头像顶彩虹弧。屏幕加白色速度线。数字爆炸：COMBO、总伤、WeakPoint 叠层。窗口结束条消失，环和冷却恢复。wiki 说 7 秒 / 最多 70 hit——录像里看到连段 17～50，没有独立切到另一套界面。</p>
</article>
</div>

<h2 id="seq">12fps 连拍：时间轴</h2>
<p class="caption">每格约 1/12 秒。连拍条是中缝裁切后横拼，点开放大。</p>
__STRIPS__
<table>
<tr><th>片段</th><th>约时长</th><th>帧上看到的事</th></tr>
<tr><td>SHOWTIME（Slide）</td><td>1.0～1.4s</td><td>红幕擦入 → 立绘特写稳住 → 金 SLIDE 标弹出 → 切回战场放技能色爆点</td></tr>
<tr><td>Drive Crush QTE</td><td>1.6～2.2s</td><td>READY 斜切 → 角色大图 + DRIVE CRUSH 斜标 → GOOD BUTTON 亮 → 点按闪光 → PERFECT 150% + TO FEVER% → 回战场</td></tr>
<tr><td>单次打击</td><td>0.2～0.4s</td><td>Boss 身上爆点 → 数字弹出并上漂 → WeakPoint 叠字比单 hit 更大、更慢消失</td></tr>
<tr><td>敌 WARNING</td><td>0.8～1.2s</td><td>红闪切入 → WARNING 稳住 → 敌技能演出 → 红幕淡出，头像恢复</td></tr>
<tr><td>Fever 窗口</td><td>数秒（条在走）</td><td>彩虹条出现 → COMBO 数字每击刷新 → 速度线常驻 → 条空了立刻摘掉字，不淡出很久</td></tr>
<tr><td>Boss 开场</td><td>~2s</td><td>海报体 + 爪影扫过 → 名字条 → 切进圆台 Idle</td></tr>
</table>

<h2 id="ragna">Ragna:Break（国际服手动）</h2>
<p>森林 / 月夜圆台，Boss Equinoctial Bari 几乎坐满台面。队：Mei / Melpomene / Jacheongbi / Abaddon / Shiozuka。顶血 4,500,013。MANUAL，所以能看清 TAP FULL、Slide 预约和 Drive 时机。</p>
<div class="grid g4">
__RAGNA_FIGS__
</div>
<div class="card">
<ul>
<li>Idle：Boss 名字条 <code>40 Equinoctial Bari</code> + 小橙血。我方头像环在转。Debuff Blast / Vampirism 挂在头像顶。</li>
<li>Slide 走 SHOWTIME（Ambush、Full Moon），不是小飘字。</li>
<li>敌 Drive 先 WARNING 再动手，不是无提示砸。</li>
<li>Perfect 后立刻 <code>83% TO FEVER</code>，火环打在 Boss 脚下。</li>
<li>Fever 里 17 COMBO / 383,040 DAMAGE，WeakPoint 44560 叠三层。</li>
<li>结算不是战场内，是暗底列表：BOSS LEVEL 40、每人 Total Damage、金 Close。</li>
</ul>
</div>

<h2 id="raid">韩服 Ragna（标题写成 Raid S12，全自动）</h2>
<p>三栏录像：左立绘 | 中战斗 | 右角色详情。进战前是 10th PARTY、LV40、900 万血、<code>전투시작</code>、<code>연속전투</code>。Boss 开场词 <code>THE MASTER OF DESIRE BOSS · 창기사 루인</code>。FULL AUTO ×3，SHOWTIME 仍然播，Drive QTE 自动打出 Perfect。</p>
<div class="grid g4">
__KR_FIGS__
</div>
<div class="card">
<ul>
<li>和手动国际服比：HUD 一字不差，只是右上写成 FULL AUTO，头像自己亮、自己放。</li>
<li>Slide 韩文技能名（화신 강림、만월）+ 底部署名说明，国际服是英文技能名不带长说明。</li>
<li>打击：绿毒潭、粉花瓣、白斩、大红 Critical 118,147。Fever 20～26 COMBO，总伤能跳到 2,356,299。</li>
<li>结算：金月桂 <code>RAGNA:BREAK COMPLETE</code>，左右 擊 / 滅，累计伤害 = Boss 满血（一发击杀），<code>RANK 1/5</code>。</li>
</ul>
</div>

<h2 id="wb">世界王 100th · Aurora King</h2>
<p>竖屏。大厅 WORLD BOSS TRIAL → 开场爪影海报 → 金色圆竞技场。20 人头像两排。Boss 是体型巨大的 Aurora King（王冠 + 彩虹披风）。后半录像切抽卡，战斗约在 64s～224s。</p>
<div class="grid g4">
__WB_FIGS__
</div>
<div class="card">
<ul>
<li>顶条语法同 Ragna：拱形血 <code>350,368 / 781,525</code>、DAMAGE 累计、×3 SPEED、FULL AUTO。Boss 名 <code>210 Passionate Ruler Aurora King</code>。</li>
<li>Drive 切镜还在（Werewolf · Lupus Fang · DRIVE CRUSH），20 人模式没有把 Drive 改成别的 UI。</li>
<li>Fever 时 20 个小头一起喷数字，COMBO 8 → 50，总伤蓝/金跳。速度线比 5 人战更满屏。</li>
<li>结束：Boss 仍站着，叠 <code>100TH HIT</code> + 名字 + 白字伤害 3,308,051。没有擊滅金环——世界王是刮血排名，不是一刀杀掉。</li>
</ul>
</div>

<h2>给复刻用的硬约束</h2>
<div class="card gold">
<ol>
<li>不要给主线 / 困难 / 星云 / Ragna / WB 做五套战斗 HUD。一套铬，换人数（5 / 20）、换顶条（PHASE vs 百万血）、换背景。</li>
<li>Slide 和 Drive 才有全屏切镜。TAP 只亮头像环和 <code>TAP FULL</code>，不要每次普技都 SHOWTIME。</li>
<li>Drive 判定是底中橙圆钮，不是横向时机条。Perfect 出 150% 和 TO FEVER%。视觉图鉴里那条横扫 QTE 示意要改成圆钮。</li>
<li>Fever 是叠加层：彩虹条 + 速度线 + 数字爆炸。禁止换皮肤、禁止隐藏头像。</li>
<li>WARNING 是红闪全屏字，1 秒级。Boss 介绍是另一张橙金海报 + 爪影。</li>
<li>数字至少三层：单 hit、WeakPoint/Crit、COMBO 总伤。单层白字会一眼假。</li>
<li>困难主线和星云在补到录像前，按主线 HUD + 换入口/背景，不要发明新底栏。</li>
</ol>
<p class="caption">帧文件在 <code>docs/reference/gamekee/_combined/battle_vids/</code>。原片不进发行包。</p>
</div>
</main>
<footer class="caption" style="max-width:1180px;margin:0 auto;padding:0 20px 48px">研究用逐帧。原作立绘、Logo、S CLASS 不进发行资源。</footer>
<div class="lb" id="lb" role="dialog" aria-modal="true"><button class="x" type="button" aria-label="关闭">×</button><img alt=""></div>
<script>
(function(){
  const lb = document.getElementById("lb");
  const img = lb.querySelector("img");
  document.querySelectorAll(".shot img").forEach(el => {
    el.addEventListener("click", () => { img.src = el.src; img.alt = el.alt; lb.classList.add("on"); });
  });
  lb.addEventListener("click", e => { if (e.target === lb || e.target.classList.contains("x")) { lb.classList.remove("on"); img.src=""; } });
  document.addEventListener("keydown", e => { if (e.key === "Escape") { lb.classList.remove("on"); img.src=""; } });
})();
</script>
</body>
</html>
'''


SUMMARY = r'''
<!-- MOTION -->
<h2 id="motion"><span class="en">06b · MOTION</span>战斗动效 · 三支录像逐帧</h2>
<p class="sec-lead">YouTube 三支实战：国际服 Ragna:Break、韩服「Raid S12」（结算是 Ragna:Break）、世界王 100th。完整帧墙和 12fps 连拍在 <a href="战斗动效.html">战斗动效.html</a>。<strong>普通主线只有 wiki 静帧；困难主线和星云这两支战场没有录像。</strong></p>
<div class="grid g4">
  <figure class="shot"><img src="../docs/reference/gamekee/_combined/battle_vids/crops/ragna_hud.jpg" alt="Ragna Idle HUD" loading="lazy"><figcaption>Ragna · 5 圆头 · 月夜圆台</figcaption></figure>
  <figure class="shot"><img src="../docs/reference/gamekee/_combined/battle_vids/crops/ragna_showtime.jpg" alt="IT'S SHOWTIME" loading="lazy"><figcaption>IT'S SHOWTIME!! · Slide 切镜</figcaption></figure>
  <figure class="shot"><img src="../docs/reference/gamekee/_combined/battle_vids/crops/drive_qte.jpg" alt="Drive Crush QTE" loading="lazy"><figcaption>DRIVE CRUSH · 底中 GOOD BUTTON</figcaption></figure>
  <figure class="shot"><img src="../docs/reference/gamekee/_combined/battle_vids/crops/wb_hud.jpg" alt="World Boss 20 portraits" loading="lazy"><figcaption>世界王 · 20 头像两排</figcaption></figure>
</div>
<div class="grid g3" style="margin-top:12px">
  <figure class="shot"><img src="../docs/reference/gamekee/_combined/battle_vids/crops/ragna_warn.jpg" alt="WARNING enemy drive" loading="lazy"><figcaption>WARNING !! 敌 Drive</figcaption></figure>
  <figure class="shot"><img src="../docs/reference/gamekee/_combined/battle_vids/crops/ragna_fever.jpg" alt="FEVER TIME combo" loading="lazy"><figcaption>FEVER TIME · 叠字 + 速度线</figcaption></figure>
  <figure class="shot"><img src="../docs/reference/gamekee/_combined/battle_vids/crops/kr_complete.jpg" alt="Ragna Break Complete" loading="lazy"><figcaption>擊滅 COMPLETE · 金月桂</figcaption></figure>
</div>
<div class="card" style="margin-top:14px">
  <h3>动效硬规则（和 HUD 示意不一致的，以录像为准）</h3>
  <ul>
    <li>Drive 判定是<strong>底中橙圆钮</strong>，不是横扫时机条。Perfect = 150% + TO FEVER%。</li>
    <li>TAP 只亮头像环；Slide / Drive 才全屏 SHOWTIME / READY TO RUMBLE。</li>
    <li>五种模式共用铬：顶拱形血、圆台、圆头像。主线多 PHASE；Ragna 一条百万血；WB 改 20 头。</li>
    <li>Fever 是叠加层，不换界面。WARNING 是 1 秒红闪全屏字。</li>
  </ul>
  <p class="caption">连拍条、模式对照表、数字分层：<a href="战斗动效.html">战斗动效.html</a>。</p>
</div>
<!-- /MOTION -->
'''


def patch_gallery():
    path = USER / "视觉图鉴.html"
    text = path.read_text(encoding="utf-8")
    start = text.find("<!-- MOTION -->")
    end = text.find("<!-- /MOTION -->")
    if start != -1 and end != -1:
        text = text[:start] + text[end + len("<!-- /MOTION -->") :]

    if 'href="#motion"' not in text:
        text = text.replace(
            '      <a href="#battle">战斗</a>\n      <a href="#worlds">场景</a>',
            '      <a href="#battle">战斗</a>\n      <a href="#motion">动效</a>\n      <a href="战斗动效.html">动效页</a>\n      <a href="#worlds">场景</a>',
        )
    text = text.replace(
        '["overview","art","registers","ui","tokens","components","battle","worlds"',
        '["overview","art","registers","ui","tokens","components","battle","motion","worlds"',
    )
    if "<!-- WORLDS -->" in text and "<!-- MOTION -->" not in text:
        text = text.replace("<!-- WORLDS -->", SUMMARY + "\n<!-- WORLDS -->")
    # Drive QTE caption correction in existing HUD mock
    old = "<strong>时机 QTE</strong> 指针扫过金区。判定改伤害和 Fever 增量。"
    new = "<strong>时机 QTE</strong> 录像里是底中橙圆 <code>GOOD BUTTON</code>，不是横条。Perfect = 伤害 150% + Fever 增量。下面横条示意仅表示判定档，复刻用圆钮。"
    if old in text:
        text = text.replace(old, new)
    path.write_text(text, encoding="utf-8")


def patch_combat_nav():
    path = USER / "战斗与数值.html"
    text = path.read_text(encoding="utf-8")
    if "战斗动效.html" not in text:
        text = text.replace(
            '    <a href="视觉图鉴.html">视觉图鉴</a>\n    <a href="汇总表.html">汇总表</a>',
            '    <a href="视觉图鉴.html">视觉图鉴</a>\n    <a href="战斗动效.html">动效</a>\n    <a href="汇总表.html">汇总表</a>',
        )
        path.write_text(text, encoding="utf-8")


def write_html():
    html = HTML
    html = html.replace(
        "__HUD_FIGS__",
        "\n".join(
            [
                fig("ragna_hud.jpg", "Ragna Idle · 5 圆头 · 拱形血"),
                fig("tap_hud.jpg", "头像环 + TAP / Drive 标"),
                fig("wb_hud2.jpg", "世界王 · 20 头两排"),
                fig("kr_hud.jpg", "韩服 Ragna · FULL AUTO"),
            ]
        ),
    )
    html = html.replace(
        "__STRIPS__",
        "\n".join(
            [
                strip("strip_showtime.jpg", "SHOWTIME · Slide 切镜 12fps"),
                strip("strip_drive.jpg", "Drive Crush · GOOD BUTTON 12fps"),
                strip("strip_hit.jpg", "打击数字 / WeakPoint 12fps"),
                strip("strip_warn.jpg", "韩服战场节奏（含 WARNING 前后）"),
                strip("strip_wb.jpg", "世界王 Drive / 打击 12fps"),
            ]
        ),
    )
    html = html.replace(
        "__RAGNA_FIGS__",
        "\n".join(
            [
                fig("ragna_showtime.jpg", "SHOWTIME · Ambush"),
                fig("ragna_ready.jpg", "READY TO RUMBLE?"),
                fig("drive_qte.jpg", "DRIVE CRUSH · GOOD BUTTON"),
                fig("ragna_great.jpg", "GREAT!"),
                fig("ragna_perfect.jpg", "PERFECT!"),
                fig("ragna_qte.jpg", "Perfect 150% · TO FEVER 83%"),
                fig("ragna_warn.jpg", "WARNING !! 敌 Drive"),
                fig("ragna_fever.jpg", "FEVER 17 COMBO"),
                fig("ragna_hud_slide.jpg", "DRIVE SKILL READY 翅膀标"),
                fig("ragna_result.jpg", "结算伤害榜"),
                fig("ragna_showtime_mei.jpg", "SHOWTIME · Full Moon"),
                fig("ragna_hud_drive.jpg", "Debuff Blast 挂头像"),
            ]
        ),
    )
    html = html.replace(
        "__KR_FIGS__",
        "\n".join(
            [
                fig("kr_lobby.jpg", "进战前 · 전투시작"),
                fig("kr_boss.jpg", "THE MASTER OF DESIRE BOSS"),
                fig("kr_showtime.jpg", "SHOWTIME · 만월"),
                fig("kr_showtime2.jpg", "SHOWTIME · 화신 강림"),
                fig("kr_perfect.jpg", "PERFECT 150% · 40% TO FEVER"),
                fig("kr_hud2.jpg", "Idle · 粉翅 Boss"),
                fig("kr_fever.jpg", "FEVER 20 COMBO"),
                fig("kr_fever2.jpg", "FEVER 26 COMBO · 235 万"),
                fig("kr_weak.jpg", "WeakPoint 4,908"),
                fig("kr_crit.jpg", "Critical 118,147"),
                fig("kr_complete.jpg", "RAGNA:BREAK COMPLETE"),
                fig("kr_perfect2.jpg", "Perfect · 80% TO FEVER"),
            ]
        ),
    )
    html = html.replace(
        "__WB_FIGS__",
        "\n".join(
            [
                fig("wb_lobby.jpg", "WORLD BOSS TRIAL 大厅"),
                fig("wb_intro.jpg", "Boss 海报 + 爪影"),
                fig("wb_drive.jpg", "DRIVE CRUSH · Lupus Fang"),
                fig("wb_hud.jpg", "金圆台 + 20 头"),
                fig("wb_fever.jpg", "FEVER 8 COMBO"),
                fig("wb_fever2.jpg", "FEVER 35 COMBO"),
                fig("wb_combo.jpg", "167,409 DAMAGE + 速度线"),
                fig("wb_result.jpg", "100TH HIT 结算"),
            ]
        ),
    )
    out = USER / "战斗动效.html"
    out.write_text(html, encoding="utf-8")
    print("wrote", out, "bytes", out.stat().st_size)


def main():
    print("cropping…")
    names = build_crops()
    print("crops", len(names))
    write_html()
    patch_gallery()
    patch_combat_nav()
    print("patched gallery + combat nav")


if __name__ == "__main__":
    main()
