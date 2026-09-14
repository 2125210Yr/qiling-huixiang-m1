# G2R14-IMPORT — integrator notes

Task lease: `EffectCapability.cs`, `Catalog.cs`, `CatalogJson.cs`, `BuffCatalog.cs` (validation only).
`BattleSim.cs` is integrator-only. This file is the Cast/ApplyEffect hook request.

## What landed (import / catalog)

- Live catalog is one `CatalogSnapshot` (`Catalog.Current`). Inventory = `Characters` / `Skills` / `Effects`. Playable roster = `PlayableEffects` / `PlayableSkills` (strict `Check().Ok`).
- Historical gaps (`dot_flame` = `status.apply`+Dot) stay in inventory. They are not in the playable roster.
- `CatalogJson.Load` parses and overlays on a **detached** builtin clone, validates the candidate against that baseline, then swaps `Current` once. Failure does not call `BuildBuiltin` or `Install`.
- `TryLoadDefault` catch no longer rebuilds builtin (V02: custom A stays A).
- New `status.apply`+Reflect/Dot (or any new unplayable opcode+kind) **throws** with id/op/kind and does not enter playable or live inventory.
- Skill validation uses the **candidate** effects table (`CheckSkill(sk, effects)`), not live `Catalog.TryEffect` (V03).
- Public gates for battle: `Catalog.EnsurePlayable` / `Catalog.IsPlayable` / `Catalog.TryGetPlayableEffect` / `EffectCapability.RejectUnplayable`.

## Atomic swap

1. `CreateBuiltinTables()` — in-memory baseline, no live write.
2. `CloneTables` + JSON overlay + `FillPlayableSlice` on the clone.
3. `CollectOverlayImportViolations` — hard errors always; unimplemented kind only if the id is **not** in the historical baseline.
4. On success: build playable filters, compute fingerprint, assign `Catalog.Current` once.
5. On failure: throw `ContentValidationException`; `Current` still points at previous A.

`Catalog.LiveFingerprint()` is the V02 identity hook.

## BattleSim Cast hook (NOT done here)

`RejectUnplayable` is still not on the real Cast/ApplyEffect path.

Do **not** call `RejectUnplayable(skill)` / `EnsurePlayable(skill)` on every Cast. Builtin skills such as `C001_slide` still link inventory-only `dot_flame`; a full skill-closure check will throw and break the vertical slice.

Recommended hunks (integrator merges):

### `ApplyEffect` / `ApplyStatus` — skip inventory-only / unplayable effects

In `BattleSim.ApplyEffect`, before `ExecuteOpcode`:

```csharp
if (!EffectCapability.IsPlayable(fx))
    return;
```

or, if the integrator prefers a hard fail on *new* unplayable content that somehow reached battle:

```csharp
if (!Catalog.TryGetPlayableEffect(fx.Id, out _))
    return;
```

Skipping (not throwing) keeps historical `dot_flame` from applying while Tap/Slide damage still settles.

### `Cast` — do not apply an unplayable linked effect

After `var fx = Catalog.TryEffect(skill.EffectId);`:

```csharp
if (fx != null && !EffectCapability.IsPlayable(fx))
    fx = null;
```

Then existing targeting / `ApplyEffect` runs only for playable effects.

Do not add `EnsurePlayable(skill)` at the top of `Cast`. Channel opcodes are already gated by `ExecuteOpcode` / `EffectOpcodes.IsImplemented`.

### `ApplyStatus` public test helper

`ApplyStatus` currently forwards to `ApplyEffect`. After the skip above, direct `sim.ApplyStatus(ally, reflectFx)` will no-op instead of storing a dead Reflect. That is the intended playable-roster policy. Tests that still need to store a gap effect for consumer experiments should construct the status instance themselves, not go through ApplyEffect.

## V01 / V02 / V03 expected behavior

| ID | Entry | Result |
|---|---|---|
| V01 | `CatalogJson.Load` with new `status.apply`+Reflect (legal params) | `ContentValidationException` with id/op/kind; not in `PlayableEffects`; live unchanged |
| V02 | Install custom A, then Load unknown opcode or bad params | throw; `LiveFingerprint` and object fields still A (not builtin, not partial B) |
| V03 | Candidate skill + candidate effects in one Load / `ActivateCandidate` | `CheckSkill(sk, candidate.Effects)` only; valid pair installs together; invalid effect fails before swap |

## Residual

- Cast/ApplyEffect hook is this patch, not this task’s source edit.
- Builtin `dot_flame` remains inventory history; playable roster excludes it.
- `Catalog.PlayableIds` is still “non-enemy chars”, not the strict effect closure.
