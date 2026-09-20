# O5 回放与交付准备代码检查点

原创回放和隔离打包入口已实现，核心与 C# 编译验证通过。**最终独立 Win64 包、14 项新增 Unity UI 用例及完整普通录像仍未完成；项目目标保持进行中。**

## 已完成

- 独立 `original-expedition-replay-v1` 格式：保存完整冻结输入及其 SHA-256、外部操作意图、全部实际命令及拒绝原因、独立 EndTick、结算明细、事件与最终新状态。历史 legacy tape 的过滤、序列化和重放规则保持原入口。
- 暂停编排的添加／替换／清除也进入意图记录；Resume 重建队列并自然产生子命令，避免重放两遍。终局同 tick 的拒绝输入仍处理。任何预期输出都不决定初始状态或结束 tick。
- 最终比较包括首领阶段／时钟／意图、面具代数、单位状态精确散列、A 能量、C 和声／强奏、遗物触发与有限请求状态、待执行队列和执行结果。ResolutionResult 使用 DataMember 属性完整序列化，避免 public-field 散列漏掉伤害数字。
- 普通原创战斗结算入口会录制、序列化再读取、重放核验，成功后以新文件保存到原创 profile 同级 `Replays`。保存失败仅记明确日志，不改变战斗结果或阻断原有进度提交；此入口已编译，尚未在新 Player 中实际执行。
- 白名单准备脚本完成一次预备副本，499 个输入文件逐字节校验、无软链接，Resources 数据仅有已核验的 Noto 字体；原参考图片、模型和原生插件未复制。该副本是中途源码快照，之后构建审计入口又补了随包 notices 和最终文件清单，**不能拿旧预备副本直接作为最后版本**。
- 随后以 `1ae47287` 为基线创建新候选副本 `dist/original-expedition-v01/prepare-1ae47287`，499 文件、11,448,923 字节，包含最终一次构建入口修改。输入清单 SHA-256 为 `09a2d0844a2b39a36dd445bf0b1c60c9579ca846fdf78d0a3bf1457d6f72425f`；它仍是预备源码快照，不是 Player 或已通过 UI 验收的最终包。
- 新副本独立核对 499/499 文件的源／目标 SHA 和大小全部匹配；247 个 meta 的 GUID 无重复或无效。Git 来源检查发现 487 项在基线已跟踪，另 12 个现有 Inochi 托管运行时 `.meta` 被第三方目录 `*.meta` 规则忽略；已精准补跟踪这 12 个原文件，保留现有 GUID 与字节，以便从版本历史重建相同输入。见 `evidence/o5/package-1ae47287-validation.json`。
- 新 `OriginalExpeditionBuild.BuildAndExit` 只接受已声明的物理副本和输入清单；验证文件 SHA、场景依赖、所有 Resources 和实际 packedAssets。成功构建后复制许可文本，并对整个 Player 输出逐文件 SHA。该入口已用本机 Unity API 编译，尚未真实构建。

## 实际验证

| 证据 | 结果与范围 |
| --- | --- |
| `evidence/o5/core-o5-isolated-suite.trx` | 538 通过、0 失败、1 项既有跳过；完整核心套件仅排除已知 Catalog 污染测试。 |
| `evidence/o5/core-o5-catalog-isolated.trx` | 上述旧测试单独运行 1/1；这两次合计列入 540 项，539 通过、1 既有跳过。隔离依据见 O2，不隐瞒单进程顺序问题。 |
| `evidence/o5/replay-boundaries.trx` | 45/45；含完整自然首领胜局重放、拒绝命令、编排、变速、版本拒绝、明细篡改与不可达边界。 |
| 同上全量核心内 `OriginalReplayStoreTests` | 4/4；真实保存再读、重复不覆盖、损坏记录拒绝、IO 失败不改战斗及原 profile 字节。 |
| `evidence/o5/replay-families.trx` | 另补规格要求的 5/5：完整 A/B/C 的实际首领胜局触发与回放，以及开局生命／遗物参数篡改拒绝。生产代码未改变，复用上述有效全量结果。 |
| `evidence/o5/relic-acceptance-boundaries.trx` | 4/4；无结果／零有效结果不蓄能，完整 A 族单敌只释放一次 A04，以及预算 16→17 经生产 Submit 明确 Failed。后者注入预算前置条件；不是正常内容自然耗尽预算的证明。当前无通用未命中机制，前两例验证结算合同。 |
| `evidence/o5/lifecycle-boundaries-green.trx` | 3/3；真实 Flow 的 N1→N2、成熟 N5→N7、EndRun→新趟，核对生命恢复及临时状态重建。前置战斗状态与胜利明确为夹具。最初 `lifecycle-boundaries.trx` 三项因测试误期待充能 0 失败；核对规格及既有开局值 35 后只修测试，保留原始报告。 |
| `evidence/o5/fixture-boss-replay.json` | 新录制并重放匹配的首领逻辑夹具；开局来自测试工厂，非普通 UI 录像。 |
| `evidence/o5/family-tapes/` | 三份真实录制后 JSON 再读取并匹配的家族首领逻辑夹具。A04 实际生命伤害 16219；B01 实际生命伤害 23259；C 形成和声并消费强奏各 14 次。仍为工厂开局／合法指令测试，非普通获取或 UI 录像。 |
| `evidence/o5/compile-03/compile-results.json` | 使用本机 Unity 6000.3.23f1 项目引用编译 6 个程序集全部 exit 0；含 App、核心、Editor 和 UI 测试源码。没有执行 Unity、资产导入或 UI 断言。 |
| `evidence/o5/package-preparation-*.json` | 复制护栏 12/12、一次真实复制与独立哈希／资源扫描通过；不是 Player 打包结果。 |
| `evidence/o5/personal-save-hashes.json` | 个人 save.json 与备份仍均为 `624CC9C6882BB512A7EFFE419F3B47332747BE33A33726796DF611612E55C2A7`，与 O0 前一致。 |

同一生产源码的上述五份不同用例报告合计列入 **552 项，551 通过、1 既有跳过**。这是有效结果的联合，不冒称在单次全套中得到；重复的定点回放报告不再计数。独立回放源码复核未发现确定且重要的新缺陷，范围见 `evidence/o5/replay-code-review.md`。

## 环境阻塞与恢复后步骤

Unity 两次在本机许可证握手阶段停住，未进入项目编译。系统此前发生内存不足，随后跨应用 WMI 查询异常；这是最强候选，尚没有线程栈证明完整因果。内存恢复后，2026-09-21 02:18（上海）独立只读 `Win32_OperatingSystem` 查询仍以 5 秒操作超时退出。见 `evidence/o3/unity-license-diagnosis.md` 与 `evidence/o5/wmi-health-probe*`。没有删除许可证、重新激活、重启服务或更改系统权限；只终止了本任务已核验身份的挂起 Unity/许可进程。

代码提交后 02:47 的重新检查仍超时，耗时 5,058 ms，系统持续运行约 620.8 分钟；见 `evidence/o5/post-checkpoint-wmi-20260921.json`。当时没有存活的 Unity／许可进程，未再次拉起编辑器。

需要在保存工作后正常重启 Windows。恢复后按以下顺序继续，不重复未变化的核心验证：

1. 先确认 WMI 查询与 Unity 许可握手恢复；执行真实 `Resonance.EditorTests` EditMode 套件，重点补 O3 的 8 项和 O4 的 6 项 UI 用例。如有真实失败，只修对应问题，再跑受影响检查。
2. 从最终已提交源码重新执行 `evidence/prepare-package-project.ps1` 到**全新物理目录**，传完整 SourceCommit；运行该副本的 `OriginalExpeditionBuild.BuildAndExit`，检查 BuildAudit 结果和实际输出文件。
3. 将本次 Player 完整输出及 notices、玩法说明复制到独立交付位置。正常无参数启动，录制 N0→N7→结算→返回→新趟的连续五战，并另录同版首领败北→原样重试→胜利。只能真实 UI 操作，不能设置战中 HP、充能或胜负。
4. 对应 Player 的真实回放必须生成并重放匹配，检查日志中 `ORIGINAL_REPLAY_MATCH`，不得把本页 fixture tape 换名代替。记录 EXE／源码／内容版本和录像哈希；更新独立验收结果后交用户体验验收。

没有推送 GitHub、发布、覆盖旧 ZIP/视频或清理历史素材。当前普通 UI 最远证据仍是 O0/O1 开发包的一场前厅胜利与领取奖励，不声称新首领已经在普通界面玩过。
