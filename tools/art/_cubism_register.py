"""One-shot Cubism 5.4 External App Integration handshake."""
from __future__ import annotations

import asyncio
import json
import os
import uuid
from pathlib import Path

import websockets

URI = "ws://127.0.0.1:22033"
TOKEN_PATH = Path.home() / ".cubism-mcp" / "token.txt"
APP_NAME = "cubism-mcp"
VERSION = "1.1.0"


def load_token() -> str:
    if TOKEN_PATH.is_file():
        return TOKEN_PATH.read_text(encoding="utf-8").strip()
    return ""


def save_token(token: str) -> None:
    TOKEN_PATH.parent.mkdir(parents=True, exist_ok=True)
    TOKEN_PATH.write_text(token, encoding="utf-8")


async def call(ws, method: str, data: dict | None = None, timeout: float = 8.0) -> dict:
    req_id = uuid.uuid4().hex
    payload = {
        "Version": VERSION,
        "RequestId": req_id,
        "Type": "Request",
        "Method": method,
        "Data": data or {},
    }
    print(f">>> {method} {json.dumps(data or {}, ensure_ascii=False)}")
    await ws.send(json.dumps(payload))
    deadline = asyncio.get_event_loop().time() + timeout
    while True:
        remaining = deadline - asyncio.get_event_loop().time()
        if remaining <= 0:
            return {"Type": "Timeout", "Method": method, "RequestId": req_id}
        try:
            raw = await asyncio.wait_for(ws.recv(), timeout=remaining)
        except asyncio.TimeoutError:
            return {"Type": "Timeout", "Method": method, "RequestId": req_id}
        msg = json.loads(raw)
        print(f"<<< {msg.get('Type')} {msg.get('Method')} {json.dumps(msg.get('Data'), ensure_ascii=False)[:800]}")
        if msg.get("RequestId") == req_id:
            return msg
        # ignore unmatched events / leftover responses


async def main() -> None:
    os.environ.setdefault("NO_PROXY", "localhost,127.0.0.1")
    token = load_token()
    print(f"token_file={TOKEN_PATH} exists={TOKEN_PATH.is_file()} token_len={len(token)}")
    async with websockets.connect(URI, open_timeout=5, close_timeout=2) as ws:
        print("connected")
        reg = await call(ws, "RegisterPlugin", {"Token": token, "Name": APP_NAME}, timeout=12)
        data = reg.get("Data") or {}
        new_token = data.get("Token") if isinstance(data, dict) else None
        if new_token:
            save_token(new_token)
            print(f"saved token len={len(new_token)}")
        allow = await call(ws, "GetIsApproval", {}, timeout=6)
        edit = await call(ws, "GetIsEditApproval", {}, timeout=6)
        allow_ok = bool((allow.get("Data") or {}).get("Result")) if allow.get("Type") == "Response" else False
        edit_ok = bool((edit.get("Data") or {}).get("Result")) if edit.get("Type") == "Response" else False
        print(f"ALLOW={allow_ok} EDIT={edit_ok}")
        if not allow_ok or not edit_ok:
            print("waiting up to 45s for Allow+Edit (check Cubism dialog / flashing icon)...")
            for i in range(15):
                await asyncio.sleep(3)
                allow = await call(ws, "GetIsApproval", {}, timeout=4)
                edit = await call(ws, "GetIsEditApproval", {}, timeout=4)
                allow_ok = bool((allow.get("Data") or {}).get("Result")) if allow.get("Type") == "Response" else False
                edit_ok = bool((edit.get("Data") or {}).get("Result")) if edit.get("Type") == "Response" else False
                print(f"poll {i+1}/15 ALLOW={allow_ok} EDIT={edit_ok}")
                if allow_ok and edit_ok:
                    break
        docs = await call(ws, "GetDocuments", {}, timeout=6)
        uid = await call(ws, "GetCurrentModelUID", {}, timeout=6)
        mode = await call(ws, "GetCurrentEditMode", {}, timeout=6)
        print("FINAL")
        print(json.dumps({
            "register_type": reg.get("Type"),
            "allow": allow_ok,
            "edit": edit_ok,
            "documents": docs.get("Data"),
            "model_uid": uid.get("Data"),
            "edit_mode": mode.get("Data"),
            "token_saved": TOKEN_PATH.is_file(),
        }, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    asyncio.run(main())
