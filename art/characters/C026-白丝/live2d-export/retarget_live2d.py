"""C026 -> Live2D part retarget. Read-only on ../layers.

Adapted from split_contour.py (checker / contact-sheet / halfplane / flood
helpers, alpha>8 mask convention, full-canvas plates). It maps the 27 source
plates onto normalized Live2D part names, closes the anti-alias seams between
them so a deformed mesh cannot show cracks, and solves a pivot per part from
where it actually touches its parent.

One geometric adaptation: the source watershed left the whole right arm in
`arm_forearm_r` and only an armpit wedge in `arm_upper_r`, so the raised arm had
no elbow. RECUT_ELBOW_R re-splits that union on the elbow line.

Outputs land in live2d-export/ only. ../layers/*.png is never written.
"""
from __future__ import annotations

import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy import ndimage as ndi
from skimage.graph import MCP_Geometric

ROOT = Path(__file__).resolve().parent.parent
SRC = ROOT / "presenter.png"
LAYERS = ROOT / "layers"
OUT = ROOT / "live2d-export"
QA = OUT / "_qa"

ALPHA_ON = 8

# name, part_id, source plate, parent, pivot rule. Listed back -> front.
PARTS = [
    ("hair_back_l", "PartHairBackL", "hair_back_l", "hair_side_l", {"rule": "ref_centroid", "of": "hair_tie_l"}),
    ("hair_back_r", "PartHairBackR", "hair_back_r", "hair_side_r", {"rule": "ref_centroid", "of": "hair_tie_r"}),
    ("hair_side_l", "PartHairSideL", "hair_top_l", "head", {"rule": "contact", "with": "head"}),
    ("hair_side_r", "PartHairSideR", "hair_top_r", "head", {"rule": "contact", "with": "head"}),
    ("leg_lower_l", "PartLegLowerL", "boot_l", "leg_upper_l", {"rule": "contact", "with": "leg_upper_l"}),
    ("leg_lower_r", "PartLegLowerR", "boot_r", "leg_upper_r", {"rule": "contact", "with": "leg_upper_r"}),
    ("leg_upper_l", "PartLegUpperL", "thigh_l", "body", {"rule": "contact", "with": "body"}),
    ("leg_upper_r", "PartLegUpperR", "thigh_r", "body", {"rule": "contact", "with": "body"}),
    ("body", "PartBody", "torso", None, {"rule": "contact_any", "with": ["leg_upper_l", "leg_upper_r"]}),
    ("chest", "PartChest", "chest", "body", {"rule": "contact", "with": "body"}),
    ("arm_upper_r", "PartArmUpperR", "arm_upper_r", "chest", {"rule": "contact", "with": "chest"}),
    ("arm_lower_r", "PartArmLowerR", "arm_forearm_r", "arm_upper_r", {"rule": "contact", "with": "arm_upper_r"}),
    ("arm_upper_l", "PartArmUpperL", "arm_upper_l", "chest", {"rule": "contact", "with": "chest"}),
    ("arm_lower_l", "PartArmLowerL", "arm_forearm_l", "arm_upper_l", {"rule": "contact", "with": "arm_upper_l"}),
    ("prop_clutch", "PartPropClutch", "clutch", "hand_l", {"rule": "contact", "with": "hand_l"}),
    ("hand_r", "PartHandR", "hand_r", "arm_lower_r", {"rule": "contact", "with": "arm_lower_r"}),
    ("hand_l", "PartHandL", "hand_l", "arm_lower_l", {"rule": "contact", "with": "arm_lower_l"}),
    ("leg_ornament_l", "PartLegOrnamentL", "boot_ornament_l", "leg_lower_l", {"rule": "contact", "with": "leg_lower_l"}),
    ("leg_ornament_r", "PartLegOrnamentR", "boot_ornament_r", "leg_lower_r", {"rule": "contact", "with": "leg_lower_r"}),
    ("accessory_wrist_r", "PartWristR", "jewelry_wrist_r", "auto", {"rule": "contact_best", "with": ["hand_r", "arm_lower_r"]}),
    ("accessory_wrist_l", "PartWristL", "jewelry_wrist_l", "auto", {"rule": "contact_best", "with": ["hand_l", "arm_lower_l"]}),
    ("accessory_choker", "PartChoker", "choker", "head", {"rule": "centroid"}),
    ("head", "PartHead", "head", "chest", {"rule": "contact", "with": "chest"}),
    ("accessory_earring_l", "PartEarringL", "earring_l", "head", {"rule": "bbox", "at": "top_center"}),
    ("accessory_earring_r", "PartEarringR", "earring_r", "head", {"rule": "bbox", "at": "top_center"}),
    ("hair_front", "PartHairFront", "hair_front", "head", {"rule": "contact", "with": "head"}),
    ("accessory_hair_tie_l", "PartHairTieL", "hair_tie_l", "hair_side_l", {"rule": "centroid"}),
    ("accessory_hair_tie_r", "PartHairTieR", "hair_tie_r", "hair_side_r", {"rule": "centroid"}),
]

# Plates carrying the black latex costume -- the recolor target for a red dress.
COSTUME_PARTS = ["chest", "body"]

DILATE_STEPS = (1, 2, 3, 4, 6, 8, 12, 16, 24, 32)

# Right arm joint coords read off _qa/_zoom_arm_r.png and the qa_ascii.py map:
# shoulder root, elbow, wrist. The arm folds into a tight V (upper arm down-left
# from the shoulder, forearm back up-left to the raised hand), so a straight cut
# line runs near-parallel to the forearm and cannot separate the two. The split is
# taken on the geodesic level set through the elbow instead, which follows the limb
# around the fold.
RECUT_ELBOW_R = {
    "upper": "arm_upper_r",
    "lower": "arm_lower_r",
    "root": (300, 215),
    "joint": (240, 315),
}


def checker(h: int, w: int, cell: int = 16) -> np.ndarray:
    yy, xx = np.indices((h, w))
    ch = np.zeros((h, w, 3), np.uint8)
    ch[((yy // cell) + (xx // cell)) % 2 == 0] = (86, 86, 86)
    ch[((yy // cell) + (xx // cell)) % 2 == 1] = (48, 48, 48)
    return ch


def flatten(rgba: np.ndarray) -> np.ndarray:
    h, w = rgba.shape[:2]
    a = rgba[:, :, 3:4].astype(np.float32) / 255.0
    return (rgba[:, :, :3] * a + checker(h, w) * (1 - a)).astype(np.uint8)


def bbox(mask: np.ndarray):
    if not mask.any():
        return None
    ys, xs = np.where(mask)
    return [int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1]


def centroid(mask: np.ndarray):
    ys, xs = np.where(mask)
    return float(xs.mean()), float(ys.mean())


def bbox_anchor(mask: np.ndarray, at: str):
    x0, y0, x1, y1 = bbox(mask)
    cx = (x0 + x1 - 1) / 2.0
    if at == "top_center":
        return cx, float(y0)
    if at == "bottom_center":
        return cx, float(y1 - 1)
    return cx, (y0 + y1 - 1) / 2.0


def component_at(mask: np.ndarray, seed) -> np.ndarray:
    """The connected component of `mask` holding `seed`, snapped to the nearest on-pixel."""
    x, y = seed
    if not mask.any():
        return np.zeros_like(mask)
    if not mask[y, x]:
        ys, xs = np.where(mask)
        i = int(np.argmin((xs - x) ** 2 + (ys - y) ** 2))
        x, y = int(xs[i]), int(ys[i])
        print(f"  seed snapped to {(x, y)}")
    lab, _ = ndi.label(mask)
    return lab == lab[y, x]


def recut_joint(masks: dict, spec: dict) -> None:
    """Re-split two adjacent plates at a joint, in place.

    Walks geodesic distance out from `root` inside the merged silhouette and keeps
    everything at or below the distance of `joint`. Distance follows the limb, so a
    folded arm splits at the elbow instead of at whatever is Euclidean-nearest.
    """
    upper, lower = spec["upper"], spec["lower"]
    whole = masks[upper] | masks[lower]
    rx, ry = spec["root"]
    jx, jy = spec["joint"]
    for label, (px, py) in (("root", spec["root"]), ("joint", spec["joint"])):
        assert whole[py, px], f"{label} {(px, py)} is outside {upper}|{lower}"
    dist, _ = MCP_Geometric(np.where(whole, 1.0, np.inf)).find_costs([[ry, rx]])
    prox = component_at(whole & np.isfinite(dist) & (dist <= dist[jy, jx]), spec["root"])
    masks[upper], masks[lower] = prox, whole & ~prox
    print(
        f"  recut {upper}/{lower} at geodesic {dist[jy, jx]:.0f}px from {spec['root']}: "
        f"{int(masks[upper].sum())} / {int(masks[lower].sum())} px"
    )


def contact(a: np.ndarray, b: np.ndarray, min_pix: int = 8):
    """Centroid of the band where two plates meet, plus the dilation it needed."""
    if not a.any() or not b.any():
        return None
    for it in DILATE_STEPS:
        band = ndi.binary_dilation(a, iterations=it) & ndi.binary_dilation(b, iterations=it)
        if int(band.sum()) >= min_pix:
            cx, cy = centroid(band)
            return cx, cy, it, int(band.sum())
    return None


def solve_pivot(name: str, spec: dict, masks: dict, src_masks: dict):
    """-> (x, y, parent_override, note)."""
    rule = spec["rule"]
    if rule == "centroid":
        cx, cy = centroid(masks[name])
        return cx, cy, None, "plate centroid"
    if rule == "bbox":
        cx, cy = bbox_anchor(masks[name], spec["at"])
        return cx, cy, None, f"bbox {spec['at']}"
    if rule == "ref_centroid":
        ref = spec["of"]
        if src_masks.get(ref) is not None and src_masks[ref].any():
            cx, cy = centroid(src_masks[ref])
            return cx, cy, None, f"centroid of source plate '{ref}' (tail root)"
        cx, cy = bbox_anchor(masks[name], "top_center")
        return cx, cy, None, f"fallback bbox top_center ('{ref}' empty)"
    if rule == "contact":
        hit = contact(masks[name], masks[spec["with"]])
        if hit:
            cx, cy, it, n = hit
            return cx, cy, None, f"contact with '{spec['with']}' (grow {it}px, {n}px band)"
        cx, cy = bbox_anchor(masks[name], "top_center")
        return cx, cy, None, f"fallback bbox top_center (no contact with '{spec['with']}')"
    if rule == "contact_any":
        other = np.logical_or.reduce([masks[k] for k in spec["with"]])
        hit = contact(masks[name], other)
        if hit:
            cx, cy, it, n = hit
            return cx, cy, None, f"contact with {'+'.join(spec['with'])} (grow {it}px, {n}px band)"
        cx, cy = bbox_anchor(masks[name], "bottom_center")
        return cx, cy, None, "fallback bbox bottom_center"
    if rule == "contact_best":
        best = None
        for cand in spec["with"]:
            hit = contact(masks[name], masks[cand])
            if hit and (best is None or hit[3] > best[1][3]):
                best = (cand, hit)
        if best:
            cand, (cx, cy, it, n) = best
            return cx, cy, cand, f"contact with '{cand}' (grow {it}px, {n}px band; best of {spec['with']})"
        cx, cy = centroid(masks[name])
        return cx, cy, spec["with"][0], "fallback plate centroid (no contact)"
    raise ValueError(rule)


def write_contact_sheet(plates_dir: Path, order, path: Path) -> None:
    cols = 5
    rows = (len(order) + cols - 1) // cols
    cell_w, cell_h = 300, 380
    sheet = np.full((rows * cell_h, cols * cell_w, 3), 28, np.uint8)
    try:
        font = ImageFont.truetype("arial.ttf", 15)
    except Exception:
        font = ImageFont.load_default()
    for i, name in enumerate(order):
        rr, cc = divmod(i, cols)
        plate = np.array(Image.open(plates_dir / f"{name}.png"))
        m = plate[:, :, 3] > ALPHA_ON
        cell = checker(cell_h - 28, cell_w, 12)
        if m.any():
            x0, y0, x1, y1 = bbox(m)
            crop = plate[max(0, y0 - 6) : y1 + 6, max(0, x0 - 6) : x1 + 6]
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
        ImageDraw.Draw(tile_im).text((8, 6), f"{i:02d} {name}", fill=(230, 230, 230), font=font)
        sheet[rr * cell_h : (rr + 1) * cell_h, cc * cell_w : (cc + 1) * cell_w] = np.array(tile_im)
    Image.fromarray(sheet).save(path, quality=93)


def write_pivot_overlay(rgba: np.ndarray, pivots: dict, path: Path) -> None:
    im = Image.fromarray(flatten(rgba))
    d = ImageDraw.Draw(im, "RGBA")
    try:
        font = ImageFont.truetype("arial.ttf", 11)
    except Exception:
        font = ImageFont.load_default()
    for name, info in pivots.items():
        parent = info["parent"]
        if parent and parent in pivots:
            px, py = pivots[parent]["pivot_px"]
            x, y = info["pivot_px"]
            d.line([(x, y), (px, py)], fill=(255, 255, 255, 90), width=1)
    for name, info in pivots.items():
        x, y = info["pivot_px"]
        col = (255, 90, 60, 255) if info["parent"] is None else (70, 220, 255, 255)
        d.ellipse([x - 4, y - 4, x + 4, y + 4], fill=col, outline=(20, 20, 20, 255))
        d.text((x + 6, y - 6), name, fill=(255, 255, 255, 235), font=font)
    im.save(path)


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    QA.mkdir(parents=True, exist_ok=True)
    for p in list(OUT.glob("*.png")) + list(QA.glob("*.png")) + list(QA.glob("*.jpg")):
        p.unlink()

    rgba = np.array(Image.open(SRC).convert("RGBA"))
    h, w = rgba.shape[:2]
    on = rgba[:, :, 3] > ALPHA_ON

    order = [p[0] for p in PARTS]
    masks: dict[str, np.ndarray] = {}
    src_masks: dict[str, np.ndarray] = {}
    for name, _pid, src, _parent, _spec in PARTS:
        plate = np.array(Image.open(LAYERS / f"{src}.png").convert("RGBA"))
        assert plate.shape[:2] == (h, w), f"{src} is {plate.shape[:2]}, expected {(h, w)}"
        m = plate[:, :, 3] > ALPHA_ON
        masks[name] = m
        src_masks[src] = m

    # ===== adaptation: give the raised right arm a real elbow =====
    recut_joint(masks, RECUT_ELBOW_R)

    # ===== seam close: hand every unowned opaque pixel to its nearest plate =====
    union = np.logical_or.reduce([masks[k] for k in order])
    leftover = on & ~union
    print(f"seam pixels before close: {int(leftover.sum())}")
    fill_counts = {k: 0 for k in order}
    max_fill_dist = 0.0
    if leftover.any():
        owner = np.zeros((h, w), np.int32)
        for i, name in enumerate(order):
            owner[masks[name] & (owner == 0)] = i + 1
        dist, (iy, ix) = ndi.distance_transform_edt(~union, return_indices=True)
        near = owner[iy, ix]
        max_fill_dist = float(dist[leftover].max())
        for i, name in enumerate(order):
            add = leftover & (near == i + 1)
            if add.any():
                masks[name] = masks[name] | add
                fill_counts[name] = int(add.sum())
    print(f"seam close: max distance {max_fill_dist:.1f}px, assigned {sum(fill_counts.values())}")

    # ===== pivots =====
    pivots: dict[str, dict] = {}
    parents = {name: parent for name, _pid, _src, parent, _spec in PARTS}
    for i, (name, pid, src, parent, spec) in enumerate(PARTS):
        cx, cy, override, note = solve_pivot(name, spec, masks, src_masks)
        if parent == "auto":
            parent = override or None
        parents[name] = parent
        pivots[name] = {
            "part_id": pid,
            "draw_index": i,
            "parent": parent,
            "pivot_px": [round(cx, 1), round(cy, 1)],
            "pivot_uv": [round(cx / w, 5), round(cy / h, 5)],
            "rule": note,
            "bbox": bbox(masks[name]),
            "centroid_px": [round(v, 1) for v in centroid(masks[name])],
            "opaque_px": int(masks[name].sum()),
        }

    # ===== write plates =====
    z = np.zeros((h, w, 4), np.uint8)
    for name in order:
        m = masks[name]
        plate = z.copy()
        plate[m] = rgba[m]
        Image.fromarray(plate).save(OUT / f"{name}.png")
        print(f"  {name:22s} {int(m.sum()):7d}  (+{fill_counts[name]} seam)")

    comp = z.copy()
    for name in order:
        m = masks[name]
        comp[m] = rgba[m]
    Image.fromarray(comp).save(QA / "composite.png")
    diff = np.abs(comp.astype(np.int16) - rgba.astype(np.int16)).sum(axis=2)
    residual = int((on & (comp[:, :, 3] <= ALPHA_ON)).sum())
    print(f"composite: max diff {int(diff.max())}, mean on opaque {float(diff[on].mean()):.4f}, unowned {residual}")
    Image.fromarray(np.clip(diff, 0, 255).astype(np.uint8)).save(QA / "composite_diff.png")

    print("\npivots (back -> front):")
    for name in order:
        p = pivots[name]
        print(f"  {name:22s} parent={str(p['parent']):16s} {str(p['pivot_px']):16s} {p['rule']}")

    write_contact_sheet(OUT, order, QA / "contact-sheet.jpg")
    write_pivot_overlay(rgba, pivots, QA / "pivots-overlay.png")

    (OUT / "pivots.json").write_text(
        json.dumps(
            {
                "source": str(SRC),
                "canvas": {"width": w, "height": h},
                "coordinate_space": "pixels, top-left origin, +y down; pivot_uv = [x/width, y/height]",
                "side_convention": "_l/_r are viewer-space (image left/right), inherited from the source split",
                "root": "body",
                "parts": pivots,
            },
            indent=2,
            ensure_ascii=False,
        ),
        encoding="utf-8",
    )
    (OUT / "parts.json").write_text(
        json.dumps(
            {
                "character": "C026-白丝",
                "source_presenter": str(SRC),
                "source_layers": str(LAYERS),
                "canvas": {"width": w, "height": h},
                "plate_space": "every PNG is full canvas, so parts align by direct overlay -- no offsets needed",
                "draw_order_back_to_front": order,
                "costume_parts_for_recolor": COSTUME_PARTS,
                "parts": [
                    {
                        "name": name,
                        "part_id": pid,
                        "draw_index": i,
                        "source_plate": f"../layers/{src}.png",
                        "parent": parents[name],
                        "opaque_px": pivots[name]["opaque_px"],
                        "seam_px_added": fill_counts[name],
                        "bbox": pivots[name]["bbox"],
                    }
                    for i, (name, pid, src, _p, _s) in enumerate(PARTS)
                ],
                "qa": {
                    "seam_px_before_close": int(leftover.sum()),
                    "seam_max_fill_distance_px": round(max_fill_dist, 2),
                    "unowned_opaque_px_after_close": residual,
                    "composite_max_channel_sum_diff": int(diff.max()),
                    "composite_mean_diff_on_opaque": round(float(diff[on].mean()), 5),
                },
            },
            indent=2,
            ensure_ascii=False,
        ),
        encoding="utf-8",
    )
    print("wrote", OUT)


if __name__ == "__main__":
    main()
