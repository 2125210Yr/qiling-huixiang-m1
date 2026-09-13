from PIL import Image
import os, json

src = r"C:\Users\Administrator\.grok\sessions\F%3A%5C%E5%A4%A9%E5%91%BD%E4%B9%8B%E5%AD%90%5C%E5%A4%A9%E5%91%BD%E4%B9%8B%E5%AD%90%E6%95%B0%E6%8D%AE\01a04270-8a48-7af2-ac52-cd1b846609a9\images\39.jpg"
out = os.path.dirname(os.path.abspath(__file__))
im = Image.open(src).convert("RGB")
w, h = im.size
print("src", w, h)
boxes = {
    "face": (0.34, 0.06, 0.70, 0.30),
    "bust": (0.28, 0.20, 0.74, 0.44),
    "mid": (0.32, 0.36, 0.68, 0.56),
    "hand": (0.12, 0.48, 0.48, 0.78),
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
