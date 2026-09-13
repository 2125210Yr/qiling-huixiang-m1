# -*- coding: utf-8 -*-
"""OCR catalog name strips (band below gold cards), 3/2/1-star.

Writes only under 天命之子数据/_layout/. Does not touch named_icons.jsonl.
"""
from __future__ import annotations

import json
import os
import re
import time

import numpy as np
from PIL import Image, ImageOps
from rapidocr_onnxruntime import RapidOCR
from zhconv import convert as zh_convert

SHOTS = r"F:\天命之子\docs\reference\mobile-archive\screenshots"
DATA = r"F:\天命之子\天命之子数据"
LAYOUT = os.path.join(DATA, "_layout")
OUT_ALL = os.path.join(LAYOUT, "06_named_3star.jsonl")
OUT_2 = os.path.join(LAYOUT, "06_named_2star.jsonl")
OUT_1 = os.path.join(LAYOUT, "06_named_1star.jsonl")
EXTRACT = os.path.join(DATA, "_extract")
ICON_DIR = os.path.join(DATA, "icons")

PAGES = {
    "3": [
        "164_charlib_3star.png",
        "165_charlib_3s_p01.png",
        "165_charlib_3s_p02.png",
        "165_charlib_3s_p03.png",
    ],
    "2": ["166_charlib_2star.png"],
    "1": ["350_charlib_1star.png"],
}

X0, Y0, DX, DY, SIDE = 28, 778, 193, 218, 152
ROWS, COLS = 10, 6
EXPECT = {"3": 99, "2": 53, "1": 50}

CJK_SPACE = re.compile(r"(?<=[\u4e00-\u9fff])\s+(?=[\u4e00-\u9fff])")
CJK_RE = re.compile(r"[\u4e00-\u9fff]")
KEEP_RE = re.compile(r"[^\u4e00-\u9fffA-Za-z]")
SKIP = ("LIBRARY", "CHARACTER", "Child", "变更", "變更", "561", "魂之歌牌", "人偶")
RANK = frozenset("SUPEC")

ENGINE = None
ENGINE_DET = None


def engine() -> RapidOCR:
    global ENGINE
    if ENGINE is None:
        print("init RapidOCR rec...", flush=True)
        ocr = RapidOCR()
        ocr.use_angle_cls = False
        ocr.use_text_det = False
        ocr.min_height = 10_000
        ocr.width_height_ratio = 1.0
        ocr.text_score = 0.28
        ENGINE = ocr
        print("RapidOCR rec ready", flush=True)
    return ENGINE


def engine_det() -> RapidOCR:
    global ENGINE_DET
    if ENGINE_DET is None:
        print("init RapidOCR det...", flush=True)
        ENGINE_DET = RapidOCR()
        ENGINE_DET.use_angle_cls = False
        ENGINE_DET.text_score = 0.35
        print("RapidOCR det ready", flush=True)
    return ENGINE_DET


def simp(s: str) -> str:
    s = CJK_SPACE.sub("", s or "")
    s = zh_convert(re.sub(r"\s+", "", s), "zh-cn")
    return KEEP_RE.sub("", s)


def empty_tile(im: Image.Image) -> bool:
    if im.size[0] < 80 or im.size[1] < 80:
        return True
    a = np.array(im.convert("RGB"), dtype=np.float32)
    mean = float(a.mean())
    std = float(a.std())
    sat = float((a.max(axis=2) - a.min(axis=2)).mean())
    if mean < 18 or std < 8:
        return True
    if sat < 12 and std < 18:
        return True
    ih, iw = a.shape[0], a.shape[1]
    if ih > 40 and iw > 40:
        interior = a[12:-12, 12:-12]
        if float(interior.std()) < 14:
            return True
    return False


def thumb(im: Image.Image) -> np.ndarray:
    return np.array(im.resize((24, 24), Image.Resampling.BILINEAR), dtype=np.float32)


def too_similar(a: np.ndarray, b: np.ndarray) -> bool:
    return float(np.mean((a - b) ** 2)) < 22


def color_sig(im: Image.Image) -> np.ndarray:
    a = np.array(im.convert("RGB"), dtype=np.float32)
    lum = a.mean(axis=2)
    mask = lum > 28
    if int(mask.sum()) < 30:
        return np.array([0.0, 0.0, 0.0, 0.0], dtype=np.float32)
    pix = a[mask]
    return np.array(
        [float(pix[:, 0].mean()), float(pix[:, 1].mean()),
         float(pix[:, 2].mean()), float(pix.std())],
        dtype=np.float32,
    )


def is_dup(th: np.ndarray, sig: np.ndarray, prev: list[tuple[np.ndarray, np.ndarray]]) -> bool:
    for old_th, old_sig in prev:
        mse = float(np.mean((th - old_th) ** 2))
        if mse < 22:
            return True
        cdist = float(np.linalg.norm(sig[:3] - old_sig[:3]))
        if mse < 280 and cdist < 32:
            return True
    return False


def white_frac(im: Image.Image, thr: int = 170) -> float:
    g = np.array(im.convert("L"))
    if g.size == 0:
        return 0.0
    return float((g >= thr).mean())


def has_star_header(im: Image.Image) -> bool:
    """99/53/49 count badge sits top-right on un-scrolled library pages."""
    w, h = im.size
    patch = np.array(im.crop((min(1020, w - 20), 630, min(1185, w - 2), min(730, h))))
    if patch.size == 0:
        return False
    r = patch[:, :, 0].astype(np.int16)
    g = patch[:, :, 1].astype(np.int16)
    b = patch[:, :, 2].astype(np.int16)
    yellow = ((r > 190) & (g > 140) & (b < 90)).mean()
    return float(yellow) > 0.10


def top_frame_sat(tile: Image.Image) -> float:
    a = np.array(tile.convert("RGB"), dtype=np.float32)
    if a.shape[0] < 10:
        return 0.0
    top = a[:6]
    return float((top.max(axis=2) - top.min(axis=2)).mean())


def find_y0(im: Image.Image) -> int:
    """Use 778 on header pages; align to gold-frame top on scrolled pages."""
    if has_star_header(im):
        return Y0
    w, h = im.size
    best_y, best_s = 620, -1.0
    for y in range(608, 658, 2):
        if y + SIDE > h - 8:
            break
        sats = []
        for c in range(COLS):
            x = X0 + c * DX
            if x + SIDE > w:
                continue
            tile = im.crop((x, y, x + SIDE, y + SIDE))
            if empty_tile(tile):
                continue
            sats.append(top_frame_sat(tile))
        if len(sats) < 3:
            continue
        score = float(np.mean(sats)) + 0.5 * len(sats)
        if score > best_s:
            best_s, best_y = score, y
    return best_y


def text_crop(strip: Image.Image) -> Image.Image | None:
    """Keep the white glyph band; drop gold-card rim and next-card art."""
    g = np.array(strip.convert("L"))
    h, w = g.shape
    if h < 8 or w < 8:
        return None
    best = None
    for thr in (165, 150, 180, 135):
        mask = np.zeros_like(g, dtype=bool)
        y0b, y1b = min(8, h // 5), max(min(8, h // 5) + 1, h - 12)
        mask[y0b:y1b] = g[y0b:y1b] >= thr
        if int(mask.sum()) < 18:
            continue
        ys, xs = np.where(mask)
        x0, x1 = max(0, int(xs.min()) - 4), min(w, int(xs.max()) + 5)
        y0, y1 = max(0, int(ys.min()) - 3), min(h, int(ys.max()) + 4)
        if (x1 - x0) < 8 or (y1 - y0) < 8:
            continue
        area = (x1 - x0) * (y1 - y0)
        dens = float(mask[y0:y1, x0:x1].mean()) if area else 0.0
        best = (dens, strip.crop((x0, y0, x1, y1)))
        if dens > 0.12:
            break
    return None if best is None else best[1]


def _parse_ocr(res) -> str:
    if not res:
        return ""
    parts = []
    for item in res:
        if not item:
            continue
        t = simp(str(item[1]))
        if not t or t in RANK:
            continue
        if any(k in t for k in SKIP):
            continue
        if not CJK_RE.search(t):
            continue
        parts.append(t)
    return "".join(parts)


def rec_line(crop: Image.Image) -> str:
    g = np.array(crop.convert("L"))
    if g.size == 0:
        return ""
    if float(g.mean()) < 127:
        bw = ImageOps.invert(Image.fromarray(g))
    else:
        bw = Image.fromarray(g)
    nh = 48
    nw = max(32, int(round(bw.width * (nh / float(max(1, bw.height))))))
    bw = bw.resize((nw, nh), Image.Resampling.LANCZOS).convert("RGB")
    res, _ = engine()(np.array(bw))
    return _parse_ocr(res)


def rec_det(band: Image.Image) -> str:
    up = band.resize((band.width * 3, max(1, band.height * 3)), Image.Resampling.LANCZOS)
    res, _ = engine_det()(np.array(up.convert("RGB")))
    return _parse_ocr(res)


def ocr_strip(strip: Image.Image) -> str:
    crop = text_crop(strip)
    if crop is None:
        return rec_line(strip)
    t = rec_line(crop)
    return t or rec_line(strip)


def ocr_cell_name(im: Image.Image, x: int, y: int, suffixes: list[str] | None = None) -> str:
    """Names drift inside the 218px cell; take the topmost CJK hit."""
    w, h = im.size
    x2 = min(x + SIDE, w - 2)
    bands: list[tuple[float, int, Image.Image]] = []
    for off in range(0, DY - 4, 6):
        y0 = y + off
        y1 = y0 + 32
        if y1 > h - 2:
            break
        band = im.crop((x, y0, x2, y1))
        wf = white_frac(band, 160)
        if wf < 0.030 or wf > 0.20:
            continue
        gmean = float(np.array(band.convert("L"), dtype=np.float32).mean())
        if gmean > 100:
            continue
        bands.append((wf, off, band))
    peaks: list[tuple[float, int, Image.Image]] = []
    for i, item in enumerate(bands):
        wf, off, band = item
        prev_wf = bands[i - 1][0] if i else -1
        next_wf = bands[i + 1][0] if i + 1 < len(bands) else -1
        if wf >= prev_wf and wf >= next_wf:
            peaks.append(item)
    peaks.sort(key=lambda z: -z[0])
    hits: list[tuple[int, str]] = []
    seen: set[str] = set()
    for wf, off, band in peaks[:4]:
        t = ocr_strip(band)
        if not t or t in seen:
            continue
        seen.add(t)
        hits.append((off, t))
    if not hits:
        ny0 = y + SIDE - 4
        ny1 = min(y + DY - 4, h - 2)
        if ny0 < h - 6 and ny1 > ny0 + 8:
            t = ocr_strip(im.crop((x, ny0, x2, ny1)))
            if t:
                hits.append((SIDE, t))
    if not hits:
        return ""
    hits.sort(key=lambda z: (len(z[1]) < 2, z[0], -len(z[1])))
    name = hits[0][1] if hits else ""
    suf = suffixes or []
    if name and name in suf:
        return name
    snapped = snap_name(name, suf) if name else ""
    if snapped and snapped != name:
        return snapped
    if (not name or len(name) < 2) and peaks:
        det = rec_det(peaks[0][2])
        if det:
            if det in suf:
                return det
            det_s = snap_name(det, suf)
            if det_s and det_s != det:
                return det_s
            return det
    return snapped or name


def load_suffixes(rar: str) -> list[str]:
    path = os.path.join(EXTRACT, "%s.jsonl" % rar)
    names: list[str] = []
    if not os.path.isfile(path):
        return names
    with open(path, encoding="utf-8") as f:
        for line in f:
            if not line.strip():
                continue
            rec = json.loads(line)
            n = simp(rec.get("name") or "")
            if n:
                names.append(n)
    suf: set[str] = set()
    for n in names:
        suf.add(n)
        for k in range(2, min(7, len(n) + 1)):
            suf.add(n[-k:])
    return list(suf)


def edist(a: str, b: str) -> int:
    if a == b:
        return 0
    la, lb = len(a), len(b)
    if abs(la - lb) > 1:
        return 9
    if la > lb:
        a, b = b, a
        la, lb = lb, la
    if la == lb:
        return sum(ch1 != ch2 for ch1, ch2 in zip(a, b))
    # insert 1 into a
    j = 0
    miss = 0
    for ch in a:
        while j < lb and b[j] != ch:
            j += 1
            miss += 1
            if miss > 1:
                return 9
        j += 1
    miss += lb - j
    return miss


def lcs_len(a: str, b: str) -> int:
    if not a or not b:
        return 0
    n, m = len(a), len(b)
    prev = [0] * (m + 1)
    for i in range(1, n + 1):
        cur = [0] * (m + 1)
        ca = a[i - 1]
        for j in range(1, m + 1):
            if ca == b[j - 1]:
                cur[j] = prev[j - 1] + 1
            else:
                cur[j] = cur[j - 1] if cur[j - 1] >= prev[j] else prev[j]
        prev = cur
    return prev[m]


def snap_score(ocr: str, suf: str) -> float:
    if not ocr or not suf:
        return 0.0
    if ocr == suf:
        return 1.0
    d = edist(ocr, suf)
    if d == 1:
        return 0.92
    if ocr in suf or suf in ocr:
        return 0.8 + 0.1 * min(len(ocr), len(suf)) / max(len(ocr), len(suf))
    lcs = lcs_len(ocr, suf)
    return lcs / float(max(len(ocr), len(suf)))


def snap_name(name: str, suffixes: list[str]) -> str:
    if not name or not suffixes:
        return name
    if name in suffixes:
        return name
    if len(name) <= 2:
        d1 = [s for s in suffixes if len(s) == len(name) and edist(s, name) == 1]
        return d1[0] if len(d1) == 1 else name
    scored = [(snap_score(name, s), s) for s in suffixes]
    good = [(sc, s) for sc, s in scored if sc >= 0.72]
    if not good:
        return name
    good.sort(key=lambda z: (-z[0], abs(len(z[1]) - len(name)), -len(z[1])))
    return good[0][1]


def icon_rel(rar: str, idx: int) -> str:
    for prefix in ("F", "R"):
        rel = "icons/%s%s_%03d.png" % (prefix, rar, idx)
        if os.path.isfile(os.path.join(DATA, rel.replace("/", os.sep))):
            return rel
    return "icons/R%s_%03d.png" % (rar, idx)


def harvest_rarity(rar: str, files: list[str], suffixes: list[str]) -> list[dict]:
    prev: list[tuple[np.ndarray, np.ndarray]] = []
    rows: list[dict] = []
    seq = 0
    for fn in files:
        path = os.path.join(SHOTS, fn)
        if not os.path.isfile(path):
            print("missing", fn, flush=True)
            continue
        im = Image.open(path).convert("RGB")
        w, h = im.size
        y0 = find_y0(im)
        n_new = n_named = 0
        page_thumbs: list[tuple[np.ndarray, np.ndarray]] = []
        for r in range(ROWS):
            y = y0 + r * DY
            if y >= h - 8:
                break
            vis = h - 8 - y
            if vis < 110:
                continue
            row_new = 0
            for c in range(COLS):
                x = X0 + c * DX
                if x + SIDE > w - 4:
                    continue
                x2 = min(x + SIDE, w - 2)
                y2 = min(y + SIDE, h - 2)
                if (x2 - x) < 110 or (y2 - y) < 110:
                    continue
                tile = im.crop((x, y, x2, y2))
                if tile.size != (SIDE, SIDE):
                    tile = tile.resize((SIDE, SIDE), Image.Resampling.BILINEAR)
                if empty_tile(tile):
                    continue
                th = thumb(tile)
                sig = color_sig(tile)
                if is_dup(th, sig, prev):
                    continue
                name = ocr_cell_name(im, x, y, suffixes)
                if len(name) < 2:
                    name = ""
                # scrolled leftover pages: drop empty checkerboard / UI chips
                if (not name) and ("_p02" in fn or "_p03" in fn):
                    continue
                rec = {
                    "rarity_key": rar,
                    "idx": seq,
                    "name_grid": name,
                    "file": icon_rel(rar, seq),
                    "source": fn,
                    "x": int(x),
                    "y": int(y),
                }
                rows.append(rec)
                page_thumbs.append((th, sig))
                seq += 1
                n_new += 1
                row_new += 1
                if name:
                    n_named += 1
            if row_new:
                print("  row", r, "y", y, "new", row_new, "seq", seq, flush=True)
        prev.extend(page_thumbs)
        named_r = sum(1 for z in rows if z["name_grid"])
        print(
            "page", fn, "y0", y0, "new", n_new, "seq", seq,
            "named+", n_named, "named_r", named_r, flush=True,
        )
    print("rarity", rar, "got", seq, "named", sum(1 for z in rows if z["name_grid"]),
          "expect", EXPECT[rar], flush=True)
    return rows


def write_jsonl(path: str, rows: list[dict]) -> None:
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8") as f:
        for rec in rows:
            f.write(json.dumps(rec, ensure_ascii=False) + "\n")


def main() -> None:
    t0 = time.time()
    os.makedirs(LAYOUT, exist_ok=True)
    engine()
    all_rows: list[dict] = []
    by_rar: dict[str, list[dict]] = {}
    for rar, files in PAGES.items():
        suffixes = load_suffixes(rar)
        rows = harvest_rarity(rar, files, suffixes)
        by_rar[rar] = rows
        all_rows.extend(rows)
    write_jsonl(OUT_ALL, all_rows)
    write_jsonl(OUT_2, by_rar.get("2") or [])
    write_jsonl(OUT_1, by_rar.get("1") or [])
    n_all = len(all_rows)
    n_named = sum(1 for z in all_rows if z["name_grid"])
    print("non-empty names", n_named, "/", n_all, flush=True)
    for rar in ("3", "2", "1"):
        rr = by_rar.get(rar) or []
        names = [z["name_grid"] for z in rr if z["name_grid"]]
        uniq = sorted(set(names))
        print(
            "  r%s tiles=%d named=%d unique_names=%d expect~%s"
            % (rar, len(rr), len(names), len(uniq), EXPECT[rar]),
            flush=True,
        )
        print("  sample", names[:12], flush=True)
    print("WROTE", OUT_ALL, OUT_2, OUT_1, "in %.1fs" % (time.time() - t0), flush=True)


if __name__ == "__main__":
    main()
