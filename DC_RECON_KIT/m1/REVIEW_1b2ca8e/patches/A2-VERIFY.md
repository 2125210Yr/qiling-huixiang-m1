# A2-VERIFY — GameRoot.StartBattleAt playable note

**task_id:** `B1-A2`  
**batch:** `B1`  
**baseline:** `1b2ca8e4ff56d9f0986e158fc97b50498f735cb3`  
**Lease writes:** `VerificationCatalog.cs`, `G2B1VerificationEntryTests.cs`, this file.  
**Do not implement here:** `BattleSim.cs`, `BattleHud.cs`, `GameRoot.cs`, `G2A53*`, `CatalogJson.cs`, `BattleReplay.cs`, EffectCapability playable tables.  
**Do not** implement Dot / Reflect. Do not inject HP / Drive / Charge / Fever. Do not skip Slide / Fever. Do not weaken A53 T01/T02.

Evidence: ENGINEERING. Not GL.

---

## 0. What Apply already binds (this lease, landed)

`VerificationCatalog.Apply` now, after cloning builtin skills and **before** `Catalog.Install`:

1. Keeps the existing charge / drive / stage scalar overlay.
2. Calls `PlayableSubstitutes.CreateStripDotFlameOverlays` on the **cloned** skill map and writes those clones back into the map (`EffectId=null` on every `dot_flame` link). Named substitute: `A53_SUB_STRIP_DOT_FLAME` (`PlayableSubstitutes.StripDotFlame`). Damage-only. **Does not implement Dot.**
3. Records the name on `InstalledMode` as `{planMode}+A53_SUB_STRIP_DOT_FLAME` and on the return string as `sub=A53_SUB_STRIP_DOT_FLAME`.
4. After Install, calls `Catalog.EvaluatePartyPlayable(DefaultParty, each verification stage)` (non-throwing) and appends `roster=ok` or `roster={Summary()}`. Never throws from `Apply`. Never calls `EnsurePartyPlayable`. Catalog static ctor is unchanged.

`RestoreBuiltin` still `BuildBuiltin()` + `TryLoadDefault()`. Production `C001_slide.EffectId` returns to `dot_flame` and is unplayable again. `Catalog.BuildBuiltin()` without Apply keeps the historical inventory.

NaturalPlay already calls `Apply` **before** FIGHT. When `VerificationCatalog.Installed` is true, `StartBattleAt` must **proceed** — the catalog already has the named substitute. Do **not** bind `PlayableSubstitutes` again. Do **not** auto-apply that substitute on the normal player path.

---

## 1. Defect (source, 1b2ca8e)

`StartBattleAt` (~L1162) builds `BattleSim(_save.PartyIds, …, stage)` with DefaultParty `C001` at p0 and whatever stage table is live. It never evaluates playable roster and never binds `A53_SUB_STRIP_DOT_FLAME`.

Natural basic still requires Player Tap+Slide slot 0. Builtin `C001_slide` → `dot_flame` is correctly Unplayable before spend (A53 gate). Charge / HP waits are not a fix.

After this lease, the **verification** path is closed by Apply. The **normal** player path must still surface the missing dependency instead of painting Slide as ready.

---

## 2. GameRoot.StartBattleAt hunk (integrator only)

Insert after `var stage = table[index];` and the existing `BattleMods` block, **before** `new BattleSim(...)`. Stamp `FailedReason` / `LastEvent` on the new sim **before** `BattleInitialHeader.Freeze` / `Show(ScreenId.Battle)`.

```csharp
        void StartBattleAt(int index)
        {
            var table = Catalog.Chapter(_save.UseHard);
            if (table == null || table.Length == 0)
            {
                table = Catalog.Stages;
                _save.UseHard = false;
            }
            if (index < 0) index = 0;
            if (index >= table.Length) index = table.Length - 1;
            if (StageLocked(index))
            {
                while (index > 0 && StageLocked(index)) index--;
            }
            if (StageLocked(index)) return;
            _activeStage = index;
            _save.LastSeed = unchecked(Environment.TickCount);
            var stage = table[index];
            var mods = new BattleMods
            {
                FoodAtkMul = Food.AtkMulOf(_save.Meal),
                CartaMul = PvpRules.CartaMulIfPvp(_save.PvpDoor)
            };

            // B1-A2: verification catalog already has A53_SUB_STRIP_DOT_FLAME.
            // Normal path: report roster holes before fight. Do not bind substitutes.
            // Do not inject HP/Drive/Charge. Do not skip Slide/Fever.
            string playableNote = null;
            if (!VerificationCatalog.Installed)
            {
                var gate = Catalog.EvaluatePartyPlayable(_save.PartyIds, stage, null, null);
                var blocking = gate != null ? gate.PlayableRosterViolations() : null;
                if (blocking != null && blocking.Count > 0)
                    playableNote = gate.Summary();
            }

            _battle = new BattleSim(_save.PartyIds, _save.LeaderSlot, _save.LastSeed, stage, _save.ProgressForParty(), mods)
            {
                Speed = _save.Speed,
                Auto = _save.Auto,
                ForceNoCrit = false,
                Profile = FormulaProfile.JP_LEGACY_EMPIRICAL
            };
            if (!string.IsNullOrEmpty(playableNote))
            {
                _battle.FailedReason = playableNote;
                _battle.LastEvent = playableNote;
            }
            BattleInitialHeader.Freeze(_battle, _save.PartyIds, stage != null ? stage.Id : "");
            _simAcc = 0f;
            Show(ScreenId.Battle);
        }
```

Rules for this hunk:

| Allowed | Forbidden |
|---|---|
| If `VerificationCatalog.Installed`, proceed with no extra bind | `PlayableSubstitutes.Bind` / `CreateStripDotFlameOverlays` on the player path |
| `EvaluatePartyPlayable` + `FailedReason` / `LastEvent` when not installed | `EnsurePartyPlayable` (throws; Unity boot / Editor smoke must stay up) |
| Keep constructing the sim so Tap / legal skills still run | Inject HP / Drive / Charge / Fever / time / Perfect |
| Keep Slide / Fever commands on the real UI path | Skip Slide / Fever to go green |
| Leave Outcome `InProgress` unless a later lease says otherwise | Treat Charge≥100 as “Slide ready” when `C001_slide` is unplayable |

HUD `RefreshPortraits` today sets `slideReady = Charge>=100 && SlideCd<=0` and shows `SLIDE READY`. That is **not** playable-ready. This lease cannot edit `BattleHud.cs`. After the hunk lands, a HUD follow-up (separate lease) must AND `Catalog.IsPlayable` / `CheckSkill` on the slot’s Slide before painting the pip. Until then, `LastEvent` / `FailedReason` is the required pre-fight surface — do not hide it.

Do **not** add this evaluate-and-stamp to `Catalog` static ctor or `BuildBuiltin`.

---

## 3. Acceptance

| Path | After Apply + this hunk |
|---|---|
| NaturalPlay `Apply` then `StartBattleAt` | Installed; catalog has named substitute; p0 Slide Accepted (see `G2B1VerificationEntryTests`) |
| Normal player, C001 + VS-1, not Installed | `EvaluatePartyPlayable` lists `C001_slide`+`dot_flame` (and E001/E003 slides); `LastEvent`/`FailedReason` set before fight; Submit Slide still Unplayable; A53 gate unchanged |
| `RestoreBuiltin` / `BuildBuiltin` without Apply | `C001_slide.EffectId==dot_flame`, not playable |
| A53 T01 / T02 | untouched |

---

## 4. Compile / test (Apply lease)

`dotnet test tools/BattleSim.Tests/BattleSim.Tests.csproj --filter "FullyQualifiedName~G2B1VerificationEntryTests|FullyQualifiedName~G2A53PlayableGateTests.T01|FullyQualifiedName~G2A53PlayableGateTests.T02"`

Passed 6 / failed 0 (4 G2B1 + A53 T01 + T02). Compile succeeded (2 unrelated xUnit2000 warnings in `M1FormulaIsolationTests`). Full suite / Unity / G3 = `NOT_RUN`. GameRoot is not edited in B1-A2.
