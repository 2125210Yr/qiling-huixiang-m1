# 使用方式

本包为固定提交 2a00445 的只读复查结果，不包含生产代码补丁，不会自动修改仓库。

先读 REVIEW.md。要交给本地执行代理时，将 G2_RECHECK_20260914 放到 DC_RECON_KIT/m1/ 下，再追加 NEXT_GOAL.md 到当前活动 goal。REGRESSIONS.md 的20个条目是待执行设计，不是已跑测试。EVIDENCE.json 记录审查范围与固定版本源码索引。

继续现有 G2 路 A，不重开搜片或模拟器，不开 G3，不降低还原验收标准。最大实际并行与任务唯一所有权同时生效。

## 本批结果骨架（G2R14，2026-09-14 追加）

当前工作树 HEAD=`05a0184`，相对审查基线 `2a00445` 仅为美术增量。本批快照见 `artifacts/CURRENT_STATE.md`；20 条新增回归见 `artifacts/REGRESSION_LOG.md`（全部 NOT_RUN）；延期指针见 `artifacts/deferred.md`。不把上一轮提交内 203/203 或 run7 FAIL 改写为本批验证通过。
