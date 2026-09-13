# -*- coding: utf-8 -*-
import csv, json, os, re
from collections import Counter, defaultdict

base = r"F:\天命之子\天命之子数据"
out = os.path.join(base, "_layout", "game_text")
os.makedirs(out, exist_ok=True)

def nonempty(x):
    return bool(str(x or "").strip())

def load(name):
    p = os.path.join(base, name)
    with open(p, encoding="utf-8-sig", newline="") as f:
        return list(csv.DictReader(f))

kids = load("汇总表.csv")
cartas = load("魂之歌牌.csv")
pups = load("人偶.csv")
buffs = load("buffs.csv")

report = {}

skill_cols = ["auto_text","tap_text","tap_ignited","slide_text","slide_ignited","drive_text","drive_ignited","leader_text"]
id_cols = ["name","name_ocr","wiki_name","wiki_id","rarity","element","role","cv","hp","atk","def_","agl","crt"]
fill = {c: sum(1 for r in kids if nonempty(r.get(c))) for c in id_cols + skill_cols}
report["kids_n"] = len(kids)
report["kids_fill"] = fill
report["rarity"] = dict(Counter(r.get("rarity","") for r in kids))

by_rarity = {}
for star, n in report["rarity"].items():
    rows = [r for r in kids if r.get("rarity")==star]
    by_rarity[star] = {
        "n": len(rows),
        **{c: sum(1 for r in rows if nonempty(r.get(c))) for c in skill_cols + ["cv","role","element","wiki_name","hp"]}
    }
report["kids_by_rarity"] = by_rarity

# any-skill fill
any_skill = sum(1 for r in kids if any(nonempty(r.get(c)) for c in skill_cols))
full_kit = sum(1 for r in kids if all(nonempty(r.get(c)) for c in ["auto_text","tap_text","slide_text","drive_text","leader_text"]))
ign_kit = sum(1 for r in kids if all(nonempty(r.get(c)) for c in ["tap_ignited","slide_ignited","drive_ignited"]))
report["kids_any_skill"] = any_skill
report["kids_full_kit"] = full_kit
report["kids_ign_kit"] = ign_kit

# garbled heuristic: CJK + punctuation only vs mixed garbage
garbled = []
for r in kids:
    n = (r.get("name") or "").strip()
    if not n:
        garbled.append({"id": r.get("id"), "name": n, "why": "empty"})
        continue
    if "·" in n and re.search(r"\d", n):
        garbled.append({"id": r.get("id"), "name": n, "why": "dot-number"})
    elif re.search(r"[A-Za-z]{4,}", n) and re.search(r"[\u4e00-\u9fff]", n) is None:
        garbled.append({"id": r.get("id"), "name": n, "why": "latin"})

report["garbled_sample"] = garbled[:30]
report["garbled_n"] = len(garbled)

# carta
cf = {}
for c in cartas[0].keys():
    cf[c] = sum(1 for r in cartas if nonempty(r.get(c)))
report["cartas_n"] = len(cartas)
report["cartas_fill"] = {k:v for k,v in cf.items() if k in ["name","special","stat_pair","element_gate","role_gate","mode_gate","collab"] or k.endswith("_init") or k.endswith("_max")}
report["carta_rarity"] = dict(Counter(r.get("rarity","") for r in cartas))
report["carta_mode"] = dict(Counter(r.get("mode_gate","") for r in cartas))
report["carta_el"] = dict(Counter(r.get("element_gate","") for r in cartas))
report["carta_role"] = dict(Counter(r.get("role_gate","") for r in cartas))

# puppets
pf = {c: sum(1 for r in pups if nonempty(r.get(c))) for c in pups[0].keys()}
report["pups_n"] = len(pups)
report["pups_fill"] = {k: pf[k] for k in ["name","element","rarity","tap","slide","drive","leader","lv1","lv70"]}
report["pups_rarity"] = dict(Counter(r.get("rarity","") for r in pups))
report["pups_el"] = dict(Counter(r.get("element","") for r in pups))
any_p = sum(1 for r in pups if any(nonempty(r.get(c)) for c in ["tap","slide","drive","leader"]))
report["pups_any_skill"] = any_p

# buffs
report["buffs_n"] = len(buffs)
report["buffs_kind"] = dict(Counter(r.get("kind","") for r in buffs))
report["buffs_cols"] = list(buffs[0].keys())

# catalog
cat_path = r"F:\天命之子\client\Assets\Content\catalog.json"
with open(cat_path, encoding="utf-8") as f:
    cat = json.load(f)
chars = cat.get("chars", [])
skills = cat.get("skills", [])
stages = cat.get("stages", cat.get("stage") and [cat["stage"]] or [])
if isinstance(cat.get("stages"), list):
    stages = cat["stages"]
report["catalog_chars"] = len(chars)
report["catalog_playable"] = sum(1 for c in chars if not c.get("enemy"))
report["catalog_enemy"] = sum(1 for c in chars if c.get("enemy"))
report["catalog_skills"] = len(skills)
report["catalog_skill_named"] = sum(1 for s in skills if nonempty(s.get("name")))
report["catalog_stages"] = [{"id": s.get("id"), "name": s.get("name")} for s in stages]
report["catalog_char_names"] = [c.get("name") for c in chars if not c.get("enemy")]
report["catalog_skill_names_sample"] = [s.get("name") for s in skills[:20]]

# unique tokens in child skills
tokens = Counter()
for r in kids:
    for c in skill_cols:
        t = r.get(c) or ""
        for m in re.findall(r"[\u4e00-\u9fffA-Za-z0-9↑↓Ⅱ＋+\-％%]+", t):
            if len(m) >= 2:
                tokens[m] += 1
report["skill_token_top"] = tokens.most_common(80)

with open(os.path.join(out, "_stats.json"), "w", encoding="utf-8") as f:
    json.dump(report, f, ensure_ascii=False, indent=2)

print("KIDS", report["kids_n"])
print("fill", {k: f"{v}/{report['kids_n']}" for k,v in fill.items()})
print("any_skill", any_skill, "full_kit", full_kit, "ign_kit", ign_kit)
print("by_rarity")
for k,v in by_rarity.items():
    print(" ", k, v)
print("CARTAS", report["cartas_n"], {k: report["cartas_fill"][k] for k in ["name","special"]})
print("PUPPETS", report["pups_n"], report["pups_fill"])
print("BUFFS", report["buffs_n"], report["buffs_kind"])
print("CATALOG chars", report["catalog_chars"], "skills", report["catalog_skills"], "stages", len(report["catalog_stages"]))
print("garbled_n", report["garbled_n"])
print("wrote", os.path.join(out, "_stats.json"))
