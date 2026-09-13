"""Build a Cubism-importable PSD from C001 paper-doll layers.

Cubism only binds textures from Photoshop-shaped PSDs: RGB document with a
4th merged alpha, negative layer count (Maximize Compatibility), VersionInfo
hasRealMergedData, and per-layer transparency channel -1 (not a user mask).
psd-tools writes that layout when the document mode is RGBA.
"""
from __future__ import annotations

from pathlib import Path

from PIL import Image
from psd_tools import PSDImage
from psd_tools.constants import CompatibilityMode, Compression

SRC = Path(r"F:\Resonance\client\Assets\Resources\Art\Characters\C001")
OUT_DIR = Path(r"F:\Resonance\cubism\C001")
OUT_PSD = OUT_DIR / "yanren.psd"

# Cubism draw order: first listed = back. Names stay ASCII for the importer.
LAYERS = [
    ("hair_back", "layer_hair_back.png"),
    ("body", "layer_body.png"),
    ("sword", "layer_sword.png"),
    ("hair_front", "layer_hair_front.png"),
]


def trim_rgba(im: Image.Image, margin: int = 2):
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


def main():
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    psd = PSDImage.new("RGBA", (1024, 1536), color=(0, 0, 0, 0))
    psd.compatibility_mode = CompatibilityMode.PHOTOSHOP
    group = psd.create_group(name="yanren", open_folder=True)

    for name, filename in LAYERS:
        src = SRC / filename
        if not src.exists():
            raise SystemExit(f"missing {src}")
        dest = OUT_DIR / filename
        dest.write_bytes(src.read_bytes())
        im = Image.open(src).convert("RGBA")
        cropped, top, left = trim_rgba(im)
        layer = psd.create_pixel_layer(
            cropped,
            name=name,
            top=top,
            left=left,
            compression=Compression.RLE,
        )
        if layer.has_mask():
            layer.remove_mask()
        group.append(layer)
        print(f"  {name} bbox ({left},{top}) {cropped.size[0]}x{cropped.size[1]}")

    # Maximize Compatibility: merged image includes transparency.
    psd._update_record()
    info = psd._record.layer_and_mask_information.layer_info
    info.layer_count = -abs(info.layer_count)

    with OUT_PSD.open("wb") as fd:
        psd.save(fd)
    print(
        f"wrote {OUT_PSD} ({OUT_PSD.stat().st_size} bytes) "
        f"ch={psd._record.header.channels} "
        f"layer_count={info.layer_count}"
    )


if __name__ == "__main__":
    main()
