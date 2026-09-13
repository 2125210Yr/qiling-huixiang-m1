"""Does the underpaint actually close the holes a moving part leaves?

Renders the same driven frame twice -- bare split vs. split + underpaint --
and reports how much of the silhouette is transparent (a hole) in each.
"""
import sys
from pathlib import Path
import numpy as np
import cv2

sys.path.insert(0, str(Path(__file__).resolve().parent))
from partrig import Model, render, imread_unicode
from drive import Driver

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent
T = 6.0
DT = 1 / 60.0


def frame(use_underpaint: bool):
    m = Model()
    if use_underpaint:
        m.add_underpaint(ROOT / "recut-layers" / "underpaint.json",
                         ROOT / "recut-layers")
    drv = Driver(m)
    for i in range(int(T * 60) + 1):
        drv.step(i / 60.0, DT)
    return render(m, drv.sk)


ref = imread_unicode(ROOT / "presenter.png")
ref_a = ref[..., 3].astype(np.float32) / 255.0

panels = []
summary = []
for label, flag in (("bare", False), ("+underpaint", True)):
    c = frame(flag)
    a = c[..., 3]
    # A "hole" is a transparent patch ENCLOSED by opaque pixels.  Merely being
    # transparent inside the rest silhouette is not enough: the anti-aliased rim
    # shifts by a pixel whenever anything moves, and at this silhouette size
    # that rim is ~20k px, which swamps the real holes.  Erode hard, and require
    # the pixel to be surrounded by the current frame's own opaque area.
    er = cv2.erode((ref_a > 0.5).astype(np.uint8), np.ones((9, 9), np.uint8))
    solid = cv2.erode((a > 0.9).astype(np.uint8), np.ones((5, 5), np.uint8))
    enclosed = cv2.dilate(solid, np.ones((31, 31), np.uint8))
    hole = (er > 0) & (a < 0.3) & (enclosed > 0)
    n = int(hole.sum())
    summary.append((label, n))
    rgb = np.clip(c[..., :3] * 255, 0, 255).astype(np.uint8)
    vis = rgb.copy()
    vis[hole] = (0, 0, 255)
    cv2.putText(vis, "%s  holes %d px" % (label, n), (14, 32),
                cv2.FONT_HERSHEY_SIMPLEX, 0.9, (0, 255, 255), 2)
    panels.append(vis)

print("=" * 66)
print("hole test at t=%.0f s   (transparent where the rest silhouette is solid)" % T)
for label, n in summary:
    print("   %-14s %8d px" % (label, n))
if summary[0][1] > 0:
    print("   reduction      %.1f%%" % (100.0 * (1 - summary[1][1] / summary[0][1])))
print("=" * 66)

diff = np.clip(panels[0].astype(np.int16) - panels[1].astype(np.int16), 0, 255)
sheet = np.hstack([panels[0], panels[1]])
cv2.imencode(".png", sheet)[1].tofile(str(HERE / "out" / "underpaint_proof.png"))
print("wrote out/underpaint_proof.png  (bare | +underpaint, holes in red)")
