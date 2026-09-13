# Cubism layer import pack

Layer order is **back to front**: index 0000 is the rearmost layer. The same
order and original filenames are recorded in `layer-manifest.json`.

PSD status: disabled with --no-psd

## Cubism Editor import checklist

1. If `cubism-layers.psd` exists, drag it into Cubism Editor or use **File > Open**.
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
