# CUE_SHEET_M1

任务：`M1-G2-VFX-CUES`。闸门：G2 进行中。**不是 M1 验收。不宣称 T28。不宣称还原通过。**

对照：`docs/reference/gl-shutdown-pve/supplementary/ragna_gl/CUES.md`（Ragna GL 补充，**不是**普通 5 人 PVE 唯一真值）。源片 30fps；普通关可测帧距仍 `UNKNOWN`。primary GT 仍 `NEEDS_FETCH`。

时钟：表现跟 `Casts` / 战斗日志 / QTE 结算事件走。`BattleSim.Cast` 先 `ExecuteOpcode` 再 `Casts.Add`，伤害不绑动画结束。本层不改 BattleSim、Smoke、TeamBoard。

工程时长是现有模块常数，**不是** GL 已测帧。缺证据写 UNKNOWN。

## 五条可区分 cue

| cue | 触发事件 | 模块 / 看见什么 | 输入 | 前摇 | 冲击 | 伤害字 | 恢复 | 下个可输入 |
|---|---|---|---|---|---|---|---|---|
| TAP | `Cast`/`Hit` `SkillType.Tap` | `VfxSkillCast` 头像环 + `VfxTapSkill` 短轨迹。无 SHOWTIME、无切镜、无评价章 | UNKNOWN（Ragna 也没孤立出手势） | UNKNOWN | 工程 `VfxTapSkill` 0.40s | 日志当下 | UNKNOWN | UNKNOWN |
| SLIDE_SHOWTIME | 我方 `Cast` Slide，且未在 Fever 条幅中 | `VfxShowtime`：红斩 + `SHOWTIME` + `上滑` + 技能名。**不是** Fever | UNKNOWN | UNKNOWN | 工程 1.20s | 日志当下（不齐切镜） | UNKNOWN | UNKNOWN |
| DRIVE_SELECT | `Drive>=100` 且尚未 `TryBeginDrive` | HUD `driveSelect` 板：`驱动选择` + 「点按一名角色释放驱动」。**不是** QTE，**不是** SHOWTIME，**不是** Fever | UNKNOWN | UNKNOWN | 工程态（满条未点） | 无 | UNKNOWN | UNKNOWN |
| DRIVE_CUTIN | 我方 `Cast` Drive，且未在 Fever 条幅中 | `PixelCombatFx.PlayDrive`：金橙技能名 + `驱动`。**不是** SHOWTIME，**不是** 评价章 | UNKNOWN（选择板见上；坐标仍缺 GT） | UNKNOWN | 工程 0.70s | 日志当下（QTE `ResolveDrive` 已结算） | UNKNOWN | UNKNOWN |
| QTE_JUDGE | HUD `FinishQte` → `VfxJudge`；适配器 `PresentationCueKind.Judge` | `完美` / `优秀` / `好` 斜章。Perfect 另印伤害% + 狂热轨。**不是** Drive 切镜 | UNKNOWN | UNKNOWN | 工程 0.96–1.28s | 先于切镜：`ResolveDrive` 已写入日志 | UNKNOWN | UNKNOWN |
| FEVER_BANNER | `Cast` `FEVER` / `PresentationCueKind.Fever` | `VfxFeverOverlay`：`狂热时间` + 粉金轨。连击走 `VfxComboBanner`。**不是** SHOWTIME | UNKNOWN | UNKNOWN | 工程窗 7s（现核，非 GL 验证） | Fever 通道日志当下 | UNKNOWN | UNKNOWN |

场上命中（跟切镜分开）：Tap 短拳；Slide `VfxSlideSlash`；Drive `VfxDriveCrush` 只有碾压，不再印 `完美 150%`。

## Ragna 补充只对照，不升格

| 源片看见 | 本表怎么用 |
|---|---|
| `IT'S SHOWTIME!!` + `SLIDE SKILL` | Slide 身份。不得写成 Fever |
| 技能名切镜（过曝） | Drive 切镜身份。选择面板仍缺 |
| `GREAT!` / `PERFECT!` + `DAMAGE 150%` + `N% TO FEVER` | QTE 评价身份。**150% / Fever% 不得写入普通 PVE 数值基准** |
| `FEVER TIME` + 连击 / WeakPoint | Fever 条幅身份 |
| `t376s`/`t381s` 五人 Total Damage + Close/Home | **RESULT_RAGNA**。**不是**普通关星级/掉落结算 |
| `READY TO RUMBLE?` | Ragna 阶段。**不是**普通 wave |
| charge / tap 手势 | UNKNOWN |
| 敌 Drive `WARNING!!` | Ragna 机制；工程有 `VfxWarning`，不进本表五条 |

## 不做

- 不把 Ragna 排名当普通关结算 cue
- 不发明普通 5 人坐标或 GL 终式
- 不宣称 T28 / M1 验收 / fidelity pass
