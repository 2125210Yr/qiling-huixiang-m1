# O5 回放与交付检查点

2026-09-21。**当前源码 `7a6ede9b643a3470a9f0924d329a5ca769b40ce2`：Unity 91/91 通过，.NET 合计 565 通过、1 既有跳过，真实 Win64 构建、审计与候选 ZIP 均完成。** 新包普通 UI、完整五战和首领失败重试录像仍未验收，整体目标未完成。

## 当前实现与版本

- 回放保存完整冻结输入及 SHA-256、操作意图、实际命令与拒绝原因、独立 EndTick、结算明细、事件及最终状态；暂停队列通过意图重建，预期输出不参与构造开局或结束 tick。普通终局核验后以新文件保存至原创 profile 同级 `Replays`，失败日志不阻断原进度提交。
- `7a6ede9b` 修复两个跨运行时根因：浮点 `ToString("R")` 改为 IEEE 位模式指纹，保留正负零且不引入容差；原创自然充能从除法开始显式使用 double 中间运算，每 tick 写回 float 一次。legacy 充能分支不变。诊断原件及详细说明见 [回放一致性修复](evidence/o5/replay-portability-fix.md)。
- 内容版本为 `original-expedition-content-v0.1.1`，内容 SHA-256 为 `69aaa51d7fa5077a5e1d30b4c5f2fd042b62f932f86d0dec59a037762880fdbd`；回放为 Schema 2 / `original-expedition-replay-v2`。v1 在模拟前明确拒绝；旧 Player tape、失败报告、旧验证器和候选包全部保留，不改写历史记录来取得匹配。
- 原创 profile 的存档 Schema 与完整性算法未改。真实已结束 v0.1.0 档案的隔离副本可加载并新开 v0.1.1，永久记录保留、源字节未变；旧活动远征仍由版本检查拒绝，应在对应旧包继续或经普通界面结束后新开。个人 save.json 与备份保持保护。
- 构建仍使用物理副本、输入白名单、场景依赖／Resources／packedAssets 审计、许可文本及逐文件 SHA。`e04e49e9` 保留移出构建时注入的精确性能测试资源，`567eec44` 将当前节点选项前移并增加滚动条；两项修复均在当前版本内，原说明分别见 `evidence/o5/build-artifact-fix.md`、`navigation-fix.md`。

## 最终实际证据

| 证据 | 结果与范围 |
| --- | --- |
| `evidence/o5/replay-portability-core-final-20260921.trx` | 当前 .NET 主套件 564 通过、0 失败、1 个既有普通操作占位跳过。 |
| `evidence/o5/fingerprint-catalog-isolated-20260921.trx` | 已知旧 Catalog 污染例单独 1/1；该路径不受最后原创充能分支改动影响，复用有效结果。与主套件合计 565 通过、1 跳过，不声称单次全套结果。 |
| `evidence/o5/replay-portability-unity-final-20260921.xml` | Unity 6000.3.23f1 真实执行 91/91、0 跳过；涵盖既有 UI／教程、构建护栏、导航、固定数值指纹与自然充能位值回归。 |
| `evidence/o5/replay-v2-portability-results-final.json` | Unity 生成 A/B/C 三份首领磁盘 tape，由新 .NET CLI 严格核验 3/3 MATCH；.NET 自生成三份也 3/3 MATCH。六份文件核验中三份跨运行时，含文件 SHA、版本、退出码及事件 hash。 |
| `evidence/o5/family-tapes-v2-unity-final/`、`family-tapes-v2-net6-final/` | A/B/C 分别 1141／1675／1899 tick 胜利并实际触发家族效果；固定工厂开局、合法 Submit/Tick，未改战中生命、充能或胜负。**这些是受控逻辑夹具，不是普通 UI 录像。** |
| `evidence/o5/profile-v011-copy-compatibility.json` | 真实已结束旧原创档副本新开 v0.1.1 成功，永久记录保留、源字节未改；不等同于修改用户活动存档。 |
| `evidence/o5/build-7a6ede9b-20260921-run.json`、`build-7a6ede9b-audit.json` | 当前源码独立副本于 12:56 构建结束，退出码 0、BuildAudit PASS。 |
| `evidence/o5/build-7a6ede9b-source-validation.json`、`package-7a6ede9b-result.json` | 499 个源输入复核匹配，170 个 Player 文件、174 个 ZIP 条目已核验，候选打包完成；`FinalAcceptancePassed=false`。 |
| `evidence/o5/portability-save-integrity.json` | 个人 save.json 与备份仍与开工前字节／SHA 一致，实际原创 profile 自 Esc 后字节未变，此后未发送桌面输入。 |

`7a6ede9b` 交付候选已位于 `dist/original-expedition-v01/candidate-7a6ede9b-20260921` 及同名 ZIP，ZIP SHA-256 为 `5b927cd0819540f02a4b73877bff25375d9a09abac8e332a6025da525cd84e2d`。原 38 项验收当前为 **26 PASS / 10 PARTIAL / 2 NOT_RUN**；O-038 因缺普通完整流程仍为 PARTIAL，包生成不等于最终验收。

原 551 通过／1 跳过的核心联合报告、75／82／84 项 Unity 报告、v1 tapes、诊断中途 B/C 不匹配和旧包都保留为历史证据，不能代替上述最终结果。旧普通 N1 tape 的 .NET `FinalState mismatch` 是本次修复的触发证据；它没有被改写，新验证器对其明确 REJECTED。原离线工具范围见 [离线工具检查点](OFFLINE-TOOLS-CHECKPOINT.md)，旧准备副本及 meta 来源检查见 `evidence/o5/package-1ae47287-validation.json`。

## 普通试玩边界与下一步

已有普通无参数试玩仅来自 `e04e49e9`：N1 胜利、领取 B02 并到 N2，因导航按钮埋底经普通界面提前结束。部分录像为仓库根下 `dist/original-expedition-v01/play-e04e49e9-20260921/partial-navigation-issue-e04e49e9.mp4`，不是完整远征或首领失败重试证据。

用户重启后 WMI 与 Unity 已恢复，见 `evidence/o5/reboot-recovery-20260921.json`；此前许可失败只保留历史原件。目前仍等待用户回复是否恢复窗口操作：Computer Use 此前报告物理 Esc 停止，此后没有再调用。新包尚未进行普通 UI 验收。

恢复窗口操作后，用新 `7a6ede9b` 包无参数启动，核验导航并完成 N0→N7→结算→返回→新趟的连续五战，另录同版首领败北→原样重试→胜利。真实 Player 产生的 v2 tapes 必须由新独立 CLI 从磁盘严格核验；受控逻辑夹具不能代替。记录源码、EXE、内容版本、录像及回放哈希，按原 38 项更新验收。普通试玩前后继续核对个人存档与备份，禁止战中设置生命、充能或胜负。

源码与输入未改变时复用当前有效验证。只有新缺陷修复才重新执行受影响检查，并在全新物理目录构建、打包；构建继续显式传入 `-giCustomCacheLocation "<副本>/Temp/GICache-Original"`，保留默认缓存失败原件，不改系统权限。未推送 GitHub、未发布、未删除或覆盖旧素材、ZIP、视频；用户已有暂存改动保留。
