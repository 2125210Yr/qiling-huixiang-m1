# -*- coding: utf-8 -*-
import json
import os
import re
import urllib.request

UA = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/124.0.0.0 Safari/537.36"
OUT = r"F:\天命之子\docs\reference\gamekee\_combined\battle_refs"
os.makedirs(OUT, exist_ok=True)


def get(url):
    req = urllib.request.Request(url, headers={"User-Agent": UA, "Accept": "*/*"})
    with urllib.request.urlopen(req, timeout=30) as r:
        return r.geturl(), r.read()


def save_img(url, name):
    if url.startswith("//"):
        url = "https:" + url
    try:
        _, raw = get(url)
        path = os.path.join(OUT, name)
        with open(path, "wb") as f:
            f.write(raw)
        print("saved", name, len(raw))
        return path
    except Exception as e:
        print("img fail", url, e)
        return ""


url = "https://gamenext.net/destiny-child/battle/"
final, raw = get(url)
text = raw.decode("utf-8", "replace")
print("gamenext", final, "len", len(text))
imgs = re.findall(r'(?:src|data-src|data-lazy-src)=["\']([^"\']+)["\']', text, re.I)
print("img count", len(imgs))
kept = []
n = 0
for i, src in enumerate(imgs):
    low = src.lower()
    if not any(x in low for x in (".jpg", ".png", ".webp", ".jpeg", "wp-content", "destiny")):
        continue
    if "logo" in low or "icon" in low and "battle" not in low:
        continue
    ext = ".jpg"
    for e in (".png", ".webp", ".jpeg", ".jpg"):
        if e in low:
            ext = e
            break
    n += 1
    path = save_img(src, "gamenext_%02d%s" % (n, ext))
    if path:
        kept.append((src, path))

# youtube thumbnail for known gameplay videos
yt = [
    ("jle-hMNlchg", "yt_official_gameplay.jpg"),
    ("hon6kCdawlo", "yt_first_impressions.jpg"),
]
for vid, name in yt:
    save_img("https://i.ytimg.com/vi/%s/maxresdefault.jpg" % vid, name)
    save_img("https://i.ytimg.com/vi/%s/hqdefault.jpg" % vid, name.replace(".jpg", "_hq.jpg"))

print("kept", len(kept))
with open(os.path.join(OUT, "manifest.txt"), "w", encoding="utf-8") as f:
    for src, path in kept:
        f.write("%s\t%s\n" % (src, path))
