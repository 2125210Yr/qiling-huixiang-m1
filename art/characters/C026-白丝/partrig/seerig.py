"""Rig the See-Through layers.

The point of switching to these: every part carries underpaint (measured -- the
eye white sits 100% behind the iris, the face 69% behind the bangs), so a part
can move without exposing the hole it left.  The earlier hand-cut set had 0%
overlap and tore on contact.

Draw order and pivots are declared here rather than derived, because with
overlapping plates the correct order is whatever reproduces `src_img`, and that
is checkable directly -- see verify_see().
"""
from __future__ import annotations

import json
import sys
from pathlib import Path

import cv2
import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
from partrig import Model, Part, imread_unicode

SEE_DIR = Path(r"F:\see-through\workspace\c026\presenter")

#: back -> front.  `objects` (the clutch) and `handwear` sit in the arms' band.
ORDER = [
    "back hair", "tail", "wings",
    "legwear", "footwear",
    "bottomwear", "neck", "topwear", "neckwear",
    "handwear", "objects",
    "head", "ears", "face",
    "eyebrow", "eyewhite", "irides", "eyelash", "nose", "mouth",
    "headwear", "eyewear", "earwear",
    "front hair",
]

#: name -> (parent, pivot rule).  A rule names the anchor the part swings about.
#:   neck   : base of the head, so the head turns about the neck
#:   root   : a fraction down the part's own mask, i.e. where it attaches upward
#:   centroid / bbox_top / bbox_bottom
#: NOTE the root: exactly one entry may have `None`, and the skeleton only
#: resolves the subtree below it.  Leaving every part with a parent silently
#: rendered one layer and dropped the other eighteen.
RIG = {
    "back hair":  ("head",     ("root", 0.02)),
    "tail":       ("head",     "centroid"),
    "wings":      ("topwear",  "centroid"),
    "legwear":    ("bottomwear", ("root", 0.02)),
    "footwear":   ("legwear",  "bbox_bottom"),
    "bottomwear": ("topwear",  ("root", 0.10)),
    "neck":       ("topwear",  ("root", 0.02)),
    "topwear":    (None,       ("root", 0.55)),
    "neckwear":   ("topwear",  "centroid"),
    "handwear":   ("topwear",  "centroid"),
    "objects":    ("handwear", "centroid"),
    "head":       ("neck",     ("root", 0.30)),
    "ears":       ("head",     "centroid"),
    "face":       ("head",     "centroid"),
    "eyebrow":    ("face",     "centroid"),
    "eyewhite":   ("face",     "centroid"),
    "irides":     ("eyewhite", "centroid"),
    "eyelash":    ("face",     ("root", 0.10)),
    "nose":       ("face",     "centroid"),
    "mouth":      ("face",     "centroid"),
    "headwear":   ("head",     "centroid"),
    "eyewear":    ("face",     "centroid"),
    "earwear":    ("ears",     "centroid"),
    "front hair": ("head",     ("root", 0.04)),
}


def _pivot_for(mask: np.ndarray, rule) -> tuple[float, float]:
    ys, xs = np.nonzero(mask)
    if len(ys) == 0:
        return 0.0, 0.0
    y0, y1 = ys.min(), ys.max()
    x0, x1 = xs.min(), xs.max()
    if rule == "centroid":
        return float(xs.mean()), float(ys.mean())
    if rule == "bbox_top":
        return float((x0 + x1) / 2), float(y0)
    if rule == "bbox_bottom":
        return float((x0 + x1) / 2), float(y1)
    if isinstance(rule, tuple) and rule[0] == "root":
        # anchor a fraction down from the top, centred on the mask's top band --
        # that is where a part hangs from, and it is what keeps a swing from
        # tearing the attachment
        band = max(1, int((y1 - y0) * 0.14))
        sel = ys <= y0 + band
        return float(xs[sel].mean()), float(y0 + (y1 - y0) * rule[1])
    raise ValueError(rule)


def load_see_model(d: Path = SEE_DIR) -> Model:
    layers = {}
    for name in ORDER:
        p = d / (name + ".png")
        if not p.exists():
            continue
        img = imread_unicode(p)
        if img is None or img[..., 3].max() == 0:
            continue
        layers[name] = img

    m = Model.__new__(Model)          # bypass the C026 loader
    m.W, m.H = layers[next(iter(layers))].shape[1], layers[next(iter(layers))].shape[0]
    m.order = [n for n in ORDER if n in layers]
    m.parts = {}
    m._win = {}
    for name in m.order:
        img = layers[name]
        f = img.astype(np.float32) / 255.0
        f[..., :3] *= f[..., 3:4]          # premultiplied
        parent, rule = RIG.get(name, (None, "centroid"))
        px, py = _pivot_for(img[..., 3] > 8, rule)
        ys, xs = np.nonzero(img[..., 3] > 8)
        m.parts[name] = Part(name=name, part_id=name, draw_index=m.order.index(name),
                             parent=parent if parent in layers else None,
                             pivot=(px, py),
                             bbox=(int(xs.min()), int(ys.min()),
                                   int(xs.max()) + 1, int(ys.max()) + 1),
                             img=f)
        reach = int(0.45 * max(xs.max() - xs.min(), ys.max() - ys.min())) + 40
        m._win[name] = (max(0, int(xs.min()) - reach), max(0, int(ys.min()) - reach),
                        min(m.W, int(xs.max()) + reach), min(m.H, int(ys.max()) + reach))
    m.root = next((n for n, p in m.parts.items() if p.parent is None), m.order[0])
    m.children = lambda name: [n for n, p in m.parts.items() if p.parent == name]
    return m


def verify_see(d: Path = SEE_DIR) -> float:
    """Composite the layers at identity and compare with src_img."""
    from partrig import Skeleton, render
    m = load_see_model(d)
    print("  parts %d, root=%s, canvas %dx%d" % (len(m.parts), m.root, m.W, m.H))
    sk = Skeleton(m)
    sk.resolve()
    c = render(m, sk)
    src = imread_unicode(d / "src_img.png")
    a = src[..., 3:4].astype(np.float32) / 255.0
    ref = src[..., :3].astype(np.float32) / 255.0 * a
    diff = np.abs(c[..., :3] - ref).max(axis=2)
    solid = (a[..., 0] > 0.995)
    print("  aligned composite vs src_img:")
    print("    interior max %.4f/255   mean %.6f" % (diff[solid].max() * 255, diff[solid].mean()))
    return diff[solid].max() * 255


if __name__ == "__main__":
    print("=" * 66)
    print("See-Through layer rig")
    print("=" * 66)
    verify_see()
