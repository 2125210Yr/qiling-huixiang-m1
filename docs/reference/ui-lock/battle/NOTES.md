# Battle 锁定包（运营期真机）

对照 `天命之子数据/视觉图鉴.html` §06 BATTLE。

纪念版客户端**没有战斗**。本夹锁的是运营期真机语法，不是 Home 棋盘，不是 PolarFloor。

## 1. 要对的图
**ref.png** = `gamekee/_combined/battle_refs/crop_inbattle.png`（636×870，含 wiki 黄签）。圆形石台 + 关卡场景。  
**hud.png** = 同目录 `crop_hud.png`（底栏五圆头；图底部另有一块「每日材料 / 图书馆」残片，不是战斗 HUD）。  
**prebattle.png** = 同目录 `crop_prebattle.png`（进战前大金剑）。  
**raid_pre.png** = `raid_intro_05.png`（韩服五人进战前）。

不要用 `001_home`、`020_character_detail`、`../_not-memorial/operational_home.png`（jp_new_02）当战场。

jp_new_03–08 全是大厅/编队/详情/技能窗/背包/My Room，不是 battle/stage，未收入。jp_new_02 是运营期 HOME，永远不要拷进本夹。

## 2. 空间关系
- 顶 = 敌人 / 计时 / PHASE / 速度 / 自动（`crop_top.png`）
- 中 = 圆形石台战场。背景是关卡场景（林、地牢、Boss 台），不是黑棋盘，不是 Home PolarFloor（`crop_arena.png`）
- 底 = 五个圆头像（不是编队竖条）+ 正中 Drive / Fever（`crop_portraits.png` + `crop_drive.png`）
- 头像环 = 充能色；满了 Tap 或上滑 Slide
- 头像下短名 + HP
- 进战前是大金剑 / 大确认「战斗开始」，不是 Home 画（`crop_start.png`）

日服 GameNext 8 号：①敌信息 ②倍速 ③剩余时间 ④自动 ⑤技能环 ⑥Slide 冷却 ⑦Drive 钮 ⑧我方 Drive / Fever / 总 HP。和裁切对得上。

## 3. 操作面
五人头像 = 整套操作。没有独立技能栏、没有摇杆、没有回合制菜单。  
点 = Tap，按住上滑 = Slide。Drive 是全队共享槽，不是第五个头像技能。Fever 满了进窗口，不换另一套 UI。

## 4. 我方发行字（VFX / HUD）
中文：开战 / 狂热时间 / 警告 / 完美。  
DRIVE 芯片：完美 150% / 优秀。  
连击 HUD：连击 / 伤害。  
不要把原作英文 catchphrase（IT'S SHOWTIME、READY TO RUMBLE、GOOD BUTTON、FEVER TIME、WARNING !!）当发行字。进战 CTA 走 loc「战斗开始」，不要把原作「戰鬥開始」位图当发行资源。

## 5. 不要
- 把 Home 六页签画进战斗
- 把战斗画成棋盘海报
- 回合制技能菜单、独立摇杆
- 抄原作头像脸当发行资源（用我们的 PixelStandIn 圆头像）
- 用 jp_new_02 当战场
- 把 hud.png 底部的材料/图书馆残片当成战斗底栏

## 本夹文件
- `ref.png` — 运营期场上（锁）= `crop_inbattle.png`
- `hud.png` — 底栏五圆头 + Fever（底部残片忽略）
- `prebattle.png` — 进战前五人队 + 大金剑
- `raid_pre.png` — Raid 五人进战前
- `crop_arena.png` — 圆石台 + 战斗体（证明不是 Home 棋盘）
- `crop_top.png` — 关卡名 / 敌 HP / PHASE / 计时 / ×3 SPEED / FULL AUTO
- `crop_portraits.png` — 五个圆头像 + 充能环 + 短名/HP
- `crop_drive.png` — 正中 DRIVE / FEVER 簇
- `crop_start.png` — 大金剑 CTA 空间（loc 战斗开始，不发行原作位图）
