"""Set hair param extremes and dump Cubism screenshots."""
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


async def call(ws, method, data=None, timeout=12.0):
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
            return {}
        msg = json.loads(await asyncio.wait_for(ws.recv(), timeout=remaining))
        if msg.get("RequestId") == req_id:
            return msg


def grab(name: str) -> None:
    r = wintypes.RECT()
    ctypes.windll.user32.GetWindowRect(HWND, ctypes.byref(r))
    img = ImageGrab.grab(bbox=(r.left, r.top, r.right, r.bottom))
    img.save(OUT / f"{name}.png")
    print("grab", name, img.size)


async def main():
    os.environ.setdefault("NO_PROXY", "localhost,127.0.0.1")
    async with websockets.connect(URI, open_timeout=5, close_timeout=2) as ws:
        await call(ws, "RegisterPlugin", {"Token": TOKEN, "Name": "cubism-mcp"})
        for name, val in [("sway_p", 1.0), ("sway_n", -1.0), ("sway_0", 0.0)]:
            await call(ws, "SetParameterValues", {
                "ModelUID": UID,
                "Parameters": [{"Id": "ParamHairBack", "Value": val}],
            })
            await asyncio.sleep(0.35)
            grab(name)
        await call(ws, "ClearParameterValues", {"ModelUID": UID})


if __name__ == "__main__":
    asyncio.run(main())
