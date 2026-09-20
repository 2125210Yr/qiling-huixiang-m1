# O1 远征、奖励与独立存档验证

执行日期：2026-09-21（本机 Asia/Shanghai）。本目录的测试为本次实际运行，未经修改的六个远征/存档源码与测试文件在运行前后 SHA-256 一致，见 `source-before-tests.json`、`source-after-tests.json`。

## 实际结果

| 运行 | 结果 | 原始记录 | 证明范围 |
|---|---:|---|---|
| 远征状态机、奖励与存档 | 28 通过，0 失败，0 跳过 | `flow-store-1.trx` / `.log` | 五战节点、HP 过场规则、分岔互斥、确定性奖励、幂等、首领输入重试、解锁与新一趟清理、原子写入和损坏恢复 |
| 实际模拟闭环（主集成者提供的用例） | 2 通过，0 失败，0 跳过 | `actual-loop-1.trx` / `.log` | 两条路线分别实际通过 BattleSim 五场战斗、奖励与首次解锁、重开后开始新一趟 |

这些用例不是普通玩家录像。28 项状态机/存档测试明确用终局 HP 和结局作为夹具输入。另 2 项调用真实 BattleSim，策略经统一 Submit 点敌、发动就绪主动技能并 Tick，没有改 HP、充能或胜负；仍属于命令策略集成夹具，不能冒充真实 UI 操作。UI、独立构建、录像和后续遗物运行时/首领调度由集成验收另行证明。

测试与实现一同建立，本目录只有实际通过结果；没有宣称不存在的行为 RED。这里不复用历史 310 或 27 项测试充当新模式验证。

## 可复现奖励路径

以下种子由测试搜索后，再走 ExpeditionFlow 的合法节点与候选选择验证。统一使用初始 `single` 配置、`N2-backstage` 路线、工坊休整；R4 后即在 N5 输入中持有完整四件套。

| 家族 | runSeed | N0 | R1 | R2 | R4 |
|---|---:|---|---|---|---|
| 防护 | 0 | A01 | A02 | A03 | A04 |
| 散射 | 3 | B01 | B02 | B03 | B04 |
| 接力 | 0 | C01 | C02 | C03 | C04 |

这只证明十二件定义通过当前奖励/节点 API 可获取，尚不证明十二件的 UI 点击与战斗触发均已验收。

## 事务与恢复边界

- 单一 `OriginalExpedition/profile.v1.json` 文档同时存储解锁、run、候选、检查点和最近结算。代码拒绝将旧 `save.json` 作为目标。
- 每次操作先复制状态，写入同目录临时文件并 `Flush(true)`，再 `File.Replace`（首文件 `File.Move`）。写入完成才发布新内存状态。
- 文件带完整内容 SHA-256；正文无效时读取可验证备份。两者无效则报错，不清档、不重置、不覆盖。
- 写入前、临时文件刷盘后、替换前、替换后四个故障点实际执行。结果只有完整旧状态或完整新状态。替换后模拟崩溃时调用方仍未显示成功，重开读取一次性已提交结果，旧 revision 不可再次领取。
- 故障后恢复备份再保存，不把损坏正文覆盖成唯一有效备份。
- 奖励候选在胜利/工坊事务内生成并持久化，UI 读取保存的顺序；选择同时检查 offerId 与 revision。弃选恢复存活角色 5% MaxHP，消费整组候选。该 5% 是规格“小次恢复”的本版明确候选值。
- 战前输入深拷贝，内容指纹、基础配置、开局 HP、完整技能/效果/首领配置和种子共同持久化。普通退出返回同一场起点；首领重试只改 attemptId，保留全部其他序列化输入。
- 新测试只写 GUID 临时测试目录，没有清理用户档案或批量删除文件。

## 运行命令

```powershell
dotnet test F:\天命之子\tools\BattleSim.Tests\BattleSim.Tests.csproj --no-restore --filter 'FullyQualifiedName~OriginalExpeditionFlowTests|FullyQualifiedName~OriginalProfileStoreTests' --logger 'trx;LogFileName=flow-store-1.trx' --results-directory F:\天命之子\docs\original-expedition\evidence\flow
dotnet test F:\天命之子\tools\BattleSim.Tests\BattleSim.Tests.csproj --no-restore --filter 'FullyQualifiedName~OriginalExpeditionLoopIntegrationTests' --logger 'trx;LogFileName=actual-loop-1.trx' --results-directory F:\天命之子\docs\original-expedition\evidence\flow
```

运行退出码均为 0。六个自有文件在前后核验中全部一致；该局部核验不声称其他并行开发文件被全局冻结。
