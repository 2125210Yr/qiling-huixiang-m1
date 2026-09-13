# 像素战斗特效 · 从 DC 语法来，不搬原作图

- Date: 2026-08-31
- Status: implement
- Research: `天命之子数据/战斗动效.html`、`docs/reference/gamekee/_combined/battle_vids/`（研究用，不进包）
- Client: Resonance 像素角色 + 现有 TAP/SLIDE/DRIVE/Fever 公式

## 一句话

**抄 DC 的时间轴和分层，不抄立绘、Logo、英文招牌。切镜用我们的像素头像；字用开演 / 驱动 / 狂热 / 警告。**

## DC 录像里实际有什么（整理）

| 层 | 原作样子 | 我们怎么像素化 |
|---|---|---|
| TAP | 头像环亮、`TAP FULL`，**没有**全屏切镜。Boss 身上属性色爆点 + 白/浅黄数字 0.2–0.4s | 点按只打属性色像素爆点 + 刀光 + 跳字。禁止每次普技开演 |
| SLIDE | 红底网点 `IT'S SHOWTIME!!` 1.0–1.4s，立绘特写 + 金 `SLIDE SKILL` 徽章 + 技能名，再切回场放技能色爆 | 红像素幕擦入 → 放大像素头像 → 「开演」+「上滑」+ 技能名 1.15s → 切回场爆点。HoldSim |
| DRIVE | `READY TO RUMBLE?` → 大图 + `DRIVE CRUSH` → 底中橙圆 GOOD → `PERFECT !` + `DAMAGE 150%` + `xx% TO FEVER` | 已有橙圆 GOOD。补「就绪？」短幕。完美出 150% 和当前狂热条百分比 |
| 敌 Drive | 整屏血红 `WARNING !!` + `ENEMY DRIVE SKILL` 0.8–1.2s | 红闪 +「警告」大字，头像压暗（已有 warn） |
| Fever | **不换 HUD**。粉紫条 `FEVER TIME`、速度线、COMBO + 总伤 + WeakPoint 叠层，约 7s | 彩虹条已有。加斜向像素速度线、COMBO/总伤金字。禁止切皮肤、禁止藏头像 |
| 数字 | 至少三层：单 hit、WeakPoint/Crit 叠字、COMBO 总伤 | 普击一层；暴击加「暴击」错位字；Fever 加 COMBO |
| 命中色 | 火环 / 青斩 / 绿潭 / 金爆，跟属性走 | TAP 爆点用施术者元素色，不上滑/驱动的技能色 |

硬约束（录像复刻用，原作英文招牌不进包）：

1. 一套战斗 HUD，不按模式做五套。
2. 只有上滑、驱动走全屏切镜；点按没有开演。
3. 驱动判定是底中圆钮，不是横条。
4. 狂热是叠加层，不是另一套界面。
5. 警告 1 秒级红闪，和 Boss 开场海报不是一张皮。

## 不进包

原作立绘、S CLASS、IT'S SHOWTIME 字标、录像帧、pck/apk 粒子。`battle_vids/crops` 只给美术对照。
