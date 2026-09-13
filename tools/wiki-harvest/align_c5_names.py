# -*- coding: utf-8 -*-
"""OCR name-strip on C5 crops, recrop face-only, set CSV name to match the portrait."""
from __future__ import annotations

import csv
import os
import re

import numpy as np
from PIL import Image
from rapidocr_onnxruntime import RapidOCR
from zhconv import convert as zh_convert

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
USER = os.path.join(ROOT, "天命之子数据")
CSV_CHILD = os.path.join(USER, "汇总表.csv")
CJK = re.compile(r"[\u4e00-\u9fffA-Za-z·・]+")


def file_ok(rel: str) -> bool:
    p = os.path.join(USER, rel.replace("\\", "/"))
    return os.path.isfile(p) and os.path.getsize(p) > 800


def ocr_strip(ocr, im: Image.Image) -> str:
    w, h = im.size
    band_h = max(22, int(h * 0.22))
    band = im.crop((0, h - band_h, w, h))
    # upscale for OCR
    band = band.resize((w * 3, band_h * 3), Image.Resampling.NEAREST)
    try:
        result, _ = ocr(np.array(band.convert("RGB")))
    except Exception:
        return ""
    parts = []
    if result:
        for item in result:
            t = item[1][0] if isinstance(item[1], (list, tuple)) else str(item[1])
            parts.append(t)
    raw = "".join(parts)
    raw = zh_convert(re.sub(r"\s+", "", raw), "zh-cn")
    m = CJK.findall(raw)
    s = "".join(m)
    if s in ("LIBRARY", "Child", "变更顺序"):
        return ""
    return s if 2 <= len(s) <= 14 else ""


def main():
    print("loading OCR…", flush=True)
    ocr = RapidOCR()
    with open(CSV_CHILD, encoding="utf-8-sig") as f:
        rows = list(csv.DictReader(f))
        fields = list(rows[0].keys())

    n_name = n_crop = 0
    for r in rows:
        av = (r.get("avatar_file") or "").replace("\\", "/")
        if not av.startswith("icons/C5_") and not av.startswith("icons/C3_"):
            continue
        src = os.path.join(USER, av)
        if not os.path.isfile(src):
            continue
        im = Image.open(src).convert("RGB")
        w, h = im.size
        name = ocr_strip(ocr, im)
        if name:
            r["name"] = name
            n_name += 1
            print(" ", av, "->", name, flush=True)
        # cut name strip: keep top ~86%
        face_h = int(h * 0.84)
        if face_h >= 96 and face_h < h:
            face = im.crop((0, 0, w, face_h))
            dest_rel = av.replace("icons/C5_", "icons/C5f_").replace("icons/C3_", "icons/C3f_")
            dest = os.path.join(USER, dest_rel)
            face.save(dest)
            r["avatar_file"] = dest_rel
            n_crop += 1

    print("renamed", n_name, "recropped", n_crop)
    with open(CSV_CHILD, "w", encoding="utf-8-sig", newline="") as f:
        w = csv.DictWriter(f, fieldnames=fields, extrasaction="ignore")
        w.writeheader()
        w.writerows(rows)


if __name__ == "__main__":
    main()
