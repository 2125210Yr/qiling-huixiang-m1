# 集成者分工锁（2026-09-13 23:10 +08:00）

检测到两个会话同时在写 `client/Assets/Scripts/Resonance.Battle/Core/BattleSim.cs`（重复的 `TryFeverTap`/`EndFever` 已由本会话删掉自己那份，保留 `CanAcceptSkillInput` / `TryFeverTap(int, out CommandReject)` 版本）。为避免继续互相覆盖，按文件划界：

## 会话 B（当前持有共享文件的一方）独占落盘

- `client/Assets/Scripts/Resonance.Battle/Core/BattleSim.cs`
- `client/Assets/Scripts/Resonance.Battle/Core/BattleSim.Commands.cs`（`BattleCommand`/`CommandReject`/`CommandRecord`/`Submit`/`RunHeader`，按 API_CONTRACT §1）
- `client/Assets/Scripts/Resonance.Battle/Core/Definitions.cs`（`EffectDef.Trigger`、`EffectDef.PeriodSec` 按 API_CONTRACT §5）
- `client/Assets/Scripts/Resonance.App/Battle/BattleHud.cs`、`client/Assets/Scripts/Resonance.App/Core/GameRoot.cs`
- 活动 Unity 编辑器与 `.unity/.prefab/.meta`

请会话 B 顺手完成：`TryResolveCombat` 改用 `FormulaResult.Computed`（本会话在 FormulaProfile/DamageMath 里新增 `Computed`/`Evidence`，`Measured` 暂保持旧语义直到 BattleSim 切换）；`GameRoot.StartBattleAt` 去掉 `Deterministic = true`；HUD 读取 `QteRemaining`/`LastDriveTiming`，删掉 `_qteT` 权威。

## 会话 A（本会话）独占落盘

- `tools/BattleSim.Tests/G2Review*.cs`（新红测试，不改旧断言）
- `client/Assets/Scripts/Resonance.Battle/Core/FormulaProfile.cs`、`DamageMath.cs`
- `client/Assets/Scripts/Resonance.Battle/Core/EffectOpcodes.cs`、`EffectCapability.cs`（新）、`Resonance.Battle/Content/Catalog*.cs` 的校验入口
- `client/Assets/Scripts/Resonance.Battle/Core/BattleReplay.cs`（新，只读 `CommandLog`/`Events`）
- `client/Assets/Scripts/Resonance.App/Debug/NaturalPlayRuntime.cs`、`client/Assets/Editor/Smoke/NaturalPlaySmoke.cs`（新）
- `DC_RECON_KIT/m1/G2_REVIEW_20260913/artifacts/**`、`API_CONTRACT.md`、本文件、入口文档修订

任何一方需要越界时，在本文件追加一行说明后再动。

## 追加 23:52 —— 会话 B 自 23:14:40 起无任何写入（37 分钟），BattleSim.cs 处于不可编译状态（`Shield` 已改只读、`Submit`/`BattleCommand` 缺失）。会话 A 接管共享文件（BattleSim.cs、BattleSim.Commands.cs、Definitions.cs、BattleHud.cs、GameRoot.cs），沿用 B 已写的 `CanAcceptSkillInput` / `TryFeverTap(int, out CommandReject)`。B 若恢复，请先读本文件并停止写这些文件。

## 追加 2026-09-14 11:45 —— 批次收口

事后确认："会话 B"其实是同一协调者第一次并行启动的 7 个子代理（启动被中断后仍在运行，与第二次启动的 7 个重叠），所以 `G2Review*` / `FormulaProfile` / `EffectCapability` / `BattleReplay` 各有两份产出互相覆盖；以磁盘现存版本为准，203/203 通过。

集成者在租约外另改：`VfxGoodButton.cs`（金币可被真实指针命中）、`VfxTipPlate.cs`（`Arial.ttf`→`UiFont()`）、`Editor/PuppetBonePreview.cs`（预览画布不再泄漏进 Play）、`NaturalPlayRuntime.cs`（命名空间修正 + 状态驱动战斗段）。所有租约解除；证据见 `artifacts/CURRENT_STATE.md`。
