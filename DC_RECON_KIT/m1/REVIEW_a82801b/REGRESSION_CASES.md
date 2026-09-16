# 有限回归：C 批已在 `3d2d1e6` 上执行

来件设计表保留。执行结果见 `artifacts/CURRENT_STATE.md` / `REGRESSION_LOG.md` / `natural-play/SESSION_RESULTS.md`。下表状态改为本批落地，不再写 NOT_RUN_BY_REVIEWER。

| ID | 条件 / 操作 | 必须断言 | 本批 |
|---|---|---|
| C1-T1 | 保留 builtin EffectDef；运行实际 VerificationCatalog.Apply，再读克隆后的定义 | Side/HasTarget/Target/Opcode/Kind/触发/堆叠等非具名替代字段不变；原件不被改写；新增非默认 HasTarget 的克隆案例受覆盖 | **Passed** `G2C1CloneTargetTests` |
| C1-T2 | 在合法验证阵容中经完整命令入口施放 C001 Drive，以及声明 Self 的嘲讽 | 存活己方获得声明增益、敌方不被意外加攻；嘲讽作用于施法者；不要直接对指定单位 ApplyStatus 代替施放链 | **Passed** 同文件 |
| C2-T1 | 同种子/配置/输入，实际 UI Slide 后继续攻击；另在无 HUD 的新模拟中读回 | 固定停顿区间、关卡时间、自动攻击时刻、最终状态和事件顺序一致；真实 HUD 帧率差异不反向改变模拟停顿政策 | **Passed** `G2C2HoldPolicyTests`；新 basic 带读回 Match=True；旧带仍 Match=False |
| C2-T2 | 真实指针选中敌方槽位后 Tap，随后保存并读回 | CommandLog 有对应 Player FocusEnemy；重复回放的聚焦/命中目标一致；拒绝输入也有记录 | **Passed** 夹具 `G2C2FocusCommandTests`。自然操作 **FocusEnemy=NOT_OBSERVED_IN_SCENARIO** |
| C3-T1 | 相同属性分别使用 SSSSS、TTTTT 预约；开战前捕获，Format/Parse，构建新 sim | 两份预约完整保留且不能被属性逆推混为一份；两条自动序列各自与原件一致；使用已支持的合成技能隔离未实现效果 | **Passed** `G2C3OpeningInputTests` |
| C3-T2 | 实际输入含等级/装备/强化/预约/本场修正；冻结后修改外部原输入对象 | 已冻结输入不被外部修改；保存/读回使用原始开局；无法恢复旧带时明确 incomplete，不静默退 Level=1 | **Passed** 同文件；新带 `openingSource=input` |
| C4-T1 | 冻结源码之后从完整测试入口无 filter 运行 | 原始 TRX、实际退出码、控制台计数一致且绑定被测版本；一项 Unity 合同 Skip 单独说明，不冒充自然运行通过 | **RAN** `C4-full.trx` 271/0/1 skip @ `3d2d1e6` |
| C4-T2 | 新版本各跑 basic、手动 fever、auto，保存各场原件，NEXT/HOME；从文件创建新模拟并 Verify | basic 有 Player Tap/Slide；Fever 有至少两槽 Player 输入；auto 有真实切换/自动命令；各自文件归属明确、回放 Match=True，各差异为零；对应构建/录像可审查 | 三组 PASS + 读回 Match=True。构建/可播录像见 CURRENT_STATE |

旧 basic 读回失败（第12事件 tick69 对40）可用于定位，但其缺失的外部停顿不能事后凭猜测写回。修复后应重新录同一路径；保留旧结果及其年代/缺失标签。

禁止为了放行删除失败断言、把必达动作改成可选、在 Unity 测试中途注入状态、扩造第二套记录系统或用新建自制回放器避开实际 BattleReplayer。
