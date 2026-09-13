"""Capture Home bust and check sleeve/belt stay put vs both globes swaying together."""
from __future__ import annotations

import sys
import time
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(r"F:\天命之子\天命之子数据\_hires_tiles")))
from capture_ui import TAB_HOME, _dpi_aware, force_foreground, print_window, tap, wait_hwnd

OUT = Path(r"F:\天命之子\天命之子数据\_layout")


def search(a: np.ndarray, b: np.ndarray, maxo: int = 16) -> tuple[float, int, int]:
    a = a.astype(np.float32)
    b = b.astype(np.float32)
    best = None
    m = max(4, maxo // 2)
    for oy in range(-8, 9):
        for ox in range(-maxo, maxo + 1):
            bb = np.roll(np.roll(b, oy, 0), ox, 1)
            err = float(np.mean(np.abs(a[m:-m, m:-m] - bb[m:-m, m:-m])))
            if best is None or err < best[0]:
                best = (err, ox, oy)
    return best


def crop(img: np.ndarray, box: tuple[int, int, int, int]) -> np.ndarray:
    x0, y0, x1, y1 = box
    return img[y0:y1, x0:x1]


def main() -> None:
    _dpi_aware()
    hwnd = wait_hwnd(40)
    if not hwnd:
        raise SystemExit("no window")
    force_foreground(hwnd)
    time.sleep(2.0)
    tap(hwnd, TAB_HOME, 1.0)
    a = np.array(print_window(hwnd).convert("RGB"))
    Image.fromarray(a).save(OUT / "bust6_rest.png")
    time.sleep(1.37)
    b = np.array(print_window(hwnd).convert("RGB"))
    Image.fromarray(b).save(OUT / "bust6_peak.png")
    time.sleep(1.37)
    c = np.array(print_window(hwnd).convert("RGB"))
    Image.fromarray(c).save(OUT / "bust6_trough.png")
    print("shots", a.shape)

    # Home window ~772x1383 including chrome. Bust from previous session: x0=240,y0=420, 280x220
    bust = (240, 420, 520, 640)
    rest, peak, trough = crop(a, bust), crop(b, bust), crop(c, bust)
    Image.fromarray(np.concatenate([rest, peak, trough], axis=1)).save(OUT / "bust6_strip.png")
    h, w = rest.shape[:2]
    boxes = {
        "L": (70, 35, 145, 130),
        "R": (145, 20, 255, 140),
        "belt": (80, 155, 210, 210),
        "sleeve": (0, 20, 70, 140),
    }
    for name, box in boxes.items():
        rp = search(crop(rest, box), crop(peak, box))
        rt = search(crop(rest, box), crop(trough, box))
        pt = search(crop(peak, box), crop(trough, box), 20)
        print(name, "rest-peak", rp, "rest-trough", rt, "peak-trough", pt)

    rg = np.zeros_like(rest)
    rg[:, :, 0] = trough[:, :, 0]
    rg[:, :, 1] = rest[:, :, 1]
    rg[:, :, 2] = rest[:, :, 2]
    Image.fromarray(rg).save(OUT / "bust6_game_rg.png")
    print("wrote overlays")


if __name__ == "__main__":
    main()
