# M1-G2-QA-VERIFY — 独立验收

- 任务：`M1-G2-QA-VERIFY`（本文件为返修**前**审计底稿）
- 返修后独立复核：见同目录 [`QA_REPAIR.md`](QA_REPAIR.md)（任务 `M1-G2-QA-REPAIR`）。**六缺口与 T01–T32 以 `QA_REPAIR.md` 为准。**
- 角色：`dc_qa`（只读生产代码；本文件为 G0 QA 报告）
- 日期：2026-09-10
- 对照：`STATUS.md` 本轮声称、`G1` 契约、`04_ACCEPTANCE_AND_TESTS.md` T01–T32、G2 切片
- **未改** `TARGET.json`（含 `region`）、未改生产代码、未 spawn、未开 G3

## 返修后指针（2026-09-10，`M1-G2-QA-REPAIR`）

**M1 仍不可验收。G2 未过。不得报 90%。**  
核路径关闭 1–5：`GL_UNKNOWN` 技能结算不再 `ComputeSkill` 改 HP；Cast 空/未知 opcode 失败关闭；Auto/Fever 不再并进 Tap `tsAmp`；Slide 不再乘 95–105%；`Self=caster`。  
第 6 条仍开：活编队 UI `TeamBoard.PartySize=5`（`DrawPartyRow` 已改但是死代码）。  
默认 `dotnet test tools/BattleSim.Tests`：**1 失败 / 110**（并行污染 `C001_tap.Opcode`）。关并行后 110 绿 ≠ 还原。T 还原通过数仍 0。

## 总判（必须先读）— 返修前底稿

**M1 不可验收。G2 未过。不得报 90%。不得报还原分 / numeric fidelity / T27。**

硬证据（任一即可否决；下列同时成立）：

1. primary GT 仍 `SEARCH_IN_PROGRESS` / `CANDIDATE_NEEDS_FETCH`。`docs/reference/gl-shutdown-pve/` **0 mp4**（仅 `README.md` + fetch 失败日志）。
2. PlayMode / EditMode = **`NOT_RUN`**。本机仅见 `UnityCrashHandler64`（pid 20340），无活动 Editor。
3. 实战结算路径在 `Profile=GL_UNKNOWN` 下仍调用 `DamageMath.ComputeSkill`（JP-like wiki 式）并改 HP，不是 `Resolve` 的 `NOT_MEASURED` 门闩。这是静默 JP 当 GL。
4. 客户端开战 `GameRoot` 设 `Deterministic = false`，`Variance()` = **95–105%** 打在含 Slide 的所有 `ComputeSkill` 上。G1 禁止把 Slide 区间当均匀 RNG 真值。
5. T27–T32、T02、T04–T09、T13、T18、T21(20 人)、T26 无原作对照或未跑。

`dotnet test` 103 通过只证明内部夹具自洽，**不能**升格为 M1 / 原作回归。

---

## 1. 声称核对

判定：`成立` / `部分成立` / `不成立`。功能实现 ≠ 还原通过。

| # | 声称 | 判定 | 证据 |
|---|---|---|---|
| 1 | SlideCd 独立；受控下充能停、SlideCd 仍走 | **成立（功能）** | `UnitState.SlideCd`；`TickSlideClocks` 在 `TickUnit` 外；`ControlPausesChargeButSlideCdContinues` 覆盖。秒数 `UnknownSlideCdSec=8f` = UNKNOWN，非 GL。G1 `ClockPolicy.json` 仍写 `"slide_cd_present": false`（契约 JSON 未跟上代码）。 |
| 2 | Auto ≠ Tap | **部分成立** | `ComputeSkill` 有 `ComputeAuto`；`ChannelOpcode` 分 `dmg.auto` / `dmg.tap`。**缺口：** `ExtraDmg` 把 Auto 并进 `tsAmp`（与 Tap 同支）；`TickFever` 用 `SkillType.Tap` + `ComputeSkill(Tap)`；`DamageMath.Compute()` 默认 Tap。 |
| 3 | 未知 opcode → Failed | **部分成立** | `ExecuteOpcode` / 带 `EffectDef.Opcode` 的 `ApplyEffect` 会 `Failed` + 抛错。`Catalog.Sk()` **从不写** `SkillDef.Opcode`；`Cast` 仅在 Opcode 非空时检查 → 内置技能静默走旧 Type/系数路径。已知 opcode 的 `ExecuteOpcode` **只记事件，不执行语义**。 |
| 4 | 毒 = on_action / on_hit_taken；禁每秒 DoT | **部分成立** | `TickStatus` 只减时长；`ApplyPoisonTriggers` 走行动/受击。静置 2s HP 不变（测到）。**缺口：** `PoisonTriggers…` 的受击断言被 Tap 伤污染，未隔离 `on_hit_taken`；内置 Catalog 无 poison 效果；帧距 = U014 UNKNOWN。 |
| 5 | 可变 party（非写死 5） | **部分成立** | `Allies = new UnitState[partyIds.Length]`；HUD `PartySlots()` 跟 Length；`PartyCapacityIsNotHardcodedFive` 过。**仍写死 5：** `FightStats.PartyCap`、`SaveBlob.ProgressForParty`、`GameRoot.DrawPartyRow` / `TeamPower` / `LeaderSlot` clamp `0..4`、Reserve 5 格（另一概念）。7 人核可建，入口/统计/存档成长仍按 5。 |
| 6 | 事件日志可哈希 | **成立（功能）** | `BattleEventLog.ComputeHash` SHA256；`EventLogHashIsStableForSameSeed` 同 seed 90 tick 一致。`SameSeedSameLog` 只比 `Outcome/HP/Drive/TimeLeft`，不是事件哈希。无 GT 日志 → 还原 BLOCKED。 |
| 7 | 默认 `GL_UNKNOWN`；禁 `GL_FINAL_VERIFIED` | **成立（字段/枚举/Resolve）** | `BattleSim.Profile` 默认 `GL_UNKNOWN`；枚举无 `*_FINAL_VERIFIED`；`Resolve(GL_UNKNOWN)` 返回 `NOT_MEASURED`。 |
| 8 | 无静默 JP 当 GL | **不成立** | 实战 `Cast` / `TickFever` 不走 `Resolve`，直接 `ComputeSkill` 改 HP。事件标 `FormulaStatus.NotMeasured`，数字仍是 JP-like 式。 |
| 9 | `BattleSimTests` 去掉 `catalog.json` 写盘 | **成立** | 套件内无 `catalog.json` 写入。`CatalogJsonRoundtrip` / `Load` 只用内存字符串。`CorruptPrimaryFallsBackToBak` 写的是 `%TEMP%` 存档。本轮后 `client/Assets/Content/catalog.json` mtime 仍为 **2026-09-09 19:26**。 |
| 10 | `dotnet test` 103 通过 | **成立（计数）** | 本轮复跑 **103 通过 / 0 失败 / 0 跳过**。声称时长 159 ms，本轮 **184 ms**（计时差，非计数差）。 |
| 11 | 竖屏 HUD 多域；Tap/Slide 分离；Slide 就绪跟 SlideCd；布局 `NEEDS_REFERENCE` | **部分成立** | `BattleHudState.Collect` 多旗；`PortraitGesture` `dy>80` 上滑否则 Tap；`slideReady = tapReady && SlideCd<=0`；`LayoutStatus = "NEEDS_REFERENCE"`。未覆盖契约全部域（缺 `ActionQueued` / `Casting` / `Resolve` 等）。手势阈值未测。PlayMode `NOT_RUN`。 |
| 12 | `ICharacterPresentation` 适配现有骨骼；cue 跟事件；伤害不绑动画结束 | **部分成立** | Adapter + `BindExisting` 存在；`Cast` 同步 `ApplyDamage`（逻辑不绑 Animator）。Cue 跟的是 `FloatText`/`CastFx` 排水，不是 `BattleEventLog`。`AttackTrail._hit` 只打 VFX。 |

---

## 2. 测试原始输出

命令（未筛掉 CatalogJson 用例：它们不写盘）：

```
dotnet test tools/BattleSim.Tests/BattleSim.Tests.csproj --nologo --verbosity minimal
```

本机原始输出（2026-09-10，cwd `F:\天命之子`）：

```
[Profile] UTF-8 ready
  正在确定要还原的项目…
  所有项目均是最新的，无法还原。
  BattleSim.Tests -> F:\天命之子\tools\BattleSim.Tests\bin\Debug\net6.0\BattleSim.Tests.dll
F:\天命之子\tools\BattleSim.Tests\bin\Debug\net6.0\BattleSim.Tests.dll (.NETCoreApp,Version=v6.0)的测试运行
正在启动测试执行，请稍候...
总共 1 个测试文件与指定模式相匹配。

已通过! - 失败:     0，通过:   103，已跳过:     0，总计:   103，持续时间: 184 ms - BattleSim.Tests.dll (net6.0)
```

退出码：`0`。未生成 trx/XML（本轮只留 stdout）。未跑 Unity EditMode/PlayMode。未跑会写 `catalog.json` 的路径。

103 = `M1CoreSliceTests` 8 + `IgnitionTests` 6 + `BattleSimTests` 89。

---

## 3. T01–T32（功能 vs 还原）

标签：`IMPLEMENTED` / `STUB` / `MISSING` / `NOT_RUN` / `BLOCKED`。  
还原列在无 primary GT 时不得标 IMPLEMENTED。

| ID | 项 | 功能 | 还原 | 说明 |
|---|---|---|---|---|
| T01 | 同 seed 事件哈希 | IMPLEMENTED | BLOCKED | 有 `ComputeHash` + 同 seed 测。无 GT 事件真值。 |
| T02 | 30/60/120 渲染同逻辑 | MISSING | BLOCKED | 核锁 30Hz。无跨渲染帧率测。 |
| T03 | Auto / 充能 / SlideCd 分时钟 | IMPLEMENTED | BLOCKED | 三套计时器已拆。秒数/原作帧距未测。 |
| T04 | 暂停 / 加速 / QTE / Fever 时钟 | STUB | BLOCKED | `Paused` 冻整核；`Speed` 乘 `TickDt`；无 `Time.timeScale`（好）。QTE/Fever 有骨架，无分时钟对照。暂停时 HUD QTE 用 `unscaledDeltaTime`，sim 侧不走。 |
| T05 | 采样不重复 Tap；上滑不误触 | STUB | BLOCKED | 手势已分。`dy<=80` 仍当 Tap。无采样率/双触测。PlayMode `NOT_RUN`。 |
| T06 | 预约 / 自动次序 / 取消 | STUB | BLOCKED | `AutoFireSkills` + 5 格 Reserve。无 `ActionQueued` 域。 |
| T07 | 最低 HP 绝对 vs 比例 | STUB | BLOCKED | 只比 `a.Hp` 绝对值。 |
| T08 | 多目标 / 多段重选 | STUB | BLOCKED | 每 hit 重 `PickFoes`。无死亡中途规范。 |
| T09 | 嘲讽 / 免疫 / 不可选 | STUB | BLOCKED | 有 Taunt 优先。无免疫/不可选。 |
| T10 | 毒：行动/受击 ≠ 每秒 DoT | IMPLEMENTED | BLOCKED | 触发路径在。U014。受击测未隔离。 |
| T11 | 护盾/穿防/反射/吸血/死亡触发 | STUB | BLOCKED | 护盾扣血有。Cast 默认 `pierce=0`。反射/吸血/复活无执行体。 |
| T12 | 堆叠 / 控制中充能 | STUB | BLOCKED | Group 替换；Stun 清 Charge；控制停充能。刷新/上限未闭合。 |
| T13 | 复活 / 迟到事件 | MISSING | BLOCKED | `revive` 仅 opcode 名。 |
| T14 | S 与固有/装备 ATK 不重复 | STUB | BLOCKED | `SkillDmg=atk*coef+flat`。有成长/装备测，不证明 S 通道。 |
| T15 | Tap / Slide / Auto 不混 | STUB | BLOCKED | 主公式已分。ExtraDmg/Fever/默认 `Compute` 仍混。 |
| T16 | 属性 / 暴击 / Fever 例外 | STUB | BLOCKED | 有元素/暴击/`FeverMul`。Fever 用 Tap 通道。 |
| T17 | 额外 vs 穿防 | STUB | BLOCKED | math 有 `TruePierce`/`ExtraDmg`。实战少传 pierce。 |
| T18 | Slide 区间 ≠ 均匀 RNG | MISSING | BLOCKED | 见回归 §4。U012。 |
| T19 | 点火 / 增幅独立 | STUB | BLOCKED | Ignition extra mul 有。U006 UNKNOWN。 |
| T20 | 舍入 / 多段 / 零血护盾 | STUB | BLOCKED | `AwayFromZero`；护盾吸收有。无临界全表。 |
| T21 | 5 人与 20 人前后排 | STUB | BLOCKED | 核数组可变。无 20 人/前后排。存档/UI 仍 5。 |
| T22 | 超时 / 退出 / 失败 / 跨波 | STUB | BLOCKED | `TimeLeft` 败；两波；`Failed` 主要服务 opcode。 |
| T23 | 奖励/票只一次 | STUB | BLOCKED | 有存档奖励测，非战斗切片幂等证明。 |
| T24 | 锁定角色不当材料 | MISSING | BLOCKED | 养成。分母保留。 |
| T25 | 坏档 / schema / 日切 | STUB | BLOCKED | `CorruptPrimaryFallsBackToBak` 功能向。 |
| T26 | 入口→编队→战斗→结算→再战 | NOT_RUN | BLOCKED | 需 PlayMode。 |
| T27 | UI 叠图误差 | NOT_RUN | BLOCKED | 布局已标 `NEEDS_REFERENCE`。未伪称 pass。 |
| T28 | 输入/命中/数字 ≤2 参考帧 | NOT_RUN | BLOCKED | 无 GT、无逐帧。 |
| T29 | 遮挡/层级人工复核 | NOT_RUN | BLOCKED | |
| T30 | 无原画只验未遮罩区 | NOT_RUN | BLOCKED | |
| T31 | 真机帧率/内存 | NOT_RUN | BLOCKED | 无真机。 |
| T32 | 无异常日志 / 进出释放 / 20 人压力 | NOT_RUN | BLOCKED | |

**T 还原通过数：0。** 功能 IMPLEMENTED 仅 T01 / T03 / T10（均内部一致性）。

---

## 4. G2 切片（有 GT 之后才谈还原）

来源：`03_MULTIAGENT` G2、`NEXT_WAVE`。

| 切片 | 功能 | 还原 | 笔记 |
|---|---|---|---|
| Auto | STUB→近 IMPLEMENTED | BLOCKED | 独立计时 + `ComputeAuto`。Amp/Fever 仍偏 Tap。 |
| Tap | IMPLEMENTED（能打） | BLOCKED | 公式 = JP-like，不是 GL。 |
| Slide | IMPLEMENTED（独立 CD + Ss 式） | BLOCKED | 8s 未知；实战 95–105% RNG。 |
| Drive + QTE | STUB | BLOCKED | `TryBeginDrive` / `ResolveDrive` / 1.2s 超时有。倍率 0.9/1.2/1.5 无 GT。 |
| Fever | STUB | BLOCKED | 写死 7s / 70 hit；伤害走 Tap。 |
| 控制 / 护盾 / 换目标 | STUB | BLOCKED | Stun/Shield/Taunt 有。`TargetRule.Self` ≠ caster。无 retarget opcode 体。 |
| 死亡 / 换波 / 结算 | STUB | BLOCKED | 两波 + Victory/Defeat。无迟到事件锁。 |
| 暂停 / 加速 / 自动 | STUB | BLOCKED | 加速乘 tick。暂停冻整核。自动有 Manual/Full。档位 = U011 UNKNOWN。 |

G2 通过条件要「全部关键状态 + 对照视频」。**当前不满足。**

---

## 5. 回归（点名项）

### 5.1 静默 fallback — **仍在**

- 空 `SkillDef.Opcode` → `Cast` 不 `ExecuteOpcode`。内置表全空。
- `GL_UNKNOWN` 实战仍 `ComputeSkill` 出伤。
- `ExtraDmg(Auto)` → `tsAmp`。
- `TickFever` → Tap 通道。
- `Catalog.TryChar` 失败则该槽 `null`，不失败战斗。
- 已知 opcode 只 `NoteEvent`，语义靠旧字段。

### 5.2 95–105% Slide RNG — **仍在（实战必中）**

```846:846:client/Assets/Scripts/Resonance.Battle/Core/BattleSim.cs
        float Variance() => 0.95f + (float)_rng.NextDouble() * 0.10f;
```

`Cast` 在 `!Deterministic` 时把 `Variance()` 乘进所有 `ComputeSkill`（含 Slide）。  
`GameRoot` 开战：`Deterministic = false`。  
单测一律 `Deterministic = true`，**测不到**。违反 `FormulaProfiles.slide_uniform_rng_as_truth_forbidden`。

### 5.3 `Self` ≠ caster — **仍在**

`Select(TargetRule.Self)` 取 `alive[0]`（该侧第一个活着的），不接收 caster。治疗/对己效果会打错人。

### 5.4 写死 `[5]` — **核数组已松，周围未松**

| 位置 | 行为 |
|---|---|
| `FightStats.PartyCap = 5` | 槽 ≥5 的伤害/治疗不入账 |
| `SaveBlob.ProgressForParty` | `new UnitProgress[5]` |
| `GameRoot.DrawPartyRow` / `TeamPower` | `for i < 5` |
| `LeaderSlot` clamp `0..4` | 7 人队长越界 |
| `NextAutoType` / `Growth.NormalizedReserve` | 5 格 Reserve（自动预约，不是编队人数） |
| `BattleHud.PartySlots` fallback | `return 5` |

### 5.5 伤害绑动画结束 — **未复现为逻辑依赖**

`ApplyDamage` 在 `Cast` 同 tick。表现后置。**此项声称成立。**

---

## 6. 阻断项

| ID | 阻断 | 状态 |
|---|---|---|
| U001 | primary GT 普通 5 人 PVE | `SEARCH_IN_PROGRESS`；`gl-shutdown-pve` 0 录像 |
| U002 | GL Tap/Slide/Auto/Fever 公式 | UNKNOWN；实战却在出 JP-like 数 |
| U003 | Drive 实际伤害 | UNKNOWN |
| U006 | Ignition/增幅组合 | UNKNOWN |
| U010 | 参考画幅 | `NOT_MEASURED` → 禁 T27 |
| U012 | Slide 随机分布 | 实战仍均匀 95–105% |
| U013 | PlayMode 基线 | `NOT_RUN` |
| U014 | 毒触发帧距 | UNKNOWN |
| — | EditMode | `NOT_RUN` |
| — | 契约 JSON 漂移 | `ClockPolicy.json.slide_cd_present=false`；`EffectSchema.json.poison.implemented_in_engine=false` |

---

## 7. 未做 / 边界

- 未改 `client/`、`ProjectSettings`、`Packages`、`TARGET.json`。
- 未宣称 fidelity、未开 G3、未升级 Unity。
- 未人工逐条重跑 103 个用例名以外的断言内容（以 stdout + 源码审计为准）。
- `G1` JSON 与代码不一致只记录，不在本任务改契约。

## 8. 结论句（返修前底稿）

续跑把 SlideCd / 毒触发 / opcode 失败 API / 事件哈希 / `Allies` 变长 / HUD 手势拆分 **往前推了一步**，内部 103 测也复现通过。  
**当时：** 声称「无静默 JP 当 GL」不成立；Slide RNG、Self、写死 5、空 opcode 静默仍在。  
无 GT、无 PlayMode、无还原样本。**M1 不能验收。**

返修后结论见 `QA_REPAIR.md`：上列 1–5 核路径已关，编队 UI 写死 5 与默认测试套件 flaky 仍开。**M1 仍不能验收。**
