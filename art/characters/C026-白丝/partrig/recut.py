"""Re-cut C026 so the parts can actually deform.

The existing `layers/` were cut for *compositing*: every part fits its neighbour
exactly, which is what you want to rebuild a still and exactly what you must not
have if the parts are going to move.  Move a part one pixel and you see the hole
it left.

`psd_cut_plan.json` already says what is missing -- eight `underpaint` pieces --
but they were never cut.  This produces them, plus the facial sub-layers the
plan names (face / brow / eyewhite / iris / lash / mouth ...) which the current
split collapses into one baked `head` plate.

Method for underpaint: take the part that will move, grow its footprint in the
direction it can travel, and fill that band by inpainting the still.  Inpaint
pulls from the adjacent material, so a bangs footprint becomes more hair, a slit
footprint becomes more thigh -- a plausible continuation rather than a hole.

Nothing here writes to `layers/`.  Output goes to `recut-layers/`.
"""
from __future__ import annotations

import json
import sys
from dataclasses import dataclass
from pathlib import Path

import cv2
import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
from partrig import Model, imread_unicode

ROOT = Path(__file__).resolve().parent.parent
DST = ROOT / "recut-layers"


def imwrite_unicode(path: Path, img: np.ndarray) -> None:
    ok, buf = cv2.imencode(".png", img)
    if not ok:
        raise RuntimeError("encode failed for %s" % path)
    buf.tofile(str(path))


# --------------------------------------------------------------- underpaint

@dataclass
class Underpaint:
    name: str
    #: parts whose footprint defines the band that must not show a hole
    covers: list[str]
    #: grow the footprint this many px (dx0, dy0, dx1, dy1)
    grow: tuple[int, int, int, int]
    #: parts whose MATERIAL this band should be made of.  Sampling the composited
    #: still instead is what turned the thigh's underpaint into hair colour --
    #: the nearest opaque thing to the left hip in the still is a white tail.
    source: list[str] = None
    #: restrict to this canvas box, or None
    clip: tuple[int, int, int, int] | None = None
    note: str = ""


#: the eight pieces psd_cut_plan.json names, mapped onto this character
PLAN = [
    Underpaint("scalp_behind_bangs", ["hair_front"], (-6, -4, 6, 18),
               source=["hair_side_l", "hair_side_r", "hair_back_l", "hair_back_r"],
               note="刘海后的头皮：刘海一甩，露出的应该是头发不是皮肤"),
    Underpaint("forehead_behind_front_hair", ["hair_front"], (-4, 8, 4, 22),
               source=["head"],
               note="刘海后的额头"),
    Underpaint("neck_inside_collar", ["accessory_choker"], (-3, -3, 3, 6),
               source=["head"],
               note="领口内的脖子：项圈移动时不能露洞"),
    # The leg pieces are clipped to the hip / slit band.  Growing a 400 px
    # thigh in every direction produces a 50k px plate, which is just a copy of
    # the leg and buys nothing -- what has to survive the suit moving is the
    # band the suit's edge actually cuts across.
    Underpaint("thigh_inside_slit", ["leg_upper_l", "leg_upper_r"], (-6, 0, 6, 26),
               source=["leg_upper_l", "leg_upper_r"],
               clip=(245, 400, 490, 600),
               note="开叉内的大腿：高开叉边缘一动就要有大腿接着"),
    Underpaint("raised_leg_hip_crease", ["leg_upper_l"], (-10, -8, 10, 14),
               source=["leg_upper_l"],
               clip=(245, 360, 372, 505),
               note="抬腿侧胯部折痕"),
    Underpaint("standing_leg_hip_under_dress", ["leg_upper_r"], (-10, -8, 10, 14),
               source=["leg_upper_r"],
               clip=(348, 360, 500, 505),
               note="支撑腿侧裙下胯部"),
    Underpaint("mouth_interior_larger", ["head"], (-3, -3, 3, 4),
               source=["head"],
               clip=(345, 166, 378, 190),
               note="比静止开口更大的口腔：嘴张开时要有内壁"),
    Underpaint("eye_white_behind_iris", ["head"], (-3, -3, 3, 3),
               source=["head"],
               clip=(335, 126, 402, 172),
               note="虹膜后的眼白：眼球转动时不能露洞"),
]


def grow_mask(mask: np.ndarray, grow) -> np.ndarray:
    dx0, dy0, dx1, dy1 = grow
    h, w = mask.shape
    out = np.zeros_like(mask)
    ys, xs = np.nonzero(mask)
    for y, x in zip(ys, xs):
        out[max(0, y + dy0):min(h, y + dy1 + 1),
            max(0, x + dx0):min(w, x + dx1 + 1)] = 255
    return out


model_HW = (1127, 672)


def make_underpaint(plates: dict, spec: Underpaint) -> tuple:
    """Fill the grown footprint with the SOURCE parts' own material.

    Inpainting the composited still is the obvious thing and it is wrong: the
    nearest opaque pixel to the left hip in the still belongs to a white
    ponytail, so a "thigh" underpaint comes out hair-coloured.  Filling from the
    plate that owns the material guarantees the extension is the right stuff.
    """
    h, w = model_HW

    foot = np.zeros((h, w), np.uint8)
    for name in spec.covers:
        if name in plates:
            foot |= (plates[name] > 0).astype(np.uint8) * 255
    band = grow_mask(foot, spec.grow)
    band = cv2.morphologyEx(band, cv2.MORPH_CLOSE, np.ones((3, 3), np.uint8))
    if spec.clip:
        x0, y0, x1, y1 = spec.clip
        keep = np.zeros_like(band)
        keep[y0:y1, x0:x1] = 255
        band &= keep

    rgb = plates["__rgb__"]
    hold = np.zeros((h, w, 3), np.uint8)      # material we are allowed to use
    for name in (spec.source or []):
        if name not in plates or name == "__rgb__":
            continue
        m = plates[name] > 0
        hold[m] = rgb[name][m]

    need = band.copy()
    need[hold.any(axis=2)] = 0                # only synthesise what is missing
    filled = cv2.inpaint(hold, need, 7, cv2.INPAINT_TELEA)
    filled[band == 0] = 0
    return filled, band


# -------------------------------------------------------------- face split

#: Feature boxes, read off a 6x zoom of the head plate.  At 86 px of head there
#: is nothing to fit; global thresholds bleed hair into "dark" and miss the
#: sclera entirely, so each feature gets an explicit box and its own cut.
FACE_BOX = {
    "brow_l":     (340, 124, 366, 138),
    "brow_r":     (370, 143, 394, 156),
    "eye_l":      (339, 130, 366, 152),
    "eye_r":      (372, 148, 396, 168),
    "mouth":      (347, 169, 372, 187),
}


def cut_box(head_rgba: np.ndarray, box) -> dict:
    """Segment one feature inside its own box."""
    x0, y0, x1, y1 = box
    sub = head_rgba[y0:y1, x0:x1]
    b, g, r, a = (sub[..., i].astype(np.float32) for i in range(4))
    bgr = sub[..., :3]
    lum = cv2.cvtColor(bgr, cv2.COLOR_BGR2GRAY).astype(np.float32)
    mx = bgr.max(axis=2).astype(np.float32)
    mn = bgr.min(axis=2).astype(np.float32)
    sat = mx - mn
    warm = r - b
    op = a > 8

    # sclera: the eye's own light interior.  Relative to the box, not absolute:
    # skin here can be nearly as bright as the sclera, but it is warm.
    bright = op & (lum > np.percentile(lum[op], 62)) & (sat < 40)
    dark = op & (lum < np.percentile(lum[op], 34))
    iris = op & (sat >= 22) & ~bright & ~dark
    return dict(sub=sub, op=op, lum=lum, sat=sat, warm=warm,
                dark=dark, bright=bright, iris=iris,
                box=(x0, y0, x1, y1))


def split_face(head_rgba: np.ndarray, box) -> dict:
    """Cut the baked head plate into the sub-layers psd_cut_plan names.

    Works on colour and luminance inside a hand-set box around the eyes and
    mouth: at this scale (a 70 px face) there is nothing to fit, only thresholds
    that have to be eyeballed, so the box and thresholds are explicit below.
    """
    x0, y0, x1, y1 = box
    sub = head_rgba[y0:y1, x0:x1]
    b, g, r, a = sub[..., 0], sub[..., 1], sub[..., 2], sub[..., 3]
    op = a > 8
    lum = cv2.cvtColor(sub[..., :3], cv2.COLOR_BGR2GRAY)
    warm = (r.astype(np.int16) - b.astype(np.int16))          # skin is warm
    sat = (sub[..., :3].max(axis=2).astype(np.int16)
           - sub[..., :3].min(axis=2).astype(np.int16))

    dark = op & (lum < 135)                        # lash, brow, liner
    very_bright = op & (lum > 168) & (sat < 46)    # sclera
    warm_skin = op & (warm > 22) & (lum > 118)

    cls = np.zeros(sub.shape[:2] + (3,), np.uint8)
    cls[warm_skin] = (60, 60, 220)      # red-ish = skin
    cls[very_bright] = (230, 230, 230)  # white = sclera
    cls[dark] = (30, 30, 30)            # black = lash/brow/liner
    return dict(sub=sub, op=op, lum=lum, dark=dark, sclera=very_bright,
                skin=warm_skin, cls=cls)


def main() -> int:
    model = Model()
    # per-part alpha (what the part owns) and its own colour (what it is made of)
    parts = {n: p.img[..., 3] for n, p in model.parts.items()}
    rgbs = {}
    for n, p in model.parts.items():
        a = p.img[..., 3:4]
        un = np.divide(p.img[..., :3], a, out=np.zeros_like(p.img[..., :3]),
                       where=a > 1e-4)          # plates are stored premultiplied
        rgbs[n] = np.clip(un * 255, 0, 255).astype(np.uint8)
    parts["__rgb__"] = rgbs

    DST.mkdir(exist_ok=True)
    print("=" * 74)
    print("re-cut C026  ->  %s" % DST)
    print("=" * 74)

    # ---- 1. underpaint ----------------------------------------------------
    print("\n[1] underpaint  (this is what stops a moving part showing a hole)")
    manifest = []
    for spec in PLAN:
        filled, band = make_underpaint(parts, spec)
        cov = int((band > 0).sum())
        plate = np.dstack([filled, band])
        imwrite_unicode(DST / ("underpaint_%s.png" % spec.name), plate)
        manifest.append(dict(name="underpaint_%s" % spec.name, kind="underpaint",
                             covers=spec.covers, grow=list(spec.grow),
                             pixels=cov, note=spec.note))
        print("   %-34s %7d px  %s" % (spec.name, cov, spec.note))

    meta = dict(source="presenter.png", canvas=dict(width=model.W, height=model.H),
                plate_space="full canvas RGBA, same convention as live2d-export",
                layers=manifest)
    (DST / "underpaint.json").write_text(
        json.dumps(meta, ensure_ascii=False, indent=2), encoding="utf-8")
    print("\n   wrote underpaint.json")

    # ---- 2. what the face actually gives us --------------------------------
    print("\n[2] face analysis  (head plate is only %dx%d)"
          % (model.parts["head"].bbox[2] - model.parts["head"].bbox[0],
             model.parts["head"].bbox[3] - model.parts["head"].bbox[1]))
    head = imread_unicode(ROOT / "live2d-export" / "head.png")
    x0, y0, x1, y1 = model.parts["head"].bbox
    f = split_face(head, (x0, y0, x1, y1))
    print("   head box %s  opaque %d px" % ((x0, y0, x1, y1), int(f["op"].sum())))
    print("   dark(lash/brow/liner) %5d px" % int(f["dark"].sum()))
    print("   sclera                %5d px" % int(f["sclera"].sum()))
    print("   warm skin             %5d px" % int(f["skin"].sum()))

    # ---- per-feature segmentation, shown so the boxes can be judged ---------
    print("\n[3] per-feature cut inside hand-set boxes")
    Z = 8
    panels = []
    for name, box in FACE_BOX.items():
        c = cut_box(head, box)
        h, w = c["op"].shape
        cls = np.zeros((h, w, 3), np.uint8)
        cls[c["bright"]] = (235, 235, 235)
        cls[c["iris"]] = (60, 200, 60)
        cls[c["dark"]] = (40, 40, 230)
        cls[~c["op"]] = (80, 80, 80)
        orig = c["sub"].copy()
        aa = orig[..., 3:4].astype(np.float32) / 255.0
        orig = np.clip(orig[..., :3].astype(np.float32) * aa + 70 * (1 - aa), 0, 255
                       ).astype(np.uint8)
        pair = np.hstack([orig, cls])
        pair = cv2.resize(pair, None, fx=Z, fy=Z, interpolation=cv2.INTER_NEAREST)
        cv2.putText(pair, name, (6, 22), cv2.FONT_HERSHEY_SIMPLEX, 0.7,
                    (0, 255, 255), 2)
        panels.append(pair)
        print("   %-9s box %-22s dark %4d  iris %4d  bright %4d"
              % (name, str(box), c["dark"].sum(), c["iris"].sum(), c["bright"].sum()))

    maxh = max(p.shape[0] for p in panels)
    panels = [np.vstack([p, np.full((maxh - p.shape[0], p.shape[1], 3), 40, np.uint8)])
              if p.shape[0] < maxh else p for p in panels]
    imwrite_unicode(DST / "face_cut_check.png", np.hstack(panels))
    print("   wrote recut-layers/face_cut_check.png   (orig | cut, per feature)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
