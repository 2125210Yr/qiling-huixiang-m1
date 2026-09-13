# CUE_SHEET_M1

任务：`M1-G2-VFX-CUES`。闸门：G2 进行中。**不是 M1 验收。不宣称 T28。不宣称还原通过。**

对照：primary handoff `aSbBuFD12HY` / `hgqXY5M9gFk`（`FETCH_LOG.txt`，MediaRecorder 竖屏）+ `supplementary/ragna_gl/CUES.md`（补充，非唯一真值）。源片约 30fps；普通关可测帧距仍 `UNKNOWN`。primary GT = **PARTIAL**（P0+P1 已入库；P2 cookies 失效待重抓）。

时钟：表现跟 `Casts` / 战斗日志 / QTE 结算事件走。`BattleSim.Cast` 先 `ExecuteOpcode` 再 `Casts.Add`，伤害不绑动画结束。本层不改 BattleSim、Smoke、TeamBoard。

叠层时长见 `m1/G0/TIMING_DIFF.md`（GT 侧草稿）。工程常数已按该草稿改过，**仍不是** T28 / GL 已测帧。缺证据写 UNKNOWN。

互斥（工程，非 GT）：QTE / Fever 开时 `VfxShowtime.KillAll()`。Drive 满条**不再**开弹板、也**不**因此杀 SHOWTIME（P0 是点头像）。Fever 条幅碰上 QTE 章或 `VfxJudge` 仍在，则等 `_showtimeT + 0.06s` 再 `BeginFeverBanner`。Fever 或**换波**时不进入 Drive 可选态。Drive 可选时 `WavePreview.SuppressDeathCues()`。SHOWTIME / QTE 时死亡章排队。换波 stamp 会 `VfxFeverOverlay.Hide()`。

## 可区分 cue

| cue | 触发事件 | 模块 / 看见什么 | 输入 | 前摇 | 冲击 | 伤害字 | 恢复 | 下个可输入 |
|---|---|---|---|---|---|---|---|---|
| TAP | `Cast`/`Hit` `SkillType.Tap` | `VfxSkillCast` 头像环 + `VfxTapSkill` 短拳（Soft，**不是** Slide 斩）。无 SHOWTIME、无切镜、无评价章、无 HUD `开演`。烟测 `06b_tap` | P0 t352 tip：Skill Gauge 满后可 tap（与 slide 二选一）。孤立 tap 手势仍 UNKNOWN | UNKNOWN | 工程 `VfxTapSkill` 0.40s | 日志当下 | UNKNOWN | UNKNOWN |
| SLIDE_SHOWTIME | 我方 `Cast` Slide，且未在 Fever 条幅中 | `VfxShowtime`：红斩 + `IT'S SHOWTIME!!` + `SLIDE SKILL` + 技能名 + `RANK n LV a/b`（内容槽；空则 `RANK — LV —/—`）。**不**用单位等级填 RANK（r50=1、r62/PVP5=7）。HUD **不再**印 `开演`。**不是** Fever。烟测 `06c_slide`。primary P0 ~t360s | P0 t358：点头像上滑 | UNKNOWN | 工程 1.47s（P0 30fps first-on→off；≠ T28） | 日志当下（不齐切镜） | UNKNOWN | UNKNOWN |
| DRIVE_SELECT | `Drive>=100` 且 `PendingDriveSlot<0` 且未开 QTE | **无弹板**。P0 t365/t380：点亮头像 + `DRIVE SKILL READY`。状态旗仍在（QTE/冒烟）。**不是** QTE，**不是** SHOWTIME，**不是** Fever。烟测 `06d_drive_select` | 点头像（教程指尖不进切片） | UNKNOWN | 工程态（满条未点） | 无 | UNKNOWN | UNKNOWN |
| DRIVE_CUTIN | 我方 `Cast` Drive / 开 QTE 窗 | HUD `READY?` / `DRIVE`；杀 SHOWTIME。**不是** SHOWTIME，**不是** 评价章 | UNKNOWN（选择板见上；坐标仍缺 GT） | UNKNOWN | 工程 0.70s | 日志当下（QTE `ResolveDrive` 已结算） | UNKNOWN | UNKNOWN |
| QTE_JUDGE | HUD `FinishQte` → `VfxJudge`；适配器 `PresentationCueKind.Judge` | `PERFECT!` / `GREAT!` / `GOOD` 斜章。Perfect 另印 `DAMAGE 150%` + **QTE 增益** `N% TO FEVER`（0→N 计数；Robin 13% 是中间帧）。杀 SHOWTIME。**不是** Drive 切镜 | UNKNOWN | UNKNOWN | Perfect 工程 1.90s（P1 ND；≠ 普通 PVE / T28）；Great/Good 未测 | 先于切镜：`ResolveDrive` 已写入日志 | UNKNOWN | UNKNOWN |
| FEVER_BANNER | `Cast` `FEVER` / `BeginFeverBanner` | 实战（P0 t440）：彩虹条 + `FEVER TIME`（无 !!）+ leftover `12.50`；底栏仍 `FEVER 0%`；连击 `N COMBO` / `N DAMAGE`。提示板（t445）才是 `FEVER TIME!!` + `TOTAL n DAMAGE`。无左侧 DPS 盒、无场上 Repeat/SKIP、无 `狂热时间` 章。杀 SHOWTIME；QTE 章未完则排队。**不是** SHOWTIME。工程窗默认 14s（t435 tip；≠ 终验） | UNKNOWN | UNKNOWN | 工程窗 14s | Fever 通道日志当下 | UNKNOWN | UNKNOWN |
| CONTROL_CHIP | `ApplyStatus` Stun/Shield/DefBuff（烟测注入，非角色稿） | 头像 chips：**`Stun` / `Barrier` / `DEF ↑`**（Robin）。无 SHOWTIME、无 Fever、无换波。烟测 `06e_control` | UNKNOWN | UNKNOWN | 工程态 | 无 | UNKNOWN | UNKNOWN |
| WAVE_ADVANCE | `BattleEventLog` `wave` | `WavePreview`：大 **`PHASE n`** + 关卡名上置 + 大 Heart（P0 t65）。场上站立单位隐藏；HUD `PHASE n/m` 停在上一阶段直到 splash first-off。杀 Fever overlay；不画 Drive 选择板；不画斩板。**不是** Fever，**不是** Ragna `READY TO RUMBLE?`。P0 密抽无 KO 字，敌死亡章不印。烟测 `10_wave` | UNKNOWN | UNKNOWN | 换波工程 2.00s（P0 30fps；≠ T28）；倒下仍 1.20 | 无 | UNKNOWN | UNKNOWN |

场上命中（跟切镜分开）：Tap 短拳；Slide `VfxSlideSlash`；Drive `VfxDriveCrush` 只有碾压，不再印 `完美 150%`。

## Ragna 补充只对照，不升格

| 源片看见 | 本表怎么用 |
|---|---|
| `IT'S SHOWTIME!!` + `SLIDE SKILL` | Slide 身份。不得写成 Fever |
| 技能名切镜（过曝） | Drive 切镜身份。工程选择板是 HUD `驱动选择`，不是切镜；坐标仍 UNKNOWN |
| `GREAT!` / `PERFECT!` + `DAMAGE 150%` + `N% TO FEVER` | QTE 评价身份。**150% / Fever% 不得写入普通 PVE 数值基准** |
| `FEVER TIME` + 连击 / WeakPoint | Fever 条幅身份 |
| `t376s`/`t381s` 五人 Total Damage + Close/Home | **RESULT_RAGNA**。**不是**普通关星级/掉落结算 |
| `READY TO RUMBLE?` | Ragna 阶段。**不是**普通 wave |
| charge / tap 手势 | UNKNOWN。烟测 `06a_charge` 只证工程充电环 |
| 敌 Drive `WARNING!!` | Ragna 机制；工程有 `VfxWarning`，不进本表 |

## Primary ordinary 5p（handoff P0 — 仍非验收）

`aSbBuFD12HY_GL_EN_Gameplay_Part1_202308.mp4`（EN 教程窗，竖屏）：

| 约时刻 | 看见 |
|---|---|
| t300s | Stage Tutorial、PHASE、FEVER 0%、5 槽队、敌 HP |
| t360s | **`IT'S SHOWTIME!!` + `SLIDE SKILL` + Freeze Lance**、FEVER 0% |
| t390–396s | **`勝利` + `victory` + `Tap the screen.`** 后再 **`CLEAR!!`** + 3★ + LEVEL/EXP/GOLD；LEVEL UP 在 t470，不得盖住 CLEAR |
| t420–445s | Fever tips：100% 触发；**14s** 窗；**PERFECT +40 / GREAT +30 / GOOD +15**；`FEVER TIME!!` |
| t510s | PHASE splash + **`>> X2 SPEED`** |
| t120s | 战中对话 + BATTLE TIME + SKIP |
| P1 robin ~t48s | foe **`COOL`/`SLIDE`** under HP + **`DRIVE SKILL READY`** / **`DEF ↑`** / **`Barrier`** |
| P1 robin ~t55–57s | PERFECT/FEVER 40% / **`40% TO FEVER`** / PAUSE+Repeat |
| P0 ~t365s | **`mm:ss BATTLE TIME`**；portrait **`N DRIVE TIME`**；**`ENEMY HP TOTAL`**；bottom **`N% SKILL GAUGE`** |
| P0 ~t90s | Heaven's Dust **PHASE 2/3**（切片仍两波；三波 inventory） |
| P0 ~t250/t510s | top stage + **`PHASE n/m`**；工程 `PHASE n/m` |
| P0 ~t345s | tip Total HP；弧标 **`SKILL HP TOTAL`**（同 meter） |
| P0 tip ~t388s | tip plate Drive；背景 **`DRIVE GAUGE`** / **`SKILL GAUGE`** |
| P1 robin ~t68s | field **`WeakPoint`** stacked + dmg；ally **`Hero` Lv Name** + COOL |
| P0 ~t352–358s | Skill ready ring + Slide tip「slide it up」；工程 `点按已满` + readyDash |
| P0 ~t250s | top **`ENEMY DRIVE`** under ENEMY HP; bottom `SKILL GAUGE` + `TEAM HP TOTAL` |
| P0 ~t60s | party tray **`60 MAX`** (at-cap) / below-cap **`Lv.N`**; tip also `ENEMY HP REM.` |
| P0 ~t396s | CLEAR：黄带左 `n LEVEL`/`n EXP`/`n GOLD`，右账号+`EXP n/m`；奖励头像 uncap★+E；工程 stub `—` |
| P0 ~t470s | account **`LEVEL UP!`**：`Level up!` + `n → m` + stamina refill/max+ + Confirm；工程 stub `—` + 黄 Confirm |

详 `gt_search/19_primary_handoff_frames.md`。工程：SHOWTIME 戳；QTE Fever% / 14s 窗 / Drive QTE **7s**（P0 `DRIVE TIME`）已按 tip 对齐（≠ 验收）。**不得**报 T28 / fidelity pass。

## Contrast ordinary 5p（仍非 primary）

`contrast/gl_newbie_clear_2022_BV14S4y1b7RR`（2022-05 中文 UI）：`IT'S SHOWTIME!!` / `DRIVE CRUSH` / `DRIVE SKILL READY` / FEVER%。工程对照布局与互斥；**不得**升格为停服窗英文主 GT。详 `gt_search/CONTRAST_NEWBIE_CLEAR_2022_CUES.md`。

## 不做

- 不把 Ragna 排名当普通关结算 cue
- 不发明普通 5 人坐标或 GL 终式
- 不宣称 T28 / M1 验收 / fidelity pass
