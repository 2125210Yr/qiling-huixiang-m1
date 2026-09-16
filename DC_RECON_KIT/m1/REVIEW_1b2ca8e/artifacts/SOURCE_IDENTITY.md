# B1-A1 source identity (pre-run)

Recorded **before** `dotnet test`. batch_id=`B1`. task=`B1-A1`.  
Not M1 / T27 / 90% / G3. Leftover `dist/`, `client/f_*.jpg`, Unity csproj were not deleted.

## Git

| Item | Value |
|---|---|
| Recorded at | 2026-09-14 (local, before test process) |
| `git rev-parse HEAD` | `1b2ca8e4ff56d9f0986e158fc97b50498f735cb3` |
| Short | `1b2ca8e` |
| Branch | `m1-gt6-review` (tracks `origin/m1-gt6-review`) |
| Review baseline | `1b2ca8e4ff56d9f0986e158fc97b50498f735cb3` |
| HEAD == baseline | **yes** |

`git status -sb` (combat / test tree only, vs HEAD):

```
(clean — no M/?? under client/Assets/Scripts/Resonance.Battle or tools/BattleSim.Tests)
```

`git diff --stat 1b2ca8e4ff56d9f0986e158fc97b50498f735cb3 -- client/Assets/Scripts/Resonance.Battle tools/BattleSim.Tests`:

```
(empty — working tree combat/test files match 1b2ca8e)
```

Workspace leftovers (not combat, not deleted): hires tiles modified; untracked `DC_RECON_KIT` zips/reviews, `client/f_*.jpg`, Unity `*.csproj`/`client.sln`, `_Recovery`, `codex专区`, `dist/`.

## File hashes (working tree)

SHA-256 is `Get-FileHash -Algorithm SHA256`. Git blob is `git hash-object`.

| Path | SHA-256 | git hash-object |
|---|---|---|
| `client/Assets/Scripts/Resonance.Battle/Core/BattleSim.cs` | `3fc96e8879e89309b0b29e2b30909999cf040471d1dd8ec5e860395483f7231b` | `72a8a3408b11afee33134e400c4fe645754abc83` |
| `client/Assets/Scripts/Resonance.Battle/Core/BattleSim.Commands.cs` | `c65a616300322a0179ec797a6ac6d6edd28fd3c036679b416cc6e0dc62cb94ca` | `85aad112efafe42812a9203cb036723fa54eb923` |
| `client/Assets/Scripts/Resonance.Battle/Content/Catalog.cs` | `d3c1c11fba42387d89e782de50e632f5b090878d5ddf8acf663b9035891fbfd5` | `55065d3d52947bd3ec6867f9d8cd75d1923b4569` |
| `client/Assets/Scripts/Resonance.Battle/Content/VerificationCatalog.cs` | `5b910f4efea247b40ad0d90c8f21dbddcf561563c741e3a9af8714548c07b8fa` | `97a6fe5e52cb9a7f7c07162f15463d1f81e653e4` |
| `client/Assets/Scripts/Resonance.Battle/Core/BattleReplay.cs` | `3f06e4076052d24503ce14f6828845782926a017c1f6b739451a43459e89fe04` | `6677c70678964575506d9c9c3f8ea5595d3c3e91` |
| `tools/BattleSim.Tests/G2ReviewFixtures.cs` | `4c3c3e608d97d273c3ff40bb8e2bf595de5342b2a368d76bcbd3112893ad0ff1` | `72ae7fdb847a3ce9a4feb8ea28c8402aeb36ee44` |
| `tools/BattleSim.Tests/M1CoreSliceTests.cs` | `b5c6df0ebc18141dbc258606cfc7d44edbc262cfcef764e815b69d995be05cbb` | `786ae5fa7f21f63d5f72b273236b19097cedb98a` |

## Historical gate TRX (do not treat as this run)

| Item | Value |
|---|---|
| Path (committed, bytes unchanged) | `DC_RECON_KIT/m1/REVIEW_1a75253/artifacts/tests/a53-gate.trx` |
| git blob | `987ffbe9ebfd493370fe20ff4258847819270cd2` |
| RunId | `561a2e62-012e-4021-ba20-6b0b3620f263` |
| Times | 2026-09-14 17:13:14.5608319+08:00 → 17:13:18.6794861+08:00 |
| ResultSummary | **Failed** |
| Counters | total=245 executed=244 **passed=234 failed=10** |
| A53 summary claim (HISTORICAL mismatch) | 244/0/1 — **not** this file |
| Copy aside | `artifacts/historical/a53-gate.trx` |

## Post-run delta

Recorded after `dotnet test` finished (log `finished=2026-09-14T22:00:44.1600608+08:00`).  
TRX kept regardless of later dirt.

| Item | Value |
|---|---|
| HEAD after run | still `1b2ca8e4ff56d9f0986e158fc97b50498f735cb3` |
| Other listed files vs pre-run | **unchanged** (same SHA-256 / hash-object) |
| `BattleReplay.cs` | **DIRTY vs 1b2ca8e** (other agent, B1-A3). Pre-run git `6677c70678964575506d9c9c3f8ea5595d3c3e91` / SHA-256 `3f06e4076052d24503ce14f6828845782926a017c1f6b739451a43459e89fe04`. Post-run snapshot git `9e5392f1a350f48edd46a07ce84203a691b205de` / SHA-256 `116386bbe1c58639061df6824e52cb32a35236d798b5e12b35359247f967ee3d`. Working tree then showed `ResolveSelectedStage` / `TryReadSimStage` (instance StageDef). |
| This run's compile | `dotnet test` built `BattleSim.Tests.dll` at start of the same process (`started=2026-09-14T22:00:30`). The saved TRX is that process's result. |

`git status --short -- client/Assets/Scripts/Resonance.Battle tools/BattleSim.Tests` after run:

```
 M client/Assets/Scripts/Resonance.Battle/Core/BattleReplay.cs
```
