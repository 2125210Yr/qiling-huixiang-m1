from pathlib import Path

import cv2
import numpy as np
from PIL import Image
from scipy import ndimage

SRC = Path(r"F:\天命之子\client\Assets\Resources\Art\Characters\C001\preview_new.png")
OUT = Path(r"F:\天命之子\client\Assets\Resources\Art\Characters\C001\PreviewLayers")
DBG = Path(r"F:\天命之子\art\characters\C001-焰刃\preview-layers")

src = np.array(Image.open(SRC).convert("RGBA"))
body = np.array(Image.open(OUT / "layer_body.png").convert("RGBA"))
hair = np.array(Image.open(OUT / "layer_hair_back.png").convert("RGBA"))
h, w = src.shape[:2]
rgb = src[:, :, :3]
r = rgb[:, :, 0].astype(np.int16)
g = rgb[:, :, 1].astype(np.int16)
bch = rgb[:, :, 2].astype(np.int16)
lum = 0.299 * r + 0.587 * g + 0.114 * bch
sat = np.maximum(np.maximum(r, g), bch) - np.minimum(np.minimum(r, g), bch)
hsv = cv2.cvtColor(rgb, cv2.COLOR_RGB2HSV)
hh, ss, vv = hsv[:, :, 0], hsv[:, :, 1], hsv[:, :, 2]
yy, xx = np.mgrid[0:h, 0:w]
nx, ny = xx / float(w), yy / float(h)
mint = ((hh > 32) & (hh < 100) & (ss > 22) & (vv > 55)) | ((g > r + 6) & (g > 68))

ax, ay, bx, by = 0.40 * w, 0.47 * h, 0.15 * w, 0.88 * h
abx, aby = bx - ax, by - ay
ab2 = abx * abx + aby * aby
tline = np.clip(((xx - ax) * abx + (yy - ay) * aby) / ab2, 0.0, 1.0)
sword_line = np.sqrt((xx - ax - tline * abx) ** 2 + (yy - ay - tline * aby) ** 2)
blade = (sword_line < 22) & (ny > 0.46) & (nx < 0.50)
guard = (nx > 0.32) & (nx < 0.46) & (ny > 0.46) & (ny < 0.56) & (lum > 100)

# Hair must not carry the sword, or the blade warps with the chain.
hair[blade | guard, 3] = 0

# Shoulder / harness hole: stamp original clothes back onto the body.
shoulder = (nx > 0.42) & (nx < 0.60) & (ny > 0.22) & (ny < 0.40) & ~mint
harness = (lum < 90) & (sat < 50) & (nx > 0.44) & (nx < 0.64) & (ny > 0.24) & (ny < 0.50)
sleeve = (lum > 145) & (sat < 50) & (nx > 0.38) & (nx < 0.52) & (ny > 0.26) & (ny < 0.54) & ~mint
stamp = ndimage.binary_dilation(shoulder | harness | sleeve, iterations=2)
body[stamp] = src[stamp]

# Drop the dark sausage around the blade, keep icy metal from the original.
dark_capsule = (sword_line < 26) & (lum < 115) & (ny > 0.50) & (nx < 0.48)
body[dark_capsule, 3] = 0
metal = ((sword_line < 16) | guard) & (lum > 118) & (ny > 0.46)
body[metal] = src[metal]
# Any leftover guard silver on the hair plate.
hair[(nx > 0.32) & (nx < 0.46) & (ny > 0.46) & (ny < 0.58) & (lum > 100) & ~mint, 3] = 0

Image.fromarray(body).save(OUT / "layer_body.png")
Image.fromarray(body).save(OUT / "layer_body_blink.png")
Image.fromarray(hair).save(OUT / "layer_hair_back.png")
Image.fromarray(body).save(DBG / "layer_body.png")
Image.fromarray(hair).save(DBG / "layer_hair_back.png")
magenta = np.full((h, w, 3), (255, 0, 220), np.uint8)
for name, im in (("body", body), ("hair_back", hair)):
    af = im[:, :, 3:4].astype(np.float32) / 255.0
    prev = (im[:, :, :3].astype(np.float32) * af + magenta.astype(np.float32) * (1.0 - af)).astype(np.uint8)
    Image.fromarray(prev).save(DBG / ("preview_layer_" + name + ".png"))
print(
    "hair_opaque", int((hair[:, :, 3] > 10).sum()),
    "body_opaque", int((body[:, :, 3] > 10).sum()),
    "hair_on_blade", int(((blade) & (hair[:, :, 3] > 10)).sum()),
    "body_shoulder_min", int(body[(nx > 0.46) & (nx < 0.55) & (ny > 0.26) & (ny < 0.34), 3].min()),
)
