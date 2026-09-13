# elevator-red 参数设计（Live2D Cubism 4）

姿势基线：**电梯轿厢内。左腿支撑，右腿高抬；左手插腰，右手不入画/贴身。高开叉红裙，赤足，右踝脚链。**

手臂已锁死为参考图，**不是**扶手+下垂臂。`ParamArmRRail` 与 `ParamArmLHang*` 不再表示 rest 姿势；插腰手臂不进物理。详见 `pose_lock.json`。

静帧即 rest：单腿支撑造成的骨盆倾斜、开叉张开量、裙摆垂坠都已经画在原画里。所有参数在 default 值时必须还原静帧（变形器为单位变换）。因此**凡是"姿势本来就有"的量，参数只做增量，default = 0**，不要把静帧状态写成 default。

左右一律指**角色自身**的左右，不是画面左右。画面朝向另有说明时会写"画面左/右"。

## 命名规则

- Cubism 官方标准 ID（`ParamAngleX`、`ParamEyeLOpen`、`ParamBreath` …）原样保留，方便直接吃官方 idle/lipsync 与 SDK 默认绑定。
- 本姿势自定义参数：`Param<部位><轴或序号><L|R>`，链式次级段用 `Sub` / `01..03` 递进，越大越靠末端。
- 物理输出参数**只能**由 physics 写。手 K 动作、待机 clip、鼠标追踪都不允许直接写这些 ID（见 `idle_clips.json` 的 `constraints.physicsOwned`）。

## 求值顺序

每帧严格按此顺序，物理必须最后跑，否则次级运动会晚一帧且吃不到追踪输入：

1. 姿势 / 手 K 动作（override）
2. 待机 clip 叠加（additive，`idle_clips.json`）
3. 鼠标追踪（`mouse_tracking.json`，写 `ParamAngle*` / `ParamEyeBall*` / `ParamBodyAngle*`）
4. 物理（`physics_groups.json`，读上面三步的结果，写次级参数）
5. 按下表 range 统一 clamp

`physics_groups.json` 内部的 `PhysicsSettings` 顺序也是有依赖的，不能随意重排：`thigh_raised` / `thigh_raised_unit` 产出的 `ParamKneeSwayR` / `ParamAnkleR` 是 `dress_slit_panel` 和 `anklet_R` 的输入，所以抬起腿那两组必须排在裙子开叉片与脚链之前。每个 `*_unit` 伴生组紧跟它的父组。改顺序前先看 `Meta.PhysicsDictionary`。

`ParamElevatorAccel` / `ParamBreath` / `ParamWeightShift` 量程是 ±1（呼吸 0..1），不能和头/身（±30 / ±10）共用一个 `Normalization.Position`。它们在各自的 `*_unit` setting 里，Position span = ±1。

## 头 / 眼 / 眉 / 口

| ID | 含义 | Range | Default | 关键点 | 驱动 | 备注 |
|---|---|---|---|---|---|---|
| `ParamAngleX` | 头水平转 | -30 .. 30 | 0 | 3 | 追踪 | 正 = 转向画面右。右肩贴后壁，正向有效上限只到 +22，见 `mouse_tracking.json` 的 `poseConstraints` |
| `ParamAngleY` | 头俯仰 | -30 .. 30 | 0 | 3 | 追踪 | 负 = 低头 |
| `ParamAngleZ` | 头倾斜 | -30 .. 30 | 0 | 3 | 追踪 + 待机 | 靠墙姿势偏向轻微倒头，待机给 -3 常驻偏置 |
| `ParamEyeBallX` | 眼球水平 | -1 .. 1 | 0 | 3 | 追踪 | 领先头部，无延迟 |
| `ParamEyeBallY` | 眼球垂直 | -1 .. 1 | 0 | 3 | 追踪 | |
| `ParamEyeLOpen` | 左眼开合 | 0 .. 2 | 1 | 3 | 待机（眨眼） | 1→2 段做睁大，只在演出里用；眨眼只走 1→0 |
| `ParamEyeROpen` | 右眼开合 | 0 .. 2 | 1 | 3 | 待机（眨眼） | 比左眼晚 8ms，别做成绝对同步 |
| `ParamEyeLSmile` | 左眼笑眼 | 0 .. 1 | 0 | 2 | 手 K | 眨眼时不动它 |
| `ParamEyeRSmile` | 右眼笑眼 | 0 .. 1 | 0 | 2 | 手 K | |
| `ParamBrowLY` | 左眉高低 | -1 .. 1 | 0 | 3 | 待机 + 手 K | 眨眼带 -0.12 微沉 |
| `ParamBrowRY` | 右眉高低 | -1 .. 1 | 0 | 3 | 待机 + 手 K | |
| `ParamBrowLForm` | 左眉形状 | -1 .. 1 | 0 | 3 | 手 K | 负 = 皱，正 = 挑 |
| `ParamBrowRForm` | 右眉形状 | -1 .. 1 | 0 | 3 | 手 K | |
| `ParamMouthOpenY` | 开口 | 0 .. 1 | 0 | 3 | 手 K / lipsync | |
| `ParamMouthForm` | 口型 | -1 .. 1 | 0 | 3 | 手 K | |
| `ParamCheek` | 脸红 | 0 .. 1 | 0 | 2 | 手 K | |

## 躯干

| ID | 含义 | Range | Default | 关键点 | 驱动 | 备注 |
|---|---|---|---|---|---|---|
| `ParamBodyAngleX` | 身体水平 | -10 .. 10 | 0 | 3 | 追踪 | 幅度是头的 1/4，靠墙不许大幅扭腰 |
| `ParamBodyAngleY` | 身体上下 | -10 .. 10 | 0 | 3 | 追踪 + 呼吸 | 呼吸耦合 ±0.8 |
| `ParamBodyAngleZ` | 身体倾斜 | -10 .. 10 | 0 | 3 | 追踪 + 重心 | 重心切换主要走这条 |
| `ParamBreath` | 呼吸 | 0 .. 1 | 0 | 2 | 待机 | 胸腔上抬 + 肩微升，不做腹部起伏 |
| `ParamWeightShift` | 重心左右 | -1 .. 1 | 0 | 3 | 待机 | 负 = 压向支撑腿（左）。抬腿侧只允许到 **+0.35**，超过会读成"要摔" |
| `ParamHipTilt` | 骨盆侧倾 | -1 .. 1 | 0 | 3 | 待机（跟随 `ParamWeightShift`） | 增量。静帧已含单腿支撑的倾斜，别把它写成 default |
| `ParamSpineLean` | 脊柱靠墙 | -1 .. 1 | 0 | 3 | 待机 | 正 = 更贴后壁，负 = 离墙 |
| `ParamShoulderLY` | 左肩高低 | -1 .. 1 | 0 | 3 | 呼吸 + 手臂 | |
| `ParamShoulderRY` | 右肩高低 | -1 .. 1 | 0 | 3 | 呼吸 | 扶扶手那侧，幅度砍半 |

## 抬起腿 / 支撑腿

| ID | 含义 | Range | Default | 关键点 | 驱动 | 备注 |
|---|---|---|---|---|---|---|
| `ParamLegRaiseR` | 右腿抬起量 | 0 .. 1 | 1 | 3 | 姿势 / 演出 | **唯一 default≠0 的姿势参数。** 静帧 = 1。0 端需要另画落腿关键帧，本 rig 只在 0.85..1 做落腿收势，别整段插值 |
| `ParamKneeSwayR` | 抬起膝摆 | -1 .. 1 | 0 | 3 | **物理输出** | `thigh_raised` 写。幅度小、迟滞大：抬起的腿是重物 |
| `ParamAnkleR` | 抬起踝摆 | -1 .. 1 | 0 | 3 | **物理输出** | `thigh_raised` 写，同时是脚链的输入 |
| `ParamToeR` | 右脚趾 | -1 .. 1 | 0 | 3 | 手 K | 蹬墙时勾/绷 |
| `ParamThighPressR` | 脚掌蹬墙挤压 | 0 .. 1 | 0 | 2 | 手 K | 脚掌与后壁接触面的压平 + 小腿肌肉收紧，跟 `ParamLegRaiseR` 同向用 |
| `ParamLegSupportL` | 支撑腿微屈 | -1 .. 1 | 0 | 3 | 待机 | 跟 `ParamWeightShift` 反相，负 = 更屈 |

## 裙 / 开叉

| ID | 含义 | Range | Default | 关键点 | 驱动 | 备注 |
|---|---|---|---|---|---|---|
| `ParamSlitOpen` | 开叉张开 | -1 .. 1 | 0 | 3 | 手 K / 演出 | 增量。正 = 露出更多大腿。给美术留了正向余量，别在待机里自动推正值 |
| `ParamSkirtSlitPanel` | 开叉自由片（根） | -1 .. 1 | 0 | 3 | **物理输出** | 抬起腿这侧的布片没有身体贴合，是真正的自由摆片 |
| `ParamSkirtSlitPanelSub` | 开叉自由片（摆） | -1 .. 1 | 0 | 3 | **物理输出** | |
| `ParamSkirtPinL` | 支撑腿侧裙片 | -1 .. 1 | 0 | 3 | **物理输出** | 贴腿，动能被接触吃掉：低 mobility、高 delay |
| `ParamSkirtBackX` | 后裙片 | -1 .. 1 | 0 | 3 | **物理输出** | 被后壁夹住，只做小幅左右 |
| `ParamSkirtHemFlutter` | 下摆抖 | -1 .. 1 | 0 | 3 | **物理输出** | 电梯启停时最明显的一条 |

## 头发

| ID | 含义 | Range | Default | 关键点 | 驱动 | 备注 |
|---|---|---|---|---|---|---|
| `ParamHairFront` | 刘海 | -1 .. 1 | 0 | 3 | **物理输出** | 短摆、硬、快回位 |
| `ParamHairSideL` / `R` | 侧发根 | -1 .. 1 | 0 | 3 | **物理输出** | 左右分开两组，参数刻意不对称 |
| `ParamHairSideLSub` / `RSub` | 侧发末端 | -1 .. 1 | 0 | 3 | **物理输出** | |
| `ParamHairBack01` | 后发根 | -1 .. 1 | 0 | 3 | **物理输出** | |
| `ParamHairBack02` | 后发中 | -1 .. 1 | 0 | 3 | **物理输出** | |
| `ParamHairBack03` | 后发梢 | -1 .. 1 | 0 | 3 | **物理输出** | 过腰长度，延迟最大的一条 |
| `ParamHairFluffy` | 发量微涨 | 0 .. 1 | 0 | 2 | 待机（呼吸耦合） | 非物理，纯呼吸联动 |

后发被后壁压住，动的是"贴着镜面滑"，不是空中甩。物理里靠低 acceleration + 高 delay 表达，不要加风。

## 胸

沿用 `tools/puppet/PUPPET_MOTION_v0.2.0.md` 的既有决定，不要在这里推翻：

| ID | 含义 | Range | Default | 关键点 | 驱动 | 备注 |
|---|---|---|---|---|---|---|
| `ParamBustLZ` | 左胸旋摆 | -1 .. 1 | 0 | 3 | **物理输出** | 附着点在**乳上沿**，左右**同向** RotZ |
| `ParamBustRZ` | 右胸旋摆 | -1 .. 1 | 0 | 3 | **物理输出** | |
| `ParamBustLY` | 左胸垂向 | -1 .. 1 | 0 | 3 | **物理输出** | 幅度上限 ±0.6，只做惯性下垂/回弹 |
| `ParamBustRY` | 右胸垂向 | -1 .. 1 | 0 | 3 | **物理输出** | |

禁止项：左右对转往中线挤（挤乳沟）、整团 `Y` 上提、缩放、转肩骨代替胸参数、任何 `Squeeze` 类参数。衬衫/裙口/肩三处必须钉住，外下侧配重，胸口画在 body 上、不挖胸洞。

## 手臂

锁定：**左手插腰**（`arm_upper_hip` / `arm_forearm_hip` / `hand_hip`）。插腰是刚体 rest，不进物理。

| ID | 含义 | Range | Default | 关键点 | 驱动 | 备注 |
|---|---|---|---|---|---|---|
| `ParamArmLHang` | （退役）左臂下垂 | -1 .. 1 | 0 | 3 | 不用 | 参考图没有下垂臂。物理组可留着但不绑插腰层 |
| `ParamArmLHangSub` | （退役）左前臂 | -1 .. 1 | 0 | 3 | 不用 | |
| `ParamHandLSwing` | （退役）左手甩 | -1 .. 1 | 0 | 3 | 不用 | |
| `ParamArmHipPlant` | 插腰贴合 | 0 .. 1 | 1 | 2 | 姿势 | **rest = 1。** 手钉在胯上 |
| `ParamArmRRail` | （退役）右臂扶手 | 0 .. 1 | 0 | 2 | 不用 | 参考图没有扶手。default 改为 0 |

## 饰品 / 环境

| ID | 含义 | Range | Default | 关键点 | 驱动 | 备注 |
|---|---|---|---|---|---|---|
| `ParamAnkletR` | 脚链摆 | -1 .. 1 | 0 | 3 | **物理输出** | 极短极快，是"这套 rig 有没有做细"的读点 |
| `ParamAnkletRSub` | 脚链坠饰 | -1 .. 1 | 0 | 3 | **物理输出** | |
| `ParamElevatorAccel` | 电梯垂向加速度 | -1 .. 1 | 0 | 3 | 演出输入 | 正 = 上行起步 / 下行制动（超重），负 = 上行制动 / 下行起步（失重）。只进 `*_unit` 物理组（Position ±1），是这套 rig 的签名输入 |

## 变形器层级建议

```
root
├─ body_rot (ParamBodyAngleX/Y/Z, ParamSpineLean)
│  ├─ hip (ParamHipTilt, ParamWeightShift)
│  │  ├─ leg_support_L (ParamLegSupportL)
│  │  ├─ leg_raise_R (ParamLegRaiseR → ParamKneeSwayR → ParamAnkleR → ParamToeR)
│  │  │  └─ anklet_R (ParamAnkletR → ParamAnkletRSub)
│  │  ├─ skirt_slit (ParamSlitOpen, ParamSkirtSlitPanel → Sub)
│  │  ├─ skirt_pin_L (ParamSkirtPinL)
│  │  └─ skirt_back (ParamSkirtBackX → ParamSkirtHemFlutter)
│  ├─ chest (ParamBreath, ParamBustL/RZ, ParamBustL/RY)
│  ├─ shoulder_L (ParamShoulderLY) → arm_L (ParamArmLHang → Sub → ParamHandLSwing)
│  ├─ shoulder_R (ParamShoulderRY) → arm_R (ParamArmRRail)
│  └─ neck → head_rot (ParamAngleX/Y/Z)
│     ├─ face (眼/眉/口)
│     ├─ hair_front (ParamHairFront)
│     └─ hair_side_L / hair_side_R (…Sub)
└─ hair_back (ParamHairBack01 → 02 → 03)   ← 挂在 root，不挂 head，否则转头会把发根拽出后壁
```

`hair_back` 刻意不做 head 的子级：后发压在镜面上，跟着头 100% 走会穿墙。它靠 physics 从 `ParamAngleX` / `ParamBodyAngleX` 取输入，衰减后再动。

## 验收 checklist

- 所有参数回 default → 渲染结果与静帧逐像素一致（`ParamLegRaiseR` = 1、`ParamArmHipPlant` = 1 是 default）。
- 静置 10s：呼吸 4s 周期可见，眨眼间隔落在 3–6s，重心 11s 周期不漂移、不累积。
- `ParamWeightShift` 推到 +0.35 上限：支撑腿仍在受力，人不"要摔"。
- `ParamElevatorAccel` 打 ±1 脉冲：发梢、下摆、脚链、胸四处延迟依次递增，脚链最快回位、发梢最慢。
- 鼠标从画面左划到右：眼球先动、头跟、身体最后；`ParamAngleX` 正向不越过 +22。
- 关掉物理：模型不变形、不跳一帧。开物理第一帧无突跳（粒子初始化在 rest）。
- 右手不离扶手、右脚掌不离后壁（任何参数组合下）。
