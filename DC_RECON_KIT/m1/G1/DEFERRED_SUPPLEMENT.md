# 延期补件（签字）

版本：`1.0-G1-DEFER-2026-09-12`。  
状态：`DEFERRED_NOT_REMOVED`。  
签字：用户，2026-09-12。原话：「这些我们暂时待定，我们接着继续推进其他的，合同你写清楚后面补」。

本文件是 G1 冻结合同的**补件**，不是删条款，也不是改门槛。  
机器可读：`DEFERRED_SUPPLEMENT.json`。

## 1. 延期 ≠ 删除

下列条目**仍在 M1 验收分母里**。现在不测、不搜、不升格，但以后必须补。  
禁止事后把它们从权重里拿掉，禁止把「待定」写成「本里程碑不验」。

| 项 | 合同位置 | 现在 | 后面补什么 |
|---|---|---|---|
| T27 叠图 | `04_ACCEPTANCE_AND_TESTS.md` §2 表现 | `BLOCKED` | 同画幅叠加；中位误差 ≤ 屏宽 1%、P95 ≤ 2% |
| T28 时序 | 同上 | `BLOCKED` | 同机位条：输入 / 命中 / 数字 / Drive / Fever ≤ 2 参考帧 |
| T14–T20 数值 | 同上 §2 数值 | `UNKNOWN` | 完整输入 + 观测输出的误差报告；GL 式不得用 KR/JP 顶替 |
| P0 高清原片 | `TARGET.json` `primary_gt` | 根目录只有 ~296×640 remux | 竖屏 ≥720p 原机或原片，放进 `DC_RECON_KIT/docs/reference/gl-shutdown-pve/` **根目录** |
| P2 Eternal Vow | `TARGET.json` `p2_blocked` | 未入库 / 仓库根 768KB 残片 | 真文件进同一根目录；不得用 stub 顶替 |
| 用户投放升格 | `gt_search/21` `23` | contrast / inventory | 只有用户再签字才许把某条升成 primary |
| 模拟器补录 | 1.1 整合包禁令仍在 | 暂停操作私服客户端 | 用户解禁后再谈；不得改 `server_addr` |
| 公开关键词搜 | `gt_search/WATCH.md` | **暂停** | 用户解禁后再搜；本机 YouTube TLS 仍 BLOCKED |

`FormulaProfiles.fidelity_pass_without_gt` **保持 `false`**。  
合同还原分 **保持 0**。  
M1 **未验收**。G2 **未过**。不得报 90%、`GL_FINAL_VERIFIED`。不得开 G3。

## 2. 现在做什么（路 A）

用户选定：**当前工程走路 A**（G2 表现闸）。路 B（合同 90 分）整包延期。

路 A 只许用**已经入库的 P0 / P1**：

- 身份表：文案 / 结构不自相矛盾（SHOWTIME ≠ Fever；PHASE ≠ 击破；`FULL AUTO` 不换成 KR 带的 `AUTO SKILL`）
- 切片可跑、可录（有图形 Editor；禁止 `-nographics` 冒充视觉）
- 功能核内部一致（夹具绿 ≠ 原作回归）

路 A **涨的是身份层**，不是还原分。身份做完也不等于 M1 过。

## 3. 明确不升格（待用户解禁）

这些材料继续只当 contrast / inventory，**不准**写进 primary 根目录、不准当 T27、不准当 GL 公式：

- `dc实际录制视频素材/dc.5人pvp.mp4`（Arena PVP）
- `dc实际录制视频素材/dc.raid.mp4`（Raid）
- `dc实际录制视频素材/dc.wb.mp4`（约 20 人 WB）
- `dc实际录制视频素材/dc普通战斗关卡.mp4`（KR 1.1 WORLD BATTLE 1-1-1 教程，窗录）
- Bilibili `BV18c411W7dT`（KR 剧情/教程 PVE）
- YouTube `mJrT2conPCI` / `Vdf4V693IcU` / `89jpoNqAwa8`（Ragna / KR Raid / WB）
- `单机dc1.1整合包` 与已拉的空编队 prep 录屏
- 仓库根 `docs/reference/gl-shutdown-pve/` 与 KIT 根不是同一条路径

站立几何可以对照 PVP 五人对向，**只作族系备注**。宽度 434–532 仍 <720，不能解锁 T27。

## 4. 解禁条件（后面补的入口）

用户任意一条即可把对应项从「待定」改回「进行中」：

1. 把 **GL、普通 5 人 PVE、竖屏 ≥720p** 的 mp4 放进 `DC_RECON_KIT/docs/reference/gl-shutdown-pve/` 根目录并说一声。
2. 书面指定某一条用户投放升格为 primary（逐条签字，禁止整夹默认升格）。
3. 书面解禁公开搜或模拟器补录。
4. 书面把数值域移出 M1——**必须改合同权重并重签**，不能用本补件偷删 20 分。

解禁前：不重开 YouTube / Archive / Bilibili 关键词洪水，不从登录会话自拉流，不操作 1.1 私服。

## 5. 指针

- 验收合同：`DC_RECON_KIT/04_ACCEPTANCE_AND_TESTS.md` §1、§6
- 目标：`DC_RECON_KIT/m1/G0/TARGET.json` → `deferred_supplement`
- 公式：`FormulaProfiles.md` / `.json`（`fidelity_pass_without_gt` 仍 false）
- 战斗：`CombatContract.md` / `.json`
- 时钟：`ClockPolicy.md` / `.json`（T28 仍在分母）
- 画幅：`UiStateMap.md`（T27 仍 `NEEDS_REFERENCE`）
- 为什么是 0：`DC_RECON_KIT/m1/G0/CUE_SCORECARD.md`
- 硬阻断：`DC_RECON_KIT/m1/G0/BLOCKED.md`、`M1_UNGATE.md`
