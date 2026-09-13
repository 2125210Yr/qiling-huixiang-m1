# _rigtools

围绕 `live2d-lab/<model>/` 的绑定文件工具链：**校验 → 生成 → 仿真**。

## 先说清楚边界

Cubism 的文件分两类：

| 类别 | 格式 | 能否生成 |
|---|---|---|
| `physics3.json` / `pose3.json` / `model3.json` / `motion3.json` | 纯文本 | **能** |
| `.cmo3`（编辑器工程）/ `.moc3`（编译产物） | 闭源二进制 | **不能** |

变形器层级、ArtMesh 摆放、参数关键帧都活在 `.cmo3` 里，**Cubism 之外没有任何东西能写它**。
所以这里做的是 `.cmo3` 之外的全部：编辑器本来要你手工配的物理、姿势组，以及一道
"拒绝产出带悬空参数 id 的文件" 的闸门。

## 用法

```cmd
python validate.py  elevator-red     :: 四个源文件交叉校验
python physics_sim.py elevator-red   :: 物理行为分析
python build.py     elevator-red     :: 从规格生成 physics3.json（写到 out/）
python build.py     elevator-red --write   :: 覆盖模型目录里的那份（先备份）
python diff_built.py elevator-red    :: 成品文件 vs 规格差在哪
```

## 四个源文件

一套 rig 由四个必须互相一致的文件描述，而目前**没有任何东西检查它们是否一致**：

```
rig/parameters.md        参数表（Id / range / default / 驱动）
rig/physics_groups.json  物理规格（Cubism physics3 形状 + 设计注释）
psd_cut_plan.json        切图计划（34 层 / pivot / underpaint）
<name>.pose3.json        姿势组（按名字引用的 Part）
```

## 校验器查什么

- 物理引用的每个参数 id 都真实存在（悬空 id 在 Cubism 里**静默失效**）
- 每个标了「物理输出」的参数都有且只有一个写入者
- 跨组依赖的执行顺序（`thigh_raised` 必须先于 `dress_slit_panel` / `anklet_R`）
- `VertexIndex` 指向真实存在的顶点
- 需要 pivot 的层有没有 pivot（面部层由头部变形器承载，不需要）
- `pose3` 的交换组是否真有两份画好的变体
- 同一参数不会被物理和手 K 同时写

## 仿真器：能信什么，不能信什么

`physics_sim.py` 是 Cubism 摆链（Mobility / Delay / Acceleration / Radius）的**忠实模型**，
**不是** `CubismPhysics.cpp` 的逐位移植。所以：

- **绝对量不可信** —— 输入归一化和输出缩放没有标定过，不要拿它预测 Cubism 里的数值。
- **相对时序可信** —— 结算时间由 Delay / Acceleration / Mobility 决定，这三个按规格原样建模，
  且结算时间按每个输出**自己的峰值**归一化，标定误差自动抵消。
- **不动点性质可信** —— 全参数回 default 后输出是否为零，是结构性质。
- **接线可信** —— 哪个组响应哪个输入，是拓扑性质。

任何绝对幅度的问题，仍然必须开 Cubism 看。

## 找到的问题

见 `FINDINGS.md`。
