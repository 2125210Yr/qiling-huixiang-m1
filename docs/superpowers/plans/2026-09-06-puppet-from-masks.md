# from_masks 遮罩进立绘 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 作者涂遮罩后，一条命令从静帧切出互斥图层、写出预览和 landmarks，可选 pack 进 C001 的 Puppet。

**Architecture:** 纯 Python（numpy/cv2/PIL）新文件 `from_masks.py`：数组级函数可单测；CLI 强制 1024×1536。挖洞永不碰 `keep`/`chest`/衬衫袖子兜底。复用现有 `pack.py` 与 `PuppetRig`，不改播片协议。

**Tech Stack:** Python 3、numpy、opencv-python、Pillow、pytest。Unity 侧本轮不改。

**Spec:** `docs/superpowers/specs/2026-09-06-puppet-from-masks-design.md`

---

## 文件职责

| 文件 | 职责 |
|---|---|
| Create: `tools/puppet/from_masks.py` | 读 still+masks，写 layers/preview/landmarks，可选 pack |
| Create: `tools/puppet/test_from_masks.py` | 小画布单测：keep 不挖、胸不挖、空遮罩失败、90% keep 失败、锚点 UV |
| Modify: `tools/puppet/README.md` | 写 from_masks 命令和 masks 文件名 |
| Reuse: `tools/puppet/pack.py` | `--pack` 时 import 调用 `pack()` |
| Reuse: `tools/puppet/skeletons/standee_front.json` | 槽顺序与默认锚点 |
| 不改: `PuppetRig.cs` | 运行时已有颈/腕/踝/胸网格 |

动层名：`hair_back` `hair_front` `hair_side` `head` `hand_r` `hand_l` `foot_r` `foot_l` `sword`。`chest` 只进 landmarks。

---

### Task 1: 遮罩布尔与衬衫兜底

**Files:**
- Create: `tools/puppet/from_masks.py`
- Create: `tools/puppet/test_from_masks.py`

- [ ] **Step 1: 写失败测试**

```python
# tools/puppet/test_from_masks.py
import numpy as np
import pytest
from from_masks import mask_bool, shirt_keep


def test_mask_bool_alpha_or_luma():
    a = np.zeros((4, 4, 4), np.uint8)
    a[1, 1] = (255, 255, 255, 255)
    a[2, 2] = (200, 200, 200, 0)
    m = mask_bool(a)
    assert m[1, 1] and not m[2, 2]


def test_shirt_keep_protects_gray_white_not_mint():
    rgb = np.zeros((8, 8, 3), np.uint8)
    rgb[2:6, 1:4] = (200, 195, 190)
    rgb[2:6, 5:7] = (100, 180, 160)
    k = shirt_keep(rgb, np.ones((8, 8), bool))
    assert k[3, 2] and not k[3, 6]
```

- [ ] **Step 2: 跑测试确认失败**

Run: `cd /d F:\天命之子\tools\puppet && python -m pytest test_from_masks.py::test_mask_bool_alpha_or_luma test_from_masks.py::test_shirt_keep_protects_gray_white_not_mint -v`

Expected: FAIL `from_masks` import error or missing names.

- [ ] **Step 3: 最小实现**

```python
# tools/puppet/from_masks.py
from __future__ import annotations

import json
from pathlib import Path

import cv2
import numpy as np
from PIL import Image

MOVE_SLOTS = (
    "hair_back", "hair_front", "hair_side",
    "head", "hand_r", "hand_l", "foot_r", "foot_l", "sword",
)
CANVAS = (1024, 1536)


def mask_bool(arr: np.ndarray) -> np.ndarray:
    if arr.ndim == 2:
        return arr > 8
    if arr.shape[2] == 4:
        return arr[:, :, 3] > 8
    luma = arr[:, :, 0].astype(np.int16) + arr[:, :, 1] + arr[:, :, 2]
    return luma > 24


def shirt_keep(rgb: np.ndarray, a0: np.ndarray) -> np.ndarray:
    r = rgb[:, :, 0].astype(np.int16)
    g = rgb[:, :, 1].astype(np.int16)
    b = rgb[:, :, 2].astype(np.int16)
    mint = a0 & (g > r + 8) & (g > 120) & (r < 190)
    skin = a0 & (r > 145) & (g > 85) & (b > 65) & (r > g) & ((r - b) > 30)
    cloth = a0 & ~mint & (r > 80) & (g > 75) & (b > 65) & (np.abs(r - g) < 42) & (np.abs(g - b) < 48) & ~skin
    h, w = a0.shape
    zone = np.zeros((h, w), bool)
    zone[310:min(h, 860), 330:min(w, 530)] = True
    zone[340:min(h, 700), 500:min(w, 670)] = True
    return cloth & zone
```

- [ ] **Step 4: 再跑测试**

Run: `cd /d F:\天命之子\tools\puppet && python -m pytest test_from_masks.py::test_mask_bool_alpha_or_luma test_from_masks.py::test_shirt_keep_protects_gray_white_not_mint -v`

Expected: PASS

- [ ] **Step 5: Commit**（若本轮允许提交）

```bash
git add tools/puppet/from_masks.py tools/puppet/test_from_masks.py
git commit -m "feat(puppet): mask_bool and shirt keep helper"
```

---

### Task 2: 图层提取与 body 挖洞（keep/胸永不挖）

**Files:**
- Modify: `tools/puppet/from_masks.py`
- Modify: `tools/puppet/test_from_masks.py`

- [ ] **Step 1: 写失败测试**

```python
from from_masks import apply_plates


def test_keep_and_chest_never_punched_from_body():
    h, w = 16, 16
    still = np.zeros((h, w, 4), np.uint8)
    still[:, :, :3] = 180
    still[:, :, 3] = 255
    still[2:8, 2:8, :3] = (100, 180, 150)
    hair = np.zeros((h, w), bool)
    hair[2:10, 2:10] = True
    keep = np.zeros((h, w), bool)
    keep[0:16, 0:5] = True
    chest = np.zeros((h, w), bool)
    chest[4:7, 4:7] = True
    out = apply_plates(still, {"hair_back": hair}, keep=keep, chest=chest)
    body = out["body"]
    assert (body[5, 3, 3] > 8)
    assert (body[5, 5, 3] > 8)
    assert out["hair_back"][3, 3, 3] > 8


def test_empty_move_mask_raises():
    still = np.zeros((8, 8, 4), np.uint8)
    still[:, :, 3] = 255
    empty = np.zeros((8, 8), bool)
    with pytest.raises(ValueError, match="empty"):
        apply_plates(still, {"hand_r": empty}, keep=None, chest=None)
```

- [ ] **Step 2: 跑测试确认失败**

Run: `cd /d F:\天命之子\tools\puppet && python -m pytest test_from_masks.py::test_keep_and_chest_never_punched_from_body test_from_masks.py::test_empty_move_mask_raises -v`

Expected: FAIL missing `apply_plates`.

- [ ] **Step 3: 实现 `apply_plates`**

```python
def apply_plates(still: np.ndarray, moves: dict[str, np.ndarray], keep: np.ndarray | None, chest: np.ndarray | None) -> dict[str, np.ndarray]:
    h, w = still.shape[:2]
    k = np.ones((3, 3), np.uint8)
    a0 = still[:, :, 3] > 8
    rgb = still[:, :, :3]
    protect = shirt_keep(rgb, a0)
    if keep is not None:
        protect = protect | keep
    if chest is not None:
        protect = protect | chest
    plates: dict[str, np.ndarray] = {}
    punch = np.zeros((h, w), np.uint8)
    for name, m in moves.items():
        if int(m.sum()) == 0:
            raise ValueError(f"empty mask: {name}")
        dil = cv2.dilate(m.astype(np.uint8) * 255, k, iterations=2) > 128
        layer = still.copy()
        layer[~dil, 3] = 0
        plates[name] = layer
        punch = np.maximum(punch, (m.astype(np.uint8) * 255))
    punch[protect] = 0
    punch = cv2.erode(punch, k, iterations=1)
    body = still.copy()
    body[punch > 16, 3] = 0
    plates["body"] = body
    return plates
```

- [ ] **Step 4: 再跑测试**

Run: `cd /d F:\天命之子\tools\puppet && python -m pytest test_from_masks.py -v`

Expected: PASS（含 Task 1）

- [ ] **Step 5: Commit**

```bash
git add tools/puppet/from_masks.py tools/puppet/test_from_masks.py
git commit -m "feat(puppet): punch moving slots, never keep or chest"
```

---

### Task 3: keep 挡住动层 90% 则失败

**Files:**
- Modify: `tools/puppet/from_masks.py`
- Modify: `tools/puppet/test_from_masks.py`

- [ ] **Step 1: 写失败测试**

```python
from from_masks import check_keep_overlap


def test_keep_blocks_hand_raises():
    hand = np.zeros((8, 8), bool)
    hand[2:6, 2:6] = True
    keep = np.ones((8, 8), bool)
    with pytest.raises(ValueError, match="keep"):
        check_keep_overlap({"hand_r": hand}, keep)


def test_keep_partial_ok():
    hand = np.zeros((8, 8), bool)
    hand[2:6, 2:6] = True
    keep = np.zeros((8, 8), bool)
    keep[2:6, 2:3] = True
    check_keep_overlap({"hand_r": hand}, keep)
```

- [ ] **Step 2: 跑测试确认失败**

Run: `cd /d F:\天命之子\tools\puppet && python -m pytest test_from_masks.py::test_keep_blocks_hand_raises test_from_masks.py::test_keep_partial_ok -v`

Expected: FAIL missing `check_keep_overlap`.

- [ ] **Step 3: 实现**

```python
def check_keep_overlap(moves: dict[str, np.ndarray], keep: np.ndarray | None) -> None:
    if keep is None:
        return
    for name, m in moves.items():
        n = int(m.sum())
        if n == 0:
            continue
        blocked = int((m & keep).sum())
        if blocked / n > 0.90:
            raise ValueError(f"keep covers {blocked}/{n} of {name}")
```

在 `apply_plates` 开头调用 `check_keep_overlap(moves, keep)`。

- [ ] **Step 4: 再跑全文件测试**

Run: `cd /d F:\天命之子\tools\puppet && python -m pytest test_from_masks.py -v`

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add tools/puppet/from_masks.py tools/puppet/test_from_masks.py
git commit -m "feat(puppet): fail if keep hides a moving slot"
```

---

### Task 4: 锚点 UV（原点左下）

**Files:**
- Modify: `tools/puppet/from_masks.py`
- Modify: `tools/puppet/test_from_masks.py`

- [ ] **Step 1: 写失败测试**

```python
from from_masks import centroid_uv, merge_landmarks


def test_centroid_uv_bottom_left_origin():
    m = np.zeros((10, 10), bool)
    m[1, 2] = True
    u, v = centroid_uv(m)
    assert abs(u - 0.25) < 0.08
    assert abs(v - 0.85) < 0.08


def test_merge_landmarks_file_overrides():
    moves = {"head": np.zeros((4, 4), bool)}
    moves["head"][0, 0] = True
    file_marks = {"head": [0.5, 0.8], "chest": [0.5, 0.7]}
    out = merge_landmarks(moves, chest=None, file_marks=file_marks, defaults={"head": [0.51, 0.82], "chest": [0.50, 0.70]})
    assert out["head"] == [0.5, 0.8]
    assert out["chest"] == [0.5, 0.7]
```

- [ ] **Step 2: 跑测试确认失败**

Run: `cd /d F:\天命之子\tools\puppet && python -m pytest test_from_masks.py::test_centroid_uv_bottom_left_origin test_from_masks.py::test_merge_landmarks_file_overrides -v`

Expected: FAIL missing names.

- [ ] **Step 3: 实现**

```python
def centroid_uv(m: np.ndarray) -> list[float]:
    ys, xs = np.where(m)
    h, w = m.shape
    u = float(xs.mean() / max(w - 1, 1))
    v = float(1.0 - ys.mean() / max(h - 1, 1))
    return [round(u, 4), round(v, 4)]


def merge_landmarks(moves: dict[str, np.ndarray], chest: np.ndarray | None, file_marks: dict | None, defaults: dict) -> dict:
    out = dict(defaults)
    for key in ("head", "hand_r", "hand_l", "foot_r", "foot_l"):
        if key in moves and int(moves[key].sum()) > 0:
            out[key] = centroid_uv(moves[key])
    if chest is not None and int(chest.sum()) > 0:
        out["chest"] = centroid_uv(chest)
    if file_marks:
        out.update(file_marks)
    return out
```

- [ ] **Step 4: 再跑测试**

Run: `cd /d F:\天命之子\tools\puppet && python -m pytest test_from_masks.py -v`

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add tools/puppet/from_masks.py tools/puppet/test_from_masks.py
git commit -m "feat(puppet): landmarks from mask centroids"
```

---

### Task 5: 预览、叠图差、keep 挖洞计数、CLI

**Files:**
- Modify: `tools/puppet/from_masks.py`
- Modify: `tools/puppet/README.md`
- Modify: `tools/puppet/test_from_masks.py`

- [ ] **Step 1: 写失败测试（叠图与 keep 计数）**

```python
from from_masks import composite_rest, keep_holes, MOVE_SLOTS


def test_composite_rest_identity_layers():
    still = np.zeros((4, 4, 4), np.uint8)
    still[:, :, :3] = 10
    still[:, :, 3] = 255
    body = still.copy()
    hair = np.zeros_like(still)
    plates = {"hair_back": hair, "body": body}
    rest = composite_rest(plates)
    assert rest.shape == still.shape
    assert rest[0, 0, 3] == 255


def test_keep_holes_zero_when_protected():
    still = np.zeros((8, 8, 4), np.uint8)
    still[:, :, 3] = 255
    body = still.copy()
    keep = np.ones((8, 8), bool)
    assert keep_holes(still, body, keep) == 0
```

- [ ] **Step 2: 跑测试确认失败**

Run: `cd /d F:\天命之子\tools\puppet && python -m pytest test_from_masks.py::test_composite_rest_identity_layers test_from_masks.py::test_keep_holes_zero_when_protected -v`

Expected: FAIL missing names.

- [ ] **Step 3: 实现预览/叠图/CLI**

在 `from_masks.py` 追加（与现有 `split_still.py` 预览同一套 over）：

```python
REST_ORDER = (
    "hair_back", "body", "head", "hair_side", "hair_front",
    "hand_l", "hand_r", "sword", "foot_l", "foot_r",
)


def over(d, s):
    a = s[:, :, 3:4].astype(np.float32) / 255.0
    return d * (1 - a) + s.astype(np.float32) * a


def composite_rest(plates: dict[str, np.ndarray]) -> np.ndarray:
    h, w = next(iter(plates.values())).shape[:2]
    rest = np.zeros((h, w, 4), np.float32)
    for name in REST_ORDER:
        if name in plates:
            rest = over(rest, plates[name])
    return np.clip(rest, 0, 255).astype(np.uint8)


def keep_holes(still: np.ndarray, body: np.ndarray, keep: np.ndarray | None) -> int:
    if keep is None:
        return 0
    st = still[:, :, 3] > 8
    bd = body[:, :, 3] > 8
    return int((keep & st & ~bd).sum())


def write_preview(path: Path, arr: np.ndarray) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    a = arr[:, :, 3:4].astype(np.float32) / 255.0
    rgb = arr[:, :, :3].astype(np.float32) * a + 32.0 * (1.0 - a)
    vis = np.clip(rgb, 0, 255).astype(np.uint8)
    m = arr[:, :, 3] > 8
    out = np.dstack([vis, np.full(vis.shape[:2], 255, np.uint8)])
    if m.any():
        ys, xs = np.where(m)
        out = out[max(0, ys.min() - 8):ys.max() + 9, max(0, xs.min() - 8):xs.max() + 9]
    Image.fromarray(out).save(path)


def load_rgba(p: Path) -> np.ndarray:
    return np.array(Image.open(p).convert("RGBA"))


def build(src: Path) -> tuple[dict[str, np.ndarray], dict, float, int]:
    still_p = src / "still.png"
    if not still_p.is_file():
        raise SystemExit("missing still.png")
    still = load_rgba(still_p)
    h, w = still.shape[:2]
    if (w, h) != CANVAS:
        raise SystemExit(f"canvas must be {CANVAS[0]}x{CANVAS[1]}, got {w}x{h}")
    mask_dir = src / "masks"
    moves = {}
    for name in MOVE_SLOTS:
        p = mask_dir / f"{name}.png"
        if not p.is_file():
            continue
        moves[name] = mask_bool(load_rgba(p))
    keep = mask_bool(load_rgba(mask_dir / "keep.png")) if (mask_dir / "keep.png").is_file() else None
    chest = mask_bool(load_rgba(mask_dir / "chest.png")) if (mask_dir / "chest.png").is_file() else None
    file_marks = json.loads((src / "landmarks.json").read_text(encoding="utf-8")) if (src / "landmarks.json").is_file() else None
    defaults = json.loads((Path(__file__).parent / "skeletons" / "standee_front.json").read_text(encoding="utf-8"))["landmarks"]
    plates = apply_plates(still, moves, keep, chest)
    marks = merge_landmarks(moves, chest, file_marks, defaults)
    rest = composite_rest(plates)
    diff = float(np.abs(rest[:, :, :3].astype(int) - still[:, :, :3].astype(int)).mean())
    holes = keep_holes(still, plates["body"], keep)
    return plates, marks, diff, holes


def write_outputs(src: Path, plates: dict[str, np.ndarray], marks: dict, diff: float, holes: int) -> Path:
    out = src / "layers"
    prev = out / "preview"
    out.mkdir(parents=True, exist_ok=True)
    for name, arr in plates.items():
        Image.fromarray(arr).save(out / f"{name}.png")
        write_preview(prev / f"{name}.png", arr)
    rest = composite_rest(plates)
    Image.fromarray(rest).save(out / "composite_rest.png")
    write_preview(prev / "composite_rest.png", rest)
    (out / "landmarks.json").write_text(json.dumps(marks, indent=2), encoding="utf-8")
    print("composite vs still mean rgb", round(diff, 2))
    print("keep holes", holes)
    return out
```

CLI `main()`：

```python
def main() -> None:
    import argparse
    from pack import pack
    ap = argparse.ArgumentParser()
    ap.add_argument("--id", required=True)
    ap.add_argument("--src", required=True, type=Path)
    ap.add_argument("--pack", action="store_true")
    args = ap.parse_args()
    plates, marks, diff, holes = build(args.src)
    layers = write_outputs(args.src, plates, marks, diff, holes)
    if args.pack:
        dest_layers = Path("client/Assets/Resources/Art/Characters") / args.id / "PuppetLayers"
        dest_layers.mkdir(parents=True, exist_ok=True)
        for p in layers.glob("*.png"):
            if p.name == "composite_rest.png":
                continue
            shutil.copy2(p, dest_layers / p.name)
        shutil.copy2(layers / "landmarks.json", dest_layers / "landmarks.json")
        pack(
            args.id,
            dest_layers,
            "standee_front",
            "PuppetLayers",
            Path("client/Assets/Resources/Art/Characters") / args.id / "Puppet",
        )
```

`from_masks.py` 顶部补 `import shutil`。

README 改为：

```markdown
# Resonance Puppet

规格：`docs/superpowers/specs/2026-09-06-puppet-from-masks-design.md`

遮罩（PS/Krita）放在 `art/characters/<角色>/puppet-src/masks/`。

```
python tools/puppet/from_masks.py --id C001 --src art/characters/C001-焰刃/puppet-src
python tools/puppet/from_masks.py --id C001 --src art/characters/C001-焰刃/puppet-src --pack
```

先看 `puppet-src/layers/preview/`，再 `--pack`。
```

- [ ] **Step 4: 跑测试**

Run: `cd /d F:\天命之子\tools\puppet && python -m pytest test_from_masks.py -v`

Expected: PASS

- [ ] **Step 5: 无遮罩时应失败**

Run: `python F:\天命之子\tools\puppet\from_masks.py --id C001 --src F:\天命之子\art\characters\C001-焰刃\puppet-src`

Expected: 若还没有 `masks/`，退出并提示缺 still 或空 moves——若 still 在而 masks 空，`apply_plates` 得到只有 body 的 plates，这合法。若完全没有 still 以外的动层，body=still 整张，允许。不要在没有动层时当成错误。

- [ ] **Step 6: Commit**

```bash
git add tools/puppet/from_masks.py tools/puppet/test_from_masks.py tools/puppet/README.md
git commit -m "feat(puppet): from_masks CLI preview and optional pack"
```

---

### Task 6: C001 用真实遮罩走一遍（需你涂的 PNG）

**Files:**
- Create under `art/characters/C001-焰刃/puppet-src/masks/`（作者提供，计划不代涂）

- [ ] **Step 1:** 确认 `still.png` 已在 `art/characters/C001-焰刃/puppet-src/still.png`。

- [ ] **Step 2:** 放入至少 `keep.png`（衬衫袖子胸口）和要动的层遮罩。没有某文件就跳过该槽。

- [ ] **Step 3:** 只预览

Run: `python F:\天命之子\tools\puppet\from_masks.py --id C001 --src F:\天命之子\art\characters\C001-焰刃\puppet-src`

Expected: 打印 `composite vs still mean rgb` 和 `keep holes`（袖子 keep 区域 holes 应接近 0）。打开 `layers/preview/body.png` 看白袖完整。

- [ ] **Step 4:** `--pack` 后 Win64 编一次，首页确认袖子无马赛克、胸呼吸、发绕颈。

- [ ] **Step 5:** 不把作者涂的 PSD 源塞进 git 除非已有资源规范；只提交生成脚本和测试。

---

## Spec 覆盖

| 规格条目 | 任务 |
|---|---|
| still + masks 命令 | Task 5 |
| keep 永不挖 | Task 2 |
| chest 只权重/landmarks | Task 2、4 |
| 衬衫兜底 | Task 1 |
| 腐蚀 1px | Task 2 |
| 空遮罩失败 | Task 2 |
| keep 90% 失败 | Task 3 |
| 1024×1536 | Task 5 `build` |
| 预览 + mean rgb + keep holes | Task 5 |
| `--pack` → pack.py | Task 5 |
| 不改 PuppetRig | 全程不改 |
| C001 验收 | Task 6 |

口型、涂抹窗口、自动贴边：规格非目标，计划无任务。
