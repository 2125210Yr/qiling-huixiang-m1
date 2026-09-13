# live2d-preview

Local layered-PNG puppet viewer (Live2D / Cubism-like, **no** Cubism runtime, no build step).

Lives under `tools/` on purpose — it does not touch the game client. Existing still-warp demos (`art/characters/_new-candidates/live2d_preview/`, `art/live2d-lab/elevator-red/preview/`) stay as they are; this one loads real PNG stacks + `pivots.json`.

## Open it

From the repo root (recommended — C001 slots load art from relative paths):

```powershell
cd F:\天命之子
python -m http.server 8777 --bind 127.0.0.1
```

Then open:

```text
http://127.0.0.1:8777/tools/live2d-preview/index.html
```

Deep links:

```text
http://127.0.0.1:8777/tools/live2d-preview/index.html?model=c001-puppet
http://127.0.0.1:8777/tools/live2d-preview/index.html?model=c001-preview
http://127.0.0.1:8777/tools/live2d-preview/index.html?model=elevator-red
```

`file://` also works for the elevator-red slot (its layers are self-contained under `sample-layers/`). C001 slots need the HTTP server because they point at `art/characters/...`.

```text
file:///F:/天命之子/tools/live2d-preview/index.html?model=elevator-red
```

## Slots

| id | Source | Notes |
|---|---|---|
| `c001-puppet` | `art/characters/C001-焰刃/puppet-src/layers/` | Cut-out head / hand / feet. Procedural lids. |
| `c001-preview` | `art/characters/C001-焰刃/preview-layers/` | Real blink plate. |
| `elevator-red` | `sample-layers/elevator-red/` | Placeholder silhouettes; raised-leg rest pose. |

Regenerate elevator placeholders:

```powershell
python tools\live2d-preview\_tools\make_placeholder_layers.py
```

## Drop any folder

Use the folder picker: pick a directory of `*.png` + optional `pivots.json`. Roles are guessed from filenames (`hair_back`, `face`, `leg_raise_thigh`, …).
