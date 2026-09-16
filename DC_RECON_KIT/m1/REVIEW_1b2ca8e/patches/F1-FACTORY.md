# F1-FACTORY — recover opening growth on disk readback

**task_id:** `B1-F1`  
**batch:** `B1`  
**path:** G2 Path A  
**Lease writes:** `NaturalPlayBattleEvidence.cs`, `readback/Program.cs`, `G2B1FactoryReadbackTests.cs`, `BattleReplay.cs` (Capture/parse snapshot + `OpeningGrowth` search only), this file.  
**Do not implement here:** `BattleSim.cs`, `GameRoot.cs`, `BattleHud.cs`, `NaturalPlayRuntime.cs`, live Unity scenes/prefabs, G2A53* assertion bodies, `a53-gate.trx`, run7, `dist/`.  
**Do not** implement Dot / Reflect. Do not inject HP / Drive / Charge / Fever / time / outcome. Do not revert A3 `ActiveStage` / `ResolveSelectedStage`. No commit / push. No G3. No T27 / 90% / M1 claims.

Evidence: ENGINEERING. Not GL. Not `PATH_A_ENGINEERING_PASS`.

---

## 0. Defect (source)

Fight-1 tape `np-20260914T152600-001` frozen header / `scenario.txt`:

`growth=C001:2460/1550+0|C007:3760/760+0|C010:2640/990+0|C003:3160/1050+0|C005:2700/1040+0`

Unity `GameRoot.StartBattleAt` opens with `_save.ProgressForParty()` (starter gear on a grown save). `NaturalPlayBattleEvidence.NewSim` / `ReplayFactory` passed `growth=null` into `new BattleSim(...)`, so `Growth.Apply` used Catalog level-1 stats (`C001` MaxHp 2200). Command tape was consumed (`CommandDiff=0` `Unconsumed=0`); Verify failed on opening identity (`Ally[0].MaxHp 2460 != 2200`, Wave 0, Drive 74, Events 152).

`NamedOpeningDiff` already reports `GrowthIdentity` / `DataIdentity`. Readback never applied recovered growth.

---

## 1. What landed (this lease)

When `growth == null`, factory recovery is **on by default**:

1. Explicit caller `growth` argument (including empty array = Catalog defaults).
2. `BattleRunRecord.OpeningProgress` written by `Capture` / parsed from `openingProgress` on new jsonl headers.
3. Deterministic `OpeningGrowth` search of `Initial.GrowthIdentity` (`id:hp/atk+extra`) via `Growth.Apply` / `BreakDown` over level / uncap / ignition / affection / gear (plus=0 equipment + unique-flat cartas). Existing fight-1 jsonl does not need re-record.

`FindStage(rec.StageId)` stays after `VerificationCatalog.Apply` (NP-BASIC instance). Same `BattleReplayer`. No second replayer.

`recoverOpeningGrowth: false` (or a non-null empty `UnitProgress[]`) is the dedicated off switch so Catalog defaults cannot silently Match a grown tape.

`readback/Program.cs` recovers, prints `NamedOpeningDiff` before Verify, then `Verify(tape, ReplayFactory(tape, recovered))`. Output overwrites `REVIEW_1b2ca8e/artifacts/natural-play/fight1-readback.txt` (this batch; not historical run7).

No `BattleSim` opening-growth getter is required. Coordinator hunk: **none**.

---

## 2. Files

| File | Change |
|---|---|
| `client/Assets/Scripts/Resonance.App/Debug/NaturalPlayBattleEvidence.cs` | `NewSim` / `ReplayFactory` resolve opening growth; `RecoverOpeningGrowth` / `ResolveOpeningGrowth` |
| `client/Assets/Scripts/Resonance.Battle/Core/BattleReplay.cs` | `OpeningProgress` Capture/parse; `OpeningGrowth` search. A3 stage resolve untouched |
| `DC_RECON_KIT/m1/REVIEW_1b2ca8e/readback/Program.cs` | recover + NamedOpeningDiff + Verify |
| `tools/BattleSim.Tests/G2B1FactoryReadbackTests.cs` | collection `G2RecheckCatalog` |
| this file | lease note |

---

## 3. Tests (isolated)

```
dotnet test tools/BattleSim.Tests/BattleSim.Tests.csproj --filter G2B1FactoryReadback
dotnet test tools/BattleSim.Tests/BattleSim.Tests.csproj --filter "FullyQualifiedName~G2A53PlayableGateTests.T01|FullyQualifiedName~G2A53PlayableGateTests.T02"
dotnet run --project DC_RECON_KIT/m1/REVIEW_1b2ca8e/readback/Readback.csproj -- DC_RECON_KIT/m1/REVIEW_1b2ca8e/artifacts/natural-play/20260914T152459-np_basic_v1/natural-play/battles/np-20260914T152600-001
```

Serial collection `G2RecheckCatalog`. Do not weaken A53 T01/T02. Do not call `EnsurePartyPlayable` on production ctor. Do not auto-bind strip on `BuildBuiltin`.

---

## 4. Isolated results (this lease)

| Filter | Result |
|---|---|
| `G2B1FactoryReadback` | 4 passed / 0 failed |
| `G2A53PlayableGateTests.T01` + `T02` | 2 passed / 0 failed |

Fight-1 readback after recovery:

- `NamedOpeningDiff=null` (Growth / Data / Clock / Rules / ForceNoCrit closed).
- `VersionDiff=0` (was `DataIdentity: tape != replayed`).
- `Ally[0].MaxHp` no longer differs (2460 restored).
- **Match=False** remains. Next residual is **mid-fight**, not clocks / mods / stage at opening: `TickIndex 191 != 163`, `Drive 58 != 68`, `TimeLeft 111.933 != 109.933`, `Events.Count 100 != 124`, first event tick skew at `[12]`. Command tape still `CommandDiff=0` `Unconsumed=0`. Identity search only pins `id:hp/atk+extra`; Agl/Def are unconstrained on historical tapes, so auto-tick can still skew. Do not treat remaining Match as factory MaxHp. No `PATH_A_ENGINEERING_PASS`.

Coordinator (after F1): Unity CS0246 `FiveStat` — nested types were declared after field use. Hoisted `FiveStat` / `GearCombo` to the top of `OpeningGrowth` so the Editor matches Roslyn. F2 `np.fever.v1` 20260914T171516 was BLOCKED on this error.
