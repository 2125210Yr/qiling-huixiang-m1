# Fixed encounter controller (O3)

`BossEncounterController` is pure C# scheduling state. It receives the accepted world tick from `BattleSim`; it never computes or applies damage, spawns units, advances legacy clocks, or consumes battle RNG.

## API

```csharp
BossEncounterController(BossEncounterDef frozenDefinition);   // N7
BossEncounterController(EliteEncounterDef frozenDefinition);  // N4
bool OwnsEnemySlot(int slot);
void Advance(float worldDt, Func<EncounterView> readCurrent, Action<EncounterRequest> apply);
void ObserveStableBoundary(Func<EncounterView> readCurrent, Action<EncounterRequest> apply);
void Cancel();
EncounterSnapshot Snapshot(EncounterView view);
```

Both definitions are defensively cloned. N7 owns enemy slots 0, 1, 2; N4 owns only slot 0. The caller suppresses the entire old `TickUnit` for those slots. N4's ordinary adds retain their existing ownership.

`EncounterView` has `IReadOnlyList<UnitState> Enemies` and `BattleOutcome Outcome`. These are current references; the controller reads them and never mutates a unit. `readCurrent` must return the state after synchronous request application, including any new mask instances or terminal result.

`EncounterRequestKind` has exactly `AutoAttack`, `AreaAttack`, `RebuildMissingMasks`. Request fields are `Kind`, `ActionId` (`long`, monotonically increasing within the encounter), `SkillId`, `CasterSlot`, `DamageMultiplier`, `MaskCharacterId`, `MaskSlots` (`int[]`), `ExpectedGenerations` (`int[]`). The action ID is a scheduler request identity, not a replacement for the native root action ID assigned by BattleSim.

The caller applies auto/area requests through the normal native skill path. A rebuild contains at most fixed slots 1 and 2; a missing instance expects generation 0. A live instance is never requested, healed, or replaced. The core validates the expected generation and creates the new instance; the controller counts only confirmed new living generations after callback return. An empty rebuild request records the single phase transition when both masks are still alive.

## Clock and boundary rules

- `Advance` is called once per accepted simulation tick after status damage and stable outcome checks. Pause and policy holds do not call it. `worldDt` is the same game time delta used by the core, never a HUD timer or `TickIndex` delta. Negative or nonfinite deltas are rejected.
- N7: auto every 3 seconds; first intent at 9; cast for 4; after completed area, next auto in 3 and next intent in 10 seconds in phase 1 or 8 in phase 2. N4: auto 3, first intent 6, cast 3, subsequent interval 12. All values come from the frozen definition.
- An intent wins a simultaneous auto deadline. The tick that starts a cast does not also subtract time from the new cast. No auto is issued while casting, including an overdue cast waiting for control to end.
- Action locks defer auto and area requests while elapsed time and visible cast time continue. Silence defers area only; it allows autos when there is no active cast, following `UnitState.SkillLocked`. An overdue blocked cast remains `IsCasting=true` with zero remaining time. Unlocking emits one due action, without a burst of missed autos.
- The area multiplier is sampled immediately before area application: `1 + DamagePerLivingMask * livingMaskCount`. Snapshot reads recompute the same current multiplier during the warning.
- At a stable native-plus-relic boundary, the first observation of living boss HP at or below the threshold latches phase pending. Healing above the threshold does not undo that observation. During a cast, retain its remaining time and defer transition until it completes. Outside a cast, transition immediately once. The first intent remains at its authored time; after a previous completed area, a phase transition shortens the next interval relative to that completion, without placing its deadline before current time.
- Synchronous core callbacks may call `ObserveStableBoundary`: while a request is dispatching, these calls may cancel or latch pending phase, but never issue nested requests. After callback return, process the pending transition. Recursive `Advance` is an error.
- Any terminal outcome or controlling unit death cancels pending work. Failed takes precedence over counting an area as completed. A completed area that causes ordinary victory/defeat still counts. Cancel preserves elapsed time and historical counters. Exceptions from request application cancel and propagate to the core.

## Read-only public state

The controller exposes `Stage`, `Phase`, `PhasePending`, `IsCasting`, `RemainingCastSec`, `NextIntentSec`, `AutoRemainingSec`, `ElapsedSec` (`double`), `ActionSerial`, `IntentSerial`, `AreaCasts`, `MaskRebuildCount`, and `Cancelled` via getters. Suppressed/cancelled pending clocks report zero; historical values remain.

`EncounterSnapshot` is a detached copy of that state with `IsBoss`, `AreaName`, `CastDurationSec`, `AliveMasks`, and `AreaMultiplier`. HUD reads this copy to display the simulation-owned warning; the snapshot has no authority to issue an attack. Historical scheduler counters are not damage totals.

## Evidence scope

- `o3-combined-red.trx`: all 16 initial pure controller cases failed from a compiling no-op API stub. The parent owns the combined report and other test classes.
- `controller-integration-first.trx`: parent reported 33/33 passing, including the initial 16 pure cases. This covers the first implementation, before the later pure boundaries and Failed-without-throw regression were added.
- `controller-boundary-red.trx`: 25 passed and the new Failed-without-throw case failed with expected area completion 0, actual 1. The production fix reads the returned outcome before incrementing the completion counter.
- `core-o3-isolated-suite.trx`: parent reported 480 passed and 1 pre-existing skip, including all 26 controller cases after that fix.
- `OriginalBossControllerTests` uses explicit artificial HP/status mutations to test scheduler boundaries. Its request recorder performs only fixture mask replacement and does not establish actual game damage or ordinary UI play.
- Further raw reports and their exact final counts are recorded after the parent runs the combined checks. This module is not a claim that the whole O3 batch or deliverable has completed.
