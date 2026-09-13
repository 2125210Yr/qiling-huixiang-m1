"""ESRGAN 4x base + close-up redraws, template-matched."""
from __future__ import annotations

import json
from pathlib import Path

import cv2
import numpy as np
from PIL import Image, ImageFilter

ROOT = Path(__file__).resolve().parent
SCALE = 4
CANVAS = (720 * SCALE, 1280 * SCALE)  # 2880 x 5120
FEATHER = 72


def load_bgr(path: Path) -> np.ndarray:
    im = Image.open(path).convert("RGB")
    return cv2.cvtColor(np.array(im), cv2.COLOR_RGB2BGR)


def to_pil(bgr: np.ndarray) -> Image.Image:
    return Image.fromarray(cv2.cvtColor(bgr, cv2.COLOR_BGR2RGB))


def feather_mask(h: int, w: int, feather: int) -> np.ndarray:
    mask = np.ones((h, w), np.float32)
    f = max(8, min(feather, h // 4, w // 4))
    for i in range(f):
        t = (i + 1) / f
        mask[i, :] *= t
        mask[h - 1 - i, :] *= t
        mask[:, i] *= t
        mask[:, w - 1 - i] *= t
    return mask


def match_place(base: np.ndarray, tile: np.ndarray, expect: tuple[int, int, int, int]):
    bh, bw = base.shape[:2]
    el, et, er, eb = expect
    ew, eh = max(8, er - el), max(8, eb - et)
    resized = cv2.resize(tile, (ew, eh), interpolation=cv2.INTER_LANCZOS4)
    gray_b = cv2.cvtColor(base, cv2.COLOR_BGR2GRAY)
    gray_t = cv2.cvtColor(resized, cv2.COLOR_BGR2GRAY)
    pad = 64
    sl = max(0, el - pad)
    st = max(0, et - pad)
    sr = min(bw, er + pad)
    sb = min(bh, eb + pad)
    region = gray_b[st:sb, sl:sr]
    if region.shape[0] < eh or region.shape[1] < ew:
        return el, et, ew, eh, 0.0, resized
    res = cv2.matchTemplate(region, gray_t, cv2.TM_CCOEFF_NORMED)
    _, maxv, _, maxloc = cv2.minMaxLoc(res)
    x = sl + maxloc[0]
    y = st + maxloc[1]
    return x, y, ew, eh, float(maxv), resized


def color_match(tile: np.ndarray, base_roi: np.ndarray) -> np.ndarray:
    t = tile.astype(np.float32)
    b = base_roi.astype(np.float32)
    out = t.copy()
    for c in range(3):
        tm, ts = t[:, :, c].mean(), t[:, :, c].std() + 1e-5
        bm, bs = b[:, :, c].mean(), b[:, :, c].std() + 1e-5
        out[:, :, c] = (t[:, :, c] - tm) * (bs / ts) * 0.40 + t[:, :, c] * 0.60
        out[:, :, c] = out[:, :, c] - out[:, :, c].mean() + bm * 0.40 + tm * 0.60
    return np.clip(out, 0, 255).astype(np.uint8)


def blend(base: np.ndarray, tile: np.ndarray, x: int, y: int, mask: np.ndarray) -> None:
    h, w = tile.shape[:2]
    bh, bw = base.shape[:2]
    x0, y0 = max(0, x), max(0, y)
    x1, y1 = min(bw, x + w), min(bh, y + h)
    tx0, ty0 = x0 - x, y0 - y
    tx1, ty1 = tx0 + (x1 - x0), ty0 + (y1 - y0)
    roi = base[y0:y1, x0:x1].astype(np.float32)
    src = tile[ty0:ty1, tx0:tx1].astype(np.float32)
    m = mask[ty0:ty1, tx0:tx1][:, :, None]
    base[y0:y1, x0:x1] = np.clip(src * m + roi * (1.0 - m), 0, 255).astype(np.uint8)


def main() -> None:
    meta = json.loads((ROOT / "meta.json").read_text(encoding="utf-8"))
    base = load_bgr(ROOT / "esrgan_x4.png")
    if base.shape[1] != CANVAS[0] or base.shape[0] != CANVAS[1]:
        base = cv2.resize(base, CANVAS, interpolation=cv2.INTER_LANCZOS4)

    # Hair/body first, face+bust last so the approved close-ups win.
    jobs = [
        ("hair_left_u", ROOT / "hair_left_u_hires.jpg", 0.72),
        ("hair_right_u", ROOT / "hair_right_u_hires.jpg", 0.72),
        ("hair_right_l", ROOT / "hair_right_l_hires.jpg", 0.72),
        ("hair_top", ROOT / "hair_top_hires.jpg", 0.70),
        ("mid", ROOT / "mid_hires.jpg", 0.72),
        ("hips", ROOT / "hips_hires.jpg", 0.72),
        ("sword", ROOT / "sword_hires.jpg", 0.70),
        ("bust", ROOT / "bust_hires.jpg", 0.62),
        ("face", ROOT / "face_hires.jpg", 0.62),
    ]
    report = []
    for name, path, min_score in jobs:
        if not path.exists() or name not in meta:
            report.append((name, 0.0, "missing"))
            continue
        box = meta[name]["box"]
        expect = (box[0] * SCALE, box[1] * SCALE, box[2] * SCALE, box[3] * SCALE)
        tile = load_bgr(path)
        x, y, tw, th, score, resized = match_place(base, tile, expect)
        print(f"{name}: score={score:.3f} at {x},{y} size {tw}x{th}")
        if score < min_score:
            print(f"  skip {name}")
            report.append((name, score, "skip"))
            continue
        roi = base[y : y + th, x : x + tw]
        if roi.shape[0] != th or roi.shape[1] != tw:
            report.append((name, score, "clip-skip"))
            continue
        matched = color_match(resized, roi)
        mask = feather_mask(th, tw, FEATHER)
        blend(base, matched, x, y, mask)
        report.append((name, score, "ok"))

    rgb = to_pil(base)
    rgb = rgb.filter(ImageFilter.UnsharpMask(radius=1.4, percent=60, threshold=3))
    out = ROOT / "preview_full.png"
    rgb.save(out, "PNG")
    w, h = rgb.size
    checks = {
        "chest": (0.28, 0.20, 0.74, 0.48),
        "hair": (0.00, 0.08, 0.42, 0.48),
        "sword": (0.08, 0.52, 0.46, 0.92),
        "legs": (0.32, 0.52, 0.70, 0.86),
        "boots": (0.34, 0.74, 0.66, 0.98),
        "face": (0.32, 0.04, 0.72, 0.30),
    }
    for n, b in checks.items():
        rgb.crop((int(w * b[0]), int(h * b[1]), int(w * b[2]), int(h * b[3]))).save(
            ROOT / f"full_check_{n}.png"
        )
    print("wrote", out, rgb.size)
    for row in report:
        print(" ", row)


if __name__ == "__main__":
    main()
