# REVIEW_1b2ca8e 批次锁

batch_id: `B1`
coordinator: this session (review-only after dispatch)
baseline: `1b2ca8e4ff56d9f0986e158fc97b50498f735cb3`
HEAD at dispatch: `1b2ca8e` — combat tree matches baseline.

A53 / REVIEW_1a75253 leases are **closed**. Do not respawn A53-* as new task_ids.
Resume only the four IDs listed below. Do not start a second coordinator.

## Integrator-only (do not write these files)

- `client/Assets/Scripts/Resonance.Battle/Core/BattleSim.cs`
- `client/Assets/Scripts/Resonance.App/Battle/BattleHud.cs`
- `client/Assets/Scripts/Resonance.App/Core/GameRoot.cs`
- Live Unity scenes/prefabs

BattleSim / GameRoot changes go to `DC_RECON_KIT/m1/REVIEW_1b2ca8e/patches/<task_id>.md`.

## Task leases

A1–A4 are **closed as executed**. Do not respawn them. New residuals:

| task_id | Owner writes only | Must not touch |
|---|---|---|
| B1-F1 | **closed as executed** — growth recovery landed; fight-1 still Match=False mid-fight. Coordinator hoisted `FiveStat`/`GearCombo` for Unity CS0246 | do not respawn for the same growth MaxHp hole |
| B1-F2 | **closed as executed** — `20260914T183658` first line PASS (Fever+two Player FeverTap). Do not respawn for the 160s spin | do not rewrite TapQte/VfxGoodWindow; do not inject Fever |

A1–A4 historical rows stay for audit; do not resume those IDs for F1/F2.

## Rules

- No git commit/push unless the user asks later.
- No G3. No T27/90%/M1 claims. No G0/G1 redo.
- Do not reset user leftovers (`dist/`, `client/f_*.jpg`, Unity csproj, `_Recovery`, `codex专区`, hires tiles).
- After battle start: no HP/Drive/Charge/Fever/time/outcome injection. No forced Perfect.
- DESIGN_PLACEHOLDER pre-battle config allowed; never label as GL.
- Keep run7 FAIL historical. Keep committed `a53-gate.trx` bytes unchanged (HISTORICAL 234/10).
- Unrun ≠ pass. Environment failure = BLOCKED + real command/error.
- Shared Catalog tests stay serial. Max parallel agents ≠ parallel xunit.
