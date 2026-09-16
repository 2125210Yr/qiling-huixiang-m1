# F2-FEVER — real vfxGood window poll (B1-F2)

**task_id:** `B1-F2`  
**batch:** `B1` / G2 Path A  
**baseline:** `1b2ca8e4ff56d9f0986e158fc97b50498f735cb3`  
**Lease writes:** `NaturalPlayRuntime.cs`, `VfxGoodButton.cs`, `VfxGoodWindow.cs` (pure math helper), `VfxGoodWindowTests.cs`, this file.  
**Do not write:** `BattleSim.cs`, `GameRoot.cs`, `BattleHud.cs` (hit→Perfect / miss→Good kept), `Catalog.cs`, `VerificationCatalog.cs`, factory/readback, G2A53*, run7, `dist/`.  
**Do not:** inject Fever / Drive / HP / Charge / time / outcome; `FirePerfect`; `Submit(DriveResolve(Perfect))` from runtime; widen the coin window; skip Fever goals; claim PATH_A pass; G3 / T27 / 90% / M1.

Evidence: ENGINEERING. Not GL.

---

## 1. Why 3×Good never opened Fever (window, not injection)

Archive: `DC_RECON_KIT/m1/REVIEW_1b2ca8e/artifacts/natural-play/20260914T153139-np_fever_v1/`

Fight-1 (`np-20260914T153202-001`): `outcome=Defeat`, `drive=88`, `fever=False`, `feverEver=False`. Commands: 3× Player `DriveBegin` + 3× `DriveResolve` **`timing=Good`**.

`BattleHud` maps `VfxGoodButton` hit→`Perfect` (`QteFever` +40) / miss→`Good` (+15). Open Fever needs gauge 100. 3×Good = 45. The Fever scenario note (“high HP so three QTE cycles survive”) is a **three-Perfect** budget (120), not three Goods.

`TapQte(..., waitWindow:true)` waited a blind `0.60f` unscaled second after **finding** the coin, then `PointerAt`. Coin pulse: `Cycle=1.2`, `WindowAt=0.60`, `WindowHalf=0.08`, `InWindow` uses button `_age` from `Begin()`. Find delay (Show already aged the coin) + 0.60 wait + pointer latency lands after `0.68` → miss → Good.

Party did survive three QTEs. They never hit the pulse. This lease does **not** set `FeverActive`, does **not** raise `QteFever(Good)`, and does **not** add a fixture Perfect path.

---

## 2. What changed

### `VfxGoodWindow` (pure math, no Unity play mode)

Public `Cycle` / `WindowAt` / `WindowHalf` (unchanged 1.2 / 0.60 / 0.08). `IsInWindow(age)`, `IsEarlyInWindow(age)` (in-window and ≥ Half remaining so pointer latency stays inside the window), `SecondsUntilWindow(age)`, `SecondsLeftInWindow(age)`.

Regression: findAge `0.12` + blind `0.60` = `0.72` is **outside** the window. Do not widen Half to cover that miss.

### `VfxGoodButton`

Exposes the same public consts plus read-only `Age`, `IsInWindow`, `IsEarlyInWindow`, `SecondsUntilWindow`, `Live`. `OnClick` still uses the same window predicate (now via `VfxGoodWindow`). Window not widened.

### `NaturalPlayRuntime.TapQte(..., waitWindow:true)`

Polls the live `vfxGood` coin until `IsEarlyInWindow`, then the same `UnityEngine.EventSystem` pointer path. Records `LastDriveTiming` / `FeverGauge` after the real `Submit` (HUD). Still no `FirePerfect`, no runtime `DriveResolve(Perfect)`.

`VerificationScenario.Fever` EnemyAtkMul / TimeLimit **not** touched — three in-window Perfects already open Fever on the current stage if the pulse is hit.

---

## 3. Tests

`dotnet test tools/BattleSim.Tests/BattleSim.Tests.csproj --filter FullyQualifiedName~VfxGoodWindowTests`

**7 passed / 0 failed** (2026-09-15). Documents the blind-wait miss and that 3×Good < 100 ≤ 3×Perfect. Does not raise `QteFever(Good)`.

---

## 4. Unity `np.fever.v1`

**Launched.** **BLOCKED** — not NOT_RUN, not a pass.

| Item | Fact |
|---|---|
| Command | `run-natural-play.ps1 -Scenario np.fever.v1 -Run` (no `-batchmode` / `-nographics` / `-quit`) |
| Stamp | `20260914T171516` |
| Archive | `DC_RECON_KIT/m1/REVIEW_1b2ca8e/artifacts/natural-play/20260914T171516-np_fever_v1/` |
| Log | `logs/editor-np-fever-v1-20260914T171516.log` |
| Play Mode | never entered |
| `natural-play.result.txt` | missing (CliFever did not run) |
| feverEver / DriveResolve / FeverTap | **no new tape** — Safe Mode before Play |

Safe Mode: `executeMethod class 'NaturalPlaySmoke' could not be found`.

Compiler (not F2 files):

```
Assets\Scripts\Resonance.Battle\Core\BattleReplay.cs(1396,27): error CS0246: The type or namespace name 'FiveStat' could not be found
```

`BattleReplay.cs` is B1-F1's lease. F2 did not edit it. `VfxGoodWindow.cs` imported; no CS error on F2 files. `dotnet test` `VfxGoodWindowTests` still 7/7. CS0104 `FindFirstObjectByType` was not reverted.

Editor pid **29856** was quit with `CloseMainWindow` (WaitForExit=True). Locks freed. F1 hoist confirmed: `FiveStat` / `GearCombo` sit above `_byFive`. Not reverted.

### Relaunch `20260914T175019` (this follow-up)

| Item | Fact |
|---|---|
| Editor quit | pid 29856 CloseMainWindow → exited; `EditorInstance.json` and `UnityLockfile` gone before relaunch |
| Compile | **ok** — no `error CS` in `logs/editor-np-fever-v1-20260914T175019.log` |
| Play | ran (`[NATURAL-PLAY] entering play mode`) |
| First line | `FAIL OpenQte#3` |
| Archive | `DC_RECON_KIT/m1/REVIEW_1b2ca8e/artifacts/natural-play/20260914T175019-np_fever_v1/` |
| feverEver | False |
| DriveResolve | seq2 Perfect (gauge 40), seq4 Perfect (gauge 80). Poll notes `age=0.521` / `0.525` |
| 3rd QTE | DriveBegin seq5 `accepted=False reason=UnitDead` (p0). WaitQteOpen timed out |
| FeverTap | none |
| Window-poll rewrite | **not done** |
| PATH_A | **not claimed** |

After this run Unity has exited (`UnityCount=0`, locks free).

---

## 5. Acceptance (this lease)

| Item | Status |
|---|---|
| Root cause = late pulse → Good, not missing injection | documented |
| Read-only window query | landed |
| Poll then EventSystem tap | landed |
| No Fever/Drive/HP writes, no FirePerfect | kept |
| FinishQte mapping | untouched |
| PATH_A pass claim | **not claimed** |
| Unity `np.fever.v1` | First launch **BLOCKED** CS0246. Relaunch **FAIL OpenQte#3** (2×Perfect, p0 dead). No PATH_A claim. |

---

## 6. Living Drive slot (follow-up)

Window poll stayed. `TapQte` / `VfxGoodWindow` not rewritten.

`20260914T175019` failed `OpenQte#3` because `PlayFeverNaturally` always tapped `p0`. After two Perfects, C001/p0 was dead (`DriveBegin slot=0 UnitDead`). Drive was 100; other allies still alive.

**Runtime only:** fever Drive-open now uses `OpenFeverDriveQte` → first living `p0`..`p4` that `CanAcceptSkillInput`, has a hittable portrait, and a playable Drive skill. EventSystem tap on that portrait. HUD still Submits Player `DriveBegin`. No FeverActive write. No runtime `DriveResolve`. If none living: Fail with the skip list.

`VerificationScenario.Fever.EnemyAtkMul` not touched (prefer living-slot Drive first).

Unity relaunch of `np.fever.v1` after this hunk: see next block.

### Relaunch `20260914T181042`

| Item | Fact |
|---|---|
| Editor | free before launch; Unity gone after |
| Compile | ok |
| First line | `FAIL FeverPlay` (real; 160s timeout, `missing=` empty) |
| Archive | `DC_RECON_KIT/m1/REVIEW_1b2ca8e/artifacts/natural-play/20260914T181042-np_fever_v1/` |
| feverEver | **True** (FeverReached via QTE Submit, not injected) |
| DriveBegin | slot 0, 0, **1** (OpenQte#3 tapped `p1`; not dead p0) |
| DriveResolve | Perfect, Perfect, Perfect (gauge 40→80→Fever) |
| FeverTap | Player accepted slots **1** and **2** |
| EnemyAtkMul | unchanged |
| PATH_A | **not claimed** |

Goals were recorded at ~74s (`slots=1,2`). First line stays FAIL because `PlayFeverNaturally` still Fails on the 160s cap after goals. Window-poll / TapQte not rewritten.

---

## 7. Yield-break after Fever goals (follow-up)

`TapQte` / `VfxGoodWindow` / `OpenFeverDriveQte` living-slot not rewritten. No inject. No EnemyAtkMul.

`PlayFeverNaturally` now records `FeverPlay` **once** (`goalsNoted`) when missing becomes empty, then **yield break** immediately. 160s Fail only if goals still missing. Does not spin, does not Fail with `missing=` empty, does not wait for wipe. `Run()` continues to WaitResult / rematch / PAUSE / HOME / `Pass()`. No `Pass()` from PlayFeverNaturally. Run() fever-goals autoExit **not** added unless WaitResult is the new first-line FAIL.

### Relaunch `20260914T183658`

| Item | Fact |
|---|---|
| Compile | ok |
| First line | `PASS` |
| Archive | `DC_RECON_KIT/m1/REVIEW_1b2ca8e/artifacts/natural-play/20260914T183658-np_fever_v1/` |
| feverEver (fight-1) | True |
| DriveBegin | slot 0, 0, 0 (p0 alive; living-slot picker) |
| DriveResolve | Perfect, Perfect, Perfect |
| FeverTap | Player accepted slots **0** and **1** |
| Run() fever-goals exit | **not added** (WaitResult got Victory / CLEAR!!) |
| PATH_A | **not claimed** |
