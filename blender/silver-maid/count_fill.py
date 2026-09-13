"""Count non-background pixels in a PNG. blender --background --python count_fill.py -- <png>"""
import os
import sys
import bpy

args = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
path = args[0] if args else os.path.join(os.path.dirname(__file__), "out", "threeq.png")
img = bpy.data.images.load(path)
w, h = img.size
pix = list(img.pixels)
n = w * h
filled = 0
for i in range(n):
    r, g, b = pix[i * 4], pix[i * 4 + 1], pix[i * 4 + 2]
    if max(r, g, b) > 0.04:
        filled += 1
frac = filled / float(n)
print("PNG", path)
print("SIZE", w, h, os.path.getsize(path))
print("FILLED", filled, "FRAC", "%.4f" % frac)
print("TALLER", h > w)
if frac < 0.08:
    raise SystemExit("fill too low")
if h <= w:
    raise SystemExit("not taller than wide")
if os.path.getsize(path) <= 10000:
    raise SystemExit("file too small")
