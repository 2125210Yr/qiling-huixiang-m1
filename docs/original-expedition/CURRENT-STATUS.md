# 当前交接：原创远征 v0.1

2026-09-21。主仓库 `F:/天命之子`，分支 `codex/original-expedition-v01`；实际 Unity 工程 `F:/Resonance/client`。**`7a6ede9b` 新包已普通玩到四战胜利、两次首领败北，六份真实回放全部严格 MATCH；但录屏冻结在入口，不能作为流程录像。** 首领胜利、完整五战闭环与失败后成功重试仍未完成。

已保存 O0/O1 入口闭环（`98e2dac0`）、O2 十二遗物及结算（`cc04ca14`）、O3 首领与编排（`a31c47ff`）、O4 真实贡献反馈（`d618adcd`）、O5 回放及打包准备（`1ae47287`）。未推送本模式、未发布。

交付工具、构建资源排除与节点导航修复分别保存在 `e8f0b7f9`／`8075dc35`、`e04e49e9`、`567eec44`；工具原始验证范围见 [离线工具检查点](OFFLINE-TOOLS-CHECKPOINT.md)。最新 `7a6ede9b` 用 IEEE 位模式指纹取代跨运行时不一致的浮点格式化，并使原创自然充能显式以 double 计算、每 tick 写回 float；legacy 充能逻辑不变。

- 当前 .NET 主套件 564 通过、1 个既有普通操作占位跳过；已知旧 Catalog 污染用例复用单独 1/1 的有效结果，合计 **565 通过、1 跳过**。真实 Unity 最终 **91/91、0 跳过**。原件见 `evidence/o5/replay-portability-core-final-20260921.trx`、`fingerprint-catalog-isolated-20260921.trx` 与 `replay-portability-unity-final-20260921.xml`。
- `fbf7ac10` 新增受控实际渲染帧率夹具，仅离线编译退出码0；**Unity运行NOT_RUN**，未设专用环境变量时会Ignore，不计入上述91项，O-023仍PARTIAL。生产源码与当前Player仍为`7a6ede9b`；运行条件见 [帧率验证说明](RENDERED-FRAME-RATE-TEST.md)，编译记录见 `evidence/o5/rendered-fps-offline-compile.json`。
- Unity 生成的 A/B/C 首领磁盘回放由新 .NET CLI 严格核验 **3/3 MATCH**；.NET 自生成三份也 **3/3 MATCH**。它们是工厂开局、合法 Submit/Tick 的受控逻辑夹具，不是普通 UI 录像。根因与完整结果见 `evidence/o5/replay-portability-fix.md`、`replay-v2-portability-results-final.json`。
- 本轮另有六份**真实普通 Player** v2 tape：N1、N2观众席、N4、N5四场 Victory，N7两次 Defeat，全部由新 CLI 从磁盘严格核验 MATCH。B01–B04经普通选择取得并在N5实际发挥；N5散射有效伤害11,177，B04两次回响合计352。独立截图与数据见 [普通试玩证据](evidence/o5/ordinary-7a6ede9b/README.md)。
- 当前内容为 `original-expedition-content-v0.1.1`，内容 SHA-256 为 `69aaa51d7fa5077a5e1d30b4c5f2fd042b62f932f86d0dec59a037762880fdbd`；回放为 Schema 2 / `original-expedition-replay-v2`。新验证器明确拒绝 v1；旧 tape、失败报告和旧包保留，不改写原件。
- `7a6ede9b` 独立物理副本于 12:56 完成真实构建，退出码 0、BuildAudit PASS；499 个源输入复核匹配，170 个 Player 文件与 174 个 ZIP 条目已核验。候选目录为 `dist/original-expedition-v01/candidate-7a6ede9b-20260921`，同名 ZIP 的 SHA-256 为 `5b927cd0819540f02a4b73877bff25375d9a09abac8e332a6025da525cd84e2d`。见 `evidence/o5/package-7a6ede9b-result.json`；`FinalAcceptancePassed=false`。
- 本轮675.8秒GDI录屏虽可解码，抽检停留在入口旧画面，最终 [视觉核验](evidence/o5/ordinary-7a6ede9b/video-visual-validation.json) 判 **`INVALID_STALE_CAPTURE`**，不能证明部分或完整战斗流程。原MP4留在 `dist/original-expedition-v01/play-7a6ede9b-20260921/ordinary-expedition-7a6ede9b.mp4`，SHA为 `61d59cad400a601ea0b242c8e576fb9ae92fa6ca595e3f7a6466be19904ff114`；14张独立Sky/WGC截图及六份真实tape有效，三张贡献截图已逐项对照原始结算，见 `evidence/o5/ordinary-7a6ede9b/ui-tape-crosscheck.json`。旧e04的GDI录像仅保留原件，未重新视觉核验，不用于新版通过结论。
- 用户曾明确恢复操作；本轮首领重试选敌后的暂停再次遭Computer Use物理Esc中止，此后没有再输入桌面。ffmpeg已通过 `q` 正常结束，Player仍运行；最后只读状态为 **BossRetry、revision 31、已结算四场**。两次首领冻结输入除AttemptId外精确相同，但都败北，尚未关闭重开或成功重试。
- 已结束v0.1.0原创profile的隔离副本兼容性检查保留，源字节未改。最新 `evidence/o5/ordinary-7a6ede9b/recording-result.json` 核实个人save.json与备份仍等于开工前哈希；本轮原创档按正常游玩前进到上述检查点。旧素材、旧视频和用户已有暂存内容未清理。

用户重启后 WMI 查询与 Unity 执行均已恢复，见 `evidence/o5/reboot-recovery-20260921.json`。此前许可握手失败仅作为历史记录保留在 `evidence/o5/unity-retry-20260921-0305-result.json`，不再是当前阻塞。

后续顺序与证据位置见 [O5 代码检查点](O5-CODE-CHECKPOINT.md)。[验收进度](ACCEPTANCE-PROGRESS.json) 为 **28 PASS / 10 PARTIAL / 0 NOT_RUN**；O-017以精确截图/结算核对、O-020以已有模拟边界加有效静态预警画面通过。队列和成型后战斗已有真实截图/tape，但所需有效普通片段未取得，O-024/O-036仍PARTIAL；0项NOT_RUN不表示整体验收完成。原始spec清单不变。

下一步恢复桌面操作后，先用WGC/gfxcapture录10秒普通页面变化，对照前后抽帧与新截图确认画面刷新；该新采集尚未执行。预检通过再另存有效录像：补首领关闭重开后同检查点成功重试、完整五战通关及结算新趟；同时补A/C相应UI、不同实际渲染帧率和普通生命周期缺口。旧冻结MP4、操作时间戳或逻辑夹具不替代连续录像。新增真实tapes再独立核验，未变化的测试与已有六份MATCH结果复用。未推送GitHub、未发布。
