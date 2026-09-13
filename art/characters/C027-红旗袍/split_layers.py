"""Coarse Live2D layers from the elevator reference frame.

WARNING: Writes only to this folder's layers/. Do not retarget OUT to
art/live2d-lab/elevator-red — that is the canonical Live2D workdir and
must not be overwritten by this blocking pass. See POINTER.md.

Pipeline: letterbox crop -> 4x upscale -> rembg figure alpha -> colour classes
(skin / dress / hair) -> geometric zone cuts -> back-to-front plates.

The source is a 546x292 video screenshot, so every plate here is a coarse
blocking pass meant to be repainted, not shipped. Landmarks are measured from
the masks and echoed into report.json so the zone constants can be retuned
against real pixel coordinates.
"""
from __future__ import annotations

import json
from pathlib import Path

import cv2
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parent
SRC = ROOT / "reference-src.jpg"
WORK = ROOT / "reference-work.png"
OUT = ROOT / "layers"
DBG = OUT / "_dbg"

SCALE = 4
# u2net beat isnet-anime and u2net_human_seg on this frame (see _probe/alpha_report.json):
# one connected component covering horns->skirt, where isnet-anime found only a lit foot.
REMBG_MODEL = "u2net"

# Zone cuts as fractions of the figure bbox, retuned from report.json.
HIP_FRAC = 0.520         # below this row skin becomes legs
TORSO_HALF_FRAC = 0.115  # half-width of the torso column, as a fraction of fig width
FACE_H_FRAC = 0.160      # face+neck band height, from the topmost lit skin row
FACE_HALF_FRAC = 0.110   # face half-width
K = np.ones((3, 3), np.uint8)

# Back-to-front. The kicked leg swings in front of the skirt; the standing leg is
# behind it. Missing plates are skipped.
DRAW_ORDER = (
    "hair_back",
    "leg_stand",
    "torso",
    "dress",
    "leg_kick",
    "arm_hip",
    "head",
    "hair_front",
    "horn_l",
    "horn_r",
)


def letterbox_crop(rgb: np.ndarray) -> tuple[int, int, int, int]:
    """Trim the near-black video bars. Returns (x0, y0, x1, y1)."""
    luma = rgb.astype(np.float32).mean(axis=2)
    rows = np.where(luma.mean(axis=1) > 12)[0]
    cols = np.where(luma.mean(axis=0) > 12)[0]
    if len(rows) == 0 or len(cols) == 0:
        return 0, 0, rgb.shape[1], rgb.shape[0]
    return int(cols[0]), int(rows[0]), int(cols[-1]) + 1, int(rows[-1]) + 1


def keep_largest(mask: np.ndarray) -> np.ndarray:
    n, lab, stats, _ = cv2.connectedComponentsWithStats(mask.astype(np.uint8), 8)
    if n <= 1:
        return mask
    return lab == 1 + int(np.argmax(stats[1:, cv2.CC_STAT_AREA]))


def drop_small(mask: np.ndarray, min_area: int) -> np.ndarray:
    n, lab, stats, _ = cv2.connectedComponentsWithStats(mask.astype(np.uint8), 8)
    out = np.zeros_like(mask, dtype=bool)
    for i in range(1, n):
        if int(stats[i, cv2.CC_STAT_AREA]) >= min_area:
            out |= lab == i
    return out


def fill_holes(mask: np.ndarray) -> np.ndarray:
    filled = mask.astype(np.uint8) * 255
    cnts, _ = cv2.findContours(filled, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
    cv2.drawContours(filled, cnts, -1, 255, cv2.FILLED)
    return filled > 128


def clean_figure(mask: np.ndarray) -> np.ndarray:
    m = cv2.morphologyEx(mask.astype(np.uint8) * 255, cv2.MORPH_CLOSE, K, iterations=4) > 128
    return fill_holes(keep_largest(m))


def grabcut_refine(work_rgb: np.ndarray, alpha: np.ndarray) -> np.ndarray:
    """Snap the soft u2net alpha onto real colour edges.

    u2net runs at 320px internally, so its boundary is a blurred guess. Feeding it
    back as a GrabCut trimap recovers edges at the work resolution.
    """
    gc = np.full(alpha.shape, cv2.GC_PR_BGD, np.uint8)
    gc[alpha >= 90] = cv2.GC_PR_FGD
    gc[alpha > 200] = cv2.GC_FGD
    gc[alpha < 25] = cv2.GC_BGD
    if not (gc == cv2.GC_FGD).any() or not (gc == cv2.GC_BGD).any():
        return alpha > 40
    bgd, fgd = np.zeros((1, 65), np.float64), np.zeros((1, 65), np.float64)
    cv2.grabCut(work_rgb, gc, None, bgd, fgd, 5, cv2.GC_INIT_WITH_MASK)
    return (gc == cv2.GC_FGD) | (gc == cv2.GC_PR_FGD)


def figure_alpha(work: Image.Image) -> tuple[np.ndarray, str]:
    """Figure mask, judged on post-cleanup coverage rather than raw alpha area.

    A raw alpha can pass an area check and still collapse to one bright limb once
    the largest component is kept, so score the candidates after cleanup.
    """
    work_rgb = np.asarray(work)
    try:
        from rembg import new_session, remove

        cut = remove(work, session=new_session(REMBG_MODEL))
        alpha = np.asarray(cut.convert("RGBA"))[:, :, 3]
        refined = clean_figure(grabcut_refine(work_rgb, alpha))
        plain = clean_figure(alpha > 40)
        if refined.sum() >= 0.5 * plain.sum() and refined.sum() > 0.03 * alpha.size:
            return refined, f"rembg:{REMBG_MODEL}+grabcut"
        if plain.sum() > 0.03 * alpha.size:
            return plain, f"rembg:{REMBG_MODEL}"
        print("rembg coverage too low; falling back to grabcut")
    except Exception as exc:  # noqa: BLE001 - fall through to GrabCut
        print("rembg unavailable:", type(exc).__name__, exc)

    a = work_rgb.astype(np.int16)
    r, g = a[:, :, 0], a[:, :, 1]
    luma = a.mean(axis=2)
    h, w = luma.shape
    gc = np.full((h, w), cv2.GC_PR_BGD, np.uint8)
    gc[(luma > 110) & (r > g + 14)] = cv2.GC_FGD          # lit skin
    gc[:, : int(w * 0.18)] = cv2.GC_BGD                    # elevator walls
    gc[:, int(w * 0.80) :] = cv2.GC_BGD
    bgd, fgd = np.zeros((1, 65), np.float64), np.zeros((1, 65), np.float64)
    cv2.grabCut(work_rgb, gc, None, bgd, fgd, 5, cv2.GC_INIT_WITH_MASK)
    return clean_figure((gc == cv2.GC_FGD) | (gc == cv2.GC_PR_FGD)), "grabcut"


def colour_classes(a: np.ndarray, fig: np.ndarray) -> dict[str, np.ndarray]:
    """Skin / dress / hair inside the figure. Every figure pixel lands somewhere."""
    r, g, b = a[:, :, 0], a[:, :, 1], a[:, :, 2]
    luma = a.mean(axis=2)
    chroma = a.max(axis=2) - a.min(axis=2)

    skin = fig & (luma > 88) & (r > g + 12) & (g >= b - 8)
    dress = fig & ~skin & (r > b + 34) & (r > g + 30) & (r > 48)
    hair = fig & ~skin & ~dress & (luma < 92)

    def tidy(m: np.ndarray) -> np.ndarray:
        m = cv2.morphologyEx(m.astype(np.uint8) * 255, cv2.MORPH_OPEN, K, iterations=1)
        m = cv2.morphologyEx(m, cv2.MORPH_CLOSE, K, iterations=3) > 128
        return drop_small(m, 400)

    skin, dress, hair = tidy(skin), tidy(dress), tidy(hair)

    # Unclaimed figure pixels go to the nearest class so no plate has holes.
    claimed = skin | dress | hair
    leftover = fig & ~claimed
    if leftover.any():
        dists = []
        for m in (skin, dress, hair):
            src = (~m).astype(np.uint8)
            dists.append(cv2.distanceTransform(src, cv2.DIST_L2, 3))
        pick = np.argmin(np.stack(dists, 0), axis=0)
        skin = skin | (leftover & (pick == 0))
        dress = dress | (leftover & (pick == 1))
        hair = hair | (leftover & (pick == 2))
    return {"skin": skin, "dress": dress, "hair": hair}


def refine_dress(cls: dict[str, np.ndarray], fig: np.ndarray, m: dict) -> None:
    """Consolidate the skirt.

    Silk in deep shadow falls under every colour threshold, so the raw dress class
    comes out riddled with holes. Below the face, anything in the figure that is
    neither skin nor hair is cloth by elimination.
    """
    yy = np.arange(fig.shape[0])[:, None]
    below_face = yy > m["face_bbox"][3]
    cloth = fig & below_face & ~cls["skin"] & ~cls["hair"]
    cloth = cv2.morphologyEx(cloth.astype(np.uint8) * 255, cv2.MORPH_CLOSE, K, iterations=6) > 128
    cls["dress"] = drop_small(cloth, 800)


def measure(fig: np.ndarray, cls: dict[str, np.ndarray]) -> dict:
    ys, xs = np.where(fig)
    fx0, fy0, fx1, fy1 = int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max())
    fw, fh = fx1 - fx0 + 1, fy1 - fy0 + 1

    # The face fuses into neck+chest+arm through the neck, so a connected component
    # is the whole figure. Take the topmost skin rows by profile instead.
    skin = cls["skin"]
    rows = skin.sum(axis=1)
    lit = np.where(rows >= 3)[0]
    if len(lit) == 0:
        raise SystemExit("no skin rows found; check the figure alpha")
    face_y0 = int(lit[0])
    face_y1 = int(min(fy1, face_y0 + FACE_H_FRAC * fh))
    band = skin[face_y0 : face_y1 + 1]
    bxs = np.where(band.any(axis=0))[0]
    face_cx = int(np.average(np.arange(band.shape[1]), weights=band.sum(axis=0)))
    face_half = int(FACE_HALF_FRAC * fw)

    # Torso column follows the figure between neck and hip, not the (tilted) face.
    hip_y = int(fy0 + HIP_FRAC * fh)
    trunk = fig[face_y1 : hip_y + 1]
    trunk_w = trunk.sum(axis=0)
    torso_cx = (
        int(np.average(np.arange(len(trunk_w)), weights=trunk_w)) if trunk_w.sum() else face_cx
    )
    return {
        "fig_bbox": [fx0, fy0, fx1, fy1],
        "fig_wh": [fw, fh],
        "face_bbox": [
            max(fx0, face_cx - face_half),
            face_y0,
            min(fx1, face_cx + face_half),
            face_y1,
        ],
        "face_band_x": [int(bxs.min()), int(bxs.max())],
        "face_cx": face_cx,
        "hip_y": hip_y,
        "torso_cx": torso_cx,
        "torso_half": int(TORSO_HALF_FRAC * fw),
    }


def zone_split(cls: dict[str, np.ndarray], m: dict, shape: tuple[int, int]) -> dict[str, np.ndarray]:
    h, w = shape
    yy, xx = np.ogrid[:h, :w]
    fx0, fy0, fx1, fy1 = m["fig_bbox"]
    fw, fh = m["fig_wh"]
    hip_y = m["hip_y"]
    cx = m["torso_cx"]
    half = m["torso_half"]
    face_x0, face_y0, face_x1, face_y1 = m["face_bbox"]

    skin, dress, hair = cls["skin"], cls["dress"], cls["hair"]
    plates: dict[str, np.ndarray] = {}

    # --- head: face ellipse plus the neck strip down to the collarbone ----
    ecx, ecy = (face_x0 + face_x1) / 2.0, (face_y0 + face_y1) / 2.0
    erx, ery = (face_x1 - face_x0) * 0.75, (face_y1 - face_y0) * 0.80
    head = ((xx - ecx) / erx) ** 2 + ((yy - ecy) / ery) ** 2 <= 1.0
    neck = (yy > ecy) & (yy < face_y1 + 0.030 * fh) & (np.abs(xx - ecx) < 0.055 * fw)
    plates["head"] = keep_largest(skin & (head | neck))

    # --- horns: spikes that rise above the smoothed hair dome -------------
    horns, horn_info = find_horns(hair, fw)
    m["horns"] = horn_info
    plates["horn_l"], plates["horn_r"] = horns

    # --- hair: bangs frame the face, the rest of the mass is back hair ----
    horn_any = plates["horn_l"] | plates["horn_r"]
    front = (
        hair
        & ~horn_any
        & (yy < face_y1)
        & (xx > face_x0 - 0.06 * fw)
        & (xx < face_x1 + 0.06 * fw)
    )
    plates["hair_front"] = drop_small(front, 300)
    plates["hair_back"] = drop_small(hair & ~horn_any & ~plates["hair_front"], 600)

    # --- dress ------------------------------------------------------------
    plates["dress"] = drop_small(dress, 800)

    # --- arm on the hip: skin right of the torso column, above the hip ----
    upper = skin & ~plates["head"] & (yy <= hip_y)
    plates["arm_hip"] = drop_small(upper & (xx > cx + half), 500)

    # --- legs -------------------------------------------------------------
    # The kicked leg swings up and to the left, so its thigh sits ABOVE the hip
    # line on the left of the torso. Claiming only below-hip skin left that thigh
    # stranded on an "arm" plate, so the whole left-of-torso skin mass is leg.
    kick_thigh = upper & (xx < cx - half)
    plates["torso"] = drop_small(upper & ~kick_thigh & ~plates["arm_hip"], 500)

    lower = skin & (yy > hip_y)
    if lower.any():
        lys, lxs = np.where(lower)
        left = lxs < cx
        seed_k = (int(lxs[left].mean()), int(lys[left].mean())) if left.any() else (fx0, fy1)
        seed_s = (int(lxs[~left].mean()), int(lys[~left].mean())) if (~left).any() else (fx1, fy1)
        dk = (xx - seed_k[0]) ** 2 + (yy - seed_k[1]) ** 2
        ds = (xx - seed_s[0]) ** 2 + (yy - seed_s[1]) ** 2
        plates["leg_kick"] = drop_small((lower & (dk < ds)) | kick_thigh, 500)
        plates["leg_stand"] = drop_small(lower & (ds <= dk), 500)
        m["leg_seeds"] = {"kick": list(seed_k), "stand": list(seed_s)}
    else:
        plates["leg_kick"] = drop_small(kick_thigh, 500)
        plates["leg_stand"] = np.zeros(shape, bool)
    return plates


def find_horns(hair: np.ndarray, fw: int) -> tuple[tuple[np.ndarray, np.ndarray], dict]:
    """Two spikes poking out of the hair silhouette.

    The horns are the same near-black as the hair, so colour cannot separate them.
    Geometry can: take the topmost hair pixel per column, median-smooth it into a
    "dome" baseline, and keep whatever rises clearly above that dome.
    """
    h, w = hair.shape
    cols = np.where(hair.any(axis=0))[0]
    empty = np.zeros((h, w), bool)
    if len(cols) < 16:
        return (empty, empty.copy()), {"found": 0}

    top = np.full(w, -1, np.int32)
    idx = np.argmax(hair, axis=0)
    top[cols] = idx[cols]

    # Median filter across roughly a sixth of the figure width -> smooth dome.
    span = max(5, int(0.16 * fw) | 1)
    dome = top.astype(np.float32).copy()
    valid = top >= 0
    for x in cols:
        lo, hi = max(0, x - span // 2), min(w, x + span // 2 + 1)
        win = top[lo:hi][valid[lo:hi]]
        if len(win):
            dome[x] = np.median(win)

    # A running median hugs the silhouette too closely to expose anything, so also
    # fit a parabola to the crown only (the long side strand's top would skew a fit
    # over every column) and treat that smooth arc as the baseline.
    y0h = int(top[cols].min())
    crown = cols[top[cols] < y0h + 0.25 * hair.shape[0] * 0.5]
    para = None
    if len(crown) >= 12:
        coef = np.polyfit(crown.astype(np.float64), top[crown].astype(np.float64), 2)
        fit = np.polyval(coef, np.arange(w).astype(np.float64))
        para = np.where(np.isin(np.arange(w), crown), fit, np.inf)

    # The hair is styled up around the horns, so they clear any baseline by only a
    # few pixels at this source resolution. Sweep the threshold down until two
    # spikes appear, and refuse anything big enough to be the hair mass itself.
    yy = np.arange(h)[:, None]
    hair_area = int(hair.sum())
    min_area, max_area = 150, max(400, int(0.10 * hair_area))
    attempts = []
    baselines = [("median", np.where(valid, dome, np.inf))]
    if para is not None:
        baselines.append(("parabola", para))

    for kind, base in baselines:
        for lift in range(max(4, int(0.030 * h)), 1, -2):
            spike = drop_small(hair & (yy < (base - lift)[None, :]), min_area)
            n, lab, stats, cent = cv2.connectedComponentsWithStats(spike.astype(np.uint8), 8)
            good = [i for i in range(1, n) if min_area <= stats[i, cv2.CC_STAT_AREA] <= max_area]
            attempts.append({"baseline": kind, "lift": lift, "n_cc": int(n - 1), "n_good": len(good)})
            if len(good) >= 2:
                order = sorted(good, key=lambda i: -stats[i, cv2.CC_STAT_AREA])[:2]
                order.sort(key=lambda i: cent[i][0])  # left first
                return (lab == order[0], lab == order[1]), {
                    "found": 2,
                    "confidence": "low",
                    "note": "geometric spike detection; verify by eye before rigging",
                    "baseline": kind,
                    "areas": [int(stats[i, cv2.CC_STAT_AREA]) for i in order],
                    "tips": [[int(cent[i][0]), int(stats[i, cv2.CC_STAT_TOP])] for i in order],
                    "dome_span_px": span,
                    "lift_px": lift,
                }
    return (empty, empty.copy()), {
        "found": 0,
        "needs_manual_cut": True,
        "note": (
            "horns never separate from the hair dome at this source resolution "
            "(~6px tall in the 546px frame); cut horn_l/horn_r by hand"
        ),
        "sweep": attempts,
    }


def landmark_overlay(work_rgb: np.ndarray, fig: np.ndarray, cls: dict, m: dict) -> None:
    """Tint the colour classes and draw the derived cut lines, so the constants
    at the top of this file can be checked against pixels instead of trusted."""
    vis = work_rgb.copy()
    vis[~fig] = (vis[~fig] * 0.30).astype(np.uint8)
    for mask, col in (
        (cls["skin"], (0, 255, 90)),
        (cls["dress"], (255, 60, 255)),
        (cls["hair"], (80, 160, 255)),
    ):
        vis[mask] = (vis[mask] * 0.45 + np.array(col) * 0.55).astype(np.uint8)
    fx0, fy0, fx1, fy1 = m["fig_bbox"]
    cv2.rectangle(vis, (fx0, fy0), (fx1, fy1), (255, 255, 0), 2)
    cv2.rectangle(vis, tuple(m["face_bbox"][:2]), tuple(m["face_bbox"][2:]), (255, 255, 255), 2)
    cv2.line(vis, (fx0, m["hip_y"]), (fx1, m["hip_y"]), (0, 255, 255), 2)
    for dx in (-m["torso_half"], m["torso_half"]):
        cv2.line(vis, (m["torso_cx"] + dx, m["face_bbox"][3]), (m["torso_cx"] + dx, m["hip_y"]), (255, 128, 0), 2)
    for tag, pt in m.get("leg_seeds", {}).items():
        cv2.circle(vis, tuple(pt), 12, (255, 255, 255), -1)
        cv2.putText(vis, tag, (pt[0] + 16, pt[1]), cv2.FONT_HERSHEY_SIMPLEX, 0.8, (255, 255, 255), 2)
    Image.fromarray(vis).save(DBG / "landmarks.png")


def bleed(work_rgb: np.ndarray, mask: np.ndarray, px: int = 3) -> np.ndarray:
    """Cut the plate and bleed its own colour outward so seams cannot show."""
    if not mask.any():
        return np.zeros((*mask.shape, 4), np.uint8)
    grown = cv2.dilate(mask.astype(np.uint8) * 255, K, iterations=px) > 128
    # Nearest-neighbour inpaint of the ring using the plate's own pixels.
    _, labels = cv2.distanceTransformWithLabels(
        (~mask).astype(np.uint8), cv2.DIST_L2, 3, labelType=cv2.DIST_LABEL_PIXEL
    )
    ys, xs = np.where(mask)
    order = np.zeros(labels.max() + 1, np.int64)
    order[labels[mask]] = np.arange(len(ys))
    idx = order[labels]
    rgb = work_rgb[ys[idx], xs[idx]]
    out = np.dstack([rgb, np.where(grown, 255, 0).astype(np.uint8)])
    out[mask, :3] = work_rgb[mask]
    soft = cv2.GaussianBlur(out[:, :, 3], (5, 5), 0)
    out[:, :, 3] = np.where(mask, 255, soft)
    return out


def over(dst: np.ndarray, src: np.ndarray) -> np.ndarray:
    a = src[:, :, 3:4].astype(np.float32) / 255.0
    return dst * (1 - a) + src.astype(np.float32) * a


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    DBG.mkdir(parents=True, exist_ok=True)
    # Plates are only written when non-empty, so a rename or a retuned threshold
    # would otherwise leave last run's PNGs lying around as if they were current.
    for stale in OUT.glob("*.png"):
        stale.unlink()

    src = Image.open(SRC).convert("RGB")
    x0, y0, x1, y1 = letterbox_crop(np.asarray(src))
    crop = src.crop((x0, y0, x1, y1))
    work = crop.resize((crop.width * SCALE, crop.height * SCALE), Image.Resampling.LANCZOS)
    work.save(WORK)
    work_rgb = np.asarray(work)
    a = work_rgb.astype(np.int16)
    h, w = work_rgb.shape[:2]

    fig, method = figure_alpha(work)
    Image.fromarray((fig.astype(np.uint8) * 255)).save(DBG / "mask_figure.png")

    cls = colour_classes(a, fig)
    for name, m_ in cls.items():
        Image.fromarray((m_.astype(np.uint8) * 255)).save(DBG / f"class_{name}.png")

    m = measure(fig, cls)
    refine_dress(cls, fig, m)
    for name, m_ in cls.items():
        Image.fromarray((m_.astype(np.uint8) * 255)).save(DBG / f"class_{name}.png")
    plates_m = zone_split(cls, m, (h, w))
    landmark_overlay(work_rgb, fig, cls, m)

    counts, saved = {}, []
    plates_rgba: dict[str, np.ndarray] = {}
    for name, mask in plates_m.items():
        counts[name] = int(mask.sum())
        if counts[name] == 0:
            continue
        rgba = bleed(work_rgb, mask)
        plates_rgba[name] = rgba
        Image.fromarray(rgba).save(OUT / f"{name}.png")
        saved.append(name)

    comp = np.zeros((h, w, 4), np.float32)
    for name in DRAW_ORDER:
        if name in plates_rgba:
            comp = over(comp, plates_rgba[name])
    comp_u = np.clip(comp, 0, 255).astype(np.uint8)
    Image.fromarray(comp_u).save(OUT / "composite.png")

    # Composite vs the figure cut from the work plate: how much did we lose?
    ref = work_rgb.copy()
    diff_mask = fig & (comp_u[:, :, 3] > 64)
    mean_diff = float(
        np.abs(comp_u[:, :, :3][diff_mask].astype(int) - ref[diff_mask].astype(int)).mean()
    ) if diff_mask.any() else -1.0
    lost = int((fig & (comp_u[:, :, 3] <= 64)).sum())

    # Contact sheet so the plates can be eyeballed without opening 11 files.
    tiles = []
    for name in DRAW_ORDER:
        if name not in plates_rgba:
            continue
        p = plates_rgba[name]
        al = p[:, :, 3:4].astype(np.float32) / 255.0
        vis = (p[:, :, :3].astype(np.float32) * al + 28 * (1 - al)).astype(np.uint8)
        tiles.append(np.asarray(Image.fromarray(vis).resize((w // 6, h // 6), Image.Resampling.LANCZOS)))
    if tiles:
        cols = 4
        rows = (len(tiles) + cols - 1) // cols
        th, tw = tiles[0].shape[:2]
        sheet = np.zeros((rows * th, cols * tw, 3), np.uint8)
        for i, t in enumerate(tiles):
            r_, c_ = divmod(i, cols)
            sheet[r_ * th : r_ * th + th, c_ * tw : c_ * tw + tw] = t
        Image.fromarray(sheet).save(DBG / "contact_sheet.png")

    report = {
        "source": str(SRC),
        "source_size": [src.width, src.height],
        "letterbox_crop": [x0, y0, x1, y1],
        "canvas": [w, h],
        "scale": SCALE,
        "alpha_method": method,
        "figure_px": int(fig.sum()),
        "landmarks": m,
        "class_counts": {k: int(v.sum()) for k, v in cls.items()},
        "plate_counts": counts,
        "saved": saved,
        "composite_mean_rgb_diff": round(mean_diff, 2),
        "figure_px_not_covered": lost,
    }
    (OUT / "report.json").write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding="utf-8")
    print(json.dumps(report, indent=2, ensure_ascii=False))


if __name__ == "__main__":
    main()
