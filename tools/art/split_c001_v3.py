"""Split C001 presenter-v3.png into Unity 2D layers. Body punches moving hair/sword."""
from pathlib import Path

from PIL import Image

SRC = Path(r"F:\天命之子\art\characters\C001-焰刃\presenter-v3.png")
BLINK = Path(r"F:\天命之子\art\characters\C001-焰刃\presenter_blink.png")
OUT = Path(r"F:\天命之子\client\Assets\Resources\Art\Characters\C001\V3Layers")
ART = Path(r"F:\天命之子\art\characters\C001-焰刃\v3-layers")


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


def punch(body, layer, extra=None):
    bp = body.load()
    lp = layer.load()
    bw, bh = body.size
    for y in range(bh):
        for x in range(bw):
            if lp[x, y][3] < 16:
                continue
            nx = x / bw
            ny = y / bh
            if extra is not None and not extra(nx, ny):
                continue
            r, g, b, a = bp[x, y]
            bp[x, y] = (r, g, b, 0)


def main():
    if not SRC.is_file():
        raise SystemExit("missing " + str(SRC))
    OUT.mkdir(parents=True, exist_ok=True)
    ART.mkdir(parents=True, exist_ok=True)
    im = Image.open(SRC).convert("RGBA")
    w, h = im.size

    def keep_front(x, y, p):
        r, g, b, a = p
        if skin_like(r, g, b, a) or not hair_like(r, g, b, a):
            return False
        ny, nx = y / h, x / w
        if 0.40 < nx < 0.62 and 0.08 < ny < 0.30:
            return False
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
        if 0.12 < ny < 0.74 and nx < 0.34:
            return True
        if 0.10 < ny < 0.36 and nx < 0.48:
            return True
        return False

    def keep_sword(x, y, p):
        r, g, b, a = p
        ny, nx = y / h, x / w
        if not (0.32 < ny < 0.92 and 0.08 < nx < 0.52):
            return False
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
    body = im.copy()
    punch(body, back, lambda nx, ny: nx < 0.38 or ny < 0.16)
    punch(body, front, lambda nx, ny: nx > 0.66 or ny < 0.14)
    punch(body, sword, lambda nx, ny: nx < 0.42 and ny > 0.40)

    for name, imout in (
        ("layer_hair_front.png", front),
        ("layer_hair_back.png", back),
        ("layer_sword.png", sword),
        ("layer_body.png", body),
    ):
        imout.save(OUT / name)
        imout.save(ART / name)

    if BLINK.is_file():
        blink = Image.open(BLINK).convert("RGBA")
        punch(blink, back, lambda nx, ny: nx < 0.38 or ny < 0.16)
        punch(blink, front, lambda nx, ny: nx > 0.66 or ny < 0.14)
        blink.save(OUT / "layer_body_blink.png")
        blink.save(ART / "layer_body_blink.png")
    print("v3 layers front", nf, "back", nb, "sword", ns, "size", w, h, "->", OUT)


if __name__ == "__main__":
    main()
