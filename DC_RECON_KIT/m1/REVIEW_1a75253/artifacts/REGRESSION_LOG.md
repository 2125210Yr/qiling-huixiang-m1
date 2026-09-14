# REVIEW_1a75253 / A53 回归日志

来源：`../REPRO_CASES.md`（A53-T01…T08）+ 既有矩阵 ID。  
不是 M1 还原验收。不抄写历史 203/203，不把 `G2_RECHECK_20260914` 隔离 PASS 写成 A53 结论。

历史自然游玩 run7 FAIL 见 `G2_REVIEW_20260913/artifacts/`，不改写。  
前批隔离复跑摘要见 `G2_RECHECK_20260914/artifacts/REGRESSION_LOG.md`（**HISTORICAL**）。

本批无 filter 核心套件：`dotnet test tools/BattleSim.Tests/BattleSim.Tests.csproj`  
TRX：`artifacts/tests/a53-gate.trx`（244 passed / 0 failed / 1 skipped / 245 total）。  
更早的 `artifacts/tests/a53-qa.trx` 是门禁合入前的 QA 红测（T01/T02 故意红），不要当本表通过证据。

## 本批（A53）

| ID | 层 | 状态 | 测试名称 | 实际命令 | 说明 |
|---|---|---|---|---|---|
| A53-T01 | 核心门禁 | PASS | `G2A53PlayableGateTests.T01_DamageSkill_UnsupportedStatusApplyReflect_SubmitMustNotAcceptSpendDamageAndSkipFx` | 无 filter `dotnet test` → `a53-gate.trx` | Submit 拒不支持 Reflect；Charge/Drive/HP 不变 |
| A53-T02 | 核心门禁 | PASS | `G2A53PlayableGateTests.T02_OverlayExistingAtkUpToReflect_ReferencingSkillsNotSilentlyPlayable` | 同上 | 历史 `atk_up` id 不是可玩豁免 |
| A53-T03 | 数据身份 | PASS | `G2A53IdentityTests.T03_*` | 同上 | Side/HasTarget/Target/Opcode/Group/SourceTier/Element/AutoSkillId/FlatHeal/HonorDeclared 任一变则身份变 |
| A53-T04 | JSON 解析 | PASS | `G2A53JsonHasTargetTests` | 同上 | hasTarget false/true/缺键三分 |
| A53-T05 | 证据落盘 | PASS | `G2A53EvidenceTests` | 同上 | 真 `NaturalPlayBattleEvidence` 两场写读 |
| A53-T06 | 验收对接 | PASS | `G2A53EvidenceTests` + 改写后的 L01/L02 | 同上 | 测现有记录器，不另造存储 |
| A53-T07 | 套件隔离 | PASS | `G2A53AssemblyIsolation` + 无 filter 全量 | 同上 | 程序集串行；`G2RecheckCatalog` 集合 |
| A53-T08 | 自然UI | NOT_RUN | — | — | Unity basic/fever/auto + NEXT/HOME 分场仍未跑 |

## 同 TRX 内执行的既有 ID（不是前批迁写）

下列在本批无 filter 套件里实际跑过。N01–N03 仍是 Skip（Unity）。不要把这几行当成 T08 通过。

| ID | 层 | 状态 | 测试名称 | 实际命令 | 说明 |
|---|---|---|---|---|---|
| N01 | 自然UI | Skip | `G2RecheckNaturalContractTests.N01_N02_N03_NaturalPlayContract_DocumentedNotFaked` | `a53-gate.trx` | 合同文档 Skip；T08 仍 NOT_RUN |
| N02 | 自然UI | Skip | 同上 | 同上 | Fever 两槽仍待 T08 |
| N03 | 自然UI | Skip | 同上 | 同上 | Auto 场景仍待 T08 |
| L01 | 证据 | PASS | `G2RecheckEvidenceContractTests`（typed `NaturalPlayBattleEvidence`） | 同上 | 不再 `FindStoreType` |
| L02 | 证据 | PASS | 同上 | 同上 | 同上 |
| E01–E03 / V01–V03 / R01–R04 / Q01–Q02 / P01–P02 | 核心 | PASS | 各 G2Review / G2Recheck 对应测 | 同上 | 本批无 filter 复跑通过；不是 `G2_RECHECK_20260914` 迁写 |

`C001_slide`+`dot_flame` 仍在库存、不在 PlayableSkills。默认纵切要打 Slide 的旧测绑定了命名替代 `A53_SUB_STRIP_DOT_FLAME`（只清链接，不实现 Dot）。
