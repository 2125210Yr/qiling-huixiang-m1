"""Drive Cubism 5.3 FREE: auto-mesh, texture atlas, save cmo3, export moc3."""
from __future__ import annotations

import ctypes
import time
from ctypes import wintypes
from pathlib import Path

import win32con
import win32gui
from PIL import ImageGrab
from pywinauto.keyboard import send_keys
from pywinauto.mouse import click

SHOT = Path(r"F:\Resonance\cubism\C001\shots")
CMO = Path(r"F:\Resonance\cubism\C001\yanren.cmo3")
EXPORT_DIR = Path(r"F:\Resonance\cubism\C001\runtime")
LOG = Path(r"C:\Users\Administrator\AppData\Roaming\Live2D\Cubism5.3_Editor\logs\log.txt")

USER32 = ctypes.windll.user32
VS_X = USER32.GetSystemMetrics(76)
VS_Y = USER32.GetSystemMetrics(77)


def cubism_hwnd():
    found = []

    def cb(h, _):
        if win32gui.IsWindowVisible(h) and "Cubism Editor" in win32gui.GetWindowText(h):
            found.append(h)
        return True

    win32gui.EnumWindows(cb, None)
    return found[0] if found else None


def rect_of(hwnd):
    return win32gui.GetWindowRect(hwnd)


def force_foreground(hwnd):
    import win32api
    import win32process

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


def raise_cubism(hwnd):
    # Park the editor on the primary monitor so Cursor/Chrome on the left
    # screen cannot steal clicks or keystrokes.
    win32gui.SetWindowPos(
        hwnd,
        win32con.HWND_TOPMOST,
        40,
        40,
        1680,
        1000,
        win32con.SWP_SHOWWINDOW,
    )
    force_foreground(hwnd)
    l, t, r, b = rect_of(hwnd)
    click(coords=(l + 200, t + 12))
    time.sleep(0.2)
    # Click the canvas (character), not overlapping palettes.
    click(coords=(l + int((r - l) * 0.48), t + int((b - t) * 0.52)))
    time.sleep(0.25)
    fg = win32gui.GetForegroundWindow()
    print("foreground", repr(win32gui.GetWindowText(fg)), hex(fg))


def print_window(hwnd) -> "Image.Image":
    import win32ui

    l, t, r, b = rect_of(hwnd)
    w, h = r - l, b - t
    hwnd_dc = win32gui.GetWindowDC(hwnd)
    mfc = win32ui.CreateDCFromHandle(hwnd_dc)
    save = mfc.CreateCompatibleDC()
    bmp = win32ui.CreateBitmap()
    bmp.CreateCompatibleBitmap(mfc, w, h)
    save.SelectObject(bmp)
    ctypes.windll.user32.PrintWindow(hwnd, save.GetSafeHdc(), 2)
    bits = bmp.GetBitmapBits(True)
    info = bmp.GetInfo()
    from PIL import Image

    img = Image.frombuffer(
        "RGB", (info["bmWidth"], info["bmHeight"]), bits, "raw", "BGRX", 0, 1
    ).copy()
    mfc.DeleteDC()
    save.DeleteDC()
    win32gui.ReleaseDC(hwnd, hwnd_dc)
    win32gui.DeleteObject(bmp.GetHandle())
    return img


def shot(tag: str) -> Path:
    SHOT.mkdir(parents=True, exist_ok=True)
    hwnd = cubism_hwnd()
    path = SHOT / f"{tag}.png"
    if hwnd:
        print_window(hwnd).save(path)
    else:
        ImageGrab.grab(all_screens=True).save(path)
    print("shot", path, path.stat().st_size)
    return path


def shot_full(tag: str) -> Path:
    SHOT.mkdir(parents=True, exist_ok=True)
    path = SHOT / f"{tag}.png"
    ImageGrab.grab(all_screens=True).save(path)
    print("full", path, path.stat().st_size)
    return path


def list_popups():
    rows = []

    def cb(h, _):
        if not win32gui.IsWindowVisible(h):
            return True
        title = win32gui.GetWindowText(h)
        if not title:
            return True
        r = win32gui.GetWindowRect(h)
        w, hgt = r[2] - r[0], r[3] - r[1]
        if w < 40 or hgt < 40:
            return True
        if any(
            k in title
            for k in (
                "Cubism",
                "Live2D",
                "自动",
                "网格",
                "纹理",
                "保存",
                "另存",
                "输出",
                "确认",
                "信息",
                "消息",
                "打开",
                "设定",
                "警告",
                "Note",
            )
        ):
            rows.append((title, r, h))
        return True

    win32gui.EnumWindows(cb, None)
    for title, r, h in rows:
        print(" win", repr(title), r)
    return rows


def log_size():
    return LOG.stat().st_size if LOG.exists() else 0


def log_delta(start: int) -> str:
    if not LOG.exists():
        return ""
    return LOG.read_text(encoding="utf-8", errors="replace")[start:]


def keys(seq: str, pause: float = 0.08):
    send_keys(seq, pause=pause, with_spaces=True)


def main(step: str):
    hwnd = cubism_hwnd()
    if not hwnd:
        raise SystemExit("Cubism editor window not found")
    print("hwnd", hex(hwnd), "title", win32gui.GetWindowText(hwnd), "rect", rect_of(hwnd))
    raise_cubism(hwnd)
    start = log_size()

    if step == "shot":
        shot("process_00")
        list_popups()
        return

    if step == "mesh":
        keys("^a")
        time.sleep(0.4)
        shot("process_01_selected")
        keys("^+a")
        time.sleep(1.2)
        shot("process_02_automesh_dlg")
        shot_full("process_02_automesh_full")
        list_popups()
        keys("{ENTER}")
        time.sleep(2.5)
        shot("process_03_after_mesh")
        print("log", log_delta(start)[-1500:])
        return

    if step == "atlas":
        keys("^t")
        time.sleep(1.5)
        shot("process_04_atlas")
        list_popups()
        keys("{ENTER}")
        time.sleep(1.5)
        shot("process_05_atlas2")
        list_popups()
        keys("{ENTER}")
        time.sleep(1.2)
        shot("process_06_after_atlas")
        print("log", log_delta(start)[-1500:])
        return

    if step == "save":
        EXPORT_DIR.mkdir(parents=True, exist_ok=True)
        keys("^s")
        time.sleep(1.2)
        shot("process_07_save")
        list_popups()
        keys(str(CMO), pause=0.03)
        time.sleep(0.2)
        keys("{ENTER}")
        time.sleep(2.0)
        shot("process_08_after_save")
        print("cmo", CMO.exists(), CMO.stat().st_size if CMO.exists() else 0)
        print("log", log_delta(start)[-1500:])
        return

    if step == "export":
        EXPORT_DIR.mkdir(parents=True, exist_ok=True)
        keys("%^s")
        time.sleep(1.5)
        shot("process_09_export")
        list_popups()
        keys("{ENTER}")
        time.sleep(1.2)
        shot("process_10_export2")
        list_popups()
        # If a folder picker appears, type the runtime path.
        keys(str(EXPORT_DIR), pause=0.03)
        time.sleep(0.2)
        keys("{ENTER}")
        time.sleep(2.5)
        shot("process_11_after_export")
        print("runtime", list(EXPORT_DIR.glob("*")) if EXPORT_DIR.exists() else [])
        print("log", log_delta(start)[-2000:])
        return

    raise SystemExit(f"unknown step {step}")


if __name__ == "__main__":
    import sys

    main(sys.argv[1] if len(sys.argv) > 1 else "shot")
