# 当前交接：原创远征 v0.1

2026-09-21。生产源码与普通 Win64 Player 均为 `7a6ede9b643a3470a9f0924d329a5ca769b40ce2`。**38 项技术验收全部通过：B 已普通败北、关闭重开并原样重试获胜；A 已完成五场普通战斗及 20:16.266667 连续录像；C 已正常取得全族并在 N5 储存、保留和消费强奏。候选 ZIP 已在工程外真实解压启动。** 用户体验的 `ACCEPTED` 仍待用户反馈。

主仓库 `F:/天命之子`，分支 `codex/original-expedition-v01`；实际 Unity 工程 `F:/Resonance/client`。O0/O1、O2、O3、O4、O5 检查点分别为 `98e2dac0`、`cc04ca14`、`a31c47ff`、`d618adcd`、`1ae47287`。后续资源排除、节点导航及跨运行时回放修复已纳入当前生产提交；测试和文档的新提交不冒充另一版 Player。

已验证范围：

- .NET 主套件 564 通过、1 个既有普通操作占位跳过，旧 Catalog 污染例单独进程 1/1，合计 **565 通过、1 跳过**。Unity Editor 最终套件 **91/91**；另一次可见 Game View 实际渲染帧率测试 **1/1**，不是一次 92 项套件。未变化的结果复用，原始报告见 [O5 检查点](O5-CODE-CHECKPOINT.md)。
- [B 普通重试归档](evidence/o5/boss-retry-7a6ede9b/README.md)：runSeed `8509421`，B01–B04，零主动自然败北后关闭重开，以 26 次成功主动获胜。两份真实 Player tape 严格 MATCH；冻结输入仅 AttemptId 不同，关闭前后检查点档案字节相同。两段有效 WGC 录像明确分段，不冒称跨进程单段录像。
- [实际帧率对照](evidence/o5/rendered-fps-20260921-37245ba5/RUN-SUMMARY.md)：两档中位约 29.99/116.82 FPS，808 个共同逻辑 tick、230 个读条 tick 的权威状态和 HUD 文字一致；129 条伤害事件及全部结算/终态一致。这是隔离冻结 N7 开局的受控渲染测试。
- A 普通 runSeed `13486312` 已五战胜利，五份实际 tape 的 EndTick 为 `685/1320/1452/1809/3331`，独立 CLI 均 MATCH。顺序为 A01→R1 A02→R2 A04→工坊休整→R4 A03。**N5 持有四件且 A02 增盾生效，未触发 A03/A04；N7 才有 A04 的 3 个根动作、A03 的 2 个根动作/3 次命中。** 见 [战斗与队列分析](evidence/o5/ordinary-full-a-7a6ede9b/reports/battle-and-queue-analysis.json)。
- A 胜利后 ActiveRun 清空；新 C runSeed `14556671` 只有 C01、全队开局生命恢复、无旧检查点，A 摘要哈希及永久解锁保留。三份只读 profile 结构和完整性校验通过，见 [两趟状态对照](evidence/o5/ordinary-full-a-7a6ede9b/reports/profile-transition-analysis.json)。
- [C 普通获取与成型](evidence/o5/ordinary-c-7a6ede9b/README.md)：C01→R1 C02→R2 C03→R4 C04；四份真实 tape 均 MATCH，EndTick 为 `990/1407/1716/1584`。N4 验证升级接力/和声，N5 tick436 三辅助储存强奏，tick832 后续点杀消费；该次原生总伤 2574，与敌 HP 和累计伤害差值一致。545.466667 秒视频完整解码、17 帧独立核验。C 选核心和 N1 开场在 A 片末段，本片从 N1 暂停衔接，只到 N6。

内容为 `original-expedition-content-v0.1.1`，SHA-256 为 `69aaa51d7fa5077a5e1d30b4c5f2fd042b62f932f86d0dec59a037762880fdbd`；回放 Schema 2 / `original-expedition-replay-v2`。Unity 生成三族首领 tape 与 .NET 自生成三份各 3/3 MATCH，属于工厂开局的受控逻辑夹具。v1 明确拒绝，原件不改写。

最终评审包为 `dist/original-expedition-v01/review-7a6ede9b-20260921-r1.zip`，SHA-256 `6c5022a559b875d670efd9b3689d24a3bd94bd97e0adc8fc32f2c4601f3eedf5`。真实 BuildAudit PASS，499 个源输入、170 个 Player 文件及最终175个ZIP条目核验通过；比旧候选多一份原字节验收摘要。见 [打包结果](evidence/o5/package-review-7a6ede9b-20260921-result.json) 和 [交付入口](DELIVERY-REVIEW-20260921.md)。旧候选及其174项ZIP完整保留。

旧 675.8 秒 GDI 录像的 `INVALID_STALE_CAPTURE` 结论不变，不能拼接或改名冒充新录像。旧 14 张独立截图和六份实际 tape 仍有效；新 WGC 已通过画面变化预检，B 两段视频完整解码并抽帧核验。历史许可/WMI 故障已恢复，不是当前阻塞。个人 legacy save.json 及备份在新 B 记录前后哈希不变；没有清理旧素材、旧视频或用户已有改动。

[38 项验收进度](ACCEPTANCE-PROGRESS.json) 当前 **38 PASS / 0 PARTIAL / 0 NOT_RUN**，共 17 份不同的普通 Player tape 严格 MATCH；受控跨运行时三族记录另计。A 的按键/返回、队列覆盖清除、五战结算及新趟连续证据见 [完整 A 远征](evidence/o5/ordinary-full-a-7a6ede9b/README.md)。候选 ZIP 工程外解压、无参数启动、恢复 C 的 N6 与正常交互见 [独立启动报告](evidence/o5/standalone-zip-launch-7a6ede9b/README.md)。最终 review 包已生成，170 个 Player 文件字节不变；未因文档更新重跑核心或重建 Player。技术验收为通过，用户验收仍为 `PENDING_USER_REVIEW`，`FinalAcceptancePassed=false`，未声称用户已经 `ACCEPTED`。
