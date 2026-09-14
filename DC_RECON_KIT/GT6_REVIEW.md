> **SUPERSEDED IN PART** by `m1/G1/DEFERRED_SUPPLEMENT.md` (2026-09-12) and `m1/G2_REVIEW_20260913/NEXT_GOAL.md`
> — GT hunting/stream capture/emulator paused; concurrency cap is environment max;
> see `G2_ENTRY.md`。正文未重写，冲突以新决定为准。

# GT6 审核说明（M1 切片，未验收）

仓库：`2125210Yr/qiling-huixiang-m1`（私有）。本包只含审核用源码与契约，**没有**立绘、录像、纪念版静帧、构建包、`client/Library`。

## 先读这个

M1 **没有验收**。G2 **没有过闸**。还原分 **0**。不要把 `dotnet test` 绿或切片冒烟 PASS 写成原作回归 / T27 / 90%。

| 闸 | 状态 |
|---|---|
| G0 | 审计件已交；主 GT 仍 `CANDIDATE_NEEDS_FETCH` / `BLOCKED`（`docs/reference/gl-shutdown-pve/` 无原始 mp4） |
| G1 | 契约已冻：`m1/G1/CombatContract` `EffectSchema` `ClockPolicy` `FormulaProfiles` `UiStateMap` |
| G2 | 五人核心切片可跑；Drive 选择板 / QTE / Fever / SHOWTIME 已拆开（工程实现，坐标 `NEEDS_REFERENCE`） |
| 公式 | 默认 `GL_UNKNOWN`。禁止 `GL_FINAL_VERIFIED`。JP/KR 仅对照 |

缺件清单：`m1/G0/M1_UNGATE.md`。状态：`m1/G0/STATUS.md`。

## 建议审的路径

- 战斗核：`client/Assets/Scripts/Resonance.Battle/Core/`（`BattleSim` `DamageMath` `EffectOpcodes` `SliceDriveSequence`）
- HUD / cue：`client/Assets/Scripts/Resonance.App/Battle/BattleHud.cs` `UI/BattleCueCopy.cs` `Battle/Vfx/`
- 冒烟（我方切片，不是 GT）：`client/Assets/Scripts/Resonance.App/Debug/VerticalSliceSmokeRuntime.cs`
- 夹具测试：`tools/BattleSim.Tests/`（内部一致性，不是原作回归）

```
dotnet test tools/BattleSim.Tests/BattleSim.Tests.csproj --nologo --verbosity minimal
```

## 明确不在本包

角色立绘与具体技能文案、普通 5 人 PVE 主 GT 录像、Unity 升级、另起通用卡牌工程。
