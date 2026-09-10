# CombatContract

版本：`1.0-G1-2026-09-09`。状态：`CONTRACT_FROZEN`。目标：GL / 停服后期含 Ignition / 竖屏 / 普通 5 人 PVE。

## 范围
M1 可观察切片：五人普通 PVE。底层编队**不得**把所有模式写死为 5。WB/Ragna/Raid 不进入本契约的伤害真值。

角色立绘、具体技能表、文案 = 用户内容。引擎只提供通用技能执行器。

## 五类动作（不可合并）
来源线索 [S04]。各自独立：输入、时钟、结算通道、UI。

| 动作 | 含义 | 禁止 |
|---|---|---|
| Auto | 自动基础攻击 | 与 Tap 共用公式/按钮 |
| Tap | 点击技能 | 与 Slide 共用 CD |
| Slide | 上滑技能 | 无独立 SlideCd |
| Drive | 选择→预约→演出→QTE→实际伤害→Fever 积累 | 用 UI 预测值当对敌伤害 [S16] |
| Fever | 独立窗口/节流/自动/目标重选/结束 | 当“高倍率 Tap” |

## 状态域（多域，非单枚举互斥）
`Alive` `Targetable` `Charging` `SkillReady` `ActionQueued` `Casting` `SlideCd` `Controlled` `DriveSelect` `Qte` `Fever` `Resolve`。可并行组合由政策表约束，不塞一个 FSM 值。

## 命令 / 事件
按 tick（30Hz）→ 阶段 → 序号排序。BattleCore 不依赖 Animator/粒子回调触发逻辑伤害。固定 seed + 输入日志可回放。

## 数值
结算必须带显式 `profile`。允许对照：`JP_LEGACY_EMPIRICAL`、`KR_LEGACY_REPORTED`。GL 通道输出 `UNKNOWN`，不得闭项。禁止 `GL_FINAL_VERIFIED`。禁止 `GenericDamage=ATK*倍率-DEF` 当终版。

## 本波工程约束
REPAIR：补 SlideCd；编队结构可变人数；毒必须执行；未知 opcode 失败。**勿 REPLACE 整核。**
