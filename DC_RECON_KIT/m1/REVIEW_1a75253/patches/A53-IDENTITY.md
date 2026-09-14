# A53-IDENTITY — expose BattleSim overlays for fight identity

**Owner:** integrator (`BattleSim.cs` only).  
**Already landed:** `client/Assets/Scripts/Resonance.Battle/Core/BattleReplay.cs` (`BattleContentIdentity` / Verify isolation).  
**Do not edit** `BattleSim.cs` from the identity agent. No `CatalogJson`. No `G2A53*`. No commit.

`Compute` / `DataIdentity` now hash catalog + party/stage/growth + bound `DesignPlaceholderPolicy` + skill/effect overlays. Overlay maps are private (`_skillOverlay` / `_effectOverlay`). The identity agent reads them via `BattleContentIdentity.TryReadSimOverlays` (public `SkillOverlays`/`EffectOverlays` first, then those private fields). QA can also call `Compute(sim, party, stage, skillOverlay, effectOverlay)` and `PolicyKey()` without BattleSim accessors.

`BattleSim.ContentFingerprint()` stays catalog-global (`g2id1-` + `Fingerprint()`). Do not fold overlays/policy into that alias.

## Why

`OverlaySkill` / `OverlayEffect` change `ResolveSkill` / `ResolveEffect` and auto DriveGain. Without a stable read of those maps, `Compute(sim, …)` cannot prove a fight-complete identity after the private-field names move or IL2CPP strips unused privates.

## Hunk — public overlay snapshots

**File:** `client/Assets/Scripts/Resonance.Battle/Core/BattleSim.cs`  
Place next to `OverlaySkill` / `OverlayEffect` (~L293).

```diff
         public void OverlayEffect(string id, EffectDef fx)
         {
             if (string.IsNullOrEmpty(id) || fx == null) return;
             if (_effectOverlay == null) _effectOverlay = new Dictionary<string, EffectDef>();
             _effectOverlay[id] = fx;
         }
+
+        /// <summary>Per-fight skill replacements consumed by ResolveSkill. Null when none applied.</summary>
+        public IReadOnlyDictionary<string, SkillDef> SkillOverlays => _skillOverlay;
+        /// <summary>Per-fight effect replacements consumed by ResolveEffect. Null when none applied.</summary>
+        public IReadOnlyDictionary<string, EffectDef> EffectOverlays => _effectOverlay;
```

No copy. Sorted hashing stays in `BattleContentIdentity`. Do not write overlays after battle start from GameRoot.

## Residual after this hunk

- Identity agent does not need another BattleSim edit for policy (`DesignPlaceholderPolicy` is already public).
- `ContentFingerprint` remains catalog-only by design.
- Shared `BattleReplayer.Divergences` / `Unconsumed` stay last-writer mirrors (`SharedDiagnosticStaticsRequireSerializedTests = true`). After `Verify`, use `ReplayReport.CommandDiff` / `Unconsumed` / `Diagnostics`.
