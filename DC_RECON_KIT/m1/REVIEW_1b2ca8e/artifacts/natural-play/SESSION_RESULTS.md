# B1-A4 executed sessions — 2026-09-14

HEAD `1b2ca8e4ff56d9f0986e158fc97b50498f735cb3`. Pointer `UnityEngine.EventSystem`. run7 historical FAIL kept.

## Sessions

| Token | Verdict | Session | Fights | Archive |
|---|---|---|---|---|
| `np.basic.v1` | **PASS** Tap+Slide Player before Drive remap; NEXT rematch + HOME | `np-20260914T152600` | `…-001` Victory, `…-002` rematch InProgress | `artifacts/natural-play/20260914T152459-np_basic_v1/` |
| `np.fever.v1` | **FAIL FeverPlay** three Player QTE Good; never Fever; party wipe (Defeat) | `np-20260914T153202` | `…-001` Defeat, `…-002` RETRY then HOME | `artifacts/natural-play/20260914T153139-np_fever_v1/` |
| `np.fever.v1` (B1-F2) | **BLOCKED** Safe Mode CS0246 `BattleReplay.OpeningGrowth` `FiveStat` (F1 lease). Play never started. Window-poll code not executed. Not NOT_RUN-as-pass. | — | — | `artifacts/natural-play/20260914T171516-np_fever_v1/` |
| `np.fever.v1` (B1-F2 relaunch) | **FAIL OpenQte#3** compile ok; 2× Player DriveResolve **Perfect** (gauge 40→80); 3rd DriveBegin `UnitDead` (p0). feverEver=False. No FeverTap. | `np-20260914T175102` | `…-001` InProgress | `artifacts/natural-play/20260914T175019-np_fever_v1/` |
| `np.fever.v1` (B1-F2 living-slot) | **FAIL FeverPlay** (160s timeout after goals). feverEver=True. 3×Perfect (p0,p0,p1). Player FeverTap slots 1,2. DriveBegin last open=p1 not dead p0. Not PATH_A. | `np-20260914T181210` | `…-001` InProgress | `artifacts/natural-play/20260914T181042-np_fever_v1/` |
| `np.fever.v1` (B1-F2 goal-exit) | First line **PASS**. feverEver=True. 3×Perfect DriveBegin 0,0,0. Player FeverTap 0,1. NEXT+HOME. Not PATH_A (readback still Match=False). | `np-20260914T183818` | `…-001` Victory, `…-002` rematch InProgress | `artifacts/natural-play/20260914T183658-np_fever_v1/` |
| `np.auto.v1` | **PASS** HUD Full Auto then Manual | `np-20260914T153840` | `…-001` | `artifacts/natural-play/20260914T153700-np_auto_v1/` |

Catalog note on all three: `mode=<token>+A53_SUB_STRIP_DOT_FLAME sub=A53_SUB_STRIP_DOT_FLAME`.

Fever FAIL is a real run, not NOT_RUN. Missing=Fever,FeverTapA,FeverTapB. Drive=88 at wipe. No state injection.

B1-F2 relaunch `20260914T171516` is **BLOCKED** (Safe Mode CS0246 `BattleReplay.cs` `FiveStat`, F1 lease). Play Mode never started. Not a fever pass.

B1-F2 relaunch `20260914T175019` compiled and Play ran. First line `FAIL OpenQte#3`. Window poll hit two Perfects (`age≈0.52`). p0 died before a third QTE. feverEver=False. No FeverTap. Not a PATH_A pass. Window-poll code not rewritten.

B1-F2 living-slot `20260914T181042`: first line `FAIL FeverPlay` (loop 160s after goals already recorded). feverEver=True. DriveBegin slots 0,0,1 (OpenQte#3=`p1`). DriveResolve Perfect×3 then FeverTap Player 1 and 2. EnemyAtkMul not changed. Not a PATH_A pass.

B1-F2 goal-exit `20260914T183658`: first line `PASS`. Compile ok. Fight-1 feverEver=True. DriveBegin slots 0,0,0 (p0 stayed alive). DriveResolve Perfect×3. FeverTap Player slots 0 and 1. WaitResult saw Victory; no Run() fever-goals exit added. Historical FAIL/BLOCKED rows kept. Not PATH_A_ENGINEERING_PASS.

## Fight-1 readback

Command: `dotnet run --project REVIEW_1b2ca8e/readback/Readback.csproj -- <fight1-dir>`

Tape: `…/20260914T152459-np_basic_v1/natural-play/battles/np-20260914T152600-001`

`NaturalPlayBattleEvidence.Load` + `BattleReplayer.Verify` on a **new** sim after `VerificationCatalog.Apply`.

**Match=False** after F1 growth recovery. `NamedOpeningDiff=null` `VersionDiff=0` MaxHp 2460. CommandDiff=0 Unconsumed=0. DigestDiff=17 EventDiff=1 FirstEventDivergence=12 ExpectedEventCount=100 ActualEventCount=124. Mid-fight: TickIndex 191≠163, Drive 58≠68. Full text: `fight1-readback.txt`.

## Recording

`REVIEW_1b2ca8e/recordings/np-continuous-20260914T143817.mp4` (656670768 bytes). One ffmpeg gdigrab from before first launch through auto.

## Build hashes

`artifacts/BUILD_HASHES.txt` — WindowsBuild 2026-09-14, not dist/windows:

```
SHA256 82EFD5A8C297A5046F6BB72C240DAB8972E64003851935EBA0A46AD121CE0305 Resonance.exe
SHA256 1996CD7BDFB3CB2A49552386D336BCB5A89449F12CE131F239DBAE5346152884 Resonance.App.dll
SHA256 70CC76F845838B00B7B059982134B6296AE9C2575FF50947931B8DBD1E9C82DB Resonance.Battle.dll
```

## First-launch BLOCKED (recovered)

`NaturalPlaySmoke.cs` CS0104 (`Object` ambiguous after `using System`) → Safe Mode → `executeMethod class could not be found`. Fixed `UnityEngine.Object.FindFirstObjectByType`. Relaunched. Log: `logs/editor-np-basic-v1-20260914T143934.log`.
