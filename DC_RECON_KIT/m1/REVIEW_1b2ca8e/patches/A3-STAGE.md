# A3-STAGE — expose BattleSim.ActiveStage for fight identity

**Owner:** integrator (`BattleSim.cs` only).  
**Already landed:** `BattleContentIdentity.Compute` prefers the sim's live StageDef (`TryReadSimStage` → public `ActiveStage`, else private `_stage`). Falls back to `FindStage(stageId)` only when the instance has no stage. `G2B1StageIdentityTests` covers uninstalled same-Id clones.  
**Do not edit** `BattleSim.cs` from the A3 agent. No `CatalogJson`. No `G2A53*`. No commit.

`ContentFingerprint` / `Fingerprint` stay catalog-global (`g2id1-`). Do not fold the instance stage into that alias.

## Why

`BattleSim` stores the constructor `StageDef` in `_stage` and uses it for `TimeLeft` / waves. Compute used to re-lookup by Id on global Catalog, so two uninstalled clones with the same Id and different `TimeLimitSec` / `EnemyHpMul` hashed equal. Reflection on `_stage` works for Editor/.NET tests; a public getter survives rename/IL2CPP strip.

## Hunk — public live stage

**File:** `client/Assets/Scripts/Resonance.Battle/Core/BattleSim.cs`  
Place next to the overlay getters (`SkillOverlays` / `EffectOverlays`, ~L308).

```diff
         public IReadOnlyDictionary<string, SkillDef> SkillOverlays => _skillOverlay;
         public IReadOnlyDictionary<string, EffectDef> EffectOverlays => _effectOverlay;
+
+        /// <summary>Stage this fight is running. Same object as ctor <c>_stage</c> (VerticalSlice fallback). No copy.</summary>
+        public StageDef ActiveStage => _stage;
```

No copy. `AppendStage` reads the live object (`id/t/hpMul/atkMul/defMul/diff/w0/w1`). Do not replace `_stage` after battle start.

## Residual after this hunk

- Catalog-global `ContentFingerprint` still ignores uninstalled ctor stages (by design).
- `FindStage` remains the fallback when `sim` is null or has no instance stage.
