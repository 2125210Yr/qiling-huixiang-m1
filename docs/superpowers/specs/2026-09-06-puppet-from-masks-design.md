# 从遮罩制作 standee 立绘（from_masks）

一张已批准静帧 + 作者涂的遮罩 → 切层、锚点、pack 进游戏。不是 Cubism 编辑器，也不是「丢一张图全自动抠」。

## 目标

作者在 PS / Krita 里涂哪些地方要动，命令行完成制作：

```
still.png + masks/*.png  →  layers/*.png + landmarks  →  pack.py  →  PuppetRig
```

第一版部位：头发、胸口呼吸、头、持剑手、双脚、武器。口型不做。

## 非目标

- 不做涂抹窗口
- 不自动贴边、不推理遮罩（C001 上已证明会挖穿袖子）
- 不把胸切成独立层、不挖胸洞、不生成 `bust.png`
- 不另画第二套姿势的手脚
- 不接 moc3 / Live2D Core

## 输入

目录：`art/characters/<角色>/puppet-src/`（C001 例：`art/characters/C001-焰刃/puppet-src/`）

| 文件 | 必选 | 含义 |
|---|---|---|
| `still.png` | 是 | 1024×1536 RGBA 静帧 |
| `masks/keep.png` | 建议 | 不准从 body 挖走（衬衫、袖子、胸口） |
| `masks/chest.png` | 否 | 只当呼吸权重，不产出图层 |
| `masks/<slot>.png` | 否 | 有文件才生成该动层 |
| `landmarks.json` | 否 | 覆盖自动锚点 |

动层文件名（有则做）：`hair_back` `hair_front` `hair_side` `head` `hand_r` `hand_l` `foot_r` `foot_l` `sword`。

遮罩与静帧同尺寸。白或 alpha>8 = 涂到的区域。头发允许涂大概，边缘不必描发丝。

## 硬规则

1. **胸口**只来自 `masks/chest.png`（没有则用骨架默认胸锚）。权重写进网格，**像素留在 body 上**。
2. **`keep.png`（及衬衫/袖子判定）上的像素永不从 body 挖走。** 头发叠在完整袖子上面。
3. 会动的层（发/头/手/脚/剑）与 body **互斥着色**，但 keep/胸除外。
4. 挖洞比图层小 1px（对挖洞 mask 腐蚀 1 次），静止叠回去应接近静帧。
5. 静止时所有槽 transform 为单位矩阵。
6. 缺可选遮罩就跳过该槽，不报假层。

## 命令

```
python tools/puppet/from_masks.py --id C001 --src art/characters/C001-焰刃/puppet-src
python tools/puppet/from_masks.py --id C001 --src art/characters/C001-焰刃/puppet-src --pack
```

不带 `--pack`：只写 `puppet-src/layers/` 和 `preview/`。带 `--pack`：再拷到 `client/Assets/Resources/Art/Characters/<id>/PuppetLayers/` 并调用现有 `pack.py` 写 `Puppet/puppet.json`。

## 生成算法

对每个存在的动层遮罩 `M`：

- 图层 RGB = `still` RGB，alpha = still.alpha ∩ M（keep 不进入该层的「从 body 挖走」集合，但头发仍可用自己的遮罩叠在 keep 上面）。
- body 初始 = still。
- 从 body 挖走：`hair_*` ∪ `head` ∪ `hand_*` ∪ `foot_*` ∪ `sword`，再减去 `keep` ∪ 衬衫袖子保护区 ∪ `chest`。
- 挖走集合腐蚀 1px 后再打洞。
- 动层自身可相对遮罩膨胀 1～2px，避免静止接缝。

衬衫保护区（`keep` 缺失时的兜底，不能替代 keep）：在袖/衣空间里、非薄荷绿、非皮肤、灰白相近的像素。有 `keep.png` 时以 keep 为准并与该兜底取并。

锚点：`landmarks.json` 优先；否则 `head`/`hand_r`/`foot_*`/`chest` 用对应遮罩不透明重心（UV 原点左下）。缺遮罩则用 `standee_front` 默认。

`chest` 遮罩不写 PNG 层，只写入 landmarks 的 `chest`。

## 输出

| 路径 | 内容 |
|---|---|
| `puppet-src/layers/*.png` | body + 动层 |
| `puppet-src/layers/landmarks.json` | 锚点 |
| `puppet-src/layers/preview/` | 预乘深灰预览 + `composite_rest.png` |
| 标准输出 | `composite vs still mean rgb X.XX`，以及 keep 区域 body 相对 still 挖掉的像素数 |

## 失败则退出、不 pack

- 没有 `still.png`，或无法得到 `body.png`
- 名单里的遮罩完全空
- `keep` 挡住某动层超过 90% 不透明像素（例如 keep 盖住整只手）
- 画布不是 1024×1536

## 运行时

不改母骨架槽名。`PuppetRig` 已有：body 网格呼吸（锁骨以上权重 0）、颈骨转头+发、腕骨转手+剑、踝骨转脚。本工具只负责合法图层；不在 from_masks 里播动画。

## 验收（C001 第一刀）

- 预览叠图接近静帧
- 袖子/衬衫在 body 上完整（对用户圈过的裂口：白袖无马赛克洞）
- `--pack` 后 Win64 首页：整身、胸呼吸、发/头/手/脚按骨动，袖子仍在

## 实现落点

- 新：`tools/puppet/from_masks.py`（可逐步替代手写 `split_still.py` 几何框）
- 复用：`tools/puppet/pack.py`、`skeletons/standee_front.json`、`PuppetRig.cs`
- 不改游戏播片协议（仍是 `puppet.json` 槽 + 锚点）
