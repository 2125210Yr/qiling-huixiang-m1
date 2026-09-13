"""Drive Cubism File→Open via keyboard + native Open dialog."""
from __future__ import annotations

import ctypes
import shutil
import time
from ctypes import wintypes
from pathlib import Path

from PIL import Image, ImageGrab

user32 = ctypes.windll.user32
kernel32 = ctypes.windll.kernel32
gdi32 = ctypes.windll.gdi32

SRC = Path(r"F:\天命之子\art\characters\C001-焰刃\bingren_cubism_import.psd")
# ASCII short path for the common file dialog
SHORT = Path(r"C:\Users\Administrator\bingren_cubism_import.psd")
OUT = Path(r"F:\天命之子\天命之子数据\_layout")
CUBISM_HWND = 663116
SW_RESTORE = 9
KEYEVENTF_KEYUP = 0x0002
VK_CONTROL = 0x11
VK_O = 0x4F
VK_RETURN = 0x0D
VK_ESCAPE = 0x1B
VK_TAB = 0x09
VK_DOWN = 0x28
WM_SETTEXT = 0x000C
BM_CLICK = 0x00F5
CF_UNICODETEXT = 13


class RECT(ctypes.Structure):
    _fields_ = [("l", ctypes.c_long), ("t", ctypes.c_long), ("r", ctypes.c_long), ("b", ctypes.c_long)]


WNDENUMPROC = ctypes.WINFUNCTYPE(ctypes.c_bool, wintypes.HWND, wintypes.LPARAM)


def grab(name: str, hwnd: int | None = None) -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    if hwnd:
        r = RECT()
        user32.GetWindowRect(hwnd, ctypes.byref(r))
        if r.r > r.l and r.b > r.t and r.l > -10000:
            img = ImageGrab.grab(bbox=(r.l, r.t, r.r, r.b))
            img.save(OUT / f"{name}.png")
            print(f"grab {name} {img.size} hwnd={hwnd} rect=({r.l},{r.t},{r.r},{r.b})")
            return
    img = ImageGrab.grab()
    img.save(OUT / f"{name}.png")
    print(f"grab {name} desktop {img.size}")


def printwindow(hwnd: int, name: str) -> None:
    r = RECT()
    user32.GetWindowRect(hwnd, ctypes.byref(r))
    w, h = r.r - r.l, r.b - r.t
    if w <= 0 or h <= 0:
        print("printwindow skip empty", hwnd)
        return
    hdc = user32.GetDC(0)
    mem = gdi32.CreateCompatibleDC(hdc)
    bmp = gdi32.CreateCompatibleBitmap(hdc, w, h)
    gdi32.SelectObject(mem, bmp)
    user32.PrintWindow(hwnd, mem, 2)

    class BIH(ctypes.Structure):
        _fields_ = [
            ("biSize", ctypes.c_uint32), ("biWidth", ctypes.c_int32), ("biHeight", ctypes.c_int32),
            ("biPlanes", ctypes.c_uint16), ("biBitCount", ctypes.c_uint16), ("biCompression", ctypes.c_uint32),
            ("biSizeImage", ctypes.c_uint32), ("biXPelsPerMeter", ctypes.c_int32),
            ("biYPelsPerMeter", ctypes.c_int32), ("biClrUsed", ctypes.c_uint32), ("biClrImportant", ctypes.c_uint32),
        ]

    class BI(ctypes.Structure):
        _fields_ = [("h", BIH), ("c", ctypes.c_uint32 * 3)]

    bi = BI()
    bi.h.biSize = 40
    bi.h.biWidth = w
    bi.h.biHeight = -h
    bi.h.biPlanes = 1
    bi.h.biBitCount = 32
    buf = ctypes.create_string_buffer(w * h * 4)
    gdi32.GetDIBits(mem, bmp, 0, h, buf, ctypes.byref(bi), 0)
    img = Image.frombuffer("RGBA", (w, h), buf, "raw", "BGRA", 0, 1)
    img.save(OUT / f"{name}.png")
    print(f"pw {name} {img.size}")
    gdi32.DeleteObject(bmp)
    gdi32.DeleteDC(mem)
    user32.ReleaseDC(0, hdc)


def window_title(hwnd: int) -> str:
    buf = ctypes.create_unicode_buffer(512)
    user32.GetWindowTextW(hwnd, buf, 512)
    return buf.value


def window_class(hwnd: int) -> str:
    buf = ctypes.create_unicode_buffer(256)
    user32.GetClassNameW(hwnd, buf, 256)
    return buf.value


def list_windows(pid: int | None = None) -> list[tuple[int, str, str, tuple, bool]]:
    found: list[tuple[int, str, str, tuple, bool]] = []

    def cb(hwnd, lparam):
        p = wintypes.DWORD()
        user32.GetWindowThreadProcessId(hwnd, ctypes.byref(p))
        if pid is not None and p.value != pid:
            return True
        vis = bool(user32.IsWindowVisible(hwnd))
        title = window_title(hwnd)
        cls = window_class(hwnd)
        r = RECT()
        user32.GetWindowRect(hwnd, ctypes.byref(r))
        if vis or title:
            found.append((hwnd, cls, title, (r.l, r.t, r.r, r.b), vis))
        return True

    user32.EnumWindows(WNDENUMPROC(cb), 0)
    return found


def focus_hwnd(hwnd: int) -> None:
    fg = user32.GetForegroundWindow()
    cur = kernel32.GetCurrentThreadId()
    fg_tid = user32.GetWindowThreadProcessId(fg, None)
    tgt_tid = user32.GetWindowThreadProcessId(hwnd, None)
    user32.AttachThreadInput(cur, fg_tid, True)
    user32.AttachThreadInput(cur, tgt_tid, True)
    user32.ShowWindow(hwnd, SW_RESTORE)
    user32.BringWindowToTop(hwnd)
    user32.SetForegroundWindow(hwnd)
    time.sleep(0.15)
    user32.AttachThreadInput(cur, fg_tid, False)
    user32.AttachThreadInput(cur, tgt_tid, False)


def key(vk: int, down: bool = True) -> None:
    user32.keybd_event(vk, 0, 0 if down else KEYEVENTF_KEYUP, 0)


def chord(*vks: int) -> None:
    for vk in vks:
        key(vk, True)
        time.sleep(0.03)
    for vk in reversed(vks):
        key(vk, False)
        time.sleep(0.03)


def tap(vk: int) -> None:
    key(vk, True)
    time.sleep(0.04)
    key(vk, False)
    time.sleep(0.04)


def set_clipboard(text: str) -> None:
    user32.OpenClipboard(None)
    user32.EmptyClipboard()
    data = (text + "\0").encode("utf-16le")
    GMEM_MOVEABLE = 0x0002
    kernel32.GlobalAlloc.restype = wintypes.HGLOBAL
    h = kernel32.GlobalAlloc(GMEM_MOVEABLE, len(data))
    ptr = kernel32.GlobalLock(h)
    ctypes.memmove(ptr, data, len(data))
    kernel32.GlobalUnlock(h)
    user32.SetClipboardData(CF_UNICODETEXT, h)
    user32.CloseClipboard()


def find_dialog(substr: str, cls: str | None = None, timeout: float = 8.0) -> int:
    deadline = time.time() + timeout
    while time.time() < deadline:
        for hwnd, c, title, rect, vis in list_windows():
            if not vis:
                continue
            if cls and c != cls:
                continue
            if substr.lower() in title.lower() or substr.lower() in c.lower():
                print(f"found {hwnd} {c!r} {title!r} {rect}")
                return hwnd
        time.sleep(0.2)
    return 0


def enum_children(hwnd: int) -> list[tuple[int, str, str]]:
    kids: list[tuple[int, str, str]] = []

    def cb(h, lparam):
        kids.append((h, window_class(h), window_title(h)))
        return True

    user32.EnumChildWindows(hwnd, WNDENUMPROC(cb), 0)
    return kids


def main() -> None:
    shutil.copy2(SRC, SHORT)
    print("short", SHORT, SHORT.exists(), SHORT.stat().st_size)
    hwnd = CUBISM_HWND
    if not user32.IsWindow(hwnd):
        raise SystemExit("Cubism hwnd gone")
    focus_hwnd(hwnd)
    print("fg", user32.GetForegroundWindow(), "title", window_title(user32.GetForegroundWindow()))
    printwindow(hwnd, "cu_before")
    grab("cu_before_grab", hwnd)

    # Ctrl+O
    chord(VK_CONTROL, VK_O)
    time.sleep(0.8)
    dlg = find_dialog("打开", cls="#32770", timeout=6) or find_dialog("Open", cls="#32770", timeout=2)
    if not dlg:
        # maybe Chinese title is 打开 / 打开文件
        for h, c, t, rect, vis in list_windows():
            if vis and c == "#32770":
                print("candidate dialog", h, t, rect)
                dlg = h
                break
    if not dlg:
        grab("cu_no_dialog")
        raise SystemExit("no open dialog")
    grab("cu_opendlg", dlg)

    kids = enum_children(dlg)
    for k in kids:
        print(" child", k)
    edit = 0
    open_btn = 0
    for h, c, t in kids:
        if c == "Edit" and not edit:
            edit = h
        if c == "Button" and t.replace("&", "") in ("打开(O)", "打开", "Open", "打开(O)"):
            open_btn = h
        if "打开" in t and c == "Button":
            open_btn = h
    print("edit", edit, "open_btn", open_btn)

    path = str(SHORT)
    if edit:
        user32.SendMessageW(edit, WM_SETTEXT, 0, path)
        time.sleep(0.2)
    else:
        set_clipboard(path)
        focus_hwnd(dlg)
        chord(VK_CONTROL, 0x56)  # V
        time.sleep(0.2)
    grab("cu_path_filled", dlg)
    if open_btn:
        user32.SendMessageW(open_btn, BM_CLICK, 0, 0)
    else:
        focus_hwnd(dlg)
        tap(VK_RETURN)
    time.sleep(1.5)

    # Model settings Java dialog
    grab("cu_after_open")
    printwindow(hwnd, "cu_after_open_pw")
    print("windows after open:")
    for h, c, t, rect, vis in list_windows(38164):
        if vis:
            print(" ", h, c, t, rect)

    # Try Enter to accept default "create new model from PSD"
    # First look for SunAwtDialog
    java_dlg = 0
    for h, c, t, rect, vis in list_windows(38164):
        if vis and c in ("SunAwtDialog", "SunAwtWindow") and h != hwnd:
            print("java popup", h, c, t, rect)
            java_dlg = h
    if java_dlg:
        grab("cu_model_settings", java_dlg)
        printwindow(java_dlg, "cu_model_settings_pw")
        focus_hwnd(java_dlg)
        time.sleep(0.2)
        tap(VK_RETURN)
        time.sleep(2.0)
    else:
        # default button might already be focused
        tap(VK_RETURN)
        time.sleep(2.0)

    grab("cu_final")
    printwindow(hwnd, "cu_final_pw")
    print("done windows:")
    for h, c, t, rect, vis in list_windows(38164):
        if vis:
            print(" ", h, c, t, rect)


if __name__ == "__main__":
    main()
