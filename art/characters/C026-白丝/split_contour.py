"""C026 fine contour split. No Photoshop.

Hair: cool-white flood, L/R scalp barrier.
Body: flood inside envelopes, then joint CUT LINES (not polygon sides).
Overlays (jewelry/choker/ties/ornaments): gold/metal CCs only.
No blind 1px dilate — only similar-color edge grow.
"""
from __future__ import annotations

import json
from pathlib import Path

import cv2
import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy import ndimage as ndi
from skimage.segmentation import watershed

ROOT = Path(r"F:\天命之子\art\characters\C026-白丝")
SRC = ROOT / "presenter.png"
OUT = ROOT / "layers"
DBG = OUT / "_dbg"

ORDER = [
    "hair_back_l",
    "hair_back_r",
    "hair_top_l",
    "hair_top_r",
    "boot_l",
    "boot_r",
    "thigh_l",
    "thigh_r",
    "torso",
    "chest",
    "arm_upper_r",
    "arm_forearm_r",
    "arm_upper_l",
    "arm_forearm_l",
    "clutch",
    "hand_r",
    "hand_l",
    "boot_ornament_l",
    "boot_ornament_r",
    "jewelry_wrist_r",
    "jewelry_wrist_l",
    "choker",
    "head",
    "earring_l",
    "earring_r",
    "hair_front",
    "hair_tie_l",
    "hair_tie_r",
]


def poly(h: int, w: int, pts) -> np.ndarray:
    m = Image.new("L", (w, h), 0)
    ImageDraw.Draw(m).polygon([(int(x), int(y)) for x, y in pts], fill=255)
    return np.asarray(m) > 0


def thick_line(h: int, w: int, p1, p2, width: int = 6) -> np.ndarray:
    m = Image.new("L", (w, h), 0)
    ImageDraw.Draw(m).line([p1, p2], fill=255, width=width)
    return np.asarray(m) > 0


def nearest_on(mask: np.ndarray, x: int, y: int) -> tuple[int, int]:
    if 0 <= y < mask.shape[0] and 0 <= x < mask.shape[1] and mask[y, x]:
        return x, y
    ys, xs = np.where(mask)
    if len(xs) == 0:
        return x, y
    i = np.argmin((xs - x) ** 2 + (ys - y) ** 2)
    return int(xs[i]), int(ys[i])


def flood(walk: np.ndarray, seeds: list[tuple[int, int]], name: str = "") -> np.ndarray:
    if not walk.any():
        print(f"  EMPTY walk {name}")
        return np.zeros_like(walk, bool)
    lab, _ = ndi.label(walk)
    out = np.zeros_like(walk, bool)
    for x, y in seeds:
        sx, sy = nearest_on(walk, x, y)
        if not walk[sy, sx]:
            print(f"  SEED MISS {name} {(x, y)}")
            continue
        if (sx, sy) != (x, y):
            print(f"  SEED SNAP {name} {(x, y)} -> {(sx, sy)}")
        out |= lab == lab[sy, sx]
    return out


def close_open(mask: np.ndarray, close: int = 1, open_: int = 0) -> np.ndarray:
    st = ndi.generate_binary_structure(2, 1)
    if close:
        mask = ndi.binary_closing(mask, structure=st, iterations=close)
    if open_:
        mask = ndi.binary_opening(mask, structure=st, iterations=open_)
    return mask


def split_markers(mask: np.ndarray, groups: list, radius: int = 12) -> np.ndarray:
    """Watershed a silhouette into parts from seed groups [(label, [(x,y),...]), ...]."""
    markers = np.zeros(mask.shape, np.int32)
    if not mask.any():
        return markers
    for lab, seeds in groups:
        for x, y in seeds:
            sx, sy = nearest_on(mask, int(x), int(y))
            if mask[sy, sx]:
                markers[sy, sx] = lab
    if markers.max() == 0:
        return markers
    markers = ndi.grey_dilation(markers, size=(radius * 2 + 1, radius * 2 + 1))
    markers[~mask] = 0
    dist = ndi.distance_transform_edt(mask)
    return watershed(-dist, markers, mask=mask)


def drop_specks(mask: np.ndarray, min_pix: int) -> np.ndarray:
    lab, n = ndi.label(mask)
    keep = np.zeros_like(mask, bool)
    for i in range(1, n + 1):
        m = lab == i
        if int(m.sum()) >= min_pix:
            keep |= m
    return keep


def smooth_mask(mask: np.ndarray, sigma: float = 0.8, thresh: float = 0.42) -> np.ndarray:
    if not mask.any():
        return mask
    f = ndi.gaussian_filter(mask.astype(np.float32), sigma)
    return f >= thresh


def dilate_similar(mask: np.ndarray, rgba: np.ndarray, on: np.ndarray, thresh: float = 32) -> np.ndarray:
    """Grow 1px only into opaque pixels close to this plate's mean color."""
    if not mask.any():
        return mask
    st = ndi.generate_binary_structure(2, 1)
    ring = ndi.binary_dilation(mask, structure=st, iterations=1) & on & ~mask
    if not ring.any():
        return mask
    mean = rgba[mask][:, :3].astype(np.float32).mean(axis=0)
    diff = np.abs(rgba[:, :, :3].astype(np.float32) - mean).sum(axis=2)
    return mask | (ring & (diff < thresh))


def halfplane(xx: np.ndarray, yy: np.ndarray, p1, p2, sample) -> np.ndarray:
    """Pixels on the same side of segment p1→p2 as `sample`."""
    x1, y1 = p1
    x2, y2 = p2
    sx, sy = sample

    def cross(x, y):
        return (x2 - x1) * (y - y1) - (y2 - y1) * (x - x1)

    sign = np.sign(cross(float(sx), float(sy)))
    if sign == 0:
        sign = 1
    return np.sign(cross(xx.astype(np.float32), yy.astype(np.float32))) == sign


def grab_refine(bgr: np.ndarray, seed: np.ndarray, dilate: int = 8) -> np.ndarray:
    if not seed.any():
        return seed
    gc = np.full(seed.shape, cv2.GC_BGD, np.uint8)
    ring = ndi.binary_dilation(seed, iterations=dilate)
    gc[ring] = cv2.GC_PR_BGD
    gc[ndi.binary_dilation(seed, iterations=2)] = cv2.GC_PR_FGD
    gc[seed] = cv2.GC_FGD
    bgd = np.zeros((1, 65), np.float64)
    fgd = np.zeros((1, 65), np.float64)
    try:
        cv2.grabCut(bgr, gc, None, bgd, fgd, 5, cv2.GC_INIT_WITH_MASK)
    except cv2.error as e:
        print("grabCut skipped", e)
        return seed
    return ((gc == cv2.GC_FGD) | (gc == cv2.GC_PR_FGD)) & ring


def checker(h: int, w: int, cell: int = 16) -> np.ndarray:
    yy, xx = np.indices((h, w))
    ch = np.zeros((h, w, 3), np.uint8)
    ch[((yy // cell) + (xx // cell)) % 2 == 0] = (86, 86, 86)
    ch[((yy // cell) + (xx // cell)) % 2 == 1] = (48, 48, 48)
    return ch


# Traced from zoom grids on presenter.png (not bounding boxes).
POLYS = {
    "arm_r": [  # whole raised arm, inner edge follows thumb→wrist→bicep
        (176, 144), (198, 136), (218, 145), (234, 168), (238, 202),
        (230, 250), (218, 268), (200, 305), (190, 342), (218, 360),
        (258, 348), (312, 300), (345, 268), (350, 248),
        (338, 245), (318, 252), (295, 268), (270, 295), (248, 322),
        (222, 275), (205, 255), (185, 248), (168, 220), (160, 185), (162, 155),
    ],
    "hand_l": [
        (526, 386), (572, 384), (612, 396), (618, 426),
        (588, 436), (542, 428), (516, 410), (514, 394),
    ],
    "fore_l": [
        (478, 358), (542, 372), (556, 412), (528, 428),
        (490, 412), (468, 380),
    ],
    "up_l": [
        (414, 316), (456, 314), (498, 352), (508, 388),
        (478, 408), (432, 382), (408, 342),
    ],
    "head": [
        (338, 104), (368, 100), (392, 112), (404, 138), (402, 168),
        (390, 192), (372, 204), (352, 208), (332, 196), (318, 176),
        (312, 150), (314, 124), (326, 108),
    ],
    "neck": [(342, 192), (388, 192), (390, 242), (340, 242)],
    "choker_band": [(338, 166), (398, 166), (400, 198), (336, 198)],
    "choker_pend": [(348, 176), (392, 176), (392, 214), (348, 214)],
    "leotard": [
        (348, 216), (388, 210), (416, 238), (438, 268),
        (455, 300), (452, 360), (442, 420), (428, 490),
        (400, 535), (372, 548), (342, 528), (318, 470),
        (308, 390), (310, 320), (322, 255),
    ],
    "thigh_l": [
        (250, 400), (348, 405), (360, 530), (356, 688),
        (308, 698), (248, 575), (244, 460),
    ],
    "thigh_r": [
        (360, 430), (455, 418), (462, 555), (442, 688),
        (372, 702), (358, 555),
    ],
    "clutch": [(520, 426), (630, 418), (638, 510), (516, 518)],
    "bangs": [(328, 98), (400, 98), (408, 132), (385, 142), (338, 142), (320, 120)],
    "bun_l": [(218, 16), (326, 12), (338, 58), (326, 112), (250, 108), (210, 60)],
    "bun_r": [(404, 18), (516, 14), (536, 64), (500, 118), (414, 114), (392, 60)],
    "tie_l": [(314, 72), (344, 72), (344, 100), (314, 100)],
    "tie_r": [(402, 90), (420, 90), (420, 116), (402, 116)],
    "ear_l": [(298, 192), (326, 192), (326, 230), (298, 230)],
    "ear_r": [(386, 218), (412, 218), (412, 250), (386, 250)],
    "br_r": [(168, 214), (220, 216), (222, 266), (166, 266)],
    "br_l": [(522, 382), (575, 384), (578, 422), (520, 420)],
    "orn_l": [(308, 572), (338, 572), (352, 625), (348, 658), (308, 658), (300, 620)],
    "orn_r": [(372, 572), (418, 572), (430, 625), (422, 658), (372, 658), (365, 620)],
}


def draw_clips(rgba: np.ndarray, lines, path: Path) -> None:
    h, w = rgba.shape[:2]
    a = rgba[:, :, 3:4].astype(np.float32) / 255.0
    vis = (rgba[:, :, :3] * a + checker(h, w) * (1 - a)).astype(np.uint8)
    p = Image.fromarray(vis)
    d = ImageDraw.Draw(p, "RGBA")
    colors = {
        "arm_r": (255, 180, 40, 255),
        "hand_l": (70, 160, 255, 255),
        "fore_l": (80, 210, 255, 255),
        "up_l": (40, 100, 255, 255),
        "head": (0, 255, 120, 255),
        "neck": (0, 200, 80, 255),
        "choker_band": (255, 215, 0, 255),
        "leotard": (220, 0, 220, 255),
        "thigh_l": (255, 0, 180, 255),
        "thigh_r": (255, 120, 200, 255),
        "clutch": (255, 255, 0, 255),
        "bangs": (180, 255, 180, 255),
        "bun_l": (200, 200, 200, 255),
        "bun_r": (200, 200, 200, 255),
        "br_r": (255, 80, 80, 255),
        "br_l": (255, 80, 80, 255),
    }
    for name, col in colors.items():
        pts = [tuple(p_) for p_ in POLYS[name]]
        d.polygon(pts, fill=col[:3] + (40,))
        d.line(pts + [pts[0]], fill=col, width=2)
    for (p1, p2, col) in lines:
        d.line([p1, p2], fill=col, width=3)
    p.save(path)


def write_contact(plates_dir: Path, order, w, h) -> None:
    cols = 5
    rows = (len(order) + cols - 1) // cols
    cell_w, cell_h = 300, 380
    sheet = np.full((rows * cell_h, cols * cell_w, 3), 28, np.uint8)
    try:
        font = ImageFont.truetype("arial.ttf", 16)
    except Exception:
        font = ImageFont.load_default()
    for i, name in enumerate(order):
        rr, cc = divmod(i, cols)
        plate = np.array(Image.open(plates_dir / f"{name}.png"))
        m = plate[:, :, 3] > 8
        cell = checker(cell_h - 28, cell_w, 12)
        if m.any():
            ys, xs = np.where(m)
            x0, x1 = max(0, xs.min() - 6), min(w, xs.max() + 7)
            y0, y1 = max(0, ys.min() - 6), min(h, ys.max() + 7)
            crop = plate[y0:y1, x0:x1]
            ch, cw = crop.shape[:2]
            scale = min((cell_w - 16) / cw, (cell_h - 44) / ch)
            nw, nh = max(1, int(cw * scale)), max(1, int(ch * scale))
            crop_np = np.array(Image.fromarray(crop).resize((nw, nh), Image.Resampling.LANCZOS))
            ox = (cell_w - nw) // 2
            oy = (cell_h - 28 - nh) // 2
            aa = crop_np[:, :, 3:4].astype(np.float32) / 255.0
            region = cell[oy : oy + nh, ox : ox + nw]
            cell[oy : oy + nh, ox : ox + nw] = (crop_np[:, :, :3] * aa + region * (1 - aa)).astype(np.uint8)
        tile = np.full((cell_h, cell_w, 3), 22, np.uint8)
        tile[28:] = cell
        tile_im = Image.fromarray(tile)
        ImageDraw.Draw(tile_im).text((8, 6), name, fill=(230, 230, 230), font=font)
        sheet[rr * cell_h : (rr + 1) * cell_h, cc * cell_w : (cc + 1) * cell_w] = np.array(tile_im)
    Image.fromarray(sheet).save(plates_dir / "contact-layers.jpg", quality=93)


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    DBG.mkdir(parents=True, exist_ok=True)
    for p in list(OUT.glob("*.png")) + list(OUT.glob("*.jpg")):
        p.unlink()

    rgba = np.array(Image.open(SRC).convert("RGBA"))
    h, w = rgba.shape[:2]
    assert (w, h) == (672, 1127), (w, h)
    r = rgba[:, :, 0].astype(np.int16)
    g = rgba[:, :, 1].astype(np.int16)
    b = rgba[:, :, 2].astype(np.int16)
    a = rgba[:, :, 3]
    on = a > 8
    luma = (r + g + b) / 3.0
    chroma = np.maximum(np.maximum(r, g), b) - np.minimum(np.minimum(r, g), b)
    bgr = cv2.cvtColor(rgba[:, :, :3], cv2.COLOR_RGB2BGR)
    yy, xx = np.indices((h, w))
    P = {k: poly(h, w, v) for k, v in POLYS.items()}

    hair_w = on & (r <= g + 8) & (luma > 42) & (np.abs(r - b) < 40)
    gold_w = (
        on & (g - b >= 24) & (r - b >= 46) & (r - g <= 52)
        & (luma >= 40) & (luma <= 168) & (chroma >= 34)
    )
    black_w = on & (luma < 92)
    peach = (r > g + 6) & (r > b + 8) & (luma > 70) & (luma < 235) & on
    rim = on & (b >= r - 2) & (luma > 150) & ~hair_w  # cool thigh rim-light
    metal = on & (r > g + 4) & (r > b + 12) & (luma > 55) & (luma < 175) & (chroma > 22)

    UNDERBUST = ((322, 356), (452, 366))
    underbust = thick_line(h, w, *UNDERBUST, 7)
    draw_clips(rgba, [(UNDERBUST[0], UNDERBUST[1], (255, 0, 255, 255))], DBG / "clips.png")

    plates: dict[str, np.ndarray] = {k: np.zeros((h, w), bool) for k in ORDER}
    skin_loose = on & (r > b + 5) & (g > 40) & (luma > 55) & ~hair_w
    spec = on & (chroma < 35) & (luma > 120) & ~hair_w  # leotard/boot specular

    # ===== HAIR =====
    head_region = P["head"] | P["neck"]
    scalp_sep = thick_line(h, w, (358, 0), (358, 250), 18) | P["head"]
    hair_l = flood(hair_w & ~scalp_sep, [(80, 200), (160, 400), (60, 350), (250, 100), (200, 500), (100, 150)], "hair_l")
    hair_r = flood(hair_w & ~scalp_sep, [(560, 200), (600, 280), (450, 150), (571, 549), (620, 480)], "hair_r")
    plates["hair_top_l"] = hair_l & P["bun_l"]
    plates["hair_top_r"] = hair_r & P["bun_r"]
    plates["hair_back_l"] = hair_l & ~ndi.binary_erosion(plates["hair_top_l"], iterations=6)
    plates["hair_back_r"] = hair_r & ~ndi.binary_erosion(plates["hair_top_r"], iterations=6)
    plates["hair_front"] = hair_w & P["bangs"]
    hair_mass = hair_l | hair_r | plates["hair_front"]
    body = on & ~hair_mass

    # ===== HEAD =====
    plates["head"] = ndi.binary_fill_holes(flood(on & ~hair_w & head_region, [(358, 153), (360, 185), (350, 215)], "head"))
    plates["head"] &= on & ~hair_w
    plates["head"] = smooth_mask(plates["head"])

    # ===== OVERLAY METAL (tight, no peach) =====
    choker_band = P["choker_band"] & on & ~hair_w & (black_w | gold_w)
    choker_pend = P["choker_pend"] & on & ~hair_w & (gold_w | (metal & ~peach))
    plates["choker"] = drop_specks(close_open(choker_band | choker_pend, 2, 0), 18)
    ear_m = (gold_w | (metal & ~peach))
    plates["earring_l"] = drop_specks(close_open(flood(P["ear_l"] & ear_m, [(308, 215), (318, 210)], "ear_l"), 1, 0), 6)
    plates["earring_r"] = drop_specks(close_open(flood(P["ear_r"] & ear_m, [(396, 234), (390, 228)], "ear_r"), 1, 0), 5)
    plates["hair_tie_l"] = drop_specks(close_open(P["tie_l"] & (gold_w | metal), 2, 0), 8)
    plates["hair_tie_r"] = drop_specks(close_open(P["tie_r"] & (gold_w | metal), 2, 0), 6)
    br_loose = (g - b >= 16) & (r - b >= 36) & (luma >= 50) & (luma <= 175) & (chroma >= 24) & on
    plates["jewelry_wrist_r"] = drop_specks(close_open(P["br_r"] & gold_w, 2, 0), 16)
    plates["jewelry_wrist_l"] = drop_specks(close_open(P["br_l"] & gold_w, 2, 0), 12)

    # ===== CLUTCH first so arms can subtract it =====
    clutch_walk = P["clutch"] & on & ~peach & ~hair_mass & (black_w | gold_w | (luma < 145))
    plates["clutch"] = smooth_mask(close_open(flood(clutch_walk, [(574, 472), (590, 480), (545, 460)], "clutch"), 1, 0))

    # ===== LEOTARD (dark + specular, never peach / rim) =====
    leo_walk = P["leotard"] & body & ~peach & (black_w | spec | (luma < 100))
    leo = flood(leo_walk, [(385, 335), (370, 280), (400, 450), (360, 500), (430, 300)], "leotard")
    leo = smooth_mask(close_open(leo, 1, 1))
    leo &= on & ~hair_mass & ~peach
    plates["chest"] = flood(leo & ~underbust, [(385, 300), (360, 280), (420, 305)], "chest")
    plates["torso"] = flood(leo & ~underbust, [(385, 450), (360, 500), (410, 480)], "torso")
    plates["chest"] |= ndi.binary_dilation(plates["chest"], iterations=3) & leo
    plates["torso"] |= ndi.binary_dilation(plates["torso"], iterations=3) & leo

    # ===== BOOTS then gothic toppers (cuff cut) =====
    boot_walk = body & (black_w | (luma < 110)) & (yy > 640) & ~peach
    boot_all = flood(boot_walk, [(330, 980), (395, 1000), (340, 850), (385, 850)], "boots")
    split_x = 358.0 - 0.012 * (yy.astype(np.float32) - 650.0)
    left_leg = xx < split_x
    plates["boot_l"] = smooth_mask(boot_all & (yy >= 664) & left_leg)
    plates["boot_r"] = smooth_mask(boot_all & (yy >= 664) & ~left_leg)
    orn_walk = (black_w | gold_w | (luma < 105)) & ~peach & (yy > 568) & (yy < 658) & on
    plates["boot_ornament_l"] = drop_specks(
        close_open(flood(orn_walk & P["orn_l"], [(328, 638), (318, 600)], "orn_l"), 1, 0), 30,
    )
    plates["boot_ornament_r"] = drop_specks(
        close_open(flood(orn_walk & P["orn_r"], [(400, 638), (400, 600)], "orn_r"), 1, 0), 30,
    )

    # ===== THIGHS (zone flood, not polygon-cut; includes inner gap) =====
    thigh_zone = (yy > 390) & (yy < 715) & (xx > 240) & (xx < 470)
    thigh_all = flood(
        body & thigh_zone & ~leo & ~boot_all & (skin_loose | rim),
        [(300, 500), (280, 560), (320, 620), (420, 540), (430, 600), (360, 520)],
        "thigh",
    )
    plates["thigh_l"] = smooth_mask(thigh_all & left_leg)
    plates["thigh_r"] = smooth_mask(thigh_all & ~left_leg)

    # ===== RAISED ARM: box flood + 3-seed watershed (no infinite halfplanes) =====
    arm_r_box = (xx > 148) & (xx < 355) & (yy > 128) & (yy < 372)
    shoulder_cut_r = thick_line(h, w, (318, 228), (350, 255), 12) | head_region
    arm_r = flood(
        body & arm_r_box & ~leo & ~black_w & ~shoulder_cut_r,
        [(189, 203), (200, 175), (211, 305), (284, 256), (320, 265), (230, 330)],
        "arm_r",
    )
    plates["hand_r"] = arm_r & (yy < 258) & (xx < 240)
    rest_r = arm_r & ~plates["hand_r"]
    ws_r = split_markers(
        rest_r,
        [(2, [(198, 290), (205, 270)]), (3, [(330, 270), (310, 255)])],
        radius=8,
    )
    plates["arm_forearm_r"] = ws_r == 2
    plates["arm_upper_r"] = ws_r == 3

    # ===== CLUTCH ARM =====
    arm_l_box = (xx > 400) & (xx < 630) & (yy > 300) & (yy < 450)
    shoulder_cut_l = thick_line(h, w, (418, 308), (448, 348), 10)
    arm_l = flood(
        body & arm_l_box & ~leo & ~black_w & ~shoulder_cut_l & ~plates["clutch"],
        [(564, 419), (545, 410), (520, 400), (450, 350), (435, 340)],
        "arm_l",
    )
    ws_l = split_markers(
        arm_l,
        [(1, [(580, 415), (560, 420)]), (2, [(520, 400), (505, 385)]), (3, [(450, 350), (435, 340)])],
        radius=12,
    )
    clutch_grow = ndi.binary_dilation(plates["clutch"], iterations=2)
    plates["hand_l"] = (ws_l == 1) & ~clutch_grow
    plates["arm_forearm_l"] = (ws_l == 2) & ~clutch_grow
    plates["arm_upper_l"] = (ws_l == 3) & ~clutch_grow

    # similar-color 1px grow — skip metal overlays
    skip_grow = {
        "jewelry_wrist_r", "jewelry_wrist_l", "earring_l", "earring_r",
        "hair_tie_l", "hair_tie_r", "choker", "boot_ornament_l", "boot_ornament_r",
    }
    for name in ORDER:
        if name in skip_grow:
            continue
        plates[name] = dilate_similar(plates[name], rgba, on, thresh=26)

    owned = np.logical_or.reduce([plates[k] for k in ORDER])
    leftover = on & ~owned
    print("leftover before fill", int(leftover.sum()), f"({100 * leftover[on].mean():.2f}% opaque)")
    Image.fromarray((leftover.astype(np.uint8) * 255)).save(DBG / "leftover.png")

    # leftover: color + tight zones (never dump neck into the hand)
    plates["hair_back_l"] |= leftover & hair_w & (xx < 358)
    plates["hair_back_r"] |= leftover & hair_w & (xx >= 358)
    plates["head"] |= leftover & ~hair_w & head_region
    plates["thigh_l"] |= leftover & (skin_loose | rim) & thigh_zone & left_leg & ~leo
    plates["thigh_r"] |= leftover & (skin_loose | rim) & thigh_zone & ~left_leg & ~leo
    plates["chest"] |= leftover & (black_w | spec) & P["leotard"] & (yy <= 370)
    plates["torso"] |= leftover & (black_w | spec) & P["leotard"] & (yy >= 350)
    # leftover on the already-flooded arm silhouette only (no neck/torso rectangles)
    plates["hand_r"] |= leftover & arm_r & (yy < 258)
    plates["arm_forearm_r"] |= leftover & rest_r & (ws_r == 2)
    plates["arm_upper_r"] |= leftover & rest_r & (ws_r == 3)
    plates["hand_l"] |= leftover & arm_l & (ws_l == 1) & ~clutch_grow
    plates["arm_forearm_l"] |= leftover & arm_l & (ws_l == 2) & ~clutch_grow
    plates["arm_upper_l"] |= leftover & arm_l & (ws_l == 3) & ~clutch_grow
    plates["boot_l"] |= leftover & ~peach & (yy >= 664) & left_leg
    plates["boot_r"] |= leftover & ~peach & (yy >= 664) & ~left_leg
    plates["clutch"] |= leftover & ~peach & P["clutch"]
    leftover = on & ~np.logical_or.reduce([plates[k] for k in ORDER])
    print("leftover final", int(leftover.sum()))
    Image.fromarray((leftover.astype(np.uint8) * 255)).save(DBG / "leftover_final.png")

    z = np.zeros((h, w, 4), np.uint8)
    counts = {}
    for name in ORDER:
        m = plates[name]
        plate = z.copy()
        plate[m] = rgba[m]
        Image.fromarray(plate).save(OUT / f"{name}.png")
        counts[name] = int(m.sum())
        print(f"  {name:18s} {counts[name]:7d}")

    comp = z.copy()
    for name in ORDER:
        m = plates[name]
        comp[m] = rgba[m]
    Image.fromarray(comp).save(OUT / "composite.png")
    diff = np.abs(comp.astype(np.int16) - rgba.astype(np.int16)).sum(axis=2)
    print("composite max channel-sum diff", int(diff.max()), "mean on opaque", float(diff[on].mean()))

    rng = np.random.default_rng(2)
    vis = (rgba[:, :, :3] * (rgba[:, :, 3:4] / 255.0) + checker(h, w) * (1 - rgba[:, :, 3:4] / 255.0)).astype(np.uint8)
    for name in ORDER:
        col = rng.integers(40, 255, size=3)
        m = plates[name]
        vis[m] = (vis[m] * 0.35 + col * 0.65).astype(np.uint8)
    Image.fromarray(vis).save(DBG / "mask_overlay.png")

    write_contact(OUT, ORDER, w, h)
    manifest = {
        "source": str(SRC),
        "size": [w, h],
        "method": "fine-all: watershed arm joints, metal overlays, zone thighs, cuff-cut ornaments; no PS",
        "draw_order_back_to_front": ORDER,
        "counts": counts,
        "leftover_final": int(leftover.sum()),
        "composite_max_diff": int(diff.max()),
    }
    (OUT / "manifest.json").write_text(json.dumps(manifest, indent=2, ensure_ascii=False), encoding="utf-8")
    print("wrote", OUT)


if __name__ == "__main__":
    main()
