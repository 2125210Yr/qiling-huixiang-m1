# BASELINE_LOGS

采集日：2026-09-09 起。**PlayMode = FAILED**（2026-09-10 `M1-G2-PLAY-RECORD`）。不是 NOT_RUN。

本轮 Editor PlayMode 原始产物在 `DC_RECON_KIT/m1/G0/BASELINE_LOGS/` 与 `docs/reference/gl-shutdown-pve/our_slice/`（**我方切片，不是原作 GT**）。已读 `vs-smoke.result.txt`：`FAIL drive never fired`。截图已目视。详见 `BASELINE_LOGS/RESULT.md`。

## 静态基线（给定，未复跑）

```
unity_project      = client/
unity_version      = 6000.3.23f1
render_pipeline    = URP
battle_core        = BattleSim
tick               = 30Hz
turn_based         = false
slide_cd           = MISSING
formation_size     = HARDCODED_5
poison             = NOT_EXECUTED
unknown_opcode     = SILENT
core_decision      = REUSE+REPAIR / DO_NOT_REPLACE
playmode           = FAILED  (2026-09-10 Editor VS Smoke; see BASELINE_LOGS/RESULT.md)
```

## 运行证据

| 项 | 路径 | 状态 |
|---|---|---|
| EditMode 结果 | — | NOT_RUN |
| PlayMode 结果 | `BASELINE_LOGS/vs-smoke.result.txt` + `editor-playmode-20260910.log` + png | FAILED |
| 战斗回放 log | — | NOT_CAPTURED |
| 对照录屏 | primary GT | SEARCH_IN_PROGRESS |
| 旧录像 | 见 REFERENCE_INDEX | 非普通战斗唯一真值 |

缺运行日志不视为通过，也不视为编译失败。`U013` 本轮改为 FAILED（有产物，结果是 FAIL；仍 UNKNOWN，不是还原通过）。

## 2026-09-10 逻辑测试（非 PlayMode）

```
dotnet test tools/BattleSim.Tests/BattleSim.Tests.csproj --nologo --verbosity minimal
已通过! - 失败:     0，通过:   103，已跳过:     0，总计:   103，持续时间: 159 ms - BattleSim.Tests.dll (net6.0)
```

PlayMode 当时 `NOT_RUN`。`M1-G2-PLAY-RECORD` 已跑 Editor VS Smoke，**FAILED**，`U013` 改为 FAILED（仍 UNKNOWN，不是还原通过）。

