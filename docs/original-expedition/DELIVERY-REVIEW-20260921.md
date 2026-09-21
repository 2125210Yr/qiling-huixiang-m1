# 原创远征 v0.1：本地交付与体验入口

本版是原创占位界面的离线单章原型。固定五人、五场战斗、十二件临时强化、三条构筑、首领阶段与原样重试已经完成实机验证。技术结果不代替玩家对玩法的评价；本版停在用户体验评审阶段。

## 启动

最终本地包：`F:/天命之子/dist/original-expedition-v01/review-7a6ede9b-20260921-r1.zip`。解压后运行 `Player/OriginalExpedition.exe`，不需要安装 Unity 或提供源码目录，也不需要调试参数。已有同版档案会恢复到保存节点；当前个人档案为 C 构筑的首领门前，五人满生命。

ZIP SHA-256：`6c5022a559b875d670efd9b3689d24a3bd94bd97e0adc8fc32f2c4601f3eedf5`。175 个 ZIP 条目含完整170个Player文件及原字节验收摘要，逐项核验通过。[最终打包结果](evidence/o5/package-review-7a6ede9b-20260921-result.json)绑定程序与内容版本；38项技术验收全部PASS，用户体验状态为 `PENDING_USER_REVIEW`。

玩法见 [一页说明](PLAY-GUIDE.md)。默认普攻自动，主动技能由玩家点击；可先暂停安排护盾、治疗、增益和攻击，再恢复按队列执行。

## 真实玩到了哪里

- **A，seed 13486312：**从普通据点开局，A01→R1 A02→R2 A04→工坊休整→R4 A03，五战通关，返回据点、查看上趟回顾，再开新 C。完整连续录像 20 分 16 秒。N5 已持有完整构筑，护盾增加和蓄能生效；A03/A04 的共同释放在 N7，不能把 N5 误标截图当作震荡触发。首领遗物追加有效伤害 24,034。
- **B，seed 8509421：**零主动技能自然败北，普通关闭重开后，冻结开局保持一致；通过不同操作、26 次成功主动获胜。失败与重开后的录像为两个原始片段，边界明确。
- **C，seed 14556671：**普通选择 C01，经 R1 C02、R2 C03、R4 C04 取得全套；N5 三名辅助完成接力和和声，储存强奏，再由下一次主动伤害消费。四场获胜后停在 N6；没有把 C 说成再次通关。

三族共十二件均有普通获取及实际作用证据。新趟清空临时遗物、战斗状态和旧检查点；首通解锁的等价横扫配置、发现记录及上趟摘要保留。没有为录像写入战中生命、充能或胜负。

## 可直接查看的录像

原件保留本地，没有放进 Git 或上传 GitHub：

- [A 完整连续远征](../../dist/original-expedition-v01/full-a-7a6ede9b-20260921-1510/ordinary-full-a.mp4)
- [B 自然败北](../../dist/original-expedition-v01/resume-7a6ede9b-20260921-144630/boss-natural-defeat-before-reopen.mp4)
- [B 重开后原样重试获胜](../../dist/original-expedition-v01/resume-7a6ede9b-20260921-144630/boss-retry-after-reopen.mp4)
- [C 正常获取与强奏链](../../dist/original-expedition-v01/ordinary-c-7a6ede9b-20260921-1526/ordinary-c-acquisition.mp4)

录像均为 Windows.Graphics.Capture 实际画面。旧 GDI 冻结录像保留 INVALID 结论，不拼接或改名充数。C01 的新趟选择和初入 N1 在 A 片末，C 片从同一场暂停状态继续；C 片不冒称另一份完整五战连续录像。

## 代码与验证范围

Player 冻结生产提交：`7a6ede9b643a3470a9f0924d329a5ca769b40ce2`；内容 `original-expedition-content-v0.1.1`，hash `69aaa51d7fa5077a5e1d30b4c5f2fd042b62f932f86d0dec59a037762880fdbd`；回放为 schema 2 / `original-expedition-replay-v2`。后续测试、文档和打包工具提交没有替换这套 Player 源码身份。

核心 .NET：主套件 564 通过、1 个既有占位跳过；已知历史 Catalog 顺序污染例在独立进程 1/1 通过，合计 **565 通过、1 跳过**。Unity Editor 套件 **91/91**，另行实际渲染帧率测试 **1/1**。打包工具新增完整报告校验，提交 `5397ed73`，夹具回归 **38/38**；此数与 MVP 的 38 个验收项目分别统计。

实际渲染对照使用隔离的冻结首领开局，是受控测试。两档实测中位约 29.99 / 116.82 FPS；808 个共同逻辑 tick、230 个读条 tick、129 条伤害事件及终态严格一致。录像容器的 15 fps 不算游戏渲染帧率证据。

同一个 N5 / single / seed 260921 的受控输入策略下，仅遗物不同：无遗物用时 **30.8 秒**，完整 A **22 秒**、B **11.6 秒**、C **19.63 秒**。这是当前通过的核心测试 StdOut 中的固定对照，说明 B 在该场景有明确效率优势，不代表所有敌阵或玩家操作下的排名。[当前核心原始结果](evidence/o5/replay-portability-core-final-20260921.trx)

独立 ZIP 启动已在工程外执行，173 个清单文件全部匹配、无参数恢复节点和详情交互正常、个人主档哈希不变；最终 review 包的 170 个 Player 文件保持相同字节。未进行整机断网实验，没有把这项启动检查写成已覆盖所有设备和离线环境。[启动核验](evidence/o5/standalone-zip-launch-7a6ede9b/README.md)

## 证据与限制

[38 项实际验收报告](ACCEPTANCE-PROGRESS.json)、[A 普通流程](evidence/o5/ordinary-full-a-7a6ede9b/README.md)、[B 原样重试](evidence/o5/boss-retry-7a6ede9b/README.md)、[C 普通获取](evidence/o5/ordinary-c-7a6ede9b/README.md)、[实际帧率](evidence/o5/rendered-fps-20260921-37245ba5/RUN-SUMMARY.md)分别列明原始报告、画面时间点及哈希。原始规格清单保留 NOT_RUN，不改写成执行报告。

本版没有原作美术还原、Live2D、第二章、联网、长期养成或 Jev 战术助手。回放 v1 明确拒绝，不改写旧证据。后续只根据本版实际体验反馈处理具体问题；不自动扩大内容、推送或发布。
