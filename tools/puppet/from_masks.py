# tools/puppet/from_masks.py
from __future__ import annotations

import json
import shutil
from pathlib import Path

import cv2
import numpy as np
from PIL import Image

MOVE_SLOTS = (
    "hair_back", "hair_front", "hair_side",
    "head", "hand_r", "hand_l", "foot_r", "foot_l", "sword",
)
CANVAS = (1024, 1536)
REST_ORDER = (
    "hair_back", "body", "head", "hair_side", "hair_front",
    "hand_l", "hand_r", "sword", "foot_l", "foot_r",
)


def mask_bool(arr: np.ndarray) -> np.ndarray:
    if arr.ndim == 2:
        return arr > 8
    if arr.shape[2] == 4:
        return arr[:, :, 3] > 8
    luma = arr[:, :, 0].astype(np.int16) + arr[:, :, 1] + arr[:, :, 2]
    return luma > 24


def shirt_keep(rgb: np.ndarray, a0: np.ndarray) -> np.ndarray:
    r = rgb[:, :, 0].astype(np.int16)
    g = rgb[:, :, 1].astype(np.int16)
    b = rgb[:, :, 2].astype(np.int16)
    mint = a0 & (g > r + 8) & (g > 120) & (r < 190)
    skin = a0 & (r > 145) & (g > 85) & (b > 65) & (r > g) & ((r - b) > 30)
    cloth = a0 & ~mint & (r > 80) & (g > 75) & (b > 65) & (np.abs(r - g) < 42) & (np.abs(g - b) < 48) & ~skin
    h, w = a0.shape
    zone = np.zeros((h, w), bool)
    if h >= 860 and w >= 670:
        zone[310:860, 330:530] = True
        zone[340:700, 500:670] = True
    else:
        zone[:, :] = True
    return cloth & zone


def check_keep_overlap(moves: dict[str, np.ndarray], keep: np.ndarray | None) -> None:
    if keep is None:
        return
    keep = keep.astype(bool)
    for name, m in moves.items():
        m = np.asarray(m).astype(bool)
        n = int(m.sum())
        if n == 0:
            continue
        blocked = int((m & keep).sum())
        if blocked / n > 0.90:
            raise ValueError(f"keep covers {blocked}/{n} of {name}")


def apply_plates(still, moves, keep, chest):
    if keep is not None:
        keep = np.asarray(keep).astype(bool)
    if chest is not None:
        chest = np.asarray(chest).astype(bool)
    moves = {name: np.asarray(m).astype(bool) for name, m in moves.items()}
    check_keep_overlap(moves, keep)
    h, w = still.shape[:2]
    k = np.ones((3, 3), np.uint8)
    a0 = still[:, :, 3] > 8
    rgb = still[:, :, :3]
    protect = shirt_keep(rgb, a0)
    if keep is not None:
        protect = protect | keep
    if chest is not None:
        protect = protect | chest
    plates = {}
    punch = np.zeros((h, w), np.uint8)
    for name, m in moves.items():
        if int(m.sum()) == 0:
            raise ValueError(f"empty mask: {name}")
        dil = cv2.dilate(m.astype(np.uint8) * 255, k, iterations=2) > 128
        layer = still.copy()
        layer[~dil, 3] = 0
        plates[name] = layer
        punch = np.maximum(punch, (m.astype(np.uint8) * 255))
    punch[protect] = 0
    punch = cv2.erode(punch, k, iterations=1)
    body = still.copy()
    body[punch > 16, 3] = 0
    plates["body"] = body
    return plates


def centroid_uv(m: np.ndarray) -> list[float]:
    ys, xs = np.where(m)
    h, w = m.shape
    u = float(xs.mean() / max(w - 1, 1))
    v = float(1.0 - ys.mean() / max(h - 1, 1))
    return [round(u, 4), round(v, 4)]


def merge_landmarks(moves, chest, file_marks, defaults):
    out = dict(defaults)
    for key in ("head", "hand_r", "hand_l", "foot_r", "foot_l"):
        if key in moves and int(np.asarray(moves[key]).astype(bool).sum()) > 0:
            out[key] = centroid_uv(np.asarray(moves[key]).astype(bool))
    if chest is not None and int(np.asarray(chest).astype(bool).sum()) > 0:
        out["chest"] = centroid_uv(np.asarray(chest).astype(bool))
    if file_marks:
        out.update(file_marks)
    return out


def over(d, s):
    a = s[:, :, 3:4].astype(np.float32) / 255.0
    return d * (1 - a) + s.astype(np.float32) * a


def composite_rest(plates: dict[str, np.ndarray]) -> np.ndarray:
    h, w = next(iter(plates.values())).shape[:2]
    rest = np.zeros((h, w, 4), np.float32)
    for name in REST_ORDER:
        if name in plates:
            rest = over(rest, plates[name])
    return np.clip(rest, 0, 255).astype(np.uint8)


def keep_holes(still: np.ndarray, body: np.ndarray, keep: np.ndarray | None) -> int:
    if keep is None:
        return 0
    st = still[:, :, 3] > 8
    bd = body[:, :, 3] > 8
    return int((keep & st & ~bd).sum())


def write_preview(path: Path, arr: np.ndarray) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    a = arr[:, :, 3:4].astype(np.float32) / 255.0
    rgb = arr[:, :, :3].astype(np.float32) * a + 32.0 * (1.0 - a)
    vis = np.clip(rgb, 0, 255).astype(np.uint8)
    m = arr[:, :, 3] > 8
    out = np.dstack([vis, np.full(vis.shape[:2], 255, np.uint8)])
    if m.any():
        ys, xs = np.where(m)
        out = out[max(0, ys.min() - 8):ys.max() + 9, max(0, xs.min() - 8):xs.max() + 9]
    Image.fromarray(out).save(path)


def load_rgba(p: Path) -> np.ndarray:
    return np.array(Image.open(p).convert("RGBA"))


def build(src: Path) -> tuple[dict[str, np.ndarray], dict, float, int]:
    still_p = src / "still.png"
    if not still_p.is_file():
        raise SystemExit("missing still.png")
    still = load_rgba(still_p)
    h, w = still.shape[:2]
    if (w, h) != CANVAS:
        raise SystemExit(f"canvas must be {CANVAS[0]}x{CANVAS[1]}, got {w}x{h}")
    mask_dir = src / "masks"
    moves = {}
    for name in MOVE_SLOTS:
        p = mask_dir / f"{name}.png"
        if not p.is_file():
            continue
        moves[name] = mask_bool(load_rgba(p))
    keep = mask_bool(load_rgba(mask_dir / "keep.png")) if (mask_dir / "keep.png").is_file() else None
    chest = mask_bool(load_rgba(mask_dir / "chest.png")) if (mask_dir / "chest.png").is_file() else None
    file_marks = json.loads((src / "landmarks.json").read_text(encoding="utf-8")) if (src / "landmarks.json").is_file() else None
    defaults = json.loads((Path(__file__).parent / "skeletons" / "standee_front.json").read_text(encoding="utf-8"))["landmarks"]
    plates = apply_plates(still, moves, keep, chest)
    marks = merge_landmarks(moves, chest, file_marks, defaults)
    rest = composite_rest(plates)
    diff = float(np.abs(rest[:, :, :3].astype(int) - still[:, :, :3].astype(int)).mean())
    holes = keep_holes(still, plates["body"], keep)
    return plates, marks, diff, holes


def write_outputs(src: Path, plates: dict[str, np.ndarray], marks: dict, diff: float, holes: int) -> Path:
    out = src / "layers"
    prev = out / "preview"
    out.mkdir(parents=True, exist_ok=True)
    for name, arr in plates.items():
        Image.fromarray(arr).save(out / f"{name}.png")
        write_preview(prev / f"{name}.png", arr)
    rest = composite_rest(plates)
    Image.fromarray(rest).save(out / "composite_rest.png")
    write_preview(prev / "composite_rest.png", rest)
    (out / "landmarks.json").write_text(json.dumps(marks, indent=2), encoding="utf-8")
    print("composite vs still mean rgb", round(diff, 2))
    print("keep holes", holes)
    return out


def main():
    import argparse
    from pack import pack
    ap = argparse.ArgumentParser()
    ap.add_argument("--id", required=True)
    ap.add_argument("--src", required=True, type=Path)
    ap.add_argument("--pack", action="store_true")
    args = ap.parse_args()
    plates, marks, diff, holes = build(args.src)
    layers = write_outputs(args.src, plates, marks, diff, holes)
    if args.pack:
        repo = Path(__file__).resolve().parents[2]
        dest_layers = repo / "client" / "Assets" / "Resources" / "Art" / "Characters" / args.id / "PuppetLayers"
        dest_layers.mkdir(parents=True, exist_ok=True)
        for p in layers.glob("*.png"):
            if p.name == "composite_rest.png":
                continue
            shutil.copy2(p, dest_layers / p.name)
        shutil.copy2(layers / "landmarks.json", dest_layers / "landmarks.json")
        pack(args.id, dest_layers, "standee_front", "PuppetLayers", repo / "client" / "Assets" / "Resources" / "Art" / "Characters" / args.id / "Puppet")


if __name__ == "__main__":
    main()
