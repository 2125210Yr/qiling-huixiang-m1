# Offline pack 1.1 — inventory (read-only)

Source: player-preserved Korean Destiny Child private-server pack at `F:\天命之子\单机dc1.1整合包`.
Author note in tutorial: sluteuio / tieba thread. Changelog: 韩服 DC 单机 1.1.

This folder is **reference only**. Do not copy original names, art, pck, apk, jar, or SQL rows into `F:\Resonance\client`.

## What we touched

| Action | Result |
|---|---|
| List split zip (no full unzip) | ~8.0 GiB, 3 volumes |
| Extract | `destinychild.sql` (4.0 MB), `application.yaml`, `config.txt`, `1.1更新包.zip` listing |
| **Not** extracted | `295.apk`, 7.7 GB data zip (`pack.pck` / `locale.pck`), Java jar, Navicat/JDK/MySQL installers |

Sandbox copies live in `_sandbox/offline-pack-1.1/` (gitignored).

## Pack layout

- **PC server**: MySQL `destinychild` + Spring `destinychild-1.0-SNAPSHOT.jar` + `开始游戏.bat`
- **Phone**: original package `com.NextFloor.DestinyChild`, `295.apk`, `config.txt` `server_addr`, `locale.pck` + `pack.pck`
- **1.1 delta zip**: jar + sql + `locale.pck` (~1.9 MB) + `pack.pck` (~8.1 MB) — small relative to the 7.7 GB full data zip
- **yaml seasons**: `raid: 49` (comment 0–56), `world: 27` (1–38), `level_max: 1` auto-max-level flag

## SQL dump shape (54 tables)

Master data is populated. All `user_*` tables are empty schemas (save lives in MySQL at runtime).

Parent-measured INSERT counts (verify if you re-parse):

| Table | Rows | Notes |
|---|---|---|
| characters | 637 | `idx`, `role`, `start_grade`, hp/cri/agi/def/atk, skill_1..5, ignition/awaken groups, `attribute` |
| character_skin | 1866 | `view_idx` like `c001_01`; type 0 = 1246, type 1 = 620 |
| item | 2952 | category 1/2/3 = wpn/**539 armor**/acc; 8 = soul carta; **101001 = 253 puppets** (not junk mats) |
| item_option | 4406 | random affix pool |
| ignition_character_skill | 2928 | 488 chars × 6 nodes (levels 1,2,5,8,11,12 → redstar 1–6) |
| ignition_item_status_tree | 3072 | hex / ignition item tree (**256×12** = Memorial 12/12) |
| skill_effect_path | 7257 | VFX path only; **5477 are `none`**. Not formulas. `skill_1..5` = AUTO/TAP/SLIDE/DRIVE/**LEADER** |
| game_area_dungeon | 304 | 30 `area_idx`; `area_type` 1 and 2 = 152 each (normal spine + **per-chapter** hard) |
| game_area_dungeon_stage | 928 | chain, stamina, **3 preview chips** filled (schema has 6; 4–6 empty) |
| game_spacewalk_area_dungeon | 256 | 32 areas; `stage_count` sum = **2048** (changelog「一千多关」) |
| game_spacewalk_area_dungeon_stage_mob | 2048 | one mob row per spacewalk stage |
| puppet / puppet_skill | 253 / 356 | doll system |
| special_raid_dungeon | 67 | raid catalog |
| world_boss_serial_mob | 74 | world boss serial |
| soul_carta_* | 170 + 1874 | soul carta enhance + options |

`character_awakens` has **zero** rows (schema only). Awaken paths exist on `user_character` (`main_awaken`, `path_200`…`path_500`).

## Deliberately absent from this dump

No gacha banner tables, no shop SKU tables, no story script tables, no PvP match tables. Those likely live in the **Java jar** and/or **pck**. Do not decompile them.

## Changelog vs SQL (1.1)

Tutorial/changelog claim: infinite tickets, bloodstone shop→mail, 100% revival first 5★, RAID/WB season switch, GM 5★ weapons, equip/skill-set fixes. Those are **server cheats**, not extra master tables.

## Product rules for Resonance

Hard bans still: no backend, accounts, networked gacha, shop, PvP, guild, ranking, events, full remake. Copy **system grammar** (5-man **party**, 4 equip + soul carta, AUTO/TAP/SLIDE/DRIVE/LEADER, Fever as **party runtime**, 6 ignition skill nodes + 12 hex layers, chapter chain with per-chapter hard). Original units only.

Measured reports: `01-schema-map.md`, `02-unit-combat-growth.md`, `03-content-loops.md`. Product: `04` / `05` / `06`.
