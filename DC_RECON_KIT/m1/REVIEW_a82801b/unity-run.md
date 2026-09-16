# C-C4 — natural-play / TRX / readback / build runbook (a82801b batch C, frozen tree 3d2d1e6)

**task_id:** `C-C4`  
**baseline:** `a82801b80fd81fb4f8ff30cf45b72b30fcd63521`  
**HEAD (frozen, verified before and after):** `3d2d1e67554251b77d05ad732dc06390a114de05` ("Land C1-C3 Path A fixes")  
**executed:** 2026-09-16 12:48–14:20 +08  
**pointer:** `UnityEngine.EventSystem` only. `os_touch=NOT_CLAIMED`.  
**historical:** run7 (`G2_REVIEW_20260913/artifacts/natural-play/run7`) stays **FAIL BattlePlay missing=Tap,Slide**; 1b2ca8e basic tape `np-20260914T152600-001` stays **Match=False** (`legacy-recovered`). Neither rewritten.  
**labels:** hold seconds 1.47/0.70/2.00 = DESIGN_PLACEHOLDER (not GL). `M1_FIDELITY=DEFERRED_NOT_REMOVED`. `G3_NOT_STARTED`. No T27 / 90% / M1 claims.  
**results:** `artifacts/natural-play/SESSION_RESULTS.md`.

---

## 1. Execution table

| Item | Fact |
|---|---|
| Unity.exe | `D:\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe` `6000.3.23f1` |
| Preconditions | HEAD ok; `git status --short -- client/Assets/Scripts tools/BattleSim.Tests` empty; 0 `Unity.exe`; no `EditorInstance.json`; `ffmpeg`/`ffprobe` 8.1.1 on PATH; `StripDotFlame` bind in `VerificationCatalog.cs` = True |
| Unfiltered TRX | exit **0**; **271 passed / 0 failed / 1 skipped** (`N01_N02_N03_NaturalPlayContract_DocumentedNotFaked`); `artifacts/tests/C4-full.trx` + `C4-full.console.txt`; single run |
| `np.basic.v1` | **PASS**; fights `np-20260916T050211-001` (Victory) / `-002` (rematch, InProgress); Player Slide tick 32 + Player Tap tick 95; hold `begin.slide 44`/`end.slide`, `begin.wave 60`/`end.wave` |
| `np.fever.v1` | **PASS** (first line); fight-1 `np-20260916T050619-001` **Defeat** after goals; 3× Player QTE Perfect; Player `FeverTap` slots 1, 2; `-002` retry InProgress; no hold events (no Slide / Player QTE resolve does not hold / no wave 2) |
| `np.auto.v1` | **PASS**; fight `np-20260916T051101-001` InProgress; `SetAuto` 1→2→0 by Player; 10 `source=Auto` Tap/Slide; `begin.slide` ×2; `begin.drive` NOT_OBSERVED_IN_SCENARIO |
| `FocusEnemy` | NOT_OBSERVED_IN_SCENARIO (all three; scenarios do not tap an enemy) |
| HUD hold writes | `fixture.DebugForceHold` / `fixture.hold`: 0 matches in all three editor logs |
| Readback new tapes | basic / fever / auto **Match=True**, `openingSource=input`, CommandDiff=Unconsumed=DigestDiff=EventDiff=VersionDiff=0 |
| Readback legacy | **Match=False**, `openingSource=legacy-recovered`, `NamedOpeningDiff=DataIdentity: tape != replayed`, VersionDiff=1 (clockIdentity 15 → 18 fields) |
| Fight-1 integrity | 21/21 files byte-identical archive vs live after fight-2 persist (`artifacts/natural-play/FIGHT1_HASHES.txt`) |
| Recording (C4) | `recordings/np-continuous-20260916T045756.mp4` 37748784 bytes; **PARTIAL_UNFINALIZED** (no `moov`) |
| Recording (playable re-run) | `recordings/np-continuous-20260916T085721.mp4` 38797348 bytes; `ffprobe` 451.97s h264 5120×1440@30. Second pass of basic/fever/auto, not the Match=True session |
| Win64 build | C-C4 first attempt BLOCKED (GICache). Coordinator recreated junction target + clean rebuild **PASS**. Hashes `artifacts/BUILD_HASHES.txt`. Player stub SHA same as 1b2ca8e engine wrapper; App/Battle DLLs are `3d2d1e6`. |

`dist/windows` not hashed. 2026-09-14 leftover exe overwritten by the 16:52 clean rebuild.

---

## 2. Exact commands run

```powershell
$Unity = "D:\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe"
$Proj  = "F:\天命之子\client"
$Kit   = "F:\天命之子\DC_RECON_KIT\m1\REVIEW_a82801b"

# 1. unfiltered suite
cd F:\天命之子\tools\BattleSim.Tests
dotnet test --nologo --logger "trx;LogFileName=C4-full.trx" 2>&1 | Tee-Object -FilePath $Kit\artifacts\tests\C4-full.console.txt   # exit 0
Copy-Item TestResults\C4-full.trx $Kit\artifacts\tests\

# 2. one continuous recording (operator-owned; helper called without -Record)
ffmpeg -y -f gdigrab -framerate 30 -draw_mouse 1 -i desktop -c:v libx264 -pix_fmt yuv420p -preset veryfast $Kit\recordings\np-continuous-20260916T045756.mp4

# 3. natural play, serial, one Editor at a time (no -batchmode / -nographics / -quit)
powershell -NoProfile -File $Kit\run-natural-play.ps1 -Scenario np.basic.v1 -Run
powershell -NoProfile -File $Kit\run-natural-play.ps1 -Scenario np.fever.v1 -Run
powershell -NoProfile -File $Kit\run-natural-play.ps1 -Scenario np.auto.v1  -Run
# after each: waited until Get-Process Unity returned nothing (Editor exits itself)

# 4. readback (standard BattleReplayer.Verify on a new sim)
dotnet run --project $Kit\readback\Readback.csproj -- <fight1 dir>   # ×3 new tapes + legacy 1b2ca8e tape

# 5. build (BLOCKED, see §4)
& $Unity -batchMode -nographics -projectPath $Proj -executeMethod Resonance.EditorTools.WindowsBuild.BuildAndExit -logFile $Kit\logs\win64-build.log
```

Helper output per scenario: `logs/helper-np-<scenario>.console.txt`; editor logs `logs/editor-np-<scenario>-<stamp>.log` (also copied into each archive).

---

## 3. Recording — what went wrong

ffmpeg was stopped with `Stop-Process` (the same method `run-natural-play.ps1` uses). Killed that way, ffmpeg does not finalize the MP4 (`moov` atom missing). The 37.7 MB file contains the encoded frames from 12:57:56 to 13:12:54 +08 but is not playable by standard tools; a raw h264 remux failed (`non-existing PPS 0 referenced`, SPS/PPS were only in the missing `moov`). Not faked, not replaced, not re-recorded. Next batch: start ffmpeg with redirected stdin and send `q`, or add `-movflags +faststart+frag_keyframe+empty_moov` so a kill leaves a playable file.

---

## 4. Build — BLOCKED detail

- Started 13:20:31 +08 (Unity pid 56184, child of the operator's script host). `Start-Process -Wait` returned exit 1 after ~5 s while the child kept running; polled the pid instead.
- Log grew to 103 MB / 755 744 lines: 62 260 repeats of  
  `CreateDirectory 'C:/Users/Administrator/AppData/LocalLow/Unity/Caches' failed: 系统找不到指定的路径。 (current dir: F:/天命之子/client)`  
  `Failed to create GICache directory at the default location: C:/Users/Administrator/AppData/LocalLow/Unity/Caches/GiCache.`  
  stack `Resonance.EditorTools.WindowsBuild:Build () (at Assets/Editor/Build/WindowsBuild.cs:30)`; nothing else progressed; `client/Temp/win.build.result.txt` never written.
- Killed at 14:09 (own process). Log kept as `logs/win64-build.attempt1-GICACHE-LOOP.log.zip` (full) + `logs/win64-build.attempt1-GICACHE-LOOP.excerpt.txt` (head/tail; contains one NUL-filled sparse line).
- Cause: `C:\Users\Administrator\AppData\LocalLow\Unity` is a junction → `f:\金山毒霸搬家目录\软件\Unity\Unity引擎(游戏)数据\10020\Unity\`; `f:\金山毒霸搬家目录\软件\Unity` no longer exists (parent modified 2026-09-16 00:59:53). The 2026-09-14 build (`REVIEW_1b2ca8e/logs/win64-build.log`) logged `Created GICache directory at C:/Users/Administrator/AppData/LocalLow/Unity/Caches/GiCache`, so the target vanished after that build.
- Not a source problem; no source edited. Fix requires touching the host outside the kit (recreate the empty junction target or repoint the junction) — proposed in `artifacts/BUILD_HASHES.txt`, **not applied**. Rerun the exact command afterwards; then hash `client/Builds/Win64/Resonance.exe`, `Resonance_Data/Managed/Resonance.App.dll`, `Resonance.Battle.dll` (refuse `dist/windows` and the 09-14 exe).

---

## 5. Output directories

| Artifact | Path |
|---|---|
| Session verdicts | `artifacts/natural-play/<stamp>-<scenario>/natural-play.result.txt` |
| Per-battle | `artifacts/natural-play/<stamp>-<scenario>/natural-play/battles/<id>/{header,commands,events,result,digest,scenario,replay.jsonl}` |
| Index | `…/natural-play/BATTLE_INDEX.txt` |
| Shots | `…/np_*.png` |
| Editor logs | `logs/editor-np-*-20260916T*.log` |
| Recording | `recordings/np-continuous-20260916T045756.mp4` (unfinalized) |
| TRX | `artifacts/tests/C4-full.trx`, `C4-full.console.txt` |
| Readback | `artifacts/readback/{np-basic-v1,np-fever-v1,np-auto-v1}-fight1-readback.txt`, `fight1-legacy-readback.txt` |
| Hashes | `artifacts/BUILD_HASHES.txt` (BLOCKED), `artifacts/natural-play/FIGHT1_HASHES.txt` |
| Results | `artifacts/natural-play/SESSION_RESULTS.md` |

---

## 6. Writes / non-writes

- Wrote (all under `REVIEW_a82801b/`): `run-natural-play.ps1` (copy of 1b2ca8e helper; header/labels only), `unity-run.md`, `artifacts/**`, `logs/**`, `recordings/**`. Per-scenario `BUILD_HASHES.txt` inside each archive is the helper's own leftover (`player_build=LEFTOVER_NOT_HASHED`, points at the 09-14 exe) — the authoritative one is `artifacts/BUILD_HASHES.txt`.
- Did **not** edit anything under `client/Assets/Scripts`, `tools/BattleSim.Tests`, `dist/`, `client/f_*.jpg`, csproj, `_Recovery`, `codex专区`, hires tiles, `REVIEW_1b2ca8e/**`, `G2_REVIEW_20260913/**`. Did not edit tapes. No git commit / push.
- Runtime side effects outside the kit (by the smoke itself, as designed): `client/captures/natural-play*`, `client/captures/np_*.png`, `client/Temp/natural-play.*`; `client/Library/EditorInstance.json` left by the killed build was removed after confirming 0 Unity processes.
