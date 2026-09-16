# G2 复审：1b2ca8e

日期：2026-09-14。仓库 `2125210Yr/qiling-huixiang-m1`，分支 `m1-gt6-review`。
固定审查提交：`1b2ca8e4ff56d9f0986e158fc97b50498f735cb3`。上一轮基线：`1a752532e39560b861c2a661db66c97ca18ffece`。

## 结论

**NEEDS_FIX。保留本轮修复；不推倒框架、不重做 G0/G1、不自动开启 G3。**

本次通过 GitHub 连接读取固定提交的生产代码、测试源码和提交内产物。没有修改远程仓库；本环境没有运行 .NET/C#、Unity、自然操作、构建或原作逐帧对照。下面的“源码确认”“提交报告”“待运行反例”不是同一种证据。

本轮应结束在几个有限问题上：测试报告与摘要一致、合法工程验证配置真正接到 Unity 路径、实际关卡参数进入数据身份、现有自然操作/分场文件/回放/构建实际执行。不要继续增加大型架构和长篇合同。

## 1. 已修复，勿再返工

| 项目 | 本次源码确认 | 验收边界 |
|---|---|---|
| 整技能执行前检查 | Tap/Slide/Drive 增加 FightSkillReady / CheckFightSkill，Submit 返回 Unplayable；普通技能在资源消耗前检查依赖。S04/S05/S19 | 不支持的技能不再应“先伤害、后跳过附带效果”；但调用方必须提供可玩的验证配置。 |
| HasTarget 解析 | 明确区分缺键、false、true；true 要求合法 target，非法布尔值会报错。S13 | 上一轮 false 被读成 true 的主问题可关闭为源码修复。 |
| 原记录器对接 | 测试项目直接链接 NaturalPlayBattleEvidence.cs，typed tests 调用 Begin/Persist/加载；新增 sim/battle_id 不匹配保护与 replay.jsonl。S15/S16/S17 | 不再要求为反射猜类名而重造核心存储。Unity NEXT/HOME 时机仍须实际验证。 |
| 共享测试串扰 | 程序集关闭测试并行，MaxParallelThreads=1。S18 | 源码配置已落地；不等于所提交全量 TRX 已通过。与最大并行开发代理不冲突。 |
| 身份字段补齐 | 元素、普攻技能、效果目标/Side/Opcode/Group/SourceTier、治疗字段、overlay、绑定验证政策已进入规范化计算。S14 | 下面仍有“实际传入关卡 vs 全局查表关卡”的明确残余。 |

## 2. A1 / 高优先：提交的验收报告与摘要矛盾

S01/S02 声称本批唯一有效的 `a53-gate.trx` 是 244 passed / 0 failed / 1 skipped / 245 total，并明确把另一个 `a53-qa.trx` 归为历史红测。

本次直接读取 S03 的文件尾部（原文件约 1580–1679 行），得到：

```xml
<ResultSummary outcome="Failed">
<Counters total="245" executed="244" passed="234" failed="10" ... />
```

该文件的 blob SHA 是 `987ffbe9ebfd493370fe20ff4258847819270cd2`；RunId 是 `561a2e62-012e-4021-ba20-6b0b3620f263`；Times 为 2026-09-14 17:13:14.5608319+08:00 至 17:13:18.6794861+08:00。StdOut/RunInfos 还明确包含自然 UI 合同跳过记录及十个失败项。

这不是拿 `a53-qa.trx` 冒充最终报告。矛盾发生在摘要自己指向的 `a53-gate.trx`。

**不能据此直接宣布最终源码重新运行仍然有这十个失败。**报告可能是未替换的早期产物：例如当前 S12 的 NewSim 已改为使用命名替代夹具，而报告仍记录该测试 Slide 失败。这说明必须先核对“被测源码”和“上传报告”是否一致，而不是猜测作者意图或把旧报告当本次独立执行。

最小修复：先固定实际被测源码提交或源文件树哈希，再执行无 filter 的完整套件；把原始 TRX、退出码、完整日志和源码身份一起提交。摘要由那份 TRX 自动生成。旧报告保留为历史，不能手动改计数。报告提交可以指向前一个源码提交，无需制造自引用 commit SHA。

### S03 中实际列出的十项失败（报告事实，不是审查者重跑）
- `Resonance.Tests.G2ReviewReplayComponentTests.FullReplayVerifyHasZeroDivergences`
- `Resonance.Tests.G2ReviewReplayComponentTests.DifferentSeedReplayReportsDivergence`
- `Resonance.Tests.M1CoreSliceTests.SlideCdIsIndependentOfCharge`
- `Resonance.Tests.M1RepairQaTests.SlideDoesNotUseUniformVarianceRng`
- `Resonance.Tests.M1RepairQaTests.CloneSkillKeepsExternalSlideRankSlots`
- `Resonance.Tests.M1RepairQaTests.EmptyOrUnknownOpcodeCastFails`
- `Resonance.Tests.BattleSimTests.TapAndSlideRecordDistinctCasts`
- `Resonance.Tests.BattleSimTests.SlideAddsFourteenDrive`
- `Resonance.Tests.BattleSimTests.AutoTapFollowsReservation`
- `Resonance.Tests.BattleSimTests.BattleSimLightDarkMatchup`

其中两项回放记录明确输出 `Slide rejected: Unplayable tick=269`。其余失败包含 Slide 冷却、伤害/预订行为及旧的“必须抛异常”断言。不能用删除失败用例、放松正常技能应成功的断言，或关闭新门禁来换绿。

## 3. A2 / 高优先：单测换用了合法替代技能，自然 UI 配置却没有同样接上

这是当前代码中可定位的集成缺口，不只是“还没提供录像”。

1. S06 的 `C001_slide` 仍链接 `dot_flame`，该效果 Kind=Dot。S07 仍将该组合登记为未实现。
2. S04/S05 的新门禁会拒绝这个完整技能，这是正确的保护。
3. S11 的 `NewJp/BindStripDotFlame` 以及 S12 的 NewSim 现在显式绑定 `A53_SUB_STRIP_DOT_FLAME`。这是命名的伤害-only 工程替代，不是在实现 Dot；用来隔离冷却/回放单测本身是合理的。
4. S08 的 VerificationCatalog.Apply 仍只克隆 builtin、调整充能/DriveGain/关卡参数，没有应用同样的合法技能替代。
5. S09 的 StartBattleAt 直接构造 BattleSim 并冻结头，没有接入命名替代或整队依赖检查；S10 的 basic 流程仍固定要求 p0 / slot 0 成功执行 Slide。

因此，在默认 C001 位于 p0、没有另行注入有效替代的这条验证创建路径上，Slide 会被新门禁拒为 Unplayable。不能把提高敌方 HP、延长等待或补充充能当作修复：能力不支持与充能不足是两件事。

### 最小修复

保留严格门禁，也保留原始库存内容。只在**明确命名的工程验证配置**中选用已有合法角色/技能或已存在的命名替代；让真实 UI 和相应集成测试使用同一份有效配置。配置在首个战斗动作前准备好，随后冻结实际身份；首次自动攻击/队长效果等也应使用预定的配置。

普通入口如仍允许选择未实现角色，应在开战前清楚报告缺失依赖，或者对不完整技能明确禁用/提示，不能让 UI 一直显示可用而只在内部日志拒绝。不要求本轮一次实现全部历史效果，也不允许偷偷把生产默认技能删效果来假装还原。

新增最小回归：复用实际验证目录准备和创建入口，不调用仅单测用的 BindStripDotFlame 来绕过问题；确认验证角色的 Tap/Slide/Drive/Leader 及敌方依赖可执行，然后走真实 UI basic。不要为此另建游戏框架。

## 4. A3 / 中优先：实际传入的 StageDef 仍可能不进入 DataIdentity

S05 构造函数接受任意 StageDef，并把它保存在 `_stage`，TimeLeft 和波次实际使用该对象。

但 S14 的 `BattleContentIdentity.Compute` 仍通过 `FindStage(stageId)` 从全局 Catalog 重新查找“同名关卡”，而不是取该 BattleSim 正在使用的 `_stage`。

待执行反例：保持 Catalog、阵容、种子、profile、时钟、overlay 相同，构造两个未安装回 Catalog 的 StageDef 克隆，令 Id 相同但 TimeLimitSec（或 EnemyHpMul）不同，分别传入 BattleSim。在首个 Tick 之前比较 DataIdentity。目前 Compute 的输入不会包含这两个实际关卡的差别。

这是源码推导，未在本环境执行。最终状态摘要可能发现运行差异，所以不能扩写成“所有回放都一定会错误地通过”；问题是“完整配置身份”仍不能识别这个实际参数差异。

最小修复：从模拟实例取得其实际、只读/复制的关卡配置进入规范化快照，或由开战时的有效配置统一提供，不再只按 ID 重新查表。加一条同 ID 不同实际参数必须不同身份的测试即可，不重建整套回放系统。

## 5. A4 / 验收缺口：代码和运行脚本不能替代 Unity 自然结果

S01/S02 仍将 Unity/T08、匹配源码的新构建列为 NOT_RUN。typed 记录器与 replay.jsonl 是有效进展，不能因此宣布自然 NEXT/HOME 文件流程已经验证。

下一批执行顺序：

- 将上述合法验证配置接入后，实际运行 basic / 手动 Fever（至少两个不同存活槽位）/ auto 三组；开战后只走既有 UI/命令链路，不能补 HP、Drive、Charge 或强制 Perfect。
- 每一场独立保存；NEXT/HOME 后从文件读回第一场，再通过已有 BattleReplayer 在新实例上核对。保留同一场的 battle_id、初始配置、完整命令、结果/退出状态和截图/录像对应关系。
- 产出与被测源码一致的可运行构建及连续录像。若环境缺安装/许可/执行器，报告具体命令和错误，把该项标 BLOCKED；不要仅以 NOT_RUN 收口后又宣称只差验收。

当前不新增原作资料搜索、不解禁延期的精确数值/T27/T28，也不启动 G3/M2。开发代理用环境允许的最大并行数，先恢复现有任务，避免重复派发；Unity 活动项目和共享文件保持单一写入者。串行运行共享全局状态的测试与此并不矛盾。

## 6. 交付与关闭条件

本轮只收四件事：A1 一致的原始测试证据；A2 真实验证入口合法且 basic/fever/auto 可执行；A3 实际关卡身份；A4 自然证据、文件回放和构建实际运行。

完成后工程门槛才可标 `PATH_A_ENGINEERING_PASS`。M1 原作还原验收仍为 `DEFERRED_NOT_REMOVED`，G3 不自动开启。若只完成代码，不把整个批次记为通过。

## 固定提交来源

- S01: [DC_RECON_KIT/m1/REVIEW_1a75253/artifacts/CURRENT_STATE.md](https://github.com/2125210Yr/qiling-huixiang-m1/blob/1b2ca8e4ff56d9f0986e158fc97b50498f735cb3/DC_RECON_KIT/m1/REVIEW_1a75253/artifacts/CURRENT_STATE.md)
- S02: [DC_RECON_KIT/m1/REVIEW_1a75253/artifacts/REGRESSION_LOG.md](https://github.com/2125210Yr/qiling-huixiang-m1/blob/1b2ca8e4ff56d9f0986e158fc97b50498f735cb3/DC_RECON_KIT/m1/REVIEW_1a75253/artifacts/REGRESSION_LOG.md)
- S03: [DC_RECON_KIT/m1/REVIEW_1a75253/artifacts/tests/a53-gate.trx](https://github.com/2125210Yr/qiling-huixiang-m1/blob/1b2ca8e4ff56d9f0986e158fc97b50498f735cb3/DC_RECON_KIT/m1/REVIEW_1a75253/artifacts/tests/a53-gate.trx)
- S04: [client/Assets/Scripts/Resonance.Battle/Core/BattleSim.Commands.cs](https://github.com/2125210Yr/qiling-huixiang-m1/blob/1b2ca8e4ff56d9f0986e158fc97b50498f735cb3/client/Assets/Scripts/Resonance.Battle/Core/BattleSim.Commands.cs)
- S05: [client/Assets/Scripts/Resonance.Battle/Core/BattleSim.cs](https://github.com/2125210Yr/qiling-huixiang-m1/blob/1b2ca8e4ff56d9f0986e158fc97b50498f735cb3/client/Assets/Scripts/Resonance.Battle/Core/BattleSim.cs)
- S06: [client/Assets/Scripts/Resonance.Battle/Content/Catalog.cs](https://github.com/2125210Yr/qiling-huixiang-m1/blob/1b2ca8e4ff56d9f0986e158fc97b50498f735cb3/client/Assets/Scripts/Resonance.Battle/Content/Catalog.cs)
- S07: [client/Assets/Scripts/Resonance.Battle/Core/EffectCapability.cs](https://github.com/2125210Yr/qiling-huixiang-m1/blob/1b2ca8e4ff56d9f0986e158fc97b50498f735cb3/client/Assets/Scripts/Resonance.Battle/Core/EffectCapability.cs)
- S08: [client/Assets/Scripts/Resonance.Battle/Content/VerificationCatalog.cs](https://github.com/2125210Yr/qiling-huixiang-m1/blob/1b2ca8e4ff56d9f0986e158fc97b50498f735cb3/client/Assets/Scripts/Resonance.Battle/Content/VerificationCatalog.cs)
- S09: [client/Assets/Scripts/Resonance.App/Core/GameRoot.cs](https://github.com/2125210Yr/qiling-huixiang-m1/blob/1b2ca8e4ff56d9f0986e158fc97b50498f735cb3/client/Assets/Scripts/Resonance.App/Core/GameRoot.cs)
- S10: [client/Assets/Scripts/Resonance.App/Debug/NaturalPlayRuntime.cs](https://github.com/2125210Yr/qiling-huixiang-m1/blob/1b2ca8e4ff56d9f0986e158fc97b50498f735cb3/client/Assets/Scripts/Resonance.App/Debug/NaturalPlayRuntime.cs)
- S11: [tools/BattleSim.Tests/G2ReviewFixtures.cs](https://github.com/2125210Yr/qiling-huixiang-m1/blob/1b2ca8e4ff56d9f0986e158fc97b50498f735cb3/tools/BattleSim.Tests/G2ReviewFixtures.cs)
- S12: [tools/BattleSim.Tests/M1CoreSliceTests.cs](https://github.com/2125210Yr/qiling-huixiang-m1/blob/1b2ca8e4ff56d9f0986e158fc97b50498f735cb3/tools/BattleSim.Tests/M1CoreSliceTests.cs)
- S13: [client/Assets/Scripts/Resonance.Battle/Content/CatalogJson.cs](https://github.com/2125210Yr/qiling-huixiang-m1/blob/1b2ca8e4ff56d9f0986e158fc97b50498f735cb3/client/Assets/Scripts/Resonance.Battle/Content/CatalogJson.cs)
- S14: [client/Assets/Scripts/Resonance.Battle/Core/BattleReplay.cs](https://github.com/2125210Yr/qiling-huixiang-m1/blob/1b2ca8e4ff56d9f0986e158fc97b50498f735cb3/client/Assets/Scripts/Resonance.Battle/Core/BattleReplay.cs)
- S15: [client/Assets/Scripts/Resonance.App/Debug/NaturalPlayBattleEvidence.cs](https://github.com/2125210Yr/qiling-huixiang-m1/blob/1b2ca8e4ff56d9f0986e158fc97b50498f735cb3/client/Assets/Scripts/Resonance.App/Debug/NaturalPlayBattleEvidence.cs)
- S16: [tools/BattleSim.Tests/G2A53EvidenceTests.cs](https://github.com/2125210Yr/qiling-huixiang-m1/blob/1b2ca8e4ff56d9f0986e158fc97b50498f735cb3/tools/BattleSim.Tests/G2A53EvidenceTests.cs)
- S17: [tools/BattleSim.Tests/BattleSim.Tests.csproj](https://github.com/2125210Yr/qiling-huixiang-m1/blob/1b2ca8e4ff56d9f0986e158fc97b50498f735cb3/tools/BattleSim.Tests/BattleSim.Tests.csproj)
- S18: [tools/BattleSim.Tests/G2A53AssemblyIsolation.cs](https://github.com/2125210Yr/qiling-huixiang-m1/blob/1b2ca8e4ff56d9f0986e158fc97b50498f735cb3/tools/BattleSim.Tests/G2A53AssemblyIsolation.cs)
- S19: [tools/BattleSim.Tests/G2A53PlayableGateTests.cs](https://github.com/2125210Yr/qiling-huixiang-m1/blob/1b2ca8e4ff56d9f0986e158fc97b50498f735cb3/tools/BattleSim.Tests/G2A53PlayableGateTests.cs)
