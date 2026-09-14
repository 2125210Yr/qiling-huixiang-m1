# G2 路 A 批次状态

记录时刻：2026-09-14 11:40 +08:00（集成者回填；骨架由文档代理 2026-09-13 写出）。  
本文件只描述**工程批次**。不是 M1 还原验收，不宣称 T27 / 90% / M1 通过。

## SHA / 分支 / 环境

| 项 | 值 |
|---|---|
| 分支 | `m1-gt6-review` |
| 审查快照 | `ed7a9ba6423f79aee9a4276f9a502c4e6d9cb6f3`（`AUDIT.md` 输入） |
| 当前 HEAD | `d631573`（**脏树**：下列改动尚未提交；本文件所有证据均出自脏树，不得当成干净提交证据） |
| Unity | `6000.3.23f1` |
| 核心测试 | `tools/BattleSim.Tests` 203 项（156 旧 + 47 `G2Review*` 新红测试） |
| 表现冒烟 | `VerticalSliceSmokeRuntime`（状态展示测试，允许夹具注入） |
| 自然游玩 E2E | `NaturalPlayRuntime` + `NaturalPlaySmoke`（仅 `EventSystem` 指针事件，无注入） |

### 脏树改动清单（`git status --short`，仅代码）

修改：`BattleSim.cs` `Definitions.cs` `BattleEventLog.cs` `DamageMath.cs` `FormulaProfile.cs` `EffectOpcodes.cs` `Catalog.cs` `CatalogJson.cs` `BuffCatalog.cs` `BattleHud.cs` `GameRoot.cs` `VfxGoodButton.cs` `VfxTipPlate.cs` `Editor/PuppetBonePreview.cs` `M1RepairQaTests.cs` `M1FormulaIsolationTests.cs`  
新增：`BattleSim.Commands.cs` `BattleReplay.cs` `EffectCapability.cs` `Debug/NaturalPlayRuntime.cs` `Editor/Smoke/NaturalPlaySmoke.cs` `tools/BattleSim.Tests/G2Review*.cs`（9 个文件）

## R01–R09 状态

状态只用 `OPEN` / `IN_PROGRESS` / `FIXED_UNVERIFIED` / `VERIFIED`。"VERIFIED" 指本批有可复跑的自动化证据；不代表与参考片一致。

| ID | 摘要 | 状态 | 证据 |
|---|---|---|---|
| R01 | 图形冒烟证明可展示，不证明自然操作走通 | IN_PROGRESS | `natural-play/run7/natural-play.result.txt`：真实指针链路 Home→关卡→FIGHT→暂停/继续→倍速→QTE×2→结算→NEXT→再战→HOME 全部走通；**Tap/Slide 技能在 VS-1 自然节奏下不可达**（见"自然游玩发现"N-01），结果 `FAIL BattlePlay` |
| R02 | 手动 Fever 未按玩家连点出手 / 未选被点角色 | VERIFIED（核心） | `G2ReviewFeverTests` F01–F04 通过；`TickFever` 只推时钟，出手来自 `Submit(FeverTap)`；节流/预算/AutoOwnsInput 有断言。自然游玩尚未触发 Fever（Perfect 只 +40 计量），Fever 的 HUD 路径只在夹具冒烟里出现 |
| R03 | QTE 双计时权威；末态回调防护不足 | VERIFIED（核心） | `G2ReviewQteClockTests` Q01–Q04；HUD `_qteT` 降为镜像，读 `QteRemaining`/`LastDriveTiming`；`ResolveDriveChecked` 拒绝 NotInProgress/Paused/NoPending。自然游玩两次 QTE 都经 `Submit(DriveBegin/DriveResolve)`（CommandLog seq 4–7） |
| R04 | 可复现被做成关闭暴击 | VERIFIED（核心） | `G2ReviewRngReplayTests` R01–R03：种子随机保留暴击；`ForceNoCrit` 仅夹具；`GameRoot.StartBattleAt` 不再置 `Deterministic=true`；自然游玩 RunHeader `forceNoCrit=0` |
| R05 | 效果能力登记未证明语义生效 | FIXED_UNVERIFIED→部分 VERIFIED | `G2ReviewEffectLifecycleTests` E02–E04 + Silence/ChargeSpeed 通过；`G2ReviewCapabilityTests` 4/4；护盾改为 `StatusInst.ShieldLeft` 按声明寿命过期。`capability-matrix.md` 列出 12 个 `Registered_NotImplemented`（如 `dot_flame` 内建内容不 tick）——**只报告，未改内容** |
| R06 | 能算出数字 ≠ 有实测依据 | VERIFIED（核心） | `G2ReviewProvenanceTests` 6/6；`FormulaResult.Computed`/`Evidence` 分离；`BattleEvent.Evidence` 入日志；`TryResolveCombat` 改读 `Computed` |
| R07 | 手势 / 冒烟 / 探针入口不统一 | IN_PROGRESS | HUD 手势、暂停/倍速/自动、`GameRoot.Tap/Slide`（Fixture 源）均经 `BattleSim.Submit`；`AutoFireSkills` 仍直接调 `TryTap`（未经 Submit，回放时由 Tick 重生，不记录）。`patches/UI_INPUT_PATCH.md` 有剩余项 |
| R08 | 养成界面出现 ≠ 养成循环已实现 | OPEN | 本批未动；结算 LEVEL UP 只作展示 |
| R09 | 旧入口文档与延期 / 并发上限冲突 | FIXED_UNVERIFIED | `G2_ENTRY.md`、`GT6_REVIEW.md`/`STANDING_ORDERS.md` 横幅、`deferred.md` 已落盘；待用户确认 |

## 测试结果

| 套件 | discovered | executed | passed | failed | skipped | 结果 | 证据 |
|---|---|---|---|---|---|---|---|
| 核心 `BattleSim.Tests`（含 47 `G2Review*`） | 203 | 203 | 203 | 0 | 0 | PASS | `artifacts/tests/g2-review-regression.trx`（2026-09-14 11:3x，脏树） |
| 表现冒烟 `VerticalSliceSmokeRuntime`（20260913e） | 1 | 1 | 1 | 0 | 0 | PASS | `artifacts/presentation-smoke/20260913e/vs-smoke.result.txt` + 27 张截图 + `editor-smoke-e.log`；tap/slide/drive/fever/wave/control/speed/auto/pause/kill/levelup/rematch 旗标全 True（**夹具注入**：补充能、直接施加护盾/控制、强制 Perfect） |
| 自然游玩 `NaturalPlayRuntime` | 1 | 1 | 0 | 1 | 0 | **FAIL**（`BattlePlay` missing=Tap,Slide） | `artifacts/natural-play/run7/`（result/events/8 张截图）+ `editor-natural-1..7.log` |
| Unity 脚本编译 | — | 7 次启动 | 6 | 1 | — | 第 1 次失败（命名空间遮蔽 `UnityEngine.Debug`，已修），后续全部通过 | `editor-natural-*.log` |
| Unity EditMode / PlayMode 测试框架 | — | — | — | — | — | NOT_RUN | 本批未用 UTF |

三个旧测试（`FeverChannelIsNotTap`、`FeverDoesNotReadTapCoefficients`、`GlUnknownDoesNotSettle`）因契约把 Fever 从计时器出手改为命令出手而失败，已把其 `RunFeverHits` 助手改为 `Submit(FeverTap)`，**原断言未改**。

## 自然游玩发现（run 1–7，真实指针事件）

| 编号 | 现象 | 判定 | 处置 |
|---|---|---|---|
| N-01 | VS-1 自然节奏：5 名队员自动攻击约 3.5 s 就把 Drive 打满；HUD 在 Drive≥100 时把头像点击映射为 DriveBegin；p0 充能 ~9 s。战斗在**无任何输入**下 ~11–24 s 自结（Victory）。Manual 玩家在整场里拿不到 Tap/Slide 窗口 | 节奏/内容问题，非崩溃。夹具冒烟用 `ReadyCharges` + 敌方加血掩盖了它 | **未改内容**（禁止新增机制/内容）。E2E 以软失败记录 `missing=Tap,Slide`，其余流程继续跑完。需用户决定：调 VS-1 敌方数值 / 改 Drive 与 Tap 的输入映射 / 接受 |
| N-02 | Home 的"关卡"按钮被 `C001_PuppetCanvas/C001_PuppetPreview/view`（编辑器骨骼预览、`raycastTarget=true`，覆盖 0.15–0.85 × 0.08–0.92）吞掉点击 | 编辑器工具泄漏进 Play，非发行缺陷；会污染所有编辑器内冒烟 | 已修：`PuppetBonePreview` 在 ExitingEditMode 销毁预览画布；有 smoke request 时不自动打开 |
| N-03 | QTE 金币 `vfxGood` 没有任何 `raycastTarget` Graphic：真实指针**无法**按下 PRESS BUTTON，只有夹具 `FireDrivePerfect` 能过 | 真实输入缺陷（R01/R07） | 已修：`rim`/`face` 开 raycastTarget。run5–7 两次 QTE 都由指针点中 `vfxGood/face` 结算（Good/Perfect） |
| N-04 | Drive QTE 期间敌方行上方一块**空白灰卡**（`VfxTipPlate`）：`Resources.GetBuiltinResource<Font>("Arial.ttf")` 在 Unity 6 抛 `ArgumentException`，`Build()` 中断，只剩底板 | 展示缺陷；也出现在夹具冒烟 06d/07 | 已修：改用 `CharacterPresenter.UiFont()`。run7 `np_04_qte.png` 显示 "TIP! Attack with a Tap Skill!" |
| N-05 | N-04 修好后暴露：该 TIP 文案在 Drive 已满时弹出，教玩家"点头像放 Tap 技能"，而此时点头像实际打开 QTE | 文案与输入映射矛盾，属 N-01 同源 | 记录，未改 |
| N-06 | 06d "Barrier 六边形"：`VfxShield.Play` 0.35 s 施加闪光，夹具一次给 5 人上盾便叠出 5 个六边形；无持久化、无泄漏（`patches/UI_STATE_LIFECYCLE_AUDIT.md` §4） | 夹具产物，非缺陷 | 关闭。自然游玩里无护盾技能触发，未出现 |
| N-07 | `NaturalPlayRuntime` 初版把 Tap→Slide→QTE 顺序写死并用 `rejectDriveWindow` 拒绝 Drive 满的状态，导致永远等不到 ChargeReady | 测试脚本假设错误 | 已改为状态驱动 `PlayBattleNaturally()`（QTE 优先、Drive 回填窗口内 Tap、Slide 随时拖）；`TapNamed` 在按钮被销毁后不再读 `go.name` |

## 构建

| 项 | 值 |
|---|---|
| 结果 | NOT_RUN（本批未出新构建；`dist/windows/` 为旧产物，未纳入证据） |
| 阻塞 | 自然游玩 N-01 未决；脏树 |

## 批次判定

`PATH_A_ENGINEERING_PASS` / `NEEDS_FIX` / `BLOCKED` = **NEEDS_FIX**

- 已达：真实输入链路（`Submit` 统一入口 + 指针 E2E 全流程走通）、核心修复（R02/R03/R04/R06 核心层有自动化证据）、203/203 核心测试、表现冒烟 20260913e PASS。
- 未达：自然游玩 E2E 红（N-01 Tap/Slide 不可达，需用户决策）；R07 `AutoFireSkills` 未经 Submit；R08 OPEN；无干净提交、无新构建。

M1_FIDELITY=DEFERRED_NOT_REMOVED  
G3_NOT_STARTED

延期分母见同目录 `deferred.md`（未改范围）。缺参考不阻止本批工程任务结束，但不能自动宣称整个 M1 通过。
