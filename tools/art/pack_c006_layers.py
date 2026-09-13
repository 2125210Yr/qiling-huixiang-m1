"""Pack C006 still into aligned paper-doll layers + a web preview.

Body is the keyed still. Hair/bow are punched only on the sides so the face
stays intact. Optional magenta isolations are registered if they match size.
"""
from __future__ import annotations

import hashlib
import json
import shutil
import sys
from pathlib import Path

import numpy as np
from PIL import Image

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[1]
sys.path.insert(0, str(HERE))
from ingest_still import (  # noqa: E402
    ASSETS,
    UNITY_CHARACTERS,
    collect_guids,
    ensure_folder_meta,
    ingest_one,
    write_texture_meta,
    stable_guid,
)

CHAR_ID = "C006"
CHAR_NAME = "荆棘猎手"
SRC = REPO / "art" / "characters" / f"{CHAR_ID}-{CHAR_NAME}" / "presenter-raw.png"
ART_DIR = REPO / "art" / "characters" / f"{CHAR_ID}-{CHAR_NAME}"
LAYERS_ART = ART_DIR / "gen-layers"
PREVIEW = ART_DIR / "layer-preview.html"
UNITY_DIR = UNITY_CHARACTERS / CHAR_ID
UNITY_LAYERS = UNITY_DIR / "Layers"
CANVAS = (1024, 1536)
PORTRAIT = (1024, 1024)

MAGENTA = np.array([255, 0, 255], dtype=np.int16)


def load_rgb(path: Path) -> np.ndarray:
    with Image.open(path) as im:
        return np.array(im.convert("RGB"), dtype=np.uint8)


def key_void(rgb: np.ndarray) -> np.ndarray:
    mx = rgb.max(axis=2)
    lum = rgb.mean(axis=2)
    # Near-black void + orange embers (small saturated warm dots).
    ember = (rgb[:, :, 0] > 90) & (rgb[:, :, 0] > rgb[:, :, 1] + 25) & (rgb[:, :, 2] < 80) & (mx < 200)
    void = (mx < 28) | ((lum < 18) & (mx < 40)) | ember
    return ~void


def skin_like(rgb: np.ndarray) -> np.ndarray:
    r, g, b = rgb[:, :, 0], rgb[:, :, 1], rgb[:, :, 2]
    return (
        (r > 118)
        & (g > 68)
        & (b > 42)
        & (r > g + 18)
        & (r > b + 28)
        & (r < 230)
        & (g < 180)
    )


def hair_like(rgb: np.ndarray) -> np.ndarray:
    r, g, b = rgb[:, :, 0], rgb[:, :, 1], rgb[:, :, 2]
    mx = rgb.max(axis=2)
    mn = rgb.min(axis=2)
    ash = (mx > 38) & (mx < 200) & ((r.astype(np.int16) - g) > -8) & ((r.astype(np.int16) - g) < 55) & (
        (g.astype(np.int16) - b) > -5
    ) & ((g.astype(np.int16) - b) < 50) & ~skin_like(rgb)
    moss = (g > 42) & (g > r.astype(np.int16) - 12) & (g > b.astype(np.int16) + 8) & (g < 160) & (r < 140)
    gold = (r > 150) & (g > 95) & (b < 90) & (r > b + 50)
    return (ash | moss | gold) & (mx > 28)


def gold_like(rgb: np.ndarray) -> np.ndarray:
    r, g, b = rgb[:, :, 0], rgb[:, :, 1], rgb[:, :, 2]
    return (r > 145) & (g > 90) & (b < 95) & (r > b + 45)


def contain(im: Image.Image, size: tuple[int, int], pad=(0, 0, 0, 0)) -> Image.Image:
    dw, dh = size
    sw, sh = im.size
    scale = min(dw / sw, dh / sh)
    nw, nh = max(1, int(round(sw * scale))), max(1, int(round(sh * scale)))
    fitted = im.resize((nw, nh), Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", size, pad)
    canvas.paste(fitted, ((dw - nw) // 2, (dh - nh) // 2))
    return canvas


def rgba_from_mask(rgb: np.ndarray, mask: np.ndarray) -> Image.Image:
    a = (mask.astype(np.uint8) * 255)
    out = np.dstack([rgb, a])
    return Image.fromarray(out, "RGBA")


def magenta_mask(rgb: np.ndarray) -> np.ndarray:
    d = np.abs(rgb.astype(np.int16) - MAGENTA).sum(axis=2)
    return d > 80


def split_master(rgb: np.ndarray):
    h, w = rgb.shape[:2]
    ys = np.linspace(0, 1, h, endpoint=False)[:, None]
    xs = np.linspace(0, 1, w, endpoint=False)[None, :]
    fg = key_void(rgb)
    skin = skin_like(rgb) & fg
    hair = hair_like(rgb) & fg & ~skin

    # Outer plates only. Clothes, bow, and face stay on the body so the
    # paper doll does not grow holes. Hair_front is side bangs, not the face.
    hair_front = hair & (ys < 0.22) & (((xs > 0.16) & (xs < 0.36)) | ((xs > 0.66) & (xs < 0.86)))
    # Left cloak only. The right edge holds the bow; do not steal it.
    hair_back = hair & ~gold_like(rgb) & (ys > 0.10) & (ys < 0.78) & (xs < 0.26)
    bow = np.zeros(fg.shape, dtype=bool)

    body = fg.copy()
    body &= ~hair_front
    body &= ~hair_back

    return {
        "fg": fg,
        "hair_front": hair_front,
        "hair_back": hair_back,
        "bow": bow,
        "body": body,
        "skin": skin,
    }


def write_debug(masks: dict[str, np.ndarray], rgb: np.ndarray, folder: Path):
    folder.mkdir(parents=True, exist_ok=True)
    for name, mask in masks.items():
        rgba_from_mask(rgb, mask).save(folder / f"mask_{name}.png")


def html_preview(rel: dict[str, str]) -> str:
    return f"""<!doctype html>
<meta charset="utf-8">
<title>C006 荆棘猎手 · 分层预览</title>
<style>
  html,body {{ margin:0; height:100%; background:#070605; color:#f3e6d2; font-family:sans-serif; }}
  .stage {{
    position:relative; width:min(72vh, 420px); height:min(108vh, 630px);
    margin:24px auto; overflow:hidden; background:#0a0908;
    box-shadow: 0 0 80px #000;
  }}
  .stage img {{
    position:absolute; inset:0; width:100%; height:100%;
    object-fit:contain; pointer-events:none;
    transform-origin: 50% 18%;
  }}
  #hair-back {{ animation: swayB 3.6s ease-in-out infinite; z-index:1; }}
  #body {{ z-index:2; }}
  #bow {{ animation: swayS 4.2s ease-in-out infinite; transform-origin: 42% 58%; z-index:3; }}
  #hair-front {{ animation: swayF 2.8s ease-in-out infinite; z-index:4; }}
  @keyframes swayF {{ 0%,100% {{ transform: rotate(-1.6deg); }} 50% {{ transform: rotate(1.8deg); }} }}
  @keyframes swayB {{ 0%,100% {{ transform: rotate(1.2deg); }} 50% {{ transform: rotate(-1.4deg); }} }}
  @keyframes swayS {{ 0%,100% {{ transform: rotate(-0.6deg); }} 50% {{ transform: rotate(0.8deg); }} }}
  p {{ text-align:center; opacity:.72; font-size:14px; }}
</style>
<div class="stage">
  <img id="hair-back" src="{rel['hair_back']}" alt="后发">
  <img id="body" src="{rel['body']}" alt="身体">
  <img id="bow" src="{rel['bow']}" alt="弓">
  <img id="hair-front" src="{rel['hair_front']}" alt="前发">
</div>
<p>网页叠层预览 · 不用 Cubism · 左侧后发在转，身体不拧</p>
"""


def main():
    rgb = load_rgb(SRC)
    masks = split_master(rgb)
    LAYERS_ART.mkdir(parents=True, exist_ok=True)
    write_debug(masks, rgb, LAYERS_ART / "_dbg")

    body = contain(rgba_from_mask(rgb, masks["body"]), CANVAS, (0, 0, 0, 0))
    hair_f = contain(rgba_from_mask(rgb, masks["hair_front"]), CANVAS, (0, 0, 0, 0))
    hair_b = contain(rgba_from_mask(rgb, masks["hair_back"]), CANVAS, (0, 0, 0, 0))
    bow = contain(rgba_from_mask(rgb, masks["bow"]), CANVAS, (0, 0, 0, 0))
    still = contain(rgba_from_mask(rgb, masks["fg"]), CANVAS, (0, 0, 0, 0))

    files = {
        "layer_body.png": body,
        "layer_hair_front.png": hair_f,
        "layer_hair_back.png": hair_b,
        "layer_sword.png": bow,
        "composite.png": still,
    }
    for name, im in files.items():
        im.save(LAYERS_ART / name)
        print("wrote", LAYERS_ART / name, im.size)

    PREVIEW.write_text(
        html_preview(
            {
                "body": "gen-layers/layer_body.png",
                "hair_front": "gen-layers/layer_hair_front.png",
                "hair_back": "gen-layers/layer_hair_back.png",
                "bow": "gen-layers/layer_sword.png",
            }
        ),
        encoding="utf-8",
    )
    print("preview", PREVIEW)

    still_src = ART_DIR / "_ingest_presenter.png"
    still.save(still_src)
    h, w = rgb.shape[:2]
    face = Image.fromarray(rgb, "RGB").crop((int(w * 0.18), int(h * 0.02), int(w * 0.82), int(h * 0.45)))
    portrait_src = ART_DIR / "_ingest_portrait.png"
    contain(face.convert("RGBA"), PORTRAIT, (0, 0, 0, 255)).save(portrait_src)

    used = collect_guids(ASSETS)
    ingest_one(CHAR_ID, CHAR_NAME, "presenter", still_src, used)
    ingest_one(CHAR_ID, CHAR_NAME, "portrait", portrait_src, used)

    ensure_folder_meta(CHAR_ID, used)
    UNITY_LAYERS.mkdir(parents=True, exist_ok=True)
    folder_meta = UNITY_DIR / "Layers.meta"
    guid = stable_guid(folder_meta, f"resonance.folder.Art.Characters.{CHAR_ID}.Layers", used)
    if not folder_meta.is_file():
        folder_meta.write_text(
            "fileFormatVersion: 2\nguid: %s\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
            % guid,
            encoding="utf-8",
        )
    for name, im in files.items():
        if name == "composite.png":
            continue
        dest = UNITY_LAYERS / name
        im.save(dest)
        meta = UNITY_LAYERS / (name + ".meta")
        g = stable_guid(meta, f"resonance.texture.Art.Characters.{CHAR_ID}.Layers.{name}", used)
        write_texture_meta(meta, g, enable_mip=False)
        print("unity", dest)

    (LAYERS_ART / "report.json").write_text(
        json.dumps(
            {k: int(v.sum()) for k, v in masks.items()},
            indent=2,
        ),
        encoding="utf-8",
    )


if __name__ == "__main__":
    raise SystemExit(main())
