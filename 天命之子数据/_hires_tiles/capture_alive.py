"""Capture 4 home frames a second apart so Ken Burns / light / foreground ice can be compared."""
from __future__ import annotations

import ctypes
import time
from pathlib import Path

import win32con
import win32gui
import win32ui
from PIL import Image

OUT = Path(r"F:\天命之子\天命之子数据\_hires_tiles")
USER32 = ctypes.windll.user32


def find_hwnd() -> int | None:
    found = []

    def cb(h, _):
        if not win32gui.IsWindowVisible(h):
            return True
        title = win32gui.GetWindowText(h)
        if "契灵回响" in title or title == "Resonance":
            found.append(h)
        return True

    win32gui.EnumWindows(cb, None)
    return found[0] if found else None


def print_window(hwnd: int) -> Image.Image:
    l, t, r, b = win32gui.GetWindowRect(hwnd)
    w, h = r - l, b - t
    hwnd_dc = win32gui.GetWindowDC(hwnd)
    mfc = win32ui.CreateDCFromHandle(hwnd_dc)
    save = mfc.CreateCompatibleDC()
    bmp = win32ui.CreateBitmap()
    bmp.CreateCompatibleBitmap(mfc, w, h)
    save.SelectObject(bmp)
    USER32.PrintWindow(hwnd, save.GetSafeHdc(), 2)
    bits = bmp.GetBitmapBits(True)
    info = bmp.GetInfo()
    img = Image.frombuffer(
        "RGB", (info["bmWidth"], info["bmHeight"]), bits, "raw", "BGRX", 0, 1
    ).copy()
    mfc.DeleteDC()
    save.DeleteDC()
    win32gui.ReleaseDC(hwnd, hwnd_dc)
    win32gui.DeleteObject(bmp.GetHandle())
    return img


def main() -> None:
    hwnd = find_hwnd()
    if not hwnd:
        raise SystemExit("no 契灵回响 window")
    OUT.mkdir(parents=True, exist_ok=True)
    paths = []
    for i in range(4):
        img = print_window(hwnd)
        p = OUT / f"alive_{i}.png"
        img.save(p)
        paths.append(p)
        print(f"wrote {p} {img.size}")
        if i < 3:
            time.sleep(1.25)
    a = Image.open(paths[0])
    c = Image.open(paths[2])
    if a.size == c.size:
        d = Image.blend(a, c, 0.5)
        diff = OUT / "alive_blend_0_2.png"
        d.save(diff)
        print(f"wrote {diff}")


if __name__ == "__main__":
    main()
