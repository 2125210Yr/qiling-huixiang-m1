# tools/puppet/test_from_masks.py
import numpy as np
import pytest
from from_masks import mask_bool, shirt_keep


def test_mask_bool_alpha_or_luma():
    a = np.zeros((4, 4, 4), np.uint8)
    a[1, 1] = (255, 255, 255, 255)
    a[2, 2] = (200, 200, 200, 0)
    m = mask_bool(a)
    assert m[1, 1] and not m[2, 2]


def test_shirt_keep_protects_gray_white_not_mint():
    rgb = np.zeros((8, 8, 3), np.uint8)
    rgb[2:6, 1:4] = (200, 195, 190)
    rgb[2:6, 5:7] = (100, 180, 160)
    k = shirt_keep(rgb, np.ones((8, 8), bool))
    assert k[3, 2] and not k[3, 6]
