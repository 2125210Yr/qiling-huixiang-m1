# G2R14-QTE — X06 restore-tick TimeLeft double-charge

**task_id:** `G2R14-QTE`  
**Lease:** this file only. Integrator applies hunks to `client/Assets/Scripts/Resonance.Battle/Core/BattleSim.cs`.  
**Do not edit** `BattleSim.cs`, `G2ReviewQteClockTests.cs`, or any other test from this task.  
**Baseline:** `2a00445` combat clock (HEAD art-only delta is irrelevant).  
**Compile/test this session:** NOT_RUN (lease forbids the production edit).  
**Integrator:** hunks A+B applied to `BattleSim.cs` 2026-09-14 (this session). Existing QTE suite to be re-run by coordinator.

---

## 1. Root cause (current tree)

`Tick` and pending-QTE `ReleaseBlocks` each spend the **same** stage policy step. That is correct while QTE stays pending (`ReleaseBlocks` returns `false`, so `Tick` never spends). It is wrong on the **restore tick**: QTE hits `QteLimitSec`, `ResolveDrive(Good)` returns, `ReleaseBlocks` returns `true`, and `Tick` spends again.

| Site | Lines | What it spends |
|---|---|---|
| `Tick` after a true `ReleaseBlocks` | 357–361 | `stageDt = ScaleClock(dt, StageCountdownScalesWithSpeed)` → `TimeLeft` + `Stats.Tick` |
| `ReleaseBlocks` manual QTE wait | 436–441 | `stageDtQ` (same `ScaleClock` / same policy) → `TimeLeft` + `Stats.Tick` |
| Restore branch | 455–457, then 461 | `_qteElapsed >= QteLimitSec` → `ResolveDrive(Good)` → `return true` → outer spend runs |

Authoritative clock accessors (unchanged):

- `QteElapsed` L251 — pending-only view of `_qteElapsed`
- `StageCountdownScalesWithSpeed` L140
- `DriveQteScalesWithSpeed` L146
- `Scale` L176–179 — `true` → `battleDt` (`TickDt * Speed`); `false` → bare `TickDt`

Existing `G2ReviewQteClockTests` **do not catch this**:

- **Q01** — 10 pending ticks (limit still 7s), then waits for timeout; asserts one Drive settle, **never `TimeLeft` on the restore tick**.
- **Q02** — first 10 pending ticks only; `ReleaseBlocks` returns `false`, so the double-spend path is never taken.

`Stats.Elapsed` is hit by the same pair of `Stats.Tick` calls (FightStats L45–48). Same restore-tick extra step.

---

## 2. Proposed control flow

**Invariant:** one `stageDt` budget per `TickIndex`. Hold still consumes **zero**. QTE wait, QTE restore, and free ticks consume **exactly one** stage-policy step. QTE uses only `qteDt`.

```
Tick:
  if terminal or Paused: return
  TickIndex++
  dt      = BattleDt(Speed)
  stageDt = ScaleClock(dt, StageCountdownScalesWithSpeed)   // compute once
  if !ReleaseBlocks(dt, stageDt): return
  // TimeLeft already applied; do not subtract again
  TickStatus / Slide / Fever / units / auto-fire / waves

ReleaseBlocks(dt, stageDt):
  if hold sticky or hold-watchdog still running:
      return false                         // no stage spend (StayHeld freeze)
  TimeLeft -= stageDt                      // THE only spend this Tick
  Stats.Tick(stageDt)
  if TimeLeft <= 0:
      defeat, clear PendingDriveSlot/_qteElapsed, return false
      // Q04: stage expiry during QTE still cancels Drive (no ResolveDrive)
  if no pending Drive:
      return InProgress
  if Auto == Full:
      ResolveDrive(Great)                  // fallback; TryBeginDrive already auto-resolves
  else:
      qteDt = ScaleClock(dt, DriveQteScalesWithSpeed)
      _qteElapsed += qteDt                 // QTE policy only
      if _qteElapsed < QteLimitSec:
          return false                     // still blocking; stage already spent
      ResolveDrive(Good)                   // restore: settle once, then Tick continues
  return Outcome == InProgress
```

Why TimeLeft **before** QTE expiry (not “skip spend on restore, let Tick spend later”):

- Q04 (`TimeLeft = TickDt * 0.5`, default 7s limit): one spend, defeat, QTE cleared, **no** Drive. Preserved.
- Dual expiry (`QteLimit = TickDt` **and** `TimeLeft` too small for one `stageDt`): same as today — stage timeout wins, no `ResolveDrive`.
- Restore with ample `TimeLeft`: one spend, then `ResolveDrive(Good)`, then the rest of `Tick` with **no** second subtract.

Do **not** keep `stageDtQ` inside the QTE branch. That is the second cashier.

---

## 3. Apply-ready hunks

File: `client/Assets/Scripts/Resonance.Battle/Core/BattleSim.cs`  
Apply **Hunk A then Hunk B** (A moves the `stageDt` compute; B is the only spender).

### Hunk A — `Tick` (L351–372): compute `stageDt` once, never spend here

```diff
--- a/client/Assets/Scripts/Resonance.Battle/Core/BattleSim.cs
+++ b/client/Assets/Scripts/Resonance.Battle/Core/BattleSim.cs
@@ -354,23 +354,10 @@
             TickIndex++;
             var dt = Clocks != null ? Clocks.BattleDt(Speed) : TickDt * (Speed < 1 ? 1 : Speed);
             if (dt <= 0f) dt = TickDt;
-            if (!ReleaseBlocks(dt)) return;
-
             var stageDt = ScaleClock(dt, Clocks != null && Clocks.StageCountdownScalesWithSpeed);
-            TimeLeft -= stageDt;
-            Stats.Tick(stageDt);
-            if (TimeLeft <= 0f)
-            {
-                TimeLeft = 0f;
-                if (Outcome == BattleOutcome.InProgress)
-                {
-                    Outcome = BattleOutcome.Defeat;
-                    LastEvent = "时间耗尽";
-                    NoteResult("timeout");
-                }
-                return;
-            }
+            if (!ReleaseBlocks(dt, stageDt)) return;
 
             TickStatus(ScaleClock(dt, Clocks != null && Clocks.StatusDurationScalesWithSpeed));
```

### Hunk B — `ReleaseBlocks` (L414–462): one stage spend after hold; QTE advances only `qteDt`

```diff
--- a/client/Assets/Scripts/Resonance.Battle/Core/BattleSim.cs
+++ b/client/Assets/Scripts/Resonance.Battle/Core/BattleSim.cs
@@ -411,7 +411,7 @@
             return battleDt > 0f ? battleDt : TickDt;
         }
 
-        bool ReleaseBlocks(float dt)
+        bool ReleaseBlocks(float dt, float stageDt)
         {
             if (_holdSim)
             {
                 if (_holdSticky)
                     return false;
                 var holdDt = ScaleClock(dt, Clocks != null && Clocks.HoldWatchdogScalesWithSpeed);
                 _holdElapsed += holdDt;
                 var holdLimit = Clocks != null ? Clocks.HoldTimeoutSec : HoldTimeoutSec;
                 if (_holdElapsed < holdLimit)
                     return false;
                 HoldSim = false;
             }
             else
                 _holdElapsed = 0f;
 
+            // X06: single TimeLeft / Stats budget for this Tick (wait, restore, or free).
+            TimeLeft -= stageDt;
+            Stats.Tick(stageDt);
+            if (TimeLeft <= 0f)
+            {
+                TimeLeft = 0f;
+                if (Outcome == BattleOutcome.InProgress)
+                {
+                    Outcome = BattleOutcome.Defeat;
+                    LastEvent = "时间耗尽";
+                    NoteResult("timeout");
+                    PendingDriveSlot = -1;
+                    _qteElapsed = 0f;
+                }
+                return false;
+            }
+
             if (PendingDriveSlot >= 0)
             {
                 if (Auto == AutoMode.Full)
                     ResolveDrive(DriveTiming.Great);
                 else
                 {
-                    // Q01/Q02: QTE clock and stage countdown follow their own scaling policies.
+                    // Q01/Q02: QTE clock follows DriveQteScalesWithSpeed; stage already spent above.
                     var qteDt = ScaleClock(dt, Clocks != null && Clocks.DriveQteScalesWithSpeed);
-                    var stageDtQ = ScaleClock(dt, Clocks != null && Clocks.StageCountdownScalesWithSpeed);
                     _qteElapsed += qteDt;
-                    TimeLeft -= stageDtQ;
-                    Stats.Tick(stageDtQ);
-                    if (TimeLeft <= 0f)
-                    {
-                        TimeLeft = 0f;
-                        if (Outcome == BattleOutcome.InProgress)
-                        {
-                            Outcome = BattleOutcome.Defeat;
-                            LastEvent = "时间耗尽";
-                            NoteResult("timeout");
-                            PendingDriveSlot = -1;
-                            _qteElapsed = 0f;
-                        }
-                        return false;
-                    }
                     if (_qteElapsed < QteLimitSec)
                         return false;
                     ResolveDrive(DriveTiming.Good);
                 }
             }
 
             return Outcome == BattleOutcome.InProgress;
         }
```

No other `ReleaseBlocks(` callers exist.

---

## 4. REGRESSIONS Q01 / Q02 (expected after apply)

Handoff for `G2R14-QA` (`G2Recheck*.cs`). Do **not** land tests from this lease. Do **not** change `G2ReviewQteClockTests` tolerances.

Shared setup (matches REVIEW X06): `NewJp` + `InflateEnemies`, `Auto=Manual`, `Drive=100`, `ChargeAll`, `Clocks.DriveQteTimeoutSec = BattleSim.TickDt`, ample `TimeLeft` (stock stage limit is enough), `TryBeginDrive(0)`, **one** `Tick()`.

Stage step (the only legal `TimeLeft` delta):

```csharp
var dt = sim.Clocks.BattleDt(sim.Speed);
var stageStep = sim.Clocks.Scale(dt, sim.Clocks.StageCountdownScalesWithSpeed);
```

| Case | Speed | StageCountdownScalesWithSpeed | DriveQteScalesWithSpeed | `TimeLeft` delta | Drive |
|---|---|---|---|---|---|
| Q01 | 1 | true (default) | true (default) | exactly `TickDt` | `Good` once, `PendingDriveSlot=-1` |
| Q02-a | 3 | true | true | exactly `3 * TickDt` | same |
| Q02-b | 3 | **false** | **true** | exactly `TickDt` (unscaled stage) | same (`qteDt=3*TickDt >= limit`) |
| Q02-c | 3 | **true** | **false** | exactly `3 * TickDt` | same (`qteDt=TickDt >= limit`) |

Must-assert (precision **4**, not a range):

- `TimeLeft == time0 - stageStep` (and `Stats.Elapsed` += the same `stageStep`)
- `LastDriveTiming == Good`, `LastDriveResolveTick == TickIndex`, Drive cast/event count **+1**
- `QteElapsed` after settle is `0` (pending cleared)

**Forbidden “passes”:**

- Accepting `time0 - 2 * stageStep` (the current bug: wait spend + Tick spend)
- `Assert.InRange` / absolute error ≥ `TickDt` (or ≥ `3*TickDt` at speed 3) — that hides a full extra tick
- Dropping Q02-b/c, or forcing both scale flags equal so a mixed-policy double-charge cannot appear

Broken-tree numbers if hunks are **not** applied (`QteLimit=TickDt`, one Tick):

| Case | buggy `TimeLeft` delta |
|---|---|
| Q01 Speed=1 both scale | `2 * TickDt` |
| Q02-a Speed=3 both scale | `6 * TickDt` |
| Q02-b stage frozen, QTE ×3 | `2 * TickDt` (both cashiers use **stage** policy) |
| Q02-c stage ×3, QTE frozen | `2 * (3 * TickDt)` |

QTE policy never belongs in `TimeLeft`. Mixed policies still double the **stage** step, not stage+qte.

Existing `G2ReviewQteClockTests` Q01/Q02/Q03/Q04 must stay green **without** loosening: pending-tick `TimeLeft` path is unchanged (still one stage step per wait tick); Q04 still expires stage first and refuses late Drive.

---

## 5. Residual risks

1. **Auto==Full + leftover pending + this tick’s `stageDt` empties `TimeLeft`.** Today `ReleaseBlocks` resolves Great *before* `Tick` spends; after this patch stage spend/defeat happens first, so that fallback Drive is skipped. `TryBeginDrive` already auto-resolves in Full, so this is only a mid-QTE Manual→Full switch on the last stage slice. Integrator: do not special-case a second spend to “restore” the old order.
2. **Replay / digest goldens** captured after a *natural 7s* QTE timeout at `2a00445` include the extra `stageDt` (and extra `Stats.Elapsed`). Those TimeLeft fields will shrink by one policy step. That is the bugfix, not a reason to widen compare epsilon.
3. **Restore tick now runs** `TickStatus` / charge / auto-fire after `ResolveDrive`. That was already true (because `ReleaseBlocks` returned true). Not introduced here. Drive damage on a non-lethal InflateEnemies fixture must still be a single cast.
4. **Hold watchdog release tick** still falls through into the single spend (same as today’s fall-through into `Tick`’s spend). Sticky `StayHeld` still spends nothing.
5. This environment did not compile or run tests. Status: **NOT_RUN**. After integrator merge, QA must execute the matrix above plus the existing 203-count core set.

---

## 6. Out of scope / do not do

- Do not move `TimeLeft` spend back into the QTE `else` and also leave it in `Tick`.
- Do not “fix” Q01/Q02 by raising `QteLimit` or skipping the restore tick.
- Do not edit HUD / a second QTE timer; core `QteElapsed` remains the authority.
- Do not touch `G2ReviewQteClockTests.cs`.
