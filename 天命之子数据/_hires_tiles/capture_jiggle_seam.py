"""Click C001 chest and check for a black seam in the bust crop."""
from __future__ import annotations

import sys
import time
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(r"F:\天命之子\天命之子数据\_hires_tiles")))
import capture_ui as cu
import win32gui

OUT = Path(r"F:\天命之子\天命之子数据\_layout")
EXE = Path(r"F:\天命之子\client\Builds\Win64\Resonance.exe")


def launch() -> None:
    import subprocess
    subprocess.Popen([str(EXE)], cwd=str(EXE.parent))


def main() -> None:
    cu._dpi_aware()
    hwnd = cu.find_hwnd()
    if not hwnd:
        launch()
        hwnd = cu.wait_hwnd(30)
    if not hwnd:
        raise SystemExit("no window")
    OUT.mkdir(parents=True, exist_ok=True)
    cu.force_foreground(hwnd)
    time.sleep(2.4)
    cu.tap(hwnd, cu.TAB_HOME, 1.0)

    rest = cu.print_window(hwnd)
    rest.save(OUT / "seam_rest.png")
    l, t, r, b = win32gui.GetWindowRect(hwnd)
    cl, ct, cw, ch = cu.client_box(hwnd)
    img_x, img_y = 440, 490
    sx, sy = l + img_x, t + img_y
    nx = (sx - cl) / cw
    ny = 1.0 - (sy - ct) / ch
    print("click", round(nx, 4), round(ny, 4))
    cu.click_nx(hwnd, nx, ny)

    t0 = time.perf_counter()
    times = (0.08, 0.16, 0.28, 0.45, 0.80)
    frames = []
    for t in times:
        remain = t - (time.perf_counter() - t0)
        if remain > 0:
            time.sleep(remain)
        im = cu.print_window(hwnd)
        im.save(OUT / f"seam_t{t:.2f}.png")
        frames.append(im)
        print("shot", t, round(time.perf_counter() - t0, 3))

    box = (300, 400, 560, 640)
    rest_c = np.array(rest.crop(box).convert("RGB")).astype(int)
    worst = 0
    worst_name = ""
    for t, im in zip(times, frames):
        crop = np.array(im.crop(box).convert("RGB")).astype(int)
        peak_dark = crop.sum(axis=2) < 50
        rest_lit = rest_c.sum(axis=2) > 120
        crack = peak_dark & rest_lit
        n = int(crack.sum())
        print(f"t={t:.2f} new_dark_px={n}")
        if n > worst:
            worst = n
            worst_name = f"t{t:.2f}"
        Image.fromarray(im.crop(box).convert("RGB")).save(OUT / f"seam_crop_{t:.2f}.png")
    Image.fromarray(rest_c.astype(np.uint8)).save(OUT / "seam_crop_rest.png")
    print("worst", worst_name, worst)
    if worst > 80:
        print("SEAM")
        raise SystemExit(2)
    print("NO_SEAM")


if __name__ == "__main__":
    main()
