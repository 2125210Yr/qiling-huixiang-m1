"""Flexible hair chain + small bust warp. No full-body cage."""
from __future__ import annotations

import asyncio
import json
import os
import uuid
from pathlib import Path

import websockets

URI = "ws://127.0.0.1:22033"
TOKEN = Path.home().joinpath(".cubism-mcp", "token.txt").read_text(encoding="utf-8").strip()
UID = "8d2b15166c01423ea58a5758c3d34e02"


async def call(ws, method, data=None, timeout=25.0):
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
            print(method, msg.get("Type"), json.dumps(msg.get("Data"), ensure_ascii=False)[:280])
            return msg


async def main():
    os.environ.setdefault("NO_PROXY", "localhost,127.0.0.1")
    async with websockets.connect(URI, open_timeout=5, close_timeout=2) as ws:
        await call(ws, "RegisterPlugin", {"Token": TOKEN, "Name": "cubism-mcp"})
        await call(ws, "EditBegin", {"Silent": True})
        steps = [
            ("AddRotationDeformer", {
                "ModelUID": UID, "Name": "hair_root", "Id": "RotHairRoot",
                "TargetObjectIds": ["hair_back"], "Mode": "AsParent",
            }),
            ("AddWarpDeformer", {
                "ModelUID": UID, "Name": "hair_back_mid", "Id": "WarpHairBack",
                "TargetObjectIds": ["hair_back"], "ParentId": "RotHairRoot",
                "Mode": "AsParent", "WarpDivH": 2, "WarpDivV": 5, "BezierDivH": 2, "BezierDivV": 3,
            }),
            ("AddWarpDeformer", {
                "ModelUID": UID, "Name": "hair_back_tip", "Id": "WarpHairBackTip",
                "TargetObjectIds": ["hair_back"], "ParentId": "WarpHairBack",
                "Mode": "AsParent", "WarpDivH": 2, "WarpDivV": 3, "BezierDivH": 2, "BezierDivV": 2,
            }),
            ("AddWarpDeformer", {
                "ModelUID": UID, "Name": "hair_side_mid", "Id": "WarpHairSide",
                "TargetObjectIds": ["hair_side"], "Mode": "AsParent",
                "WarpDivH": 2, "WarpDivV": 5, "BezierDivH": 2, "BezierDivV": 3,
            }),
            ("AddWarpDeformer", {
                "ModelUID": UID, "Name": "hair_front_warp", "Id": "WarpHairFront",
                "TargetObjectIds": ["hair_front"], "Mode": "AsParent",
                "WarpDivH": 3, "WarpDivV": 3, "BezierDivH": 2, "BezierDivV": 2,
            }),
            ("AddWarpDeformer", {
                "ModelUID": UID, "Name": "bust_warp", "Id": "WarpBust",
                "TargetObjectIds": ["bust"], "Mode": "AsParent",
                "WarpDivH": 2, "WarpDivV": 2, "BezierDivH": 2, "BezierDivV": 2,
            }),
        ]
        failed = False
        for method, data in steps:
            resp = await call(ws, method, data)
            if resp.get("Type") == "Error":
                failed = True
                break
        # keys + keyforms on the rotation (pass Parameters so it writes that key)
        if not failed:
            for kv in (-1.0, 0.0, 1.0):
                await call(ws, "AddParameterKey", {
                    "ModelUID": UID, "ObjectId": "RotHairRoot",
                    "ParameterId": "ParamHairBack", "KeyValue": kv,
                })
            for kv, ang in ((1.0, 16.0), (-1.0, -16.0), (0.0, 0.0)):
                await call(ws, "EditRotationDeformer", {
                    "ModelUID": UID, "Id": "RotHairRoot", "Angle": ang,
                    "IsExactMatch": True,
                    "Parameters": [{"Id": "ParamHairBack", "Value": kv}],
                })
            for kv in (-30.0, 0.0, 30.0):
                await call(ws, "AddParameterKey", {
                    "ModelUID": UID, "ObjectId": "RotHairRoot",
                    "ParameterId": "ParamAngleX", "KeyValue": kv,
                })
            for kv, ang in ((30.0, 10.0), (-30.0, -10.0), (0.0, 0.0)):
                await call(ws, "EditRotationDeformer", {
                    "ModelUID": UID, "Id": "RotHairRoot", "Angle": ang,
                    "IsExactMatch": True,
                    "Parameters": [{"Id": "ParamHairBack", "Value": 0.0}, {"Id": "ParamAngleX", "Value": kv}],
                })
            for oid, pid in (("WarpHairSide", "ParamHairSide"), ("WarpHairFront", "ParamHairFront"),
                             ("WarpBust", "ParamBreath")):
                for kv in ((-1.0, 1.0) if pid != "ParamBreath" else (0.0, 1.0)):
                    await call(ws, "AddParameterKey", {
                        "ModelUID": UID, "ObjectId": oid, "ParameterId": pid, "KeyValue": kv,
                    })
        await call(ws, "EditEnd", {"Cancel": failed})
        defs = await call(ws, "GetDeformerStructure", {"ModelUID": UID})
        rot = await call(ws, "GetObject", {
            "ModelUID": UID, "Id": "RotHairRoot",
            "Parameters": [{"Id": "ParamHairBack", "Value": 1.0}],
        })
        print("DEFS", json.dumps(defs.get("Data"), ensure_ascii=False, indent=2)[:2500])
        print("ROT@1", json.dumps(rot.get("Data"), ensure_ascii=False)[:500])


if __name__ == "__main__":
    asyncio.run(main())
