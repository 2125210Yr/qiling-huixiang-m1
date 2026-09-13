"""SPEC layer folder -> Cubism import PSD + Live2D model3/physics3 stub + web idle preview.

Generalises tools/art/build_c001_cubism_psd.py: same psd-tools recipe (RGBA doc,
Maximize Compatibility, negative layer count) but driven by a skeleton order and
any SPEC folder written by from_masks.py / split_still.py, so hair_side / head /
hands / feet reach Cubism instead of the old four-plate v3 set.

The preview warps the *still* as one mesh. Influence fields are baked from the
real layer alphas, so nothing is ever composited from punched plates: the ragged
holes in body.png / hair_*.png cannot show up on screen.

    python tools/puppet/export_cubism.py --id C001 --src art/characters/C001-焰刃/puppet-src

Outputs land in <src>/cubism-export/.
"""
from __future__ import annotations

import argparse
import base64
import json
from pathlib import Path

import cv2
import numpy as np
from PIL import Image
from psd_tools import PSDImage
from psd_tools.constants import CompatibilityMode, Compression

ROOT = Path(__file__).resolve().parent
SKELETONS = ROOT / "skeletons"

# Hair-like slots get a root pivot + taper lever and ride the wind springs.
HAIR_SLOTS = ("hair_back", "hair_side", "hair_front")
# Slots that swing rigidly about a single pivot.
RIGID_SLOTS = ("head", "sword")
FIELD_W, FIELD_H = 160, 240


# ---------------------------------------------------------------- layer loading


def load_rgba(p: Path) -> np.ndarray:
    return np.array(Image.open(p).convert("RGBA"))


def alpha_mask(arr: np.ndarray) -> np.ndarray:
    return arr[:, :, 3] > 8


def bbox_of(m: np.ndarray) -> list[int] | None:
    if not m.any():
        return None
    ys, xs = np.where(m)
    return [int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max())]


def keep_largest(m: np.ndarray) -> np.ndarray:
    n, lab, stats, _ = cv2.connectedComponentsWithStats(m.astype(np.uint8), 8)
    if n <= 1:
        return m
    return lab == 1 + int(np.argmax(stats[1:, cv2.CC_STAT_AREA]))


def root_of(m: np.ndarray) -> tuple[int, int]:
    """Attachment point: centroid of the topmost sliver of the mask."""
    ys, xs = np.where(m)
    cut = np.percentile(ys, 6)
    sel = ys <= cut
    return int(xs[sel].mean()), int(ys[sel].mean())


def disk(r: int) -> np.ndarray:
    return cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (2 * r + 1, 2 * r + 1))


def rigid_core(still_a: np.ndarray, hair: np.ndarray) -> np.ndarray:
    """Shirt / pants / boots / arms / face: the silhouette with the hair taken out.

    Cannot be read off body.png -- from_masks writes the untouched still there
    whenever the split is additive, so body alpha is the whole silhouette.
    """
    raw = still_a & ~(cv2.dilate(hair.astype(np.uint8), disk(8)) > 0)
    healed = cv2.morphologyEx(raw.astype(np.uint8), cv2.MORPH_CLOSE, disk(13)) > 0
    core = keep_largest(healed & still_a)
    return keep_largest(cv2.morphologyEx(core.astype(np.uint8), cv2.MORPH_OPEN, disk(4)) > 0)


def collect(src: Path, skeleton: dict) -> tuple[np.ndarray, dict[str, np.ndarray]]:
    still_p = src / "still.png"
    if not still_p.is_file():
        raise SystemExit(f"missing {still_p}")
    still = load_rgba(still_p)
    cw, ch = skeleton.get("canvas") or [1024, 1536]
    if (still.shape[1], still.shape[0]) != (cw, ch):
        raise SystemExit(f"still is {still.shape[1]}x{still.shape[0]}, skeleton wants {cw}x{ch}")
    layers_dir = src / "layers"
    plates: dict[str, np.ndarray] = {}
    for name in skeleton["order"]:
        p = layers_dir / f"{name}.png"
        if not p.is_file():
            continue
        arr = load_rgba(p)
        if arr.shape[:2] != still.shape[:2]:
            raise SystemExit(f"{p.name} is {arr.shape[1]}x{arr.shape[0]}, still is {still.shape[1]}x{still.shape[0]}")
        if not alpha_mask(arr).any():
            print(f"  skip empty {name}")
            continue
        plates[name] = arr
    missing = [n for n in skeleton["required"] if n not in plates]
    if missing:
        raise SystemExit(f"missing required layers: {missing}")
    return still, plates


def locate_crop(still: np.ndarray, crop: np.ndarray) -> tuple[int, int, float]:
    """Where a keyframe crop sits on the canvas, by exact template match."""
    res = cv2.matchTemplate(still[:, :, :3], crop[:, :, :3], cv2.TM_SQDIFF_NORMED)
    err, _, loc, _ = cv2.minMaxLoc(res)
    return int(loc[0]), int(loc[1]), float(err)


# ------------------------------------------------------------------------- PSD


def trim_rgba(im: Image.Image, margin: int = 2) -> tuple[Image.Image, int, int]:
    alpha = im.split()[-1]
    bbox = alpha.point(lambda p: 255 if p > 8 else 0).getbbox()
    if not bbox:
        return im, 0, 0
    left, top, right, bottom = bbox
    left = max(0, left - margin)
    top = max(0, top - margin)
    right = min(im.width, right + margin)
    bottom = min(im.height, bottom + margin)
    return im.crop((left, top, right, bottom)), top, left


def exclusive_body(still: np.ndarray, plates: dict[str, np.ndarray]) -> tuple[np.ndarray, int]:
    """Cubism needs one owner per pixel: a moving slot and the body cannot both
    hold the same hair, or the static copy ghosts through when the slot moves.

    from_masks leaves body.png as the untouched still whenever the split went
    additive, so punch it here and report how much has to be repainted.
    """
    others = np.zeros(still.shape[:2], bool)
    for name, arr in plates.items():
        if name != "body":
            others |= alpha_mask(arr)
    punch = cv2.erode(others.astype(np.uint8), disk(1)) > 0
    body = still.copy()
    body[punch, 3] = 0
    # Holes are only real where the still had paint and nothing else covers it.
    return body, int((alpha_mask(still) & punch).sum())


def write_psd(
    dest: Path,
    char_id: str,
    plates: dict[str, np.ndarray],
    order: list[str],
    canvas: tuple[int, int],
    still: np.ndarray | None = None,
) -> None:
    """Photoshop-shaped PSD: Cubism only binds textures when the merged image
    carries real alpha and the layer count is negative."""
    psd = PSDImage.new("RGBA", canvas, color=(0, 0, 0, 0))
    psd.compatibility_mode = CompatibilityMode.PHOTOSHOP
    group = psd.create_group(name=char_id, open_folder=True)
    if still is not None:
        ref, top, left = trim_rgba(Image.fromarray(still))
        layer = psd.create_pixel_layer(ref, name="_still_ref", top=top, left=left, compression=Compression.RLE)
        if layer.has_mask():
            layer.remove_mask()
        layer.visible = False
        group.append(layer)
        print("  psd _still_ref  (hidden repaint reference)")
    for name in order:
        if name not in plates:
            continue
        cropped, top, left = trim_rgba(Image.fromarray(plates[name]))
        layer = psd.create_pixel_layer(cropped, name=name, top=top, left=left, compression=Compression.RLE)
        if layer.has_mask():
            layer.remove_mask()
        group.append(layer)
        print(f"  psd {name:11s} ({left},{top}) {cropped.size[0]}x{cropped.size[1]}")
    psd._update_record()
    info = psd._record.layer_and_mask_information.layer_info
    info.layer_count = -abs(info.layer_count)
    with dest.open("wb") as fd:
        psd.save(fd)
    print(f"  wrote {dest.name} ({dest.stat().st_size} bytes) layer_count={info.layer_count}")


# -------------------------------------------------------------- Live2D handoff


def hair_setting(idx: int, name: str, param: str, rig: dict, angle: float, mobility: float) -> dict:
    """Two-vertex pendulum: root pinned at the attachment, tip carries the swing."""
    length = rig["length"]
    return {
        "Id": f"PhysicsSetting{idx}",
        "Input": [
            {"Source": {"Target": "Parameter", "Id": "ParamAngleX"}, "Weight": 60, "Type": "X", "Reflect": False},
            {"Source": {"Target": "Parameter", "Id": "ParamAngleZ"}, "Weight": 30, "Type": "Angle", "Reflect": False},
            {"Source": {"Target": "Parameter", "Id": "ParamBodyAngleX"}, "Weight": 40, "Type": "X", "Reflect": False},
        ],
        "Output": [
            {
                "Destination": {"Target": "Parameter", "Id": param},
                "VertexIndex": 2,
                "Scale": round(angle, 3),
                "Weight": 100,
                "Type": "Angle",
                "Reflect": False,
            }
        ],
        "Vertices": [
            {"Position": {"X": 0.0, "Y": 0.0}, "Mobility": 1.0, "Delay": 1.0, "Acceleration": 1.0, "Radius": 0.0},
            {
                "Position": {"X": 0.0, "Y": -round(length, 2)},
                "Mobility": round(mobility, 3),
                "Delay": round(0.6 + 0.5 * mobility, 3),
                "Acceleration": 1.4,
                "Radius": round(length, 2),
            },
        ],
        "Normalization": {
            "Position": {"Minimum": -10.0, "Default": 0.0, "Maximum": 10.0},
            "Angle": {"Minimum": -10.0, "Default": 0.0, "Maximum": 10.0},
        },
    }


def chest_setting(idx: int, param: str, reflect: bool) -> dict:
    """Idle A: each lobe hangs from its own upper edge, same sign, no squeeze."""
    return {
        "Id": f"PhysicsSetting{idx}",
        "Input": [
            {"Source": {"Target": "Parameter", "Id": "ParamBodyAngleX"}, "Weight": 60, "Type": "X", "Reflect": reflect},
            {"Source": {"Target": "Parameter", "Id": "ParamBodyAngleZ"}, "Weight": 30, "Type": "Angle", "Reflect": reflect},
            {"Source": {"Target": "Parameter", "Id": "ParamBreath"}, "Weight": 20, "Type": "Y", "Reflect": False},
        ],
        "Output": [
            {
                "Destination": {"Target": "Parameter", "Id": param},
                "VertexIndex": 2,
                "Scale": 1.1,
                "Weight": 100,
                "Type": "Angle",
                "Reflect": reflect,
            }
        ],
        "Vertices": [
            {"Position": {"X": 0.0, "Y": 0.0}, "Mobility": 1.0, "Delay": 1.0, "Acceleration": 1.0, "Radius": 0.0},
            {"Position": {"X": 0.0, "Y": -3.2}, "Mobility": 0.86, "Delay": 0.62, "Acceleration": 1.9, "Radius": 3.2},
        ],
        "Normalization": {
            "Position": {"Minimum": -10.0, "Default": 0.0, "Maximum": 10.0},
            "Angle": {"Minimum": -6.0, "Default": 0.0, "Maximum": 6.0},
        },
    }


def build_physics3(rigs: dict[str, dict], has_chest: bool) -> dict:
    settings: list[dict] = []
    names: list[dict] = []
    param_for = {"hair_back": "ParamHairBack", "hair_side": "ParamHairSide", "hair_front": "ParamHairFront"}
    label = {"hair_back": "Hair Back", "hair_side": "Hair Side", "hair_front": "Hair Front"}
    # Longer, heavier masses swing wider and settle slower.
    tune = {"hair_back": (1.35, 0.94), "hair_side": (1.15, 0.9), "hair_front": (0.7, 0.8)}
    for slot in HAIR_SLOTS:
        if slot not in rigs:
            continue
        idx = len(settings) + 1
        angle, mobility = tune[slot]
        settings.append(hair_setting(idx, slot, param_for[slot], rigs[slot], angle, mobility))
        names.append({"Id": f"PhysicsSetting{idx}", "Name": label[slot]})
    if has_chest:
        for param, reflect, nm in (("ParamChestL", False, "Chest L"), ("ParamChestR", True, "Chest R")):
            idx = len(settings) + 1
            settings.append(chest_setting(idx, param, reflect))
            names.append({"Id": f"PhysicsSetting{idx}", "Name": nm})
    return {
        "Version": 3,
        "Meta": {
            "PhysicsSettingCount": len(settings),
            "TotalInputCount": sum(len(s["Input"]) for s in settings),
            "TotalOutputCount": sum(len(s["Output"]) for s in settings),
            "VertexCount": sum(len(s["Vertices"]) for s in settings),
            "EffectiveForces": {"Gravity": {"X": 0.0, "Y": -1.0}, "Wind": {"X": 0.0, "Y": 0.0}},
            "PhysicsDictionary": names,
        },
        "PhysicsSettings": settings,
    }


def build_model3(char_id: str, plates: dict[str, np.ndarray]) -> dict:
    hit = [{"Id": "HitAreaHead", "Name": "Head"}, {"Id": "HitAreaChest", "Name": "Chest"}, {"Id": "HitAreaBody", "Name": "Body"}]
    if "sword" in plates:
        hit.append({"Id": "HitAreaSword", "Name": "Sword"})
    return {
        "Version": 3,
        "FileReferences": {
            "Moc": f"{char_id}.moc3",
            "Textures": [f"{char_id}.2048/texture_00.png"],
            "Physics": f"{char_id}.physics3.json",
            "Motions": {
                "Idle": [{"File": f"motions/{char_id}.idle.motion3.json", "FadeInTime": 0.8, "FadeOutTime": 0.8}]
            },
        },
        "Groups": [
            {"Target": "Parameter", "Name": "EyeBlink", "Ids": ["ParamEyeLOpen", "ParamEyeROpen"]},
            {"Target": "Parameter", "Name": "LipSync", "Ids": ["ParamMouthOpenY"]},
        ],
        "HitAreas": hit,
    }


def curve(param: str, points: list[tuple[float, float]]) -> dict:
    seg: list[float] = [round(points[0][0], 3), round(points[0][1], 4)]
    for t, v in points[1:]:
        seg += [1.0, round(t, 3), round(v, 4)]  # 1 = linear segment
    return {"Target": "Parameter", "Id": param, "Segments": seg}


def build_idle_motion(dur: float = 6.0) -> dict:
    """Breath / lean / weight-shift only. Hair and chest come from physics."""
    def wave(freq: float, amp: float, base: float = 0.0, phase: float = 0.0, steps: int = 24):
        return [
            (dur * i / steps, base + amp * np.sin(2 * np.pi * (freq * dur * i / steps + phase)))
            for i in range(steps + 1)
        ]

    curves = [
        curve("ParamBreath", wave(1 / dur * 2, 0.5, 0.5, -0.25)),
        curve("ParamAngleX", wave(1 / dur, 4.0, 0.0, 0.12)),
        curve("ParamAngleZ", wave(1 / dur * 1.5, 2.2)),
        curve("ParamBodyAngleX", wave(1 / dur, 2.6, 0.0, 0.35)),
        curve("ParamBodyAngleZ", wave(1 / dur * 0.5, 1.4)),
    ]
    return {
        "Version": 3,
        "Meta": {
            "Duration": dur,
            "Fps": 30.0,
            "Loop": True,
            "AreBeziersRestricted": True,
            "CurveCount": len(curves),
            "TotalSegmentCount": sum((len(c["Segments"]) - 2) // 3 for c in curves),
            "TotalPointCount": sum((len(c["Segments"]) + 1) // 3 + 1 for c in curves),
            "UserDataCount": 0,
            "TotalUserDataSize": 0,
        },
        "Curves": curves,
    }


# ------------------------------------------------------------- influence fields


def blur01(m: np.ndarray, sigma: float) -> np.ndarray:
    f = cv2.resize(m.astype(np.float32), (FIELD_W, FIELD_H), interpolation=cv2.INTER_AREA)
    return np.clip(cv2.GaussianBlur(f, (0, 0), sigma), 0.0, 1.0)


def lever_field(m: np.ndarray, root: tuple[int, int], canvas: tuple[int, int]) -> np.ndarray:
    """0 at the attachment point, 1 at the far tip. Tapers the swing."""
    cw, ch = canvas
    ys, xs = np.mgrid[0:FIELD_H, 0:FIELD_W].astype(np.float32)
    px = (xs + 0.5) / FIELD_W * cw
    py = (ys + 0.5) / FIELD_H * ch
    d = np.sqrt((px - root[0]) ** 2 + (py - root[1]) ** 2)
    small = cv2.resize(m.astype(np.uint8), (FIELD_W, FIELD_H), interpolation=cv2.INTER_AREA) > 0
    far = float(np.percentile(d[small], 96)) if small.any() else 1.0
    t = np.clip(d / max(far, 1.0), 0.0, 1.0)
    return t * t * (3 - 2 * t)


def bake_fields(still: np.ndarray, plates: dict[str, np.ndarray], chest: dict | None, canvas: tuple[int, int]):
    """Three RGBA field textures the vertex shader samples. Baked, not guessed."""
    cw, ch = canvas
    masks = {n: alpha_mask(a) for n, a in plates.items()}
    still_a = alpha_mask(still)
    hair_u = np.zeros((ch, cw), bool)
    for slot in HAIR_SLOTS:
        if slot in masks:
            hair_u |= masks[slot]
    rigid = rigid_core(still_a, hair_u)
    rigid_f = blur01(rigid, 3.0)

    rigs: dict[str, dict] = {}
    fa = np.zeros((FIELD_H, FIELD_W, 4), np.float32)
    fl = np.zeros((FIELD_H, FIELD_W, 4), np.float32)
    for i, slot in enumerate(HAIR_SLOTS):
        if slot not in masks:
            continue
        m = masks[slot]
        root = root_of(m)
        bb = bbox_of(m)
        # Physics pendulum length in Cubism normalized units (~10 per canvas half).
        reach = max(bb[3] - root[1], abs(bb[0] - root[0]), abs(bb[2] - root[0]))
        rigs[slot] = {
            "root": [root[0], root[1]],
            "bbox": bb,
            "length": round(10.0 * reach / (ch * 0.5), 2),
            "px": int(m.sum()),
        }
        # Hold the hair weight off the shirt so a gust cannot drag the torso.
        fa[:, :, i] = np.clip(blur01(m, 4.0) * (1.0 - 0.88 * rigid_f), 0.0, 1.0)
        fl[:, :, i] = lever_field(m, root, canvas)

    head = masks.get("head")
    if head is not None:
        fa[:, :, 3] = blur01(head, 3.0)
        rigs["head"] = {"root": list(root_of(head)), "bbox": bbox_of(head)}

    fb = np.zeros((FIELD_H, FIELD_W, 4), np.float32)
    fb[:, :, 1] = rigid_f
    if chest is not None:
        cx0, cy0, cw_, ch_ = chest["rect"]
        yy, xx = np.mgrid[0:ch, 0:cw]
        cx, cy = cx0 + cw_ * 0.5, cy0 + ch_ * 0.42
        e = ((xx - cx) / (cw_ * 0.52)) ** 2 + ((yy - cy) / (ch_ * 0.58)) ** 2 <= 1.0
        fb[:, :, 0] = blur01(e & rigid, 3.5)

    rb = bbox_of(rigid) or [0, 0, cw - 1, ch - 1]
    head_bb = rigs.get("head", {}).get("bbox")
    shoulder_y = float(head_bb[3]) if head_bb else rb[1] + (rb[3] - rb[1]) * 0.18
    hip_y = shoulder_y + (rb[3] - shoulder_y) * 0.22
    yy = (np.arange(FIELD_H, dtype=np.float32) + 0.5) / FIELD_H * ch
    # Breath lever: 0 at the hips, 1 at the shoulders, falls off over the head.
    up = np.clip((hip_y - yy) / max(hip_y - shoulder_y, 1.0), 0.0, 1.0)
    up = np.where(yy < shoulder_y, np.clip(1.0 - (shoulder_y - yy) / 130.0, 0.0, 1.0), up)
    fl[:, :, 3] = rigid_f * (up * up * (3 - 2 * up))[:, None]
    # Pin the ground contact so the figure cannot slide off its feet.
    foot_y = rb[3] - (rb[3] - rb[1]) * 0.09
    pin = np.clip((yy - foot_y) / max(rb[3] - foot_y, 1.0), 0.0, 1.0)
    fb[:, :, 3] = np.broadcast_to(pin[:, None], (FIELD_H, FIELD_W)).copy()

    band = rigid[int(max(hip_y - 40, 0)):int(min(hip_y + 40, ch)), :]
    hip_x = float(np.where(band.any(0))[0].mean()) if band.any() else cw * 0.5
    hip = [hip_x, float(hip_y)]

    sword = masks.get("sword")
    if sword is not None:
        fb[:, :, 2] = blur01(sword, 3.0)
        rigs["sword"] = {"root": list(root_of(sword)), "bbox": bbox_of(sword)}
    return fa, fb, fl, rigs, hip, rigid


def png_data_uri(arr: np.ndarray, mode: str = "RGBA") -> str:
    import io

    buf = io.BytesIO()
    Image.fromarray(arr, mode).save(buf, format="PNG", optimize=True)
    return "data:image/png;base64," + base64.b64encode(buf.getvalue()).decode("ascii")


# ------------------------------------------------------------------- preview

PREVIEW = (ROOT / "preview_template.html").read_text(encoding="utf-8")


def write_preview(dest: Path, char_id: str, still: np.ndarray, fa, fb, fl, rig: dict, scale: float) -> None:
    ch, cw = still.shape[:2]
    tex = still
    if scale != 1.0:
        tw, th = int(cw * scale), int(ch * scale)
        tex = np.array(Image.fromarray(still).resize((tw, th), Image.LANCZOS))
    payload = dict(rig)
    payload["still"] = png_data_uri(tex)
    payload["fieldA"] = png_data_uri((np.clip(fa, 0, 1) * 255).astype(np.uint8))
    payload["fieldB"] = png_data_uri((np.clip(fb, 0, 1) * 255).astype(np.uint8))
    payload["fieldL"] = png_data_uri((np.clip(fl, 0, 1) * 255).astype(np.uint8))
    html = PREVIEW.replace("__TITLE__", char_id).replace(
        "__RIG__", json.dumps(payload, ensure_ascii=False, separators=(",", ":"))
    )
    dest.write_text(html, encoding="utf-8")
    print(f"  wrote {dest.name} ({dest.stat().st_size // 1024} KB)")


# ----------------------------------------------------------------------- main


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--id", required=True)
    ap.add_argument("--src", required=True, type=Path, help="puppet-src folder (still.png + layers/)")
    ap.add_argument("--skeleton", default="standee_front")
    ap.add_argument("--out", type=Path, default=None)
    ap.add_argument("--preview-scale", type=float, default=1.0, help="downscale the baked still to shrink the html")
    ap.add_argument("--no-psd", action="store_true")
    ap.add_argument(
        "--psd-mode",
        choices=("exclusive", "as-is"),
        default="exclusive",
        help="exclusive punches the moving slots out of body (Cubism-correct, leaves holes to repaint); "
        "as-is ships the plates untouched",
    )
    ap.add_argument("--debug-fields", action="store_true", help="also dump the influence fields as viewable pngs")
    args = ap.parse_args()

    skeleton = json.loads((SKELETONS / f"{args.skeleton}.json").read_text(encoding="utf-8"))
    out = args.out or (args.src / "cubism-export")
    out.mkdir(parents=True, exist_ok=True)

    still, plates = collect(args.src, skeleton)
    ch, cw = still.shape[:2]
    order = [n for n in skeleton["order"] if n in plates]
    print("layers:", ", ".join(order))

    marks = json.loads((args.src / "layers" / "landmarks.json").read_text(encoding="utf-8"))

    chest = None
    rest_p, bounce_p = args.src / "chest_rest.png", args.src / "chest_bounce.png"
    if rest_p.is_file() and bounce_p.is_file():
        rest, bounce = load_rgba(rest_p), load_rgba(bounce_p)
        if rest.shape == bounce.shape:
            x, y, err = locate_crop(still, rest)
            if err < 1e-4:
                chest = {"rect": [x, y, rest.shape[1], rest.shape[0]], "bounce": bounce, "err": err}
                print(f"  chest keyframes at ({x},{y}) {rest.shape[1]}x{rest.shape[0]} match_err={err:.2e}")
            else:
                print(f"  chest_rest does not match the still (err={err:.4f}); skipping bounce")

    fa, fb, fl, rigs, hip, rigid = bake_fields(still, plates, chest, (cw, ch))

    body_is_still = "body" in plates and np.array_equal(plates["body"], still)
    if body_is_still:
        print("  note: layers/body.png is byte-identical to still.png -> the split is additive, not exclusive")
    holes = 0
    if not args.no_psd:
        psd_plates = dict(plates)
        if args.psd_mode == "exclusive":
            psd_plates["body"], holes = exclusive_body(still, plates)
            print(f"  psd body punched: {holes} px need repainting behind the moving slots")
        write_psd(out / f"{args.id}.psd", args.id, psd_plates, order, (cw, ch), still=still)

    physics = build_physics3(rigs, chest is not None)
    (out / f"{args.id}.physics3.json").write_text(json.dumps(physics, indent=2) + "\n", encoding="utf-8")
    (out / f"{args.id}.model3.json").write_text(
        json.dumps(build_model3(args.id, plates), indent=2) + "\n", encoding="utf-8"
    )
    (out / "motions").mkdir(exist_ok=True)
    (out / "motions" / f"{args.id}.idle.motion3.json").write_text(
        json.dumps(build_idle_motion(), indent=2) + "\n", encoding="utf-8"
    )
    print(f"  wrote {args.id}.model3.json, {args.id}.physics3.json, motions/{args.id}.idle.motion3.json")

    rig = {
        "id": args.id,
        "canvas": [cw, ch],
        "order": order,
        "landmarks": marks,
        "hip": [round(hip[0], 1), round(hip[1], 1)],
        "rigs": rigs,
        "chest": ({"rect": chest["rect"]} if chest else None),
        "texScale": args.preview_scale,
    }
    manifest = dict(rig)
    manifest["bbox"] = {n: bbox_of(alpha_mask(a)) for n, a in plates.items()}
    manifest["rigidBbox"] = bbox_of(rigid)
    manifest["bodyIsStill"] = body_is_still
    manifest["psdMode"] = "none" if args.no_psd else args.psd_mode
    manifest["psdRepaintPx"] = holes
    (out / "manifest.json").write_text(json.dumps(manifest, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")

    if chest is not None:
        rig["chest"]["bounce"] = png_data_uri(chest["bounce"])
    write_preview(out / "preview.html", args.id, still, fa, fb, fl, rig, args.preview_scale)

    if args.debug_fields:
        dbg = out / "_fields"
        dbg.mkdir(exist_ok=True)
        chans = [
            ("A0_w_hair_back", fa[:, :, 0]), ("A1_w_hair_side", fa[:, :, 1]),
            ("A2_w_hair_front", fa[:, :, 2]), ("A3_w_head", fa[:, :, 3]),
            ("B0_w_chest", fb[:, :, 0]), ("B1_w_torso", fb[:, :, 1]),
            ("B2_w_sword", fb[:, :, 2]), ("B3_pin", fb[:, :, 3]),
            ("L0_lever_hair_back", fl[:, :, 0]), ("L1_lever_hair_side", fl[:, :, 1]),
            ("L2_lever_hair_front", fl[:, :, 2]), ("L3_lever_torso", fl[:, :, 3]),
        ]
        # Grey field over the silhouette so a weight can be read against the art.
        sil = cv2.resize((alpha_mask(still).astype(np.float32)), (cw // 2, ch // 2), interpolation=cv2.INTER_AREA)
        Image.fromarray((rigid.astype(np.uint8) * 255)).save(dbg / "rigid_core.png")
        for nm, f in chans:
            v = cv2.resize(np.clip(f, 0, 1), (cw // 2, ch // 2), interpolation=cv2.INTER_LINEAR)
            rgb = np.dstack([v, v * 0.35 + sil * 0.22, v * 0.15 + sil * 0.30])
            Image.fromarray((np.clip(rgb, 0, 1) * 255).astype(np.uint8), "RGB").save(dbg / f"{nm}.png")
        print(f"  wrote {dbg.name}/ ({len(chans)} channels)")

    print("done ->", out)


if __name__ == "__main__":
    main()
