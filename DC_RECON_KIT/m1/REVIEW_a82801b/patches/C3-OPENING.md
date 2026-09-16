# C3-OPENING — record real opening inputs (Reserve + mods)

**task_id:** `C-C3`  
**batch:** `C`  
**baseline:** `a82801b`  
**Lease writes:** `BattleReplay.cs`, `NaturalPlayBattleEvidence.cs`, `REVIEW_a82801b/readback/**`, `tools/BattleSim.Tests/G2C3OpeningInputTests.cs`, this file.  
**Did not write:** `BattleSim.cs` / `BattleSim.Commands.cs` / `BattleHud.cs` / `GameRoot.cs` / `VerificationCatalog.cs` / `NaturalPlayRuntime.cs` / `Growth.cs` / existing tests or fixtures.  
**Did not** enlarge the gear search space. Did not inject HP/Drive/Charge/Fever/time/outcome. No commit / push.

Evidence: ENGINEERING. Not GL.

---

## 0. Defect

`BattleRunRecord.Capture` wrote `OpeningProgress` via `OpeningGrowth.FromSim` (reverse-search of final Hp/Atk/Extra/Def/Agl/Crt). Search cannot see `UnitProgress.Reserve`. `Format`/`Parse` omitted `Reserve`. Two parties with identical stats and `SSSSS` vs `TTTTT` were indistinguishable; `BattleSim.NextAutoType` then regenerated the wrong auto Tap/Slide sequence.

`FromSim` / `FromIdentity` falling back to `Level=1` was treated as an exact opening.

Integrator hunk already on `BattleSim`: ctor copies growth/mods; `OpeningGrowthInput` / `OpeningMods` are the real inputs. This lease consumes those; it does not redo the ctor.

---

## 1. What landed

### Capture (normal path)

`rec.OpeningProgress = OpeningGrowth.Copy(sim.OpeningGrowthInput)` — **no `FromSim` search**.

| ctor growth | Capture |
|---|---|
| non-null | copied rows, `openingSource=input` |
| null | empty/null progress, `openingSource=input` (catalog defaults) |

Also persists ctor `BattleMods` (`FoodAtkMul`, `CartaMul`). `FromSim` remains as a labelled legacy helper only.

### Format / Parse

Round-trips every `UnitProgress` field already written, plus:

- `;r=SSSSS` always written (missing `r` on old strings → default `EEEEE`)
- `;sk=<SkinId>` when non-empty

### Header identity

`BattleInitialHeader.OpeningInputsIdentity` is computed at `Freeze`/`Snapshot` from `sim.OpeningGrowthInput` + `OpeningMods`:

`{Format(rows)}#m={FormatMods(mods)}`  
empty rows → `m=food=1.000;carta=1.000`

`Copy` copies the field. JSON writes/reads it. **`GrowthIdentity` (`id:hp/atk+extra`) is unchanged** for old tapes.

### Factory

`OpeningGrowth.Recover(rec)`:

1. Prefer `rec.OpeningProgress` → `openingSource=input` (if unset).
2. Else `GrowthIdentity` search (`FromIdentity` / `Search`, **same gear space**) → `legacy-recovered`, or `incomplete` if any row fails to match (Level=1 fallback is not an exact claim).
3. Else `openingSource=input` + no identity → catalog defaults (`null`). Else `incomplete`.

`NaturalPlayBattleEvidence.NewSim` / `ReplayFactory` pass **recorded** `BattleMods` into `new BattleSim(..., growth, mods)`. They do **not** recompute `Food.AtkMulOf(save.Meal)` / `PvpRules.CartaMulIfPvp`.

`NamedOpeningDiff` compares `OpeningInputsIdentity` only when the tape has one (after `GrowthIdentity` / `DataIdentity`, so G2B1 catalog-default still names Growth/Data).

### Freeze semantics

Ctor already deep-copies. Mutating the caller's `UnitProgress[]` / `BattleMods` after construction does not change `OpeningGrowthInput`, Capture, or the frozen `OpeningInputsIdentity` (C3-T2).

---

## 2. JSON keys added (header line)

| key | meaning |
|---|---|
| `openingProgress` | existing; now real rows including `;r=` / `;sk=` |
| `openingMods` | `food=1.000;carta=1.000` |
| `openingSource` | `input` \| `legacy-recovered` \| `incomplete` |
| `openingInputsIdentity` | real-row + mods identity |
| `growthIdentity` | **unchanged** old `id:hp/atk+extra` |

Old fight-1 jsonl has none of the new keys except the pre-existing `growthIdentity`. Not modified.

Scenario/result text from `NaturalPlayBattleEvidence` also writes `openingInputs` / `openingSource` / `openingMods` for C4 folders. `RunHeader()` string is unchanged.

---

## 3. Files

| File | Change |
|---|---|
| `client/Assets/Scripts/Resonance.Battle/Core/BattleReplay.cs` | Capture from `OpeningGrowthInput`; Format/Parse Reserve+SkinId; header `OpeningInputsIdentity`; Recover labels; mods helpers |
| `client/Assets/Scripts/Resonance.App/Debug/NaturalPlayBattleEvidence.cs` | factory passes recorded mods; NamedOpeningDiff; scenario keys |
| `DC_RECON_KIT/m1/REVIEW_a82801b/readback/Program.cs` | copied from `REVIEW_1b2ca8e/readback/`; prints `openingSource`, `openingMods`, NamedOpeningDiff, Verify |
| `DC_RECON_KIT/m1/REVIEW_a82801b/readback/Readback.csproj` | copy (same relative client includes) |
| `tools/BattleSim.Tests/G2C3OpeningInputTests.cs` | C3-T1 / C3-T2 |
| `DC_RECON_KIT/m1/REVIEW_a82801b/artifacts/readback/fight1-legacy-readback.txt` | old basic fight-1 readback |
| this file | lease note |

---

## 4. Tests

```
dotnet test tools/BattleSim.Tests/BattleSim.Tests.csproj --nologo --filter "FullyQualifiedName~G2C3"
dotnet test tools/BattleSim.Tests/BattleSim.Tests.csproj --nologo --filter "FullyQualifiedName~G2B1Factory|FullyQualifiedName~G2A53|FullyQualifiedName~G2ReviewReplay|FullyQualifiedName~G2RecheckReplay"
```

| Filter | Result |
|---|---|
| `G2C3` | **2 passed / 0 failed** — `C3_T1_ReserveRoundTrip_DistinctAutoSequences_FactoryMatch`, `C3_T2_FrozenInputs_IgnoreCallerMutation_LegacyIncompleteLabels` |
| `G2B1Factory\|G2A53\|G2ReviewReplay\|G2RecheckReplay` | **39 passed / 0 failed** |

A53 T01/T02 included and green. G2B1 factory / Fight1 identity search green. No assertion weakened. **No DataIdentity / header-string test needed a change**: `GrowthIdentity` and `BattleContentIdentity.Compute` are the same format; `OpeningInputsIdentity` is additive.

Unfiltered suite not run (C4).

### Red-on-old (Capture only)

Locally reverted **only** the Capture assignment to `OpeningGrowth.FromSim(sim, rec.PartyIds)`, then:

```
dotnet test ... --filter "FullyQualifiedName~C3_T1"
```

**FAIL** `C3_T1_ReserveRoundTrip_DistinctAutoSequences_FactoryMatch`:

```
Assert.Equal() Failure: Strings differ
Expected: "SSSSS"
Actual:   "EEEEE"
at G2C3OpeningInputTests.cs:line 49   // recS.OpeningProgress[0].Reserve
```

Restored `Copy(sim.OpeningGrowthInput)`. `G2C3` 2/2 green again.

---

## 5. Legacy fight-1 readback

```
dotnet run --project DC_RECON_KIT/m1/REVIEW_a82801b/readback/Readback.csproj -- DC_RECON_KIT/m1/REVIEW_1b2ca8e/artifacts/natural-play/20260914T152459-np_basic_v1/natural-play/battles/np-20260914T152600-001
```

Output: `REVIEW_a82801b/artifacts/readback/fight1-legacy-readback.txt`  
Old tape **not** modified.

First lines that matter:

```
openingSource=legacy-recovered
openingMods=food=1.000;carta=1.000
NamedOpeningDiff=DataIdentity: tape != replayed
Match=False
```

`Match=False` is required (legacy tape has no HUD hold; C2 owns hold). Recovered rows are the same starter search as F1 (`C001:lv=1;g0=EQ_WPN;g3=SC006` + `;r=EEEEE`). `openingSource=legacy-recovered` is the C3 label.

`NamedOpeningDiff` / `VersionDiff=1` / `CommandDiff=1` / first event `hold|begin.slide` vs F1's `NamedOpeningDiff=null` / `CommandDiff=0` / `dmg.auto` are **not** from this lease's identity format. Current tree already has C2 hold events in-core; `BattleContentIdentity.Compute` can disagree with the frozen a82801b tape. C3 did not change `Compute` / `GrowthIdentity`. Do not treat this as a factory MaxHp regression (search still returns the F1 starter set).

---

## 6. Coordinator apply

**GameRoot: no hunk required.** `StartBattleAt` already builds

```
new BattleMods { FoodAtkMul = Food.AtkMulOf(_save.Meal), CartaMul = PvpRules.CartaMulIfPvp(_save.PvpDoor) }
```

and passes `_save.ProgressForParty()` + mods into `new BattleSim(...)`. The integrator copies those; C3 Capture/factory read `OpeningGrowthInput` / `OpeningMods`. Do **not** recompute meal/PVP on readback.

Optional later (not this lease): if Unity wants `scenario.txt` keys on disk, Persist already writes `openingInputs` / `openingSource` / `openingMods` after Capture.

C4: use `REVIEW_a82801b/readback` (prints `openingSource`). Re-record after C2 hold lands; keep this old tape FAIL + `legacy-recovered`.

---

## 7. Blockers

None for C3. Parallel C2 compile errors in `G2C2FocusCommandTests.cs` (`FocusRejectFactory` missing) appeared once; waited 60s and retried; did not edit C2 files.
