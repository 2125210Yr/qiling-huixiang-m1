# NEXT_WAVE — G2 / M1 核心战斗切片

当前：`TARGET_FROZEN_GT_PARTIAL` / `CONTRACT_FROZEN` / `M1 IN_PROGRESS` / `G2 IN_PROGRESS_CORE_SLICE`。  
延期补件（用户 2026-09-12）：`DEFERRED_SUPPLEMENT.md` — T27/T28/T14–T20/P0高清/P2 **后面补，不删**。GT 搜 **暂停**。当前只走路 A。  
**未 commit 要求不在本文件。未升 Unity。未开 G3。还原分 0。M1 未验收。**

## 已落地（不要重做）

- SlideCd 与充能分离；受控下充能停、SlideCd 仍走
- Auto ≠ Tap 通道（`dmg.auto` / `ComputeAuto`）
- 未知 opcode → `Failed` + 抛错
- 毒 = on_action / on_hit_taken，禁止每秒 DoT
- 编队容量 = `partyIds.Length`（M1 默认仍 5）
- 可哈希 `BattleEventLog`；默认剖面 `GL_UNKNOWN`
- `dotnet test tools/BattleSim.Tests`：156 通过（夹具自洽 ≠ 原作）
- 竖屏 HUD 多域 + Tap/Slide 分离；布局 `NEEDS_REFERENCE`
- `ICharacterPresentation` 适配现有骨骼；cue 跟事件；伤害不绑动画结束
- PlayMode 切片冒烟 `PASSED_SLICE_SMOKE`（`20260912m` / `20260912n`）— **我方切片，不是 GT**
- 身份层（P0/P1）：SHOWTIME / PHASE / FEVER / WEAKPOINT / FULL AUTO 等，见 `CUE_SCORECARD.md`

## 先做（路 A，现有 P0/P1）

1. 身份表剩余自相矛盾：场上 Fever 条幅（缺片则不动）、孤立 tap（无 GT 不发明）、WEAKPOINT 是否只盖一张脸（未测不猜）。PAUSE 工程长文已去掉，`20260913b` 已目视。
2. 切片可跑：`20260913c` 已含契灵/编队/详情/LEVEL UP/再战开战。T26 合同对照仍 BLOCKED。禁止 `-nographics` 冒充视觉。
3. 公式只挂 `JP_LEGACY_EMPIRICAL` / `KR_LEGACY_REPORTED` 对照。禁止 `GL_FINAL_VERIFIED`。
4. `fidelity_pass_without_gt` 保持 false。

## 待定、后面补（不从本波「先做」里删，也不现在做）

1. P0 ≥720p / P2 入库。
2. T27 叠图、T28 同机位条、T14–T20 GL 数值。
3. 用户投放升格、模拟器补录、公开关键词搜。
4. `mJrT2conPCI` 仅 Ragna；`Vdf4V693IcU` 仅 KR Raid；`89jpoNqAwa8` 仅 WB。contrast 不进 ROOT。

## 不做

不升级 Unity/URP。不另起通用卡牌项目。不把 Ragna/Raid/WB/KR 教程/PVP 当普通战斗唯一真值。不发明 GL 终式。不自动开 G3。不伪 T27/T28/90%/M1。
