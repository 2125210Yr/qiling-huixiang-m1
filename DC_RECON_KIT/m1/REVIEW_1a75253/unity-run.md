# A53-UNITY — natural-play Editor/CLI runbook (1a75253)

**task_id:** `A53-UNITY` / `A53-05` / `T08`  
**baseline:** `1a752532e39560b861c2a661db66c97ca18ffece`  
**probe date:** 2026-09-14  
**pointer path:** `UnityEngine.EventSystem` only. `os_touch=NOT_CLAIMED`.  
**historical run7:** `DC_RECON_KIT/m1/G2_REVIEW_20260913/artifacts/natural-play/run7` stays **FAIL BattlePlay missing=Tap,Slide**. Do not rewrite it.  
**this batch run:** **NOT_RUN** (see §9). Do not treat leftover `client/captures/` or `dist/windows` as A53 evidence.

---

## 1. Probe facts (this session)

| Item | Value |
|---|---|
| Unity Editor binary | **FOUND** `D:\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe` |
| ProductVersion | `6000.3.23f1_09d2ecc7fb28` (matches `client/ProjectSettings/ProjectVersion.txt`) |
| Editor occupying `client/` | **no** (`Library/EditorInstance.json` absent; no Unity/Resonance process) |
| `client/Temp` | absent at probe (smoke creates it) |
| Matching-source Win64 player | **not found** under `client/Builds/Win64/` |
| ffmpeg | FOUND (`Gyan.FFmpeg` 8.1.1 on PATH) |
| Play Mode / matrix | **NOT_RUN** |
| Player / new build | **NOT_RUN** |
| Continuous recording | **NOT_RUN** |

Unity was **not** launched against `client/`. Other A53 writers already have dirty `CatalogJson.cs` and `BattleReplay.cs`. Opening the project would compile those files and write `client/Library`. Coordinator re-runs after source freeze.

---

## 2. Required matrix (four sessions)

Run **after source freeze**, one Editor process at a time. No `-batchmode`, no `-nographics`, no `-quit` (smoke exits itself). No post-start HP/Drive/Charge/Fever/time/outcome writes. No `FirePerfect`. No forced Perfect.

| Order | Token | Regression | Must observe | Session shape |
|---|---|---|---|---|
| 1 | `np.basic.v1` | N01 | Player `Tap` + Player `Slide` on slot 0 via EventSystem **before** HUD Drive remap | one fight; then NEXT rematch persist; PAUSE → HOME |
| 2 | `np.fever.v1` | N02 | QTE (`vfxGood` tap) + Fever + **two distinct slots** with accepted `FeverTap` **`source=Player`** | same rematch/HOME as basic |
| 3 | `np.auto.v1` | N03 | HUD Full Auto → auto cast/submit evidence → HUD Manual | last fight auto-exits (no rematch), then PAUSE → HOME |
| 4 | `np.matrix.v1` | N01–N03 + L01/L02 | fight1 **NEXT** fight2 **NEXT** fight3 then PAUSE → HOME; per-battle folders | `VerificationRunPlan` fights = Basic, Fever, Auto |

Aliases accepted by `VerificationRunPlan` / `VerificationScenario.Named`: `basic`/`n01`, `fever`/`n02`, `auto`/`n03`, `matrix`/`all`/`1`. Prefer the `np.*.v1` tokens in the request file.

There is **no** leased two-fight-only token. The required “fight1 NEXT fight2 HOME + per-battle folders” session is **`np.matrix.v1`** (three fights; first NEXT is fight1→fight2; then fight3; then HOME). Single-scenario basic/fever also tap NEXT, but that rematch is the **same** scenario, not a second plan row.

`VerificationCatalog.Apply` runs **before** FIGHT (`DESIGN_PLACEHOLDER`). After start: read-only inspection only.

---

## 3. Existing smoke / Editor entrypoints (do not mix)

| Entry | Token / request | Use for A53-05? |
|---|---|---|
| `Resonance/Smoke/Natural Play` | `matrix` | yes (same as matrix) |
| `Resonance/Smoke/Natural Play Matrix` | `np.matrix.v1` | yes |
| `Resonance/Smoke/Natural Play Basic` | `np.basic.v1` | yes |
| `Resonance/Smoke/Natural Play Fever` | `np.fever.v1` | yes |
| `Resonance/Smoke/Natural Play Auto` | `np.auto.v1` | yes |
| CLI `NaturalPlaySmoke.Cli{Basic,Fever,Auto,Matrix,FromArgs}` | see §5 | yes |
| `Resonance/Run Vertical Slice Smoke` | `Temp/vs-smoke.request` | **no** |
| `Resonance/Play For Look` | play only | **no** |
| `CaptureRuntime` `-capture` | shots; uses `FireDrivePerfect` / `StartVsBattle` | **no** |
| `PuppetRigVerification.Run` | puppet fold check | **no** |
| `Resonance/Build Windows Player` / `WindowsBuild.BuildAndExit` | `Builds/Win64/Resonance.exe` | build only, after freeze |

---

## 4. Request file format

**Path (Editor, cwd = `client/`):** `client/Temp/natural-play.request`

**Body:** one token, UTF-8, no BOM. First whitespace-delimited word is the plan. Extra lines ignored.

```
np.basic.v1
```

```
np.fever.v1
```

```
np.auto.v1
```

```
np.matrix.v1
```

Runtime also reads `Temp/natural-play.running` (smoke renames request → running on enter play). Player/Editor argv `-natural-scenario <token>` overrides the file when present.

**Do not pre-write the request and then cold-start Unity.** G0 PlayMode showed startup can wipe `client/Temp`. Use `-executeMethod` / `-natural-play` (smoke arms after load) or write the request only after the Editor is up.

Companion files (smoke-owned, relative to `client/`):

| File | Role |
|---|---|
| `Temp/natural-play.request` | arm token |
| `Temp/natural-play.running` | in-play token |
| `Temp/natural-play.result.txt` | session verdict (also copied to captures) |
| `Temp/natural-play.quit` | Editor `Exit` after result |
| `Temp/natural-play-save/save.json` | isolated save (deleted on arm) |

---

## 5. Exact Editor / CLI commands

Set once:

```powershell
$Unity = "D:\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe"
$Proj  = "F:\天命之子\client"
$Kit   = "F:\天命之子\DC_RECON_KIT\m1\REVIEW_1a75253"
New-Item -ItemType Directory -Force -Path "$Kit\logs" | Out-Null
```

**Hard rules:** no `-batchmode`, no `-nographics`, no `-quit`. `NaturalPlaySmoke.Poll` / `TryEnterPlay` return immediately in batch mode. Smoke calls `EditorApplication.Exit` when `Temp/natural-play.result.txt` exists.

### 5.1 Recommended: `-executeMethod` (arms after load)

```powershell
# 1) N01 basic
& $Unity -projectPath $Proj -natural-play -natural-scenario np.basic.v1 `
  -executeMethod Resonance.EditorTools.NaturalPlaySmoke.CliBasic `
  -logFile "$Kit\logs\editor-np-basic.log"

# 2) N02 fever
& $Unity -projectPath $Proj -natural-play -natural-scenario np.fever.v1 `
  -executeMethod Resonance.EditorTools.NaturalPlaySmoke.CliFever `
  -logFile "$Kit\logs\editor-np-fever.log"

# 3) N03 auto
& $Unity -projectPath $Proj -natural-play -natural-scenario np.auto.v1 `
  -executeMethod Resonance.EditorTools.NaturalPlaySmoke.CliAuto `
  -logFile "$Kit\logs\editor-np-auto.log"

# 4) NEXT/HOME multi-fight (Basic → NEXT → Fever → NEXT → Auto → HOME)
& $Unity -projectPath $Proj -natural-play -natural-scenario np.matrix.v1 `
  -executeMethod Resonance.EditorTools.NaturalPlaySmoke.CliMatrix `
  -logFile "$Kit\logs\editor-np-matrix.log"
```

Equivalent: `-executeMethod Resonance.EditorTools.NaturalPlaySmoke.CliFromArgs` with the same `-natural-scenario`.

One-shot helper (starts one continuous ffmpeg, then Unity; does not inject battle state):

```powershell
powershell -NoProfile -File "$Kit\run-natural-play.ps1" -Scenario np.basic.v1 -Record
# after freeze, add -Run. Default is print-only.
```

### 5.2 Menus (already-open Editor)

`Resonance → Smoke → Natural Play Basic | Fever | Auto | Matrix`.

### 5.3 Argv-only (no executeMethod)

```powershell
& $Unity -projectPath $Proj -natural-play -natural-scenario np.basic.v1 `
  -logFile "$Kit\logs\editor-np-basic.log"
```

`NaturalPlaySmoke` `InitializeOnLoad` writes the request if `-natural-play` or `-natural-scenario` is present and no request/running file exists.

### 5.4 Player (only a build hashed to **this** frozen HEAD)

```powershell
# after §7
& "F:\天命之子\client\Builds\Win64\Resonance.exe" -natural-play -natural-scenario np.basic.v1
```

Standalone `Application.dataPath` is `Resonance_Data`, so results land in `client/Builds/Win64/captures/` and `.../Temp/`, not `client/captures/`. Copy them into `$Kit\artifacts\` immediately. Do **not** use `dist/windows/Resonance.exe`.

---

## 6. Output directories

Editor `CaptureShots.CapturesDir()` = `client/captures/` (absolute: `F:\天命之子\client\captures`).

| Artifact | Path |
|---|---|
| Session verdict | `client/captures/natural-play.result.txt` (copy of `client/Temp/natural-play.result.txt`) |
| Session index | `client/captures/natural-play.events.txt` **and** `client/captures/natural-play/BATTLE_INDEX.txt` (index only; not fight-1 after NEXT) |
| Per-battle folder | `client/captures/natural-play/battles/<battle_id>/` |
| Per-battle files | `header.txt`, `commands.txt`, `events.txt`, `result.txt`, `digest.txt`, `scenario.txt` |
| Shots | `client/captures/np_01_battle.png`, `np_02_tap.png`, `np_03_slide.png`, `np_04_qte.png`, `np_05_judge.png`, `np_06_pause.png`, `np_07_speed.png`, `np_08_fever.png`, `np_09_result.png`, `np_10_rematch.png`, `np_11_home.png` (created only if that phase runs) |
| Editor log | `DC_RECON_KIT/m1/REVIEW_1a75253/logs/editor-np-*.log` |
| Recording | `DC_RECON_KIT/m1/REVIEW_1a75253/recordings/np-<token>-<utc>.mp4` |
| Archived copy | `DC_RECON_KIT/m1/REVIEW_1a75253/artifacts/natural-play/<utc>/` |

`battle_id` = `np-yyyyMMddTHHmmss-00N`. First fight persist is **pre-next**, before `GameRoot.Battle` is replaced.

After each session, copy `client/captures/natural-play*` into the kit artifacts folder. A later session overwrites `natural-play.result.txt`.

Leftover `client/captures/natural-play.result.txt` (probe saw 2026-09-14 11:44, `FAIL BattlePlay`) is **not** this batch. Historical original: run7 path in the header.

---

## 7. Hash the matching-source build

Build **after** freeze, same HEAD as the Play Mode logs:

```powershell
& $Unity -batchMode -nographics -projectPath $Proj `
  -executeMethod Resonance.EditorTools.WindowsBuild.BuildAndExit `
  -logFile "$Kit\logs\win64-build.log"
```

(`WindowsBuild` may use `-batchMode`; natural-play smoke must not.)

Exe: `F:\天命之子\client\Builds\Win64\Resonance.exe`

```powershell
$exe = "F:\天命之子\client\Builds\Win64\Resonance.exe"
$app = "F:\天命之子\client\Builds\Win64\Resonance_Data\Managed\Resonance.App.dll"
$bat = "F:\天命之子\client\Builds\Win64\Resonance_Data\Managed\Resonance.Battle.dll"
"HEAD=$(git -C 'F:\天命之子' rev-parse HEAD)" | Set-Content "$Kit\artifacts\BUILD_HASHES.txt"
Get-FileHash -Algorithm SHA256 $exe, $app, $bat | Format-List | Add-Content "$Kit\artifacts\BUILD_HASHES.txt"
Get-ChildItem "F:\天命之子\client\Builds\Win64" -Recurse -File |
  Get-FileHash -Algorithm SHA256 |
  Export-Csv "$Kit\artifacts\BUILD_HASHES.csv" -NoTypeInformation
```

This batch: **NOT_RUN**. Do not hash or cite `dist/windows`.

---

## 8. One continuous recording

Attach **before** Unity Play Mode. One file per session (or one file covering all four if run back-to-back without stopping ffmpeg). Do not stitch after the fact and call it continuous.

```powershell
$recDir = "$Kit\recordings"
New-Item -ItemType Directory -Force -Path $recDir | Out-Null
$mp4 = Join-Path $recDir ("np-matrix-{0:yyyyMMddTHHmmssZ}.mp4" -f (Get-Date).ToUniversalTime())
# Desktop grab includes the Editor Game view (portrait 1080×1920). Stop ffmpeg after Editor Exit.
ffmpeg -y -f gdigrab -framerate 30 -draw_mouse 1 -i desktop -c:v libx264 -pix_fmt yuv420p -preset veryfast $mp4
```

`run-natural-play.ps1 -Record` starts this, then Unity, then stops ffmpeg when the result file appears or Unity exits.

This batch: **NOT_RUN**.

---

## 9. BLOCKED / NOT_RUN reasons (this probe)

| Check | Result |
|---|---|
| Unity 6000.3.23f1 binary | available |
| License / Hub | not exercised (Editor not launched) |
| `client/` Editor lock | free |
| Source freeze | **not frozen** — `CatalogJson.cs`, `BattleReplay.cs` dirty (other A53 leases) |
| Play Mode | **NOT_RUN** — would compile contested sources and write `Library` |
| Scene fight | avoided (no second Editor; also avoided first Editor against live A53 edits) |
| Matching-source player | **NOT_RUN** / missing `client/Builds/Win64/Resonance.exe` |
| Recording | **NOT_RUN** (no session to attach) |

Unrun = **NOT_RUN**. Coordinator re-runs §5 after freeze. A missing artifact stays NOT_RUN/BLOCKED; do not backfill from run7 or an old dist.

---

## 10. Post-run acceptance (when someone actually runs)

1. `natural-play.result.txt` first line `PASS` or `FAIL <phase>`. Either is a real result; do not coerce to Perfect.
2. `pointer=UnityEngine.EventSystem`. `historical_verdict=FAIL` for run7 must still be printed.
3. `np.basic.v1`: `commands.txt` has accepted `kind=Tap` and `kind=Slide` with `source=Player`.
4. `np.fever.v1`: accepted `DriveResolve` (QTE), Fever reached, and **≥2 distinct `slot=`** on lines `kind=FeverTap source=Player accepted=True`. **`source=Auto` FeverTap must not count as Player.** In-memory `DistinctAcceptedFeverSlots` currently does **not** filter `CommandSource`; treat `commands.txt` as authoritative.
5. `np.auto.v1`: Auto toggled via HUD (`> MANUAL` / `> SEMI AUTO` / `> FULL AUTO`); record Submit Auto if present; SoftFail if AutoFire still bypasses Submit.
6. Matrix: `BATTLE_INDEX.txt` lists ≥2 persisted `battle_id`s; fight-1 `header.txt` / `commands.txt` / `events.txt` unchanged after fight-2 persist.
7. Replay read-back is A53-EVIDENCE / coordinator, not this lease.

---

## 11. What this task changed / ran

- Edited (lease): `client/Assets/Editor/Smoke/NaturalPlaySmoke.cs` — menu token `np.matrix.v1`; CLI `CliBasic` / `CliFever` / `CliAuto` / `CliMatrix` / `CliFromArgs`; `-natural-play` / `-natural-scenario` arm.
- Wrote: this file; `run-natural-play.ps1`.
- Did **not** edit BattleSim, Catalog, GameRoot, tests, run7, or `dist/`.
- Did **not** launch Unity Play Mode. Matrix / basic / fever / auto / build / recording = **NOT_RUN**.
