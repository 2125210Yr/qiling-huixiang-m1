# 原创远征：范围变更

生效规格：[ORIGINAL-EXPEDITION-MVP v0.1](spec/01_MVP_SPEC.md)，用户于本任务授权实施。四份输入文件已按随包 MANIFEST.sha256 核验，原件保留于 `修改/Original_Expedition_MVP_v0_1_Astra.zip`。

- 目标改为固定五人、一章八节点五场战斗、十二件临时强化、三个家族、双阶段首领、首领前原样重试及等价永久配置解锁。
- 替代旧 `G2-EX02-SHOWTIME-CASTER-FOCUS` 单项工单；原作 M1/G3 保真闸门不再阻塞本模式。历史资料保留，不追認旧保真验收通过。
- 不扩展第二章、永久数值养成、Live2D、通用技能引擎、联网或模型服务。继续使用现有 Unity 工程。
- 原个人 `save.json` 不读写；新模式使用独立 profile。既有未提交素材和历史证据不改动。
- 依次交付 O0—O5；真实测试、受控夹具与普通实玩分别记录。用户体验验收由用户决定。

```text
LEGACY_DC_FIDELITY = ARCHIVED_UNACCEPTED
LEGACY_PATH_A_ENGINEERING = PASS_WITH_RECORDED_LIMITATIONS（历史）
ACTIVE_PRODUCT = ORIGINAL_CHAPTER_EXPEDITION_RPG
ORIGINAL_MVP = IN_PROGRESS
```

接手实际本地与远端基线均为 `8e56ee7c562c55ea336e3402f8c8c85d8c0223f4`，来源分支 `m1-gt6-review`。实现分支 `codex/original-expedition-v01`。Git 根为 `F:/天命之子`，其隐藏 `client` junction 指向既有工程 `F:/Resonance/client`；Unity 版本 `6000.3.23f1`。不升级工具链，不自行推送或发布本次新模式。

原始验收清单保留 [spec/02_ACCEPTANCE_MATRIX.json](spec/02_ACCEPTANCE_MATRIX.json)；执行结果另存，未经实际执行的条目保持 NOT_RUN。旧 310/0/1 和 27 项 UI 结果仅是基线历史，不是原创模式新验证。
