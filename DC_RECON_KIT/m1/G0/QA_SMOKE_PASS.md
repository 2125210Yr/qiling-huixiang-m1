# M1-G2-QA-SMOKE-PASS — 独立切片冒烟复核

- 任务：`M1-G2-QA-SMOKE-PASS`
- 角色：`dc_qa`（只读生产代码；本文件为 G0 QA 报告）
- 日期：2026-09-10
- 对照：`STATUS.md`、`QA_DRIVE.md`、`BASELINE_LOGS/`、`docs/reference/gl-shutdown-pve/our_slice/`、`T_MATRIX.md`、`M1_UNGATE.md`、`04_ACCEPTANCE_AND_TESTS.md` T26–T28
- **未改** 生产代码、`TARGET.json`、未 spawn、**未开 Unity Editor**、未开 G3
- **未改** 用户 LocalLow `save.json` / `save.json.bak`（只读核对）
- **不采信** 实现代理自述 / `STATUS.md`「已 PASS」= M1 过 / `QA_DRIVE.md` 旧 FAIL 句当成本轮否决

## 总判（必须先读）

**切片冒烟结果文件本轮为 PASS。Drive / Fever 只在日志成立，没有画面。M1 不可验收。G2 未过。不得报 90%。还原分仍为 0。**

不得把本轮切片 PASS 升格为 T26 / T27 / T28 或 M1 验收。

四问：

| # | 问题 | 判定 |
|---|---|---|
| 1 | 结果文件是否真是 PASS、时间戳是否本轮 | **是。** 归档第一行 `PASS`；mtime `2026-09-10 17:20:24`；Editor 日志同文。`client/Temp/` 原件已不在。 |
| 2 | Drive / Fever 是日志成立还是有画面 | **仅日志。** 无 QTE / Fever / 结算 png。开战图 Drive≈0%。 |
| 3 | 是否写入用户 LocalLow `save.json` | **是。** 冒烟路径写过 `…\Resonance\契灵回响\save.json`。本任务只报告，未改档。 |
| 4 | 可否升格 T26 / T27 / T28 / M1 | **否。** 切片 PASS ≠ 全链 / 叠图 / 逐帧 / 验收。 |

`QA_DRIVE.md`（`M1-G2-QA-DRIVE`）仍写归档 `FAIL drive never fired`、并禁止报 PlayMode PASS。那是修 Drive 之后、**重跑之前**的独立复核，已被 `M1-G2-RESMOKE` 产物覆盖。本文件以本轮结果文件为准，不沿用那份 PlayMode 否决，也不把新 PASS 写成验收。

---

## 1. 结果文件是否真是 PASS、时间戳是否本轮

### 1.1 原路径已空

`client/Temp/vs-smoke.result.txt` **不存在**（`client/Temp` 目录也不在）。`STATUS.md` / `RESULT.md` 写的「已读 `client/Temp/…`」是当时现场；本任务核的是归档副本。

### 1.2 本轮归档（两份同文）

| 路径 | bytes | mtime | SHA256 |
|---|---|---|---|
| `DC_RECON_KIT/m1/G0/BASELINE_LOGS/vs-smoke.result.txt` | 3150 | **2026-09-10 17:20:24** | `AD8BDDDD1A990A392F2DFC318FA433BF99EC6A0D1D71996A9AF0C1AD5A7D113F` |
| `docs/reference/gl-shutdown-pve/our_slice/vs-smoke.result.txt` | 3150 | **2026-09-10 17:20:24** | 同上 |

第一行：`PASS`。  
`tap=True slide=True drive=True fever=True`  
`result=胜利` / `cleared=True`  
`screens=Boot,Home,Characters,Team,Inspect,Stage,Battle,Result`

`LAUNCH.txt` / `RESULT.md` 记录的结果 mtime 与磁盘一致（`17:20:24`）。截图 mtime `17:20:16`–`17:20:19`，早于结果文件数秒，同一轮。

Editor 成功这次：`BASELINE_LOGS/editor-playmode-20260910-resmoke-c.log`

- 启动：`2026-09-10T09:09:28Z`（UTC）= 本地约 17:09
- `BatchMode: 0`，Unity `6000.3.23f1`，pid **13000**
- `[VS-SMOKE] entering play mode`
- 随后整段 `[VS-SMOKE]` + `PASS` + Drive/Fever 日志，与结果文件同文
- `WriteResult` 栈：`VerticalSliceSmokeRuntime.cs:322`

失败启动日志 `…-resmoke.log` / `…-resmoke-b.log`（16:26 / 16:43）**不是**本轮判定。

### 1.3 不是本轮的旧 PASS

`client/Builds/Win64/Temp/vs-smoke.result.txt`：mtime **2026-08-28 19:53:38**，1231 bytes，另一哈希。`save=` 指向旧产品目录 `LocalLow\Resonance\Resonance\save.json`。Drive 日志形态也不同（无 `drive qte slot=`）。**不是** `M1-G2-RESMOKE`。

### 1.4 判定

结果文件**真是 PASS**。时间戳**是本轮**（2026-09-10 17:20）。启动成功 ≠ 验收。有图形（非 `-nographics`）只证明这次冒烟不是无头伪绿。

---

## 2. Drive / Fever：日志成立，没有画面

### 2.1 日志（成立）

归档结果与 `resmoke-c.log` 同序：

```
log:tap 0
log:slide 1
log:drive qte slot=0
log:drive perfect fever=60 active=False
log:drive qte slot=0
log:drive perfect fever=0 active=True
log:result 胜利
log:phase Result
```

这是冒烟自己的 `HoldForDrive` + `ReadyCharges` / `TryFillDrive` / `FireDrivePerfect` 灌条，不是玩家手打 QTE。Fever 第二条 Perfect 后 `active=True`。

### 2.2 画面（不成立）

`captures=` 只有：

`01_home.png, ui_home.png, 02_roster.png, 02_characters.png, ui_roster.png, 04_team.png, ui_team.png, 03_inspect.png, ui_inspect.png, 06_battle.png, ui_battle.png`

三处目录（`client/captures/`、`our_slice/`、`BASELINE_LOGS/`）均 **无** `05_stage.png` / `07_qte.png` / `07_fever.png` / `08_result.png`。窗口补拍 0 张。

本任务已目视 `our_slice/`（非黑帧，**我方切片，不是 GT**）：

| 文件 | 看见 | 不是 |
|---|---|---|
| `01_home.png` | 大厅「翡翠兔」看板 + 底栏 | Drive / Fever / 结算 |
| `04_team.png` | 五人编队（白昼守望等） | 原作编队真值 |
| `03_inspect.png` | 「冰刃」详情 | 战斗态 |
| `06_battle.png` / `ui_battle.png` | 「开战」闪屏；底 Drive 条空 / ≈0%；色块敌人占位 | QTE、翼标、Fever 条幅、结算 |

`screens=` 含 `Result`，但**没有结算截图**。冒烟只在开战闪屏拍一帧（`VerticalSliceSmokeRuntime` 开战 `ShotDone("06_battle.png","ui_battle.png")` 后不再拍）。

`OUR_SLICE_CUES.md` 仍按旧 FAIL 写 Drive/Fever MISSING。画面结论与本轮一致；结果文件第一行已被 RESMOKE 覆盖为 PASS。cue 表未改，不把旧 FAIL 句当成本轮结果文件。

### 2.3 判定

Drive / QTE / Fever / 「胜利」= **日志成立**。  
Drive / QTE / Fever / 结算 = **无画面**。  
不得用日志行或开战图冒充视觉通过。

---

## 3. 用户 LocalLow `save.json`（只报告，未改档）

冒烟结果：

```
save=C:\Users\Administrator\AppData\LocalLow\Resonance\契灵回响\save.json
```

`Finish()` **读**该路径并写入结果 `save-json=`；缺文件会 `FAIL save.json missing`。活路径 `GameRoot.Persist()` → `SaveStore.Write(..., DefaultPath)` 会**写**同一文件。本轮冒烟会触发写入的点：

- `SetLeader(1)` → 立刻 `Persist()`（编队阶段）
- 开战结算 `ApplyVictory` + `Persist()`
- Drive/Fever 齐后 `EnsureAutoOn()`：若存档还不是 Full，会把 `Auto=Full` 并 `Persist()`

磁盘（本任务只读）：

| 文件 | mtime | 与冒烟 `save-json` |
|---|---|---|
| `save.json.bak` | **2026-09-10 17:20:24**（与结果文件同一秒） | **一致**（`leader=1` `seed=25371703` `auto=2` `hard=false` `clearedN=12`） |
| `save.json` | **2026-09-10 17:37:27**（冒烟后约 17 分钟） | **不一致**（`leader=2` `seed=24078156` `hard=true`；`auto` 仍为 `2`） |

`SaveStore.Commit` 用 `File.Replace(..., bak)`：后一次写入会把当时的 `save.json` 挪成 `.bak`。因此 `.bak` 保住了冒烟结束时的快照；当前 `save.json` 是冒烟之后另一次 Persist（Editor 日志一直写到 17:51:40）。

旧包档 `LocalLow\Resonance\Resonance\save.json`（2026-08-28 / 08-29）**不是**本轮。

**报告：本轮切片冒烟写过用户 LocalLow `契灵回响\save.json`（至少 `SetLeader` / 结算 Persist；`auto=2` 已在快照里）。当前盘上的 `save.json` 已被后续写入改过。本任务未改、未还原、未删用户存档。**

---

## 4. 不得升格 T26 / T27 / T28 / M1

`04_ACCEPTANCE_AND_TESTS.md` 原文：

| 项 | 要求 | 本轮 |
|---|---|---|
| T26 | 入口→编队→战斗→结算→**养成→再战**，没有断链 | 冒烟只走到 Result 日志。无养成、无再战、无结算图。`our_slice/` 不是 GT。**仍 NOT_RUN / BLOCKED。** |
| T27 | 参考原生 UI 同画幅叠加；误差门槛 | 画幅 `NOT_MEASURED`（U010）。布局 `NEEDS_REFERENCE`。无 primary GT。**禁伪 pass。** |
| T28 | 输入/命中/数字/Drive/Fever ≤2 参考帧 | 无 primary GT，未逐帧。日志时间戳不是参考帧。**仍 BLOCKED。** |

T26 全链未过，不能因为 `screens=` 含 Result 或 Home/编队/开战有图就改 IMPLEMENTED。  
T27 / T28 无对照视频则还原列不得 IMPLEMENTED。

同时仍成立：

- primary GT：`docs/reference/gl-shutdown-pve/` **0 mp4**（本任务复核，无 `.mp4`）
- `TARGET.json`：`TARGET_FROZEN_GT_SEARCHING`；`GL_FINAL_VERIFIED` 未创建且禁止
- EditMode：`NOT_RUN`
- G2：`IN_PROGRESS_CORE_SLICE`（**未过**）。G2 要「全部关键状态 + 对照视频」
- M1：`IN_PROGRESS`（**未验收**）
- 本任务未开 Editor（`Library/EditorInstance.json` 不在）

`T_MATRIX.md` 里 T26 行仍写旧 FAIL。那是过时备注，**不是**把本轮 PASS 写进 T26。本任务不改那张表。

---

## 还原分

**0 / not claimed。**

- T 还原 IMPLEMENTED：**0 / 32**
- G2 还原：**0 / 9**
- 无 numeric fidelity、无 90%、无 T27 pass
- 大厅立绘、开战闪屏、日志「胜利」、切片 PASS、夹具绿数，都不是原作回归

功能核内部一致性（T01 / T03 / T10 / T15 / T18）与本轮切片 PASS **无关升格**。本任务未重跑 `dotnet test`。

---

## 读过 / 未做

读过：归档 `vs-smoke.result.txt`（两份）、`STATUS.md`、`QA_DRIVE.md`、`RESULT.md`、`LAUNCH.txt`、`editor-playmode-20260910-resmoke-c.log`、`our_slice/README.md`、目视 `our_slice` 大厅/编队/详情/开战图、`T_MATRIX.md`、`M1_UNGATE.md`、`TARGET.json`、`VerticalSliceSmokeRuntime` / `GameRoot.Persist` / `SaveStore`（只读）、用户 `save.json` + `.bak`（只读）。

未做：改 `client/`、开 Editor、写 `vs-smoke.request`、改用户存档、发明 `GL_FINAL_VERIFIED`、开 G3、报还原分。

## 结论句

`M1-G2-RESMOKE` 的 Vertical Slice 结果文件本轮确为 **PASS**（2026-09-10 17:20:24；原 `client/Temp` 已空，归档同文）。Drive / Fever 只在日志里；开战图 Drive 仍空，无 QTE / Fever / 结算画面。冒烟写过用户 LocalLow `契灵回响\save.json`（`.bak` 即当时快照）；之后档被另一次写入改过。本任务未动该档。

**切片 PASS ≠ T26 / T27 / T28 ≠ M1 验收。G2 未过。还原分仍为 0。**
