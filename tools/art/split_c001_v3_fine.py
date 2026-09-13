"""Fine split of C001 presenter-v3 using warm/cool separation."""
from __future__ import annotations

import json
from pathlib import Path

import cv2
import numpy as np
from PIL import Image
from scipy import ndimage

SRC = Path(r"F:\天命之子\art\characters\C001-焰刃\presenter-v3.png")
BLINK = Path(r"F:\天命之子\art\characters\C001-焰刃\presenter_blink.png")
OUT = Path(r"F:\天命之子\client\Assets\Resources\Art\Characters\C001\V3Layers")
ART = Path(r"F:\天命之子\art\characters\C001-焰刃\v3-layers")
DBG = ART / "_dbg"
MAGENTA = (255, 0, 220)


def save_rgba(name: str, rgb: np.ndarray, alpha: np.ndarray) -> None:
    a = np.clip(alpha * 255.0, 0, 255).astype(np.uint8)
    im = Image.fromarray(np.dstack([rgb, a]), "RGBA")
    im.save(OUT / name)
    im.save(ART / name)
    bg = np.full_like(rgb, MAGENTA)
    af = (a.astype(np.float32) / 255.0)[..., None]
    prev = (rgb.astype(np.float32) * af + bg.astype(np.float32) * (1.0 - af)).astype(np.uint8)
    Image.fromarray(prev).save(DBG / ("preview_" + name))


def feather(mask: np.ndarray, px: float) -> np.ndarray:
    m = mask.astype(np.float32)
    if px > 0:
        m = ndimage.gaussian_filter(m, px)
    return np.clip(m, 0, 1)


def biggest(m: np.ndarray, min_px: int = 350, rel: float = 0.06) -> np.ndarray:
    lab, n = ndimage.label(m)
    if n == 0:
        return m
    sizes = ndimage.sum(m, lab, index=np.arange(1, n + 1))
    keep = np.where(sizes >= max(min_px, float(np.max(sizes)) * rel))[0] + 1
    return np.isin(lab, keep)


def flood_seeds(rgb: np.ndarray, barrier: np.ndarray, seeds: list[tuple[int, int]], lo: int) -> np.ndarray:
    h, w = barrier.shape
    mask = np.zeros((h + 2, w + 2), np.uint8)
    mask[1:-1, 1:-1][barrier] = 1
    img = rgb.copy()
    flags = 4 | cv2.FLOODFILL_MASK_ONLY | (255 << 8)
    for x, y in seeds:
        if 0 <= x < w and 0 <= y < h and not barrier[y, x]:
            cv2.floodFill(img, mask, (x, y), (255, 255, 255),
                          loDiff=(lo, lo, lo), upDiff=(lo, lo, lo), flags=flags)
    return mask[1:-1, 1:-1] == 255


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    ART.mkdir(parents=True, exist_ok=True)
    DBG.mkdir(parents=True, exist_ok=True)

    rgb = np.array(Image.open(SRC).convert("RGB"))
    h, w = rgb.shape[:2]
    rf = rgb[:, :, 0].astype(np.float32)
    gf = rgb[:, :, 1].astype(np.float32)
    bf = rgb[:, :, 2].astype(np.float32)
    r = rf.astype(np.int16)
    g = gf.astype(np.int16)
    b = bf.astype(np.int16)
    lum = 0.299 * rf + 0.587 * gf + 0.114 * bf
    sat = np.maximum(np.maximum(rf, gf), bf) - np.minimum(np.minimum(rf, gf), bf)
    rb = rf - bf
    lap = np.abs(ndimage.laplace(lum))
    yy, xx = np.mgrid[0:h, 0:w]
    nx = xx / float(w)
    ny = yy / float(h)

    hair_red = (r > 52) & (r > g + 18) & (r > b + 14) & ((r - g) > 16)
    hair_red &= ~((nx > 0.45) & (nx < 0.64) & (ny > 0.38) & (ny < 0.54))
    hair_red &= ~((ny > 0.84) & (nx > 0.44) & (nx < 0.66))

    skin = (r > 112) & (g > 68) & (b > 48) & (r > g + 8) & (r > b + 16) & (r < 242)
    mx = np.maximum(np.maximum(r, g), b)
    mn = np.minimum(np.minimum(r, g), b)
    metal = (mx > 120) & ((mx - mn) < 48) & (np.abs(r - g) < 24) & (ny > 0.36) & (nx < 0.52) & (ny < 0.93)

    cool_flat = (rb < 2.0) & (sat < 14) & (lap < 3.6)
    vignette = lum < 7
    spotlight = (ny < 0.22) & (nx > 0.28) & (nx < 0.58) & ~hair_red & ~skin & (rb > 8) & (gf > 14)

    face_keep = (nx > 0.50) & (nx < 0.76) & (ny > 0.12) & (ny < 0.34)
    jacket_keep = (nx > 0.39) & (nx < 0.66) & (ny > 0.30) & (ny < 0.58)
    legs_keep = (nx > 0.43) & (nx < 0.68) & (ny > 0.52) & (ny < 0.95)
    torso_keep = ndimage.binary_dilation(face_keep | jacket_keep | legs_keep, iterations=3)

    # Hair: grow red streaks into warm dark strands. Never into cool checker or gold spotlight.
    gold = (gf > 16) & (rb > 8) & ((rf - gf) < 28) & ~hair_red & (lap < 11)
    warm_strand = ((rb > 4) | hair_red) & (lum > 8) & (lum < 140) & ~skin
    warm_strand &= ~cool_flat
    warm_strand &= ~spotlight
    warm_strand &= ~gold
    hair = hair_red & ~torso_keep
    for _ in range(14):
        hair = ndimage.binary_dilation(hair, iterations=1) & warm_strand & ~torso_keep
    hair = ndimage.binary_closing(hair, iterations=2)
    hair = ndimage.binary_opening(hair, iterations=1)
    hair = biggest(hair, 500, 0.05)

    jacket_chunk = (nx > 0.42) & (ny > 0.28) & (ny < 0.58)
    hair_left = hair & (nx < 0.56) & ~jacket_chunk
    hair_left |= hair_red & (nx < 0.50) & ~torso_keep
    hair_left &= ~metal
    hair_left &= ~gold
    hair_left &= ~spotlight
    # Blade from the right hand down-left. Must not ride the hair plate.
    ax, ay, bx, by = 430.0, 620.0, 210.0, 1220.0
    abx, aby = bx - ax, by - ay
    ab2 = abx * abx + aby * aby
    apx, apy = xx - ax, yy - ay
    tline = np.clip((apx * abx + apy * aby) / ab2, 0.0, 1.0)
    dist = np.sqrt((apx - tline * abx) ** 2 + (apy - tline * aby) ** 2)
    sword = dist < 34
    sword = ndimage.binary_dilation(sword, iterations=3)
    hair_left &= ~sword
    gold_blob = (nx < 0.42) & (ny < 0.36) & (gf > 14) & (rb > 6) & ~hair_red
    gold_blob = ndimage.binary_dilation(gold_blob, iterations=2)
    hair_left &= ~gold_blob
    # Outer flowing mass only — roots stay on the painted body.
    hair_left &= (nx < 0.40) | ((nx < 0.48) & (ny < 0.28))
    hair_left = biggest(hair_left, 400, 0.08)
    # Right lock is cooler / more purple — allow a lower r-b floor.
    right_strand = ((nx > 0.62) & ((rb > 1.5) | hair_red) & (lum > 6) & (lum < 110) & ~cool_flat & ~skin)
    hair_right = (hair & (nx > 0.63)) | (hair_red & (nx > 0.64)) | right_strand
    hair_right &= ~torso_keep
    for _ in range(8):
        hair_right = ndimage.binary_dilation(hair_right, iterations=1) & right_strand
    hair_right = biggest(hair_right, 200, 0.08)
    hair_left = ndimage.binary_closing(hair_left, iterations=2)
    hair_right = ndimage.binary_closing(hair_right, iterations=2)

    body_band = (
        ((ny > 0.11) & (ny < 0.36) & (nx > 0.48) & (nx < 0.76))
        | ((ny > 0.28) & (ny < 0.60) & (nx > 0.38) & (nx < 0.70))
        | ((ny > 0.50) & (ny < 0.96) & (nx > 0.42) & (nx < 0.69))
        | ((ny > 0.38) & (ny < 0.93) & (nx > 0.14) & (nx < 0.48))
    )
    barrier = (~body_band) | vignette | cool_flat | spotlight
    body_seeds = [
        (630, 270), (650, 310), (600, 350),
        (500, 500), (620, 470), (560, 500), (480, 540),
        (560, 620), (540, 720), (580, 780),
        (550, 900), (540, 1050), (555, 1180),
        (510, 1310), (590, 1340),
        (430, 610), (360, 780), (300, 920), (250, 1080),
    ]
    body = flood_seeds(rgb, barrier, body_seeds, lo=14)
    body = body | skin | metal
    body &= ~cool_flat
    body &= ~spotlight
    body = ndimage.binary_closing(body, iterations=2)
    body = ndimage.binary_fill_holes(body)
    body = biggest(body, 800, 0.04)

    fg = body | hair_left | hair_right | hair_red | skin
    fg = ndimage.binary_closing(fg, iterations=2)
    fg &= ~vignette

    # Full painting stays as the body plate. Punch swinging hair and inpaint
    # so rotation reveals the original atmosphere instead of a hole or a ghost.
    hole = ndimage.binary_dilation(hair_left | hair_right, iterations=2)
    hole &= ~metal
    hole &= ~sword
    hole &= ~gold_blob
    hole_u8 = (hole.astype(np.uint8) * 255)
    inpainted = cv2.inpaint(rgb, hole_u8, 6, cv2.INPAINT_TELEA)
    blink_rgb = rgb
    if BLINK.is_file():
        blink_rgb = np.array(Image.open(BLINK).convert("RGB"))
        if blink_rgb.shape[:2] != (h, w):
            blink_rgb = np.array(Image.fromarray(blink_rgb).resize((w, h), Image.Resampling.BICUBIC))
        blink_rgb = cv2.inpaint(blink_rgb, hole_u8, 6, cv2.INPAINT_TELEA)

    opaque = np.ones((h, w), np.float32)
    back_a = feather(hair_left, 0.85)
    front_a = feather(hair_right, 0.85)
    save_rgba("layer_cutout.png", rgb, opaque)
    save_rgba("layer_body.png", inpainted, opaque)
    save_rgba("layer_hair_back.png", rgb, back_a)
    save_rgba("layer_hair_front.png", rgb, front_a)
    save_rgba("layer_body_blink.png", blink_rgb, opaque)

    def pivot_top(mask: np.ndarray, fb: list[float]) -> list[float]:
        ys, xs = np.where(mask)
        if len(xs) < 50:
            return fb
        top = np.percentile(ys, 6)
        band = ys <= top + 36
        if band.sum() < 16:
            band = ys <= np.percentile(ys, 15)
        return [round(float(xs[band].mean()) / w, 4), round(1.0 - float(ys[band].mean()) / h, 4)]

    piv = {
        "body": [0.50, 0.50],
        "hairBack": [0.547, 0.844],
        "hairFront": [0.685, 0.705],
        "size": [w, h],
    }
    (OUT / "pivots.json").write_text(json.dumps(piv, indent=2), encoding="utf-8")
    (ART / "pivots.json").write_text(json.dumps(piv, indent=2), encoding="utf-8")
    Image.fromarray((fg.astype(np.uint8) * 255)).save(DBG / "fg.png")
    Image.fromarray((body.astype(np.uint8) * 255)).save(DBG / "body.png")
    Image.fromarray((hair_left.astype(np.uint8) * 255)).save(DBG / "hair_back.png")
    Image.fromarray((hair_right.astype(np.uint8) * 255)).save(DBG / "hair_front.png")
    Image.fromarray((cool_flat.astype(np.uint8) * 255)).save(DBG / "cool.png")
    print("fine-split fg", int(fg.sum()), "body", int(body.sum()),
          "back", int(hair_left.sum()), "front", int(hair_right.sum()), "piv", piv)


if __name__ == "__main__":
    main()
