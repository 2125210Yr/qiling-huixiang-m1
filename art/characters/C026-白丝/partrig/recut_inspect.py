"""Zoom the head region so the face split can be planned against real pixels."""
import sys
from pathlib import Path
import numpy as np
import cv2

sys.path.insert(0, str(Path(__file__).resolve().parent))
from partrig import imread_unicode

ROOT = Path(__file__).resolve().parent.parent
img = imread_unicode(ROOT / "presenter.png")
print("presenter %dx%d" % (img.shape[1], img.shape[0]))

# head plate bbox from pivots.json is (312,112,398,243); take a generous crop
X0, Y0, X1, Y1 = 296, 96, 414, 262
crop = img[Y0:Y1, X0:X1].copy()
h, w = crop.shape[:2]
Z = 6
big = cv2.resize(crop, (w * Z, h * Z), interpolation=cv2.INTER_NEAREST)

# composite over a mid grey so alpha edges read, and add a 10px grid
a = big[..., 3:4].astype(np.float32) / 255.0
rgb = big[..., :3].astype(np.float32)
flat = rgb * a + 60 * (1 - a)
for gx in range(0, w * Z, 10 * Z):
    flat[:, gx] = (0, 255, 0)
for gy in range(0, h * Z, 10 * Z):
    flat[gy, :] = (0, 255, 0)
for gx in range(0, w * Z, 50 * Z):
    flat[:, gx] = (255, 0, 255)
for gy in range(0, h * Z, 50 * Z):
    flat[gy, :] = (255, 0, 255)
flat = np.clip(flat, 0, 255).astype(np.uint8)

out = Path(__file__).resolve().parent / "out"
out.mkdir(exist_ok=True)
cv2.imencode(".png", flat)[1].tofile(str(out / "head_zoom.png"))
print("wrote out/head_zoom.png  %dx%d  (crop origin %d,%d, zoom %d, grid 10px / 50px)"
      % (flat.shape[1], flat.shape[0], X0, Y0, Z))

# also a luminance pass, which is where the eye/mouth shapes live
lum = (cv2.cvtColor(crop[..., :3], cv2.COLOR_BGR2GRAY).astype(np.float32) / 255.0
       ) * (crop[..., 3].astype(np.float32) / 255.0)
lb = cv2.resize(lum, (w * Z, h * Z), interpolation=cv2.INTER_NEAREST)
lb = np.clip(lb * 255, 0, 255).astype(np.uint8)
cv2.imencode(".png", lb)[1].tofile(str(out / "head_lum.png"))
print("wrote out/head_lum.png")
