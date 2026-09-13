"""Re-import updated PSD into the open Cubism model (replace)."""
from __future__ import annotations

import ctypes
import time
from ctypes import wintypes

from PIL import ImageGrab

user32 = ctypes.windll.user32
kernel32 = ctypes.windll.kernel32

KEYEVENTF_KEYUP = 2
VK_CONTROL = 0x11
VK_O = 0x4F
VK_V = 0x56
VK_A = 0x41
VK_RETURN = 0x0D
VK_DOWN = 0x28
MOUSEEVENTF_LEFTDOWN = 2
MOUSEEVENTF_LEFTUP = 4
SW_RESTORE = 9
OUT = r"F:\天命之子\天命之子数据\_layout"
CUBISM = 663116
PATH = r"C:\Users\Administrator\bingren_cubism_import.psd"

WNDENUMPROC = ctypes.WINFUNCTYPE(ctypes.c_bool, wintypes.HWND, wintypes.LPARAM)


class RECT(ctypes.Structure):
    _fields_ = [("l", ctypes.c_long), ("t", ctypes.c_long), ("r", ctypes.c_long), ("b", ctypes.c_long)]


def grab(name, bbox=None):
    img = ImageGrab.grab(bbox=bbox) if bbox else ImageGrab.grab()
    img.save(fr"{OUT}\{name}.png")
    print("grab", name, img.size)


def title(h):
    b = ctypes.create_unicode_buffer(256)
    user32.GetWindowTextW(h, b, 256)
    return b.value


def cls(h):
    b = ctypes.create_unicode_buffer(128)
    user32.GetClassNameW(h, b, 128)
    return b.value


def focus(h):
    fg = user32.GetForegroundWindow()
    cur = kernel32.GetCurrentThreadId()
    user32.AttachThreadInput(cur, user32.GetWindowThreadProcessId(fg, None), True)
    user32.AttachThreadInput(cur, user32.GetWindowThreadProcessId(h, None), True)
    user32.ShowWindow(h, SW_RESTORE)
    user32.BringWindowToTop(h)
    user32.SetForegroundWindow(h)
    time.sleep(0.15)
    user32.AttachThreadInput(cur, user32.GetWindowThreadProcessId(fg, None), False)
    user32.AttachThreadInput(cur, user32.GetWindowThreadProcessId(h, None), False)


def key(vk, down=True):
    user32.keybd_event(vk, 0, 0 if down else KEYEVENTF_KEYUP, 0)


def chord(*vks):
    for v in vks:
        key(v, True)
        time.sleep(0.03)
    for v in reversed(vks):
        key(v, False)
        time.sleep(0.03)


def tap(vk):
    key(vk, True)
    time.sleep(0.04)
    key(vk, False)
    time.sleep(0.05)


def click(x, y):
    user32.SetCursorPos(int(x), int(y))
    time.sleep(0.05)
    user32.mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0)
    time.sleep(0.04)
    user32.mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0)
    time.sleep(0.08)


def find_dialog(name: str) -> int:
    found = 0

    def cb(h, lparam):
        nonlocal found
        if user32.IsWindowVisible(h) and cls(h) == "SunAwtDialog" and name in title(h):
            found = h
            return False
        return True

    user32.EnumWindows(WNDENUMPROC(cb), 0)
    return found


def list_java_dialogs():
    items = []

    def cb(h, lparam):
        if user32.IsWindowVisible(h):
            p = wintypes.DWORD()
            user32.GetWindowThreadProcessId(h, ctypes.byref(p))
            if p.value == 38164:
                r = RECT()
                user32.GetWindowRect(h, ctypes.byref(r))
                items.append((h, cls(h), title(h), r.l, r.t, r.r, r.b))
        return True

    user32.EnumWindows(WNDENUMPROC(cb), 0)
    return items


def main():
    import subprocess
    subprocess.run(["powershell", "-NoProfile", "-Command", f"Set-Clipboard -Value '{PATH}'"], check=False)
    focus(CUBISM)
    chord(VK_CONTROL, VK_O)
    time.sleep(0.9)
    dlg = find_dialog("打开")
    print("open dlg", dlg, list_java_dialogs())
    if not dlg:
        grab("re_no_open")
        raise SystemExit("no open")
    r = RECT()
    user32.GetWindowRect(dlg, ctypes.byref(r))
    focus(dlg)
    click(r.l + 280, r.t + 388)
    time.sleep(0.15)
    chord(VK_CONTROL, VK_A)
    time.sleep(0.08)
    chord(VK_CONTROL, VK_V)
    time.sleep(0.25)
    grab("re_path", (r.l, r.t, r.r, r.b))
    click(r.l + 575, r.t + 448)
    time.sleep(0.4)
    tap(VK_RETURN)
    time.sleep(1.4)
    print("after open", list_java_dialogs())
    grab("re_model", (60, 60, 1760, 1080))
    # 模型设置: 3 options, we want the last (replace). Down twice, Enter, then OK.
    ms = find_dialog("模型设置")
    print("model settings", ms)
    if ms:
        focus(ms)
        time.sleep(0.2)
        tap(VK_DOWN)
        time.sleep(0.12)
        tap(VK_DOWN)
        time.sleep(0.12)
        grab("re_opt", (60, 60, 1760, 1080))
        tap(VK_RETURN)
        time.sleep(0.4)
        tap(VK_RETURN)
    time.sleep(3.0)
    print("final", list_java_dialogs())
    grab("re_done", (60, 60, 1760, 1080))


if __name__ == "__main__":
    main()
