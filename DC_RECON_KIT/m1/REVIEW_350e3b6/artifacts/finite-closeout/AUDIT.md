# 定点核对记录

本轮最终代码为 `672cdff6`。结论为 `PATH_A_ENGINEERING=PASS_WITH_RECORDED_LIMITATIONS`（仅限本轮 G2 有限收口），`M1_FIDELITY=DEFERRED_NOT_REMOVED`，`G3=NOT_STARTED`，`REMOTE_SYNC=NOT_PUSHED`。交付入口与清单见 [DELIVERY.md](DELIVERY.md)。

## 起点与修正依据

开始时主项目 HEAD 为 `38629718`。读到的 6pro 意见要求有限收口与独立试玩，允许纠正不合理实现，不扩大到 G3/M2。上轮完成过的项目不因新会话重复开启。

Fever 定点反例确认了需要最小修正的问题。以当时的测试程序集加载现有 `BeforeVictory` 夹具，同一份 active-terminal tape 原始读回为 Match=True、FeverActive=True、FeverLeft=0.21666667、Events=12、Tick=1。仅篡改预期 `FinalDigest.FeverActive=false` 后，Match=False，但实际重演变成 FeverActive=False、FeverLeft=0、Events=13，Tick 仍为 1。它没有把单字段篡改误判通过；问题是预期答案确实改变了模拟输出。

因此本轮现代记录改用独立录制的战斗结束 tick、终局 Fever 时钟次数及终局命令时钟偏移，预期 digest 和事件只作比较。普通旧档仍保留以旧 digest.TickIndex 定位结束的兼容路径，不能说它们具备现代独立边界保证。终局调速与时钟推进需要保留实际交错，不能把同一 battle tick 的全部命令先执行完再补时钟。具体红绿测试和最终源码身份以本目录交付报告为准。

确切缺少独立时钟的旧终局 Fever 原件是 `np-20260920T064437-001`，预期 879 条事件。此前以预期状态补推得到的 879/879 绿色读回只能作为历史运行结果保留。当前 reader 对未修改原件明确报告 `TerminalFeverTicks: legacy record has no terminal Fever clock boundary.`，Match=False、Ok=False、exit 1；实际 878 条事件，CommandDiff=1、DigestDiff=3、EventDiff=1、VersionDiff=0、Unconsumed=0。这是正确拒绝缺输入的单独负向对照，见 `legacy-readback/terminal-legacy-summary.json`。

不能把此结论扩成“旧 Fever 不能读回”。指定的原有 9 份档案在最终 reader 下 **9/9 Match=True、Ok=True、差异全 0**，63 个原件文件指纹不变。其中最终 Fever `np-20260920T071843-001` 的 1141 条事件仍有效：Fever 在 tick 2263 结束，Victory 在 tick 3268，没有缺时钟的终局尾段。879 事件负向对照不属于这 9 份档案，不能混入或贬低其通过率。明细见 `legacy-readback/README.md` 与 `summary.json`。所有旧原件不回填、不修改。

## NEXT 第二场的准确范围

档案：`../natural-play/20260920T080750-np_basic_v1/battles/np-20260920T080808-002/`。

- `replay.jsonl` 的 digest：TickIndex=8、Outcome=InProgress、TimeLeft=119.467。
- 8 个事件全在 tick 0，只包含进入波次与状态施加。
- 唯一命令是 tick 8 的 Pause。
- C003 HP / MaxHP=3283，原来的 8/8 精确读回证明开局成长属性和已录短片段、暂停状态。
- 同 session 首场 TickIndex=272、Victory、132 个事件，首场确实覆盖终局。

第二场没有完整打完。这里纠正验收表述，不重开已经有跨运行时矩阵和固定回归支持的成长舍入缺陷。

## 初始版本绑定与复用限制

只读核查时 Growth、Bond、BattleReplay、NaturalPlayRuntime、NaturalPlayBattleEvidence 五份源文件 SHA 与 `20260920T080750-np_basic_v1/source-hashes.json` 一致；Growth/Bond 另与构建、5,625 组矩阵及读回摘要一致。9 份原档案的 63 个文件当前 SHA 与原读回后的记录全部一致。

初始 Win64 Battle DLL：`0258465CAB197DC8D837C61BAA533FFD1FA3D187A869B46BE23F9A2E741923BC`。初始 Readback DLL：`85E9C06938E07F8988BF6094A696EBB1C1B54F731A6B935E0DD3AADCBCBCFD86`。本轮修改回放边界后必须重新绑定受影响的读回器与构建，不能直接沿用这些二进制哈希。

旧 `SOURCE_IDENTITY.md` 仅对应历史 `3d2d1e6` / `1b2ca8e`。上轮 `full-run.json` 只有时间、退出码与 filter，未保存全树或测试程序集哈希；自然操作和读回记录的 HEAD 是修改尚未提交时的 `60962824`，必须结合源文件 SHA，不能说当时直接测试了已提交的 `38629718`。本段是事后核对，不追认不存在的事前冻结。

`F:/天命之子/client` 已确认是指向 `F:/Resonance/client` 的 junction，两套路径是同一工程，不移动工程或重组目录。

## 唯一既有跳过

历史成长修复全量 TRX：285 总数、284 通过、0 失败。本轮最终无 filter 全量 TRX：**311 总数、310 通过、0 失败、1 跳过**，退出 0，绑定代码 `672cdff6`。唯一跳过仍为 `N01_N02_N03_NaturalPlayContract_DocumentedNotFaked`，要求 Unity 真实 EventSystem 操作与 battle_id、Submit Tap / Slide / FeverTap / SetAuto。核心 xUnit 不能伪造这部分通过。既有自然运行与此次普通 Win64 试玩属于单独证据；不把它们改写成该 xUnit 已执行。原件为 `full.trx`、`full-test.log`、`full-test-run.json`。

## 最终源码、构建与独立普通试玩

`frozen-source-hashes.json` 是本轮运行前冻结清单。最终验证后的 `validated-source-and-build.json` 对照 **225 项输入、0 差异**，与前文的历史事后核对分别记录。Win64 构建于 `2026-09-20T09:57:19.7446406Z` 成功、退出 0，见 `build-run.json`。

| 当前二进制 | SHA-256 |
|---|---|
| `Resonance.Battle.dll` | `8D87435CA8BE5B8B82F59610C8DE0E407C989BF4CC3172798EC077DA1161A723` |
| `Resonance.App.dll` | `E6B5BCE1A4CA2B43967B73F0BB0ADC572FE4B6CADE1E60C7F6DBEEEA2A1E2FB6` |
| 兼容读回使用的 `Readback.dll` | `30A6873E54C159A717EC5A85B054051EF0AE3BAEE27A55150C004202AE5732E6` |

独立程序实际运行于 `F:/Resonance/deliveries/G2-PathA-20260920/Resonance.exe`，无启动参数。启动前准备默认新档后，经普通首页/关卡和真实 OS 鼠标点击、上滑完成首场 Slide、Victory、NEXT；第二场到达 PHASE 2/2，暂停时剩余 01:06、敌方 HP 54%，随后返回 Home。**第二场没有结算**。没有战斗状态注入或强制 QTE 评分，结算后误点头像也没有被计作有效技能输入。实际进程和动作见 `standalone-process.json`、`standalone-actions.json`。

该版本实际录像为 **125.9 秒**，信息见 `standalone-video-probe.json`，完整交付及录像检查记录见 [DELIVERY.md](DELIVERY.md)。试玩后原 `save.json` 和 `.bak` 已逐文件恢复，恢复后 SHA 与备份一致，见 `save-restoration.json`；此处不把人工 OS 流程声称为新录制的精确回放 tape。

## 明确保留的限制

内部 `NaturalPlayRuntime` standalone 尝试在 `TapToStage` 失败，见 `fever-player/natural-play.result.txt` 和 `fever-player.log`。`fever-player-run.json` 的进程退出 0 不能当作场景 PASS；本轮没有产出这次尝试的新成功自然 Fever 档案。随后无参数普通 Win64 操作成功，是另一条独立证据，不能覆盖或隐藏内部场景失败。

原作像素、精确时序和精确数值还原继续 `DEFERRED_NOT_REMOVED`。保留旧 Unity 成长结果只代表本项目兼容性；没有证明它等于原版游戏数值，也没有验证 IL2CPP 或其他未来平台。

本轮停止在有限收口与独立试玩交付。`NEXT_EXPERIENCE_CANDIDATES.md` 的三个候选不自动开工；不启动 G3/M2，不恢复已暂停搜片，不扩养成、抽卡、网络或动画方案。
