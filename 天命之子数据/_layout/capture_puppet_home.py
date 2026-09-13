"""Capture Home rest then idle of 契灵回响. Does not launch the player."""
from __future__ import annotations

import sys
import time
from pathlib import Path

sys.path.insert(0, str(Path(r"F:\天命之子\天命之子数据\_hires_tiles")))
from capture_ui import TAB_HOME, _dpi_aware, force_foreground, print_window, tap, wait_hwnd

OUT = Path(r"F:\天命之子\天命之子数据\_layout")


def main() -> None:
    _dpi_aware()
    hwnd = wait_hwnd(30)
    if not hwnd:
        raise SystemExit("no 契灵回响 window")
    OUT.mkdir(parents=True, exist_ok=True)
    force_foreground(hwnd)
    time.sleep(1.2)
    tap(hwnd, TAB_HOME, 0.8)
    img0 = print_window(hwnd)
    p0 = OUT / "puppet_v3_rest.png"
    img0.save(p0)
    print("wrote", p0, img0.size)
    time.sleep(3.2)
    img1 = print_window(hwnd)
    p1 = OUT / "puppet_v3_idle.png"
    img1.save(p1)
    print("wrote", p1, img1.size)


if __name__ == "__main__":
    main()
