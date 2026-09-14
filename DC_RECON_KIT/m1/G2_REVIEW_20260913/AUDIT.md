# Destiny Child / 契灵回响：G2 仓库审查与下一步

审查日期：2026-09-13  
仓库：`2125210Yr/qiling-huixiang-m1`  
审查快照：`ed7a9ba6423f79aee9a4276f9a502c4e6d9cb6f3`（master 当时的 HEAD）  
审查方式：通过已连接的 GitHub 逐文件读取源码、测试、状态及契约，并与此前交付的执行包对比。未修改远程仓库。

## 1. 结论

继续复用 Unity 工程，不推倒重写，不重做 G0/G1，不自动进入 G3/M2。下一轮是 **G2 路 A 的工程收敛**：把能强制展示多个战斗状态的切片，推进为普通玩家通过实际界面输入可以完成、测试可以复现的切片。

本轮不是全仓每个文件的穷尽审计，也不是原作相似度评分。审查环境没有可用的 Unity 或 .NET/C# 运行工具，未重新执行仓库的 156 项测试或图形冒烟；未完成原片和重制画面的像素、音频或逐帧对照。下面区分源码能直接确认的实现、据此推导的风险，以及需要本地实际执行的回归测试。

仓库记录的测试 PASS 不是凭空否定，但不能改写成“外部审查者已重跑通过”。同样，合同目前记账为 0 分，不代表所有工程工作没有价值，而是没有获得合同要求的还原验收分。

## 2. 本轮必须遵守的新范围

`DC_RECON_KIT/m1/G1/DEFERRED_SUPPLEMENT.md` 记录了用户在 2026-09-12 同意先推进其他工作的决定：

- T27 像素叠图、T28 精确时序、T14–T20 数值还原及相关参考补件为 `DEFERRED_NOT_REMOVED`，仍保留在最终验收范围。
- 只使用已入库 P0/P1 推进路 A 的结构、可辨识文案、状态与工程切片；不擅自把其他地区、模式或用户投放资料提升为主参考。
- 公开关键词搜片、登录会话拉流和私服/模拟器补录继续暂停，不能因用户说“看看下一步”就重启。
- M1 仍未验收，G3 不自动开启。

此前工单里的“最多四个代理”已经被用户后续要求覆盖。仓库 `codex_templates/config.fragment.toml` 仍有四个的样例值，但它只是模板，不能据此判断本地实际并发。执行方应核对并使用当前环境支持的最大并行数量。

## 3. 原计划与当前快照的对照

| 领域 | 原计划要求 | 当前源码/记录 | 本轮决定 |
|---|---|---|---|
| 阶段与工程 | 保留框架，先审计和冻结契约 | GOAL_LOCK 记录 G0/G1 已过；实际项目在 client | 不重新从零规划 |
| 核心战斗 | 五动作、独立时钟、通用效果和目标选择 | Tap/Slide/Auto/Drive/Fever 已分支，Slide CD 与充能分开；有可变队伍人数、事件流和若干效果 | 复用，修行为与时钟问题 |
| 输入与回放 | 玩家、自动与回放可观测、可重现 | UI 和冒烟入口不同；有事件哈希测试，但不等于真实手势回放 | 统一命令路径，补真实界面 E2E |
| 数值 | 历史候选和 GL 未知隔离，按分支说明证据 | 默认核心 GL_UNKNOWN；普通应用入口显式使用 JP 对照；部分未知分支混入 Ok/Measured | 本轮修计算状态与证据状态，最终 GL 校准继续延期 |
| UI/表现 | 逻辑与表现同步，按参考复原 | 已有 HUD、QTE、SHOWTIME、Fever、换波、结算、角色适配；布局明确 NEEDS_REFERENCE | 修状态触发/阻挡/生命周期，不继续只换字符串 |
| 测试 | 核心测试 + Unity 测试 + 实际输入流程 | .NET 项目主要编译 Resonance.Battle；图形冒烟大量注入状态 | 保留状态展示测试，新增无注入实际操作测试 |
| 养成/全模式 | M1 后再验成长闭环与模式差异 | 有很多旧页面和预览按钮，部分直接循环修改等级/突破 | 不算完成的养成系统；暂不扩新模式 |
| 动态角色 | 原生骨骼先保留，接口隔离 | BattleHud 使用 ICharacterPresentation/BindExisting；大厅有其他展示适配 | 不在本轮全面切换动画方案 |

## 4. 主要发现

### R01：图形冒烟主要证明状态可以展示，不证明玩家自然操作能走通

证据：`client/Assets/Scripts/Resonance.App/Debug/VerticalSliceSmokeRuntime.cs` 的 `WaitIsolatedActions`、`FireDrivesUntilFever`、`HoldForDrive`、`Finish`、`DriveRematch`、`PassAfterHomeReturn`。

该脚本会直接切页，补满充能，填充 Drive，调用 Perfect，直接施加控制/护盾，强制排空 Fever，清理部分正在播放的 VFX，然后截图。它还允许某些界面、换波和 LEVEL UP 超时后跳过；最终 PASS 没有把所有相关状态旗标列为必要断言。

这是一种有用的“表现状态展示测试”，不是作弊指控，也不应删除。但是它不能替代普通玩家输入测试。再战调用 `StartVsBattle()`，也不证明结算界面的实际按钮可用。

此外，`tools/BattleSim.Tests/BattleSim.Tests.csproj` 的生产代码引用是 `Resonance.Battle/**/*.cs`；这套 .NET 测试不等于实际运行 Unity 的 HUD、手势、遮挡和界面生命周期。

处理：保留并明确命名为状态展示测试；新增独立的 NaturalPlay/E2E。测试初始数据可以是公开可见、固定的合成测试关，但开战后只发送正常玩家输入，禁止改 HP、充能、Drive、时间、胜负或强制清除 VFX。必经状态缺失必须 FAIL，不得跳过后整体 PASS。

### R02：手动 Fever 没有通过玩家连点来决定出手

证据：`BattleSim.cs` 的 `TickFever`、`SettleFeverHit`；`BattleHud.cs` 的 `OnPortraitTap` 和肖像手势绑定。

当前 Fever 由计时器持续出手，不检查是否处于手动模式，也不等待 Fever 点击命令；每次使用 `FirstAlive(Allies)`，而不是被点击的角色。肖像点击路径仍是 Drive 或普通 Tap，没有独立 Fever 输入。

默认实现为每三个 30Hz tick 消耗一次攻击预算，约每模拟秒十次；预算为七十次。所以在无暂停等干扰的默认条件下，预算约七模拟秒耗尽，早于声明的十四秒时间窗。这是当前代码参数之间的关系，不是对原作时长的重新定论。

处理：将 Fever 的窗口、命中预算、输入节流、施法者选择及自动点击政策拆开。手动输入由玩家触发，自动模式通过同一命令入口生成输入。精确原作参数继续标待验证，不为通过验收随意宣称新常量正确。Fever 独立通道不意味着永远使用与角色技能数据无关的固定系数。

### R03：QTE 有两个计时权威，末态回调防护也不足

证据：`BattleSim.cs::ReleaseBlocks/ResolveDrive`；`BattleHud.cs::TickQte/FinishQte/OpenQte`。

核心以 `DriveQteScalesWithSpeed` 和模拟步推进 QTE；HUD 又用 `Time.unscaledDeltaTime` 维护 `_qteT` 并自行超时。两边还读取不同层面的时限。核心在等待 QTE 时以 qteDt 扣关卡时间，没有继续独立使用关卡倒计时政策。

因此倍速、暂停、界面重建时存在判定与显示不同步风险；本轮没有运行 Unity 来测出具体偏移量，不应凭源码声称已经现场复现了多少秒误差。

`ResolveDrive` 只检查是否有待决槽位，没有统一拒绝终局等失效状态；HUD 在调用它之后也未检查成功与否就继续展示评价。需要建立终局后输入和迟到回调的无副作用测试。

处理：核心唯一维护 QTE 状态和时间；HUD 读取快照并提交玩家操作。模拟时钟、关卡倒计时和展示时钟分别定义，不再两套代码独立判结束。

### R04：可复现被实现成了关闭暴击

证据：`GameRoot.cs::StartBattleAt`；`BattleSim.cs::SettleSkill`。

普通入口设置 `Deterministic=true`；技能结算在这个标记下强制 `crit=false`。这意味着此入口的普通战斗不发生暴击，并非仅仅固定随机种子。

处理：正常预览使用带种子的随机流。同一初始状态、数据版本和命令序列仍可重复得到同一结果，同时允许发生暴击与其他随机行为。需要完全无暴击的公式夹具可以保留独立测试选项，不与“可回放”混用。暴击概率公式本身仍是待校准/设计参数，本轮不冒充最终 GL 曲线。

### R05：效果能力登记还不能证明具体语义真正生效

证据：`EffectOpcodes.cs`、`BattleSim.cs::UnitState/SettleEffect/TickOne`、`m1/G1/EffectSchema.md`、`M1EffectOrderTests.cs`。

已确认未知 opcode 会失败，这是正确的防护。但还存在更细的能力缺口：

- Silence 映射到可执行 control.apply，但行动锁只判断 Stun/Freeze；ChargeSpeed 映射到 charge.rate，但充能倍率只读取 ChargeHaste。
- MaxStack 没有在已读到的状态施加逻辑中参与堆叠更新。
- 限时状态过期仅移除状态列表项，护盾数额独立留在 UnitState.Shield，缺少显式生命周期关联。应测试并决定到期、驱散、替换分别如何处理，不把持续吸收当作天然正确。
- 穿防、额外伤害、驱散、目标重选、复活等仍不在 Implemented 列表；相关“抛错通过”测试证明防护有效，不证明功能已完成。
- 新 EffectSchema 把毒与流血合并为同类触发。原始计划只明确指出毒不能一律当周期 DoT，不能把这条要求自动扩写为所有持续伤害都采用相同行为。此次不重新联网考据；先隔离触发政策与证据状态。

处理：能力声明细化到 opcode + effect kind + 必需参数。已实现的语义须有运行测试；未实现的语义在数据校验时报错。状态持有者应能释放到期或移除时的副作用。不是要求本轮一次做完所有角色技能。

### R06：能算出数字与“有实测依据”仍被同一状态表达

证据：`FormulaProfile.cs`、`DamageMath.cs::Resolve/ComputeDs`、`M1FormulaIsolationTests.cs`、原执行包 `02_NUMERICS_AND_CALIBRATION.md`。

GL_UNKNOWN 不返回伪数值的防护有效；主应用为了演示明确使用 JP 对照，这本身可以保留，但不是最终国际服。

当前 `FormulaResult.Measured` 由 Ok 状态推导，JP/KR 所有可返回值的分支都会成为 Ok。实际 Drive 分支使用一套可计算系数，原执行包却没有为“实际对敌 Drive”提供已验证公式；已有测试反而要求 jpDrive.Measured 为 true。

另有可直接比对的历史参照偏差：KR Tap 增加二次防御衰减，KR Slide 没有补原计划声明的对应衰减；TruePierce 未限制原候选的适用防御区间。这里应做候选分支一致性审计，而不是让它们升级为 GL 最终真值。

处理：计算状态和证据状态分离。工程预览可以得到计算值，同时标注 DesignPlaceholder；历史候选逐分支带来源和适用条件；没有支持的分支不得仅靠 profile 名字成为 Measured。保留 GL 严格验证入口不接受未验证值。不要把主入口简单切回 GL_UNKNOWN，导致所有战斗不能结算。

### R07：实际手势与冒烟/探针并非同一入口

证据：`GameRoot.cs::Tap/Slide` 有 HitChainProbe；HUD 的肖像输入直接调用 BattleSim；冒烟通过 GameRoot 或 SliceDriveSequence 执行。

处理：建立一个最小公共输入入口，玩家手势、自动策略、回放都提交同一种命令；拒绝原因、槽位、序号、tick 可记录。不要借此另建大型 Agent 或第二套战斗系统。

### R08：养成界面出现，不等于养成循环已实现

证据：`GameRoot.cs::DrawInspect` 的等级/突破回调直接递增并在上限归零或归一；`DrawResult` 在胜利时触发 LEVEL UP 展示；STATUS 明确其数据是 stub。

这是已有原型，不要求本轮扩建为完整 M2。应隔离开发预览控件，不把弹窗出现或再调用 StartVsBattle 计作真实养成、消耗和存档事务验收。

### R09：旧入口文档与新延期决定互相冲突

证据：`GT6_REVIEW.md` 仍描述旧的私有、源码审核状态；`STANDING_ORDERS.md` 留有缺 GT 持续搜片/重启条款；9 月12 日补件已经暂停相关工作；并发模板仍为4。

处理：更新一份短的当前入口说明，并在旧文档显著标注被哪条新决定覆盖。不要重新写完整合同。进程看门狗只保障就绪任务运行，不能把不断重启、重新写同一状态文件当作交付。

## 5. 下一批任务的顺序

第一批先有回归：冻结此快照对应的问题，新增暴击、手动 Fever、QTE 时钟、末态回调、效果能力与状态到期的测试。由独立 QA 写出红测试，不能靠改变断言来绿。

第二批同时修核心和 UI：共享 BattleSim 文件由一个集成者落盘；其他代理在独立工作区提交边界清楚的补丁、组件和测试。Fever、QTE、随机、效果、输入日志、预览公式证据、UI 状态可以并行准备，避免互相覆盖。

第三批完成真实操作：从普通入口走到战斗、结算、返回/再战；截图状态展示与自然流程分成两套结果；产出运行包、录屏、事件/命令日志和测试原始结果。

以3—5天作为本批收敛时间盒，而非完成整个游戏的承诺。第一天应看到新增失败测试和一条真实输入路径的录像或明确阻塞；不得把工作时间主要消耗在改文字、重复搜片和扩充状态报告。

## 6. 批次结束怎样报告

建议新增内部批次状态 `PATH_A_ENGINEERING_PASS`，它不是 M1 相似度验收，也不改旧合同权重。

工程通过至少应满足：关键内部回归通过；规定的真实输入链路完成；必经步骤不跳过；无测试状态注入；无终局后的额外结算；能提供可运行构建或明确的构建环境阻塞；测试、视频、数据和提交可追溯。

并列保留：`M1_FIDELITY=DEFERRED_NOT_REMOVED`、`G3_NOT_STARTED`。缺参考不阻止本批工程任务结束，但不能自动宣称整个 M1 通过。完成本批后交用户复核，而非继续无限循环或自行开 M2。

## 7. 来源定位（全部为上述固定提交，原文件除外）

| 编号 | 路径 / 定位 |
|---|---|
| S01 | DC_RECON_KIT/GT6_REVIEW.md |
| S02 | DC_RECON_KIT/GOAL_LOCK.md |
| S03 | DC_RECON_KIT/STANDING_ORDERS.md |
| S04 | DC_RECON_KIT/m1/G0/STATUS.md，2026-09-13a/b/c/d记录 |
| S05 | DC_RECON_KIT/m1/G1/DEFERRED_SUPPLEMENT.md |
| S06 | DC_RECON_KIT/m1/G1/CombatContract.md |
| S07 | DC_RECON_KIT/m1/G1/EffectSchema.md |
| S08 | DC_RECON_KIT/codex_templates/config.fragment.toml |
| S09 | client/ProjectSettings/ProjectVersion.txt：6000.3.23f1 |
| S10 | client/Packages/manifest.json：现有 Test Framework/Input/UGUI/2D 等依赖 |
| S11 | client/Assets/Scripts/Resonance.Battle/Core/BattleSim.cs |
| S12 | client/Assets/Scripts/Resonance.Battle/Core/DamageMath.cs |
| S13 | client/Assets/Scripts/Resonance.Battle/Core/FormulaProfile.cs |
| S14 | client/Assets/Scripts/Resonance.Battle/Core/EffectOpcodes.cs |
| S15 | client/Assets/Scripts/Resonance.App/Core/GameRoot.cs |
| S16 | client/Assets/Scripts/Resonance.App/Battle/BattleHud.cs |
| S17 | client/Assets/Scripts/Resonance.App/Debug/VerticalSliceSmokeRuntime.cs |
| S18 | tools/BattleSim.Tests/BattleSim.Tests.csproj |
| S19 | tools/BattleSim.Tests/M1CoreSliceTests.cs |
| S20 | tools/BattleSim.Tests/M1EffectOrderTests.cs |
| S21 | tools/BattleSim.Tests/M1FormulaIsolationTests.cs |
| S22 | DC_RECON_KIT/docs/reference/gl-shutdown-pve/our_slice/shots_20260913d/：确认有截图文件，未据此冒充本轮视觉对照 |
| S24 | client/Assets/Scripts/Resonance.Battle/Core/Definitions.cs：技能与效果参数字段 |
| S25 | client/Assets/Scripts/Resonance.Battle/Core/Enums.cs：ChargeHaste=6 与 ChargeSpeed=23，不是同值别名 |
| S23 | 原交付文件 DC_Reconstruction_Report_2026-09-09.md 与 05_GOAL_PROMPTS.md：原计划、历史公式边界、验收要求；旧并发4已被后续用户要求覆盖 |

不要把本审查中的建议测试写入“已执行测试结果”。实际执行后另行产生新的提交、日志与状态。
