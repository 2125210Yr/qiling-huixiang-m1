# REVIEW_1a75253 批次锁

batch_id: `A53`
coordinator: this session (review-only after dispatch)
baseline: `1a752532e39560b861c2a661db66c97ca18ffece`
HEAD at dispatch: `1a75253` — combat tree matches baseline. Keep every 1a75253 / 2a00445 combat fix.

Old G2R14 / G2_REVIEW_20260913 leases are **closed**. Do not respawn those task_ids.

## Integrator-only (do not write these files)

- `client/Assets/Scripts/Resonance.Battle/Core/BattleSim.cs`
- `client/Assets/Scripts/Resonance.App/Battle/BattleHud.cs`
- `client/Assets/Scripts/Resonance.App/Core/GameRoot.cs`
- Live Unity scenes/prefabs

BattleSim / GameRoot changes go to `DC_RECON_KIT/m1/REVIEW_1a75253/patches/<task_id>.md`.

## Task leases

| task_id | Owner writes only | Must not touch |
|---|---|---|
| A53-GATE | `EffectCapability.cs`, `Catalog.cs` (playable closure only), `patches/A53-GATE.md` | BattleSim.cs, G2Recheck*, G2A53* |
| A53-IDENTITY | `BattleReplay.cs` (`BattleContentIdentity` / Compute / Verify isolation), `patches/A53-IDENTITY.md` if overlay/policy needs BattleSim | BattleSim.cs unless patch; CatalogJson |
| A53-JSON | `CatalogJson.cs` ReadEffect/OverlayEffect only | BattleSim, tests |
| A53-QA | `tools/BattleSim.Tests/G2A53*.cs`, rewrite `G2RecheckEvidenceContractTests.cs`, `BattleSim.Tests.csproj`, `xunit.runner.json` | production except compile-include of `NaturalPlayBattleEvidence.cs` |
| A53-EVIDENCE | `NaturalPlayBattleEvidence.cs`, `patches/A53-EVIDENCE.md` (Runtime/GameRoot export) | BattleSim.cs, G2A53* |
| A53-UNITY | `NaturalPlaySmoke.cs`, `REVIEW_1a75253/unity-run.md` (how to invoke). May probe Unity. Do not claim PASS if not run. | BattleSim, Catalog, tests |
| A53-DOCS | `REVIEW_1a75253/artifacts/**` skeleton | source, tests |

## Rules

- No git commit/push unless the user asks later.
- No G3. No T27/90%/M1 claims. No G0/G1 redo.
- Do not reset user leftovers.
- After battle start: no HP/Drive/Charge/Fever/time/outcome injection. No forced Perfect.
- DESIGN_PLACEHOLDER pre-battle config allowed; never label as GL.
- Keep run7 FAIL historical.
- Unrun = NOT_RUN. Historical 203/203 is not this batch.
