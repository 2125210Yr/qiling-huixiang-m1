from PIL import Image
import os, json

src = os.path.join(os.path.dirname(os.path.abspath(__file__)), "src_39.jpg")
out = os.path.dirname(os.path.abspath(__file__))
im = Image.open(src).convert("RGB")
w, h = im.size
print("src", w, h)
boxes = {
    "face": (0.34, 0.06, 0.70, 0.30),
    "bust": (0.28, 0.20, 0.74, 0.44),
    "mid": (0.32, 0.38, 0.70, 0.62),
    "hips": (0.34, 0.50, 0.70, 0.76),
    "legs": (0.34, 0.64, 0.66, 0.88),
    "boots": (0.36, 0.76, 0.64, 1.00),
    "hand": (0.14, 0.46, 0.48, 0.72),
    "sword": (0.08, 0.58, 0.44, 0.98),
    "sleeve": (0.16, 0.26, 0.42, 0.56),
    "hair_top": (0.22, 0.00, 0.70, 0.24),
    "hair_left_u": (0.00, 0.06, 0.40, 0.40),
    "hair_left_l": (0.00, 0.32, 0.42, 0.80),
    "hair_right_u": (0.58, 0.10, 1.00, 0.48),
    "hair_right_l": (0.52, 0.38, 1.00, 0.80),
}
meta = {"src_size": [w, h]}
for name, (l, t, r, b) in boxes.items():
    box = (int(w * l), int(h * t), int(w * r), int(h * b))
    crop = im.crop(box)
    path = os.path.join(out, f"{name}.png")
    crop.save(path)
    meta[name] = {"box": list(box), "size": list(crop.size)}
    print(name, box, crop.size)
with open(os.path.join(out, "meta.json"), "w", encoding="utf-8") as f:
    json.dump(meta, f, indent=2)
print("ok")
