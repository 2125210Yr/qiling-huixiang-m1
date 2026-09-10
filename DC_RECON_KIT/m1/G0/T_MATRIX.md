# T_MATRIX — T01–T32 与 G2 切片（功能 / 还原）

- 任务：`M1-G2-TMATRIX`
- 日期：2026-09-10
- 角色：只读 QA 矩阵。**未改** `client/`。**未开** Editor。未重跑 `dotnet test`。未 spawn。未开 G3。
- 项定义：`DC_RECON_KIT/04_ACCEPTANCE_AND_TESTS.md`
- G2 切片定义：`DC_RECON_KIT/03_MULTIAGENT_AND_MILESTONES.md` G2、`DC_RECON_KIT/m1/G1/NEXT_WAVE.md`、`UiStateMap.md`（入口/返回不得从分母删）

## 总判（必须先读）

**M1 未验收。G2 未过。不得报 90%。不得报还原分 / numeric fidelity / T27。**

功能 IMPLEMENTED ≠ 还原通过。夹具绿 ≠ 原作回归。PlayMode 启动成功 ≠ 视觉通过。补充 Ragna 帧 ≠ primary GT。

| 闸门 | 值 |
|---|---|
| TARGET | `TARGET_FROZEN_GT_SEARCHING` |
| CONTRACT | `CONTRACT_FROZEN` |
| MILESTONE | `M1 IN_PROGRESS`（**未验收**） |
| G2 | `IN_PROGRESS_CORE_SLICE`（**未过**） |
| PlayMode | `FAILED`（`FAIL drive never fired`；未重跑） |
| EditMode | `NOT_RUN` |
| primary GT | `CANDIDATE_NEEDS_FETCH` / `BLOCKED`（`docs/reference/gl-shutdown-pve/` **0 mp4**） |
| `GL_FINAL_VERIFIED` | **forbidden / not created** |
| 还原 IMPLEMENTED | **0** |
| 功能 IMPLEMENTED（内部一致性） | T01 / T03 / T10 / T15 / T18 |

---

## 读过的材料

### STATUS / QA（时间序，后者覆盖前者的过时句）

| 文件 | 用途 |
|---|---|
| `STATUS.md` | 最新闸门。PlayMode=`FAILED`。还原分 0。功能核 T01/T03/T10/T15/T18。 |
| `QA_EXECUTOR.md` | 最新独立 QA 表。T 标签以此为底。当时 PlayMode 仍写 `NOT_RUN`（已被 STATUS 推进为 FAILED）。默认套件记录 **0 失败 / 115**。 |
| `QA_REPAIR2.md` / `QA_REPAIR.md` / `QA_VERIFY.md` | 前序独立复核。T15/T18 自 VERIFY 的 STUB/MISSING 升为功能 IMPLEMENTED。 |
| `M1_UNGATE.md` | 硬缺件。T26–T32 仍 `NOT_RUN`/`BLOCKED`。 |
| `GL_PVE_GROUND_TRUTH.md` / `.json` | primary 仍需拉取。`supplementary` ≠ `primary`。 |
| `UNKNOWN_REGISTER.json` | U001–U014。U013=`FAILED`。U010 禁 T27。 |

`QA_EXECUTOR` 之后 STATUS 已记：`M1-G2-CATALOG-OP`、`M1-G2-PLAY-RECORD`、`M1-G2-FIX-DRIVE-SMOKE`、`M1-G2-SUPP-CUES`。本表采纳这些闸门，**不**把后续夹具文件名写成新的验收跑次。

### G1 契约（`CONTRACT_FROZEN`）

| 契约 | 本表用到的冻结点 |
|---|---|
| `CombatContract.md` / `.json` | 五动作不可合并；`tick_hz=30`；`party_size_hardcoded=false`；`poison_policy=ACTION_AND_HIT_TRIGGERS`；未知 opcode=`FAIL`；禁 `GL_FINAL_VERIFIED` |
| `ClockPolicy.md` / `.json` | SlideCd 已独立；`slide_cd_sec=UNKNOWN`；禁全局 `timeScale` 当还原；Drive/Fever 事件拆分 |
| `EffectSchema.md` / `.json` | 毒 on_action/on_hit；`unimplemented_opcodes`：pierce / extra_flat / dispel / retarget / revive |
| `FormulaProfiles.md` / `.json` | 只允许 JP/KR 对照；GL=`UNKNOWN`；`fidelity_pass_without_gt=false` |
| `UiStateMap.md` | 画幅 `NOT_MEASURED`；M1 必测状态 + 入口/返回；WB 20 人页不从分母删除 |

### 测试文件名（工程自有，不含 PackageCache）

`client/Assets` 下 **0** 个 `*Test*.cs`。EditMode / PlayMode UTF **无**工程测文件。

`tools/BattleSim.Tests/`：

| 文件 | 方法数（只数文件名，不宣称本轮绿） | 主要对着 |
|---|---|---|
| `BattleSimTests.cs` | 90 | 旧夹具：元素/Wiki 式/存档/Drive 骨架/养成。`SameSeedSameLog` 只比 Outcome/HP/Drive/TimeLeft |
| `M1CoreSliceTests.cs` | 10 | T01 / T03 / T10 / T15 / T21 容量 / Drive·Fever 序列 / StayHeld |
| `M1RepairQaTests.cs` | 12 | `GL_UNKNOWN` 门闩、opcode 施加/失败、Fever 不借 Tap、T18 非均匀 RNG、T21 Length |
| `IgnitionTests.cs` | 6 | T19 通道形（U006 仍 UNKNOWN） |
| `M1EventLogTests.cs` | 4 | T01 导出哈希（`QA_EXECUTOR` 的 115 清单里还没有此文件） |
| `M1FormulaIsolationTests.cs` | 4 | T15 通道隔离；文件头写明不是还原 |
| `DC_RECON_KIT/tools/test_candidate_formulas.py` | 候选式，不是 T 回归 | |

STATUS / `QA_EXECUTOR` 记录的默认 `dotnet test`：**0 失败 / 115**（夹具自洽）。本任务未重跑。磁盘上现有 6 个 cs 测文件，方法数合计 126。**不得**把 115 或 126 写成 M1 / 90% / 还原。

---

## 标签

`IMPLEMENTED` / `STUB` / `MISSING` / `NOT_RUN` / `BLOCKED`

- 功能 `IMPLEMENTED`：内部一致性（有夹具或契约对齐的执行路径）。**不是**原版回归。
- 还原列：无 primary GT 时不得标 `IMPLEMENTED`。本表 **32/32 还原 = `BLOCKED`**。
- `STUB`：有骨架或子集，分母项未齐。
- `MISSING`：定义项无执行体。
- `NOT_RUN`：要 PlayMode / 真机 / 叠图，本闸未交出通过证据。
- PlayMode 环境令牌是 `FAILED`，不把 T26–T32 改成还原通过，也不从分母拿掉。

---

## 覆盖率（分母不删）

约定范围分母保持完整。未实现、未跑、未知**不**静默剔除。

| 集合 | 分母 | 功能 IMPLEMENTED | 还原 IMPLEMENTED | 不得写成 |
|---|---|---|---|---|
| T01–T32 | **32** | **5**（T01 T03 T10 T15 T18） | **0** | 5/5、90%、还原通过 |
| G2 切片 | **9**（8 战斗切片 + 入口/返回） | 见下表；无「9/9 功能过」 | **0** | G2 通过 |
| UiStateMap M1 必测状态 | **10** + 入口/返回 | 未齐 | **0** | T27 / 视觉还原 |

功能列计数：IMPLEMENTED 5 · STUB 17 · MISSING 3 · NOT_RUN 7 = **32**。

---

## T01–T32

项文摘自 `04_ACCEPTANCE_AND_TESTS.md`。标签底稿 = `QA_EXECUTOR.md`。PlayMode 注记按最新 `STATUS.md`。

| ID | 项 | 功能 | 还原 | 证据 / 缺口 |
|---|---|---|---|---|
| T01 | 同 seed、同命令及 tick，事件日志哈希相同 | IMPLEMENTED | BLOCKED | `EventLogHashIsStableForSameSeed`；`M1EventLogTests` 导出哈希。`SameSeedSameLog` 不是事件哈希。无 GT 事件真值。 |
| T02 | 30/60/120 渲染下逻辑结果相同 | MISSING | BLOCKED | 核锁 30Hz（契约：实现选择 ≠ GL 规格）。无跨渲染帧率测。 |
| T03 | Auto / 充能 / SlideCd 不共享时钟 | IMPLEMENTED | BLOCKED | `SlideCdIsIndependentOfCharge`；`ControlPausesChargeButSlideCdContinues`。`ClockPolicy.slide_cd_present=true`。秒数 UNKNOWN。 |
| T04 | 暂停 / 加速 / QTE / Fever 时钟与参考一致 | STUB | BLOCKED | `StayHeldKeepsManualBattleFrozen`；QTE 超时夹具有。窗仍 7s/70 hit。无 GT 分时钟。U011。 |
| T05 | 采样不重复 Tap；上滑不误触 | STUB | BLOCKED | 手势代码已分。无采样率/双触测。PlayMode `FAILED`，不是手势通过。 |
| T06 | 预约 / 自动次序 / 取消 | STUB | BLOCKED | `AutoTapFollowsReservation`。Reserve 仍 5 格。取消/无效命令未闭合。 |
| T07 | 最低 HP 绝对 vs 比例 | STUB | BLOCKED | 仍只比绝对值。比例键在契约 `target_keys`，未测。 |
| T08 | 多目标 / 多段重选；中途死亡 | STUB | BLOCKED | `retarget` 未实现，执行失败关闭。 |
| T09 | 嘲讽 / 免疫 / 不可选 | STUB | BLOCKED | 嘲讽施加+优先选取在（`KnownOpcodeSettlesShieldStunAndTaunt`）。免疫/不可选无。 |
| T10 | 毒：行动/受击 ≠ 每秒 DoT | IMPLEMENTED | BLOCKED | `PoisonTriggersOnActionAndHitTakenNotPerSecond`。`EffectSchema.poison.implemented_in_engine=true`。`GL_UNKNOWN` 不改 HP。U014。 |
| T11 | 护盾/穿防/反射/吸血/死亡触发 | STUB | BLOCKED | 盾施加+吸收在。`dmg.pierce` 仍失败。反射/吸血/死亡触发无体。分母五段都留。 |
| T12 | 堆叠 / 控制中充能与冷却 | STUB | BLOCKED | 受控停充能、SlideCd 仍走。刷新/上限/堆叠未闭合。 |
| T13 | 复活 / 迟到事件 | MISSING | BLOCKED | `revive` 未实现，失败关闭（正确）。迟到奖励锁无。 |
| T14 | S 与固有/装备 ATK 不重复 | STUB | BLOCKED | `GrowthPlusZeroDoesNotDoubleWeapon` 等成长测，不证明 S 通道。 |
| T15 | Tap / Slide / Auto / 特殊连打不混 | IMPLEMENTED | BLOCKED | `AutoChannelIsNotTap`；`ExtraDmgDoesNotFoldAutoOrFeverIntoTapTsAmp`；`T15ChannelsStayIsolated`；`FeverDoesNotReadTapAtkCoefOrFlatPower`。GL 式 UNKNOWN。 |
| T16 | 属性 / 暴击 / Fever 例外 | STUB | BLOCKED | 元素/暴击夹具在。Fever 例外无 GT。 |
| T17 | 额外 vs 穿防 | STUB | BLOCKED | `dmg.extra_flat` / `dmg.pierce` 未实现。math 有对照形，不是 GL。 |
| T18 | Slide 区间 ≠ 均匀 RNG | IMPLEMENTED | BLOCKED | `SlideDoesNotUseUniformVarianceRng`。实战 `variance=1`。分布仍 U012。 |
| T19 | 点火 / 增幅独立 | STUB | BLOCKED | `IgnitionTests` + `ChannelsAreIndependent`。U006 UNKNOWN。 |
| T20 | 舍入 / 多段 / 零血护盾 | STUB | BLOCKED | 盾吸收在。临界全表无。U007。 |
| T21 | 5 人与 20 人前后排 | STUB | BLOCKED | `PartyCapacityIsNotHardcodedFive`；`PartyCapSaveAndStatsFollowPartyLength`。默认 5。**20 人/前后排留在分母**，未做。 |
| T22 | 超时 / 退出 / 失败 / 跨波 | STUB | BLOCKED | 空/未知/未实现 opcode → `Failed`。跨波有骨架。退出/继承未闭合。 |
| T23 | 奖励/票只一次 | STUB | BLOCKED | 存档奖励夹具，非战斗切片幂等证明。 |
| T24 | 锁定角色不当材料 | MISSING | BLOCKED | 养成。**分母保留。** |
| T25 | 坏档 / schema / 日切 | STUB | BLOCKED | `CorruptPrimaryFallsBackToBak`。日切/活动切换无。 |
| T26 | 入口→编队→战斗→结算→养成→再战 | NOT_RUN | BLOCKED | PlayMode VS Smoke **FAILED**（开战有图，Drive 未打出，无 Fever/结算图）。全链未过。`our_slice/` 不是 GT。 |
| T27 | UI 叠图误差 | NOT_RUN | BLOCKED | 布局 `NEEDS_REFERENCE`。U010 `NOT_MEASURED`。禁伪 pass。 |
| T28 | 输入/命中/数字 ≤2 参考帧 | NOT_RUN | BLOCKED | 无 primary GT，未逐帧。 |
| T29 | 遮挡/层级人工复核 | NOT_RUN | BLOCKED | |
| T30 | 无原画只验未遮罩区 | NOT_RUN | BLOCKED | |
| T31 | 真机帧率/内存 | NOT_RUN | BLOCKED | 无真机。不得报真机通过。 |
| T32 | 无异常日志 / 进出释放 / 20 人压力 | NOT_RUN | BLOCKED | 20 人压力留在分母。 |

**T 还原通过数：0 / 32。**  
功能 IMPLEMENTED：T01 / T03 / T10 / T15 / T18（均内部一致性）。

---

## G2 切片

来源：`03_MULTIAGENT`「自动、点击、上滑、Drive/QTE、Fever、控制/护盾/目标切换、死亡/结算、暂停/加速/自动」+「基础入口和返回」。`NEXT_WAVE` 八切片。入口/返回不删。

通过条件（原文）：全部关键状态测试 + 对照视频。数值未知必须显式说明。无 GT 精确分支不得用临时值换 fidelity pass。

| 切片 | 功能 | 还原 | 笔记 |
|---|---|---|---|
| Auto | 近 IMPLEMENTED | BLOCKED | 独立通道 + 不进 tsAmp。`GL_UNKNOWN` 不结算伤。 |
| Tap | IMPLEMENTED（能打） | BLOCKED | 默认不结算；客户端开战仍 `JP_LEGACY_EMPIRICAL`。 |
| Slide | IMPLEMENTED（CD + 无均匀 RNG） | BLOCKED | 占位 8s ≠ GL。区间压成 1 ≠ 分布真值。 |
| Drive + QTE | STUB | BLOCKED | 夹具能打 Drive/QTE；PlayMode 冒烟 `drive never fired`。倍率 0.9/1.2/1.5 无 GT。U003。 |
| Fever | STUB | BLOCKED | 不再借 Tap 系数；通道形仍 Ts×FeverMul。仍 7s/70 hit。PlayMode 无 Fever。 |
| 控制 / 护盾 / 换目标 | STUB | BLOCKED | 盾/Stun/Freeze/嘲讽施加在。无 `retarget` 体。免疫/不可选无。 |
| 死亡 / 换波 / 结算 | STUB | BLOCKED | `revive` 仍失败。PlayMode 无结算图。迟到事件锁无。 |
| 暂停 / 加速 / 自动 | STUB | BLOCKED | StayHeld / Full Auto 夹具有。档位 U011 UNKNOWN。 |
| 基础入口 / 返回 | STUB | BLOCKED | 冒烟截过 Home / 编队 / Inspect / 开战。无闭环返回、无对照视频。 |

**G2 还原通过数：0 / 9。不满足「全部关键状态 + 对照视频」。**

### UiStateMap 必测状态（分母保留）

| 状态 | 功能 | 还原 |
|---|---|---|
| BattleIdle | STUB | BLOCKED |
| ChargingReady | STUB | BLOCKED |
| Tap | IMPLEMENTED（能打） | BLOCKED |
| Slide | IMPLEMENTED（CD） | BLOCKED |
| DriveSelect | STUB | BLOCKED |
| Qte | STUB | BLOCKED |
| Fever | STUB | BLOCKED |
| Controlled | STUB | BLOCKED |
| DeathOrWave | STUB | BLOCKED |
| Result | STUB | BLOCKED |

10/10 状态仍在分母。还原 0。不得用大厅立绘或「胜利」日志写成状态还原通过。

---

## BLOCKED（本矩阵仍开）

| ID | 项 | 状态 |
|---|---|---|
| U001 | primary GT 普通 5 人 PVE | `SEARCH_IN_PROGRESS`；目录 0 mp4。补充 Ragna 帧不是 primary |
| U002 | GL Tap/Slide/Auto/Fever 公式 | UNKNOWN |
| U003 | Drive 实际伤害 | UNKNOWN |
| U006 | Ignition/增幅组合 | UNKNOWN |
| U010 | 参考画幅 | `NOT_MEASURED` → 禁 T27 |
| U011 | 速度/自动参考档 | UNKNOWN |
| U012 | Slide 随机分布 | 非均匀夹具 ≠ GL 真值 |
| U013 | PlayMode 基线 | `FAILED`（未重跑） |
| U014 | 毒触发帧距 | UNKNOWN |
| — | EditMode | `NOT_RUN` |
| — | T21 20 人 / T24 / T31 / T32 | 留在分母，未做或未跑 |

---

## 本任务边界

- 只写本文件。未改 client。未开 Editor。未宣称 90%。未宣称 M1 验收。未写 G3 入口。
- 未把 115 绿、126 方法、大厅截图、补充 cue、或 `FAIL drive never fired` 升格为还原通过。
