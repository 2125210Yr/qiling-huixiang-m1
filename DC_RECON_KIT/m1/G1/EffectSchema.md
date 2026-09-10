# EffectSchema

版本：`1.0-G1-2026-09-09`。状态：`CONTRACT_FROZEN`。

用户填具体技能；引擎只执行版本化 opcode + 强类型参数。禁止技能内任意脚本。

## 未知 opcode
**FAIL**。禁止静默忽略（当前工程 REPAIR 项）。未实现码必须进入事件流错误，战斗标失败/不可验收，不得当 0 伤成功。

## 目标选择（每次效果声明）
- 键：绝对 HP / HP 比例 / 攻击排行 / 指定属性 / 前后排 / 随机。
- 数量、可否重复、平局。
- 排名：施放快照 vs 每命中重算（默认 UNKNOWN，显式声明）。
- 嘲讽、不可选目标优先级。

## 结算顺序（声明槽，GL 顺序仍 UNKNOWN）
段伤 → 追加效果 → 每目标/每命中概率 → 驱散 → 抵抗 → 免疫 → 护盾 → 反射 → 吸血 → 复活。缺证据不得用“通用卡牌顺序”填满。

## 触发时钟
时间型 / 行动型 / 受击型分开。毒/流血：[S19] 行动与受击，**禁止每秒 DoT**。GL 精确帧距 = `U014` UNKNOWN。当前工程：毒/流血已按 on_action / on_hit_taken 执行（剖面门闩）；不是每秒 DoT。

## 堆叠
堆叠、刷新、替换、上限、来源、驱散标签。递归触发必须有预算。快照 vs 实时 = `U009` UNKNOWN。

## 最低 opcode 面（实现清单，不是 GL 已验证表）

| opcode | 语义 | GL 数值 |
|---|---|---|
| `dmg.tap` / `dmg.slide` / `dmg.auto` | 分通道伤害 | UNKNOWN |
| `dmg.pierce` | 穿防通道，非全额真伤 [S08] | UNKNOWN |
| `dmg.extra_flat` | 不进普通 E+c/0.6 通道 [S09][S10] | UNKNOWN |
| `dmg.drive_actual` | 对敌 Drive；≠ UI 预测 [S16] | UNKNOWN |
| `dmg.fever_parts` | base / pierce / extra 分记 | UNKNOWN |
| `status.apply` / `status.dispel` | 带时长时钟与标签 | UNKNOWN |
| `poison.apply` | 行动/受击触发 | UNKNOWN；必须执行 |
| `shield.apply` / `control.apply` | 护盾/控制 | UNKNOWN |
| `charge.add` / `charge.rate` / `slide_cd` | 充能≠CD [S11][S14] | UNKNOWN |
| `retarget` / `revive` | 目标重选/复活 | UNKNOWN |

未列出的码同样走 FAIL，不得静默。
