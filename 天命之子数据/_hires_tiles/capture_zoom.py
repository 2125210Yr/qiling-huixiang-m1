"""Launch-independent: find 契灵回响, PrintWindow, wheel-zoom, crop chest."""
from __future__ import annotations

import ctypes
import time
from ctypes import wintypes
from pathlib import Path

import win32con
import win32gui
import win32api
import win32process
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


def force_foreground(hwnd: int) -> None:
    fg = win32gui.GetForegroundWindow()
    cur = win32api.GetCurrentThreadId()
    fg_tid, _ = win32process.GetWindowThreadProcessId(fg)
    tgt_tid, _ = win32process.GetWindowThreadProcessId(hwnd)
    win32process.AttachThreadInput(cur, tgt_tid, True)
    if fg_tid and fg_tid != tgt_tid:
        win32process.AttachThreadInput(fg_tid, tgt_tid, True)
    win32gui.ShowWindow(hwnd, win32con.SW_RESTORE)
    win32gui.BringWindowToTop(hwnd)
    try:
        win32gui.SetForegroundWindow(hwnd)
    except Exception:
        pass
    win32process.AttachThreadInput(cur, tgt_tid, False)
    if fg_tid and fg_tid != tgt_tid:
        try:
            win32process.AttachThreadInput(fg_tid, tgt_tid, False)
        except Exception:
            pass


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


def wheel_at(hwnd: int, nx: float, ny: float, steps: int) -> None:
    l, t, r, b = win32gui.GetWindowRect(hwnd)
    x = int(l + (r - l) * nx)
    y = int(t + (b - t) * ny)
    USER32.SetCursorPos(x, y)
    time.sleep(0.05)
    # MOUSEEVENTF_WHEEL = 0x0800, WHEEL_DELTA = 120
    for _ in range(steps):
        USER32.mouse_event(0x0800, 0, 0, 120, 0)
        time.sleep(0.04)


def main() -> None:
    hwnd = None
    for i in range(40):
        hwnd = find_hwnd()
        if hwnd:
            break
        time.sleep(0.4)
    if not hwnd:
        raise SystemExit("no window")
    print("hwnd", hex(hwnd), win32gui.GetWindowText(hwnd), win32gui.GetClientRect(hwnd))
    force_foreground(hwnd)
    time.sleep(0.8)

    def dbl():
        USER32.mouse_event(0x0002, 0, 0, 0, 0)
        USER32.mouse_event(0x0004, 0, 0, 0, 0)
        time.sleep(0.06)
        USER32.mouse_event(0x0002, 0, 0, 0, 0)
        USER32.mouse_event(0x0004, 0, 0, 0, 0)
        time.sleep(0.25)

    full = print_window(hwnd)
    full.save(OUT / "game_home.png")
    print("home", full.size)

    shots = [
        ("chest", 0.52, 0.38, 16),
        ("hair", 0.22, 0.32, 16),
        ("sword", 0.38, 0.62, 16),
        ("legs", 0.52, 0.70, 16),
    ]
    for name, nx, ny, steps in shots:
        dbl()
        wheel_at(hwnd, nx, ny, steps)
        time.sleep(0.35)
        img = print_window(hwnd)
        img.save(OUT / f"game_zoom_{name}.png")
        print("zoom", name, img.size)
    dbl()


if __name__ == "__main__":
    main()
