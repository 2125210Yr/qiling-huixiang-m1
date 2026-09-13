"""Bring Cubism to front, confirm PSD import, try save/export."""
from __future__ import annotations

import ctypes
import subprocess
import time
from ctypes import wintypes
from pathlib import Path

from PIL import ImageGrab
from pywinauto.keyboard import send_keys
from pywinauto.mouse import click

USER32 = ctypes.windll.user32
HWND_TOPMOST = -1
HWND_NOTOPMOST = -2
SWP_NOMOVE = 0x0002
SWP_NOSIZE = 0x0001
SWP_SHOWWINDOW = 0x0040
SW_RESTORE = 9
SW_MAXIMIZE = 3

BAT = Path(r"C:\Program Files\Live2D Cubism 5.3\CubismEditor5.bat")
PSD = Path(r"F:\Resonance\cubism\C001\yanren.psd")
CMO = Path(r"F:\Resonance\cubism\C001\yanren.cmo3")
SHOT = Path(r"F:\Resonance\cubism\C001\shots")
LOG = Path(r"C:\Users\Administrator\AppData\Roaming\Live2D\Cubism5.3_Editor\logs\log.txt")

EnumWindowsProc = ctypes.WINFUNCTYPE(ctypes.c_bool, wintypes.HWND, wintypes.LPARAM)
GetWindowText = USER32.GetWindowTextW
GetWindowTextLength = USER32.GetWindowTextLengthW
IsWindowVisible = USER32.IsWindowVisible
GetWindowRect = USER32.GetWindowRect
GetWindowThreadProcessId = USER32.GetWindowThreadProcessId


def enum_cubism():
    found = []

    def foreach(hwnd, _lparam):
        if not IsWindowVisible(hwnd):
            return True
        length = GetWindowTextLength(hwnd)
        buff = ctypes.create_unicode_buffer(length + 1)
        GetWindowText(hwnd, buff, length + 1)
        title = buff.value or ""
        if "Cubism" in title or "Live2D" in title or title in ("启动", "消息", "模型设置"):
            pid = wintypes.DWORD()
            GetWindowThreadProcessId(hwnd, ctypes.byref(pid))
            rect = wintypes.RECT()
            GetWindowRect(hwnd, ctypes.byref(rect))
            found.append(
                {
                    "hwnd": hwnd,
                    "pid": pid.value,
                    "title": title,
                    "rect": (rect.left, rect.top, rect.right, rect.bottom),
                }
            )
        return True

    USER32.EnumWindows(EnumWindowsProc(foreach), 0)
    return found


def topmost(hwnd, on=True):
    USER32.ShowWindow(hwnd, SW_RESTORE)
    USER32.SetWindowPos(
        hwnd,
        HWND_TOPMOST if on else HWND_NOTOPMOST,
        0,
        0,
        0,
        0,
        SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW,
    )


def shot(tag: str):
    SHOT.mkdir(parents=True, exist_ok=True)
    path = SHOT / f"{tag}.png"
    ImageGrab.grab().save(path)
    print("shot", path)
    return path


def log_tail(from_pos: int) -> str:
    if not LOG.exists():
        return ""
    data = LOG.read_text(encoding="utf-8", errors="replace")
    return data[from_pos:]


def main():
    SHOT.mkdir(parents=True, exist_ok=True)
    start_pos = LOG.stat().st_size if LOG.exists() else 0
    existing = enum_cubism()
    print("existing", existing)
    if not existing:
        print("launching", BAT, PSD)
        subprocess.Popen(
            ["cmd", "/c", "start", "", str(BAT), str(PSD)],
            cwd=str(BAT.parent),
        )

    editor = None
    deadline = time.time() + 75
    while time.time() < deadline:
        time.sleep(2)
        wins = enum_cubism()
        print("wins", [(w["title"], hex(w["hwnd"])) for w in wins])
        frames = [w for w in wins if "Cubism Editor" in w["title"]]
        if frames:
            editor = max(frames, key=lambda w: (w["rect"][2] - w["rect"][0]) * (w["rect"][3] - w["rect"][1]))
            topmost(editor["hwnd"], True)
            shot("topmost")
            break

    if editor is None:
        print("no editor window")
        shot("no_editor")
        return

    # Click title bar so keystrokes land in Cubism, not Chrome.
    l, t, r, b = editor["rect"]
    click(coords=(l + 120, t + 12))
    time.sleep(0.4)
    send_keys("{ESC}")
    time.sleep(0.4)
    send_keys("{ENTER}")
    time.sleep(1)
    shot("after_enter")

    # Wait until log shows PSD load / project tree.
    deadline = time.time() + 40
    while time.time() < deadline:
        chunk = log_tail(start_pos)
        if "PSDDocument" in chunk or "buildProjectTree" in chunk or "yanren" in (editor["title"] if editor else ""):
            print("import signal in log or title")
            break
        time.sleep(2)
        wins = enum_cubism()
        print("wins", [w["title"] for w in wins])
        frames = [w for w in wins if "Cubism Editor" in w["title"]]
        if frames:
            editor = frames[0]
            topmost(editor["hwnd"], True)

    shot("imported")
    print("log delta:\n", log_tail(start_pos)[-2500:])

    # Save As
    l, t, r, b = editor["rect"]
    click(coords=(l + 120, t + 12))
    time.sleep(0.3)
    send_keys("^s")
    time.sleep(1.2)
    shot("save_dialog")
    send_keys(str(CMO), pause=0.05)
    time.sleep(0.2)
    send_keys("{ENTER}")
    time.sleep(2)
    shot("after_save")
    print("cmo exists", CMO.exists(), CMO if CMO.exists() else "")
    print("final wins", [w["title"] for w in enum_cubism()])


if __name__ == "__main__":
    main()
