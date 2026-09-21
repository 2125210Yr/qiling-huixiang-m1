# O5 回放与交付检查点

2026-09-21。**当前源码 `7a6ede9b643a3470a9f0924d329a5ca769b40ce2`：Unity 91/91、.NET合计565通过/1既有跳过，新包及审计完成；普通四场胜利与两次首领败北产生的六份真实tape全部严格MATCH。** 本轮GDI录屏冻结在入口，不能作为流程证据；完整五战通关、关闭重开后首领成功重试及新趟仍未完成。

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
| `evidence/o5/rendered-fps-offline-compile.json` | `fbf7ac10` 新增受控实际渲染帧率夹具仅离线编译退出码0；Unity运行NOT_RUN，未设专用环境变量时Ignore。未计入91项，O-023仍PARTIAL；未改生产源码或当前Player包。 |
| `evidence/o5/replay-v2-portability-results-final.json` | Unity 生成 A/B/C 三份首领磁盘 tape，由新 .NET CLI 严格核验 3/3 MATCH；.NET 自生成三份也 3/3 MATCH。六份文件核验中三份跨运行时，含文件 SHA、版本、退出码及事件 hash。 |
| `evidence/o5/family-tapes-v2-unity-final/`、`family-tapes-v2-net6-final/` | A/B/C 分别 1141／1675／1899 tick 胜利并实际触发家族效果；固定工厂开局、合法 Submit/Tick，未改战中生命、充能或胜负。**这些是受控逻辑夹具，不是普通 UI 录像。** |
| `evidence/o5/profile-v011-copy-compatibility.json` | 真实已结束旧原创档副本新开 v0.1.1 成功，永久记录保留、源字节未改；不等同于修改用户活动存档。 |
| `evidence/o5/build-7a6ede9b-20260921-run.json`、`build-7a6ede9b-audit.json` | 当前源码独立副本于 12:56 构建结束，退出码 0、BuildAudit PASS。 |
| `evidence/o5/build-7a6ede9b-source-validation.json`、`package-7a6ede9b-result.json` | 499 个源输入复核匹配，170 个 Player 文件、174 个 ZIP 条目已核验，候选打包完成；`FinalAcceptancePassed=false`。 |
| `evidence/o5/portability-save-integrity.json` | 先前暂停桌面操作时的存档完整性检查；不代表后续已授权普通游玩没有推进原创profile。 |
| `evidence/o5/ordinary-7a6ede9b/replays/ordinary-run-replay-summary.json` | 六份真实普通Player v2 tape均严格MATCH：N1/N2/N4/N5四场Victory、N7两次Defeat；与上面的六份受控家族记录分开。 |
| `evidence/o5/ordinary-7a6ede9b/replays/n4-n5-settlement-evidence.json` | N5正常持有B01–B04，散射有效伤害11,177、两次B04回响217+135=352；B03有34个双目标根动作，未把B02/B03重复计入伤害。 |
| `evidence/o5/ordinary-7a6ede9b/ui-tape-crosscheck.json` | 三张独立Sky/WGC截图对应真实结算前缀：N4累计5990/最近95+95，N5累计13699/最近91，N2累计9880/有效治疗3522/最近179；3/3一致。截图不是失效GDI视频抽帧。 |
| `evidence/o5/ordinary-7a6ede9b/replays/verification-batch-03.json` | 两次首领冻结输入除AttemptId外精确相同并匹配战前检查点；两次均Defeat，不能证明关闭重开或改变操作获胜。 |
| `evidence/o5/ordinary-7a6ede9b/recording-result.json` | 初次容器检查原件：MP4正常结束且可解码，个人save.json及备份匹配开工前哈希；不能据此判录像有效。 |
| `evidence/o5/ordinary-7a6ede9b/video-visual-validation.json` | 最终视觉判定INVALID_STALE_CAPTURE：0.5／341／668秒抽帧仍为入口，标题区16.67秒后持续冻结；原MP4与诊断保留。 |

`7a6ede9b` 交付候选已位于 `dist/original-expedition-v01/candidate-7a6ede9b-20260921` 及同名ZIP，ZIP SHA-256为 `5b927cd0819540f02a4b73877bff25375d9a09abac8e332a6025da525cd84e2d`。原38项当前 **28 PASS / 10 PARTIAL / 0 NOT_RUN**，整体未完成。O-017精确用例加截图、O-020模拟边界加静态预警画面已满足；O-024/O-036仍缺有效普通片段，O-003/O-027/O-038仍缺完整通关或成功重试录像。

原 551 通过／1 跳过的核心联合报告、75／82／84 项 Unity 报告、v1 tapes、诊断中途 B/C 不匹配和旧包都保留为历史证据，不能代替上述最终结果。旧普通 N1 tape 的 .NET `FinalState mismatch` 是本次修复的触发证据；它没有被改写，新验证器对其明确 REJECTED。原离线工具范围见 [离线工具检查点](OFFLINE-TOOLS-CHECKPOINT.md)，旧准备副本及 meta 来源检查见 `evidence/o5/package-1ae47287-validation.json`。

## 普通试玩边界与下一步

本轮普通run为 `158dfbcbfa944ee5ad276bb015e20236`，界面种子8509421；经普通入口选B01，N1后取B02、N2观众席后取B03、工坊休整、N4后取B04、N5四件套胜利，再于首领门前恢复全队进入N7。有效来源为14张独立Sky/WGC截图和六份原始tape，不是调试注入；输入日志label仅表示操作意图，例如最后一次拟点N4攻击实际上已进入奖励页并展开B04详情，没有算成成功攻击。见 [普通试玩证据说明](evidence/o5/ordinary-7a6ede9b/README.md)。

原片 `dist/original-expedition-v01/play-7a6ede9b-20260921/ordinary-expedition-7a6ede9b.mp4` 长675.8秒、SHA-256为 `61d59cad400a601ea0b242c8e576fb9ae92fa6ca595e3f7a6466be19904ff114`。GDI只捕获了入口旧画面，虽正常结束并可解码，仍判 **INVALID_STALE_CAPTURE**；不能证明四战、败北或完整流程。15 FPS仅是录屏参数，也不证明游戏实际帧率。原片保留为失败采集证据，不能改名或拼接冒充通关；旧e04的GDI录像未重新视觉抽检，仅保留原件，不用于新版通过结论。

用户曾明确恢复操作；本轮N7第二次尝试选面具后，暂停操作再次被Computer Use以物理Esc停止，此后没有再输入桌面，第二次尝试自然败北。ffmpeg已由stdin `q` 正常收尾，Player仍运行；最后只读观察为BossRetry、revision31、已结算四战。首领未胜利、未关闭重开，未结算新趟。WMI/许可问题已经恢复，与当前采集故障分开记录。

恢复桌面操作后先以WGC/gfxcapture录10秒普通页面变化，对照前后抽帧与新截图验证更新；尚未执行该新采集。预检通过再另存首领失败→关闭重开→同检查点成功重试，以及同版完整N0→N7胜利→结算→返回→新趟的连续录像。O-024暂停编排输入片段、O-036成型后N5/N7发挥的流程时间点也须有效画面；O-015尚缺A/C八件相应UI范围，O-023尚缺不同实际渲染帧率对照，普通热键/返回栈与关闭重建仍待。新产生tapes再从磁盘严格核验，已有六份真实MATCH与未变化的测试结果复用；继续保护个人存档，禁止战中注入生命、充能或胜负。

源码与输入未改变时复用当前有效验证。只有新缺陷修复才重新执行受影响检查，并在全新物理目录构建、打包；构建继续显式传入 `-giCustomCacheLocation "<副本>/Temp/GICache-Original"`，保留默认缓存失败原件，不改系统权限。未推送 GitHub、未发布、未删除或覆盖旧素材、ZIP、视频；用户已有暂存改动保留。

O-023已有 [受控实际渲染帧率测试说明](RENDERED-FRAME-RATE-TEST.md)：须恢复桌面控制且当前Player录制结束后，以可见GameView和专用输出环境变量运行；不能用batchmode/nographics或离线编译结果代替实测。当前尚未运行，也不替代普通Player流程录像。
