"""Shift baked orange embers in the C001 still toward ice cyan. Leave skin/cloth."""
from pathlib import Path
import shutil
import numpy as np
from PIL import Image

src = Path(r"F:\天命之子\client\Assets\Resources\Art\Characters\C001\preview_new.png")
bak = src.with_name("preview_new.before_ice.png")
if not bak.exists():
    shutil.copy2(src, bak)

im = Image.open(src).convert("RGB")
arr = np.asarray(im).astype(np.float32)
r, g, b = arr[..., 0], arr[..., 1], arr[..., 2]
mx = np.maximum(np.maximum(r, g), b)
mn = np.minimum(np.minimum(r, g), b)
sat = (mx - mn) / (mx + 1e-5)
# orange motes: warm, fairly saturated, not peach skin (skin sat lower, more G)
ember = (r > g + 12) & (g > b + 8) & (sat > 0.42) & (r > 70) & (b < 90)
# dark warm void: lift blue a little
void = (mx < 55) & (r >= g) & (r >= b)

out = arr.copy()
er, eg, eb = out[..., 0][ember], out[..., 1][ember], out[..., 2][ember]
lum = 0.30 * er + 0.50 * eg + 0.20 * eb
out[..., 0][ember] = np.clip(lum * 0.55, 0, 255)
out[..., 1][ember] = np.clip(lum * 0.92 + 18, 0, 255)
out[..., 2][ember] = np.clip(lum * 1.15 + 36, 0, 255)

out[..., 2][void] = np.clip(out[..., 2][void] + 10, 0, 255)
out[..., 1][void] = np.clip(out[..., 1][void] + 4, 0, 255)

Image.fromarray(out.astype(np.uint8)).save(src)
print("ember_px", int(ember.sum()), "void_px", int(void.sum()), "saved", src)
# check crops
h, w = out.shape[:2]
for name, box in {
    "void": (0.02, 0.02, 0.28, 0.22),
    "chest": (0.30, 0.22, 0.72, 0.46),
    "sword": (0.08, 0.52, 0.40, 0.90),
}.items():
    l, t, r2, b2 = int(w * box[0]), int(h * box[1]), int(w * box[2]), int(h * box[3])
    Image.fromarray(out[t:b2, l:r2].astype(np.uint8)).save(Path(__file__).parent / f"ice_check_{name}.png")
