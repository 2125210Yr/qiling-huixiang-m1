"""Plan (and optionally apply) the ElevatorRed hair / breath / skirt rig via Cubism MCP.

Safe by default. With no flags this prints the plan and opens no socket.

    python tools/art/elevator_red_cubism_plan.py             # dry run, offline
    python tools/art/elevator_red_cubism_plan.py --probe      # connect, introspect, mutate nothing
    python tools/art/elevator_red_cubism_plan.py --probe --probe-writes
    python tools/art/elevator_red_cubism_plan.py --apply      # requires reachable editor + approvals

The Cubism MCP surface used by the sibling _cubism_*.py scripts has no PSD import and
no document/model creation: those go through the Win32 GUI driver (_cubism_open_psd.py,
_cubism_reimport.py). So "create ElevatorRed" is not something this script can do over
the socket -- it locates an already-open ElevatorRed document and refuses to touch
anything else unless --force-uid is passed. --probe checks whether a newer editor build
has grown import/create/parameter methods.

Parameter IDs follow art/live2d-lab/elevator-red/rig/parameters.md and
rig/physics_groups.json, which are canonical. elevator_red.cdi3.json is an earlier,
coarser draft (ParamHairBack vs ParamHairBack01..03) and is used here only for ArtMesh IDs.
"""
from __future__ import annotations

import argparse
import asyncio
import json
import socket
import sys
import uuid
from pathlib import Path

HOST = "127.0.0.1"
PORT = 22033
URI = f"ws://{HOST}:{PORT}"
VERSION = "1.1.0"
APP_NAME = "cubism-mcp"
TOKEN_PATH = Path.home() / ".cubism-mcp" / "token.txt"

MODEL_NAME = "ElevatorRed"
# Document titles the editor may show for this character.
MODEL_ALIASES = ("elevatorred", "elevator_red", "elevator-red", "电梯红")
PROJECT = Path(__file__).resolve().parents[2] / "art" / "live2d-lab" / "elevator-red"

# ArtMesh IDs from elevator_red.cdi3.json.
MESH_HAIR_BACK = "ArtMeshHairBack"
MESH_HAIR_TIP_L = "ArtMeshHairTipL"
MESH_HAIR_TIP_R = "ArtMeshHairTipR"
MESH_HAIR_SIDE_L = "ArtMeshHairSideL"
MESH_HAIR_SIDE_R = "ArtMeshHairSideR"
MESH_HAIR_FRONT = "ArtMeshHairFront"
MESH_CHEST = "ArtMeshChest"
MESH_DRESS = "ArtMeshDress"
MESH_DRESS_HEM = "ArtMeshDressHem"
MESH_DRESS_SLIT = "ArtMeshDressSlit"

# (Id, Name, Min, Default, Max, group) -- hair / breath / skirt only.
# Every one is an increment on the still frame, so Default is 0 (parameters.md).
PARAMS: tuple[tuple[str, str, float, float, float, str], ...] = (
    ("ParamHairFront", "Front Hair Sway", -1.0, 0.0, 1.0, "hair"),
    ("ParamHairSideL", "Side Hair Root L", -1.0, 0.0, 1.0, "hair"),
    ("ParamHairSideLSub", "Side Hair Tip L", -1.0, 0.0, 1.0, "hair"),
    ("ParamHairSideR", "Side Hair Root R", -1.0, 0.0, 1.0, "hair"),
    ("ParamHairSideRSub", "Side Hair Tip R", -1.0, 0.0, 1.0, "hair"),
    ("ParamHairBack01", "Back Hair Root", -1.0, 0.0, 1.0, "hair"),
    ("ParamHairBack02", "Back Hair Mid", -1.0, 0.0, 1.0, "hair"),
    ("ParamHairBack03", "Back Hair Tip", -1.0, 0.0, 1.0, "hair"),
    ("ParamBreath", "Breath", 0.0, 0.0, 1.0, "breath"),
    ("ParamBustLZ", "Bust Rot L", -1.0, 0.0, 1.0, "breath"),
    ("ParamBustRZ", "Bust Rot R", -1.0, 0.0, 1.0, "breath"),
    ("ParamBustLY", "Bust Drop L", -0.6, 0.0, 0.6, "breath"),
    ("ParamBustRY", "Bust Drop R", -0.6, 0.0, 0.6, "breath"),
    ("ParamSlitOpen", "Slit Open", -1.0, 0.0, 1.0, "skirt"),
    ("ParamSkirtSlitPanel", "Slit Panel Root", -1.0, 0.0, 1.0, "skirt"),
    ("ParamSkirtSlitPanelSub", "Slit Panel Tip", -1.0, 0.0, 1.0, "skirt"),
    ("ParamSkirtPinL", "Skirt Pinned L", -1.0, 0.0, 1.0, "skirt"),
    ("ParamSkirtBackX", "Skirt Back X", -1.0, 0.0, 1.0, "skirt"),
    ("ParamSkirtHemFlutter", "Hem Flutter", -1.0, 0.0, 1.0, "skirt"),
)

# Deformers, parents first. Rotation deformers carry angle; warps carry a cage.
# hair_back hangs off the root, never off the head: the back hair is pressed against
# the mirror and following the head 1:1 pushes it through the wall (parameters.md).
DEFORMERS: tuple[dict, ...] = (
    {
        "kind": "rotation", "id": "RotHairBack01", "name": "hair_back_root",
        "targets": [MESH_HAIR_BACK], "parent": None, "group": "hair",
    },
    {
        "kind": "warp", "id": "WarpHairBack02", "name": "hair_back_mid",
        "targets": [MESH_HAIR_BACK], "parent": "RotHairBack01", "group": "hair",
        "div": (2, 5), "bezier": (2, 3),
    },
    {
        "kind": "warp", "id": "WarpHairBack03", "name": "hair_back_tip",
        "targets": [MESH_HAIR_BACK, MESH_HAIR_TIP_L, MESH_HAIR_TIP_R],
        "parent": "WarpHairBack02", "group": "hair",
        "div": (2, 3), "bezier": (2, 2),
    },
    {
        "kind": "rotation", "id": "RotHairSideL", "name": "hair_side_l_root",
        "targets": [MESH_HAIR_SIDE_L], "parent": None, "group": "hair",
    },
    {
        "kind": "warp", "id": "WarpHairSideLSub", "name": "hair_side_l_tip",
        "targets": [MESH_HAIR_SIDE_L], "parent": "RotHairSideL", "group": "hair",
        "div": (2, 4), "bezier": (2, 2),
    },
    {
        "kind": "rotation", "id": "RotHairSideR", "name": "hair_side_r_root",
        "targets": [MESH_HAIR_SIDE_R], "parent": None, "group": "hair",
    },
    {
        "kind": "warp", "id": "WarpHairSideRSub", "name": "hair_side_r_tip",
        "targets": [MESH_HAIR_SIDE_R], "parent": "RotHairSideR", "group": "hair",
        "div": (2, 4), "bezier": (2, 2),
    },
    {
        "kind": "warp", "id": "WarpHairFront", "name": "hair_front",
        "targets": [MESH_HAIR_FRONT], "parent": None, "group": "hair",
        "div": (3, 3), "bezier": (2, 2),
    },
    {
        "kind": "warp", "id": "WarpChestBreath", "name": "chest_breath",
        "targets": [MESH_CHEST], "parent": None, "group": "breath",
        "div": (2, 2), "bezier": (2, 2),
    },
    {
        "kind": "warp", "id": "WarpBustBounce", "name": "bust_bounce",
        "targets": [MESH_CHEST], "parent": "WarpChestBreath", "group": "breath",
        "div": (2, 2), "bezier": (2, 2),
    },
    {
        "kind": "warp", "id": "WarpSkirtBack", "name": "skirt_back",
        "targets": [MESH_DRESS], "parent": None, "group": "skirt",
        "div": (3, 3), "bezier": (2, 2),
    },
    {
        "kind": "warp", "id": "WarpSkirtHem", "name": "skirt_hem",
        "targets": [MESH_DRESS_HEM], "parent": "WarpSkirtBack", "group": "skirt",
        "div": (4, 3), "bezier": (3, 2),
    },
    {
        "kind": "rotation", "id": "RotSkirtSlit", "name": "skirt_slit_panel",
        "targets": [MESH_DRESS_SLIT], "parent": "WarpSkirtBack", "group": "skirt",
    },
    {
        "kind": "warp", "id": "WarpSkirtSlitSub", "name": "skirt_slit_tip",
        "targets": [MESH_DRESS_SLIT], "parent": "RotSkirtSlit", "group": "skirt",
        "div": (2, 4), "bezier": (2, 2),
    },
)

# Which parameter each deformer is keyed on, and the keyform extremes.
# Rotation angles are degrees; warp extremes are placeholders that the artist tunes --
# the point of the script is to create the keys so there is something to tune.
KEYFORMS: tuple[dict, ...] = (
    {"object": "RotHairBack01", "param": "ParamHairBack01", "keys": (-1.0, 0.0, 1.0),
     "angles": {-1.0: -9.0, 0.0: 0.0, 1.0: 9.0}},
    {"object": "WarpHairBack02", "param": "ParamHairBack02", "keys": (-1.0, 0.0, 1.0)},
    {"object": "WarpHairBack03", "param": "ParamHairBack03", "keys": (-1.0, 0.0, 1.0)},
    {"object": "RotHairSideL", "param": "ParamHairSideL", "keys": (-1.0, 0.0, 1.0),
     "angles": {-1.0: -12.0, 0.0: 0.0, 1.0: 12.0}},
    {"object": "WarpHairSideLSub", "param": "ParamHairSideLSub", "keys": (-1.0, 0.0, 1.0)},
    {"object": "RotHairSideR", "param": "ParamHairSideR", "keys": (-1.0, 0.0, 1.0),
     "angles": {-1.0: -13.0, 0.0: 0.0, 1.0: 13.0}},
    {"object": "WarpHairSideRSub", "param": "ParamHairSideRSub", "keys": (-1.0, 0.0, 1.0)},
    {"object": "WarpHairFront", "param": "ParamHairFront", "keys": (-1.0, 0.0, 1.0)},
    {"object": "WarpChestBreath", "param": "ParamBreath", "keys": (0.0, 1.0)},
    {"object": "WarpBustBounce", "param": "ParamBustLZ", "keys": (-1.0, 0.0, 1.0)},
    {"object": "WarpBustBounce", "param": "ParamBustLY", "keys": (-0.6, 0.0, 0.6)},
    {"object": "WarpSkirtBack", "param": "ParamSkirtBackX", "keys": (-1.0, 0.0, 1.0)},
    {"object": "WarpSkirtHem", "param": "ParamSkirtHemFlutter", "keys": (-1.0, 0.0, 1.0)},
    {"object": "RotSkirtSlit", "param": "ParamSkirtSlitPanel", "keys": (-1.0, 0.0, 1.0),
     "angles": {-1.0: -14.0, 0.0: 0.0, 1.0: 14.0}},
    {"object": "RotSkirtSlit", "param": "ParamSlitOpen", "keys": (-1.0, 0.0, 1.0)},
    {"object": "WarpSkirtSlitSub", "param": "ParamSkirtSlitPanelSub", "keys": (-1.0, 0.0, 1.0)},
)

# Methods the sibling scripts have exercised against a live editor.
KNOWN_METHODS = (
    "RegisterPlugin", "GetIsApproval", "GetIsEditApproval", "GetDocuments",
    "GetCurrentModelUID", "GetCurrentEditMode", "GetPartStructure",
    "GetDeformerStructure", "GetParameterStructure", "GetSelectedObjects",
    "GetObject", "GetParameterKeys", "EditBegin", "EditEnd", "EditSendLog",
    "AddRotationDeformer", "AddWarpDeformer", "EditRotationDeformer",
    "EditWarpDeformer", "EditArtMesh", "DeleteObject", "AddParameterKey",
    "SetParameterValues", "ClearParameterValues",
)

# Read-only introspection, safe to fire with empty Data.
PROBE_READ = (
    "GetApiVersion", "GetCapabilities", "GetMethods", "GetSupportedMethods",
    "GetDocuments", "GetCurrentModelUID", "GetCurrentEditMode",
)

# Might mutate. Only fired inside EditBegin/EditEnd(Cancel=True) under --probe-writes.
PROBE_WRITE = (
    "AddParameter", "CreateParameter", "AddParameterGroup", "SetParameterStructure",
    "EditParameter", "DeleteParameter",
    "CreateModel", "NewModel", "OpenDocument", "ImportPsd", "ImportPSD",
    "ImportLayeredImage", "SetCurrentModel", "SetCurrentDocument", "ActivateDocument",
    "SaveDocument", "SaveAs", "ExportModel",
)


def load_token() -> str:
    """Read the handshake token. Never printed -- only its presence and length."""
    if TOKEN_PATH.is_file():
        return TOKEN_PATH.read_text(encoding="utf-8").strip()
    return ""


def port_open(timeout: float = 1.5) -> tuple[bool, str]:
    try:
        with socket.create_connection((HOST, PORT), timeout=timeout):
            return True, "listening"
    except OSError as exc:
        return False, f"{type(exc).__name__}: {exc}"


def reachability() -> dict:
    token = load_token()
    open_, detail = port_open()
    return {
        "uri": URI,
        "port_open": open_,
        "port_detail": detail,
        "token_present": bool(token),
        "token_len": len(token),
        "token_path": str(TOKEN_PATH),
        "project_dir": str(PROJECT),
        "project_exists": PROJECT.is_dir(),
        "layers_cut": bool(list((PROJECT / "layers").glob("*.png"))) if (PROJECT / "layers").is_dir() else False,
        "reachable": open_ and bool(token),
    }


def plan_steps() -> list[tuple[str, str, dict]]:
    """The exact (phase, Method, Data) sequence --apply would send, in order."""
    steps: list[tuple[str, str, dict]] = []
    uid = "<ModelUID>"
    steps.append(("handshake", "RegisterPlugin", {"Token": "<token>", "Name": APP_NAME}))
    steps.append(("handshake", "GetIsApproval", {}))
    steps.append(("handshake", "GetIsEditApproval", {}))
    steps.append(("handshake", "GetDocuments", {}))
    steps.append(("handshake", "GetCurrentModelUID", {}))
    steps.append(("inspect", "GetParameterStructure", {"ModelUID": uid}))
    steps.append(("inspect", "GetPartStructure", {"ModelUID": uid}))
    steps.append(("inspect", "GetDeformerStructure", {"ModelUID": uid}))
    steps.append(("edit", "EditBegin", {"Silent": True}))

    for d in DEFORMERS:
        steps.append(("edit", "EditSendLog", {"Message": f"ElevatorRed {d['group']}: {d['id']}"}))
        if d["kind"] == "rotation":
            data = {
                "ModelUID": uid, "Name": d["name"], "Id": d["id"],
                "TargetObjectIds": d["targets"], "Mode": "AsParent",
            }
            if d["parent"]:
                data["ParentId"] = d["parent"]
            steps.append(("edit", "AddRotationDeformer", data))
        else:
            dh, dv = d["div"]
            bh, bv = d["bezier"]
            data = {
                "ModelUID": uid, "Name": d["name"], "Id": d["id"],
                "TargetObjectIds": d["targets"], "Mode": "AsParent",
                "WarpDivH": dh, "WarpDivV": dv, "BezierDivH": bh, "BezierDivV": bv,
                "SnapCenter": False,
            }
            if d["parent"]:
                data["ParentId"] = d["parent"]
            steps.append(("edit", "AddWarpDeformer", data))

    for kf in KEYFORMS:
        for kv in kf["keys"]:
            steps.append(("edit", "AddParameterKey", {
                "ModelUID": uid, "ObjectId": kf["object"],
                "ParameterId": kf["param"], "KeyValue": kv,
            }))
        angles = kf.get("angles")
        if angles:
            for kv, angle in angles.items():
                steps.append(("edit", "EditRotationDeformer", {
                    "ModelUID": uid, "Id": kf["object"], "Angle": angle,
                    "IsExactMatch": True,
                    "Parameters": [{"Id": kf["param"], "Value": kv}],
                }))

    steps.append(("edit", "EditEnd", {"Cancel": False}))
    steps.append(("verify", "GetDeformerStructure", {"ModelUID": uid}))
    for kf in KEYFORMS:
        steps.append(("verify", "GetParameterKeys", {"ModelUID": uid, "ObjectId": kf["object"]}))
    steps.append(("verify", "ClearParameterValues", {"ModelUID": uid}))
    return steps


def print_plan(verbose: bool) -> None:
    info = reachability()
    print("=== ElevatorRed Cubism rig plan (DRY RUN -- nothing sent) ===")
    print(json.dumps(info, ensure_ascii=False, indent=2))
    print()

    meshes = sorted({m for d in DEFORMERS for m in d["targets"]})
    by_group: dict[str, list[str]] = {}
    for pid, _n, _mn, _d, _mx, group in PARAMS:
        by_group.setdefault(group, []).append(pid)
    print(f"model            : {MODEL_NAME}")
    print(f"parameters       : {len(PARAMS)}")
    for group, ids in by_group.items():
        print(f"  {group:<8}: {', '.join(ids)}")
    print(f"deformers        : {len(DEFORMERS)}")
    for d in DEFORMERS:
        print(f"  {d['kind']:<8} {d['id']:<20} parent={d['parent'] or 'root':<16} -> {', '.join(d['targets'])}")
    print(f"artmeshes needed : {len(meshes)}")
    print(f"  {', '.join(meshes)}")

    steps = plan_steps()
    counts: dict[str, int] = {}
    for _phase, method, _data in steps:
        counts[method] = counts.get(method, 0) + 1
    print(f"requests         : {len(steps)}")
    for method, n in sorted(counts.items(), key=lambda kv: -kv[1]):
        flag = "" if method in KNOWN_METHODS else "  (UNVERIFIED)"
        print(f"  {n:>4}x {method}{flag}")

    print()
    print("PREREQUISITES not doable over this socket:")
    print("  1. Cut the PSD. art/live2d-lab/elevator-red/layers/ is empty; see psd_cut_plan.md.")
    print("  2. Import the PSD and create the model in the GUI. The MCP surface exposed by")
    print("     the editor has no import/create method -- use _cubism_open_psd.py (Ctrl+O +")
    print("     native dialog) or _cubism_reimport.py to replace an existing import.")
    print("  3. Create the 19 parameters above. No AddParameter method is known; the standard")
    print("     PSD-import template ships only 27 stock params (ParamHairBack, not")
    print("     ParamHairBack01..03; no skirt params at all). Run --probe --probe-writes")
    print("     against a live editor to see whether this build grew one.")
    print("  4. ArtMeshChest is one mesh, so bust L/R share WarpBustBounce. Split the chest")
    print("     mesh if independent L/R rotation is wanted (parameters.md forbids squeeze).")
    print("  5. ParamSkirtPinL has no dedicated mesh in the cdi3 draft; the standing-leg")
    print("     skirt panel needs its own drawable before that deformer can be added.")

    if verbose:
        print()
        print("--- full request sequence ---")
        for i, (phase, method, data) in enumerate(steps, 1):
            print(f"{i:>4} [{phase}] {method} {json.dumps(data, ensure_ascii=False)}")

    print()
    if info["reachable"]:
        print("Cubism looks reachable. Next: --probe, then --apply once prerequisites are met.")
    else:
        print("Cubism is NOT reachable, so this stays a dry run.")


async def call(ws, method: str, data: dict | None = None, timeout: float = 10.0) -> dict:
    req_id = uuid.uuid4().hex
    await ws.send(json.dumps({
        "Version": VERSION,
        "RequestId": req_id,
        "Type": "Request",
        "Method": method,
        "Data": data or {},
    }))
    loop = asyncio.get_event_loop()
    deadline = loop.time() + timeout
    while True:
        remaining = deadline - loop.time()
        if remaining <= 0:
            return {"Type": "Timeout", "Method": method, "Data": {}}
        try:
            raw = await asyncio.wait_for(ws.recv(), timeout=remaining)
        except asyncio.TimeoutError:
            return {"Type": "Timeout", "Method": method, "Data": {}}
        msg = json.loads(raw)
        if msg.get("RequestId") == req_id:
            return msg


def classify(resp: dict) -> str:
    """Does the method exist, judging by the response?"""
    kind = resp.get("Type")
    if kind == "Response":
        return "present"
    if kind == "Timeout":
        return "unknown(timeout)"
    blob = json.dumps(resp.get("Data") or {}, ensure_ascii=False).lower()
    for needle in ("unknown method", "not supported", "unsupported", "no such method",
                   "invalid method", "notfound", "not found"):
        if needle in blob:
            return "absent"
    return "present(bad-args)"


async def connect():
    try:
        import websockets
    except ImportError:
        print("websockets is not installed: python -m pip install websockets", file=sys.stderr)
        raise SystemExit(2)
    return websockets.connect(URI, open_timeout=5, close_timeout=2)


def find_model(docs: dict, force_uid: str | None) -> tuple[str | None, list[dict]]:
    """Pick the ElevatorRed document out of GetDocuments, tolerating shape drift."""
    data = docs.get("Data") or {}
    entries: list[dict] = []
    if isinstance(data, dict):
        for key in ("Documents", "Models", "Items", "List"):
            got = data.get(key)
            if isinstance(got, list):
                entries = [e for e in got if isinstance(e, dict)]
                break
    elif isinstance(data, list):
        entries = [e for e in data if isinstance(e, dict)]
    if force_uid:
        return force_uid, entries
    for entry in entries:
        label = " ".join(
            str(entry.get(k, "")) for k in ("Name", "Title", "FileName", "Path", "DocumentName")
        ).lower()
        if any(alias in label for alias in MODEL_ALIASES):
            for k in ("ModelUID", "Uid", "UID", "Id"):
                if entry.get(k):
                    return str(entry[k]), entries
    return None, entries


async def probe(probe_writes: bool) -> int:
    info = reachability()
    print(json.dumps(info, ensure_ascii=False, indent=2))
    if not info["port_open"]:
        print(f"\nUNREACHABLE: nothing is listening on {URI}.")
        print("Cubism Editor hosts that socket itself; start it and enable")
        print("File > Preferences > External App Integration, then re-probe.")
        return 3
    if not info["token_present"]:
        print("\nNo token. Run _cubism_register.py first to do the approval handshake.")
        return 3

    token = load_token()
    async with await connect() as ws:
        reg = await call(ws, "RegisterPlugin", {"Token": token, "Name": APP_NAME}, timeout=12)
        allow = await call(ws, "GetIsApproval", {}, timeout=6)
        edit = await call(ws, "GetIsEditApproval", {}, timeout=6)
        allow_ok = bool((allow.get("Data") or {}).get("Result"))
        edit_ok = bool((edit.get("Data") or {}).get("Result"))
        print(f"\nregister={reg.get('Type')} ALLOW={allow_ok} EDIT={edit_ok}")

        docs = await call(ws, "GetDocuments", {}, timeout=6)
        uid_msg = await call(ws, "GetCurrentModelUID", {}, timeout=6)
        current_uid = (uid_msg.get("Data") or {}).get("ModelUID")
        target_uid, entries = find_model(docs, None)
        print(f"open documents={len(entries)} current_uid={current_uid}")
        print(f"{MODEL_NAME} document uid={target_uid}")

        print("\n--- read-only method probe ---")
        for method in PROBE_READ:
            resp = await call(ws, method, {}, timeout=5)
            print(f"  {method:<24} {classify(resp)}")

        if probe_writes:
            print("\n--- write method probe (inside EditBegin, rolled back) ---")
            begin = await call(ws, "EditBegin", {"Silent": True}, timeout=8)
            if begin.get("Type") == "Error":
                print("  EditBegin refused; skipping. Grant edit approval in the editor.")
            else:
                try:
                    for method in PROBE_WRITE:
                        resp = await call(ws, method, {}, timeout=5)
                        print(f"  {method:<24} {classify(resp)}")
                finally:
                    await call(ws, "EditEnd", {"Cancel": True}, timeout=8)
                    print("  EditEnd(Cancel=True) -- probe rolled back")
        else:
            print("\n(pass --probe-writes to test import / parameter-creation methods)")

        if target_uid:
            params = await call(ws, "GetParameterStructure", {"ModelUID": target_uid}, timeout=8)
            existing = set(_collect_param_ids(params.get("Data") or {}))
            wanted = {p[0] for p in PARAMS}
            print(f"\nparams on model: {len(existing)}")
            print(f"plan needs      : {len(wanted)}")
            missing = sorted(wanted - existing)
            print(f"missing         : {len(missing)}")
            if missing:
                print("  " + ", ".join(missing))
    return 0


def _collect_param_ids(data) -> list[str]:
    out: list[str] = []

    def walk(node) -> None:
        if isinstance(node, dict):
            if node.get("EntryType") == "Parameter" and node.get("Id"):
                out.append(node["Id"])
            for value in node.values():
                walk(value)
        elif isinstance(node, list):
            for value in node:
                walk(value)

    walk(data)
    return out


async def apply(force_uid: str | None, yes: bool) -> int:
    info = reachability()
    if not info["reachable"]:
        print(json.dumps(info, ensure_ascii=False, indent=2))
        print(f"\nREFUSING --apply: Cubism is not reachable on {URI}.")
        return 3

    token = load_token()
    async with await connect() as ws:
        await call(ws, "RegisterPlugin", {"Token": token, "Name": APP_NAME}, timeout=12)
        allow = await call(ws, "GetIsApproval", {}, timeout=6)
        edit = await call(ws, "GetIsEditApproval", {}, timeout=6)
        if not (bool((allow.get("Data") or {}).get("Result"))
                and bool((edit.get("Data") or {}).get("Result"))):
            print("REFUSING --apply: allow/edit approval missing. Run _cubism_register.py.")
            return 4

        docs = await call(ws, "GetDocuments", {}, timeout=6)
        uid, entries = find_model(docs, force_uid)
        if not uid:
            print(f"REFUSING --apply: no open document matching {MODEL_NAME}.")
            print(f"  open documents: {json.dumps(entries, ensure_ascii=False)[:800]}")
            print("  Import the PSD first (see --dry-run prerequisites), or pass --force-uid.")
            return 5
        print(f"target model uid={uid}")

        params = await call(ws, "GetParameterStructure", {"ModelUID": uid}, timeout=8)
        existing = set(_collect_param_ids(params.get("Data") or {}))
        missing = sorted({p[0] for p in PARAMS} - existing)
        if missing:
            print(f"REFUSING --apply: {len(missing)} parameters do not exist on the model:")
            print("  " + ", ".join(missing))
            print("  Create them in the editor's parameter palette, then re-run.")
            print("  Keying a nonexistent parameter silently produces a dead rig.")
            return 5

        if not yes:
            print(f"\nWould send {len(plan_steps())} requests to model {uid}.")
            print("Re-run with --yes to actually write. Nothing sent.")
            return 0

        await call(ws, "EditBegin", {"Silent": True}, timeout=10)
        failed: str | None = None
        for phase, method, data in plan_steps():
            if phase != "edit" or method in ("EditBegin", "EditEnd"):
                continue
            payload = {**data}
            if payload.get("ModelUID") == "<ModelUID>":
                payload["ModelUID"] = uid
            resp = await call(ws, method, payload, timeout=25)
            if resp.get("Type") == "Error":
                failed = f"{method} {json.dumps(resp.get('Data'), ensure_ascii=False)[:300]}"
                print(f"ERROR {failed}")
                break
        await call(ws, "EditEnd", {"Cancel": bool(failed)}, timeout=15)
        print("rolled back" if failed else "committed")

        structure = await call(ws, "GetDeformerStructure", {"ModelUID": uid}, timeout=10)
        print(json.dumps(structure.get("Data"), ensure_ascii=False, indent=2)[:3000])
        await call(ws, "ClearParameterValues", {"ModelUID": uid}, timeout=6)
    return 1 if failed else 0


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--probe", action="store_true", help="connect and introspect; mutates nothing")
    ap.add_argument("--probe-writes", action="store_true",
                    help="with --probe, test mutating methods inside a rolled-back edit")
    ap.add_argument("--apply", action="store_true", help="build the rig on the open ElevatorRed model")
    ap.add_argument("--yes", action="store_true", help="with --apply, actually write")
    ap.add_argument("--force-uid", default=None, help="target this ModelUID instead of matching by name")
    ap.add_argument("-v", "--verbose", action="store_true", help="with the dry run, print every request")
    args = ap.parse_args()

    if args.apply:
        return asyncio.run(apply(args.force_uid, args.yes))
    if args.probe:
        return asyncio.run(probe(args.probe_writes))
    print_plan(args.verbose)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
