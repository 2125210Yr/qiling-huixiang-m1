from pathlib import Path

from pytoshop import PsdFile

sample = next(Path(r"F:\Resonance\cubism\C001\sample_psd").rglob("*.psd"))
with sample.open("rb") as f:
    psd = PsdFile.read(f)
    recs = psd.layer_and_mask_info.layer_info.layer_records
    n = 0
    for rec in recs:
        if rec.right <= rec.left or rec.bottom <= rec.top:
            continue
        keys = [getattr(b, "code", b"?") for b in rec.blocks]
        ch = []
        for cid, c in rec.channels.items():
            img = getattr(c, "image", None)
            shape = None if img is None else getattr(img, "shape", None)
            ch.append((cid, c.compression, shape))
        name = rec.name.encode("unicode_escape").decode("ascii")
        print(name, "bbox", rec.left, rec.top, rec.right, rec.bottom, "ch", ch, "blocks", keys)
        n += 1
        if n >= 6:
            break
print("doc channels", psd.num_channels, "comp", psd.image_data.compression)
print("image_data channels shape", None if psd.image_data._channels is None else getattr(psd.image_data, "channels", None))
