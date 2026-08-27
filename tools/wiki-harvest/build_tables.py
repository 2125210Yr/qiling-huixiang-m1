# -*- coding: utf-8 -*-
"""Turn harvested GameKee pages into character / skill / soul-carta tables."""
from __future__ import annotations

import csv
import json
import os
import re
import shutil
import sys

from html_table import extract_rows

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
PAGES = os.path.join(ROOT, "docs", "reference", "gamekee", "pages")
CSV_INDEX = os.path.join(ROOT, "docs", "reference", "gamekee", "catalog_index.csv")
OUT = os.path.join(ROOT, "docs", "reference", "gamekee", "tables")
USER_OUT = os.path.join(ROOT, "天命之子数据")

PAIR = re.compile(r"(\d+(?:\.\d+)?)\s*[（(]\s*(\d+(?:\.\d+)?)\s*[)）]")
NUM = re.compile(r"\d+(?:\.\d+)?")
EL_TOKENS = (("火", "火"), ("水", "水"), ("木", "木"), ("光", "光"), ("暗", "暗"), ("闇", "暗"))
ROLE_TOKENS = (
    ("攻击型", "攻击型"),
    ("攻擊型", "攻击型"),
    ("防御型", "防御型"),
    ("防禦型", "防御型"),
    ("干扰型", "干扰型"),
    ("干擾型", "干扰型"),
    ("妨害", "干扰型"),
    ("治疗型", "治疗型"),
    ("治療型", "治疗型"),
    ("治疗", "治疗型"),
    ("治療", "治疗型"),
    ("辅助型", "辅助型"),
    ("輔助型", "辅助型"),
    ("辅助", "辅助型"),
    ("輔助", "辅助型"),
)


def load_page(cid: int) -> dict:
    path = os.path.join(PAGES, "%d.json" % cid)
    with open(path, encoding="utf-8") as f:
        return json.load(f).get("data") or {}


def page_rows(cid: int):
    data = load_page(cid)
    return extract_rows(data.get("content") or ""), data.get("title") or ""


def last_text(row):
    for cell in reversed(row):
        if cell:
            return cell
    return ""


def first_num(s):
    m = NUM.search(s or "")
    return m.group(0) if m else ""


def pair_first(s):
    m = PAIR.search(s or "")
    if m:
        return m.group(1), m.group(2)
    n = first_num(s)
    return n, n


def damage_from(s):
    if not s:
        return "", ""
    m = re.search(r"造成\s*%s" % PAIR.pattern, s)
    if m:
        return m.group(1), m.group(2)
    m = re.search(r"造成\s*(\d+(?:\.\d+)?)\s*伤害", s)
    if m:
        return m.group(1), m.group(1)
    a, b = pair_first(s)
    return a, b


def detect_element(rows, blob=""):
    text = " | ".join(" | ".join(r) for r in rows[:6]) + " " + blob
    for tok, key in EL_TOKENS:
        if tok in text:
            return key
    return ""


def detect_role(rows, blob=""):
    exact = {
        "攻击": "攻击型", "攻击型": "攻击型", "攻擊型": "攻击型",
        "防御": "防御型", "防御型": "防御型", "防禦型": "防御型",
        "干扰": "干扰型", "干扰型": "干扰型", "干擾": "干扰型", "干擾型": "干扰型", "妨害": "干扰型",
        "治疗": "治疗型", "治疗型": "治疗型", "治療": "治疗型", "治療型": "治疗型",
        "辅助": "辅助型", "辅助型": "辅助型", "輔助": "辅助型", "輔助型": "辅助型",
    }
    for row in rows[:6]:
        for cell in row:
            key = exact.get((cell or "").strip())
            if key:
                return key
    text = " | ".join(" | ".join(r) for r in rows[:8]) + " " + blob
    for tok, key in ROLE_TOKENS:
        if tok in text:
            return key
    return ""


def parse_stats(rows):
    stats = {
        "cp_init": "", "hp_init": "", "atk_init": "", "def_init": "", "agl_init": "", "crt_init": "",
        "cp_max": "", "hp_max": "", "atk_max": "", "def_max": "", "agl_max": "", "crt_max": "",
    }
    header = None
    for row in rows:
        cells = [c.replace(" ", "") for c in row]
        joined = "".join(cells)
        if "面板" in joined or (header is None and "HP" in cells and any("攻击" in c for c in cells)):
            header = cells
            continue
        key = None
        if cells and cells[0] in ("初始", "满破"):
            key = "init" if cells[0] == "初始" else "max"
            nums = [c for c in cells[1:] if re.fullmatch(r"\d+", c or "")]
        else:
            continue
        mapping = ["cp", "hp", "atk", "def", "agl", "crt"]
        if header:
            colmap = []
            for h in header:
                if "战斗" in h:
                    colmap.append("cp")
                elif h == "HP":
                    colmap.append("hp")
                elif "攻击" in h:
                    colmap.append("atk")
                elif "防御" in h:
                    colmap.append("def")
                elif "敏捷" in h:
                    colmap.append("agl")
                elif "暴" in h:
                    colmap.append("crt")
            if len(colmap) == len(nums):
                mapping = colmap
        for i, n in enumerate(nums[:6]):
            field = mapping[i] if i < len(mapping) else None
            if field:
                stats["%s_%s" % (field, key)] = n
    return stats


def is_skill_head(cell):
    c = (cell or "").replace(" ", "")
    if re.match(r"^U$", c) or (c.endswith("U") and any(x in c for x in ("重击", "滑动", "大招", "TS", "SS", "DS"))):
        return "ignite"
    if "普攻" in c or "自动" in c:
        return "auto"
    if "重击" in c or c.startswith("TS") or "(TS)" in c or "（TS）" in c:
        return "tap"
    if "滑动" in c or "SLIDE" in c or "(SS)" in c or "（SS）" in c:
        return "slide"
    if "大招" in c or "DRIVE" in c or "(DS)" in c or "（DS）" in c:
        return "drive"
    if "队长" in c or "LEADER" in c:
        return "leader"
    return ""


def parse_skills(rows):
    out = {k: {"text": "", "text_ignited": "", "dmg_min": "", "dmg_max": "", "dmg_ignited": ""}
           for k in ("auto", "tap", "slide", "drive", "leader")}
    pending = None
    for row in rows:
        if not row:
            continue
        kind = is_skill_head(row[0])
        if not kind and pending and (row[0] or "").replace(" ", "") in ("U", "重击U", "滑动U", "大招U"):
            kind = "ignite"
        text = last_text(row if kind != "ignite" else row)
        if kind == "ignite":
            if pending:
                out[pending]["text_ignited"] = text
                a, b = damage_from(text)
                out[pending]["dmg_ignited"] = b or a
            continue
        if not kind:
            continue
        pending = kind
        out[kind]["text"] = text
        a, b = damage_from(text)
        out[kind]["dmg_min"] = a
        out[kind]["dmg_max"] = b
        if kind == "leader":
            pending = None
    return out


def parse_character(cid, name, path, rarity):
    rows, title = page_rows(cid)
    stats = parse_stats(rows)
    skills = parse_skills(rows)
    element = detect_element(rows, path + title)
    role = detect_role(rows, path + title)
    # combat power sometimes sits alone
    if not stats["cp_init"]:
        for row in rows[:4]:
            nums = [c for c in row if re.fullmatch(r"\d{3,6}", c or "")]
            if len(nums) == 1 and not any(is_skill_head(x) for x in row):
                stats["cp_init"] = nums[0]
                break
    rec = {
        "content_id": cid,
        "name": name.strip(),
        "title": title,
        "rarity": rarity,
        "element": element,
        "role": role,
        "path": path,
    }
    rec.update(stats)
    rec["auto_text"] = skills["auto"]["text"]
    rec["tap_text"] = skills["tap"]["text"]
    rec["tap_ignited"] = skills["tap"]["text_ignited"]
    rec["slide_text"] = skills["slide"]["text"]
    rec["slide_ignited"] = skills["slide"]["text_ignited"]
    rec["drive_text"] = skills["drive"]["text"]
    rec["drive_ignited"] = skills["drive"]["text_ignited"]
    rec["leader_text"] = skills["leader"]["text"]
    rec["_skills"] = skills
    rec["table_rows"] = len(rows)
    return rec


def parse_carta_page(cid, name, rarity_hint):
    rows, title = page_rows(cid)
    rec = {
        "content_id": cid,
        "name": name.strip() or title,
        "rarity": rarity_hint,
        "stat_pair": "",
        "element_gate": "",
        "role_gate": "",
        "mode_gate": "",
        "collab": "",
        "special": "",
        "plain_hp_init": "", "plain_atk_init": "", "plain_def_init": "", "plain_agl_init": "", "plain_crt_init": "",
        "plain_hp_max": "", "plain_atk_max": "", "plain_def_max": "", "plain_agl_max": "", "plain_crt_max": "",
        "flash_hp_init": "", "flash_atk_init": "", "flash_def_init": "", "flash_agl_init": "", "flash_crt_init": "",
        "flash_hp_max": "", "flash_atk_max": "", "flash_def_max": "", "flash_agl_max": "", "flash_crt_max": "",
    }
    if len(rows) >= 2:
        r1 = rows[1]
        # ['5星','血攻','水','通用','通用','否']
        if r1:
            if not rec["rarity"] and r1[0]:
                rec["rarity"] = r1[0]
            if len(r1) > 1:
                rec["stat_pair"] = r1[1]
            if len(r1) > 2:
                rec["element_gate"] = r1[2]
            if len(r1) > 3:
                rec["role_gate"] = r1[3]
            if len(r1) > 4:
                rec["mode_gate"] = r1[4]
            if len(r1) > 5:
                rec["collab"] = r1[5]
    if len(rows) >= 3 and rows[2]:
        rec["special"] = last_text(rows[2])
    # numeric block
    # ['普卡','初始', hp, atk, def, agl, crt]
    mode = None  # plain/flash
    for row in rows:
        cells = list(row)
        head = (cells[0] if cells else "")
        if head == "普卡":
            mode = "plain"
        elif head == "闪卡":
            mode = "flash"
        stage = None
        nums_from = 0
        if head in ("普卡", "闪卡") and len(cells) > 1 and cells[1] in ("初始", "满破"):
            stage = "init" if cells[1] == "初始" else "max"
            nums_from = 2
        elif head in ("初始", "满破") and mode:
            stage = "init" if head == "初始" else "max"
            nums_from = 1
        if not mode or not stage:
            continue
        nums = cells[nums_from:]
        # pad to 5: HP 攻击 防御 敏捷 暴击
        while len(nums) < 5:
            nums.append("")
        fields = ("hp", "atk", "def", "agl", "crt")
        for i, f in enumerate(fields):
            rec["%s_%s_%s" % (mode, f, stage)] = nums[i]
    return rec


def parse_screening():
    rows, _ = page_rows(58309)
    items = []
    for row in rows[1:]:
        if len(row) < 3:
            continue
        # ['', name, rarity, pair, el, role, mode, collab, special]
        name = row[1] if len(row) > 1 else ""
        if not name:
            continue
        items.append({
            "name": name.strip(),
            "rarity": row[2] if len(row) > 2 else "",
            "stat_pair": row[3] if len(row) > 3 else "",
            "element_gate": row[4] if len(row) > 4 else "",
            "role_gate": row[5] if len(row) > 5 else "",
            "mode_gate": row[6] if len(row) > 6 else "",
            "collab": row[7] if len(row) > 7 else "",
            "special": row[8] if len(row) > 8 else last_text(row),
        })
    return items


def parse_puppet(cid, name, rarity_folder):
    rows, title = page_rows(cid)
    rec = {
        "content_id": cid,
        "name": name.strip() or title,
        "element": "",
        "rarity": rarity_folder,
        "tap": "",
        "slide": "",
        "drive": "",
        "leader": "",
        "lv1": "",
        "lv70": "",
    }
    if len(rows) >= 2:
        r1 = rows[1]
        if len(r1) >= 3:
            rec["name"] = r1[0] or rec["name"]
            rec["element"] = r1[1]
            rec["rarity"] = r1[2] or rec["rarity"]
    slot = ""
    for row in rows[2:]:
        head = (row[0] if row else "")
        if "滑动" in head or "SS" in head:
            slot = "slide"
            rec["slide"] = last_text(row)
        elif "重击" in head or "TS" in head or "普通" in head or "NORMAL" in head:
            slot = "tap"
            rec["tap"] = last_text(row)
        elif "大招" in head or "DS" in head or "DRIVE" in head:
            slot = "drive"
            rec["drive"] = last_text(row)
        elif "队长" in head or "LEADER" in head:
            slot = "leader"
            rec["leader"] = last_text(row)
        elif head.startswith("LV1") or head == "LV1":
            rec["lv1"] = last_text(row)
            if slot and not rec[slot]:
                rec[slot] = rec["lv1"]
        elif "LV70" in head:
            rec["lv70"] = last_text(row)
        elif slot and last_text(row) and not rec[slot]:
            rec[slot] = last_text(row)
    return rec


def parse_buffs():
    rows, _ = page_rows(20902)
    items = []
    kind = "buff"
    for row in rows:
        joined = " ".join(row)
        if "Debuff" in joined and len(row) <= 3:
            kind = "debuff"
            continue
        name = ""
        effect = ""
        if len(row) >= 2:
            # skip icon-only
            cand = [c for c in row if c and c not in ("图标", "名称", "效果")]
            if len(cand) >= 2:
                name, effect = cand[0], cand[1]
            elif len(cand) == 1 and "↑" in cand[0] or "↓" in (cand[0] if cand else ""):
                name = cand[0]
        if name and name not in ("Buff", "Debuff"):
            items.append({"kind": kind, "name": name, "effect": effect})
    # de-dup
    seen = set()
    uniq = []
    for it in items:
        if it["name"] in seen:
            continue
        seen.add(it["name"])
        uniq.append(it)
    return uniq


def index_rows():
    with open(CSV_INDEX, encoding="utf-8-sig") as f:
        return list(csv.DictReader(f))


def write_csv(path, rows, fields):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8-sig", newline="") as f:
        w = csv.DictWriter(f, fieldnames=fields, extrasaction="ignore")
        w.writeheader()
        for r in rows:
            w.writerow({k: r.get(k, "") for k in fields})


def write_json(path, rows):
    with open(path, "w", encoding="utf-8") as f:
        json.dump(rows, f, ensure_ascii=False, indent=2)


def copy_to_user(src):
    os.makedirs(USER_OUT, exist_ok=True)
    shutil.copy2(src, os.path.join(USER_OUT, os.path.basename(src)))


def main() -> int:
    idx = index_rows()
    chars = []
    skills = []
    cartas = []
    puppets = []

    char_mode = None
    carta_mode = None
    puppet_mode = None

    for r in idx:
        if r["section"] != "图鉴资料":
            continue
        name = (r.get("name") or "").strip()
        cid = r.get("content_id") or ""
        path = r.get("path") or ""
        if not cid:
            if name.startswith("5星天子"):
                char_mode, carta_mode, puppet_mode = "5", None, None
            elif name.startswith("4星天子"):
                char_mode, carta_mode, puppet_mode = "4", None, None
            elif name in ("暗", "火", "木", "水") and char_mode:
                pass
            elif name.startswith("人偶") or name in ("史诗", "稀有", "罕见", "常见"):
                puppet_mode = name.replace("人偶：", "") or name
                char_mode = None
                carta_mode = None
            elif name.startswith("歌牌"):
                carta_mode = "5星"
                char_mode = None
                puppet_mode = None
            elif name == "4星" and carta_mode:
                carta_mode = "4星"
            elif name == "3星" and carta_mode:
                carta_mode = "3星"
            elif name == "图鉴列表":
                char_mode = carta_mode = puppet_mode = None
            continue

        cid_i = int(cid)
        page = os.path.join(PAGES, "%d.json" % cid_i)
        if not os.path.isfile(page):
            continue

        if char_mode in ("5", "4"):
            rec = parse_character(cid_i, name, path, char_mode + "星")
            chars.append(rec)
            sk = rec.pop("_skills")
            for slot in ("auto", "tap", "slide", "drive", "leader"):
                s = sk[slot]
                if not s["text"] and not s["text_ignited"]:
                    continue
                skills.append({
                    "content_id": cid_i,
                    "name": rec["name"],
                    "rarity": rec["rarity"],
                    "element": rec["element"],
                    "role": rec["role"],
                    "slot": slot,
                    "text": s["text"],
                    "text_ignited": s["text_ignited"],
                    "dmg_min": s["dmg_min"],
                    "dmg_max": s["dmg_max"],
                    "dmg_ignited": s["dmg_ignited"],
                })
        elif carta_mode:
            cartas.append(parse_carta_page(cid_i, name, carta_mode))
        elif puppet_mode:
            puppets.append(parse_puppet(cid_i, name, puppet_mode))

    # merge screening table onto cartas
    screen = {x["name"]: x for x in parse_screening()}
    for c in cartas:
        s = screen.get(c["name"])
        if not s:
            # fuzzy: screening may miss spaces
            for k, v in screen.items():
                if k.replace(" ", "") == c["name"].replace(" ", ""):
                    s = v
                    break
        if not s:
            continue
        for k in ("rarity", "stat_pair", "element_gate", "role_gate", "mode_gate", "collab", "special"):
            if s.get(k) and (not c.get(k) or c.get(k) in ("", "通用") and s[k] not in ("",)):
                if not c.get(k):
                    c[k] = s[k]
        if s.get("special"):
            c["special"] = s["special"]
        for k in ("rarity", "stat_pair", "element_gate", "role_gate", "mode_gate", "collab"):
            if s.get(k):
                c[k] = s[k] or c[k]

    buffs = parse_buffs()

    os.makedirs(OUT, exist_ok=True)
    char_fields = [
        "content_id", "name", "rarity", "element", "role", "path",
        "cp_init", "hp_init", "atk_init", "def_init", "agl_init", "crt_init",
        "cp_max", "hp_max", "atk_max", "def_max", "agl_max", "crt_max",
        "auto_text", "tap_text", "tap_ignited", "slide_text", "slide_ignited",
        "drive_text", "drive_ignited", "leader_text",
    ]
    skill_fields = [
        "content_id", "name", "rarity", "element", "role", "slot",
        "dmg_min", "dmg_max", "dmg_ignited", "text", "text_ignited",
    ]
    carta_fields = [
        "content_id", "name", "rarity", "stat_pair", "element_gate", "role_gate",
        "mode_gate", "collab", "special",
        "plain_hp_init", "plain_atk_init", "plain_def_init", "plain_agl_init", "plain_crt_init",
        "plain_hp_max", "plain_atk_max", "plain_def_max", "plain_agl_max", "plain_crt_max",
        "flash_hp_init", "flash_atk_init", "flash_def_init", "flash_agl_init", "flash_crt_init",
        "flash_hp_max", "flash_atk_max", "flash_def_max", "flash_agl_max", "flash_crt_max",
    ]
    puppet_fields = ["content_id", "name", "element", "rarity", "tap", "slide", "drive", "leader", "lv1", "lv70"]
    buff_fields = ["kind", "name", "effect"]

    # strip internal
    char_out = [{k: c.get(k, "") for k in char_fields} for c in chars]

    write_csv(os.path.join(OUT, "characters.csv"), char_out, char_fields)
    write_csv(os.path.join(OUT, "skills.csv"), skills, skill_fields)
    write_csv(os.path.join(OUT, "soul_cartas.csv"), cartas, carta_fields)
    write_csv(os.path.join(OUT, "puppets.csv"), puppets, puppet_fields)
    write_csv(os.path.join(OUT, "buffs.csv"), buffs, buff_fields)
    write_json(os.path.join(OUT, "characters.json"), char_out)
    write_json(os.path.join(OUT, "skills.json"), skills)
    write_json(os.path.join(OUT, "soul_cartas.json"), cartas)

    n_el = sum(1 for c in chars if c.get("element"))
    n_role = sum(1 for c in chars if c.get("role"))
    n_hp = sum(1 for c in chars if c.get("hp_init"))
    n_hpmax = sum(1 for c in chars if c.get("hp_max"))
    n_tap = sum(1 for c in chars if c.get("tap_text"))
    n_tap_u = sum(1 for c in chars if c.get("tap_ignited"))
    n_c_pair = sum(1 for c in cartas if c.get("stat_pair"))
    n_c_hp = sum(1 for c in cartas if c.get("plain_hp_init") or c.get("plain_atk_init") or c.get("plain_def_init"))
    n_c_sp = sum(1 for c in cartas if c.get("special"))

    readme = """# GameKee 整理数据表

来源：https://www.gamekee.com/dc/ 已抓取的 1227 篇词条。
这些表在 `docs/reference/gamekee/tables/`，并复制了一份到 `天命之子数据/`。

| 文件 | 行数 | 说明 |
|---|---:|---|
| characters.csv | %d | 5★+4★ 天子：属性、职业、初始/满破面板、技能原文 |
| skills.csv | %d | 每人 普攻/TS/SS/DS/队长，含 MIN/MAX 与点火 U |
| soul_cartas.csv | %d | 魂之歌牌：分类、限定、特效、普卡/闪卡初始数值 |
| puppets.csv | %d | 人偶 |
| buffs.csv | %d | Buff/Debuff 名称与效果 |

## 覆盖率

- 天子 element %d/%d，role %d/%d
- 初始 HP %d/%d，满破 HP %d/%d
- TS 技能文本 %d/%d，点火 U %d/%d
- 魂卡分类 %d/%d，特效 %d/%d，至少一项基础数值 %d/%d

满破面板很多词条本身就空着。3★/2★/1★ 天子 wiki 没有独立页。装备图鉴几乎只有图没有名字，未进表。

用 Excel / WPS 打开 CSV（UTF-8 带 BOM）。
""" % (
        len(chars), len(skills), len(cartas), len(puppets), len(buffs),
        n_el, len(chars), n_role, len(chars),
        n_hp, len(chars), n_hpmax, len(chars),
        n_tap, len(chars), n_tap_u, len(chars),
        n_c_pair, len(cartas), n_c_sp, len(cartas), n_c_hp, len(cartas),
    )
    with open(os.path.join(OUT, "README.md"), "w", encoding="utf-8") as f:
        f.write(readme)

    for fn in ("characters.csv", "skills.csv", "soul_cartas.csv", "puppets.csv", "buffs.csv", "README.md"):
        copy_to_user(os.path.join(OUT, fn))

    # checks
    thor = next((c for c in chars if c["name"] == "索尔"), None)
    carta = next((c for c in cartas if "炎热夏日" in c["name"]), None)
    print(readme)
    print("CHECK 索尔", thor and thor.get("element"), thor and thor.get("role"),
          thor and thor.get("tap_text", "")[:40], "U", (thor or {}).get("tap_ignited", "")[:20])
    print("CHECK 炎热夏日", carta)
    ok = True
    if not thor or thor.get("element") != "火" or "2243" not in (thor.get("tap_text") or ""):
        print("FAIL 索尔")
        ok = False
    if not carta or carta.get("stat_pair") != "血攻":
        print("FAIL 炎热夏日")
        ok = False
    print("chars", len(chars), "skills", len(skills), "cartas", len(cartas), "puppets", len(puppets))
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
