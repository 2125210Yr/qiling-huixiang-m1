# Offline pack 1.1 — unit combat & growth (SQL grammar)

Source: `_sandbox/offline-pack-1.1/destinychild.sql` + `schema-only.sql` (gitignored sandbox copies of the 1.1 dump).  
Cross-check: `DATA_MODEL_FROM_REFERENCE.md`, Memorial UI, Resonance `client/Assets/Scripts/Resonance.Battle/*`.

**This file is reference only.** Do not copy original names, flavor, art, skill IDs, or numeric rows into `F:\Resonance\client`. Do not ship this dump. At most 2–3 original proper names appear below as grammar examples; product content uses `name_key` / `C001`…`C012` only.

Pack inventory: `00-pack-inventory.md`. Master tables are populated; all `user_*` tables are **empty schemas** (0 INSERT rows).

---

## 0. What this dump can and cannot prove

| Can prove | Cannot prove |
|---|---|
| Catalog columns, enum distributions, ID **grammar** | Damage formulas (no skill-effect magnitude table) |
| Which skill *slot* an ID belongs to | TAP/SLIDE/DRIVE coefficients, targeting, hit counts |
| Equipment slot taxonomy + enhance/over-limit **tables exist** | Exact enhance math in combat |
| Save-instance field list for growth | Runtime save values (empty `user_*`) |
| Ignition **node schedule** (6 nodes → redstar 1–6) | Which core stat a given hex node grants (that is `ignition_item_status_tree`, P3) |

`skill_effect_path` is Unity **VFX path** (`sfx_atk_normal`, `drive/…`, `buff/…`, or `none`). **5477 / 7257** paths are the literal string `none`. It is not a formula table. Skill numbers live in the Java jar / pck — **do not decompile**.

---

## 1. `characters` (637 rows)

### 1.1 Columns (catalog)

| Column | Role in grammar |
|---|---|
| `idx` | Stable id. Prefix **101** = 170 early playables, **102** = 326 later playables, **201** = 141 mobs/fodder (all role 6–9 live here) |
| `name` | Original. **Do not copy** |
| `role` | 1–9. Combat roster is 1–5 |
| `start_grade` | Native star 1–6 |
| `hp` `atk` `def` `agi` `cri` | Five combat stats (dump order is hp, cri, agi, def, atk) |
| `skill_1` … `skill_5` | AUTO / TAP / SLIDE / DRIVE / LEADER ids (see §2) |
| `ignition_group` | `311` / `411` / `511` by native 3/4/5★; `0` = no hex |
| `enable_awaken` | `2` = 598, `0` = 39. Flag only |
| `awaken_group` | Correlates with native star (`501`≈5★, `401`≈4★, `301`≈3★). **Not** a filled awaken catalog — `character_awakens` has **0 rows** |
| `attribute` | 1–5, five elements, nearly balanced |
| `peerage` | **Always `1`**. Ignore |
| `enable_library` / `library_open_date` / `bbs_idx` / coin buy fields | Presentation / shop. Out of MVP |

Playable pool **101+102 = 496**, all `role` 1–5. Prefix **201** holds every role 6–9 row.

### 1.2 `role` 1–9

Measured by **role id** (not sorted by count):

| `role` | n | Mean HP / ATK / DEF / AGI / CRT | Hypothesis (Memorial labels, not product copy) |
|---:|---:|---|---|
| 1 | **179** | 1155 / **942** / 475 / 583 / 517 | 攻擊型 → product `Attacker` |
| 2 | **114** | 1428 / 405 / **822** / 442 / 328 | 防禦型 → `Defender` |
| 3 | **56** | 1351 / 521 / 678 / 640 / 520 | 干擾型 → `Debuffer` (smallest combat role) |
| 4 | **115** | 1429 / 807 / 622 / 714 / 622 | 治癒型 → `Healer` |
| 5 | **135** | **1637** / 732 / 708 / **894** / 708 | 輔助型 → `Supporter` |
| 6 | 25 | low, flat | fodder / material (prefix 201 only) |
| 7 | 5 | low | mob special |
| 8 | 5 | low | mob special |
| 9 | 3 | high outlier | mob/boss-like |

Descending counts **179 / 135 / 115 / 114 / 56 / 25 / 5 / 5 / 3** = roles **1, 5, 4, 2, 3, 6, 7, 8, 9**. Same census, different sort.

Resonance enum is 0-based (`Attacker=0` … `Supporter=4`). SQL is 1-based. Map `sql_role → sql_role - 1`. Never store original role strings in client.

Roles 6–9 **fail** the skill-id grammar in §2 (38/38 grammar misses). Combat slice uses 1–5 only.

### 1.3 `start_grade`

| Native star | n | Notes |
|---:|---:|---|
| 1 | 50 | `ignition_group=0` |
| 2 | 57 | `ignition_group=0` |
| 3 | 107 | 99 × group `311`, 8 with no hex |
| 4 | 85 | 77 × `411`, 8 with no hex |
| 5 | **336** | 312 × `511`, 24 with no hex |
| 6 | 2 | no hex; one is prefix 101, one is 201 |

**336 / 637 are 5★ native.** Mean catalog stats scale hard with star (3★ mean ATK ~319 vs 5★ ~1041). **Do not copy these means** into Resonance — they are original balance.

### 1.4 `attribute` 1–5

| attribute | n |
|---:|---:|
| 1 | 132 |
| 2 | 139 |
| 3 | 122 |
| 4 | 127 |
| 5 | 117 |

Balanced (~117–139). Mean stats do **not** differ by attribute (all ~700 ATK). Attribute is an element tag, not a stat archetype.

Product already uses `Fire Water Wood Light Dark`. This dump does **not** contain a labeled map of int→element. Do not invent the permutation from SQL ints; keep Resonance’s existing enum and original roster tags.

### 1.5 Five stats

Catalog ranges (including one 6★ row and one `hp=0` protagonist-like row):

| Stat | min | max (catalog) |
|---|---:|---:|
| HP | 0 | 2856 |
| ATK | 79 | 1693 |
| DEF | 79 | 1428 |
| AGI | 59 | 1491 |
| CRT | 59 | 1332 |

These are **naked catalog bases**, not Memorial display totals (those include level / affection / gear / ignition). Memorial four-way split (Child / 好感度 / 裝備 / 裝備鑲嵌) is a **runtime assemble**, not extra `characters` columns.

Resonance already stores the same five fields as `Hp Atk Def Agl Crt` on `CharacterDef`. Keep using **invented** C001–C012 numbers.

### 1.6 Skill slots on the row

636 / 637 rows have all five skill ids non-zero. The one all-zero row is a 6★ prefix-101 stub (no kit).

`ignition_group ≠ 0` on **488** rows = exactly the population of `ignition_character_skill` (488 × 6 nodes = 2928).

---

## 2. Skill ID grammar

### 2.1 Slot = first digit (not Fever)

| Slot on `characters` | First digit | Prefix examples | Clean-room `SkillType` | Memorial label |
|---|---|---|---|---|
| `skill_1` | **1** | `11xxxxxx` (636/637) | `AUTO` | DEFAULT |
| `skill_2` | **2** | `21`–`25` | `TAP` | NORMAL |
| `skill_3` | **3** | `31`–`35` | `SLIDE` | SLIDE |
| `skill_4` | **4** | `41`–`45` | `DRIVE` | DRIVE |
| `skill_5` | **5** | `51`–`55` | `LEADER` | LEADER BUFF |

**Fever is not `skill_5`.** Fever is a **party runtime** (duration, hit cap, TAP-scale) with no per-unit skill id in this dump. Prefix `53` / `54` / `55` are **5th-slot (LEADER)** kits at native 3/4/5★, not Fever.

Second digit of TAP/SLIDE/DRIVE/LEADER = **`start_grade`**:

| Native | TAP p2 | SLIDE p2 | DRIVE p2 | LEADER p2 | n (matches grade table) |
|---|---|---|---|---|---:|
| 1★ | 21 | 31 | 41 | 51 | 50 |
| 2★ | 22 | 32 | 42 | 52 | 57 |
| 3★ | 23 | 33 | 43 | 53 | 107 |
| 4★ | 24 | 34 | 44 | 54 | 84–85 |
| 5★ | **25** | **35** | **45** | **55** | **338** (336 5★ + two extra kits) |

AUTO is always prefix **`11`**, not `13`/`15`. Grade is encoded later in the AUTO id, not in digit 2.

### 2.2 Digits 3–4: attribute × role

For TAP/SLIDE/DRIVE/LEADER on **roles 1–5**:

```
[slot 1–5][native_star 1–5][attribute 1–5][role 1–5][rest]
```

Match rate: **598 / 598** combat-role rows with a non-zero kit (636 non-zero kits − 38 role-6–9 rows).

AUTO: `11` + `attribute` as digits 3–4 (e.g. attr 1 → `1101….`, attr 5 → `1105….`). Match rate **634 / 636**.

Numeric examples only (no original names):

| `idx` | role | star | attr | AUTO | TAP | SLIDE | DRIVE | LEADER |
|---|---:|---:|---:|---|---|---|---|---|
| 10100002 | 2 | 3 | 5 | `11050201` | `23520030` | `33521000` | `43521000` | `53520010` |
| 10100003 | 5 | 3 | 1 | `11010201` | `23150030` | `33151000` | `43151000` | `53150010` |

`11010201` / `11050201` are the AUTO samples from the pack prompt: both `11`, third-nibble = attribute.

**Product: do not reuse these ints.** Resonance already uses `C001_auto` / `_tap` / `_slide` / `_drive` / `_leader`.

### 2.3 `skill_effect_path` (7257) vs prefixes 25/35/45/55

| id prefix | n | Why huge |
|---|---:|---|
| 35 | 937 | 5★ SLIDE + ignited variants (extra trailing digit) |
| 25 | 858 | 5★ TAP + ignited |
| 45 | 715 | 5★ DRIVE + ignited; many real `drive/…` paths |
| 55 | 347 | 5★ LEADER (fewer unique VFX) |
| 23/24/33/34/43/44/53/54 | 150–202 each | 3★/4★ kits + ignited |
| 11 | 45 | AUTO; almost all `sfx_atk_normal` |
| 8x | ~1290 | non-child VFX bank (not `characters.skill_*`) |

Path kinds: `none` 5477, `drive/` 940, `sfx_*` 453, `buff/` 387. Cross-check: ignited ids such as `251100102` sit in this table beside base `25110010`. Still **paths**, not formulas.

### 2.4 `ignition_character_skill` (2928 = 488 × 6)

| `ignition_level` | `redstar` | n | What the row does |
|---:|---:|---:|---|
| 1 | 1 | 488 | Copies **all five** base skill ids |
| 2 | 2 | 488 | Swap **one** slot (majority TAP: 366) |
| 5 | 3 | 488 | Swap one slot (majority SLIDE: 333) |
| 8 | 4 | 488 | Split DRIVE 164 / LEADER 165 |
| 11 | 5 | 488 | Majority DRIVE (262) |
| 12 | 6 | 488 | Majority SLIDE again (344) |

- **AUTO (`skill_1`) is never swapped after node 1** (`auto_swapped_after_l1 = 0`).
- Non-zero post-L1 ids: **2440 / 2440** equal `str(base_id) + str(redstar)` (example: TAP `23520030` → `235200302` at redstar 2).
- Node → which slot is **per character**, not a global TAP-then-SLIDE script. Majority pattern ≠ hard rule.

Product: Resonance already has `SkillDef.IsIgnitedVariant` + `BaseSkillId`. For a **12-unit growth slice, leave them unused.** Keep `UnitProgress.Ignition` as the existing **stat multiplier only** (`Growth.Apply`). Do not import 2928 swap rows.

`ignition_item_status_tree`: 3072 rows = **256 trees × 12 levels**, `core_slot` 1/2/3. Hex *stat* cores, not skill ids. P3.

---

## 3. Equipment

### 3.1 `item` (2952)

| `category` | n | `view_idx` prefix | Hypothesis | Product slot |
|---|---:|---|---|---|
| **1** | **589** | `w*` 582 | Weapon | `Weapon` |
| **2** | **539** | `a*` 535 | Armor | `Armor` |
| **3** | **593** | `j*` 459 (also `a*`/`e*`) | Accessory | `Accessory` |
| **8** | **316** | `p*` 306 | Soul carta / build card | `Relic` (P2 — out of small slice) |
| 101001 | 253 | `y*` 253 | Puppet item shell (= `puppet` count) | out |
| 4 | 120 | `s*` 119 | Skin-like items | wardrobe, not gear |
| other | rest | materials / tickets | economy | out |

Prompt ballpark “1/2/3 ~589–593 each” is **wrong for armor**: category 2 is **539**, not ~590. Weapon 589 and acc 593 match.

`type = 1` → **1770** rows ≈ weapon+armor+acc (1721) plus a handful of misc. Category 8 is `type = 2` (316/316).

Grade on gear is 5★-heavy (weapon 496/589 are grade 5). Three GM/debug weapons use sentinel stats `999999` / `9999999` — ignore; they are why naive category-1 **means** are garbage. Medians (sane items): weapon ATK p50 **727**, p90 **1215**; armor HP p50 **174**; carta HP p50 **767**. **Do not copy these.**

Memorial UI shows **four circles**: 武器 / 防具 / 裝飾品 / 魂之歌碑. That is 3 combat slots + 1 carta, not six runes.

### 3.2 Enhance / over-limit / affix tables

| Table | Rows | Grammar |
|---|---:|---|
| `item_enhancement_status` | **60** | 30 `type=1` + 30 `type=2`. Columns `enhancement_1`…`enhancement_15` = bonus at +1…+15. Sample group `101`: +5, +10, … +75 (linear 5×N) |
| `item_over_limit_status` | **17** | Four groups. Group `5` is 5 steps (values 150,300,450,600,750). Items point here via `over_limit_status_idx` (298 items use group `5`; 2630 items use `0`) |
| `item_option` | **4406** | Affix pool. `type` 14 = 4244, 16 = 162. `skill_type` 1/2/3/4 = 348/498/564/354 → TAP/SLIDE/DRIVE/LEADER-gated lines; `0` = 2606 generic. `value_type` 1 vs 2 = flat vs percent-ish |
| `soul_carta_enhancement_status` | 170 | 4 groups; enhance cap 30 / 40 / **50** |
| `soul_carta_option` | 1874 | 316 cartas × over_limit 0–5 (296 still present at +5) |

`user_item` instance fields (empty in dump, schema only): `enhancement`, `enhancement_point`, `over_limit`, `plus_option_1/2/3`, `equip_character_uid`. Memorial item cards showing SSS +15 and three option bars match this schema.

**Small slice: do not port 4406 affixes or 15-step enhance tables.** One flat `GearDef` per slot is enough (Resonance already has four stub defs).

### 3.3 `user_character` gear slots vs Memorial 4 circles

| Save column | Flag | Notes |
|---|---|---|
| `weapon` | (always open) | Slot 0 |
| `armor` | (always open) | Slot 1 |
| `acc_1` | `is_open_acc_1` | Slot 2 — the Memorial accessory circle |
| `acc_2` / `acc_3` | `is_open_acc_2` / `is_open_acc_3` | Extra acc unlocks. **Not** on the 4-circle detail UI |
| `soul_carta` | `is_open_soul_carta` | Slot 3 — Memorial 魂之歌碑 |

`user_equip_bookmark` stores weapon/armor/acc_1/soul_carta presets (no acc_2/3).

Product 12-unit slice: **Weapon / Armor / Accessory** only. Keep the fourth Resonance slot (`残章` / `Gear3`) as a **stub or empty** — do not fill it with original carta stats. Do not implement acc_2/3.

---

## 4. Growth instance (`user_character` schema)

Zero rows in the dump. Field list is the save grammar.

### 4.1 Combat-relevant

| SQL column | Clean-room name | Small 12-unit slice |
|---|---|---|
| `uid` | `instance_id` | yes (or derive from `def_id` if 1 copy per def) |
| `idx` | `def_id` | `C001`…`C012` |
| `view_idx` | `equipped_skin_id` | stub skins already exist |
| `spa_view_idx` | — | **out** (spa) |
| `grade` | `star` (display, 3–6) | yes, vs catalog `native_star` |
| `level` / `exp` | `level` / `exp` | yes, cap 60 |
| `over_limit` | `uncap` | yes, +0–+6 (`UncapMax=6` already on `CharacterDef`) |
| `attraction_level` / `attraction_exp` | `affection_rank` / `affection_points` | optional thin %; **out** if slice stays combat-first |
| `bathing_hour` | — | **out** |
| `main_awaken`, `path_200_awaken`…`path_500_awaken` | — | **out** (master `character_awakens` is empty anyway) |
| `skill_1_level` `skill_2_level` `skill_3_level` | `skill_lv_tap` `skill_lv_slide` `skill_lv_drive` | freeze at max **or** 1–10 if you want a slider; only **three** level fields → AUTO + LEADER are not independently leveled |
| `weapon` `armor` `acc_1` (`acc_2` `acc_3`) `soul_carta` | `slot_weapon` `slot_armor` `slot_acc` (`slot_relic` P2) | 3 slots |
| `is_open_*` | unlock flags | freeze open |
| `underground_hp_per` | — | **out** (P1 dungeon inherit) |
| `is_exploration` | — | **out** |
| `is_spa_enter` / `is_spa_view_open` | — | **out** |
| `protect` | lock-from-sacrifice | **out** (no fodder eat) |

Hypothesis: instance `skill_1/2/3_level` = TAP / SLIDE / DRIVE. Catalog `skill_1` is AUTO (unleveled). Memorial “LV 10/10” + “TIER 7” maps to skill level and uncap-gated tier (`DATA_MODEL` rebuild: +2 cap per uncap), not a fourth SQL column.

Related empty schemas:

- `user_character_ignition` — per-core inserts (`ignition_slot`, `ignition_core`, `ignition_level`). Slice: one int `Ignition` 0–12.
- `user_character_skill_reserve` — five `reserve_n_skill_type` (Memorial T/S/E). **P1, out.**
- `user_character_awakens` — path copy. Out.
- `user_party` — 5 `uid` + `leader_uid`. Resonance already has `SaveBlob.PartyIds[5]` + `LeaderSlot`.

### 4.2 Affection table `character_spa_status_percent` (240 rows)

Caps match Memorial heart maxima **60 / 80 / 100**:

| `start_grade` | rows | max `attraction_level` |
|---:|---:|---:|
| 3 | 60 | **60** |
| 4 | 80 | **80** |
| 5 | 100 | **100** |

Values are five equal (or HP-biased at the top) integers per level — a **percent-ish bonus** keyed by native star. Lower native star gets a **steeper** table (3★ compensates smaller bases).

**Do not copy any cell into client.** If the 12-unit slice wants affection at all, invent a 5-point curve (E–S) and store `affection_rank` only.

### 4.3 Skins (not growth, but on the same instance)

`character_skin` 1866 (`type` 0 = 1246, `type` 1 = 620). `character_spa_skin` 488. Product: `SkinCatalog` already stubs `echo` / `night`. Do not import original `view_idx` (`c001_01` etc.).

---

## 5. Puppet (out of the 12-unit slice)

| Table | n |
|---|---:|
| `puppet` | **253** (= `item.category` 101001) |
| `puppet_skill` | **356** |
| `puppet_diorama` | 5-slot display sets + set skill |
| `user_character_puppet` | 1 puppet per character uid (schema only) |

Puppet `attribute` 1–5 is balanced (~49–52). `skill_1` first digit ∈ {2,3,4,5} — **no AUTO**. `puppet_skill.skill_type` is **identical** to that first digit (100/124/76/56 for types 2/3/4/5) → TAP/SLIDE/DRIVE/LEADER-like doll skills.

**Leave puppet out** of Resonance until a cosmetics/collection slice. Do not copy doll names or skill ids.

---

## 6. Resonance already has vs needs (12 original units)

Roster `C001`–`C012` already covers 5 elements and 5 roles (Attacker×3, Defender×2, Debuffer×2, Healer×2, Supporter×3). Enemies `E001`–`E005` + `EBOSS`. Chapter 1 × 12 stages exist as HP/ATK/DEF multipliers. **Do not grow the roster from this SQL.**

### 6.1 Already in client (keep; these are original)

| Piece | Where |
|---|---|
| 5 stats + 5 skill links + element + role | `CharacterDef` / `catalog.json` |
| `AUTO TAP SLIDE DRIVE LEADER` | `SkillType` |
| Native/max star, uncap max 6, ignition max 12 | `CharacterDef` fields (catalog already sets them) |
| Level 1–60, uncap, ignition as **stat mul** | `Growth.Apply` (`1 + 0.035*(lv-1) + 0.02*uncap + 0.012*ign`) — invented, keep |
| 4 gear slots, flat HP/ATK/DEF | `UnitProgress.Gear0–3` + `GearCatalog` stubs |
| Skin stub | `SkinCatalog` |
| Party of 5 + leader | `SaveBlob` |
| Ignited-variant **fields** on `SkillDef` | unused — keep unused |

### 6.2 Add for a **small** growth slice (clean-room names only)

Extend `UnitProgress` / save JSON. **Do not add original columns under original names.**

| Add | JSON key | Why |
|---|---|---|
| `Exp` | `exp` | Level-up without a hidden counter |
| `Star` | `star` | Display stars 5–6 (evolution chip). Catalog keeps `NativeStar` |
| `SkillLvTap` `SkillLvSlide` `SkillLvDrive` | `lvTap` `lvSlide` `lvDrive` | Optional 1–10; **or freeze = 10** and skip UI |
| `AffectionRank` | `aff` | Optional 0–5 (E–S). Skip if slice is still “fight then grow stats via level/uncap” |

Level cap from uncap can stay **derived** (e.g. 50 + 2×uncap, or freeze 60). Do not copy original cap tables.

### 6.3 Explicitly **do not** add in this slice

| System | Why out |
|---|---|
| Ignition **skill-id swaps** + 256 hex trees | P3. Current `Ignition` mul is enough |
| Soul carta / 4406 `item_option` / +15 enhance curves | P2. One stub gear piece per slot |
| `acc_2` `acc_3` | Not on Memorial 4-slot bar |
| Awaken paths 200–500 | Master table empty; P3 |
| Puppet + diorama | Collection, not combat MVP |
| Spa / bathing / exploration flags | Out of combat loop |
| Skill reservation 5× T/S/E | P1 UI |
| Original HP/ATK numbers, skill ints, option `value`s | IP |

### 6.4 Stat assemble (product, matches Memorial columns + current code)

1. Catalog bases (`CharacterDef`)  
2. × `Growth.Apply` level / uncap / ignition mul → **Child**  
3. Optional invented affection % → **好感度**  
4. + `GearDef` flats → **裝備**  
5. Inlay / carta / hex cores → **later**  
6. Leader / battle effects → runtime  

Combat Power stays **UI-only**. Do not feed it into `DamageMath`.

---

## 7. Clean-room field card (copy this, not SQL)

### CharacterDef (catalog — already exists)

`id` `name_key` `element` `role` `native_star` `max_star` `uncap_max` `ignition_max`  
`base_hp` `base_atk` `base_def` `base_agl` `base_crt` `charge_time_sec`  
`auto_skill_id` `tap_skill_id` `slide_skill_id` `drive_skill_id` `leader_skill_id`  
`default_skin_id`

### UnitProgress (save — extend)

`id` `level` `exp` `uncap` `star` `ignition`  
`skill_lv_tap` `skill_lv_slide` `skill_lv_drive`  
`slot_weapon` `slot_armor` `slot_acc` (`slot_relic` unused)  
`skin_id`  
`affection_rank` (optional)

### GearDef (catalog — stubs exist)

`id` `name_key` `slot` (`Weapon` `Armor` `Accessory`) `hp` `atk` `def` `agl` `crt`

Invent new stub numbers if you add more pieces. **Do not** paste dump medians.

### SkillDef (catalog — already exists)

`id` `name_key` `skill_type` `target_rule` `target_count` `hit_count` `atk_coef` `flat_power` `drive_gain` `effect_id`  
`is_ignited_variant` `base_skill_id` — **leave false / empty**

---

## 8. Implementation order (12 units)

1. Wire `exp` → `level` 1–60 on the existing 12 defs (Growth mul already runs).  
2. Uncap +0–+6 already in save; expose it. Display `star` 5 or 6 as a cosmetic of uncap≥0 / evo flag — **do not** import 3★ native kits.  
3. Three gear slots using current `GearCatalog` (leave 残章 empty).  
4. Stop. Do not import ignition swaps, carta, puppet, or original stats.

If that loop is fun, *then* consider a 5-row invented affection curve. If it is not fun, do not add systems this dump merely proves exist.
