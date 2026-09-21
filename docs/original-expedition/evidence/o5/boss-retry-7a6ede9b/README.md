# 7a6ede9b：普通首领败北、重开与获胜证据

2026-09-21，实际 Windows Player、普通入口、同一趟 B01–B04 构筑。本目录只归档回放、独立核验报告、画面和媒体索引；未修改用户存档、原始 tape、生产代码或全局验收状态。

结论：零主动技能自然败北后，普通关闭并重开，保持原冻结检查点，通过暂停编排 26 次主动技能获胜。两份实际 Player 回放均由现有 .NET 6 CLI 严格重放为 **MATCH**。两段录像分别完整解码且已亲自检查前、中、后帧，画面随战斗及界面转换而变化。

## 严格重放及输入比较

| 实际 tape | 结果 / endTick | 主动成功 / 拒绝 | CLI |
| --- | --- | --- | --- |
| `0aed9659585c4708995d6c591a206753` | Defeat / 2070 | 0 / 0 | MATCH，exit 0 |
| `5481ae3998634dd98a63671583599c11` | Victory / 2553 | 26 / 0 | MATCH，exit 0 |

完整 tape 在 `replays/`，未改写任何字段；逐份 CLI JSON 在 `reports/*.verify.json`。命令：

```powershell
dotnet 'F:/天命之子/tools/OriginalReplay.Verify/bin/Release/net6.0/OriginalReplay.Verify.dll' '<本目录/replays/实际文件名>'
```

Verifier DLL SHA-256：`8dd4971f1ef08a421e436559386af1f653fadd5b9ed0610d622fd8703b6b3d1e`。schema 2 / format `original-expedition-replay-v2` / content `original-expedition-content-v0.1.1`，内容 hash `69aaa51d7fa5077a5e1d30b4c5f2fd042b62f932f86d0dec59a037762880fdbd`。

`reports/checkpoint-profile-comparison.json` 记录下列独立检查：

- RunId `158dfbcbfa944ee5ad276bb015e20236`，runSeed `8509421`，bossSeed `1964763356`，N7 / 第 5 场，single，B01–B04，开局生命 `[5200,6800,5600,5000,5400]`。
- 败北与胜利 `Input` 递归逐叶比较，唯一差异为 `AttemptId`：`90d18bd8377d49f8975a1f4299e0933a` → `ac39636336f14edfb19d643c680cea47`。
- 两份 tape、关闭前 Boss/Current checkpoint、重开后 Boss/Current checkpoint 共六份类型化输入，在内存克隆中仅将 `AttemptId` 设为真正的 null 后，IEEE 精确 canonical hash 均为 `1dc39a7735865a389af94e700aa59ea6a381088af1d45b7115131e4c048e3172`。未改数字精度或使用容差。
- `checkpoint-before-close.json` 与 `checkpoint-after-reopen.json` 原始字节相同，SHA-256 `29d221f71f58cbc12a9575b0eb087b03775f8e0ca2ed2ed7f2842e8ac13a386f`，revision 33 / BossRetry / 已完成 4 场。
- 通关与返回据点快照原始字节相同，SHA-256 `05cd4b68967181bc852cd3d5cde85e8765a86d70399d8d23d96cb151bab3cc19`，revision 35，`ActiveRun=null`，永久首通 `silent-theatre`、配置 `sweep` 已解锁，原 6 件发现保留。上趟摘要标记 Victory / ResultApplied / UnlockedNewPreset，并保留完成 5 场及路线。
- 四份 profile 均通过结构验证与原存档完整性 hash 重算。完整个人 profile 只保留在本地录制目录，未复制进本目录。
- 个人 legacy `save.json` 及 `.bak` 本轮前后均为 `624cc9c6882bb512a7effe419f3b47332747be33a33726796df611612e55c2a7`，字节 hash 不变。

`build-analysis.ps1` 只读上述输入，生成本目录内分析文件；对 string 字段使用反射设 null，避免 PowerShell 将 `$null` 隐式转换为空字符串。它不会调用保存用户档案的方法。

## 可核对的真实结算

`reports/battle-settlement-analysis.json` 从 tape 的原始 resolutions / commands 提取，而非依据遗物图标。

| 指标 | 自然败北 | 重开后胜利 |
| --- | ---: | ---: |
| 有效敌伤 | 32734 | 73000 |
| 其中遗物追加有效伤害 | 12408 | 25139 |
| 护盾实际吸收 | 0 | 21590 |
| 队员生命伤害 | 28000 | 22435 |
| 有效治疗 / 过量治疗 | 0 / 0 | 20410 / 2590 |
| B03 双目标散射的 root 数 | 52 | 65 |
| B04 回响次数 / 有效伤害 | 1 / 134 | 1 / 137 |

胜利 tape 有 8 次 Pause、8 次 Resume、4 次 FocusEnemy，26 个 Tap 全部成功。B04 胜利样本 root 159：`OE_HEALER_auto` 对第二代面具造成原生击杀（请求 137、有效 54、过量 83），随后 B01 分别向两个不同存活敌人散射 96，并由 B04 向首领回响 137。结算页显示的敌伤、追加伤害、治疗、吸收及主动次数均与这些计数相同。

## 两段有效录像与时间索引

本地原件目录：`F:/天命之子/dist/original-expedition-v01/resume-7a6ede9b-20260921-144630`。MP4 未复制进 Git 证据目录。

| 原件 | 时长 / 帧数 | SHA-256 |
| --- | --- | --- |
| `boss-natural-defeat-before-reopen.mp4` | 112.066667 秒 / 1681 | `3dc1b57837c2a5ac2de063237c60d0446b85d746971ed6218d4626deae13dd3f` |
| `boss-retry-after-reopen.mp4` | 466 秒 / 6990 | `6ef5fe03726e4dfa5892d943b7053998386b241240b423fd553cbd915e10ab4d` |

均为 756×1344、H.264、15 fps **采集**。这不是 O-023 的真实渲染 30/120 档证据。独立执行 `ffmpeg -hide_banner -v error -i <原件> -f null -`，两份全部解码 exit 0、无错误；`reports/video-analysis.json` 保存 ffprobe 元数据、所有抽帧媒体时间和亲自检查的画面描述。

| 视频 / 媒体偏移 | 已亲自查看的画面 |
| --- | --- |
| defeat 5 秒 | 原样重试、全员冻结开局生命、B01–B04 |
| defeat 26 / 30 秒 | 两面具存活时的 2.0 倍读条；其后群攻造成生命变化，主动次数仍为 0 |
| defeat 60 秒 | 面具已清空，1.0 倍，队员生命下降 |
| defeat 90 / 110 秒 | 自然败北后的原样重试页；结果页静止属预期 |
| retry 5 秒 | 重开后仍是同一原样重试页 |
| retry 45 / 90 秒 | 暂停编排；四个主动执行后，两面具已倒，1.0 倍群攻读条可见 |
| retry 180 / 245 秒 | 首领生命继续下降；阶段 2，两面具复建 |
| retry 330 / 410 秒 | 第二代面具已倒；护盾、生命和主动次数更新，最后首领剩 269 生命 |
| retry 425 秒 | 胜利结算、完成 5 场、横扫首通解锁 |
| retry 442 / 450 / 462 秒 | 据点横扫可选 → 打开上趟回顾 → 返回据点 |

抽帧是 `video-frames/` 中的原视频解码输出，无拼接或画面修改。`screens/` 另保留 6 张普通 UI 操作截图。`events.json` 包含最终 56 次操作记录，末条为 `hub-close-before-isolated-render-test`。UTC 操作记录和媒体偏移分别保留，不把文件创建时间冒充准确视频时间码。

第一段在普通关闭 Player 时收到 EOF、exit 0；第二段在重新启动后单独开始，最后由 stdin `q` 正常结束、exit 0。因此这是**两段录像加相同冻结检查点与操作记录**，不声称存在跨程序退出的单段连续录像。

## 录屏修复预检及精确命令

旧 GDI 冻结视频的 INVALID 结论保持不变。改用 `gfxcapture` / Windows.Graphics.Capture。最初 `OriginalExpedition[.]exe` 中方括号被 FFmpeg filtergraph 解析而报 `Trailing garbage`；操作者改为下列实际成功命令，同时精确匹配标题，启动时确认只有一个对应 Player。

```powershell
ffmpeg -hide_banner -loglevel warning -n -filter_complex 'gfxcapture=window_title=^契灵回响$:window_exe=(?i)^OriginalExpedition.exe$:max_framerate=15,hwdownload,format=bgra,fps=15,format=yuv420p' -t 600 -c:v libx264 -preset veryfast -crf 20 'F:/天命之子/dist/original-expedition-v01/resume-7a6ede9b-20260921-144630/boss-natural-defeat-before-reopen.mp4'
ffmpeg -hide_banner -loglevel warning -n -filter_complex 'gfxcapture=window_title=^契灵回响$:window_exe=(?i)^OriginalExpedition.exe$:max_framerate=15,hwdownload,format=bgra,fps=15,format=yuv420p' -t 1800 -c:v libx264 -preset veryfast -crf 20 'F:/天命之子/dist/original-expedition-v01/resume-7a6ede9b-20260921-144630/boss-retry-after-reopen.mp4'
```

没有指定缩放尺寸；窗口客户区原尺寸为 756×1344。上述原文由实际录像操作者提供；本证据 agent 独立验证产物与抽帧，不冒称自己启动了录屏。首段 session 72598；第二段 session 52306。

预检第一次 `-t 20` 的录像结束后 3 秒才点击，故为 INCONCLUSIVE。第二次使用相同参数、`-t 60`、输出 `gfxcapture-preflight-02.mp4`、session 91980，通过 `q` 于 28.6 秒结束。`preflight/recording-preflight-result.json` 与两张 PNG 原样复制；本 agent 也亲自查看，B01 详情从展开变为收起，证明该预检捕获到了 UI 转换。预检只证明录屏更新，不证明首领通关或真实渲染帧率。

## 本轮证据能支持的验收范围

| 子项 | 本轮提供的证据及边界 |
| --- | --- |
| O-027 | 该实际种子 / B 构筑的自然败北 → 普通关闭重开 → 原检查点 → 不同玩家指令获胜，严格输入比对与两段有效录像齐备。 |
| O-001 | 本轮前后个人 legacy 两文件 hash 未变；补充实际 Player 隔离证据。 |
| O-017 / O-032 | 实际首领败北与跨阶段胜利严格重放匹配；真实结算与画面数字一致。 |
| O-020 / O-021 | 补充 2 面具 / 0 面具读条、清面具及阶段 2 复建的普通画面；不替代 0/1/2 全部边界和死亡优先模拟用例。 |
| O-024 | 普通暂停编排 8 轮、26 个主动成功；本轮未覆盖同 slot 覆盖、目标失效或资源不足拒绝全部边界。 |
| O-028 | 本次首通清章和横扫解锁；重复回调 / 重复通关幂等仍由专门用例证明。 |
| O-029 | 证明胜利清空 ActiveRun、永久发现 / 解锁保留、据点上趟摘要可读；**本轮没有创建下一趟，不能单凭本轮宣称两趟连续实玩通过**。 |
| O-036 | B01–B04 在 N7 胜利前已持有且 B03 / B04 实际触发；与此前 N5 证据合用，不补造前四场录像。 |
| O-037 | 本轮暂停、败北、退出、重开、重试、阶段 2、胜利、据点和上趟摘要可见；不等于所有 UI 压力场景都已验收。 |
| O-003 / O-038 | **仍缺新包从 N0 到 N7 的有效完整普通流程录像**。此前前四场 GDI 录像冻结，本轮仅补齐首领段，不能拼称完整五战连续录像。 |
| O-023 | 本目录不提供真实渲染 30/120 档的测量结论，由独立帧率夹具报告。 |

`.gitattributes` 对本证据目录设置 `* -text`；`sha256-manifest.json` 记录归档文件以及保留本地的媒体 / profile 原件 hash。未提交此目录，由主任务统一审阅、更新验收文档并提交。
