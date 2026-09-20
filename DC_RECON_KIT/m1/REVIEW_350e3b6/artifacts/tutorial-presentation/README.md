# 教程提示避让技能演出 — 2026-09-20

已完成上一轮体验候选第 1 项：普通教程提示会避让 SHOWTIME、Drive QTE、评分、波次提示、暂停和终局；被挡住时保留剩余时间，解除阻挡后恢复。到期遇其他提示不再永久丢失。成功释放 Tap / Slide / Drive 后，撤下准确匹配的过时操作提示，保留 Team HP 等有效信息和 Fever 专属提示链。

最新 Windows 程序：[Resonance.exe](F:/Resonance/client/Builds/Win64/Resonance.exe)，也可使用 [在电脑上看.cmd](F:/天命之子/launch/在电脑上看.cmd)。构建于 **2026-09-20 20:34:48+08** 完成。本轮没有覆盖上一轮独立交付 ZIP 或视频；`G2-PathA-20260920.zip` 仍对应 `672cdff6`，不是本次 UI 更新。

## 实现范围

- `BattleHud` 为延迟提示、技能就绪提示和普通 FeverGauge 提示使用统一展示条件；在帧首、消费 cast 后以及即时演出入口同步隐藏。
- `VfxTipPlate.SetSuppressed` 只控制显示和提示寿命，不改变战斗；恢复保留原渐隐程度。普通 Show 清除旧 Fever session 标志，Begin 和 LateUpdate 都遵守抑制状态。
- 成功己方 cast 按标题和正文准确撤销相关操作教学；不会把 Mona 共用标题下的其他教程误删，也不影响被拒绝输入、敌方技能或基础 Auto。Tap 教学被消费时仍衔接 Keep → TeamHP → Childs 链。
- QTE 分支不再重复调用 TickOverlays，所有 HUD 显示计时由正常 Refresh 每帧推进一次。战斗核心、伤害、输入判定和回放未改。

## 验证

| 验证 | 结果 | 证据 |
|---|---|---|
| 原代码定点红相 | **17 失败 / 3 通过 / 20 总数** | `red-tests.xml`、`red-tests-run.json` |
| 最终 Unity Editor UI 回归 | **27 通过 / 0 失败 / 0 跳过** | `final-tests.xml`、`final-tests-run.json` |
| 完整 HUD 组件渲染 | **6 个场景检查通过**，1080×1920；实际查看提示显示、暂停隐藏、恢复、Slide、QTE、评分 | `render-final-frames/`、`render-final.log` |
| Win64 构建 | **Succeeded / exit 0** | `win64-build.log`、`win64-build-run.json` |
| 构建源码一致性 | 构建前后 **228 项输入、0 差异** | `source-hashes.json`、`verification.json` |
| 核心证据复用 | 83 项核心/测试/内容输入指纹不变，复用此前 **310 通过 / 0 失败 / 1 既有跳过**；本轮未重跑核心全量 | `core-evidence-reuse.json`、`../finite-closeout/full.trx` |
| 独立代码审查 | 未发现阻塞；确认提示修复不新增核心状态写入 | 本次差异审查，测试和渲染单独执行 |

27 项 UI 回归覆盖：可逆抑制与余时、0.1 秒渐隐恢复、新 Begin 不闪现、SHOWTIME / QTE / 暂停 / 终局、零/负延迟重试、实际 Submit 成功的 Tap / Slide / Drive 消费教学、拒绝/敌方/普攻不消费、有效 TeamHP 保留、Fever 链及 session 清理、销毁旧 UI 根后的隔离。

这些是 **Unity EditMode 的真实组件测试及受控渲染夹具**。夹具使用 inactive GameRoot，不进入存档/启动生命周期；为产生演出在夹具中设置充能及 Drive，并指定 QTE 评级。因此它们不是普通玩家输入录像，也不替代原版精确时序验收。本轮没有录制新普通试玩，上一轮视频维持其原版本身份。

两个验证过程中发现的夹具问题已明确处理，原件保留：

- `red.log` / `red-run.json` 是最初编译失败，原因是本地 NUnit 不提供 NonParallelizable，以及渲染夹具错误地将 EnsureStarterKit 当作实例方法；不计为回归红相。修正后才得到 `red-tests.xml` 的 17 个真实失败。
- `green-tests.xml` 是中间 **19/20**：DriveCrush 的运行态延迟 Destroy 在 EditMode 报错。最终测试只精确预期这一条已知消息，不忽略其他日志，仍执行真实 Submit 和 HUD cast 消费。生产逻辑未为测试更改。
- 首次 `frames/06-judge-tip-hidden.png` 尚未执行 QTE 按钮的 LateUpdate 或 Judge 动画首帧，画面仍是 QTE；没有将它当作有效评分画面。最终渲染夹具推进这些实际组件阶段，并检查 Judge 确实存在；最终证据在 `render-final-frames/06-judge-tip-hidden.png`。

## 程序身份

| 文件 | SHA-256 |
|---|---|
| 新 Resonance.App.dll | `B60C9FEBE10CD33995CC68A40A9B0128FEDFC1734A0ADBB04579BAA6A4EC8A08` |
| 未变 Resonance.Battle.dll | `8D87435CA8BE5B8B82F59610C8DE0E407C989BF4CC3172798EC077DA1161A723` |

`source-hashes.json` 在最终 UI 测试和渲染完成后、Win64 构建前生成，基线 HEAD 为 `92929d3f`，包含本轮工作树内容；没有冒称它是测试前冻结。本轮生产代码在最终测试到构建之间未变，源码与图片的具体 SHA 见清单。

本批限教程呈现，不包含 SHOWTIME 角色特写、弧形 HUD 或其他候选。原作还原仍 `DEFERRED_NOT_REMOVED`，G3 未启动，未推送远端。此前 standalone 自然操作脚本在 TapToStage 的限制没有在本批修复。
