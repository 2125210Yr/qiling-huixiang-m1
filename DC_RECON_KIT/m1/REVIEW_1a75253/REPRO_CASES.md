# 1a75253 对应反例与验证目标

状态：全部 NOT_RUN。本审查没有执行 C#/Unity。以下是有限的补充测试设计；可合并到既有测试，不要求按这些名字新增生产类型。

| ID | 构造 | 必须验证 |
|---|---|---|
| A53-T01 | 有效损伤技能关联不支持的 status.apply + Reflect；用合法入口选择/施放 | 入场或施放前明确拒绝/失败；没有成功命令加部分伤害/资源消费 |
| A53-T02 | overlay 既有 atk_up ID 为未支持 Reflect；依赖它的既有技能仍被角色引用 | 历史 ID 不构成可玩豁免；库存容忍不使技能静默上场 |
| A53-T03 | 只改变效果 Side、HasTarget、Target、Opcode、Group、SourceTier；分别改变元素/AutoSkillId/FlatHeal及执行政策 | 实际行为相关字段任一改变均改变数据身份；相同配置稳定 |
| A53-T04 | JSON 含 hasTarget:false，无target；另测true+合法target、缺hasTarget+target及缺两键 | false取消覆盖；缺键保持定义语义；错误组合明确报错；不会无意改成Self |
| A53-T05 | 调用真实 NaturalPlayBattleEvidence 记录/落盘两场；第二场保存后重新读第一场 | 第一场文件不被覆盖；battle_id、头、命令、事件归属一致；误传另一Sim被拒绝 |
| A53-T06 | 测试项目编译实际记录器（可显式链接纯C#文件），而非反射猜未约定的类名 | L01/L02测行为，不对不存在的架构约定报错；无伪造第二套存储 |
| A53-T07 | 完整核心套件连续运行；共享Catalog的测试按统一隔离安排执行 | 无跨测试污染；原始TRX/日志对应固定源码；不得只选filter报告整体成功 |
| A53-T08 | 新Unity会话执行basic/fever/auto，第一场NEXT进入第二场再HOME；实际文件经标准回放读回 | 所有必达行为真实触发，Fever为两槽Player成功输入；完整分场证据、对应构建和连续录像 |

保持反例边界：核心单测可以配置初始状态，模拟边界；自然UI开战后禁止注入HP/充能/Drive/时间/胜负。不得为了“无注入”放弃核心边界测试，也不得把单测夹具带进自然流程。
