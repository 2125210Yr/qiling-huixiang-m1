# C027 红旗袍 — layer plan + blocking plates

Working set for the elevator / red-cheongsam reference. **Nothing here is shippable art.**
The source is a 546x292 compressed video screenshot, so these plates exist to lock the
**part list, the z-order and the pivots** before anyone paints at target resolution.

## Heads up: this folder is shared

`art/characters/C027-红旗袍/` was being written by another pass at the same time (it produced
`split_layers.py`, `_probe/alpha_*.png` and `layers/` at the character root, with a different
plate naming scheme — `torso` / `leg_kick` / `leg_stand` / `horn_l` / `horn_r`). To avoid
clobbering it, everything here lives under `rig-plan/`. The two are complementary:

| | root pass | `rig-plan/` (this one) |
|---|---|---|
| figure isolation | rembg u2net | reuses the same alpha |
| part assignment | geometric zone cuts off the bbox | hand-traced envelopes + material gates |
| plate names | `torso`, `leg_kick`, `leg_stand`, `horn_*` | the 10 requested rig names |
| pivots / z-order | none | `layer_plan.json` |

If the two are ever merged, keep the root pass's alpha and this pass's `layer_plan.json`.

## Files

| file | what it is |
|---|---|
| `layer_plan.json` | **the deliverable.** 10 layers with z-order, pivots, parents, deform hints, traced envelope polygons and measured material gates |
| `build_plates.py` | figure alpha ∧ traced envelope ∧ material gate → plates. Reads all geometry from `layer_plan.json` |
| `probe_reference.py` | letterbox crop, 4x upscale, coordinate grids, zoom tiles, material colour samples |
| `layers/*.png` | 10 blocking plates on the 2176x1168 work canvas |
| `layers/manifest.json` | per-plate pixel counts, bboxes, coverage |
| `layers/contact-layers.jpg` | contact sheet — look at this first |
| `layers/_dbg/clips.png` | traced envelopes + pivot crosshairs over the reference. **Edit the trace here, in `layer_plan.json`, not in code** |

Run: `python art/characters/C027-红旗袍/rig-plan/build_plates.py`
(needs Pillow + numpy + scipy; `C:\Users\Administrator\AppData\Local\Programs\Python\Python313\python.exe` has them, the default `python` on PATH does not).

## Coordinate conventions

- Work canvas `2176x1168` = the letterbox-cropped frame upscaled 4x. All px coordinates in
  `layer_plan.json` are in this space.
- Pivots are **normalized, origin bottom-left, y up** — same as
  `C001-焰刃/v3-layers/pivots.json` and `C001-焰刃/puppet-src/layers/landmarks.json`.

## Z-order (back to front)

`hair_back → arm_hanging → standing_leg → body → dress → raised_leg → arm_hip → head → hair_front → accessory`

The two that matter: **`raised_leg` sits in front of `dress`** (the thigh occludes the skirt,
and the high slit is what reads the pose), and **`standing_leg` sits behind it** (it emerges
from under the hem).

## Plate quality — read before using any of these

| plate | px | usable? |
|---|---|---|
| `raised_leg` | 113,987 | yes — clean thigh→ankle→foot silhouette, the hero shape |
| `dress` | 108,070 | yes — collar to hem |
| `standing_leg` | 29,990 | yes, but has no foot (out of frame) |
| `hair_front` | 14,847 | yes — bangs only |
| `arm_hip` | 19,360 | yes — akimbo triangle reads correctly |
| `head` | 10,908 | yes as a face blob; no separable features |
| `body` | 15,231 | fragmented, shoulders/chest only |
| `hair_back` | 19,428 | **poor** — outline and strands, hollow interior |
| `accessory` | 702 | position marker only (~25 source px) |
| `arm_hanging` | 228 | **noise — treat as empty** |

Figure coverage is 0.67: a third of the rembg silhouette is unclaimed, almost all of it the
dark skirt interior and the shadow between the legs, where no colour rule separates cloth
from elevator.

## Known gaps

1. **`arm_hanging` does not exist in this frame.** Her right arm is fully hidden behind the
   torso and the raised thigh. The 228 px is noise. It must be repainted from scratch — the
   rig needs the layer, the reference cannot supply it.
2. **`hair_back` is hollow.** Only rim-lit hair is cool (b > r); the shadowed bulk picks up
   red bounce off the door and reads warm, so no positive colour rule catches it. The
   subtractive gate (`not skin and not dress`) recovers the outline but not the interior.
3. **The figure is cut off at the frame bottom** — no dress hem, no standing foot.
4. **Face features are below resolution.** An eye is ~10 source px wide. `eye_l` / `eye_r` /
   `brow_*` / `mouth` cannot be cut and must be painted.
5. **Horns** are folded into `hair_back` here; they need their own rigid plates.

## Next steps

### 1. Replace the source (blocking, do this first)
Everything above is gated on a 546x292 screenshot. Get a full-resolution uncropped
illustration, then re-ingest with the repo's normal path:

```
python tools/art/ingest_still.py --id C027 --name 红旗袍 --presenter <src> --portrait <src>
```

That writes `presenter.png` at 1024x1536 plus the Unity `.meta`, matching C003/C005/C007.

### 2. Re-cut plates at target resolution
`layer_plan.json` polygons are in work space; scale them by
`1024/2176` in x and re-trace y against the uncropped figure (the crop origin changes).
At real resolution, cut the `planned_subdivisions` list in the plan, not the coarse 10 —
in particular `raised_thigh` / `raised_calf` / `raised_foot`, because one plate cannot flex
at the ankle, and `chest_l` / `chest_r` for the bounce rig.

### 3. Mesh
Follow `C001-焰刃/puppet-src`. Densities that matched C001:
- `raised_leg` / `raised_foot`: densest in the rig. The foot is strongly foreshortened with
  the sole toward camera; a coarse mesh will shear the toes.
- `dress`: 4x6 warp, top row pinned at the waist, free at the hem.
- `hair_back`: 3x5 warp, root row pinned at the crown.
- `head`, `arm_hip`, `accessory`: rigid or near-rigid; rotation only.

### 4. Rig
Bone chain: `root(body hips) → {dress, raised_leg, standing_leg, arm_hip, arm_hanging, body}`,
`body → head → {hair_front, hair_back}`, `raised_leg(ankle) → accessory`.
Joint centres measured off the reference, in work px:
- raised leg: hip `[960,440]`, knee `[650,600]`, ankle `[672,730]`
- arm_hip: shoulder `[1150,355]`, elbow `[1340,480]`, wrist `[1180,555]`

Params: `head_angle_x/y/z`, `body_angle_x/z`, `breath`, `hair_sway` (hair_back and the dress
hem share the phase, hem delayed), `chest_bounce`, `leg_kick` (drives the raised-leg chain).

Reference implementations already in the repo:
`tools/puppet/from_masks.py`, `tools/puppet/pack.py`, `tools/art/cubism_process.py`,
`tools/art/build_c001_cubism_psd.py`.

### 5. Do not
- Do not resample these plates up to 1024x1536. They are 4x-upscaled JPEG; upscaling again
  bakes in the compression blocks.
- Do not copy anything from here into `client/Assets/Resources/Art/Characters/C027/`.
  Per `art/README.md`, `art/` holds sources only, and nothing here is finished.
