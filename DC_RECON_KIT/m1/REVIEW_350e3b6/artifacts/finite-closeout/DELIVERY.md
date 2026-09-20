# G2 路 A 有限收口交付

本轮完成了独立回放边界修复、最终版本验证，以及可直接启动的 Windows 包和对应普通操作录像。结论限定为本次 G2 工程收口，不宣称整个游戏或原作还原已完成。

| 状态 | 结论 |
|---|---|
| PATH_A_ENGINEERING | **PASS_WITH_RECORDED_LIMITATIONS**，限本轮 TASK.md 的 G2 收口范围 |
| M1_FIDELITY | **DEFERRED_NOT_REMOVED** |
| G3 | **NOT_STARTED** |
| REMOTE_SYNC | **NOT_PUSHED**；开始时远端 m1-gt6-review 实查为 350e3b6459081807cd3699a530c83dbf40b66886 |

## 可试玩交付

- 压缩包：[G2-PathA-20260920.zip](F:/Resonance/deliveries/G2-PathA-20260920.zip)。完整解压后双击 `开始试玩.cmd` 或 `Resonance.exe`。
- 已实际运行的同内容目录：`F:/Resonance/deliveries/G2-PathA-20260920/`；[试玩说明](F:/Resonance/deliveries/G2-PathA-20260920/试玩说明.md)。
- 连续录像：[试玩实录.mp4](F:/Resonance/deliveries/G2-PathA-20260920/试玩实录.mp4)，125.9 秒、H.264、756×1344、无音轨。
- 精确包大小、SHA-256、ZIP 内容流校验见 [package-summary.json](package-summary.json)。录像 SHA、抽帧范围和完整解码结果见 [video-proof.json](video-proof.json)。

包包含 UnityPlayer、Mono、Data、D3D12、原生插件和 catalog.json，普通路径不依赖源码目录。外部配置按现有查找规则置于包内 `Assets/Content/catalog.json`，SHA 为 `1185CBA420CC3E571337C0F2283BE54A60DD312A26FF1418386A56D366A302D6`。运行前后的 170 个运行文件指纹一致。没有把个人存档、原始桌面片段或开发调试符号放入交付包。

实际进程路径是本交付目录的 `Resonance.exe`，无 smoke/natural 参数，见 `standalone-process.json`。通过 `@oai/sky` 操作系统鼠标点击及上滑执行：普通首页 → 关卡 → FIGHT → Slide → 首场 CLEAR → NEXT → 第二场第二波 → PAUSE → HOME。没有在战斗开始后修改 HP、充能、Drive、时间或胜负，也没有强制 QTE 评级。

第二场在 phase 2/2、剩余 01:06、敌方 HP 54% 时暂停返回，**没有第二场终局**。点敌尝试和结果出现后的头像点击不冒充已接受技能证据；本轮无需重新认证已有 D2 的命令落盘。输入时刻见 `standalone-actions.json`。录像检查采用全片每秒抽帧、关键帧和完整解码；抽帧均为游戏区域，没有观察到桌面遮挡，不声称逐帧人工观看。游戏客户区从录制开始到结束保持可见，交付视频是原连续游戏区视频的逐字节副本。

启动前用未修改的 `SaveBlob` 默认值及 `EnsureStarterKit` 生成一级队伍，手动模式、1 倍速。试玩后的单位等级为 2。程序退出后逐一恢复 `save.json` 及 `.bak`，两者 SHA 均与录制前一致；见 `profile-preparation.json`、`post-play-save.json`、`save-restoration.json`。包不包含原个人存档。

## 修复与验证

代码提交：**672cdff6**（完整 SHA `672cdff6a7a821347cb14ab75e11760cc1b9fe6d`）。最终验证前冻结 225 个源码、测试及构建输入；测试/构建后 0 差异，见 `frozen-source-hashes.json`、`validated-source-and-build.json`。

原实现使用预期 `FinalDigest.FeverActive` 决定终局时钟是否继续，篡改预期会改变实际重演。现代记录现在独立保存战斗停止 tick、终局 Fever 时钟次数和终局命令偏移；回放按这些输入推进，预期 digest/事件仅参与比较。调速/自动指令与终局时钟保持原顺序。没有改战斗伤害、胜负或奖励逻辑。现代边界字段缺失、重复或非法时明确拒绝；旧终局尾段缺边界也拒绝，不从预期结果反推。普通旧档保留以旧 digest.TickIndex 定位结束的既有兼容路径，不声称它们拥有现代独立边界保证。详见 [fever-boundary/README.md](fever-boundary/README.md)。

| 验证 | 结果及范围 |
|---|---|
| 最终全量核心测试 | **310 通过 / 0 失败 / 1 既有跳过 / 总数 311**，无 filter、exit 0；`full.trx`、`full-test-run.json` |
| Fever 最终定点回归 | **28/28**；包含篡改预期、终局输入顺序、非法边界及序列化反例 |
| 最终 Win64 构建 | **成功、exit 0**，2026-09-20T09:57:19.7446406Z；`build-run.json`、`win64-build.log` |
| 新 reader 读取指定旧档案 | **9/9 Match=True**，全部差异 0，63 个原件文件前后指纹不变；`legacy-readback/summary.json` |
| 旧终局尾段负向对照 | **按预期拒绝**：879 事件样本缺独立终局时钟，Match=False / exit 1；`legacy-readback/terminal-legacy-summary.json` |
| 当前包普通实际操作 | **首场结算 → NEXT → 第二场第二波 → 暂停返回通过**；连续视频、进程身份及输入记录 |
| 存档及运行文件恢复/完整性 | 原存档两文件恢复一致；包内 170 个运行文件试玩前后 0 差异 |

唯一跳过仍是 `N01_N02_N03_NaturalPlayContract_DocumentedNotFaked`，要求 Unity 真实 EventSystem、battle_id 和相应 Submit 操作，不能由核心测试伪造通过。新普通试玩不被改写为该 xUnit 已执行，也不声称覆盖了 Fever/Auto 全部输入。已有 D2/D3 对应版本的自然操作证据保留。

本轮额外尝试的独立程序内部 `NaturalPlayRuntime` 在 `TapToStage` 因首页 raycast 为空失败，尚未开始战斗；进程 exit 0 **不等于场景通过**。`fever-player/`、`fever-player.log` 原样保留，此次失败不作为 Fever 实玩证据。普通入口实际鼠标操作随后成功，终局回放代码以定点回归、严格旧档读回和负向对照验证。此自动操作脚本限制没有扩展为额外修复任务。

| 构建程序集 | SHA-256 |
|---|---|
| Resonance.Battle.dll | `8D87435CA8BE5B8B82F59610C8DE0E407C989BF4CC3172798EC077DA1161A723` |
| Resonance.App.dll | `E6B5BCE1A4CA2B43967B73F0BB0ADC572FE4B6CADE1E60C7F6DBEEEA2A1E2FB6` |
| 新 Readback.dll | `30A6873E54C159A717EC5A85B054051EF0AE3BAEE27A55150C004202AE5732E6` |

## 历史证据的准确边界

旧 NEXT 录制 `np-20260920T080808-002` 的 8 个事件都在 tick 0，只有 tick 8 的 Pause 命令，Outcome=InProgress。它证明开局属性 HP/MaxHP=3283 与已录短片段，不是第二场打完。同 session 首场确有 Victory、132 个事件。此次更长的第二场录像也仍不代表第二场结算。

旧 Fever 879 事件档案 `np-20260920T064437-001` 无独立终局边界；此前依赖预期补时钟的绿色输出仅作历史记录，不能继续作为严格重放通过。最终 D3 的 1141 事件档案 `np-20260920T071843-001` 则在 tick 2263 结束 Fever、tick 3268 才 Victory，故无需终局尾段即可正确读回。两者不能混为一谈。

上轮测试没有事前保存全部输入哈希，不追认其直接验证已提交的 38629718；已有自然操作/矩阵的源文件哈希关系及旧 SOURCE_IDENTITY 版本限制见 [AUDIT.md](AUDIT.md)。本轮使用事前冻结清单绑定最终测试、构建与试玩。已有 5,625 组成长跨运行时 0 差异证据复用，没有修改对应计算或重做 G0/G1。

后续仅列出 [3 项已有 P0/P1 支持的体验候选](NEXT_EXPERIENCE_CANDIDATES.md)，尚未实施。原作像素、精确节奏、数值还原仍待验收；没有启动 G3/M2、动画替换或恢复资料采集。
