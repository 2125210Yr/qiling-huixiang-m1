# O-023 受控实际渲染帧率对照：PASS

2026-09-21 首次执行通过，未修改夹具、未重试。测试为 Unity EditMode 入口进入真实 PlayMode，原版 GameRoot.Update 使用真实 Time.deltaTime 推进；不属于普通 Player 试玩或视频 FPS 检查。

- Unity 6000.3.23f1，DX12，3840×2160 可见且激活的 Game View，进程 37228；全新 GI 缓存路径在 launch-config.json 中。
- test-results.xml：1/1 Passed，failed=0，skipped=0；process-exit.json：退出码 0。Editor 在 07:03:18 UTC 自行正常退出，无强制终止。
- 30 档实际中位 29.993821 FPS、平均 29.664588 FPS；874 个真实相机帧，含 61 个暂停帧。
- 120 档实际中位 116.823795 FPS、平均 118.157218 FPS；3482 个真实相机帧，含 239 个暂停帧。
- 808 个共同逻辑 tick，含 230 个蓄势 tick：权威意图和 HUD 标题、倒计时严格相同。
- 四条接受的指令都在 tick 0：FocusEnemy、SetSpeed、Pause、Resume；所有指令与输入日志严格一致。
- 129 条伤害事件（含事件 tick）、全部事件、结算和最终状态哈希完全一致。两档自然败北终局 tick 均为 825。
- 两档各三个完整 PNG；截图元数据表示异步截图的请求帧/tick，不声称精确像素采样帧。已观察两档实时 Game View，并查看 30 档结算前一秒及 120 档群攻后截图。

## 存档和进程隔离

启动前通过进程枚举确认 Player、Unity、ffmpeg 均已结束。测试在 EnterPlayMode 之前使用现有 RESONANCE_ORIGINAL_PROFILE 入口切换到唯一 Temp 夹具；两档分别读取相同冻结检查点的独立副本。先前四场历史是受控夹具，不能用于普通流程通关证明。

personal-profile-before.json 与 personal-profile-after.json 中，个人 OriginalExpedition 目录的 12 个文件路径、大小、SHA256 全部相同。主档 SHA256：

`05CD4B68967181BC852CD3D5CDE85E8765A86D70399D8D23D96CB151BAB3CC19`

postflight.json 记录 unchanged=true，且结束后无 Unity、Player、ffmpeg。Unity 测试框架另写入用户持久化目录根部的 TestResults.xml；该文件位于 OriginalExpedition 游戏档案目录之外。没有编辑生产代码或个人游戏档案，没有删除文件，没有提交。

## 精确标识与范围

完整哈希见 rendered/comparison.json；实际加载 App/Battle 程序集与关键源码 SHA256 见 rendered/source-identity.json。

- 内容哈希：`69aaa51d7fa5077a5e1d30b4c5f2fd042b62f932f86d0dec59a037762880fdbd`
- 测试源码 SHA256：`65aa54f9d78ae6802791adf07a0f688d607d79784955bac5299621f5bc95d03a`
- 输入哈希：`d5e056caa2c082d6648354594da253477cd86b3ea8f6cd5cc04543fa3e689bf0`
- 最终状态哈希：`5e473d203639e2464a5f9af49d03b0caef4f299fc73974936f01c2a0722482c3`

这是一个固定种子、固定冻结首领开局、tick 0 指令时间线的两档实际渲染比较；不扩张为全部设备/种子或物理显示刷新率保证。覆盖 O-023 不同实际画面帧率与受控暂停子项，未独立覆盖 1×/2×政策停顿关系。Editor 中显示的旧开发诊断横幅不代表这个受控测试的存档来源，来源以隔离检查和源码/程序集标识为准。
