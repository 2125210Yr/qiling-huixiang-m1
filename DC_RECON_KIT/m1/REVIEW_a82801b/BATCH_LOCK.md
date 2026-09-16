# REVIEW_a82801b 批次锁（C 批）

batch_id: `C`
coordinator: this session (review + serial merge; no second coordinator)
baseline: `a82801b80fd81fb4f8ff30cf45b72b30fcd63521`
HEAD at dispatch: `a82801b` (= origin/m1-gt6-review). Combat tree clean except the integrator hunk below.

Scope: **C1–C4 only** (REVIEW.md / NEXT_TASK.md / REGRESSION_CASES.md in this dir). No G3. No G0/G1 redo.
Keep: strict Unplayable gate, named substitutes, instance `ActiveStage`, existing recorder + external-input replayer, F2 window/living-slot/goal-exit, natural-play scenarios.

## Integrator hunk already landed (do not redo / do not revert)

`BattleSim.cs` ctor: `_growth = OpeningGrowth.Copy(growth)`; `OpeningMods` copied; new read-only
`OpeningGrowthInput` / `OpeningMods`. C3 builds on these; C2 must not touch that region.

## File ownership (one writer per file; others deliver patches to `patches/<task>.md`)

| task_id | Sole writer of | Must not write |
|---|---|---|
| C-C1 | **closed as executed** `f7184820` — `CloneEffects` → `Catalog.CloneEffect`; `G2C1CloneTargetTests` 2/2 (red 0/2 on old copy); A53+B1 26/26. Patch `patches/C1-CLONE.md` | do not respawn |
| C-C2 | **closed as executed** `689c42ef` — core tick-budget holds (slide 1.47s / drive 0.70s non-QTE / wave 2.00s, DESIGN_PLACEHOLDER), `HoldSim` setter internal, HUD/WavePreview read `HoldLeftSec`, `GameRoot.TryFocusEnemy` → `Submit(FocusEnemy, Player)`; `G2C2*` 7/7; neighbours 97/1 skip. Patch `patches/C2-HOLD-FOCUS.md`. **Coordinator pending:** `ClockKey` += 3 hold fields in `BattleReplay.cs` after C3 lease closes | do not respawn |
| C-C3 | **closed as executed** `a31c3745` — Capture copies `OpeningGrowthInput`/`OpeningMods`; JSON `openingProgress` (`;r=` Reserve, `;sk=`), `openingMods`, `openingSource` (`input`/`legacy-recovered`/`incomplete`), `openingInputsIdentity`; factory passes recorded mods; `G2C3` 2/2; B1/A53/replay 39/39. Patch `patches/C3-OPENING.md` | do not respawn |
| coordinator | **done** — `ClockKey` += 3 hold fields; 3 pre-C2 tests updated to drain policy hold. Full unfiltered: 271/0/1 skip. Patch `patches/C-MERGE.md`. **Tree frozen at the C-merge commit.** | — |
| C-C4 | on the frozen commit: Unity natural play basic/fever/auto, unfiltered TRX, readback (new tapes Match=True, legacy stays FAIL), build, recording, `artifacts/**` | any source file under `client/Assets/Scripts` or `tools/BattleSim.Tests` |

Shared test fixtures (`G2ReviewFixtures.cs`, `G2RecheckFixtures.cs`, existing test files) are read-only in this batch. Add new test files only.

## Rules

- Parallel agents share one working tree. A compile error in a file you do not own = another lease mid-edit: wait 60 s and retry, do not edit it, report if it persists 5 min.
- Full suite stays serial (`xunit.runner.json`). Run filtered tests while developing; the unfiltered TRX is C4 on the frozen tree only.
- No git commit/push. Coordinator commits when the user asks.
- After battle start: no HP/Drive/Charge/Fever/time/outcome writes, no FirePerfect. DESIGN_PLACEHOLDER pre-battle config only; never label as GL.
- Do not hard-code a 29-tick delay to make the old basic tape green. Do not drop TickIndex/EventDiff/TimeLeft/Version comparisons. Old tapes stay FAIL with their missing-input label.
- Do not reset user leftovers (`dist/`, `client/f_*.jpg`, Unity csproj, `_Recovery`, `codex专区`, hires tiles).
- Unrun = NOT_RUN. Environment failure = BLOCKED + real command/error.
- No T27/90%/M1 claims. `M1_FIDELITY=DEFERRED_NOT_REMOVED`. `G3_NOT_STARTED`.
