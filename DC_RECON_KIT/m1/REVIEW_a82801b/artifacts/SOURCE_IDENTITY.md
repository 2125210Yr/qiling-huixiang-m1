# C 批源码身份（冻结提交 `3d2d1e6`）

记录时刻：2026-09-16。batch_id=`C`。被测树冻结后未再改 `client/Assets/Scripts` / `tools/BattleSim.Tests`。  
不是 M1 / T27 / 90% / G3。用户遗留 `dist/`、`client/f_*.jpg`、csproj、hires tiles 未删除。

## Git

| 项 | 值 |
|---|---|
| 审查基线 | `a82801b80fd81fb4f8ff30cf45b72b30fcd63521` |
| 对照基线 | `1b2ca8e4ff56d9f0986e158fc97b50498f735cb3` |
| 冻结 HEAD | `3d2d1e67554251b77d05ad732dc06390a114de05`（`Land C1-C3 Path A fixes`） |
| 分支 | `m1-gt6-review` |
| C4 跑前/跑后战斗树 | `git status --short -- client/Assets/Scripts tools/BattleSim.Tests` 为空 |

## 冻结文件身份

SHA-256 = `Get-FileHash -Algorithm SHA256`。git blob = `git hash-object`。对照 REVIEW.md 第五节 a82801b 原 blob。

| 路径 | git blob @ 3d2d1e6 | SHA-256 | a82801b 原 blob |
|---|---|---|---|
| `BattleSim.cs` | `f2485b9a4747543322fdabb9b7450a623edc35d7` | `FF8F93CD…3DCD48` | `89dbea518fba4009147a92be35848fc0bf2e5e17` |
| `BattleSim.Commands.cs` | `d5827a5c6928789606d3c82966b7eb1c66cdd279` | `C1154C53…92721B` | （C2 加 `FocusEnemy` 工厂） |
| `VerificationCatalog.cs` | `af116cb413c728aa76816668e4a0435ebbc3b84a` | `F5B9ED8B…DD336A` | `bc19a8c984c086a6292e0c4d582629fe703fc0d2` |
| `BattleReplay.cs` | `cbc8f525ec78413acb9e1bea59f8e617d3e31e06` | `720F138E…CC68CA` | `f739d7097400a008e420f460c0f0a567456e93ac` |
| `BattleHud.cs` | `622cddd912336c894a709115f9c835e316cb2aa6` | `30D3C7AE…C83E32` | （不再写 `HoldSim`） |
| `GameRoot.cs` | `4220eaa0a2934e172d1d063de3455a1d9853a0c2` | `D81763D3…C2E1F` | `TryFocusEnemy` → `Submit` |
| `WavePreview.cs` | `9dec1bf48b404c72f9ebb175f63c2ecaa6100b4b` | `EAE7C17B…2997B7` | （不再写 `HoldSim`） |
| `NaturalPlayBattleEvidence.cs` | `2a4d80cb1fce216b4ba8616d800075c964902be6` | `387FDA21…63312B` | 工厂传入 recorded mods |

C4 无 filter TRX / 三组自然操作 / 读回绑定本表 HEAD，不是 a82801b 原件上的 A1 244/0。
