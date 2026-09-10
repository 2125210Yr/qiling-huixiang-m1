# ClockPolicy

版本：`1.0-G1-2026-09-09`。状态：`CONTRACT_FROZEN`。

实现时钟 = BattleSim **30Hz**。30Hz 是本工程选择，不是原作引擎事实。最终对照帧距等 primary GT，不得把 30Hz 写成 GL 官方规格。

## 必须拆开的时钟
1. Auto 间隔  
2. 技能充能  
3. **SlideCd**（已独立实现；秒数 `UNKNOWN`，占位非 GL）  
4. 状态持续时间  
5. 周期触发（≠毒的默认形态）  
6. 关卡倒计时  
7. Drive / QTE  
8. Fever 窗口  
9. UI 实时时钟（暂停战斗不得冻死 QTE 按钮）

充能 ≠ CD [S11]。睡眠等控制下充能与 SlideCd 必须能独立停/走 [S14]。禁止“一个受控 flag 停掉该角色全部进度”作为默认。

`charge.rate`：`T_new = T_base / (1+b)` 仅为历史候选结构，GL=`UNKNOWN`。`charge.add` 是当前进度离散改变，不是永久缩短冷却。

## 速度 / 自动 / 暂停
快进走本政策的倍率表，**禁止**把所有系统 `Time.timeScale` 调成同一值当还原。参考速度档与自动设置 = `U011` UNKNOWN。未测前开发层可有 1x，不得标目标版默认。

## Drive / Fever 事件拆分
Drive：选择 → 预约 → 演出 → QTE 评价 → **实际伤害** → Fever 积累。  
Fever：可输入窗、连点节流、自动速度、目标重选、伤害分支、结束条件各自配置。早期秒数/充能量不得直接当 GL 后期值 [S04]。

## 命令排序
`tick_index` → `phase` → `seq`。回放绑定 30Hz tick 与输入日志。GT 未到，时钟精度不报 pass。
