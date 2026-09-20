# O3 same-opening retry strategy fixture

`OriginalBossRetryStrategyTests.SamePersistedBossOpeningNaturallyLosesThenWinsWithLegalCommands` persists an explicitly artificial N6 state using the real `OriginalProfileStore`. Its four earlier settled battles are fixture history. It does not claim to have played N0–N5 or provide ordinary UI/video evidence.

The fixture uses run seed 260921, preset `single`, and the legally retained initial core `C01`; the other three rewards are recorded as declined. No battle object or frozen combat input is modified. `BeginBattle` itself derives and persists the N7 seed, opening party HP, content, boss rules, relics, and identities.

The first attempt calls only normal `Tick()` until the real terminal outcome. The test requires natural all-party defeat and no submitted player commands. Actual ending HP is passed to `CompleteBattle`. A new Flow/Store instance then reloads that persisted defeat and calls `RetryBattle`.

Every public `ExpeditionBattleInput` field is separately serialized and SHA-256 compared. Only `AttemptId` may change; the full input JSON also must match after normalizing that one field. This includes nested character/skill/effect stats, opening HP, growth, relic parameters, clocks, boss/elite definitions, seed, versions, IDs, and mode flags. Raw hashes of both input instances and the shared normalized hash are emitted in the raw test output.

The second attempt uses only accepted `Submit` commands: target the living mask with lowest HP, use guardian Tap when the authoritative area warning has at most 0.2 seconds left, heal if a living teammate is at or below 80% HP, and use other ready primary skills. This is a deterministic test policy, not a human playthrough. Both runs leave `ForceNoCrit` false, do not change time scale or seed, and never set in-battle HP/charge.

Assertions cover actual Defeat→Victory, real mask kill records, accepted shield/heal commands, equal max/opening HP, no reward redraft or unlock on defeat/retry, unchanged original checkpoint, rejection of the old attempt's settlement, and a single win settlement/unlock. Exact game times, outcomes, counters, both raw input hashes, per-field hashes, and final HP are printed for the parent's TRX/log capture.

Executed successfully in `../o4/retry-comparison.trx` (combined with the separate N5 comparison). Natural idle attempt: Defeat at 55.0 game seconds, all five allies at 0 HP. Legal-command retry: Victory at 90.2 seconds, phase 2, two actual mask rebuilds, seven accepted shield commands and three accepted heal commands. Both use battle seed 1302221392 and content hash `5be148f01a9107e6c6d149cf95798e5e57d810832b071f0e237268398e7506f6`. Normalized frozen input SHA-256: `c2f7340b1b78a4dd62b59d15c193d50a2438450d761b0bc3f36a27d4e945d977`. This verifies the combat and persisted retry contract, not ordinary UI/video delivery.
