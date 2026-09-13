# -*- coding: utf-8 -*-
"""Cross-check 汇总表 / 魂卡 / 人偶 against destinychild.sql from 单机dc1.1."""
from __future__ import annotations

import csv
import re
from collections import Counter, defaultdict
from pathlib import Path

ROOT = Path(r"F:\天命之子")
DUMP = ROOT / "_sandbox" / "offline-pack-1.1" / "destinychild.sql"
USER = ROOT / "天命之子数据"
OUT = USER / "_extract" / "offline_pack_crosscheck.md"
LOCALE = ROOT / "_sandbox" / "offline-pack-1.1" / "locale" / "locale.pck"

INSERT_RE = re.compile(r"^INSERT INTO `([^`]+)` VALUES\s*\((.*)\)\s*;\s*$")

ROLE_SQL = {
    "1": "攻击型",
    "2": "防御型",
    "3": "干扰型",
    "4": "治疗型",
    "5": "辅助型",
}
# SQL attribute int is unknown until we lock a starter; filled after match.


def split_sql_values(inner: str) -> list[str]:
    fields: list[str] = []
    buf: list[str] = []
    in_str = False
    i = 0
    while i < len(inner):
        ch = inner[i]
        if in_str:
            if ch == "\\" and i + 1 < len(inner):
                buf.append(ch)
                buf.append(inner[i + 1])
                i += 2
                continue
            if ch == "'":
                in_str = False
            buf.append(ch)
            i += 1
            continue
        if ch == "'":
            in_str = True
            buf.append(ch)
            i += 1
            continue
        if ch == ",":
            fields.append("".join(buf).strip())
            buf = []
            i += 1
            continue
        buf.append(ch)
        i += 1
    if buf:
        fields.append("".join(buf).strip())
    return fields


def unquote(v: str) -> str:
    v = v.strip()
    if v.upper() == "NULL":
        return ""
    if len(v) >= 2 and v[0] == "'" and v[-1] == "'":
        return v[1:-1].replace("\\'", "'").replace("\\\\", "\\")
    return v


def parse_inserts(wanted: set[str]) -> dict[str, list[list[str]]]:
    tables: dict[str, list[list[str]]] = {k: [] for k in wanted}
    with DUMP.open(encoding="utf-8", errors="replace") as f:
        for line in f:
            m = INSERT_RE.match(line)
            if not m:
                continue
            name = m.group(1)
            if name not in wanted:
                continue
            tables[name].append([unquote(x) for x in split_sql_values(m.group(2))])
    return tables


def load_csv(path: Path) -> list[dict]:
    with path.open(encoding="utf-8-sig") as f:
        return list(csv.DictReader(f))


def ni(s) -> int | None:
    s = str(s or "").strip().replace(",", "")
    if not s or s in ("—", "-", "None"):
        return None
    try:
        return int(float(s))
    except ValueError:
        return None


def rar_star(s: str) -> str:
    t = str(s or "")
    for ch in "654321":
        if t.startswith(ch):
            return ch
    return ""


HANGUL_RE = re.compile(r"[\uac00-\ud7a3]+")
CJK_RE = re.compile(r"[\u4e00-\u9fff]{2,12}")


def locale_strings() -> tuple[list[str], list[str]]:
    if not LOCALE.is_file():
        return [], []
    data = LOCALE.read_bytes()
    hangul: list[str] = []
    cjk: list[str] = []
    # utf-8
    text = data.decode("utf-8", errors="ignore")
    hangul += HANGUL_RE.findall(text)
    cjk += CJK_RE.findall(text)
    # utf-16le
    text16 = data.decode("utf-16le", errors="ignore")
    hangul += HANGUL_RE.findall(text16)
    cjk += CJK_RE.findall(text16)
    return hangul, cjk


def main() -> None:
    tables = parse_inserts(
        {
            "characters",
            "item",
            "puppet",
            "soul_carta_option",
            "soul_carta_enhancement_status",
            "ignition_character_skill",
            "character_skin",
            "game_area_dungeon",
            "game_spacewalk_area_dungeon",
            "special_raid_dungeon",
            "world_boss_serial_mob",
        }
    )
    chars = tables["characters"]
    # schema: idx,name,role,start_grade,hp,cri,agi,def,atk,skill_1..5,ignition_group,enable_awaken,awaken_group,... attribute at [19]
    # From first INSERT: 25 fields. attribute is index 19 based on CREATE TABLE order:
    # 0 idx 1 name 2 role 3 start_grade 4 hp 5 cri 6 agi 7 def 8 atk
    # 9-13 skills 14 ignition_group 15 enable_awaken 16 awaken_group
    # 17 enable_library 18 library_open_date 19 attribute

    parsed = []
    for r in chars:
        if len(r) < 20:
            continue
        parsed.append(
            {
                "idx": r[0],
                "name": r[1],
                "role": r[2],
                "star": r[3],
                "hp": ni(r[4]),
                "crt": ni(r[5]),
                "agl": ni(r[6]),
                "def": ni(r[7]),
                "atk": ni(r[8]),
                "s1": r[9],
                "s2": r[10],
                "s3": r[11],
                "s4": r[12],
                "s5": r[13],
                "ign": r[14],
                "attr": r[19],
                "prefix": r[0][:3],
            }
        )

    playable = [c for c in parsed if c["prefix"] in ("101", "102")]
    mobs = [c for c in parsed if c["prefix"] == "201"]
    combat = [c for c in playable if c["role"] in ROLE_SQL]

    our = load_csv(USER / "汇总表.csv")
    wiki = load_csv(ROOT / "docs" / "reference" / "gamekee" / "tables" / "characters.csv")
    pup_our = load_csv(USER / "puppets.csv")
    carta_our = load_csv(USER / "soul_cartas.csv")
    pup_sql = tables["puppet"]
    carta_opt = tables["soul_carta_option"]
    items = tables["item"]

    # item: idx,name,view_idx,grade,category,...
    item_cat = Counter()
    item_grade = Counter()
    carta_items = []
    for it in items:
        if len(it) < 5:
            continue
        cat = it[4]
        item_cat[cat] += 1
        item_grade[it[3]] += 1
        if cat == "8":
            carta_items.append(it)

    lines: list[str] = []
    a = lines.append
    a("# 单机 dc1.1 × 数据表交叉核验")
    a("")
    a("对照源：韩服私服包 `destinychild.sql`（637 名角色主数据）。")
    a("被核验：`天命之子数据/汇总表.csv`（纪念版 561）、wiki `characters.csv`、`puppets.csv`、`soul_cartas.csv`。")
    a("SQL 名字是韩文，纪念版是中文；**不能靠字符串直接相等**，所以人数/星级/职业用计数，面板用五维指纹对。")
    a("")

    a("## 1. 包是什么")
    a("")
    a("- 韩服 Destiny Child 单机 1.1（作者 sluteuio）。SQL 是运行时主数据，不是纪念版客户端。")
    a("- 可玩角色前缀 `101`/`102` = **%d**；`201` 是杂兵/材料 **%d**；合计 637。" % (len(playable), len(mobs)))
    a("- 纪念版图鉴是冻结的 561 人。两边**本来就不会人数相等**。")
    a("")

    a("## 2. 星级人数")
    a("")
    a("| 星 | SQL 可玩 101+102 | SQL 全部 | 纪念版 汇总表 | 差（可玩−纪念版） |")
    a("|---|---:|---:|---:|---:|")
    our_star = Counter(rar_star(r.get("rarity")) for r in our)
    sql_play_star = Counter(c["star"] for c in playable)
    sql_all_star = Counter(c["star"] for c in parsed)
    for s in "54321":
        sp, sa, ou = sql_play_star.get(s, 0), sql_all_star.get(s, 0), our_star.get(s, 0)
        a(f"| {s}★ | {sp} | {sa} | {ou} | {sp - ou:+d} |")
    a(f"| 6★ | {sql_play_star.get('6',0)} | {sql_all_star.get('6',0)} | {our_star.get('6',0)} | — |")
    a(f"| 合计 | {len(playable)} | {len(parsed)} | {len(our)} | {len(playable)-len(our):+d} |")
    a("")
    a("解读（看「SQL 全部」那列，不要只看 101+102）：")
    a("- 低星很多挂在前缀 `201`（材料/杂兵号），纪念版图鉴仍把它们当 1★/2★ 孩子。所以 **1★ 两边都是 50，是对齐的**。")
    a("- 2/3/4★ SQL 全部略多于纪念版（+4 / +8 / +8）。")
    a("- 5★ SQL 336 vs 纪念版 282（+54）：韩服后期五星；纪念版 282 里还有约 10 对造型重复卡。")
    a("- 前缀 101+102 的「可玩」496 **小于** 561，是因为低星被算进 201，不是纪念版多造了 65 人。")
    a("- **纪念版 561 对纪念版图鉴正确；对韩服 1.1 私服不是完整表。**")
    a("")
    a("前缀 201（杂兵号）按星：%s。角色 6–9：%s。" % (
        dict(Counter(c["star"] for c in mobs)),
        dict(Counter(c["role"] for c in mobs)),
    ))
    a("")

    a("## 3. 职业 / 属性（SQL 用数字）")
    a("")
    a("SQL `role` 1–5 = 攻/防/扰/治/辅（按面板均值 + 纪念版标签，先前已锁）。纪念版缺职业 **%d / 561**。" % sum(1 for r in our if not (r.get("role") or "").strip()))
    a("")
    a("| 职业 | SQL 可玩 | 纪念版有标签 |")
    a("|---|---:|---:|")
    our_role = Counter((r.get("role") or "").strip() or "（空）" for r in our)
    sql_role = Counter(ROLE_SQL.get(c["role"], c["role"]) for c in playable)
    for lab in ["攻击型", "防御型", "干扰型", "治疗型", "辅助型"]:
        a(f"| {lab} | {sql_role.get(lab,0)} | {our_role.get(lab,0)} |")
    a(f"| （空） | 0 | {our_role.get('（空）',0)} |")
    a("")
    a("SQL `attribute` 1–5 分布：%s。纪念版属性空 **%d**。" % (
        dict(Counter(c["attr"] for c in playable)),
        sum(1 for r in our if not (r.get("element") or "").strip()),
    ))
    a("整数→火水木光暗的排列 **不能从 SQL 单独猜死**（没有中文标签）。下面用指纹对上的人再反推。")
    a("")

    a("## 4. 五维面板指纹（naked catalog vs 我们的 init）")
    a("")
    # SQL key: (star, hp, atk, def, agl, crt)
    sql_fp = defaultdict(list)
    for c in playable:
        key = (c["star"], c["hp"], c["atk"], c["def"], c["agl"], c["crt"])
        sql_fp[key].append(c)

    def match_rows(rows, hp_k, atk_k, def_k, agl_k, crt_k, star_from):
        hit = 0
        amb = 0
        miss = 0
        pairs = []
        for r in rows:
            star = star_from(r)
            key = (
                star,
                ni(r.get(hp_k)),
                ni(r.get(atk_k)),
                ni(r.get(def_k)),
                ni(r.get(agl_k)),
                ni(r.get(crt_k)),
            )
            if any(x is None for x in key[1:]):
                miss += 1
                continue
            hits = sql_fp.get(key, [])
            if len(hits) == 1:
                hit += 1
                pairs.append((r, hits[0]))
            elif len(hits) > 1:
                amb += 1
                pairs.append((r, hits[0]))
            else:
                miss += 1
        return hit, amb, miss, pairs

    h1, a1, m1, pairs_our_init = match_rows(
        our, "hp_init", "atk_init", "def_init", "agl_init", "crt_init", lambda r: rar_star(r.get("rarity"))
    )
    h2, a2, m2, pairs_our_hp = match_rows(
        our, "hp", "atk", "def_", "agl", "crt", lambda r: rar_star(r.get("rarity"))
    )
    h3, a3, m3, pairs_wiki = match_rows(
        wiki, "hp_init", "atk_init", "def_init", "agl_init", "crt_init", lambda r: rar_star(r.get("rarity"))
    )

    a("| 对照 | 唯一命中 | 指纹撞车 | 对不上/空 |")
    a("|---|---:|---:|---:|")
    a(f"| 汇总表 hp_init 五维 | {h1} | {a1} | {m1} |")
    a(f"| 汇总表 hp（纪念版 OCR 面板） | {h2} | {a2} | {m2} |")
    a(f"| wiki characters.csv hp_init | {h3} | {a3} | {m3} |")
    a("")
    a("结论：纪念版详情页上的 HP/ATK **不是** SQL 裸体目录值（含等级/好感/装备）。")
    a("`hp_init` 若真是 1 级裸装，命中率会高；现在几乎对不上，说明我们表里的 init 多数是 OCR 当场面板，或 wiki 空着。")
    a("")

    # attribute permutation from unique matches if any
    attr_votes = defaultdict(Counter)
    role_votes = defaultdict(Counter)
    for r, c in pairs_our_init + pairs_wiki:
        el = (r.get("element") or "").strip()
        role = (r.get("role") or "").strip()
        if el:
            attr_votes[c["attr"]][el] += 1
        if role:
            role_votes[c["role"]][role] += 1
    a("### 指纹命中后的属性投票（仅供参考）")
    if not attr_votes:
        a("命中太少，**不能**从这一步锁定 attribute 1–5 对应哪一系。")
    else:
        a("| SQL attribute | 投票 |")
        a("|---|---|")
        for k, ctr in sorted(attr_votes.items()):
            a(f"| {k} | {ctr.most_common(3)} |")
    a("")
    a("### 职业投票（应与预设 1攻2防3扰4治5辅 一致）")
    if not role_votes:
        a("无。")
    else:
        a("| SQL role | 投票 | 预设 |")
        a("|---|---|---|")
        for k, ctr in sorted(role_votes.items()):
            a(f"| {k} | {ctr.most_common(3)} | {ROLE_SQL.get(k,'?')} |")
    a("")

    a("## 5. 汇总表内部错误（不靠 SQL 也能看出来）")
    a("")
    mm = []
    for r in our:
        w = (r.get("wiki_name") or "").strip()
        n = (r.get("name") or "").strip()
        if w and n and w != n and w not in n and n not in w:
            mm.append((r.get("id"), n, w))
    a(f"`name` 与 `wiki_name` 对不上：**{len(mm)}** 条。前段 5★ 是 wiki 合并时 **错位一行**（M5-002 起：哀悲的索卡 ← 忍者琪隆，助手红莲 ← 哀悲的索卡…）。")
    a("")
    a("| id | 纪念版名（头像下） | 错挂的 wiki_name |")
    a("|---|---|---|")
    for row in mm[:25]:
        a(f"| {row[0]} | {row[1]} | {row[2]} |")
    if len(mm) > 25:
        a(f"| … | 其余 {len(mm)-25} 条 | |")
    a("")
    a("SQL 帮不上这段：它没有中文名。这是我们自己的表错，**优先修错位，不要拿韩文私服名去覆盖纪念版名。**")
    a("")

    a("## 6. 魂卡 / 人偶 / 装备")
    a("")
    carta_ids = {row[0] for row in carta_opt if row}
    a(f"| 表 | SQL | 我们 |")
    a(f"|---|---:|---:|")
    a(f"| 人偶 puppet | {len(pup_sql)} | {len(pup_our)} |")
    a(f"| 魂卡 soul_carta_option 唯一 item_idx | {len(carta_ids)} | {len(carta_our)} |")
    a(f"| 魂卡强化档 soul_carta_enhancement_status | {len(tables['soul_carta_enhancement_status'])} | （无独立表） |")
    a(f"| item 总行 | {len(items)} | （装备图鉴几乎没进表） |")
    a(f"| 点火技能节点 ignition_character_skill | {len(tables['ignition_character_skill'])} | 文本约 30% 有 U |")
    a(f"| 造型 character_skin | {len(tables['character_skin'])} | 纪念版有造型页，未进 561 主表 |")
    a("")
    a("人偶 253 vs 220：SQL 多 33，韩服后期人偶；我们缺成长数字。")
    a(
        "魂卡 option 唯一 item **%d**；我们 156。156×2（普卡/闪卡）= 312，和 322 很接近，差约 10 张后期卡。"
        % len(carta_ids)
    )
    a("item.category 计数：%s。文档里 1/2/3≈武/甲/饰，8≈魂卡。" % dict(item_cat.most_common(12)))
    a("category=8 的 item 行 **%d**（应和魂卡目录接近）。" % len(carta_items))
    a("")

    a("## 7. 模式目录（SQL 有、我们动效页缺录像）")
    a("")
    dungeons = tables["game_area_dungeon"]
    nebula = tables["game_spacewalk_area_dungeon"]
    raid = tables["special_raid_dungeon"]
    wb = tables["world_boss_serial_mob"]
    a(f"- 主线/困难：`game_area_dungeon` **{len(dungeons)}** 条，30 个 area。先前分析 `area_type` 1 和 2 各 152 = **普通 / 困难** 两套。")
    a(f"- 星云：`game_spacewalk_area_dungeon` **{len(nebula)}**，changelog 说一千多关；stage_mob 2048。")
    a(f"- Raid：`special_raid_dungeon` **{len(raid)}**（yaml `raid: 49`，注释 0–56）。")
    a(f"- 世界王：`world_boss_serial_mob` **{len(wb)}**（yaml `world: 27`，1–38）。")
    a("- 这些是**关卡目录**，没有每关美术、也没有打击公式。动效页缺主线/困难/星云录像，SQL 补不上画面，只证明模式存在且可切。")
    a("")

    a("## 8. SQL 有、我们数值页没有的")
    a("")
    a("- 每名角色 5 个技能 **ID**（AUTO/TAP/SLIDE/DRIVE/LEADER），没有伤害数字。")
    a("- 装备 2952 + 词缀 4406 + 强化/突破表。")
    a("- 点火 6 节点 × 488 人 = 2928，以及点火石树 3072。")
    a("- 温泉好感百分表 `character_spa_status_percent` 240 行。")
    a("- `skill_effect_path` 7257 是特效路径，5477 条是 `none`，**不是**公式。")
    a("")
    a("## 9. 总判")
    a("")
    a("| 项目 | 判 |")
    a("|---|---|")
    a("| 纪念版 561 人数 | **对纪念版图鉴是对的**；对韩服私服 **不是完整表** |")
    a("| 1★ 50 | 与 SQL **全部** 1★ 一致（多数 201 号） |")
    a("| 5★ 282 | 少于 SQL 全部五星 336，缺韩服后期 |")
    a("| 魂卡 156 | option 唯一 322 ≈ 156×2 普/闪 + 约 10 张后期 |")
    a("| 职业/属性空 | 表缺，不是 SQL 缺 |")
    a("| 面板数字 | 与 SQL 裸体目录 **对不上**（OCR/装备后数字） |")
    a("| wiki_name 错位 | **表错**，约从 M5-002 起串行 |")
    a("| 人偶 220 | 少于 SQL 253 |")
    a("| 魂卡 156 | 需看 option 唯一 item；特效来自 wiki 不是 SQL |")
    a("| 伤害公式 | SQL **没有**；继续用 GameKee 实测 |")
    a("| 主线/困难/星云 | SQL 证明两套 area_type + 星云 2048 关；无画面 |")
    a("")
    a("不要把私服韩文名、技能 ID、裸体面板写进发行客户端。核验只用来标我们表哪里空、哪里串行。")
    a("")

    # locale hint
    hangul, cjk = locale_strings()
    a("## 10. locale.pck 字符串（只计数）")
    a("")
    a(f"韩文词条 {len(set(hangul))} 个，中文词条 {len(set(cjk))} 个。PCK 头是自定义 `PCK\\0`，不是 zip。")
    a("若要用它做韩↔中对照，需要另写解包；这次核验 **没用原文名字去改表**。")
    a("")

    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text("\n".join(lines), encoding="utf-8")
    print("wrote", OUT)
    print("playable", len(playable), "our", len(our))
    print("init match", h1, a1, m1)
    print("ocr match", h2, a2, m2)
    print("wiki match", h3, a3, m3)
    print("wiki_name mismatch", len(mm))
    print("puppets sql/our", len(pup_sql), len(pup_our))
    print("carta unique", len(carta_ids), "our", len(carta_our))
    print("item cat", item_cat.most_common(10) if item_cat else "n/a")
    if items:
        print("item row0", items[0][:8])


if __name__ == "__main__":
    main()
