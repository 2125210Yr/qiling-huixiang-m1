# Authenticity lock (2026-08-28)

Player direction: **art is secondary**. Characters may be **close twins** of Destiny Child combat roles. **Gameplay verbs and combat numbers must feel original.**

This revises “unique original IP first.” It does **not** license importing NextFloor names, `pck` art, Live2D, APK, jar, or `destinychild.sql` rows into `F:\Resonance\client`.

## Locked

| Layer | Decision |
|---|---|
| Art | Geometric stand-ins / later original illustration. Not a blocker. |
| Names / portraits / logos | Stay original (`C001` 焰刃 …). No 모나 / 燃燒的姬瓦 / NextFloor package strings in the shipped client. |
| Role twins | Each shipped unit occupies a **DC combat niche** (element × role × kit shape), not a unique unrelated kit. |
| Verbs | Auto / TAP / SLIDE / Drive QTE / Fever / 5-man / leader. These are the game. |
| Damage math | GameKee 52136 player-tested formulas already in `DamageMath.ComputeTs` / `ComputeSs` (element 1.4/0.7, crit +1.0, Fever ×0.6). **Canonical write-up:** `天命之子数据/战斗与数值.html` §公式. Drive Perfect = **150%** (`TimingDamage` 1.50, matches 动效页). |
| Skill numbers | Must stop being 5 role templates (`0.90×ATK+120` for every attacker). Target: per-unit coefficients reverse-fitted from Memorial **displayed** skill damage + stats, then applied to original names. |
| SQL dump | Schema + ID grammar + ignition 6-node **shape**. Not live skill formulas. Do not treat `characters.hp` 811 as memorial HP 19408. |
| Still banned | Backend, gacha, shop, PvP, guild, ranking, events, 637-row import, 2048 spacewalk, private server, APK/pck/jar decompile. |

## Honest gap (now vs 原汁原味)

| Piece | Now | Authentic target |
|---|---|---|
| Formula kernel | `ComputeTs` exists and tests against GameKee (2182 / 3740) | Keep; battle path must actually use TS vs SS |
| Battle path | `DamageMath.Compute` always TS; Slide does not use `ComputeSs`; Fever scales **coef ×0.6** instead of `feverMul` | Tap/Auto/Fever → TS; Slide → SS; Fever → `feverMul=0.6` |
| Stats | C001 HP 2300 ATK 1180 (slice band) | Memorial band (e.g. ~20k HP / ~7k ATK at built 6★) **or** a documented scale factor so formula + DEF denom still feel like DC |
| Kits | One `RoleKit` per role, 12 units share | 12 distinct twins: targeting, hit count, buff/debuff lines from Memorial grammar |
| Growth | `1 + 0.035*(lv-1) + 0.02*uncap + 0.012*ign` | Rebuild from memorial lv1 vs lv60 / uncap+6 / ignition 12 when those captures exist |
| CP | `ATK + DEF/2 + HP/8 + AGL/4` | Research `HP×0.25 + ATK×1.8 + DEF×1.3 + AGL×0.8 + CRT×0.7 + SkillScore` (display only) |

## Not in the dump

`skill_effect_path` is Unity **asset paths**, not coefficients. Official client formulas stay in jar/pck — we do not decompile. Authenticity stack is: **GameKee formula + Memorial observed numbers + SQL system shape**.
