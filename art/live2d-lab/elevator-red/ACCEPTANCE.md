# ACCEPTANCE — elevator-red

Inspected on disk **2026-09-11**. Not a Cubism Editor export. This file is a snapshot, not a commit.

Goal (CreateGoal, same day):

> 制作一个与参考图类似的 Live2D：黑发红裙/旗袍女性、电梯站姿（单腿高抬、手插腰、赤足脚链）；交付可播放的分层角色 + Cubism/伪 Live2D 预览（呼吸、眼睛、头发/裙摆、鼠标跟随），并复用仓库现有 Cubism/presenter 管线。验证：磁盘上存在分层 PNG + 模型/物理 JSON + 可打开的预览器，视觉贴近参考图。

**Overall: PARTIAL.** Fake Live2D preview plays today. Official `.moc3` is not on disk. Do not treat this as a Cubism runtime character.

---

## 1. Layered PNGs — PARTIAL

| Proof | Path / URL |
|---|---|
| Named auto-cuts (1280×720, from painted still) | `layers/back_hair.png` `front_hair.png` `torso.png` `chest.png` `dress_body.png` `dress_slit_panel.png` `leg_stand.png` `leg_raise_thigh.png` `leg_raise_calf.png` `foot_raise.png` `anklet.png` `arm_hip.png` `neck.png` `face.png` `eyes.png` `horns.png` |
| GIF rembg plate (1092×584, preserved) | `layers/body.png` |
| Painted-still rembg plate (1280×720) | `layers/body_original.png` |
| 2048 pads of the same pixels | `layers/01_back_hair.png` … `41_headwear.png` (15 files) |
| Slot table | `textures/layer-manifest.json` (35 slots; `original: 0`) |
| Live file | http://127.0.0.1:8767/elevator-red/layers/body.png |
| Live file | http://127.0.0.1:8767/elevator-red/layers/body_original.png |

What this does **not** prove:

- These are rembg + colour/bbox masks from `split_elevator_red.py`, not painted Cubism parts.
- Two canvases are mixed: `body.png` / `cutout.png` stay on the 546×292 GIF; most named parts were recut onto the 1280×720 still. `web-puppet` still points at `body.png` plus the named files, so the layered player is size-mismatched.
- Manifest marks many slots `auto-cut` whose **exact filenames are missing** (`horn_l.png`, `horn_r.png`, `arm_upper_hip.png`, `eyewhite_l.png`, `iris_l.png`, …). Those slots share a combined proxy (`horns.png`, `arm_hip.png`, `eyes.png`).
- Still missing as files: `side_hair_l/r`, `ear_l/r`, `brow_l/r`, `nose`, `mouth_*`, `dress_slit_panel_front`.
- Quality check of the new 1280×720 cuts: `face.png` is a failed bun-shaped hole, `dress_body.png` overcuts legs/arms/face, `anklet.png` is a speck. Not import-grade.

---

## 2. Model / physics JSON — PASS as text skeleton only

| Proof | Path / URL |
|---|---|
| Runtime entry (points at missing moc3) | `elevator_red.model3.json` |
| Physics, 12 groups | `elevator_red.physics3.json` — `Meta.PhysicsSettingCount: 12` (hair_front / hair_side_L / hair_side_R / hair_back / thigh_raised / dress_slit_panel / dress_support_panel / dress_back_hem / arm_hang_L / chest_L / chest_R / anklet_R) |
| Physics design source | `rig/physics_groups.json` |
| DisplayInfo (57 params) | `elevator_red.cdi3.json` |
| Pose switch stub | `elevator_red.pose3.json` |
| Baked idle | `motions/idle.motion3.json` |
| Idle / mouse / param design | `rig/idle_clips.json` `rig/mouse_tracking.json` `rig/parameters.md` |
| Live JSON | http://127.0.0.1:8767/elevator-red/elevator_red.physics3.json |

`model3.json` FileReferences: `Moc: elevator_red.moc3`, `Textures: ["textures/texture_00.png"]`. Those two files are **not on disk**. Official Cubism Web / Unity / pixi-live2d-display cannot load this model. JSON existing ≠ a runnable `.moc3`.

`_rigtools/FINDINGS.md` said physics3 was a 4-group stub. That note is stale versus today's `elevator_red.physics3.json` (12 groups, 51 in / 24 out / 34 verts). Do not use FINDINGS as current proof either way.

---

## 3. Playable preview (breath, eyes, hair/skirt, mouse) — PASS as fake Live2D

Three players answer 200 today. None of them is Cubism Core.

| Player | What it actually runs | Proof URL / file |
|---|---|---|
| Mesh warp of the GIF still | `preview/index.html` + `preview/still.jpg`. Inline springs: breath, blink (button + timer), hair/hem, raised-leg micro bounce, pointer look. **Does not** load `rig/physics.js` or `layers/*.png` (README is wrong on that). | http://127.0.0.1:8767/elevator-red/preview/ |
| Still plate used by that player | SHA256 identical to `reference.jpg` | `preview/still.jpg` — http://127.0.0.1:8767/elevator-red/preview/still.jpg |
| Layered PNG puppet | `web-puppet/index.html` + `web-puppet/models/elevator-red.model.js` + `web-puppet/puppet.js` (breath / blink / hair+cloth sway / mouse follow / rest pose) | http://127.0.0.1:8767/web-puppet/ (also http://127.0.0.1:8766/) |
| Bundled canvas puppet | `elevator-red-puppet/index.html` (405191 bytes, embedded assets) | http://127.0.0.1:8765/ and http://127.0.0.1:8767/elevator-red-puppet/ |

Code proof for the four motions (mesh player): `preview/index.html` — `breath`, `blink` / `#blink`, `sHair` / `sHem`, `pointermove` → `look`.

Code proof (layered player): `web-puppet/puppet.js` header: mouse-follow, idle breath, hair/cloth springs, blink.

Official runtime preview: **no**. `http://127.0.0.1:8767/elevator-red/elevator_red.moc3` → 404.

---

## 4. Visual likeness to reference — PARTIAL

There are **two** stills on disk. They are not the same character design.

| Still | Size | Proof |
|---|---|---|
| GIF / elevator frame (horns, short dark hair, hand on hip, raised R leg, anklet) | 546×292 | `reference.jpg` |
| Mesh preview plate (byte-identical copy) | 546×292 | `preview/still.jpg` — SHA256 `7E7AC47B7E5B816E1763A523CEC48B5CC223DCE3D8C8EA1CAB3664176EDEDF34` matches `reference.jpg` |
| Painted still (double buns, long flying hair, no horns) | 1280×720 | `original-still.png` = `preview/still-original.png` — http://127.0.0.1:8767/elevator-red/original-still.png |
| Pose lock (hand-on-hip, not handrail) | text | `pose_lock.json` |

What likeness is actually proven today:

- The **mesh preview** is the GIF reference pixels warping. Closest likeness to `reference.jpg`.
- `layers/body.png` is rembg of that GIF figure (elevator gone). Pose/costume match the GIF.
- `layers/body_original.png` matches the **painted** still, not the GIF. Theme is the same (black hair, red slit qipao, raised leg, hand on hip, anklet); hair/horns/face are a different drawing.
- Auto-cut parts do **not** composite back to either still. They do not prove import-grade likeness.

---

## 5. Still missing

| Missing | Disk check today | Why it matters |
|---|---|---|
| `.moc3` | `elevator_red.moc3` **absent**. HTTP 404. No `*.moc3` under `art/live2d-lab/`. | Only Cubism Editor can write this. `model3.json` cannot load without it. **Not done.** |
| Texture atlas | `textures/texture_00.png` **absent** | Editor atlas export. |
| Cubism Editor project | `elevator_red.cmo3` **absent** | No Editor scene, no meshes, no deformers. |
| Import PSD | `elevator_red_cubism_import.psd` **absent** | README step 2 never ran. |
| High-res / import-grade cut | Manifest `StatusCounts.original: 0`. Cuts are rembg masks. 2048 files are pads of those masks, not a new paint. | Not Cubism-import art. Hidden-area underpaint (scalp, slit thigh, mouth interior, …) is still a plan in `psd_cut_plan.json` / manifest `Underpaint`. |
| Cubism Editor | No Editor export in this folder. C001's `tools/art/_cubism_open_psd.py` is a different character. | Official runtime / Unity Live2D path is closed until Editor export. |

Canonical workdir pointer (not a substitute for the above): `art/characters/C027-红旗袍/POINTER.md` → this folder.

---

## How to open the proofs that exist

From `art/live2d-lab` (already listening on **8767** today):

```text
http://127.0.0.1:8767/elevator-red/preview/
http://127.0.0.1:8767/web-puppet/
```

`8765` today is the bundled `elevator-red-puppet`, not `preview/still.jpg` (that URL 404s on 8765).
