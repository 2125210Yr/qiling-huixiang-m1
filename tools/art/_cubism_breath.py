"""Key a chest-scale rotation deformer to ParamBreath for inhale/exhale."""
from __future__ import annotations

import asyncio
import json
import os
import time
import uuid
from pathlib import Path

import ctypes
from ctypes import wintypes

import websockets
from PIL import ImageGrab

URI = "ws://127.0.0.1:22033"
TOKEN = Path.home().joinpath(".cubism-mcp", "token.txt").read_text(encoding="utf-8").strip()
UID = "8d2b15166c01423ea58a5758c3d34e02"
OUT = Path(r"F:\天命之子\天命之子数据\_layout")
HWND = 663116


async def call(ws, method, data=None, timeout=20.0):
    req_id = uuid.uuid4().hex
    await ws.send(json.dumps({
        "Version": "1.1.0",
        "RequestId": req_id,
        "Type": "Request",
        "Method": method,
        "Data": data or {},
    }))
    deadline = asyncio.get_event_loop().time() + timeout
    while True:
        remaining = deadline - asyncio.get_event_loop().time()
        if remaining <= 0:
            print("TIMEOUT", method)
            return {"Type": "Timeout", "Data": {}}
        msg = json.loads(await asyncio.wait_for(ws.recv(), timeout=remaining))
        if msg.get("RequestId") == req_id:
            print(method, msg.get("Type"), json.dumps(msg.get("Data"), ensure_ascii=False)[:400])
            return msg


def grab(name: str) -> None:
    r = wintypes.RECT()
    ctypes.windll.user32.GetWindowRect(HWND, ctypes.byref(r))
    img = ImageGrab.grab(bbox=(r.left, r.top, r.right, r.bottom))
    img.save(OUT / f"{name}.png")
    print("grab", name, img.size)


def focus_cubism() -> None:
    user32 = ctypes.windll.user32
    kernel32 = ctypes.windll.kernel32
    fg = user32.GetForegroundWindow()
    cur = kernel32.GetCurrentThreadId()
    user32.AttachThreadInput(cur, user32.GetWindowThreadProcessId(fg, None), True)
    user32.AttachThreadInput(cur, user32.GetWindowThreadProcessId(HWND, None), True)
    user32.ShowWindow(HWND, 9)
    user32.BringWindowToTop(HWND)
    user32.SetForegroundWindow(HWND)
    user32.AttachThreadInput(cur, user32.GetWindowThreadProcessId(fg, None), False)
    user32.AttachThreadInput(cur, user32.GetWindowThreadProcessId(HWND, None), False)


async def main():
    os.environ.setdefault("NO_PROXY", "localhost,127.0.0.1")
    async with websockets.connect(URI, open_timeout=5, close_timeout=2) as ws:
        await call(ws, "RegisterPlugin", {"Token": TOKEN, "Name": "cubism-mcp"})
        uid_now = (await call(ws, "GetCurrentModelUID", {})).get("Data", {}).get("ModelUID")
        uid = uid_now or UID
        print("using uid", uid)
        await call(ws, "EditBegin", {"Silent": True})
        add = await call(ws, "AddRotationDeformer", {
            "ModelUID": uid,
            "Name": "bust_breath",
            "Id": "RotBust",
            "TargetObjectIds": ["WarpBust"],
            "Mode": "AsParent",
        })
        if add.get("Type") == "Error":
            add = await call(ws, "AddRotationDeformer", {
                "ModelUID": uid,
                "Name": "bust_breath",
                "Id": "RotBust",
                "TargetObjectIds": ["bust"],
                "ParentId": "WarpBust",
                "Mode": "AsParent",
            })
        for kv in (0.0, 1.0):
            await call(ws, "AddParameterKey", {
                "ModelUID": uid, "ObjectId": "RotBust",
                "ParameterId": "ParamBreath", "KeyValue": kv,
            })
        await call(ws, "EditRotationDeformer", {
            "ModelUID": uid, "Id": "RotBust", "Scale": 100.0, "Angle": 0.0,
            "IsExactMatch": True,
            "Parameters": [{"Id": "ParamBreath", "Value": 0.0}],
        })
        await call(ws, "EditRotationDeformer", {
            "ModelUID": uid, "Id": "RotBust", "Scale": 107.0, "Angle": 0.0,
            "IsExactMatch": True,
            "Parameters": [{"Id": "ParamBreath", "Value": 1.0}],
        })
        await call(ws, "EditEnd", {"Cancel": False})
        for v in (0.0, 1.0):
            obj = await call(ws, "GetObject", {
                "ModelUID": uid, "Id": "RotBust",
                "Parameters": [{"Id": "ParamBreath", "Value": v}],
            })
            d = (obj.get("Data") or {}).get("Data") or obj.get("Data") or {}
            print("breath", v, "scale", d.get("Scale") if isinstance(d, dict) else d)
        focus_cubism()
        await asyncio.sleep(0.3)
        for name, val in (("breath_0", 0.0), ("breath_1", 1.0), ("breath_0b", 0.0)):
            await call(ws, "SetParameterValues", {
                "ModelUID": uid,
                "Parameters": [{"Id": "ParamBreath", "Value": val}],
            })
            await asyncio.sleep(0.4)
            grab(name)


if __name__ == "__main__":
    asyncio.run(main())
