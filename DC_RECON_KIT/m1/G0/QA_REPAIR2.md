# M1-G2-QA-REPAIR2 — 独立返修-2 复核

- 任务：`M1-G2-QA-REPAIR2`
- 角色：`dc_qa`（只读生产代码；本文件为 G0 QA 报告）
- 日期：2026-09-10
- 对照：`QA_REPAIR.md` 仍开项、返修-2 的 BattleSim / TeamBoard / Catalog / `M1RepairQaTests`
- **未改** 生产代码、`TARGET.json`、未 spawn、未开 G3
- **不采信** 实现代理自述 / `STATUS.md` 本轮声称 / 「112 绿 = M1 过」

## 总判（必须先读）

**M1 不可验收。G2 未过。不得报 90%。不得报还原分 / numeric fidelity / T27。**

返修-2 把上一轮仍开的核路径关了 3 条：**默认套件并行污染（opcode 就地改坏）**、**`ApplyEffect` 空/未知静默**、**`GL_UNKNOWN` 毒 MaxHp% 改 HP**。  
`TeamBoard` 活编队**不再按 5 截断槽位**；默认仍是 5 人，无 20 人/前后排。

这仍是内部一致性。硬否决同时成立：

1. primary GT 仍 `SEARCH_IN_PROGRESS` / `CANDIDATE_NEEDS_FETCH`。`docs/reference/gl-shutdown-pve/` **0 mp4**（仅 `README.md` + `FETCH_LOG.txt`）。
2. PlayMode / EditMode = **`NOT_RUN`**。本机仅见 `UnityCrashHandler64`（pid 20340），无活动 Editor。
3. 默认 `dotnet test` 本轮 **0 失败 / 112**（见 §测试）。绿的是夹具自洽，**不是**原作回归，**不能**升格为 M1 验收。
4. T27–T32、T02、T04–T09、T13、T18（还原）、T21(20 人)、T26 无原作对照或未跑。

`GameRoot.StartBattleAt` 仍显式 `Profile = JP_LEGACY_EMPIRICAL`、`Deterministic = true`。客户端实战打的是 JP 对照数，不是 GL。

---

## 六问逐条（源码，不信自述）

| # | 问题 | 本轮 | 判定 |
|---|---|---|---|
| 1 | 默认 `dotnet test tools/BattleSim.Tests` 是否 0 失败 | 连跑 3 次均为 0 失败 / 112 通过 / exit 0 | **关闭（默认套件绿）**；不是原作回归 |
| 2 | 全局 Catalog 是否还会被测例改坏 | opcode/Target 就地改坏已隔离；`CatalogJson.Load`/`BuildBuiltin` 仍换全局表 | **关闭（就地污染）**；换表竞态仍开 |
| 3 | `TeamBoard` 活编队是否仍写死 5 | 槽/战力/焦点跟 `LivePartyCap`；`PartySize` 常数仍 5（间距） | **关闭（截断）**；默认 5 / 无 20 人仍开 |
| 4 | `ApplyEffect` 空/未知 opcode 是否还静默 | 先 `ExecuteOpcode`；空/未知抛错 + `Failed`，不上状态 | **关闭** |
| 5 | `GL_UNKNOWN` 毒 MaxHp% 是否还改 HP | 非 JP/KR 只记 `unresolved`，不 `ApplyDamage` | **关闭（HP）** |
| 6 | 已知 opcode 只记账 / Fever 借 Tap / 契约 JSON 漂移 | 三处源码与 JSON 仍在 | **仍开（属实）** |

### 1. 默认 `dotnet test` — **本轮 0 失败**

命令（cwd `F:\天命之子`）：

```
dotnet test tools/BattleSim.Tests/BattleSim.Tests.csproj --nologo --verbosity minimal
```

**第一次默认并行，本轮原始输出：**

```
[Profile] UTF-8 ready
  正在确定要还原的项目…
  所有项目均是最新的，无法还原。
  BattleSim.Tests -> F:\天命之子\tools\BattleSim.Tests\bin\Debug\net6.0\BattleSim.Tests.dll
F:\天命之子\tools\BattleSim.Tests\bin\Debug\net6.0\BattleSim.Tests.dll (.NETCoreApp,Version=v6.0)的测试运行
正在启动测试执行，请稍候...
总共 1 个测试文件与指定模式相匹配。

已通过! - 失败:     0，通过:   112，已跳过:     0，总计:   112，持续时间: 121 ms - BattleSim.Tests.dll (net6.0)
```

退出码：`0`。

同命令再跑 2 次（默认并行，不关并行、不加 filter）：`0` 失败 / 112 / 126 ms；`0` 失败 / 112 / 127 ms。退出码均 `0`。

`--list-tests` 点名 112 例：`BattleSimTests` 89 + `IgnitionTests` 6 + `M1CoreSliceTests` 8 + `M1RepairQaTests` 9。上一轮 110 = 前三类不变 + 返修测 7；本轮多 2 条：`EmptyOrUnknownOpcodeApplyEffectFails`、`GlUnknownPoisonDoesNotApplyMaxHpAsSettled`。

无 `xunit.runner.json`。未生成 trx/XML。未跑 Unity EditMode/PlayMode。

**不得把 112 绿写成 M1 / 原作回归。** 只证明默认并行下这套夹具本轮没炸。

### 2. 全局 Catalog 并行隔离 — **就地改坏已关；换表仍开**

上一轮失败机理：`EmptyOrUnknownOpcodeCastFails` 把共享 `C001_tap.Opcode` 置空，并行类 `TryTap` 吃到空 opcode。

返修-2：

- `Catalog.CloneSkill` 拷贝含 `Opcode`
- `BattleSim.OverlaySkill` / `ResolveSkill`：技能覆盖在**该实例**字典，不写回 `Catalog.Skills`
- `EmptyOrUnknownOpcodeCastFails` / `SelfTargetsCasterNotAliveZero` 改的是 clone；结束后断言全局 `C001_tap.Opcode` / `C003_tap.Target` 未变
- Auto / Drive / Fever / 队长技也走 `ResolveSkill`，overlay 对那条 sim 生效

无 `CollectionBehavior` / `xunit.runner.json`。全局表仍会被换：

| 路径 | 行为 |
|---|---|
| `Catalog.BuildBuiltin` | `Install` 换 Characters/Skills/Effects |
| `CatalogJson.Load` | 先 `BuildBuiltin`，再 overlay，再 `Install` |
| `CatalogJsonFillsEmptySliceFromBuiltin` / `CatalogJsonRoundtrip` | 测例仍走上述全局 Load |
| `catalog.json` | 无 `op` 字段；`Serialize` 技能/效果都不写 `op`。现网靠 builtin + overlay **不覆盖** opcode。`ReadSkill` 新建技能 `Opcode=""` → Cast 失败关闭 |

本轮默认并行 3 次未见 `UNKNOWN_OPCODE <empty>`。就地改 opcode 的污染路径关闭。  
`Load`/`Install` 换引用的竞态没有夹具锁，**不能**写成「Catalog 已线程安全」。

### 3. `TeamBoard` 活编队是否仍写死 5 — **槽截断已关**

活路径跟 `save.LivePartyCap`（`FightStats.CapForLength(PartyLength)`）：

- `DrawTeam` / `DrawRoster` / `DrawParty`：`n = LivePartySize`，`for i < n`，不再 `i < 5`
- `TeamPower` / `FocusSlot` / `InParty`：同样跟 `LivePartySize`
- `GameRoot.DrawTeam` 仍只调 `TeamBoard.DrawTeam`（活编队屏）
- `GameRoot.TeamPower` / `DrawPartyRow` 跟 `PartyIds.Length`；`DrawPartyRow` **仍无调用点**

`PartySize = FightStats.DefaultPartyCap`（5）还在，**只当间距阈值**：`n <= PartySize ? 0.17f : 0.76f/n`。7 人档会画 7 槽，间距变窄，不是截成 5。

测到：`PartyCapSaveAndStatsFollowPartyLength`、`PartyCapacityIsNotHardcodedFive`（核 3/7）。

仍是 5 / 不是 20 人：

| 位置 | 行为 |
|---|---|
| `Catalog.DefaultPartyCore` | M1 默认仍 5 人 |
| `SaveBlob.PartyIds` 初值 | `DefaultParty` 克隆；`SetPartySlot` **不能扩长** |
| `BattleHud.PartySlots` | 无 `Allies` 时回退 `DefaultPartyCap`（5） |
| `NextAutoType` / `Growth.NormalizedReserve` | 预约 **5 格**（不是编队人数） |
| T21 | 无 20 人、无前后排目标规则 |

上一轮「活编队 UI 截断到 5」关闭。默认开档仍 5 人；无扩编 UI。**不是 T21 还原。**

### 4. `ApplyEffect` 空/未知是否还静默 — **关闭**

```
ApplyEffect → ExecuteOpcode(fx.Opcode)
ExecuteOpcode：空或不在 Known → FailUnknownOpcode + throw UnknownOpcodeException
已知 → 只 NoteEvent("opcode", …)
```

`EmptyOrUnknownOpcodeApplyEffectFails`：空 opcode 的 Stun **不上**；未知 opcode 的 AtkBuff **不上**；`Outcome=Failed`。  
`Cast` 仍先 `ExecuteOpcode(skill.Opcode)`。空/未知不再静默走 Kind。

### 5. `GL_UNKNOWN` 毒 MaxHp% 是否还改 HP — **关闭（HP）**

`ApplyPoisonTriggers`：剖面不是 `JP_LEGACY_EMPIRICAL` / `KR_LEGACY_REPORTED` 时 `NoteEvent("unresolved", poison.apply)` 并 `continue`，**不** `MaxHp * Magnitude`、**不** `ApplyDamage`。

默认 `Profile = GL_UNKNOWN`。`GlUnknownPoisonDoesNotApplyMaxHpAsSettled`：上毒后 `TryTap`，施法者 HP 不变；有 `unresolved`+`poison.apply`+`NotMeasured`；无 `hit`+`poison.apply`。

`Cast` 在结算前仍 `TriggerPoisonOnAction`，所以这条测的是毒门闩，不是「Tap 也没伤所以 HP 碰巧不变」。  
JP 对照剖面仍按 MaxHp% 扣（`PoisonTriggersOnActionAndHitTakenNotPerSecond`）。U014 时序仍 UNKNOWN。

状态仍会挂上（`poison.apply` 是已知 opcode，`ApplyEffect` 会入 `Status`）。关的是 **改 HP**，不是「毒完全不存在」。

### 6. 仍开项是否属实 — **属实**

**已知 opcode 只记账。** `ExecuteOpcode` 对 Known 表只 `NoteEvent`。伤/疗/盾/毒触发仍看 `SkillType` / 系数 / `EffectKind`。`revive` / `retarget` / `dmg.pierce` 等有名字、无解释器体。门闩，不是 opcode 语义机。

**Fever 仍借 Tap 系数。** `TickFever`：`ResolveSkill(TapSkillId)`，把 `tap.AtkCoef` / `FlatPower` / `PercentAtk` / `SkillFlat` 送进 `TryResolveCombat(Fever, …, FeverMul)`。`DamageMath.ComputeFever` = `ComputeTs * feverMul`。通道已拆（`ExtraDmg` 不给 Fever 加 `tsAmp`；事件 `dmg.fever_parts` / `SkillType.Fever`），系数源仍是 Tap。

**契约 JSON 漂移。**

| 契约 | 字段 | 代码 |
|---|---|---|
| `ClockPolicy.json` | `slide_cd_present: false`；`slide_cd_decision: REPAIR` | `BattleSim` 有独立 `SlideCd` 时钟；`UnknownSlideCdSec=8` |
| `ClockPolicy.md` | 「SlideCd（当前缺失 → REPAIR）」 | 与 JSON 一同过时 |
| `EffectSchema.json` | `poison.implemented_in_engine: false` | 引擎执行 on_action / on_hit_taken（有剖面门闩） |
| `EffectSchema.md` | 「当前工程：毒未执行 → REPAIR」 | 与 JSON 一同过时 |
| `catalog.json` / `Serialize` | 无 `op` | 运行时 opcode 靠 builtin |

`CombatContract.json` 已写 `party_size_hardcoded: false`、`m1_visible_party_size: 5`、`poison_policy: ACTION_AND_HIT_TRIGGERS`。Clock/Effect 两份 G1 JSON **未跟代码对齐**。只记录，本轮不改契约。

---

## T01–T32（功能 vs 还原）

标签：`IMPLEMENTED` / `STUB` / `MISSING` / `NOT_RUN` / `BLOCKED`。  
无 primary GT 时还原列不得标 IMPLEMENTED。

| ID | 项 | 功能 | 还原 | 相对 QA_REPAIR |
|---|---|---|---|---|
| T01 | 同 seed 事件哈希 | IMPLEMENTED | BLOCKED | 无变。无 GT 事件真值。 |
| T02 | 30/60/120 渲染同逻辑 | MISSING | BLOCKED | 核仍锁 30Hz。 |
| T03 | Auto / 充能 / SlideCd 分时钟 | IMPLEMENTED | BLOCKED | 秒数仍 UNKNOWN。默认套件不再因 Catalog 污染 flaky。 |
| T04 | 暂停 / 加速 / QTE / Fever 时钟 | STUB | BLOCKED | 无变。 |
| T05 | 采样不重复 Tap；上滑不误触 | STUB | BLOCKED | PlayMode `NOT_RUN`。 |
| T06 | 预约 / 自动次序 / 取消 | STUB | BLOCKED | Reserve 仍 5 格。 |
| T07 | 最低 HP 绝对 vs 比例 | STUB | BLOCKED | 仍只比绝对值。 |
| T08 | 多目标 / 多段重选 | STUB | BLOCKED | 无变。 |
| T09 | 嘲讽 / 免疫 / 不可选 | STUB | BLOCKED | 嘲讽有；免疫/不可选无。 |
| T10 | 毒：行动/受击 ≠ 每秒 DoT | IMPLEMENTED | BLOCKED | `GL_UNKNOWN` 不再改 HP。JP 对照仍 MaxHp%。U014。 |
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
| T21 | 5 人与 20 人前后排 | STUB | BLOCKED | 核+编队槽跟 Length；默认 5；无扩编 UI；无 20 人。 |
| T22 | 超时 / 退出 / 失败 / 跨波 | STUB | BLOCKED | Cast/ApplyEffect 空未知会 `Failed`。 |
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
功能 IMPLEMENTED：T01 / T03 / T10 / T15 / T18（均内部一致性）。

---

## G2 切片

| 切片 | 功能 | 还原 | 笔记 |
|---|---|---|---|
| Auto | 近 IMPLEMENTED | BLOCKED | 独立通道 + 不进 tsAmp。`GL_UNKNOWN` 不结算伤。 |
| Tap | IMPLEMENTED（能打） | BLOCKED | 默认不结算；客户端 JP 对照才打。 |
| Slide | IMPLEMENTED（CD + 无均匀 RNG） | BLOCKED | 8s 未知；区间压成 1 ≠ GL。 |
| Drive + QTE | STUB | BLOCKED | 倍率 0.9/1.2/1.5 无 GT。 |
| Fever | STUB | BLOCKED | 通道已拆；仍 7s/70 hit；借 Tap 系数。 |
| 控制 / 护盾 / 换目标 | STUB | BLOCKED | Self=caster 仍在。无 retarget 体。空效果 opcode 已失败关闭。 |
| 死亡 / 换波 / 结算 | STUB | BLOCKED | 无变。 |
| 暂停 / 加速 / 自动 | STUB | BLOCKED | 无变。 |

G2 要「全部关键状态 + 对照视频」。**不满足。**

---

## BLOCKED

| ID | 阻断 | 状态 |
|---|---|---|
| U001 | primary GT 普通 5 人 PVE | `SEARCH_IN_PROGRESS`；`gl-shutdown-pve` 0 录像 |
| U002 | GL Tap/Slide/Auto/Fever 公式 | UNKNOWN；默认不冒充已测；客户端 JP 对照会出数 |
| U003 | Drive 实际伤害 | UNKNOWN |
| U006 | Ignition/增幅组合 | UNKNOWN |
| U010 | 参考画幅 | `NOT_MEASURED` → 禁 T27 |
| U012 | Slide 随机分布 | 实战不再均匀抽样；真值仍 UNKNOWN |
| U013 | PlayMode 基线 | `NOT_RUN`（仅 CrashHandler pid 20340） |
| U014 | 毒触发帧距 | UNKNOWN；`GL_UNKNOWN` 不再改 HP |
| — | EditMode | `NOT_RUN` |
| — | 默认 `dotnet test` | 本轮 0 失败 / 112；不是原作回归 |
| — | Catalog 换表 | `Load`/`BuildBuiltin` 仍全局 `Install`；无并行锁 |
| — | 编队默认 | 活槽跟 Length；默认 5；无 20 人 |
| — | 已知 opcode | 只 `NoteEvent` |
| — | Fever 系数 | 借 Tap `AtkCoef`/`FlatPower` |
| — | 契约 JSON 漂移 | `ClockPolicy.json.slide_cd_present=false`；`EffectSchema.json.poison.implemented_in_engine=false` |

---

## 关闭 / 仍开

**关闭（相对 QA_REPAIR 仍开项）：**

- 默认并行套件因全局 `C001_tap.Opcode` 置空而失败
- `ApplyEffect` 空/未知 opcode 静默上状态
- `GL_UNKNOWN` 毒 `MaxHp * Magnitude` 改 HP
- `TeamBoard` 活编队槽/战力/焦点截断到 5

**仍开：**

- 已知 opcode 只记账
- Fever 借 Tap 系数
- G1 `ClockPolicy` / `EffectSchema` JSON（及对应 md）与代码漂移
- `CatalogJson.Load`/`BuildBuiltin` 换全局表（无 Collection）
- 默认 5 人；无扩编 UI；无 20 人/前后排
- primary GT / PlayMode / EditMode / T 还原 0

## 未做 / 边界

- 未改 `client/`、`ProjectSettings`、`Packages`、`TARGET.json`。
- 未宣称 fidelity、未开 G3、未升级 Unity。
- 未把 112 绿写成 M1 验收或 90%。
- `G1` JSON 与代码不一致只记录。

## 结论句

返修-2 关闭了：默认套件 opcode 就地污染、`ApplyEffect` 空/未知静默、`GL_UNKNOWN` 毒改 HP、`TeamBoard` 活槽截断到 5。  
默认 `dotnet test` 本轮 **0 失败 / 112**（原始输出见上）。  
仍开：已知 opcode 只记账、Fever 借 Tap、契约 JSON 漂移、Catalog 换表无锁、无 GT、无 PlayMode、还原 0。  
**M1 不能验收。**
