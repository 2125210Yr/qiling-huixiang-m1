# O3 encounter input contract

`EliteEncounterDef` freezes the N4 encounter's separate behavior owner. Default design candidates are version `prompter-v1`, caster slot 0 (`OE_PROMPTER`), auto `OE_PROMPTER_auto`, area `OE_PROMPTER_tap`, first intent 6 seconds, cast 3 seconds, subsequent intent interval 12 seconds, and auto interval 3 seconds. The definition is persisted within `ExpeditionBattleInput.Elite`; N4 requires it and other nodes do not accept it.

N7 retains version `white-conductor-v1` and exactly three fixed enemy slots: `OE_BOSS`, `OE_MASK`, `OE_MASK`. Its existing damage, HP, masks, and timings are unchanged. Both scripted encounter types require a single-target enemy-facing Auto damage skill and a five-target `AllEnemies` area damage skill.

The content fingerprint must include the authored elite definition. The pre-O3 content hash read from the O2 test assembly was:

```text
03a8cc088289181ba9ba9c2f2cdee6de52b1bd6e191aabdd86b5d8d7638c3437
```

Adding the elite definition intentionally changes content compatibility. Existing profiles/checkpoints that carry the previous content hash must use the existing compatibility recovery UI. This work does not migrate, reset, or delete user profiles. `SchemaVersion` and the existing numerical candidate values remain unchanged.

Actual validation: the nine input cases in `o3-combined-red.trx` produced eight failures and one pass before implementation. `input-green.trx` subsequently passed all nine. The new compiled content hash is `5be148f01a9107e6c6d149cf95798e5e57d810832b071f0e237268398e7506f6`.
