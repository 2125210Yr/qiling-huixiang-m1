"""Which part paints the dark wedge on the left thigh at t=6?"""
import sys
from pathlib import Path
import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
from partrig import Model, Skeleton, render, imread_unicode
from drive import Driver

m = Model()
drv = Driver(m)
dt = 1 / 60.0
for i in range(int(6.0 * 60) + 1):
    drv.step(i / 60.0, dt)

# where the wedge appears (canvas px), read off the pose sheet
BX0, BY0, BX1, BY1 = 150, 690, 320, 900

full = render(m, drv.sk)
box_a = full[BY0:BY1, BX0:BX1, 3] > 0.5
print("composite in box: %d opaque px" % box_a.sum())

# reference: what the still has there
ref = imread_unicode(Path(__file__).resolve().parent.parent / "presenter.png")
ref_a = (ref[..., 3].astype(np.float32) / 255.0)[BY0:BY1, BX0:BX1] > 0.5
print("presenter in box: %d opaque px" % ref_a.sum())

print("\nper-part contribution inside the box (drawn alone):")
for name in m.order:
    solo = render(m, drv.sk, only=name)
    a = solo[BY0:BY1, BX0:BX1, 3] > 0.5
    n = int(a.sum())
    if n < 20:
        continue
    rgb = solo[BY0:BY1, BX0:BX1, :3][a]        # premultiplied already
    lum = float((rgb.mean(axis=1)).mean())
    p = m.parts[name]
    print("  %-22s %7d px  mean_lum %.3f %s   tx=%.1f ty=%.1f rot=%.3f shear=%.3f"
          % (name, n, lum, "DARK" if lum < 0.25 else "    ",
             p.tx, p.ty, p.rot, p.shear))
