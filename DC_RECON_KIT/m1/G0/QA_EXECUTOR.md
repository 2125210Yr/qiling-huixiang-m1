# M1-G2-QA-EXECUTOR — 独立执行方复核

- 任务：`M1-G2-QA-EXECUTOR`
- 角色：`dc_qa`（只读生产代码；本文件为 G0 QA 报告）
- 日期：2026-09-10
- 对照：`QA_REPAIR2.md` 仍开项、`STATUS.md` 本轮声称、G1 `ClockPolicy` / `EffectSchema`、`BattleSim.cs` / `EffectOpcodes.cs` / `DamageMath.cs` / `M1RepairQaTests.cs`
- **未改** 生产代码、`TARGET.json`、未 spawn、未开 G3
- **不采信** 实现代理自述 / `STATUS.md` 本轮声称 / 「115 绿 = M1 过」

## 总判（必须先读）

**M1 不可验收。G2 未过。不得报 90%。不得报还原分 / numeric fidelity / T27。**

相对 `QA_REPAIR2` 仍开的三条核路径，本轮源码+夹具属实关了：

1. 已知 opcode 在 `ApplyEffect` / `Cast` / Fever 上下文里会进 `SettleEffect` / `SettleSkill` / `SettleFeverHit`，盾/Stun/Freeze/嘲讽状态/毒施加不再只 `NoteEvent`。
2. Fever 结算用 `DamageMath.FeverChannelAtkCoef` / `FeverChannelFlat`，**不再** `ResolveSkill(TapSkillId)` 读 Tap `AtkCoef`/`FlatPower`。
3. `ClockPolicy.json.slide_cd_present` 与 `EffectSchema.json.poison.implemented_in_engine` 已与代码对齐。

这仍是内部一致性。硬否决同时成立：

1. primary GT 仍 `SEARCH_IN_PROGRESS` / `CANDIDATE_NEEDS_FETCH`。`docs/reference/gl-shutdown-pve/` **0 mp4**（仅 `README.md` + `FETCH_LOG.txt`）。
2. PlayMode / EditMode = **`NOT_RUN`**。本机仅见 `UnityCrashHandler64`（pid 20340），无活动 Editor。
3. 默认 `dotnet test` 本轮 **0 失败 / 115**（见 §测试）。绿的是夹具自洽，**不是**原作回归，**不能**升格为 M1 验收。
4. T27–T32、T02、T04–T09、T13、T18（还原）、T21(20 人)、T26 无原作对照或未跑。

`GameRoot.StartBattleAt` / `MvpLoop` 仍显式 `Profile = JP_LEGACY_EMPIRICAL`。客户端实战打的是 JP 对照数，不是 GL。默认核 `BattleSim.Profile = GL_UNKNOWN` 仍不结算伤。

---

## 六问逐条（源码，不信自述）

| # | 问题 | 本轮 | 判定 |
|---|---|---|---|
| 1 | 默认 `dotnet test tools/BattleSim.Tests` 是否 0 失败 | 连跑 2 次均为 0 失败 / 115 通过 / exit 0 | **关闭（默认套件绿）**；不是原作回归 |
| 2 | 已知 opcode 是否真改盾/Stun/Taunt/毒施加 | `ExecuteOpcode` 在有 `_execFx`/`_execSkill`/`_execFever` 时调用 Settle*；盾写 `Shield`，Stun/Freeze 清充能+`ActionLocked`，嘲讽入 `Status` 且 `PickFrom` 优先，毒入 `Status` | **关闭（施加路径）**；不是完整 opcode VM，也不是 GL 语义 |
| 3 | 未实现（pierce/revive 等）是否仍失败 | `IsImplemented` 不含 pierce / extra_flat / dispel / retarget / revive；`ExecuteOpcode` / `ApplyStatus` 抛错 + `Failed`，不上状态、不改 HP | **关闭（仍失败）** |
| 4 | Fever 是否还读 Tap 系数；`GL_UNKNOWN` 是否仍不改 HP | Fever 用固定 `FeverChannelAtkCoef=1` / `FeverChannelFlat=0`；`GL_UNKNOWN` 的 Cast / Fever / 毒触发都不 `ApplyDamage` | **关闭（两条均属实）** |
| 5 | 契约 JSON 是否已与代码对齐（`slide_cd_present`、`poison.implemented`） | JSON 已改；`catalog.json` / `Serialize` 仍无 `op`；`EffectSchema.md` 未知码一句仍写 REPAIR | **关闭（点名两字段）**；`op` 序列化仍开 |
| 6 | 先前已关缺口有无回归 | 静默 JP（默认剖面）、空 opcode、Self、TeamBoard 截断、就地测隔离：源码+夹具仍在 | **无回归（已关项）**；Catalog 换表竞态仍开 |

### 1. 默认 `dotnet test` — **本轮 0 失败 / 115**

命令（cwd `F:\天命之子`）：

```
dotnet test tools/BattleSim.Tests/BattleSim.Tests.csproj --nologo --verbosity minimal
```

**第一次（默认并行）原始输出：**

```
[Profile] UTF-8 ready
  正在确定要还原的项目…
  所有项目均是最新的，无法还原。
  BattleSim.Tests -> F:\天命之子\tools\BattleSim.Tests\bin\Debug\net6.0\BattleSim.Tests.dll
F:\天命之子\tools\BattleSim.Tests\bin\Debug\net6.0\BattleSim.Tests.dll (.NETCoreApp,Version=v6.0)的测试运行
正在启动测试执行，请稍候...
总共 1 个测试文件与指定模式相匹配。

已通过! - 失败:     0，通过:   115，已跳过:     0，总计:   115，持续时间: 125 ms - BattleSim.Tests.dll (net6.0)
```

退出码：`0`。

**第二次（同命令，默认并行，不关并行、不加 filter）原始输出：**

```
[Profile] UTF-8 ready
  正在确定要还原的项目…
  所有项目均是最新的，无法还原。
  BattleSim.Tests -> F:\天命之子\tools\BattleSim.Tests\bin\Debug\net6.0\BattleSim.Tests.dll
F:\天命之子\tools\BattleSim.Tests\bin\Debug\net6.0\BattleSim.Tests.dll (.NETCoreApp,Version=v6.0)的测试运行
正在启动测试执行，请稍候...
总共 1 个测试文件与指定模式相匹配。

已通过! - 失败:     0，通过:   115，已跳过:     0，总计:   115，持续时间: 124 ms - BattleSim.Tests.dll (net6.0)
```

退出码：`0`。

`--list-tests`：`BattleSimTests` 89 + `IgnitionTests` 6 + `M1CoreSliceTests` 8 + `M1RepairQaTests` 12 = **115**。相对 `QA_REPAIR2` 的 112（返修测 9），本轮多 3 条：`KnownOpcodeSettlesShieldStunAndTaunt`、`UnimplementedOpcodeStillFails`、`FeverDoesNotReadTapAtkCoefOrFlatPower`。

无 `xunit.runner.json`。无 `CollectionBehavior`。未生成 trx/XML。未跑 Unity EditMode/PlayMode。

**不得把 115 绿写成 M1 / 原作回归。** 只证明默认并行下这套夹具本轮连跑 2 次没炸。

### 2. 已知 opcode 是否真改状态 — **关闭（施加路径）**

`QA_REPAIR2` 仍开句：「`ExecuteOpcode` 对 Known 表只 `NoteEvent`」。本轮源码已不是那样。

`EffectOpcodes.Implemented`：

`dmg.tap` / `dmg.slide` / `dmg.auto` / `dmg.drive_actual` / `dmg.fever_parts` / `status.apply` / `poison.apply` / `shield.apply` / `control.apply` / `charge.add` / `charge.rate` / `slide_cd`

`ExecuteOpcode`：

1. 空 / 未知 / **未实现** → `FailUnknownOpcode` + `UnknownOpcodeException`（不再把「已知但未实现」当成功记账）。
2. 已实现 → `NoteEvent("opcode", …)`，然后：
   - `_execFever` → `SettleFeverHit`
   - `_execSkill` → `SettleSkill`
   - `_execFx` → `SettleEffect`

`ApplyStatus` = `ApplyEffect`：先设 `_execFx`/`_execTarget`，再 `ExecuteOpcode(fx.Opcode)`。`Cast` 先 `ExecuteOpcode(skill.Opcode)`（技能伤/疗），再对挂接效果 `ApplyEffect`。

| 效果 | 是否只记账 | 源码事实 |
|---|---|---|
| 盾 `shield.apply` | 否 | `ApplyControlAndShield`：`Shield = max(Shield, round(MaxHp * Magnitude))`；`ApplyDamage` 先吃盾 |
| Stun / Freeze `control.apply` | 否 | 入 `Status`；`Charge = 0`；`ActionLocked`；`TickUnit` 直接 return（充能停）；`TickSlideClocks` 仍走 |
| 嘲讽 `control.apply` + `Kind=Taunt` | 部分（施加是真的） | 入 `Status`，`Has(Taunt)`；敌打我方时 `PickFrom(..., preferTaunt: true)` 只收嘲讽者。无独立 `retarget` 体 |
| 毒/流血施加 `poison.apply` | 否（施加） | `SettleEffect` 入 `Status`；`poison.apply` 且 Kind 不是 DoT 时强制 `Kind=Poison`。HP 仍只在 on_action / on_hit_taken + JP/KR 剖面扣 |

`KnownOpcodeSettlesShieldStunAndTaunt`：JP 对照下 `Shield > 0`、Stun 后 `ActionLocked` 且 1s 内 Charge 不动、SlideCd 下降、Freeze/Taunt/Bleed 都 `Has(...)`。

边界（不得写成「opcode 解释器已完成」）：

- 无上下文的裸 `ExecuteOpcode("shield.apply")`：`_execFx` 空，`SettleEffect` 直接 return，只留下 `opcode` 事件。
- 技能伤仍看 `SkillType` / `AtkCoef` / `FlatPower`，不是按 opcode 名拆公式。
- `charge.rate` 在 Implemented 表里，但 `SettleEffect` **没有**独立 `charge.rate` 分支；只能当状态挂上，靠 `ChargeHaste` 的 `ChargeSpeedMul`。
- `Silence` 的 `ForKind` 映射到 `control.apply`，但 `ActionLocked` 只认 Stun/Freeze。
- 数值仍 UNKNOWN。不是 GL。

### 3. 未实现码是否仍失败 — **关闭（仍失败）**

`EffectSchema.json.unimplemented_opcodes` 与 `EffectOpcodes` 缺口一致：

`dmg.pierce` / `dmg.extra_flat` / `status.dispel` / `retarget` / `revive`

均 `IsKnown=true`、`IsImplemented=false`。`ExecuteOpcode` 第三条门闩就是 `!IsImplemented`。

`UnimplementedOpcodeStillFails`：

- `ExecuteOpcode(revive)` / `ExecuteOpcode(retarget)` 抛 `UnknownOpcodeException`，`Outcome=Failed`，HP 不变，有 `fail` 事件。
- `ApplyStatus(revive + Kind=Heal)` 同样失败，**不上** `revive_fx`，HP 不变。

pierce / extra_flat / dispel 与 revive 走同一门闩；夹具点名了 pierce 的 Known/Implemented 断言，执行路径测的是 revive/retarget。未实现码不会静默当 0 伤成功。

### 4. Fever 系数与 `GL_UNKNOWN` HP — **关闭**

`TickFever` **不再** `ResolveSkill(TapSkillId)`。只设 `_execFever` 后 `ExecuteOpcode(dmg.fever_parts)` → `SettleFeverHit`：

```
TryResolveCombat(Fever, caster, target,
    DamageMath.FeverChannelAtkCoef,   // 1f
    DamageMath.FeverChannelFlat,      // 0
    …, FeverMul, percentAtk=0, skillFlat=0)
```

`FeverDoesNotReadTapAtkCoefOrFlatPower`：把 `C001_tap` 的 `AtkCoef`/`FlatPower` 放大 50× 并 overlay，JP Fever 掉血与基线相等；事件是 `dmg.fever_parts` + `SkillType.Fever`，没有 Fever 通道上的 `dmg.tap`；全局 catalog 系数未变。

`GL_UNKNOWN` 仍不改 HP（默认 `Profile`）：

| 路径 | 行为 |
|---|---|
| Cast / Fever | `DamageMath.Resolve(GL_UNKNOWN)` → `NOT_MEASURED` → 只 `unresolved`，不 `ApplyDamage` |
| Heal | 非 JP/KR 只 `unresolved` |
| 毒触发 | 非 JP/KR 只 `unresolved` + `continue`，不 `MaxHp * Magnitude` |

夹具：`GlUnknownCastDoesNotApplyJpHpAsSettled`、`GlUnknownPoisonDoesNotApplyMaxHpAsSettled`、Fever 测例末段 GL HP 不变。

残留：`ComputeFever` 仍是 `ComputeTs * feverMul`（对照通道形）。客户端开战仍挂 JP 对照剖面。都不是「又借 Tap 系数」或「GL 默默改 HP」。

### 5. 契约 JSON — **点名两字段已对齐**

| 契约 | 字段 | 代码 | 本轮 |
|---|---|---|---|
| `ClockPolicy.json` | `slide_cd_present: true`；`slide_cd_decision: IMPLEMENTED`；`slide_cd_sec: null` / `UNKNOWN` | `UnitState.SlideCd`；`TickSlideClocks` 独立于充能；`UnknownSlideCdSec=8` | **对齐**（秒数仍 UNKNOWN） |
| `ClockPolicy.md` | 「SlideCd（已独立实现；秒数 UNKNOWN）」 | 同上 | **对齐** |
| `EffectSchema.json` | `poison.implemented_in_engine: true`；`decision: IMPLEMENTED`；`triggers: on_action, on_hit` | `ApplyPoisonTriggers`；禁每秒 DoT；剖面门闩 | **对齐** |
| `EffectSchema.json` | `bleed` / `shield` / `control` / `taunt` 的 `implemented_in_engine: true` | 施加路径存在；GL 值 UNKNOWN | **功能对齐**；不是还原 |
| `EffectSchema.json` | `unimplemented_opcodes` 五码 | 与 `IsImplemented` 缺口一致 | **对齐** |
| `EffectSchema.md` | 毒「已按 on_action / on_hit_taken 执行」 | 与 JSON 一致 | **对齐** |
| `EffectSchema.md` | 未知码「当前工程 REPAIR 项」 | 代码已 FAIL | **md 一句过时** |
| `catalog.json` / `Serialize` | 技能/效果仍不写 `op` | 运行时靠 builtin `Opcode`；overlay 无 `op` 不覆盖 | **仍开（不是本问答的两字段）** |
| `CombatContract.json` | `poison_policy: ACTION_AND_HIT_TRIGGERS`；`party_size_hardcoded: false` | 先前已写 | 无变 |

`STATUS.md`「ClockPolicy / EffectSchema 与代码对齐」对点名两字段成立；不得写成「全部 G1 文本零漂移」。

### 6. 先前已关缺口 — **无回归**

| 已关项 | 本轮源码 | 回归？ |
|---|---|---|
| 默认 `GL_UNKNOWN` 技能结算不静默用 JP 改 HP | `TryResolveCombat` / `Heal` / 毒触发均有剖面门闩 | **无**。客户端显式 JP 对照仍出数（标对照即可） |
| `ApplyEffect` / `Cast` 空/未知静默 | 先 `ExecuteOpcode`；空/未知/`!IsImplemented` 抛错 + `Failed`，不上 Stun/AtkBuff | **无** |
| `Self` ≠ 活着的 caster | `Select(Self)`：`caster != null && caster.Alive` 才入列 | **无**。`SelfTargetsCasterNotAliveZero` 仍 overlay clone |
| `TeamBoard` 活编队截成 5 | `DrawTeam`/`DrawParty`/`TeamPower`/`FocusSlot`/`InParty` 跟 `LivePartySize`；循环 `i < n` | **无**。`PartySize` 仍只当间距阈值 |
| 测例就地改坏全局 `C001_tap.Opcode` | `CloneSkill` 拷 Opcode；`OverlaySkill` 写实例字典；测后断言全局未变 | **无**（连跑 2 次未见 `UNKNOWN_OPCODE <empty>`） |

仍开、与回归无关：

- `Catalog.BuildBuiltin` / `CatalogJson.Load` → `Install` 换全局表；`CatalogJsonFillsEmptySliceFromBuiltin` / `CatalogJsonRoundtrip` 仍走。无 `CollectionBehavior`。
- 默认开档 5 人；`SetPartySlot` 不扩长；无 20 人/前后排。`GameRoot.DrawPartyRow` **仍无调用点**（死代码，跟 Length 不截 5）。
- 预约 `NextAutoType` 仍 5 格。

---

## T01–T32（功能 vs 还原）

标签：`IMPLEMENTED` / `STUB` / `MISSING` / `NOT_RUN` / `BLOCKED`。  
无 primary GT 时还原列不得标 IMPLEMENTED。

| ID | 项 | 功能 | 还原 | 相对 QA_REPAIR2 |
|---|---|---|---|---|
| T01 | 同 seed 事件哈希 | IMPLEMENTED | BLOCKED | 无变。无 GT 事件真值。 |
| T02 | 30/60/120 渲染同逻辑 | MISSING | BLOCKED | 核仍锁 30Hz。 |
| T03 | Auto / 充能 / SlideCd 分时钟 | IMPLEMENTED | BLOCKED | JSON 已认独立 SlideCd。秒数仍 UNKNOWN。 |
| T04 | 暂停 / 加速 / QTE / Fever 时钟 | STUB | BLOCKED | Fever 不再借 Tap 系数；窗仍 7s/70 hit。 |
| T05 | 采样不重复 Tap；上滑不误触 | STUB | BLOCKED | PlayMode `NOT_RUN`。 |
| T06 | 预约 / 自动次序 / 取消 | STUB | BLOCKED | Reserve 仍 5 格。 |
| T07 | 最低 HP 绝对 vs 比例 | STUB | BLOCKED | 仍只比绝对值。 |
| T08 | 多目标 / 多段重选 | STUB | BLOCKED | `retarget` 仍失败关闭。 |
| T09 | 嘲讽 / 免疫 / 不可选 | STUB | BLOCKED | 嘲讽状态+优先选取在；免疫/不可选无。 |
| T10 | 毒：行动/受击 ≠ 每秒 DoT | IMPLEMENTED | BLOCKED | JSON 已标 implemented。`GL_UNKNOWN` 不改 HP。U014。 |
| T11 | 护盾/穿防/反射/吸血/死亡触发 | STUB | BLOCKED | 盾施加+吸收在；`dmg.pierce` 仍失败。 |
| T12 | 堆叠 / 控制中充能 | STUB | BLOCKED | 受控停充能、SlideCd 仍走。 |
| T13 | 复活 / 迟到事件 | MISSING | BLOCKED | `revive` 仍失败关闭。 |
| T14 | S 与固有/装备 ATK 不重复 | STUB | BLOCKED | 无变。 |
| T15 | Tap / Slide / Auto 不混 | IMPLEMENTED | BLOCKED | ExtraDmg/Fever 通道已拆；Fever 不再借 Tap 系数。 |
| T16 | 属性 / 暴击 / Fever 例外 | STUB | BLOCKED | Fever 通道已标，例外无 GT。 |
| T17 | 额外 vs 穿防 | STUB | BLOCKED | `dmg.extra_flat` / `dmg.pierce` 未实现。 |
| T18 | Slide 区间 ≠ 均匀 RNG | IMPLEMENTED | BLOCKED | 实战 `variance=1`。分布仍 U012。 |
| T19 | 点火 / 增幅独立 | STUB | BLOCKED | U006。 |
| T20 | 舍入 / 多段 / 零血护盾 | STUB | BLOCKED | 盾吸收在；零血护盾无 GT。 |
| T21 | 5 人与 20 人前后排 | STUB | BLOCKED | 核+编队槽跟 Length；默认 5；无扩编 UI；无 20 人。 |
| T22 | 超时 / 退出 / 失败 / 跨波 | STUB | BLOCKED | 空/未知/未实现 opcode → `Failed`。 |
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
| Fever | STUB | BLOCKED | 不再借 Tap 系数；仍 7s/70 hit；公式形仍 Ts×0.6。 |
| 控制 / 护盾 / 换目标 | STUB | BLOCKED | 盾/Stun/嘲讽施加在。无 `retarget` 体。空/未实现效果 opcode 失败关闭。 |
| 死亡 / 换波 / 结算 | STUB | BLOCKED | `revive` 仍失败。 |
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
| U014 | 毒触发帧距 | UNKNOWN；`GL_UNKNOWN` 不改 HP |
| — | EditMode | `NOT_RUN` |
| — | 默认 `dotnet test` | 本轮 0 失败 / 115（连跑 2 次）；不是原作回归 |
| — | Catalog 换表 | `Load`/`BuildBuiltin` 仍全局 `Install`；无并行锁 |
| — | 编队默认 | 活槽跟 Length；默认 5；无 20 人 |
| — | 未实现 opcode | pierce / extra_flat / dispel / retarget / revive 仍 FAIL（正确） |
| — | Fever 公式 | 不借 Tap 系数；通道形仍 Ts×FeverMul；秒/击次数未知 |
| — | `catalog.json` `op` | Serialize 仍不写 `op` |

---

## 关闭 / 仍开

**关闭（相对 QA_REPAIR2 仍开项）：**

- 已知 opcode 只记账（盾/Stun/Freeze/嘲讽状态/毒施加已走 Settle*）
- Fever 借 Tap `AtkCoef`/`FlatPower`
- G1 `ClockPolicy.json.slide_cd_present` / `EffectSchema.json.poison.implemented_in_engine` 与代码漂移

**仍开：**

- `CatalogJson.Load`/`BuildBuiltin` 换全局表（无 Collection）
- 默认 5 人；无扩编 UI；无 20 人/前后排
- `catalog.json` / `Serialize` 无 `op`
- `charge.rate` 有名无独立结算体；`Silence` 不锁行动；嘲讽不是 `retarget`
- primary GT / PlayMode / EditMode / T 还原 0

## 未做 / 边界

- 未改 `client/`、`ProjectSettings`、`Packages`、`TARGET.json`。
- 未宣称 fidelity、未开 G3、未升级 Unity。
- 未把 115 绿写成 M1 验收或 90%。
- 未实现码继续失败是正确行为，不是缺口回潮。

## 结论句

执行方本轮关了：已知 opcode 施加路径（盾/Stun/嘲讽/毒入状态）、Fever 不再读 Tap 系数、`slide_cd_present` / `poison.implemented_in_engine` 对齐。  
默认 `dotnet test` 连跑 2 次 **0 失败 / 115**（原始输出见上）。  
先前已关项（静默 JP 默认剖面、空 opcode、Self、TeamBoard 截断、就地隔离）**无回归**。  
仍开：Catalog 换表无锁、无 GT、无 PlayMode、还原 0。  
**M1 不能验收。**
