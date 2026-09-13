"""C027 rig plates: figure alpha AND traced envelope AND material gate.

Adapted from art/characters/C026-\u767d\u4e1d/split_contour.py (poly / flood / drop_specks /
smooth_mask / contact sheet / manifest conventions).

Why three terms instead of C026's two: C026 split an alpha-cut presenter, so a colour flood
was enough. This source is an opaque video frame in which the figure and the elevator share
one red/violet palette. So:
  1. figure alpha  - removes the elevator. Reuses ../_probe/alpha_u2net.png if present
                     (rembg u2net), else falls back to the union of the traced envelopes.
  2. traced envelope - assigns pixels to a body part. Polygons live in layer_plan.json.
  3. material gate   - trims envelope overshoot at part borders (thresholds in layer_plan.json).

Plates are then claimed front-to-back so the nearer part owns contested pixels.
Output is BLOCKING quality; see README.md.
"""
from __future__ import annotations

import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy import ndimage as ndi

ROOT = Path(__file__).resolve().parent
CHAR = ROOT.parent
PLAN = ROOT / "layer_plan.json"
SRC = CHAR / "reference-work.png"
ALPHA_CANDIDATES = (
    CHAR / "_probe" / "alpha_u2net.png",
    CHAR / "_probe" / "alpha_isnet-general-use.png",
    CHAR / "_probe" / "alpha_u2net_human_seg.png",
)
OUT = ROOT / "layers"
DBG = OUT / "_dbg"

MIN_SPECK = 120


def poly_mask(h: int, w: int, pts) -> np.ndarray:
    m = Image.new("L", (w, h), 0)
    ImageDraw.Draw(m).polygon([(int(x), int(y)) for x, y in pts], fill=255)
    return np.asarray(m) > 0


def close_open(mask: np.ndarray, close: int = 2, open_: int = 1) -> np.ndarray:
    st = ndi.generate_binary_structure(2, 1)
    if close:
        mask = ndi.binary_closing(mask, structure=st, iterations=close)
    if open_:
        mask = ndi.binary_opening(mask, structure=st, iterations=open_)
    return mask


def drop_specks(mask: np.ndarray, min_pix: int) -> np.ndarray:
    lab, n = ndi.label(mask)
    if n == 0:
        return mask
    sizes = ndi.sum_labels(mask, lab, index=np.arange(1, n + 1))
    keep = np.zeros(n + 1, bool)
    keep[1:] = sizes >= min_pix
    return keep[lab]


def smooth_mask(mask: np.ndarray, sigma: float = 1.2, thresh: float = 0.45) -> np.ndarray:
    if not mask.any():
        return mask
    return ndi.gaussian_filter(mask.astype(np.float32), sigma) >= thresh


def checker(h: int, w: int, cell: int = 16) -> np.ndarray:
    yy, xx = np.indices((h, w))
    ch = np.zeros((h, w, 3), np.uint8)
    odd = ((yy // cell) + (xx // cell)) % 2 == 0
    ch[odd] = (86, 86, 86)
    ch[~odd] = (48, 48, 48)
    return ch


def font(size: int):
    try:
        return ImageFont.truetype("arial.ttf", size)
    except OSError:
        return ImageFont.load_default()


def load_figure_alpha(h: int, w: int) -> tuple[np.ndarray | None, str]:
    for path in ALPHA_CANDIDATES:
        if not path.is_file():
            continue
        im = Image.open(path)
        a = np.array(im.convert("RGBA"))[:, :, 3] if im.mode == "RGBA" else np.array(im.convert("L"))
        if a.shape != (h, w):
            a = np.array(Image.fromarray(a).resize((w, h), Image.Resampling.BILINEAR))
        return a > 96, path.name
    return None, "none (envelope union)"


def draw_clips(rgb: np.ndarray, plan: dict, path: Path) -> None:
    """Envelope + pivot overlay, so the trace can be corrected without reading code."""
    vis = Image.fromarray(rgb).convert("RGB")
    d = ImageDraw.Draw(vis, "RGBA")
    f = font(20)
    rng = np.random.default_rng(7)
    for layer in plan["layers"]:
        col = tuple(int(v) for v in rng.integers(70, 255, size=3))
        pts = [(int(x), int(y)) for x, y in layer["polygon"]]
        d.polygon(pts, fill=col + (46,))
        d.line(pts + [pts[0]], fill=col + (255,), width=3)
        px, py = layer["pivot_px"]
        d.line([(px - 14, py), (px + 14, py)], fill=(255, 255, 255, 255), width=3)
        d.line([(px, py - 14), (px, py + 14)], fill=(255, 255, 255, 255), width=3)
        d.text((pts[0][0] + 4, pts[0][1] + 4), layer["name"], fill=col + (255,), font=f)
    vis.save(path)


def write_contact(order, w: int, h: int) -> None:
    cols = 5
    rows = (len(order) + cols - 1) // cols
    cell_w, cell_h = 320, 400
    sheet = np.full((rows * cell_h, cols * cell_w, 3), 28, np.uint8)
    f = font(16)
    for i, name in enumerate(order):
        rr, cc = divmod(i, cols)
        plate = np.array(Image.open(OUT / f"{name}.png"))
        m = plate[:, :, 3] > 8
        cell = checker(cell_h - 28, cell_w, 12)
        if m.any():
            ys, xs = np.where(m)
            x0, x1 = max(0, xs.min() - 6), min(w, xs.max() + 7)
            y0, y1 = max(0, ys.min() - 6), min(h, ys.max() + 7)
            crop = plate[y0:y1, x0:x1]
            ch, cw = crop.shape[:2]
            scale = min((cell_w - 16) / cw, (cell_h - 44) / ch)
            nw, nh = max(1, int(cw * scale)), max(1, int(ch * scale))
            crop_np = np.array(Image.fromarray(crop).resize((nw, nh), Image.Resampling.LANCZOS))
            ox, oy = (cell_w - nw) // 2, (cell_h - 28 - nh) // 2
            aa = crop_np[:, :, 3:4].astype(np.float32) / 255.0
            region = cell[oy : oy + nh, ox : ox + nw]
            cell[oy : oy + nh, ox : ox + nw] = (crop_np[:, :, :3] * aa + region * (1 - aa)).astype(np.uint8)
        tile = np.full((cell_h, cell_w, 3), 22, np.uint8)
        tile[28:] = cell
        tile_im = Image.fromarray(tile)
        ImageDraw.Draw(tile_im).text((8, 6), f"{i}. {name}", fill=(230, 230, 230), font=f)
        sheet[rr * cell_h : (rr + 1) * cell_h, cc * cell_w : (cc + 1) * cell_w] = np.array(tile_im)
    Image.fromarray(sheet).save(OUT / "contact-layers.jpg", quality=93)


def main() -> None:
    plan = json.loads(PLAN.read_text(encoding="utf-8"))
    order = plan["draw_order_back_to_front"]
    by_name = {layer["name"]: layer for layer in plan["layers"]}
    assert set(order) == set(by_name), "draw order and layers disagree"

    OUT.mkdir(parents=True, exist_ok=True)
    DBG.mkdir(parents=True, exist_ok=True)
    for p in list(OUT.glob("*.png")) + list(OUT.glob("*.jpg")) + list(DBG.glob("*.png")):
        p.unlink()

    rgb = np.array(Image.open(SRC).convert("RGB"))
    h, w = rgb.shape[:2]
    assert [w, h] == plan["canvas"]["work_size"], (w, h)
    a = rgb.astype(np.int16)
    r, g, b = a[:, :, 0], a[:, :, 1], a[:, :, 2]
    luma = a.mean(axis=2)
    chroma = a.max(axis=2) - a.min(axis=2)

    # Transcribed from layer_plan.json -> material_gates (measured in ../_probe/probe.json).
    gates = {
        "skin": (luma > 118) & (r >= g - 2) & (r > b - 8),
        "dress": (r - g > 42) & (r - b > 40) & (g >= b - 8) & (luma < 132),
    }
    # Only the rim-lit hair is actually cool (b > r); the shadowed bulk picks up red bounce off
    # the door and reads warm, so a positive colour rule finds the outline and misses the mass.
    # Inside the hair envelope AND the figure alpha, whatever is neither skin nor silk is hair.
    gates["hair"] = ~gates["skin"] & ~gates["dress"] & (luma < 150)
    for name, m in gates.items():
        Image.fromarray(m.astype(np.uint8) * 255).save(DBG / f"gate_{name}.png")

    envelopes = {name: poly_mask(h, w, layer["polygon"]) for name, layer in by_name.items()}
    env_union = np.logical_or.reduce([envelopes[n] for n in order])
    fig, alpha_src = load_figure_alpha(h, w)
    if fig is None:
        fig = env_union
    else:
        # Envelopes are traced a little wide on purpose; the alpha is the authority on the silhouette.
        fig = fig | (env_union & ndi.binary_dilation(fig, iterations=6))
    Image.fromarray(fig.astype(np.uint8) * 255).save(DBG / "figure_alpha.png")
    draw_clips(rgb, plan, DBG / "clips.png")

    plates: dict[str, np.ndarray] = {}
    for name in order:
        layer = by_name[name]
        env = envelopes[name] & fig
        if layer["gate"] == "highlight":
            # Anklet sits on skin with no colour separation: take the bright tail inside the envelope.
            vals = luma[env] if env.any() else luma.ravel()
            cut = float(np.median(vals) + 0.85 * vals.std())
            m = drop_specks(close_open(env & (luma >= cut), 1, 0), 90)
        else:
            m = drop_specks(close_open(env & gates[layer["gate"]]), MIN_SPECK)
            m = smooth_mask(m) & env
        plates[name] = m

    # Front plates own contested pixels: walk the draw order backwards and subtract what is claimed.
    claimed = np.zeros((h, w), bool)
    for name in reversed(order):
        plates[name] &= ~claimed
        plates[name] = drop_specks(plates[name], 40)
        claimed |= plates[name]

    rgba = np.dstack([rgb, np.full((h, w), 255, np.uint8)])
    z = np.zeros((h, w, 4), np.uint8)
    counts, bboxes = {}, {}
    for name in order:
        m = plates[name]
        plate = z.copy()
        plate[m] = rgba[m]
        Image.fromarray(plate).save(OUT / f"{name}.png")
        counts[name] = int(m.sum())
        bboxes[name] = None
        if m.any():
            ys, xs = np.where(m)
            bboxes[name] = [int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max())]
        print(f"  {name:14s} {counts[name]:8d}  bbox={bboxes[name]}")

    comp = z.copy()
    for name in order:
        comp[plates[name]] = rgba[plates[name]]
    Image.fromarray(comp).save(OUT / "composite.png")

    # The source is opaque, so the only meaningful check is "how much of the figure did the
    # plates keep, and where did they drop it".
    missed = fig & ~claimed
    Image.fromarray(missed.astype(np.uint8) * 255).save(DBG / "figure_leftover.png")

    vis = rgb.copy()
    rng = np.random.default_rng(3)
    for name in order:
        col = rng.integers(40, 255, size=3)
        m = plates[name]
        vis[m] = (vis[m] * 0.35 + col * 0.65).astype(np.uint8)
    Image.fromarray(vis).save(DBG / "mask_overlay.png")

    write_contact(order, w, h)

    manifest = {
        "source": SRC.name,
        "plan": PLAN.name,
        "size": [w, h],
        "quality": "blocking",
        "method": "figure alpha AND traced envelope AND material gate; front-to-back exclusive claim; no Photoshop",
        "figure_alpha": alpha_src,
        "draw_order_back_to_front": order,
        "counts": counts,
        "bboxes": bboxes,
        "figure_px": int(fig.sum()),
        "claimed_px": int(claimed.sum()),
        "figure_coverage": round(float(claimed.sum() / max(1, fig.sum())), 4),
        "figure_px_not_covered": int(missed.sum()),
    }
    (OUT / "manifest.json").write_text(json.dumps(manifest, indent=2, ensure_ascii=False), encoding="utf-8")
    print("figure coverage", manifest["figure_coverage"], "alpha", alpha_src, "->", OUT)


if __name__ == "__main__":
    main()
