# -*- coding: utf-8 -*-
"""OCR Destiny Child Memorial library viewer screenshots (561 Childs)."""
from __future__ import annotations

import argparse
import json
import os
import re
import sys
import time

import numpy as np
from PIL import Image
from rapidocr_onnxruntime import RapidOCR
from zhconv import convert as zh_convert

SHOTS = r"F:\天命之子\docs\reference\mobile-archive\screenshots"
OUT_DIR = r"F:\天命之子\天命之子数据\_extract"

STATS_BOX = (185, 360, 590, 860)
NAME_BOX = (20, 1580, 920, 1980)
SKILL_BOX = (40, 240, 1160, 2200)
QUOTE_BOX = (20, 1760, 920, 2100)

SERIES = {
    "5": {
        "rarity": "5星",
        "n": 282,
        "stats": "300_libstats_{:03d}.png",
        "skills": "301_libskills_{:03d}.png",
        "skills_b": "302_libskills_{:03d}_b.png",
        "skills_max": "303_libskills_{:03d}_max.png",
    },
    "4": {
        "rarity": "4星",
        "n": 77,
        "stats": "320_4s_stats_{:03d}.png",
        "skills": "321_4s_skills_{:03d}.png",
        "skills_b": "322_4s_skills_{:03d}_b.png",
        "skills_max": "323_4s_skills_{:03d}_max.png",
    },
    "3": {
        "rarity": "3星",
        "n": 99,
        "stats": "330_3s_stats_{:03d}.png",
        "skills": "331_3s_skills_{:03d}.png",
        "skills_b": "332_3s_skills_{:03d}_b.png",
        "skills_max": "333_3s_skills_{:03d}_max.png",
    },
    "2": {
        "rarity": "2星",
        "n": 53,
        "stats": "340_2s_stats_{:03d}.png",
        "skills": "340_2s_skills_{:03d}.png",
        "skills_b": "340_2s_skills_{:03d}_b.png",
        "skills_max": "",
    },
    "1": {
        "rarity": "1星",
        "n": 50,
        "stats": "350_1s_stats_{:03d}.png",
        "skills": "350_1s_skills_{:03d}.png",
        "skills_b": "350_1s_skills_{:03d}_b.png",
        "skills_max": "",
    },
}

EL_MAP = [
    ("闇", "暗"), ("暗", "暗"), ("火", "火"), ("水", "水"),
    ("木", "木"), ("光", "光"),
]
ROLE_MAP = [
    ("攻擊", "攻击型"), ("攻击", "攻击型"),
    ("防禦", "防御型"), ("防御", "防御型"),
    ("妨害", "干扰型"), ("干擾", "干扰型"), ("干扰", "干扰型"),
    ("治癒", "治疗型"), ("治療", "治疗型"), ("治疗", "治疗型"),
    ("輔助", "辅助型"), ("辅助", "辅助型"),
]
NUM_RE = re.compile(r"\d{1,3}(?:[,.]\d{3})+|\d+")
CJK_SPACE = re.compile(r"(?<=[\u4e00-\u9fff])\s+(?=[\u4e00-\u9fff])")
ENGINE = None


def engine():
    global ENGINE
    if ENGINE is None:
        ENGINE = RapidOCR()
    return ENGINE


def simp(s: str) -> str:
    s = CJK_SPACE.sub("", s or "")
    s = zh_convert(s, "zh-cn")
    return re.sub(r"\s+", " ", s).strip()


def ocr_crop(im: Image.Image, box, max_w: int = 0) -> list[str]:
    crop = im.crop(box).convert("RGB")
    if max_w and crop.width > max_w:
        nh = int(crop.height * (max_w / float(crop.width)))
        crop = crop.resize((max_w, max(1, nh)), Image.Resampling.BILINEAR)
    res, _ = engine()(np.array(crop))
    if not res:
        return []
    out = []
    for item in res:
        if not item:
            continue
        text = item[1] if len(item) > 1 else ""
        if text:
            out.append(simp(str(text)))
    return out


def parse_num(tok: str) -> str:
    t = (tok or "").replace(" ", "")
    if re.fullmatch(r"\d{1,3}\.\d{3}", t):
        t = t.replace(".", "")
    t = t.replace(",", "")
    return t if t.isdigit() else ""


def parse_stats(lines: list[str]) -> dict:
    blob = " ".join(lines)
    rec = {
        "element": "", "role": "", "cp": "", "hp": "",
        "atk": "", "def": "", "agl": "", "crt": "",
        "has_stats_panel": False,
        "stats_ocr": " | ".join(lines),
    }
    for tok, key in EL_MAP:
        if tok in blob:
            rec["element"] = key
            break
    for tok, key in ROLE_MAP:
        if tok in blob:
            rec["role"] = key
            break
    nums = []
    for ln in lines:
        for m in NUM_RE.findall(ln.replace(" ", "")):
            n = parse_num(m)
            if n and int(n) > 0:
                nums.append(n)
    # drop 5-star count "5" if present
    nums = [n for n in nums if not (len(n) == 1 and n in "12345")]
    keys = ["cp", "hp", "atk", "def", "agl", "crt"]
    for i, k in enumerate(keys):
        if i < len(nums):
            rec[k] = nums[i]
    rec["has_stats_panel"] = bool(rec["cp"] or rec["hp"])
    return rec


def parse_name(lines: list[str]) -> dict:
    rec = {"name": "", "cv": "", "quote": "", "name_ocr": " | ".join(lines)}
    joined = lines[:]
    name = ""
    cv = ""
    quote_parts = []
    for ln in joined:
        if "简介" in ln or "簡介" in ln:
            continue
        m = re.search(r"(.+?)\s*CV\s*[:：]?\s*(.+)", ln, re.I)
        if m and not name:
            name = m.group(1).strip()
            cv = m.group(2).strip()
            continue
        if ln.upper().startswith("CV"):
            cv = re.sub(r"^CV\s*[:：]?\s*", "", ln, flags=re.I)
            continue
        if not name and re.search(r"[\u4e00-\u9fff]{2,}", ln):
            if any(x in ln for x in ("属性", "屬性", "战斗", "戰鬥", "HP")):
                continue
            name = ln
            continue
        if name and ln and "CV" not in ln.upper():
            quote_parts.append(ln)
    rec["name"] = re.sub(r"CV.*", "", name).strip(" :：")
    rec["cv"] = cv
    rec["quote"] = " ".join(quote_parts)[:200]
    return rec


def _clean_skill_blob(s: str) -> str:
    s = re.sub(r"\b(MIN|MAX|BUFF|SKILL INFORMATION|基能力|基础能力|圖库模式|图库模式)\b", " ", s, flags=re.I)
    s = re.sub(r"\s+", " ", s).strip(" |")
    return s


def parse_skills(lines: list[str]) -> dict:
    rec = {
        "auto_text": "", "tap_text": "", "slide_text": "",
        "drive_text": "", "leader_text": "",
        "skills_ocr": " | ".join(lines),
    }
    blob = " || ".join(lines)
    parts = re.split(
        r"(DEFAULT|NORMAL|ESLIDE|SLIDE|DRIVE|LEADER)",
        blob,
        flags=re.I,
    )
    # OCR order: [auto body] DEFAULT [tap body] NORMAL [slide] SLIDE [drive] LEADER [leader]
    chunks = {"auto": "", "tap": "", "slide": "", "drive": "", "leader": ""}
    if parts:
        chunks["auto"] = parts[0]
        cur = None
        nxt = {
            "DEFAULT": "tap", "NORMAL": "slide", "SLIDE": "drive", "ESLIDE": "drive",
            "DRIVE": "leader", "LEADER": "leader",
        }
        i = 1
        while i < len(parts):
            tag = parts[i].upper()
            body = parts[i + 1] if i + 1 < len(parts) else ""
            dest = nxt.get(tag)
            if dest == "leader" and tag == "LEADER":
                chunks["leader"] += " " + body
            elif dest:
                # body after DEFAULT is TAP, etc.
                if tag == "DEFAULT":
                    chunks["tap"] += " " + body
                elif tag == "NORMAL":
                    chunks["slide"] += " " + body
                elif tag in ("SLIDE", "ESLIDE"):
                    chunks["drive"] += " " + body
                elif tag == "DRIVE":
                    chunks["leader"] += " " + body
            i += 2
    rec["auto_text"] = _clean_skill_blob(chunks["auto"])
    rec["tap_text"] = _clean_skill_blob(chunks["tap"])
    rec["slide_text"] = _clean_skill_blob(chunks["slide"])
    rec["drive_text"] = _clean_skill_blob(chunks["drive"])
    rec["leader_text"] = _clean_skill_blob(chunks["leader"])
    return rec


def load_img(path: str):
    if not path or not os.path.isfile(path):
        return None
    return Image.open(path).convert("RGB")


def extract_skills_into(rec: dict, rarity_key: str, idx: int) -> dict:
    spec = SERIES[rarity_key]
    skills_fn = spec["skills"].format(idx)
    skills_b_fn = spec["skills_b"].format(idx) if spec.get("skills_b") else ""
    skill_lines = []
    for fn in (skills_fn, skills_b_fn):
        if not fn:
            continue
        sim = load_img(os.path.join(SHOTS, fn))
        if sim is None:
            continue
        skill_lines.extend(ocr_crop(sim, SKILL_BOX, max_w=640))
    rec.update(parse_skills(skill_lines))
    rec["skills_file"] = skills_fn
    return rec


def extract_one(rarity_key: str, idx: int, skip_skills: bool = False) -> dict:
    spec = SERIES[rarity_key]
    stats_fn = spec["stats"].format(idx)
    skills_fn = spec["skills"].format(idx)
    skills_b_fn = spec["skills_b"].format(idx) if spec.get("skills_b") else ""
    rec = {
        "idx": idx,
        "rarity": spec["rarity"],
        "rarity_key": rarity_key,
        "stats_file": stats_fn,
        "skills_file": skills_fn,
    }
    stats_im = load_img(os.path.join(SHOTS, stats_fn))
    if stats_im is None:
        rec["error"] = "missing-stats-file"
        return rec
    rec.update(parse_name(ocr_crop(stats_im, NAME_BOX)))
    rec.update(parse_stats(ocr_crop(stats_im, STATS_BOX)))
    skill_lines = []
    if not skip_skills:
        for fn in (skills_fn, skills_b_fn):
            if not fn:
                continue
            sim = load_img(os.path.join(SHOTS, fn))
            if sim is None:
                continue
            skill_lines.extend(ocr_crop(sim, SKILL_BOX, max_w=720))
    rec.update(parse_skills(skill_lines))
    rec["id"] = "M%s-%03d" % (rarity_key, idx)
    return rec


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--rarity", required=True, choices=list(SERIES))
    ap.add_argument("--start", type=int, default=0)
    ap.add_argument("--end", type=int, default=None, help="exclusive")
    ap.add_argument("--out", required=True)
    ap.add_argument("--skip-skills", action="store_true")
    ap.add_argument("--skills-only", action="store_true")
    ap.add_argument("--patch", default="", help="existing jsonl to copy names from")
    args = ap.parse_args()
    spec = SERIES[args.rarity]
    end = spec["n"] if args.end is None else args.end
    os.makedirs(os.path.dirname(args.out) or ".", exist_ok=True)
    prev = {}
    if args.patch and os.path.isfile(args.patch):
        with open(args.patch, encoding="utf-8") as f:
            for line in f:
                line = line.strip()
                if not line.startswith("{"):
                    continue
                try:
                    rec = json.loads(line)
                except json.JSONDecodeError:
                    continue
                prev[int(rec["idx"])] = rec
    engine()
    t0 = time.time()
    n = 0
    with open(args.out, "w", encoding="utf-8") as f:
        for i in range(args.start, end):
            if args.skills_only:
                rec = dict(prev.get(i) or {
                    "idx": i, "rarity": spec["rarity"], "rarity_key": args.rarity,
                    "id": "M%s-%03d" % (args.rarity, i),
                })
                rec = extract_skills_into(rec, args.rarity, i)
            else:
                rec = extract_one(args.rarity, i, skip_skills=args.skip_skills)
            f.write(json.dumps(rec, ensure_ascii=False) + "\n")
            f.flush()
            n += 1
            if n % 5 == 0 or i == end - 1:
                print(
                    "r%s %d/%d  last=%s tap=%s  %.0fs"
                    % (
                        args.rarity,
                        i + 1 - args.start,
                        end - args.start,
                        rec.get("name"),
                        bool(rec.get("tap_text") or rec.get("auto_text")),
                        time.time() - t0,
                    ),
                    flush=True,
                )
    print("DONE", args.out, n, "rows", flush=True)
    return 0


if __name__ == "__main__":
    sys.exit(main())
