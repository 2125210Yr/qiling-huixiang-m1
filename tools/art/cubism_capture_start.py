"""Launch Cubism once and capture the 启动 dialog for a precise click."""
import ctypes
import subprocess
import time
from ctypes import wintypes
from pathlib import Path

from PIL import ImageGrab
from pywinauto.mouse import click

USER32 = ctypes.windll.user32
HWND_TOPMOST = -1
SWP_NOMOVE = 0x0002
SWP_NOSIZE = 0x0001
SWP_SHOWWINDOW = 0x0040
SW_RESTORE = 9
EnumWindowsProc = ctypes.WINFUNCTYPE(ctypes.c_bool, wintypes.HWND, wintypes.LPARAM)

EXE = r"C:\Program Files\Live2D Cubism 5.3\CubismEditor5.exe"
PSD = r"F:\Resonance\cubism\C001\yanren.psd"
SHOT = Path(r"F:\Resonance\cubism\C001\shots")


def windows():
    found = []

    def foreach(hwnd, _):
        if not USER32.IsWindowVisible(hwnd):
            return True
        n = USER32.GetWindowTextLengthW(hwnd)
        buf = ctypes.create_unicode_buffer(n + 1)
        USER32.GetWindowTextW(hwnd, buf, n + 1)
        title = buf.value or ""
        if not title:
            return True
        if any(k in title for k in ("Cubism", "Live2D", "启动", "消息", "模型")):
            rc = wintypes.RECT()
            USER32.GetWindowRect(hwnd, ctypes.byref(rc))
            found.append((hwnd, title, rc.left, rc.top, rc.right, rc.bottom))
        return True

    USER32.EnumWindows(EnumWindowsProc(foreach), 0)
    return found


def topmost(hwnd):
    USER32.ShowWindow(hwnd, SW_RESTORE)
    USER32.SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW)


def main():
    SHOT.mkdir(parents=True, exist_ok=True)
    print("launch")
    subprocess.Popen([EXE, PSD], cwd=str(Path(EXE).parent))
    dlg = None
    for i in range(40):
        time.sleep(2)
        wins = windows()
        print(i, [(t, x1, y1, x2, y2) for _, t, x1, y1, x2, y2 in wins])
        for hwnd, title, x1, y1, x2, y2 in wins:
            if title.strip() in ("启动", "Start") or "启动" in title:
                dlg = (hwnd, x1, y1, x2, y2)
        frames = [w for w in wins if "Cubism Editor" in w[1]]
        if frames:
            topmost(frames[0][0])
        if dlg:
            break
    img = ImageGrab.grab()
    img.save(SHOT / "full.png")
    if not dlg:
        print("no start dialog")
        return
    hwnd, x1, y1, x2, y2 = dlg
    topmost(hwnd)
    time.sleep(0.3)
    img = ImageGrab.grab()
    crop = img.crop((x1, y1, x2, y2))
    crop.save(SHOT / "start_dlg.png")
    h = y2 - y1
    # 4 option buttons occupy roughly the middle of the dialog
    for i, (a, b) in enumerate([(0.18, 0.32), (0.32, 0.46), (0.46, 0.60), (0.60, 0.74), (0.74, 0.92)]):
        band = crop.crop((0, int(h * a), crop.width, int(h * b)))
        band.save(SHOT / f"btn_{i}.png")
        print("band", i, int(h * a), int(h * b))
    print("dialog", x1, y1, x2, y2)


if __name__ == "__main__":
    main()
