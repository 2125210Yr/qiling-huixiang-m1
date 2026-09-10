# 我方切片 vs Ragna 补充（`M1-G2-COMPARE`）

日期：2026-09-10。只读对照。**未改 Unity。未跑 yt-dlp。未重跑 PlayMode。**

**primary 仍 `NEEDS_FETCH`。**  
`docs/reference/gl-shutdown-pve/` 根目录仍 **0 mp4**。  
本对照**不是**普通 5 人 PVE 真值。**不宣称还原通过。** 还原分仍 0。不得发明 `GL_FINAL_VERIFIED`。

两侧都**不能**升格为 `gl-shutdown-pve/` 的 primary GT。

| 层 | 是什么 | 角色 |
|---|---|---|
| 我方切片 | 契灵回响 / Resonance Vertical Slice | 内部冒烟证据；**不是**原作 GT |
| Ragna 补充 | GL Ragna:Break 2019 实机静帧 | 状态轮廓提示；**不是**普通 5 人 PVE 唯一真值 |
| primary | 停服窗普通 5 人 PVE 实机 | **仍缺**（`CANDIDATE_NEEDS_FETCH` / `BLOCKED`） |

## 读了什么

### 我方切片 `docs/reference/gl-shutdown-pve/our_slice/`

来源：`M1-G2-PLAY-RECORD`（Editor PlayMode VS Smoke，Unity 6000.3.23f1，非 batch）。  
结果文件：`vs-smoke.result.txt` → **`FAIL drive never fired`**。`phase=Result`。`tap 0` / `slide 1` / `result 胜利`。

| 文件 | 看见什么（我方 UI） |
|---|---|
| `01_home.png` `ui_home.png` | 大厅「翡翠兔」看板；底栏首页/契灵/关卡/编录/书库/深渊 |
| `02_roster.png` `02_characters.png` `ui_roster.png` | 契灵一览；五人卡 + 战力条（冰刃等） |
| `04_team.png` `ui_team.png` | 五人编队；已入队标记 |
| `03_inspect.png` `ui_inspect.png` | 角色详情「冰刃」；战力/六维/技能入口 |
| `06_battle.png` `ui_battle.png` | 战斗「开战」闪屏；底五圆头；Drive 条约 **0%**；敌方为色块占位；顶栏暂停 / ×2 / 全自动 |

**无** Fever 图。**无** 结算图。README 已写：这些 png **不得**挪到上一级当 GT。

### Ragna 补充 `docs/reference/gl-shutdown-pve/supplementary/ragna_gl/`

草稿：`CUES.md`（`M1-G2-SUPP-CUES`）。  
源片：`docs/reference/gamekee/_combined/battle_vids/mJrT2conPCI.mp4`（Yamashiro / 2019-07-08 / GL Ragna / 1280×720 / 三栏横录，游戏在中栏）。  
抽出：`full/` 27 整帧 + `ui/` 27 中栏竖屏（`crop=406:720:437:0`）。`ui/` **只为读 HUD，禁止当布局坐标**。

本对照目视了 `ui/`：`t004s`（详情）`t040s`（Slide）`t056s`（GREAT!）`t100s`（阶段）`t120s`/`t176s`（HUD）`t164s`（敌 Drive 警告）`t168s`（翼标）`t200s`（WeakPoint）`t236s`（PERFECT!）`t240s`（FEVER TIME）`t308s`（Showtime）`t376s`/`t381s`（Ragna 排名结算）。

`Vdf4V693IcU` / `89jpoNqAwa8` **未**抽进本目录；只许记 Raid/WB 模式差。

## 覆盖总表（charge–result）

取值：`有` = 我方切片有可见帧或结果文件写到；`无` = 切片未拍到 / 未触发；Ragna 列只写**补充能提示什么**，不是普通 PVE 已验证。

| 键 | 我方切片 | Ragna 补充能提示 | 绝不能当普通 5 人 PVE 真值 |
|---|---|---|---|
| charge | **无**孤立长按。开战图底环/条在，Drive≈0%，手势没拆出来 | HUD 能看见头像环与条（`t120s` `t176s`）；点按/长按本身 **UNKNOWN** | 环颜色、百分比、Ragna Boss 顶条数值 |
| tap | **无**可见 tap。日志 `tap 0` | 同左：手势没孤立 | 任何「点一下打多少」 |
| slide | **仅日志** `slide 1`。**无**切镜/Showtime 图 | **能提示** `IT'S SHOWTIME!!` + `SLIDE SKILL` 切镜（`t040s` Ambush；`t308s` Fullmoon）。底五圆头仍在 | Showtime **不是** Fever；技能名/RANK/LV 不是普通关数值 |
| drive | **无**。冒烟 **FAIL drive never fired**。开战图无翼标、无选 Drive 面板、无大招切镜 | **能提示** 就绪=头像翼标（`t168s` `t200s` `t296s`）；大招=技能名切镜（`t056s`/`t092s`/`t263s`/`t330s`，过曝）。**未见**独立 Drive 选择面板 | 翼标几何、切镜过曝帧、技能名；Drive 选择面板仍缺，不得用切镜冒充选人 UI |
| qte | **无** | **能提示** `GREAT!`（`t056s`）/ `PERFECT!` + `DAMAGE 150%` + `N% TO FEVER`（`t236s` `t365s`） | **150% / TO FEVER 百分比不得写入普通数值基准** |
| fever | **无**图、无条幅 | **能提示** `FEVER TIME` 条幅 + 连击/弱点字；条被抽空（`t240s` `t244s`） | Fever% / COMBO / 伤害字不得当普通基准；**不得**把 Showtime 当 Fever |
| hit | 开战图有「弱点」字，**不是**受击结算帧 | 弱提示：`WeakPoint` / `CRITICAL` 飘字（`t200s`）。Ragna Boss 场 | Boss 飘字数字、弱点倍率；不是普通小兵受击 GT |
| death | **无** | **n/a**（本片无我方死亡） | 不得用空位编造死亡演出 |
| wave | **无**换波图。日志有 `phase Stage` 但无截图 | **n/a**。`READY TO RUMBLE?`（`t100s`）是 **Ragna 阶段条** | **禁止**把 `READY TO RUMBLE?` 写成普通换波 |
| result | 日志 `result 胜利` / `phase=Result`。**无结算截图**（Fail 发生在拍结算前） | **能提示** Ragna **伤害排名** + Close/Home（`t376s` `t381s`） | **不是**关卡星级/掉落结算。排名伤害数字禁止当普通基准 |

## M1 必测状态（`UiStateMap`）

对照 `DC_RECON_KIT/m1/G1/UiStateMap.md` 的战斗必测状态。有/无只描述**本轮切片可见性**，不是实现完成声明。

| 状态 | 我方切片 | Ragna 补充 |
|---|---|---|
| `BattleIdle` | 仅「开战」一瞬；未见稳定待命 HUD | `t120s` `t176s` `t350s` 可见 MANUAL / 五圆头 / Fever 条结构 |
| `ChargingReady` | **无**孤立充能就绪 | 环/条看得见；手势 **UNKNOWN** |
| `Tap` | **无**（日志 0） | **UNKNOWN** |
| `Slide` | 仅计数，无图 | Showtime 切镜 **看见** |
| `DriveSelect` | **无** | **仍缺**。只有翼标 + 切镜 |
| `Qte` | **无** | GREAT! / PERFECT! **看见** |
| `Fever` | **无** | `FEVER TIME` **看见** |
| `Controlled` | **无** | 本补充未当作控制真值 |
| `DeathOrWave` | **无** | n/a；阶段条不是 wave |
| `Result` | 仅日志「胜利」，无图 | Ragna 排名结算 **看见**（模式不同） |

后续页（Home / 契灵一览 / 详情 / 编队）：我方切片**有**自己的页，那是 Resonance 壳，**不是**原作大厅/图鉴真值。Ragna 片头 `t004s`/`t008s`/`t020s` 是原作角色详情（Mei / Melpomene / Shiozuka），**不能当战斗坐标**，也不能拿来验收我方详情页还原。

## Ragna 补充可以提示什么（只提示，不闭项）

这些只回答「原作 GL 客户端里，这类状态大概长什么样」。**不能**填普通 5 人 PVE 洞，**不能**当停服窗 primary。

1. **竖屏战斗 HUD 轮廓：** 顶红 Boss 血条 + `x2 SPEED` + `MANUAL` + 计时；场中圆石台；底 **五圆头** + 通栏绿 **FEVER** 条；头上 `COOL TIME` / 状态字（`t120s` `t176s`）。三栏横录 + 中栏裁切 → **禁止当像素坐标 / T27**。
2. **Drive 存在形态：** 就绪=头像翼标；释放=立绘切镜 + 技能名。**不是**「先弹出选 Drive 面板」的证据——那块普通关仍缺。
3. **QTE 存在形态：** GREAT! / PERFECT! 大字；Perfect 可叠伤害%与 Fever 填充提示。数字只属于本 Ragna 场。
4. **Fever 存在形态：** 独立 `FEVER TIME` 条幅，条被消耗，场上连打/弱点字。与 Slide Showtime **分开**。
5. **Slide 存在形态：** `IT'S SHOWTIME!!` + `SLIDE SKILL <名>`。切镜时底栏仍在。
6. **结算存在形态（仅 Ragna）：** Boss 剩余时间 + 五人 Total Damage 排名 + `Close` / `Home`。用来**对照「普通关不是这样结」**，不是普通结账模板。
7. **Ragna 独有，用来排除：** `WARNING!!` / `ENEMY DRIVE SKILL`（`t164s`）；`READY TO RUMBLE?` 阶段（`t100s`）。看到这些应标 Ragna，不得写进普通 PVE 状态机当 wave/death。

Raid/WB（未抽帧，只记差）：韩文 UI、多人房、WB 多槽、默认 `FULL AUTO`、奖牌/总伤结算。**禁止**写入普通战斗数值基准。

`battle_refs/` HUD 轮廓：`evidence_class=REPORTED`，JP/KR + 中文 wiki 黄签混区。纪念版大厅图无战斗。同上，不是 GL 已验证坐标。

## 绝不能当普通 PVE 真值（硬名单）

任一条拿去报 T27 / 公式闭项 / `GL_FINAL_VERIFIED` / 「还原通过」都算越权。

### 我方切片侧

- `our_slice/` 全部 png 与 `vs-smoke.result.txt`
- 大厅翡翠兔、契灵一览、冰刃详情、五人编队、开战闪屏
- 日志里的 `slide 1` / `result 胜利` / `phase=Result`（无结算图；Drive 未触发）
- 开战图上的色块敌人、暂停/×2/全自动、Drive 0% 条
- 本轮实战剖面仍是 `JP_LEGACY_EMPIRICAL`（对照，不是 GL）
- PlayMode **FAILED** 本身
- `dotnet test` 绿数（夹具自洽，与本对照无关）

### Ragna / 其它本地片侧

- `mJrT2conPCI` 及其 `ragna_gl/full|ui` 任一帧、任一数字（150%、Fever%、排名伤害、Boss HP、计时）
- `IT'S SHOWTIME!!` 当 Fever
- `READY TO RUMBLE?` 当普通 wave
- `WARNING!! ENEMY DRIVE SKILL` 当普通 PVE 必测
- Ragna 伤害排名当星级/掉落结算
- 三栏装饰、角色详情、过曝/白闪帧（`t000s` `t056s` `t092s` `t167s` `t263s` `t315s` `t330s` `t365s`）当布局或 HUD 几何
- `Vdf4V693IcU`（KR Raid）、`89jpoNqAwa8`（WB）
- `battle_refs/` 混区静帧、纪念版截图、`专区/` 骨骼预览
- 2019 英文 5 人远程候选（若以后入库也只标 `cross_era_gl`，不得冒充停服窗 primary）

## 两侧都填不了的普通 5 人洞

primary 仍 `NEEDS_FETCH`。切片 FAIL + Ragna 补充**不能**填：

1. 停服窗 GL 英文 UI 普通 5 人实机 mp4（根目录仍 0）
2. 普通关 charge / tap 孤立手势
3. 普通关 Drive **选择**面板
4. 普通关换波 / 我方死亡
5. 普通关星级 / 掉落结算
6. 可测量布局坐标（切片是我方壳；Ragna 是三栏横录；`battle_refs` 混区）
7. GL 公式通道（仍 `UNKNOWN`）

## 结论

- 我方切片**有**：Home / 一览 / 详情 / 编队 / 开战战斗壳。**无**：Drive / QTE / Fever / 死亡 / 换波 / 结算图。Slide 只有计数。PlayMode **FAILED**。
- Ragna 补充**能提示**：Slide 切镜、Drive 翼标+大招切镜、QTE 字、Fever 条幅、Ragna 排名结算、竖屏 HUD 轮廓。**不能**提示普通 Drive 选择、普通换波/死亡、普通星级结算、charge/tap 手势。
- 两边都**不是**普通 5 人 PVE 真值。primary 仍 **NEEDS_FETCH**。本文件不构成还原通过。
