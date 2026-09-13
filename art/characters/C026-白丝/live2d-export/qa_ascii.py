"""Coarse ASCII occupancy map of a region, one char per NxN block.

Reading limb topology off a colour overlay is error prone; this prints the
silhouette so joint coordinates can be picked without guessing.
"""
from __future__ import annotations

import sys
from pathlib import Path

import numpy as np
from PIL import Image

OUT = Path(__file__).resolve().parent
ROOT = OUT.parent


def main() -> None:
    x0, x1, y0, y1, step = 150, 370, 140, 380, 5
    args = sys.argv[1:]
    src = OUT
    if args and args[0] == "--layers":
        src, args = ROOT / "layers", args[1:]
    names = args or ["arm_upper_r", "arm_lower_r", "hand_r"]
    glyphs = "#*+o=%"
    pres = np.array(Image.open(ROOT / "presenter.png").convert("RGBA"))
    grid = np.zeros(pres.shape[:2], np.int8)
    for i, n in enumerate(names):
        m = np.array(Image.open(src / f"{n}.png"))[:, :, 3] > 8
        grid[m & (grid == 0)] = i + 1
    # everything else that is opaque, so limb gaps vs neighbours are visible
    grid[(pres[:, :, 3] > 8) & (grid == 0)] = len(names) + 1
    glyphs = glyphs[: len(names)] + "-"
    print("legend: " + ", ".join(f"{glyphs[i]}={n}" for i, n in enumerate(names)))
    print("     " + "".join(f"{x // 100 % 10}" for x in range(x0, x1, step)))
    print("     " + "".join(f"{x // 10 % 10}" for x in range(x0, x1, step)))
    for y in range(y0, y1, step):
        row = ""
        for x in range(x0, x1, step):
            block = grid[y : y + step, x : x + step]
            vals, counts = np.unique(block[block > 0], return_counts=True)
            row += "." if len(vals) == 0 else glyphs[int(vals[np.argmax(counts)]) - 1]
        print(f"{y:4d} {row}")


if __name__ == "__main__":
    main()
