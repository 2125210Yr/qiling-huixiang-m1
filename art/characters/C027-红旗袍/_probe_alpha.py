"""Compare rembg backbones + a GrabCut baseline on the elevator frame.

Throwaway probe: writes每 candidate alpha into _probe/ so the real splitter can
pick the backbone that actually finds the whole figure instead of one bright foot.
"""
from __future__ import annotations

import json
from pathlib import Path

import cv2
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parent
OUT = ROOT / "_probe"
WORK = ROOT / "reference-work.png"
K = np.ones((3, 3), np.uint8)


def stats(alpha: np.ndarray) -> dict:
    m = alpha > 40
    if not m.any():
        return {"frac": 0.0, "bbox": None, "n_cc": 0, "largest_frac": 0.0}
    n, lab, st, _ = cv2.connectedComponentsWithStats(m.astype(np.uint8), 8)
    ys, xs = np.where(m)
    big = int(st[1:, cv2.CC_STAT_AREA].max()) if n > 1 else 0
    return {
        "frac": round(float(m.sum()) / m.size, 4),
        "bbox": [int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max())],
        "n_cc": int(n - 1),
        "largest_frac": round(big / m.size, 4),
    }


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    work = Image.open(WORK).convert("RGB")
    rgb = np.asarray(work)
    report = {}

    from rembg import new_session, remove

    for name in ("isnet-anime", "u2net_human_seg", "u2net", "isnet-general-use"):
        try:
            cut = remove(work, session=new_session(name))
            alpha = np.asarray(cut.convert("RGBA"))[:, :, 3]
        except Exception as exc:  # noqa: BLE001
            report[name] = {"error": f"{type(exc).__name__}: {exc}"[:160]}
            continue
        Image.fromarray(alpha).save(OUT / f"alpha_{name}.png")
        report[name] = stats(alpha)

    # Also try the un-upscaled source: these nets were trained near 320px and the
    # 4x LANCZOS blur may be what is confusing them.
    small = Image.open(ROOT / "reference-src.jpg").convert("RGB").crop((1, 0, 545, 292))
    for name in ("isnet-anime", "u2net_human_seg"):
        try:
            cut = remove(small, session=new_session(name))
            alpha = np.asarray(cut.convert("RGBA"))[:, :, 3]
        except Exception as exc:  # noqa: BLE001
            report[f"{name}@1x"] = {"error": str(exc)[:160]}
            continue
        up = cv2.resize(alpha, (rgb.shape[1], rgb.shape[0]), interpolation=cv2.INTER_CUBIC)
        Image.fromarray(up).save(OUT / f"alpha_{name}_1x.png")
        report[f"{name}@1x"] = stats(up)

    (OUT / "alpha_report.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
