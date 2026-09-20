# Legacy Catalog contamination probe

Run from the repository root:

```powershell
dotnet run --project 'docs/original-expedition/evidence/o2/legacy-catalog-probe/LegacyCatalogProbe.csproj'
```

This small console references the existing test project and calls existing test methods in a fixed order, in one process. It does not modify their source or the production source. `BuildBuiltin` is internal, so the probe invokes it through reflection.

Observed sequence in `probe.log`:

1. `Catalog.BuildBuiltin` then `M1CoreSliceTests.PoisonTriggersOnActionAndHitTakenNotPerSecond`: PASS.
2. `BattleSimTests.CatalogJsonSerializeRoundtripKeepsOpcode`: completes, leaving `C001_tap.Opcode=dmg.fever_parts` and `stun.Opcode=poison.apply` in global Catalog.
3. The same poison test: FAIL at line 132, Expected 1800 / Actual 0. Applying the polluted stun also changes its shared Kind from Stun to Poison.
4. `Catalog.BuildBuiltin` then the same poison test: PASS.

The probe returns exit code 0 only when that pass/fail/pass sequence is observed. This is evidence of the pre-existing test isolation defect, not a passing full test suite.

The polluting test restores neither Catalog entry in its `finally` block; it only deletes its own temporary JSON file (`BattleSimTests.cs:379-406`). `M1CoreSliceTests.NewSim` does not reset Catalog. `M1ClockModeTests.LockFoes` and `LockOtherAllies` (`:346-358`) are consumers of the shared stun definition, not independent assignments to its opcode. `BattleSim.SettleEffect` converts a non-DOT kind to Poison when its opcode is `poison.apply`, explaining the observed Kind mutation and loss of the stun lock. Magnitude remains 1, so this polluted status represents full-MaxHP poison damage.

No unrelated production or existing test repair was made for this probe.
