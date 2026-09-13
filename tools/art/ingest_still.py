# Ingest still presenter/portrait PNGs into art/ and Unity Resources.
# Requires: pip install Pillow
"""Fit still illustrations onto presenter/portrait canvases. No stretch, no chroma-key."""

from __future__ import annotations

import argparse
import hashlib
import re
import shutil
import sys
from pathlib import Path

try:
    from PIL import Image, ImageOps
except ImportError:
    raise SystemExit("Pillow is required: pip install Pillow")

try:
    RESAMPLE = Image.Resampling.LANCZOS
except AttributeError:
    RESAMPLE = Image.LANCZOS

REPO = Path(__file__).resolve().parents[2]
ASSETS = REPO / "client" / "Assets"
ART_CHARACTERS = REPO / "art" / "characters"
UNITY_CHARACTERS = ASSETS / "Resources" / "Art" / "Characters"

PRESENTER_SIZE = (1024, 1536)
PORTRAIT_SIZE = (1024, 1024)
CONSTITUTION_ID = "C001"
ID_RE = re.compile(r"^C\d{3}$")
GUID_RE = re.compile(r"^guid:\s*([0-9a-fA-F]{32})\s*$", re.M)
BAD_NAME_CHARS = '\\/:*?"<>|\n\r\t'

FOLDER_META = """fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""

TEXTURE_META = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: {mip}
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 0
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 0
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: Standalone
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: Android
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData: 
    physicsShape: []
    bones: []
    spriteID: 
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def _write_text(path, text):
    if not text.endswith("\n"):
        text += "\n"
    path.parent.mkdir(parents=True, exist_ok=True)
    with open(path, "w", encoding="utf-8", newline="\n") as fh:
        fh.write(text)


def collect_guids(assets_root):
    used = set()
    if not assets_root.is_dir():
        return used
    for meta in assets_root.rglob("*.meta"):
        try:
            text = meta.read_text(encoding="utf-8")
        except OSError:
            continue
        match = GUID_RE.search(text)
        if match:
            used.add(match.group(1).lower())
    return used


def read_guid(meta_path):
    if not meta_path.is_file():
        return None
    match = GUID_RE.search(meta_path.read_text(encoding="utf-8"))
    if not match:
        return None
    return match.group(1).lower()


def make_guid(key, used):
    n = 0
    seed = key
    while True:
        guid = hashlib.md5(seed.encode("utf-8")).hexdigest()
        if guid not in used:
            used.add(guid)
            return guid
        n += 1
        seed = "%s#%d" % (key, n)


def stable_guid(meta_path, key, used):
    existing = read_guid(meta_path)
    if existing:
        used.add(existing)
        return existing
    return make_guid(key, used)


def source_has_transparency(rgba):
    extrema = rgba.getextrema()
    if not extrema:
        return False
    return extrema[3][0] < 255


def contain_size(src_w, src_h, dst_w, dst_h):
    if src_w <= 0 or src_h <= 0:
        raise ValueError("source image has empty size")
    scale = min(dst_w / float(src_w), dst_h / float(src_h))
    fit_w = max(1, int(round(src_w * scale)))
    fit_h = max(1, int(round(src_h * scale)))
    if fit_w > dst_w:
        fit_w = dst_w
    if fit_h > dst_h:
        fit_h = dst_h
    return fit_w, fit_h


def fit_contain(src_rgba, dst_size):
    dst_w, dst_h = dst_size
    src_w, src_h = src_rgba.size
    if source_has_transparency(src_rgba):
        pad = (0, 0, 0, 0)
    else:
        pad = (0, 0, 0, 255)
    if (src_w, src_h) == (dst_w, dst_h):
        return src_rgba.copy()
    fit_w, fit_h = contain_size(src_w, src_h, dst_w, dst_h)
    fitted = src_rgba.resize((fit_w, fit_h), RESAMPLE)
    canvas = Image.new("RGBA", (dst_w, dst_h), pad)
    # Copy pixels; do not composite onto the pad (would darken semi-transparent edges).
    canvas.paste(fitted, ((dst_w - fit_w) // 2, (dst_h - fit_h) // 2))
    return canvas


def load_rgba(path):
    path = Path(path)
    if not path.is_file():
        raise SystemExit("missing source: %s" % path)
    with Image.open(path) as im:
        im.load()
        try:
            im = ImageOps.exif_transpose(im)
        except Exception:
            pass
        return im.convert("RGBA")


def ensure_folder_meta(char_id, used):
    folder = UNITY_CHARACTERS / char_id
    folder.mkdir(parents=True, exist_ok=True)
    meta_path = UNITY_CHARACTERS / ("%s.meta" % char_id)
    guid = stable_guid(meta_path, "resonance.folder.Art.Characters.%s" % char_id, used)
    if not meta_path.is_file():
        _write_text(meta_path, FOLDER_META.format(guid=guid))
    return guid


def write_texture_meta(meta_path, guid, enable_mip):
    _write_text(
        meta_path,
        TEXTURE_META.format(guid=guid, mip=1 if enable_mip else 0),
    )


def ingest_one(char_id, name, kind, src_path, used):
    if kind == "presenter":
        size = PRESENTER_SIZE
        filename = "presenter.png"
        enable_mip = False
    elif kind == "portrait":
        size = PORTRAIT_SIZE
        filename = "portrait.png"
        enable_mip = True
    else:
        raise ValueError("unknown kind: %s" % kind)

    rgba = load_rgba(src_path)
    canvas = fit_contain(rgba, size)

    art_dir = ART_CHARACTERS / ("%s-%s" % (char_id, name))
    unity_dir = UNITY_CHARACTERS / char_id
    art_dir.mkdir(parents=True, exist_ok=True)
    unity_dir.mkdir(parents=True, exist_ok=True)

    art_png = art_dir / filename
    unity_png = unity_dir / filename
    meta_path = unity_dir / ("%s.meta" % filename)

    guid = stable_guid(
        meta_path,
        "resonance.texture.Art.Characters.%s.%s" % (char_id, filename),
        used,
    )
    canvas.save(art_png, format="PNG")
    shutil.copyfile(art_png, unity_png)
    write_texture_meta(meta_path, guid, enable_mip)

    check = Image.open(unity_png)
    try:
        mode = check.mode
        width, height = check.size
    finally:
        check.close()
    try:
        shown = unity_png.relative_to(REPO).as_posix()
    except ValueError:
        shown = str(unity_png)
    print("%s %dx%d %s guid=%s" % (shown, width, height, mode, guid))
    return guid


def parse_args(argv):
    parser = argparse.ArgumentParser(
        description="Ingest still presenter/portrait PNG or JPEG (contain, no stretch, no chroma-key)."
    )
    parser.add_argument("--id", required=True, help="Character id, e.g. C007 (C001 is refused).")
    parser.add_argument("--name", required=True, help="Folder name used under art/characters/{id}-{name}.")
    parser.add_argument("--presenter", default=None, help="Source presenter PNG or JPEG (optional).")
    parser.add_argument("--portrait", default=None, help="Source portrait PNG or JPEG (optional).")
    parser.add_argument("--force", action="store_true", help="Allow overwriting C001.")
    return parser.parse_args(argv)


def validate(args):
    char_id = args.id.strip()
    name = args.name.strip()
    if not ID_RE.match(char_id):
        raise SystemExit("invalid --id %r (expected C000-style)" % args.id)
    if char_id == CONSTITUTION_ID and not args.force:
        raise SystemExit("refusing to ingest %s (constitution; pass --force to overwrite)" % CONSTITUTION_ID)
    if not name or any(ch in BAD_NAME_CHARS for ch in name):
        raise SystemExit("invalid --name %r" % args.name)
    if not args.presenter and not args.portrait:
        raise SystemExit("pass --presenter and/or --portrait")
    for label, src in (("--presenter", args.presenter), ("--portrait", args.portrait)):
        if src and not Path(src).is_file():
            raise SystemExit("missing source: %s" % Path(src))
    return char_id, name


def main(argv=None):
    if hasattr(sys.stdout, "reconfigure"):
        try:
            sys.stdout.reconfigure(encoding="utf-8")
        except Exception:
            pass
    args = parse_args(sys.argv[1:] if argv is None else argv)
    char_id, name = validate(args)
    used = collect_guids(ASSETS)
    ensure_folder_meta(char_id, used)
    if args.presenter:
        ingest_one(char_id, name, "presenter", args.presenter, used)
    if args.portrait:
        ingest_one(char_id, name, "portrait", args.portrait, used)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
