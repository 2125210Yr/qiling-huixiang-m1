# G2 当前入口（2026-09-13）

短入口。旧合同与 `GT6_REVIEW.md` / `STANDING_ORDERS.md` 正文未重写；冲突处以本页与下列新决定为准。

## 先读

1. `m1/G1/DEFERRED_SUPPLEMENT.md`（2026-09-12 签字：延期 ≠ 删除）
2. `m1/G2_REVIEW_20260913/AUDIT.md`、`NEXT_GOAL.md`、`REGRESSION_MATRIX.md`
3. `m1/G2_REVIEW_20260913/API_CONTRACT.md`、`INTEGRATOR_LOCK.md`
4. `m1/G2_REVIEW_20260913/artifacts/CURRENT_STATE.md`、`artifacts/deferred.md`

`GOAL_LOCK.md` 的阶段钉（G0/G1 已过、G3 未开）仍有效；其中「继续搜片 / 补齐 primary GT」已被补件暂停覆盖。

## 本批范围

分支 `m1-gt6-review`，HEAD `ed7a9ba6423f79aee9a4276f9a502c4e6d9cb6f3`（== 审查快照）。Unity `6000.3.23f1`。  
**G2 路 A 工程收敛**：真实输入、Fever、QTE/时钟、种子随机、效果能力与寿命、计算/证据分离、自然游玩测试。不重做 G0/G1，不升级 Unity，不开 G3/M2。  
工程批次用 `PATH_A_ENGINEERING_PASS` / `NEEDS_FIX` / `BLOCKED`；与 M1 还原验收分开。当前骨架判定 **NEEDS_FIX**。

`M1_FIDELITY=DEFERRED_NOT_REMOVED`  
`G3_NOT_STARTED`

共享 `BattleSim.cs` / `BattleHud.cs` / `GameRoot.cs` 只由集成者落盘。已有两协调会话，以 `INTEGRATOR_LOCK.md` 划界。

## 暂停（未从分母删除）

T27 叠图、T28 时序、T14–T20 数值，以及公开关键词搜片、登录会话拉流、私服/模拟器补录：**暂停，仍在最终分母**。详见 `artifacts/deferred.md`。已入库 P0/P1 可走路 A 身份/结构；不把 contrast 升格为 primary。

## 并发

旧工单「最多四个代理」**已废**。按**当前环境允许的最大并行**调度就绪且互不覆盖的任务；`codex_templates/config.fragment.toml` 里的 `4` 只是模板样例，不是有效上限。不伪造「无限」。不同时开第二个会改同一工作区的主协调者。

## 两层测试（勿混）

| 层 | 入口 | 允许 | 不能代替 |
|---|---|---|---|
| 表现冒烟 | `VerticalSliceSmokeRuntime` | 显式状态注入，证明能展示 | 自然流程验收 |
| 自然游玩 | `NaturalPlayRuntime`（创建中） | 开战只走真实 UI/指针；必经缺失则 FAIL | 不得跳过后整体 PASS |

核心 `BattleSim.Tests` 现有 156 个 `[Fact]`；未在本入口重跑，不得写成已通过。未执行写 `NOT_RUN`，不写 PASS。
