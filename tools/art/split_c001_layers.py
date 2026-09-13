"""Split C001 into aligned paper-doll layers. Outer hair/sword only; body stays full."""
from pathlib import Path

from PIL import Image

SRC = Path(r"F:\Resonance\client\Assets\Resources\Art\Characters\C001\presenter.png")
BLINK = Path(r"F:\Resonance\client\Assets\Resources\Art\Characters\C001\presenter_blink.png")
OUT = Path(r"F:\Resonance\client\Assets\Resources\Art\Characters\C001\Layers")


def hair_like(r, g, b, a):
    if a < 24:
        return False
    mx = max(r, g, b)
    if r > 80 and r > g * 1.4 and r > b * 1.3 and r - g > 22:
        return True
    if mx <= 52 and r + g + b <= 120:
        return True
    return False


def skin_like(r, g, b, a):
    if a < 24:
        return False
    return r > 95 and g > 58 and b > 42 and r > g and r - b > 20 and r < 220


def metal_blade(r, g, b, a):
    if a < 24:
        return False
    mx = max(r, g, b)
    mn = min(r, g, b)
    if mx < 120 or mx - mn > 42:
        return False
    return abs(r - g) < 22 and abs(g - b) < 26


def gold_hilt(r, g, b, a):
    if a < 24:
        return False
    return r > 130 and 70 < g < 170 and b < 90 and r > b + 50


def mask_layer(im, keep):
    w, h = im.size
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    sp = im.load()
    op = out.load()
    n = 0
    for y in range(h):
        for x in range(w):
            if keep(x, y, sp[x, y]):
                op[x, y] = sp[x, y]
                n += 1
    return out, n


def main():
    im = Image.open(SRC).convert("RGBA")
    w, h = im.size
    px = im.load()

    def keep_front(x, y, p):
        r, g, b, a = p
        if skin_like(r, g, b, a) or not hair_like(r, g, b, a):
            return False
        ny, nx = y / h, x / w
        face = 0.40 < nx < 0.62 and 0.08 < ny < 0.30
        if face:
            return False
        # shoulders / jacket are not hair
        if ny > 0.26 and 0.34 < nx < 0.66:
            return False
        if ny < 0.36 and 0.22 < nx < 0.80:
            return True
        if 0.18 < ny < 0.72 and nx > 0.70:
            return True
        return False

    def keep_back(x, y, p):
        r, g, b, a = p
        if skin_like(r, g, b, a) or not hair_like(r, g, b, a):
            return False
        ny, nx = y / h, x / w
        if ny > 0.82:
            return False
        # mass behind / to the left, not the jacket column
        if 0.12 < ny < 0.74 and nx < 0.34:
            return True
        if 0.10 < ny < 0.36 and nx < 0.48:
            return True
        return False

    def keep_sword(x, y, p):
        r, g, b, a = p
        ny, nx = y / h, x / w
        # blade corridor: hand to lower-left
        in_blade = 0.32 < ny < 0.92 and 0.08 < nx < 0.52
        if not in_blade:
            return False
        # skip belt gold at hip
        if 0.52 < ny < 0.64 and 0.42 < nx < 0.58:
            return False
        if metal_blade(r, g, b, a):
            return True
        if gold_hilt(r, g, b, a) and 0.34 < ny < 0.55 and 0.28 < nx < 0.50:
            return True
        return False

    front, nf = mask_layer(im, keep_front)
    back, nb = mask_layer(im, keep_back)
    sword, ns = mask_layer(im, keep_sword)

    def punch(body, layer):
        bp = body.load()
        lp = layer.load()
        bw, bh = body.size
        for y in range(bh):
            for x in range(bw):
                la = lp[x, y][3]
                if la < 16:
                    continue
                nx = x / bw
                ny = y / bh
                # only punch hair sitting on void (sides / far tail), never the face or jacket
                if nx < 0.36 or nx > 0.68 or ny < 0.14:
                    r, g, b, a = bp[x, y]
                    bp[x, y] = (r, g, b, 0)

    body = im.copy()
    punch(body, back)
    punch(body, front)
    front.save(OUT / "layer_hair_front.png")
    back.save(OUT / "layer_hair_back.png")
    sword.save(OUT / "layer_sword.png")
    body.save(OUT / "layer_body.png")

    blink = Image.open(BLINK).convert("RGBA")
    punch(blink, back)
    punch(blink, front)
    blink.save(OUT / "layer_body_blink.png")
    print("front", nf, "back", nb, "sword", ns, "size", w, h)


if __name__ == "__main__":
    main()
