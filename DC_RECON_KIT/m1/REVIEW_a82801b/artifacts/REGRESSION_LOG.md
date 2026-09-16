# REVIEW_a82801b / C 批回归日志

计数只认本批冻结树上的 `artifacts/tests/C4-full.trx` `<ResultSummary>`，不发明，不把 A1 的 244/0 冒充本 HEAD。

命令（无 filter）：

```
cd tools/BattleSim.Tests
dotnet test --nologo --logger "trx;LogFileName=C4-full.trx"
```

| 本跑 TRX 字段 | 值 |
|---|---|
| outcome | Completed |
| total | 272 |
| executed | 271 |
| passed | 271 |
| failed | 0 |
| 退出码 | 0 |
| RunId | `ca7f974c-d775-4d61-b5bb-7e2cb16ceafd` |
| Times | 2026-09-16 12:55:22+08 → 12:55:33+08 |
| 控制台 | `artifacts/tests/C4-full.console.txt`：失败 0，通过 271，已跳过 1，总计 272 |
| 跳过 | `G2RecheckNaturalContractTests.N01_N02_N03_NaturalPlayContract_DocumentedNotFaked`（Unity 合同 Skip）。**不是**把 Editor 自然操作写成这三行 PASS。 |
| 被测 HEAD | `3d2d1e67554251b77d05ad732dc06390a114de05` |

合并后第一次无 filter 曾 3 失败 / 268 通过（pre-C2 测试未排空 policy hold）。按 `patches/C-MERGE.md` 改那三测后再跑，即上表。单次保留，无隐藏重跑。

## C1–C3 合同（核心套件，本跑 TRX 内 Passed）

| ID | 测试 | 本跑 |
|---|---|---|
| C1-T1 / C1-T2 | `G2C1CloneTargetTests`（2） | Passed |
| C2-T1 / C2-T1b / Legacy | `G2C2HoldPolicyTests` | Passed（旧 1b2ca8e basic 带 **VerifyMatchFalse** 锁住） |
| C2-T2 | `G2C2FocusCommandTests` | Passed（夹具 Submit FocusEnemy；自然操作未点敌） |
| C3-T1 / C3-T2 | `G2C3OpeningInputTests` | Passed |

历史 a53-gate 十项失败：本跑 failed=0，未复发。不改旧 TRX。

## C4 自然操作 / 读回

详见 `artifacts/natural-play/SESSION_RESULTS.md`。

| ID | 层 | 状态 | 说明 |
|---|---|---|---|
| C4-T1 | 核心套件 | **RAN** | 上表 271/0/1 skip，绑定 `3d2d1e6` |
| C4-T2 basic | 自然 UI | **PASS** | `20260916T050046-np_basic_v1`；Player Slide+Tap；读回 Match=True |
| C4-T2 fever | 自然 UI | **PASS** | `20260916T050523-np_fever_v1`；Player FeverTap 1,2；fight-1 随后 Defeat；读回 Match=True |
| C4-T2 auto | 自然 UI | **PASS** | `20260916T050949-np_auto_v1`；HUD Auto→Manual + source=Auto；读回 Match=True |
| C4-T2 旧带 | 文件回放 | **FAIL（保留）** | 1b2ca8e `np-20260914T152600-001` Match=False / `legacy-recovered` |
| C4-T2 录像 | 交付 | **RAN（可播）** | 第二场 `np-continuous-20260916T085721.mp4` 451.97s 可 ffprobe。首场无 moov 残片保留 |
| C4-T2 构建 | 交付 | **PASS** 干净重打 `3d2d1e6`；`BUILD_HASHES.txt`。首次 GICache BLOCKED 日志保留 |

run7 `FAIL BattlePlay` 仍为历史 FAIL，不改写。
