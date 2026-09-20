# ExpeditionRelicRuntime 接口与验证边界

模块：`client/Assets/Scripts/Resonance.Battle/Expedition/ExpeditionRelicRuntime.cs`。纯执行器测试：`tools/BattleSim.Tests/OriginalExpeditionRelicRuntimeTests.cs`。本模块不修改 UnitState，不调用 Cast、伤害公式、随机数、Unity 或存档；主集成者提供唯一的真实结算委托。

## 精确接入

```csharp
new ExpeditionRelicRuntime(ExpeditionBattleInput input, int[] openingMaxHp)
RelicNativeAction BeginNativeAction(long rootActionId, UnitState caster, SkillDef skill)
float DamageChannelBonus(RelicNativeAction action)
int AdjustNativeShield(int basePoints, bool sourceAlly, bool targetAlly)
void ObserveResolution(RelicNativeAction action, ResolutionResult result)
void CompleteNativeAction(
    RelicNativeAction action,
    Func<RelicBattleView> readCurrent,
    Action<RelicRequest> applyRequest)
```

1. 构造时检查冻结输入，复制遗物集合和参数。`openingMaxHp` 必须包含五人实际开局最大生命，包括本场开局已经倒下的队员；它不是当前生命。阈值为五人平均最大生命乘配置比例后向下取整，至少 1。
2. 技能已经通过统一合法性校验后，分配严格递增、正数 rootActionId 并 Begin。token 生命周期覆盖原生命中、原生治疗、linked effect 与原生动作完成；不因 `_execCaster` 临时清理而丢失身份。
3. `DamageChannelBonus` 是此动作开始时已有强奏的同通道加数，应加入 ExtraDmg 通道后再求伤害；不能把最终伤害直接乘 2.2。首次有效原生伤害消费强奏后，token 对后续本技能命中仍返回同一加数。
4. 原生护盾生成时调用 `AdjustNativeShield`，应用最终数值后再 Observe 实际 `ShieldProduced`，以免计算出来但未落地的增强显示为触发。
5. 所有有效原生结算调用唯一共享 `ResolutionResult` 的 Observe。全盾吸收也必须调用。Derived 与 Dot 会在检查 token 活跃性之前被直接忽略，允许主集成者在 Complete 的 apply 回调内统一记录衍生结果。
6. 原生动作完成后只调用一次 Complete。它按 A→B→C 执行，每次衍生请求前重读 View。任何异常交给主集成者标记工程 Failed；不得恢复为普通败北或吞掉超界错误继续打。

`RelicBattleView` 字段为 `IReadOnlyList<UnitState> Allies/Enemies`、`int FocusEnemySlot`、`bool StopDerivedDamage`、`Func<UnitState,int> GenerationProvider`。UnitState 只读；提供者可以返回最新单位列表。`StopDerivedDamage` 在工程 Failed 或首领已死亡时为 true。代数提供者缺省时为 0，接入面具实例后使用其真实代数。

`RelicRequest` 仅有 Damage、Charge 两种。包含 SourceRelicId、RootActionId、固定 ProcDepth=1、SkillId、SourceSlot/SourceAlly，以及 Target 引用、TargetSlot/TargetAlly/TargetGeneration、Damage 或 Charge。主集成者应核对引用和代数后应用；伤害直接进入共同扣盾/扣血/死亡路径，不重新算暴击、攻击、防御、毒触发或眩晕延长；充能上限 100，不能自动施法。

只读状态：`BarrierThreshold`（别名 `Threshold`）、`BarrierEnergy`、`DistinctActorBits`（别名 `HarmonyActorMask`）、`ForteStored`、`LastCompletedRootActionId`、`LastRequestCount`、`TriggerSerial`、`LastTriggeredRelicId`。触发反馈同一原生动作同一遗物只标一次；实际金额取 ResolutionResult/ResolutionLedger，不从触发次数反推伤害。

## 确定规则

- A 只收集敌方 Auto/Tap/Slide 原生直接攻击的真实盾吸收，单次动作先归集再入池；最多 3T。A04 替代 A01 主震荡，只消费完整 T；A03 复制最终主震荡力度，稳定槽位选择不同副目标。无合法主目标不消费。
- B 资格由技能定义的单体直接伤害冻结。固定第一次有效主命中的扣盾前请求值；群攻即使只剩一个敌人也无效。先散射，再判断原生击杀标记执行 B04。两者都不使用随机流，衍生击杀无法写入原生击杀标记。
- C 只收成功 Tap/Slide。接力循环按 0…4 出场顺序找存活、未满且非施法者队员。和声记录不同槽位；完成时清空记录。C04 只储一份，治疗/护盾/无有效伤害不消费；当前动作可消费旧强奏，并在结尾形成新和声重新储一份。
- 新遗物伤害和盾使用共享 `ResolutionMath.Scale(int,float)`，作者配置 float 先按其十进制数值解释，最终 AwayFromZero 四舍五入。只有阈值明确向下取整；例如 5×0.7f=4。
- 合法配置至多 12 次单位变化请求（A 3、B 3、C 6），工程硬上限 16。配置越界、数值溢出、无效视图或重复完成均明确异常，没有静默截断。构造/数值域限制使合法输入不会依赖预算截断。

## 已执行记录

| 原始记录 | 实际结果 | 说明 |
|---|---|---|
| `runtime-integration-red.trx` / `.log` | 8 失败、0 通过、0 跳过 | 可编译最小 Runtime 桩 3 项，加主集成者旧挂点 5 项；全部具体数值断言失败 |
| `runtime-a-green.trx` / `.log` | 3 通过 | A 纵向实现后 |
| `runtime-bc-red.trx` / `.log` | 6 失败、3 通过 | B/C 仍为桩，保留已通过 A |
| `runtime-abc-green.trx` / `.log` | 9 通过 | B/C 完整实现后 |
| `runtime-boundary-red.trx`（主集成者运行） | 27 通过、1 失败 | 新增边界暴露 Leader 通道误计吸收；期望 0，实际 600 |
| `boundaries-integration-green.trx`（主集成者运行，按类名核验） | 本类 28 通过 | 修复后所有 `OriginalExpeditionRelicRuntimeTests` 均通过；该合并报告整体为 49 通过、1 个主集成者其他用例失败，不能据此宣称合并运行全绿 |

Leader 边界通过在 Observe 中要求 `action.IsDirectDamage` 修复，修复后本类 28 项已从实际合并 TRX 中逐项核验。最终全量回归、Unity 和交付以主集成者的后续记录为准。纯 Runtime fixtures 自行构造结算结果和终局单位状态，不能作为普通实玩、Unity 交互或遗物自然触发录像。
