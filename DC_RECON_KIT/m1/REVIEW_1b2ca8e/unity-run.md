# B1-A4 — natural-play runbook (1b2ca8e)

**task_id:** `B1-A4` (closed A53-UNITY / A53-05 / T08 is not this batch)  
**baseline / HEAD:** `1b2ca8e4ff56d9f0986e158fc97b50498f735cb3`  
**executed:** 2026-09-14 22:39–23:55 +08  
**pointer:** `UnityEngine.EventSystem` only. `os_touch=NOT_CLAIMED`.  
**historical run7:** `DC_RECON_KIT/m1/G2_REVIEW_20260913/artifacts/natural-play/run7` stays **FAIL BattlePlay missing=Tap,Slide**. Not rewritten.  
**this turn:** Editor **launched**. Results: `artifacts/natural-play/SESSION_RESULTS.md`.

---

## 1. Execution table (this turn)

| Item | Fact |
|---|---|
| Unity.exe | `D:\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe` `6000.3.23f1` |
| `StripDotFlame` in `VerificationCatalog.cs` | **yes** (lines 10, 42, 46, 64, 71) before relaunch |
| `np.basic.v1` | **PASS** Tap+Slide `source=Player` slot 0; NEXT + HOME; fights `np-20260914T152600-001/002` |
| `np.fever.v1` | **FAIL FeverPlay** (3× QTE Good, no Fever, Defeat). Real fail, not NOT_RUN |
| `np.auto.v1` | **PASS** Auto=Full via HUD then Manual; Auto Submit observed |
| Fight-1 `BattleReplayer.Verify` | **ran** on new sim; **Match=False** (Digest/Event/Version). `fight1-readback.txt` |
| Recording | `recordings/np-continuous-20260914T143817.mp4` (656670768 bytes) |
| Win64 | `WindowsBuild.BuildAndExit` wrote exe 23:53 + managed dlls 23:47. Hashes in `artifacts/BUILD_HASHES.txt` |
| First Editor open | **BLOCKED** then recovered: CS0104 Safe Mode (`logs/editor-np-basic-v1-20260914T143934.log`) |

`dist/windows` not hashed. run7 not rewritten.

---

## 3. After A2 freeze — required matrix

One Editor at a time. **No** `-batchmode`, **no** `-nographics`, **no** `-quit` (smoke exits itself). After FIGHT: no HP/Drive/Charge/Fever/time/outcome writes. No `FirePerfect`. No forced Perfect. `VerificationCatalog.Apply` (with A2 substitute) runs **before** start.

| Order | Token | Must observe |
|---|---|---|
| 1 | `np.basic.v1` | Player `Tap` + Player `Slide` (EventSystem) before HUD Drive remap |
| 2 | `np.fever.v1` | QTE (`vfxGood`) + Fever + **two distinct slots** accepted `FeverTap` **`source=Player`**. Auto FeverTap must not count. Runtime now calls `DistinctAcceptedFeverSlots(..., Player)`. Still confirm `commands.txt`. |
| 3 | `np.auto.v1` | HUD Full Auto → auto evidence → HUD Manual |
| 4 | `np.matrix.v1` | fight1 **NEXT** fight2 **NEXT** fight3 then PAUSE → HOME; per-fight folders |

Aliases: `basic`/`n01`, `fever`/`n02`, `auto`/`n03`, `matrix`/`all`/`1`. Prefer `np.*.v1`.

### 3.1 File readback of fight-1 (new sim)

After matrix persist, do **not** compare the live `GameRoot.Battle` object to fight-1.

```csharp
var dir = @"F:\天命之子\client\captures\natural-play\battles\<fight1-id>";
var ev = NaturalPlayBattleEvidence.Load(dir);          // header + replay.jsonl
BattleRunRecord tape;
NaturalPlayBattleEvidence.TryReadRecord(dir, out tape);
NaturalPlayBattleEvidence.BindOpeningPolicy(ev);       // same DESIGN_PLACEHOLDER policy
var factory = NaturalPlayBattleEvidence.ReplayFactory(tape);
var report = BattleReplayer.Verify(tape, factory);     // NEW BattleSim inside factory
// require report.CommandDiff / Unconsumed empty for fight-1 tape
```

Fight-1 `header.txt` / `commands.txt` / `events.txt` / `replay.jsonl` must be unchanged after fight-2 persist. Index: `captures/natural-play/BATTLE_INDEX.txt` (not a single overwritten log).

### 3.2 Matching-source Win64 + hashes

Only after A2 is in `VerificationCatalog.cs` and Play Mode source is frozen:

```powershell
$Unity = "D:\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe"
$Proj  = "F:\天命之子\client"
$Kit   = "F:\天命之子\DC_RECON_KIT\m1\REVIEW_1b2ca8e"
& $Unity -batchMode -nographics -projectPath $Proj `
  -executeMethod Resonance.EditorTools.WindowsBuild.BuildAndExit `
  -logFile "$Kit\logs\win64-build.log"
```

(`WindowsBuild` may use `-batchMode`; **natural-play smoke must not**.)

Then hash **this** exe + managed assemblies + `git rev-parse HEAD` into `$Kit\artifacts\BUILD_HASHES.txt`. Refuse to hash the 2026-08-28 leftover or `dist/windows`.

### 3.3 One continuous recording

Start ffmpeg **before** Play Mode; one file per session (or one file covering all four if never stopped):

```powershell
$mp4 = "$Kit\recordings\np-matrix-<utc>.mp4"
ffmpeg -y -f gdigrab -framerate 30 -draw_mouse 1 -i desktop -c:v libx264 -pix_fmt yuv420p -preset veryfast $mp4
```

`run-natural-play.ps1 -Record -Run` does this. Helper **refuses `-Run`** until the A2 bind string exists in `VerificationCatalog.cs`.

---

## 4. Exact Editor / CLI commands (post-A2)

```powershell
$Unity = "D:\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe"
$Proj  = "F:\天命之子\client"
$Kit   = "F:\天命之子\DC_RECON_KIT\m1\REVIEW_1b2ca8e"
```

No `-batchmode` / `-nographics` / `-quit`:

```powershell
& $Unity -projectPath $Proj -natural-play -natural-scenario np.basic.v1 `
  -executeMethod Resonance.EditorTools.NaturalPlaySmoke.CliBasic `
  -logFile "$Kit\logs\editor-np-basic.log"

& $Unity -projectPath $Proj -natural-play -natural-scenario np.fever.v1 `
  -executeMethod Resonance.EditorTools.NaturalPlaySmoke.CliFever `
  -logFile "$Kit\logs\editor-np-fever.log"

& $Unity -projectPath $Proj -natural-play -natural-scenario np.auto.v1 `
  -executeMethod Resonance.EditorTools.NaturalPlaySmoke.CliAuto `
  -logFile "$Kit\logs\editor-np-auto.log"

& $Unity -projectPath $Proj -natural-play -natural-scenario np.matrix.v1 `
  -executeMethod Resonance.EditorTools.NaturalPlaySmoke.CliMatrix `
  -logFile "$Kit\logs\editor-np-matrix.log"
```

Helper (print-only until A2; then `-Run`):

```powershell
powershell -NoProfile -File "$Kit\run-natural-play.ps1" -Scenario np.basic.v1
powershell -NoProfile -File "$Kit\run-natural-play.ps1" -Scenario np.matrix.v1 -Record -Run
```

Menus (already-open Editor after freeze): `Resonance/Smoke/Natural Play {Basic,Fever,Auto,Matrix}`.

Request file (write **after** Editor is up, or use `-executeMethod`): `client/Temp/natural-play.request` — one token, UTF-8, first word only.

Do **not** use Vertical Slice Smoke, `Play For Look`, or `CaptureRuntime -capture` (`FireDrivePerfect`).

---

## 5. Output directories

| Artifact | Path |
|---|---|
| Session verdict | `client/captures/natural-play.result.txt` |
| Per-battle | `client/captures/natural-play/battles/<battle_id>/{header,commands,events,result,digest,scenario,replay.jsonl}` |
| Index | `client/captures/natural-play/BATTLE_INDEX.txt` |
| Shots | `client/captures/np_*.png` |
| Editor log | `REVIEW_1b2ca8e/logs/editor-np-*.log` |
| Recording | `REVIEW_1b2ca8e/recordings/np-*.mp4` |
| Archive | `REVIEW_1b2ca8e/artifacts/natural-play/<utc>/` |

Leftover `client/captures/natural-play.result.txt` (2026-09-14 11:44, historical FAIL text) is **not** B1 evidence.

---

## 6. Post-run checks (when launched)

1. First line of `natural-play.result.txt` is `PASS` or `FAIL <phase>` — keep the real line.  
2. `pointer=UnityEngine.EventSystem`; run7 historical FAIL still printed.  
3. Fever: ≥2 distinct `slot=` on `kind=FeverTap source=Player accepted=True`.  
4. Fight-1 folder unchanged after NEXT; `BattleReplayer.Verify` on a **new** sim from `replay.jsonl`.  
5. Build hashes match the same HEAD as the Editor logs.

---

## 7. This turn’s writes / non-writes

- Wrote: this file; `run-natural-play.ps1`; `artifacts/unity-probe.md`.  
- Did **not** edit BattleSim, Catalog, GameRoot, tests, run7, `dist/`.  
- Did **not** launch Unity Editor or Play Mode.
