# M1 HARD BLOCKED — frame GT / formulas still open

Date: 2026-09-12 (deferred supplement signed)  
Status: **M1 not accepted. G2 not passed.** Primary mp4 **partially landed**; fidelity still **0**.

**用户 2026-09-12 签字：** T27 / T28 / T14–T20 / P0 高清 / P2 / 投放升格 / 模拟器 / 公开搜 **暂时待定、后面补**。延期 ≠ 删除。补件：`../G1/DEFERRED_SUPPLEMENT.md`。当前只走路 A。GT 搜 **暂停**。

## Done (not acceptance)

| Item | Evidence |
|---|---|
| G0 audit + G1 contract freeze | `STATUS.md` `PASSED_WITH_GT_OPEN` / `PASSED_CONTRACT_FROZEN` |
| Battle core slice + opcode executor | `BattleSim` + `dotnet test` green (fixture self-consistency) |
| PlayMode vertical-slice smoke | `PASSED_SLICE_SMOKE` (`editor-playmode-20260911e`) — **our slice, not GT** |
| Multi-channel GT search | `gt_search/01`–`10` + `WATCH.md` (all collectors idle) |
| Bilibili short-clip fetch + classify | `gt_search/11`–`14`, `13_bili_classify.md`, round-2 below |
| Contrast / reject local mp4 | under `docs/reference/gl-shutdown-pve/contrast/` and `rejected_not_primary/` |
| Supplementary Ragna cues | `supplementary/ragna_gl/` ≠ primary |

2026-09-12 G2 continue-16: Drive plate removed (P0 tap-icon). Repo-root EternalVow mp4 is 768KB stub, not P2. Still **≠** T27/T28/M1; fidelity **0**.

2026-09-12 G2 continue-15: win smoke `vs-smoke-timing-20260912h` PASS + `06j_auto_hit` / `06f_kill` (1080×1920). Mid-kill has damage numbers, no 击破. Still **≠** T27/T28/M1; fidelity **0**.

2026-09-12 G2 continue-14: portrait 1080×1920 smoke + hit-chain 0.000 + GT hstack `strip_20260912g/overlay`. Still **≠** T27/T28/M1; fidelity **0**.

## Remaining (blocks M1)

1. ~~Primary GT mp4 in ROOT~~ → **PARTIAL**: handoff P0+P1 in `DC_RECON_KIT/docs/reference/gl-shutdown-pve/` (`FETCH_LOG.txt`). P2 Eternal Vow re-capture in progress. Capture = MediaRecorder (vp9), not original yt-dlp bytes; window was narrow (~288×640) — may re-capture landscape for HUD frames.
2. Frame-by-frame Drive / QTE / Fever / hit / death / wave / result against that GT (**inventory started** in `gt_search/19_primary_handoff_frames.md`; fidelity stays 0).
3. GL numeric formulas: tip-aligned QTE Fever% + 14s window only; damage / hit budget still `UNKNOWN`; no `GL_FINAL_VERIFIED`.
4. EditMode UTF (0 in project); T26–T32 visual fidelity still `BLOCKED`/`NOT_RUN`.

## Round-2 fetch (2026-09-11 evening) — still 0 primary

| BV | Verdict | Why |
|---|---|---|
| `BV11q4y1z7xH` | **contrast_only** → `contrast/gl_tryout_2021_BV11q4y1z7xH.mp4` | Has 5p battle HUD (FEVER / SLIDE READY / AUTO / ×3) but **2021** + **Chinese UI**, not shutdown-window English primary |
| `BV1f54y1W76w` | REJECT | Stamina VLOG; menus / gacha only |
| `BV1614y1z75t` | REJECT | EOS essay / KR lobby splice; no usable ordinary PVE fight |
| `BV1mh4y117CW` | REJECT | EOS memorial; KR menus only |
| `BV16v4y1i7vm` | REJECT | Puppet system slides; no battle HUD |

YouTube-family remote fetch remains **TLS BLOCKED** on this machine (do not re-run yt-dlp against YouTube). Bilibili path works; inventory still has no primary.

## Exact next step (human)

Put **one** `.mp4` of **international-server / GL, ordinary 5-person PVE** (story / daily / narrative normal), ideally English UI, from the late pre-shutdown window, into:

```text
docs/reference/gl-shutdown-pve/
```

Requirements and blacklist: `LOCAL_PVE_MISSING.md`.  
After the file lands, say so — then frame GT and resume G2 acceptance work. **Do not** promote contrast / Ragna / Raid / WB / memorial / post-EOS private clients to primary.

## G2 engineering while blocked (not acceptance)

2026-09-12 G2 continue-106/107: Hard SHOWTIME cool `LEAD TIME` (r50 FEVER~0) / `CLICK TIME` (r67 banked Fever); Drive tip `13% SKILL RATE` (t388); LEVEL UP `Level n ▶ m` (t470); Stage2 CLEAR / FINAL/NEXT inventory; LOCK TIME still unconfirmed; `_frames_g2_cont59`–`61`. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-103/105: Hard SHOWTIME `N LEAD TIME` (r50); near-Fever tray `WEAKPOINT`+`SKILL RESERVE` (r15); PHASE splash `CHILD` pre-SkillReady (t280/t340) → `SKILL` (t372/t065) → `TEAM+(CURRENT)` (t510); tip arcs Childs=`SKILL HP TOTAL` / SkillReady=`SKILL UP TOTAL` (t250/t352); Fresh Skull `The Ranger` (t88); death cont57 still no KO; `_frames_g2_cont55`–`58`. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-102: Total HP tip `TOTAL HP TOTAL` (t345); SHOWTIME `ENEMY HP LEFT` (t360); Drive tip `SKILL GAUGE` + `13% Skill Rate` (t365/t388); `_frames_g2_cont54`. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-101: PHASE splash `ENEMY/TEAM HP TOTAL` + `(CURRENT)` (t510); `_frames_g2_cont53`. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-100: Pomona Slide `White Veil` (Hard t62.5); cap `SKILL UP TOTAL` 0% label; LOCK TIME still unconfirmed on dense re-scan; death t64 no KO; `_frames_g2_cont52`. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-99: Hard Fever cool `N FEVER TIME` (r67); top `DMG OF TOTAL`; Sytry plate `Lovey-Dovey`; `_frames_g2_cont51`. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-98: LEVEL UP `n > m` (t470); Fever tip `FEVER TIME!!` + `TOTAL 427 DAMAGE`; fever-tip `SKILL HP TOTAL`; PERFECT `Skill Addition`; Mona wipe inventory; `_frames_g2_cont50`. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-97: Cap-story tip stack `MY HP TOTAL`/`PARTY HP TOTAL` (t80); Hard QTE `ENEMY HP` short; death still no KO; `_frames_g2_cont49`. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-96: Hard QTE top `ENEMY HP` short (r55); `QteDamageWord`; Ranger/SKILL UP inventory only; death still no KO; `_frames_g2_cont49`. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-95: Auto cast `Auto`+skill / fallback `AUTO SKILL`; Sytry `Lovey-Dovey`; `_frames_g2_cont48`. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-94: TipSpeed body (P0 t451); TipSlide Mona→TIP! TipSlidePower queue; Pixie Auto `Dark Water` (t452); Hades VN line inventory; `_frames_g2_cont47`. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-93: Hades `Death Patch` + Davi `Okey-do.` inventory (t74/t78); death window still no KO stamp; `_frames_g2_cont45`. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-92: Mona Auto `Eclipse` (t500/t456); `SilentHunter` + `NOW LOADING...` inventory; `_frames_g2_cont43/44`. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-91: Drive-tip cool portraits `SKILL TIME` (P0 t365); SHOWTIME same (r67); mid stays `COOL TIME`. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-90: PERFECT `Live Skill Addition` (r56); Auto portrait `AUTO SKILL` (r36); Sakuragawa Slide `Royal Hellboard` (r49); `DARKNESS TOTAL` inventory; `_frames_g2_cont41`. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-89: SHOWTIME cool portraits `SKILL TIME` (r67); `SLIDE TIME` inventory; `Tap the screen...` inventory; `_frames_g2_cont40`. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-88: Pixie Slide `Freeze Lance` (P0 t360); frames `_frames_g2_cont39`. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-87: Drive Ready field `DRIVE TOTAL` (r48); `REVEAL` inventory (r12). Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-86: Pomona Auto `Wedding Bells` (r22); `LOCK TIME` inventory (r38); Tiamat `Snake Bite` confirm (r38); frames `_frames_g2_cont38`. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-85: Pomona Slide `Saint's Blessings` (robin_t45); `White Veil` inventory (r62); Hard SHOWTIME top `ENEMY DRIVE` vs mid `PREPARATION`. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-84: CLEAR `Retry`/`Back` + `GameplayHe` + `Black Widow`; Sakuragawa `Silent Hunter`; Robin Slide `In the Name of Justice`; Hard SHOWTIME/QTE `PARTY HP TOTAL`. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-83: CLEAR rail + costume consts wired (same frame locks as continue-84 close-out). Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-82: Childs-tip `CURRENT HP` (t250); TipChilds full stem. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-81: `SHARED HP TOTAL` (t345); `DRIVE TOTAL` tip window (t388); Hard `PREPARATION` + `EVA` (r61); CLEAR star arc right-large (t395). Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-80: Drive-tip `PARTY HP TOTAL` (t380); Mona Drive `Filled with Love` / Tiamat Slide `Snake Bite` stubs. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-79: PHASE splash CHILD when party below cap (t446); post-Fever low-level `MY HP TOTAL` (t456); Mona Tips slide stem (t355); LEVEL UP `Level 2 ▶ 3` (t470). Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-78: Fever portraits WEAKPOINT over Drive Ready (t442/t452). Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-77: `CHILD HP TOTAL` when save party below cap (t512) or wave0; PHASE splash stays `SKILL`. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-76: PlayMode smoke `20260912m` PASS (Result→Home). RANK stub `—`. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-75: CLEAR `Total 2` stub (t398). Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-74: PHASE splash party label back to `SKILL HP TOTAL` (death60 t65); t280 CHILD-on-PHASE inventory. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-73: DriveCrush `DRIVE SKILL` badge + skill name (r52); CN 碾压 fallback. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-72: TipKeepAttacking (t60); early PHASE splash `CHILD HP TOTAL` (t280). Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-71: PHASE splash `DRIVE GAUGE` (t096); ordinary splash keeps `SKILL HP TOTAL`; dual `+/SLIDE` pip (r58); Boss intro Desire/WARNING/Dark Prince (t100). Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-70: Tiamat Primordial Serpent costume; SKILL GAUGE TOTAL; HERE IS A POINT; Drive Tables tip. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-69: `SKILL GAUGE TOTAL` (t500); QTE `HERE IS A POINT!!`; Drive tip Tables chained. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-70: SHOWTIME RANK from skill slots (not unit level); drop TAP READY word; Result smoke unstick. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-68: TipTap→…→TipDriveSkill→Timing; TipDriveTables inventory. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-67: TipTapSkill first in tip queue (t58); Dracula Werewolf plate; SHOWTIME RANK. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-66: Dracula Werewolf no forced Oracle title (r50); TipDriveTiming chain; SHOWTIME RANK. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-65: TipDriveTiming after TipDriveTables; SHOWTIME RANK by level; CHILD/WEAKPOINT. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-64: SHOWTIME RANK 1 LV 10/10|1/10 by unit level (r50); CHILD HP; WEAKPOINT; TipSlidePower. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-63: wave0 `CHILD HP TOTAL` (t448); portrait `WEAKPOINT` (t452); TipSlidePower. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-62: TipDriveTables on first Drive ready; Fever gauge tip; Hard Stage 8 arch. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-61: Fever gauge tip at ≥40%; Hard Stage 8 arch; tip/CLEAR/Skill Boost. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-60: Hard Stage 8 arch title frame-lock (r30/r70); Green Victorix; tip/CLEAR stack. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-59: Green Victorix foe title (r30); Hard `|| PAUSE`; tip/CLEAR/Skill Boost stack. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-58: Hard field `|| PAUSE` (r30/r70); II PAUSE inventory; HardRailTabs note. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-57: Skill Boost float map; Hard field Repeat; tip queue + CLEAR rail/band. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-56: Hard field Repeat (r70); BATTLE NO inventory-only; Slide/Tap/Drive Skill Boost chips. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-55: CLEAR right-rail BOSS/RETRY/HOME stubs (t472); Hard `BATTLE NO.` + QTE Repeat. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-54: Hard `BATTLE NO. 1` clock + field Repeat on QTE (r55); tip queue + CLEAR stubs. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-53: TipSkillReady on first tap; tip queue Team→Childs→Skill→Slide/Drive→Fever. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-52: tip queue Team HP → Childs tray; Slide/Drive/Fever tips; CLEAR stubs. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-51: Tip Team HP (t345) + TipSlide/Drive once; tip TOTAL art; CLEAR stubs. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-50: TipSlide/Drive once-per-battle plates; tip TOTAL art; CLEAR stubs; portrait `+`. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-49: tip rates art `TOTAL 427 DAMAGE` (t444) + ChildNote; CLEAR stubs + portrait `+`. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-48: CLEAR band t472 stubs `3/6/70` + `EXP 0/18` + LEVEL UP chip; portrait ready `+`; ResultBoss inventory. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-47: Fever tip rates chain (t445); PHASE splash `MY HP TOTAL` (t446); TEAM inventory. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-46: Fever tip plate `TIP!`/`Activate Fever Time!` (t442); portrait `WEAK POINT`; DPS live `n DAMAGE` (no TOTAL); Own HP / tip TOTAL inventory. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-45: SHOWTIME `SLIDE TIME` cool; CRT Rate ↑ chip; Fever combo `n DAMAGE`; Weak Point portrait inventory; `_frames_p0_cont37`. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-44: live Fever `FEVER TIME` + `N.Ns` (t440); CLEAR Silent Pixie/Mona; REMAINING TIME / Flanger / FINAL TIME / Primordial Serpent inventory; `_frames_p0_cont36`. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-43: Tap no Slash ribbon; LEVEL UP `n > m`; .../Log; PHASE TEAM HP TOTAL; PRESS BUTTON QTE; Drive timing tip; Pause single PAUSE; `_frames_p0_cont35`. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-42: Fresh/Old/Seething foe title stack (t70/t83); EnemyHp LEFT/REM inventory. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-41: cool→COOL TIME; Hard II PAUSE + bottom MY HP TOTAL + (Hard) arch; hide ESCAPE; ND foe Title stack; tip inventory; Inspect EN; cont33/p1-cont17 frames. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-40: VN Log; Hard boss arch name; SKILL TIME cool; foe Hero Robin. Still **≠** M1; fidelity **0**.


2026-09-12 G2 continue-39: ND VICTORY splash; The Hanged foe banner; CHILD HP/SKILL TIME/Mona Tips inventory. Still **≠** M1; fidelity **0**.


2026-09-12 G2 continue-38: SKIP chrome stub (t120); tray MAX Name; ND foe titles; SHOWTIME DRIVE GAUGE. Still **≠** M1; fidelity **0**.


2026-09-12 G2 continue-37: tray 60 MAX Name; ND foe titles; SHOWTIME DRIVE GAUGE; fever tip inventory. Still **≠** M1; fidelity **0**.


2026-09-12 G2 continue-36: portrait costume titles (frame-locked names); bloop Small dedupe. Still **≠** M1; fidelity **0**.


2026-09-12 G2 continue-35: Weak Point DEF ↓; Barrier VFX; EN buff floats kept; LEVEL UP stamina split; TIP! inventory. Still **≠** M1; fidelity **0**.


2026-09-12 G2 continue-34: stacked BATTLE TIME; bloop Small plate. Still **≠** M1; fidelity **0**.


2026-09-12 G2 continue-33: stacked PctOverLabel meters (t250/t508). Still **≠** M1; fidelity **0**.


2026-09-12 G2 continue-32: CLEAR mid-star; reward E top-right; costume title inventory. Still **≠** M1; fidelity **0**.


2026-09-12 G2 continue-31: tray `60 Name` + MAX badge; foe `Lv. N`. Still **≠** M1; fidelity **0**.


2026-09-12 G2 continue-30: Recovery chip/VFX; Hard MY HP TOTAL; ally Hero/N Name; DriveReady EN-only; FEVER TIME!! display. Still **≠** M1; fidelity **0**.


2026-09-11 `M1-G2-UNSTACK-DRIVE-SELECT`: Drive 选择板打开时同步清 `击破`/`倒下`（`WavePreview.SuppressDeathCues`）。PlayMode smoke `editor-playmode-20260911f` **PASS**；`06d` 目视无同帧「击破」。仍 **≠** M1 验收。

2026-09-11 night: round-3 Bilibili classify (`gt_search/15_bili_classify.md`) — primary still 0. `M1TargetingTests.FocusClearsWhenEnemyDiesThenNextTapPicksAliveFoe` green (focus clear only; `retarget` opcode still UNKNOWN). Drive select plate taller / title larger (engineering). Contrast tryout cues: `gt_search/CONTRAST_TRYOUT_CUES.md`.

2026-09-11 late-night: round-5 (`gt_search/17_bili_classify.md`) — contrast newbie clear `BV14S4y1b7RR` (2022-05 CN UI, SHOWTIME/FEVER/SLIDE); offline/ash/defense-war reject. Smoke: Result→Home return phase (`11_home_return`). **Primary still 0.**

2026-09-11 late: story anthology P3–P6 REJECT (VN only). Round-4/5 (`gt_search/16_bili_classify.md`): post-EOS / bot / equip reject; added contrast 5p HUD clips (`BV1xt41187U4` AUTO+FEVER 2019 CN UI; tryout 1-1; JP change/enhance). Still **not** primary. `MultiHitRetargetsAfterFocusedEnemyDiesMidSkill` green. Drive title 52px. `dotnet test` **152**. Bilibili search API 412; Playwright + direct BV yt-dlp OK. **Primary still 0.**

2026-09-12 G2 continue-29: foe `Lv N Name`; RANK LV spaces; AUTO SKILL/AREA/VICTORY inventory; Settings EN. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-28: ResultTitle CLEAR!!/DEFEAT; Wave Enemies/BOSS; CLEAR 2★/3★; boss CORE; SKIP inventory. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-27: `N% SKILL HP TOTAL` word order; TEAM/PARTY inventory; LEADER/SUMMON/UNCAP/REST EN. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-26: damage elem F/W/G/L/D; loot GOT; Bond tag. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-25: foe `COOL  SLIDE` dual tag; BLIND color; cont20/21 frames. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-24: BLIND chip; MY HP TOTAL inventory (Robin ND); scorecard EN rows. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-23: mid-fight `SKILL GAUGE`; tip `DRIVE GAUGE` variant; `vampirism` chip; `N COOL TIME` single-line; Sleep `Z`. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-22: TOTAL n DAMAGE field order (P0 t445); cont18 frames. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-21: LEVEL UP stamina EN (P0 t470); Confirm EN-only; ally `Hero  Lv N`; CLEAR LEVEL/EXP/GOLD split. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-20: DRIVE SKILL READY primary; status VFX Stun/Silence/Taunt/… EN; OnBuff EN match. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-19: CRIT punch/popup; Heal/Regen tags; FxWord Barrier/Stun/etc EN; CONTROL_CHIP cue EN. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-18: GOOD button; DEFEAT/LOOT/RESULT; Boss BOSS/WAVE; buff floats Regen/Vampirism; Total HP tip note. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-17: DRIVE SELECT/READY?/WARNING!!/TAP READY/SLIDE READY/FIGHT!; Pause+Result CTAs EN. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-16: COMBO/TOTAL DAMAGE; PHASE splash + heart crest; WAVE START; DOWN/KO eng; Active Skill Addition inventory. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-15: `WeakPoint`/`SKILL HP TOTAL`/`DRIVE GAUGE`/`FEVER TIME!!` primary; ally `Hero` plate; `DRIVE CRUSH` EN; RES/CRIT floats. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-14: `PHASE`/`BATTLE TIME`/`|| PAUSE`/`ESCAPE`; EN chips DEF↑/Barrier; foe COOL↔SLIDE; `PARTY HP TOTAL`; QTE `PERFECT!`/`DAMAGE`. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-13: CLEAR/LEVEL UP closer to P0; primary EN HUD (`SPEED`/`FULL AUTO`/`SKILL GAUGE`/`FEVER`/`TO FEVER`/`ENEMY HP LEFT`/`DRIVE TIME`/`COOL TIME`). Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-12: CLEAR→`LEVEL UP!` modal stub (`Level — → —` / stamina / 确认); still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-11: foe SLIDE charge; RES→抵抗 BuffFloat; LiveSkillAddition inventory. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-10: cool/drive two-line portrait tags; top crest stub; PHASE 2/3 inventory. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-9: top `阶段 n/m` from Wave0/1; chip `吸血` for lifesteal. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-8: field `|| 暂停`; tap-ready `点按已满` + glow ring; portrait HP fill. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-7: portrait `N MAX`/`Lv.N`; CLEAR yellow band; top `ENEMY DRIVE` (`敌驱动`). Still **≠** M1; fidelity **0**.

2026-09-12 overnight: handoff YT **P0+P1 landed** in `DC_RECON_KIT/docs/reference/gl-shutdown-pve/` via CDP+cookies+MediaRecorder (`FETCH_LOG.txt`). P2 re-capture pending bot-gate. **Do not** claim M1; fidelity still 0 until frame GT.

2026-09-12 G2 continue: primary frame seek — Robin `X3 SPEED`/`FULL AUTO`/`PAUSE`/`PERFECT!`/`FEVER 40%`; P0 tip Fever **+40/+30/+15** and **14s** window; ordinary `victory` plate. Engineering: speed cycle 1–3; `QteFever` + `UnknownFeverWindowSec=14`. `dotnet test` **154**. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-6: `DriveQteTimeoutSec=7` (P0 DRIVE TIME); SLIDE pip; 队员 HP; Robin ND VICTORY noted. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-5: Pause `PAUSE`+`Repeat` restart; HUD Repeat; QTE portrait `驱动 n` (DRIVE TIME). Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-4: Fever toward `n% →狂热` (Robin TO FEVER); CLEAR EXP/GOLD chrome; EnsureSpeed2 fix; T_MATRIX gate refresh. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-3: HUD chrome `>> ×N` / `> 全自动` / `战斗` / `敌血`; Fever substamp `FEVER TIME-!!`; wave title `阶段 n`. P0 no live Fever banner. Still **≠** M1; fidelity **0**.

2026-09-12 G2 continue-2: QTE stamp tip **gain** `N% →狂热`; portrait `冷却 N`; win splash `勝利`+`victory`; result `CLEAR!!`+3★ decor. Still **≠** M1; fidelity **0**.

2026-09-11 ~23:50 round-6 (`gt_search/18_bili_classify.md`): 麻麻 VLOG#45/#46/#48 (2023-01-24) = menu/guide slides only → `rejected_not_primary/`. ND trailer shorts (~15s, 2020–2021) reject. In-window LaoTie/琉璃 hits = Raid/星云 only. Added contrast Hell Metro `BV12s4y1r7j7` (5p HUD+ESCAPE, special mode). Handoff YT P0–P2 was TLS BLOCKED at the time. G2: `DriveReadyPortrait` → `VfxDriveReady`; smoke `06i_pause.png`. `dotnet test` **152**.
