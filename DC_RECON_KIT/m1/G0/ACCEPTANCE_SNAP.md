# ACCEPTANCE_SNAP — M1 验收快照（`acceptance_results` 风格）

- 任务：`M1-G2-UNKNOWNS-DOC`
- 日期：2026-09-10
- 对照：`STATUS.md`、`QA_DRIVE.md`、`QA_EXECUTOR.md`、`T_MATRIX.md`、`SLICE_VS_RAGNA.md`、`GL_PVE_GROUND_TRUTH.*`、`UNKNOWN_REGISTER.json`、`04_ACCEPTANCE_AND_TESTS.md`
- **未改** `client/`、`TARGET.json`。未开 Editor。未开 G3。

## 总判（必须先读）

**M1 未验收。G2 未过。不得报 90%。不得报还原分 / numeric fidelity / T27 / PlayMode PASS。**

权重只是合同建议，不是已打分。本快照 **scores 全为 null**，不把权重加成总分，也不用夹具绿数回推完成度。未实现、未跑、未知 **留在分母**。

---

## Header（对齐 `templates/acceptance_results.json`）

| 字段 | 值 |
|---|---|
| `milestone` | `M1`（`IN_PROGRESS`，**未验收**） |
| `target_manifest_hash` | 未计算。`TARGET.json` = `TARGET_FROZEN_GT_SEARCHING` |
| `scope_coverage` | 约定范围分母在册：**T01–T32 = 32/32 仍在**；G2 切片 **9/9 仍在**（含入口/返回）；UiStateMap 必测 **10/10 + 入口/返回仍在**。覆盖率分子不是“已过条数”。 |
| `evidence_coverage` | primary GT **0 mp4**；PlayMode **FAILED**；EditMode **NOT_RUN**；无 trx/XML、无 UI_DIFF、无 TIMING_DIFF、无 NUMERIC_ERROR_REPORT。证据覆盖 **不能**写成已齐。 |
| `weights` | combat 35 / ui 20 / presentation 15 / numerics 20 / meta 10（建议，未用来打总分） |
| `scores.combat` | `null` |
| `scores.ui` | `null` |
| `scores.presentation` | `null` |
| `scores.numerics` | `null` |
| `scores.meta` | `null` |
| `critical_pass` | `false` |
| `unknowns` | U001–U015，**闭项 0**（见 `UNKNOWN_REGISTER.json`） |
| `status` | `NOT_ACCEPTED` / `BLOCKED` |

建议门槛（合同原文：总分 ≥90、每领域 ≥85、阻断规则全过）**本闸未进入打分**。禁止把 null 写成 90，禁止把 5 条功能 IMPLEMENTED 写成 5/5 或 90%。

还原分：**0**（T 还原 IMPLEMENTED = 0 / 32；G2 还原 = 0 / 9）。

---

## `scores` 为什么是 null

| 域 | 权重 | 分数 | 原因 |
|---|---|---|---|
| combat | 35 | null | 功能核夹具自洽 ≠ 原作回归。无 primary GT。PlayMode Drive 未打出。 |
| ui | 20 | null | 画幅 `NOT_MEASURED`（U010）。禁 T27。我方壳 / Ragna 三栏裁切不是坐标。 |
| presentation | 15 | null | 无对照视频、无逐帧、无 cue 对照通过。 |
| numerics | 20 | null | GL 公式通道 UNKNOWN（U002/U003/U006）。`GL_FINAL_VERIFIED` 未创建且禁止。 |
| meta | 10 | null | T23–T25 / 养成闭环未齐。T24 仍 MISSING，**不从分母删**。 |

---

## 分母（未实现不删）

计数是“仍在约定范围”，不是通过数。

| 集合 | 分母 | 功能 IMPLEMENTED | 还原 IMPLEMENTED | 明确留着的未实现 / 未跑 |
|---|---|---|---|---|
| T01–T32 | **32** | **5**（T01 T03 T10 T15 T18，内部一致性） | **0** | T02 / T13 / T24 = MISSING；T21 的 **20 人/前后排**；T26–T32 = NOT_RUN；T31 无真机；T32 含 20 人压力 |
| G2 切片 | **9** | 不报 9/9 | **0** | **入口/返回**留在分母。Drive+QTE / Fever / 死亡·结算 未齐 |
| UiStateMap M1 必测 | **10** + 入口/返回 | 未齐 | **0** | `DriveSelect` / `Qte` / `Fever` / `Controlled` / `DeathOrWave` / `Result` 仍在。后续 WB 20 人页不在本切片测，**也不从后续分母删** |
| UNKNOWN | **15**（U001–U015） | — | 闭项 **0** | 全部 `class=UNKNOWN` |

功能列（T_MATRIX）：IMPLEMENTED 5 · STUB 17 · MISSING 3 · NOT_RUN 7 = **32**。

未实现 opcode（`dmg.pierce` / `dmg.extra_flat` / `status.dispel` / `retarget` / `revive`）继续 FAIL 是正确行为，**不**从效果分母里划走。

---

## `tests`

标签只复述 `T_MATRIX.md` / `QA_EXECUTOR.md` / `QA_DRIVE.md`。无 GT 时还原列不得标 IMPLEMENTED。本任务未重跑测试。

### 逻辑夹具（不是原作回归）

| 记录 | 计数 | 用途 |
|---|---|---|
| `QA_DRIVE.md`（最新独立 QA） | 默认 `dotnet test` **0 失败 / 118** | 夹具自洽。**≠** PlayMode / M1 |
| `STATUS.md` 归档句 | **0 失败 / 115** | 较早记录；不要和 118 合成“更绿” |
| `T_MATRIX.md` 磁盘方法名 | 6 个 cs 合计 **126** 方法 | 未宣称本轮全绿 |
| Unity EditMode / PlayMode UTF | **无**工程 `*Test*.cs` | EditMode = `NOT_RUN` |
| 必交 XML/JSON | **无** | 合同第 4 节工件未齐 |

118 / 115 / 126 **都不是**还原通过，也不能加成 90%。

### T01–T32（分母 32）

| ID | 功能 | 还原 | 分母处理 |
|---|---|---|---|
| T01 | IMPLEMENTED | BLOCKED | 留 |
| T02 | MISSING | BLOCKED | **留**（核锁 30Hz ≠ GL 规格） |
| T03 | IMPLEMENTED | BLOCKED | 留；秒数 UNKNOWN |
| T04 | STUB | BLOCKED | 留；U011 |
| T05 | STUB | BLOCKED | 留；PlayMode FAILED ≠ 手势过 |
| T06 | STUB | BLOCKED | 留 |
| T07 | STUB | BLOCKED | 留 |
| T08 | STUB | BLOCKED | 留；`retarget` 未实现 |
| T09 | STUB | BLOCKED | 留；免疫/不可选无 |
| T10 | IMPLEMENTED | BLOCKED | 留；U014 |
| T11 | STUB | BLOCKED | 留；穿防/反射/吸血/死亡触发未齐 |
| T12 | STUB | BLOCKED | 留 |
| T13 | MISSING | BLOCKED | **留**；`revive` 失败关闭 |
| T14 | STUB | BLOCKED | 留 |
| T15 | IMPLEMENTED | BLOCKED | 留；GL 式 UNKNOWN |
| T16 | STUB | BLOCKED | 留 |
| T17 | STUB | BLOCKED | 留 |
| T18 | IMPLEMENTED | BLOCKED | 留；分布 U012 |
| T19 | STUB | BLOCKED | 留；U006 |
| T20 | STUB | BLOCKED | 留；U007 |
| T21 | STUB | BLOCKED | **20 人/前后排留在分母**，未做 |
| T22 | STUB | BLOCKED | 留 |
| T23 | STUB | BLOCKED | 留 |
| T24 | MISSING | BLOCKED | **留** |
| T25 | STUB | BLOCKED | 留 |
| T26 | NOT_RUN | BLOCKED | **留**；PlayMode FAILED，全链未过 |
| T27 | NOT_RUN | BLOCKED | **留**；U010 禁伪 pass |
| T28 | NOT_RUN | BLOCKED | **留** |
| T29 | NOT_RUN | BLOCKED | **留** |
| T30 | NOT_RUN | BLOCKED | **留** |
| T31 | NOT_RUN | BLOCKED | **留**；无真机不得报真机通过 |
| T32 | NOT_RUN | BLOCKED | **留**；20 人压力留在分母 |

**T 还原通过数：0 / 32。**

### G2 切片（分母 9，含入口/返回）

| 切片 | 功能 | 还原 |
|---|---|---|
| Auto | 近 IMPLEMENTED | BLOCKED |
| Tap | IMPLEMENTED（能打） | BLOCKED |
| Slide | IMPLEMENTED（CD + 非均匀夹具） | BLOCKED |
| Drive + QTE | STUB（PlayMode 未打出 Drive） | BLOCKED |
| Fever | STUB（PlayMode 无 Fever） | BLOCKED |
| 控制 / 护盾 / 换目标 | STUB | BLOCKED |
| 死亡 / 换波 / 结算 | STUB（无结算图） | BLOCKED |
| 暂停 / 加速 / 自动 | STUB | BLOCKED |
| 基础入口 / 返回 | STUB（冒烟有 Home/编队/Inspect/开战；无闭环返回、无对照视频） | BLOCKED |

**G2 还原通过数：0 / 9。** 不满足「全部关键状态 + 对照视频」。

---

## `raw_artifacts`

| 合同工件 | 本闸 |
|---|---|
| BUILD_LOG | Editor 日志有：`DC_RECON_KIT/m1/G0/BASELINE_LOGS/editor-playmode-20260910.log`。不是验收绿。 |
| TEST_RESULTS XML/JSON | **缺**。只有 stdout 摘录（115 / 118）。 |
| BATTLE_EVENT_LOG | 夹具哈希有；无 GT 事件真值。 |
| REFERENCE_ID | primary **null** / `SEARCH_IN_PROGRESS` |
| 截图/录屏实际路径 | 我方切片 `docs/reference/gl-shutdown-pve/our_slice/` + `BASELINE_LOGS/`。**不是 GT。** Ragna 补充 `…/supplementary/ragna_gl/` ≠ primary。`gl-shutdown-pve/` 根目录 **0 mp4**。 |
| UI_DIFF | **无**（U010） |
| TIMING_DIFF | **无** |
| NUMERIC_ERROR_REPORT | **无** |
| COVERAGE | 本文件分母表 + `T_MATRIX.md` |
| UNKNOWNS | `UNKNOWN_REGISTER.json`（U001–U015，闭项 0） |

PlayMode 原始结果：`FAIL drive never fired`（归档）。`QA_DRIVE` 未重开 Editor，不得把代码侧 StayHeld 写成冒烟绿。

---

## `unknowns`

闭项规则：UNKNOWN 保持 UNKNOWN。补充帧、我方切片、JP/KR 对照、夹具绿 **都不能**闭项。

| ID | topic | status | blocking | 本轮核对 |
|---|---|---|---|---|
| U001 | primary GT 普通 5 人 PVE | `SEARCH_IN_PROGRESS` | 是 | 根目录 0 mp4；补充 ≠ primary |
| U002 | GL Tap/Slide/Auto/Fever 公式 | OPEN | 是 | 客户端开战仍 JP 对照 |
| U003 | Drive 实际伤害 | OPEN | 是 | 冒烟无 Drive；Ragna 150% 禁写入 |
| U004 | 暴击曲线 | OPEN | 否 | 留在 T16 |
| U005 | 命中/抗控 | OPEN | 否 | 不锁 S13 |
| U006 | Ignition/增幅组合 | OPEN | 是 | 通道形 ≠ compose |
| U007 | 舍入/结算序 | OPEN | 否 | 留在 T20 |
| U008 | Boss 防 / 模式减伤 | OPEN | 否 | Raid/WB 对照不得闭 |
| U009 | buff 快照 vs 实时 | OPEN | 否 | 留在 T12 |
| U010 | 参考画幅 px | `NOT_MEASURED` | 是 | **禁 T27** |
| U011 | 速度/自动参考 | OPEN | 否 | 含 SlideCd 秒 / Fever 窗未测 |
| U012 | Slide 随机分布 | OPEN | 否 | 非均匀夹具 ≠ 真值 |
| U013 | PlayMode 基线 | `FAILED` | 是 | 仍 UNKNOWN；未重跑 |
| U014 | 毒触发帧距 | OPEN | 否 | 禁每秒 DoT；GL 时序未测 |
| U015 | EditMode 基线 | `NOT_RUN` | 是 | **新入册，避免从分母消失** |

普通 5 人仍缺（`SLICE_VS_RAGNA`）：孤立 charge/tap、Drive **选择**面板、换波/死亡、星级/掉落结算、可测坐标、GL 公式。全部挂在上表，不另开假通过列。

---

## 硬否决（任一成立即不可验收）

1. `docs/reference/gl-shutdown-pve/` **0** 条普通 5 人 PVE mp4。primary = `CANDIDATE_NEEDS_FETCH` / `BLOCKED`。
2. PlayMode 归档 **FAILED**（`drive never fired`）。本任务未重跑。
3. EditMode **NOT_RUN**（U015）。
4. `critical_pass = false`。阻断级还原规则未过。
5. 无 `GL_FINAL_VERIFIED`（禁止发明）。

---

## 不得写成

- 90% / 总分 90 / 领域 85
- 5/5、9/9、10/10 功能过、G2 通过、M1 验收
- numeric fidelity / T27 pass / 视觉还原通过
- 115 或 118 绿 = 原作回归
- 大厅立绘、「胜利」日志、Ragna 补充 cue = 状态还原通过
- 代码侧 Drive 冒烟修补 = PlayMode PASS

验收还缺什么见 `M1_UNGATE.md`。**M1 未验收，不写 G3 入口。**
