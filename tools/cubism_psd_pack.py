"""Prepare named PNG layers for import into Live2D Cubism Editor.

The manifest and ordered PNG fallback require only Python's standard library.
If Pillow and psd-tools are installed, the script also writes a simple layered
PSD whose layer order matches the manifest.
"""
from __future__ import annotations

import argparse
import json
import shutil
import struct
import sys
from pathlib import Path
from typing import Any


PNG_SIGNATURE = b"\x89PNG\r\n\x1a\n"


def png_size(path: Path) -> tuple[int, int]:
    """Read a PNG's dimensions without requiring Pillow."""
    with path.open("rb") as stream:
        header = stream.read(24)
    if len(header) < 24 or header[:8] != PNG_SIGNATURE or header[12:16] != b"IHDR":
        raise ValueError(f"not a valid PNG: {path}")
    return struct.unpack(">II", header[16:24])


def load_pivots(path: Path | None) -> tuple[dict[str, Any] | None, Path | None]:
    if path is None or not path.is_file():
        return None, None
    data = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(data, dict):
        raise ValueError(f"pivots JSON must contain an object: {path}")
    return data, path


def resolve_order(pngs: list[Path], requested: str | None) -> list[Path]:
    by_name = {path.name.casefold(): path for path in pngs}
    by_stem = {path.stem.casefold(): path for path in pngs}
    if not requested:
        return sorted(pngs, key=lambda path: path.name.casefold())

    result: list[Path] = []
    for item in (part.strip() for part in requested.split(",")):
        if not item:
            continue
        path = by_name.get(item.casefold()) or by_stem.get(item.casefold())
        if path is None:
            raise ValueError(f"--order names an unknown layer: {item}")
        if path in result:
            raise ValueError(f"--order names a layer more than once: {item}")
        result.append(path)
    missing = [path.name for path in pngs if path not in result]
    if missing:
        raise ValueError("--order must include every PNG; missing: " + ", ".join(missing))
    return result


def pivot_for_layer(pivots: dict[str, Any] | None, stem: str) -> Any:
    if not pivots:
        return None
    if stem in pivots:
        return pivots[stem]
    folded = stem.casefold().replace("_", "").replace("-", "")
    for key, value in pivots.items():
        normalized = key.casefold().replace("_", "").replace("-", "")
        if normalized == folded:
            return value
    return None


def write_psd(layer_paths: list[Path], output: Path, canvas: tuple[int, int]) -> None:
    try:
        from PIL import Image
        from psd_tools import PSDImage
        from psd_tools.constants import CompatibilityMode, Compression
    except ImportError as exc:
        raise RuntimeError(
            "PSD skipped: install optional dependencies with "
            "`python -m pip install Pillow psd-tools`."
        ) from exc

    psd = PSDImage.new("RGBA", canvas, color=(0, 0, 0, 0))
    psd.compatibility_mode = CompatibilityMode.PHOTOSHOP
    for path in layer_paths:
        with Image.open(path) as source:
            image = source.convert("RGBA")
            layer = psd.create_pixel_layer(
                image,
                name=path.stem,
                top=0,
                left=0,
                compression=Compression.RLE,
            )
        if layer.has_mask():
            layer.remove_mask()
        psd.append(layer)

    # Cubism expects Photoshop-style merged transparency ("Maximize
    # Compatibility"). psd-tools currently exposes this detail internally.
    psd._update_record()
    layer_info = psd._record.layer_and_mask_information.layer_info
    layer_info.layer_count = -abs(layer_info.layer_count)
    with output.open("wb") as stream:
        psd.save(stream)


def fallback_readme(psd_status: str, psd_name: str) -> str:
    return f"""# Cubism layer import pack

Layer order is **back to front**: index 0000 is the rearmost layer. The same
order and original filenames are recorded in `layer-manifest.json`.

PSD status: {psd_status}

## Cubism Editor import checklist

1. If `{psd_name}` exists, drag it into Cubism Editor or use **File > Open**.
2. Otherwise, open the files in `ordered-png/` as layers in Photoshop, Krita,
   Photopea, or another PSD-capable editor. Keep every layer at canvas origin
   `(0, 0)` and preserve the numbered back-to-front order.
3. Remove the numeric prefixes from layer names if desired; the manifest's
   `name` values are the intended ArtMesh names.
4. Export an RGB/RGBA PSD with transparency and Photoshop compatibility
   (merged image / Maximize Compatibility) enabled.
5. Import the PSD into Cubism Editor. Confirm canvas size, layer order, alpha,
   and any pivot hints from `layer-manifest.json` before creating ArtMeshes.

To enable direct PSD output:

```text
python -m pip install Pillow psd-tools
```
"""


def build_pack(args: argparse.Namespace) -> int:
    source = args.input.resolve()
    output = args.output.resolve()
    if not source.is_dir():
        raise ValueError(f"input folder does not exist: {source}")
    pngs = [path for path in source.iterdir() if path.is_file() and path.suffix.casefold() == ".png"]
    if not pngs:
        raise ValueError(f"no PNG layers found in: {source}")
    layers = resolve_order(pngs, args.order)

    sizes = {path: png_size(path) for path in layers}
    unique_sizes = set(sizes.values())
    if len(unique_sizes) != 1:
        details = ", ".join(f"{path.name}={size[0]}x{size[1]}" for path, size in sizes.items())
        raise ValueError(
            "all layers must use one full-canvas size so their offsets remain aligned: " + details
        )
    canvas = next(iter(unique_sizes))

    pivot_path = args.pivots.resolve() if args.pivots else source / "pivots.json"
    pivots, used_pivot_path = load_pivots(pivot_path)
    output.mkdir(parents=True, exist_ok=True)
    ordered_dir = output / "ordered-png"
    ordered_dir.mkdir(parents=True, exist_ok=True)
    for stale in ordered_dir.glob("*.png"):
        stale.unlink()

    manifest_layers = []
    digits = max(4, len(str(len(layers) - 1)))
    for index, path in enumerate(layers):
        ordered_name = f"{index:0{digits}d}_{path.name}"
        shutil.copy2(path, ordered_dir / ordered_name)
        entry: dict[str, Any] = {
            "index": index,
            "name": path.stem,
            "sourceFile": path.name,
            "orderedFile": f"ordered-png/{ordered_name}",
            "width": sizes[path][0],
            "height": sizes[path][1],
            "offset": {"x": 0, "y": 0},
        }
        pivot = pivot_for_layer(pivots, path.stem)
        if pivot is not None:
            entry["pivot"] = pivot
        manifest_layers.append(entry)

    psd_name = args.psd_name
    psd_status = "disabled with --no-psd"
    psd_path = output / psd_name
    if not args.no_psd:
        try:
            write_psd(layers, psd_path, canvas)
            psd_status = f"written to `{psd_name}`"
        except RuntimeError as exc:
            psd_status = str(exc)
            if psd_path.exists():
                psd_path.unlink()

    manifest = {
        "format": "cubism-layer-manifest",
        "version": 1,
        "order": "back-to-front",
        "canvas": {"width": canvas[0], "height": canvas[1]},
        "sourceFolder": str(source),
        "pivotsFile": str(used_pivot_path) if used_pivot_path else None,
        "pivots": pivots,
        "psd": psd_name if psd_path.is_file() else None,
        "layers": manifest_layers,
    }
    (output / "layer-manifest.json").write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    (output / "README.md").write_text(fallback_readme(psd_status, psd_name), encoding="utf-8")
    print(f"Wrote {len(layers)} layers ({canvas[0]}x{canvas[1]}) to {output}")
    print(f"PSD: {psd_status}")
    return 0


def parse_args(argv: list[str] | None = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Write a Cubism layer manifest, ordered PNG fallback, and optional PSD."
    )
    parser.add_argument("input", type=Path, help="folder containing full-canvas named PNG layers")
    parser.add_argument("-o", "--output", type=Path, required=True, help="output pack folder")
    parser.add_argument("--pivots", type=Path, help="optional pivots JSON (defaults to INPUT/pivots.json)")
    parser.add_argument(
        "--order",
        help="comma-separated layer filenames or stems, back to front (default: filename sort)",
    )
    parser.add_argument("--no-psd", action="store_true", help="do not attempt optional PSD creation")
    parser.add_argument("--psd-name", default="cubism-layers.psd", help="PSD filename inside output")
    return parser.parse_args(argv)


def main(argv: list[str] | None = None) -> int:
    try:
        return build_pack(parse_args(argv))
    except (OSError, ValueError, json.JSONDecodeError) as exc:
        print(f"error: {exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
