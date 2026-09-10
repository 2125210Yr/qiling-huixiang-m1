# Resonance content schema

Product tables live in this folder. **No original Destiny Child names, flavor, or art.**

`catalog.json` is the runtime pack loaded by `CatalogJson.TryLoadDefault()`. Builtin `Catalog.BuildBuiltin()` is the fallback when the file is missing.

## catalog.json

```
party: string[]
stage: { id, name, time, w0[], w1[] }
effects[]: { id, kind, mag, dur, stack, tier, group }
chars[]: { id, name, el, role, hp, atk, def, agl, crt, charge, auto, tap, slide, drive, leader, enemy, boss, native, maxStar, uncap, ign }
skills[]: { id, name, type, target, n, hits, coef, flat, drive, fx, hcoef, hflat, hfrac, sflat, pct, ign, base }
```

Enums are integer values from `Enums.cs`.

Playable roster is **C001–C025** (5 elements × 5 roles), not the memorial 561. C011 is Fire Support, not a second Fire Attacker. C001 stays **冰刃 / Fire / Attacker**. Empty `name` / `el` / `role` in JSON keep the builtin clean-room cell.

| | 攻击 | 防御 | 干扰 | 治疗 | 辅助 |
|---|---|---|---|---|---|
| 火 | C001 冰刃 | C002 炉心卫士 | C013 熔渣咒印 | C014 炉灰医师 | C011 灼红侍从 |
| 水 | C015 裂潮刃 | C016 堰门卫 | C004 深蓝咒师 | C003 潮汐祭司 | C012 冰镜使者 |
| 木 | C006 荆棘猎手 | C017 根墙守 | C018 毒棘使 | C019 青苔愈 | C005 森语引路者 |
| 光 | C020 昼锋 | C007 白昼守望 | C021 眩光缚 | C022 晨露愈 | C008 晨星歌者 |
| 暗 | C023 夜刃 | C024 影壁 | C010 影缚者 | C009 夜幕医师 | C025 低语引 |

Damage: TAP/AUTO/FEVER → `ComputeTs`; SLIDE → `ComputeSs`; DRIVE → `ComputeDs` (130 / `0.12*def+400`). QTE Perfect 1.50.

Lv1 catalog stats are naked 5★ role baselines. Runtime growth: `BodyMul = 1 + 0.035*(lv-1) + 0.02*uncap + 0.012*ign`.

Reference (original names allowed): `docs/reference/gamekee/parsed/`. Spec: `docs/superpowers/specs/2026-08-29-combat-growth-rebuild-design.md`.
