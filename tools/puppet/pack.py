"""Pack a SPEC layer folder onto a mother skeleton → puppet.json."""
from __future__ import annotations

import argparse
import json
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parent
SKELETONS = ROOT / "skeletons"


def load_json(p: Path) -> dict:
    return json.loads(p.read_text(encoding="utf-8"))


def pack(char_id: str, layers: Path, skeleton_id: str, tex_prefix: str, out: Path) -> Path:
    skel = load_json(SKELETONS / f"{skeleton_id}.json")
    marks = dict(skel.get("landmarks") or {})
    extra = layers / "landmarks.json"
    if extra.is_file():
        marks.update(load_json(extra))

    missing = [n for n in skel["required"] if not (layers / f"{n}.png").is_file()]
    if missing:
        raise SystemExit(f"missing required layers: {missing}")
    skip = set()

    slots = []
    order = 0
    mesh_slots = set(skel.get("meshSlots") or [])
    for name in skel["order"]:
        png = layers / f"{name}.png"
        if not png.is_file() or name in skip:
            continue
        slots.append({
            "id": name,
            "tex": f"{tex_prefix}/{name}",
            "order": order,
            "mesh": name in mesh_slots,
        })
        order += 1

    head = marks.get("head") or [0.51, 0.82]
    chest = marks.get("chest") or [0.50, 0.70]
    hand_r = marks.get("hand_r") or [0.43, 0.55]
    foot_r = marks.get("foot_r") or [0.46, 0.20]
    foot_l = marks.get("foot_l") or [0.54, 0.23]
    canvas = skel.get("canvas") or [1024, 1536]
    dto = {
        "id": char_id,
        "skeleton": skeleton_id,
        "canvasW": int(canvas[0]),
        "canvasH": int(canvas[1]),
        "headU": float(head[0]),
        "headV": float(head[1]),
        "chestU": float(chest[0]),
        "chestV": float(chest[1]),
        "handRU": float(hand_r[0]),
        "handRV": float(hand_r[1]),
        "footRU": float(foot_r[0]),
        "footRV": float(foot_r[1]),
        "footLU": float(foot_l[0]),
        "footLV": float(foot_l[1]),
        "slots": slots,
    }
    out.mkdir(parents=True, exist_ok=True)
    dest = out / "puppet.json"
    dest.write_text(json.dumps(dto, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print("wrote", dest)
    for s in slots:
        print("  slot", s["id"], "->", s["tex"])
    return dest


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--id", required=True)
    ap.add_argument("--layers", required=True, type=Path)
    ap.add_argument("--skeleton", default="standee_front")
    ap.add_argument("--tex-prefix", default="")
    ap.add_argument("--out", required=True, type=Path)
    args = ap.parse_args()
    prefix = args.tex_prefix or args.layers.name
    pack(args.id, args.layers, args.skeleton, prefix, args.out)


if __name__ == "__main__":
    main()
