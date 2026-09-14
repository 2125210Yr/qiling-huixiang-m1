# G2 建议新增回归矩阵

状态：**测试设计，尚未在本审查环境执行**。以下名称是建议新增的测试，不代表仓库已有或已失败的测试用例。

固定审查 SHA：`ed7a9ba6423f79aee9a4276f9a502c4e6d9cb6f3`。

这些测试检查工程契约与预览政策，不自动证明最终国际服还原。原作未知项保持延期；每项新政策必须显式标注设计/候选/已验证范围。

| ID | 建议测试 | 操作/夹具 | 本批工程断言 |
|---|---|---|---|
| N01 | NaturalPlay_UsesVisibleUiOnly | 固定玩家可见测试关；通过指针实际操作界面 | 进入、作战、结算、返回/再战；无运行中状态注入 |
| N02 | NaturalPlay_MissingRequiredPhaseFails | 故意使编队/换波/结算按钮不可用 | 测试失败，不跳过后整体 PASS |
| N03 | PointerSlide_DoesNotAlsoTap | 实际 down/drag/up，包含阈值边界与取消 | 单个手势只产生被接受的对应命令 |
| N04 | NaturalAndGalleryResultsAreSeparate | 同次构建运行两种测试 | 展示测试 PASS 不代替自然测试结果 |
| F01 | ManualFever_RequiresInput | 手动模式开启固定 Fever 夹具，不发送输入 | 无玩家 Fever 命令就不产生玩家 Fever 命中；政策标为工程预览 |
| F02 | ManualFever_UsesSelectedCaster | 点击不同可用角色；首位角色仍存活 | 日志的出手角色与输入选择一致 |
| F03 | Fever_WindowAndBudgetAreConsistent | 用显式预算/时限/节流运行；分别耗尽预算和时间 | 结束原因和显示剩余值一致；无未声明的较早终止 |
| F04 | AutoFever_UsesSameCommandPath | 切自动策略并暂停/恢复 | 自动输入受同一规则验证，暂停/终局不继续结算 |
| Q01 | Qte_OneAuthoritativeClock | 速度1/2/3、30/60/120展示帧率 | HUD读取同一QTE状态，不产生第二次独立超时 |
| Q02 | Qte_StageClockRespectsItsPolicy | StageCountdownScalesWithSpeed=false，QTE缩放=true | 关卡计时不被QTE缩放政策覆盖 |
| Q03 | ResolveDrive_RejectsTerminalBattle | 核心夹具设置待决Drive后进入终局，再送迟到回调 | 返回拒绝；HP、Drive、Fever、奖励无新增变化 |
| Q04 | PauseAndCancelledQteHaveNoLateSideEffects | QTE中暂停/关闭/切页/终局 | 无重复评价、不可见交互层、迟到伤害 |
| R01 | SeededRandom_StillPermitsCriticalHits | 固定非零暴击概率的设计夹具，运行受控多次判定 | 使用随机流而非因可复现标记强制全部不暴击 |
| R02 | SeedAndCommands_ReplayStateAndEvents | 同seed、初始数据和tick命令跑两次 | 最终状态与事件记录符合约定精度一致 |
| R03 | ForceNoCrit_IsFixtureOnly | 正常预览入口与公式夹具入口分别启动 | 仅显式夹具关闭暴击，正常预览不隐式关闭 |
| E01 | UnsupportedEffectKindRejectedBeforePlay | Silence/ChargeSpeed等当前未支持的语义组合 | 实现且可测，或导入时报未支持；不接受后无效 |
| E02 | ShieldExpiry_UsesDeclaredLifetime | 限时护盾，关闭普通伤害干扰；到期后再受击 | 数值副作用与声明寿命一致；不只删除图标 |
| E03 | StackCapRefreshAndReplacement | 对声明可堆叠的效果重复施加、换来源/强度 | MaxStack/刷新/替换政策真实生效，未支持则拒绝 |
| E04 | DotTriggers_AreEffectSpecific | 分别配置行动型、受击型、周期型测试效果 | 不因全部映射同一opcode而丢失触发语义 |
| P01 | ComputedValueDoesNotImplyEmpiricalEvidence | 对设计Drive和历史Tap分别执行计算 | 可计算性与证据类型分开；未知不变Measured真值 |
| P02 | LegacyKrSlide_MatchesDeclaredCandidate | 与原参照器的同输入/同分支/同取整口径比较 | 有声明的KR衰减，或明确该分支未支持 |
| P03 | PierceCandidate_RejectsUnsupportedDomain | 历史候选防御值超声明域 | 拒绝外推或显式设计扩展，不标历史已验证 |
| U01 | RepeatedBattleEntry_ReleasesUiState | 连续进入、暂停、QTE、结算、重进 | 不残留遮挡、输入订阅、角色展示和VFX实例 |

注意：核心单元测试允许控制初始状态和构造终局边界；“无注入”约束用于 N01 自然界面流程。不能为了禁止测试夹具而失去覆盖边界的能力，也不能把夹具注入带回自然流程。

记录实际测试结论时使用 `PASS / FAIL / NOT_RUN / BLOCKED`，另列证据层级 `ENGINEERING / HISTORICAL_CANDIDATE / FIDELITY_DEFERRED`。每个结果绑定具体提交和原始日志。
