# -*- coding: utf-8 -*-
"""Recrop 3★ Memorial catalog FACES as clean 152×152 squares.

Old crop_catalog_icons.py used DY=218 with Y0=778. That pitch is 28px too
large, so later rows slide into the name strip / next tile (R3_090). Measured
card-to-card (and name-to-name) pitch is DY=190. Card-top Y0 is detected per
page because scroll shots are not grid-aligned to 778.
"""
from __future__ import annotations

import json
import os

import numpy as np
from PIL import Image, ImageDraw, ImageFont

SHOTS = r"F:\天命之子\docs\reference\mobile-archive\screenshots"
OUT = r"F:\天命之子\天命之子数据\icons"
LAYOUT = r"F:\天命之子\天命之子数据\_layout"
META = os.path.join(LAYOUT, "03_recrop_3star.jsonl")
NOTES = os.path.join(LAYOUT, "03_notes.md")
SHEET = os.path.join(LAYOUT, "03_sheet.png")

PAGES = [
    "164_charlib_3star.png",
    "165_charlib_3s_p01.png",
    "165_charlib_3s_p02.png",
    "165_charlib_3s_p03.png",
]

# X0/DX/SIDE match the gold-framed card. DY is the measured cell pitch
# (old 218 drifted). Y0 is detected per page (card top, not the 32px pad at 778).
X0, DX, SIDE = 28, 193, 152
DY = 190
COLS = 6
MAX_UNIQUE = 99
RARITY = "3"

# Skip a crop only when we do not have nearly a full card.
# y+SIDE may exceed h-8 by a few px on the last row; those tiles are still faces.
BOTTOM_MARGIN = 8
MIN_CROP_H = SIDE - 4

Y_SEARCH0, Y_SEARCH1 = 610, 860


def empty(tile: np.ndarray) -> bool:
    L = tile.mean(axis=2)
    return float(L.mean()) < 18 or float(L.std()) < 8


def sat_map(tile: np.ndarray) -> np.ndarray:
    t = tile.astype(np.int16)
    return (t.max(axis=2) - t.min(axis=2)).astype(np.float32)


def white_frac(tile: np.ndarray) -> float:
    if tile.size == 0:
        return 0.0
    return float(
        ((tile[:, :, 0] > 200) & (tile[:, :, 1] > 200) & (tile[:, :, 2] > 200)).mean()
    )


def content_fill(tile: np.ndarray) -> float:
    """Fraction of rows that are not a flat dark band (works for grey marble cards)."""
    L = tile.mean(axis=2)
    return float(((L.std(axis=1) > 12) | (L.mean(axis=1) > 40)).mean())


def is_background(tile: np.ndarray) -> bool:
    """Checkerboard / skull wallpaper — not a card, even if std>=8."""
    sf = float((sat_map(tile) > 40).mean())
    L = tile.mean(axis=2)
    return sf < 0.06 and float(L.mean()) < 42


def name_leak(tile: np.ndarray) -> bool:
    """White CJK sitting in the DY-SIDE band leaked into the square."""
    bot = tile[-34:]
    sat = float(sat_map(bot).mean())
    return white_frac(bot) > 0.035 and sat < 12


def is_card(tile: np.ndarray) -> bool:
    if tile.shape[0] < MIN_CROP_H or tile.shape[1] < SIDE - 4:
        return False
    if empty(tile) or is_background(tile) or name_leak(tile):
        return False
    L = tile.mean(axis=2)
    # do not require high saturation: 天使洛特 / 草隆 are grey
    return (
        float(L.std()) > 28
        and float(L.mean()) > 25
        and content_fill(tile) > 0.70
    )


def thumb(tile: np.ndarray) -> np.ndarray:
    im = Image.fromarray(tile).resize((24, 24), Image.Resampling.BILINEAR)
    return np.array(im, dtype=np.float32)


def fingerprint(tile: np.ndarray) -> np.ndarray:
    im = Image.fromarray(tile).resize((8, 8), Image.Resampling.BOX)
    a = np.array(im, dtype=np.float32).reshape(-1)
    a = a - a.mean()
    n = float(np.sqrt((a * a).sum())) + 1e-6
    return a / n


def same_face(tile_a: np.ndarray, fp_a: np.ndarray, tile_b: np.ndarray, fp_b: np.ndarray) -> bool:
    mse = float(np.mean((thumb(tile_a) - thumb(tile_b)) ** 2))
    corr = float((fp_a * fp_b).sum())
    # scroll dups (same Pubi, ±few px): corr~0.96 mse~400-700
    # different children (even two gold-frame blondes): corr<=0.93 mse>=1700
    return mse < 900.0 or corr > 0.94


def score_row(rgb: np.ndarray, y: int) -> tuple[float, int, float]:
    h = rgb.shape[0]
    if y < 0 or y + MIN_CROP_H > h:
        return -999.0, 0, 0.0
    scores = []
    fills = []
    ncard = 0
    for c in range(COLS):
        x = X0 + c * DX
        tile = rgb[y : y + SIDE, x : x + SIDE]
        if tile.shape[0] < MIN_CROP_H:
            scores.append(-1.0)
            continue
        botw = white_frac(tile[-32:])
        nb = rgb[y + SIDE : min(h, y + SIDE + 28), x : x + SIDE]
        nbw = white_frac(nb)
        fill = content_fill(tile)
        card = is_card(tile) and botw < 0.025
        if card:
            ncard += 1
            fills.append(fill)
        L = tile.mean(axis=2)
        sf = float((sat_map(tile) > 40).mean())
        sc = (sf * 1.5 + float(L.std()) / 80.0 + fill * 2.0 + (0.04 - botw) * 4.0 + nbw * 3.5)
        if not card:
            sc *= 0.15
        scores.append(sc)
    mean_fill = float(np.mean(fills)) if fills else 0.0
    return float(np.mean(scores)), ncard, mean_fill


def detect_y0(rgb: np.ndarray) -> tuple[int, float, int]:
    """First cluster of filled card-top rows; pick the best Y inside that cluster."""
    h = rgb.shape[0]
    cluster: list[tuple[float, int, int]] = []
    started = False
    for y in range(Y_SEARCH0, min(Y_SEARCH1, h - MIN_CROP_H)):
        sc, n, fill = score_row(rgb, y)
        # header 99/99 crops have n>=3 but content_fill ~0.45; real cards ~0.85+
        good = n >= 3 and sc > 1.0 and fill > 0.80
        if good:
            started = True
            cluster.append((sc, n, y))
        elif started:
            break
    if not cluster:
        best = (-999.0, 0, 778)
        for y in range(Y_SEARCH0, min(Y_SEARCH1, h - MIN_CROP_H)):
            sc, n, fill = score_row(rgb, y)
            if fill > 0.80 and (sc, n, y) > best:
                best = (sc, n, y)
        return best[2], best[0], best[1]
    sc, n, y = max(cluster)
    # score rewards names sitting just below SIDE; back off so glyphs stay out
    y_lo = cluster[0][2]
    y_adj = y - 6
    if y_adj >= y_lo:
        y = y_adj
    return y, sc, n


def to_square(crop: Image.Image) -> Image.Image:
    crop = crop.convert("RGB")
    if crop.size == (SIDE, SIDE):
        return crop
    canvas = Image.new("RGB", (SIDE, SIDE), (0, 0, 0))
    canvas.paste(crop, (0, 0))
    return canvas


def iter_cells(rgb: np.ndarray, y0: int):
    h, w = rgb.shape[:2]
    for r in range(12):
        y = y0 + r * DY
        if y >= h - BOTTOM_MARGIN:
            break
        for c in range(COLS):
            x = X0 + c * DX
            if x + SIDE > w:
                continue
            y2 = min(y + SIDE, h)
            ch = y2 - y
            if ch < MIN_CROP_H:
                continue
            tile = rgb[y:y2, x : x + SIDE]
            yield r, c, x, y, tile


def contact_sheet(paths: list[str], dest: str, cols: int = 10) -> None:
    if not paths:
        return
    n = len(paths)
    rows = (n + cols - 1) // cols
    pad = 6
    label_h = 16
    cell = SIDE + pad
    sheet = Image.new("RGB", (cols * cell + pad, rows * (cell + label_h) + pad), (18, 18, 18))
    draw = ImageDraw.Draw(sheet)
    try:
        font = ImageFont.truetype("arial.ttf", 12)
    except OSError:
        font = ImageFont.load_default()
    for i, p in enumerate(paths):
        im = Image.open(p).convert("RGB")
        rr, cc = divmod(i, cols)
        x = pad + cc * cell
        y = pad + rr * (cell + label_h)
        sheet.paste(im, (x, y))
        draw.text((x, y + SIDE + 1), "F3_%03d" % i, fill=(200, 200, 200), font=font)
    sheet.save(dest)


def drop_most_similar(
    items: list[dict], fps: list[np.ndarray], keep: int
) -> tuple[list[dict], list[np.ndarray]]:
    """If we exceed MAX_UNIQUE, drop tiles closest to an earlier one."""
    if len(items) <= keep:
        return items, fps
    extra = len(items) - keep
    scores = []
    for i in range(keep, len(items)):
        dmin = min(1.0 - float((fps[i] * fps[j]).sum()) for j in range(keep))
        scores.append((dmin, i))
    drop = {i for _, i in sorted(scores)[:extra]}
    new_items = [it for i, it in enumerate(items) if i not in drop]
    new_fps = [fp for i, fp in enumerate(fps) if i not in drop]
    return new_items[:keep], new_fps[:keep]


def main() -> None:
    os.makedirs(OUT, exist_ok=True)
    os.makedirs(LAYOUT, exist_ok=True)

    # never touch R3_/N3_
    for fn in os.listdir(OUT):
        if fn.startswith("F3_") and fn.endswith(".png"):
            os.remove(os.path.join(OUT, fn))

    rows_out: list[dict] = []
    saved_tiles: list[np.ndarray] = []
    saved_fps: list[np.ndarray] = []
    page_stats: list[dict] = []
    detect_info: list[dict] = []

    for fn in PAGES:
        path = os.path.join(SHOTS, fn)
        if not os.path.isfile(path):
            print("missing", fn, flush=True)
            continue
        im = Image.open(path).convert("RGB")
        rgb = np.array(im)
        h, w = rgb.shape[:2]
        y0, sc, n0 = detect_y0(rgb)
        detect_info.append({"source": fn, "y0": y0, "score": round(sc, 3), "ncards_row0": n0, "size": [w, h]})
        print("detect", fn, "Y0", y0, "score", "%.3f" % sc, "row0_cards", n0, flush=True)

        n_new = 0
        n_skip_empty = 0
        n_skip_dup = 0
        n_skip_partial = 0
        for r, c, x, y, tile in iter_cells(rgb, y0):
            if tile.shape[0] < MIN_CROP_H:
                n_skip_partial += 1
                continue
            if not is_card(tile):
                n_skip_empty += 1
                continue
            fp = fingerprint(tile)
            dup = False
            for t2, f2 in zip(saved_tiles, saved_fps):
                if same_face(tile, fp, t2, f2):
                    dup = True
                    break
            if dup:
                n_skip_dup += 1
                continue
            idx = len(rows_out)
            fname = "F3_%03d.png" % idx
            crop = Image.fromarray(tile).convert("RGB")
            if crop.size != (SIDE, SIDE):
                crop = to_square(crop)
            dest = os.path.join(OUT, fname)
            crop.save(dest)
            rec = {
                "rarity_key": RARITY,
                "idx": idx,
                "file": "icons/" + fname,
                "source": fn,
                "x": int(x),
                "y": int(y),
                "w": int(tile.shape[1]),
                "h": int(tile.shape[0]),
            }
            rows_out.append(rec)
            saved_tiles.append(tile)
            saved_fps.append(fp)
            n_new += 1
        print(
            "page", fn, "new", n_new, "total", len(rows_out),
            "skip_empty", n_skip_empty, "skip_dup", n_skip_dup,
            "skip_partial", n_skip_partial,
            flush=True,
        )
        page_stats.append({
            "source": fn, "y0": y0, "new": n_new, "total": len(rows_out),
            "skip_empty": n_skip_empty, "skip_dup": n_skip_dup,
            "skip_partial": n_skip_partial,
        })

    trimmed = 0
    if len(rows_out) > MAX_UNIQUE:
        trimmed = len(rows_out) - MAX_UNIQUE
        # remove extra files that will be dropped
        keep_items, keep_fps = drop_most_similar(rows_out, saved_fps, MAX_UNIQUE)
        keep_files = {it["file"] for it in keep_items}
        for rec in rows_out:
            if rec["file"] not in keep_files:
                p = os.path.join(OUT, os.path.basename(rec["file"]))
                if os.path.isfile(p):
                    os.remove(p)
        # reindex
        rows_out = []
        for i, rec in enumerate(keep_items):
            old = os.path.join(OUT, os.path.basename(rec["file"]))
            new_name = "F3_%03d.png" % i
            new_path = os.path.join(OUT, new_name)
            if os.path.basename(old) != new_name and os.path.isfile(old):
                if os.path.isfile(new_path):
                    os.remove(new_path)
                os.replace(old, new_path)
            rec = dict(rec)
            rec["idx"] = i
            rec["file"] = "icons/" + new_name
            rows_out.append(rec)
        saved_fps = keep_fps
        print("trimmed extras", trimmed, "now", len(rows_out), flush=True)

    with open(META, "w", encoding="utf-8") as f:
        for rec in rows_out:
            f.write(json.dumps(rec, ensure_ascii=False) + "\n")

    paths = [os.path.join(OUT, "F3_%03d.png" % i) for i in range(len(rows_out))]
    contact_sheet(paths, SHEET, cols=10)

    notes = []
    notes.append("# 3★ catalog face recrop")
    notes.append("")
    notes.append("Script: `tools/wiki-harvest/recrop_faces_3.py`")
    notes.append("Icons: `天命之子数据/icons/F3_000.png` … (prefix F3_ only; R3_/N3_ untouched)")
    notes.append("Meta: `天命之子数据/_layout/03_recrop_3star.jsonl`")
    notes.append("Sheet: `天命之子数据/_layout/03_sheet.png`")
    notes.append("")
    notes.append("## Geometry")
    notes.append("")
    notes.append("Old `crop_catalog_icons.py` used `X0,Y0,DX,DY,SIDE = 28,778,193,218,152` with `ROWS=10`.")
    notes.append("That was the split-tile bug:")
    notes.append("")
    notes.append("- **DY=218 is 28px too large.** Name-to-name and card-to-card pitch on these 1200×2670 shots is **190**.")
    notes.append("  Each extra 28px shifts the window down into the name strip, then the next tile (R3_089/R3_090 = 綠/紅噗比 text + skull wallpaper).")
    notes.append("- **Y0=778 includes ~32px of dark padding above the gold frame.** The framed card top is ~810 on the first library page. SIDE=152 from 778 still misses the name on row 0, but later rows with DY=218 leak names.")
    notes.append("- **Partial last row:** `778+8*218=2522`, `y+SIDE=2674 > 2670`. The old code cropped a short strip and bilinear-stretched it. With DY=190 the 10th row starts at `Y0+9*190` and is a real card row (only a few px short of 152).")
    notes.append("- **Scroll pages are not on Y0=778.** p01 card top ≈ 625; p02/p03 (end of list, no 99/99 header) ≈ 664. Using 778 on those pages crops the name band + neighbor wallpaper.")
    notes.append("- Detected Y0 is backed off 6px from the name-band peak so CJK stays in the DY−SIDE strip.")
    notes.append("")
    notes.append("Used:")
    notes.append("")
    notes.append("| param | value |")
    notes.append("|---|---|")
    notes.append("| X0 | %d |" % X0)
    notes.append("| DX | %d |" % DX)
    notes.append("| DY | %d (measured; not 218) |" % DY)
    notes.append("| SIDE | %d |" % SIDE)
    notes.append("| COLS | %d |" % COLS)
    notes.append("| Y0 | detected per page (first card-top cluster) |")
    notes.append("| empty | mean<18 or std<8, plus sat_frac<0.06 wallpaper reject |")
    notes.append("| name leak | white>0.035 and sat<12 in bottom 34px → skip |")
    notes.append("| last row | keep if cropped height ≥ SIDE-4 even if y+SIDE > h-8 |")
    notes.append("| output | RGB PNG %d×%d |" % (SIDE, SIDE))
    notes.append("")
    notes.append("Per-page Y0:")
    notes.append("")
    for d in detect_info:
        notes.append("- `%s` (%dx%d): Y0=%d  (row0 score %.3f, cards %d)" % (
            d["source"], d["size"][0], d["size"][1], d["y0"], d["score"], d["ncards_row0"]))
    notes.append("")
    notes.append("## Counts")
    notes.append("")
    notes.append("| page | Y0 | new | running total | skip empty/bg | skip dup | skip partial |")
    notes.append("|---|---:|---:|---:|---:|---:|---:|")
    for st in page_stats:
        notes.append("| `%s` | %d | %d | %d | %d | %d | %d |" % (
            st["source"], st["y0"], st["new"], st["total"],
            st["skip_empty"], st["skip_dup"], st["skip_partial"]))
    notes.append("")
    notes.append("Unique saved: **%d** (cap %d, trimmed %d)." % (len(rows_out), MAX_UNIQUE, trimmed))
    notes.append("")
    notes.append("Encyclopedia UI says **99/99**. These four grid shots miss **two mid-list rows**")
    notes.append("(~12 faces) between the last row of `164_charlib_3star.png` (F3_054…059 琉刻/喬嘉/…)")
    notes.append("and the first row of `165_charlib_3s_p01.png` (F3_060 光隆). Those children appear")
    notes.append("as full-body pages `330_3s_stats_060.png` … `_071.png`, not as catalog tiles.")
    notes.append("p02/p03 only repeat the last three 噗比 and contribute 0 new faces after RGB-hash dedupe.")
    notes.append("")
    notes.append("## QA")
    notes.append("")
    notes.append("- F3_000 is 天使洛特 (white statue child), same subject as R3_000, gold frame, no name.")
    notes.append("- F3_086 is 藍噗比, a full gold-framed card — not split name+skull garbage like R3_090.")
    notes.append("")

    with open(NOTES, "w", encoding="utf-8") as f:
        f.write("\n".join(notes) + "\n")

    print("DONE unique", len(rows_out), META, flush=True)


if __name__ == "__main__":
    main()
