"""Key RotHairRoot to hair-back / angle-X so the ponytail actually swings."""
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
            print(method, msg.get("Type"), json.dumps(msg.get("Data"), ensure_ascii=False)[:300])
            return msg


async def main():
    os.environ.setdefault("NO_PROXY", "localhost,127.0.0.1")
    async with websockets.connect(URI, open_timeout=5, close_timeout=2) as ws:
        await call(ws, "RegisterPlugin", {"Token": TOKEN, "Name": "cubism-mcp"})
        await call(ws, "EditBegin", {"Silent": True})
        keys = [
            ("RotHairRoot", "ParamHairBack", -1.0),
            ("RotHairRoot", "ParamHairBack", 0.0),
            ("RotHairRoot", "ParamHairBack", 1.0),
            ("RotHairRoot", "ParamAngleX", -30.0),
            ("RotHairRoot", "ParamAngleX", 0.0),
            ("RotHairRoot", "ParamAngleX", 30.0),
            ("WarpHairSide", "ParamHairSide", -1.0),
            ("WarpHairSide", "ParamHairSide", 0.0),
            ("WarpHairSide", "ParamHairSide", 1.0),
            ("WarpHairFront", "ParamHairFront", -1.0),
            ("WarpHairFront", "ParamHairFront", 0.0),
            ("WarpHairFront", "ParamHairFront", 1.0),
            ("WarpBodyBust", "ParamBreath", 0.0),
            ("WarpBodyBust", "ParamBreath", 1.0),
        ]
        for oid, pid, kv in keys:
            await call(ws, "AddParameterKey", {
                "ModelUID": UID, "ObjectId": oid, "ParameterId": pid, "KeyValue": kv,
            })
        # pose the rotation deformer at hair-back extremes
        poses = [
            ([{"Id": "ParamHairBack", "Value": 1.0}], 14.0),
            ([{"Id": "ParamHairBack", "Value": -1.0}], -14.0),
            ([{"Id": "ParamHairBack", "Value": 0.0}, {"Id": "ParamAngleX", "Value": 30.0}], 10.0),
            ([{"Id": "ParamHairBack", "Value": 0.0}, {"Id": "ParamAngleX", "Value": -30.0}], -10.0),
            ([{"Id": "ParamHairBack", "Value": 0.0}, {"Id": "ParamAngleX", "Value": 0.0}], 0.0),
        ]
        for params, angle in poses:
            await call(ws, "SetParameterValues", {"ModelUID": UID, "Parameters": params})
            await call(ws, "EditRotationDeformer", {
                "ModelUID": UID, "Id": "RotHairRoot", "Angle": angle,
            })
        await call(ws, "EditEnd", {"Cancel": False})
        keys_now = await call(ws, "GetParameterKeys", {"ModelUID": UID, "ObjectId": "RotHairRoot"})
        print("KEYS", json.dumps(keys_now.get("Data"), ensure_ascii=False, indent=2)[:2000])


if __name__ == "__main__":
    asyncio.run(main())
