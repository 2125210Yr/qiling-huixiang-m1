# 旧档案兼容读回 — 2026-09-20

使用已构建的新 reader 直接重读此前两份 summary 里的全部 9 个档案，没有重建、改原件或放宽比较。

- 冻结源码提交：`672cdff6a7a821347cb14ab75e11760cc1b9fe6d`；运行前后 HEAD 相同。
- Reader SHA-256：`30A6873E54C159A717EC5A85B054051EF0AE3BAEE27A55150C004202AE5732E6`，运行前后相同。
- 结果：**9/9 Match=True、Ok=True、退出 0**；Command、Unconsumed、Digest、Event、Version 差异全部 0。
- 每份档案的全部 7 个文件均记录前后 SHA-256，共 **63/63 不变**。Reader、Program 与相关源码哈希也前后不变，见 `summary.json`。

任务预期其中旧 Fever 首场因缺少终局时钟 boundary 失败，但实际选中的 `np-20260920T071843-001` 并没有终局 Fever 尾段：`fever_end/TimeUp` 在 tick 2263，`result/clear` 在 tick 3268。它的 1141 条事件可以由战斗时钟独立重现，因此兼容通过是正确结果。

此前真正出现终局 `fever_end` 的 879 事件样本 `np-20260920T064437-001` 不属于这 9 个指定档案。本次 9/9 不能被解释为“无 boundary 的旧终局尾段也可验证”。

其中 NEXT 第二场短片段仍仅覆盖开局及暂停，不能据事件读回通过声明第二场完整战斗终局。

命令形式：`dotnet <现有 Readback.dll> <battlePath>`。每份 stdout、stderr、退出码及原件指纹随本目录保存；具体路径、时刻和事件数见 `summary.json`。


## 单独追加的旧终局尾段负向对照

`np-20260920T064437-001` 已使用同一 reader 追加读回，结果符合预期：**Match=False、Ok=False、exit 1**，明确诊断 `TerminalFeverTicks: legacy record has no terminal Fever clock boundary.`。原件无 boundary 行；它的终局后 `fever_end` 缺少独立时钟输入，不能从期望输出反推重放。

该档案预期 879 条事件，实际 878；CommandDiff=1、DigestDiff=3、EventDiff=1，VersionDiff=0、Unconsumed=0。差异为 FeverActive、FeverHitsLeft、事件数及明确的缺边界诊断。没有改原件或降低匹配标准来消除失败。

此负向对照单列于 `terminal-legacy-summary.json` 和 `terminal-legacy-fever-001.*`，不纳入前述原 9 档案通过率。7 个原件文件 SHA 前后完全相同，reader SHA 与前 9 档案运行一致。
