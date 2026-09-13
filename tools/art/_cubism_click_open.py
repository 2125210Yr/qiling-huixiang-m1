"""Fill Cubism Java Open dialog and confirm."""
from __future__ import annotations

import ctypes
import time
from ctypes import wintypes

from PIL import ImageGrab

user32 = ctypes.windll.user32
kernel32 = ctypes.windll.kernel32

KEYEVENTF_KEYUP = 2
VK_CONTROL = 0x11
VK_V = 0x56
VK_A = 0x41
VK_RETURN = 0x0D
MOUSEEVENTF_LEFTDOWN = 0x0002
MOUSEEVENTF_LEFTUP = 0x0004
SW_RESTORE = 9
OUT = r"F:\天命之子\天命之子数据\_layout"


class RECT(ctypes.Structure):
    _fields_ = [("l", ctypes.c_long), ("t", ctypes.c_long), ("r", ctypes.c_long), ("b", ctypes.c_long)]


WNDENUMPROC = ctypes.WINFUNCTYPE(ctypes.c_bool, wintypes.HWND, wintypes.LPARAM)


def find_open_dialog() -> int:
    found = 0

    def cb(hwnd, lparam):
        nonlocal found
        if not user32.IsWindowVisible(hwnd):
            return True
        buf = ctypes.create_unicode_buffer(256)
        user32.GetWindowTextW(hwnd, buf, 256)
        cls = ctypes.create_unicode_buffer(128)
        user32.GetClassNameW(hwnd, cls, 128)
        if cls.value == "SunAwtDialog" and buf.value == "打开":
            found = hwnd
            return False
        return True

    user32.EnumWindows(WNDENUMPROC(cb), 0)
    return found


def focus(hwnd: int) -> None:
    fg = user32.GetForegroundWindow()
    cur = kernel32.GetCurrentThreadId()
    fg_tid = user32.GetWindowThreadProcessId(fg, None)
    tgt = user32.GetWindowThreadProcessId(hwnd, None)
    user32.AttachThreadInput(cur, fg_tid, True)
    user32.AttachThreadInput(cur, tgt, True)
    user32.ShowWindow(hwnd, SW_RESTORE)
    user32.BringWindowToTop(hwnd)
    print("fg", user32.SetForegroundWindow(hwnd), user32.GetForegroundWindow())
    time.sleep(0.2)
    user32.AttachThreadInput(cur, fg_tid, False)
    user32.AttachThreadInput(cur, tgt, False)


def key(vk: int, down: bool = True) -> None:
    user32.keybd_event(vk, 0, 0 if down else KEYEVENTF_KEYUP, 0)


def chord(*vks: int) -> None:
    for v in vks:
        key(v, True)
        time.sleep(0.03)
    for v in reversed(vks):
        key(v, False)
        time.sleep(0.03)


def tap(vk: int) -> None:
    key(vk, True)
    time.sleep(0.04)
    key(vk, False)
    time.sleep(0.05)


def click(x: int, y: int) -> None:
    user32.SetCursorPos(x, y)
    time.sleep(0.06)
    user32.mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0)
    time.sleep(0.05)
    user32.mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0)
    time.sleep(0.1)


def main() -> None:
    dlg = find_open_dialog()
    print("dialog", dlg)
    if not dlg:
        raise SystemExit("open dialog not found")
    r = RECT()
    user32.GetWindowRect(dlg, ctypes.byref(r))
    print("rect", r.l, r.t, r.r, r.b)
    focus(dlg)
    click(r.l + 280, r.t + 388)
    time.sleep(0.2)
    chord(VK_CONTROL, VK_A)
    time.sleep(0.1)
    chord(VK_CONTROL, VK_V)
    time.sleep(0.35)
    img = ImageGrab.grab(bbox=(r.l, r.t, r.r, r.b))
    img.save(OUT + r"\cu_path_typed.png")
    print("typed", img.size)
    click(r.l + 575, r.t + 448)
    time.sleep(0.6)
    tap(VK_RETURN)
    time.sleep(1.8)
    img = ImageGrab.grab(bbox=(60, 60, 1760, 1080))
    img.save(OUT + r"\cu_after_confirm.png")
    print("after", img.size)


if __name__ == "__main__":
    main()
