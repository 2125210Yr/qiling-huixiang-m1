# Turn a magenta-backed sprite JPEG into engine-ready pixel presenter/portrait PNGs.
# Then call ingest_still.py. Does not touch C001.
from __future__ import annotations

import argparse
import subprocess
import sys
from pathlib import Path

from PIL import Image

REPO = Path(__file__).resolve().parents[2]
INGEST = Path(__file__).resolve().parent / "ingest_still.py"
NATIVE = (96, 144)


def sample_bg(rgb):
    w, h = rgb.size
    px = rgb.load()
    samples = []
    for x, y in ((2, 2), (w - 3, 2), (2, h - 3), (w - 3, h - 3), (w // 2, 2), (2, h // 2)):
        samples.append(px[x, y][:3])
    samples.sort()
    return samples[len(samples) // 2]


def key_bg(im, thresh=48):
    rgb = im.convert("RGB")
    br, bg, bb = sample_bg(rgb)
    src = rgb.load()
    w, h = rgb.size
    out = Image.new("RGBA", (w, h))
    dst = out.load()
    t2 = thresh * thresh
    for y in range(h):
        for x in range(w):
            r, g, b = src[x, y]
            d2 = (r - br) * (r - br) + (g - bg) * (g - bg) + (b - bb) * (b - bb)
            if d2 <= t2:
                dst[x, y] = (0, 0, 0, 0)
            else:
                dst[x, y] = (r, g, b, 255)
    return out


def content_bbox(rgba, pad=4):
    a = rgba.getchannel("A")
    box = a.getbbox()
    if not box:
        return (0, 0, rgba.size[0], rgba.size[1])
    l, t, r, b = box
    l = max(0, l - pad)
    t = max(0, t - pad)
    r = min(rgba.size[0], r + pad)
    b = min(rgba.size[1], b + pad)
    return (l, t, r, b)


def to_native(rgba, size=NATIVE):
    dw, dh = size
    cropped = rgba.crop(content_bbox(rgba))
    cw, ch = cropped.size
    scale = min(dw / float(cw), dh / float(ch))
    nw = max(1, int(round(cw * scale)))
    nh = max(1, int(round(ch * scale)))
    small = cropped.resize((nw, nh), Image.Resampling.BOX)
    pal = small.convert("P", palette=Image.ADAPTIVE, colors=16)
    small = pal.convert("RGBA")
    canvas = Image.new("RGBA", (dw, dh), (0, 0, 0, 0))
    canvas.paste(small, ((dw - nw) // 2, (dh - nh) // 2), small)
    return canvas


def nn_fit(native, dst_size):
    dw, dh = dst_size
    nw, nh = native.size
    k = min(dw // nw, dh // nh)
    if k < 1:
        k = 1
    scaled = native.resize((nw * k, nh * k), Image.Resampling.NEAREST)
    canvas = Image.new("RGBA", (dw, dh), (0, 0, 0, 0))
    canvas.paste(scaled, ((dw - scaled.size[0]) // 2, (dh - scaled.size[1]) // 2), scaled)
    return canvas


def portrait_from_native(native):
    w, h = native.size
    side = w
    top = max(0, int(h * 0.02))
    crop = native.crop((0, top, w, min(h, top + side)))
    if crop.size[1] < side:
        pad = Image.new("RGBA", (side, side), (0, 0, 0, 0))
        pad.paste(crop, (0, 0), crop)
        crop = pad
    return crop


def patch_point_filter(meta_path):
    text = meta_path.read_text(encoding="utf-8")
    text = text.replace("filterMode: 1", "filterMode: 0")
    text = text.replace("enableMipMap: 1", "enableMipMap: 0")
    meta_path.write_text(text, encoding="utf-8")


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--id", required=True)
    p.add_argument("--name", required=True)
    p.add_argument("--src", required=True)
    p.add_argument("--force", action="store_true", help="Allow overwriting C001.")
    args = p.parse_args()
    src = Path(args.src)
    keyed = key_bg(Image.open(src))
    native = to_native(keyed)
    art = REPO / "art" / "characters" / ("%s-%s" % (args.id, args.name))
    art.mkdir(parents=True, exist_ok=True)
    native_path = art / "presenter-pixel.png"
    native.save(native_path)
    pres = nn_fit(native, (1024, 1536))
    port = nn_fit(portrait_from_native(native), (1024, 1024))
    pres_path = art / "_ing_presenter.png"
    port_path = art / "_ing_portrait.png"
    pres.save(pres_path)
    port.save(port_path)
    cmd = [
        sys.executable,
        str(INGEST),
        "--id",
        args.id,
        "--name",
        args.name,
        "--presenter",
        str(pres_path),
        "--portrait",
        str(port_path),
    ]
    if args.force:
        cmd.append("--force")
    subprocess.check_call(cmd)
    unity = REPO / "client" / "Assets" / "Resources" / "Art" / "Characters" / args.id
    patch_point_filter(unity / "presenter.png.meta")
    patch_point_filter(unity / "portrait.png.meta")
    print("pixel %s native=%dx%d" % (args.id, native.size[0], native.size[1]))


if __name__ == "__main__":
    raise SystemExit(main())
