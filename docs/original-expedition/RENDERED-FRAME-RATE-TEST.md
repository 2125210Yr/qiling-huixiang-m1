# O-023：真实渲染帧率对照夹具

这是**受控 Unity Play Mode 渲染测试**，不是普通 Player 试玩，也不是录屏 FPS 检查。2026-09-21 首次实际执行通过，覆盖 O-023 的不同实际画面帧率子项；完整记录见下方“已执行证据”。

入口：`Resonance.EditorTests.OriginalRenderedFrameRateTests.SameFrozenBossAndTickZeroCommandsMatchAtTwoActualRenderedFrameRates`。代码位于 `client/Assets/Tests/Editor/OriginalRenderedFrameRateTests.cs`，复用既有 EditorTests asmdef，不修改生产代码、Packages 或发布包。

## 隔离与执行前提

- 必须等当前普通 Player 录像结束、桌面操作得到恢复确认后，再执行本测试。测试准备阶段不得关闭或操作正在使用的 Player。
- 使用**可见、保持前台的 Game View**；不能使用 `-batchmode` 或 `-nographics`。当前 `evidence/run-unity.ps1` 固定为 batch mode，不适用。
- 不允许已有 Play Mode 会话；禁止 `--legacy`，并要求 `Time.captureFramerate == 0`、`Time.timeScale == 1`。
- 默认整套 EditMode 测试不会执行这个场景：未设置专用输出环境变量时，该用例明确忽略。
- 测试在 `EnterPlayMode` **之前**设置进程级 `RESONANCE_ORIGINAL_PROFILE`。这是 `GameRoot.InitializeOriginalExpedition` 真实读取的现有入口，能跨越域重载；默认场景的 `AutoBoot/Awake` 因而读取 Temp 档案，而非用户档案。
- Temp 使用唯一 GUID 目录，创建一个明确标为夹具的 N6 记录，然后由 `ExpeditionFlow.BeginBattle()` 冻结 N7 检查点。两档运行分别读取其字节相同的副本；四场前序历史是夹具，不能计入普通远征证明。
- 只允许新建、尚不存在的输出目录，拒绝链接路径。所有 Temp 档案和证据保留，不递归删除。

## 单独运行

以下命令**只供获准恢复桌面操作之后执行**。应先确认该 Unity 项目没有被另一个 Editor 占用。不要把示例路径复用于第二次运行。

```powershell
$evidencePath = 'F:\天命之子\docs\original-expedition\evidence\o5\rendered-fps-UNIQUE-RUN'
foreach ($path in @($evidencePath, ($evidencePath + '.xml'), ($evidencePath + '.log'))) {
    if (Test-Path -LiteralPath $path) { throw '请使用新的证据目录和结果文件路径' }
}
$giCachePath = Join-Path ([IO.Path]::GetTempPath()) ('original-rendered-fps-gi-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $giCachePath | Out-Null
$previousSetting = [Environment]::GetEnvironmentVariable('RESONANCE_ORIGINAL_RENDERED_FPS_DIRECTORY', 'Process')
try {
    [Environment]::SetEnvironmentVariable('RESONANCE_ORIGINAL_RENDERED_FPS_DIRECTORY', $evidencePath, 'Process')
    & 'D:\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe' `
        -projectPath 'F:\Resonance\client' `
        -giCustomCacheLocation $giCachePath `
        -runTests -testPlatform EditMode `
        -testFilter 'Resonance.EditorTests.OriginalRenderedFrameRateTests.SameFrozenBossAndTickZeroCommandsMatchAtTwoActualRenderedFrameRates' `
        -testResults ($evidencePath + '.xml') `
        -logFile ($evidencePath + '.log')
} finally {
    [Environment]::SetEnvironmentVariable('RESONANCE_ORIGINAL_RENDERED_FPS_DIRECTORY', $previousSetting, 'Process')
}
```

测试自身打开并聚焦 Game View，进入 Play Mode，结束后退出 Play Mode，恢复原来的帧率、VSync、timeScale、Original profile 环境变量和窗口焦点。若原来没有 Game View，则关闭测试创建的窗口。断言失败也走 UnityTearDown 清理；强制杀死 Editor 进程不属于正常测试生命周期，不能承诺执行托管清理，不过该进程的环境变量不会污染另一个正在运行的 Player。

## 实际测量和断言

每档先预热三秒，再通过现有 HUD 的按钮点击回调恢复同一冻结开局。在逻辑 tick 0 依次集火面具 1、设置 2×、暂停；真实渲染暂停两秒后恢复，恢复指令仍在 tick 0。此后由原版 `GameRoot.Update` 使用真实 `Time.deltaTime` 自然推进到战斗终局。测试不直接调用 `Tick()`，不修改战中 HP/充能/结果，不改变时间步长或使用 `Camera.Render()`。

两档目标为 30 和 120 FPS、`vSyncCount=0`。通过 URP `RenderPipelineManager.endCameraRendering` 记录真实游戏相机的回调，按 Unity 帧号去重；使用 `Stopwatch` 测量间隔，并记录 `Time.renderedFrameCount`。这测量的是 Unity 实际游戏画面渲染节奏，不是视频容器 FPS，也不等同于显示器物理刷新率。

通过条件包括：

1. 每档至少 100 个实际渲染样本；实际中位 FPS 高于 8。高档实际中位 FPS 至少为低档的 1.5 倍，实际平均 FPS 至少为 1.35 倍。仅设置 `targetFrameRate` 不算证据。
2. 两段冻结输入哈希完全相同，四条指令全部在 tick 0 被接受；指令日志、输入日志、终局 tick、事件哈希、伤害事件及其 tick、结算哈希和最终状态哈希严格相等。
3. 至少 100 个共同逻辑 tick、其中至少 15 个蓄势 tick；这些 tick 的权威意图快照哈希、HUD 标题和倒计时文字严格一致。每个实际渲染样本同时验证倒计时文字对应当时的权威剩余时间。
4. 两秒真实渲染暂停期间，完整战斗快照哈希保持不变。
5. 每档有首次预告、结算前一秒、第一次群攻后三张真实 Game View 截图。截图元数据记录**请求捕获**的帧/tick；截图 API 异步完成，不把该值伪称为精确像素采样帧。

若 Editor 被遮挡或失焦降速，两档实际 FPS 不分离、没有真实渲染回调、输出不完整、或同 tick 数据不同，测试会失败，不用目标 FPS 或容差补成通过。测试主协程使用正常帧让步；渲染回调停止超过八秒会报错。

## 产物与边界

- 根目录：`source-identity.json`（内容哈希、实际程序集及关键源码 SHA256），全部比较成功后才生成 `comparison.json`。
- `30/`、`120/`：逐渲染帧 JSON、帧间隔 CSV、三张 PNG、完整 v2 回放。单档失败保留已有帧数据并标记 `INCOMPLETE`；单档通过也只标记 `PASS_SINGLE_RENDER_RUN`。
- `comparison.json` 的 `PASS` 才表示本项两档实际渲染对照通过；缺失该文件不能判通过。
- 它补齐 O-023 的不同画面帧率子项及这次受控暂停；1×/2×逻辑关系和政策停顿仍引用已有专门证据，不冒称本测试覆盖它们。

## 已执行证据

2026-09-21 07:02–07:03 UTC，在可见并激活的 Unity 6000.3.23f1 Game View 中首次运行；3840×2160、DX12、独立 GI 缓存。未使用 batch mode，无夹具修改或重试。测试执行前确认无 Player、Unity、ffmpeg；测试完成后 Editor 自行正常退出，退出码 0，XML 为 1/1 Passed。

证据目录：[rendered-fps-20260921-37245ba5](evidence/o5/rendered-fps-20260921-37245ba5/)。[comparison.json](evidence/o5/rendered-fps-20260921-37245ba5/rendered/comparison.json)、[test-results.xml](evidence/o5/rendered-fps-20260921-37245ba5/test-results.xml)、[运行摘要](evidence/o5/rendered-fps-20260921-37245ba5/RUN-SUMMARY.md)保留原始结果。

| 目标 FPS | 实测中位 FPS | 实测平均 FPS | 真实相机帧 | 暂停帧 |
| --- | ---: | ---: | ---: | ---: |
| 30 | 29.9938 | 29.6646 | 874 | 61 |
| 120 | 116.8238 | 118.1572 | 3482 | 239 |

808 个共同逻辑 tick（其中 230 个蓄势 tick）的意图和 HUD 倒计时严格相等。129 条伤害事件及其 tick、全部指令、事件、结算和最终状态哈希一致；两档均在 tick 825 自然败北。各三张截图完整，人工查看了两档实时 Game View 和代表性截图。

个人 `OriginalExpedition` 目录执行前后均为 12 个文件，路径、大小、SHA256 全部相同；[postflight.json](evidence/o5/rendered-fps-20260921-37245ba5/postflight.json)还记录结束后无相关进程。测试框架另按 Unity 默认行为写入用户持久化目录根部的 `TestResults.xml`，不是游戏档案；本次 CLI 指定的独立结果文件也已生成。没有改动生产代码或个人存档。

API 依据：[targetFrameRate](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/application/targetframerate)、[URP endCameraRendering](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Rendering.RenderPipelineManager-endCameraRendering.html)。
