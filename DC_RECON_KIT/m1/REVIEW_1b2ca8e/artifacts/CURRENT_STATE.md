# REVIEW_1b2ca8e / B1-A1 当前结果

记录时刻：2026-09-14。batch_id=`B1`。task=`B1-A1`。  
本文件只描述**本批本跑**的核心套件证据。不是 M1 还原验收，不宣称 T27 / 90% / M1 通过，不开 G3。

## 本跑（唯一有效核心计数）

计数从本跑 TRX `<ResultSummary>` **原样抄出**，不是 A53 摘要里的 244 声称，也不是手改旧 TRX。

| 项 | 值 |
|---|---|
| 命令 | `dotnet test tools/BattleSim.Tests/BattleSim.Tests.csproj --nologo --logger "trx;LogFileName=a1-1b2ca8e.trx" --results-directory DC_RECON_KIT/m1/REVIEW_1b2ca8e/artifacts/tests`（**无 filter**） |
| 退出码 | **0**（`artifacts/a1-dotnet.log` 末行 `EXIT_CODE=0`） |
| TRX | `artifacts/tests/a1-1b2ca8e.trx` |
| git blob（本跑 TRX） | `4a1739ee6144c0857553a802b6898c586aeb1dfc` |
| RunId | `a457d5a0-4d49-4492-b868-a50fb82349d8` |
| Times | start `2026-09-14T22:00:39.2784084+08:00` → finish `2026-09-14T22:00:43.7135661+08:00` |
| ResultSummary outcome | **Completed** |
| Counters | total=**245** executed=**244** passed=**244** failed=**0** error=0 notExecuted=0 |
| 控制台（同日志） | 失败 0，通过 244，已跳过 1，总计 245 |
| 跳过 | `G2RecheckNaturalContractTests.N01_N02_N03_NaturalPlayContract_DocumentedNotFaked`（Unity 合同 Skip；TRX `NotExecuted`）。**不是核心失败。** |
| 源码身份 | `artifacts/SOURCE_IDENTITY.md`（跑前 HEAD=`1b2ca8e`；战斗树与基线一致） |
| 完整控制台 | `artifacts/a1-dotnet.log` |

## 源码绑定

| 项 | 值 |
|---|---|
| 分支 | `m1-gt6-review` |
| 审查 / 本批基线 | `1b2ca8e4ff56d9f0986e158fc97b50498f735cb3`（`1b2ca8e`） |
| 跑前 HEAD | 同上；`git diff --stat` 对 `Resonance.Battle` + `BattleSim.Tests` 为空 |
| 跑后 | HEAD 仍为 `1b2ca8e`。**另一代理在跑后弄脏了** `BattleReplay.cs`（见 SOURCE_IDENTITY 事后段）。本跑 TRX 已保存。 |
| Unity / A4 | **RAN**（见下；FeverPlay FAIL + fight-1 读回 Match=False） |
| 新可玩构建 | `client/Builds/Win64/` 2026-09-14 WindowsBuild；哈希见 `artifacts/BUILD_HASHES.txt`。旧 `dist/` 不当证据 |

## 历史 a53-gate.trx（矛盾件，不当本跑）

| 项 | 值 |
|---|---|
| 提交内路径（字节未改） | `DC_RECON_KIT/m1/REVIEW_1a75253/artifacts/tests/a53-gate.trx` |
| git blob | `987ffbe9ebfd493370fe20ff4258847819270cd2` |
| RunId | `561a2e62-012e-4021-ba20-6b0b3620f263` |
| Times | 2026-09-14 17:13:14+08 → 17:13:18+08 |
| ResultSummary | **Failed** |
| Counters | total=245 executed=244 **passed=234 failed=10** |
| A53 `CURRENT_STATE`/`REGRESSION_LOG` 声称 | 244 passed / 0 failed / 1 skipped — **与该 TRX 不符**（证据错配） |
| 旁路副本 | `artifacts/historical/a53-gate.trx`（同 blob `987ffbe9`） |

自然游玩 run7 `FAIL BattlePlay`（missing=Tap,Slide）仍是历史基线，**不得改写成通过**。

## 批次判定

| 标志 | 值 |
|---|---|
| A1 | 本跑原始 TRX 已对齐；**不是**把旧 234/10 改写成绿 |
| A2 | `VerificationCatalog.Apply` 绑 `A53_SUB_STRIP_DOT_FLAME`；`StartBattleAt` 预战可玩提示已合入 |
| A3 | `Compute` 读实例 `ActiveStage` |
| A4 | 三组已实跑，不是 NOT_RUN。basic/auto PASS；fever 历史 **FAIL**（`153139` 3× Good）保留 |
| F1 | 读回工厂已恢复 opening growth（MaxHp 2460，`NamedOpeningDiff=null`，`VersionDiff=0`）。fight-1 **仍 Match=False**（中段 Tick/Drive/Events） |
| F2 | **closed as executed**。`20260914T183658` 首行 **PASS**：fight-1 feverEver=True，3×Perfect（DriveBegin 0,0,0），Player FeverTap 0+1，CLEAR!! → NEXT → HOME。历史 `153139`/`171516`/`175019`/`181042` 保留 |
| PATH_A | **NEEDS_FIX**（Fever 烟测已绿；basic fight-1 读回中段仍 Match=False） |
| M1_FIDELITY | **DEFERRED_NOT_REMOVED** |
| G3 | **G3_NOT_STARTED** |

未达 `PATH_A_ENGINEERING_PASS`。A53 已关闭，不重开。

## A4 实跑（2026-09-14）

目录：`artifacts/natural-play/SESSION_RESULTS.md`。指针 `UnityEngine.EventSystem`。run7 历史 FAIL 不改写。

| 项 | 结果 |
|---|---|
| `np.basic.v1` | PASS（Player Slide+Tap p0；NEXT/HOME 分场） |
| `np.fever.v1` | 最新 `183658` **PASS**（Fever+FeverTap Player 0,1；Victory）。历史 FAIL/BLOCKED 行保留 |
| `np.auto.v1` | PASS（HUD Full→Manual） |
| fight-1 读回 | F1 后：`NamedOpeningDiff=null` `VersionDiff=0` MaxHp 2460；**仍 Match=False**（Tick 191≠163，Drive 58≠68，Events 100≠124）。见 `fight1-readback.txt` |
| 录像 | `REVIEW_1b2ca8e/recordings/np-continuous-20260914T143817.mp4` |
| 构建 | exe `82EFD5A8…0305` / App `1996CD7B…2884` / Battle `70CC76F8…82DB` |
