# M1-G2-QA-DRIVE — 独立 Drive 冒烟复核

- 任务：`M1-G2-QA-DRIVE`
- 角色：`dc_qa`（只读生产代码；本文件为 G0 QA 报告）
- 日期：2026-09-10
- 对照：归档 PlayMode `FAIL drive never fired`、`STATUS.md`「代码侧已修」、`StayHeld` / `SliceDriveSequence` / `FirePerfect` / `VerticalSliceSmoke`、`M1CoreSliceTests` Drive 序列、`M1RepairQaTests` Fever 通道
- **未改** 生产代码、`TARGET.json`、未 spawn、未开 Unity Editor、未开 G3
- **不采信** 实现代理自述 / `STATUS.md`「已修」= PlayMode 过

## 总判（必须先读）

**M1 不可验收。G2 未过。不得报 90%。不得报还原分 / PlayMode PASS。**

三问（源码 + 默认 `dotnet test` + 归档冒烟，**不是**本轮 Editor 重跑）：

| # | 问题 | 判定 |
|---|---|---|
| 1 | 冒烟失败根因是否真修了 | **代码/夹具针对成立；PlayMode 未证实** |
| 2 | 是否只黑了 Smoke，真实 GameRoot 手动仍能 Drive | **成立（源码）** |
| 3 | Full Auto 是否还会开战秒杀 | **会（核未改）** |

硬否决仍在：

1. primary GT 仍缺。`docs/reference/gl-shutdown-pve/` **0 mp4**。
2. 本任务 **未开 Editor**。归档 PlayMode 仍是 `FAIL drive never fired`。不得把代码修补写成冒烟绿。
3. 默认 `dotnet test` 本轮 **0 失败 / 118**。绿的是夹具自洽，**不是**原作回归，**不能**升格为 M1 / PlayMode 过。

`GameRoot.StartBattleAt` 仍 `Profile = JP_LEGACY_EMPIRICAL`。客户端实战打 JP 对照数，不是 GL。

---

## 1. 冒烟失败根因是否真修了

### 1.1 归档失败（本任务未重跑）

`DC_RECON_KIT/m1/G0/BASELINE_LOGS/vs-smoke.result.txt`（`M1-G2-PLAY-RECORD`）：

```
FAIL drive never fired
phase=Result
...
log:tap 0
log:slide 1
log:result 胜利
```

开战闪屏 Drive≈0%。只有一次 Tap(0)+Slide(1)，随后 Result「胜利」。无 Drive / Fever / 结算图。

### 1.2 根因（源码，对得上日志）

`GameRoot.Update` 每帧按 `Time.deltaTime * TickHz` 调 `_battle.Tick()`。`Tick` 里 `CheckWave()`：两波清空就 `Victory`。

`ReleaseBlocks`：

- `Auto == Full`：**无视** `HoldSim`，战斗照走（`FullAutoIgnoresHoldSim` 仍断言这一点）。
- `Auto != Full` 且 `HoldSim`：最多冻 `HoldTimeoutSec = 2s`，然后放行（`HoldSimWatchdogReleasesManual` 仍在）。
- `StayHeld()` = `HoldSim=true` 且 `_holdTicks=0`。只有**每帧重刷**才不会被 2s 看门狗放行。

归档当时的冒烟：开战不强制 Manual、不每帧 `StayHeld`；`FireDrivePerfect()` / HUD `FirePerfect()` 要 `Drive>=100` 才 `TryBeginDrive`。Tap+6、Slide+14，两次技能 Drive≪100。核继续 `Tick` → 开口关 `VS-1`（`hpMul=1`，波 0 约 5900 HP + 首领 9800）被 Auto / 充能技清空 → Result，Drive 从未打出。

这就是「开战秒杀、Drive 来不及」：**Tick 先结算胜利**，不是 Drive API 没写。

### 1.3 代码侧针对性修补（属实）

`VerticalSliceSmokeRuntime.HoldForDrive`：`Auto = Manual` + `StayHeld()`。开战当帧、以及 Drive/Fever 未齐的每个 `Update` 都刷。`DefaultExecutionOrder(-1000)` 先于 `GameRoot`，同帧 `Tick` 时 hold 已在。

灌条：`ReadyCharges` → `TryFillDrive`（满充能 Tap/Slide 直到 Drive≥100）→ `TryBeginDrive` / `g.FireDrivePerfect()` / `SliceDriveSequence.TryFirePerfect`（Perfect，Fever +60；两条 Perfect 进 Fever）。齐了才 `HoldSim=false`、`EnsureSpeed2`、`EnsureAutoOn`。

`CheckWave` **只在 `Tick` 里**。Hold 住时 JP 灌条可以把敌 HP 打到 0，但 Outcome 仍 `InProgress`，屏幕不进 Result，序列还能打 Drive/Fever。这正好堵住归档那种「先胜利、后没 Drive」。

夹具：

- `M1CoreSliceTests.StayHeldKeepsManualBattleFrozen`：Manual + 每 tick `StayHeld`，5s 时钟不动、无 Drive cast。
- `M1CoreSliceTests.FixedSeedFivePersonSequenceFiresDriveAndFever`：`PlayUntilFever` 打出 Drive cast + `FeverActive`。

Headless `VerticalSliceSmoke.RunHeadlessLoop` 同样走 `PlayUntilFever`。

### 1.4 未闭合（不得写成「已修完」）

| 缺口 | 说明 |
|---|---|
| PlayMode 未证实 | 本任务未开 Editor。归档仍 FAIL。 |
| 序列测不是 JP 实战剖面 | `FixedSeedFivePersonSequenceFiresDriveAndFever` 与 headless **未设** `JP_LEGACY_EMPIRICAL`，默认 `GL_UNKNOWN`，Tap/Slide **不改 HP**。只证明条能走完，不证明开口关 JP 伤不会在未 Hold 时先 `CheckWave`。 |
| 2s 看门狗仍在 | 单帧 hitch ≥60 tick（`GameRoot` 每帧最多追 2s）且该帧只 `StayHeld` 一次，Hold 可能被放行。 |
| 冒烟成功后写档 Full | Drive/Fever 齐后 `EnsureAutoOn()` 把 **存档** 打成 Full。下一次非冒烟开战会吃 Full Auto。 |

**判定：** 归档根因（Tick 先胜利）在冒烟路径上被 Manual+每帧 StayHeld + 灌条对准了。**不是** PlayMode 已绿，也不是 Full Auto / 看门狗从核里删掉了。

---

## 2. 是否只黑了 Smoke；GameRoot 手动还能 Drive

**是。活战斗没有被 StayHeld / ReadyCharges 锁死。**

`GameRoot.StartBattleAt`：`Auto = _save.Auto`（默认 `Manual`），**不** `StayHeld`，**不** `ReadyCharges`。`DrawBattle` 必建 `BattleHud`。

活路径：

- 立绘 `TryPortraitTap`：Drive≥100 则 `TryBeginDrive`，否则 Tap。
- Manual：`OpenQte`，玩家打 QTE；`FirePerfect()` → `FinishQte(Perfect)`。
- `GameRoot.FireDrivePerfect()`：有 HUD 走 `BattleHud.FirePerfect()`（**不** `TryFillDrive`）。HUD 空才 fallback `SliceDriveSequence.TryFirePerfect`（会灌条）。活战斗有 HUD，不走这条。

只在冒烟 / headless / 夹具里出现的黑盒：

- `HoldForDrive` / 每帧 `StayHeld`
- `SliceDriveSequence.ReadyCharges` / `TryFillDrive` / `PlayUntilFever`

`CaptureRuntime` 仍是 `EnsureAutoOn` + 一次性 `HoldSim=true`（不是 `StayHeld` 重刷），与冒烟修补无关。

结论：手动 GameRoot 仍能自己充 Drive、开 QTE、Perfect。冒烟用的是冻核 + 满充能灌条，**没有**把 `TryBeginDrive` / HUD QTE 从生产开战里拆掉。

---

## 3. Full Auto 是否还会开战秒杀

**会。核故意如此，本轮没改。**

```291:297:client/Assets/Scripts/Resonance.Battle/Core/BattleSim.cs
            if (HoldSim && Auto != AutoMode.Full)
            {
                _holdTicks++;
                if (_holdTicks * TickDt < HoldTimeoutSec)
                    return false;
                HoldSim = false;
```

`AutoFireDrive` / `AutoFireSkills` / `TickUnit` 的 Auto 技在 Full 下照跑。`FullAutoIgnoresHoldSim` 要求 Full 时 `HoldSim` 挡不住时钟。

开口关可被 Full Auto 清完：`NakedTeamClearsOpeningNotFinale`、`MvpChapterClearsOnAuto`、`AutoPlayCompletesStageWithAllVerbs`（皆 `JP_LEGACY_EMPIRICAL` + `AutoTap`/`Full`）。

「开战第 1 秒」字面：盟友 Charge 初值 35，`AutoFireSkills` 要 100；`ChargeTimeSec` 7.5–9s，约 6s 才第一次预约技。`AutoTimer` 首发约 1.9s。更准确的失败态是：**开战数秒内 `CheckWave`→Result，Drive 条没打完**。归档那次就是。

冒烟只在**自己的** Battle 上改 `b.Auto = Manual`，不改存档。存档若已是 Full（设置、旧 `"auto":true`、上次冒烟 `EnsureAutoOn`、`CaptureRuntime`），`StartBattleAt` 仍按 Full 开战，Hold 无效。

---

## 4. `M1RepairQaTests` 里没有 Drive 序列测

Drive/Fever **切片序列**在 `M1CoreSliceTests`：

- `FixedSeedFivePersonSequenceFiresDriveAndFever`
- `StayHeldKeepsManualBattleFrozen`

`M1RepairQaTests` 只有 Fever **通道/系数**（不是灌条）：

- `ExtraDmgDoesNotFoldAutoOrFeverIntoTapTsAmp`
- `FeverChannelIsNotTap`
- `FeverDoesNotReadTapAtkCoefOrFlatPower`

另：`BattleSimTests.PerfectDrivesReachFever` / `TwoGreatDrivesTriggerFever` / `AutoTapFiresDriveWhenReady` / `SemiAutoDoesNotFireDrive` 覆盖倍率与 Full/Semi，不覆盖冒烟 Hold。

---

## 5. 测试原始输出

命令：

```
dotnet test tools/BattleSim.Tests/BattleSim.Tests.csproj --nologo --verbosity minimal
```

cwd `F:\天命之子`，2026-09-10。**默认并行，本轮原始输出：**

```
[Profile] UTF-8 ready
  正在确定要还原的项目…
  所有项目均是最新的，无法还原。
  BattleSim.Tests -> F:\天命之子\tools\BattleSim.Tests\bin\Debug\net6.0\BattleSim.Tests.dll
F:\天命之子\tools\BattleSim.Tests\bin\Debug\net6.0\BattleSim.Tests.dll (.NETCoreApp,Version=v6.0)的测试运行
正在启动测试执行，请稍候...
总共 1 个测试文件与指定模式相匹配。

已通过! - 失败:     0，通过:   118，已跳过:     0，总计:   118，持续时间: 443 ms - BattleSim.Tests.dll (net6.0)
```

退出码：`0`。

118 = `BattleSimTests` 89 + `IgnitionTests` 6 + `M1CoreSliceTests` 10 + `M1RepairQaTests` 13。  
未生成 trx/XML。未跑 Unity EditMode/PlayMode。

**118 绿 ≠ PlayMode 过 ≠ M1 验收。**

---

## BLOCKED

| 项 | 状态 |
|---|---|
| PlayMode / VS Smoke | 本任务未开 Editor。归档仍 `FAIL drive never fired` |
| EditMode | `NOT_RUN` |
| primary GT | `CANDIDATE_NEEDS_FETCH`；0 mp4 |
| JP 剖面下的 Drive 序列夹具 | 没有；现有序列测走 `GL_UNKNOWN` |
| Full Auto 开战 | 核仍无视 Hold；开口关仍可被 Auto 清完 |
| U003 Drive 实际伤害 | UNKNOWN |

---

## 未做 / 边界

- 未改 `client/`、`ProjectSettings`、`Packages`、`TARGET.json`。
- 未开 Unity Editor，未写 `vs-smoke.request`。
- 未宣称 fidelity、未开 G3、未把 118 绿写成验收。

## 结论句

冒烟 FAIL 的根因是开战 `Tick`/`CheckWave` 先胜利，Drive 条没打完。代码把**冒烟自己的**战斗冻成 Manual+每帧 `StayHeld`，再用五人灌条打 Drive/QTE/Fever；夹具在 `GL_UNKNOWN` 上能复现这条。  
活 `GameRoot` 开战不冻、不灌充能，HUD 手动 Drive/QTE 仍在。Full Auto 仍无视 Hold，开口关仍能被 Auto 清完。  
PlayMode 未重跑。**不能验收。**
