"""Cubism plates: continuous hair (soft overlap, no sword hole), body, sword, bust overlay."""
from __future__ import annotations

from pathlib import Path

import cv2
import numpy as np
from PIL import Image

SRC = Path(r"F:\天命之子\天命之子数据\_layout\ice_src.png")
OUT = Path(r"F:\天命之子\art\characters\C001-焰刃\cubism-import")
VIZ = Path(r"F:\天命之子\天命之子数据\_layout")


def save_plate(name: str, rgba: np.ndarray, mask: np.ndarray) -> None:
    m = (mask > 0).astype(np.uint8)
    out = rgba.copy()
    out[:, :, 3] = np.where(m > 0, rgba[:, :, 3], 0)
    Image.fromarray(out, "RGBA").save(OUT / f"{name}.png")
    vis = np.full(rgba.shape[:2] + (3,), (255, 0, 255), np.uint8)
    a = out[:, :, 3:4].astype(np.float32) / 255.0
    vis = (out[:, :, :3].astype(np.float32) * a + vis.astype(np.float32) * (1.0 - a)).astype(np.uint8)
    thumb = Image.fromarray(vis)
    thumb.thumbnail((400, 600))
    thumb.save(VIZ / f"viz4_{name}.jpg", quality=90)
    print(f"{name:12} px={int(m.sum()):7d} {100 * m.mean():.2f}%")


def dist_to_segment(x, y, x1, y1, x2, y2):
    px, py = x2 - x1, y2 - y1
    n = px * px + py * py + 1e-6
    u = np.clip(((x - x1) * px + (y - y1) * py) / n, 0, 1)
    return np.sqrt((x - (x1 + u * px)) ** 2 + (y - (y1 + u * py)) ** 2)


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    rgba = np.array(Image.open(SRC).convert("RGBA"))
    orig_rgba = rgba.copy()
    rgb = rgba[:, :, :3]
    alpha = rgba[:, :, 3]
    char = alpha > 16
    hsv = cv2.cvtColor(rgb, cv2.COLOR_RGB2HSV)
    h, s, v = hsv[:, :, 0], hsv[:, :, 1], hsv[:, :, 2]
    yy, xx = np.indices(alpha.shape)

    k3 = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (3, 3))
    k5 = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (5, 5))
    k9 = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (9, 9))
    k15 = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (15, 15))

    face = np.zeros(alpha.shape, np.uint8)
    cv2.ellipse(face, (520, 305), (78, 98), 0, 0, 360, 255, -1)
    face = face.astype(bool)

    torso = np.zeros(alpha.shape, np.uint8)
    cv2.ellipse(torso, (515, 640), (120, 300), 8, 0, 360, 255, -1)
    torso = torso.astype(bool) & char & ~face

    mint = char & (h >= 68) & (h <= 102) & (s >= 18) & (v >= 60)
    plat = char & (v >= 150) & (s >= 6) & (s <= 95) & (h <= 45) & ~torso
    hair = (mint | plat) & ~face
    hair = cv2.morphologyEx(hair.astype(np.uint8), cv2.MORPH_CLOSE, k9).astype(bool)
    hair = cv2.dilate(hair.astype(np.uint8), k3, iterations=1).astype(bool)
    hair = hair & char & ~face

    # Real blade is more vertical: hilt ~ (385,710) → tip ~ (255,1270).
    corridor = dist_to_segment(xx, yy, 385, 710, 255, 1270) < 13
    sword = char & corridor & (yy > 690) & (xx < 420) & ~face
    sword = cv2.morphologyEx(sword.astype(np.uint8), cv2.MORPH_CLOSE, k3).astype(bool)

    # Natural hair pieces by connected components + soft overlap band.
    bangs = hair & (xx >= 410) & (xx <= 630) & (yy < 395) & ~torso
    rest = hair & ~bangs
    nlab, lab = cv2.connectedComponents(rest.astype(np.uint8))
    hair_back = np.zeros_like(rest)
    hair_side = np.zeros_like(rest)
    for i in range(1, nlab):
        comp = lab == i
        xs = xx[comp]
        if xs.size == 0:
            continue
        cx = float(xs.mean())
        # ponytail lives on the left / over the blade; side lock on the right
        if cx >= 610:
            hair_side[comp] = True
        else:
            hair_back[comp] = True
    hair_back = hair_back.astype(bool)
    hair_side = hair_side.astype(bool)

    # Soft overlap so the split does not read as a knife cut.
    overlap = cv2.dilate(hair_back.astype(np.uint8), k15, 1).astype(bool) & hair_side
    hair_back = hair_back | overlap
    hair_side = hair_side | overlap

    # Keep bangs out of the big locks
    hair_back = hair_back & ~bangs
    hair_side = hair_side & ~bangs
    hair_front = bangs
    # Keep mint around the blade. Only peel the thin icy core onto the sword layer.
    hair_back = hair_back | ((hair & (xx < 520) & ~bangs))
    hair_back = hair_back & ~sword
    rgba = orig_rgba.copy()

    body = (char & ~hair & ~sword) | (char & face)
    body = cv2.morphologyEx(body.astype(np.uint8), cv2.MORPH_CLOSE, k3).astype(bool)
    body = body | (char & face)

    # Bust overlay: duplicate chest so a small warp can bounce without caging the whole body.
    bust = np.zeros(alpha.shape, np.uint8)
    cv2.ellipse(bust, (508, 455), (95, 85), 6, 0, 360, 255, -1)
    bust = bust.astype(bool) & body & (yy < 560) & (yy > 340)

    save_plate("hair_back", rgba, hair_back)
    save_plate("hair_front", orig_rgba, hair_front)
    save_plate("hair_side", orig_rgba, hair_side)
    save_plate("body", orig_rgba, body)
    save_plate("bust", orig_rgba, bust)
    save_plate("sword", orig_rgba, sword)

    order = ["hair_back", "body", "bust", "hair_side", "hair_front", "sword"]
    comp = np.zeros_like(rgba)
    for name in order:
        layer = np.array(Image.open(OUT / f"{name}.png"))
        a = layer[:, :, 3:4].astype(np.float32) / 255.0
        rgb_c = layer[:, :, :3].astype(np.float32) * a + comp[:, :, :3].astype(np.float32) * (1 - a)
        aa = np.maximum(comp[:, :, 3:4], layer[:, :, 3:4])
        comp = np.concatenate([rgb_c, aa], axis=2).astype(np.uint8)
    Image.fromarray(comp, "RGBA").save(VIZ / "split_composite.png")
    thumb = Image.fromarray(comp)
    thumb.thumbnail((400, 600))
    bg = Image.new("RGB", thumb.size, (255, 0, 255))
    bg.paste(thumb, mask=thumb.split()[-1])
    bg.save(VIZ / "viz4_composite.jpg", quality=92)
    hole = char & (comp[:, :, 3] == 0)
    print("missing character px", int(hole.sum()))


if __name__ == "__main__":
    main()
