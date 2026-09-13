"""Cut elevator-red layers from the painted still (preferred) or the GIF.

Uses system Python 3.13 (rembg + cv2 + PIL):

    C:\\Users\\Administrator\\AppData\\Local\\Programs\\Python\\Python313\\python.exe
    art\\live2d-lab\\elevator-red\\split_elevator_red.py

Writes (never deletes layers/body.png):
  layers/body_original.png   rembg figure, same canvas as the source still
  layers/<name>.png          photo-masked parts on that same canvas
  layers/01_*.png ...        same pixels padded to 2048 for Cubism pack
  layers/_dbg/*              masks + landmark overlay

The 546x292 GIF path still works (SCALE=2, old UVs). The 1280x720 painted
still uses SCALE=1 and figure-bbox / colour cuts — do not feed it the GIF UVs.
up2_mask always expands a 0/1 mask to 0/255 before cv2.resize.
"""
from __future__ import annotations

import json
from pathlib import Path

import cv2
import numpy as np
from PIL import Image, ImageDraw
from rembg import new_session, remove

ROOT = Path(__file__).resolve().parent
OUT = ROOT / "layers"
DBG = OUT / "_dbg"
MANIFEST = ROOT / "textures" / "layer-manifest.json"
PRESERVE = frozenset({"body.png"})  # old GIF 2x plate; do not overwrite

# Placeholder / puppet canvas is 2x only when the source is the tiny GIF.
PAD = 2048
K = np.ones((3, 3), np.uint8)

NAME_TO_FILE = {
    "back_hair": "01_back_hair.png",
    "front_hair": "40_front_hair.png",
    "horns": "41_headwear.png",
    "torso": "07_torso.png",
    "chest": "08_chest.png",
    "dress_body": "09_dress.png",
    "dress_slit_panel": "11_dress_slit_flap.png",
    "leg_stand": "13_leg_stand.png",
    "leg_raise_thigh": "14_leg_raise_thigh.png",
    "leg_raise_calf": "15_leg_raise_calf.png",
    "foot_raise": "17_shoe_raise.png",
    "arm_hip": "21_arm_upper_r.png",
    "neck": "24_neck.png",
    "face": "27_face.png",
    "eyes": "31_iris_l.png",
    "anklet": "18_anklet.png",
}


def pick_source() -> Path:
    for p in (ROOT / "original-still.png", ROOT / "reference.jpg"):
        if p.is_file():
            return p
    raise SystemExit(f"missing original-still.png or reference.jpg under {ROOT}")


def is_gif_still(w: int, h: int) -> bool:
    """The 546x292 elevator screenshot — not the painted 1280x720 still."""
    return w < 800 or (abs(w / h - 546 / 292) < 0.08 and w < 900)


def poly(h: int, w: int, uvs: list[tuple[float, float]]) -> np.ndarray:
    pts = [(int(u * w), int(v * h)) for u, v in uvs]
    m = Image.new("L", (w, h), 0)
    ImageDraw.Draw(m).polygon(pts, fill=255)
    return np.asarray(m) > 0


def ellipse(h: int, w: int, cu: float, cv: float, ru: float, rv: float) -> np.ndarray:
    yy, xx = np.ogrid[:h, :w]
    return ((xx - cu * w) / (ru * w)) ** 2 + ((yy - cv * h) / (rv * h)) ** 2 <= 1.0


def save_rgba(path: Path, rgb: np.ndarray, alpha: np.ndarray) -> None:
    a = np.clip(alpha.astype(np.float32), 0, 1)
    out = np.zeros((*rgb.shape[:2], 4), np.uint8)
    out[..., :3] = rgb
    out[..., 3] = (a * 255).astype(np.uint8)
    Image.fromarray(out, "RGBA").save(path)


def up2(arr: np.ndarray, scale: int) -> np.ndarray:
    if scale == 1:
        return arr
    h, w = arr.shape[:2]
    return cv2.resize(arr, (w * scale, h * scale), interpolation=cv2.INTER_LINEAR)


def up2_mask(mask: np.ndarray, scale: int) -> np.ndarray:
    """Resize a boolean mask.

    cv2.resize on a 0/1 array with INTER_LINEAR interpolates toward 0, so the
    thresholded result dies. Expand to 0/255 first, then threshold.
    """
    if scale == 1:
        return mask.astype(bool)
    return up2(mask.astype(np.uint8) * 255, scale) > 127


def pad2048(im: Image.Image) -> Image.Image:
    if im.width > PAD or im.height > PAD:
        im = im.copy()
        im.thumbnail((PAD, PAD), Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", (PAD, PAD), (0, 0, 0, 0))
    x = (PAD - im.width) // 2
    y = (PAD - im.height) // 2
    canvas.paste(im, (x, y), im)
    return canvas


def keep_overlapping(mask: np.ndarray, seed: np.ndarray) -> np.ndarray:
    n, lab, stats, _ = cv2.connectedComponentsWithStats(mask.astype(np.uint8), 8)
    out = np.zeros_like(mask, dtype=bool)
    for i in range(1, n):
        if seed[lab == i].any():
            out |= lab == i
    if not out.any() and n > 1:
        out = lab == 1 + int(np.argmax(stats[1:, cv2.CC_STAT_AREA]))
    return out


def drop_small(mask: np.ndarray, min_area: int) -> np.ndarray:
    n, lab, stats, _ = cv2.connectedComponentsWithStats(mask.astype(np.uint8), 8)
    out = np.zeros_like(mask, dtype=bool)
    for i in range(1, n):
        if int(stats[i, cv2.CC_STAT_AREA]) >= min_area:
            out |= lab == i
    return out


def keep_largest(mask: np.ndarray) -> np.ndarray:
    n, lab, stats, _ = cv2.connectedComponentsWithStats(mask.astype(np.uint8), 8)
    if n <= 1:
        return mask
    return lab == 1 + int(np.argmax(stats[1:, cv2.CC_STAT_AREA]))


def recover_hair_edge(rembg_fig: np.ndarray, rembg_alpha: np.ndarray) -> np.ndarray:
    """Keep rembg's own wispy hair (soft alpha) without the elevator wall.

    The painted still's void is a lit gradient (luma >> 8), so a raw ink
    flood swallows the cabin. Only grow using rembg's soft edge.
    """
    soft = (rembg_alpha > 12) & ~rembg_fig
    k = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (11, 11))
    near = cv2.dilate(rembg_fig.astype(np.uint8), k) > 0
    return rembg_fig | (soft & near)


def colour_classes(rgb: np.ndarray, fig: np.ndarray) -> dict[str, np.ndarray]:
    """Exclusive dress / skin / hair.

    Painted skin and the qipao share hue ~8, so a raw red threshold swallows
    limbs. Seed the dress from high-saturation red, grow only into dark
    saturated red, and keep bright flesh out of the dress.
    """
    hsv = cv2.cvtColor(rgb, cv2.COLOR_RGB2HSV)
    hh, ss, vv = hsv[..., 0], hsv[..., 1], hsv[..., 2]
    r = rgb[..., 0].astype(np.int16)
    g = rgb[..., 1].astype(np.int16)
    b = rgb[..., 2].astype(np.int16)
    luma = rgb.mean(axis=2)

    core = fig & ((hh <= 10) | (hh >= 170)) & (ss > 140) & (r > g + 40)
    core = drop_small(core, 200)
    cand = fig & ((hh <= 12) | (hh >= 168)) & (ss > 95) & (r > g + 20) & (luma < 145)
    red = keep_overlapping(cand | core, core) if core.any() else cand
    red = cv2.morphologyEx(red.astype(np.uint8) * 255, cv2.MORPH_CLOSE, K, iterations=2) > 128
    red = keep_largest(red & fig)

    skin = fig & ~red & (luma > 70) & (r > b - 8) & (r > g - 6)
    skin = cv2.morphologyEx(skin.astype(np.uint8) * 255, cv2.MORPH_CLOSE, K, iterations=2) > 128
    skin = drop_small(skin & fig & ~red, 40)

    dark = fig & ~red & ~skin
    gold = fig & ~red & (hh >= 12) & (hh <= 40) & (ss > 70) & (vv > 90) & (r > 90)
    return {"red": red, "skin": skin, "dark": dark, "gold": gold}


def regions_gif(
    h: int, w: int, fig: np.ndarray, red: np.ndarray, skin: np.ndarray, dark: np.ndarray
) -> dict[str, np.ndarray]:
    """UVs traced from the 546x292 elevator GIF (top-left origin)."""
    regions = {
        "back_hair": dark
        & (ellipse(h, w, 0.430, 0.300, 0.090, 0.170) | ellipse(h, w, 0.455, 0.420, 0.055, 0.220)),
        "front_hair": dark & ellipse(h, w, 0.412, 0.175, 0.055, 0.055),
        "horns": dark
        & (ellipse(h, w, 0.378, 0.118, 0.028, 0.040) | ellipse(h, w, 0.448, 0.112, 0.028, 0.040)),
        "face": skin & ellipse(h, w, 0.412, 0.205, 0.048, 0.075),
        "neck": skin & ellipse(h, w, 0.420, 0.285, 0.028, 0.040),
        "chest": (skin | red) & ellipse(h, w, 0.418, 0.365, 0.070, 0.075),
        "torso": (skin | red)
        & poly(
            h,
            w,
            [(0.368, 0.300), (0.470, 0.295), (0.490, 0.520), (0.400, 0.560), (0.370, 0.430)],
        ),
        "dress_body": red
        & poly(
            h,
            w,
            [
                (0.365, 0.300),
                (0.472, 0.292),
                (0.500, 0.620),
                (0.478, 0.820),
                (0.400, 0.830),
                (0.368, 0.560),
            ],
        ),
        "dress_slit_panel": red
        & poly(
            h,
            w,
            [(0.390, 0.500), (0.430, 0.520), (0.418, 0.880), (0.355, 0.870), (0.368, 0.560)],
        ),
        "leg_stand": skin
        & poly(
            h,
            w,
            [(0.420, 0.560), (0.500, 0.560), (0.495, 0.930), (0.430, 0.945), (0.418, 0.720)],
        ),
        "leg_raise_thigh": skin
        & poly(h, w, [(0.300, 0.300), (0.430, 0.320), (0.445, 0.560), (0.300, 0.500)]),
        "leg_raise_calf": skin
        & poly(
            h,
            w,
            [(0.200, 0.500), (0.360, 0.360), (0.380, 0.560), (0.250, 0.720), (0.190, 0.680)],
        ),
        "foot_raise": skin
        & poly(
            h,
            w,
            [(0.175, 0.640), (0.280, 0.620), (0.300, 0.780), (0.200, 0.840), (0.165, 0.760)],
        ),
        "arm_hip": skin
        & poly(
            h,
            w,
            [(0.450, 0.300), (0.545, 0.300), (0.540, 0.500), (0.455, 0.560), (0.440, 0.420)],
        ),
        "eyes": ellipse(h, w, 0.399, 0.198, 0.012, 0.014)
        | ellipse(h, w, 0.428, 0.196, 0.012, 0.014),
    }
    regions["torso"] &= ~regions["dress_body"] | skin
    return regions


def _cc_list(mask: np.ndarray, min_area: int) -> list[tuple[int, np.ndarray, np.ndarray]]:
    n, lab, stats, cents = cv2.connectedComponentsWithStats(mask.astype(np.uint8), 8)
    out = []
    for i in range(1, n):
        if int(stats[i, cv2.CC_STAT_AREA]) < min_area:
            continue
        out.append((i, lab == i, cents[i]))
    return out


def regions_painted(
    h: int,
    w: int,
    fig: np.ndarray,
    red: np.ndarray,
    skin: np.ndarray,
    dark: np.ndarray,
    gold: np.ndarray,
) -> dict[str, np.ndarray]:
    """Colour + bbox cuts for the 1280x720 painted still (raised leg on the right)."""
    yy, xx = np.ogrid[:h, :w]
    empty = np.zeros((h, w), dtype=bool)
    ys, xs = np.where(fig)
    if len(ys) == 0:
        return {}
    fx0, fy0, fx1, fy1 = int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max())
    fw, fh = max(1, fx1 - fx0 + 1), max(1, fy1 - fy0 + 1)
    min_px = max(60, int(fig.sum() * 0.0003))

    # Collar = first dress row that is actually wide (ignore lip/speck red).
    if red.any():
        r_width = red.sum(axis=1)
        wide = np.where(r_width >= max(18, int(0.03 * fw)))[0]
        r_all = np.where(red.any(axis=1))[0]
        collar_y = int(wide[0]) if len(wide) else int(r_all[0])
        r_y0, r_y1 = int(r_all[0]), int(r_all[-1])
        upper = red & (yy < collar_y + 0.42 * (r_y1 - collar_y + 1))
        ux = np.where(upper.any(axis=0))[0] if upper.any() else np.where(red.any(axis=0))[0]
        torso_cx = int(np.median(ux))
        hip_y = int(collar_y + 0.38 * (r_y1 - collar_y + 1))
    else:
        torso_cx = int(fx0 + 0.55 * fw)
        hip_y = int(fy0 + 0.42 * fh)
        r_y0, r_y1 = hip_y, fy1
        collar_y = int(fy0 + 0.16 * fh)
    torso_half = int(0.10 * fw)

    face_y0 = int(fy0)
    face_y1 = int(min(fy1, collar_y + 0.02 * fh))
    band = skin[face_y0 : face_y1 + 1]
    col_w = band.sum(axis=0).astype(np.float64)
    if col_w.sum() <= 0:
        face_cx = torso_cx
    else:
        face_cx = int(np.average(np.arange(w), weights=col_w))
    face_half = int(0.10 * fw)
    face = (
        skin
        & (yy >= face_y0)
        & (yy <= face_y1)
        & (np.abs(xx - face_cx) <= face_half)
    )
    face = keep_largest(drop_small(face, min_px // 2))

    neck = (
        skin
        & ~face
        & (yy > face_y1 - 0.04 * fh)
        & (yy < collar_y + 0.06 * fh)
        & (np.abs(xx - face_cx) < 0.055 * fw)
    )
    neck = drop_small(neck, min_px // 3)

    # Face is at the top of this still; raised foot is the rightmost skin, standing
    # foot is the lowest. Do not use "topmost skin" — that is the forehead.
    non_face = skin & ~face & ~neck
    raise_mask = empty.copy()
    stand_mask = empty.copy()
    arm_mask = empty.copy()
    if non_face.any():
        nys, nxs = np.where(non_face)
        seed_raise = (int(nxs[int(np.argmax(nxs))]), int(nys[int(np.argmax(nxs))]))
        seed_stand = (int(nxs[int(np.argmax(nys))]), int(nys[int(np.argmax(nys))]))
        for _i, cc, cent in _cc_list(non_face, min_px):
            if cc[seed_raise[1], seed_raise[0]]:
                raise_mask |= cc
            elif cc[seed_stand[1], seed_stand[0]]:
                stand_mask |= cc
            else:
                cx, cy = float(cent[0]), float(cent[1])
                ys2, xs2 = np.where(cc)
                wide = int(xs2.max() - xs2.min())
                tall = int(ys2.max() - ys2.min())
                if cy > hip_y + 0.08 * fh and abs(cx - torso_cx) < 0.18 * fw:
                    stand_mask |= cc
                elif wide > 0.22 * fw or (cx > torso_cx + torso_half and tall > 0.18 * fh):
                    raise_mask |= cc
                else:
                    arm_mask |= cc

    # If the arm was swallowed by the raise CC (same side on this still), peel
    # the compact waist blob nearest the torso.
    if raise_mask.any() and not arm_mask.any():
        waist = (
            raise_mask
            & (yy > hip_y - 0.10 * fh)
            & (yy < hip_y + 0.08 * fh)
            & (np.abs(xx - torso_cx) < 0.20 * fw)
            & (np.abs(xx - torso_cx) > 0.04 * fw)
        )
        waist = drop_small(waist, min_px)
        if waist.any():
            arm_mask = keep_largest(waist)
            raise_mask = raise_mask & ~arm_mask

    hip_hand = (
        skin
        & ~face
        & ~neck
        & ~stand_mask
        & (xx > torso_cx)
        & (xx < torso_cx + 0.18 * fw)
        & (yy > hip_y - 0.14 * fh)
        & (yy < hip_y + 0.12 * fh)
    )
    hip_hand = drop_small(hip_hand, min_px // 2)
    if hip_hand.any():
        arm_mask = keep_largest(hip_hand)
        raise_mask = raise_mask & ~arm_mask
    raise_mask = raise_mask & ~dark & ~arm_mask

    # Split the raised limb along hip-joint → toes.
    # On this still the KNEE is highest; the foot is the rightmost skin.
    thigh = calf = foot = empty.copy()
    if raise_mask.any():
        rys, rxs = np.where(raise_mask)
        foot_i = int(np.argmax(rxs))
        foot_pt = np.array([rxs[foot_i], rys[foot_i]], np.float64)
        near_dress = cv2.dilate(red.astype(np.uint8), np.ones((7, 7), np.uint8)) > 0
        joint = raise_mask & near_dress
        if joint.any():
            jy, jx = np.where(joint)
            hip_pt = np.array([float(np.median(jx)), float(np.median(jy))], np.float64)
        else:
            hip_pt = np.array(
                [torso_cx + 0.05 * np.sign(foot_pt[0] - torso_cx) * fw, float(hip_y)],
                np.float64,
            )
        vec = foot_pt - hip_pt
        span = float(np.linalg.norm(vec)) + 1e-6
        vec = vec / span
        pts = np.stack([rxs, rys], 1).astype(np.float64)
        t = ((pts - hip_pt) @ vec) / span
        t_im = np.zeros((h, w), np.float32)
        t_im[rys, rxs] = t
        thigh = raise_mask & (t_im < 0.40)
        calf = raise_mask & (t_im >= 0.40) & (t_im < 0.76)
        foot = raise_mask & (t_im >= 0.76)
        if not thigh.any():
            thigh = raise_mask & (np.abs(xx - torso_cx) < 0.16 * fw)
        if not foot.any():
            foot = raise_mask & (yy <= rys.min() + 0.10 * fh)

    # Hair: buns sit above the face; bangs frame it; the rest is the long back mass.
    bun_band = dark & (yy < face_y0 + 0.02 * fh) & (np.abs(xx - face_cx) < 0.12 * fw)
    horns = drop_small(bun_band, max(20, min_px // 4))
    front = (
        dark
        & ~horns
        & (yy < face_y1 + 0.02 * fh)
        & (xx > face_cx - 0.08 * fw)
        & (xx < face_cx + 0.08 * fw)
    )
    front = drop_small(front, min_px // 3)
    back = drop_small(dark & ~horns & ~front, min_px)

    # Standing shin sits in dress-shadow; peel the narrow column below the hem.
    if red.any():
        r_width = red.sum(axis=1)
        wide = np.where(r_width >= max(18, int(0.03 * fw)))[0]
        hem_y = int(wide[-1]) if len(wide) else r_y1
        stand_shin = (
            fig
            & ~dark
            & (yy > hem_y - 0.03 * fh)
            & (np.abs(xx - torso_cx) < 0.075 * fw)
        )
        if stand_shin.any():
            stand_mask = stand_mask | stand_shin
            red = red & ~stand_shin

    dress_body = drop_small(red, min_px)
    raise_side = -1.0
    if raise_mask.any():
        raise_side = 1.0 if float(np.mean(np.where(raise_mask)[1])) > torso_cx else -1.0
    slit = dress_body & (yy > hip_y) & ((xx - torso_cx) * raise_side < 0)
    slit = drop_small(slit, min_px)

    chest = (skin | red) & (yy > face_y1) & (yy < hip_y) & (np.abs(xx - torso_cx) < torso_half * 1.15)
    chest = drop_small(chest, min_px)
    torso = (skin | red) & (yy > face_y1 - 0.02 * fh) & (yy < hip_y + 0.06 * fh) & (np.abs(xx - torso_cx) < torso_half)
    torso = drop_small(torso & ~dress_body | (torso & skin), min_px)
    if not torso.any():
        torso = drop_small(chest & skin, min_px // 2)

    eye_y = face_y0 + 0.42 * (face_y1 - face_y0 + 1)
    eyes = ellipse(h, w, face_cx / w - 0.012, eye_y / h, 0.010, 0.012) | ellipse(
        h, w, face_cx / w + 0.012, eye_y / h, 0.010, 0.012
    )

    anklet = empty.copy()
    if gold.any() and foot.any():
        fy = np.where(foot)[0]
        fx = np.where(foot)[1]
        anklet = gold & (yy > fy.min() - 0.04 * fh) & (yy < fy.max() + 0.02 * fh)
        anklet &= np.abs(xx - int(np.median(fx))) < 0.08 * fw
        anklet = drop_small(anklet, 8)

    return {
        "back_hair": back,
        "front_hair": front,
        "horns": horns,
        "face": face,
        "neck": neck,
        "chest": chest,
        "torso": torso,
        "dress_body": dress_body,
        "dress_slit_panel": slit,
        "leg_stand": drop_small(stand_mask, min_px),
        "leg_raise_thigh": drop_small(thigh, min_px // 2),
        "leg_raise_calf": drop_small(calf, min_px // 2),
        "foot_raise": drop_small(foot, min_px // 3),
        "arm_hip": drop_small(arm_mask, min_px // 2),
        "eyes": eyes & fig,
        "anklet": anklet,
        "_meta": {  # type: ignore[dict-item]
            "face_cx": face_cx,
            "face_y0": face_y0,
            "face_y1": face_y1,
            "torso_cx": torso_cx,
            "hip_y": hip_y,
            "fig_bbox": [fx0, fy0, fx1, fy1],
            "raise_side": raise_side,
        },
    }


def landmark_overlay(
    rgb: np.ndarray,
    fig: np.ndarray,
    cls: dict[str, np.ndarray],
    regions: dict[str, np.ndarray],
) -> None:
    vis = rgb.copy()
    vis[~fig] = (vis[~fig] * 0.25).astype(np.uint8)
    for key, col in (("skin", (0, 255, 90)), ("red", (255, 60, 255)), ("dark", (80, 160, 255))):
        m = cls.get(key)
        if m is not None:
            vis[m] = (vis[m] * 0.45 + np.array(col) * 0.55).astype(np.uint8)
    meta = regions.get("_meta") if isinstance(regions.get("_meta"), dict) else None
    if meta:
        fx0, fy0, fx1, fy1 = meta["fig_bbox"]
        cv2.rectangle(vis, (fx0, fy0), (fx1, fy1), (255, 255, 0), 2)
        cv2.line(vis, (fx0, meta["hip_y"]), (fx1, meta["hip_y"]), (0, 255, 255), 2)
        cv2.line(vis, (meta["torso_cx"], fy0), (meta["torso_cx"], fy1), (255, 128, 0), 2)
        cv2.rectangle(
            vis,
            (meta["face_cx"] - 20, meta["face_y0"]),
            (meta["face_cx"] + 20, meta["face_y1"]),
            (255, 255, 255),
            2,
        )
    Image.fromarray(vis).save(DBG / "landmarks.png")


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    DBG.mkdir(parents=True, exist_ok=True)

    src = pick_source()
    still = Image.open(src).convert("RGBA")
    w0, h0 = still.size
    gif = is_gif_still(w0, h0)
    scale = 2 if gif else 1

    body_path = OUT / "body_original.png"
    if body_path.is_file() and body_path.stat().st_mtime >= src.stat().st_mtime:
        cut = Image.open(body_path).convert("RGBA")
        print(f"reuse {body_path}")
    else:
        session = new_session("u2net")
        cut = remove(still, session=session)
        cut.save(body_path)
    # Do not write body.png — the old 2x GIF plate stays if present.

    rgba = np.asarray(cut.convert("RGBA"))
    rgb = np.asarray(still.convert("RGB"))
    rembg_alpha = rgba[..., 3]
    rembg_fig = rembg_alpha > 40
    fig = rembg_fig if gif else recover_hair_edge(rembg_fig, rembg_alpha)
    Image.fromarray((rembg_fig.astype(np.uint8) * 255), "L").save(DBG / "figure_rembg.png")
    Image.fromarray((fig.astype(np.uint8) * 255), "L").save(DBG / "figure.png")

    cls = colour_classes(rgb, fig)
    for name, m in cls.items():
        Image.fromarray((m.astype(np.uint8) * 255), "L").save(DBG / f"{name}.png")

    h, w = fig.shape
    raw = (
        regions_gif(h, w, fig, cls["red"], cls["skin"], cls["dark"])
        if gif
        else regions_painted(h, w, fig, cls["red"], cls["skin"], cls["dark"], cls["gold"])
    )
    meta = raw.pop("_meta", None)
    landmark_overlay(rgb, fig, cls, {**raw, "_meta": meta} if meta else raw)

    rgb2, fig2 = up2(rgb, scale), up2_mask(fig, scale)
    # body_original stays native rembg; parts are same-canvas (or 2x for the GIF).
    written: dict[str, int] = {}
    for name, mask in raw.items():
        if name.startswith("_"):
            continue
        m2 = up2_mask(mask, scale) & fig2
        dest = OUT / f"{name}.png"
        save_rgba(dest, rgb2, m2.astype(np.float32))
        written[name] = int(m2.sum())
        Image.fromarray((mask.astype(np.uint8) * 255), "L").save(DBG / f"{name}.png")

    for name, dest in NAME_TO_FILE.items():
        part = OUT / f"{name}.png"
        if part.is_file():
            pad2048(Image.open(part)).save(OUT / dest)

    if MANIFEST.is_file():
        data = json.loads(MANIFEST.read_text(encoding="utf-8"))
        done = set(NAME_TO_FILE.values()) | {f"{n}.png" for n in written}
        for layer in data.get("Layers", []):
            if Path(layer["File"]).name in done or Path(layer.get("Proxy", "")).name in done:
                layer["Status"] = "auto-cut"
        MANIFEST.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    body_a = rgba[..., 3] > 40
    report = {
        "source": str(src),
        "gif_path": gif,
        "canvas": [w0, h0],
        "scale": scale,
        "body_original_px": int(body_a.sum()),
        "figure_recovered_px": int(fig.sum()),
        "body_png_preserved": (OUT / "body.png").is_file(),
        "parts": written,
        "meta": meta,
    }
    (OUT / "report.json").write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding="utf-8")
    print(json.dumps(report, indent=2, ensure_ascii=False))
    print(f"cut {w}x{h} scale={scale} -> {len(written)} parts in {OUT}")
    print(f"preserved body.png={report['body_png_preserved']}")


if __name__ == "__main__":
    main()
