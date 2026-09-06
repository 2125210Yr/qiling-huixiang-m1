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


def apply_plates(still, moves, keep, chest):
    h, w = still.shape[:2]
    k = np.ones((3, 3), np.uint8)
    a0 = still[:, :, 3] > 8
    rgb = still[:, :, :3]
    protect = shirt_keep(rgb, a0)
    if keep is not None:
        protect = protect | keep
    if chest is not None:
        protect = protect | chest
    plates = {}
    punch = np.zeros((h, w), np.uint8)
    for name, m in moves.items():
        if int(m.sum()) == 0:
            raise ValueError(f"empty mask: {name}")
        dil = cv2.dilate(m.astype(np.uint8) * 255, k, iterations=2) > 128
        layer = still.copy()
        layer[~dil, 3] = 0
        plates[name] = layer
        punch = np.maximum(punch, (m.astype(np.uint8) * 255))
    punch[protect] = 0
    punch = cv2.erode(punch, k, iterations=1)
    body = still.copy()
    body[punch > 16, 3] = 0
    plates["body"] = body
    return plates
