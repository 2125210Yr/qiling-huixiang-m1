# Resonance Puppet（窄工具）

给契灵回响一条 **Spine 思路的换装骨架**，不是 Cubism / Inochi / Umamo 编辑器复刻。

## 做什么

```
按规范切好的分层 PNG  →  套进 1 套母骨架  →  游戏里播 转头 / 呼吸 / 口型
```

v0 只做母骨架 **`standee_front`**（首页/详情 2:3 立绘）。另外两套（半身、战斗）列在边界外，有第一套能播再加。

## 不做什么

- 不复刻 Cubism Editor、不读不写 `.moc3` / `.cmo3`、不接 Live2D Core
- 不嵌 Inochi Creator / Stretchy Studio / Umamo 本体；只借 **槽位+参数+网格变形** 的格式层
- 不从一张静帧自动抠层（See-Through / PS 挖胸那条已经证明会裂）
- 不做物理、胶水、弯曲变形器编辑器、全套表情参数
- 不把肢体另切成第二套姿势往静帧上贴

## 三层

| 层 | 职责 | 本仓库 |
|---|---|---|
| 输入 | 人（或 agent）按 `tools/puppet/SPEC.md` 交 PNG | 规范，不是抠图器 |
| 中间 | `pack.py` 把图层对上母骨架的槽和锚点 | 点对齐；缺可选槽就降级 |
| 输出 | `PuppetRig` 播 3 个参数 | 转头、呼吸、口型 |

换脸 / 换发 / 换衣 = 换同名槽的 PNG，骨架和锚点不动。

## 三个参数（封顶）

| 参数 | 缺层时 |
|---|---|
| `angleX` −1..1 | 没有 `head` 槽：只绕头锚转 `hair_front` / `hair_back` |
| `breath` 0..1 | 在 **未切割的 `body` 网格** 上胸口加权变形，禁止再叠一块胸图层 |
| `mouth` 0/1/2 | 没有 `mouth_*`：保持闭嘴，不编造口型 |

## 母骨架（v0 只有一套）

`standee_front`：画布建议 1024×1536（2:3）。必选槽 `body`。可选 `hair_back` `hair_front` `hair_side` `head` `mouth_0/1/2` `hand_r` `hand_l` `foot_r` `foot_l` `sword`。

锚点 UV：`head` `chest` `hand_r` `foot_r` `foot_l`（原点左下）。pack 时可用 `landmarks.json` 覆盖。`meshSlots` 只含未切割的 `body`；三角形朝向 puppet 相机（−Z）。

## 运行时文件

`Resources/Art/Characters/<id>/Puppet/puppet.json` 指向已有 Resources 贴图，不复制 PNG。

C001：从已批准 still 按 CubismLayers 发/剑 alpha + 同姿势几何框取出 head / hand_r / feet。无口型。转头 = 头与发绕颈锚；手与剑绕腕锚；脚绕踝。禁止另画姿势的手脚。
