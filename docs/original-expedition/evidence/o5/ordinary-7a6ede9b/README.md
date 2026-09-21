# 7a6ede9b 普通试玩：四场胜利与首领败北

2026-09-21，使用当前独立 Win64 候选包无参数启动，经真实界面选择 B 核心并依次完成 N1、N2 观众席、工坊休整、N4、N5，再进入 N7。**普通试玩已中断；录像抽检发现画面冻结，状态为 INVALID_STALE_CAPTURE，不能作为任何战斗流程录像证据。** 有效证据仅为独立 Sky 截图、输入记录及真实 Player 回放。首领首次自然败北后通过界面原样重试，选敌后的暂停操作被 Computer Use 以物理 Esc 中止；没有再发送桌面输入，第二次尝试随后自然败北。未取得首领胜利、关闭重开后的重试或结算后新趟证据。

## 版本与记录

- Player 源码：`7a6ede9b643a3470a9f0924d329a5ca769b40ce2`，内容 `original-expedition-content-v0.1.1`，回放 Schema 2。
- 普通 run：`158dfbcbfa944ee5ad276bb015e20236`，界面种子 `8509421`。不是受控家族测试的固定种子。
- [启动身份](session.json)、[实际发出的输入记录](events.json)、[录像与存档检查](recording-result.json)、[副本哈希清单](evidence-files.json)。`events.json` 的 label 表示操作意图，实际执行以画面与回放命令为准；例如 N4 最后一次拟点击攻击时已进入奖励页，实际展开了 B04 详情，没有把它计作成功攻击。
- 原片：`F:/天命之子/dist/original-expedition-v01/play-7a6ede9b-20260921/ordinary-expedition-7a6ede9b.mp4`。675.8 秒、756×1344、15 FPS、10,137 帧；SHA-256 `61d59cad400a601ea0b242c8e576fb9ae92fa6ca595e3f7a6466be19904ff114`。录屏通过 stdin `q` 正常结束，完整解码 exit 0；但随后抽取 0.5、341、668 秒画面均为陈旧入口，标题区域从 16.666667 秒起持续冻结。初次容器检查保留在 `recording-result.json`，**最终视觉判定以 [video-visual-validation.json](video-visual-validation.json) 为准**。15 FPS 是录屏参数，不证明游戏实际渲染帧率。
- 没有调试启动参数或战中状态注入。原个人 `save.json` 与备份 SHA 均保持开工前值。实际原创档案和检查点副本仅保存在本地录制目录，没有加入此证据目录。

## 实际回放结果

六份原件只读复制后，由独立 .NET v2 CLI 从磁盘核验，全部严格 MATCH。每份 tape、独立报告、输入/输出哈希和验证器身份在 [回放总表](replays/ordinary-run-replay-summary.json) 与三个 `verification-batch-*.json` 中。

| 场次 | 结果 | EndTick | 普通流程范围 |
| --- | --- | ---: | --- |
| N1 前厅 | Victory | 660 | B01，战后正常选择 B02 |
| N2 观众席 | Victory | 726 | B01/B02，战后正常选择 B03 |
| N4 排练厅 | Victory | 924 | B01/B02/B03，战后正常选择 B04 |
| N5 返场回廊 | Victory | 714 | B01–B04 成型后真实使用，随后门前全队恢复 |
| N7 首次 | Defeat | 2070 | 未释放主动技能，自然全灭 |
| N7 重试 | Defeat | 2070 | 第 172 tick 选面具；随后的暂停被 Esc 中止，未释放主动技能 |

两次 N7 的完整冻结输入除 `AttemptId` 外精确相同，并等于首次开战前保存的检查点；这证明本次败北重试保留了输入，**不证明改变操作后获胜**。最后只读观察为 `BossRetry`、revision 31、已结算四场。

## 构筑与界面证据

- N1 实际完成队列覆盖、清除、重加到队尾及按序执行；N2 前一指令击倒选定目标后，后续指令明确显示“原目标已失效，请重新选敌”，重选后正常施放。截图见 `frames/010-pause-queue-ordered.jpg` 与 `frames/023-queue-target-rejection.jpg`。
- B01、B02、B03、B04 均经本趟普通入口和奖励按钮取得；B04 详情与 N5 四件入场画面分别为 `frames/038-B04-normal-reward-detail.jpg`、`frames/040-N5-four-piece-entry.jpg`。
- [N4/N5 结构化结算分析](replays/n4-n5-settlement-evidence.json) 按实际有效伤害汇总，排除过杀：N5 对敌伤害 21,700，其中原生 10,171、遗物追加 11,529；散射追加 11,177，B04 两次回响追加 217 + 135 = 352。B03 有 34 个根动作命中两个不同敌方实例。数据来自已严格匹配的真实 tape，不由图标亮起推算。
- N4 暂停画面同时包含 1.4 秒群攻预告、两个散射目标各 95 及本场累计 5,990；N7 实际记录了面具 2/2、群攻力度 2.0×，以及清完后的 0/2、1.0×。截图和原片保留；本趟没有首领二阶段的普通画面证据。

完整五战连续录像、首领失败后关闭重开并改变操作获胜、A/C 的相应界面范围、不同实际渲染帧率对照，以及通关后的解锁/新趟仍按验收表保留待办。后续录像另存新文件，不将本段改名或拼接冒充一次连续通关。

## 录制修复的下一步

当前 `gdigrab` 窗口捕获在此 Unity Player 上失效。已确认本机 FFmpeg 8.1.1 提供 `gfxcapture`；[官方文档](https://ffmpeg.org/ffmpeg-filters.html#gfxcapture) 说明其使用 Windows.Graphics.Capture，输出 D3D11 帧，可按窗口标题及程序名定位。该替代方式尚未实际执行，不能先报修复成功。

恢复窗口操作后，先录约 10 秒正常页面切换并抽检前后画面；与新的 Sky 截图一致后，才开始完整远征。候选命令如下，必须使用新的输出文件及可发送 `q` 的终端会话；不得复用或覆盖本次失败原片。

```powershell
ffmpeg -hide_banner -loglevel warning -n -filter_complex 'gfxcapture=window_title=^契灵回响$:window_exe=(?i)^OriginalExpedition[.]exe$:max_framerate=15,hwdownload,format=bgra,fps=15,format=yuv420p' -t 10 -c:v libx264 -preset veryfast -crf 20 '<新的诊断输出文件>.mp4'
```

录屏帧率、实际渲染帧率和模拟 tick 是三种不同的证据；不得相互代替。`gfxcapture` 的窗口选择也应先确认当前只有一个匹配 Player。
