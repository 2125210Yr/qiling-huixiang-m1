# 离线交付工具检查点

2026-09-21。Unity 许可阻塞期间已完成两项可独立使用的工具：磁盘回放核验与 Player 交付打包。没有修改 Unity 生产代码，没有产出新版真实 Player，也未推送或发布。

## 独立回放核验

提交 `e8f0b7f9`。用 .NET 6 编译并运行 [OriginalReplay.Verify](../../tools/OriginalReplay.Verify/README.md)，读取指定 JSON 文件，调用现有权威重放实现，输出单份 JSON。结果包含原文件 SHA-256、记录版本、内容版本、重放结果及差异；输入文件保持原字节。

退出码 0 表示 MATCH；1 表示版本拒绝或结果不匹配；2 表示参数、读取或解析错误。实现读取一次文件字节，散列与反序列化使用同一份输入。

- 自动化命令测试 8/8 通过，覆盖成功、版本拒绝、差异、损坏记录及错误入口。保留了最初测试编译入口冲突与占位实现 RED 报告；修复的是 CLI／测试装配，没有改动战斗生产规则。[GREEN 原件](evidence/o5/replay-cli-green.trx)。
- Release 构建 0 警告、0 错误。[构建日志](evidence/o5/replay-cli-release-build.log)。
- 实际 Release 进程调用 5 次：A/B/C 三份完整首领逻辑夹具均 MATCH，分别结束于 1141／1675／1899 tick；未知记录版本返回 REJECTED／exit 1，损坏 JSON 返回 ERROR／exit 2。三份原始 tape 哈希不变。[进程结果](evidence/o5/replay-cli-process-results.json)。

这些记录来自受控战斗夹具，尚无当前真实 Player 产生的完整普通流程 tape。

## Player 交付候选打包

[OriginalExpedition.Delivery](../../tools/OriginalExpedition.Delivery/README.md) 接受实际构建入口输出的 PASS BuildAudit 和独立指定的源码／内容版本，核对整个 Player 文件集、字节数、SHA-256、输入清单与必需声明，随后复制到全新目录并生成 ZIP。

产物保留完整 Player 与 notices，附玩法说明、原始构建审计、VERSION.json 和文件哈希清单。ZIP 创建后逐项读取解压流核对文件集及哈希。已有目标一律拒绝，失败保留部分产物；工具不执行删除或覆盖。

23/23 PowerShell 夹具通过，覆盖正确打包与 ZIP 独立核对、FAIL 审计、版本不符、缺失／额外／篡改文件、清单及声明缺失、路径越界、Windows 路径别名、已有目标和 junction。[最终 GREEN 原件](../../tools/OriginalExpedition.Delivery/tests/evidence/green-20260920T192553031Z.json)。首次 RED 和中间 GREEN 一并保留。所有假 EXE／DLL／ZIP 均在明确标记 FIXTURE 的临时目录，未执行、未加入版本历史。

工具明确输出交付候选状态，`FinalAcceptancePassed=false`。BuildAudit 没有签名，必须来自可信的实际构建过程；工具不替代 Unity 构建、界面检查、试玩或录像。

## 验证边界与下一步

两项实现均经过独立子代理源码复核，未发现需要修改的重要缺陷。本轮核对先前记录的 203 份生产 C# 文件哈希，全部未变，因此复用已有核心证据：551 通过、1 项既有跳过。新 CLI 的 8 项与打包的 23 项单独记账，不伪称重跑了全部核心套件。

[38 项验收进度](ACCEPTANCE-PROGRESS.json) 仍为 24 PASS／11 PARTIAL／3 NOT_RUN。最新 Unity 重试仍在许可初始化阶段失败，没有开始项目测试。待环境恢复，按 [O5 后续顺序](O5-CODE-CHECKPOINT.md) 完成真实 Unity 回归、冻结源码并构建，再用这两项工具处理实际 Player 和实际回放，最后补完整五战及首领失败重试录像。
