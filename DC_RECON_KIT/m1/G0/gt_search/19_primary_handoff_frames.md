# Primary GT frame notes (handoff P0/P1) — not acceptance

Date: 2026-09-12  
Sources:
- `aSbBuFD12HY_GL_EN_Gameplay_Part1_202308.mp4` (P0, ~584s) — ordinary GL EN tutorial/story PVE
- `hgqXY5M9gFk_GL_ND_Robin_Boss.mp4` (P1, ~74s) — ND Robin (special mode; Drive/QTE/VFX useful)

Method caveat: MediaRecorder remux (vp9 296×640, avg 30.00 fps). Cue inventory only; fidelity **0**. Overlay holds: `m1/G0/TIMING_DIFF.md` (draft, not T28).

## Dense scan (`_frames_p0_scan/`, ~30s step)

| t | Scene | GT cues |
|---|---|---|
| 0–30 | early / load | — |
| **352** | Mona tip (skill ready) | Skill Gauge ready; Pixie yellow ready ring |
| **358** | Slide tip | “Try out a Slide Skill!” / “Touch the icon, and slide it up!”; hand + ready ring on Pixie |
| **380** | Drive tip | “Try out a Drive Skill!”; Mona **DRIVE SKILL READY** + hand; cool on Pixie; TEAM/SKILL HP labels |
| **388** | Drive tip plate | Drive builds per attack; Slide/crit faster; tip art 5× READY; field **`13% Skill Rate`**; cool dual COOL TIME |
| **60** | Battle `1-1 Heaven's Dust` **PHASE 1/3** | Tip overlay; dmg `15300`; party Lisa/Mona/Davi **`60 MAX`**; **`58% ENEMY HP REM.`**; **`TEAM HP TOTAL` 99%** + `SKILL HP TOTAL`; **FEVER 0%**; Mona portrait glow |
| **90** | Same stage **PHASE 2/3** | Boss `The Hanged`; Drive cut-in (Davi); short **`SKILL GAUGE` 94%**; **FEVER 0%**; ENEMY HP ~21% |
| 120 | Battle dialogue | EN `Mona` line + `BATTLE TIME` + `>> SKIP` |
| 150–180 | transition | thin/black frames |
| 210 | Story street VN | `SKIP` + EN narration (Infernal Realm) — not battle HUD |
| 240 | thin | skip |
| 270–330 | mix | — |
| **360** | Tutorial Slide | **`IT'S SHOWTIME!!`** + yellow **`SLIDE SKILL`** + `Freeze Lance` **`RANK 1 LV 1/10`**; top **`ENEMY HP LEFT`**; FEVER 0% |
| **365** | Drive tutorial | Mona Drive tip hand; Pixie **`7 SKILL TIME`**; short **`SKILL GAUGE`**; FEVER 0% |
| **390–392** | Victory splash | Kanji **`勝利`** + laurel + lowercase **`victory`** + `Tap the screen.` |
| **396** | CLEAR plate | **`CLEAR!!`** + **3★ arc** + reward portraits (Silent Pixie/Mona `Lv.1`) + yellow band `2 LEVEL` / `EXP 6/12` / `6 EXP` / `73 GOLD` (tutorial values; not formula proof) |
| **470** | Account LEVEL UP | **`LEVEL UP!`** emblem + `Level up!` + `Level 2 → 3` + stamina raise lines + Confirm（数值教程窗；公式 UNKNOWN） |
| **345** | HP tip | Team **Total HP** tip; bottom **`TOTAL HP TOTAL`** 100% (SHARED inventory) |
| **420** | Fever tip | Mona's Tips: Fever Time when gauge → **100%**; FEVER 0% |
| **435** | Fever tip | `Activate Fever Time!` / **For 14 seconds** tap icons; FEVER 0% |
| **445** | Fever tip (rates) | Banner `FEVER TIME!!`; tip field **`TOTAL 427 DAMAGE`**; **PERFECT +40% / GREAT +30% / GOOD +15%** |
| **88** | Mid story fight | Fresh Skull / inventory OCR **`The Ranger`** vs wooden **`The Hanged`** (t83); Davi **`Fire Serpent`**; inventory **`SKILL UP TOTAL`**; `ENEMY HP TOTAL` / `SKILL GAUGE TOTAL` |
| **435** | Fever tip | TIP! Activate Fever Time! / 14s body; bottom **`SKILL HP TOTAL`**; FEVER 0% |
| **440** | Fever live + tip | `FEVER TIME` + `12.5s`; tip Activate; **`N COMBO` / `N DAMAGE`**; FEVER 0% arc |
| **445** | Fever rates tip | TIP! barrage + **`FEVER TIME!!`** art; PERFECT 40 / GREAT 30 / GOOD 15; field **`TOTAL 427 DAMAGE`** |
| **470** | Account LEVEL UP | **`LEVEL UP!`** + `Level up!` + **`2 > 3`** + stamina refill/max+1 + Confirm |
| **472** | CLEAR Stage 3 | **`CLEAR!!`** + 3★; Silent Pixie/Mona; band `3 LEVEL` / `EXP 0/18` / `6 EXP` / `70 GOLD` / `LEVEL UP` chip; rail **BOSS** / Retry / Back |
| **450** | Tutorial fight | `>> X1 SPEED` dashed highlight; FEVER 0%; Mona tip typing (`I`) |
| **451** | Speed tip (full) | Mona's Tips: *If you want to kick up the pace a bit in battle, tap **X2 SPEED** to make all attacks **two times faster**.* |
| **452** | Mid tutorial | Pixie Auto/costume **`Dark Water`** + **`WEAKPOINT`**; Mona **`Eclipse`**; `MY HP TOTAL` / `SKILL GAUGE TOTAL`; FEVER 0% |
| **495–504** | Mid tutorial fight | `>> X2 SPEED`; **`SKILL HP TOTAL`**; `PHASE 1/2`; `ENEMY HP TOTAL`; FEVER 0% |
| **65** | Wave stamp | Center **PHASE 2** + stage `1-1 Heaven's Dust`; heart crest; **`SKILL HP TOTAL`**; FEVER 0% |
| **510** | Wave stamp | `Stage 4 Tutorial 4` + **PHASE 2** splash; top **`ENEMY HP TOTAL`/`ENEMY HP (CURRENT)`**; bottom **`TEAM HP TOTAL`/`TEAM HP (CURRENT)`**; `>> X2 SPEED`; FEVER 0% |
| **512** | Phase 2 mid | `PHASE 2/2`; bottom **`SKILL HP TOTAL`**; Valkyrie Lv.7 + Pixie/Mona |
| 555–580 | lobby / summon / NOTICE | out of M1 battle scope |

## Robin dense (`_frames_p1_robin*`)

| t | Scene | GT cues |
|---|---|---|
| ~5 | Drive cut-in | full portrait on gold ring; party tray LV60 |
| ~35 | field + pause chrome | `PAUSE` / `>> X1 SPEED` / `> FULL AUTO`; Drive-ready ghost portrait |
| **~45** (`_frames_p1_robin/t45.png`) | SHOWTIME Slide | **`IT'S SHOWTIME!!`** + **`SLIDE SKILL`** + `Saint's Blessings`; **`>> X3 SPEED`** + **`> FULL AUTO`** + **`PAUSE`**; FEVER 0% |
| ~48 | field | `DRIVE SKILL READY` on portrait; DEF↑ chips; PAUSE/AUTO/X1 |
| **~55** | QTE settle | **`PERFECT!`** + **`DAMAGE 150%`** + **`N% TO FEVER`** counting **0→13→40**; bottom **`FEVER 40%`**. 13% is mid-tween, not a second formula. X1 + FULL AUTO |
| ~56.5 | mid-fight | large **`40% TO FEVER`** + bottom **`FEVER 40%`**; Drive-ready Pomona; cool times; X1 + FULL AUTO |
| ~57.5 | paused field | PAUSE overlay + Repeat; FEVER 40%; Drive ghost portrait |
| ~62 | SHOWTIME mid-fight | Slide `White Veil` + SHOWTIME; **`RANK 7 LV 10/10`**; **`ENEMY HP REMAINING`**; FEVER 40% (gauge banked, not FEVER TIME window) |
| ~68 | field | `WeakPoint` + dmg; **FEVER 40%**; PAUSE/AUTO/X1 |
| **~73** | ND victory | lowercase **`victory`** + laurel/stars + **Repeat** + `Tap the screen.` (same stamp role as P0; prior uppercase-VICTORY OCR note retracted) |

## Cue map vs G2 slice

| GT (EN) | Our slice | Status |
|---|---|---|
| `IT'S SHOWTIME!!` + `SLIDE SKILL` + skill + `RANK 1 LV n/m` | `VfxShowtime` + `RANK 1 LV —/—` / HUD `开演` | **Engineering** (P0 t360 / Robin t15; LV math UNKNOWN; Robin t62 also `RANK 7 LV 10/10`) |
| `PERFECT!` + `DAMAGE 150%` + `N% TO FEVER` | `PERFECT!` + `DAMAGE 150%` + `N% TO FEVER` (gain, tip rates) | **Engineering** (Robin t55). Robin showed **13%** once — tip uses 40/30/15; ND may differ |
| Tip Perfect/Great/Good → Fever +40/+30/+15 | `BattleSim.QteFever` | **Engineering aligned** to P0 t445 tip (≠ `GL_FINAL_VERIFIED`) |
| Tip Fever window **14s** | `UnknownFeverWindowSec = 14` | **Engineering aligned** to P0 t435 (still not verified hit budget) |
| `FEVER n%` / mid **`n% TO FEVER`** / tip `FEVER TIME!!` | arc `FEVER n%` + toward `n% TO FEVER` + banner `狂热时间`/`FEVER TIME!!` | **Engineering** (Robin t56; P0 tip banner). Live Fever-active window still thin on P0 |
| `>> Xn SPEED` (1/2/3) | HUD `>> ×N 速`; pause chips ×1/×2/×3; cycle 1→2→3 | **Engineering** |
| `> FULL AUTO` / `|| PAUSE` / `>> Xn SPEED` | HUD `> FULL AUTO` / `|| 暂停` / `>> ×N SPEED` | **Engineering** (EN mode/speed; pause CN+`||`) |
| stage name + `PHASE n/m` | archMeta `名\n阶段 n/m` + `VfxPhaseBar` (max from Wave0/1) | **Engineering** (P0 t250/t510) |
| portrait `vampirism` | chip `vampirism` via lifesteal id | **Engineering** (P0 t510) |
| enemy skull under HP | field spark stub under HP bar | **Engineering** (P0 bloops role) |
| `mm:ss BATTLE TIME` | timer `mm:ss BATTLE TIME` | **Engineering** |
| `PHASE n/m` | arch + wave splash `PHASE n/m` | **Engineering** |
| `|| PAUSE` / `PAUSE` | HUD `|| PAUSE` / resume `CONTINUE` | **Engineering** |
| foe `COOL` + `SLIDE` | field tag flips; dual `COOL  SLIDE` when both | **Engineering** (Robin t48) |
| `WeakPoint` + dmg | field `WeakPoint` stamp | **Engineering** (Robin t68) |
| `SKILL HP TOTAL` / `PARTY HP TOTAL` | bottom party arc `N% SKILL HP TOTAL` | **Engineering** (same meter; mid-fight SKILL; order N% first) |
| tip `DRIVE GAUGE` / mid-fight `SKILL GAUGE` | bottom `N% SKILL GAUGE` (DriveGauge tip variant) | **Engineering** (t388/t510 live SKILL; tip art DRIVE) |
| tip `FEVER TIME!!` | overlay primary + CN 狂热时间 sub | **Engineering** |
| ally `Hero` + Lv + name | field ally nameplate `Hero  Lv N  Name` | **Engineering** (Robin t68; role stub) |
| `N% ENEMY HP LEFT` / `ENEMY HP TOTAL` | arch `N% ENEMY HP TOTAL` | **Engineering** |
| foe `Lv N Name` | field plate `Lv N  Name` | **Engineering** (P0 t70 Fresh Skull) |
| SHOWTIME `RANK 1 LV n / m` | stub `RANK 1 LV — / —` | **Engineering** (spaces; math UNKNOWN) |
| Robin `AUTO SKILL` / `AREA n/m` / `VICTORY` | inventory consts | **Inventory** (ND variants) |
| P0 ~t120 `>> SKIP` | inventory `BattleSkip` | **Inventory** (dialogue overlay) |
| `PHASE n` splash | wave stamp title `阶段 n` + sub `换波` | **Engineering** (P0 t510) |
| `N COOL TIME` / Hard SHOWTIME **`N LEAD TIME`** (FEVER~0) / **`N CLICK TIME`** (banked Fever) / Fever-active **`N FEVER TIME`** | portrait readyTag | **Engineering** (r28 COOL; r50 LEAD; r67 CLICK; Fever-active FEVER) |
| Hard near-Fever SHOWTIME **`WEAKPOINT` + `SKILL RESERVE`** | readyTag stacked | **Engineering** (r15) |
| Drive tip **`13% SKILL RATE`** | NamePop | **Engineering** (t388) |
| `WeakPoint` | `VfxWeakPoint` `弱点` | Role aligned (CN); Robin ~t68 stacks WeakPoint + number |
| `DEF ↑` | chip `DEF ↑` via `EffectKind.DefBuff` | **Engineering** (Robin mid-fight) |
| tip field `TOTAL n DAMAGE` | combo/DPS `TOTAL n DAMAGE` | **Engineering** (P0 t445) |
| P0 t65 PHASE splash | large `PHASE n` + stage above + heart crest | **Engineering** |
| Robin `Active Skill Addition` | `ActiveSkillAdditionEn` const | **Inventory** |
| `DRIVE SKILL READY` | portrait EN primary + CN verb; ready burst EN | **Engineering** (tip t388 @100%) |
| `DRIVE CRUSH` | crush `碾压` + EN `DRIVE CRUSH` | **Engineering** (EN substamp) |
| `victory` / `勝利` splash + `Tap the screen.` | `VfxStageClear` `勝利` + `victory` + EN tap hint; CLEAR waits | **Engineering** (P0 t390–392) |
| `CLEAR!!` + 3★ arc (right larger) + EXP/GOLD | `ResultBoard` `CLEAR!!` + arc stars + reward stubs + yellow `n LEVEL`/`EXP —/—`/`— EXP`/`— GOLD` | **Layout only**; star grade / EXP/GOLD math UNKNOWN |
| portrait under-name `60 MAX` / `Lv.N` | `_lvTag` via `PortraitLevelLine` | **Engineering** (P0 t60 / Robin tray `60 MAX`; below-cap `Lv.N`) |
| `|| PAUSE` field control | HUD `|| 暂停` | **Engineering** (Robin / ordinary) |
| tap-ready dashed/glow ring + TAP FULL role | readyDash glow + readyTag `点按已满` + `VfxTapReady` | **Engineering** (P0 tip t352/t358) |
| portrait under HP bar | `_hpBar` fill | **Engineering** (P0/Robin tray) |
| enemy element pip left of name | field `elPip` | **Engineering** (P0 bloops water/sun role) |
| Slide tip “slide it up” | gesture `OnSlide` + tip inventory | **Inventory** (P0 t358); isolated gesture timing UNKNOWN |
| tip `ENEMY HP REM.` / `LEFT` / `TOTAL` / Robin `REMAINING` | arch `N% 敌血余` | **Engineering** (variants noted; CN role) |
| `ENEMY DRIVE` under enemy HP | archDrive `N% 敌驱动` + 3 pips | **Engineering** (P0 t250; fill = max enemy Charge; not GL-final) |
| `SKILL GAUGE` / `DRIVE GAUGE` | bottom `N% SKILL GAUGE` (+ DriveGauge inventory) | **Engineering** (label variants) |
| `PAUSE` + red `Repeat` | PauseBoard `PAUSE`/`暂停` + `Repeat`→restart + CONTINUE under 继续; HUD `Repeat` | **Engineering** (Robin pause) |
| portrait `N DRIVE TIME` / `N COOL TIME` | readyTag single-line rich `N` + label | **Engineering** (P0 t365; Robin cool) |
| top crest over stage name | `UiSprites.Heart` crest | **Engineering** (P0/Robin heart-in-gear role) |
| Robin `Drive Skill Addition` | `DriveSkillAdditionEn` const | **Inventory** (ND; no CN settle) |
| foe green `SLIDE` charge under HP | field `_charge` + `SLIDE` tag | **Engineering** (Robin ~t48) |
| field `RES` | BuffFloat → `抵抗` | **Engineering** (Robin mid-fight; trigger still event-driven) |
| Robin `Live Skill Addition` | `LiveSkillAdditionEn` | **Inventory** (OCR/variant of Drive Skill Addition) |
| Heaven's Dust `PHASE 2/3` | slice max 2 via Wave0/1 | **Inventory** (GT 3-phase; sim still 2-wave) |
| portrait corner `SLIDE` | pip `SLIDE` | **Engineering** |
| `TEAM HP TOTAL` / `OWN` / `SKILL` / `CHILD HP TOTAL` | `N% SKILL HP TOTAL` (+ TEAM/PARTY inventory) | **Engineering** (tip t345; t60 TEAM+SKILL; t504 order N% first) |
| `Tap the screen.` under win stamp | `VfxStageClear` `Tap the screen.` | **Engineering** (P0 t391) |
| ND win stamp | same `勝利`+`victory`+tap (Robin t73 lowercase; uppercase note retracted) | Inventory |

## P0 tap/slide tutorial 1fps (`_frames_p0_tap330/`, t330–t365)

| t | Seen |
|---|---|
| 330 | Pre-battle party / companion tip — out of M1 combat |
| 340 | PHASE 1 splash + 3 bloops + Pixie/Mona |
| 345 | Tip: team **total HP** bar (lose only at 0%) |
| 348 | Idle field; no isolated tap VFX in this frame |
| 350–352 | Tip: Skill Gauge full → **tap or slide** |
| 355–358 | Slide tip; finger **slide up** on portrait |
| 360 | SHOWTIME Freeze Lance **RANK 1 LV 1/10** |
| 362 | Post-slide tip; yellow slime gone; ENEMY HP 33% |
| 365 | Finger on Mona; Pixie **6 COOL TIME** |

One Skill Gauge, two gestures. Isolated tap VFX still missing. Engine SlideCd stays split (G1 contract); not a formula close.

## P2

`IRDqNAhKKr4` file still **missing** in KIT ROOT (repo-root 768KB stub ≠ P2). 2026-09-12 afternoon: user Chrome watch page was `playability=OK` at native 1280×720 / 2756s. Not pulled from that session. Drop the file into KIT ROOT to ungate P2 inventory.

## P0 death window 1fps (`_frames_p0_death60/`, t60–t90)

| t | Seen |
|---|---|
| 64 | Wave-1 gone; ENEMY HP **0%**; heart splash; **no 击破** |
| 65 | Center **PHASE 2** + stage `1-1 Heaven's Dust`; party still up; FEVER 0% |
| 66 | Empty field then next wave HP **100%** |
| 67 | PHASE 2/3; Hanged spawn glow |
| 70 | Huge hit number + blast (no KO word) |
| 72–78 | VN + `SKIP` (Hades / Davi); not a death stamp |
| 83–89 | Cut-ins + `12,120` / `72,730`; still **no 击破** |

Wave-clear cue = PHASE splash, not 击破. Mid-kill = damage/cut-in only.

## Listing check (2026-09-12)

Frame dirs on disk: `_frames_p0_victory` 10, `_frames_p0_fever_live` 21, `_frames_p0_scan` 18, `_frames_p1_fever_banner` 16, `_frames_p1_robin*` + speed_seek. Extra stills:

| File | Seen |
|---|---|
| `t60.png` | 1-1 PHASE 1/3; tip; dmg `15,300`; FEVER 0%; ready arrows on portraits. No death stamp |
| `t90.png` | PHASE 2/3 Drive cut-in; dmg `72,730`; FEVER 0%. No death stamp |
| `robin_t73.png` | Same win splash (`victory` + Tap the screen) + Pause `Repeat` still up |

Robin clip never hits Fever 100% (stays ~40%) → no live `FEVER TIME!!` expected in P1.

## Next

1. Live Fever Time field banner now framed on P0 t440 (`FEVER TIME` + `N.NN`); tip still `FEVER TIME!!` — keep distinct
2. Death / KO text stamp still not observed on P0 dense 60–96; keep engineering `DOWN`/`KO`
3. Retry P2 when fresh interactive YT login exists
4. EXP/GOLD / RANK LV numeric formulas still UNKNOWN (CLEAR t472 band uses frame-locked tutorial stubs only)
5. Portrait inner costume title — frame-locked stubs only; no full EN costume table
6. Tip UI: Tap→**KeepAttacking(t60)**→Team→Childs→SkillReady→Slide→SlidePower→DriveSkill→DriveTiming→DriveTables→Fever(+TOTAL art)+gauge wired
7. Hard mid bottom `MY HP TOTAL` (r70); Hard SHOWTIME/QTE `PARTY HP TOTAL` (r50/r55); PHASE splash `SKILL HP TOTAL` (t065/t096) + bottom `DRIVE GAUGE`; wave0 ordinary `CHILD HP TOTAL` (t448); mid `SKILL HP TOTAL` / `CHILD` by level; top ENEMY HP TOTAL + ENEMY DRIVE / Hard PREPARATION
8. Mid drive meter: `SKILL GAUGE TOTAL` (t500); SHOWTIME/PHASE splash `DRIVE GAUGE`; short `SKILL GAUGE` inventory
9. `REMAINING TIME` / `FINAL TIME` / HARD_RAIL_TABS inventory only; Flanger/Hanged banners wired; portrait `WEAKPOINT` on Fever
10. SHOWTIME RANK from skill-slot content fields (not unit level); empty → `RANK — LV —/—`
11. DriveCrush: `DRIVE CRUSH` + `DRIVE SKILL` + skill name (r52 Silent Hunter / Sakuragawa); Boss intro Desire/WARNING/Dark Prince (t100); dual `+/SLIDE` pip (r58)
12. CLEAR t472: right-rail **Retry/Back** + account **GameplayHe**; tray costume Valkyrie **Black Widow** (t501); Robin Slide **In the Name of Justice** (r50)
13. Fidelity stays **0**; **no G3**; **no M1 claim**

## Continue-71..73 frame notes (2026-09-12)

| Frame | Seen / land |
|---|---|
| p0_t096 | PHASE splash: bottom **`DRIVE GAUGE`** + **`SKILL HP TOTAL`** |
| t280 | early PHASE splash: **`CHILD HP TOTAL`** |
| Hard r58 | stacked green **`+`** / red **`SLIDE`**; foe Hero Robin; MY HP TOTAL |
| t100 | Boss intro **`THE MASTER OF DESIRE` / `BOSS` / `WARNING` / `Dark Prince` / Loki** |
| p0_t060 | tip **`Keep using your skills and attack the enemy!`**; Old Skull; SKILL GAUGE+HP TOTAL |
| Hard r52 | Drive cut-in **`DRIVE CRUSH`** + **`DRIVE SKILL`** + **`Silent Hunter`** + Repeat |
| Hard r55 | PERFECT! / DAMAGE 150% / 13% TO FEVER / **`PARTY HP TOTAL`** / FEVER 40% |
| Hard r50 | SHOWTIME **`In the Name of Justice`** + `DRIVE GAUGE` + **`PARTY HP TOTAL`** |
| Hard r62 | SHOWTIME Pomona **`White Veil`** RANK 7 (primary Pomona Slide stub); top **`ENEMY DRIVE`**; bottom PARTY + DRIVE GAUGE |
| robin_t45 | SHOWTIME Pomona **`Saint's Blessings`** RANK 7 (inventory OCR/variant) |
| Hard r38 / cont38 | Tiamat SHOWTIME **`Snake Bite`** RANK 7; dual cool reads **`COOL TIME`** on re-scan — **`LOCK TIME` still unconfirmed** |
| Hard r22 / cont38 | Pomona Auto float **`Wedding Bells`**; Barrier; MY HP TOTAL + SKILL GAUGE TOTAL |
| Hard r48 / cont38 | Drive Ready → bottom **`DRIVE TOTAL`** + Sakuragawa `DRIVE SKILL READY` |
| Hard r12 / cont38 | Oracle Werewolf overhead **`REVEAL`** (inventory) |
| P0 t360 / cont39 | Pixie SHOWTIME **`Freeze Lance`** RANK 1 LV 1/10 |
| Hard r56 / cont40 | PERFECT + **`Skill Addition`** (Live/Active inventory) + Bleed + RES |
| Hard r67 / cont51 | SHOWTIME Snake Bite; cool **`N FEVER TIME`**; top **`DMG OF TOTAL`**; FEVER 40% |
| Hard r50 / cont56 | SHOWTIME `In the Name of Justice`; cool **`N LEAD TIME`** (FEVER 0%); FEVER 0% |
| Hard r67 / cont61 | SHOWTIME Snake Bite; cool **`N CLICK TIME`** (FEVER 40% banked; prior FEVER TIME note retracted for this window) |
| t388 / cont61 | Drive tip; field **`13% SKILL RATE`**; tip art 5× DRIVE SKILL READY |
| Hard r15 / cont55 | near-Fever SHOWTIME tray **`WEAKPOINT` + `SKILL RESERVE`**; SERVICE/ISOLATION inventory |
| t280 / t340 / cont55-56 | early Tutorial PHASE splash **`CHILD HP TOTAL`** |
| t065 / cont55 | Heaven's Dust PHASE splash **`SKILL HP TOTAL`** (cap party) |
| t250 / cont56 | Childs tip; bottom arc **`SKILL HP TOTAL`** + short **`SKILL GAUGE`** |
| t352 / cont58 | SkillReady tip; bottom arc **`SKILL UP TOTAL`** (live tip window) |
| t372 / cont58 | Stage2 PHASE splash after SkillReady: **`SKILL HP TOTAL`** + cool **`COOL TIME`** |
| t358 / cont58 | Slide tip; bottom **`SKILL HP TOTAL`**; Pixie Dark Water + hand |
| t442 / cont58 | Live Fever: field **`FEVER TIME`** + `10.88`; tip Activate; Pixie **`WEAKPOINT`** |
| Hard r14 / cont58 | mid **`ENEMY HP LEFT`** OCR; under-timer **`1.1 FEVER`**; ray **`SLIDE SKILL performance`**; Reveal (inventory) |
| t88 / cont57 | Fresh Skull overhead **`The Ranger`** (live); OCR **`ENEMY TEAM HP TOTAL`** inventory; death window still **no KO** |
| t391 / cont59 | win splash **`勝利`** + lowercase **`victory`** + **`Tap the screen.`** |
| t396 / cont59 | CLEAR!! + mid-star larger; Stage2 band **`2 LEVEL`/`6 EXP`/`73 GOLD`/`EXP 6/12`/`GameplayHe`** (inventory vs t472 live) |
| t470 / cont59 | LEVEL UP modal **`Level 2 ▶ 3`** + stamina refill/max+1 + Confirm |
| t472 / cont59 | CLEAR!! + **`LEVEL UP` chip** + BOSS/Retry/Back rail + **`EXP 0/18`** |
| t530 / cont59 | CLEAR!! Stage4; **right star** larger/glow (vs mid-star t396) |
| Hard r36 / cont60 | OCR **`NEXT 0.0s TOTAL`** under arch (inventory); cool **`COOL TIME`** |
| Hard r21 / cont60 | SHOWTIME Pomona **`Saint's Blessings`**; OCR **`FINAL 1/1`** (inventory; live PHASE) |
| Hard r38 / cont60 | SHOWTIME **`Snake Bite`** RANK 7; cool digits — **LOCK TIME still unconfirmed** |
| Hard r73 / cont51 | ND win **`VICTORY`** + Tap the screen. + Repeat (already VfxStageClear ndWin) |
| Hard r36 / cont41 | Robin tray **`AUTO SKILL`**; OCR inventory **`DARKNESS TOTAL`** |
| Hard r49 / cont41 | Sakuragawa badge **`Royal Hellboard`** (Slide stub) |
| P0 t365 / cont42 | Drive tip cool portraits **`SKILL TIME`** (Pixie 7) |
| P0 t500 / t456 | Mona Auto cast **`Eclipse`** / `Lv.1 Eclipse` |
| Hard r51 / cont43 | DriveCrush + PRESS BUTTON + OCR **`SilentHunter`** |
| Hard r72 / cont43 | stage exit **`NOW LOADING...`** (inventory) |
| P0 t74 / cont45 | Hades VN **`Death Patch`** forehead; still **no KO** stamp |
| P0 t78 / cont45 | Davi cut-in **`Okey-do.`**; The Hanged / Fresh Skull |
| Hard r70 | mid **`MY HP TOTAL`** + PREPARATION + Slide Skill Boost |
| t472 | CLEAR!! **Retry/Back** rail + **GameplayHe** yellow band |
| t501 | tray **`Lv.1 Black Widow`** / Valkyrie; **`CHILD HP TOTAL`** + **`SKILL GAUGE TOTAL`** |
| t530 | CLEAR!! + Stage 4 Tutorial 4 + 3★ arc (ResultBoard stage head already) |
