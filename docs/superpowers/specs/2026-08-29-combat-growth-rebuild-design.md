# Clean-room combat + growth reconstruction

- Date: 2026-08-29
- Status: Approved
- Product tables: `client/Assets/Content/catalog.json`
- Runtime: `Resonance.Battle` (`DamageMath`, `Growth`, `Gear`, `Catalog`)

## Overview

Memorial / wiki / SQL do not form a complete shippable number pack. This reconstruction keeps Destiny Child's **combat verbs and measured TAP/Slide/Fever formulas**, fills the missing Drive sibling, and generates a **25-character (5 elements × 5 roles) clean-room roster** plus growth/gear curves. Original names, skill text, and art stay out of the client.

## Goals

- TAP / Slide / Fever formulas stay locked (tests: 2182 / 3740 / fever 0.6).
- Drive is a third sibling formula + existing QTE (Bad 0.90 / Good 1.00 / Great 1.20 / Perfect 1.50).
- 25 unique kits, one per element×role cell. Existing C001–C012 occupy unique cells; C011 moves off the duplicate Fire Attacker cell.
- Body stats from role baselines, not copied SQL/wiki rows.
- Growth keeps `Growth.BodyMul` / affection / ignition stops / four gear slots.
- Handbook page documents the reconstructed pack.

## Non-goals

- Do not copy 561 memorial names or skill prose into `catalog.json`.
- Do not decompile the private-server jar.
- Do not invent a second HUD or 12-person combat.
- Do not fill the research `汇总表.csv` in this pass.

## Combat math

| Verb | Function | extraAtk weight | def term | extra |
|---|---|---:|---|---|
| TAP / AUTO / FEVER | `ComputeTs` | 125 | `0.15*def+400` | `feverMul` 1 or 0.6 |
| SLIDE | `ComputeSs` | 120 | `0.2*def+400` | agiTerm unused |
| DRIVE | `ComputeDs` (new) | 130 | `0.12*def+400` | QTE via `extraDmgMul` |

`skillDmg = round(atk * coef + flat)` as today (`extraAtk = 0` until red ignition is wired).

Drive worked example (same 2707 skillDmg / 2500 def / 1.4 as TAP 2182):

```
(0*130 + 2707*400) * 1.4 / (0.12*2500 + 400) = 1515920 / 700 = 2165.6 → 2166
```

Element: fire>wood>water>fire, light↔dark. `elemCrit` = 1.4/1.0/0.7, crit adds +1. Overlay Drive > Slide > Tap. ExtraDmgMul floor 0.1.

## Body curve (catalog = Lv1 naked 5★)

| Role | HP | ATK | DEF | AGL | CRT | Charge s |
|---|---:|---:|---:|---:|---:|---:|
| Attacker | 2200 | 1180 | 640 | 900 | 820 | 9.0 |
| Defender | 3300 | 740 | 1160 | 660 | 520 | 8.5 |
| Debuffer | 2500 | 880 | 740 | 1100 | 700 | 8.0 |
| Healer | 2900 | 900 | 800 | 800 | 660 | 9.0 |
| Support | 2700 | 820 | 840 | 1040 | 600 | 7.5 |

Per-id offsets ±40 so cells are not clones. Enemies stay a lower band (~0.55× body).

`BodyMul(lv,uncap,ign) = 1 + 0.035*(lv-1) + 0.02*uncap + 0.012*ign`  
Lv60 / uncap6 / ign12 ≈ **3.329**. Affection S = ×1.18. Gear flats add after the product.

## Roster

Keep C001–C010, C012. Reassign **C011 灼红侍从** Fire Attacker → Fire Support. Add C013–C025 for the remaining 13 cells. Default party stays `C001, C007, C010, C003, C005` (five elements, five roles).

## Key decisions

1. Lock wiki TAP/SS/FT; reconstruct Drive as sibling, not `atk*coef` only.
2. 25 clean-room kits, not 561 original rows.
3. Catalog remains source of truth via `BuildBuiltin` → `catalog.json` roundtrip.

## Alternatives rejected

- Simplify all damage to `atk*coef`: drops locked tests.
- Copy SQL role means: original balance, banned from product tables.

## PR Plan

1. `DamageMath.ComputeDs` + tests.
2. Catalog 25 + C011 reassignment; tests update C001 HP.
3. Handbook `重构数值.html` + nav.
4. Gear/carta table copy on the handbook (runtime gear set can stay four slots).
