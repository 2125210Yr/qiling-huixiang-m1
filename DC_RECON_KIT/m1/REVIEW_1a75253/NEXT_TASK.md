# 继续现有 G2：1a75253 复审后的有限收口

将本目录放到现有项目 DC_RECON_KIT/m1/REVIEW_1a75253/。向当前 goal 追加，不启动第二个改相同工作区的协调者。

```text
继续当前 G2 路 A，不重做 G0/G1，不开 G3。
先读取 DC_RECON_KIT/m1/REVIEW_1a75253/REVIEW.md 和 REPRO_CASES.md。
基线 1a752532e39560b861c2a661db66c97ca18ffece；HEAD 有更新就核对差异，已修项不回滚。

环境允许的最大并行 subagent；先核对旧任务，避免重复派发。
共享 BattleSim/Catalog/HUD 等文件由唯一集成者落盘。不要让并行测试共用可变 Catalog 或结果目录。

只做本批 A53-01 至 A53-05：
1. 接通整技能及依赖的可玩门禁：在消耗、基础伤害之前验证；库存可保留，非法技能不能先伤害再跳过效果并返回成功。验证同一历史 ID 改成不支持 kind 的路径。
2. 数据身份补全实际会改变执行的字段：Effect 的 Side/Target/HasTarget/Opcode/Group/SourceTier，Character 的 Element/AutoSkillId，技能实际数值、overlay 和逐场政策。用参数化测试覆盖，不另建哈希框架。
3. 修 JSON hasTarget:false；区分缺键/false/true，不擅自默认为 Self。
4. 修 L01/L02 的测试对象：直接测现有 NaturalPlayBattleEvidence，不为反射猜类名重建一个存储系统。写两场临时文件并读回。修共享 Catalog/回放静态状态的测试隔离，完整套件单入口稳定运行，保存原始结果。
5. 本轮必须实际执行 Unity basic/fever/auto 三组自然矩阵，接每场初始头、命令、事件、结束/退出快照与标准回放读回。Fever 两槽位为 Player 输入；开战后不得注入状态。交付固定源码版本对应的构建、连续录像和原始测试/运行日志。

保留本次已修的效果选边、敌方沉默、原子候选安装、QTE单扣时间、Measured/Bounds迁移、外部输入重放。
不增加游戏模式，不做完整养成，不扩大美术/动画工作，不重启暂停的公开搜片或模拟器采集。
修改验证场景的开战前 DESIGN_PLACEHOLDER 参数可执行，不要再为此停下来请用户选择；不得把它标为GL真值。

失败要保留原因和失败用例；不能放宽断言、把缺动作改成可选，或过滤失败测试来报告全绿。
不能执行的步骤标 NOT_RUN/BLOCKED；历史203/203不是本批结果。
完成以上收口后交用户复核，不自行扩到G3，也不无限循环扩写合同。
```
