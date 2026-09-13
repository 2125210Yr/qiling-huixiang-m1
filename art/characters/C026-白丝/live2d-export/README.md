# C026-白丝 — Live2D retarget export

Normalized part set for a Live2D / Cubism rig, derived from `../layers/` (the
`split_contour.py` output). **Nothing in `../layers/` is modified** — this folder is
generated, and `retarget_live2d.py` only ever reads the source plates.

Regenerate with:

```
py -3.13 retarget_live2d.py
```

## Why C026 and not C001-焰刃

C026 is the better starting body for a red-dress Live2D:

| | C026-白丝 | C001-焰刃 |
|---|---|---|
| Best layer set | 27 plates, one canonical set | 9 plates (`puppet-src/layers`), best of 6 competing sets |
| Limb separation | upper arm / forearm / hand, thigh / boot per side | whole body in one plate; arms only as `arm_pocket` / `arm_sword` |
| Pose | near-frontal, arms held away from the torso | 3/4 turn, weight shift, one hand in a pocket, sword across the body |
| Occlusion holes | almost none — the torso plate is whole | arms overlap the torso and the sword crosses the legs |
| Costume | sleeveless leotard = 2 plates (`chest`, `torso`) | jacket + top + jeans, lapels and pockets baked in |
| Provenance | reproducible script + manifest + QA numbers | plate sets with no single source of truth |

The costume point is what decides it for a red dress: on C026 the garment is two
plates over an otherwise complete body, so recoloring or replacing it does not mean
rebuilding a jacket. C001 would need the outfit repainted before rigging even starts.

## Parts

Full canvas 672×1127 per plate, so parts align by direct overlay — no offsets. Draw
order back to front, matching `parts.json`:

| # | part | Cubism part id | source plate |
|---|---|---|---|
| 0 | `hair_back_l` | PartHairBackL | `hair_back_l` |
| 1 | `hair_back_r` | PartHairBackR | `hair_back_r` |
| 2 | `hair_side_l` | PartHairSideL | `hair_top_l` |
| 3 | `hair_side_r` | PartHairSideR | `hair_top_r` |
| 4 | `leg_lower_l` | PartLegLowerL | `boot_l` |
| 5 | `leg_lower_r` | PartLegLowerR | `boot_r` |
| 6 | `leg_upper_l` | PartLegUpperL | `thigh_l` |
| 7 | `leg_upper_r` | PartLegUpperR | `thigh_r` |
| 8 | `body` | PartBody | `torso` |
| 9 | `chest` | PartChest | `chest` |
| 10 | `arm_upper_r` | PartArmUpperR | `arm_upper_r` + `arm_forearm_r`, re-cut |
| 11 | `arm_lower_r` | PartArmLowerR | `arm_upper_r` + `arm_forearm_r`, re-cut |
| 12 | `arm_upper_l` | PartArmUpperL | `arm_upper_l` |
| 13 | `arm_lower_l` | PartArmLowerL | `arm_forearm_l` |
| 14 | `prop_clutch` | PartPropClutch | `clutch` |
| 15 | `hand_r` | PartHandR | `hand_r` |
| 16 | `hand_l` | PartHandL | `hand_l` |
| 17 | `leg_ornament_l` | PartLegOrnamentL | `boot_ornament_l` |
| 18 | `leg_ornament_r` | PartLegOrnamentR | `boot_ornament_r` |
| 19 | `accessory_wrist_r` | PartWristR | `jewelry_wrist_r` |
| 20 | `accessory_wrist_l` | PartWristL | `jewelry_wrist_l` |
| 21 | `accessory_choker` | PartChoker | `choker` |
| 22 | `head` | PartHead | `head` |
| 23 | `accessory_earring_l` | PartEarringL | `earring_l` |
| 24 | `accessory_earring_r` | PartEarringR | `earring_r` |
| 25 | `hair_front` | PartHairFront | `hair_front` |
| 26 | `accessory_hair_tie_l` | PartHairTieL | `hair_tie_l` |
| 27 | `accessory_hair_tie_r` | PartHairTieR | `hair_tie_r` |

`body` is the hip/lower leotard and is the rig root; `chest` is the bust above the
underbust line, kept separate so it can carry its own breathing and bounce deformer.
`_l` / `_r` are viewer-space (image left/right), inherited from the source split.

## pivots.json

One entry per part with `parent`, `pivot_px`, `pivot_uv` (`[x/width, y/height]`),
`bbox`, `centroid_px` and `opaque_px`. Pixel space is top-left origin, +y down.

Pivots are not eyeballed: each one is the centroid of the band where the part
actually touches its parent, grown a pixel at a time until the two overlap, so
`arm_lower_r` lands on the elbow, `head` on the neck line, `chest` on the underbust,
and `body` on the hip line. The hair tails are the exception — they pivot on the
centroid of their hair tie, which is the physical tail root. `rule` records how each
pivot was derived. `_qa/pivots-overlay.png` draws every pivot and parent link over
the art.

## Two things done to the geometry

1. **Seams closed.** The source plates leave 5457 anti-aliased pixels unowned
   between neighbours, which would show as cracks once a mesh deforms. Every one is
   assigned to its nearest plate (max travel 11.7 px), so recompositing the export
   reproduces `presenter.png` exactly: max channel-sum diff 0, zero unowned pixels.
2. **Right elbow re-cut.** The source watershed put the whole raised arm in
   `arm_forearm_r` and only an armpit wedge in `arm_upper_r`, so there was no elbow
   to rig. The arm folds into a tight V, which means any straight cut line runs
   near-parallel to the forearm and grazes it, so the split is taken on the geodesic
   level set through the elbow — distance measured *along* the limb, around the fold.
   Result: 8683 px shoulder-to-elbow, 3145 px elbow-to-wrist.

## Known limits for the rigger

- `arm_upper_r` also carries the armpit / side-of-chest skin that the source split
  assigned to the arm. Rotating the shoulder will drag it; move that wedge onto
  `chest` in Cubism if it reads badly.
- `leg_lower_*` is the whole thigh-high boot, cut at the boot cuff rather than at the
  knee, so its pivot is the cuff (y≈680) and not the joint (y≈760). Good enough for a
  weight-shift sway, not enough for a knee bend.
- `arm_lower_l` is only 973 px — the left forearm is heavily foreshortened toward the
  clutch. The cut sits at roughly 70% of the way to the wrist, close enough to serve
  as an elbow but tighter than the right side.
- Cut-out plates have no art behind them, so large rotations will expose gaps at the
  shoulders and hips. Standard for a 2D cut-out rig; paint fill behind `chest` and
  `body` if the animation needs range.

## For the red dress

The black latex is only `chest.png` and `body.png` (see `costume_parts_for_recolor`
in `parts.json`). Recolor or repaint those two and the rest of the body, hair and
accessories carry over unchanged. Note that a dress with a skirt will also need to
cover `leg_upper_l` / `leg_upper_r`, which are bare skin here.

## QA

- `_qa/composite.png`, `_qa/composite_diff.png` — export recomposited, and its
  difference against `../presenter.png` (black = identical).
- `_qa/contact-sheet.jpg` — every exported plate, labelled and in draw order.
- `_qa/pivots-overlay.png` — pivots and parent links over the art.
- `_qa/_zoom_arm_*.png`, `_qa/_zoom_legs.png`, `_qa/_strip_arm_r.png` — joint checks
  from `qa_joints.py`.
- `qa_ascii.py` prints a coarse ASCII silhouette of a region, which is how the elbow
  and wrist coordinates were read off the art:
  `py -3.13 qa_ascii.py --layers arm_upper_r arm_forearm_r hand_r`
