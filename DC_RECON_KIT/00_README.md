# Destiny Child 高相似度复刻执行包
研究日期：2026-09-09。适用：已经存在的 Unity 项目；用户负责角色立绘、具体技能内容和文案。

## 先读这个结论
继续用现有 Unity，不先推倒重写。目标是复原玩家能观察到的规则、时间、界面与反馈，不是强行复制原作内部架构。第一交付物应当是可对照原作的战斗切片，而不是更多菜单。

本包包含研究、历史数值候选、校准方法、分阶段 /goal 工单、8 个自定义子代理模板、记录模板，以及一个标准库 Python 数值参照器。它不是已完成的 Unity 游戏，也不是从原作提取出的源码。

## 本次已经做过 / 尚未做过
已经查阅官方游戏指南、官方技术说明、原始玩家实验、公开仓库、Codex 当前文档；已经生成并测试本包 Python 公式参照器。
尚未读取你的 Unity 工程；未运行你的游戏；没有对指定服别的完整录屏逐帧测量；没有验证停服前最终版全套伤害公式。测试通过只能证明参照器按文档计算，不能证明与原作一致。

## 如何交给本地 Codex
1. 把整个 `DC_RECON_KIT` 文件夹放在 Unity 项目根目录，与 `Assets`、`Packages`、`ProjectSettings` 同级。
2. 先保留现有 `AGENTS.md` 和 `.codex` 配置。阅读 `AGENTS_APPEND.md` 后合并规则，不覆盖原文件。
3. `codex_templates` 是配置样例，不会自动安装。根据本地 Codex 实际版本合并配置，把需要的 agent TOML 放入项目 `.codex/agents/`。不要重复创建已有 `[agents]` 表。
4. 在该项目启动 Codex，使用 `05_GOAL_PROMPTS.md` 的第一条启动命令。不会自动启动后续里程碑，前一个验收后再执行下一条。
5. 角色参考、录屏、已有 APK/纪念版截图可放入用户自选的本地资料目录；不要把隐私账户数据或原作资产提交公开仓库。

没有确认 KR/JP/GL 时，先做工程审计和资料整理。不能把“韩国游戏”当成“韩服目标”。无法确认某项时填 UNKNOWN，而不是选一个看起来顺眼的数字。

## 阅读顺序
- `01_RESEARCH_AND_SPEC.md`：复刻边界、架构、模式与 UI/动效。
- `02_NUMERICS_AND_CALIBRATION.md`：有来源的历史候选与不确定项。
- `03_MULTIAGENT_AND_MILESTONES.md`：角色、文件所有权、阶段依赖。
- `04_ACCEPTANCE_AND_TESTS.md`：怎样证明像原作，而不只是能运行。
- `05_GOAL_PROMPTS.md`：直接执行的目标工单。
- `sources.json`：S01–S31 的真实网址及证据限制。

## 运行本包自检
在 Python 3.10+ 环境执行：
```sh
python -m unittest discover -s DC_RECON_KIT/tools -p 'test_*.py' -v
```
只有 Python 标准库，不需模型、密钥或网络。TOML 模板语法审计使用 Python 3.11+ 的 `tomllib`，不代表 Codex 已加载成功。

## 三条红线
- 不能用“历史日服候选公式”冒充“2023 国际服官方公式”。
- 不能用一场五人战斗的高分冒充全部游戏 90% 完成度。
- 不能因 token 充足就让多个代理同时改同一 Unity 场景、Prefab、包锁或活动编辑器。
