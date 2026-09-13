# elevator-red 绑定文件核查结论

跑法：

```cmd
cd F:\天命之子\art\live2d-lab\_rigtools
python validate.py   elevator-red
python physics_sim.py elevator-red
python diff_built.py  elevator-red
```

源文件状态：`parameters.md` 57 个参数 / `physics_groups.json` 12 组（51 入 24 出 34 顶点）/
`psd_cut_plan.json` 34 层 / `elevator_red.pose3.json` 4 组。

---

## 结论 1（阻塞级）：成品 physics3.json 是一个 4 组存根

`elevator_red.physics3.json` 与 `rig/physics_groups.json` 完全脱节：

| | 规格 | 成品 |
|---|---|---|
| 组数 | 12 | **4** |
| Fps | 60 | 30 |
| `EffectiveForces` | 有 | **缺** |
| `PhysicsDictionary` | 12 条 | **缺** |

而且成品引用了两个**不存在的参数**：

```
output  ParamLegRaise   <- parameters.md 里没有
output  ParamSkirt      <- parameters.md 里没有
```

`parameters.md` 里的 ID 是 `ParamLegRaiseR` 和 `ParamSkirtSlitPanel`。Cubism 遇到不存在的
参数 id **不会报错**，只会让那一组静默不动 —— 所以这份文件看上去"有物理"，实际一半没在跑。

**`EffectiveForces` 缺失尤其要紧**：没有重力，摆链不会下垂，只会飘着。

规格本身是对的（12 组、51 入 24 出 34 顶点，与 Meta 自报数字完全吻合）。
`build.py` 直接按规格产出完整文件。

---

## 结论 2（设计级）：`ParamElevatorAccel` 其实是惰性输入

这是 `parameters.md` 里被称作**「这套 rig 的签名输入」**的那一条，验收清单也专门测它：

> `ParamElevatorAccel` 打 ±1 脉冲：发梢、下摆、脚链、胸四处延迟依次递增，
> 脚链最快回位、发梢最慢

问题出在 `Normalization.Position`：它是**每个 setting 一个 span（都是 20）**，
但各输入的**量程差 30 倍**——头/身是 ±30 / ±10，而 accel / breath / weightshift 是 ±1。

于是单位权重下的实际影响：

```
authority = reach / PositionSpan × weight
          = ±30 的参数  30/20 × w = 1.5w
          = ±1  的参数   1/20 × w = 0.05w      <- 差 30 倍
```

实测各组（`physics_sim.py` 的 [2] 段）：

| 组 | 注释声称的主输入 | 实测主导 | ElevatorAccel 实权 |
|---|---|---|---|
| `hair_back` | 电梯加速度 w30 | `ParamAngleX` **43%** | **1%** |
| `thigh_raised` | 「主输入是重心而不是头」w55 | `ParamBodyAngleX` **72%** | 9% |
| `dress_back_hem` | 「电梯启停最明显」 | `ParamBodyAngleX` **66%** | 5% |
| `chest_L` / `chest_R` | accel w35 是最高权重 | `ParamBodyAngleX` **37%** | **4%** |
| `anklet_R` | accel w40 | `ParamAnkleR` 54% | 31%（最好的一组） |

`thigh_raised` 的注释说「主输入是重心而不是头」，实测 `ParamWeightShift` 只占 **16%**，
`ParamBodyAngleX` 占 72% —— **和注释写的正好相反**。

仿真验证：`ParamElevatorAccel` 打脉冲，**24 个输出里只有 2 个（左右胸的 Y）有任何响应**。
验收清单里那句「发梢、下摆、脚链、胸四处延迟依次递增」**目前根本走不到**，
因为那个脉冲到不了那四处。

### 为什么不能靠调 Weight 解决

要让 ±1 的 accel 和 ±30 的头**等权**，需要 `w_accel = 30 × w_head`。
`hair_back` 里头权重 55，accel 就得写 **1650** —— 而 Cubism 的 Weight 是 0–100 的百分比。

**量程差 30 倍，在一个 0–100 的权重预算里补不回来。**
`Normalization.Position` 又是 per-setting 的，没法按输入分别设。

### 可行的两条路

**(a) 按量程拆组**（推荐）——把 ±1 的输入拆到独立 setting，各自给合适的
`Normalization.Position`。例如 `hair_back` 拆成「头驱动的后发」与「加速度驱动的后发」两组，
后者 span 设 ±1。代价是顶点链要建两套，收益是 accel 真正有量。

**(b) 接受它是次要输入** —— 把注释和验收清单改成符合实际的描述，
`ParamElevatorAccel` 降级为细微扰动。这是诚实的做法，如果它本来就只是想加一点抖动。

**不要**直接把 weight 乘 30 —— 会超出 0–100 被 Cubism 截断，而且掩盖了真正的问题。

---

## 结论 3（好消息）：rest 是干净的不动点

```
[1] rest stability, all parameters at default, 10 s
    max |output| = 0.000000   OK - rest is a fixed point
```

验收清单第一条「所有参数回 default → 渲染结果与静帧逐像素一致」，**就物理部分而言成立**。
这条不依赖仿真器的标定，是结构性质。

---

## 结论 4：四个文件对"姿势"的描述互相矛盾

| 文件 | 手臂怎么描述 |
|---|---|
| `parameters.md` | 「左腿支撑，右腿抬起…**右臂搭扶手，左臂自然下垂**」；参数 `ParamArmRRail`（扶扶手，default 1）、`ParamArmLHang`（垂摆） |
| `psd_cut_plan.json` | 只有一组手臂层：`arm_upper_hip` / `arm_forearm_hip` / `hand_hip` —— **手搭在胯上** |
| `elevator_red.pose3.json` | `PartArmLFront` / `PartArmLBack` / `PartArmRFront` / `PartArmRBack` 交换组 |

三份文件对"手在哪"给了三个不同答案。`parameters.md` 描述的是**扶栏杆 + 垂手**，
切图计划画的是**手搭胯**。这直接决定了 `arm_hang_L` 那组物理（参数 μ 指向"下垂的手臂"）
要绑到哪条手臂上，以及 `ParamArmRRail` 是否还需要存在。

**`psd_cut_plan.json` 里没有栏杆、也没有垂下的手臂层。** 需要确认哪一份是当前的意图。

---

## 结论 5：切图计划里缺 pivot 的层

34 层里 21 层有 pivot（13 个面部层由头部变形器承载，不需要）。以下 12 层既没有 pivot
也不是面部层，但它们都会摆动或是物理目标：

```
side_hair_l   side_hair_r   horn_l   horn_r   dress_body   leg_stand
anklet        arm_upper_hip arm_forearm_hip   neck   ear_l   ear_r
```

其中 `side_hair_l/r`（物理组 2/3 的输出目标）、`anklet`（组 12）、
`arm_upper_hip`/`arm_forearm_hip`（组 9）**是物理要驱动的部件**，没有 pivot 就没法转。

另：`pivots` 里有一个 `head` 键，但层表里叫 `face` —— 键名对不上，`head` 的 pivot 目前不会被任何层用到。

---

## 结论 6：pose3 的四组交换，其中三组没有第二份画稿

`pose3.json` 声明 4 组左右/前后交换。交换的前提是**两份都画了**。实测：

```
pose3#0  PartArmL     0 个匹配层 —— 第二份没画
pose3#1  PartArmR     0 个匹配层 —— 第二份没画
pose3#2  PartLegRaise 2 个匹配层 —— 有
pose3#3  PartProp     0 个匹配层 —— 第二份没画
```

手臂交换组目前是空转的。要么补画，要么从 `pose3.json` 里删掉，别留着让人以为它在工作。
