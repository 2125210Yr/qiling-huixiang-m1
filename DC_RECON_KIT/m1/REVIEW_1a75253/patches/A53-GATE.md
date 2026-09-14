# A53-GATE — playable skill closure before spend

**task_id:** `A53-GATE`  
**batch:** `A53`  
**baseline:** `1a752532e39560b861c2a661db66c97ca18ffece`  
**Lease:** `EffectCapability.cs`, `Catalog.cs` (playable closure / `FilterPlayable*` / public gates), this file.  
**Do not** implement here: `BattleSim.cs`, `BattleHud.cs`, `GameRoot.cs`, `G2Recheck*`, `G2A53*`, `CatalogJson.cs`, `BattleReplay.cs`.  
**Do not** implement Dot / Reflect consumers.  
**Evidence:** ENGINEERING. Not GL.

Closes REVIEW A53-01 (catalog vs fight path). Keeps 1a75253 TargetSemantics, enemy Silence, atomic Catalog, QTE single debit, Measured, AutoFire Submit.

---

## 0. Defect (source, 1a75253)

`InstallTables` already builds `PlayableSkills` / `PlayableEffects`, but the fight path does not use that closure:

1. `BattleSim.ResolveSkill` → `Catalog.TrySkill` (full inventory).
2. `UsePlayerSkill` (~L817) zeros Charge, adds Drive, then `Cast`.
3. `Cast` settles damage via `ExecuteOpcode`, then `ApplyLinkedEffect` `Check`s the effect.
4. Missing / unplayable linked effect only `NoteEvent`s (`missing_effect` / `unplayable_effect`) and returns.
5. Overlay import `baseline.ContainsKey(id)` lets historical `atk_up` retargeted to Reflect stay in inventory. That row is still `Cast` today.

Result: player can `Accepted` + spend + deal damage with a truncated skill.

---

## 1. What landed in this lease (source)

Inventory (`Catalog.Skills` / `Effects` / `TrySkill` / `TryEffect`) still holds historical rows. Unity startup still `BuildBuiltin()` + `TryLoadDefault()` and does **not** throw because `C001_slide` links inventory-only `dot_flame`.

Playable skill = `EffectCapability.CheckSkill(sk, effects, overlays).Ok`:

- skill channel opcode has an Implemented row
- if `EffectId` is set, the linked row exists after overlay resolve **and** `Check(fx).Ok`
- missing or unplayable `EffectId` ⇒ not playable
- same id with a **new** unsupported kind is re-validated; `ContainsKey(id)` is not a playable grant

`InstallTables` still calls `FilterPlayableEffects` / `FilterPlayableSkills`. Those filters ignore historical id existence.

`Catalog.PlayableIds` is unchanged (non-enemy inventory roster, still 25). Archive / TeamBoard keep listing C001. Fight entry is `EnsurePartyPlayable`, not this list.

### Public integrator gates

| Gate | Throws? | Use |
|---|---|---|
| `Catalog.IsPlayable(SkillDef)` / overlay overload | no | single skill + current `Effects` |
| `Catalog.IsPlayable(CharacterDef)` / overlay overload | no | all declared auto/tap/slide/drive/leader |
| `Catalog.TryGetPlayableSkill` / `TryGetPlayableEffect` | no | live playable roster only (no overlay) |
| `Catalog.EvaluatePartyPlayable(party, enemies\|stage, skillOverlays, effectOverlays)` | no | report |
| `Catalog.EnsurePartyPlayable(...)` | yes `ContentValidationException` | verification / pre-battle only |
| `EffectCapability.CheckSkill(sk, effects, effectOverlays)` | no | fight path (overlay wins, re-Check) |
| `EffectCapability.FightRejectReason(sk, verdict)` | no | `FailedReason` / log text |
| `EffectCapability.LookupSkill` / `LookupEffect` | no | overlay-then-inventory |

Do **not** call `EnsurePlayable(skill)` / `EnsurePartyPlayable` from `Catalog` static ctor, `GameRoot` boot, or `BuildBuiltin`. Builtin `C001_slide`+`dot_flame` would throw and the app would not start.

Do **not** call `RejectUnplayable` / `EnsurePlayable` inside `Cast` after Charge/Drive already moved. Check first; reject without throw when a legal auto on the same tick must continue.

### C001_slide + `dot_flame` (builtin)

| Surface | Behavior after this lease |
|---|---|
| `Catalog.TrySkill("C001_slide")` / `TryEffect("dot_flame")` | still present (inventory visible) |
| `Catalog.TryGetPlayableSkill("C001_slide")` | false |
| `Catalog.TryGetPlayableEffect("dot_flame")` | false |
| `Catalog.IsPlayable(C001_slide)` | false (`Check` Dot = Registered_NotImplemented) |
| `Catalog.IsPlayable(C001)` | false (slide link) |
| `Catalog.ValidateContent().BlockingViolations()` | still empty (start-safe historical kind-gap) |
| `EnsurePartyPlayable(DefaultParty, VS-1 waves)` | throws (C001_slide + E001/E003/E005 slides) |
| Unity boot | still succeeds |

After the BattleSim hunks below: `C001` tap / auto / drive / leader still fire (those rows are playable). `C001_slide` is `CommandReject.Unplayable` **before** Charge dump / DriveGain / damage. Auto policy Submit of Slide is the same reject — it must not abort a later legal Tap on another slot.

### Named developer / verification substitutes

Never auto-applied. Caller must bind one of these by name.

| Name | API | What it does |
|---|---|---|
| `A53_SUB_STRIP_DOT_FLAME` | `PlayableSubstitutes.StripDotFlame` + `CreateStripDotFlameOverlays(Catalog.Skills)` | Overlay every skill whose `EffectId=="dot_flame"` with a clone that has `EffectId=null`. Damage-only. Does **not** implement Dot. |
| `A53_SUB_PLAYABLE_PARTY` | `PlayableSubstitutes.PlayableParty` / `PartyIds` | `C006,C007,C003,C008,C009` — no declared Dot links. |
| `A53_SUB_PLAYABLE_WAVE` | `PlayableSubstitutes.PlayableWave` / `EnemyIds` | `E002,E004` — defender + healer kits. |

A53-T02 (`atk_up` overlay kind=Reflect): inventory import may still tolerate the historical id. `IsPlayable(atk_up overlay)` is false; every skill that still links `atk_up` is not playable; `EnsurePartyPlayable` fails. There is **no** `A53_SUB_*` that grants playable inheritance from the old name. To fight anyway, overlay the **skill** (`EffectId=null` or a playable effect id) or restore `atk_up` to `AtkBuff`.

---

## 2. BattleSim hunks (integrator applies)

All checks use current overlay maps **and** inventory effects:

```csharp
CapabilityVerdict CheckFightSkill(SkillDef sk)
{
    return EffectCapability.CheckSkill(sk, Catalog.Effects, _effectOverlay);
}

bool FightSkillReady(SkillDef sk)
{
    return sk != null && CheckFightSkill(sk).Ok;
}
```

`ResolveSkill` may still read inventory (`TrySkill`) so overlays and historical rows resolve. **Playability is the Check, not `PlayableSkills.ContainsKey`** — an overlay skill is never in the catalog playable table.

Do not implement Dot/Reflect in `SettleEffect`.

### 2.1 `CommandReject` + `DriveResolveResult` (`BattleSim.Commands.cs` / L185)

```csharp
public enum CommandReject
{
    None, NotInProgress, Paused, QtePending, NoQtePending, SlotInvalid, UnitDead,
    ActionLocked, Silenced, NotCharged, SlideOnCooldown, DriveNotReady, FeverNotActive, FeverThrottled,
    FeverBudgetExhausted, AutoOwnsInput, InvalidValue,
    Unplayable
}

public enum DriveResolveResult { Accepted, NoPending, NotInProgress, Paused, Unplayable }
```

Append only. Replay `ParseEnum` already defaults unknown tokens to `None`.

### 2.2 `UsePlayerSkill` (~L817) — Check **before** Charge / SlideCd / DriveGain / Cast

Replace:

```csharp
            var skill = ResolveSkill(id);
            if (skill == null) return false;
            u.Charge = 0f;
            if (type == SkillType.Slide) u.SlideCd = SlideCdDurationSec;
            Drive = Math.Min(100f, Drive + skill.DriveGain);
            Cast(u, true, skill, 1f);
```

with:

```csharp
            var skill = ResolveSkill(id);
            if (skill == null) return false;
            if (!FightSkillReady(skill)) return false;
            u.Charge = 0f;
            if (type == SkillType.Slide) u.SlideCd = SlideCdDurationSec;
            Drive = Math.Min(100f, Drive + skill.DriveGain);
            Cast(u, true, skill, 1f);
```

`AutoFireSkills` already `Submit`s Tap/Slide. This reject is the AutoFire gate. Do not throw.

### 2.3 `Execute` Tap / Slide / DriveBegin (`BattleSim.Commands.cs` ~L141)

Surface a clear reject (not `InvalidValue`) **before** `TryTap`/`TrySlide`/`TryBeginDrive` spend:

```csharp
                case BattleCommandKind.Tap:
                {
                    if (!CanAcceptSkillInput(cmd.Slot, out var r)) return r;
                    if (Allies[cmd.Slot].Charge < 100f) return CommandReject.NotCharged;
                    var tapId = Allies[cmd.Slot].Def != null ? Allies[cmd.Slot].Def.TapSkillId : null;
                    var tap = ResolveSkill(tapId);
                    if (!FightSkillReady(tap)) return CommandReject.Unplayable;
                    return TryTap(cmd.Slot) ? CommandReject.None : CommandReject.InvalidValue;
                }
                case BattleCommandKind.Slide:
                {
                    if (!CanAcceptSkillInput(cmd.Slot, out var r)) return r;
                    if (Allies[cmd.Slot].Charge < 100f) return CommandReject.NotCharged;
                    if (Allies[cmd.Slot].SlideCd > 0f) return CommandReject.SlideOnCooldown;
                    var slideId = Allies[cmd.Slot].Def != null ? Allies[cmd.Slot].Def.SlideSkillId : null;
                    var slide = ResolveSkill(slideId);
                    if (!FightSkillReady(slide)) return CommandReject.Unplayable;
                    return TrySlide(cmd.Slot) ? CommandReject.None : CommandReject.InvalidValue;
                }
                case BattleCommandKind.DriveBegin:
                {
                    if (!CanAcceptSkillInput(cmd.Slot, out var r)) return r;
                    if (Drive < 100f) return CommandReject.DriveNotReady;
                    var driveId = Allies[cmd.Slot].Def != null ? Allies[cmd.Slot].Def.DriveSkillId : null;
                    var drive = ResolveSkill(driveId);
                    if (!FightSkillReady(drive)) return CommandReject.Unplayable;
                    return TryBeginDrive(cmd.Slot) ? CommandReject.None : CommandReject.InvalidValue;
                }
                case BattleCommandKind.DriveResolve:
                {
                    switch (ResolveDriveChecked(cmd.Timing))
                    {
                        case DriveResolveResult.Accepted: return CommandReject.None;
                        case DriveResolveResult.NoPending: return CommandReject.NoQtePending;
                        case DriveResolveResult.Paused: return CommandReject.Paused;
                        case DriveResolveResult.Unplayable: return CommandReject.Unplayable;
                        default: return CommandReject.NotInProgress;
                    }
                }
```

Failure = `Accepted=false`, `Reason=Unplayable`. No Charge / Drive / damage.

### 2.4 `TryBeginDrive` (~L589) — reject before pending QTE

```csharp
        public bool TryBeginDrive(int slot)
        {
            if (!CanAcceptSkillInput(slot, out _)) return false;
            if (Drive < 100f) return false;
            var unit = Allies[slot];
            var drive = unit != null && unit.Def != null ? ResolveSkill(unit.Def.DriveSkillId) : null;
            if (!FightSkillReady(drive)) return false;
            PendingDriveSlot = slot;
            _qteElapsed = 0f;
            LastEvent = Allies[slot].Def.Name + " 准备 Drive";
            if (Auto == AutoMode.Full)
                return ResolveDrive(DriveTiming.Great);
            return true;
        }
```

### 2.5 `ResolveDriveChecked` (~L602) — Check **before** Drive dump / Charge dump / Cast

Current order clears `PendingDriveSlot`, zeros `Drive`, then looks up the skill (null still `Accepted`). Replace the body after the three early returns:

```csharp
            var slot = PendingDriveSlot;
            var unit = Allies[slot];
            var skill = unit != null && unit.Def != null ? ResolveSkill(unit.Def.DriveSkillId) : null;
            if (!FightSkillReady(skill))
            {
                PendingDriveSlot = -1;
                _qteElapsed = 0f;
                LastEvent = EffectCapability.FightRejectReason(skill, CheckFightSkill(skill));
                return DriveResolveResult.Unplayable;
            }
            PendingDriveSlot = -1;
            _qteElapsed = 0f;
            Drive = 0f;
            LastDriveTiming = timing;
            LastDriveResolveTick = TickIndex;
            LastDriveResolveSlot = slot;
            var mul = TimingDamage(timing);
            AddFever(TimingFever(timing));
            unit.Charge = 0f;
            Cast(unit, true, skill, mul);
            LastEvent = "DRIVE  " + unit.Def.Name + "  " + skill.Name + "  " + timing;
            return DriveResolveResult.Accepted;
```

Drive gauge is **not** spent on Unplayable. Fever is not added. Do not throw (would abort a legal AutoFire Tap later in the same `Tick`).

### 2.6 `TickUnit` auto (~L655) — Check **before** AutoTimer dump / Cast / DriveGain

Replace:

```csharp
            if (u.AutoTimer >= autoCd)
            {
                u.AutoTimer = 0f;
                var autoSkill = ResolveSkill(u.Def.AutoSkillId);
                if (autoSkill != null)
                {
                    Cast(u, ally, autoSkill, 1f);
                    if (ally)
                    {
                        var gain = autoSkill.DriveGain;
                        // ... HonorDeclaredAutoDriveGain ...
                        Drive = Math.Min(100f, Drive + gain);
                    }
                }
            }
```

with:

```csharp
            if (u.AutoTimer >= autoCd)
            {
                var autoSkill = ResolveSkill(u.Def.AutoSkillId);
                if (autoSkill != null && !FightSkillReady(autoSkill))
                {
                    u.AutoTimer = 0f; // prevent spin; no Cast, no DriveGain
                }
                else
                {
                    u.AutoTimer = 0f;
                    if (autoSkill != null)
                    {
                        Cast(u, ally, autoSkill, 1f);
                        if (ally)
                        {
                            var gain = autoSkill.DriveGain;
                            var overlaid = !string.IsNullOrEmpty(u.Def.AutoSkillId)
                                && _skillOverlay != null
                                && _skillOverlay.ContainsKey(u.Def.AutoSkillId);
                            if (!DesignPlaceholderPolicy.HonorDeclaredAutoDriveGain && !overlaid)
                                gain = Math.Max(14, gain);
                            Drive = Math.Min(100f, Drive + gain);
                        }
                    }
                }
            }
```

Builtin `C001_auto` has no `EffectId` and stays playable. An overlay that attaches Reflect/Dot is skipped without throwing, so a legal enemy tap on the same tick can still run.

### 2.7 Enemy declared Cast (~L680) — Check **before** Charge dump / SlideCd / Cast

```csharp
            if (!ally && u.Charge >= 100f)
            {
                if (!CanAcceptSkillInput(false, u.Slot, out _))
                    return;
                var wantSlide = _rng.NextDouble() >= 0.65 && u.SlideCd <= 0f;
                var skill = ResolveSkill(wantSlide ? u.Def.SlideSkillId : u.Def.TapSkillId);
                if (skill == null) return;
                if (!FightSkillReady(skill)) return; // keep Charge; next tick may pick the other declared skill
                u.Charge = 0f;
                if (wantSlide) u.SlideCd = SlideCdDurationSec;
                Cast(u, false, skill, 1f);
            }
```

Builtin `E001_slide` / `E003_tap` / `E005_slide` link `dot_flame`. They no longer deal slide/tap damage with the effect stripped. Charge stays so a later playable tap can fire. Do not `Failed` the whole battle here (would kill a legal ally auto). Strict entry is `EnsurePartyPlayable`.

### 2.8 `Cast` (~L920) — last line of defense, **no** opcode / damage / linked apply

Insert immediately after the null-skill fail, **before** `_execSkill = skill`:

```csharp
            if (!FightSkillReady(skill))
            {
                var msg = EffectCapability.FightRejectReason(skill, CheckFightSkill(skill));
                NoteEvent("unplayable_skill", skill.Id ?? "", caster, null, 0, skill.Type);
                LastEvent = msg;
                return;
            }
```

Callers in 2.2–2.7 must have checked already so this path is idle. If a future caller forgets, this still must **not** `ExecuteOpcode` then skip the effect. Do not throw (aborts the Tick). Do **not** set `Outcome=Failed` here for a single auto miss; player commands already returned `CommandReject.Unplayable`.

If the integrator wants a hard battle fail for a **declared** skill that reached Cast after spend (should be unreachable), set `Outcome=Failed` / `FailedReason=msg` only in a debug build. Default: return.

### 2.9 `ApplyLinkedEffect` (~L959) — no longer a silent success path

After 2.8, a fight `Cast` only reaches here when `CheckSkill` passed, so `ResolveEffect` + `Check` should succeed. Replace the note-and-return arms so a mismatch cannot look like a successful truncated skill:

```csharp
            var fx = ResolveEffect(skill.EffectId);
            if (fx == null)
            {
                Outcome = BattleOutcome.Failed;
                FailedReason = "UNPLAYABLE_SKILL " + skill.Id + " missing_effect " + skill.EffectId;
                LastEvent = FailedReason;
                NoteEvent("missing_effect", skill.EffectId, caster, null, 0, skill.Type);
                return;
            }
            var verdict = EffectCapability.Check(fx);
            if (!verdict.Ok)
            {
                Outcome = BattleOutcome.Failed;
                FailedReason = EffectCapability.FightRejectReason(skill, verdict);
                LastEvent = FailedReason;
                NoteEvent("unplayable_effect", fx.Opcode ?? "", caster, null, (int)fx.Kind, skill.Type);
                return;
            }
```

This `Failed` is only for the inconsistent path (overlay changed between Check and Apply). The happy path is unchanged. Still no Dot/Reflect implementation.

### 2.10 `ApplyLeader` (~L1551) — Check before Cast

```csharp
            var skill = ResolveSkill(leader.Def.LeaderSkillId);
            if (!FightSkillReady(skill)) return;
            Cast(leader, true, skill, 1f);
```

Constructor must **not** throw. Unplayable leader is skipped (no partial apply). Builtin `C001_leader` → `atk_up` stays playable unless an overlay retargets `atk_up`.

### 2.11 Optional ctor / wave entry (do **not** put on Unity boot)

After overlays are bound, verification may:

```csharp
Catalog.EnsurePartyPlayable(partyIds, _stage, _skillOverlay, _effectOverlay);
```

or the non-throwing report:

```csharp
var gate = Catalog.EvaluatePartyPlayable(partyIds, _stage, _skillOverlay, _effectOverlay);
if (gate.PlayableRosterViolations().Count > 0)
{
    Outcome = BattleOutcome.Failed;
    FailedReason = gate.Summary();
}
```

**Do not** add this to `BattleSim(string[] partyIds, ...)` unconditionally. Default `C001` + VS-1 (`E001,E002,E003` / `EBOSS`) fails the closure (`dot_flame` slides). That would break the vertical slice and existing tests. Bind `A53_SUB_STRIP_DOT_FLAME` or `A53_SUB_PLAYABLE_PARTY` + `A53_SUB_PLAYABLE_WAVE` first when the caller wants a closed kit.

`GameRoot` must not call `EnsurePartyPlayable` at startup.

---

## 3. Acceptance mapping

| Case | After hunks |
|---|---|
| A53-T01 damage skill + `status.apply`+Reflect | `CheckSkill` fails; Submit `Unplayable`; Charge/Drive/HP unchanged |
| A53-T02 historical `atk_up` kind=Reflect | inventory may keep the row; not in `PlayableEffects`; skills linking it not in `PlayableSkills`; fight Check uses overlay row; no Accepted+partial |
| Builtin `C001_slide` | inventory yes; playable no; Slide command rejected before spend; app boots |
| Legal `C001_tap` / `C001_auto` | still Accepted; AutoFire must not throw because slide is unplayable |

---

## 4. Compile / test (this session)

Isolated `G2ReviewCapabilityTests` only. Do not weaken old assertions. Full suite / Unity / G3 = `NOT_RUN` here.
