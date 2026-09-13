"""Part-based rig for C026.

Why this exists: a single-plate warp drives every pixel with one continuous
displacement field, so the chest bouncing also drags the face and the feet.
Real Live2D does not work that way -- it is a stack of independent parts, each
with its own pivot and its own deformer.  The chest is a different part from the
face, so one can move without touching the other.

`live2d-export/` gives exactly what that needs:
  - 28 full-canvas RGBA plates (so compositing is a straight overlay, no offsets)
  - `draw_order_back_to_front`
  - `pivots.json` with `pivot_px` + `parent` per part, root = `body`

So the rig here is: a skeleton (one local transform per part, composed through
the parent chain) plus a back-to-front compositor.  Nothing is coupled unless
the hierarchy says so.

    python partrig.py --sheet        # render the rest pose + a few poses
"""
from __future__ import annotations

import json
import math
from dataclasses import dataclass, field
from pathlib import Path

import cv2
import numpy as np

EXPORT = Path(__file__).resolve().parent.parent / "live2d-export"
CANVAS = (672, 1127)          # w, h


def imread_unicode(path) -> np.ndarray | None:
    """cv2.imread cannot open non-ASCII Windows paths."""
    try:
        buf = np.fromfile(str(path), dtype=np.uint8)
    except OSError:
        return None
    if buf.size == 0:
        return None
    return cv2.imdecode(buf, cv2.IMREAD_UNCHANGED)


# --------------------------------------------------------------------- model

@dataclass
class Part:
    name: str
    part_id: str
    draw_index: int
    parent: str | None
    pivot: tuple[float, float]
    bbox: tuple[int, int, int, int]
    img: np.ndarray = field(default=None, repr=False)   # BGRA float32 0..1
    # local transform, written by the driver each frame
    rot: float = 0.0
    sx: float = 1.0
    sy: float = 1.0
    tx: float = 0.0
    ty: float = 0.0
    shear: float = 0.0


class Model:
    def __init__(self, export: Path = EXPORT) -> None:
        self.parts_json = json.loads((export / "parts.json").read_text(encoding="utf-8"))
        self.pivots_json = json.loads((export / "pivots.json").read_text(encoding="utf-8"))
        self.W, self.H = CANVAS

        pv = self.pivots_json["parts"]
        order = self.parts_json["draw_order_back_to_front"]
        self.order = order
        self.parts: dict[str, Part] = {}
        for name in order:
            p = pv[name]
            img = imread_unicode(export / f"{name}.png")
            if img is None:
                raise RuntimeError("missing plate %s.png" % name)
            if img.shape[2] == 3:
                img = np.dstack([img, np.full(img.shape[:2], 255, np.uint8)])
            f = img.astype(np.float32) / 255.0
            # Store PREMULTIPLIED.  Warping straight (non-premultiplied) alpha
            # bilinearly mixes a part's colour with the black that sits under
            # its transparent pixels, which paints dark fringes along every
            # edge the moment the part rotates.
            f[..., :3] *= f[..., 3:4]
            self.parts[name] = Part(
                name=name, part_id=p["part_id"], draw_index=p["draw_index"],
                parent=p.get("parent"), pivot=tuple(p["pivot_px"]),
                bbox=tuple(p["bbox"]), img=f,
            )
        self.root = next((n for n, p in self.parts.items() if p.parent is None), "body")

        # Cached warp window per part.  The margin has to cover how far the
        # part's content can swing once a transform is applied -- a thigh 400 px
        # tall rotating about its hip moves its silhouette well past its own
        # bbox, and clipping that shows up as a hard-edged rectangle.
        self._win: dict[str, tuple[int, int, int, int]] = {}
        for n, p in self.parts.items():
            x0, y0, x1, y1 = p.bbox
            reach = int(0.35 * max(x1 - x0, y1 - y0)) + 24
            self._win[n] = (max(0, x0 - reach), max(0, y0 - reach),
                            min(self.W, x1 + reach), min(self.H, y1 + reach))

    def add_underpaint(self, spec_path, plate_dir) -> list[str]:
        """Splice underpaint plates into the draw order, each one just behind
        the earliest-drawn part it exists to back."""
        import json as _json
        spec = _json.loads(Path(spec_path).read_text(encoding="utf-8"))
        inserted = []
        for entry in spec["layers"]:
            name = entry["name"]
            img = imread_unicode(Path(plate_dir) / ("%s.png" % name))
            if img is None:
                continue
            f = img.astype(np.float32) / 255.0
            f[..., :3] *= f[..., 3:4]
            covers = [c for c in entry["covers"] if c in self.parts]
            if not covers:
                continue
            at = min(self.order.index(c) for c in covers)
            self.parts[name] = Part(
                name=name, part_id=name, draw_index=at, parent=self.root,
                pivot=(float(self.W) / 2, float(self.H) / 2),
                bbox=(0, 0, self.W, self.H), img=f)
            self.order.insert(at, name)
            # re-index the parts that shifted
            for i, n in enumerate(self.order):
                self.parts[n].draw_index = i
            xs = np.nonzero(f[..., 3] > 0)
            m = 60
            self._win[name] = (max(0, int(xs[1].min()) - m) if xs[1].size else 0,
                               max(0, int(xs[0].min()) - m) if xs[0].size else 0,
                               min(self.W, int(xs[1].max()) + m) if xs[1].size else 0,
                               min(self.H, int(xs[0].max()) + m) if xs[0].size else 0)
            inserted.append(name)
            print("   underpaint %-34s behind %s" % (name, covers[0]))
        return inserted

    def children(self, name: str) -> list[str]:
        return [n for n, p in self.parts.items() if p.parent == name]


# ----------------------------------------------------------------- transforms

def _about(pivot, M, pt):
    px, py = pivot
    x, y = pt
    dx, dy = x - px, y - py
    return (M[0, 0] * dx + M[0, 1] * dy + px + M[0, 2],
            M[1, 0] * dx + M[1, 1] * dy + py + M[1, 2])


def _trans(x: float, y: float) -> np.ndarray:
    return np.array([[1.0, 0.0, x], [0.0, 1.0, y], [0.0, 0.0, 1.0]], np.float64)


def local_matrix(p: Part) -> np.ndarray:
    """Scale -> shear -> rotate about the part's own pivot, then translate.

    Everything is homogeneous 3x3 so the parent chain is a plain product.
    """
    c, s = math.cos(p.rot), math.sin(p.rot)
    R = np.array([[c, -s], [s, c]], np.float64)
    S = np.array([[p.sx, 0.0], [0.0, p.sy]], np.float64)
    H = np.array([[1.0, p.shear], [0.0, 1.0]], np.float64)
    A = R @ S @ H
    M = np.eye(3, dtype=np.float64)
    M[:2, :2] = A
    M[:2, 2] = (p.tx, p.ty)
    return M


def compose(parent: np.ndarray | None, child_local: np.ndarray,
            child_pivot) -> np.ndarray:
    """A local transform acts about the child's own pivot, so conjugate it by
    that pivot before folding in the parent:  world = parent . T(p) . L . T(-p).
    """
    px, py = child_pivot
    M = _trans(px, py) @ child_local @ _trans(-px, -py)
    return M if parent is None else parent @ M


class Skeleton:
    """World matrices for every part, resolved through the parent chain."""

    def __init__(self, model: Model) -> None:
        self.m = model
        self.world: dict[str, np.ndarray] = {}

    def resolve(self) -> dict[str, np.ndarray]:
        m = self.m
        self.world = {}

        def walk(name: str, parent_world: np.ndarray | None) -> None:
            p = m.parts[name]
            local = local_matrix(p)
            w = compose(parent_world, local, p.pivot)
            self.world[name] = w
            for c in m.children(name):
                walk(c, w)

        walk(m.root, None)
        return self.world


# ---------------------------------------------------------------- compositor

def render(model: Model, skeleton: Skeleton, out: np.ndarray | None = None,
           hits: dict[str, tuple] | None = None,
           only: str | None = None) -> np.ndarray:
    """Draw every part back to front.  `hits` optionally receives, per part, the
    window it was drawn into (for tests).  `only` isolates one part."""
    W, H = model.W, model.H
    canvas = np.zeros((H, W, 4), np.float32) if out is None else out
    canvas[:] = 0.0
    world = skeleton.world

    for name in model.order:
        if only is not None and name != only:
            continue
        p = model.parts[name]
        M = world.get(name)
        if M is None:
            continue
        x0, y0, x1, y1 = model._win[name]
        win_w, win_h = x1 - x0, y1 - y0
        if win_w <= 0 or win_h <= 0:
            continue
        # Both the source and the destination are the part's window, so the
        # canvas-space matrix has to be conjugated by the window origin:
        #     src_win = M . (dst_win + o) - o
        # Just subtracting o from the translation is wrong - it shifts the part
        # clean off the canvas.
        Mw = M[:2, :].copy()
        o = np.array([x0, y0], np.float64)
        Mw[:, 2] = M[:2, 2] + M[:2, :2] @ o - o
        src = p.img[y0:y1, x0:x1]
        warped = cv2.warpAffine(src, Mw, (win_w, win_h), flags=cv2.INTER_LINEAR,
                                borderMode=cv2.BORDER_CONSTANT, borderValue=(0, 0, 0, 0))
        dst = canvas[y0:y1, x0:x1]
        a = warped[..., 3:4]
        # premultiplied "over":  out = src + dst * (1 - src_a)
        dst *= (1.0 - a)
        dst += warped
        if hits is not None:
            hits[name] = (x0, y0, x1, y1)
    return canvas
