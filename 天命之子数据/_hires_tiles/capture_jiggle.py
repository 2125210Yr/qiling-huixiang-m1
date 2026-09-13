"""Launch Home, click C001 chest, burst-capture the jiggle."""
from __future__ import annotations

import sys
import time
from pathlib import Path

sys.path.insert(0, str(Path(r"F:\天命之子\天命之子数据\_hires_tiles")))
import capture_ui as cu
from PIL import Image

OUT = Path(r"F:\天命之子\天命之子数据\_layout")
EXE = Path(r"F:\天命之子\client\Builds\Win64\Resonance.exe")


def launch() -> None:
    import subprocess
    subprocess.Popen([str(EXE)], cwd=str(EXE.parent))


def chest_click_frac(img: Image.Image) -> tuple[float, float]:
    """Chest in client-normalized coords (x from left, y from bottom)."""
    w, h = img.size
    # Title bar sits in PrintWindow; use client box mapping instead.
    # Approximate: character 2:3 fitted, chest ~u=0.50 v=0.70 of the plate.
    return 0.50, 0.61


def main() -> None:
    cu._dpi_aware()
    hwnd = cu.find_hwnd()
    if not hwnd:
        launch()
        hwnd = cu.wait_hwnd(25)
    if not hwnd:
        raise SystemExit("no 契灵回响 window")
    OUT.mkdir(parents=True, exist_ok=True)
    cu.force_foreground(hwnd)
    time.sleep(2.2)
    cu.tap(hwnd, cu.TAB_HOME, 0.9)
    rest = cu.print_window(hwnd)
    rest.save(OUT / "jiggle_home_rest.png")
    print("rest", rest.size)

    nx, ny = chest_click_frac(rest)
    print("click", nx, ny)
    cu.click_nx(hwnd, nx, ny)
    times = (0.04, 0.10, 0.16, 0.24, 0.34, 0.48, 0.70, 1.00)
    frames = []
    t0 = time.perf_counter()
    for t in times:
        remain = t - (time.perf_counter() - t0)
        if remain > 0:
            time.sleep(remain)
        im = cu.print_window(hwnd)
        p = OUT / f"jiggle_t{t:.2f}.png"
        im.save(p)
        frames.append(im)
        print("shot", p.name, "at", round(time.perf_counter() - t0, 3))

    # chest crops: middle of window, upper-middle
    w, h = rest.size
    box = (int(w * 0.28), int(h * 0.18), int(w * 0.72), int(h * 0.52))
    crops = [rest.crop(box)] + [im.crop(box) for im in frames]
    sw, sh = crops[0].size
    strip = Image.new("RGB", (sw * len(crops), sh))
    for i, c in enumerate(crops):
        strip.paste(c.convert("RGB"), (i * sw, 0))
    strip.save(OUT / "jiggle_game_strip.png")
    print("strip", strip.size)

    gif_frames = [c.convert("P", palette=Image.ADAPTIVE, colors=128) for c in crops]
    gif_frames[0].save(
        OUT / "jiggle_game.gif",
        save_all=True,
        append_images=gif_frames[1:],
        duration=90,
        loop=0,
    )
    print("gif", OUT / "jiggle_game.gif")


if __name__ == "__main__":
    main()
