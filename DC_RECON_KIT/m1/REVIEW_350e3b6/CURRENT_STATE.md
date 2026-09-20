# 开发状态 — 2026-09-20

最新推进：用户要求继续开发后，已完成体验候选第 1 项「教程提示避让技能演出」。提示在 SHOWTIME / QTE / 评分 / 暂停等阶段隐藏并保留余时，空闲后恢复；修复到期提示丢失，成功释放技能后撤下过时教学。Unity UI 回归 **27/27 通过**，6 个完整 HUD 渲染夹具已核验，最新 Win64 构建成功（20:34:48+08）。详情及证据见 [tutorial-presentation/README.md](artifacts/tutorial-presentation/README.md)。

当前程序：`F:/Resonance/client/Builds/Win64/Resonance.exe`，App DLL 为 `B60C9FEBE10CD33995CC68A40A9B0128FEDFC1734A0ADBB04579BAA6A4EC8A08`。Battle DLL 与下文 G2 版本一致，83 项核心输入未变，复用此前 310/0/1 核心验证；本轮新跑的是 Unity UI 测试。本轮未录制普通试玩，也未替换旧交付包；下文 125.9 秒视频与 ZIP 仍对应 `672cdff6`。其余体验候选尚未实施，G3 未启动，未推送。

## 上一轮 G2 有限收口与独立交付（历史冻结版本）

本轮 G2 路 A 有限收口已完成，最终代码提交为 `672cdff6`。现代记录的 Fever 终局重放改用独立录制的时钟边界，预期 digest 和事件只作比较；普通旧档仍保留旧结束 tick 的兼容路径。D1 默认开局、D2 真实点敌与 NEXT 成长舍入修复保留。最新全量核心测试 **310 通过 / 0 失败 / 1 既有跳过**，225 项事前冻结输入在验证后 **0 差异**，Win64 构建成功。独立试玩程序经无参数普通入口和 OS 鼠标完成首场结算、NEXT 第二场第二波、暂停及返回首页，第二场未结算。交付入口、录像与完整版本指纹见 [DELIVERY.md](artifacts/finite-closeout/DELIVERY.md)。

| 状态 | 结论与范围 |
|---|---|
| `PATH_A_ENGINEERING` | `PASS_WITH_RECORDED_LIMITATIONS`，仅限本轮 G2 有限收口与独立试玩交付；限制见下文 |
| `M1_FIDELITY` | `DEFERRED_NOT_REMOVED`，原作像素、精确时序与数值等延期项仍在最终分母 |
| `G3` | `NOT_STARTED` |
| `REMOTE_SYNC` | `NOT_PUSHED` |

D1 / D2 基础修复为 `40ab2fb`，历史 D3 为 `60962824`，成长舍入修复为 `38629718`。其中 `60962824` 以 FinalDigest 决定终局 Fever 推进的实现已被本轮替换，旧绿色输出仅作为历史证据保留。

## 已完成的修复

- `OpeningGrowth.Recover` 优先处理明确 `source=input`，null/空数组/全 null 输入不再进入旧档案反推；`Parse` 保留 `-` 的 null 槽语义。既有序列化格式将空数组规范为 null，两者均表示默认开局，不额外扩格式。
- 新增 4 种输入形状回归与旧记录对照。既有 B1 夹具现在同时清空输入和来源字段，以真实模拟缺字段的旧档案；没有放宽身份或回放比较。
- basic 自然场景经已有 EventSystem 的 down/up/click 点击非首个存活敌人的 `focusHit`，检查新接受的 `Player FocusEnemy`、焦点变化、`commands.txt` 与解析后的 `replay.jsonl`；保留其余原有流程。
- 测试保留模式 `RESONANCE_KEEP_TEST_ARTIFACTS=1` 仅跳过递归清理，全部测试与断言仍执行。符合用户不批量删除文件的要求。
- `launch/在电脑上看.cmd` 现在先找 `client/Builds/Win64/Resonance.exe`，再回退 `dist/windows`，两者都不存在时明确退出。之前启动脚本优先命中 08-30 旧 DLL 的问题已改。
- `BattleReplayer` 对现代记录按独立录制的战斗结束 tick、终局 Fever 时钟次数和终局命令偏移重建收尾，保留调速与时钟推进的实际交错。现代记录的 FinalDigest 和预期事件不再决定模拟推进；缺少独立终局时钟的旧尾段明确报错，不回填原件或放宽比较。普通旧档保留以旧 digest.TickIndex 定位结束的兼容路径。
- `Growth` 明确倍率组成与属性乘积的精度边界，`Bond` 为成长计算提供共享原系数的内部精度路径；Unity Mono 与 .NET 现在得到相同成长属性，保持旧 Unity 数值与到偶数舍入。新增 6 项固定数值回归，录制脚本同时记录 Growth / Bond 源文件哈希。

## 真实执行结果

| 检查 | 结果 | 原件 |
|---|---|---|
| D1 红灯 | 4 失败 / 2 通过，失败对应新输入来源及 null 槽缺陷 | `artifacts/D1/D1-red.trx`、`.log` |
| D1 修复后 | 6 通过 / 0 失败 | `artifacts/D1/D1-green.trx`、`.log` |
| D1 后无 filter 全量核心测试 | 276 通过 / 0 失败 / 1 跳过 / 总数 277，退出 0 | `artifacts/tests/D-full.trx`、`D-full.console.txt`、`D-full-run.json` |
| 历史 FinalDigest 驱动的终局 Fever 回归 | 当时先 1 失败 / 1 通过，后相关回归 14/14；实现已被本轮替换 | `artifacts/tests/fever-terminal-{red,green}.{log,trx}` |
| 历史终局 Fever 后无 filter 全量核心测试 | 278 通过 / 0 失败 / 1 跳过 / 总数 279，退出 0 | `artifacts/tests/D-final.trx`、`D-final.console.txt`、`D-final-run.json` |
| 历史成长修复后无 filter 全量核心测试 | 284 通过 / 0 失败 / 1 既有跳过 / 总数 285，退出 0 | `artifacts/growth-rounding/rounding-full.trx`、`rounding-full.log`、`full-run.json` |
| 成长修复时同一 DLL 跨运行时属性矩阵 | **5,625 组 / 0 差异；相对旧 Unity 也 0 差异**，作为未改成长计算的历史证据复用 | `artifacts/growth-rounding/after-summary.json` 与前后运行输出 |
| 成长修复后的旧档案精确读回 | **7/7 Match=True**，包括两份此前失败的 NEXT 场；所有差异 0，原件未改 | `artifacts/growth-rounding/readback/summary.json` |
| 成长修复时 Unity 续战及两场读回 | **场景 PASS；2/2 Match=True**；首场 132/132，第二场 8/8 仅为开局与暂停短片段，截图 HP 3283 | `artifacts/natural-play/20260920T080750-np_basic_v1/`、`artifacts/growth-rounding/readback/fresh-next-summary.json` |
| 独立代码审查 | 未发现阻塞问题；确认中途 Persist 后结果页仍会更新最终 tape | 审查范围为 6 个生产/测试/启动文件；没有用审查代替运行 |
| 新 Unity basic | **PASS**，Editor 退出 0；Victory→NEXT→Pause→Home | `artifacts/natural-play/20260920T050523-np_basic_focus/` |
| 新首场回放 | **Match=True**，Command/Unconsumed/Digest/Event/Version 差异全 0 | `artifacts/readback/np-20260920T050557-001.txt` |
| 旧可播录像所对应的第二批首场 tapes | basic / fever / auto 均 **Match=True**，差异全 0 | `artifacts/readback/second-{basic,fever,auto}.txt` |
| 历史可见补录 basic / Fever / Auto 首场 | 当时分别 155/155、879/879、69/69；879 事件旧终局 Fever 的绿色结果已被当前严格负向对照取代 | `artifacts/readback/visible-{basic,fever,auto}-final.txt`、`visible-final-summary.json` |
| 最终 Fever 重录首场 | **1141/1141 Match=True**；Fever 在 Victory 前结束，本轮严格读回仍通过 | 历史 `artifacts/readback/visible-fever-new.txt`；当前 `artifacts/finite-closeout/legacy-readback/d3-fever-001.stdout.txt` |
| 历史成长修复 Win64 构建 | 成功，退出 0；已被本轮构建替代 | `artifacts/growth-rounding/build-run.json`、`win64-build.log` |
| 最新无 filter 全量核心测试 | **310 通过 / 0 失败 / 1 既有跳过 / 总数 311**，退出 0 | `artifacts/finite-closeout/full.trx`、`full-test.log`、`full-test-run.json` |
| 最终源码与构建绑定 | **225 项事前冻结输入 / 0 差异**，代码 `672cdff6`；Win64 构建退出 0 | `artifacts/finite-closeout/frozen-source-hashes.json`、`validated-source-and-build.json`、`build-run.json` |
| 最终 reader 读回原有 9 份档案 | **9/9 Match=True、Ok=True、全部差异 0**；63 个原件文件指纹不变 | `artifacts/finite-closeout/legacy-readback/summary.json`、`README.md` |
| 879 事件旧终局 Fever 负向对照 | **按预期拒绝**：缺独立终局时钟，Match=False / exit 1；不属于上行 9 份有效档案 | `artifacts/finite-closeout/legacy-readback/terminal-legacy-summary.json` |
| 独立程序普通操作与录像 | 无参数、OS 鼠标；首场 Victory→NEXT→第二场第二波→Pause→Home；第二场未结算；录像 **125.9 秒** | `artifacts/finite-closeout/standalone-process.json`、`standalone-actions.json`、`standalone-video-probe.json`、[交付报告](artifacts/finite-closeout/DELIVERY.md) |

全量核心测试的唯一跳过仍是 `N01_N02_N03_NaturalPlayContract_DocumentedNotFaked`，它要求 Unity 真实操作，不能由核心测试伪造通过。D 批已有 Unity basic / Fever / Auto 证据；本轮另外执行了独立普通程序操作，但不把这些证据改写成该 xUnit 已执行。内部 `NaturalPlayRuntime` 的 standalone 尝试在 `TapToStage` 失败，没有产出本轮新的成功自然 Fever 场景；程序退出码 0 不能覆盖场景失败。失败原件见 `artifacts/finite-closeout/fever-player/natural-play.result.txt` 与 `fever-player.log`。随后无参数普通入口的独立操作成功，二者分别记录。

## D2 点敌证据

Session：`np-20260920T050557`；首场：`np-20260920T050557-001`。

```text
target=GameRoot/Canvas/field/foe/focusHit
focus=-1->1
seq=1 tick=17 kind=FocusEnemy slot=1 source=Player accepted=True reason=None
commands=True replay=True
```

未聚焦时目标依赖各技能规则，可能随机；这里的“非首个敌人”不等于假定已经知道上一次随机目标。上述命令由现有 UI 回调提交，没有直接写战斗状态。`np_04_focus.png` 已实际查看，包含游戏战场；随后 Tap、Slide、结算、NEXT 和 Home 的原有场景检查均通过。

当前模拟档案、截图和源文件 SHA 保存在同一新归档。归档 `source-hashes.json` 绑定受测源码；`run.json` 绑定 HEAD、session、battle IDs、时间及进程退出码。运行前备份的原 captures 摘要、截图和 BATTLE_INDEX 已逐文件恢复；新 battle 文件夹保留。

## D3 录像与同场回放

- 09-16 `np-continuous-20260916T085721.mp4` 容器可播；本次 140/270/420 秒三帧抽查均是其他桌面应用，没有观察到游戏 HUD。这是抽样结论，不断言全片没有任何游戏帧。
- 对应的三份第二批首场档案本次已实际读回通过。档案正确和视频录到游戏是两项不同验收。
- 本次尝试捕获隐藏 Unity 窗口生成 `basic-focus.mp4`，容器时长 17.633333 秒，但抽出的 9 秒帧为空白；**该新录像同样不合格**，原件保留。
- 详见 `artifacts/video-check/D3-mapping.md`。其中桌面抽样 PNG 留本机，不纳入此 Git 提交。

用户已明确回复“允许”显示 Unity 窗口，随后执行了可见补录。标题窗口捕获即使显示窗口仍会得到白屏，因此脚本改为录制实际 Unity 客户区对应的桌面区域。原片可能含启动/退出桌面，新增 raw 仅留本机；交付 `gameplay-proof.mp4`，同目录 `proof.json` 保留原 SHA、精确 PTS 截取范围、视觉抽样和完整解码结果。

`run-basic-focus.ps1` 复用原场景，允许 `-Scenario np.basic.v1 / np.fever.v1 / np.auto.v1`，要求 `-ShowWindow`。它使用本次 GI Cache 路径、从 durable captures 读取结果、记录前台/窗口位置警告、检查 ffmpeg 提前退出及退出码，并以 `q` 正常结束。为防遮挡，最后补录期间将专属 Unity 窗口置顶；关闭进程即结束。`Scenario=PASS` 与视频验收分开，录像仍需实际看帧。

三份派生片段均已独立抽帧与完整解码通过：basic 10.9 秒，Auto 5.5 秒，最终 Fever 110.766667 秒。Fever 包含实际 Fever HUD 和 CLEAR 结算，最后一帧仍显示首页；其后的 Game view 清空和桌面没有纳入。完整映射见 `artifacts/video-check/D3-visible-recordings.md`，失败尝试见 `artifacts/natural-play/VISIBLE_ATTEMPTS.md`。录制前的 15 个 captures 摘要、截图和索引均已恢复并逐一核对 SHA 相同，记录在 `artifacts/natural-play/capture-restoration-check.json`。

## 终局 Fever：历史实现及本轮替换

`np-20260920T064437-001` 首次读回只少 `fever_end/TimeUp`，Digest 差异为 FeverActive、FeverHitsLeft、事件数量。Unity 的 `GameRoot` 会在终局后继续 `TickFeverOnly`，其间 TickIndex 固定；旧 replayer 到 FinalDigest.TickIndex 即停止，未复现这段收尾。

历史实现通过 `FinishRecordedFever` 读取预期 FinalDigest 后有条件推进时钟，当时 `visible-fever-final.txt` 得到 879/879。该结果只能保留为历史：本轮反例证明，仅改变预期 FeverActive 就会改变实际重演输出，即使最终仍判 Match=False，也违反预期只用于比较的要求。

最终 `672cdff6` 改为独立录制与读取战斗结束 tick、终局 Fever 时钟及命令偏移。旧 `np-20260920T064437-001` 的 879 事件原件没有这一独立边界，当前 reader 正确报告 `TerminalFeverTicks: legacy record has no terminal Fever clock boundary.`，实际 878、预期 879，Match=False / exit 1。原件不回填、不修改。它是单独的负向对照，不在原有 9 份有效档案中。

原有 9 份档案本轮均严格通过。其最终 Fever 录像所对应的 `np-20260920T071843-001` 在 tick 2263 已记录 `fever_end/TimeUp`，tick 3268 才 `result/clear`，不存在缺时钟的终局尾段；1141 条事件继续通过是正确结果。诊断、当前回归与兼容范围见 [AUDIT.md](artifacts/finite-closeout/AUDIT.md)、[legacy-readback/README.md](artifacts/finite-closeout/legacy-readback/README.md) 和 [DELIVERY.md](artifacts/finite-closeout/DELIVERY.md)。

## 构建与环境

最终构建：`F:\Resonance\client\Builds\Win64\Resonance.exe`。实际独立试玩：`F:\Resonance\deliveries\G2-PathA-20260920\Resonance.exe`，无启动参数；包内程序身份见交付清单。

| 内容 | SHA-256 |
|---|---|
| `Resonance.App.dll` | `E6B5BCE1A4CA2B43967B73F0BB0ADC572FE4B6CADE1E60C7F6DBEEEA2A1E2FB6` |
| `Resonance.Battle.dll` | `8D87435CA8BE5B8B82F59610C8DE0E407C989BF4CC3172798EC077DA1161A723` |

最新构建于 `2026-09-20T09:57:19.7446406Z`（17:57:19+08）完成，绑定最终代码 `672cdff6`。exe 是 Unity 引擎壳，不能以 exe 单独判断源码版本。最新记录在 `artifacts/finite-closeout/build-run.json` 与 `validated-source-and-build.json`；此前 D1 / D3 / Growth 构建记录保留为历史证据。

独立普通试玩使用启动前生成的默认新档，未在开战后注入 HP、充能、Drive、时间或胜负。真实 OS 点击与上滑完成首场 Slide、Victory、NEXT，第二场到达 PHASE 2/2 后暂停、返回 Home；不声称第二场完成结算。125.9 秒录像与操作记录分开绑定，试玩后原 `save.json` 和 `.bak` 已恢复、SHA 与备份一致，见 `artifacts/finite-closeout/save-restoration.json`。

首次构建在默认 GI Cache 路径反复失败，已停止该次专属进程并保留日志；该目录当前可写，未武断认定为旧的缺失 junction 目标问题。重试只为本次 Editor 进程增加 `-giCustomCacheLocation F:/Resonance/client/Temp/GICache-D-batch` 后成功，没有修改全局偏好或注册表。该参数的会话范围见 [Unity 6000.3 官方文档](https://docs.unity3d.com/6000.3/Documentation/Manual/EditorCommandLineArguments.html)。

## 已修复：NEXT 成长跨运行时舍入

此前读回 NEXT 后的第二场 `np-20260920T050557-002` 为 `Match=False`，差异为 Ally[3] HP/MaxHP `3283 != 3284` 和 DataIdentity；命令、事件全 0。旧第二场 `np-20260916T085931-002` 也有相同失败，因此不是 D1 回归。原失败输出保留为历史记录；本轮对这两份未修改的原件重新读回，均已 **Match=True / 全差异 0**。

同一旧游戏 DLL 在 Mono / .NET 的 5,625 组输入中有 529 组属性差异。问题同时涉及 Body 倍率、好感与 Bond 倍率组成，以及 `Math.Round(int * float)` 乘积的中间精度。现在显式保留旧 float 系数的二进制值，以 double 完成中间计算并固定转换边界；新 DLL 的同一矩阵跨运行时 0 差异、相对旧 Unity 0 差异。全部 5,460 种 Body 系数组合与 101 档好感也逐值对照旧 Mono 一致。

成长修复时 Unity session `np-20260920T080808` 实际完成胜利 → NEXT → 第二场 → 暂停 → 首页；第二场 C003 HP 为 3283。首场 132 条事件、第二场 8 条事件均精确匹配。其中第二场 TickIndex=8、Outcome=InProgress，8 条事件全部是 tick 0 的开局波次/状态，唯一命令为 tick 8 Pause：它证明准确开局与已录短片段，不能证明第二场完整战斗。包含旧档案在内共 9 份录制在本轮最终 reader 下仍全部通过，原件指纹不变。此前运行前的 15 个 captures 文件已逐文件恢复并核对 SHA。

成长修复的诊断、回归与版本指纹见 [growth-rounding/README.md](artifacts/growth-rounding/README.md)。该历史成长修复没有更改 RulesVersion、录制格式或比较标准，也未为任意按旧 .NET 错误数值生成的档案增加迁移；本轮新增的独立终局时钟记录另见交付报告。上述验证不代表整个养成系统或原作还原已完成。后续最多三项体验候选见 [NEXT_EXPERIENCE_CANDIDATES.md](artifacts/finite-closeout/NEXT_EXPERIENCE_CANDIDATES.md)，仅列候选，不自动开工。
