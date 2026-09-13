"""Exclusive body (face+clothes) + moving parts. Same pixels, joint overlap only."""
from __future__ import annotations

import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter

CUT = Path(r"F:\ps_live\master_cut.png")
BLINK = Path(r"F:\天命之子\client\Assets\Resources\Art\Characters\C001\LiveLayers\layer_head_blink.png")
OUT = Path(r"F:\天命之子\client\Assets\Resources\Art\Characters\C001\LiveLayers")
PREV = Path(r"F:\天命之子\天命之子数据\_hires_tiles")


def opaque_bbox(im: Image.Image, thr: int = 18):
    a = np.array(im)[..., 3]
    ys, xs = np.where(a > thr)
    if len(xs) == 0:
        return None
    return int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1


def uv_of(im: Image.Image, kind: str, W: int, H: int):
    bb = opaque_bbox(im)
    if bb is None:
        return [0.5, 0.5]
    l, t, r, b = bb
    if kind == "top":
        x, y = (l + r) / 2.0, t + (b - t) * 0.08
    elif kind == "chest":
        x, y = (l + r) / 2.0, t + (b - t) * 0.22
    else:
        x, y = (l + r) / 2.0, (t + b) / 2.0
    return [round(x / (W - 1), 4), round(1.0 - y / (H - 1), 4)]


def morph(mask: np.ndarray, mode: str, px: int) -> np.ndarray:
    if px <= 0:
        return mask
    k = px * 2 + 1
    im = Image.fromarray((mask.astype(np.uint8) * 255), "L")
    im = im.filter(ImageFilter.MaxFilter(k) if mode == "dilate" else ImageFilter.MinFilter(k))
    return np.array(im) > 40


def layer_from(cut: np.ndarray, mask: np.ndarray) -> Image.Image:
    out = np.zeros_like(cut)
    m = mask.astype(bool)
    out[..., :3] = np.where(m[..., None], cut[..., :3], 0)
    out[..., 3] = np.where(m, cut[..., 3], 0)
    return Image.fromarray(out, "RGBA")


def main():
    cut = np.array(Image.open(CUT).convert("RGBA"))
    H, W = cut.shape[:2]
    OUT.mkdir(parents=True, exist_ok=True)
    yy, xx = np.mgrid[0:H, 0:W]
    xn, yn = xx / (W - 1.0), yy / (H - 1.0)
    rgb = cut[..., :3].astype(np.float32) / 255.0
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    mx = np.maximum(np.maximum(r, g), b)
    mn = np.minimum(np.minimum(r, g), b)
    ss = (mx - mn) / (mx + 1e-6)
    sil = cut[..., 3] > 12
    mint = sil & (g > r + 0.03) & (g > 0.32)
    plat = sil & (ss < 0.22) & (mx > 0.55)
    skin = sil & (r > g + 0.02) & (r > 0.38) & (ss > 0.10)
    white = sil & (mx > 0.72) & (ss < 0.16)

    # Ponytail only — scalp/clip stay on the body so the neck never splits.
    hair = (mint | (plat & (yn < 0.40))) & (xn < 0.52) & (yn > 0.10) & (yn < 0.52)
    hair = hair & ~((xn > 0.44) & (yn < 0.28))
    hair = hair & ~white
    hair = morph(hair, "dilate", 2)

    face = sil & (xn > 0.44) & (xn < 0.68) & (yn > 0.07) & (yn < 0.24)

    sword = sil & (xn > 0.14) & (xn < 0.48) & (yn > 0.44) & (yn < 0.88)
    sword = sword & (b > 0.42) & (ss < 0.42) & ~mint

    arm_r = skin & (xn > 0.28) & (xn < 0.50) & (yn > 0.24) & (yn < 0.54) & ~white
    arm_l = skin & (xn > 0.62) & (xn < 0.86) & (yn > 0.26) & (yn < 0.60) & ~white
    hand_r = skin & (xn > 0.32) & (xn < 0.48) & (yn > 0.42) & (yn < 0.56)
    hand_l = skin & (xn > 0.68) & (xn < 0.88) & (yn > 0.46) & (yn < 0.62)
    foot_r = sil & (xn > 0.40) & (xn < 0.58) & (yn > 0.80)
    foot_l = sil & (xn > 0.50) & (xn < 0.72) & (yn > 0.82)
    leg_r = sil & (xn > 0.42) & (xn < 0.60) & (yn > 0.50) & (yn < 0.88) & ~white
    leg_l = sil & (xn > 0.52) & (xn < 0.76) & (yn > 0.52) & (yn < 0.90) & ~white

    arm_r = arm_r & ~hand_r
    arm_l = arm_l & ~hand_l
    leg_r = leg_r & ~foot_r
    leg_l = leg_l & ~foot_l

    # Never punch the body. Holes show as black when overlays swing.
    # Body is the complete still; hair/arms sit on top (same pixels at rest).
    body = sil

    layers = {
        "hair_back": layer_from(cut, morph(hair, "dilate", 3) & sil),
        "head": layer_from(cut, morph(face, "dilate", 2) & sil),
        "torso": layer_from(cut, body),
        "arm_r": layer_from(cut, morph(arm_r | hand_r, "dilate", 2) & sil),
        "arm_l": layer_from(cut, morph(arm_l | hand_l, "dilate", 2) & sil),
        "hand_r": layer_from(cut, morph(hand_r, "dilate", 2) & sil),
        "hand_l": layer_from(cut, morph(hand_l, "dilate", 2) & sil),
        "sword": layer_from(cut, morph(sword, "dilate", 2) & sil),
        "leg_r": Image.new("RGBA", (W, H), (0, 0, 0, 0)),
        "leg_l": Image.new("RGBA", (W, H), (0, 0, 0, 0)),
        "foot_r": Image.new("RGBA", (W, H), (0, 0, 0, 0)),
        "foot_l": Image.new("RGBA", (W, H), (0, 0, 0, 0)),
    }

    blink_path = Path(r"F:\ps_live\blink74.jpg")
    if blink_path.exists():
        blink_im = Image.open(blink_path).convert("RGBA").resize((W, H), Image.Resampling.LANCZOS)
        ba = np.array(blink_im)
        ha = np.array(layers["head"])
        ba[..., 3] = ha[..., 3]
        ba[..., :3] = np.where(ha[..., 3:4] < 8, 0, ba[..., :3])
        blink = Image.fromarray(ba, "RGBA")
    else:
        blink = layers["head"]

    empty = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    files = {
        "layer_hair_back.png": layers["hair_back"],
        "layer_head.png": layers["head"],
        "layer_head_blink.png": blink,
        "layer_torso.png": layers["torso"],
        "layer_arm_r.png": layers["arm_r"],
        "layer_arm_l.png": layers["arm_l"],
        "layer_hand_r.png": layers["hand_r"],
        "layer_hand_l.png": layers["hand_l"],
        "layer_sword.png": layers["sword"],
        "layer_leg_r.png": layers["leg_r"],
        "layer_leg_l.png": layers["leg_l"],
        "layer_foot_r.png": layers["foot_r"],
        "layer_foot_l.png": layers["foot_l"],
        "layer_hair_front.png": empty,
    }
    for name, im in files.items():
        im.save(OUT / name)

    piv = {
        "hairBack": uv_of(layers["hair_back"], "top", W, H),
        "hairFront": [0.62, 0.84],
        "head": uv_of(layers["head"], "top", W, H),
        "torso": uv_of(layers["torso"], "chest", W, H),
        "armL": uv_of(layers["arm_l"], "top", W, H),
        "armR": uv_of(layers["arm_r"], "top", W, H),
        "handL": uv_of(layers["hand_l"], "top", W, H),
        "handR": uv_of(layers["hand_r"], "top", W, H),
        "legL": uv_of(layers["leg_l"], "top", W, H),
        "legR": uv_of(layers["leg_r"], "top", W, H),
        "footL": uv_of(layers["foot_l"], "top", W, H),
        "footR": uv_of(layers["foot_r"], "top", W, H),
        "sword": uv_of(layers["sword"], "top", W, H),
    }
    (OUT / "pivots.json").write_text(json.dumps(piv, indent=2), encoding="utf-8")

    preview = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    for key in (
        "hair_back", "leg_r", "leg_l", "foot_r", "foot_l", "torso",
        "arm_r", "sword", "arm_l", "hand_r", "hand_l",
    ):
        preview.alpha_composite(layers[key])
    preview.save(PREV / "live_rest_preview.png")
    print("wrote", OUT)
    print(json.dumps(piv))
    for name, im in files.items():
        a = np.array(im)[..., 3]
        print(name, int((a > 10).sum()), opaque_bbox(im))


if __name__ == "__main__":
    main()
