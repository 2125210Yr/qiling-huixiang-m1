# Ragna GL cue 草稿（`M1-G2-SUPP-CUES`）

**不是普通 5 人 PVE 唯一真值。不是 primary GT。不宣称还原通过。**

源片：`docs/reference/gamekee/_combined/battle_vids/mJrT2conPCI.mp4`  
（Yamashiro / 2019-07-08 / `[DESTINY CHILD Global] Ragna:Break S3` / 383.05s / 1280×720 / 30fps / 三栏横录，游戏在中栏）

抽出：`docs/reference/gl-shutdown-pve/supplementary/ragna_gl/`  
- `full/` 整帧 1280×720  
- `ui/` 中栏竖屏 `crop=406:720:437:0`（只为读 HUD；**禁止当布局坐标**）

工具：ffprobe/ffmpeg 8.1.1。未跑 yt-dlp。未改 Unity。

## 覆盖（Ragna 补充，不是普通 PVE）

| 状态 | 判定 | 代表帧（`ui/`） |
|---|---|---|
| Drive | **看见就绪 + 大招切镜**；未见独立「选 Drive」面板 | `t168s` 头像翼标；`t056s`/`t092s`/`t263s`/`t330s` 技能名切镜 |
| QTE | **看见** GREAT! / PERFECT!；Perfect 叠 `DAMAGE 150%` + `N% TO FEVER` | `t056s` GREAT!；`t236s`/`t365s` PERFECT!+TO FEVER |
| Fever | **看见** `FEVER TIME` 条幅 + 连击/弱点字 | `t240s` `t244s` |
| 结算 | **看见** Ragna 伤害排名 + Close/Home | `t376s` `t381s`（**不是**关卡星级/掉落结算） |
| Slide | **看见** `IT'S SHOWTIME!!` + `SLIDE SKILL`（不是 Fever） | `t040s` Ambush；`t308s` Fullmoon |
| charge / tap | **UNKNOWN**（环/条看得见，点按/长按本身没孤立出来） | `t120s` `t176s` |
| 受击 | 弱：`WeakPoint` / `CRITICAL` 飘字 | `t200s`（Ragna Boss 场，不是普通小兵受击） |
| death / wave | **n/a**（本片无普通换波/我方死亡） | `t100s` `READY TO RUMBLE?` 是 Ragna 阶段条，不是普通 wave |
| 敌 Drive 警告 | Ragna 机制 | `t164s` `WARNING!!` / `ENEMY DRIVE SKILL` |

看不清：`t000s` 黑场；`t167s` `t315s` 白闪；`t056s` `t092s` `t263s` `t330s` 过曝，字能认、HUD 轮廓 **UNKNOWN**。

## 逐帧

| 文件 | 秒 | cue | 看见什么 |
|---|---:|---|---|
| `t000s` | 0 | UNKNOWN | 黑 |
| `t004s` `t008s` `t020s` | 4 / 8 / 20 | NOT_COMBAT | 角色详情（Mei / Melpomene / Shiozuka）。**不能当战斗坐标** |
| `t040s` | 40 | SLIDE_CUTIN | `IT'S SHOWTIME!!` + `SLIDE SKILL Ambush`。底栏五圆头仍在 |
| `t056s` | 55.83 | QTE + DRIVE_CUTIN | `GREAT!` + `True Domination`。过曝 |
| `t092s` | 91.90 | QTE + DRIVE_CUTIN | `PERFECT!` + `Catastrophe II`。过曝 |
| `t100s` | 100 | RAGNA_PHASE | `READY TO RUMBLE?`。Fever 条约 70%。不是普通换波 |
| `t120s` | 120 | HUD | 顶红血条 / `x2 SPEED` / `MANUAL` / 计时；底五圆头 + 绿 Fever 条；`Vampirism`；`COOL TIME`。charge/tap **UNKNOWN** |
| `t164s` | 164 | RAGNA_WARN | 红闪 `WARNING!!` + `ENEMY DRIVE SKILL` |
| `t167s` | 166.60 | UNKNOWN | 白光，无字 |
| `t168s` | 168 | DRIVE_READY + HUD | 若干头像带翼标（Drive 就绪）；`Debuff Blast`；`MANUAL`；Fever 0% |
| `t176s` | 176 | HUD | 同结构；Fever 约 40%；未见 Drive 翼标 |
| `t200s` | 200 | HIT_FX + DRIVE_READY | `WeakPoint` / `CRITICAL`；翼标仍在。不是普通小兵受击基准 |
| `t236s` | 236 | QTE | `PERFECT!` `DAMAGE 150%` `83% TO FEVER`；底条 Fever 100%；`COOL TIME` |
| `t240s` | 240 | FEVER | `FEVER TIME`；`17 COMBO` / 伤害字 / `WeakPoint`；条被抽空约 0% |
| `t244s` | 244 | FEVER | 仍 `FEVER TIME`；连击升高 |
| `t263s` | 263.47 | QTE + DRIVE_CUTIN | `PERFECT!` + `True Domination`。过曝 |
| `t280s` | 280 | HUD + SLIDE_TIP | 场上 `Fullmoon` 技能说明字。不是 Showtime 切镜 |
| `t296s` | 296 | HUD + DRIVE_READY | `ATK Stack` / `Regen`；翼标；Fever 约 40% |
| `t308s` | 308 | SLIDE_CUTIN | `IT'S SHOWTIME!!` + `SLIDE SKILL Fullmoon` |
| `t315s` | 315.13 | UNKNOWN | 白光 |
| `t330s` | 330.27 | QTE + DRIVE_CUTIN | `PERFECT!` + `Sounds of the Ring`。过曝 |
| `t350s` | 350 | HUD | 残血阶段；`COOL TIME`；Fever 0%；无翼标 |
| `t365s` | 364.80 | QTE | `PERFECT!` `DAMAGE 150%` `40% TO FEVER`。画面洗白，HUD **UNKNOWN** |
| `t376s` `t381s` | 376 / 381 | RESULT_RAGNA | Boss 剩余时间 / 五人 **Total Damage** 排名 / `Close` / `Home`。**不是**普通关卡结算 |

## 禁止升格

- 本目录任何数字（150%、Fever%、排名伤害）**不得**写入普通 5 人 PVE 数值基准。
- `IT'S SHOWTIME!!` = Slide 切镜，**不是** Fever。
- `READY TO RUMBLE?` = Ragna 阶段，**不是**普通 wave。
- 三栏装饰画 / 角色详情 / 纪念版大厅：**不能当战斗坐标**。

## Raid / WB（只记模式差，不抽进本目录）

未把 `Vdf4V693IcU` / `89jpoNqAwa8` 抽到 `ragna_gl/`。对照仍用已有 `battle_vids` 静帧：

| 片 | 模式差（只对照） | 禁止 |
|---|---|---|
| `Vdf4V693IcU` | 韩文 UI；进战/完成更像 Raid·Ragna 奖牌（`RAGNA:BREAK` / `撃滅` / `COMPLETE`）；大厅可见多人房 | 韩文数字、排名、击破分 **禁止**当普通 PVE 基准 |
| `89jpoNqAwa8` | World Boss：底栏远多于 5 圆头；默认 `FULL AUTO`；结算是 Boss 名+总伤，不是五人排名关卡 | 同上 |

## `battle_refs/` HUD 轮廓（REPORTED，混区）

路径：`docs/reference/gamekee/_combined/battle_refs/`  
`evidence_class=REPORTED`。**JP / KR / 中文 wiki 黄签混用**，不能当 GL 已验证，不能当像素坐标。

| 静帧 | 区 | 只许记的轮廓 |
|---|---|---|
| `crop_hud.png` | 混（中文黄签 + 非 GL 核验 UI） | 底弧 **五圆头**；头像环=充能色；头下短名+HP；底通栏绿 **FEVER** 条。图底「每日材料/图书馆」是拼贴残片，**不是**战斗底栏 |
| `crop_inbattle.png` | 混（繁中黄签） | 顶：倍速 / 计时 / `FULL AUTO`；中：圆石台；敌血条在角色顶上 |
| `crop_prebattle.png` | 混（繁中黄签） | 五人编队 + 大金剑「战斗开始」。这是进战前，不是场上 HUD |
| `crop_livehome.png` | 混（大厅） | **大厅**。禁止当战斗坐标 |
| `jp_new_01.png` 等 | JP 宣发/大厅 | 非战斗 |
| `sys_*.png` | 混 | 商城/社区拼图，非战斗 |

纪念版（`docs/reference/mobile-archive/`、`docs/reference/ui-lock/`）：INDEX 写明 **无战斗 HUD**。大厅图不能当战斗坐标。

## 仍缺的普通 5 人 PVE

primary 仍 `NEEDS_FETCH`。本补充**不能**填这些洞：

1. 停服窗 GL 英文 UI 普通 5 人实机 mp4  
2. 普通关 charge / tap 手势孤立帧  
3. 普通关 Drive **选择**面板（本片只有就绪翼标+切镜）  
4. 普通关换波 / 我方死亡  
5. 普通关星级/掉落结算  
6. 可测量的布局坐标（本片是三栏横录；`battle_refs` 混区）
