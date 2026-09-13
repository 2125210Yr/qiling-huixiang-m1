"""Keep Cubism External API socket alive until Allow+Edit are granted."""
from __future__ import annotations

import asyncio
import json
import os
import sys
import time
import uuid
from pathlib import Path

import websockets

URI = "ws://127.0.0.1:22033"
TOKEN_PATH = Path.home() / ".cubism-mcp" / "token.txt"
STATUS_PATH = Path(r"F:\天命之子\tools\art\_cubism_wait_status.json")
APP_NAME = "cubism-mcp"
VERSION = "1.1.0"


def load_token() -> str:
    if TOKEN_PATH.is_file():
        return TOKEN_PATH.read_text(encoding="utf-8").strip()
    return ""


def save_token(token: str) -> None:
    TOKEN_PATH.parent.mkdir(parents=True, exist_ok=True)
    TOKEN_PATH.write_text(token, encoding="utf-8")


def write_status(payload: dict) -> None:
    payload["ts"] = time.strftime("%H:%M:%S")
    STATUS_PATH.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps(payload, ensure_ascii=False), flush=True)


async def call(ws, method: str, data: dict | None = None, timeout: float = 8.0) -> dict:
    req_id = uuid.uuid4().hex
    await ws.send(json.dumps({
        "Version": VERSION,
        "RequestId": req_id,
        "Type": "Request",
        "Method": method,
        "Data": data or {},
    }))
    deadline = asyncio.get_event_loop().time() + timeout
    while True:
        remaining = deadline - asyncio.get_event_loop().time()
        if remaining <= 0:
            return {"Type": "Timeout", "Method": method, "Data": {}}
        raw = await asyncio.wait_for(ws.recv(), timeout=remaining)
        msg = json.loads(raw)
        if msg.get("RequestId") == req_id:
            return msg


async def main() -> None:
    os.environ.setdefault("NO_PROXY", "localhost,127.0.0.1")
    token = load_token()
    write_status({"phase": "connecting", "allow": False, "edit": False})
    async with websockets.connect(URI, open_timeout=5, close_timeout=2) as ws:
        reg = await call(ws, "RegisterPlugin", {"Token": token, "Name": APP_NAME, "Path": sys.executable})
        data = reg.get("Data") or {}
        new_token = data.get("Token") if isinstance(data, dict) else None
        if new_token:
            save_token(new_token)
        write_status({
            "phase": "registered",
            "register_type": reg.get("Type"),
            "allow": False,
            "edit": False,
            "hint": "Cubism 文件 → 外部应用程序集成的设置 → cubism-mcp 勾选 允许 和 编辑",
        })
        while True:
            allow_msg = await call(ws, "GetIsApproval", {}, timeout=5)
            allow_ok = bool((allow_msg.get("Data") or {}).get("Result")) if allow_msg.get("Type") == "Response" else False
            edit_ok = False
            edit_err = None
            if allow_ok:
                edit_msg = await call(ws, "GetIsEditApproval", {}, timeout=5)
                if edit_msg.get("Type") == "Response":
                    edit_ok = bool((edit_msg.get("Data") or {}).get("Result"))
                else:
                    edit_err = (edit_msg.get("Data") or {}).get("ErrorType")
            write_status({
                "phase": "waiting" if not (allow_ok and edit_ok) else "ready",
                "allow": allow_ok,
                "edit": edit_ok,
                "edit_err": edit_err,
            })
            if allow_ok and edit_ok:
                docs = await call(ws, "GetDocuments", {}, timeout=5)
                uid = await call(ws, "GetCurrentModelUID", {}, timeout=5)
                write_status({
                    "phase": "ready",
                    "allow": True,
                    "edit": True,
                    "documents": docs.get("Data"),
                    "model_uid": uid.get("Data"),
                })
                # keep socket alive a bit so Cubism doesn't drop the grant
                await asyncio.sleep(30)
                return
            await asyncio.sleep(2)


if __name__ == "__main__":
    asyncio.run(main())
