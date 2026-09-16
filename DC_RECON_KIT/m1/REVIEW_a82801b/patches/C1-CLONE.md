# C1-CLONE — VerificationCatalog.CloneEffects keeps declared Side/HasTarget/Target

**task_id:** `C-C1`  
**batch:** `C`  
**baseline:** `a82801b80fd81fb4f8ff30cf45b72b30fcd63521`  
**Lease writes:** `VerificationCatalog.cs`, new `tools/BattleSim.Tests/G2C1CloneTargetTests.cs`, this file.  
**Did not write:** `BattleSim*`, `BattleHud`, `GameRoot`, `BattleReplay`, `NaturalPlay*`, `Catalog.cs`, existing tests/fixtures.

Evidence: ENGINEERING. Not GL.

---

## What changed

`VerificationCatalog.CloneEffects()` now publishes `Catalog.CloneEffect(e)` for every builtin row. That helper already copies `Id/Opcode/Kind/HasTarget/Target/Side/Magnitude/DurationSec/MaxStack/SourceTier/Group` and `EffectCapability.CopyLifecycle` (`Trigger`/`PeriodSec`).

The previous local `new EffectDef { ... }` omitted `HasTarget`/`Target`/`Side`. New instances therefore had `Side=FromRule` and `HasTarget=false`. `Catalog.Install` published those incomplete clones as the live table.

`ApplyNamedSubstitute` is unchanged: only `PlayableSubstitutes.StripDotFlame` runs, and it only clears `SkillDef.EffectId` on `dot_flame` links (C001_slide). No EffectDef field is a declared substitute. No HP/ATK tuning. No effects removed. Gates untouched.

## Why

Builtin `burst_atk` is Ally; `C001_drive` damage picker is `HighestAtkEnemies`. Under the old overlay, `TargetSemantics.Side` inherited the foe rule and `ApplyLinkedEffect` buffed living enemies. The same hole dropped `taunt` Self and `def_down` Foe.

## Tests

| case | method |
|---|---|
| C1-T1 | `Resonance.Tests.G2C1CloneTargetTests.C1T1_Apply_NpBasic_PreservesBuiltinEffectDefFields` |
| C1-T2 | `Resonance.Tests.G2C1CloneTargetTests.C1T2_C001Drive_BurstAtkOnAlliesNotEnemies_TauntOnCasterViaOverlayTap` |

C1-T1 snapshots every builtin EffectDef after `Catalog.BuildBuiltin`, runs `VerificationCatalog.Apply` for `np.basic.v1`, asserts clone `Side/HasTarget/Target/Opcode/Kind/Trigger/PeriodSec/MaxStack/Magnitude/DurationSec/SourceTier/Group` match, then asserts the original object refs and the post-`RestoreBuiltin` table still match.

C1-T2 uses `Catalog.DefaultParty` (C001 first) + the installed verification stage. Pre-tick fixtures inflate HP and stun foes; Drive accrues by Tick (no post-Tick Drive write). Real `Submit(BattleCommand.DriveBegin(0))` then `Submit(BattleCommand.DriveResolve(DriveTiming.Perfect))`. Asserts `burst_atk` on living allies and on **no** enemy.

## Red / green

Temporarily restored the incomplete `new EffectDef { ... }` (no Side/HasTarget/Target), ran, then put `Catalog.CloneEffect` back.

```
dotnet test tools/BattleSim.Tests/BattleSim.Tests.csproj --nologo --filter "FullyQualifiedName~G2C1"
```

**Red (old CloneEffects):** 失败 2 / 通过 0 / 跳过 0 / 总计 2

- C1-T1: `cloned table after VerificationCatalog.Apply id=dot_flame field Side expected=Foe actual=FromRule`
- C1-T2: `C001_drive burst_atk must land on living allies. ally=0 enemy=3`

**Green (CloneEffect):** 失败 0 / 通过 2 / 跳过 0 / 总计 2

Neighbor filter (owned files only; not the unfiltered suite):

```
dotnet test tools/BattleSim.Tests/BattleSim.Tests.csproj --nologo --filter "FullyQualifiedName~G2B1VerificationEntry|FullyQualifiedName~G2A53"
```

**A53 + B1:** 失败 0 / 通过 26 / 跳过 0 / 总计 26  
(4× `G2B1VerificationEntryTests` + 22× `G2A53*`)

## Deviations

- Builtin `CreateBuiltinTables` never sets `HasTarget=true` (Side-only `Fx(...)`). C1-T1 therefore asserts declared Side on `burst_atk=Ally`, `taunt=Self`, `def_down=Foe` and records that no non-default HasTarget row exists to clone.
- DefaultParty has no Tap/Slide that links `taunt` (`C007_drive` is Drive-only). C1-T2 overlays a synthetic Tap (`EffectId=taunt`, still a foe damage picker) and `Submit(Tap)`. A lost Side would taunt the enemy pool; Self must keep it on C001 only.
- First `dotnet test` hit C3 mid-edit: `BattleReplay.cs` referenced `OpeningGrowth.InputsKey` before that helper existed. Waited ~60s and retried. Did not edit C3 files.
- Did not run the unfiltered suite (C4).
- No git commit/push.
