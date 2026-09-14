# G2 路 A：1a75253 固定提交复审

审查日期：2026-09-14。仓库：2125210Yr/qiling-huixiang-m1。
分支：m1-gt6-review。提交：1a752532e39560b861c2a661db66c97ca18ffece。
对照基线：2a00445c67dbb2ca9251d81428e3096b71c233fb。

## 结论与边界

**NEEDS_FIX。保留已修部分；不重做 G0/G1，不另建框架，不开 G3。**

本轮有实际修复，不能沿用“完全没改”的判断。下一批只补运行门禁、数据身份/解析、测试对接和真实运行交付，不增加新游戏系统。

本次实际通过 GitHub 连接读取固定提交、增量、当前源码、测试代码与批次状态。未修改远程仓库。当前审查容器没有可用 dotnet/Mono/C# 或 Unity 命令；尝试准备本地源文件时网络解析失败。因此没有独立编译或执行 C#/Unity，不将下文反例设计写成已执行结果，也不将对方的隔离 PASS 记作独立复跑。未验收原作像素、时序、精确 GL 数值或真机表现。

本批 artifacts 目录返回 CURRENT_STATE.md、REGRESSION_LOG.md、deferred.md 三份文档；该批目录没有新的原始 TRX、自然运行产物或构建。REGRESSION_LOG 的 PASS 是提交者的运行摘要，本轮未取得同批完整原始结果。历史 203/203 仍属于前一批。

## 一、上一轮问题的复核

| 范围 | 当前源码判断 | 尚需什么 |
|---|---|---|
| ChargeSpeed/ChargeAmount/Barrier 选边 | 原 EffectKind 白名单被 TargetSemantics 替换，ApplyLinkedEffect 按声明选边 | 保留修复；补下面 A53-01 的整技能门禁 |
| 敌方沉默 | TickUnit 的声明技能路径使用与己方同一 CanAcceptSkillInput | 保留修复，完整回归 |
| 导入失败污染活动表 | Load 在候选容器解析，ActivateCandidate 先验证后 InstallTables 单次发布 Current | 此前原子性主问题有代码修复；不等于所有内容已可玩 |
| 自动技能统一入口 | AutoFireSkills/AutoFireDrive 经 Submit，自动行为可记录 | TryBeginDrive 的即时自动 Resolve、系统超时等仍需明确是内部派生事件而非外部输入 |
| 自动 Fever 重复重放 | BattleReplayer 筛外部输入，自动策略由 Tick 生成；分歧和未消费命令进入结果 | 保留修复；哈希字段与测试隔离见下文 |
| 开局 Speed/Auto | GameRoot 调 FreezeInitialHeader，首次 Tick 也提供冻结入口 | 输出文件还需绑定同一初始头，不靠运行末态头代替 |
| QTE 恢复 tick 双扣时间 | 关卡时间预算集中于 ReleaseBlocks，外层不重复扣 | 源码主问题已改；需同批原始测试结果 |
| Measured 混同 Computed | Measured 只看 Evidence；Bounds 无策略时拒绝 RequireInt | 保留，不回滚过渡别名 |
| 自然动作覆盖 | 新增 basic/fever/auto 场景和指针流程，Fever 有两个槽位必达要求 | 本批 Unity NOT_RUN，不能判通过 |
| 第一场证据被 NEXT 覆盖 | 新增 NaturalPlayBattleEvidence，pre-next 保存每场文件 | 有实现但未运行；L01/L02 当前测试查错程序集，见 A53-04 |

## 二、待修事项

### A53-01 / P1：资料库和可玩表分开了，但战斗仍能执行不完整技能

**路径**
- client/Assets/Scripts/Resonance.Battle/Content/Catalog.cs：InstallTables、TrySkill、PlayableIds。
- client/Assets/Scripts/Resonance.Battle/Core/BattleSim.cs：ResolveSkill、UsePlayerSkill、Cast、ApplyLinkedEffect。
- client/Assets/Scripts/Resonance.Battle/Core/EffectCapability.cs：CollectOverlayImportViolations、OverlayRowBlocksImport。

**当前调用链**
1. InstallTables 计算 PlayableSkills/PlayableEffects，但 TrySkill 仍读 Skills（资料全表），PlayableIds 只按非敌人角色收集，没有依赖闭包门禁。
2. UsePlayerSkill 先扣充能、设置 CD/Drive，再调用 Cast。
3. Cast 已经可以结算基础伤害并记录 cast；随后 ApplyLinkedEffect 才 Check(fx)。
4. 不支持的效果只写 unplayable_effect 并 return，缺效果只写 missing_effect 并 return。对外输入仍可能 Accepted，战斗继续。

这不是“旧效果已不执行所以问题消失”，而是“玩家仍能使用一个被截掉部分效果的技能”。原计划要求未知/未支持语义不可静默当完整技能通过。

还存在旧 ID 例外：导入用 baseline.ContainsKey(id) 判断历史库存。把既有 atk_up 的 kind 改为 Reflect（opcode 仍 status.apply）会走历史库存容忍分支。容忍库存本身可以保留，但必须确保它以及依赖它的技能不会进入可玩链路；当前 TrySkill/PlayableIds/晚检查并没有保证这一点。

**最小修复**
- 实际入场检查选中双方角色→技能→效果依赖闭包；不支持者在战斗开始前阻止进入，不能删掉用户原始资料。
- 真正施法路径也要先验证完整技能及其本次 overlay，再消耗资源和结算。失败应明确拒绝/失败，不能先伤害再跳效果。
- 不要把已有 builtin 缺口变成整个应用无法启动；资料仍可查看，测试场景选受支持的合成角色/技能，开发替代必须显式命名与记录。
- 相同 ID 的新内容必须重新校验；不是“名字以前存在”就天然继承可玩权限。

**验收反例**：已有有效效果 ID 被改成未支持 kind；损伤技能关联未支持/缺失效果；两种情况下都不得产生“Accepted + 已扣资源/伤害 + 效果被忽略”的成功执行。

### A53-02 / P1：数据身份仍遗漏会改变战斗结果的字段

**路径**：BattleReplay.cs 的 BattleContentIdentity.AppendChars/AppendSkills/AppendEffects/Compute；BattleSim.Commands.cs 的 ContentFingerprint；DesignPlaceholderPolicy.cs。

当前已补 HP、FlatPower、Trigger、PeriodSec、关卡数据，值得保留；但“完整身份”仍不成立：
- AppendChars 未计 Element、AutoSkillId 等实际被战斗读取的字段。
- AppendSkills 未计 Type、FlatHeal、HealMaxHpFrac、SkillFlat 等。
- AppendEffects 未计 Opcode、Group、SourceTier，以及本轮新加的 HasTarget、Target、Side。
- 技能/效果 overlay 和 HonorDeclaredAutoDriveGain 这类会影响执行的外部策略没有形成完整的逐场身份。

例如只把效果 Side 从 Ally 改为 Foe，执行目标改变，当前指纹却不变。不是哈希算法的问题，而是根本没有把该字段送入哈希。已有 R04 只证明所列少数字段改变指纹，不证明全部关键字段。

**最小修复**
- 用一份明确、稳定排序的战斗配置快照生成身份。只需覆盖当前模拟真正消费的字段，不要求把纯美术内容全量哈希。
- 包含有效 overlays、逐场执行政策和实际关卡/阵容；不只依赖全局 Catalog 或常量 rules 字符串。
- 只改 Side、Element、FlatHeal、效果 Opcode 或执行策略的表驱动测试必须导致身份不同。
- 每次 Verify 使用独立诊断上下文；静态 Divergences/Unconsumed 不得被另一轮验证清空。若暂不支持并发 Verify，必须由真实入口串行约束并测试，不能只在说明里提醒。

### A53-03 / P2：JSON 的 hasTarget=false 被读成 true

**路径**：CatalogJson.cs 的 ReadEffect、OverlayEffect；TargetSemantics.Rule。

两处逻辑判断“是否存在 target 或 hasTarget 键”，随后无条件 HasTarget=true。显式传 hasTarget:false 也变成 true；ReadEffect 若没有 target，Target 还会读成默认 0（Self）。这会把原本应继承技能目标的效果变成自我施加，或让 overlay 无法关闭旧的目标覆盖。

**最小修复**：区分缺键、false、true。false 应取消覆盖；true 要求合法 target；仅提供 target 的兼容语义应明确。给三种输入各写反例，不再用键存在冒充布尔真值。

示例：在一个有效效果上 overlay {"id":"atk_up","hasTarget":false} 后，HasTarget 应为 false；一个 AllAllies 技能仍应按技能目标选边。

### A53-04 / P1（验收链路）：L01/L02 测错代码层，隔离跑绿不能代替完整回归

**路径**
- tools/BattleSim.Tests/G2RecheckEvidenceContractTests.cs：FindStoreType/CreateStore。
- tools/BattleSim.Tests/BattleSim.Tests.csproj。
- client/Assets/Scripts/Resonance.App/Debug/NaturalPlayBattleEvidence.cs。
- tools/BattleSim.Tests/G2RecheckFixtures.cs；artifacts/REGRESSION_LOG.md。

L01/L02 只在 typeof(BattleSim).Assembly 找名字形似 BattleEvidence/RecordStore 的类型，还要求无参构造和猜测的 Save/Load 方法。测试项目只链接 Resonance.Battle 下的源码，新增记录器却在 Resonance.App/Debug。这种 FAIL 说明测试没有对上实现，不能直接推出“分场记录器不存在/第一场一定仍丢失”。

原复查要求的是“行为正确”，没有要求记录器必须使用那个类名、在那个程序集或采用那个构造函数。**不要为满足反射猜名再造第二套存储系统。**

**最小修复**
- 直接对现有 NaturalPlayBattleEvidence.Begin/Persist/WriteSessionIndex 做强类型行为测试。该文件当前只依赖 System 与 Resonance.Battle，可以选择在 .NET 测试项目显式链接这一份纯 C# 文件；也可以使用合适的 Unity 测试程序集。不要为了测试把整个 App UI 链进 .NET。
- 在临时目录写入两场记录，读取第一场文件并验证哈希不被第二场改变。重复保存同一场不得用 live 参数误写另一场对象；绑定对象/battle_id 检查。
- 真正的 NEXT/HOME 时机仍须 EventSystem/Unity 集成测试，存储单测不能代替。

另外，回归日志明确承认与 Catalog 变异并行时会污染 DataIdentity/静态 Divergences。G2RecheckCatalog 的局部锁/collection 不能保护仍在其他 collection 执行的 G2Review。应把共享全局数据的测试放进同一受控隔离安排，或显式让这一测试程序集串行；独立子代理仍可最大并行。

修完后执行完整测试入口并保存原始结果。不能只挑每个 filter 单跑为绿，就忽略标准入口的共享状态串扰；串扰是测试设施缺陷，也不是无意义失败。

### A53-05 / 验证未完成：实际跑完自然矩阵与对应构建

NaturalPlayRuntime 已有预配置 basic/fever/auto、pre-next 保存，并非完全没有工作。但 CURRENT_STATE/REGRESSION_LOG 都明确本批 Unity、自然矩阵、构建 NOT_RUN，三项自然测试只是 Skip 合同。因此尚不能外部确认正常加载、真实输入、完整持久化或回放已通过。

**同批交付要求**
- 新固定源码快照上的 basic/fever/auto 三组真实 EventSystem 结果；Fever 两个槽位的成功输入需检查 Source=Player，不能误把 Auto 的成功命令算成人工操作覆盖。
- 场景配置允许开战前合成；开战后不可改 HP/充能/Drive/时间/胜负，不强制 Perfect，不降低必达项。
- 第一场结算→NEXT→下一场→HOME 的分场记录。头、命令、事件、结果均对应 battle_id。
- 当前记录器只是冻结一次 RunHeader 文本，没有直接使用丰富的 BattleInitialHeader，也没有导出既有回放器的标准记录格式。应接现有序列化/回放流程，用真实写出的文件在新模拟实例中读回验证；不可只在同一内存对象中比较。开局成长/装备、政策、数据身份需足以重建或明确验证所需配置。
- 原始测试结果、Unity 编译/运行日志、连续录屏、对应构建及文件哈希。无法生成的工件明确 NOT_RUN/BLOCKED，不能用历史 203/203 或旧 dist 替代。
- 更新短入口的源码基线；保留历史证据，不把尚未运行的版本写成当前全套通过。

## 三、下一批范围固定

只做上述 A53 项，不重做已修复的选边/沉默/原子安装/QTE/Measured；不增加玩法、不重开已暂停的资料搜索、不切换动画系统、不进入 G3。

继续环境允许的最大并行子代理。共享文件单写入者，重启前核对旧任务；独立代理并行不等于让多个测试进程抢同一 Catalog/产物目录。不要为本批新建调度平台。

阶段状态并列：PATH_A=NEEDS_FIX；M1_FIDELITY=DEFERRED_NOT_REMOVED；G3_NOT_STARTED。

## 四、固定版本来源

所有路径都基于 1a752532e39560b861c2a661db66c97ca18ffece：
- DC_RECON_KIT/m1/G2_RECHECK_20260914/artifacts/CURRENT_STATE.md
- DC_RECON_KIT/m1/G2_RECHECK_20260914/artifacts/REGRESSION_LOG.md
- client/Assets/Scripts/Resonance.Battle/Core/BattleSim.cs
- client/Assets/Scripts/Resonance.Battle/Core/BattleSim.Commands.cs
- client/Assets/Scripts/Resonance.Battle/Core/BattleReplay.cs
- client/Assets/Scripts/Resonance.Battle/Core/EffectCapability.cs
- client/Assets/Scripts/Resonance.Battle/Core/TargetSemantics.cs
- client/Assets/Scripts/Resonance.Battle/Core/FormulaProfile.cs
- client/Assets/Scripts/Resonance.Battle/Content/Catalog.cs
- client/Assets/Scripts/Resonance.Battle/Content/CatalogJson.cs
- client/Assets/Scripts/Resonance.Battle/Content/VerificationScenario.cs
- client/Assets/Scripts/Resonance.Battle/Content/VerificationCatalog.cs
- client/Assets/Scripts/Resonance.Battle/Content/DesignPlaceholderPolicy.cs
- client/Assets/Scripts/Resonance.App/Debug/NaturalPlayBattleEvidence.cs
- client/Assets/Scripts/Resonance.App/Debug/NaturalPlayRuntime.cs
- tools/BattleSim.Tests/BattleSim.Tests.csproj
- tools/BattleSim.Tests/G2RecheckEvidenceContractTests.cs
- tools/BattleSim.Tests/G2RecheckFixtures.cs

这些来源是源码与提交方记录。本文设计的反例状态都是 NOT_RUN，不是独立执行报告。
