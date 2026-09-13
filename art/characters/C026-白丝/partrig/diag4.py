"""Dump one driven frame next to the still, plus where they differ."""
import sys
from pathlib import Path
import numpy as np
import cv2

sys.path.insert(0, str(Path(__file__).resolve().parent))
from partrig import Model, render, imread_unicode
from drive import Driver

m = Model()
drv = Driver(m)
dt = 1 / 60.0
T = 6.0
for i in range(int(T * 60) + 1):
    drv.step(i / 60.0, dt)

c = render(m, drv.sk)
got = np.clip(c[..., :3] * 255, 0, 255).astype(np.uint8)      # premultiplied over black

ref = imread_unicode(Path(__file__).resolve().parent.parent / "presenter.png")
ref_rgb = ref[..., :3].astype(np.float32) / 255.0
ref_a = ref[..., 3:4].astype(np.float32) / 255.0
still = np.clip(ref_rgb * ref_a * 255, 0, 255).astype(np.uint8)

d = np.abs(c[..., :3] - ref_rgb * ref_a).max(axis=2)
heat = np.clip(d * 3, 0, 1)
heat = (np.dstack([heat, heat * 0.25, 1.0 - heat]) * 255).astype(np.uint8)

ys, xs = np.where(d > 30 / 255)
print("pixels differing >30/255 between driven frame and still: %d" % len(ys))
if len(ys):
    print("  bbox x[%d..%d] y[%d..%d]" % (xs.min(), xs.max(), ys.min(), ys.max()))
    # cluster by y band to see if it is one region or edges everywhere
    hist, edges = np.histogram(ys, bins=12, range=(0, m.H))
    for i, h in enumerate(hist):
        print("    y %4d-%4d : %6d px" % (edges[i], edges[i + 1], h))

sheet = np.hstack([still, got, heat])
cv2.imencode(".png", sheet)[1].tofile(str(Path(__file__).resolve().parent / "out" / "frame_vs_still.png"))
print("wrote out/frame_vs_still.png  (still | driven t=6 | diff heat)")
