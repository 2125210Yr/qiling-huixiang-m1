"""Paste close-up redraws onto a 3x canvas with template-match alignment."""
from __future__ import annotations

import json
from pathlib import Path

import cv2
import numpy as np
from PIL import Image, ImageFilter

ROOT = Path(__file__).resolve().parent
SCALE = 3
CANVAS = (720 * SCALE, 1280 * SCALE)  # 2160 x 3840
FEATHER = 56


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
    """Search around expected box; return (x, y, tw, th, score)."""
    bh, bw = base.shape[:2]
    el, et, er, eb = expect
    ew, eh = max(8, er - el), max(8, eb - et)
    # resize tile to expected size first
    resized = cv2.resize(tile, (ew, eh), interpolation=cv2.INTER_LANCZOS4)
    gray_b = cv2.cvtColor(base, cv2.COLOR_BGR2GRAY)
    gray_t = cv2.cvtColor(resized, cv2.COLOR_BGR2GRAY)
    # search window
    pad = 48
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
        out[:, :, c] = (t[:, :, c] - tm) * (bs / ts) * 0.55 + t[:, :, c] * 0.45
        out[:, :, c] = out[:, :, c] - out[:, :, c].mean() + bm * 0.55 + tm * 0.45
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
    src = load_bgr(ROOT / "src_39.jpg")
    base = cv2.resize(src, CANVAS, interpolation=cv2.INTER_LANCZOS4)

    jobs = [
        ("bust", ROOT / "bust_hires.jpg", 0.55),
        ("face", ROOT / "face_hires.jpg", 0.62),
    ]
    report = []
    for name, path, min_score in jobs:
        box = meta[name]["box"]
        expect = (box[0] * SCALE, box[1] * SCALE, box[2] * SCALE, box[3] * SCALE)
        tile = load_bgr(path)
        x, y, tw, th, score, resized = match_place(base, tile, expect)
        print(f"{name}: score={score:.3f} at {x},{y} size {tw}x{th}")
        if score < min_score:
            print(f"  skip {name} (low match)")
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
    rgb = rgb.filter(ImageFilter.UnsharpMask(radius=1.6, percent=85, threshold=2))
    out = ROOT / "preview_composite.png"
    rgb.save(out, "PNG")
    # chest crop for visual check (same region as user zoom)
    w, h = rgb.size
    chest = rgb.crop((int(w * 0.28), int(h * 0.22), int(w * 0.74), int(h * 0.48)))
    chest.save(ROOT / "check_chest.png")
    face = rgb.crop((int(w * 0.32), int(h * 0.06), int(w * 0.72), int(h * 0.32)))
    face.save(ROOT / "check_face.png")
    print("wrote", out, rgb.size, "report", report)


if __name__ == "__main__":
    main()
