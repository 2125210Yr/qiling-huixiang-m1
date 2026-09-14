# G2 批次保留的延期项（未改范围）

来源：`DC_RECON_KIT/m1/G1/DEFERRED_SUPPLEMENT.md`（`1.0-G1-DEFER-2026-09-12`，用户签字）。  
状态仍是 **`DEFERRED_NOT_REMOVED`**：下列条目**仍在最终验收分母里**，范围与门槛**未改**。本批路 A 工程收敛不测、不搜、不升格、不从权重删除，也不把「待定」写成「本里程碑不验」。

`FormulaProfiles.fidelity_pass_without_gt` **保持 `false`**。合同还原分 **保持 0**。M1 **未验收**。G2 **未过**。不得报 90%、`GL_FINAL_VERIFIED`。不得开 G3。

下列表格与暂停句从补件**按范围原文抄录**。

## T27 / T28 / T14–T20（合同分母，未改）

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

## 已暂停：公开搜片 / 登录会话拉流 / 模拟器补录（未改）

解禁前：不重开 YouTube / Archive / Bilibili 关键词洪水，不从登录会话自拉流，不操作 1.1 私服。

用户任意一条即可把对应项从「待定」改回「进行中」：

1. 把 **GL、普通 5 人 PVE、竖屏 ≥720p** 的 mp4 放进 `DC_RECON_KIT/docs/reference/gl-shutdown-pve/` 根目录并说一声。
2. 书面指定某一条用户投放升格为 primary（逐条签字，禁止整夹默认升格）。
3. 书面解禁公开搜或模拟器补录。
4. 书面把数值域移出 M1——**必须改合同权重并重签**，不能用本补件偷删 20 分。

指针仍以补件 §5 为准。本文件只记账，不改合同。
