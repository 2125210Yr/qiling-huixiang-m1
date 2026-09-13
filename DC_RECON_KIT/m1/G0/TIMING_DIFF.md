# TIMING_DIFF — draft, not T28

Date: 2026-09-12  
Status: **DRAFT**. GT overlay holds + one Editor smoke probe. This file does **not** pass T28.

T28 (`04_ACCEPTANCE_AND_TESTS.md`): input / hit / numbers / sfx / Drive / Fever, same setup, error ≤ 2 reference frames. Low-fps / dropped-frame samples must report uncertainty.

## Why this is not T28

| Required | This draft |
|---|---|
| Our slice recorded at the same setup | Smoke probe log only (`CueTimingProbe` unscaled on→off). **Not** a 30fps video strip. |
| Input → hit → number chain | **Not measured.** Overlay hold only. |
| ≤ 2 frames vs our strip | Cannot compute. |
| ≥720p original GT | P0/P1 are MediaRecorder **296×640** vp9 remux. `r_frame_rate` container lie `16000/1`; `avg_frame_rate` ≈ 30.00 (17524 frames / 584.119s). |
| Sampling | Tight windows extracted at **30 fps**. Fade edges ±1–2 source frames. MediaRecorder may drop frames. Uncertainty **≥ T28 budget** even before comparing to us. |
| P1 QTE | ND Robin (special mode), not ordinary 5p PVE. |

Contract fidelity stays **0**. Do not write `GL_FINAL_VERIFIED`.

## Probe

```
ffprobe aSbBuFD12HY_GL_EN_Gameplay_Part1_202308.mp4
codec=vp9 width=296 height=640
r_frame_rate=16000/1
avg_frame_rate=17524000/584119
duration=584.119 nb_frames=17524
```

Frames: `_frames_timing/` (10 fps scout) and `_frames_timing30/` (30 fps bounds) under `DC_RECON_KIT/docs/reference/gl-shutdown-pve/`.

Clock: `t = ss + (n-1)/fps`.

## Overlay hold (GT wall-clock)

### 1. P0 SHOWTIME — `IT'S SHOWTIME!!`

Window `ss=358.95` @ 30 fps.

| Bound | File | t | Note |
|---|---|---|---|
| last pre | `showtime/f_003` | 359.017 | tip still up |
| first-on | `showtime/f_004` | 359.050 | orange wipe + ghost title |
| readable | `showtime/f_006` | 359.117 | title + `8 COOL TIME` already on Pixie |
| last full | `showtime/f_044` | 360.383 | title + `SLIDE SKILL` + RANK |
| remnant | `showtime/f_047` | 360.483 | fade |
| first-off | `showtime/f_049` | 360.550 | field only |

**Measured hold (first-on → first-off): 1.50s ± 2 frames.** Readable→remnant ≈ 1.37s.

P0 HUD `BATTLE TIME` stays **04:56** across the overlay (presentation hold). Manual-path `HoldSim` already exists; this draft does not claim Full Auto also freezes.

Was `VfxShowtime.Duration = 1.20`. Now **1.47** (engineering, not T28). Manual cut-hold uses the same constant.

### 2. P0 PHASE 2 splash

Window `ss=64.15` @ 30 fps.

| Bound | File | t | Note |
|---|---|---|---|
| last pre | `phase/f_005` | 64.283 | `76,520` / `13,500` only; **no 击破** |
| first-on | `phase/f_007` | 64.350 | heart + faint `PHASE 2` |
| last faded | `phase/f_065` | 66.283 | stage name + `PHASE 2` still there |
| first-off | `phase/f_068` | 66.383 | empty field, new wave incoming |

**Measured hold: 2.03s ± 2 frames.** HUD `PHASE 1/3` during splash; `PHASE 2/3` after spawn. `BATTLE TIME` stays **04:49**.

`WaveCueBoard` **WaveAdvance** life was 1.20, now **2.00**. AllyDown stays 1.20 (unmeasured).

### 3. P0 victory splash → CLEAR

Window `ss=389.5` @ 10 fps (tap is user-timed; 30 fps not required).

| Bound | File | t | Note |
|---|---|---|---|
| empty board | `victory/f_001` | 389.5 | |
| first ghost | `victory/f_005` | 389.9 | wreath + `Tap the screen.` |
| `勝利` readable | `victory/f_012` | 390.6 | + `victory` |
| still splash | `victory/f_035` | 392.9 | |
| CLEAR | `victory/f_040` | 393.4 | `CLEAR!!` + stars + `NOW LOADING` |

Splash hold ≈ **3.0s until tap**. Not a fixed duration. `VfxStageClear` intro **1.35** + tap hold + **8s** fallback unchanged.

Tutorial CLEAR band `6 EXP` / `73 GOLD` is **not** a formula.

### 4. P1 Robin QTE — ND only

Window `ss=53.50` @ 30 fps. Mode: `Stage 8 You Won't Get Away! (Hard)` + `> FULL AUTO`.

| Bound | File | t | Note |
|---|---|---|---|
| pre | `robin_qte/f_012` | 53.867 | no PERFECT |
| 0% TO FEVER | `robin_qte/f_016` | 54.000 | `150%` + count start |
| PERFECT on | `robin_qte/f_018` | 54.067 | |
| mid-tween | `robin_qte/f_040` | 54.800 | **13% TO FEVER** (not a second formula) |
| settle | `robin_qte/f_055` | 55.300 | **40% TO FEVER**; bottom `FEVER 40%` |
| PERFECT last | `robin_qte/f_075` | 55.967 | |
| PERFECT off | `robin_qte/f_080` | 56.133 | `40% TO FEVER` remains |

**PERFECT hold ≈ 1.90s.** `TO FEVER` count 0→40 over **1.30s** (ease-in matches the 13% mid sample). Plate outlives PERFECT (still on at window end 56.40). Ordinary PVE QTE unseen.

Was `LifePerfect = 1.28`. Now **1.90** + 0→N count-up. Great/Good unmeasured.

Robin `DAMAGE 150%` / +40 Fever stay **tip/ND identity**, not ordinary numeric baseline.

## Our slice column (Editor VS Smoke 2026-09-12b)

Source: `BASELINE_LOGS/vs-smoke-timing-20260912b.txt`. Isolated save. Editor quit after PASS. Probe = unscaled on→off. **Not a 30fps video strip. Not T28.**

| Cue | GT hold | Our probe | Δ | Note |
|---|---|---|---|---|
| SHOWTIME | 1.50s ±2f | **1.473s** (+ three `0.000`) | 0.027s ≈ 0.8f | First is natural `Duration=1.47`. Zeros = leftover `KillAll` |
| PHASE 2 | 2.03s ±2f | **2.009s** | 0.021s ≈ 0.6f | After smoke waited for Hide |
| QTE object | PERFECT 1.90s; plate ≥2.40s | **2.502 / 2.501 / 1.253** | plate ≈ FeverPlateLife 2.50 | Third cut by Fever→wave. Stamp fade is 1.90 inside the 2.50 object |
| Victory intro | tap (~3s user) | **1.434s** | n/a | Measured `VfxStageClear.Duration` then `SkipLive`. Not tap-hold |

Deltas are **probe vs GT overlay hold**, not input→hit, not stacked video. T28 = **BLOCKED**.

## Our slice 30fps strip (2026-09-12f) — still not T28

Smoke `vs-smoke-timing-20260912f.txt`. Frames: `docs/reference/gl-shutdown-pve/our_slice/strip_20260912f/<cue>/` + `INDEX.txt` + playback `*.mp4` (30fps sequential, **not** jitter-corrected).

| Cue | frames | hold | measured fps | first gap | Notes |
|---|---|---|---|---|---|
| showtime | 43 | 1.485s | 28.95 | f1→f2 **211ms** | Identity `IT'S SHOWTIME!!`. Early jitter |
| qte | 75 | 2.501s | 29.99 | f2→f3 156ms | First QTE only; ND-style judge |
| phase | 60 | 2.009s | 29.86 | f2→f3 165ms | HUD previous PHASE; standees hidden |
| victory | 45 | 1.488s | 30.23 | f19→f20 **138ms** | `勝利` / `Tap the screen.` |

Mean fps ≈ 30, but several **>2 frame** gaps. Game-view JPG 360px. **No GT overlay. No input→hit→number.** T28 stays **BLOCKED**.

## Portrait + hit chain (2026-09-12g)

Smoke `vs-smoke-timing-20260912g.txt`. Game view **1080×1920** (log `portrait index=7`). Strip JPG 360×640. GT remux 296×640.

| Our | GT pair | Overlay |
|---|---|---|
| showtime f_001 | `_frames_timing30/showtime/f_004` first-on | `overlay/showtime_hstack.png` |
| showtime f_022 | `f_044` last full | `overlay/showtime_mid_hstack.png` |
| phase f_001 | `phase/f_007` first-on | `overlay/phase_hstack.png` |
| victory f_012 | `_frames_timing/victory/f_012` | `overlay/victory_hstack.png` |

Hstack is **identity only**. Field art / wreath / 3-person tutorial vs 5-person slice. T27 1% **fails by inspection**.

| Chain | Our | GT |
|---|---|---|
| tap in→num | **0.000s** (same frame) | UNKNOWN — isolated tap not in P0 30fps |
| slide in→num | **0.000s** (same frame) | UNKNOWN — SHOWTIME covers the hit |

T28 still **BLOCKED**: not same setup (tutorial 3p vs slice 5p; no original pixels), no sfx, no Drive/Fever event overlay, jitter remains.

### Lose + semi (2026-09-12c)

`BASELINE_LOGS/vs-smoke-lose-20260912.txt`. `result=DEFEAT` `semi=True` `outcome=Defeat`. Isolated save.

| Cue | Our probe | Note |
|---|---|---|
| DEFEAT splash | **1.353s** | Matches `VfxStageClear.Duration` 1.35 then SkipLive to RESULT board |

No GT ordinary-PVE defeat splash timing. Identity only (`DEFEAT` board). Semi-auto is engineering; GT still only shows FULL AUTO.

## Observed, not closed

- P0 Pixie shows **`8 COOL TIME`** as SHOWTIME starts. Skill-specific tutorial display. `slide_cd_sec` stays **UNKNOWN**.
- SHOWTIME / PHASE freeze `BATTLE TIME` on P0. Full Auto + speed scale **UNKNOWN** (Robin QTE at AUTO still advances `04:25`→`04:24`).
- Live field `FEVER TIME` + leftover `12.50` framed on P0 t440/t442 (tutorial fight). Bottom HUD stays `FEVER 0%`. P1 Robin never reaches 100%.
- Drive select panel coordinates still **UNKNOWN**.
- P0 PHASE splash: HUD stays previous `PHASE n/m`; field standees gone. Our slice now hides standees and holds HUD (engineering). Enemy HP 0→100 switch frame still UNKNOWN. Not T28.
