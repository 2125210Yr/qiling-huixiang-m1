"""Undo the full-body WarpChest cage. Keep a chest-only warp on bust."""
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
            print(method, msg.get("Type"), json.dumps(msg.get("Data"), ensure_ascii=False)[:500])
            return msg


async def main():
    os.environ.setdefault("NO_PROXY", "localhost,127.0.0.1")
    async with websockets.connect(URI, open_timeout=5, close_timeout=2) as ws:
        await call(ws, "RegisterPlugin", {"Token": TOKEN, "Name": "cubism-mcp"})
        uid = (await call(ws, "GetCurrentModelUID", {})).get("Data", {}).get("ModelUID")
        await call(ws, "EditBegin", {"Silent": True})
        # body should not sit in a full-canvas cage
        await call(ws, "EditArtMesh", {
            "ModelUID": uid, "Id": "body",
            "ParentDeformerId": "%Root",
        })
        await call(ws, "EditArtMesh", {
            "ModelUID": uid, "Id": "bust",
            "ParentDeformerId": "WarpBust",
        })
        await call(ws, "DeleteObject", {"ModelUID": uid, "Id": "WarpChest"})
        await call(ws, "EditEnd", {"Cancel": False})
        await call(ws, "GetDeformerStructure", {"ModelUID": uid})
        for oid in ("body", "bust", "WarpBust", "WarpChest"):
            await call(ws, "GetObject", {"ModelUID": uid, "Id": oid})


if __name__ == "__main__":
    asyncio.run(main())
