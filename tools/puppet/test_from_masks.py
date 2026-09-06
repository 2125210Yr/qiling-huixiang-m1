# tools/puppet/test_from_masks.py
import numpy as np
import pytest
from from_masks import apply_plates, check_keep_overlap, mask_bool, shirt_keep


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


def test_keep_and_chest_never_punched_from_body():
    h, w = 16, 16
    still = np.zeros((h, w, 4), np.uint8)
    still[:, :, :3] = (40, 40, 80)  # not shirt cloth
    still[:, :, 3] = 255
    still[2:8, 2:8, :3] = (100, 180, 150)  # mint hair
    hair = np.zeros((h, w), bool)
    hair[2:10, 2:10] = True
    keep = np.zeros((h, w), bool)
    keep[0:16, 0:5] = True
    chest = np.zeros((h, w), bool)
    chest[4:7, 4:7] = True
    out = apply_plates(still, {"hair_back": hair}, keep=keep, chest=chest)
    body = out["body"]
    assert (body[5, 3, 3] > 8)  # keep
    assert (body[5, 5, 3] > 8)  # chest
    assert out["hair_back"][3, 3, 3] > 8


def test_hair_outside_keep_chest_is_punched_from_body():
    h, w = 16, 16
    still = np.zeros((h, w, 4), np.uint8)
    still[:, :, :3] = (40, 40, 80)  # not shirt cloth
    still[:, :, 3] = 255
    still[2:8, 2:8, :3] = (100, 180, 150)  # mint hair
    hair = np.zeros((h, w), bool)
    hair[2:12, 2:12] = True  # large enough that (9,9) survives punch erode
    keep = np.zeros((h, w), bool)
    keep[0:16, 0:5] = True
    chest = np.zeros((h, w), bool)
    chest[4:7, 4:7] = True
    out = apply_plates(still, {"hair_back": hair}, keep=keep, chest=chest)
    body = out["body"]
    assert body[9, 9, 3] == 0


def test_empty_move_mask_raises():
    still = np.zeros((8, 8, 4), np.uint8)
    still[:, :, 3] = 255
    empty = np.zeros((8, 8), bool)
    with pytest.raises(ValueError, match="empty"):
        apply_plates(still, {"hand_r": empty}, keep=None, chest=None)


def test_keep_blocks_hand_raises():
    hand = np.zeros((8, 8), bool)
    hand[2:6, 2:6] = True
    keep = np.ones((8, 8), bool)
    with pytest.raises(ValueError, match="keep"):
        check_keep_overlap({"hand_r": hand}, keep)


def test_keep_partial_ok():
    hand = np.zeros((8, 8), bool)
    hand[2:6, 2:6] = True
    keep = np.zeros((8, 8), bool)
    keep[2:6, 2:3] = True
    check_keep_overlap({"hand_r": hand}, keep)


def test_apply_plates_keep_blocks_hand_raises():
    still = np.zeros((8, 8, 4), np.uint8)
    still[:, :, 3] = 255
    hand = np.zeros((8, 8), bool)
    hand[2:6, 2:6] = True
    keep = np.ones((8, 8), bool)
    with pytest.raises(ValueError, match="keep"):
        apply_plates(still, {"hand_r": hand}, keep=keep, chest=None)
