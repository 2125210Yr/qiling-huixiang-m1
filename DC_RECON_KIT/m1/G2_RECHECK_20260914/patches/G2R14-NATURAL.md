# G2R14-NATURAL — TickUnit DriveGain floor + natural-play verification

**task_id:** `G2R14-NATURAL`  
**Lease:** `NaturalPlayRuntime.cs`, `NaturalPlaySmoke.cs`, new verification config under `Resonance.Battle/Content/` or `Resonance.App/Debug/`, this file.  
**Do not edit from this task:** `BattleSim.cs`, `BattleHud.cs`, `GameRoot.cs`, `G2Recheck*.cs`.  
**HUD contract (unchanged):** `BattleHud.OnPortraitTap` maps `Drive>=100` to `DriveBegin`. Do not change that mapping.  
**Compile/test this session:** NOT_RUN. Unity Editor was not launched. Historical run7 was not rewritten.

---

## 0. What this task shipped without a BattleSim edit

Named, versioned `DESIGN_PLACEHOLDER` scenarios (`G2R14-NP-1`):

| Name | Regression | Required goals | ChargeTimeSec | DeclaredAutoDriveGain | EnemyHpMul | TimeLimitSec |
|---|---|---|---|---|---|---|
| `np.basic.v1` | N01 | Tap, Slide | 1.25 | 2 | 3.0 | 120 |
| `np.fever.v1` | N02 | QTE, **Fever**, FeverTapA, FeverTapB | 1.25 | 2 | 20.0 | 180 |
| `np.auto.v1` | N03 | AutoFull, AutoSkillObserved, AutoSubmit, AutoManual | 1.25 | 2 | 16.0 | 180 |
| `np.matrix.v1` | N01–N03 + L01/L02 | three fights then pause→home | same policy | 2 | per stage | per stage |

These are **not** original-game / GL numbers. `VerificationCatalog.Apply` runs **before** FIGHT (`StartBattleAt`). After start: no HP/Drive/Charge/Fever/time/outcome writes, no `FirePerfect`, no VFX-clear-as-progress. Pointer path remains `UnityEngine.EventSystem`.

`TickUnit` still does `Drive += Math.Max(14f, autoSkill.DriveGain)` on this tree. Documented, not edited.

Allies spawn at `Charge=35` (`BattleSim.Spawn`). With `ChargeTimeSec=1.25`, p0 is ready in ~0.8s. First auto wave is ~2.0s (`autoCd = Max(1.1, 2.4 - Agl/2000)`), Drive ≈ 70. Second wave ~4.0s, Drive = 100. Slide (not remapped) then Tap while `Drive<100` is the N01 window. **N01 reachability does not require this hunk.** The hunk is for N04: honor the declared `DriveGain=2` when `DesignPlaceholderPolicy.HonorDeclaredAutoDriveGain` is true.

Historical run7 (`DC_RECON_KIT/m1/G2_REVIEW_20260913/artifacts/natural-play/run7`) stays **FAIL BattlePlay missing=Tap,Slide**.

---

## 1. Exact BattleSim hunk (integrator only)

File: `client/Assets/Scripts/Resonance.Battle/Core/BattleSim.cs`  
Method: `TickUnit`  
Current lines (HEAD / 2a00445 combat):

```csharp
            u.AutoTimer += autoDt;
            var autoCd = Math.Max(1.1f, 2.4f - u.Def.Agl / 2000f);
            if (u.AutoTimer >= autoCd)
            {
                u.AutoTimer = 0f;
                var autoSkill = ResolveSkill(u.Def.AutoSkillId);
                if (autoSkill != null)
                {
                    Cast(u, ally, autoSkill, 1f);
                    if (ally)
                        Drive = Math.Min(100f, Drive + Math.Max(14f, autoSkill.DriveGain));
                }
            }
```

Replace the `if (ally) Drive = ...` line with:

```csharp
                    if (ally)
                    {
                        // DESIGN_PLACEHOLDER: honor declared auto DriveGain when verification policy is bound.
                        // Production keeps the engineering floor of 14. Neither number is GL.
                        var gain = autoSkill.DriveGain;
                        if (!DesignPlaceholderPolicy.HonorDeclaredAutoDriveGain)
                            gain = Math.Max(14, gain);
                        Drive = Math.Min(100f, Drive + gain);
                    }
```

Unified diff:

```diff
--- a/client/Assets/Scripts/Resonance.Battle/Core/BattleSim.cs
+++ b/client/Assets/Scripts/Resonance.Battle/Core/BattleSim.cs
@@ -621,8 +621,16 @@
                 if (autoSkill != null)
                 {
                     Cast(u, ally, autoSkill, 1f);
                     if (ally)
-                        Drive = Math.Min(100f, Drive + Math.Max(14f, autoSkill.DriveGain));
+                    {
+                        // DESIGN_PLACEHOLDER: honor declared auto DriveGain when verification policy is bound.
+                        // Production keeps the engineering floor of 14. Neither number is GL.
+                        var gain = autoSkill.DriveGain;
+                        if (!DesignPlaceholderPolicy.HonorDeclaredAutoDriveGain)
+                            gain = Math.Max(14, gain);
+                        Drive = Math.Min(100f, Drive + gain);
+                    }
                 }
             }
```

`DesignPlaceholderPolicy` lives in `client/Assets/Scripts/Resonance.Battle/Content/DesignPlaceholderPolicy.cs` (same assembly). Default `HonorDeclaredAutoDriveGain=false` (cleared on `RestoreBuiltin`). Natural-play binds it `true` only while the verification catalog is installed.

Do **not** claim `2` or `14` is GL.

---

## 2. Related, not owned here: AutoFire still bypasses Submit (X05 / N03)

`AutoFireSkills` / `AutoFireDrive` still call `TryTap` / `TrySlide` / `TryBeginDrive` + `ResolveDrive(Great)` and do not `Submit`. Auto Fever **does** `Submit(FeverTap, Source=Auto)` from `TickFever`.

N03 therefore prefers `CommandLog` rows with `Source=Auto` and kind in `{Tap,Slide,DriveBegin,DriveResolve,FeverTap}`. Until REPLAY lands AutoFire→Submit, the auto scenario can still record Source=Auto **FeverTap** if Full Auto reaches Fever on the tanky `np.auto.v1` wave. Missing Submit Auto skills is a SoftFail, not a forged PASS.

Do not apply an AutoFire hunk from this file; that lease is `G2R14-REPLAY`.

---

## 3. Per-battle evidence (L01/L02) — already in NaturalPlayRuntime

Before `NEXT` / `RETRY` / quit:

1. Assign `battle_id` = `np-yyyyMMddTHHmmss-00N`
2. Freeze `RunHeader()` at battle start
3. Write `captures/natural-play/battles/<id>/{header,commands,events,result,digest,scenario}.txt`
4. Session `natural-play.result.txt` cites `battle_ids=`
5. `natural-play.events.txt` is an **index**, not fight-2 overwrite of fight-1

Do not persist fight 2 into fight 1 files. `PersistCurrentBattle` refuses to write when `GameRoot.Battle` already points at a new sim and the previous capture was saved.

---

## 4. What was not run

- Unity Editor / play mode / natural-play smoke
- 203-core TRX
- Phone / OS touch (EventSystem only; `os_touch=NOT_CLAIMED`)
- No git commit
