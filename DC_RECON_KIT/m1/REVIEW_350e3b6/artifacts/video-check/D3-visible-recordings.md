# D3 可见录像与同场回放

2026-09-20，用户明确允许显示 Unity 后执行。每次只运行一个 Editor，使用已有 EventSystem 自然操作场景。录像不是 OS 触控实测，也不是原作还原度认证。

| 场景 | 归档（相对于 `../natural-play/`） | 视频对应首场 | 新读回器结果 |
|---|---|---|---|
| basic，含实际点敌 | `20260920T062640-np_basic_v1` | `np-20260920T062700-001` | Match=True，155/155 事件，全部差异 0 |
| Fever，最终重录 | `20260920T071823-np_fever_v1` | `np-20260920T071843-001` | Match=True，1141/1141 事件，全部差异 0 |
| Auto | `20260920T064643-np_auto_v1` | `np-20260920T064701-001` | Match=True，69/69 事件，全部差异 0 |

三份片段已验收：basic **10.9 秒 / 303 帧**，Fever **110.766667 秒 / 3025 帧**，Auto **5.5 秒 / 158 帧**。每份均实际查看端点与关键动作样本，全片解码退出 0、错误日志为空；Fever / Auto 还逐帧核对了输出时间戳与所选原帧一致。Fever 末帧仍显示游戏首页，但 Editor 停止控制已变化；不声称它仍处于 Play，后一帧清空 Game view 已被排除。

每个归档内的 `gameplay-proof.mp4` 是交付片段，`proof.json` 记录原视频 SHA、精确 PTS 范围、派生过程、媒体元数据、视觉抽样与全片解码证据。`run.json` 绑定 session / battle IDs / 时间 / 捕获区域；`natural-play.result.txt`、`battles/<battle_id>/` 是该场原始游戏记录。

对应读回原件分别为 `../readback/visible-basic-final.txt`、`visible-fever-new.txt`、`visible-auto-final.txt`，汇总及各原件 SHA 在 `visible-final-summary.json` 和 `visible-fever-new-summary.json`。三份 replay 共 168 / 1159 / 102 行，JSON 解析均无错误。

basic 显示 FIGHT、点敌后的技能战斗、CLEAR、NEXT 后暂停及返回首页；Auto 显示 MANUAL → SEMI AUTO → FULL AUTO → MANUAL 与退出；Fever 的关键操作为三次 QTE Perfect、两个 slot 的 FeverTap，最终胜利并返回首页。场景操作事实由同场命令/事件与截图交叉核对，不把视频抽样当作逐帧全片目视检查。

basic / Auto 来自 `40ab2fb` 游戏源码；最终 Fever 的 `BattleReplay.cs` 已加入本轮终局修复，三个受测源文件哈希在各自 `source-hashes.json`。新读回器已对 basic、Auto 与修复前失败的 Fever 原件复验；既有录像不因只有回放执行器改动而重复采集。最终读回器 SHA-256 为 `77D38EFC3023D53614B007D5B47AF106E8FC6E85654D7FF225511B9C297B0348`，`BattleReplay.cs` 为 `96A476683FBA85898CAC5F4F7CB81D346D54B7E6E1BA4B16D8136F5B073FDBC6`。

`20260920T064419` 的 Fever 场景虽 PASS，但原回放缺 `fever_end/TimeUp`，录像后段有其他应用遮挡。失败原件保留；修复后旧 tape 879/879 事件匹配，仍用最后的置顶窗口重录承担 Fever 视频验收，不能将旧失败改写成当时已通过。

原始 desktop 录屏与视觉抽样含非游戏画面的部分仅留本机，未纳入 Git；没有覆盖、伪造或拼接不同场次。交付片段连续，保留完整 Unity 窗口画幅及实际帧时间间隔。不同场景的片段不合并成一条伪称连续运行的视频。

验收边界为 D1–D3 首场操作、录像与回放对应。NEXT 后第二场已有的成长 HP 舍入差异继续单列在 `../../CURRENT_STATE.md`，本轮没有修复它，也未宣布整个 PATH_A 或原作还原验收完成。
