"""Inspect bingren model and add hair/chest deformers."""
from __future__ import annotations

import asyncio
import json
import os
import uuid
from pathlib import Path

import websockets

URI = "ws://127.0.0.1:22033"
TOKEN = Path.home().joinpath(".cubism-mcp", "token.txt").read_text(encoding="utf-8").strip()
OUT = Path(r"F:\天命之子\tools\art\_cubism_rig.json")


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
            return {"Type": "Timeout", "Method": method}
        msg = json.loads(await asyncio.wait_for(ws.recv(), timeout=remaining))
        if msg.get("RequestId") == req_id:
            return msg


async def main():
    os.environ.setdefault("NO_PROXY", "localhost,127.0.0.1")
    async with websockets.connect(URI, open_timeout=5, close_timeout=2) as ws:
        reg = await call(ws, "RegisterPlugin", {"Token": TOKEN, "Name": "cubism-mcp"})
        allow = await call(ws, "GetIsApproval", {})
        edit = await call(ws, "GetIsEditApproval", {})
        uid_msg = await call(ws, "GetCurrentModelUID", {})
        uid = (uid_msg.get("Data") or {}).get("ModelUID")
        print("uid", uid, "allow", allow.get("Data"), "edit", edit.get("Data"))
        parts = await call(ws, "GetPartStructure", {"ModelUID": uid})
        defs = await call(ws, "GetDeformerStructure", {"ModelUID": uid})
        params = await call(ws, "GetParameterStructure", {"ModelUID": uid})
        selected = await call(ws, "GetSelectedObjects", {"ModelUID": uid})
        dump = {
            "uid": uid,
            "register": reg.get("Data"),
            "parts": parts.get("Data") or parts,
            "deformers": defs.get("Data") or defs,
            "params": params.get("Data") or params,
            "selected": selected.get("Data") or selected,
        }
        OUT.write_text(json.dumps(dump, ensure_ascii=False, indent=2), encoding="utf-8")
        print("wrote", OUT, "parts keys", list((parts.get("Data") or {}).keys())[:10])


if __name__ == "__main__":
    asyncio.run(main())
