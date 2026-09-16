# REVIEW_a82801b / C 批当前结果

记录时刻：2026-09-16。batch_id=`C`。  
本文件只描述**本批在冻结提交 `3d2d1e6` 上的执行**。不是 M1 还原验收，不宣称 T27 / 90% / M1 通过，不开 G3。

审查来件 `REVIEW.md` 对 a82801b 的判定仍是 NEEDS_FIX（C1–C4）。下表是落地后的本跑证据，不把来件改写成已经审查通过。

## 本跑（唯一有效核心计数）

| 项 | 值 |
|---|---|
| 命令 | `dotnet test --nologo --logger "trx;LogFileName=C4-full.trx"`（`tools/BattleSim.Tests`，**无 filter**） |
| 退出码 | **0** |
| TRX | `artifacts/tests/C4-full.trx` |
| RunId | `ca7f974c-d775-4d61-b5bb-7e2cb16ceafd` |
| Times | 2026-09-16 12:55:22+08 → 12:55:33+08 |
| ResultSummary | **Completed** |
| Counters | total=**272** executed=**271** passed=**271** failed=**0** |
| 控制台 | 失败 0，通过 271，已跳过 1，总计 272（`artifacts/tests/C4-full.console.txt`） |
| 跳过 | `N01_N02_N03_NaturalPlayContract_DocumentedNotFaked`（Unity 合同 Skip）。**不是核心失败。** |
| 源码身份 | `artifacts/SOURCE_IDENTITY.md`（HEAD=`3d2d1e67554251b77d05ad732dc06390a114de05`） |

A1 的 `a1-1b2ca8e.trx` 244/0 仍只绑定 `1b2ca8e`，不当本 HEAD。

## C1–C3 源码（已合入 `3d2d1e6`）

| 项 | 落地 |
|---|---|
| C1 | `CloneEffects` → `Catalog.CloneEffect`；`G2C1` 2/2。补丁 `patches/C1-CLONE.md` |
| C2 | Slide/Drive/wave 为核心 tick 预算（1.47/0.70/2.00 DESIGN_PLACEHOLDER）；`HoldSim` setter internal；HUD/WavePreview 只读 `HoldLeftSec`；`TryFocusEnemy` → `Submit(FocusEnemy, Player)`；`ClockKey` 含三字段。`G2C2` 7/7。补丁 `patches/C2-HOLD-FOCUS.md`、`C-MERGE.md` |
| C3 | Capture 拷 `OpeningGrowthInput`/`OpeningMods`；`;r=` Reserve；`openingSource=input\|legacy-recovered\|incomplete`。`G2C3` 2/2。补丁 `patches/C3-OPENING.md` |

## C4 自然操作 / 读回

目录：`artifacts/natural-play/SESSION_RESULTS.md`。指针 `UnityEngine.EventSystem`。`os_touch=NOT_CLAIMED`。run7 历史 FAIL 不改写。

| 项 | 结果 |
|---|---|
| `np.basic.v1` | **PASS**；`np-20260916T050211-001` Victory / `-002` rematch；Player Slide tick 32 + Tap tick 95；`hold begin.slide` / `begin.wave` |
| `np.fever.v1` | **PASS**（首行）；3× Player Perfect；FeverTap Player 槽 1,2；fight-1 随后 **Defeat**（不藏） |
| `np.auto.v1` | **PASS**；`SetAuto` 后 `source=Auto` Tap/Slide，再 Manual |
| `FocusEnemy` | 自然操作 **NOT_OBSERVED_IN_SCENARIO**（场景未点敌）。夹具 C2-T2 已绿 |
| 新 fight-1 读回 | 三条 **Match=True**，`openingSource=input`，Command/Unconsumed/Digest/Event/Version 全 0。`artifacts/readback/np-*-fight1-readback.txt` |
| 旧 1b2ca8e fight-1 | **Match=False** / `legacy-recovered`（要求如此）。`artifacts/readback/fight1-legacy-readback.txt` |
| fight-1 完整性 | 21/21 档案与 `client/captures` 字节相同（`FIGHT1_HASHES.txt`） |
| 录像（C4 首场） | `recordings/np-continuous-20260916T045756.mp4` 37748784 B；**PARTIAL_UNFINALIZED**（无 moov）。保留，不伪造 |
| 录像（可播重录） | `recordings/np-continuous-20260916T085721.mp4` 38797348 B；`ffprobe` duration=451.97s h264 5120×1440@30；`+frag_keyframe+empty_moov`。覆盖 basic/fever/auto 第二场同路径（档案 `20260916T085805` / `090011` / `090337`）。**不是** Match=True 那场的同一条带 |
| 构建 | 首次 **BLOCKED**（GICache junction 目标缺失）。补空目录后干净重打 **PASS**。`artifacts/BUILD_HASHES.txt`：exe stub `82EFD5A8…0305`（与 1b2ca8e 引擎壳相同）；App `42DABD5A…220D39`；Battle `353240AB…D9A6CF1`。mtime 2026-09-16 16:52。`dist/windows` 未哈希 |

## 批次判定

| 标志 | 值 |
|---|---|
| C1 | **closed as executed**（源码 + G2C1） |
| C2 | **closed as executed**（源码 + G2C2；自然操作未覆盖点敌） |
| C3 | **closed as executed**（源码 + G2C3 + 新带 `openingSource=input`） |
| C4-T1 | **RAN**（271/0/1 skip，绑定 `3d2d1e6`） |
| C4-T2 新带读回 | **RAN / Match=True** |
| C4-T2 旧带 | 保留 FAIL |
| C4 构建 | **PASS**（干净重打；见 `BUILD_HASHES.txt`） |
| C4 可播录像 | **RAN**（第二场连续录像可 ffprobe；首场残片保留） |
| PATH_A | **NEEDS_FIX**（C1–C3、新带读回、构建、可播录像已有。剩余：自然操作未点敌 FocusEnemy；可播录像与 Match=True 带不是同一场。不报 `PATH_A_ENGINEERING_PASS`） |
| M1_FIDELITY | **DEFERRED_NOT_REMOVED** |
| G3 | **G3_NOT_STARTED** |

停顿秒数 1.47 / 0.70 / 2.00 是 DESIGN_PLACEHOLDER，不是 GL。
