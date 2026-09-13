"""Capture home / roster / team / inspect / battle of 契灵回响.

Expects the player already on Home (or Boot that becomes Home).
Does not launch or kill Resonance.exe.

    python capture_ui.py
"""
from __future__ import annotations

import ctypes
import time
from pathlib import Path

import win32api
import win32con
import win32gui
import win32process
import win32ui
from PIL import Image

OUT = Path(r"F:\天命之子\天命之子数据\_hires_tiles")
USER32 = ctypes.windll.user32

# Unity Screen-space anchors (x from left, y from bottom) on the 1080x1920 canvas.
TAB_HOME = (0.083, 0.042)
TAB_ROSTER = (0.250, 0.042)
TAB_STAGE = (0.417, 0.042)
BTN_TEAM = (0.320, 0.125)
BTN_DETAIL = (0.860, 0.655)
BTN_INSPECT_CLOSE = (0.935, 0.955)
BTN_BATTLE = (0.500, 0.078)


def _dpi_aware() -> None:
    try:
        ctypes.windll.shcore.SetProcessDpiAwareness(2)
    except Exception:
        try:
            USER32.SetProcessDPIAware()
        except Exception:
            pass


def find_hwnd():
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


def wait_hwnd(timeout=20.0):
    deadline = time.time() + timeout
    hwnd = None
    while time.time() < deadline:
        hwnd = find_hwnd()
        if hwnd:
            return hwnd
        time.sleep(0.4)
    return None


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


def client_box(hwnd):
    cl, ct = win32gui.ClientToScreen(hwnd, (0, 0))
    _, _, cw, ch = win32gui.GetClientRect(hwnd)
    return cl, ct, cw, ch


def print_window(hwnd):
    l, t, r, b = win32gui.GetWindowRect(hwnd)
    w, h = r - l, b - t
    hdc = win32gui.GetWindowDC(hwnd)
    mfc = win32ui.CreateDCFromHandle(hdc)
    save = mfc.CreateCompatibleDC()
    bmp = win32ui.CreateBitmap()
    bmp.CreateCompatibleBitmap(mfc, w, h)
    save.SelectObject(bmp)
    USER32.PrintWindow(hwnd, save.GetSafeHdc(), 2)
    bits = bmp.GetBitmapBits(True)
    info = bmp.GetInfo()
    img = Image.frombuffer("RGB", (info["bmWidth"], info["bmHeight"]), bits, "raw", "BGRX", 0, 1).copy()
    mfc.DeleteDC()
    save.DeleteDC()
    win32gui.ReleaseDC(hwnd, hdc)
    win32gui.DeleteObject(bmp.GetHandle())
    return img


def click_nx(hwnd, nx, ny):
    cl, ct, cw, ch = client_box(hwnd)
    x, y = int(cl + nx * cw), int(ct + (1.0 - ny) * ch)
    win32api.SetCursorPos((x, y))
    time.sleep(0.04)
    win32api.mouse_event(win32con.MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0)
    win32api.mouse_event(win32con.MOUSEEVENTF_LEFTUP, 0, 0, 0, 0)


def shot(hwnd, name):
    img = print_window(hwnd)
    p = OUT / name
    img.save(p)
    print("wrote", p, img.size)
    return p


def tap(hwnd, xy, wait):
    click_nx(hwnd, xy[0], xy[1])
    time.sleep(wait)


def main():
    _dpi_aware()
    hwnd = wait_hwnd()
    if not hwnd:
        raise SystemExit("no 契灵回响 / Resonance window")
    OUT.mkdir(parents=True, exist_ok=True)
    force_foreground(hwnd)
    cl, ct, cw, ch = client_box(hwnd)
    print("hwnd", hex(hwnd), win32gui.GetWindowText(hwnd), "client", cw, ch)

    time.sleep(2.0)
    tap(hwnd, TAB_HOME, 0.7)
    shot(hwnd, "ui_home.png")

    tap(hwnd, TAB_ROSTER, 0.8)
    shot(hwnd, "ui_roster.png")

    tap(hwnd, BTN_TEAM, 0.8)
    shot(hwnd, "ui_team.png")

    tap(hwnd, BTN_DETAIL, 0.8)
    shot(hwnd, "ui_inspect.png")

    tap(hwnd, BTN_INSPECT_CLOSE, 0.6)
    tap(hwnd, TAB_STAGE, 0.8)
    tap(hwnd, BTN_BATTLE, 0.9)
    shot(hwnd, "ui_battle.png")


if __name__ == "__main__":
    main()
