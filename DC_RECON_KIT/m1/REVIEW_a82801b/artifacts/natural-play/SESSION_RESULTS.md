# C-C4 executed sessions — 2026-09-16

HEAD `3d2d1e67554251b77d05ad732dc06390a114de05` ("Land C1-C3 Path A fixes"), frozen; `git status --short -- client/Assets/Scripts tools/BattleSim.Tests` empty before and after. Unity `6000.3.23f1`. Pointer `UnityEngine.EventSystem`. `os_touch=NOT_CLAIMED`. run7 and all 1b2ca8e tapes stay FAIL, not rewritten. Hold seconds 1.47 / 0.70 / 2.00 are DESIGN_PLACEHOLDER, not GL. `M1_FIDELITY=DEFERRED_NOT_REMOVED`. `G3_NOT_STARTED`. No T27 / 90% / M1 claims.

## Unfiltered suite (C4-T1)

`cd tools\BattleSim.Tests; dotnet test --nologo --logger "trx;LogFileName=C4-full.trx"` → exit **0**, **271 passed / 0 failed / 1 skipped / 272 total** (6 s). Skip = `G2RecheckNaturalContractTests.N01_N02_N03_NaturalPlayContract_DocumentedNotFaked` (Unity contract; documented, not faked). Files: `artifacts/tests/C4-full.trx` (`<Counters total="272" executed="271" passed="271" failed="0" ...>`), `artifacts/tests/C4-full.console.txt`. Single run; no rerun.

## Sessions (one Editor at a time, serial; no -batchmode/-nographics/-quit; helper `-Run` without `-Record`)

| Token | Verdict (first line) | Session | Fights | Archive |
|---|---|---|---|---|
| `np.basic.v1` | **PASS** Player Slide (tick 32) + Player Tap (tick 95); NEXT rematch + PAUSE → HOME | `np-20260916T050211` | `…-001` Victory, `…-002` rematch InProgress | `artifacts/natural-play/20260916T050046-np_basic_v1/` |
| `np.fever.v1` | **PASS** 3× Player QTE `DriveResolve` Perfect (slots 0,0,1 → fever); Player `FeverTap` slots 1 and 2; fight-1 later ended **Defeat**; RETRY → PAUSE → HOME | `np-20260916T050619` | `…-001` Defeat, `…-002` retry InProgress | `artifacts/natural-play/20260916T050523-np_fever_v1/` |
| `np.auto.v1` | **PASS** HUD `SetAuto` 1 → 2 (Auto=Full) → 0 (Manual); 10 `source=Auto` Tap/Slide submits at ticks 25 and 107 | `np-20260916T051101` | `…-001` InProgress | `artifacts/natural-play/20260916T050949-np_auto_v1/` |

Catalog line on all three: `mode=<token>+A53_SUB_STRIP_DOT_FLAME sub=A53_SUB_STRIP_DOT_FLAME`. Each archive contains `natural-play.result.txt`, `natural-play/battles/<id>/{header,commands,events,result,digest,scenario,replay.jsonl}`, `natural-play/BATTLE_INDEX.txt`, `np_*.png`, editor log. Unity exited by itself after each scenario (0 `Unity.exe` before the next launch; `EditorInstance.json` absent).

Fever note: first line is `PASS` (goal-exit contract: Fever + two Player FeverTap slots recorded before the wipe). Fight-1 outcome is `Defeat` (`title=DEFEAT`). Not hidden. No HP/Drive/Charge/Fever/time/outcome writes; no FirePerfect.

## C-batch facts per fight-1 tape

| Fact | basic `np-20260916T050211-001` | fever `np-20260916T050619-001` | auto `np-20260916T051101-001` |
|---|---|---|---|
| `openingSource` | `input` | `input` | `input` |
| `openingProgress` has `;r=` | yes (5 rows, e.g. `C001:lv=1;g0=EQ_WPN;g3=SC006;r=TSTEE`) | yes | yes |
| `openingMods` | `food=1.000;carta=1.000` | same | same |
| `openingInputsIdentity` | present (`…#m=food=1.000;carta=1.000`) | present | present |
| `clockIdentity` fields | **18**, last three `1.470|0.700|2.000` | 18, `1.470|0.700|2.000` | 18, `1.470|0.700|2.000` |
| `kind=hold` events | `32 begin.slide 44` / `76 end.slide`; `295 begin.wave 60` / `355 end.wave` | **none** (no Slide, Player QTE resolve does not hold, no wave 2 — by C2 design) | `25 begin.slide 44` / `69 end.slide`; `107 begin.slide 44` (tape ends at tick 121, still held) |
| `begin.drive` | not observed | not observed | **NOT_OBSERVED_IN_SCENARIO** (Auto never fired a Drive; gauge 94 at exit) |
| Player Tap/Slide before Drive remap | `seq=4 tick=32 kind=Slide source=Player accepted=True`, `seq=5 tick=95 kind=Tap source=Player accepted=True`; no Drive in fight | n/a | n/a |
| FeverTap | n/a | `seq=7 tick=1913 kind=FeverTap slot=1 source=Player accepted=True`, `seq=8 tick=1929 kind=FeverTap slot=2 source=Player accepted=True` (2 distinct slots; no Auto FeverTap) | n/a |
| `source=Auto` | none | none | seq 2–6 (tick 25) and 8–12 (tick 107) Tap/Slide `source=Auto accepted=True`; `SetAuto` Player value 1 (tick 18), 2 (tick 25), 0 (tick 111) |
| `FocusEnemy` | **FocusEnemy=NOT_OBSERVED_IN_SCENARIO** | NOT_OBSERVED_IN_SCENARIO | NOT_OBSERVED_IN_SCENARIO |
| Editor log `fixture.DebugForceHold` / `fixture.hold` | 0 matches | 0 matches | 0 matches |
| Editor log `error CS` / Safe Mode | 0 / 0 | 0 / 0 | 0 / 0 |

Events format is `tick|phase|idx|kind|opcode|…`; hold rows appear as `…|hold|begin.slide|…|44|…` (replay.jsonl `"kind":"hold","opcode":"begin.slide","amount":44`).

## File readback (standard `BattleReplayer.Verify`, new sim, `REVIEW_a82801b/readback`)

Command: `dotnet run --project DC_RECON_KIT\m1\REVIEW_a82801b\readback\Readback.csproj -- <fight1 dir>`

| Tape | openingSource | Match | CommandDiff / Unconsumed / DigestDiff / EventDiff / VersionDiff | Events | File |
|---|---|---|---|---|---|
| basic `np-20260916T050211-001` | `input` | **True** | 0 / 0 / 0 / 0 / 0, `NamedOpeningDiff=null`, `FirstEventDivergence=-1` | 187 = 187 | `artifacts/readback/np-basic-v1-fight1-readback.txt` |
| fever `np-20260916T050619-001` | `input` | **True** | 0 / 0 / 0 / 0 / 0, `NamedOpeningDiff=null` | 1146 = 1146 | `artifacts/readback/np-fever-v1-fight1-readback.txt` |
| auto `np-20260916T051101-001` | `input` | **True** | 0 / 0 / 0 / 0 / 0, `NamedOpeningDiff=null` | 69 = 69 | `artifacts/readback/np-auto-v1-fight1-readback.txt` |
| legacy 1b2ca8e `np-20260914T152600-001` | `legacy-recovered` (`r=EEEEE` rows) | **False** (required) | 1 / 0 / 19 / 1 / 1; first named diff `NamedOpeningDiff=DataIdentity: tape != replayed`; first event diff `[12] 69|resolve|opcode|dmg.auto|0|2|-1 != 33|resolve|hold|begin.slide|44|-1|-1`; legacy `clockIdentity` has 15 fields vs 18 now (ClockKey gained the three hold fields) | 100 vs 117 | `artifacts/readback/fight1-legacy-readback.txt` |

Legacy tape untouched (mtime 2026-09-14 23:26:12; `git status` clean for `REVIEW_1b2ca8e`).

Fight-1 integrity: all 7 files of each fight-1 folder are byte-identical between the archive copy and `client/captures/natural-play/battles/<id>` after the fight-2 persist / HOME (`artifacts/natural-play/FIGHT1_HASHES.txt`, 21/21 `archive_same=True`).

## Recording

C-C4 first file: `recordings/np-continuous-20260916T045756.mp4` **37748784 bytes**. **PARTIAL_UNFINALIZED** (no `moov`; `ffprobe` fails). Kept, not faked.

Coordinator second pass (2026-09-16 16:57–17:04 +08), same three tokens, `ffmpeg -movflags +frag_keyframe+empty_moov`: `recordings/np-continuous-20260916T085721.mp4` **38797348 bytes**, `ffprobe` duration=**451.966667** s, h264 5120×1440 @ 30. Helper first lines all **PASS**. Archives `20260916T085805-np_basic_v1`, `20260916T090011-np_fever_v1`, `20260916T090337-np_auto_v1`. This video is **not** the Match=True fight-1 session.

## Win64 build

C-C4 first attempt **BLOCKED** (GICache junction target missing; 49 min loop; log zip kept). Coordinator recreated the empty junction target and clean-rebuilt **PASS**. Authoritative hashes: `artifacts/BUILD_HASHES.txt` (also `BUILD_HASHES.PASS.txt`).

- exe `82EFD5A8C297A5046F6BB72C240DAB8972E64003851935EBA0A46AD121CE0305` (Unity player stub; same SHA as 1b2ca8e engine wrapper)
- App.dll `42DABD5AA0CD3D5441247A50957BD507BFC55986FCD30829A9189F241C220D39`
- Battle.dll `353240ABC188842C3CD266146F45D1AF9D7C996FD73214EBEAF61DEBDD9A6CF1`
- mtime 2026-09-16 16:52 +08; log `logs/win64-build.clean.log`
- `dist/windows` not hashed

## Preconditions recorded

HEAD `3d2d1e6…` ✔; combat tree clean ✔; `Unity.exe` present ✔; 0 Unity processes and no `EditorInstance.json` before each launch ✔; `ffmpeg` 8.1.1 and `ffprobe` on PATH ✔; `dotnet` 6.0.410. Helper copied from 1b2ca8e with header/labels only changed; StripDotFlame gate kept (`A2 bind … True`).
