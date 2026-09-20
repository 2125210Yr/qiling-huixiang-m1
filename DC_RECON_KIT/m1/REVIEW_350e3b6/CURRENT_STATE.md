# D 批修复状态 — 2026-09-20

D1–D3 限定事项已完成：默认开局恢复、真实点敌、三种场景的可见录像与同场首场回放。D1 / D2 基础修复提交：`40ab2fb`；终局 Fever 与 D3 收口提交：`60962824`。本轮继续修复了 NEXT 续战成长的跨运行时舍入差异，保留旧 Unity 数值；7 份旧档案和 2 份新档案全部精确读回。最新全量核心测试 **284 通过 / 0 失败 / 1 既有跳过**，Win64 构建成功。本批没有启动 G3，也没有宣称整个 PATH_A 或原作还原验收通过。

## 已完成的修复

- `OpeningGrowth.Recover` 优先处理明确 `source=input`，null/空数组/全 null 输入不再进入旧档案反推；`Parse` 保留 `-` 的 null 槽语义。既有序列化格式将空数组规范为 null，两者均表示默认开局，不额外扩格式。
- 新增 4 种输入形状回归与旧记录对照。既有 B1 夹具现在同时清空输入和来源字段，以真实模拟缺字段的旧档案；没有放宽身份或回放比较。
- basic 自然场景经已有 EventSystem 的 down/up/click 点击非首个存活敌人的 `focusHit`，检查新接受的 `Player FocusEnemy`、焦点变化、`commands.txt` 与解析后的 `replay.jsonl`；保留其余原有流程。
- 测试保留模式 `RESONANCE_KEEP_TEST_ARTIFACTS=1` 仅跳过递归清理，全部测试与断言仍执行。符合用户不批量删除文件的要求。
- `launch/在电脑上看.cmd` 现在先找 `client/Builds/Win64/Resonance.exe`，再回退 `dist/windows`，两者都不存在时明确退出。之前启动脚本优先命中 08-30 旧 DLL 的问题已改。
- `BattleReplayer` 现在按 FinalDigest 的边界补齐终局 Fever 收尾：仅在记录明确要求 Fever 已关闭且终局 Outcome / Tick 一致时调用真实 `TickFeverOnly`，保留仍活跃的终局快照。没有改战斗数值或放宽事件比较。
- `Growth` 明确倍率组成与属性乘积的精度边界，`Bond` 为成长计算提供共享原系数的内部精度路径；Unity Mono 与 .NET 现在得到相同成长属性，保持旧 Unity 数值与到偶数舍入。新增 6 项固定数值回归，录制脚本同时记录 Growth / Bond 源文件哈希。

## 真实执行结果

| 检查 | 结果 | 原件 |
|---|---|---|
| D1 红灯 | 4 失败 / 2 通过，失败对应新输入来源及 null 槽缺陷 | `artifacts/D1/D1-red.trx`、`.log` |
| D1 修复后 | 6 通过 / 0 失败 | `artifacts/D1/D1-green.trx`、`.log` |
| D1 后无 filter 全量核心测试 | 276 通过 / 0 失败 / 1 跳过 / 总数 277，退出 0 | `artifacts/tests/D-full.trx`、`D-full.console.txt`、`D-full-run.json` |
| 终局 Fever 回归红→绿 | 新回归先 1 失败 / 1 通过；修复后相关回归 14/14 通过 | `artifacts/tests/fever-terminal-{red,green}.{log,trx}` |
| 终局 Fever 后无 filter 全量核心测试 | 278 通过 / 0 失败 / 1 跳过 / 总数 279，退出 0 | `artifacts/tests/D-final.trx`、`D-final.console.txt`、`D-final-run.json` |
| 最新成长修复后无 filter 全量核心测试 | **284 通过 / 0 失败 / 1 既有跳过 / 总数 285**，退出 0 | `artifacts/growth-rounding/rounding-full.trx`、`rounding-full.log`、`full-run.json` |
| 同一新游戏 DLL 跨运行时属性矩阵 | **5,625 组 / 0 差异；相对旧 Unity 也 0 差异** | `artifacts/growth-rounding/after-summary.json` 与前后运行输出 |
| 成长修复后的旧档案精确读回 | **7/7 Match=True**，包括两份此前失败的 NEXT 场；所有差异 0，原件未改 | `artifacts/growth-rounding/readback/summary.json` |
| 新 Unity 续战及两场读回 | **场景 PASS；2/2 Match=True**，事件 132/132 与 8/8，截图 HP 3283 | `artifacts/natural-play/20260920T080750-np_basic_v1/`、`artifacts/growth-rounding/readback/fresh-next-summary.json` |
| 独立代码审查 | 未发现阻塞问题；确认中途 Persist 后结果页仍会更新最终 tape | 审查范围为 6 个生产/测试/启动文件；没有用审查代替运行 |
| 新 Unity basic | **PASS**，Editor 退出 0；Victory→NEXT→Pause→Home | `artifacts/natural-play/20260920T050523-np_basic_focus/` |
| 新首场回放 | **Match=True**，Command/Unconsumed/Digest/Event/Version 差异全 0 | `artifacts/readback/np-20260920T050557-001.txt` |
| 旧可播录像所对应的第二批首场 tapes | basic / fever / auto 均 **Match=True**，差异全 0 | `artifacts/readback/second-{basic,fever,auto}.txt` |
| 可见补录 basic / Fever / Auto 首场 | 新读回器全部 **Match=True**，分别 155/155、879/879、69/69 事件，差异全 0 | `artifacts/readback/visible-{basic,fever,auto}-final.txt`、`visible-final-summary.json` |
| 最终 Fever 重录首场 | **Match=True**，1141/1141 事件，差异全 0，1159 行 JSON 无错误 | `artifacts/readback/visible-fever-new.txt`、`visible-fever-new-summary.json` |
| 最新 Unity Win64 构建 | **成功**，退出 0 | `artifacts/growth-rounding/build-run.json`、`win64-build.log` |

全量核心测试的唯一跳过仍是 `N01_N02_N03_NaturalPlayContract_DocumentedNotFaked`，它要求 Unity 真实操作，不能由核心测试伪造通过。本次 Unity basic / Fever / Auto 已另行实际运行，但没有将此 xUnit 跳过改为通过。

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

## 本轮发现并修复的终局 Fever 边界

`np-20260920T064437-001` 首次读回只少 `fever_end/TimeUp`，Digest 差异为 FeverActive、FeverHitsLeft、事件数量。Unity 的 `GameRoot` 会在终局后继续 `TickFeverOnly`，其间 TickIndex 固定；旧 replayer 到 FinalDigest.TickIndex 即停止，未复现这段收尾。

新增回归先复现同样三项差异，再通过 `FinishRecordedFever` 有条件推进真实时钟解决。另一个测试保证终局仍活跃的快照不被推进；无 FinalDigest 的旧 Run 路径不受影响。修复前失败保留在 `visible-fever.txt`，修复后同一原件 `visible-fever-final.txt` 为 **Match=True / 879 对 879 / 全差异 0**。独立只读审查未发现阻塞问题；全量测试已对新源码重跑。

## 构建与环境

实际新构建：`F:\Resonance\client\Builds\Win64\Resonance.exe`。

| 内容 | SHA-256 |
|---|---|
| `Resonance.App.dll` | `C9C1E4A79C5E26A567BE7869A6A0201F25DDE08DE38A14269203485D0CD73F6F` |
| `Resonance.Battle.dll` | `0258465CAB197DC8D837C61BAA533FFD1FA3D187A869B46BE23F9A2E741923BC` |

最新构建于 09-20 16:06:40+08 完成；Battle DLL 修改时间 16:06:33+08，未变更的 App DLL 沿用 13:14:39+08 产物。exe 是 Unity 引擎壳，不能以 exe 单独判断源码版本。最新记录在 `artifacts/growth-rounding/build-run.json` 与 `after-summary.json`，此前 D1 / D3 构建记录保留为历史证据。

首次构建在默认 GI Cache 路径反复失败，已停止该次专属进程并保留日志；该目录当前可写，未武断认定为旧的缺失 junction 目标问题。重试只为本次 Editor 进程增加 `-giCustomCacheLocation F:/Resonance/client/Temp/GICache-D-batch` 后成功，没有修改全局偏好或注册表。该参数的会话范围见 [Unity 6000.3 官方文档](https://docs.unity3d.com/6000.3/Documentation/Manual/EditorCommandLineArguments.html)。

## 已修复：NEXT 成长跨运行时舍入

此前读回 NEXT 后的第二场 `np-20260920T050557-002` 为 `Match=False`，差异为 Ally[3] HP/MaxHP `3283 != 3284` 和 DataIdentity；命令、事件全 0。旧第二场 `np-20260916T085931-002` 也有相同失败，因此不是 D1 回归。原失败输出保留为历史记录；本轮对这两份未修改的原件重新读回，均已 **Match=True / 全差异 0**。

同一旧游戏 DLL 在 Mono / .NET 的 5,625 组输入中有 529 组属性差异。问题同时涉及 Body 倍率、好感与 Bond 倍率组成，以及 `Math.Round(int * float)` 乘积的中间精度。现在显式保留旧 float 系数的二进制值，以 double 完成中间计算并固定转换边界；新 DLL 的同一矩阵跨运行时 0 差异、相对旧 Unity 0 差异。全部 5,460 种 Body 系数组合与 101 档好感也逐值对照旧 Mono 一致。

新 Unity session `np-20260920T080808` 实际完成胜利 → NEXT → 第二场 → 暂停 → 首页；第二场 C003 HP 为 3283。首场 132 条事件、第二场 8 条事件均精确匹配。包含旧档案在内共 9 份录制读回全部通过，原件指纹不变。独立只读代码审查未发现阻塞问题，运行前的 15 个 captures 文件已逐文件恢复并核对 SHA。

完整诊断、回归与版本指纹见 [growth-rounding/README.md](artifacts/growth-rounding/README.md)。没有更改 RulesVersion、录制格式或比较标准，也未为任意按旧 .NET 错误数值生成的档案增加迁移。此轮验证覆盖成长精度与实际续战回放，不代表整个养成系统或原作还原已完成。
