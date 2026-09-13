"""SPEC layers from the approved still. Same canvas, same pose, keep the face pixels."""
from __future__ import annotations

import json
from pathlib import Path

import cv2
import numpy as np
from PIL import Image

STILL = Path(r"F:\天命之子\art\characters\C001-焰刃\puppet-src\still.png")
MASKS = Path(r"F:\天命之子\client\Assets\Resources\Art\Characters\C001\CubismLayers")
OUT = Path(r"F:\天命之子\art\characters\C001-焰刃\puppet-src\layers")
PREV = OUT / "preview"

# Rest stack matches standee_front order (skip missing).
REST_ORDER = (
    "hair_back",
    "body",
    "head",
    "hair_side",
    "hair_front",
    "hand_r",
    "sword",
    "foot_l",
    "foot_r",
)


def load(p: Path) -> np.ndarray:
    return np.array(Image.open(p).convert("RGBA"))


def save(name: str, arr: np.ndarray) -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    Image.fromarray(arr).save(OUT / name)
    print(name, "opaque", int((arr[:, :, 3] > 8).sum()))


def over(d, s):
    a = s[:, :, 3:4].astype(np.float32) / 255.0
    return d * (1 - a) + s.astype(np.float32) * a


def ellipse(h, w, cx, cy, rx, ry):
    yy, xx = np.ogrid[:h, :w]
    return ((xx - cx) ** 2) / (rx ** 2) + ((yy - cy) ** 2) / (ry ** 2) <= 1


def box(h, w, x0, y0, x1, y1):
    m = np.zeros((h, w), bool)
    m[y0:y1, x0:x1] = True
    return m


def apply_mask(still: np.ndarray, mask: np.ndarray) -> np.ndarray:
    out = still.copy()
    out[~mask, 3] = 0
    return out


def keep_largest(mask: np.ndarray) -> np.ndarray:
    n, lab, stats, _ = cv2.connectedComponentsWithStats(mask.astype(np.uint8), 8)
    if n <= 1:
        return mask
    idx = 1 + int(np.argmax(stats[1:, cv2.CC_STAT_AREA]))
    return lab == idx


def drop_small(mask: np.ndarray, min_area: int = 80) -> np.ndarray:
    n, lab, stats, _ = cv2.connectedComponentsWithStats(mask.astype(np.uint8), 8)
    out = np.zeros_like(mask, dtype=bool)
    for i in range(1, n):
        if int(stats[i, cv2.CC_STAT_AREA]) >= min_area:
            out |= lab == i
    return out


def write_preview(name: str, arr: np.ndarray) -> None:
    """Premultiply onto dark gray so leftover RGB cannot fake opacity."""
    PREV.mkdir(parents=True, exist_ok=True)
    a = arr[:, :, 3:4].astype(np.float32) / 255.0
    rgb = arr[:, :, :3].astype(np.float32) * a + 32.0 * (1.0 - a)
    vis = np.clip(rgb, 0, 255).astype(np.uint8)
    vis_rgba = np.dstack([vis, np.full(vis.shape[:2], 255, np.uint8)])
    m = arr[:, :, 3] > 8
    if m.any():
        ys, xs = np.where(m)
        y0, y1 = max(0, int(ys.min()) - 8), min(arr.shape[0], int(ys.max()) + 9)
        x0, x1 = max(0, int(xs.min()) - 8), min(arr.shape[1], int(xs.max()) + 9)
        vis_rgba = vis_rgba[y0:y1, x0:x1]
    Image.fromarray(vis_rgba).save(PREV / name)


def main() -> None:
    still = load(STILL)
    h, w = still.shape[:2]
    k = np.ones((3, 3), np.uint8)
    a0 = still[:, :, 3] > 8
    rgb = still[:, :, :3].astype(np.int16)
    r, g, b = rgb[:, :, 0], rgb[:, :, 1], rgb[:, :, 2]
    yy, xx = np.ogrid[:h, :w]

    def plate_from_file(name: str, dilate: int = 0) -> tuple[np.ndarray, np.ndarray]:
        m = load(MASKS / name)[:, :, 3]
        if dilate > 0:
            m = cv2.dilate(m, k, iterations=dilate)
        out = still.copy()
        out[:, :, 3] = np.minimum(out[:, :, 3], m)
        return out, m > 8

    hair_back, m_hb = plate_from_file("hair_back.png", 2)
    hair_front, m_hf = plate_from_file("hair_front.png", 1)
    hair_side, m_hs = plate_from_file("hair_side.png", 2)
    sword, m_sw = plate_from_file("sword.png", 0)

    mint = a0 & (g > r + 8) & (g > 120) & (r < 190)
    white_cloth = a0 & (r > 190) & (g > 180) & (b > 170) & (np.abs(r - g) < 28)
    # Shirt + sleeve including shadows. Must stay on body even where hair overlaps.
    shirt = (
        a0
        & ~mint
        & (r > 80)
        & (g > 75)
        & (b > 65)
        & (np.abs(r - g) < 42)
        & (np.abs(g - b) < 48)
    )
    sleeve_zone = box(h, w, 330, 310, 530, 860) | box(h, w, 500, 340, 670, 700)
    shirt &= sleeve_zone
    dark = a0 & (r < 62) & (g < 58) & (b < 58)
    skin = a0 & (r > 145) & (g > 85) & (b > 65) & (r > g) & ((r - b) > 30)
    shirt &= ~skin
    boot_box = (yy > 1138) & (yy < 1505) & (xx > 400) & (xx < 655)
    # CubismLayers hair alphas leak the rapier and boots. Keep mint hair; drop metal.
    sword_box = box(h, w, 220, 770, 400, 1310)
    metal = (np.abs(r - g) < 40) & (np.abs(g - b) < 40) & (r > 70) & (r < 230)
    m_sw_d = cv2.dilate(m_sw.astype(np.uint8) * 255, k, iterations=6) > 128
    m_hb = keep_largest(m_hb & ~m_sw_d & ~boot_box & ~white_cloth & ~shirt & ~(sword_box & metal))
    m_hf = keep_largest(m_hf & ~m_sw_d & ~white_cloth & ~shirt)
    m_hs = keep_largest(m_hs & ~m_sw_d & ~white_cloth & ~shirt & ~skin & (yy > 500))
    hair_back = apply_mask(still, m_hb)
    hair_front = apply_mask(still, m_hf)
    hair_side = apply_mask(still, m_hs)

    # Head: face + neck + earrings. Stop above the chest / white wrap.
    head_m = ellipse(h, w, 528, 225, 98, 112)
    head_m &= a0 & (yy < 336) & (xx > 438) & (xx < 638)
    head_m &= (yy < 316) | ((xx >= 488) & (xx <= 572))
    head_m &= ~((yy > 312) & white_cloth)
    head_m &= ~((xx < 490) & mint)
    head_m = keep_largest(head_m)
    head_dil = cv2.dilate(head_m.astype(np.uint8) * 255, k, iterations=2) > 128
    # Hair lives on hair slots so the head can turn without a second set of bangs.
    head_dil &= ~(m_hb | m_hf | m_hs)
    head_dil = keep_largest(head_dil)
    head = apply_mask(still, head_dil)

    # Right hand: exposed skin at the cuff, not mint hair, not the sleeve.
    grip = box(h, w, 375, 685, 470, 830)
    skin_h = (
        a0
        & (r > 95)
        & (g > 45)
        & (b > 35)
        & (r > g)
        & ((r - b) > 18)
        & (g < r + 6)
    )
    hand_r_m = grip & skin_h & ~white_cloth & ~m_sw
    hand_r_m = cv2.morphologyEx(hand_r_m.astype(np.uint8) * 255, cv2.MORPH_CLOSE, k, iterations=2) > 128
    hand_r_dil = cv2.dilate(hand_r_m.astype(np.uint8) * 255, k, iterations=2) > 128
    hand_r_dil = keep_largest(hand_r_dil)
    hand_r = apply_mask(still, hand_r_dil)

    # Boots: Voronoi from shaft seeds so a heel cannot land on the other foot.
    dark_pants = (r < 70) & (g < 65) & (b < 65) & (yy < 1225)
    silver = a0 & (np.abs(r - g) < 40) & (np.abs(g - b) < 40) & (r > 55) & (r < 235) & (g > 55)
    sil_d = cv2.dilate((boot_box & silver).astype(np.uint8) * 255, k, iterations=6) > 128
    boot_like = boot_box & a0 & ~mint & ~dark_pants & (silver | sil_d)
    seed_r = (455, 1240)
    seed_l = (535, 1245)
    dist_r = (xx - seed_r[0]) ** 2 + (yy - seed_r[1]) ** 2
    dist_l = (xx - seed_l[0]) ** 2 + (yy - seed_l[1]) ** 2
    foot_r_m = drop_small(boot_like & (dist_r < dist_l), 40)
    foot_l_m = drop_small(boot_like & (dist_l <= dist_r), 40)
    foot_r_dil = cv2.dilate(foot_r_m.astype(np.uint8) * 255, k, iterations=2) > 128
    foot_l_dil = cv2.dilate(foot_l_m.astype(np.uint8) * 255, k, iterations=2) > 128
    both = foot_r_dil & foot_l_dil
    foot_r_dil &= ~both
    foot_l_dil &= ~both
    foot_r_dil &= boot_box & a0 & ~mint
    foot_l_dil &= boot_box & a0 & ~mint
    foot_r = apply_mask(still, foot_r_dil)
    foot_l = apply_mask(still, foot_l_dil)

    # Moving slots own their pixels. Never punch the chest / white wrap.
    hair_u = m_hb | m_hf | m_hs
    moving = np.zeros((h, w), np.uint8)
    for m in (hair_u, m_sw, head_dil, hand_r_dil, foot_r_dil, foot_l_dil):
        moving = np.maximum(moving, (m.astype(np.uint8) * 255))
    moving[shirt | white_cloth] = 0
    punch = cv2.erode(moving, k, iterations=1)
    body = still.copy()
    body[punch > 16, 3] = 0

    plates = {
        "hair_back": hair_back,
        "hair_front": hair_front,
        "hair_side": hair_side,
        "head": head,
        "body": body,
        "hand_r": hand_r,
        "foot_r": foot_r,
        "foot_l": foot_l,
        "sword": sword,
    }
    for name, arr in plates.items():
        save(f"{name}.png", arr)
        write_preview(f"{name}.png", arr)

    leftover = OUT / "mouth_0.png"
    if leftover.exists():
        leftover.unlink()
        print("removed", leftover)
    old_mouth_prev = PREV / "mouth_0.png"
    if old_mouth_prev.exists():
        old_mouth_prev.unlink()

    rest = np.zeros_like(still, np.float32)
    for name in REST_ORDER:
        rest = over(rest, plates[name])
    rest_u = np.clip(rest, 0, 255).astype(np.uint8)
    save("composite_rest.png", rest_u)
    write_preview("composite_rest.png", rest_u)
    diff = np.abs(rest_u[:, :, :3].astype(int) - still[:, :, :3].astype(int)).mean()
    print("composite vs still mean rgb", round(float(diff), 2))

    marks = {
        "head": [0.508, 0.779],
        "chest": [0.503, 0.703],
        "hand_r": [0.425, 0.547],
        "hand_l": [0.590, 0.620],
        "foot_r": [0.459, 0.202],
        "foot_l": [0.542, 0.228],
    }
    (OUT / "landmarks.json").write_text(json.dumps(marks, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
