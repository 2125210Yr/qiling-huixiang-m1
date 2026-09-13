from pathlib import Path

from pytoshop import PsdFile


def inspect(path: Path, label: str) -> None:
    with path.open("rb") as f:
        psd = PsdFile.read(f)
    print("====", label, path.name, "====")
    print(
        "size",
        psd.width,
        psd.height,
        "depth",
        psd.depth,
        "mode",
        psd.color_mode,
        "ver",
        psd.version,
        "ch",
        psd.num_channels,
    )
    blocks = getattr(psd.image_resources, "blocks", None)
    if blocks:
        print("resources", len(blocks), [type(b).__name__ for b in blocks[:12]])
    recs = psd.layer_and_mask_info.layer_info.layer_records
    print("nlayers", len(recs))
    for rec in recs[:12]:
        bids = []
        for b in rec.blocks:
            key = getattr(b, "key", None) or getattr(b, "code", None) or type(b).__name__
            bids.append(str(key)[:48])
        chinfo = []
        for cid, ch in rec.channels.items():
            chinfo.append("%s:comp=%s" % (cid, getattr(ch, "compression", None)))
        print(
            "  %r vis=%s op=%s bbox=(%s,%s,%s,%s) ch=%s blocks=%s"
            % (
                rec.name,
                rec.visible,
                rec.opacity,
                rec.left,
                rec.top,
                rec.right,
                rec.bottom,
                chinfo,
                bids,
            )
        )
    if len(recs) > 12:
        print("  ...", len(recs) - 12, "more")
    print()


def main() -> None:
    sample_dir = Path(r"F:\Resonance\cubism\C001\sample_psd")
    official = next(sample_dir.rglob("*.psd"))
    inspect(official, "official")
    inspect(Path(r"F:\Resonance\cubism\C001\yanren.psd"), "ours")


if __name__ == "__main__":
    main()
