# G2_RECHECK_20260914 回归日志

来源：`../REGRESSIONS.md`（20 条反例设计）。  
核心 C# 已按 ID 隔离复跑。Unity 自然流程 / 新构建 / 录像仍未跑。不是 M1 还原验收。

历史自然游玩 run7 FAIL 见 `G2_REVIEW_20260913/artifacts/`，不改写。

| ID | 层 | 状态 | 测试名称 | 实际命令 | 说明 |
|---|---|---|---|---|---|
| N01 | 自然UI | SKIP / Unity NOT_RUN | `G2RecheckNaturalContractTests.N01_N02_N03_NaturalPlayContract_DocumentedNotFaked` | `dotnet test --filter FullyQualifiedName~G2RecheckNatural` | EventSystem 合同已写；Editor 未跑 |
| N02 | 自然UI | SKIP / Unity NOT_RUN | 同上 | 同上 | Fever 必达写在 `np.fever.v1`；Editor 未跑 |
| N03 | 自然UI | SKIP / Unity NOT_RUN | 同上 | 同上 | Auto 场景已写；Editor 未跑 |
| N04 | 参数/流程 | PASS | `G2RecheckDriveGainTests.N04_AutoAttack_HonorsDeclaredDriveGainBelow14` | `dotnet test --filter FullyQualifiedName~G2RecheckDriveGainTests` | 声明/overlay DriveGain=5，不再被 14 地板盖住 |
| L01 | 证据 | FAIL | `G2RecheckEvidenceContractTests.L01_TwoBattleIds_FirstFightEventsSurviveNext` | `dotnet test --filter FullyQualifiedName~G2RecheckEvidence` | Battle 程序集无 `BattleEvidence` 存储；Unity 分场落盘测不到 |
| L02 | 证据 | FAIL | `G2RecheckEvidenceContractTests.L02_EachBattle_HasFrozenHeaderCommandsAndExitSnapshot` | 同上 | 同上 |
| E01 | 核心技能链 | PASS | `G2RecheckEffectPathTests.E01_ChargeSpeed_AllAllies_ViaSkillDefSubmit` | `dotnet test --filter FullyQualifiedName~G2RecheckEffectPathTests` | Submit Slide + AllAllies，敌方不加速 |
| E02 | 核心技能链 | PASS | `E02_ChargeAmount_AllAllies_ViaSkillDefSubmit` / `E02_Barrier_AllAllies_ViaSkillDefSubmit` | 同上 | ChargeAmount / Barrier 只打己方 |
| E03 | 敌方策略 | PASS | `E03_EnemySilence_BlocksTapSlide_AutoAttackStillRuns` | 同上 | 沉默挡声明技，普攻仍跑 |
| V01 | 导入/活动门禁 | PASS | `G2RecheckImportGateTests` V01 | `dotnet test --filter FullyQualifiedName~G2RecheckImportGateTests` | 新 Reflect 进不了可玩表 |
| V02 | 导入原子性 | PASS | V02（需隔离，勿与 `BuildBuiltin` 并行） | 同上 | 失败 Load 后 A 仍在 |
| V03 | 依赖校验 | PASS | V03 | 同上 | 候选 effects 表，不偷读旧 Catalog |
| R01 | 实际回放器 | PASS | `G2RecheckReplayContractTests.R01_AutoFever_ReplayedOnce_ViaBattleReplayer` | `dotnet test --filter FullyQualifiedName~G2RecheckReplayContractTests` | **必须隔离**；与 catalog 变异并行会污染静态 `Divergences` |
| R02 | 实际回放器 | PASS | `R02_Speed3AutoFull_StartThenChange_HeaderNotInferredAs1Manual` | 同上 | 首次 Tick 冻结开局 Speed/Auto |
| R03 | 回放判定 | PASS | `R03_ReplayReject_ForcesMatchFalse_EvenIfHpMatches` | 同上 | Match=false 且 Diff 含 seq/reject |
| R04 | 数据身份 | PASS | `R04_HashChanges_WhenHpFlatPowerTriggerOrStageChange` | 同上 | `ContentFingerprint` → `g2id1-` + `BattleContentIdentity` |
| Q01 | 核心时钟 | PASS | `G2RecheckQteClockTests` Q01 | `dotnet test --filter FullyQualifiedName~G2RecheckQteClockTests` | 恢复 tick TimeLeft 只扣一次 |
| Q02 | 核心时钟 | PASS | Q02 | 同上 | speed3 分政策 |
| P01 | 数值证据 | PASS | `G2RecheckProvenanceTests.P01_DesignPlaceholderDrive_ComputedTrue_MeasuredFalse` | `dotnet test --filter FullyQualifiedName~G2RecheckProvenanceTests` | Measured ≠ Computed |
| P02 | 数值接口 | PASS | P02 | 同上 | Bounds.RequireInt 无政策则抛 |

既有 `G2ReviewReplay*` 隔离 **9/9 PASS**；其余 `G2Review*` **39/39 PASS**。与 G2Recheck catalog 变异同进程并行时，回放 `DataIdentity` / 静态 `Divergences` 会串，不作为失败依据。
