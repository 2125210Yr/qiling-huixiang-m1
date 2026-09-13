# -*- coding: utf-8 -*-
"""Inject the shared 4-module nav into handbook pages."""
from __future__ import annotations

from pathlib import Path

USER = Path(r"F:\天命之子\天命之子数据")
TOOLS = Path(r"F:\天命之子\tools\wiki-harvest")

CSS_SNIP = """
.mods{display:flex;gap:6px;flex-wrap:wrap;align-items:center}
.mods a{color:#CF9403;text-decoration:none;font-size:13px;line-height:1;padding:7px 14px;border-radius:999px;border:1px solid #3a3018;background:transparent;white-space:nowrap}
.mods a:hover{color:#fff;border-color:#5c4a10;background:#1a1408}
.mods a.on,.mods a[aria-current="page"]{color:#111;background:#FFC400;border-color:#FFC400;font-weight:700}
header .mods{flex:1}
"""

MODS = {
    "handbook": '<nav class="mods" aria-label="设计模块"><a href="设计手册.html">手册</a><a href="视觉图鉴.html">UI</a><a href="战斗与数值.html">数值</a><a href="战斗动效.html">动效</a><a href="汇总表.html">图鉴</a></nav>',
    "ui": '<nav class="mods" aria-label="设计模块"><a href="设计手册.html">手册</a><a href="视觉图鉴.html" class="on" aria-current="page">UI</a><a href="战斗与数值.html">数值</a><a href="战斗动效.html">动效</a><a href="汇总表.html">图鉴</a></nav>',
    "num": '<nav class="mods" aria-label="设计模块"><a href="设计手册.html">手册</a><a href="视觉图鉴.html">UI</a><a href="战斗与数值.html" class="on" aria-current="page">数值</a><a href="战斗动效.html">动效</a><a href="汇总表.html">图鉴</a></nav>',
    "fx": '<nav class="mods" aria-label="设计模块"><a href="设计手册.html">手册</a><a href="视觉图鉴.html">UI</a><a href="战斗与数值.html">数值</a><a href="战斗动效.html" class="on" aria-current="page">动效</a><a href="汇总表.html">图鉴</a></nav>',
    "cat": '<nav class="mods" aria-label="设计模块"><a href="设计手册.html">手册</a><a href="视觉图鉴.html">UI</a><a href="战斗与数值.html">数值</a><a href="战斗动效.html">动效</a><a href="汇总表.html" class="on" aria-current="page">图鉴</a></nav>',
}


def ensure_css(html: str) -> str:
    if ".mods a.on" in html:
        return html
    if "</style>" in html:
        return html.replace("</style>", CSS_SNIP + "\n</style>", 1)
    return html


def patch_gallery():
    p = USER / "视觉图鉴.html"
    t = p.read_text(encoding="utf-8")
    t = ensure_css(t)
    old_nav = """    <nav class="tabs" id="nav">
      <a href="#overview">总览</a>
      <a href="#art">画风</a>
      <a href="#registers">六套画</a>
      <a href="#ui">UI</a>
      <a href="#tokens">色板</a>
      <a href="#components">组件</a>
      <a href="#battle">战斗</a>
      <a href="#motion">动效</a>
      <a href="战斗动效.html">动效页</a>
      <a href="#worlds">场景</a>
      <a href="#growth">养成</a>
      <a href="#data">数据</a>
      <a href="战斗与数值.html">数值</a>
      <a href="#children">天子</a>
      <a href="#cartas">魂卡</a>
      <a href="#puppets">人偶</a>
      <a href="#uifilm">UI廊</a>
      <a href="#dont">禁区</a>
    </nav>"""
    new_nav = f"""    {MODS["ui"]}
    <nav class="tabs" id="nav">
      <a href="#overview">总览</a>
      <a href="#art">画风</a>
      <a href="#registers">六套画</a>
      <a href="#ui">布局</a>
      <a href="#tokens">色板</a>
      <a href="#components">组件</a>
      <a href="#battle">战斗铬</a>
      <a href="#motion">动效摘要</a>
      <a href="#worlds">场景</a>
      <a href="#growth">养成</a>
      <a href="#uifilm">UI廊</a>
      <a href="#dont">禁区</a>
    </nav>"""
    if old_nav in t:
        t = t.replace(old_nav, new_nav)
    elif 'class="on" aria-current="page">UI</a>' not in t:
        t = t.replace(
            '<small>VISUAL BIBLE · 研究用</small>',
            '<small>UI 设计 · 研究用</small>',
        )
        t = t.replace(
            '    <nav class="tabs" id="nav">',
            f"    {MODS['ui']}\n    <nav class=\"tabs\" id=\"nav\">",
        )
    t = t.replace(
        "<small>VISUAL BIBLE · 研究用</small>",
        "<small>UI 设计 · 研究用</small>",
    )
    t = t.replace(
        '<div class="kicker">SHIFT UP · MEMORIAL ARCHIVE</div>',
        '<div class="kicker">01 · UI 模块 · 设计手册</div>',
    )
    old_lede_bit = "这份图鉴把本地"
    if "顶栏换模块" not in t:
        t = t.replace(
            old_lede_bit,
            "这是设计手册的 <a href=\"设计手册.html\">UI 模块</a>。数值 / 动效 / 图鉴走顶栏。这份图鉴把本地",
            1,
        )
    old_child = "这是纪念版<strong>全部 561 名角色</strong>的头像检索墙"
    new_child = "完整检索请用 <a href=\"汇总表.html\">图鉴模块</a>。下面是同一份 561 墙的速览，"
    if old_child in t and "图鉴模块" not in t[t.find("#children") if "#children" in t else 0 : t.find("#children") + 800 if "#children" in t else 0]:
        t = t.replace(old_child, new_child + "这是纪念版<strong>全部 561 名角色</strong>的头像检索墙", 1)
    p.write_text(t, encoding="utf-8")
    print("patched", p.name)


def patch_combat():
    p = USER / "战斗与数值.html"
    t = p.read_text(encoding="utf-8")
    t = ensure_css(t)
    old = """  <div class="brand"><b>DESTINY CHILD</b><div class="caption">战斗公式 · 数值表 · 三站 wiki</div></div>
  <nav>
    <a href="视觉图鉴.html">视觉图鉴</a>
    <a href="战斗动效.html">动效</a>
    <a href="汇总表.html">汇总表</a>
    <a href="#formula">公式</a>
    <a href="#ignition">点火</a>
    <a href="#children">天子数值</a>
    <a href="#wiki5">Wiki 面板</a>
    <a href="#cartas">魂卡</a>
    <a href="#cover">覆盖</a>
    <a href="#puppets">人偶</a>
    <a href="#buffs">Buff</a>
  </nav>"""
    new = f"""  <div class="brand"><b>DESTINY CHILD</b><div class="caption">02 · 数值设计</div></div>
  {MODS["num"]}
  <nav class="page-nav">
    <a href="#cover">覆盖</a>
    <a href="#formula">公式</a>
    <a href="#ignition">点火</a>
    <a href="#children">天子数值</a>
    <a href="#wiki5">Wiki 面板</a>
    <a href="#cartas">魂卡</a>
    <a href="#puppets">人偶</a>
    <a href="#buffs">Buff</a>
  </nav>"""
    if old in t:
        t = t.replace(old, new)
    elif 'aria-current="page">数值</a>' not in t:
        t = t.replace(
            '<div class="caption">战斗公式 · 数值表 · 三站 wiki</div>',
            '<div class="caption">02 · 数值设计</div>',
        )
        t = t.replace("<nav>", f"{MODS['num']}\n  <nav class=\"page-nav\">", 1)
    if "设计手册的" not in t:
        t = t.replace(
            "UI 画风见 <a href=\"视觉图鉴.html\">视觉图鉴.html</a>。",
            "这是设计手册的 <a href=\"设计手册.html\">数值模块</a>。UI 见 <a href=\"视觉图鉴.html\">UI</a>，动效见 <a href=\"战斗动效.html\">动效</a>。",
            1,
        )
    t = t.replace("<h1>战斗与数值</h1>", "<h1>数值设计</h1>")
    p.write_text(t, encoding="utf-8")
    print("patched", p.name)


def patch_motion():
    p = USER / "战斗动效.html"
    t = p.read_text(encoding="utf-8")
    t = ensure_css(t)
    old = """  <div class="brand"><b>DESTINY CHILD</b><div class="caption">战斗动效 · 逐帧规格 · 研究用</div></div>
  <nav>
    <a href="视觉图鉴.html">视觉图鉴</a>
    <a href="战斗与数值.html">数值</a>
    <a href="#cover">覆盖</a>
    <a href="#hud">HUD</a>
    <a href="#modes">模式</a>
    <a href="#vfx">打击</a>
    <a href="#seq">时间轴</a>
    <a href="#ragna">Ragna</a>
    <a href="#raid">Raid</a>
    <a href="#wb">世界王</a>
  </nav>"""
    new = f"""  <div class="brand"><b>DESTINY CHILD</b><div class="caption">03 · 动效设计</div></div>
  {MODS["fx"]}
  <nav class="page-nav">
    <a href="#cover">覆盖</a>
    <a href="#hud">HUD</a>
    <a href="#modes">模式</a>
    <a href="#vfx">打击</a>
    <a href="#seq">时间轴</a>
    <a href="#ragna">Ragna</a>
    <a href="#raid">Raid</a>
    <a href="#wb">世界王</a>
  </nav>"""
    if old in t:
        t = t.replace(old, new)
    elif 'aria-current="page">动效</a>' not in t:
        t = t.replace("<nav>", f"{MODS['fx']}\n  <nav class=\"page-nav\">", 1)
    t = t.replace("<title>天命之子 · 战斗动效</title>", "<title>天命之子 · 动效设计</title>")
    t = t.replace("<h1>战斗动效 · 模式对照</h1>", "<h1>动效设计 · 模式对照</h1>")
    if "数值模块" not in t[:2500]:
        t = t.replace(
            "三支 YouTube 按关键时刻抽了静帧和 12fps 连拍。",
            "这是设计手册的 <a href=\"设计手册.html\">动效模块</a>。UI 铬见 <a href=\"视觉图鉴.html\">UI</a>，公式见 <a href=\"战斗与数值.html\">数值</a>。三支 YouTube 按关键时刻抽了静帧和 12fps 连拍。",
            1,
        )
    p.write_text(t, encoding="utf-8")
    print("patched", p.name)


def patch_catalog():
    p = USER / "汇总表.html"
    t = p.read_text(encoding="utf-8")
    t = ensure_css(t)
    t = t.replace(
        "<title>天命之子图鉴 · 561</title>",
        "<title>天命之子 · 图鉴设计 · 561</title>",
    )
    t = t.replace(
        "天命之子图鉴 <small>",
        "图鉴设计 <small>",
        1,
    )
    old = '<a href="视觉图鉴.html">视觉图鉴</a><a href="战斗与数值.html">战斗数值</a>'
    new = (
        '</nav>'
        + MODS["cat"]
        + '<nav class="rnav" aria-label="跳到稀有度">'
    )
    # keep rarity jumps; replace only the two cross links at start of rnav
    if old in t and 'aria-current="page">图鉴</a>' not in t:
        t = t.replace(old, "", 1)
        t = t.replace(
            '<nav class="rnav" aria-label="跳到稀有度">',
            MODS["cat"] + '\n<nav class="rnav" aria-label="跳到稀有度">',
            1,
        )
    elif 'aria-current="page">图鉴</a>' not in t:
        t = t.replace(
            '<h1>',
            MODS["cat"] + "\n<h1>",
            1,
        )
    p.write_text(t, encoding="utf-8")
    print("patched", p.name)


def patch_generators():
    gal = TOOLS / "gallery_html.py"
    t = gal.read_text(encoding="utf-8")
    if "设计手册.html" not in t:
        t = t.replace(
            '<a href="视觉图鉴.html">视觉图鉴</a><a href="战斗与数值.html">战斗数值</a>',
            "</nav>\\n"
            + MODS["cat"].replace('"', '\\"')
            + '\\n<nav class="rnav" aria-label="跳到稀有度">',
        )
        # safer: just swap the two links for the module bar + rarity nav start
        t = gal.read_text(encoding="utf-8")
        t = t.replace(
            '<a href="视觉图鉴.html">视觉图鉴</a><a href="战斗与数值.html">战斗数值</a>\n<a href="#r5">5★</a>',
            "</nav>\\n"
            + MODS["cat"]
            + "\\n<nav class=\"rnav\" aria-label=\"跳到稀有度\">\\n"
            '<a href="#r5">5★</a>',
        )
        # The template is a normal string - don't over-escape. Use exact template text.
        t = gal.read_text(encoding="utf-8")
        t = t.replace(
            '<a href="视觉图鉴.html">视觉图鉴</a><a href="战斗与数值.html">战斗数值</a>\n<a href="#r5">5★</a>',
            MODS["cat"]
            + "\n"
            + '<a href="#r5">5★</a>',
        )
        t = t.replace(
            "<title>天命之子图鉴 · 561</title>",
            "<title>天命之子 · 图鉴设计 · 561</title>",
        )
        t = t.replace(
            "<h1>天命之子图鉴 <small>",
            "<h1>图鉴设计 <small>",
        )
        if ".mods a.on" not in t:
            t = t.replace(
                ':focus-visible{outline:2px solid var(--gold);outline-offset:2px}',
                ':focus-visible{outline:2px solid var(--gold);outline-offset:2px}\n'
                + CSS_SNIP,
            )
        gal.write_text(t, encoding="utf-8")
        print("patched gallery_html.py")

    combat = TOOLS / "build_combat_html.py"
    ct = combat.read_text(encoding="utf-8")
    if "设计手册.html" not in ct:
        ct = ct.replace(
            """  <div class="brand"><b>DESTINY CHILD</b><div class="caption">战斗公式 · 数值表 · 三站 wiki</div></div>
  <nav>
    <a href="视觉图鉴.html">视觉图鉴</a>
    <a href="汇总表.html">汇总表</a>""",
            f"""  <div class="brand"><b>DESTINY CHILD</b><div class="caption">02 · 数值设计</div></div>
  {MODS["num"]}
  <nav class="page-nav">
    <a href="设计手册.html">手册</a>""",
        )
        if ".mods a.on" not in ct:
            ct = ct.replace(
                ".pill{{display:inline-block;border:1px solid #5a4714;border-radius:999px;padding:2px 10px;color:var(--gold);font-size:12px;margin:2px}}",
                ".pill{{display:inline-block;border:1px solid #5a4714;border-radius:999px;padding:2px 10px;color:var(--gold);font-size:12px;margin:2px}}\n"
                + CSS_SNIP.replace("{", "{{").replace("}", "}}"),
            )
        combat.write_text(ct, encoding="utf-8")
        print("patched build_combat_html.py")

    motion = TOOLS / "build_motion_html.py"
    mt = motion.read_text(encoding="utf-8")
    if "设计手册.html" not in mt:
        mt = mt.replace(
            """  <div class="brand"><b>DESTINY CHILD</b><div class="caption">战斗动效 · 逐帧规格 · 研究用</div></div>
  <nav>
    <a href="视觉图鉴.html">视觉图鉴</a>
    <a href="战斗与数值.html">数值</a>""",
            f"""  <div class="brand"><b>DESTINY CHILD</b><div class="caption">03 · 动效设计</div></div>
  {MODS["fx"]}
  <nav class="page-nav">
    <a href="#cover">覆盖</a>""",
        )
        motion.write_text(mt, encoding="utf-8")
        print("patched build_motion_html.py")


def main():
    patch_gallery()
    patch_combat()
    patch_motion()
    patch_catalog()
    patch_generators()


if __name__ == "__main__":
    main()
