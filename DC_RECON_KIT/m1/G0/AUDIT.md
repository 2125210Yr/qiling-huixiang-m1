# G0 AUDIT — 工程基线（本波不重扫全仓）

日期：2026-09-09。范围：只读落盘。未改 Unity。未 commit。未宣称 PlayMode 通过。

## 后续闸门（2026-09-12 对照，不重扫）

下表覆盖本文 §1–§2 已过时的句子。细节以 `STATUS.md` 为准。

| 2026-09-09 原句 | 现在 |
|---|---|
| PlayMode `NOT_RUN` | 有图形切片冒烟 **PASS**（胜/负+半自动）。我方切片，不是 GT |
| SlideCd **无** | 独立时钟已实现；秒数仍 `UNKNOWN` |
| 编队 **写死 5** | 容量不写死；默认展示仍 5 |
| 毒 **未执行** | on_action / on_hit_taken；`GL_UNKNOWN` 不改 HP |
| 未知 opcode **静默** | 失败关闭 |
| primary GT 未入库 | P0+P1 在 `DC_RECON_KIT/docs/reference/gl-shutdown-pve/`；P2 bot-gate |
| 不得报还原通过 | **仍成立**。合同分 0。无 `GL_FINAL_VERIFIED` |

本文件写入已确认 TARGET 与既有工程要点，**不**对本仓做第二次全量审计。路径级结论以下列快照为准；细节符号未在本波重核。

## 1. 工程快照（给定，未复跑）

| 项 | 值 | 证据级 |
|---|---|---|
| Unity 工程根 | `client/` | 给定 |
| Unity | `6000.3.23f1` | 给定 |
| RP | URP | 给定 |
| 战斗核 | BattleSim | 给定 |
| 时钟 | 30Hz 固定 tick | 给定 |
| 回合制？ | **否**（非回合制） | 给定 |
| PlayMode | `NOT_RUN` | 本波 |
| 编译/启动基线 | 未采集 | 本波 |

结构判断：BattleSim 已是实时 tick 核，不是回合制卡牌核。分类 **REUSE 核 + REPAIR 缺口**。**禁止 REPLACE 整核。**

## 2. 模块决策

| 模块 | 决策 | 现状 | 最小修复 | 测试 |
|---|---|---|---|---|
| BattleSim 时钟/调度 | REUSE | 30Hz 非回合制 | 保持 tick 核；快进走 ClockPolicy，不盲改全局 `Time.timeScale` | 逻辑 tick 单测；PlayMode 未跑 |
| SlideCd | REPAIR | **无 SlideCd** | 独立 Slide 冷却时钟，与充能分离 [S14] | 睡眠/受控下充能 vs SlideCd 分测 |
| 编队人数 | REPAIR | **写死 5** | 底层可变人数/前后排；M1 展示仍为 5 人 PVE | 5 人切片 + 人数≠5 的数据结构测 |
| 毒 | REPAIR | **未执行** | 行动/受击触发 [S19]；禁止每秒 DoT 冒充 | opcode 执行红测 |
| 未知 opcode | REPAIR | **静默** | 必须报错/失败，禁止吞掉 | 未知码失败测 |
| 公式入口 | ISOLATE | 不得默认一套“原版公式” | 显式 profile；GL=`UNKNOWN`；对照仅 JP/KR legacy | 禁 `GL_FINAL_VERIFIED` |
| 整核 | **勿 REPLACE** | 核假设（实时 30Hz）与目标同类 | 只补时钟/效果/编队缺口 | — |

未在本波重核的 UI/表现/养成：保持未分类，不升格为 REUSE/REPLACE。

## 3. 目标冻结（用户已确认）

- 服：GL 国际服。
- 时期：停服前后期，**含 Ignition**。
- 设备：手机竖屏。
- M1 模式：普通 5 人 PVE。
- primary GT：`SEARCH_IN_PROGRESS`。
- 旧录像：`mJrT2conPCI`=GL Ragna **补充**；`Vdf4V693IcU`=KR Raid **差异**；`89jpoNqAwa8`=WB **差异**。三者都不是普通战斗唯一真值。
- 公式：只允许 `JP_LEGACY_EMPIRICAL` / `KR_LEGACY_REPORTED` 作对照。GL 数值 `UNKNOWN`。不得发明 `GL_FINAL_VERIFIED`。

## 4. 不像原作的已知缺口（实现侧）

1. 无 SlideCd → Slide 与 Tap/充能会糊成同一进度。
2. 编队写死 5 → 后续 WB/支援结构会被五人假设污染。
3. 毒未执行 → 控制/DoT 语义缺席。
4. 未知 opcode 静默 → 技能表缺口被当成“已结算”。
5. 无 primary GT + PlayMode 未跑 → 不可报还原分。

## 5. 本波不做

不改 Unity。不升级编辑器/URP。不另起通用卡牌项目。不把三份旧录像当普通战斗 GT。不把 JP/KR 候选写成 GL 终式。
