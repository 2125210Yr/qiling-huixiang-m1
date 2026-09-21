# 当前交接：原创远征 v0.1

2026-09-21。主仓库 `F:/天命之子`，分支 `codex/original-expedition-v01`；实际 Unity 工程 `F:/Resonance/client`。**当前源码 `7a6ede9b` 已修复跨运行时回放差异，Unity 91/91 通过，新 Win64 候选目录与 ZIP 已生成；普通界面验收、完整五战及首领失败重试录像仍未完成。** 继续本模式，不回到旧 SHOWTIME 单项工单。

已保存 O0/O1 入口闭环（`98e2dac0`）、O2 十二遗物及结算（`cc04ca14`）、O3 首领与编排（`a31c47ff`）、O4 真实贡献反馈（`d618adcd`）、O5 回放及打包准备（`1ae47287`）。未推送本模式、未发布。

交付工具、构建资源排除与节点导航修复分别保存在 `e8f0b7f9`／`8075dc35`、`e04e49e9`、`567eec44`；工具原始验证范围见 [离线工具检查点](OFFLINE-TOOLS-CHECKPOINT.md)。最新 `7a6ede9b` 用 IEEE 位模式指纹取代跨运行时不一致的浮点格式化，并使原创自然充能显式以 double 计算、每 tick 写回 float；legacy 充能逻辑不变。

- 当前 .NET 主套件 564 通过、1 个既有普通操作占位跳过；已知旧 Catalog 污染用例复用单独 1/1 的有效结果，合计 **565 通过、1 跳过**。真实 Unity 最终 **91/91、0 跳过**。原件见 `evidence/o5/replay-portability-core-final-20260921.trx`、`fingerprint-catalog-isolated-20260921.trx` 与 `replay-portability-unity-final-20260921.xml`。
- Unity 生成的 A/B/C 首领磁盘回放由新 .NET CLI 严格核验 **3/3 MATCH**；.NET 自生成三份也 **3/3 MATCH**。它们是工厂开局、合法 Submit/Tick 的受控逻辑夹具，不是普通 UI 录像。根因与完整结果见 `evidence/o5/replay-portability-fix.md`、`replay-v2-portability-results-final.json`。
- 当前内容为 `original-expedition-content-v0.1.1`，内容 SHA-256 为 `69aaa51d7fa5077a5e1d30b4c5f2fd042b62f932f86d0dec59a037762880fdbd`；回放为 Schema 2 / `original-expedition-replay-v2`。新验证器明确拒绝 v1；旧 tape、失败报告和旧包保留，不改写原件。
- `7a6ede9b` 独立物理副本于 12:56 完成真实构建，退出码 0、BuildAudit PASS；499 个源输入复核匹配，170 个 Player 文件与 174 个 ZIP 条目已核验。候选目录为 `dist/original-expedition-v01/candidate-7a6ede9b-20260921`，同名 ZIP 的 SHA-256 为 `5b927cd0819540f02a4b73877bff25375d9a09abac8e332a6025da525cd84e2d`。见 `evidence/o5/package-7a6ede9b-result.json`；`FinalAcceptancePassed=false`。
- `e04e49e9` 无参数普通试玩仅完成 N1 胜利、领取 B02 并到 N2；因继续按钮埋在长页面底部，经普通界面提前结束。录像保留在 `dist/original-expedition-v01/play-e04e49e9-20260921/partial-navigation-issue-e04e49e9.mp4`，不是完整远征或首领重试证据。
- 当前仍等待用户回复是否恢复窗口操作；此前 Computer Use 启动时报告物理 Esc 停止，此后未再调用。新包普通 UI 与真实 Player v2 tape 尚未验收。
- 已结束 v0.1.0 原创 profile 的真实隔离副本可新开 v0.1.1 远征，永久记录保留、源字节未改，见 `evidence/o5/profile-v011-copy-compatibility.json`。旧活动远征仍受版本检查保护。`evidence/o5/portability-save-integrity.json` 核实个人 save.json 与备份仍与开工前一致，实际原创 profile 自 Esc 后字节未变；旧素材、旧视频和用户已有暂存内容未清理。

用户重启后 WMI 查询与 Unity 执行均已恢复，见 `evidence/o5/reboot-recovery-20260921.json`。此前许可握手失败仅作为历史记录保留在 `evidence/o5/unity-retry-20260921-0305-result.json`，不再是当前阻塞。

后续顺序与证据位置见 [O5 代码检查点](O5-CODE-CHECKPOINT.md)。[验收进度](ACCEPTANCE-PROGRESS.json) 为 **26 PASS / 10 PARTIAL / 2 NOT_RUN**，按原 38 项分别记录；O-038 已有真实构建与包证据，仍缺普通完整流程，保持 PARTIAL。原始 spec 清单不变，[玩法说明](PLAY-GUIDE.md) 已随候选包交付。

下一步在窗口操作恢复后，用新 `7a6ede9b` 包核验普通 UI、完成 N0→N7 五战连续流程及同版首领失败→原样重试→胜利录像，并用新 CLI 严格核验真实 Player v2 tapes。历史准备副本、旧包及部分录像不冒充新版证据；没有新变更则复用当前有效验证。未推送 GitHub、未发布。
