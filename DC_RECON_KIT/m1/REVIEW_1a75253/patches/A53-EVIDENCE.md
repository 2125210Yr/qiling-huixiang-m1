# A53-EVIDENCE — freeze BattleInitialHeader + standard replay.jsonl

**task_id:** `A53-EVIDENCE`  
**Baseline:** `1a752532e39560b861c2a661db66c97ca18ffece`  
**Owner writes:** `client/Assets/Scripts/Resonance.App/Debug/NaturalPlayBattleEvidence.cs` (this session), this file.  
**Do not edit from this task:** `BattleSim.cs`, `GameRoot.cs`, `BattleHud.cs`, `G2A53*`, `G2Recheck*`.  
**HUD contract (unchanged):** `BattleHud.OnPortraitTap` maps `Drive>=100` to `DriveBegin`. Fever required slots are `CommandSource.Player` (scenario N02). Do not remap Auto FeverTap into those slots.  
**Compile this session:** `dotnet` net6.0 library of `Resonance.Battle/**/*.cs` + `NaturalPlayBattleEvidence.cs` — 0 errors, 0 warnings. Unity Editor / GameRoot / Runtime / tests **NOT_RUN**.

Same store as 1a75253: `captures/natural-play/battles/<battle_id>/`. No second archive. Persist stays pre-NEXT / pre-HOME.

---

## 0. Why

`Begin` only froze `RunHeader()` text. Persist wrote `header/commands/events/result/digest/scenario.txt` but not the `BattleRunRecord` JSON-lines `BattleRunRecord.ParseJsonLines` / `BattleReplayer` already read. Passing fight-2 as `live` wrote fight-2 into the fight-1 folder (`var sim = live != null ? live : Sim`).

---

## 1. Landed API (`NaturalPlayBattleEvidence`)

| Call | Contract |
|---|---|
| `Begin(sim, sessionId, seq, sc)` | After Speed/Auto/Profile are set. `sim.FreezeInitialHeader` (first Freeze wins) + copy into `FrozenInitial`. Keeps `FrozenHeader = sim.RunHeader()`. Binds `_boundSim` / `_boundBattleId`. |
| `Persist(capturesRoot, live)` | Same per-battle folder. Writes the old txt files **and** `replay.jsonl` via `BattleRunRecord.Capture` + `ToJsonLines`. |
| Refuse | `live` is a different instance than the bound sim → `sim-mismatch`. `BattleId` mutated away from Begin, or `scenario.txt` in the folder has another `battle_id` → `battle-id-mismatch`. **Does not write.** `live == null` flushes the bound sim. |
| `Load(dir)` / `TryLoad` | Rebuilds evidence + record from the folder. |
| `TryReadRecord(dir, out rec)` | `ParseJsonLines` on `replay.jsonl` (else `record.jsonl`). |
| `NewSim(rec, growth?)` / `ReplayFactory(rec, growth?)` | **New** `BattleSim` from opening party/stage/profile/leader/seed. Not same-object. |
| `BindOpeningPolicy(ev)` | Rebinds `DesignPlaceholderPolicy` from recorded scenario name (empty name clears). |
| `NamedOpeningDiff(tape, rebuilt)` | `DataIdentity` / `GrowthIdentity` / `ClockIdentity` / `RulesVersion` / `ForceNoCrit`. Null = enough to Verify. |
| `DistinctAcceptedFeverSlots` | Counts **Player** FeverTap only. |

Opening growth/gear/policy/data identity live on `BattleInitialHeader` inside `replay.jsonl` (`growthIdentity`, `clockIdentity`, `dataIdentity`, `forceNoCrit`) and `scenario.txt`. A factory that rebuilds party+stage+profile+growth either Verify-matches or fails with those named Diffs (`BattleReplayer.Verify` → `VersionDiff` / `DigestDiff`).

---

## 2. Files written on disk (one fight folder)

`{capturesRoot}/natural-play/battles/<battle_id>/`

| File | Role |
|---|---|
| `header.txt` | Frozen opening `RunHeader()` text (not end-state Speed/Auto). |
| `commands.txt` | CommandLog text. |
| `events.txt` | `Events.ExportCanonical()`. |
| `result.txt` | Outcome + frozen header + initial header + commands + events. |
| `digest.txt` | Hash of frozen header + commands + events + outcome. |
| `scenario.txt` | `battle_id`, scenario, seed, stage, policy, growth/clock/data identity. |
| **`replay.jsonl`** | Standard `BattleRunRecord` tape (header/cmd/digest/unit/status/event lines). |

Session index (unchanged location): `natural-play/BATTLE_INDEX.txt` and `natural-play.events.txt` (index, not fight-2 overwrite). Index rows now also cite `replay=`.

---

## 3. `NaturalPlayRuntime` hunks (integrator / this file only — do not edit Runtime from other tasks)

File: `client/Assets/Scripts/Resonance.App/Debug/NaturalPlayRuntime.cs`  
HUD mapping: **do not change**. Fever Player vs Auto is already required by `np.fever.v1`; evidence now filters Player on `DistinctAcceptedFeverSlots`.

### Hunk R1 — `Begin` after Freeze

`WaitBattleBuilt` already waits until `GameRoot.Battle` exists. `StartBattleAt` Freezes after Speed/Auto/Profile. Keep `BeginBattleCapture` **after** that, never before `StartBattleAt`.

Current `BeginBattleCapture` is already in the right place (`Run` → `WaitBattleBuilt` → `BeginBattleCapture`). Bind policy then `Begin` (Begin now Freezes InitialHeader; first-call-wins with GameRoot):

```diff
--- a/client/Assets/Scripts/Resonance.App/Debug/NaturalPlayRuntime.cs
+++ b/client/Assets/Scripts/Resonance.App/Debug/NaturalPlayRuntime.cs
@@ -1532,18 +1532,19 @@
         void BeginBattleCapture(VerificationScenario sc)
         {
             var live = Battle();
             if (live == null) return;
             if (_current != null && _current.Sim == live) return;
             if (_current != null && !_current.Persisted)
                 PersistCurrentBattle("late-begin");
             DesignPlaceholderPolicy.Bind(sc);
+            // After GameRoot StartBattleAt Freeze (Speed/Auto/Profile already set).
             _current = NaturalPlayBattleEvidence.Begin(live, _sessionId, _battles.Count + 1, sc);
             _battles.Add(_current);
             _cmdLogSeen = 0;
             _feverShot = false;
-            Record("BattleId", "unique battle_id + frozen RunHeader",
+            Record("BattleId", "unique battle_id + frozen BattleInitialHeader + RunHeader",
                 "id=" + _current.BattleId + " scenario=" + _current.ScenarioName
-                + " header=" + _current.FrozenHeader, true);
+                + " frozen=" + (_current.FrozenInitial != null && _current.FrozenInitial.FrozenAtStart)
+                + " header=" + _current.FrozenHeader, true);
         }
```

### Hunk R2 — Persist before NEXT/HOME; never pass fight-2 as `live`

Replace the fallback that forwarded `_current.Sim` after the live pointer moved (safe only when `Persist` ignored `live`). New `Persist` **refuses** a different instance. Flush the bound fight with `null` if NEXT already replaced `GameRoot.Battle`.

```diff
--- a/client/Assets/Scripts/Resonance.App/Debug/NaturalPlayRuntime.cs
+++ b/client/Assets/Scripts/Resonance.App/Debug/NaturalPlayRuntime.cs
@@ -1549,20 +1549,25 @@
         void PersistCurrentBattle(string why)
         {
             if (_current == null) return;
             var live = Battle();
             try
             {
-                BattleSim sim;
-                if (live == _current.Sim || live == null)
-                    sim = _current.Sim;
-                else if (!_current.Persisted)
-                    sim = _current.Sim;
-                else
-                    return;
-                _current.Persist(CapturesDir(), sim);
-                Note("persist " + _current.BattleId + " why=" + why
-                    + " outcome=" + _current.OutcomeAtPersist
-                    + " digest=" + _current.Digest);
+                var status = _current.Persist(CapturesDir(), live);
+                if (status == NaturalPlayBattleEvidence.RefuseSimMismatch
+                    || status == NaturalPlayBattleEvidence.RefuseBattleIdMismatch)
+                {
+                    if (!_current.Persisted)
+                        status = _current.Persist(CapturesDir(), null);
+                    else
+                    {
+                        Note("persist-refuse " + why + " " + status);
+                        return;
+                    }
+                }
+                Note("persist " + _current.BattleId + " why=" + why
+                    + " status=" + status
+                    + " outcome=" + _current.OutcomeAtPersist
+                    + " digest=" + _current.Digest
+                    + " replay=" + (_current.Dir != null
+                        ? Path.Combine(_current.Dir, NaturalPlayBattleEvidence.ReplayFileName) : ""));
             }
             catch (Exception e)
             {
                 Note("persist-fail " + why + " " + e.Message);
             }
         }
```

Existing call sites stay. Do not move them after the NEXT/HOME tap:

| Why | When |
|---|---|
| `pre-next` | After result, **before** `TapNamed(..., "RETRY", "NEXT")`. |
| `pre-home` | After pause board, **before** `TapNamed(..., "HOME")`. |
| `auto-exit` / `rematch-enter` / `quit` / `late-begin` | Keep as-is; refuse + `Persist(null)` covers a replaced `Battle`. |

Do **not** change `PlayFeverNaturally` HUD targeting. Two Fever slots remain EventSystem portrait taps; Auto FeverTap is not a required slot.

---

## 4. `GameRoot` hunk (integrator only — **no GameRoot edit from this task**)

File: `client/Assets/Scripts/Resonance.App/Core/GameRoot.cs`  
`StartBattleAt` on 1a75253 already Freezes after Speed/Auto/Profile and before the first Tick. **Keep that order. No source change required** unless someone moves those assignments.

```csharp
_battle = new BattleSim(_save.PartyIds, _save.LeaderSlot, _save.LastSeed, stage, _save.ProgressForParty(), mods)
{
    Speed = _save.Speed,
    Auto = _save.Auto,
    ForceNoCrit = false,
    Profile = FormulaProfile.JP_LEGACY_EMPIRICAL
};
// R02: freeze real opening seed/party/growth/gear/leader/clocks/profile/auto/speed/data identity.
BattleInitialHeader.Freeze(_battle, _save.PartyIds, stage != null ? stage.Id : "");
```

Rules for any later GameRoot edit:

1. Do not Freeze before Speed/Auto/Profile object-initializer assignment.
2. Do not Tick / show HUD input before Freeze.
3. Do not call `NaturalPlayBattleEvidence.Begin` from GameRoot (Runtime owns capture after `WaitBattleBuilt`).
4. NEXT/HOME replace `_battle` via `StartBattleAt` / `Show(Home)`. Runtime must Persist **before** those taps. GameRoot must not write evidence files.
5. Do not change `BattleHud` Drive≥100 → DriveBegin.

If Freeze is ever removed from `StartBattleAt`, restore it with the block above. Runtime `Begin` will Freeze as fallback (first-call-wins).

---

## 5. Read-back (new sim, not same object)

```csharp
var ev = NaturalPlayBattleEvidence.Load(fightDir);
BattleRunRecord rec;
NaturalPlayBattleEvidence.TryReadRecord(fightDir, out rec);
NaturalPlayBattleEvidence.BindOpeningPolicy(ev);
var report = BattleReplayer.Verify(rec, NaturalPlayBattleEvidence.ReplayFactory(rec, growthOrNull));
// or: var fresh = NaturalPlayBattleEvidence.NewSim(rec, growthOrNull);
//     var named = NaturalPlayBattleEvidence.NamedOpeningDiff(rec, fresh);
```

`growthOrNull`: pass `SaveBlob.ProgressForParty()` (or the same `UnitProgress[]` used at start) to rebuild gear. Omitting growth uses Catalog base stats; if the tape had gear, Verify fails `DataIdentity: tape != replayed` / `GrowthIdentity: tape != replayed`.

---

## 6. Compile / what was not run

- **Compiled:** temp `net6.0` class library (`Resonance.Battle/**/*.cs` + this evidence file). `dotnet` 6.0.410: 0 errors, 0 warnings.
- Unity Editor / play mode / natural-play basic/fever/auto: **NOT_RUN**
- GameRoot / Runtime source not edited (hunks above only)
- `BattleSim.Tests` / G2A53* (A53-QA lease): **NOT_RUN**
- No git commit, no G3
