"""Reconnect with saved token and print Allow/Edit/docs."""
from __future__ import annotations

import asyncio
import json
import os
import uuid
from pathlib import Path

import websockets

URI = "ws://127.0.0.1:22033"
TOKEN = Path.home().joinpath(".cubism-mcp", "token.txt").read_text(encoding="utf-8").strip()


async def call(ws, method, data=None, timeout=8.0):
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
        docs = await call(ws, "GetDocuments", {})
        uid = await call(ws, "GetCurrentModelUID", {})
        mode = await call(ws, "GetCurrentEditMode", {})
        print(json.dumps({
            "token_prefix": TOKEN[:8],
            "register": (reg.get("Data") or {}).get("Token", "")[:8],
            "allow": (allow.get("Data") or {}).get("Result"),
            "edit": (edit.get("Data") or {}).get("Result"),
            "documents": docs.get("Data"),
            "model_uid": uid.get("Data"),
            "edit_mode": mode.get("Data"),
        }, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    asyncio.run(main())
