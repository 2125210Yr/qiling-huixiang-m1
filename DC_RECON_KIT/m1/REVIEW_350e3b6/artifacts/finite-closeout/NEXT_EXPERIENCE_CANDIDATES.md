# 有限收口后的体验候选

日期：2026-09-20。只读评估时主仓 HEAD：`38629718`。

本文件记录现有 P0/P1、现有代码与真实游戏截图支持的下一批候选。先完成本轮有限收口与独立 Win64 试玩交付；以下条目不增加本轮验收门槛，也不授权自动开工。此次未修改代码、没有联网搜片，没有启动 G3/M2、养成、抽卡、网络或动画管线切换。

## 参考和当前画面

原始 P0：`F:/天命之子/DC_RECON_KIT/docs/reference/gl-shutdown-pve/aSbBuFD12HY_GL_EN_Gameplay_Part1_202308.mp4`，普通 Global EN 教程/剧情 PVE。

原始 P1：`F:/天命之子/DC_RECON_KIT/docs/reference/gl-shutdown-pve/hgqXY5M9gFk_GL_ND_Robin_Boss.mp4`，Global ND Robin Boss。P1 是特殊模式，本文件只使用可辨的 HUD、技能演出结构，不将模式特有规则外推为普通 PVE 规则。

入库身份和捕获方式见 `F:/天命之子/DC_RECON_KIT/docs/reference/gl-shutdown-pve/FETCH_LOG.txt`。这些素材是低清 MediaRecorder remux，不是高清原片。图像查看工具可能放大显示，不能据放大后的尺寸认定已达到高清门槛。

当前游戏画面来自已完成自然流程：`F:/天命之子/DC_RECON_KIT/m1/REVIEW_350e3b6/artifacts/natural-play/20260920T080750-np_basic_v1/`。本文已实际查看下面引用的图片；没有把截图代替逐帧录像或独立 Win64 运行验收。

## 1. 教程提示与技能演出的遮挡、触发时机

**当前差异：** `np_03_slide.png` 同时出现 “Attack with a Tap Skill!” 提示板、Slide SHOWTIME 和伤害。玩家刚使用 Slide，提示仍要求 Tap；提示板占据演出上方的主要视线区域。候选先处理当前画面可确认的遮挡与提示时机。

- 当前截图：`F:/天命之子/DC_RECON_KIT/m1/REVIEW_350e3b6/artifacts/natural-play/20260920T080750-np_basic_v1/np_03_slide.png`。
- P0 教程提示帧：`F:/天命之子/DC_RECON_KIT/docs/reference/gl-shutdown-pve/_frames_p0_tap330/p0_t352.png`。
- P0 Slide 演出帧：`F:/天命之子/DC_RECON_KIT/docs/reference/gl-shutdown-pve/_frames_timing30/showtime/f_025.png` 和 `f_035.png`。两帧没有当前 Tap 提示板遮挡。该序列从 P0 的 358.95 秒开始以 30 fps 提取，出处为同参考根目录 `_extract_timing30.py`；时序身份记录见 `DC_RECON_KIT/m1/G0/TIMING_DIFF.md`。
- 代码：`F:/天命之子/client/Assets/Scripts/Resonance.App/Battle/BattleHud.cs:1532` 的延迟提示仅检查其他提示与 Fever，没有检查 SHOWTIME/QTE；准备态提示还有 `BattleHud.cs:1247` 入口。`F:/天命之子/client/Assets/Scripts/Resonance.App/Battle/Vfx/VfxTipPlate.cs:199` 将提示置顶。

**限制：** 这些帧证明当前可读性差异，不能据此声称已掌握原作所有教程触发、暂停或互斥规则。若实施，必须保留核心已经确定的停顿和回放语义，不能为了展示方便重新让 HUD 直接控制未记录的模拟停顿。

## 2. SHOWTIME 缺少施法角色聚焦

**当前差异：** P0 中的大幅施法角色、标题与技能名有明确主次；当前画面是整队站立图叠红色和大字，没有施法角色特写，RANK 行还与场上 Hero 文案交叠。候选可复用现有角色图与现有 UI 展示能力，不要求更换动画管线。

- 当前截图：`F:/天命之子/DC_RECON_KIT/m1/REVIEW_350e3b6/artifacts/natural-play/20260920T080750-np_basic_v1/np_03_slide.png`。
- P0 证据：`F:/天命之子/DC_RECON_KIT/docs/reference/gl-shutdown-pve/_frames_timing30/showtime/f_025.png`、`f_035.png`。
- P1 补充：`F:/天命之子/DC_RECON_KIT/docs/reference/gl-shutdown-pve/_frames_p1_robin/t15.png`，同样可见施法角色聚焦；不用于外推普通 PVE 的数值或时间。
- 既有缺口记录：`F:/天命之子/DC_RECON_KIT/m1/G0/CUE_SCORECARD.md:37` 已将“全屏立绘切镜”列为仍缺项。
- 代码：`F:/天命之子/client/Assets/Scripts/Resonance.App/Battle/Vfx/VfxRouter.cs:179` 向 SHOWTIME 传入 null 角色图；`F:/天命之子/client/Assets/Scripts/Resonance.App/Battle/Vfx/VfxShowtime.cs:59` 明确忽略 portrait，当前仅构建文字和色块；`F:/天命之子/client/Assets/Scripts/Resonance.App/Battle/BattleHud.cs:1880` 的 BeginShowtime 只维护展示停顿。

**限制：** 当前证据支持身份结构与画面主次，不能证明角色动作、镜头轨迹或逐帧时序已还原。不得虚构 RANK/LV 内容，也不借此扩大到新动画方案、绑定重做或素材生产项目。

## 3. 顶底 HUD 的主次和弧形结构

**当前差异：** 参考具有连续顶弧血量区、集中的关卡/计时信息和底部大面积绿色弧形量表。当前界面由细直线与分离矩形栏组成，底部 HP、Drive、Fever 很细，信息层级和形态差异明显。候选只做现有字段的结构与可读性整理。

- 当前截图：`F:/天命之子/DC_RECON_KIT/m1/REVIEW_350e3b6/artifacts/natural-play/20260920T080750-np_basic_v1/np_01_battle.png`。
- P0 证据：`F:/天命之子/DC_RECON_KIT/docs/reference/gl-shutdown-pve/_frames_p0_battle2/p0_t500.png`。
- P1 补充：`F:/天命之子/DC_RECON_KIT/docs/reference/gl-shutdown-pve/_frames_p1_robin/t35.png`。
- 代码：`F:/天命之子/client/Assets/Scripts/Resonance.App/Battle/BattleHud.cs:251` 顶栏采用 Pixel 与水平 Filled Image；`BattleHud.cs:376` 底栏同样采用直条。统一构建入口为 `F:/天命之子/client/Assets/Scripts/Resonance.App/Core/GameRoot.cs:1221`，现有 Canvas 参考画幅配置在 `GameRoot.cs:1464`。

**限制：** 低清参考足以观察弧形/直条和信息主次，不能据此给出像素级误差、精确坐标或 T27 通过结论，也不能改变 HP/Drive/Fever 的数据来源与含义。

## 上一轮有限收口与延期边界

可核验的来件复审为 `F:/天命之子/DC_RECON_KIT/m1/REVIEW_a82801b/REVIEW.md` 与同目录 `NEXT_TASK.md`。`REVIEW_350e3b6` 在本次只读检查时没有 `REVIEW.md`，其 `CURRENT_STATE.md` 是修复状态记录，不能冒充新的独立复审意见。

- `REVIEW_a82801b/REVIEW.md:13`：只收验证配置语义、可重放时间线、准确开局记录与运行证据；不重做 G0/G1，不开 G3。
- `REVIEW_a82801b/REVIEW.md:145`：修复后冻结源码；无 filter 全量回归及 basic/fever/auto；新分场经标准回放器读回；保存版本、TRX、退出码、日志、构建标识与连续关键流程录像。哈希文本不等于独立构建验收。
- `REVIEW_a82801b/REVIEW.md:155`：满足目标、精确回放与源码证据对应后结束本批交复审，不无限扩写工单。
- `REVIEW_a82801b/NEXT_TASK.md`：自然 UI 开战后不直接补 HP/充能/Drive/时间/胜负，不强制 Perfect；不扩养成、新模式，不换动画管线。
- `F:/天命之子/DC_RECON_KIT/m1/G1/DEFERRED_SUPPLEMENT.md:30`：路 A 仅用已入库 P0/P1 做身份/结构、切片可跑可录和功能核内部一致性。T27、T28、T14–T20、高清 P0/P2 等为 `DEFERRED_NOT_REMOVED`；暂停外部搜片、模拟器补录与未经授权的参考升格。

三个候选均不代表 PATH_A、M1 或原作还原验收通过，不改变既有延期项、验收权重和 G3/M2 边界。独立试玩交付完成后，结合用户反馈确定下一批范围，再进入实现。
