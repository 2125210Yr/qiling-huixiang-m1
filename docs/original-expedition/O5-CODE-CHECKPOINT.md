# O5 回放与交付检查点

2026-09-21。**38 项技术验收全部通过。当前生产源码和 Win64 Player 为 `7a6ede9b643a3470a9f0924d329a5ca769b40ce2`：核心合计 565 通过/1 既有跳过，Unity 套件 91/91，独立实际渲染帧率测试 1/1。B 已普通重开重试获胜；A 五战连续录像与 C 普通全族获取、N5 强奏消费均已独立核验；候选 ZIP 已在工程外实际启动。** 用户体验仍为 `PENDING_USER_REVIEW`，未宣称 `ACCEPTED`。

## 实现与版本

- 回放保存冻结输入及指纹、外部操作意图、实际接受/拒绝命令、独立 EndTick、结算明细、事件与最终状态。暂停队列从意图重建；预期输出不参与开局或时间推进。普通终局核验后以新文件保存至原创 profile 同级 Replays，证据写入失败不阻断进度保存。
- 当前版本用 IEEE 位模式指纹解决跨运行时浮点字符串差异；原创自然充能从除法开始显式使用 double，每 tick 写回 float 一次。legacy 分支不变，根因与历史失败原件见 [回放修复说明](evidence/o5/replay-portability-fix.md)。
- 内容版本 `original-expedition-content-v0.1.1`，内容 SHA-256 `69aaa51d7fa5077a5e1d30b4c5f2fd042b62f932f86d0dec59a037762880fdbd`；Schema 2 / `original-expedition-replay-v2`。v1 在模拟前明确拒绝，不改写旧 tape 求匹配。原创 profile 的 Schema 与完整性算法未变；已结束旧档的隔离副本能保留永久记录并新开，旧活动远征仍按版本检查处理。
- 构建采用物理副本、输入白名单、场景依赖/Resources/packedAssets 审计、许可文本和逐文件 SHA。资源排除与节点滚动修复见 [构建护栏](evidence/o5/build-artifact-fix.md)、[导航修复](evidence/o5/navigation-fix.md)。测试/文档后续提交不代表新的生产二进制。

## 实际验证与证据范围

| 证据 | 结果与范围 |
| --- | --- |
| [核心主套件](evidence/o5/replay-portability-core-final-20260921.trx) | 564 通过、0 失败、1 个既有普通操作占位跳过。 |
| [旧 Catalog 隔离用例](evidence/o5/fingerprint-catalog-isolated-20260921.trx) | 独立进程 1/1；与主套件合计 565 通过、1 跳过，非一次全套数字。该路径未受最后原创充能修改影响，复用有效结果。 |
| [Unity 最终套件](evidence/o5/replay-portability-unity-final-20260921.xml) | 真实 Unity 6000.3.23f1，91/91、0 跳过；含 UI、教程、导航、构建护栏、数值指纹与自然充能回归。 |
| [实际渲染帧率运行](evidence/o5/rendered-fps-20260921-37245ba5/RUN-SUMMARY.md) | 首次 1/1；实际中位约 29.99/116.82 FPS，808 个共同 tick、230 个读条 tick、129 条伤害事件及终态精确一致。与 91 项套件独立计数，是可见 Game View 的受控测试。 |
| [跨运行时三族回放](evidence/o5/replay-v2-portability-results-final.json) | Unity 生成三份与 .NET 自生成三份各 3/3 MATCH。A/B/C 首领 EndTick 1141/1675/1899，工厂冻结构筑、合法 Submit/Tick；不是正常 UI 领取或普通录像。 |
| [旧原创档副本兼容](evidence/o5/profile-v011-copy-compatibility.json) | 已结束旧档在隔离副本新开 v0.1.1，永久记录保留，源字节不变。 |
| [当前构建审计](evidence/o5/build-7a6ede9b-audit.json) | 真正 BuildPlayer exit 0 / BuildAudit PASS；输入/产物由独立物理副本审计。 |
| [候选包结果](evidence/o5/package-7a6ede9b-result.json) | 499 个源输入、170 个 Player 文件、174 个 ZIP 条目已核对。历史候选 FinalAcceptancePassed=false 原件不重写。 |
| [最初普通 B 记录](evidence/o5/ordinary-7a6ede9b/README.md) | N1/N2/N4/N5 四胜与 N7 两败的六份实际 tape 严格 MATCH；14 张独立截图有效，GDI 视频无效。 |
| [B 关闭重开与获胜](evidence/o5/boss-retry-7a6ede9b/README.md) | 新两份实际 tape 严格 MATCH：自然 Defeat tick2070→关闭重开→26 次成功主动 Victory tick2553。两段 WGC 录像完整解码、可见变化；输入仅 AttemptId 不同。 |
| [B 检查点精确比较](evidence/o5/boss-retry-7a6ede9b/reports/checkpoint-profile-comparison.json) | 六份输入仅归一 AttemptId 后 IEEE 指纹完全相同；关闭前后 profile 字节相同。胜利与据点字节一致，首通横扫解锁、摘要和发现保留；legacy 两文件 hash 不变。 |
| [A 五战与队列分析](evidence/o5/ordinary-full-a-7a6ede9b/reports/battle-and-queue-analysis.json) | runSeed13486312，五份实际 tape 严格 MATCH，EndTick685/1320/1452/1809/3331。N1 tick206 的覆盖、清除重加后只执行 [1,4,0,3] 各一次。 |
| [A 胜利及新 C 状态](evidence/o5/ordinary-full-a-7a6ede9b/reports/profile-transition-analysis.json) | 三份 profile 结构/完整性通过；A 胜利 ActiveRun=null，新 C 只有 C01、满开局 HP、无旧检查点，A 摘要及永久记录保留。 |
| [C 获取与实际因果](evidence/o5/ordinary-c-7a6ede9b/README.md) | 四份真实 tape 严格 MATCH，EndTick990/1407/1716/1584；C01–C04 正常取得，N4 升级接力/和声、N5 强奏储存与后续原生消费有 trace 及画面。至 N6，未挑战 C 首领。 |
| [C 视频核验](evidence/o5/ordinary-c-7a6ede9b/reports/video-analysis.json) | 545.466667 秒完整解码、17 帧独立查看；438/452/493 秒对应强奏储存、保留、消费。C 的普通创建和开场位于 A 片末段，明确分段。 |

## 普通取得与实际发挥

B runSeed `8509421` 经普通核心选择及 R1/R2/R4 取得 B01–B04。N5 散射有效伤害 11,177、两次 B04 回响合计 352；后续重开首领获胜中 B03/B04 亦实际触发。其普通首领两段录像与旧冻结 GDI 片分开保留。

A runSeed `13486312` 的路径为 A01→R1 A02→R2 A04→后台/工坊休整→R4 A03。五战均普通胜利。**N5 已完整持有四件且 A02 增盾生效，但没有 A03/A04 衍生结算；N7 的 A04 为 3 个根动作，A03 为 2 个根动作/3 次命中。** 不把截图文件名或操作意图当作触发事实。[A 连续视频归档](evidence/o5/ordinary-full-a-7a6ede9b/README.md)为 20:16.266667，完整解码通过，20 个关键帧均经独立查看；SHA-256 为 `8fbac4be1c7d4780a920e82b94efab00e4e47132e3b7236dabc02b6b22c037da`。时间点与原片身份见 [视频报告](evidence/o5/ordinary-full-a-7a6ede9b/reports/video-analysis.json)。

新 C runSeed `14556671` 从 A 结算后的普通据点建立，C01→R1 C02→R2 C03→R4 C04，工坊休整。N1 有 C01 接力，N2 虽持有 C02 却未触发，N4 才有升级接力及和声的可靠证据。N5 tick436 三辅助使 `ForteStored false→true`，tick832 后续点杀使 `true→false`、C04 触发序号 `0→6`；敌 HP `4036→1462` 与累计敌伤 `10116→12690` 均差 2574。这是该次原生总伤，不全部归为遗物额外贡献；C04 为同通道加成。四份实际 tape 全部严格 MATCH，见 [因果报告](evidence/o5/ordinary-c-7a6ede9b/reports/battle-causal-analysis.json) 与 [N5 输入 trace](evidence/o5/ordinary-c-7a6ede9b/reports/01d69be1b02d4d5cb28c1aaa1ad4e467.input-trace.json)。545.466667 秒视频完整解码、17 帧独立核验；原片 SHA-256 为 `f1d95044a9a0e773288b0b7d414abb95304766eef42a3c57e41fa6ce9c720e1c`。C 开场在 A 视频末段，本片从 N1 暂停衔接，仅到 N6，未称 C 首领通关。

当前 v0.1.1 的固定 N5/single/seed260921 六组对照来自 [最终主套件 TRX](evidence/o5/replay-portability-core-final-20260921.trx) 中 `FixedN5_SixLegalBuildsShareEveryOtherInput_AndActualResultsReconcileWithTheLedger` 的已通过结果及 StdOut：无遗物 924 tick/30.8 秒，A 660/22，B 348/11.6，C 589/19.633333，混搭 544/18.133333，B+C01 345/11.5，均 Victory。输入除遗物外相同，默认暴击及同一策略保持；完整 B 有明确效率优势。旧 [完整对照 JSON](evidence/o4/comparison-n5-single-seed260921.json) 属 v0.1.0 历史材料，不标成当前完整输入原件；固定对照也不等同玩家主观体验。

## 当前交付边界

[38 项矩阵](ACCEPTANCE-PROGRESS.json) 当前 **38 PASS / 0 PARTIAL / 0 NOT_RUN**。新 B/FPS 证据补齐 O-027/O-023；B 普通退出重建、后续交互与已有 UI 测试合用补齐 O-037。A 完整录像与实际回放补齐导航/热键、队列、成型战斗、结算和下一趟，对应 O-002/O-003/O-024/O-029/O-036；三族普通获取及 C 的实际因果补齐 O-015。同版共 17 份不同的普通 Player tape 严格 MATCH。O-038 已有 [工程外解压启动报告](evidence/o5/standalone-zip-launch-7a6ede9b/README.md)：173 个清单文件逐字节一致，加清单共174文件；无参数启动恢复 C 的 N6、详情可交互、主档不变。未做整机断网实验；日志仅有不影响本次操作的 D3D info queue 诊断。

最终 review 包为 `dist/original-expedition-v01/review-7a6ede9b-20260921-r1.zip`，SHA-256 `6c5022a559b875d670efd9b3689d24a3bd94bd97e0adc8fc32f2c4601f3eedf5`，175 个 ZIP 条目。交付工具已绑定相同 SourceCommit/ContentHash 和38项完整技术验收摘要，核对170个Player文件字节不变。摘要原字节SHA-256为 `41cc24734e76a376add797471275e78697becf62eeb2050f4ea38ab5a708b052`，详见 [最终打包结果](evidence/o5/package-review-7a6ede9b-20260921-result.json)。原候选及ZIP保留。当前 `technicalAcceptancePassed=true`，用户体验为 `PENDING_USER_REVIEW`，`finalAcceptancePassed=false`；技术通过不替代用户体验反馈。

旧 GDI 原片 ordinary-expedition-7a6ede9b.mp4 为 675.8 秒，SHA-256 `61d59cad400a601ea0b242c8e576fb9ae92fa6ca595e3f7a6466be19904ff114`；[视觉结论](evidence/o5/ordinary-7a6ede9b/video-visual-validation.json) 仍为 INVALID_STALE_CAPTURE。正常解码不等于有效画面，不拼接旧片补造完整通关。新 WGC 预检和 B 两段视频已验证更新；录屏 15 FPS 与游戏真实渲染帧率分别记账。

原 551/1、75/82/84 项报告，许可诊断，v1 tape、跨运行时失败与旧包继续保留。未变化的有效测试复用，只有实际修复或输入变化才重跑相关检查、重新构建。未删除旧素材、用户存档或已有工作区内容；文档、证据和交付由主集成者统一提交，技术交付后等待用户体验反馈，不自行扩展下一章或额外视频要求。
