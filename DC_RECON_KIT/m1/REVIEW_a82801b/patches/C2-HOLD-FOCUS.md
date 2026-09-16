# C2-HOLD-FOCUS — core-owned showtime/wave hold + recorded FocusEnemy

**task_id:** `C-C2`  
**batch:** `C`  
**baseline:** `a82801b80fd81fb4f8ff30cf45b72b30fcd63521`  
**Lease writes:** `BattleSim.cs` (hold / Slide / Drive / LoadWave; ctor opening-input region untouched), `BattleSim.Commands.cs`, `BattleHud.cs`, `GameRoot.cs`, `WavePreview.cs`, `VerticalSliceSmokeRuntime.cs`, `CaptureRuntime.cs`, new `G2C2HoldPolicyTests.cs` / `G2C2FocusCommandTests.cs`, this file.  
**Did not write:** `VerificationCatalog.cs`, `BattleReplay.cs`, `NaturalPlay*`, `BattleFighter.cs` (BindFocus already delegated to GameRoot), existing tests/fixtures, C3 ctor hunk.

Evidence: ENGINEERING. Hold seconds are DESIGN_PLACEHOLDER, not GL. No G3.

---

## Design

Real HUD Slide wrote `battle.HoldSim` from `BeginShowtime` and released it with `Time.unscaledDeltaTime`. `BattleReplayer.ReplayCore` has no HUD, so the post-Slide auto landed ~29 ticks early on the 1b2ca8e basic tape (event 12: tick 69 vs 40). That gap was unrecorded wall-clock sim state.

**Core owns the freeze.** On an accepted ally Slide cast, and on the ally Drive cast that HUD used `hold:true` for (Auto Full immediate resolve, Auto `DriveResolve`, QTE timeout — not Player/Fixture/Replay QTE resolve, and not fixture `ResolveDrive()` used by `ArmFever`), `BattleSim` starts a tick-budget hold:

| field | DESIGN_PLACEHOLDER | presentation twin |
|---|---|---|
| `Clocks.SlideShowtimeHoldSec` | `1.47` (`BattleSim.DesignSlideShowtimeHoldSec`) | `VfxShowtime.Duration` |
| `Clocks.DriveCastHoldSec` | `0.70` (`DesignDriveCastHoldSec`) | HUD Drive cast overlay |
| `Clocks.WaveAdvanceHoldSec` | `2.00` (`DesignWaveAdvanceHoldSec`) | `WaveCueBoard.PhaseLifeSec` |

Budget = `TicksForPolicyHold(sec) = max(1, round(sec * TickHz))` (44 / 21 / 60 ticks). Each `Tick()` still increments `TickIndex` then spends one budget tick; `TimeLeft` / charge / auto do not advance. Release is independent of Unity frame rate and of `Speed` (counts raw ticks, matching HUD `unscaledDeltaTime`). `HoldTimeoutSec` watchdog remains for sticky/`FixtureHold` / internal `HoldSim=true` (no policy budget).

A later policy hold may replace a shorter remaining budget (Slide then PHASE in one Tick → 60-tick wave hold). Events: `kind=hold` `opcode=begin.slide|begin.drive|begin.wave` / `end.*`.

**HUD / WavePreview are read-only.** `BeginShowtime` / `TickCutHold` / PHASE `Play`/`Hide` never write `HoldSim`. Overlay life follows `HoldLeftSec` when the core is holding.

**PHASE is core-owned, not a new command.** A Hold/Release `BattleCommandKind` would need replayer awareness in `BattleReplay.cs` (C3). `LoadWave(index>0)` starts the fixed wave hold so ReplayCore reproduces it from `CheckWave` alone. Wave-0 ctor enter does not hold.

**`HoldSim` setter is `internal`.** Presentation (Resonance.App) cannot assign it. Same-assembly fixtures (`BattleSimTests`, `M1ClockModeTests`) still compile. App debug hosts use `DebugForceHold` / `DebugRelease` (logs `fixture.Debug*`; natural-play must never call them). `StayHeld()` / `FixtureHold(bool)` remain.

**C2b:** `GameRoot.TryFocusEnemy` returns bool via `_battle.Submit(BattleCommand.FocusEnemy(slot, Player))`. HUD `BindFocus` already called `TryFocusEnemy`. Factory added on `BattleCommand`.

---

## Files / regions

- `BattleClockPolicy`: `SlideShowtimeHoldSec` / `DriveCastHoldSec` / `WaveAdvanceHoldSec` + ctor defaults.
- `BattleSim.cs`: hold fields, `HoldLeftSec`, `TicksForPolicyHold`, `BeginPolicyHold` / `EndPolicyHold` / `ClearHoldState`, `ReleaseBlocks` policy branch, Slide in `UsePlayerSkill`, Drive flag in `TryBeginDrive` / QTE timeout / `ResolveDriveChecked`, wave hold in `LoadWave(index>0)`. **Ctor C3 opening-input block not touched.**
- `BattleSim.Commands.cs`: `BattleCommand.FocusEnemy`; Auto `DriveResolve` sets `_requestDriveHold`.
- `BattleHud.cs`: `BeginShowtime` / `TickCutHold` overlay-only.
- `GameRoot.cs`: `TryFocusEnemy` → Submit.
- `WavePreview.cs`: PHASE splash no longer writes `HoldSim`; `_life` can follow `HoldLeftSec`.
- `VerticalSliceSmokeRuntime.cs` / `CaptureRuntime.cs`: `DebugRelease` / `DebugForceHold`.

---

## Coordinator must apply in `BattleReplay.cs` (C3 file; not edited here)

`ClockKey` / `BattleContentIdentity.ClockKey` (~line 468) enumerates clock fields and **does not yet include** the three new hold seconds. After C3 is free, append:

```
+ "|" + BattleStateDigest.F3(c.SlideShowtimeHoldSec)
+ "|" + BattleStateDigest.F3(c.DriveCastHoldSec)
+ "|" + BattleStateDigest.F3(c.WaveAdvanceHoldSec)
```

after the existing `HoldTimeoutSec` token (or at the end of the key). Until then ClockIdentity is unchanged, which keeps current tapes from growing a ClockIdentity Diff solely from unused defaults.

**`IsExternalInput`:** no change. It is `Accepted && Source != Auto`. Player `FocusEnemy` is already re-injected. Rejected focus rows stay in CommandLog and are not replayed (same as other rejects).

---

## Tests

| case | method |
|---|---|
| C2-T1 | `G2C2HoldPolicyTests.C2_T1_SlideHold_FreezesClocksThenReleases_AndReplays` |
| C2-T1 setter | `G2C2HoldPolicyTests.C2_T1_HoldSimSetter_IsNotPublic` |
| C2-T1b Drive | `G2C2HoldPolicyTests.C2_T1b_DriveCastHold_Replays` |
| C2-T1b Wave | `G2C2HoldPolicyTests.C2_T1b_WaveAdvanceHold_Replays` |
| C2-T1 Legacy | `G2C2HoldPolicyTests.C2_T1_LegacyBasicTape_HoldUnrecorded_VerifyMatchFalse` |
| C2-T2 | `G2C2FocusCommandTests.C2_T2_FocusEnemy_AcceptedThenTap_ReplayHitTarget` |
| C2-T2 reject | `G2C2FocusCommandTests.C2_T2_FocusEnemy_InvalidAndDead_RecordedRejected` |

Legacy tape: `DC_RECON_KIT/m1/REVIEW_1b2ca8e/artifacts/natural-play/20260914T152459-np_basic_v1/natural-play/battles/np-20260914T152600-001`. Documented FAIL. Not patched. No 29-tick delay inserted.

---

## Red / green

Temporarily removed the Slide `BeginPolicyHold` hook in `UsePlayerSkill`, ran, then restored it.

```
dotnet test tools/BattleSim.Tests/BattleSim.Tests.csproj --nologo --filter "FullyQualifiedName~G2C2"
dotnet test tools/BattleSim.Tests/BattleSim.Tests.csproj --nologo --filter "FullyQualifiedName~C2_T1_SlideHold"
```

**Red (Slide hook off):** `C2_T1_SlideHold_FreezesClocksThenReleases_AndReplays` — `Assert.True(sim.HoldSim)` Expected True, Actual False. 失败 1 / 通过 0 / 总计 1

**Green (hook on):** 失败 0 / 通过 7 / 跳过 0 / 总计 7

Neighbor filter (not the unfiltered suite):

```
dotnet test tools/BattleSim.Tests/BattleSim.Tests.csproj --nologo --filter "FullyQualifiedName~G2Review|FullyQualifiedName~G2Recheck|FullyQualifiedName~G2A53|FullyQualifiedName~G2B1"
```

**G2Review + G2Recheck + G2A53 + G2B1:** 失败 0 / 通过 97 / 跳过 1 / 总计 98  
Skip is `G2RecheckNaturalContractTests.N01_N02_N03_NaturalPlayContract_DocumentedNotFaked` (Unity EventSystem; documented, not faked).

No existing assertion in that filter needed a tick-count rewrite. Unfiltered suite left for C4.

Legacy tape test is a documented FAIL (`Match=False`); it is a passing xUnit fact that asserts the old basic tape still does not Verify.

---

## Existing-test expectation shifts (propose only; coordinator decides)

Any scripted fight that `Submit`s an ally Slide now freezes `TimeLeft`/charge for 44 ticks per Slide (plus 60 ticks on wave advance, 21 on Auto Drive). **Original vs Replay still Match** because both go through the core. Tests that hard-code an absolute `TickIndex` / `TimeLeft` / first-auto tick after a Slide (without going through Capture+Verify) would need their numbers updated; this lease did not edit those files.

`ArmFever` still calls `ResolveDrive()` directly and does **not** start a Drive hold (preserves R01 Fever fixtures). Player QTE `DriveResolve` does not hold (matches HUD `_judgeFromQte`).

---

## Not done / blockers

- Unity Editor not launched (C4).
- Unfiltered suite not run (C4).
- No git commit/push.
- ClockKey one-liner left for coordinator / C3.
