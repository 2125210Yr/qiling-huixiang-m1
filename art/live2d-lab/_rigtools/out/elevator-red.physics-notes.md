# elevator-red physics notes

Generated from `rig/physics_groups.json`. Cubism does not
carry per-setting comments, so they live here.

## hair_front  (PhysicsSetting1)

刘海。短摆、硬、快回位。只吃头部，不吃电梯加速度：额前碎发不该被垂向加速甩起来。

- in : ParamAngleX, ParamAngleZ, ParamBodyAngleX
- out: ParamHairFront
- vertices: 2

## hair_side_L  (PhysicsSetting2)

左侧发（下垂那侧手臂同侧）。两段。左右两组数值刻意不镜像，避免看出对称。 ±1 输入已拆到 hair_side_L_unit（Normalization.Position ±1）。

- in : ParamAngleX, ParamAngleZ, ParamBodyAngleX
- out: ParamHairSideL, ParamHairSideLSub
- vertices: 3

## hair_side_L_unit  (PhysicsSetting3)

hair_side_L 的 ±1 量程伴生组（ParamElevatorAccel / ParamBreath / ParamWeightShift）。Normalization.Position 为 ±1，与输入量程对齐，避免和头/身共 span 时被稀释约 30 倍。顶点链从 hair_side_L 复制，写同一批输出。

- in : ParamElevatorAccel
- out: ParamHairSideL, ParamHairSideLSub
- vertices: 3

## hair_side_R  (PhysicsSetting4)

右侧发。贴扶手/后壁那侧，略短略沉，delay 比左侧大一点。 ±1 输入已拆到 hair_side_R_unit（Normalization.Position ±1）。

- in : ParamAngleX, ParamAngleZ, ParamBodyAngleX
- out: ParamHairSideR, ParamHairSideRSub
- vertices: 3

## hair_side_R_unit  (PhysicsSetting5)

hair_side_R 的 ±1 量程伴生组（ParamElevatorAccel / ParamBreath / ParamWeightShift）。Normalization.Position 为 ±1，与输入量程对齐，避免和头/身共 span 时被稀释约 30 倍。顶点链从 hair_side_R 复制，写同一批输出。

- in : ParamElevatorAccel
- out: ParamHairSideR, ParamHairSideRSub
- vertices: 3

## hair_back  (PhysicsSetting6)

后发，过腰，三段。压在镜面上：低 acceleration + 高 delay，表现为贴墙滑动而不是空中甩。Wind 必须保持 0，轿厢里没有风。 ±1 输入已拆到 hair_back_unit（Normalization.Position ±1）。

- in : ParamAngleX, ParamAngleY, ParamAngleZ, ParamBodyAngleX, ParamBodyAngleZ
- out: ParamHairBack01, ParamHairBack02, ParamHairBack03
- vertices: 4

## hair_back_unit  (PhysicsSetting7)

hair_back 的 ±1 量程伴生组（ParamElevatorAccel / ParamBreath / ParamWeightShift）。Normalization.Position 为 ±1，与输入量程对齐，避免和头/身共 span 时被稀释约 30 倍。顶点链从 hair_back 复制，写同一批输出。

- in : ParamElevatorAccel
- out: ParamHairBack01, ParamHairBack02, ParamHairBack03
- vertices: 4

## thigh_raised  (PhysicsSetting8)

抬起的右腿。必须排在 dress_slit_panel 与 anklet_R 之前：它产出的 ParamKneeSwayR / ParamAnkleR 是那两组的输入。抬起的腿是重物 —— 低 mobility、高 delay、低 acceleration，幅度小、迟滞明显。主输入是重心而不是头。 ±1 输入已拆到 thigh_raised_unit（Normalization.Position ±1）。

- in : ParamBodyAngleX
- out: ParamKneeSwayR, ParamAnkleR
- vertices: 3

## thigh_raised_unit  (PhysicsSetting9)

thigh_raised 的 ±1 量程伴生组（ParamElevatorAccel / ParamBreath / ParamWeightShift）。Normalization.Position 为 ±1，与输入量程对齐，避免和头/身共 span 时被稀释约 30 倍。顶点链从 thigh_raised 复制，写同一批输出。

- in : ParamWeightShift, ParamBreath, ParamElevatorAccel
- out: ParamKneeSwayR, ParamAnkleR
- vertices: 3

## dress_slit_panel  (PhysicsSetting10)

开叉侧的自由裙片（抬起腿这侧）。没有身体贴合，是全 rig 里最自由的布：高 mobility、低 delay、高 acceleration。吃 ParamKneeSwayR，所以腿一动布片先走、腿停了布还在收。 ±1 输入已拆到 dress_slit_panel_unit（Normalization.Position ±1）。

- in : ParamBodyAngleX, ParamBodyAngleZ, ParamKneeSwayR
- out: ParamSkirtSlitPanel, ParamSkirtSlitPanelSub
- vertices: 3

## dress_slit_panel_unit  (PhysicsSetting11)

dress_slit_panel 的 ±1 量程伴生组（ParamElevatorAccel / ParamBreath / ParamWeightShift）。Normalization.Position 为 ±1，与输入量程对齐，避免和头/身共 span 时被稀释约 30 倍。顶点链从 dress_slit_panel 复制，写同一批输出。

- in : ParamWeightShift, ParamElevatorAccel
- out: ParamSkirtSlitPanel, ParamSkirtSlitPanelSub
- vertices: 3

## dress_support_panel  (PhysicsSetting12)

支撑腿（左）侧的裙片。贴腿，动能被接触吃掉：低 mobility、高 delay、低 acceleration。跟 dress_slit_panel 的对比就是这套 rig 的核心读点 —— 同一条裙子两侧行为必须明显不同。不吃电梯加速度，贴腿的布不会被垂向加速甩开。 ±1 输入已拆到 dress_support_panel_unit（Normalization.Position ±1）。

- in : ParamBodyAngleX, ParamBodyAngleZ
- out: ParamSkirtPinL
- vertices: 2

## dress_support_panel_unit  (PhysicsSetting13)

dress_support_panel 的 ±1 量程伴生组（ParamElevatorAccel / ParamBreath / ParamWeightShift）。Normalization.Position 为 ±1，与输入量程对齐，避免和头/身共 span 时被稀释约 30 倍。顶点链从 dress_support_panel 复制，写同一批输出。

- in : ParamWeightShift
- out: ParamSkirtPinL
- vertices: 2

## dress_back_hem  (PhysicsSetting14)

后裙片 + 下摆。被后壁夹住，根部小幅、末端放开。ParamSkirtHemFlutter 是电梯启停最明显的一条读点。 ±1 输入已拆到 dress_back_hem_unit（Normalization.Position ±1）。

- in : ParamBodyAngleX, ParamBodyAngleY
- out: ParamSkirtBackX, ParamSkirtHemFlutter
- vertices: 3

## dress_back_hem_unit  (PhysicsSetting15)

dress_back_hem 的 ±1 量程伴生组（ParamElevatorAccel / ParamBreath / ParamWeightShift）。Normalization.Position 为 ±1，与输入量程对齐，避免和头/身共 span 时被稀释约 30 倍。顶点链从 dress_back_hem 复制，写同一批输出。

- in : ParamElevatorAccel, ParamBreath
- out: ParamSkirtBackX, ParamSkirtHemFlutter
- vertices: 3

## arm_hang_L  (PhysicsSetting16)

下垂左臂物理组（保留，不绑插腰层）。pose_lock：左手插腰、ParamArmRRail 已退役，这组不表示 rest。±1 输入已拆到 arm_hang_L_unit。

- in : ParamBodyAngleX, ParamBodyAngleZ
- out: ParamArmLHang, ParamArmLHangSub, ParamHandLSwing
- vertices: 4

## arm_hang_L_unit  (PhysicsSetting17)

arm_hang_L 的 ±1 量程伴生组（ParamElevatorAccel / ParamBreath / ParamWeightShift）。Normalization.Position 为 ±1，与输入量程对齐，避免和头/身共 span 时被稀释约 30 倍。顶点链从 arm_hang_L 复制，写同一批输出。

- in : ParamWeightShift, ParamBreath, ParamElevatorAccel
- out: ParamArmLHang, ParamArmLHangSub, ParamHandLSwing
- vertices: 4

## chest_L  (PhysicsSetting18)

左胸。单段、短半径、高 acceleration、低 delay = 弹但立刻停。附着点在乳上沿，Angle 归一化收到 ±8 做硬性限幅。左右同向，禁止对转挤中线、禁止整团 Y 上提。 ±1 输入已拆到 chest_L_unit（Normalization.Position ±1）。

- in : ParamBodyAngleX, ParamBodyAngleZ, ParamAngleZ
- out: ParamBustLZ, ParamBustLY
- vertices: 2

## chest_L_unit  (PhysicsSetting19)

chest_L 的 ±1 量程伴生组（ParamElevatorAccel / ParamBreath / ParamWeightShift）。Normalization.Position 为 ±1，与输入量程对齐，避免和头/身共 span 时被稀释约 30 倍。顶点链从 chest_L 复制，写同一批输出。

- in : ParamBreath, ParamElevatorAccel
- out: ParamBustLZ, ParamBustLY
- vertices: 2

## chest_R  (PhysicsSetting20)

右胸。与左侧同向、参数略有差异（半径 3.4 / delay 0.58），制造自然不对称。不要把这组做成 PhysicsSetting10 的精确镜像。 ±1 输入已拆到 chest_R_unit（Normalization.Position ±1）。

- in : ParamBodyAngleX, ParamBodyAngleZ, ParamAngleZ
- out: ParamBustRZ, ParamBustRY
- vertices: 2

## chest_R_unit  (PhysicsSetting21)

chest_R 的 ±1 量程伴生组（ParamElevatorAccel / ParamBreath / ParamWeightShift）。Normalization.Position 为 ±1，与输入量程对齐，避免和头/身共 span 时被稀释约 30 倍。顶点链从 chest_R 复制，写同一批输出。

- in : ParamBreath, ParamElevatorAccel
- out: ParamBustRZ, ParamBustRY
- vertices: 2

## anklet_R  (PhysicsSetting22)

右踝脚链。必须排在 thigh_raised 之后。极短半径 + 极低 delay + 高 acceleration = 高频细碎、最快回位。这条是判断 rig 精细度的读点，别省。 ±1 输入已拆到 anklet_R_unit（Normalization.Position ±1）。

- in : ParamAnkleR, ParamKneeSwayR
- out: ParamAnkletR, ParamAnkletRSub
- vertices: 3

## anklet_R_unit  (PhysicsSetting23)

anklet_R 的 ±1 量程伴生组（ParamElevatorAccel / ParamBreath / ParamWeightShift）。Normalization.Position 为 ±1，与输入量程对齐，避免和头/身共 span 时被稀释约 30 倍。顶点链从 anklet_R 复制，写同一批输出。

- in : ParamElevatorAccel
- out: ParamAnkletR, ParamAnkletRSub
- vertices: 3

