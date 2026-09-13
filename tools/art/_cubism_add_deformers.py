"""Add hair/chest deformers on the imported 冰刃 model."""
from __future__ import annotations

import asyncio
import json
import os
import uuid
from pathlib import Path

import websockets

URI = "ws://127.0.0.1:22033"
TOKEN = Path.home().joinpath(".cubism-mcp", "token.txt").read_text(encoding="utf-8").strip()
UID = "1f53aa0dd37143fc9a71e33bdb372a87"


async def call(ws, method, data=None, timeout=30.0):
    req_id = uuid.uuid4().hex
    payload = {
        "Version": "1.1.0",
        "RequestId": req_id,
        "Type": "Request",
        "Method": method,
        "Data": data or {},
    }
    print(">>>", method, json.dumps(data or {}, ensure_ascii=False)[:240])
    await ws.send(json.dumps(payload))
    deadline = asyncio.get_event_loop().time() + timeout
    while True:
        remaining = deadline - asyncio.get_event_loop().time()
        if remaining <= 0:
            print("<<< TIMEOUT", method)
            return {"Type": "Timeout", "Method": method, "Data": {}}
        msg = json.loads(await asyncio.wait_for(ws.recv(), timeout=remaining))
        if msg.get("RequestId") == req_id:
            print("<<<", msg.get("Type"), method, json.dumps(msg.get("Data"), ensure_ascii=False)[:400])
            return msg


async def main():
    os.environ.setdefault("NO_PROXY", "localhost,127.0.0.1")
    async with websockets.connect(URI, open_timeout=5, close_timeout=2) as ws:
        await call(ws, "RegisterPlugin", {"Token": TOKEN, "Name": "cubism-mcp"})
        begin = await call(ws, "EditBegin", {"Silent": False})
        if begin.get("Type") == "Error":
            return
        steps = [
            ("AddRotationDeformer", {
                "ModelUID": UID,
                "Name": "hair_root",
                "Id": "RotHairRoot",
                "TargetObjectIds": ["hair_back"],
                "Mode": "AsParent",
            }),
            ("AddWarpDeformer", {
                "ModelUID": UID,
                "Name": "hair_back_warp",
                "Id": "WarpHairBack",
                "TargetObjectIds": ["hair_back"],
                "ParentId": "RotHairRoot",
                "Mode": "AsParent",
                "WarpDivH": 3,
                "WarpDivV": 5,
                "BezierDivH": 2,
                "BezierDivV": 3,
                "SnapCenter": False,
            }),
            ("AddWarpDeformer", {
                "ModelUID": UID,
                "Name": "hair_side_warp",
                "Id": "WarpHairSide",
                "TargetObjectIds": ["hair_side"],
                "Mode": "AsParent",
                "WarpDivH": 3,
                "WarpDivV": 5,
                "BezierDivH": 2,
                "BezierDivV": 3,
            }),
            ("AddWarpDeformer", {
                "ModelUID": UID,
                "Name": "hair_front_warp",
                "Id": "WarpHairFront",
                "TargetObjectIds": ["hair_front"],
                "Mode": "AsParent",
                "WarpDivH": 3,
                "WarpDivV": 3,
                "BezierDivH": 2,
                "BezierDivV": 2,
            }),
            ("AddWarpDeformer", {
                "ModelUID": UID,
                "Name": "body_bust",
                "Id": "WarpBodyBust",
                "TargetObjectIds": ["body"],
                "Mode": "AsParent",
                "WarpDivH": 3,
                "WarpDivV": 3,
                "BezierDivH": 2,
                "BezierDivV": 2,
            }),
        ]
        results = []
        failed = False
        for method, data in steps:
            await call(ws, "EditSendLog", {"Message": method})
            resp = await call(ws, method, data)
            results.append({"action": method, "type": resp.get("Type"), "data": resp.get("Data")})
            if resp.get("Type") == "Error":
                failed = True
                break
        end = await call(ws, "EditEnd", {"Cancel": failed})
        structure = await call(ws, "GetDeformerStructure", {"ModelUID": UID})
        print("FINAL", json.dumps({
            "failed": failed,
            "results": results,
            "edit_end": end.get("Data"),
            "deformers": structure.get("Data"),
        }, ensure_ascii=False, indent=2)[:4000])


if __name__ == "__main__":
    asyncio.run(main())
