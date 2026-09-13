# elevator-red Cubism pack

16 photo cuts from `original-still.png`, canvas **1280×720**, order **back → front**.
PSD written with Python 3.13 + Pillow + psd-tools (`elevator_red.psd`).

## Cubism cannot open 天命之子

The Windows / Cubism file dialog fails on the Chinese path `F:\天命之子\...`.
**Copy the PSD to an ASCII short path first:**

```text
mkdir C:\tmp
copy "F:\天命之子\art\live2d-lab\elevator-red\cubism-pack\elevator_red.psd" C:\tmp\elevator_red.psd
```

Then **File → Open** `C:\tmp\elevator_red.psd` (or drag that copy). After edits,
copy it back here if you want the repo copy updated.

## Import

1. Open the `C:\tmp` copy. Layer names match the stems below (no `0000_` prefix).
2. Fallback: stack `ordered-png/` in index order, all at origin `(0,0)`, same canvas.
3. Confirm alpha + order against `layer-manifest.json` before making ArtMeshes.
4. Missing plan slots were never drawn — do not invent empty layers.

## Packed layers (0000 rear → 0015 front)

`back_hair` → `horns` → `torso` → `chest` → `dress_body` → `dress_slit_panel` →
`leg_stand` → `leg_raise_thigh` → `leg_raise_calf` → `foot_raise` → `anklet` →
`arm_hip` → `neck` → `face` → `eyes` → `front_hair`

Combined cuts sit at the first matching plan slot: `horns` (`horn_l`),
`arm_hip` (`arm_upper_hip`), `eyes` (`eyewhite_l`). Sparse but non-empty:
`horns` / `anklet` / `leg_stand` / `leg_raise_calf`.

Skipped: old GIF `body.png`, composite `body_original.png`, 2048 padded `01_*`,
`bg_elevator`, `_dbg`. No 0-opaque files. Still missing from the 35-slot plan:
side hair, `horn_r`, slit-front, split arm/hand, ears, brows, split eyes,
nose, mouth.
