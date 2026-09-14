# G2_RECHECK_20260914 批次锁

batch_id: `G2R14`
coordinator: this session (review-only after dispatch)
baseline: `2a00445c67dbb2ca9251d81428e3096b71c233fb`
HEAD at dispatch: `05a0184` — delta vs baseline is art-only (Feimi/Jiangxiao etc.). Keep every 2a00445 combat fix.

Old G2_REVIEW_20260913 leases are **closed**. No live agents to resume. Do not respawn those task_ids.

## Integrator-only (do not write these files)

- `client/Assets/Scripts/Resonance.Battle/Core/BattleSim.cs`
- `client/Assets/Scripts/Resonance.App/Battle/BattleHud.cs`
- `client/Assets/Scripts/Resonance.App/Core/GameRoot.cs`
- Live Unity scenes/prefabs

If you need a change there, write `DC_RECON_KIT/m1/G2_RECHECK_20260914/patches/<task_id>.md` (or `.diff`) with exact hunks. Coordinator merges.

## Task leases

| task_id | Owner writes only | Must not touch |
|---|---|---|
| G2R14-QA | `tools/BattleSim.Tests/G2Recheck*.cs` | production, old G2Review* except read |
| G2R14-NATURAL | `NaturalPlayRuntime.cs`, `NaturalPlaySmoke.cs`, new verification config under `Resonance.Battle/Content/` or `Resonance.App/Debug/`, `patches/G2R14-NATURAL.md` | BattleSim.cs, G2Recheck*.cs |
| G2R14-EFFECT | `patches/G2R14-EFFECT.md` only (+ optional fixture JSON under `G2_RECHECK_20260914/fixtures/`) | BattleSim.cs, tests |
| G2R14-IMPORT | `EffectCapability.cs`, `Catalog.cs`, `CatalogJson.cs`, `BuffCatalog.cs` validation only | BattleSim.cs, G2Recheck*.cs |
| G2R14-REPLAY | `BattleReplay.cs`, `patches/G2R14-REPLAY.md` (AutoFire→Submit) | BattleSim.cs unless patch file |
| G2R14-QTE | `patches/G2R14-QTE.md` only | BattleSim.cs, tests |
| G2R14-MEASURED | `FormulaProfile.cs`, `DamageMath.cs`; may update `G2ReviewProvenanceTests.cs` only to drop Measured==Computed | BattleSim.cs, G2Recheck*.cs |
| G2R14-DOCS | `G2_RECHECK_20260914/artifacts/**` skeleton, `HEAD_DELTA.md` | source, tests |

## Rules

- No git commit/push. No G3. No T27/90%/M1 claims.
- Do not reset/clean user uncommitted files (`client/f_*.jpg`, `dist/`, art hires, `codex专区`).
- Do not inject HP/Drive/Charge/Fever/time/outcome after battle start. No forced Perfect in natural play.
- DESIGN_PLACEHOLDER verification config is allowed **before** battle start only; never label it as original-game numbers.
- Keep run7 FAIL as historical baseline; do not rewrite it to PASS.
- Report: files written, compile/test if you ran them, residual risks. NOT_RUN / BLOCKED if not executed.
