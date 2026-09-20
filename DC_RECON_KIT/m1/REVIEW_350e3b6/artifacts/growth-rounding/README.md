# NEXT 成长舍入修复 — 2026-09-20

修复目标：NEXT 第二场的 C003 在 Unity 中为 HP 3283，.NET 回放却算成 3284，导致 MaxHP、DataIdentity 与开局身份不匹配。以既有 Unity 结果为基准修复计算，不改录制原件和比较标准。

## 根因与改动

同一份旧 Win64 `Resonance.Battle.dll` 分别在 Unity 自带 Mono 与 .NET 6 执行，5,625 组输入中 529 组有属性差异。原因有三处：

1. `BodyMul` 中多个 float 运算的中间精度不同。
2. `AffMul` 与 Bond 倍率组合也存在提前舍入。
3. `Math.Round(int * float)` 的乘积在 .NET 中先舍为 single，Unity Mono 保留更宽的中间值。

`Growth.cs` 现在显式以 double 组成倍率，完成后只转换为 float 一次；属性乘法在乘之前提升为 double，再沿用 `Math.Round` 的到偶数舍入。保留 `0.035f`、`0.02f`、`0.012f`、`0.18f`、`0.01f` 原 float 常量的二进制值，没有换成精确十进制公式。`Bond.cs` 新增 internal 精度路径共享原系数，公开 UI 路径保持原表达式。

没有更改 RulesVersion、存档格式、开局输入恢复或身份检查。该修复使 .NET 与旧 Unity 的成长语义一致；没有为已按错误 .NET 数值生成的任意历史档案添加迁移或放宽校验。

## 验证

| 检查 | 实际结果 | 证据 |
|---|---|---|
| 新回归初次红测 | 4 失败，复现 NEXT 和好感边界 | `rounding-red.log/.trx` |
| 仅提升属性乘积的中间方案 | 2 失败 / 13 通过，说明倍率组成也必须固定；文件名中的 green 不代表通过 | `rounding-green.log/.trx` |
| 加入组合倍率边界后红测 | 4 失败 / 2 通过 | `rounding-coeff-red.log/.trx` |
| 完整修复针对性回归 | 17 通过 / 0 失败 | `rounding-final-green.log/.trx` |
| 无 filter 全量核心测试 | **284 通过 / 0 失败 / 1 既有跳过 / 总数 285** | `rounding-full.log/.trx`、`full-run.json` |
| 候选倍率与旧 Mono | 5,460 种 Body 组合 + 101 档好感，全部逐值相同 | `coefficients-candidate-mono.txt` |
| 新游戏 DLL 跨运行时矩阵 | **5,625 / 5,625 一致，0 差异；与旧 Mono 也 0 差异** | `before-summary.json`、`after-summary.json`、四份运行输出 |
| 7 份既有真实录制读回 | **7/7 Match=True**，所有差异 0；包含两份此前失败的第二场 | `readback/summary.json` 与各档 stdout/exit-code |
| 新 Unity 自然续战 | **PASS**：胜利 → NEXT → 第二场 → 暂停 → 首页；截图显示 C003 HP 3283 | `../natural-play/20260920T080750-np_basic_v1/` |
| 新首场及 NEXT 场读回 | **2/2 Match=True**，事件 132/132 与 8/8，所有差异 0，原件指纹不变 | `readback/fresh-next-summary.json` |
| Win64 新构建 | **成功 / exit 0** | `win64-build.log`、`build-run.json` |

跨运行时矩阵为 25 名角色 × 5 个等级（1/2/20/59/60）× 3 档突破（0/1/6）× 3 档点火（0/1/12）× 5 档好感（0/4/20/60/100），装备统一 SC006。它是代表性属性矩阵；系数比较另外覆盖全部 60×7×13 种 Body 组合与 0–100 好感。未宣称穷尽任意基础属性、全部装备或其他战斗公式。

新增 6 项自动回归固定旧 Unity 的数值，不在测试中重写待测公式。唯一跳过仍是要求实际 Unity 操作的旧合同项；此轮自然操作另有真实证据。测试启用 `RESONANCE_KEEP_TEST_ARTIFACTS=1`，没有批量清理。独立只读审查未发现阻塞问题。

## 重现方式与版本

`GrowthRuntimeProbe.cs` 和 `GrowthMultiplierProbe.cs` 通过 Unity 随附的 `mcs.exe` 编译，引用同目录游戏 DLL；同一探针和 DLL 分别由 Unity 的 `MonoBleedingEdge/bin/mono.exe` 和 `.NET dotnet exec --runtimeconfig GrowthRuntimeProbe.runtimeconfig.json` 执行。探针 exe/DLL 留本机，不纳入 Git。系数初始诊断日志和中间失败保留，不覆盖为最终通过。

- 旧游戏 DLL：`C05D101F9EC0270200B6AE8821A6F89767E511E497A03AC1C3C379A3B8271508`
- 新游戏 DLL：`0258465CAB197DC8D837C61BAA533FFD1FA3D187A869B46BE23F9A2E741923BC`
- `Growth.cs`：`E92CA5973FE65C1F5AF9901E4B5C4A239AEF11A4592052ECCB2EBCE011196010`
- `Bond.cs`：`3AD72CBC3260C205A11A3A7C4D29595FCF406638928FB1FC9785130B6EA05E7E`

新自然操作 session 为 `np-20260920T080808`。`source-hashes.json` 已加入 Growth/Bond，绑定本轮源码；run.json 的 HEAD 是修改前提交，须结合源文件哈希判断受测代码。复用录制器产生的 raw 视频仅留本机，本轮验收依据为实际场景、截图及两场回放；不把未单独验收的视频声明为新的 D3 成片。
