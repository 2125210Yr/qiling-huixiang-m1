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

Reference (original names allowed): `docs/reference/gamekee/parsed/`.
