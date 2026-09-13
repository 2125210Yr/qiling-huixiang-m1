"""Place regenerated whole locks on the 2880x5120 still. Punch flowing hair from body."""
from __future__ import annotations

import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter

SRC = Path(r"F:\天命之子\client\Assets\Resources\Art\Characters\C001\preview_new.png")
LAY = Path(r"F:\天命之子\client\Assets\Resources\Art\Characters\C001\SpineLayers")
GEN = Path(r"F:\天命之子\天命之子数据\_hires_tiles\layer_src")
PREV = Path(r"F:\天命之子\天命之子数据\_hires_tiles")


def key_black(im: Image.Image, thr: int = 26) -> Image.Image:
    im = im.convert("RGBA")
    a = np.array(im)
    rgb = a[..., :3].astype(np.int16)
    lum = 0.3 * rgb[..., 0] + 0.59 * rgb[..., 1] + 0.11 * rgb[..., 2]
    mx = rgb.max(axis=2)
    chroma = mx - rgb.min(axis=2)
    mask = (lum < thr) & (mx < thr + 16) & (chroma < 18)
    a[..., 3] = np.where(mask, 0, 255)
    # zero rgb of transparent (no fringe)
    a[..., :3] = np.where(a[..., 3:4] == 0, 0, a[..., :3])
    return Image.fromarray(a, "RGBA")


def opaque_bbox(im: Image.Image, thr: int = 24):
    a = np.array(im)[..., 3]
    ys, xs = np.where(a > thr)
    if len(xs) == 0:
        return None
    return int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1


def root_of(im: Image.Image, kind: str):
    """kind: 'clip' = top of lock; 'hip' = top-left of lock."""
    a = np.array(im)[..., 3]
    bb = opaque_bbox(im)
    if bb is None:
        return None
    l, t, r, b = bb
    h = max(1, b - t)
    if kind == "clip":
        band = t + max(8, int(h * 0.10))
        ys, xs = np.where((a > 40) & (np.arange(a.shape[0])[:, None] <= band) & (np.arange(a.shape[1])[None, :] >= l))
        if len(xs) == 0:
            return (l + r) // 2, t
        # clip sits on the right of the left-ponytail mass
        return int(np.percentile(xs, 75)), int(np.percentile(ys, 20))
    # hip: top-left of the lock
    band = t + max(8, int(h * 0.18))
    ys, xs = np.where((a > 40) & (np.arange(a.shape[0])[:, None] <= band) & (np.arange(a.shape[1])[None, :] <= l + (r - l) * 0.45))
    if len(xs) == 0:
        return l, t
    return int(np.percentile(xs, 20)), int(np.percentile(ys, 20))


def fade_root(im: Image.Image, kind: str, frac: float = 0.08) -> Image.Image:
    """Soften the attachment so the lock tucks under scalp / hip."""
    a = np.array(im)
    bb = opaque_bbox(im)
    if bb is None:
        return im
    l, t, r, b = bb
    h = max(1, b - t)
    fade_h = max(12, int(h * frac))
    yy = np.arange(a.shape[0])[:, None]
    w = np.clip((yy - t) / fade_h, 0.0, 1.0)
    if kind == "clip":
        # also fade the inner (right) edge into the torso so there is no knife seam
        fade_w = max(24, int((r - l) * 0.14))
        xx = np.arange(a.shape[1])[None, :]
        wr = np.clip((r - xx) / fade_w, 0.0, 1.0)
        w = np.minimum(w, wr)
    a[..., 3] = (a[..., 3].astype(np.float32) * w).astype(np.uint8)
    return Image.fromarray(a, "RGBA")


def place_root(canvas: Image.Image, part: Image.Image, src_root, dst_root, scale: float):
    pw, ph = part.size
    nw, nh = max(1, int(pw * scale)), max(1, int(ph * scale))
    part = part.resize((nw, nh), Image.Resampling.LANCZOS)
    sx, sy = int(src_root[0] * scale), int(src_root[1] * scale)
    x = int(dst_root[0] - sx)
    y = int(dst_root[1] - sy)
    tmp = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    tmp.alpha_composite(part, (x, y))
    canvas.alpha_composite(tmp)
    return x, y, nw, nh


def dilate(mask: np.ndarray, px: int) -> np.ndarray:
    im = Image.fromarray((mask.astype(np.uint8) * 255), "L")
    im = im.filter(ImageFilter.MaxFilter(px * 2 + 1))
    return np.array(im) > 40


def erode(mask: np.ndarray, px: int) -> np.ndarray:
    im = Image.fromarray((mask.astype(np.uint8) * 255), "L")
    im = im.filter(ImageFilter.MinFilter(px * 2 + 1))
    return np.array(im) > 40


def close_mask(mask: np.ndarray, px: int) -> np.ndarray:
    return erode(dilate(mask, px), px)


def extract_original_lock(still: Image.Image, region: str) -> Image.Image:
    """One closed lock from the approved still. No strand-level split."""
    a = np.array(still)
    H, W = a.shape[:2]
    rgb = a[..., :3].astype(np.float32) / 255.0
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    mx = np.maximum(np.maximum(r, g), b)
    mn = np.minimum(np.minimum(r, g), b)
    ss = (mx - mn) / (mx + 1e-6)
    alpha = a[..., 3] > 18
    yy, xx = np.mgrid[0:H, 0:W]
    xn, yn = xx / (W - 1.0), yy / (H - 1.0)
    mint = alpha & (g > r + 0.04) & (g > 0.35) & (b > 0.30)
    plat = alpha & (ss < 0.22) & (mx > 0.55)
    hair = mint | plat
    core = (xn > 0.40) & (xn < 0.76) & (yn > 0.07) & (yn < 0.36)
    if region == "right":
        lock = hair & (xn > 0.52) & (yn > 0.22) & (yn < 0.80) & ~core
    else:
        lock = hair & (xn < 0.46) & (yn > 0.14) & (yn < 0.80) & ~core
    lock = dilate(lock, 2)
    out = np.zeros_like(a)
    out[..., :3] = np.where(lock[..., None], a[..., :3], 0)
    out[..., 3] = np.where(lock, a[..., 3], 0)
    return Image.fromarray(out, "RGBA")


def extract_sword(still: Image.Image) -> Image.Image:
    a = np.array(still)
    H, W = a.shape[:2]
    rgb = a[..., :3].astype(np.float32) / 255.0
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    mx = np.maximum(np.maximum(r, g), b)
    mn = np.minimum(np.minimum(r, g), b)
    ss = (mx - mn) / (mx + 1e-6)
    alpha = a[..., 3] > 18
    yy, xx = np.mgrid[0:H, 0:W]
    xn, yn = xx / (W - 1.0), yy / (H - 1.0)
    mint = alpha & (g > r + 0.04) & (g > 0.35) & (b > 0.30)
    skin = alpha & (r > g + 0.04) & (r > 0.40) & (g > 0.22) & (b < 0.55)
    sword = alpha & (xn > 0.12) & (xn < 0.36) & (yn > 0.48) & (yn < 0.85)
    sword = sword & (mx > 0.38) & (ss < 0.38) & ~mint & ~skin
    sword = sword & ~((xn > 0.30) & (yn > 0.74))
    sword = dilate(sword, 2)
    sword = erode(sword, 1)
    out = np.zeros_like(a)
    out[..., :3] = np.where(sword[..., None], a[..., :3], 0)
    out[..., 3] = np.where(sword, 255, 0).astype(np.uint8)
    return Image.fromarray(out, "RGBA")


def main():
    still = Image.open(SRC).convert("RGBA")
    W, H = still.size
    hair_back = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    hair_front = Image.new("RGBA", (W, H), (0, 0, 0, 0))

    left = fade_root(key_black(Image.open(GEN / "hair_left_whole_x4.png"), thr=24), "clip", 0.08)
    # Right lock stays the approved still: one piece, already in place. Regen plate had a knife edge.
    hair_front = extract_original_lock(still, "right")
    sword = Image.new("RGBA", (W, H), (0, 0, 0, 0))

    lroot = root_of(left, "clip")
    print("left root", lroot, "bbox", opaque_bbox(left))

    lbb = opaque_bbox(left)
    l_h = lbb[3] - lbb[1]
    place_root(hair_back, left, lroot, (1420, 540), 3380.0 / l_h)

    body = np.array(still)
    rgb = body[..., :3].astype(np.float32) / 255.0
    r, g, bch = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    mx = np.maximum(np.maximum(r, g), bch)
    mn = np.minimum(np.minimum(r, g), bch)
    ss = (mx - mn) / (mx + 1e-6)
    yy, xx = np.mgrid[0:H, 0:W]
    xn, yn = xx / (W - 1.0), yy / (H - 1.0)
    alpha = body[..., 3] > 18
    mint = alpha & (g > r + 0.04) & (g > 0.35) & (bch > 0.30)
    plat = alpha & (ss < 0.22) & (mx > 0.55)
    hair_col = mint | plat
    head = (xn > 0.44) & (xn < 0.76) & (yn > 0.07) & (yn < 0.28)
    hand = (xn > 0.30) & (xn < 0.42) & (yn > 0.40) & (yn < 0.50)
    shirt = (xn > 0.40) & (xn < 0.72) & (yn > 0.20) & (yn < 0.48) & (mx > 0.70) & (ss < 0.14) & (g <= r + 0.02)
    pants = (xn > 0.42) & (xn < 0.70) & (yn > 0.44) & (yn < 0.86) & (mx < 0.32)
    boots = (xn > 0.44) & (xn < 0.68) & (yn > 0.74)
    skin = (r > g + 0.04) & (r > 0.45) & (g > 0.26) & (bch < 0.55) & (ss > 0.12)
    protect = head | hand | shirt | pants | boots | skin
    hb = np.array(hair_back)[..., 3] > 18
    hf = np.array(hair_front)[..., 3] > 18
    blade = (xn > 0.10) & (xn < 0.40) & (yn > 0.44) & (yn < 0.88)
    near_torso = (xn > 0.36) & (xn < 0.50)
    punch = (dilate(hb, 8) | dilate(hf, 2)) & ~protect & ~blade & ~near_torso
    # hard punch — leftover still-hair is the Cubism split
    body[..., 3] = np.where(punch, 0, body[..., 3])
    body[..., :3] = np.where(punch[..., None], 0, body[..., :3])
    body_im = Image.fromarray(body, "RGBA")

    LAY.mkdir(parents=True, exist_ok=True)
    body_im.save(LAY / "layer_body.png")
    hair_back.save(LAY / "layer_hair_back.png")
    hair_front.save(LAY / "layer_hair_front.png")
    Image.new("RGBA", (W, H), (0, 0, 0, 0)).save(LAY / "layer_hair_tip.png")
    sword.save(LAY / "layer_sword.png")

    # Unity pivot y is from bottom
    piv = {
        "hairBack": [round(1420 / W, 4), round(1.0 - 540 / H, 4)],
        "hairFront": [0.66, 0.61],
        "hairTip": [0.40, 0.70],
        "sword": [0.30, 0.50],
    }
    (LAY / "pivots.json").write_text(json.dumps(piv, indent=2), encoding="utf-8")

    # rest-pose preview on black
    preview = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    preview.alpha_composite(hair_back)
    preview.alpha_composite(body_im)
    preview.alpha_composite(sword)
    preview.alpha_composite(hair_front)
    preview.save(PREV / "spine_rest_preview.png")

    print("wrote", LAY)
    print("pivots", piv)
    for n in ["layer_body.png", "layer_hair_back.png", "layer_hair_front.png", "layer_sword.png"]:
        im = Image.open(LAY / n)
        a = np.array(im)[..., 3]
        print(n, im.size, "opaque", int((a > 10).sum()), "bbox", opaque_bbox(im))


if __name__ == "__main__":
    main()
