"""Set up a Cubism warp deformer on the uncut body+bust (not a PS cut)."""
from __future__ import annotations

import asyncio
import json
import os
import uuid
from pathlib import Path

import websockets

URI = "ws://127.0.0.1:22033"
TOKEN = Path.home().joinpath(".cubism-mcp", "token.txt").read_text(encoding="utf-8").strip()


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
            blob = json.dumps(msg.get("Data"), ensure_ascii=False)
            print(method, msg.get("Type"), blob[:800])
            return msg


async def main():
    os.environ.setdefault("NO_PROXY", "localhost,127.0.0.1")
    async with websockets.connect(URI, open_timeout=5, close_timeout=2) as ws:
        await call(ws, "RegisterPlugin", {"Token": TOKEN, "Name": "cubism-mcp"})
        uid = (await call(ws, "GetCurrentModelUID", {})).get("Data", {}).get("ModelUID")
        print("uid", uid)
        await call(ws, "GetDeformerStructure", {"ModelUID": uid})
        await call(ws, "GetPartStructure", {"ModelUID": uid})
        for oid in ("WarpBust", "RotBust", "bust", "body"):
            await call(ws, "GetObject", {"ModelUID": uid, "Id": oid})
        await call(ws, "EditBegin", {"Silent": True})
        # denser warp on the existing bust deformer; also try parenting body
        await call(ws, "EditWarpDeformer", {
            "ModelUID": uid, "Id": "WarpBust",
            "WarpDivH": 5, "WarpDivV": 5,
            "BezierDivH": 3, "BezierDivV": 3,
        })
        add = await call(ws, "AddWarpDeformer", {
            "ModelUID": uid, "Name": "chest_warp", "Id": "WarpChest",
            "TargetObjectIds": ["body", "bust"],
            "Mode": "AsParent",
            "WarpDivH": 5, "WarpDivV": 5,
            "BezierDivH": 3, "BezierDivV": 3,
        })
        await call(ws, "EditEnd", {"Cancel": False})
        await call(ws, "GetDeformerStructure", {"ModelUID": uid})
        await call(ws, "GetObject", {"ModelUID": uid, "Id": "WarpChest"})
        await call(ws, "GetObject", {"ModelUID": uid, "Id": "WarpBust"})


if __name__ == "__main__":
    asyncio.run(main())
