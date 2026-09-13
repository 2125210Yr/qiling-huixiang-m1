# Offline pack 1.1 — MVP 缺口与禁令

Source: `destinychild` schema in `_sandbox/offline-pack-1.1/schema-only.sql` + pack inventory `00-pack-inventory.md`.  
Resonance now: `F:\Resonance\client\Assets\Scripts` (`Definitions`, `Growth`, `Gear`/`SkinCatalog`, `SaveStore`, `BattleSim`, `Catalog`, `GameRoot`).  
Product data model: `docs/reference/mobile-archive/_drafts/DATA_MODEL_FROM_REFERENCE.md`.

This pack is a **pirated private-server wrapper** around the original APK + `pck` + MySQL. We read **schema grammar** for a nostalgia MVP. We do **not** run it, merge it, or ship it.

**Never copy original IP into `F:\Resonance\client`.** Copy system grammar. Original names, art, logos, Memorial portraits, SQL `name` rows, Unity `skill_effect_path` strings, and `pck` assets stay out of the product.

**Superseded in part by** `05-authenticity-lock.md` (player, 2026-08-28): art is secondary; 12 units may be **combat twins** of original niches; **verbs + damage numbers** are the product. Section 4 below said “leave combat math frozen” — that is **wrong** after the lock. Still do **not** retune from SQL `characters.hp` rows (wrong scale, no formulas). Authenticity stack: GameKee TS/SS + Memorial displayed damage + SQL **shape**.

---

## 0. Locked product (do not reopen)

| Lock | Value |
|---|---|
| Roster | Original 12 only: `C001`–`C012`（焰刃、炉心卫士、潮汐祭司、深蓝咒师、森语引路者、荆棘猎手、白昼守望、晨星歌者、夜幕医师、影缚者、灼红侍从、冰镜使者） |
| Party | 5 slots + one leader |
| Stages now | Chapter 1 × **12** (`VS-1` + `CH1-2`…`CH1-12`) |
| Combat verbs | Auto / TAP / SLIDE / Drive + QTE / Fever |
| Persistence | Local `save.json` only |
| Hard bans | No backend, accounts, networked gacha, shop, PvP, guild, ranking, events, full remake |
| Content ceiling | Do **not** grow toward SQL’s 637 characters or 2048 spacewalk stages |

`GameRoot` already stubs 图录 / 书库 / 深途. Those stubs are **IA placeholders**, not a license to import 图鉴 / 图书馆 / 星云 dumps.

---

## 1. Original system → Resonance now → later local-only? → BAN

Legend: **YES** = local grammar on the original 12 + a small original catalog is allowed later. **NO** = never, even locally. **DEFER** = maybe after v0.2, still local, still tiny.

| Original system (SQL / pack) | Resonance now | Later local-only? | BAN |
|---|---|---|---|
| `characters` 637 rows: `idx`, `role`, `attribute`, `start_grade`, hp/cri/agi/def/atk, `skill_1..5`, ignition/awaken groups | 12 `CharacterDef` (`C001`–`C012`) + 6 enemy defs; 5 stats; 5 skill ids; `NativeStar`/`MaxStar` | Keep **12**. Growth curves as original numbers, not SQL row import | **BAN** importing 637 rows / original `name` / `bbs_idx` / arena-dungeon coin SKUs |
| `character_skin` 1866: `sub_type` PK, `type` 0=1246 / 1=620, `view_idx` like `c001_01`, `drive_skill_effect`, library dates | `SkinCatalog`: 3 global ids (`""` / `echo` / `night`); `UnitProgress.SkinId`; inspect cycle 「本貌/残响/夜巡」; **not** per-character, **no** `sub_type` | **YES** — per-unit wardrobe of 2–3 original skins + `sub_type` enum (`Normal` / later `Special`) | **BAN** 1866 rows, original skin names, `view_idx` that resolve into `pck`, spa skins, library-open dates |
| `character_spa_skin` + `character_spa_status_percent` (温泉好感%表) | Absent. `Deep`/`Library` stubs only | Affection **save fields** yes (see below). Spa loop **NO** | **BAN** 魔界温泉 / bathing / spa coin / spa decoration |
| `character_awakens` **0 rows**; awaken lives on `user_character` (`main_awaken`, `path_200`…`path_500`) | No awaken tree. Inspect shows stars from `NativeStar` + uncap | **DEFER** a one-line “rank E–S” display tied to affection, not 5 awaken paths | **BAN** path_200–500 trees, voice-unlock flags, importing empty master as if it were content |
| `user_character.attraction_level` / `attraction_exp` | **No** affection fields on `UnitProgress` | **YES** — `affection_rank` + `affection_points` on save; four-way stat column 「契体 / 好感 / 装备 / 镶嵌」 | **BAN** spa-hour meters, heart-reward indexes, original S CLASS badge art |
| `user_character.over_limit` (+ `item_over_limit_status`) | `UnitProgress.Uncap` 0–6; Growth `+0.02 * uncap`; UI 「突破 +N」 | **YES** — keep uncap; alias in data as `over_limit` so Memorial grammar matches | **BAN** copying `item_over_limit_status` value tables from SQL |
| `user_character`: `weapon` / `armor` / `acc_1` / `soul_carta` (+ locked `acc_2`/`acc_3`) | **Already 4 slots**: `Gear0..3` = 武器 / 防具 / 饰品 / 残章. One stub `GearDef` each. Flat hp/atk/def only | **YES** — same 4 slots; small original EQ set; enhance + over_limit on **instance** | **BAN** `acc_2`/`acc_3` unlock ladder, 2952 `item` rows, original item `name`/`view_idx` |
| `item` 2952: category 1/2/3 = wpn/armor 539/acc, 8 = soul carta, **101001 = puppets** | 4 stubs (`EQ_WPN`/`EQ_ARM`/`EQ_ACC`/`EQ_CRD`) | **YES** — ~12–24 original pieces for 12 units | **BAN** puppet dump, shop coins, library tags |
| `item_option` 4406 random affix pool; `item_enhancement_status` enhance ladders | Gear has **no** substats, **no** enhance_level | **DEFER** 0–1 rolled line per piece (P2 inlay). Enhance as `+N` on instance | **BAN** 4406-row affix import, `equation`/`logic` strings from SQL |
| `soul_carta_enhancement_status` 170 + `soul_carta_option` 1874 | 4th slot exists as 「残章」; no enhance, no plus_option_1..3 | **YES** — carta as 4th slot with 1–2 original option lines, enhance 0–N | **BAN** 1874 option dump, original carta names/art |
| `ignition_character_skill` 2928 = 488 chars × **6 nodes** (levels 1,2,5,8,11,12 → `redstar` 1–6), skill_1..5 swaps | `UnitProgress.Ignition` **0–12 linear**; Growth `+0.012 * ign`; UI 「燃起 N/12」; `SkillDef.IsIgnitedVariant` unused in builtin kits | **YES** — **6 discrete nodes** on 12 units; node N may point at ignited skill ids | **BAN** 488-character tree, hex item cores from other units, 12/12 as a content farm |
| `ignition_item_status_tree` 3072 hex / core stats (`core_slot`, amp_atk…) | Ignition is a scalar, not a hex board | **DEFER** a 6-node strip (not a hex grid). Report already rejected a 6-slot **rune** grid | **BAN** 3072-row tree, original core items, GM cores |
| `skill_effect_path` 7257: id → Unity path (`sfx_atk_normal`, `buff/…`) | `EffectDef` handful + `EffectKind` enum (~30). **Coefficients live in C#**, not SQL | Rebuild original effects as data. Paths are **not** formulas | **BAN** copying those paths, attaching them to `pck` SFX, treating path table as combat spec |
| Skills themselves | Five types AUTO/TAP/SLIDE/DRIVE/LEADER on every unit (role kits). One `EffectId` per skill | Keep. Later: `effect_ids[]`, reservation, ignited variants | **BAN** decompiling jar skill formulas; **BAN** PvP-only extra lines (`context=PvpOnly`) |
| `user_character_skill_reserve` 5× `reserve_N_skill_type` (T/S/E) | **None**. Battle is manual TAP/SLIDE + optional `AutoTap` | **YES** — 5 tokens on `UnitProgress` (P1). Drive is not a token | Not a BAN if original and local |
| `user_party` 5 uids + `leader_uid` | `SaveBlob.PartyIds[5]` + `LeaderSlot`; unique-def swap on slot | **YES** — several **local presets** (Memorial 7/9 is grammar, not a quota to copy) | **BAN** friend share slot, `share_char`, networked assist |
| `user_chapter_boss_party` / `user_world_boss_party` **20** uids + `drive_skill_slots` | 5-man only | **NO** | **BAN** 20-slot raid/WB parties |
| `game_area_dungeon` 304, 30 `area_idx`, `area_type` 1 and 2 = 152 each (normal/hard), `need_dungeon_idx`, `need_user_level` | `Catalog.MakeChapter`: 12 names, linear lock via `clearedN`, **no** `area_type`, **no** stamina, **no** player level gate | **YES** — **one** chapter pair: Normal 12 + Hard 12, `need_stage` chain, optional account level later | **BAN** 30 areas / 304 dungeons / original area `name`/`view_idx` |
| `game_area_dungeon_stage` 928: chain, stamina, `show_char_1..6` — **3 chips filled** (4–6 unused) | `StageDef`: `Wave0[5]` + `Wave1[boss]` (our **combat** wave, not a dump of 928 previews) | **YES** — preview **our** wave (3–5 chips) + boss pip + `need_stage`. Hard = **per-chapter** gate after that chapter’s last normal, not a second 152-long spine | **BAN** 928-row import, original titles |
| `area_dungeon_clear`: `is_clear`, `star_count`, `last_clear_stage_idx` | `SaveBlob.ClearedCount` (prefix unlock only), no stars | **YES** — per-stage clear + optional 3-star, still 12×2 | Not a BAN |
| `game_spacewalk_area_dungeon` 32 areas, `stage_count` sum **2048**; `*_stage_mob` one mob row/stage; changelog「一千多关」 | `ScreenId.Deep` stub 「更深的裂口尚未开启」 | **NO** for v0.x. If ever: a **12-floor** original “deep” with HP inherit — not 2048 | **BAN** spacewalk dump, 2048 mob rows, `position_1..5_view_idx` from original |
| `chapter_boss_mob` / `area_boss_mob`: hp_phase, enemy_buff_1..3, skill_1..5, `view_idx` | One `EBOSS` scaled per stage mul | **YES** — original boss **template** (phases 1–2, 1–2 buffs) on 12 stages | **BAN** importing every boss `view_idx` / original skills |
| `special_raid_dungeon` 67: `season_idx`, date windows, assistant dates, yaml `raid: 49` (0–56) | None | **NO** | **BAN** raid catalog, season switch, raid coin, 1.1「RAID赛季切换」 |
| `world_boss_serial_mob` 74 + `*_data`; yaml `world: 27` (1–38) | None | **NO** | **BAN** world boss, WB season switch, serial coin |
| `puppet` 253 / `puppet_skill` 356 / `puppet_diorama` | None | **NO** | **BAN** doll / diorama / 人偶券 |
| `user_exploration` 5-man dispatch timers | None | **DEFER** a local “away” timer on original stages | **BAN** original exploration indexes |
| `user_home_*` lobby / stickers / spa flag | Home is original IA, not a housing sim | **NO** | **BAN** home editor, sticker coords, lobby BGM overwrite flags from SQL |
| `do_login` / `do_login_user` / `do_platform_login`: `nfguid`, gold/gem/`blood_gem`/onyx, arena/dungeon/raid/spa/skin coins, nickname, mileage, tickets, boosts, inventory caps | Local save: party, leader, clearedN, seed, speed, auto, per-unit lv/uncap/ign/skin/g0–g3. **No currencies, no account** | Gold + account_exp **optional** local. Player “Lv” as display only | **BAN** accounts, `nfguid`, gems, bloodstone, all `*_coin`, mileage, IAP packages, device uuid login |
| Gacha / shop tables | **Absent from this dump** (jar and/or `pck`). Changelog: infinite 天子/装备/人偶券; 血石商店→邮件; 100% 还魂 first 5★ | Local recruit **if ever**: a **pity-free grant** of the 12, not a banner sim | **BAN** networked gacha, tickets, bloodstone shop, mail as shop delivery, 还魂, summon_mileage |
| `user_mail` | None | **NO** | **BAN** mail, shop→mail cheat path |
| `user_item` instance: enhance, over_limit, plus_option_1..3, protect, counts | Gear ids on unit only; no inventory bag | **YES** — tiny bag of original EQ instances | **BAN** 6751-auto-increment dump, protect flags as online anti-trade |
| `user_synthesis_character_list` seasonal 6-fuse | None | **NO** | **BAN** synthesis / 还魂 / seasonal fuse lists |
| `user_character_ignition` cores per slot | Scalar ignition | Covered by 6-node strip later | **BAN** original `ignition_core` item ids |
| `user_equip_bookmark` loadouts | None | **DEFER** 1–3 local gear presets | Fine if original |
| `battle_type` from→to matrix (PvP type map) | PvE only | **NO** | **BAN** PvP, duel tickets, arena trophy, arena enemy refresh |
| `user_character.underground_hp_per` + login underground flags | None | **DEFER** with a tiny deep mode | **BAN** original underground as 2048-floor clone |
| Pack runtime: `295.apk`, `pack.pck` / `locale.pck`, `destinychild-1.0-SNAPSHOT.jar`, MySQL `destinychild`, `开始游戏.bat`, `config.txt` `server_addr` | Unity clean-room client | Never | **BAN** install/run/merge/ship any of it (see §3) |
| Changelog 1.1 server cheats (equip/skill-set fixes, infinite tickets, persist if DB kept, spacewalk 1000+, bloodstone shop, 100% revival, one-click server, RAID/WB season, **GM 5★ weapons**) | N/A | Cheats are **not** systems | **BAN** all of them as product features |
| Story / scripts / live2d / Memorial portraits | Original Live2D addresses not in this SQL. Client uses original presenter + mosaic | Original copy + original illustrator work | **BAN** original names, flavor, logos, Memorial shots as UI chrome. `skill_effect_path` is not a story table — **story is not in this dump** |

---

## 2. Highest-value grammar to steal (MVP v0.2+) — without 637 / 2048

Steal **shape**, not **volume**. Every item below must work on **12 original units** and **≤24 stages**. If a field needs a 637-row join to feel “real”, it is the wrong field.

### 2.1 Four equip slots + carta (already 80% in client)

SQL `user_character` and Memorial agree: 武器 / 防具 / 装饰品 / 魂之歌碑. Resonance already has four circles and a 4th stub 「残章」.

**Steal:** slot count, empty-state copy grammar (“未装备” → original wording), instance fields `enhance_level` + `over_limit`, carta as a **build card** (1–2 option lines), not a 5th combat pet.

**Do not steal:** 2952 items, `acc_2`/`acc_3`, 4406 affixes, inlay grid (P2, and never a 6-rune board).

**v0.2 bar:** each of 12 units can equip 4 original pieces; Growth still adds flat hp/atk/def; carta options are data, not SQL `equation` strings.

### 2.2 Six ignition nodes (not a 0–12 slider)

SQL: `ignition_character_skill` keys `(character_idx, ignition_level)` with **six** populated levels `1,2,5,8,11,12` mapping `redstar` 1–6 and **skill_1..5 replacements**. Memorial hex `12/12` is the **display cap**, not “click 12 times for +1.2% stats”.

Resonance now: `IgnitionMax = 12` linear multiplier. That is the wrong grammar.

**Steal:** 6 nodes on the inspect strip; node N may swap TAP/SLIDE/DRIVE (and only those) to an ignited `SkillDef` (`IsIgnitedVariant` + `BaseSkillId` already exist). Red-star pips 1–6 as UI.

**Do not steal:** 488 characters × 6, hex item tree 3072, core inventory, 12/12 as a grind that requires 637 fodder.

**v0.2 bar:** C001–C012 each have 6 original nodes; filling a node is a local spend (gold or a generic “ember” counter), not a gacha.

### 2.3 Normal / Hard chapter chain (not 304 dungeons)

SQL: `game_area_dungeon.area_type` 1 and 2 split 152/152; `need_dungeon_idx`; stages chain with `need_stage_idx`.

Resonance now: 12 linearly gated names, same 5-trash + 1-boss waves, only `EnemyHpMul` climbs.

**Steal:** `difficulty = Normal | Hard`; Hard unlocks when Normal-12 is cleared; each difficulty has `need_stage` so 3 cannot be clicked before 2; Hard uses the **same 12 original maps** with higher muls (and later, a second enemy mix from **our** E001–E005, not 928 imported `show_char_*`).

**Do not steal:** 30 `area_idx`, spacewalk 2048, original area titles, user-level gates that exist to sell the live game.

**v0.2 bar:** 12 Normal + 12 Hard = 24 fights. `SaveBlob` stores `clearedNormal` / `clearedHard` (or a per-stage map). Still one chapter.

### 2.4 Five-man enemy wave preview

SQL `game_area_dungeon_stage.show_char_1..5_*`: idx, view, grade, awaken, over_limit, level, is_boss. That is the pre-battle **who’s in this fight** strip. A 6th column exists in the dump; product grammar is **five** to match the 5v5 board.

Resonance now: you only see stage **names**. Waves exist on `StageDef.Wave0/Wave1` but the Stage screen does not render them.

**Steal:** before 出战, five enemy chips (element + role + boss pip + level). Same five slots as the player party.

**Do not steal:** original `view_idx` portraits, awaken/over_limit as imported numbers from 928 rows.

**v0.2 bar:** Stage screen shows `Wave0` icons from **our** `E001`–`E005` / `EBOSS`. Hard can swap one trash for a second boss flag.

### 2.5 Skin `sub_type`

SQL PK is `(idx, sub_type)`. `type` 0 vs 1 is a second axis (1246 vs 620). Memorial wardrobe is a **list** with 一般造型 / 穿著 / 目前穿著中.

Resonance now: one global 3-cycle, shared by all 12.

**Steal:** `SkinDef.sub_type` (`Normal` first; a second value only if we actually author a second original costume). Per-`character_def_id` list. Equip is `UnitProgress.SkinId`. UI states: 可穿 / 穿着中.

**Do not steal:** 1866 rows, spa skins, `drive_skill_effect` that points at original VFX, `enable_library` date gates, original wardrobe titles.

**v0.2 bar:** each of 12 units has default + 1 original alt; cycling is per-unit.

### 2.6 Affection + over_limit as save fields

SQL instance: `attraction_level`, `attraction_exp`, `over_limit`. Memorial: S CLASS badge **and** `好感度 100/100`, plus `+N` uncap chip. Data model already tagged these P0 for full MVP; Vertical Slice left them out. Growth still has **no** affection term.

**Steal:** `UnitProgress.AffectionRank` (E–S) + `AffectionPoints`; keep `Uncap` as over_limit. Assemble stats: body → affection% → gear → (later inlay) → (later ignition nodes). Combat Power remains UI-only.

**Do not steal:** spa as the only way to gain affection, original % table `character_spa_status_percent` as literal live-game numbers, the DEF `+400` mystery chip as a real stat.

**v0.2 bar:** fields persist in `save.json`; inspect shows them; a local action (clear stage / gift a generic token) raises points. No 温泉.

### 2.7 What not to “steal” even though the dump is large

| Tempting dump | Why it is low value for this MVP |
|---|---|
| 637 characters | Product lock is 12 original |
| 2048 spacewalk | Stub 深途; would become a second game |
| Raid/WB seasons | Events + ranking + yaml cheats |
| Puppet / diorama | Extra combat pet + housing |
| 7257 effect paths | Asset index, not math |
| Coin / gem / blood_gem matrix | Shop + account |
| 20-man raid parties | Breaks 5-man identity |

---

## 3. Explicit do-not-do list

These are **not** “later, locally”. They are **never**.

1. **Do not install `295.apk` into the product** — not as a Unity plugin, not as an emulator sidecar, not as “reference viewer inside the repo”. The APK is `com.NextFloor.DestinyChild`.
2. **Do not copy `pack.pck` / `locale.pck`** (full 7.7 GB zip **or** the 1.1 delta ~8.1 / 1.9 MB) into `F:\Resonance\client`, StreamingAssets, or any shipped build.
3. **Do not import `destinychild.sql` names** (or `view_idx`, flavor, skill titles, item names) into catalogs, loc files, or `CharacterDef.Name`. Original roster names are **焰刃…**, not SQL `characters.name`.
4. **Do not run, decompile, wrap, or ship the Java `destinychild-1.0-SNAPSHOT.jar` / `开始游戏.bat` / Spring `application.yaml`.** That is the private GameServer. Schema reading is enough; combat and gacha live in the jar and stay unread.
5. **Do not implement gacha tickets** (天子券 / 装备券 / 人偶券, infinite or otherwise). Changelog item 3 is a **server cheat**, not a feature request.
6. **Do not implement a bloodstone shop** (`blood_gem`, 血石商店, shop→`user_mail`). No shop, no mail-as-fulfillment.
7. **Do not implement RAID / WB season switch** (`special_raid_dungeon.season_idx`, `world_boss_serial_*`, yaml `raid:` / `world:`). No events, no ranking, no “restart server to change season”.
8. **Do not add GM 5★ weapons** (changelog item 10, 「秒天秒地」). No debug one-shot gear in the live catalog; if a sandbox GM item exists, it must not ship and must not use original item ids.

Also never:

- Point `config.txt` `server_addr` at anything, or add accounts / `nfguid` / `do_platform_login`.
- Merge this MySQL into Resonance saves.
- Treat `skill_effect_path` Unity strings as ours.
- Copy Memorial portraits, logos, or original skill copy.
- Grow content to 637 characters or 2048 stages “because the dump has them”.
- Networked gacha, PvP, guild, ranking, live events, full remake of the KR client.

Analyze schema for nostalgia MVP. **Do not run, merge, or ship the pack.**

---

## 4. Prioritized 8-item backlog (original-roster local game)

Data fields + UI grammar only. No new IP, no roster growth, no backend.

| # | Item | Why now | Fields / UI | Explicitly out |
|---|---|---|---|---|
| 1 | **Normal / Hard chapter chain** on the existing 12 maps | Highest “this is the adventure loop” gap; SQL `area_type` + `need_stage_idx` is cheap | `StageDef.Difficulty`, `NeedStageId`; save `clearedNormal` / `clearedHard`; Stage UI: two tabs, lock Hard until Normal-12 | 304 dungeons, 30 areas, 2048 spacewalk, original titles |
| 2 | **Enemy wave preview** on Stage | Show **our** `Wave0` (3–5) + boss; dump story preview was **3** chips | Stage chips from `Wave0`/`Wave1` | Original `show_char_*_view_idx`, claiming dump preview is 5-man |
| 3 | **Affection + over_limit on save** + four-way stat line | Memorial identity; Growth has uncap but no affection term | `UnitProgress.AffectionRank`, `AffectionPoints`; inspect 「突破 +N」 stays; add 好感 E–S and 100/100; assemble body→affection→gear | Spa, original % SQL, S CLASS art |
| 4 | **4 slots + carta as real build** | Slots exist; carta is still a dummy ATK piece | Per-slot `GearDef` + instance `enhance`/`over_limit`; carta 1–2 `option` keys; empty slot copy | 2952 items, affix pool 4406, acc_2/acc_3, GM weapons |
| 5 | **6 ignition nodes** (replace 0–12 slider) | Current `/12` slider teaches the wrong system | `IgnitionNode[6]` or level∈{1,2,5,8,11,12}; optional `ignited_*_skill_id` on the 12 kits; UI 6 pips | 2928-row import, hex tree, cores as items |
| 6 | **Skin `sub_type` + per-unit wardrobe** | Global 3-cycle is not a wardrobe | `SkinDef { characterId, subType, isDefault }`; 12×(default+1); 穿著 / 穿着中 | 1866 skins, spa skins, original names, `pck` live2d |
| 7 | **Skill reservation T/S/E × 5** | SQL + Memorial; Drive stays a party resource | `UnitProgress.Reservation[5]` enum Empty/Tap/Slide; battle auto uses the list | Drive as a 6th token, PvP reservation |
| 8 | **Local team presets (≤7)** | `user_party` is multi-row; Memorial 7 TEAM is grammar | `SaveBlob.Teams[]` each 5 ids + leader; Team screen picker | 20-slot raid/WB parties, friend assist |

Suggested order after `05-authenticity-lock.md`: **0 combat authenticity** (TS vs SS, Fever `feverMul`, per-unit twin kits from Memorial numbers) **then** **1 → 2** (stage loop) → **3** (inspect) → **4** (loadout) → **5** (six ignition nodes) → **6–8**. Stop after 4 on the grammar list only if combat pass already landed.

Do **not** retune from this SQL’s base-stat INSERTs — **the dump has no formulas**. Do retune battle path and kits from GameKee + Memorial; see `05-authenticity-lock.md`.

---

## 5. Risk: this SQL is a truncated private-server subset

1. **Master is partial; runtime save is empty.** 54 tables. Catalog INSERTs exist for combat-adjacent masters. All `user_*` tables are empty schemas. You cannot infer live economy, inventory, or story progress from this dump.
2. **Combat formulas are not here.** `skill_effect_path` maps ids to Unity-style **asset paths**, not `atk_coef` / DEF / element tables. Real skill logic is in the **unopened jar** and/or **pck**. Resonance damage is the research-report **rebuild** (`DamageMath` / `BattleSim`). Do not “port” numbers out of `characters.hp` as if they were the live formula.
3. **Story is not here.** No script tables, no quest text, no Memorial library bodies. Narrative cannot be recovered from `game_area_dungeon.name` varchar(32) stubs. Do not pretend 12 original stage titles are a port of the KR plot.
4. **Gacha, shop, PvP, and most live ops are not here.** Inventory already noted: no banner tables, no shop SKUs, no PvP match tables. Changelog 1.1 “features” are **Java cheats** (infinite tickets, bloodstone→mail, season ints in yaml, GM weapons), not extra masters. Absence of a table is not permission to invent that system locally if it is already hard-banned.
5. **`character_awakens` is schema-only (0 rows).** Awaken paths on `user_character` do not give us a design we can copy honestly. Treat E–S as Memorial grammar, not as this dump’s awaken tree.
6. **Row counts are a trap.** 637 / 1866 / 2048 / 928 measure the **pirated live game**, not the MVP. Using them as a backlog size is how a clean-room 12-unit game becomes a remake.
7. **Private-server ≠ original balance.** Pack author sluteuio patched equip/skill-set bugs and added GM gear. Even if we did run it (we will not), those numbers are not KR live and not ours.

**Working rule:** schema tells us *what slots and chains existed*. Memorial + the research report tell us *how it felt*. Resonance code tells us *what v0.1 already ships*. Product content stays original. This dump stays in `_sandbox/` and `docs/reference/`, gitignored binaries untouched.

---

## 6. One-page “now vs v0.2” for implementers

| Surface | v0.1 (now) | v0.2 target (this doc) |
|---|---|---|
| Units | 12 original, frozen kits | Same 12; save grows |
| `UnitProgress` | `lv`, `uncap`, `ign` 0–12, `skin`, `g0–g3` | + `affectionRank/Points`; `ign` → 6 nodes; gear instances |
| Stage | 12 Normal, name list, prefix unlock | 12+12 Hard, need_stage, 5-man preview |
| Skin | 3 global labels | Per-unit `sub_type` |
| Battle | Auto/TAP/SLIDE/Drive/Fever; role-template coefs; Slide still on TS | TS/SS split; Fever `feverMul`; 12 twin kits; then optional 5-slot reservation |
| Network | None | None |
| Pack APK/pck/jar/SQL rows | Not in client | Still not in client |
