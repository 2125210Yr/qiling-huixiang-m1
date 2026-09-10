# FormulaProfiles

版本：`1.0-G1-2026-09-09`。状态：`CONTRACT_FROZEN`。

## 允许的 profile（对照，不是 GL 定版）

| Profile | 证据类 | 用途 |
|---|---|---|
| `JP_LEGACY_EMPIRICAL` | EMPIRICAL / 历史 JP [S05–S10][S16] | 对照、防抄错、红测夹具 |
| `KR_LEGACY_REPORTED` | REPORTED / KR 转述二次衰减 [S07] | 对照差异 |

GL 数值 = **UNKNOWN**。接入时必须显式选上述之一做对照，或走 `UNKNOWN` 拒绝结算为“已验证”。

## 禁止
- 发明或注册 `GL_FINAL_VERIFIED` / `JP_FINAL_VERIFIED` / `KR_FINAL_VERIFIED`。
- 让 GL 偷偷 inherit JP/KR。
- `GenericDamage = ATK * 倍率 - DEF` 当终版。
- 用 Drive UI 预测当对敌伤害 [S16]。
- 把 Slide 区间当成均匀 RNG 真值 [S06]。
- 用三份旧录像反推 GL 公式并闭 UNKNOWN。

## 对照式（仅 legacy；未含最终减伤/舍入/buff）
见 `DC_RECON_KIT/02_NUMERICS_AND_CALIBRATION.md` 与 `tools/candidate_formulas.py`。

- Tap：`Btap = (400*S + 125*Ae)/(400 + 0.15*D) * (E+c)`；KR 另乘 `max(0.6, 1-0.00008*D)`。
- Slide：区间式，返回 bounds，不抽样。
- 穿防：`P_eff = P_nominal * (0.6 + 0.4*D/20000)`，域 0–20000。
- Fever：`0.6*Btap + 0.6*P_eff + ExtraFlat`（普通分支；连打型例外另册）。
- Auto：独立结构，不与 Tap 合并。
- Drive UI：预测显示，**无**实际 Drive 伪公式。

`S` = 技能显示伤害，不是总 ATK，也不是再乘一次的倍率。`c`：优势+暴击算例为 2.4，不是 2.8。

## 目标窗额外约束
停服后期含 Ignition。Ignition/增幅与普通属性组合 = `U006` UNKNOWN。不得用 Ignition 前 JP 式关闭该项。

验收门槛（有完整输入的确定性样本时的**项目目标**，不是已达标宣称）：平均相对误差 ≤3%，P95 ≤5%。无 GT、无 GL 输入真值时不得报 numeric pass。
