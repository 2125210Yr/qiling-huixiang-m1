"""Hand-traced Live2D import PSD from spec-swim-still. Same canvas. No Photoshop."""
from __future__ import annotations

import json
from pathlib import Path

import cv2
import numpy as np
from PIL import Image, ImageDraw, ImageFont
from pytoshop import enums
from pytoshop.user.nested_layers import Image as PsdImage
from pytoshop.user.nested_layers import nested_layers_to_psd
from scipy import ndimage as ndi

SRC = Path(r"C:\Users\Administrator\.grok\skills\dc-presenter-art\eval\spec-swim-still.jpg")
OUT_DIR = Path(r"F:\天命之子\art\characters\_new-candidates")
OUT_PSD = OUT_DIR / "spec-swim-import.psd"
EVAL = Path(r"C:\Users\Administrator\.grok\skills\dc-presenter-art\eval")

# Traced on _zoom grids (20px). Absolute still coords.
POLYS = {
    "goggles": [
        (408, 178), (448, 170), (498, 186), (508, 210),
        (492, 248), (450, 256), (412, 248), (398, 218), (400, 190),
    ],
    "face": [
        (400, 188), (430, 172), (460, 176), (480, 196),
        (490, 232), (478, 270), (456, 296), (428, 304),
        (406, 290), (394, 250), (396, 210),
    ],
    "swimsuit": [
        (412, 292), (438, 276), (458, 292), (478, 328),
        (492, 360), (500, 400), (508, 448), (520, 470),
        (528, 480), (516, 508), (492, 548), (460, 568),
        (428, 548), (408, 500), (396, 448), (388, 400),
        (386, 360), (392, 320),
    ],
    "hand_l": [
        (190, 498), (186, 522), (208, 548), (252, 550),
        (262, 522), (242, 498), (214, 492),
    ],
    "arm_forearm_l": [
        (236, 428), (228, 500), (258, 548), (298, 508),
        (300, 448), (270, 418),
    ],
    "arm_upper_l": [
        (308, 298), (332, 286), (350, 340), (348, 400),
        (338, 450), (300, 500), (268, 430), (286, 340),
    ],
    "hand_r": [
        (618, 426), (668, 428), (686, 452), (672, 486),
        (632, 492), (608, 458),
    ],
    "arm_forearm_r": [
        (548, 418), (628, 426), (622, 490), (538, 482), (528, 440),
    ],
    "arm_upper_r": [
        (488, 392), (528, 386), (558, 420), (542, 482),
        (500, 508), (478, 450),
    ],
    "leg_l": [
        (398, 538), (386, 560), (378, 640), (388, 800),
        (398, 920), (390, 1000), (376, 1048), (400, 1062),
        (422, 1000), (418, 880), (408, 700), (418, 580),
    ],
    "leg_r": [
        (428, 548), (460, 568), (500, 620), (520, 700),
        (528, 800), (520, 920), (500, 1020), (478, 1100),
        (448, 1130), (428, 1100), (438, 1000), (448, 800),
        (438, 640),
    ],
    "footwear_l": [
        (366, 998), (400, 990), (430, 1006), (430, 1062),
        (400, 1072), (364, 1056),
    ],
    "footwear_r": [
        (400, 1070), (448, 1058), (470, 1088), (462, 1150),
        (418, 1162), (390, 1134),
    ],
    "neck": [
        (400, 268), (430, 258), (468, 262), (490, 290),
        (478, 330), (430, 340), (398, 322),
    ],
    "back_hair": [  # ponytail bun on the crown
        (318, 126), (348, 110), (400, 108), (442, 124),
        (458, 156), (448, 186), (412, 168), (372, 156),
        (332, 166), (308, 146),
    ],
    "front_hair_l": [
        (376, 166), (400, 154), (420, 168), (426, 198),
        (414, 228), (392, 232), (376, 204), (370, 180),
    ],
    "front_hair_r": [
        (484, 172), (512, 164), (532, 182), (536, 216),
        (522, 240), (500, 232), (484, 204),
    ],
    "front_hair": [  # placeholder, replaced by L|R union
        (388, 174), (412, 164), (432, 188), (422, 228), (396, 226), (384, 198),
    ],
    "side_hair_l": [  # left cascade, wide enough to catch the S
        (300, 148), (348, 128), (372, 168), (350, 240),
        (328, 310), (290, 370), (230, 420), (150, 460),
        (90, 520), (100, 560), (180, 540), (250, 520),
        (300, 548), (340, 500), (352, 420), (358, 330), (348, 220), (328, 168),
    ],
    "side_hair_r": [  # right cascade under the arm
        (516, 330), (548, 350), (620, 390), (700, 430),
        (748, 490), (738, 550), (680, 560), (600, 530),
        (548, 490), (520, 430), (512, 370),
    ],
    "hair_tip_front_l": [  # C-hook + connecting ribbon, 10px grid
        (108, 250), (158, 242), (210, 246), (242, 258),
        (248, 310), (232, 352), (190, 362), (140, 352),
        (112, 322), (106, 272),
    ],
    "hair_tip_front_r": [  # over the right hand
        (630, 400), (710, 430), (752, 490), (720, 540),
        (640, 530), (580, 480), (600, 420),
    ],
    "hair_tip_back_l": [  # bottom left S
        (72, 540), (140, 520), (220, 560), (280, 620),
        (330, 700), (340, 780), (300, 840), (220, 830),
        (140, 780), (90, 700), (70, 600),
    ],
    "hair_tip_back_r": [  # bottom right S
        (620, 560), (700, 550), (752, 600), (730, 680),
        (690, 760), (630, 810), (570, 760), (560, 660),
        (580, 590),
    ],
    "ear_l": [(348, 218), (372, 210), (388, 240), (372, 278), (348, 260)],
    "ear_r": [(500, 220), (528, 218), (548, 248), (528, 278), (500, 250)],
}

ZOOM = Path(r"C:\Users\Administrator\.grok\skills\dc-presenter-art\eval\_zoom")
for jf in ("polys_hair.json", "polys_face.json", "polys_body.json", "polys_arms.json", "polys_legs.json"):
    p = ZOOM / jf
    if not p.exists():
        print("missing", jf)
        continue
    data = json.loads(p.read_text(encoding="utf-8"))
    n = 0
    for k, pts in data.items():
        if not pts or not isinstance(pts, list):
            continue
        if isinstance(pts[0][0], (int, float)):
            POLYS[k] = [(int(x), int(y)) for x, y in pts]
            n += 1
    print("merged", jf, n, "keys")


def poly(h, w, pts):
    m = Image.new("L", (w, h), 0)
    ImageDraw.Draw(m).polygon([(int(x), int(y)) for x, y in pts], fill=255)
    return np.asarray(m) > 0


def close_open(m, c=1, o=0):
    st = ndi.generate_binary_structure(2, 1)
    if c:
        m = ndi.binary_closing(m, structure=st, iterations=c)
    if o:
        m = ndi.binary_opening(m, structure=st, iterations=o)
    return m


def plate(rgb, mask):
    out = np.zeros(rgb.shape[:2] + (4,), np.uint8)
    m = np.asarray(mask, bool)
    out[m, :3] = rgb[m]
    out[m, 3] = 255
    return out


def inpaint_under(rgb, hole, guide):
    if not hole.any():
        return rgb.copy()
    src = rgb.copy()
    if guide.any():
        src[hole] = rgb[guide].mean(axis=0)
    filled = cv2.inpaint(src, hole.astype(np.uint8) * 255, 9, cv2.INPAINT_TELEA)
    out = rgb.copy()
    out[hole] = filled[hole]
    return out


def rgba_layer(name, rgba, full):
    a = rgba[:, :, 3]
    if int((a > 8).sum()) < 16:
        print(f"  skip {name}")
        return None
    ys, xs = np.where(a > 8)
    y0, y1 = max(0, int(ys.min()) - 1), min(full[0], int(ys.max()) + 2)
    x0, x1 = max(0, int(xs.min()) - 1), min(full[1], int(xs.max()) + 2)
    crop = rgba[y0:y1, x0:x1]
    print(f"  {name:22s} {int((a > 8).sum()):7d}")
    return PsdImage(
        name=str(name),
        top=y0,
        left=x0,
        bottom=y1,
        right=x1,
        channels={
            0: np.ascontiguousarray(crop[:, :, 0]),
            1: np.ascontiguousarray(crop[:, :, 1]),
            2: np.ascontiguousarray(crop[:, :, 2]),
            -1: np.ascontiguousarray(crop[:, :, 3]),
        },
        color_mode=enums.ColorMode.rgb,
    )


def checker(h, w, cell=12):
    yy, xx = np.indices((h, w))
    ch = np.zeros((h, w, 3), np.uint8)
    ch[((yy // cell) + (xx // cell)) % 2 == 0] = (86, 86, 86)
    ch[((yy // cell) + (xx // cell)) % 2 == 1] = (48, 48, 48)
    return ch


def main():
    rgb = np.array(Image.open(SRC).convert("RGB"))
    h, w = rgb.shape[:2]
    r = rgb[:, :, 0].astype(np.int16)
    g = rgb[:, :, 1].astype(np.int16)
    bch = rgb[:, :, 2].astype(np.int16)
    luma = (r + g + bch) / 3.0
    chroma = np.maximum(np.maximum(r, g), bch) - np.minimum(np.minimum(r, g), bch)
    bg = (np.abs(r - 21) + np.abs(g - 21) + np.abs(bch - 29)) < 22
    on = ~bg
    P = {k: poly(h, w, v) for k, v in POLYS.items()}
    P["front_hair"] = P["front_hair_l"] | P["front_hair_r"]
    yy, xx = np.indices((h, w))
    peach = on & (r > g + 5) & (r > bch + 6) & (luma > 70)
    hair_w = on & (luma > 120) & (np.abs(r - g) < 24)
    dark = on & (luma < 90) & ~peach

    def vis(name, color_gate=None, dilate=0, morph=True):
        m = P[name]
        if dilate:
            m = ndi.binary_dilation(m, iterations=dilate)
        if color_gate is None:
            m = m & on
        else:
            m = m & color_gate
        if morph:
            m = close_open(m, 1, 0)
        return m

    goggles = vis("goggles", dark, 0)
    swimsuit = vis("swimsuit", dark & (luma < 100), 1)
    hand_keep = vis("hand_l", peach, 1) | vis("hand_r", peach, 1)

    hair_loose = on & (luma > 88) & (np.abs(r - g) < 42) & ~peach

    def hair_cut(name, loose=False):
        gate = hair_loose if loose else hair_w
        m = P[name] & gate & ~peach & ~goggles & ~hand_keep
        m = close_open(m, 1, 0)
        return m

    peach_mean = rgb[peach].mean(axis=0) if peach.any() else np.array([220, 180, 160])

    def skin_plate(mask, hole):
        """Underpaint plate: original peach + inpainted skin, never keep black fabric."""
        rgb2 = inpaint_under(rgb, mask & hole, peach)
        out = plate(rgb2, mask)
        dark_here = (out[:, :, 3] > 8) & (luma < 108)
        out[dark_here, 0] = peach_mean[0]
        out[dark_here, 1] = peach_mean[1]
        out[dark_here, 2] = peach_mean[2]
        return out

    chest_mask = P.get("chest", P["swimsuit"] & (yy < 510))
    torso_mask = P.get("torso", P["swimsuit"] & (yy >= 470))
    chest_p = skin_plate(chest_mask, ~peach)
    torso_p = skin_plate(torso_mask, ~peach)

    gog_grow = ndi.binary_dilation(goggles, iterations=3)
    hair_on_cheek = hair_w & (yy > 198)
    face_mask = P["face"] & ~gog_grow & ~hair_on_cheek
    face_p = plate(rgb, face_mask & peach)
    forehead = P["face"] & (yy <= 202) & ~gog_grow
    face_p[forehead, 0] = peach_mean[0]
    face_p[forehead, 1] = peach_mean[1]
    face_p[forehead, 2] = peach_mean[2]
    face_p[forehead, 3] = 255
    face_p[gog_grow] = 0
    face_p[hair_on_cheek] = 0

    neck_p = skin_plate(P["neck"], ~peach)

    ear_l_p = plate(rgb, vis("ear_l", peach, 0))
    ear_r_gate = on & (luma > 38) & (luma < 145) & ~peach
    ear_r_p = plate(rgb, vis("ear_r", ear_r_gate, 0))

    order = [
        ("back_hair", plate(rgb, hair_cut("back_hair"))),
        ("hair_tip_back_l", plate(rgb, hair_cut("hair_tip_back_l"))),
        ("hair_tip_back_r", plate(rgb, hair_cut("hair_tip_back_r"))),
        ("leg_l", plate(rgb, vis("leg_l", peach, 1))),
        ("leg_r", plate(rgb, vis("leg_r", peach, 1))),
        ("torso", torso_p),
        ("chest", chest_p),
        ("swimsuit", plate(rgb, swimsuit)),
        ("arm_upper_l", plate(rgb, vis("arm_upper_l", peach, 1))),
        ("arm_forearm_l", plate(rgb, vis("arm_forearm_l", peach, 1))),
        ("hand_l", plate(rgb, vis("hand_l", peach, 1))),
        ("arm_upper_r", plate(rgb, vis("arm_upper_r", peach, 1))),
        ("arm_forearm_r", plate(rgb, vis("arm_forearm_r", peach, 1))),
        ("hand_r", plate(rgb, vis("hand_r", peach, 1))),
        ("neck", neck_p),
        ("ear_l", ear_l_p),
        ("ear_r", ear_r_p),
        ("face", face_p),
        ("brow_l", plate(rgb, P["brow_l"] & on)),
        ("brow_r", plate(rgb, P["brow_r"] & on)),
        ("iris_l", plate(rgb, P["iris_l"] & on)),
        ("iris_r", plate(rgb, P["iris_r"] & on)),
        ("eyewhite_l", plate(rgb, (P["eyewhite_l"] & on) & ~P["iris_l"])),
        ("eyewhite_r", plate(rgb, (P["eyewhite_r"] & on) & ~P["iris_r"])),
        ("lash_l", plate(rgb, P["lash_l"] & on)),
        ("lash_r", plate(rgb, P["lash_r"] & on)),
        ("nose", plate(rgb, P["nose"] & peach)),
        ("mouth_inside", plate(rgb, P["mouth_inside"] & on)),
        ("mouth_upper", plate(rgb, P["mouth_upper"] & on)),
        ("mouth_lower", plate(rgb, P["mouth_lower"] & on)),
        ("front_hair", plate(rgb, hair_cut("front_hair", loose=True))),
        ("hair_tip_front_l", plate(
            rgb,
            close_open(
                ndi.binary_propagation(
                    P["hair_tip_front_l"] & hair_loose,
                    mask=ndi.binary_dilation(P["hair_tip_front_l"], iterations=3) & hair_loose & ~peach & ~hand_keep,
                ),
                3,
                0,
            ),
        )),
        ("hair_tip_front_r", plate(rgb, hair_cut("hair_tip_front_r"))),
        ("side_hair_l", plate(rgb, hair_cut("side_hair_l"))),
        ("side_hair_r", plate(rgb, hair_cut("side_hair_r"))),
        ("goggles", plate(rgb, goggles)),
        ("footwear_l", plate(rgb, vis("footwear_l", dark, 1))),
        ("footwear_r", plate(rgb, vis("footwear_r", dark, 1))),
    ]

    layers = []
    for name, arr in order:
        lyr = rgba_layer(name, arr, (h, w))
        if lyr is not None:
            layers.append(lyr)

    comp = np.zeros((h, w, 4), np.uint8)
    for _, arr in order:
        m = arr[:, :, 3] > 8
        comp[m] = arr[m]
    guide = rgba_layer("_guide_composite", comp, (h, w))
    if guide is not None:
        guide.visible = False
        layers.insert(0, guide)

    psd = nested_layers_to_psd(layers, color_mode=enums.ColorMode.rgb, size=(h, w), compression=enums.Compression.raw)
    with OUT_PSD.open("wb") as f:
        psd.write(f)
    with (EVAL / "spec-swim-import.psd").open("wb") as f:
        psd.write(f)
    print("wrote", OUT_PSD, OUT_PSD.stat().st_size)

    vis_names = [n for n, a in order if int((a[:, :, 3] > 8).sum()) >= 16]
    cols = 5
    rows = (len(vis_names) + cols - 1) // cols
    cw, ch = 240, 300
    sheet = np.full((rows * ch, cols * cw, 3), 28, np.uint8)
    try:
        font = ImageFont.truetype("arial.ttf", 15)
    except Exception:
        font = ImageFont.load_default()
    lookup = dict(order)
    for i, name in enumerate(vis_names):
        rr, cc = divmod(i, cols)
        pa = lookup[name]
        m = pa[:, :, 3] > 8
        cell = checker(ch - 26, cw, 10)
        if m.any():
            ys, xs = np.where(m)
            x0, x1 = max(0, xs.min() - 6), min(w, xs.max() + 7)
            y0, y1 = max(0, ys.min() - 6), min(h, ys.max() + 7)
            crop = pa[y0:y1, x0:x1]
            hh, ww = crop.shape[:2]
            scale = min((cw - 14) / ww, (ch - 44) / hh)
            nw, nh = max(1, int(ww * scale)), max(1, int(hh * scale))
            crop_np = np.array(Image.fromarray(crop).resize((nw, nh), Image.Resampling.LANCZOS))
            ox = (cw - nw) // 2
            oy = (ch - 26 - nh) // 2
            aa = crop_np[:, :, 3:4].astype(np.float32) / 255.0
            region = cell[oy : oy + nh, ox : ox + nw]
            cell[oy : oy + nh, ox : ox + nw] = (crop_np[:, :, :3] * aa + region * (1 - aa)).astype(np.uint8)
        tile = np.full((ch, cw, 3), 22, np.uint8)
        tile[26:] = cell
        tim = Image.fromarray(tile)
        ImageDraw.Draw(tim).text((8, 5), name, fill=(230, 230, 230), font=font)
        sheet[rr * ch : (rr + 1) * ch, cc * cw : (cc + 1) * cw] = np.array(tim)
    Image.fromarray(sheet).save(OUT_DIR / "spec-swim-psd-contact.jpg", quality=93)
    Image.fromarray(sheet).save(EVAL / "spec-swim-psd-contact.jpg", quality=93)
    print("contact ok", len(vis_names), "layers")
    hair_names = [n for n in vis_names if "hair" in n]
    cols_h, cw_h, ch_h = 4, 280, 340
    rows_h = (len(hair_names) + cols_h - 1) // cols_h
    hsheet = np.full((rows_h * ch_h, cols_h * cw_h, 3), 28, np.uint8)
    for i, name in enumerate(hair_names):
        rr, cc = divmod(i, cols_h)
        pa = lookup[name]
        m = pa[:, :, 3] > 8
        cell = checker(ch_h - 26, cw_h, 10)
        if m.any():
            ys, xs = np.where(m)
            x0, x1 = max(0, xs.min() - 6), min(w, xs.max() + 7)
            y0, y1 = max(0, ys.min() - 6), min(h, ys.max() + 7)
            crop = pa[y0:y1, x0:x1]
            hh, ww = crop.shape[:2]
            scale = min((cw_h - 14) / ww, (ch_h - 44) / hh)
            nw, nh = max(1, int(ww * scale)), max(1, int(hh * scale))
            crop_np = np.array(Image.fromarray(crop).resize((nw, nh), Image.Resampling.LANCZOS))
            ox = (cw_h - nw) // 2
            oy = (ch_h - 26 - nh) // 2
            aa = crop_np[:, :, 3:4].astype(np.float32) / 255.0
            region = cell[oy : oy + nh, ox : ox + nw]
            cell[oy : oy + nh, ox : ox + nw] = (crop_np[:, :, :3] * aa + region * (1 - aa)).astype(np.uint8)
        tile = np.full((ch_h, cw_h, 3), 22, np.uint8)
        tile[26:] = cell
        tim = Image.fromarray(tile)
        ImageDraw.Draw(tim).text((8, 5), name, fill=(230, 230, 230), font=font)
        hsheet[rr * ch_h : (rr + 1) * ch_h, cc * cw_h : (cc + 1) * cw_h] = np.array(tim)
    Image.fromarray(hsheet).save(OUT_DIR / "spec-swim-hair-contact.jpg", quality=93)
    Image.fromarray(hsheet).save(EVAL / "spec-swim-hair-contact.jpg", quality=93)
    print("hair contact", hair_names)


if __name__ == "__main__":
    main()
