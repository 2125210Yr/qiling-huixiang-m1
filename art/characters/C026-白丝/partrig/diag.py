import sys
from pathlib import Path
import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
from partrig import Model, Skeleton

m = Model()
print("root =", m.root)
print("\n%-24s %-24s %10s %10s" % ("part", "parent", "alpha_px", "bbox"))
tot = 0
for n in m.order:
    p = m.parts[n]
    a = int((p.img[..., 3] > 0.5).sum())
    tot += a
    print("%-24s %-24s %10d %s" % (n, str(p.parent), a, p.bbox))
print("\nsum of per-part alpha: %d" % tot)

sk = Skeleton(m)
w = sk.resolve()
missing = [n for n in m.order if n not in w]
print("parts with NO world matrix: %s" % (missing or "none"))

# whose parent points at something that is not a part?
names = set(m.parts)
for n in m.order:
    pa = m.parts[n].parent
    if pa is not None and pa not in names:
        print("DANGLING parent: %s -> %s" % (n, pa))
