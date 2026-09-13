# elevator-red — YouArt NanoBananaPro CREATE

Ready-to-run still prompt. This session has **no YouArt MCP**, so nothing was submitted.
Do **not** `image_edit` the GIF frame or `elevator-red-original-still.png`. Create a new person.

Repo scripts found (not callable from here):

- `tools/art/youart-c006/canvas_state.json` — prior NanoBananaPro **edit** pack (C006 荆棘猎手, 2:3 / 2K, magenta isolations). Project `99a2b597-c353-438e-90fd-44624d7922f4`. Reuse the node class, not that canvas.
- `tools/art/pack_c006_layers.py` — keys a finished still into aligned paper-doll layers. Run only after a still lands.

When MCP is back: new YouArt project → one `NanoBananaProGenerate` node → paste **Node params** + **Prompt**. Style lock if the UI allows a ref (do not copy the person): `eval/spec-swim-still.jpg` (8-head, graphic hair, painted flesh, flat near-black void).

## Node params

| Field | Value |
|---|---|
| `class_type` | `NanoBananaProGenerate` |
| `mode` | `create` |
| `aspect_ratio` | `16:9` |
| `resolution` | `2K` |
| `num_images` | `1` |
| `free` | `true` (only if the account flag is on; omit otherwise) |

Save the result as `art/live2d-lab/elevator-red/original-still-2k.png` (replace the LANCZOS stand-in if one exists).

## Prompt

Paste as the node's `prompt` (one block):

```
High-end Korean mobile-game character illustration, thick painterly full-body fashion still, 16:9 landscape poster. Adult 8-head fashion-model proportions: long neck, long legs, slim waist, designed bust and hip volume under cloth, not a slim anime teen, not chibi.

ORIGINAL PERSON. Invent a new face, new hair cut, new identity. Do not copy any GIF frame, elevator meme, or any Destiny Child / Memorial child (no Hiva, no named still). New face: Korean commercial-illustration adult, sharp chin, close-set detailed irises with a saturated rim and two catchlights, makeup (liner, blush shape, tinted lips), cool confident expression, mouth slightly parted, looking at camera. Not photoreal, not a 3D doll.

POSE (locked to this elevator-red rest, not the retired handrail design): full body, slightly left of center in a dark elevator shaft. LEFT hand planted on the hip (fingers visible, doing a job). RIGHT leg raised high, knee bent, bare sole angled toward camera, thin gold anklet on the raised ankle. Support leg is the LEFT, foot on the floor, toes visible. RIGHT ARM NOT IN FRAME — cropped by the torso / behind the body. No handrail. No hanging idle arm. No empty dangling hands.

COSTUME: high-collar red silk qipao, high side slit on the raised-leg side so the thigh reads as a separate mass, gold frog buttons and a thin gold hem/slit piping. Chest covered by the opaque bodice. Hips covered by the skirt panel; the slit reveals thigh, not underwear, not hip flesh. Satin cloth sheen as painted bars, not wet skin. Barefoot.

HAIR (must be separable for Live2D): huge designed graphic hair slabs, bigger than the torso silhouette. Black / near-black hair carries the dark slot; the red dress carries chroma; gold is hardware only (3 hues). Split the hair into readable masses: back torrent, left side slab, right side slab, front bang slab. Interior of each mass 20–40% darker; warm rim on the key side. Optional small gold horn-pin ornaments sitting ON the hair (their own pixels, not fused into the scalp). Not strand noise, not 3D blowout, not corkscrew springs, not the GIF's twin round buns.

STAGE: dark elevator, treated as a near-black void. Flat near-black field (#0A0A0C), one dim overhead disk and a tiny distant red floor LED optional. No perspective checkerboard. No orange embers. No UI, no text, no logo, no watermark. Character is the light source. Warm key from upper left; peach skin with form shadow (mauve-brown under jaw, under breast-cloth, inner thigh) and flush on cheek / ear / knee. Skin family: warm peach satin sheen as a painted plane on the lit thigh and clavicle — still a drawing.

PAINT RULES: 2D Korean commercial illustration. Flesh sheen, still a drawing. Not 3D, not photoreal, not oil-canvas, not flat cel. Thin almost-lost linework; silhouette from value, not black outlines. Live2D-ready clear silhouette: hair masses separable, raised thigh not fused to the skirt, standing hip not fused to the dress, jewelry on its own pixels, limbs not fused to the torso.

UNDERPAINT (paint the hidden forms so a later PSD cut has no holes): scalp behind bangs; forehead behind front hair; neck inside the high collar; shoulder inside the qipao sleeve; raised-leg hip crease and the thigh inside the slit; standing-leg hip under the dress; full eyewhite behind each iris; mouth interior larger than the rest opening.

Single full-body illustration, 16:9, 2K, no collage, no split sheet, no extra people.
```

## Refuse / anti-anchors

Do not land on: photoreal pores; cel-shaded anime; generic maid/lolita; Western club latex; storybook pinafore; oil-canvas strokes; checker + embers presenter stage; copying the source GIF face or twin-bun hair; handrail / right hand on a bar; hanging right arm as the rest pose; uncovered chest or hips; text.

In-repo fails to avoid (style only): `eval/test-stargazer.jpg`, `eval/test-eclipse.jpg`, `eval/test-tide.jpg`, `eval/test-flesh.jpg`, `eval/test-3color.jpg`, `eval/test-void.jpg`.

## After the still lands

1. Drop the 2K PNG at `art/live2d-lab/elevator-red/original-still-2k.png`.
2. Cut per `psd_cut_plan.json` / `textures/layer-manifest.json` (same canvas, no recrop).
3. Underpaint slots that stay empty after auto-cut: scalp, collar neck, slit thigh, hip crease, eyewhite, mouth interior.
4. Optional pack, same idea as `tools/art/pack_c006_layers.py` (void-key the near-black, keep face intact).

## Pose lock (do not “improve”)

From `art/live2d-lab/elevator-red/pose_lock.json`:

- support L / raise R
- left arm = hand on hip
- right arm = not in frame
- forbidden: handrail, hanging_arm_as_rest
