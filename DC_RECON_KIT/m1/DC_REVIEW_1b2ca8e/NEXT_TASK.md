# 1b2ca8e 后的有限收口任务

将本目录放到 `DC_RECON_KIT/m1/REVIEW_1b2ca8e/`。追加给当前活动 goal，不另开共享工作区协调者。

```text
继续当前 G2，只处理本轮复审的 A1–A4，不重做 G0/G1，不开 G3。
先读 REVIEW_1b2ca8e/REVIEW.md 和 EVIDENCE.json。
基线 1b2ca8e4ff56d9f0986e158fc97b50498f735cb3；当前 HEAD 更新则核对已修复项，保护所有用户未提交资产。

A1：CURRENT_STATE/REGRESSION_LOG 写 244 passed，但已提交 a53-gate.trx
实际是 total245/executed244/passed234/failed10，ResultSummary=Failed。
核对是否误上传早期报告。不要断言最终源码必然仍十项失败，也不要改旧TRX计数。
固定源码，运行无 filter 全量测试，保存退出码/原始TRX/日志/源码身份，由报告生成摘要。
可以先提交源码C，再提交仅包含证据的D；证据清单明确指向被测C。

A2：保留整技能 Unplayable 门禁。C001_slide→dot_flame 仍不可执行；
旧单测有 A53_SUB_STRIP_DOT_FLAME，但 VerificationCatalog/StartBattleAt/basic p0
尚未统一到同一合法配置。把已有命名替代或合法角色技能接到明确的工程验证入口，
在首个动作前完成，不偷偷修改原始库存或生产默认内容，不在战斗中加血加条。
先校验有效阵容和敌方依赖，再实际执行 basic/fever/auto。
不得改成跳过 Slide/Fever 或关门禁来绿。

A3：DataIdentity 对模拟实际持有的 StageDef 做规范化，不能只用同名ID查全局Catalog。
验证同ID、不同实际TimeLimitSec/EnemyHpMul，即使没有重新安装Catalog也身份不同。
只做最小修复，不另造回放框架。

A4：执行 Unity 三组自然流程、NEXT/HOME分场保存、磁盘读回、已有实际回放器核对，
交匹配源码的构建和连续录像。缺环境时列真实错误并标BLOCKED，不把NOT_RUN当通过。

用当前环境允许的最大并行subagent数量，先接管旧任务，禁止重复派发。
共享代码/活动Unity资源唯一写入者；共享全局状态的测试串行运行。
精确GL数值/像素时序及既有资料搜索延期不变；不增加大型后台或新玩法。
下一次报告只列A1–A4关闭状态与真实产物，不扩写整套合同。
```
