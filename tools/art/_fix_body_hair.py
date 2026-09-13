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
b = rgb[:, :, 2].astype(np.int16)
lum = 0.299 * r + 0.587 * g + 0.114 * b
sat = np.maximum(np.maximum(r, g), b) - np.minimum(np.minimum(r, g), b)
hsv = cv2.cvtColor(rgb, cv2.COLOR_RGB2HSV)
hh, ss, vv = hsv[:, :, 0], hsv[:, :, 1], hsv[:, :, 2]
yy, xx = np.mgrid[0:h, 0:w]
nx, ny = xx / float(w), yy / float(h)
mint = ((hh > 32) & (hh < 100) & (ss > 22) & (vv > 55)) | ((g > r + 6) & (g > 68))
bun = (nx > 0.44) & (nx < 0.60) & (ny > 0.10) & (ny < 0.24)
face_l = (nx > 0.46) & (nx < 0.58) & (ny > 0.14) & (ny < 0.32) & (r > g + 8)
add = ndimage.binary_dilation(bun | face_l, iterations=2) & ~((nx < 0.42) & (ny > 0.22))
body[add] = src[add]
cuff = (lum > 155) & (sat < 48) & (nx > 0.38) & (nx < 0.52) & (ny > 0.24) & (ny < 0.56) & ~mint
hair[cuff, 3] = 0
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
print("body", int((body[:, :, 3] > 10).sum()), "hair", int((hair[:, :, 3] > 10).sum()))
