# 我方切片 cue（`M1-G2-OUR-CUES`）

2026-09-12h 补：`our_slice/shots_20260912h/06j_auto_hit.png`（自动普攻伤害数字）与 `06f_kill.png`（敌死无击破章）。仍是我方切片，不是 GT。

日期：2026-09-10。`our_slice/` 与 `client/captures/`。主协调者目视过 `07_drive` / `08_fever` / `09_result`（非黑帧）。

**这是契灵回响 / Resonance 我方 Vertical Slice，不是原作 GT。**  
不得挪到 `docs/reference/gl-shutdown-pve/` 上一级当 primary。不得报还原通过 / T27 / T28 / `GL_FINAL_VERIFIED`。

来源：
- 17:20 `M1-G2-RESMOKE`：日志 **PASS**，当时只有开战图。
- 20:06 `M1-G2-CAPTURE-STATES`：补拍 Drive / Fever / 结算；存档隔离到 Temp，未改用户 LocalLow。

`ui_*.png` 与同屏编号图同尺寸，按副本处理。`02_characters.png` = `02_roster.png`（同帧别名）。

## 覆盖（我方壳，不是原作）

| 状态 | 判定 | 代表帧 |
|---|---|---|
| Home | **看见** 大厅「翡翠兔」看板 + 底栏 | `01_home.png` / `ui_home.png` |
| 契灵一览 | **看见** 五人卡 + 战力；底栏「契灵」高亮 | `02_roster.png` / `02_characters.png` / `ui_roster.png` |
| 编队 | **看见** 五人入队；顶栏五槽 | `04_team.png` / `ui_team.png` |
| Inspect | **看见** 「冰刃」详情 / 六维 / 技能入口 | `03_inspect.png` / `ui_inspect.png` |
| Stage | **MISSING** | 日志有 `phase Stage`，**无截图** |
| 开战 | **看见** 「开战」闪屏；Drive 条曾为 0% | `06_battle.png` |
| Drive / QTE | **看见（切片占位）** | `07_drive.png`：场上「好」+ SHOWTIME 叠字；**未见**独立 Drive 选择面板 |
| Fever | **看见（切片占位）** | `08_fever.png`：「完美」+ 150% + 「狂热时间」与 SHOWTIME 叠在一起 |
| 结算 | **看见（切片占位）** | `09_result.png`：黄底「完成」+ 回首页 / 下一关；不是 Ragna 排名 |

## 逐帧（已目视，非黑帧）

| 文件 | cue | 看见什么 |
|---|---|---|
| `01_home.png` `ui_home.png` | HOME | 棋盘底；红沙发「翡翠兔」立绘；左标「翡翠兔 / 大厅看板」；右「看板·翡翠兔」；右侧竖排功能钮；底栏：首页（高亮）/ 契灵 / 关卡 / 编录 / 书库 / 深渊 |
| `02_roster.png` `02_characters.png` `ui_roster.png` | ROSTER | 顶五槽立绘；中五张角色卡（冰刃已入队、选中「详情」）；总战力字；底选「冰刃」火攻摘要；底栏「契灵」高亮 |
| `04_team.png` `ui_team.png` | TEAM | 与一览同壳；顶五人已入队；选中「白昼守望」；总战力字；底栏仍「契灵」高亮（Resonance 编队页，不是原作编队真值） |
| `03_inspect.png` `ui_inspect.png` | INSPECT | 「冰刃」全身立绘；等级 / 好感 / 战力 / 生命·攻击·防御·敏捷·暴击；右上图库/姿势；底「技能」 |
| `06_battle.png` `ui_battle.png` | BATTLE_START | 中大字「开战」+「弱点」；顶：暂停 / 敌血条 / 全自动；`×2`；三色块敌人占位；五人立绘；底五圆头；Drive 条约 **0%**。**不是**稳定待命，**不是** Drive/Fever/结算 |

## 战斗必测（`UiStateMap`）— 本轮切片可见性

有/无只描述**本轮拍到没有**，不是实现完成声明。

| 状态 | 本轮 |
|---|---|
| `BattleIdle` | 仅「开战」一瞬；未见稳定待命 HUD |
| `ChargingReady` | **MISSING** |
| `Tap` | **MISSING**（仅日志 `tap 0`） |
| `Slide` | **MISSING**（仅日志 `slide 1`；无 Showtime / 切镜图） |
| `DriveSelect` | **MISSING** |
| `Qte` | 有图 `07_drive`「好」、`08_fever`「完美」；叠字重，不是原作 QTE 条 |
| `Fever` | 有图 `08_fever`「狂热时间」；与 SHOWTIME 同帧 |
| `Controlled` | **MISSING** |
| `DeathOrWave` | **MISSING** |
| `Result` | 有图 `09_result`「完成」 |

后续页：Home / 一览 / 详情 / 编队 **有**自己的页，是 Resonance 壳。养成 / 关卡选择 / 队伍确认 / 掉落 / 装备 **MISSING**。

## 硬缺（对照 / 还原）

1. 仍无 primary GT mp4。`07`/`08`/`09` **不能**当原作帧。
2. Drive **选择面板**仍未见。
3. `08_fever` 上「完美 / SHOWTIME / 狂热时间」叠在同一帧，不能当逐帧 cue 真值。
4. 受控 / 换波 / 死亡图仍缺。

## 禁止升格

- `our_slice/` 与 `client/captures/` 全部 png、`vs-smoke.result.txt`
- 大厅翡翠兔、契灵一览、冰刃详情、五人编队、开战闪屏
- 日志 `tap 0` / `slide 1` / `result 胜利` / `phase=Result`
- 开战图色块敌人、暂停 / ×2 / 全自动、Drive 0%
- 切片冒烟 PASS、`07`/`08`/`09` 我方图、`dotnet test` 绿数
- 实战剖面仍是 `JP_LEGACY_EMPIRICAL`（对照，不是 GL）

primary 仍 `NEEDS_FETCH`。`docs/reference/gl-shutdown-pve/` 根目录仍 **0 mp4**。本文件不构成还原通过。
