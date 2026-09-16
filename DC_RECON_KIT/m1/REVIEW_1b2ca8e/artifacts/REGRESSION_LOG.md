# REVIEW_1b2ca8e / B1-A1 回归日志

本表计数只认本跑 TRX `artifacts/tests/a1-1b2ca8e.trx` 的 `<ResultSummary>`，不发明，不抄 A53 的 244 声称，不改旧 TRX。

命令（无 filter）：

```
dotnet test tools/BattleSim.Tests/BattleSim.Tests.csproj --nologo --logger "trx;LogFileName=a1-1b2ca8e.trx" --results-directory DC_RECON_KIT/m1/REVIEW_1b2ca8e/artifacts/tests
```

| 本跑 TRX 字段 | 值 |
|---|---|
| outcome | Completed |
| total | 245 |
| executed | 244 |
| passed | 244 |
| failed | 0 |
| notExecuted | 0 |
| 退出码 | 0 |
| RunId | `a457d5a0-4d49-4492-b868-a50fb82349d8` |
| 日志 | `artifacts/a1-dotnet.log` |

控制台同行：失败 0，通过 244，已跳过 1，总计 245。跳过的 1 项是 N01–N03 Unity 合同，不是核心失败。

## 历史 a53-gate.trx = HISTORICAL 234/10

`DC_RECON_KIT/m1/REVIEW_1a75253/artifacts/tests/a53-gate.trx`（blob `987ffbe9ebfd493370fe20ff4258847819270cd2`，RunId `561a2e62-012e-4021-ba20-6b0b3620f263`，2026-09-14 17:13+08）`ResultSummary=Failed`，passed=234 failed=10。  
A53 摘要写 244/0/1 是**证据错配**，不是本跑。该文件字节未改。副本：`artifacts/historical/a53-gate.trx`。

run7 `FAIL BattlePlay` 仍为历史 FAIL，不改写。

## 历史十项失败在本跑是否复发

下列名称来自 EVIDENCE.json / REVIEW 对 **a53-gate.trx** 的列出。本跑 TRX 中均为 `outcome="Passed"`。**未复发。**

| 历史失败（a53-gate 17:13） | 本跑 `a1-1b2ca8e.trx` |
|---|---|
| `G2ReviewReplayComponentTests.FullReplayVerifyHasZeroDivergences` | Passed |
| `G2ReviewReplayComponentTests.DifferentSeedReplayReportsDivergence` | Passed |
| `M1CoreSliceTests.SlideCdIsIndependentOfCharge` | Passed |
| `M1RepairQaTests.SlideDoesNotUseUniformVarianceRng` | Passed |
| `M1RepairQaTests.CloneSkillKeepsExternalSlideRankSlots` | Passed |
| `M1RepairQaTests.EmptyOrUnknownOpcodeCastFails` | Passed |
| `BattleSimTests.TapAndSlideRecordDistinctCasts` | Passed |
| `BattleSimTests.SlideAddsFourteenDrive` | Passed |
| `BattleSimTests.AutoTapFollowsReservation` | Passed |
| `BattleSimTests.BattleSimLightDarkMatchup` | Passed |

## 本跑其它合同（不是 T08 / Unity 通过）

| ID | 层 | 状态 | 说明 |
|---|---|---|---|
| N01–N03 | 核心套件 | Skip / TRX NotExecuted | xunit 合同仍 Skip。**不是**把 A4 Editor 结果写进这三行 PASS。 |
| G2A53* / L01 / L02 | 核心 | Passed（本跑 TRX failed=0） | A53 已关闭；断言未削弱。 |
| A4 basic | 自然 UI | PASS | `np.basic.v1` 实跑；`SESSION_RESULTS.md` |
| A4 fever | 自然 UI | FAIL | `np.fever.v1` FeverPlay；3× QTE 未开 Fever |
| A4 auto | 自然 UI | PASS | `np.auto.v1` HUD Full→Manual |
| A4 读回 | 文件回放 | FAIL（开局） | 当时 MaxHp 2200；现已由 F1 关闭 |
| F1 读回 | 文件回放 | FAIL（中段） | growth 已恢复；Tick/Drive/Events 仍偏；`fight1-readback.txt` |
| F2 fever | 自然 UI | FAIL OpenQte#3 | `20260914T175019` 2× Perfect（80/100）；第 3 次 DriveBegin p0 UnitDead。窗口已验证。`171516` BLOCKED 保留 |
| F2 fever 活槽 | 自然 UI | FAIL FeverPlay（目标已记） | `20260914T181042` feverEver=True；DriveBegin 0,0,1；3×Perfect；FeverTap Player 1,2。首行 160s 超时（`missing=` 空）。不报 PATH_A |
| F2 fever 收口 | 自然 UI | PASS | `20260914T183658` 目标记一次后 yield-break。fight-1 FeverTap Player 0,1；3×Perfect；CLEAR!! → NEXT → HOME。不报 PATH_A |
| A4 构建/录像 | 交付 | RAN | `BUILD_HASHES.txt` + `recordings/np-continuous-20260914T143817.mp4` |

A2/A3/F1/F2 源码已合入。`PATH_A` 仍 NEEDS_FIX（读回中段 Match=False）。
