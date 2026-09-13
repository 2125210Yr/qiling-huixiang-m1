import sys
from pathlib import Path
import numpy as np
import cv2

sys.path.insert(0, str(Path(__file__).resolve().parent))
from partrig import Model, Skeleton

m = Model()
sk = Skeleton(m)
sk.resolve()

print("%-24s %-22s %8s %8s %8s" % ("part", "window", "src_a", "warp_a", "M[0:2,2]"))
for name in m.order:
    p = m.parts[name]
    M = sk.world[name]
    x0, y0, x1, y1 = m._win[name]
    src = p.img[y0:y1, x0:x1]
    Mw = M[:2, :].copy()
    Mw[0, 2] -= x0
    Mw[1, 2] -= y0
    w = cv2.warpAffine(src, Mw, (x1 - x0, y1 - y0), flags=cv2.INTER_LINEAR,
                       borderMode=cv2.BORDER_CONSTANT, borderValue=(0, 0, 0, 0))
    print("%-24s %-22s %8d %8d   %s" % (
        name, "%dx%d" % (x1 - x0, y1 - y0),
        int((src[..., 3] > 0.5).sum()), int((w[..., 3] > 0.5).sum()),
        np.round(Mw[:, 2], 3)))
