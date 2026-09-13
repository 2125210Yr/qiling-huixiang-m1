# -*- coding: utf-8 -*-
"""Recrop 1-star Memorial library card faces -> icons/F1_*.png.

Catalog-default cell (crop_catalog_icons.py) is 28,778,193,218,152.
On 350_charlib_1star.png that origin sits in the 49/49 gutter and 193/218
skips a row, so faces use the measured grid below. Does not touch R1_*.
"""
from __future__ import annotations

import json
import os

import numpy as np
from PIL import Image, ImageDraw

SHOTS = r"F:\天命之子\docs\reference\mobile-archive\screenshots"
OUT = r"F:\天命之子\天命之子数据\icons"
LAYOUT = r"F:\天命之子\天命之子数据\_layout"
MEMORIAL = r"F:\天命之子\天命之子数据\_extract\1.jsonl"

# Specified catalog cell (documented; not used for 1-star faces).
CATALOG_X0, CATALOG_Y0, CATALOG_DX, CATALOG_DY, CATALOG_SIDE = 28, 778, 193, 218, 152

# Measured face grid on 350_charlib_1star.png (and 360_charlib.png).
X0, Y0, DX, DY, SIDE = 82, 810, 180, 190, 152
COLS = 6
EXPECT = 50
# 350 is the only 1★ grid. 360 is the same 49/49 page (do not merge: JPEG
# delta fools thumb-MSE and yields a duplicate 布丁 as a fake 50th).
PAGES = ["350_charlib_1star.png"]
OTHER_1STAR = ["360_charlib.png", "167_charlib_1star.png", "157_archive_1star.png"]


def empty(im: Image.Image) -> bool:
    a = np.array(im.convert("L"), dtype=np.float32)
    return float(a.mean()) < 22 or float(a.std()) < 12 or float(a.max()) < 80


def thumb(im: Image.Image) -> np.ndarray:
    return np.array(im.resize((24, 24)), dtype=np.float32)


def too_similar(a: np.ndarray, b: np.ndarray) -> bool:
    return float(np.mean((a - b) ** 2)) < 40


def load_memorial() -> list[dict]:
    rows = []
    if os.path.isfile(MEMORIAL):
        with open(MEMORIAL, encoding="utf-8") as f:
            for line in f:
                line = line.strip()
                if line:
                    rows.append(json.loads(line))
    return rows


def crop_page(fn: str, seen: list[np.ndarray]) -> list[dict]:
    path = os.path.join(SHOTS, fn)
    if not os.path.isfile(path):
        print("missing", fn, flush=True)
        return []
    im = Image.open(path).convert("RGB")
    w, h = im.size
    out = []
    n_skip_partial = n_skip_empty = n_skip_dup = 0
    r = 0
    while True:
        y = Y0 + r * DY
        if y >= h:
            break
        row_hit = False
        for c in range(COLS):
            x = X0 + c * DX
            if y + SIDE > h - 8:
                n_skip_partial += 1
                continue
            if x + SIDE > w + 2 or x < 0:
                continue
            tile = im.crop((x, y, x + SIDE, y + SIDE))
            if empty(tile):
                n_skip_empty += 1
                continue
            th = thumb(tile)
            if any(too_similar(th, old) for old in seen):
                n_skip_dup += 1
                continue
            seen.append(th)
            out.append({
                "tile": tile,
                "source": fn,
                "row": r,
                "col": c,
                "x": x,
                "y": y,
                "w": SIDE,
                "h": SIDE,
            })
            row_hit = True
        if not row_hit and y + SIDE > h - 8:
            break
        r += 1
        if r > 16:
            break
    print(
        "page", fn, "new", len(out),
        "skip_empty", n_skip_empty, "skip_partial", n_skip_partial,
        "skip_dup", n_skip_dup, "size", w, h, flush=True,
    )
    return out


def contact_sheet(tiles: list[Image.Image], cols: int = 6) -> Image.Image:
    gap = 4
    n = len(tiles)
    rows = max(1, (n + cols - 1) // cols)
    sheet = Image.new("RGB", (cols * (SIDE + gap) + gap, rows * (SIDE + gap) + gap), (20, 20, 20))
    dr = ImageDraw.Draw(sheet)
    for i, t in enumerate(tiles):
        rr, cc = divmod(i, cols)
        x, y = gap + cc * (SIDE + gap), gap + rr * (SIDE + gap)
        sheet.paste(t, (x, y))
        dr.rectangle((x, y, x + SIDE - 1, y + SIDE - 1), outline=(40, 40, 40))
    return sheet


def write_notes(n: int, records: list[dict]) -> None:
    last_name = records[-1].get("memorial_name") if records else ""
    lines = [
        "# 05 1★ catalog face recrop",
        "",
        "Do **not** overwrite `icons/R1_*`. Do not edit `merge_561.py` / `汇总表.html`.",
        "",
        "## Count",
        "",
        "| 项 | 数 |",
        "|---|---:|",
        f"| 本脚本 `F1_000`…`F1_{n-1:03d}` | **{n}** |",
        "| 纪念版 swipe `350_1s_stats_000…049` | 50 |",
        "| 图鉴 UI `350_charlib_1star.png` | **49/49** |",
        "| 旧 `icons/R1_*`（catalog 几何，含空底） | 52 |",
        "",
        f"Clean unique faces: **{n}** vs memorial target **{EXPECT}**.",
        "",
        "Memorial idx 48 = 小蓝水滴（有立绘）。idx 49 再滑一次仍是小蓝水滴，",
        "`350_1s_stats_049.png` / `359_1s_end.png` 没有立绘。图鉴格子也只有一张蓝水滴。",
        "没有第 50 张独立卡面可裁。F1 下标 0–48 对齐纪念版 0–48。",
        "",
        "## Geometry",
        "",
        "`crop_catalog_icons.py` 默认 `X0,Y0,DX,DY,SIDE = 28,778,193,218,152`。",
        "1★ 顶页（未滚动）里：",
        "",
        "- Y=778 落在 49/49 黄虚线下面的黑缝，卡顶约 Y=810",
        "- X=28 在滚动条右侧空底，卡左约 X=82",
        "- 间距实测 DX=180、DY=190（218 会跳行，193 会往右漂）",
        "- SIDE=152 仍用默认边长，刚好套住框 + 右上 E 角",
        "",
        "本脚本使用：",
        "",
        "```",
        f"X0,Y0,DX,DY,SIDE = {X0},{Y0},{DX},{DY},{SIDE}",
        "COLS = 6",
        "skip if y+SIDE > h-8   # 半截底行",
        "skip empty             # mean<22 or std<12 or max<80",
        "scan order             # row-major on 350_charlib_1star.png",
        "```",
        "",
        "末行蓝水滴 y=2330，y+SIDE=2482 < h-8，整张留下。",
        "",
        "## Other 1★ screenshots",
        "",
        "| 文件 | 实际内容 |",
        "|---|---|",
        "| `350_charlib_1star.png` | 1★ 顶页 49/49，唯一裁源 |",
        "| `360_charlib.png` | 同一 49/49 再拍，不入库（JPEG 差会误留重复） |",
        "| `167_charlib_1star.png` | **2★** 53/53（文件名写错） |",
        "| `157_archive_1star.png` | 退出游戏 NOTICE，无格子 |",
        "| 无其它 `350_charlib*` 翻页 | 1★ 一页装完 |",
        "",
        "## Order",
        "",
        "F1 idx = 纪念版 1★ 滑动顺序（与格子扫描一致）：",
        "星影灵牛…战士…暗影…注射器…芙咕利…蛹…福多…牙精…驴子…明胶…水滴…蓝水滴。",
        f"最后一张 F1_{n-1:03d} = {last_name or '小蓝水滴'}。",
        "",
        "输出：",
        "",
        "- `icons/F1_000.png` …",
        "- `_layout/05_recrop_1star.jsonl`",
        "- `_layout/05_sheet.png`",
        "",
        "`tools/wiki-harvest/recrop_faces.py` 用 28,778,193,204,128 会盖掉这些 F1；1★ 以本脚本为准。",
        "",
    ]
    path = os.path.join(LAYOUT, "05_notes.md")
    with open(path, "w", encoding="utf-8") as f:
        f.write("\n".join(lines))
    print("notes", path, flush=True)


def main() -> None:
    os.makedirs(OUT, exist_ok=True)
    os.makedirs(LAYOUT, exist_ok=True)
    for fn in OTHER_1STAR:
        p = os.path.join(SHOTS, fn)
        if not os.path.isfile(p):
            print("other missing", fn, flush=True)
            continue
        im = Image.open(p)
        print("other", fn, im.size, flush=True)

    memorial = load_memorial()
    seen: list[np.ndarray] = []
    cards: list[dict] = []
    for fn in PAGES:
        cards.extend(crop_page(fn, seen))

    records = []
    tiles = []
    for i, c in enumerate(cards):
        fname = "F1_%03d.png" % i
        dest = os.path.join(OUT, fname)
        c["tile"].save(dest)
        tiles.append(c["tile"])
        mem = memorial[i] if i < len(memorial) else {}
        rec = {
            "rarity_key": "1",
            "idx": i,
            "file": "icons/" + fname,
            "source": c["source"],
            "x": c["x"],
            "y": c["y"],
            "row": c["row"],
            "col": c["col"],
            "w": c["w"],
            "h": c["h"],
            "geom": [X0, Y0, DX, DY, SIDE],
            "memorial_id": mem.get("id", ""),
            "memorial_name": mem.get("name", ""),
        }
        records.append(rec)
        print(
            "F1_%03d r%dc%d (%d,%d) %s %s"
            % (i, c["row"], c["col"], c["x"], c["y"], rec["memorial_name"], c["source"]),
            flush=True,
        )

    extra = [n for n in os.listdir(OUT) if n.startswith("F1_") and n.endswith(".png")]
    keep = {"F1_%03d.png" % i for i in range(len(records))}
    for n in extra:
        if n not in keep:
            os.remove(os.path.join(OUT, n))
            print("removed leftover", n, flush=True)

    meta = os.path.join(LAYOUT, "05_recrop_1star.jsonl")
    with open(meta, "w", encoding="utf-8") as f:
        for rec in records:
            f.write(json.dumps(rec, ensure_ascii=False) + "\n")

    sheet = contact_sheet(tiles)
    sheet_path = os.path.join(LAYOUT, "05_sheet.png")
    sheet.save(sheet_path)

    write_notes(len(records), records)
    print("jsonl", meta, flush=True)
    print("sheet", sheet_path, flush=True)
    print(
        "COUNT %d unique F1 vs memorial %d (library UI 49/49)"
        % (len(records), EXPECT),
        flush=True,
    )
    if len(records) < EXPECT:
        print(
            "SHORT %d — no 50th unique face on 1-star grids "
            "(memorial idx 49 duplicates 小蓝水滴)"
            % (EXPECT - len(records)),
            flush=True,
        )


if __name__ == "__main__":
    main()
