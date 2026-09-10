# NEXT_WAVE — G2 / M1 核心战斗切片

当前：`TARGET_FROZEN_GT_SEARCHING` / `CONTRACT_FROZEN` / `M1 IN_PROGRESS` / `G2 IN_PROGRESS_CORE_SLICE`。  
本波已 REPAIR 战斗核与表现适配，**未 commit，未升 Unity，未开 G3**。

## 已落地（不要重做）

- SlideCd 与充能分离；受控下充能停、SlideCd 仍走
- Auto ≠ Tap 通道（`dmg.auto` / `ComputeAuto`）
- 未知 opcode → `Failed` + 抛错
- 毒 = on_action / on_hit_taken，禁止每秒 DoT
- 编队容量 = `partyIds.Length`（M1 默认仍 5）
- 可哈希 `BattleEventLog`；默认剖面 `GL_UNKNOWN`
- `dotnet test tools/BattleSim.Tests`：103 通过（已去掉 catalog.json 写盘）
- 竖屏 HUD 多域 + Tap/Slide 分离；布局 `NEEDS_REFERENCE`
- `ICharacterPresentation` 适配现有骨骼；cue 跟事件；伤害不绑动画结束

## 先做

1. 入库 primary GT。网络通后重拉 `-SUrcOZav_c` 到 `docs/reference/gl-shutdown-pve/`。2019 英文 5 人只标 `cross_era_gl`。
2. `mJrT2conPCI` 仅 Ragna；`Vdf4V693IcU` 仅 KR Raid；`89jpoNqAwa8` 仅 WB。
3. 公式只挂 `JP_LEGACY_EMPIRICAL` / `KR_LEGACY_REPORTED` 对照。禁止 `GL_FINAL_VERIFIED`。
4. PlayMode 现为 `NOT_RUN`。有活动 Editor 再跑，禁止为测污染 Library。

## 不做

不升级 Unity/URP。不另起通用卡牌项目。不把 Ragna/Raid/WB 当普通战斗唯一真值。不发明 GL 终式。不自动开 G3。不伪 T27。

## 切片验收（有 GT 之后）

Auto / Tap / Slide / Drive+QTE / Fever / 控制·护盾·换目标 / 死亡·结算 / 暂停·加速·自动。未知数值显式保留。无 GT 精确分支不得用临时值换 fidelity pass。
