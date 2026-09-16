# C-MERGE — coordinator hunks after C1/C2/C3 leases closed

baseline: `a82801b`
applied by: coordinator (single integrator), after `689c42ef` (C2) and `a31c3745` (C3) returned.

## 1. `BattleReplay.cs` — `BattleContentIdentity.ClockKey`

Appended `SlideShowtimeHoldSec | DriveCastHoldSec | WaveAdvanceHoldSec` (F3) to the clock
identity string. Reason: C2 made the three showtime/PHASE freezes core clock policy. A tape
replayed under a different hold budget must fail `ClockIdentity`, not silently diverge in
`TickIndex`/`TimeLeft`. Old tapes with a pre-C2 `clockIdentity` now name this diff; they were
already `Match=False` (unrecorded HUD hold) and are not edited.

## 2. Pre-existing tests updated for the C2 contract (test-only, no source change)

Full unfiltered run after merge: **3 failed / 268 passed / 1 skip**. All three encoded the
pre-C2 assumption "no core hold after an ally Slide / Full-auto Drive / wave advance". Updated
to drain `PolicyHoldTicksLeft` (ticks advance, clocks do not) instead of assuming zero hold.
No assertion on damage, Fever, outcome, or tick-count golden strings was weakened.

| Test | Old assumption | Update |
|---|---|---|
| `BattleSimTests.AutoTapReachesFever` | Full-auto Drive fires every tick | drain policy hold before each Drive round |
| `M1ClockModeTests.StayHeldSurvivesCatchUpTicksOnFull` | tick after `HoldSim=false` leaves `HoldSim==false` | assert `!HoldSticky` and `!HoldSim || PolicyHoldTicksLeft>0`; `TimeLeft` decreased unchanged |
| `M1ClockModeTests.DeathWaveAndResultEventsAreLogged` | result within 8 ticks of wave-1 kill | assert `hold begin.wave` + `PolicyHoldTicksLeft>0`, drain, assert `end.wave`, then original 8-tick result check |

Rerun: **271 passed / 0 failed / 1 skip (N01_N02_N03 Unity natural-play, documented)** —
`tools/BattleSim.Tests/TestResults/C-merge-full.trx`.

## Not done here

- No Unity run, no build, no recording, no readback re-record (C4 on the frozen tree).
- Hold seconds remain DESIGN_PLACEHOLDER (1.47 / 0.70 / 2.00), not GL.
- Legacy 1b2ca8e basic tape stays `Match=False`, `openingSource=legacy-recovered`.
