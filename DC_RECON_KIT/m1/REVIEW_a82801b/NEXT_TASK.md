# 下一轮：a82801b 有限收口

将本目录放到 `DC_RECON_KIT/m1/REVIEW_a82801b/`。继续现有协调会话，不再启动另一个会修改同一工作区的主协调者。

```text
继续当前 G2 路 A。读取 REVIEW_a82801b/REVIEW.md、REGRESSION_CASES.md、EVIDENCE.json。
基线 a82801b80fd81fb4f8ff30cf45b72b30fcd63521；HEAD 有后续改动先核对，不回滚已修项。

保留：严格 Unplayable 门禁、命名合法替代、实际 ActiveStage 身份、现有记录器及外部输入重放、已经跑出的自然流程。A1 的基线原始报告已对齐，不再说它是十项失败，也不把它冒充最终源码的测试。

只收 C1–C4：
C1：VerificationCatalog.CloneEffects 保留 Side/HasTarget/Target 等完整字段。
通过真实技能链验证 C001 Drive 的 burst_atk 不加给敌人，盾/嘲讽保持声明目标。
只允许具名替代修改其声明范围；不要靠加血降攻掩盖错阵营。

C2：解决 BattleHud 直接写 HoldSim、用 unscaledDeltaTime 释放导致的无记录模拟停顿。
优先让核心固定 tick 政策维护停顿，HUD 只读；保留演出，不硬塞旧样本29tick。
把实际敌方点击接到已有 FocusEnemy Submit，不再直接绕过命令记录。
保留所有回放状态/事件/版本断言；未记录的历史控制不能通过忽略差异变绿。

C3：新 Capture 不再通过 OpeningGrowth.Search 反推开局。
保存首个动作前实际成长、装备、Reserve、修正参数的深拷贝；Format/Parse 完整往返 Reserve。
逆推只留作明确标记的历史恢复，不得默默 Level=1 后当准确记录。不要扩造逆推系统。

C4：修复后冻结被测源码与数据，跑无 filter 全套测试和 basic/fever/auto 自然操作。
把新产生的分场文件经标准回放器读回到新 sim，要求 Match=True、差异为零。
旧 FAIL 保留，用新同路径录制验证修复，不手改旧带。
交原始 TRX/退出码/日志、源码绑定、构建和连续关键流程录像；不能只上传源码后又标 Unity NOT_RUN。
真实环境阻塞如实给错误和已尝试步骤，不伪造运行结果。

环境允许的最大并行 subagent 数量；先接管旧任务避免重复派发。
共享 BattleSim/BattleReplay/HUD 唯一集成者；最终验收期间不再改被测树。
核心单测可设置夹具；自然 UI 开战后禁止直接补 HP/充能/Drive/时间/胜负、强制 Perfect。
不重做 G0/G1，不开 G3，不重启暂停的原作搜片，不扩养成/新模式，不更换动画管线。
完成这四项并给真实证据后结束本批交复审。
```
