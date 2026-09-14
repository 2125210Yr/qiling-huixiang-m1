# HEAD vs 2a00445

- Review baseline: `2a00445` (G2 Path A command pipeline + evidence).
- Dispatch HEAD: `05a0184` (`art: add Feimi demon magician character`).
- `git diff --stat 2a00445..HEAD`: character concept PNGs/prompts only. No BattleSim / HUD / test change.
- Keep all 2a00445 engineering fixes. Do not redo G0/G1.
- Working tree has local leftovers (Unity csproj, stray `client/f_*.jpg`, `dist/`). Do not delete them.
