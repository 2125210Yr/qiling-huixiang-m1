# M1-G2-QA-REPAIR — 独立返修复核

- 任务：`M1-G2-QA-REPAIR`
- 角色：`dc_qa`（只读生产代码；本文件为 G0 QA 报告）
- 日期：2026-09-10
- 对照：上一轮 `QA_VERIFY.md` 点名的 6 个缺口、`G1` 契约、`04_ACCEPTANCE_AND_TESTS.md` T01–T32
- **未改** 生产代码、`TARGET.json`、未 spawn、未开 G3
- **不采信** 实现代理自述 / `STATUS.md` 本轮声称

## 总判（必须先读）

**M1 不可验收。G2 未过。不得报 90%。不得报还原分 / numeric fidelity / T27。**

功能返修把先前 6 个缺口关了 **5 条（核路径）**，第 6 条只关了核/存档、**活编队 UI 仍写死 5**。  
这仍是内部一致性，不是原作还原。

硬否决（同时成立）：

1. primary GT 仍 `SEARCH_IN_PROGRESS` / `CANDIDATE_NEEDS_FETCH`。`docs/reference/gl-shutdown-pve/` **0 mp4**（仅 `README.md` + `FETCH_LOG.txt`）。
2. PlayMode / EditMode = **`NOT_RUN`**。本机仅见 `UnityCrashHandler64`（pid 20340），无活动 Editor。
3. 默认 `dotnet test tools/BattleSim.Tests` **未绿**：110 例中 **1 失败**（并行夹具污染）。关并行后 110 通过，不能升格为原作回归，也不能当「套件稳定绿」。
4. T27–T32、T02、T04–T09、T13、T18（还原）、T21(20 人)、T26 无原作对照或未跑。

`GameRoot` 开战挂 `JP_LEGACY_EMPIRICAL` 是显式对照剖面，**可以**；默认 / `GL_UNKNOWN` 技能结算不再出假 GL 伤。客户端实战打的是 JP 对照数，不是 GL。

---

## 六缺口逐条（源码，不信自述）

| # | 先前缺口 | 本轮 | 判定 |
|---|---|---|---|
| 1 | `GL_UNKNOWN` 实战仍 `ComputeSkill` 改 HP | 技能伤/疗走 `TryResolveCombat` / `Heal` 门闩 | **关闭（技能结算）** |
| 2 | 空/未知 opcode 静默旧路径 | `Cast` 必先 `ExecuteOpcode`；空/未知抛错 + `Failed` | **关闭（Cast）** |
| 3 | Auto/Fever 并进 Tap `tsAmp` | `ExtraDmg` 仅 Tap 加 `tsAmp`；Fever 走 `SkillType.Fever` | **关闭** |
| 4 | Slide 95–105% Variance | `Variance()` 已删；Slide 强制 `variance=1`；实战传入恒 `1f` | **关闭（实战）** |
| 5 | `Self` ≠ caster | `Select(Self)` 取活着的 `caster` | **关闭** |
| 6 | PartyCap/存档/HUD 写死 5 | 核/存档/队长夹取已跟 Length；**活编队 UI 仍 5** | **仍开（UI）** |

### 1. GL_UNKNOWN 是否仍用 JP 式改 HP — **技能结算已关**

默认 `BattleSim.Profile = GL_UNKNOWN`。

实战伤不再直调 `ComputeSkill`：

- `Cast` / `TickFever` → `TryResolveCombat` → `DamageMath.Resolve`
- `Resolve(GL_UNKNOWN)` 返回 `NOT_MEASURED`，`TryResolveCombat` 记 `unresolved`、**不** `ApplyDamage`
- `Heal`：非 `JP_LEGACY_EMPIRICAL` / `KR_LEGACY_REPORTED` 只记 `unresolved`、不改 HP
- 生产脚本里 `ComputeSkill` 的唯二调用点：`DamageMath.Compute`（Tap 助手）与 `Resolve` 内部。`BattleSim` 不直接调

`GameRoot.StartBattleAt` / `MvpLoop` 显式 `Profile = JP_LEGACY_EMPIRICAL`、`Deterministic = true`。对照剖面会出 JP 数，**标成对照即可**，不是默默把 GL 当已测。

仍开的旁路（不是原缺口的 `ComputeSkill`，但会在 `GL_UNKNOWN` 改状态）：

- `ApplyPoisonTriggers` 仍 `MaxHp * Magnitude` 直接 `ApplyDamage`，无剖面门闩。内置表无 `EffectKind.Poison`（`dot_flame` 是 `Dot`，静置不扣血）。
- `ApplyLeader` 在构造里走 `Cast`：`GL_UNKNOWN` 下队长 buff/盾仍上，技能伤/疗被拦住。
- `ApplyEffect` 空 opcode 仍上状态。

`M1RepairQaTests.GlUnknownCastDoesNotApplyJpHpAsSettled` 只覆盖默认剖面 `TryTap` 不掉血。未覆盖 Auto / Fever / 毒。

### 2. 空/未知 opcode 是否还能静默旧路径 — **Cast 已关**

- `Cast`：`skill==null` 或 `ExecuteOpcode(skill.Opcode)` 先于系数路径。空 / 未知 → `Failed` + `UnknownOpcodeException`。
- `Catalog.Sk()` 写 `Opcode = DamageMath.ChannelOpcode(type)`（含 Auto / Heal）。
- `catalog.json` **无 `op` 字段**，`Serialize` 也不写 `op`。现网靠 `BuildBuiltin` + overlay **不覆盖** opcode 才活着；`ReadSkill` / `EnsureSkill` 新建技能 opcode 为空 → **失败关闭**，不再静默旧路径。

仍开：

- 已知 opcode 只 `NoteEvent`，语义仍靠 Type/系数（门闩，不是解释器）。
- `ApplyEffect`：空 opcode 继续旧 `Kind`；只拦非空且未知。
- `M1RepairQaTests.EmptyOrUnknownOpcodeCastFails` **全局改** `C001_tap.Opcode`。默认 xUnit 并行下污染 `M1CoreSliceTests.SlideCdIsIndependentOfCharge`（见 §测试）。

### 3. Auto/Fever 是否还并进 Tap tsAmp — **关闭**

`DamageMath.ExtraDmg`：`tsAmp` 仅 `SkillType.Tap`；Auto / Fever = 0（另加 `skillDefDown` / 克制 `weakDefDown`）。  
`TickFever`：`_activeKind = Fever`，`ExtraDmg(..., Fever)`，`TryResolveCombat(Fever)`，事件 `dmg.fever_parts`。

残留（不是原并进缺口）：Fever 仍借 **Tap 技能系数**（`tap.AtkCoef` / `FlatPower`）；`ComputeFever` 仍是 `ComputeTs * feverMul`；`DamageMath.Compute()` 仍默认 Tap。

### 4. Slide 是否还有 95–105% Variance — **实战已关**

- `BattleSim` 已无 `Variance()`。
- `TryResolveCombat` 传给 `Resolve` 的 variance **恒 1f**。
- `ComputeSkill` 对 Slide **强制 `variance = 1f`**（0.95 / 1.05 与 1.0 同结果）。
- `GameRoot` 开战 `Deterministic = true`（ crit 也不抽）。

U012 仍 UNKNOWN。把区间压成 1 不是 GL 真值，只是不再把均匀 RNG 当 Slide 真值。无 GT 分布。

### 5. Self 是否 = caster — **关闭**

`Select(TargetRule.Self)`：`caster != null && caster.Alive` 则只加 caster，不再 `alive[0]`。  
嘲讽效果另强制 `new[] { caster }`。  
`M1RepairQaTests.SelfTargetsCasterNotAliveZero`：槽 1 治疗 Self，槽 0 HP=1 不变、施法者回血（JP 对照剖面）。

### 6. PartyCap / 存档 / HUD 是否仍写死 5 — **核/档关，活 UI 开**

已跟 `partyIds.Length` / `PartyLength`：

- `FightStats.PartyCap`（默认常数 `DefaultPartyCap=5` 只作空回退）
- `SaveBlob.ProgressForParty` / `Serialize` / `Sanitize` / `ClampLeaderSlot` / `LeaderId`
- `GameRoot.TeamPower` 跟 `PartyIds.Length`

**仍写死 5（活路径）：**

| 位置 | 行为 |
|---|---|
| `TeamBoard.PartySize = 5` | `GameRoot.DrawTeam` / `DrawRoster` **实际编队屏**；槽/战力/`FocusSlot` 截断到 5 |
| `GameRoot.DrawPartyRow` | 已改跟 Length，**无调用点**，修的是死代码 |
| `BattleHud.PartySlots` | 无 `Allies` 时 `return DefaultPartyCap`（5） |
| `NextAutoType` / `Growth.NormalizedReserve` | 5 格 Reserve（预约，不是编队人数） |
| `Catalog.DefaultPartyCore` | M1 默认仍 5 人 |

7 人核能建、档能序列化、槽 6 能入 `AllyDealt`（测到）。编队屏仍 5 格。无 20 人 / 前后排。

---

## 测试原始输出

命令：

```
dotnet test tools/BattleSim.Tests/BattleSim.Tests.csproj --nologo --verbosity minimal
```

cwd `F:\天命之子`，2026-09-10。**默认并行，本轮原始输出：**

```
[Profile] UTF-8 ready
  正在确定要还原的项目…
  所有项目均是最新的，无法还原。
  BattleSim.Tests -> F:\天命之子\tools\BattleSim.Tests\bin\Debug\net6.0\BattleSim.Tests.dll
F:\天命之子\tools\BattleSim.Tests\bin\Debug\net6.0\BattleSim.Tests.dll (.NETCoreApp,Version=v6.0)的测试运行
正在启动测试执行，请稍候...
总共 1 个测试文件与指定模式相匹配。
[xUnit.net 00:00:00.27]     Resonance.Tests.M1CoreSliceTests.SlideCdIsIndependentOfCharge [FAIL]
  失败 Resonance.Tests.M1CoreSliceTests.SlideCdIsIndependentOfCharge [1 ms]
  错误消息:
   Resonance.Battle.UnknownOpcodeException : UNKNOWN_OPCODE <empty>
  堆栈跟踪:
     at Resonance.Battle.BattleSim.ExecuteOpcode(String opcode) ... BattleSim.cs:line 514
     at Resonance.Battle.BattleSim.Cast(...) ... line 573
     at Resonance.Battle.BattleSim.UsePlayerSkill(...) ... line 504
     at Resonance.Battle.BattleSim.TryTap(...) ... line 299
     at Resonance.Tests.M1CoreSliceTests.SlideCdIsIndependentOfCharge() ... M1CoreSliceTests.cs:line 58

失败!  - 失败:     1，通过:   109，已跳过:     0，总计:   110，持续时间: 166 ms - BattleSim.Tests.dll (net6.0)
```

退出码：`1`。

对照（同一 DLL，非默认命令，仅用于定位，**不得替换上面的默认输出**）：

| 命令 | 结果 |
|---|---|
| `--filter FullyQualifiedName~M1RepairQaTests` | 7 通过 |
| `--filter FullyQualifiedName~M1CoreSliceTests` | 8 通过 |
| `-- xUnit.ParallelizeTestCollections=false` | **110 通过 / 0 失败 / 248 ms** |

失败机理：`EmptyOrUnknownOpcodeCastFails` 把共享 `C001_tap.Opcode` 置空；并行类同时 `TryTap` 默认队长。无 `xunit.runner.json`。  
110 = `M1CoreSliceTests` 8 + `IgnitionTests` 6 + `BattleSimTests` 89 + `M1RepairQaTests` 7。  
未生成 trx/XML。未跑 Unity EditMode/PlayMode。

**默认套件未绿。关并行的 110 绿只证明夹具自洽，不能当 M1 / 原作回归。**

---

## T01–T32（功能 vs 还原）

标签：`IMPLEMENTED` / `STUB` / `MISSING` / `NOT_RUN` / `BLOCKED`。  
无 primary GT 时还原列不得标 IMPLEMENTED。

| ID | 项 | 功能 | 还原 | 相对上一轮 |
|---|---|---|---|---|
| T01 | 同 seed 事件哈希 | IMPLEMENTED | BLOCKED | 无变。无 GT 事件真值。 |
| T02 | 30/60/120 渲染同逻辑 | MISSING | BLOCKED | 核仍锁 30Hz。 |
| T03 | Auto / 充能 / SlideCd 分时钟 | IMPLEMENTED | BLOCKED | 秒数仍 UNKNOWN。默认套件里 SlideCd 测 flaky。 |
| T04 | 暂停 / 加速 / QTE / Fever 时钟 | STUB | BLOCKED | 无变。 |
| T05 | 采样不重复 Tap；上滑不误触 | STUB | BLOCKED | PlayMode `NOT_RUN`。 |
| T06 | 预约 / 自动次序 / 取消 | STUB | BLOCKED | Reserve 仍 5 格。 |
| T07 | 最低 HP 绝对 vs 比例 | STUB | BLOCKED | 仍只比绝对值。 |
| T08 | 多目标 / 多段重选 | STUB | BLOCKED | 无变。 |
| T09 | 嘲讽 / 免疫 / 不可选 | STUB | BLOCKED | 嘲讽有；免疫/不可选无。 |
| T10 | 毒：行动/受击 ≠ 每秒 DoT | IMPLEMENTED | BLOCKED | 受击断言仍被 JP Tap 伤污染。U014。 |
| T11 | 护盾/穿防/反射/吸血/死亡触发 | STUB | BLOCKED | 无变。 |
| T12 | 堆叠 / 控制中充能 | STUB | BLOCKED | 无变。 |
| T13 | 复活 / 迟到事件 | MISSING | BLOCKED | `revive` 仍只是 opcode 名。 |
| T14 | S 与固有/装备 ATK 不重复 | STUB | BLOCKED | 无变。 |
| T15 | Tap / Slide / Auto 不混 | IMPLEMENTED | BLOCKED | ExtraDmg/Fever 通道已拆。Fever 仍借 Tap 系数。 |
| T16 | 属性 / 暴击 / Fever 例外 | STUB | BLOCKED | Fever 通道已标，例外无 GT。 |
| T17 | 额外 vs 穿防 | STUB | BLOCKED | 实战少传 pierce。 |
| T18 | Slide 区间 ≠ 均匀 RNG | IMPLEMENTED | BLOCKED | 实战不再乘 95–105%。分布仍 U012。 |
| T19 | 点火 / 增幅独立 | STUB | BLOCKED | U006。 |
| T20 | 舍入 / 多段 / 零血护盾 | STUB | BLOCKED | 无变。 |
| T21 | 5 人与 20 人前后排 | STUB | BLOCKED | 核可变；编队屏仍 5；无 20 人。 |
| T22 | 超时 / 退出 / 失败 / 跨波 | STUB | BLOCKED | opcode 失败现会 `Failed`。 |
| T23 | 奖励/票只一次 | STUB | BLOCKED | 无变。 |
| T24 | 锁定角色不当材料 | MISSING | BLOCKED | 无变。 |
| T25 | 坏档 / schema / 日切 | STUB | BLOCKED | 无变。 |
| T26 | 入口→编队→战斗→结算→再战 | NOT_RUN | BLOCKED | 需 PlayMode。 |
| T27 | UI 叠图误差 | NOT_RUN | BLOCKED | 仍 `NEEDS_REFERENCE`。 |
| T28 | 输入/命中/数字 ≤2 参考帧 | NOT_RUN | BLOCKED | 无 GT。 |
| T29 | 遮挡/层级人工复核 | NOT_RUN | BLOCKED | |
| T30 | 无原画只验未遮罩区 | NOT_RUN | BLOCKED | |
| T31 | 真机帧率/内存 | NOT_RUN | BLOCKED | 无真机。 |
| T32 | 无异常日志 / 进出释放 / 20 人压力 | NOT_RUN | BLOCKED | |

**T 还原通过数：0。**  
功能 IMPLEMENTED：T01 / T03 / T10 / T15 / T18（均内部一致性）。上一轮 T15/T18 为 STUB/MISSING。

---

## G2 切片

| 切片 | 功能 | 还原 | 笔记 |
|---|---|---|---|
| Auto | 近 IMPLEMENTED | BLOCKED | 独立通道 + 不进 tsAmp。`GL_UNKNOWN` 不结算伤。 |
| Tap | IMPLEMENTED（能打） | BLOCKED | 默认不结算；客户端 JP 对照才打。 |
| Slide | IMPLEMENTED（CD + 无均匀 RNG） | BLOCKED | 8s 未知；区间压成 1 ≠ GL。 |
| Drive + QTE | STUB | BLOCKED | 倍率 0.9/1.2/1.5 无 GT。 |
| Fever | STUB | BLOCKED | 通道已拆；仍 7s/70 hit；借 Tap 系数。 |
| 控制 / 护盾 / 换目标 | STUB | BLOCKED | Self=caster 已修。无 retarget 体。 |
| 死亡 / 换波 / 结算 | STUB | BLOCKED | 无变。 |
| 暂停 / 加速 / 自动 | STUB | BLOCKED | 无变。 |

G2 要「全部关键状态 + 对照视频」。**不满足。**

---

## BLOCKED

| ID | 阻断 | 状态 |
|---|---|---|
| U001 | primary GT 普通 5 人 PVE | `SEARCH_IN_PROGRESS`；`gl-shutdown-pve` 0 录像 |
| U002 | GL Tap/Slide/Auto/Fever 公式 | UNKNOWN；默认不再冒充已测；客户端 JP 对照会出数 |
| U003 | Drive 实际伤害 | UNKNOWN |
| U006 | Ignition/增幅组合 | UNKNOWN |
| U010 | 参考画幅 | `NOT_MEASURED` → 禁 T27 |
| U012 | Slide 随机分布 | 实战不再均匀抽样；真值仍 UNKNOWN |
| U013 | PlayMode 基线 | `NOT_RUN`（仅 CrashHandler） |
| U014 | 毒触发帧距 | UNKNOWN；`GL_UNKNOWN` 毒仍可改 HP |
| — | EditMode | `NOT_RUN` |
| — | 默认 `dotnet test` | **1 失败 / 110**（并行改全局 Catalog） |
| — | 编队 HUD | `TeamBoard.PartySize=5` |
| — | 契约 JSON 漂移 | `ClockPolicy.json.slide_cd_present=false`；`EffectSchema.json.poison.implemented_in_engine=false` |

---

## 未做 / 边界

- 未改 `client/`、`ProjectSettings`、`Packages`、`TARGET.json`。
- 未宣称 fidelity、未开 G3、未升级 Unity。
- 未把关并行的 110 绿写成默认套件绿。
- `G1` JSON 与代码不一致只记录。

## 结论句

返修关闭了：`GL_UNKNOWN` 技能结算假 GL 伤、Cast 空/未知 opcode 静默、Auto/Fever 并进 `tsAmp`、Slide 均匀 95–105%、`Self≠caster`。  
仍开：`TeamBoard` 写死 5；毒/空效果 opcode 旁路；已知 opcode 只记账；默认测试套件 flaky。  
无 GT、无 PlayMode、还原 0。**M1 不能验收。**
