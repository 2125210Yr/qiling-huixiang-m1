# 2a00445 新增回归设计

**状态：全部 NOT_RUN（本审查环境未执行 C#/Unity）。**
下面是给本地开发/QA的反例设计，不是已经新增到仓库的可执行测试，也不是声称这些用例已由审查者跑红。核心单测可以构造边界；自然 UI 用例开战后不得写入模拟状态。

| ID | 层 | 最小输入/步骤 | 必要断言 |
|---|---|---|---|
| N01 | 自然UI | 明示工程场景，从普通关卡入口进入；手动Tap与Slide | 成功Submit类型、槽位与命中/效果事件正确；上滑不同时触发Tap/Drive；不靠截图存在计通过 |
| N02 | 自然UI | 有限耐久的预配置场景，实际QTE积累至Fever，用两个不同存活槽位点击 | Fever必达；两个槽位各有被接受的FeverTap和匹配施法者事件；无中途加条或强制Perfect |
| N03 | 自然UI | 真实自动按钮切换，观察自动技能/Drive/Fever，再切回手动 | 自动策略经过同一命令入口；不重复施放；手动权属与反馈一致 |
| N04 | 参数/流程 | 自动技能DriveGain设为低于14的声明值，走普通攻击路径 | 结果遵循显式工程配置；隐藏14下限不能覆盖已声明值；不宣称该值是原作 |
| L01 | 证据 | 第一场成功→NEXT第二场→暂停→首页 | 两个battle_id；第一场完整Drive/伤害/result事件仍存在；第二场头不能代替第一场 |
| L02 | 证据 | 自然流程多次切战斗并结束测试 | 每场均有开局头、命令全字段和终局/退出快照；数据/源码版本与引用一致 |
| E01 | 核心技能链 | 创建Target=AllAllies、EffectId为ChargeSpeed的可执行技能，经Submit施放 | 所有符合目标的己方加速，敌方不改变；不是直接ApplyStatus |
| E02 | 核心技能链 | 同上，分别使用ChargeAmount和Barrier | 己方加条/屏障，敌方不受益；参数与目标数量被尊重 |
| E03 | 敌方策略 | 敌方充能就绪并持有Silence，执行Tick | 按工程规则无敌方Tap/Slide，普攻仍可执行；沉默到期后技能恢复 |
| V01 | 导入/活动门禁 | 已知status.apply + 未实现Reflect，参数合法，通过实际导入/激活入口 | 不得进入可玩内容；明确错误ID/op/kind；诊断库存与活动内容分离 |
| V02 | 导入原子性 | 先安装有效自定义数据A，再导入含未知opcode或坏参数的B | B抛错且活动数据仍为A，非builtin、非部分B |
| V03 | 依赖校验 | 候选技能引用候选effects表里的新增有效/无效效果 | 验证针对候选快照，不偷读旧活动Catalog；有效依赖可一起原子安装 |
| R01 | 实际回放器 | 自动模式在预设Fever场景运行，捕获再用BattleReplayer验证 | 自动Fever只执行一次；无双投命令；无被忽略的拒绝；状态与完整事件匹配 |
| R02 | 实际回放器 | Speed=3/Auto=Full开局，中途Submit改为2/Manual；捕获重播 | 开局头仍是3/Full，途中切换时刻一致，不推断成1/Manual |
| R03 | 回放判定 | 构造一个原记录accepted但重播拒绝的命令，同时保持末态数值可能相同 | Verify/Match必须失败并指出命令Seq/Tick/拒绝原因，不能只看末态 |
| R04 | 数据身份 | 分别改变角色HP、技能FlatPower/目标、Trigger/PeriodSec、关卡数据 | 完整版本/哈希变化；与磁带不一致必须报告，不以相同条目数量放行 |
| Q01 | 核心时钟 | QTE limit=TickDt，充足关卡时间、非致死Drive；开始QTE后一个Tick | TimeLeft恰好扣一次关卡政策步长，不重复扣；仅结算一次Drive |
| Q02 | 核心时钟 | Q01扩展到speed3，stage/qte缩放政策分别同/异 | 恢复tick及累计时长都遵守各自政策；不靠放宽误差吞掉一个完整tick |
| P01 | 数值证据 | 可计算的设计Drive或历史Tap候选 | Computed=true且Measured=false；不存在Measured==Computed过渡断言 |
| P02 | 数值接口 | FormulaResult.Bounds(min=10,max=20) | 读取区间合法；RequireInt不能悄悄返回Value默认0，需显式拒绝/选择策略 |

## 完成记录格式

为每个ID记录：源码提交/树、数据配置身份、测试名称、实际命令、PASS/FAIL/NOT_RUN/BLOCKED、原始结果路径、必要的battle_id/录像时间戳。

自然测试可以拆场景；不强制每场短战斗都有所有动作，但本批覆盖矩阵不能遗漏手动Fever、自动模式和真实Tap/Slide。
