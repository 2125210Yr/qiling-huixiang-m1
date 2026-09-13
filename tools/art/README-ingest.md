# Still illustration ingest

Drop generated presenters/portraits (PNG or JPEG) into `art/` and Unity Resources. Contain/letterbox only (no stretch, no chroma-key).

Requires: `pip install Pillow`

C001 is constitution — do not ingest it.

From repo root:

```
python tools/art/ingest_still.py --id C007 --name 白昼守望 --presenter PATH --portrait PATH
python tools/art/ingest_still.py --id C010 --name 影缚者 --presenter PATH --portrait PATH
python tools/art/ingest_still.py --id C003 --name 潮汐祭司 --presenter PATH --portrait PATH
python tools/art/ingest_still.py --id C005 --name 森语引路者 --presenter PATH --portrait PATH
```

`--presenter` and `--portrait` are independently optional; at least one is required. Missing source paths fail before Unity folders are created.

Sizes:

- `presenter.png` 1024x1536 RGBA (2:3). Mipmaps off.
- `portrait.png` 1024x1024 RGBA (1:1). Mipmaps ok.

`presenter_blink.png` is optional on disk; skip if missing. This script does not ingest blink.

Writes:

- `art/characters/{id}-{name}/presenter.png` and/or `portrait.png`
- `client/Assets/Resources/Art/Characters/{id}/` same files plus Unity `.meta`

Opaque sources pad with RGB(0,0,0) A=255. Sources that already have transparency pad with (0,0,0,0).
