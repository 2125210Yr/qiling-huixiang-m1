# ZIP 解压后的独立 Player 启动

2026-09-21 实际执行。将候选 ZIP 解压到 `F:/Resonance/OriginalExpeditionReviewRuns/candidate-7a6ede9b-20260921`，该目录位于 Unity 工程 `F:/Resonance/client` 之外。原件保留，未移动或删除源码、旧包或个人存档。

`extraction-verification.json` 记录解压阶段：ZIP SHA-256 为 `5b927cd0819540f02a4b73877bff25375d9a09abac8e332a6025da525cd84e2d`，FILE-HASHES 的 173 个文件全部字节数和 SHA-256 一致，零差异；加清单自身共 174 个文件，其中 170 个属于 Player。EXE SHA-256 为 `82efd5a8c297a5046f6bb72c240dab8972e64003851935eba0a46ad121ce0305`。该阶段报告保留当时的 NOT_YET_RUN，启动结果见独立后续报告。

通过普通标题栏关闭正在运行的候选 Player，确认进程数为 0。随后由桌面工具直接启动解压目录的 `Player/OriginalExpedition.exe`，不带命令行参数、不启动 Unity。`launch-verification.json` 的进程路径及完整命令行确认实际运行的是此解压副本。

亲自检查界面：恢复原 C runSeed **14556671** 的 N6 首领门前，五人满生命、单体配置、当前 C 构筑；点击 C01“查看效果细节”后说明正常展开。三张截图和三条事件记录在本目录。测试没有开始另一场战斗，也没有改写档案；启动前后主档 SHA-256 相同，为 `58a7644d80e733ecd246d6c082d78bea9bfa4a66d097ffd5f13f451a30d15e9b`。完整个人 profile 没有复制到 Git。

Player 日志扫描未发现托管异常；保留一条 Direct3D 诊断 `d3d12: failed to query info queue interface (0x80004002).`，游戏正常渲染和交互。本检查没有禁用整机网络，不能宣称做过断网实验；离线单章行为及资源隔离另由源码/资源审计和普通流程证据覆盖。

此证据证明冻结生产版本 **7a6ede9b** 的真实 ZIP 副本可在工程外无参数启动、恢复及交互。最终 review 包可在确认其 170 个 Player 文件逐字节相同后引用本证据；更新说明和验收报告不冒充另一次程序构建或新一轮完整通关。
