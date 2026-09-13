# Cubism 导出 v0.1.0（C001 焰刃）

把 SPEC 分层集导成 Cubism 可导入 PSD + Live2D `model3/physics3/motion3` + 一个能直接双击打开的
待机预览。脚本：`tools/puppet/export_cubism.py`，模板：`tools/puppet/preview_template.html`。

```
python tools/puppet/export_cubism.py --id C001 --src art/characters/C001-焰刃/puppet-src
```

产物落在 `<src>/cubism-export/`。

---

## 1. C001 的四套分层，哪套是最新最全的

审计是按画布尺寸、alpha 覆盖、是否能叠回静帧做的，不是按目录名猜的。

| 目录 | 画布 | 槽位 | 结论 |
|---|---|---|---|
| **`puppet-src/layers`** | 1024×1536 | hair_back / body / head / hair_side / hair_front / hand_r / sword / foot_l / foot_r + `landmarks.json` | **采用。** 唯一符合 `SPEC.md` 画布、唯一带 6 个锚点、唯一能叠回静帧 |
| `cubism-import` | 1024×1536 | body / body_base / **bust** / hair_back / hair_front / hair_side / sword | 弃用。`SPEC.md` 明令禁止 `bust.png` 当独立胸图层，且 `body.png` 被挖了胸洞（166 486 px vs body_base 146 631 px）；没有 head / 手 / 脚 |
| `preview-layers` | **1080×1920** | body / hair_back / hair_front / hair_tip / sword + 20 张调试 mask | 弃用。画布不对，来自另一张静帧，`pivots.json` 只有三个头发点 |
| `v3-layers` | 1024×1536 | body / body_blink / cutout / hair_back / hair_front / sword | 弃用。`layer_body*.png` 和 `layer_cutout.png` 全画布不透明（1 572 864 px = 1024×1536），背景被烤进去了，没法叠 |
| `gen-layers` / `gen-layers-new` | 832×1248 / 900×1400 | AI 重绘的 head / torso / arm_* | 弃用。画布不对、整张不透明，是重绘稿不是拆层 |

对应的 PSD：`bingren_cubism_import.psd` 与 `冰刃-cubism-import.psd` **内容完全相同**，都是上表第二行那套
六层 + `bust`，即旧的 Cubism 分支。`冰刃-mcp.psd` 只有一层 `ice`，是合并稿。

## 2. 最完整的一条管线

```
puppet-src/masks/*.png                （手涂遮罩，源头）
        │  tools/puppet/from_masks.py      通用版，从遮罩切片 + 合成校验 + 写 landmarks
        │  tools/puppet/split_still.py     C001 专用版，遮罩取自 client/.../C001/CubismLayers
        ▼
puppet-src/layers/*.png + landmarks.json + composite_rest.png
        │
        ├─ tools/puppet/pack.py ──────────▶ client/.../C001/Puppet/puppet.json   （Unity，已跑过）
        │     骨架 tools/puppet/skeletons/standee_front.json
        │
        └─ tools/puppet/export_cubism.py ─▶ puppet-src/cubism-export/            （本次新增）
                                             C001.psd / model3 / physics3 / motion3 / preview.html
```

侧支：`tools/art/_cubism_*.py` 是通过 Cubism Editor 远程 API 直接操作编辑器的一组脚本
（注册、开 PSD、加变形器、打头发关键帧、呼吸、胸部 warp）。`tools/art/_cubism_rig.json` 里已经是
一份标准 Live2D 参数表，`export_cubism.py` 生成的 `physics3.json` 输出参数与它对齐
（`ParamHairFront/Side/Back`、`ParamBreath`、`ParamBodyAngle*`）。

`tools/art/build_c001_cubism_psd.py` 是旧的 PSD 生成器，只认 v3 那四层、路径写死在
`F:\Resonance`。`export_cubism.py` 复用了它那套关键写法（RGBA 文档 + `CompatibilityMode.PHOTOSHOP` +
负数 layer_count，Cubism 只认这种 Photoshop 形状的 PSD），但改成读骨架顺序 + 任意 SPEC 目录。

## 3. 已知缺口（这次没有隐瞒地绕过去）

**`puppet-src/layers/body.png` 与 `still.png` 字节完全相同**（md5 `795ca8cc…`，937 704 字节）。
也就是说这套分层是**叠加式**的，不是 `SPEC.md` 要求的互斥式：body 是整张静帧，hair / head / hand /
foot / sword 是在它之上又复制了一份同样的像素。叠回去当然能还原静帧（RGB 平均差 0.266），但
**任何一个槽位一动，底下那份静止的副本就会露出来**。

`puppet-src/masks/` 是空目录，所以 `from_masks.py` 现在重跑不出东西；`split_still.py` 用的遮罩在
`client/Assets/Resources/Art/Characters/C001/CubismLayers/`（junction 指向 `F:\Resonance\client`，
现在还在）。

两条路各自的处理：

- **给 Cubism 的 PSD**：`--psd-mode exclusive`（默认）会把各运动槽从 body 里抠掉，做成互斥叠层，并
  打印需要补画的面积。C001 是 **171 928 px**。PSD 底部另附一层隐藏的 `_still_ref`（整张原静帧），
  就是给补画时当参照的。`--psd-mode as-is` 保留原样，只用于检查。
- **给预览**：根本不拼图层。见下。

## 4. 预览是怎么做的（为什么它不会露洞）

`cubism-export/preview.html` 把**整张静帧当一张网格**（128×192 顶点，WebGL2）来变形，一个像素都没有
重新拼贴，所以上面那些破洞在物理上不可能出现在画面里。

各部位的影响域不是手调的解析 blob，而是**从真实分层 alpha 烘出来的**，烘进三张小贴图，顶点着色器直接采样：

| 贴图 | R | G | B | A |
|---|---|---|---|---|
| fieldA | hair_back 权重 | hair_side 权重 | hair_front 权重 | head 权重 |
| fieldB | 胸权重 | 刚体（躯干）权重 | sword 权重 | 脚下钉死度 |
| fieldL | 三组头发的力臂（根部 0 → 发梢 1） | | | 呼吸力臂（胯 0 → 肩 1） |

几个关键点：

- **刚体核心**不能从 `body.png` 读（它就是整张静帧）。做法是 `静帧 alpha − 膨胀后的头发并集`，闭运算
  补缝后取最大连通域，得到衬衫 / 皮带 / 长裤 / 靴子 / 手臂 / 脸。头发权重再乘 `(1 − 0.88 × 刚体)`，
  所以吹风时头发动、衬衫不动。
- **附着点**取每层遮罩最上沿 6% 的质心：后发 (335, 308)、侧发 (726, 516)、前发 (479, 151)、
  头 (520, 201)、剑 (363, 810)。胯部枢轴 (476, 578) 由刚体包围盒推出。
- **脚**在底部 9% 被钉住，整体摆动乘 `(1 − 钉死度)`，人不会从靴子上滑走。
- **胸**用 `chest_rest.png` / `chest_bounce.png` 这对关键帧。两张 236×180 的裁切在静帧上做模板匹配，
  命中 (468, 328)，`TM_SQDIFF_NORMED = 0.0`（逐像素完全一致），所以贴回去的位置是算出来的不是量出来的。
  片元着色器只在这个矩形内按弹簧幅度在 rest / bounce 之间插值，边缘 10% 羽化。
- 整张 HTML **自带贴图**（静帧和三张场贴图都是 base64 data URI），双击就能看，不需要起服务器，也不会
  撞 file:// 的跨域。

待机幅度照 `PUPPET_IDLE_A.md`：左右胸**同向**摆、各自从上沿吊着、不缩放、不往中线挤，限位 1.9°；
点胸只加冲量不重置位置。

## 5. Live2D 交付物

| 文件 | 说明 |
|---|---|
| `C001.psd` | 10 层（含隐藏 `_still_ref`），Photoshop 形状，Cubism 可直接导入 |
| `C001.model3.json` | 引用 `C001.moc3`（**需要在 Cubism Editor 里建模后产出，本次没有 moc3**）、贴图、physics、Idle motion；`EyeBlink` / `LipSync` 参数组；`HitArea` = Head / Chest / Body / Sword |
| `C001.physics3.json` | 5 组：后发 / 侧发 / 前发 / 胸左 / 胸右。每组两顶点摆锤，摆长按各层遮罩实测换算成 Cubism 归一化单位（后发 9.45、侧发 7.06、前发 2.81）。胸两组用 `Reflect` 区分左右，输入接 `ParamBodyAngleX/Z` + `ParamBreath` |
| `motions/C001.idle.motion3.json` | 6 秒循环，只驱动 `ParamBreath` / `ParamAngleX·Z` / `ParamBodyAngleX·Z`。头发和胸**故意不写关键帧**，交给 physics 求解器 |
| `manifest.json` | 各层包围盒、锚点、刚体包围盒、胯部枢轴、胸矩形、`bodyIsStill` / `psdRepaintPx` 等审计字段 |
| `_fields/` | `--debug-fields` 才生成，12 张单通道权重图，用来肉眼核对影响域 |

**没有 `.moc3`。** 网格、变形器、参数绑定必须在 Cubism Editor 里做，脚本只能把输入准备到位。
`physics3.json` 的输出参数 ID 要和建模时建的参数名对上才会生效。

## 6. 往红裙电梯那张参考上retarget

要换的是数据，不是脚本：

1. 新静帧按 `SPEC.md` 放到 `<角色>/puppet-src/still.png`，画布 1024×1536，RGBA。
2. 遮罩涂进 `puppet-src/masks/`，跑 `from_masks.py`。**这次务必把 masks 提交进仓库**，C001 的丢了，
   导致分层没法重跑。
3. 裙子这类布料：`SPEC.md` 目前没有 `skirt` / `cloth` 槽，C001 也没有（它是衬衫 + 长裤，所以本次
   没有布料物理）。红裙那张需要在 `skeletons/standee_front.json` 的 `optional` / `order` 里加
   `skirt_front` / `skirt_back`，`export_cubism.py` 的 `HAIR_SLOTS` 常量改成一张"像布料一样摆动的
   槽位"表即可 —— 摆锤、力臂、权重抑制那套逻辑对裙子和对头发是同一套，摆长会自动从遮罩实测出来。
4. 那张参考是**单人定机位 + 手臂叉腰 + 抬腿**，不是全身走位。抬起的那条腿要单独成槽并单独补画，
   `PUPPET_MOTION_v0.2.0.md` 里记的"挥剑没接上"是同一类问题：**缺的是补画，不是缺代码**。
