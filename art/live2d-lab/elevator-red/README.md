# elevator-red · 操作指南

电梯红裙 Live2D 工作目录。**本目录的预览不是 Cubism Editor，也不是官方 `.moc3` 运行时。**

跑的是 `rig/physics.js` + `layers/*.png` 粗代理图（网格变形 / 呼吸 / 眨眼 / 物理弹簧）。`elevator_red.moc3` 与 `textures/texture_00.png` 仍缺，须 Cubism Editor 导出后才能用官方 SDK。

---

## 启动本地服务

**必须从 `art/live2d-lab` 起服务**（否则 web-puppet 读不到 `../elevator-red/layers/`）：

```text
cd /d F:\天命之子\art\live2d-lab
python -m http.server 8767
```

### 分层木偶（web-puppet）

打开 http://127.0.0.1:8767/web-puppet/ ，下拉选 **「电梯红裙 · rembg 底板 + 粗切层」**。

### 网格预览（mesh preview）

先把 `reference.jpg` 复制为 `preview/still.jpg`（若还没有），再打开：

http://127.0.0.1:8767/elevator-red/preview/

同一端口即可；也可单独在 `preview/` 下 `python -m http.server 8765` 后访问 http://127.0.0.1:8765/ 。

---

## 姿势锁 · hand on hip

权威文件：`pose_lock.json`（2026-09-11 锁定）。

| 项 | 值 |
|---|---|
| 支撑腿 | 左 |
| 抬起腿 | 右（`ParamLegRaiseR` default = 1） |
| 左臂 | **hand on hip**（`arm_upper_hip` / `arm_forearm_hip` / `hand_hip`） |
| 右臂 | 不在画幅内 |
| 禁止 | 扶手、下垂臂当静息 |

物理与 idle **不写** poseLocked 参数；静帧回 default 须逐像素还原参考图。

---

## 重新切层（Python 3.13 + rembg）

需要 `rembg`、`opencv-python`、`Pillow`（系统 Python 3.13 已装；PATH 上的 `python` 可能没有）。

```text
cd /d F:\天命之子\art\live2d-lab\elevator-red
C:\Users\Administrator\AppData\Local\Programs\Python\Python313\python.exe split_elevator_red.py
```

产出：`layers/cutout.png`（rembg 全身）、`layers/*.png`（粗切代理）、`layers/_dbg/*`（遮罩调试）。  
`layers/*.png` 仅供预览，**不是** Cubism 导入素材；正式出图按 `textures/layer-manifest.json`。

---

## Cubism Editor 已打开时 · 下一步

1. **备分层** — 按 `textures/layer-manifest.json` 出 35 个 Drawable；补画见 manifest 的 `Underpaint`。全图层同画布、与 `reference.jpg` 对齐，无剪贴蒙版/滤镜。
2. **合成 PSD** — 按 `Order` 叠成 `elevator_red_cubism_import.psd` 放本目录。参考 C001：`art/characters/C001-焰刃/bingren_cubism_import.psd`。
3. **导入** — Editor「文件 → 打开」选 PSD；素材更新后重导入选 **第三项（替换）**，保留网格与参数。GUI 脚本：`tools/art/_cubism_open_psd.py` / `_cubism_reimport.py`（须改 `CUBISM_HWND` 与路径；中文路径先复制到 ASCII 短路径）。
4. **建模** — 变形器层级见 `rig/parameters.md` 末尾；参数 ID 一字不差对齐 `elevator_red.cdi3.json`。
5. **导出** — `elevator_red.moc3` + `textures/texture_00.png`。导出会覆盖 `cdi3.json`（正常）。**保留本目录的 `elevator_red.physics3.json`**，别用 Editor 空模板覆盖。
6. **验收** — 参数回 default = 静帧；idle 10s 无漂移；`ParamElevatorAccel` 脉冲时发梢 > 下摆 > 胸 > 脚链延迟递增。

补齐 moc3 + 纹理后，`elevator_red.model3.json` 可直接喂官方运行时。

---

## 设计文档（非操作必读）

`rig/parameters.md` · `rig/physics_groups.json` · `rig/idle_clips.json` · `psd_cut_plan.json` · `textures/layer-manifest.json`
