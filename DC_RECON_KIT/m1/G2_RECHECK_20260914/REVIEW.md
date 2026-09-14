# G2 第二轮复查：2a00445

审查日期：2026-09-14。仓库：2125210Yr/qiling-huixiang-m1。
分支：m1-gt6-review。固定提交：2a00445c67dbb2ca9251d81428e3096b71c233fb。
上一轮基线：ed7a9ba6423f79aee9a4276f9a502c4e6d9cb6f3。

## 结论

**NEEDS_FIX。保留本轮修复，继续 G2 路 A；不推倒重写，不重做 G0/G1，不开启 G3。**

本轮已经有实质工程进展：真实 EventSystem 指针测试发现并记录了普通入口问题，手动 Fever、种子暴击、QTE 主计时和状态生命周期有实际修改。不能把这批工作说成只是改文档；也不能用 203 项核心测试代替自然游玩通过。

本次为固定版本源码、测试代码、原始 TRX 和提交中的自然游玩记录复核。未修改远程仓库。本环境未找到 dotnet/Mono/C# 或 Unity 运行工具；没有重跑 203 项测试、Unity Editor 或真机。未完成图片像素、音频或逐帧原作对照。下文新增反例均为源码路径分析和待运行测试，不是已现场复现的 C# 执行结果。

## 一、证据核对

| 项目 | 实际核对结果 | 可以证明什么 |
|---|---|---|
| 指定分支 | HEAD 为 2a00445，非旧 master 快照 | 本轮审查对象已固定 |
| 核心测试原件 | g2-review-regression.trx 的 Counters 为 total/executed/passed=203，failed=0，notExecuted=0 | 提交内确实保存了一份 203/203 的运行报告；不是审查者独立重跑 |
| 运行时间 | TRX 记录 2026-09-14 11:52 +08:00，早于最终提交 | 需进一步绑定测试时的源码树/文件哈希，不可仅靠后来提交了报告自动推定完全一致 |
| 自然游玩 run7 | FAIL BattlePlay，missing=Tap,Slide；叙述记录中两次 QTE 成功，未激活 Fever | 部分真实界面路径通过，但规定的核心交互未完整覆盖 |
| run7 事件原件 | natural-play.events.txt 仅 8 条 tick=0 初始化事件 | 第一场战斗的独立完整事件记录没有保存在该文件 |
| 新构建 | CURRENT_STATE 写明 NOT_RUN，旧 dist 不作为证据 | 不可称本批已交付新可玩构建 |
| 原作还原 | 精确数值/T27/T28 等仍延期 | 不评分，不宣布 M1 通过 |

CURRENT_STATE 中“当前 HEAD=d631573、脏树、未提交”是证据产生当时的描述。现在代码已在 2a00445 中，不能据此断言用户此刻仍未提交，也不能得知其本地工作区是否干净。应保留历史记录，新增当前快照对应的结果摘要。

## 二、上一轮问题：哪些改善了

| 原编号 | 本轮判断 | 边界 |
|---|---|---|
| R01 自然操作 | 有真实推进，未通过 | 新 NaturalPlayRuntime 通过射线命中和指针事件驱动；run7 正确保留 FAIL，而非跳过后冒充 PASS |
| R02 手动 Fever | 核心主要修复已落地 | 手动模式不再由计时器自动出手；点击指定槽位并检查节流/预算。自然 HUD Fever 覆盖仍不足 |
| R03 QTE 时钟 | 主问题已改，边界仍有缺陷 | HUD 读取核心时间；暂停/终局结算防护已增加，但 QTE 超时结束的那个 tick 会重复扣关卡时间 |
| R04 暴击与可复现 | 核心及普通入口修改可确认 | GameRoot 明确 ForceNoCrit=false；随机仍由 seed 决定。完整回放另有问题 |
| R05 效果语义 | 部分修复 | ChargeSpeed、Silence、护盾实例、堆叠/驱散有消费者；技能级选边、敌方路径、导入门禁未闭合 |
| R06 计算/证据 | 新字段有效，但迁移未收尾 | Computed/Evidence 已分开；公开 Measured 仍是旧计算成功别名，过渡测试还在固定旧语义 |
| R07 统一命令 | 部分完成 | 玩家手势经 Submit；自动 Tap/Slide/Drive 仍旁路，自动 Fever 又进入命令带，回放两种策略混用 |
| R08 成长预览 | 本批不扩展 | 真正养成属于后续阶段，不把它新增为本批必须建设的系统 |
| R09 入口/协调 | 已加覆盖说明，存在过期摘要 | 继续最大实际并行；恢复任务前先接管既有代理，避免重复启动同一批任务 |

这些判断仅涉及工程，不代表对应原作规则已经验证。

## 三、剩余问题与最小修复

### X01 / 高优先：自然测试尚未覆盖核心动作，且验收条件漏掉 Fever

来源：NaturalPlayRuntime.PlayBattleNaturally / MissingGoals；run7/natural-play.result.txt；BattleSim.TickUnit；BattleHud.OnPortraitTap。

run7 中没有成功执行 Tap 或 Slide，Fever 也未发生。脚本只使用 p0 尝试 Tap/Slide，并倾向于 Drive 满后立即使用它，所以该录像只能证明这个场景和策略没覆盖到动作，不能严格证明所有玩家、所有槽位、所有操作策略都绝对无法使用 Tap/Slide。

但确实存在一个需要检查的硬编码：普通攻击给 Drive 加值时使用 Math.Max(14f, autoSkill.DriveGain)。即使把表中的 DriveGain 改到 1、2、5，单次仍至少加 14。五名角色各打一次就至少贡献 70，容易在技能充能就绪前进入 Drive 输入模式。只增敌人血量或只改表里 DriveGain，未必解决完整节奏问题。

另外，MissingGoals 只包含 QTE/Tap/Slide/Pause/Speed，Fever 被明确写成“发生时再测、不是必需”。将来这几个 bool 为 true，即使 Fever 输入彻底坏掉，当前脚本也可能 PASS。这一层遗漏必须在本批测试矩阵补回。

最小修复：
- 保留原 run7 失败记录作为基线，不接受“缺 Tap/Slide 也算通过”。
- 使用明确命名、版本化的工程验证场景/配置。允许开战前设置合成敌方 HP、关卡时长、初始阵容和占位充能政策，所有内容必须标 DESIGN_PLACEHOLDER，不伪装原作数值。
- 不为测试通过擅自改掉原参考所要求的 Drive/Tap 映射。先记录充能完成、Drive 满、技能执行和战斗结束的时间线，再修不合理的工程硬编码或验证数据。
- 一个场景不必承担所有动作；可拆“基本操作”“手动 Fever”“自动模式”三个独立用例，每个有必须达到的交互和断言。全部用正式 UI/命令/结算路径，开战后禁止改 HP、Drive、充能、时间、胜负或强制 Perfect。
- Fever 用例要有真实输入产生的成功 FeverTap，包含不同存活槽位；QTE 成功/评价应匹配提交命令与核心事件，不能只凭窗口消失。
- 当前指针测试是 Unity EventSystem 层，不等于已完成操作系统输入或手机触摸验收。

### X02 / 高优先：第一场战斗日志在 NEXT 后被第二场覆盖

来源：NaturalPlayRuntime.WriteAndQuit / WriteEvents / BuildResult；GameRoot.StartBattleAt；run7 的 result/events。

自然测试末尾才调用 Battle() 读取当前 GameRoot.Battle。此时已经按 NEXT 进入 CH1-2，再回首页。结果报告上方叙述了第一场的 QTE/胜利，底部 RunHeader 和 CommandLog 却属于下一场，命令只剩一次 Pause；独立 events.txt 也只有新战斗的初始化。

这是可直接从源码和产物互相印证的记录缺陷。它不是说第一场没发生，而是第一场没有完整、可复播的独立证据。

最小修复：每场生成唯一 battle_id；开战时冻结初始头；切到下一场或销毁引用前保存命令、事件、结果和校验摘要；每场独立文件。场景流程报告引用这些 battle_id。至少对齐 battle-001 的 Drive/胜利、battle-002 的进入/暂停/退出，禁止用第二场头配第一场截图。

### X03 / 高优先：效果在直接单测里生效，经过真实技能路径仍可能用错对象

来源：BattleSim.Cast / TickUnit / UnitState.SkillLocked。

Cast 对效果选边使用硬编码白名单：AtkBuff、DefBuff、Shield、ChargeHaste、Taunt 才走己方，其余走敌方。新增的 ChargeSpeed、ChargeAmount 和 Barrier 未包含。因此一个声明 Target=AllAllies 的充能加速/加条/屏障技能，经过该链路会把敌人集合交给目标选择器。直接 sim.ApplyStatus(ally, fx) 的测试绕开了这个错误。

此外，Silence 的 SkillLocked 只在己方命令入口检查；敌方在 TickUnit 中充满后直接 Cast Tap/Slide，没有 SkillLocked 检查。仓库自己声明的“沉默阻止技能、普攻继续”的工程规则因阵营而异。

最小修复：
- 目标阵营由明确的目标语义/效果参数决定，不能维护一个不断漏项的正面效果白名单。
- 为 ChargeSpeed/ChargeAmount/Barrier 分别构造有实际 SkillDef/EffectId 的 AllAllies 技能，走真实技能结算；验证己方改变、敌方不变。
- 双方行动使用同一可用性判断。沉默阻止的是已声明的技能类别，不应无意停掉普攻。
- 不需要一次补全所有原作效果；已宣称支持的效果必须全路径有效，未支持的不能静默上场。

### X04 / 高优先：能力检查存在旁路；导入失败并不原子

来源：EffectCapability.ToViolation / ThrowIfExternalImportInvalid / RejectUnplayable；CatalogJson.Load；Catalog.Install；G2ReviewCapabilityTests。

能力表可以识别 status.apply+Reflect/Dot 等未实现组合，但 BlocksExternalImport 只阻止未知 opcode、参数错误或“整个 opcode 完全没有任何实现”。只要 status.apply 对其他 kind 有实现，这个无效组合就可能被放行。严格 RejectUnplayable 存在，却没有进入实际 Cast/ApplyEffect 的必经路径。

这与“细化到 opcode+kind+参数”的目标不一致。当前测试证明直接调用严格帮助函数会抛错；导入测试只覆盖完全未实现的 revive opcode，没覆盖“已知 opcode + 未实现 kind”。

另一个独立缺陷：CatalogJson.Load 先 BuildBuiltin（已经改变全局），后 Install 新表，再 ThrowIfExternalImportInvalid。调用者收到异常时，内存数据可能已经被重置或换成错误的新表。TryLoadDefault 的 catch 会重建 builtin，但不是保留导入前最后一份有效自定义数据，也不能保证其他 Load 调用者安全。

最小修复：
- 分离“资料库存储/诊断”和“可上场内容”政策。未知历史内容可保留在非可玩库存中；实际使用的角色/技能/效果依赖闭包必须严格验证。
- 临时容器中解析、叠加、验证全部候选数据，通过后一次交换活动快照。失败不改变现有活动表。
- 不以保留 builtin 的 JSON roundtrip 为理由，对所有外部未实现组合放行；也不要求删掉用户的原始资料。
- 新增导入前后哈希/对象内容断言；既验证 throws，也验证没有污染现有数据。

### X05 / 高优先：统一命令和回放混用了两种驱动方式

来源：BattleSim.AutoFireSkills / AutoFireDrive / TickFever；BattleReplay.BattleRunRecord.Capture / BattleReplayer.Replay / DrainAt / Compare。

自动 Tap/Slide/Drive 仍直接走 Try*；自动 Fever 经 Submit 且写入 CommandLog。BattleRunRecord 收集所有来源命令，BattleReplayer 又把所有 accepted 命令重放，同时继续正常 Tick 自动策略。

这样自动 Fever 同时可能被 Tick 重生、又从命令带再投一次。第二次可能被节流拒绝，也可能在其他条件下产生差异。更严重的是，SubmitReplay 的拒绝只写入静态 Divergences，Verify/ReplayReport.Ok 不把它计入结果：即便命令重放失败，只要最终摘要/事件摘要相同仍可能判 Match。这里是已找到的双驱动与判定缺口；具体 fixture 的 C# 结果需要执行新增回归验证，不声称本环境已经跑出它。

初始数据也有问题：InferInitialSpeed/Auto 从事后的变更命令猜初始值。假设实际以 Speed=3、Auto=Full 开局，之后改为 Speed=2、Auto=Manual；若无 tick=0 的设置命令，记录会猜成初始 Speed=1、Manual。不是可靠的回放头。

ContentFingerprint 只覆盖部分技能/效果字段和角色数量，未覆盖角色实际 HP/ATK、关卡、技能平值、目标、Trigger/PeriodSec 等；不可把它当完整战斗数据身份。

最小修复（推荐策略）：
- 采用“外部输入重放”：玩家外部指令录制/重放，自动策略在模拟中确定性再生成，自动命令也走 Submit 以便观测，但不从磁带二次投递。
- 不改为一半重放、一半重生；若选全命令驱动，则必须关闭对应策略并记录 tick 内阶段顺序。
- 开战时显式冻结初始 seed、角色/成长/装备/leader、mode/clock/profile、auto/speed 和完整版本化数据身份。不要从终局推断初始设置。
- 验证把拒绝、命令遗漏/未消费、版本不匹配纳入失败报告；不只比较最终血量或事件数量。
- 核心 RNG 有暴击且同 seed 一致的修复保留，不回滚为禁暴击。

### X06 / 中高优先：QTE 超时结束的那个 tick 仍重复扣时

来源：BattleSim.ReleaseBlocks / Tick；G2ReviewQteClockTests。

等待 QTE 时 ReleaseBlocks 扣 stageDtQ。当 QTE 达到时限，ResolveDrive(Good) 后返回 true，外层 Tick 接着又扣 stageDt。普通等待的 tick 扣一次，超时恢复的 tick 扣两次。

现有 Q02 检查的是等待中的前十个 tick，Q01 检查 QTE 时限和一次结算，没有断言恢复那个 tick 的 TimeLeft 总扣减，所以它们通过不能排除这个缺陷。

最小回归：非致死阵容，Speed=1，QteLimit=TickDt，足够 TimeLeft；开始 QTE 后执行一个 Tick，断言关卡只扣一次政策时间。再测 speed=3、QTE/关卡两种缩放政策相反的组合。由统一 tick 预算/明确分支处理恢复，不在两处重复消耗同一时间。

### X07 / 中优先：Measured 过渡别名没有完成迁移

来源：FormulaProfile.Measured；BattleSim.TryResolveCombat；G2ReviewProvenanceTests.P01。

当前已有 Computed 和 Evidence，这是正确进展；主计算调用方已用 Computed。但公开 Measured 仍返回“Status=Ok 且 Code!=NOT_MEASURED”，所以 DesignPlaceholder 的可计算值仍可 Measured=true。

注释说等 BattleSim 迁移后再修，可迁移已经完成。新测试还写 Assert.Equal(r.Computed, r.Measured)，把本该短期存在的过渡行为固定了。

最小修复：查清调用者，把“是否有数值”统一迁到 Computed；Measured 改为 Evidence==Measured 或删除/禁用旧别名。仅有区间结果的 RequireInt 也应拒绝而不是默取 Value=0。不要改成 GL_UNKNOWN 所有预览都不出数。

## 四、协调、证据与范围

INTEGRATOR_LOCK 记录：第一批 7 个代理在启动被中断后仍运行，第二批 7 个重复启动导致覆盖。不是环境最大并行本身有问题，而是同一任务没有唯一身份与接管机制。

下一批继续最大实际并行，但采用 batch_id + task_id + worktree/路径租约；会话恢复先查询已有代理，接管或取消重复任务，不直接再次 spawn 相同任务。共享代码由唯一集成者合并；独立测试/修复/导出可以并行准备。不要以“最后磁盘版本能通过测试”替代语义对齐。

旧状态文档保留为历史，不伪装成最新 HEAD；添加一个短的本次结果页就够了。不重写整份研究合同，不为了冲突修大量无关目录。

公开搜片、模拟器、精确 GL 数值与像素/时序对照仍按已有延期决定暂停。没有参考不是这些工程缺陷的阻塞理由。R08 真实养成不是本批新增建设任务；原型保持隔离即可。

## 五、下一个批次的放行条件

1. 保留已修复部分，通过新增反例和原有回归，不用降低断言来通过。
2. 基本操作、手动 Fever、自动模式各有真实界面输入证据；不要求每个短关都涵盖所有动作，但约定矩阵不能漏。
3. 第一场与后续场的事件/命令/初始数据分别完整保存，回放使用实际 BattleReplayer，而不只使用测试文件里另写的重放循环。
4. 已支持效果走实际技能路径正确；未支持的活动内容被严格拦截；导入异常不污染已安装数据。
5. QTE 边界、Measured 语义收尾。
6. 固定源码提交后重跑，附源码树/配置哈希、命令、原始结果和运行包；工具缺失则明确 BLOCKED。构建不公开再分发无权发布的素材。

工程批次满足条件才标 PATH_A_ENGINEERING_PASS；M1_FIDELITY 继续 DEFERRED_NOT_REMOVED；G3 不自动开启。

完整任务指令见 NEXT_GOAL.md；新增反例定义见 REGRESSIONS.md，全部初始为 NOT_RUN。
