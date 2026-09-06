# tools/puppet/from_masks.py
from __future__ import annotations

import json
from pathlib import Path

import cv2
import numpy as np
from PIL import Image

MOVE_SLOTS = (
    "hair_back", "hair_front", "hair_side",
    "head", "hand_r", "hand_l", "foot_r", "foot_l", "sword",
)
CANVAS = (1024, 1536)


def mask_bool(arr: np.ndarray) -> np.ndarray:
    if arr.ndim == 2:
        return arr > 8
    if arr.shape[2] == 4:
        return arr[:, :, 3] > 8
    luma = arr[:, :, 0].astype(np.int16) + arr[:, :, 1] + arr[:, :, 2]
    return luma > 24


def shirt_keep(rgb: np.ndarray, a0: np.ndarray) -> np.ndarray:
    r = rgb[:, :, 0].astype(np.int16)
    g = rgb[:, :, 1].astype(np.int16)
    b = rgb[:, :, 2].astype(np.int16)
    mint = a0 & (g > r + 8) & (g > 120) & (r < 190)
    skin = a0 & (r > 145) & (g > 85) & (b > 65) & (r > g) & ((r - b) > 30)
    cloth = a0 & ~mint & (r > 80) & (g > 75) & (b > 65) & (np.abs(r - g) < 42) & (np.abs(g - b) < 48) & ~skin
    h, w = a0.shape
    zone = np.zeros((h, w), bool)
    if h >= 860 and w >= 670:
        zone[310:860, 330:530] = True
        zone[340:700, 500:670] = True
    else:
        zone[:, :] = True
    return cloth & zone
