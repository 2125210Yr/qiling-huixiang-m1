# 独立回放结束边界

修复 `38629718` 中由预期 FinalDigest 反向决定终局 Fever 推进的问题。现代录制独立保存 BattleTickIndex、TerminalFeverTicks，并在 Submit 时为终局命令采样 TerminalFeverTick。重放按真实记录的 tick 与偏移执行，再独立比较 digest / events；调速、Auto 和收尾时钟保留原先交错。

终局只运行既有 TickFeverOnly；没有修改伤害、结算或奖励实现。缺边界且仍涉及终局 Fever 的历史档案明确报告信息不足，不修改原件、不从答案反推。负值、无效/倒序/超界偏移、重复或缺字段边界均明确失败；存在 BoundaryError 的记录拒绝重新序列化，不能清除诊断后冒充有效记录。

证据按执行顺序保留：

| 文件前缀 | 结果及用途 |
|---|---|
| boundary-red | 2 失败 / 3 通过；部分收尾时间丢失、预期 FeverActive 改变模拟 |
| boundary-green | 第一版独立时钟，15 通过 |
| boundary-related-green | 第一版相关回归，37 通过 |
| boundary-order-red | 2 失败；预期 TickIndex 改变模拟、调速与收尾时钟次序丢失 |
| boundary-order-green | **中间失败结果：26 通过 / 1 失败**；不是最终绿灯，测试比较范围还混入了初始化头命令 |
| boundary-order-related-green | 修正比较范围后 49 通过，覆盖 27 个边界案例与 22 个既有相关案例 |
| boundary-duplicate-red | 1 失败；重复边界经重新序列化被消除 |
| boundary-final-green | 最后增加序列化拒绝后 **28 通过 / 0 失败** |

最终四个源码/测试文件的 diff --check 通过。独立只读复审核对了正常与异常边界、三种 Auto 下无额外伤害/重复 result、以及记录深拷贝和序列化，未发现剩余阻塞。全量、实际构建和独立试玩证据由上级 finite-closeout 目录记录。
